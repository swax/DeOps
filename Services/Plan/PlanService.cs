using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;

using DeOps.Services.Transfer;
using DeOps.Services.Trust;
using DeOps.Services.Location;
using DeOps.Services.Assist;


namespace DeOps.Services.Plan
{
    internal delegate void PlanUpdateHandler(OpPlan plan);


    internal class PlanService : OpService
    {
        public string Name { get { return "Plan"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.Plan; } }

        const uint DataTypeFile = 0x01;

        
        internal OpCore      Core;
        internal G2Protocol  Protocol;
        internal DhtNetwork  Network;
        internal DhtStore    Store;
        internal TrustService Trust;

        internal OpPlan LocalPlan;

        int RunSaveLocal;
        int SaveInterval = 60*10; // 10 min stagger, prevent cascade up

        internal ThreadedDictionary<ulong, OpPlan> PlanMap = new ThreadedDictionary<ulong, OpPlan>();

        internal event PlanUpdateHandler PlanUpdate;

        internal VersionedCache Cache;


        internal PlanService(OpCore core)
        {
            Core = core;
            Network = core.Network;
            Protocol = Network.Protocol;
            Store = Network.Store;
            Trust = Core.Trust;
            
            if (Core.Sim != null)
                SaveInterval = 30;
            
            Core.SecondTimerEvent += new TimerHandler(Core_SecondTimer);

            Cache = new VersionedCache(Network, ServiceID, DataTypeFile, false);  
         
            Cache.FileAquired += new FileAquiredHandler(Cache_FileAquired);
            Cache.FileRemoved += new FileRemovedHandler(Cache_FileRemoved);
            Cache.Load();
 
            if (!PlanMap.SafeContainsKey(Core.UserID))
            {
                LocalPlan = new OpPlan(new OpVersionedFile(Core.User.Settings.KeyPublic));
                LocalPlan.Init();
                LocalPlan.Loaded = true;
                PlanMap.SafeAdd(Core.UserID, LocalPlan);
            }
        }

        public void Dispose()
        {
            Core.SecondTimerEvent -= new TimerHandler(Core_SecondTimer);

            Cache.FileAquired -= new FileAquiredHandler(Cache_FileAquired);
            Cache.FileRemoved -= new FileRemovedHandler(Cache_FileRemoved);
            Cache.Dispose();
        }

        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong user, uint project)
        {
            if (menuType == InterfaceMenuType.Internal)
            {
                menus.Add(new MenuItemInfo("Plans/Schedule", PlanRes.Schedule, new EventHandler(Menu_ScheduleView)));
                menus.Add(new MenuItemInfo("Plans/Goals", PlanRes.Goals, new EventHandler(Menu_GoalsView)));
            }

            if (menuType == InterfaceMenuType.External)
            {
                menus.Add(new MenuItemInfo("Schedule", PlanRes.Schedule, new EventHandler(Menu_ScheduleView)));
                menus.Add(new MenuItemInfo("Goals", PlanRes.Goals, new EventHandler(Menu_GoalsView)));
            }
        }

        void Menu_ScheduleView(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            ScheduleView view = new ScheduleView(this, node.GetUser(), node.GetProject());

            Core.InvokeView(node.IsExternal(), view);
        }

        void Menu_GoalsView(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            GoalsView view = new GoalsView(this, node.GetUser(), node.GetProject());

            Core.InvokeView(node.IsExternal(), view);
        }

        void Core_SecondTimer()
        {
            // triggered on update estimates use time out so update doesnt cascade the network all the way up
            //  our save local can cause other save locals
            if (RunSaveLocal > 0)
            {
                RunSaveLocal--;

                if (RunSaveLocal == 0)
                    SaveLocal();
            }
        }

