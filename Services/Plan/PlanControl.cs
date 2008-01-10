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
using DeOps.Services.Link;
using DeOps.Services.Location;
using DeOps.Services.VersionedFile;


namespace DeOps.Services.Plan
{
    internal delegate void PlanUpdateHandler(OpPlan plan);
    internal delegate List<ulong> PlanGetFocusedHandler();

    internal class PlanControl : OpComponent
    {
        internal OpCore Core;
        internal G2Protocol Protocol;
        internal DhtNetwork Network;
        internal DhtStore Store;
        internal LinkControl Links;

        internal OpPlan LocalPlan;

        int  RunSaveLocal;
        int SaveInterval = 60*10; // 10 min stagger, prevent cascade up

        internal ThreadedDictionary<ulong, OpPlan> PlanMap = new ThreadedDictionary<ulong, OpPlan>();

        internal event PlanUpdateHandler PlanUpdate;
        internal event PlanGetFocusedHandler GetFocused;


        enum DataType { File = 0x01 };

        VersionedFileAssist PlanFiles;


        internal PlanControl(OpCore core)
        {
            Core = core;
            Core.Plans = this;
            Protocol = core.Protocol;
            Network = core.OperationNet;
            Store = Network.Store;

            if (Core.Sim != null)
                SaveInterval = 30;
            
            Core.LoadEvent += new LoadHandler(Core_Load);
            Core.TimerEvent += new TimerHandler(Core_Timer);
            

            PlanFiles = new VersionedFileAssist(Network, ComponentID.Plan, (ushort)DataType.File);
            
            PlanFiles.FileAquired += new FileAquiredHandler(PlanFiles_FileAquired);
        }

        internal override List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong key, uint proj)
        {
            List<MenuItemInfo> menus = new List<MenuItemInfo>();

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

            return menus;
        }

        void Menu_ScheduleView(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            ScheduleView view = new ScheduleView(this, node.GetKey(), node.GetProject());

            Core.InvokeView(node.IsExternal(), view);
        }

        void Menu_GoalsView(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            GoalsView view = new GoalsView(this, node.GetKey(), node.GetProject());

            Core.InvokeView(node.IsExternal(), view);
        }

        void Core_Load()
        {
            Links = Core.Links;

            // try not to auto-publish empty file
            if (!PlanMap.SafeContainsKey(Core.LocalDhtID))
            {
                LocalPlan = new OpPlan(new OpVersionedFile(Core.User.Settings.KeyPublic));
                LocalPlan.Init();
                LocalPlan.Loaded = true;
                PlanMap.SafeAdd(Core.LocalDhtID, LocalPlan);
            }
        }

        void Core_Timer()
        {
            // triggered on update estimates use time out so update doesnt cascade the network all the way up
            //  our save local can cause other save locals
            if (RunSaveLocal > 0)
            {
                RunSaveLocal--;

                if (RunSaveLocal == 0)
                    SaveLocal();
            }

            // do below once per minute
            if (Core.TimeNow.Second != 0)
                return;


            List<ulong> focused = new List<ulong>();

            if (GetFocused != null)
                foreach (PlanGetFocusedHandler handler in GetFocused.GetInvocationList())
                    foreach (ulong id in handler.Invoke())
                        if (!focused.Contains(id))
                            focused.Add(id);

            // unload
            PlanMap.LockReading(delegate()
            {
                foreach (OpPlan plan in PlanMap.Values)
                    if (plan.Loaded && plan != LocalPlan && !focused.Contains(plan.DhtID))
                    {
                        plan.Loaded = false;
                        plan.Blocks = null;
                        plan.GoalMap = null;
                        plan.ItemMap = null;
                    }
            });
        }

        private void PlanFiles_FileAquired(OpVersionedFile file)
        {
            OpPlan prevPlan = GetPlan(file.Header.KeyID, false);

            OpPlan newPlan = new OpPlan(file);
            PlanMap.SafeAdd(newPlan.DhtID, newPlan);

            if (file.Header.KeyID == Core.LocalDhtID)
                LocalPlan = newPlan;

            if ((newPlan == LocalPlan) || (prevPlan != null && prevPlan.Loaded)) // if loaded, reload
                LoadPlan(newPlan.DhtID);


            // update subs
            if (Network.Established)
            {
                List<LocationData> locations = new List<LocationData>();

                Links.ProjectRoots.LockReading(delegate()
                {
                    foreach (uint project in Links.ProjectRoots.Keys)
                        if (newPlan.DhtID == Core.LocalDhtID || Links.IsHigher(newPlan.DhtID, project))
                            Links.GetLocsBelow(Core.LocalDhtID, project, locations);
                });

                Store.PublishDirect(locations, newPlan.DhtID, ComponentID.Plan, (ushort)DataType.File, newPlan.File.SignedHeader);
            }


            // see if we need to update our own goal estimates
            if (newPlan.DhtID != Core.LocalDhtID && LocalPlan != null)
                Links.ProjectRoots.LockReading(delegate()
                {
                    foreach (uint project in Links.ProjectRoots.Keys)
                        if (Links.IsLower(Core.LocalDhtID, newPlan.DhtID, project)) // updated plan must be lower than us to have an effect
                            foreach (int ident in LocalPlan.GoalMap.Keys)
                            {
                                if (!newPlan.Loaded)
                                    LoadPlan(newPlan.DhtID);

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


            if (PlanUpdate != null)
                Core.RunInGuiThread(PlanUpdate, newPlan);

            if (Core.NewsWorthy(newPlan.DhtID, 0, false))
                Core.MakeNews("Plan updated by " + Links.GetName(newPlan.DhtID), newPlan.DhtID, 0, false, PlanRes.Schedule, Menu_ScheduleView);
                
        }

        internal void SaveLocal()
        {
            try
            {
                OpPlan plan = GetPlan(Core.LocalDhtID, true);

                RijndaelManaged key = new RijndaelManaged();
                key.GenerateKey();
                key.IV = new byte[key.IV.Length]; 

                string tempPath = Core.GetTempPath();
                FileStream tempFile = new FileStream(tempPath, FileMode.CreateNew);
                CryptoStream stream = new CryptoStream(tempFile, key.CreateEncryptor(), CryptoStreamMode.Write);

                // write dummy block if nothing to write
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

                stream.FlushFinalBlock();
                stream.Close();

                OpVersionedFile file = PlanFiles.UpdateLocal(tempPath, key);

                Store.PublishDirect(Core.Links.GetLocsAbove(), Core.LocalDhtID, ComponentID.Plan, (ushort) DataType.File, file.SignedHeader);

            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Plan", "Error updating local " + ex.Message);
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
                if (plan.DhtID == Core.LocalDhtID)
                    plan.Init();

                return;
            }

            try
            {
                string path = PlanFiles.GetFilePath(plan.File.Header);

                if (!File.Exists(path))
                    return;

                plan.Init();

                List<int> myjobs = new List<int>();

                FileStream   file   = new FileStream(path, FileMode.Open);
                CryptoStream crypto = new CryptoStream(file, plan.File.Header.FileKey.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Core.Protocol, FileAccess.Read);

                G2Header root = null;

                while (stream.ReadPacket(ref root))
                {
                    if (root.Name == PlanPacket.Block)
                    {
                        PlanBlock block = PlanBlock.Decode(Core.Protocol, root);

                        if (block != null)
                            plan.AddBlock(block);
                    }

                    if (root.Name == PlanPacket.Goal)
                    {
                        PlanGoal goal = PlanGoal.Decode(Core.Protocol, root);

                        if (goal != null)
                            plan.AddGoal(goal);
                    }

                    if (root.Name == PlanPacket.Item)
                    {
                        PlanItem item = PlanItem.Decode(Core.Protocol, root);

                        if (item != null)
                            plan.AddItem(item);
                    }
                }

                stream.Close();

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
                Core.OperationNet.UpdateLog("Plan", "Error loading plan " + ex.Message);
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
                        if (Links.TrustMap.SafeContainsKey(sub.Person) && !Links.IsLower(goal.Person, sub.Person, goal.Project))
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
            List<ulong> ids = Links.GetUplinkIDs(target, project);
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
            PlanFiles.Research(id);
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

        internal ulong DhtID
        {
            get
            {
                return File.DhtID;
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