        public void SimTest()
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { SimTest(); });
                return;
            }

            uint project = 0;

            // schedule
            PlanBlock block = new PlanBlock();

            block.ProjectID = project;
            block.Title = Core.TextGen.GenerateWords(1)[0];
            block.StartTime = Core.TimeNow.AddDays(Core.RndGen.Next(365)); // anytime in next year
            block.EndTime = block.StartTime.AddDays(Core.RndGen.Next(3, 90)); // half a week to 3 months
            block.Description = Core.TextGen.GenerateSentences(1)[0];
            block.Scope = -1;
            block.Unique = Core.RndGen.Next();

            // add to local plan
            LocalPlan.AddBlock(block);

    
            // goals

            // get  uplinks including self and scan all goals for  anything assigned to local
            List<ulong> uplinks = Core.Trust.GetUplinkIDs(Core.UserID, project);
            uplinks.Add(Core.UserID);
               
            List<PlanGoal> assignedGoals = new List<PlanGoal>();

            PlanMap.LockReading(delegate()
            {
                foreach (ulong uplink in uplinks)
                    if (PlanMap.ContainsKey(uplink) && PlanMap[uplink].GoalMap != null)
                        foreach (List<PlanGoal> list in PlanMap[uplink].GoalMap.Values)
                            foreach (PlanGoal goal in list)
                                if (goal.Person == Core.UserID)
                                    assignedGoals.Add(goal);
            });


            PlanGoal randGoal = null;
            if (assignedGoals.Count > 0)
                randGoal = assignedGoals[Core.RndGen.Next(assignedGoals.Count)];


            List<ulong> downlinks = Core.Trust.GetDownlinkIDs(Core.UserID, project, 3);

            if (downlinks.Count > 0)
            {
                // create new goal
                PlanGoal newGoal = new PlanGoal();

                newGoal.Project = project;
                newGoal.Title = GetRandomTitle(); 
                newGoal.End = Core.TimeNow.AddDays(Core.RndGen.Next(30, 300));
                newGoal.Description = Core.TextGen.GenerateSentences(1)[0];

                int choice = Core.RndGen.Next(100);

                // create new goal
                if (randGoal == null || choice < 10)
                {
                    newGoal.Ident = Core.RndGen.Next();
                    newGoal.Person = Core.UserID;
                }

                // delegate goal to sub
                else if (randGoal != null && choice < 50)
                {
                    PlanGoal head = randGoal;

                    // delegate down
                    newGoal.Ident = head.Ident;
                    newGoal.BranchUp = head.BranchDown;
                    newGoal.BranchDown = Core.RndGen.Next();
                    newGoal.Person = downlinks[Core.RndGen.Next(downlinks.Count)];
                }
                else
                    newGoal = null;


                if(newGoal != null)
                    LocalPlan.AddGoal(newGoal);
            }


            // add item to random goal
            if (randGoal != null)
            {
                PlanItem item = new PlanItem();

                item.Ident = randGoal.Ident;
                item.Project = randGoal.Project;
                item.BranchUp = randGoal.BranchDown;

                item.Title = GetRandomTitle(); 
                item.HoursTotal = Core.RndGen.Next(3, 30);
                item.HoursCompleted = Core.RndGen.Next(0, item.HoursTotal);
                item.Description = Core.TextGen.GenerateSentences(1)[0];

                LocalPlan.AddItem(item);
            }

            SaveLocal();
        }

        private string GetRandomTitle()
        {
            string[] words = Core.TextGen.GenerateWords(Core.RndGen.Next(1,5));

            string title = "";
            foreach (string word in words)
                title = word + " ";

            return title;
        }

        public void SimCleanup()
        {
        }

        void Cache_FileRemoved(OpVersionedFile file)
        {
            OpPlan plan = GetPlan(file.UserID, false);

            if (plan == null)
                return;

            PlanMap.SafeRemove(file.UserID);
        }

        private void Cache_FileAquired(OpVersionedFile file)
        {
            OpPlan prevPlan = GetPlan(file.UserID, false);

            OpPlan newPlan = new OpPlan(file);
            PlanMap.SafeAdd(newPlan.UserID, newPlan);

            if (file.UserID == Core.UserID)
                LocalPlan = newPlan;

            if ((newPlan == LocalPlan) || (prevPlan != null && prevPlan.Loaded)) // if loaded, reload
                LoadPlan(newPlan.UserID);


            // update subs
            if (Network.Established)
            {
                List<LocationData> locations = new List<LocationData>();

                Trust.ProjectRoots.LockReading(delegate()
                {
                    foreach (uint project in Trust.ProjectRoots.Keys)
                        if (newPlan.UserID == Core.UserID || Trust.IsHigher(newPlan.UserID, project))
                            Trust.GetLocsBelow(Core.UserID, project, locations);
                });

                Store.PublishDirect(locations, newPlan.UserID, ServiceID, DataTypeFile, newPlan.File.SignedHeader);
            }


            // see if we need to update our own goal estimates
            if (newPlan.UserID != Core.UserID && LocalPlan != null)
                Trust.ProjectRoots.LockReading(delegate()
                {
                    foreach (uint project in Trust.ProjectRoots.Keys)
                        if (Trust.IsLower(Core.UserID, newPlan.UserID, project)) // updated plan must be lower than us to have an effect
                            foreach (int ident in LocalPlan.GoalMap.Keys)
                            {
                                if (!newPlan.Loaded)
                                    LoadPlan(newPlan.UserID);

                                // if updated plan part of the same goal ident, re-estimate our own goals, incorporating update's changes
                                if (newPlan.GoalMap.ContainsKey(ident) || newPlan.ItemMap.ContainsKey(ident))
                                    foreach (PlanGoal goal in LocalPlan.GoalMap[ident])
                                    {
                                        int completed = 0, total = 0;

                                        GetEstimate(goal, ref completed, ref total);

                                        if (completed != goal.EstCompleted || total != goal.EstTotal)
                                        {
                                            goal.EstCompleted = completed;
                                            goal.EstTotal = total;

                                            if (RunSaveLocal == 0) // if countdown not started, start
                                                RunSaveLocal = SaveInterval;
                                        }
                                    }
                            }
                });


            Core.RunInGuiThread(PlanUpdate, newPlan);

            if (Core.NewsWorthy(newPlan.UserID, 0, false))
                Core.MakeNews("Plan updated by " + Core.GetName(newPlan.UserID), newPlan.UserID, 0, false, PlanRes.Schedule, Menu_ScheduleView);
                
        }

        internal void SaveLocal()
        {
            try
            {
                string tempPath = Core.GetTempPath();
                byte[] key = Utilities.GenerateKey(Core.StrongRndGen, 256);
                using (IVCryptoStream stream = IVCryptoStream.Save(tempPath, key))
                {
                    // write dummy block if nothing to write
                    OpPlan plan = GetPlan(Core.UserID, true);

                    if (plan == null ||
                        plan.Blocks == null ||
                        plan.Blocks.Count == 0)
                        Protocol.WriteToFile(new PlanBlock(), stream);


                    if (plan != null)
                    {
                        foreach (List<PlanBlock> list in plan.Blocks.Values)
                            foreach (PlanBlock block in list)
                                Protocol.WriteToFile(block, stream);

                        foreach (List<PlanGoal> list in plan.GoalMap.Values)
                            foreach (PlanGoal goal in list)
                            {
                                GetEstimate(goal, ref goal.EstCompleted, ref goal.EstTotal);
                                Protocol.WriteToFile(goal, stream);
                            }

                        foreach (List<PlanItem> list in plan.ItemMap.Values)
                            foreach (PlanItem item in list)
                                Protocol.WriteToFile(item, stream);
                    }

                    stream.WriteByte(0); // signal last packet

                    stream.FlushFinalBlock();
                }

                OpVersionedFile file = Cache.UpdateLocal(tempPath, key, null);

                Store.PublishDirect(Core.Trust.GetLocsAbove(), Core.UserID, ServiceID, DataTypeFile, file.SignedHeader);
            }
            catch (Exception ex)
            {
                Core.Network.UpdateLog("Plan", "Error updating local " + ex.Message);
            }
        }

        internal void LoadPlan(ulong id)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreBlocked(delegate() { LoadPlan(id); });
                return;
            }

            OpPlan plan = GetPlan(id, false);

            if (plan == null)
                return;

            // if local plan file not created yet
            if (plan.File.Header == null)
            {
                if (plan.UserID == Core.UserID)
                    plan.Init();

                return;
            }

            try
            {
                string path = Cache.GetFilePath(plan.File.Header);

                if (!File.Exists(path))
                    return;

                plan.Init();

                List<int> myjobs = new List<int>();

                using (TaggedStream file = new TaggedStream(path, Network.Protocol))
                using (IVCryptoStream crypto = IVCryptoStream.Load(file, plan.File.Header.FileKey))
                {
                    PacketStream stream = new PacketStream(crypto, Network.Protocol, FileAccess.Read);

                    G2Header root = null;

                    while (stream.ReadPacket(ref root))
                    {
                        if (root.Name == PlanPacket.Block)
                        {
                            PlanBlock block = PlanBlock.Decode(root);

                            if (block != null)
                                plan.AddBlock(block);
                        }

                        if (root.Name == PlanPacket.Goal)
                        {
                            PlanGoal goal = PlanGoal.Decode(root);

                            if (goal != null)
                                plan.AddGoal(goal);
                        }

                        if (root.Name == PlanPacket.Item)
                        {
                            PlanItem item = PlanItem.Decode(root);

                            if (item != null)
                                plan.AddItem(item);
                        }
                    }
                }

                plan.Loaded = true;


                // check if we have tasks for this person, that those jobs still exist
                //crit do check with plan items, make sure goal exists for them
                /*List<PlanTask> removeList = new List<PlanTask>();
                bool update = false;

                foreach(List<PlanTask> tasklist in LocalPlan.TaskMap.Values)
                {
                    removeList.Clear();

                    foreach (PlanTask task in tasklist)
                        if(task.Assigner == id)
                            if(!myjobs.Contains(task.Unique))
                                removeList.Add(task);

                    foreach(PlanTask task in removeList)
                        tasklist.Remove(task);

                    if (removeList.Count > 0)
                        update = true;
                }

                if (update)
                    SaveLocal();*/
            }
            catch (Exception ex)
            {
                Core.Network.UpdateLog("Plan", "Error loading plan " + ex.Message);
            }

        }

        internal OpPlan GetPlan(ulong id, bool tryLoad)
        {
            OpPlan plan = null;

            PlanMap.SafeTryGetValue(id, out plan);

            if (plan == null)
                return null;

            if (tryLoad && !plan.Loaded)
                LoadPlan(id);

            return (!tryLoad || (tryLoad && plan.Loaded)) ? plan : null;
        }

        internal void GetEstimate(PlanGoal goal, ref int completed, ref int total)
        {
            OpPlan plan = GetPlan(goal.Person, true);

            // if person not found use last estimate
            if (plan == null)
            {
                completed = goal.EstCompleted;
                total = goal.EstTotal;
                return;
            }

            // add person's items to estimate
            if (plan.ItemMap.ContainsKey(goal.Ident))
                foreach (PlanItem item in plan.ItemMap[goal.Ident])
                    if (item.BranchUp == goal.BranchDown)
                    {
                        completed += item.HoursCompleted;
                        total += item.HoursTotal;
                    }

            // add person's delegated goals to estimate
            if (plan.GoalMap.ContainsKey(goal.Ident))
                foreach (PlanGoal sub in plan.GoalMap[goal.Ident])
                    if (goal.BranchDown == sub.BranchUp && sub.BranchDown != 0)
                    {
                        if (Trust.TrustMap.SafeContainsKey(sub.Person) && !Trust.IsLower(goal.Person, sub.Person, goal.Project))
                            continue; // only pass if link file for sub is loaded, else assume linked so whole net can be reported

                        GetEstimate(sub, ref completed, ref total);
                    }
        }

        internal void GetAssignedGoals(ulong target, uint project, List<PlanGoal> roots, List<PlanGoal> archived)
        {
            List<PlanGoal> tempRoots = new List<PlanGoal>();
            List<PlanGoal> tempArchived = new List<PlanGoal>();

            List<int> assigned = new List<int>();

            // foreach self & higher
            List<ulong> ids = Trust.GetUplinkIDs(target, project);
            ids.Add(target);

            foreach (ulong id in ids)
            {
                OpPlan plan = GetPlan(id, true);

                if (plan == null)
                    continue;

                // apart of goals we have been assigned to

                foreach (List<PlanGoal> list in plan.GoalMap.Values)
                    foreach (PlanGoal goal in list)
                    {
                        if (goal.Project != project)
                            break;

                        if (goal.Person == target && !assigned.Contains(goal.Ident))
                            assigned.Add(goal.Ident);

                        if (goal.BranchDown == 0)
                        {
                            if (goal.Archived)
                                tempArchived.Add(goal);
                            else
                                tempRoots.Add(goal);
                        }
                    }
            }

            foreach (PlanGoal goal in tempArchived)
                if (assigned.Contains(goal.Ident))
                    archived.Add(goal);

            foreach (PlanGoal goal in tempRoots)
                if (assigned.Contains(goal.Ident))
                    roots.Add(goal);
        }

        internal void Research(ulong id)
        {
            Cache.Research(id);
        }
    }

    internal class OpPlan
    {
        internal OpVersionedFile File;

        internal bool Loaded; // true if blocks/goals loaded

        internal Dictionary<uint, List<PlanBlock>> Blocks = null;

        internal Dictionary<int, List<PlanGoal>> GoalMap = null;
        internal Dictionary<int, List<PlanItem>> ItemMap = null;

        internal OpPlan(OpVersionedFile file)
        {
            File = file;
        }

        internal void Init()
        {
            Blocks = new Dictionary<uint, List<PlanBlock>>();
            GoalMap = new Dictionary<int, List<PlanGoal>>();
            ItemMap = new Dictionary<int, List<PlanItem>>();
        }

        internal ulong UserID
        {
            get
            {
                return File.UserID;
            }
        }

        internal void AddBlock(PlanBlock block)
        {
            if (Blocks == null)
                Blocks = new Dictionary<uint, List<PlanBlock>>();

            if (!Blocks.ContainsKey(block.ProjectID))
                Blocks[block.ProjectID] = new List<PlanBlock>();

            int i = 0;

            foreach (PlanBlock compare in Blocks[block.ProjectID])
            {
                if (compare.StartTime > block.StartTime)
                    break;

                i++;
            }

            Blocks[block.ProjectID].Insert(i, block);
        }

        internal void AddGoal(PlanGoal goal)
        {
            if (!GoalMap.ContainsKey(goal.Ident))
                GoalMap[goal.Ident] = new List<PlanGoal>();

            GoalMap[goal.Ident].Add(goal);
        }

        internal void AddItem(PlanItem item)
        {
            if (!ItemMap.ContainsKey(item.Ident))
                ItemMap[item.Ident] = new List<PlanItem>();

            ItemMap[item.Ident].Add(item);
        }

        internal void RemoveItem(PlanItem item)
        {
            if (ItemMap.ContainsKey(item.Ident))
                if (ItemMap[item.Ident].Contains(item))
                    ItemMap[item.Ident].Remove(item);
        }

        internal void RemoveGoal(PlanGoal goal)
        {
            if (GoalMap.ContainsKey(goal.Ident))
                if (GoalMap[goal.Ident].Contains(goal))
                    GoalMap[goal.Ident].Remove(goal);
        }


    }
}
