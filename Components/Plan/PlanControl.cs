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

using DeOps.Components.Transfer;
using DeOps.Components.Link;
using DeOps.Components.Location;


namespace DeOps.Components.Plan
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

        internal string PlanPath;
        internal OpPlan LocalPlan;
        RijndaelManaged LocalFileKey;

        bool RunSaveHeaders;
        int  RunSaveLocal;
        int SaveInterval = 60*10; // 10 min stagger, prevent cascade up

        internal ThreadedDictionary<ulong, OpPlan> PlanMap = new ThreadedDictionary<ulong, OpPlan>();
        internal event PlanUpdateHandler PlanUpdate;
        internal event PlanGetFocusedHandler GetFocused;

        int PruneSize = 100;

        Dictionary<ulong, DateTime> NextResearch = new Dictionary<ulong, DateTime>();
        Dictionary<ulong, uint> DownloadLater = new Dictionary<ulong, uint>();


        internal PlanControl(OpCore core)
        {
            Core = core;
            Core.Plans = this;
            Protocol = core.Protocol;
            Network = core.OperationNet;
            Store = Network.Store;

            Core.LoadEvent += new LoadHandler(Core_Load);
            Core.TimerEvent += new TimerHandler(Core_Timer);

            Network.EstablishedEvent += new EstablishedHandler(Network_Established);
            
            Store.StoreEvent[ComponentID.Plan] = new StoreHandler(Store_Local);
            Store.ReplicateEvent[ComponentID.Plan] = new ReplicateHandler(Store_Replicate);
            Store.PatchEvent[ComponentID.Plan] = new PatchHandler(Store_Patch);

            Network.Searches.SearchEvent[ComponentID.Plan] = new SearchRequestHandler(Search_Local);

            if (Core.Sim != null)
            {
                PruneSize = 25;
                SaveInterval = 30;
            }
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
            Core.Transfers.FileSearch[ComponentID.Plan] = new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[ComponentID.Plan] = new FileRequestHandler(Transfers_FileRequest);

            PlanPath = Core.User.RootPath + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + ComponentID.Plan.ToString();
            Directory.CreateDirectory(PlanPath);

            LocalFileKey = Core.User.Settings.FileKey;

            LoadHeaders();

            if (!PlanMap.SafeContainsKey(Core.LocalDhtID))
                SaveLocal();   
        }

        void Core_Timer()
        {
            if (RunSaveHeaders)
                SaveHeaders();

            // clean download later map
            if (!Network.Established)
                Utilities.PruneMap(DownloadLater, Core.LocalDhtID, PruneSize);


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

            // prune
            List<ulong> removeIDs = new List<ulong>();

            PlanMap.LockReading(delegate()
            {
                if (PlanMap.Count > PruneSize)
                    foreach (OpPlan plan in PlanMap.Values)
                        if (plan.DhtID != Core.LocalDhtID &&
                            !Core.Links.LinkMap.SafeContainsKey(plan.DhtID) && // dont remove nodes in our local hierarchy
                            !focused.Contains(plan.DhtID) &&
                            !Utilities.InBounds(plan.DhtID, plan.DhtBounds, Core.LocalDhtID))
                            removeIDs.Add(plan.DhtID);
            });

            if (removeIDs.Count > 0)
                PlanMap.LockWriting(delegate()
                {
                    while (removeIDs.Count > 0 && PlanMap.Count > PruneSize / 2)
                    {
                        ulong furthest = Core.LocalDhtID;
                        OpPlan plan = PlanMap[furthest];

                        foreach (ulong id in removeIDs)
                            if ((id ^ Core.LocalDhtID) > (furthest ^ Core.LocalDhtID))
                                furthest = id;

                        if (plan.Header != null)
                            try { File.Delete(GetFilePath(plan.Header)); }
                            catch { }

                        PlanMap.Remove(furthest);
                        removeIDs.Remove(furthest);
                        RunSaveHeaders = true;
                    }
                });

            // clean research map
            removeIDs.Clear();

            foreach (KeyValuePair<ulong, DateTime> pair in NextResearch)
                if (Core.TimeNow > pair.Value)
                    removeIDs.Add(pair.Key);

            if (removeIDs.Count > 0)
                foreach (ulong id in removeIDs)
                    NextResearch.Remove(id);
        }

        void Network_Established()
        {
            ulong localBounds = Store.RecalcBounds(Core.LocalDhtID);

            // set bounds for objects
            PlanMap.LockReading(delegate()
            {
                foreach (OpPlan plan in PlanMap.Values)
                {
                    plan.DhtBounds = Store.RecalcBounds(plan.DhtID);

                    // republish objects that were not seen on the network during startup
                    if (plan.Unique && Utilities.InBounds(Core.LocalDhtID, localBounds, plan.DhtID))
                        Store.PublishNetwork(plan.DhtID, ComponentID.Plan, plan.SignedHeader);
                }
            });

            // only download those objects in our local area
            foreach (KeyValuePair<ulong, uint> pair in DownloadLater)
                if (Utilities.InBounds(Core.LocalDhtID, localBounds, pair.Key))
                    StartSearch(pair.Key, pair.Value);

            DownloadLater.Clear();
        }

        private void LoadHeaders()
        {
            try
            {
                string path = PlanPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "PlanHeaders");

                if (!File.Exists(path))
                    return;

                FileStream file = new FileStream(path, FileMode.Open);
                CryptoStream crypto = new CryptoStream(file, LocalFileKey.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Core.Protocol, FileAccess.Read);

                G2Header root = null;

                while (stream.ReadPacket(ref root))
                    if (root.Name == DataPacket.SignedData)
                    {
                        SignedData signed = SignedData.Decode(Core.Protocol, root);
                        G2Header embedded = new G2Header(signed.Data);


                        // figure out data contained
                        if (Core.Protocol.ReadPacket(embedded))
                            if (embedded.Name == PlanPacket.Header)
                                Process_PlanHeader(null, signed, PlanHeader.Decode(Core.Protocol, embedded));
                    }

                stream.Close();
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Plan", "Error loading plan " + ex.Message);
            }
        }

        private void SaveHeaders()
        {
            RunSaveHeaders = false;

            try
            {
                string tempPath = Core.GetTempPath();
                FileStream file = new FileStream(tempPath, FileMode.Create);
                CryptoStream stream = new CryptoStream(file, LocalFileKey.CreateEncryptor(), CryptoStreamMode.Write);

                PlanMap.LockReading(delegate()
                {
                    foreach (OpPlan plan in PlanMap.Values)
                        if (plan.SignedHeader != null)
                            stream.Write(plan.SignedHeader, 0, plan.SignedHeader.Length);
                });

                stream.FlushFinalBlock();
                stream.Close();


                string finalPath = PlanPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "PlanHeaders");
                File.Delete(finalPath);
                File.Move(tempPath, finalPath);
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Plan", "Error saving plans " + ex.Message);
            }
        }


        private void Process_PlanHeader(DataReq data, SignedData signed, PlanHeader header)
        {
            Core.IndexKey(header.KeyID, ref header.Key);

            OpPlan current = GetPlan(header.KeyID, false);

            // if link loaded
            if (current != null)
            {
                // lower version
                if (header.Version < current.Header.Version)
                {
                    if (data != null && data.Sources != null)
                        foreach (DhtAddress source in data.Sources)
                            Store.Send_StoreReq(source, data.LocalProxy, new DataReq(null, current.DhtID, ComponentID.Plan, current.SignedHeader));

                    return;
                }

                // higher version
                else if (header.Version > current.Header.Version)
                {
                    CachePlan(signed, header);
                }
            }

            // else load file, set new header after file loaded
            else
                CachePlan(signed, header);
        }

        private void CachePlan(SignedData signedHeader, PlanHeader header)
        {
            if (Core.InvokeRequired)
                Debug.Assert(false);

            try
            {
                // check if file exists           
                string path = GetFilePath(header);
                if (!File.Exists(path))
                {
                    DownloadPlan(signedHeader, header);
                    return;
                }

                // get plan
                OpPlan prevPlan = GetPlan(header.KeyID, false);

                OpPlan newPlan = new OpPlan(header.Key);

     
                // delete old file
                if (prevPlan != null)
                {
                    if (header.Version < prevPlan.Header.Version)
                        return; // dont update with older version

                    string oldPath = GetFilePath(prevPlan.Header);
                    if (path != oldPath && File.Exists(oldPath))
                        try { File.Delete(oldPath); }
                        catch { }
                }

                // set new header
                newPlan.Header = header;
                newPlan.SignedHeader = signedHeader.Encode(Core.Protocol);
                newPlan.Unique = Core.Loading;

                PlanMap.SafeAdd(header.KeyID, newPlan);

                if (header.KeyID == Core.LocalDhtID)
                    LocalPlan = newPlan;

                if ((newPlan == LocalPlan ) || (prevPlan != null && prevPlan.Loaded)) // if loaded, reload
                    LoadPlan(newPlan.DhtID);
                
                RunSaveHeaders = true;
                
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

                    Store.PublishDirect(locations, newPlan.DhtID, ComponentID.Plan, newPlan.SignedHeader);
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
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Plan", "Error caching plan " + ex.Message);
            }
        }

        private void DownloadPlan(SignedData signed, PlanHeader header)
        {
            Utilities.CheckSignedData(header.Key, signed.Data, signed.Signature);

            FileDetails details = new FileDetails(ComponentID.Plan, header.FileHash, header.FileSize, null);

            Core.Transfers.StartDownload(header.KeyID, details, new object[] { signed, header }, new EndDownloadHandler(EndDownload));
        }

        private void EndDownload(string path, object[] args)
        {
            SignedData signedHeader = (SignedData)args[0];
            PlanHeader header = (PlanHeader)args[1];

            try
            {
                File.Move(path, GetFilePath(header));
            }
            catch { return; }

            CachePlan(signedHeader, header);
        }

        private string GetFilePath(PlanHeader header)
        {
            return PlanPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, header.KeyID, header.FileHash);
        }

        bool Transfers_FileSearch(ulong key, FileDetails details)
        {
            OpPlan plan = GetPlan(key, false);

            if (plan != null)
                if (details.Size == plan.Header.FileSize && Utilities.MemCompare(details.Hash, plan.Header.FileHash))
                    return true;

            return false;
        }

        string Transfers_FileRequest(ulong key, FileDetails details)
        {
            OpPlan plan = GetPlan(key, false);

            if (plan != null)
                if (details.Size == plan.Header.FileSize && Utilities.MemCompare(details.Hash, plan.Header.FileHash))
                    return GetFilePath(plan.Header);


            return null;
        }

        void Store_Local(DataReq store)
        {
            // getting published to - search results - patch

            SignedData signed = SignedData.Decode(Core.Protocol, store.Data);
            PlanHeader header = PlanHeader.Decode(Core.Protocol, signed.Data);

            Process_PlanHeader(null, signed, header);
        }

        const int PatchEntrySize = 12;

        ReplicateData Store_Replicate(DhtContact contact, bool add)
        {
            if (!Network.Established)
                return null;
            
            ReplicateData data = new ReplicateData(ComponentID.Plan, PatchEntrySize);

            byte[] patch = new byte[PatchEntrySize];

            PlanMap.LockReading(delegate()
            {
                foreach (OpPlan plan in PlanMap.Values)
                    if (Utilities.InBounds(plan.DhtID, plan.DhtBounds, contact.DhtID))
                    {
                        DhtContact target = contact;
                        plan.DhtBounds = Store.RecalcBounds(plan.DhtID, add, ref target);

                        if (target != null)
                        {
                            BitConverter.GetBytes(plan.DhtID).CopyTo(patch, 0);
                            BitConverter.GetBytes(plan.Header.Version).CopyTo(patch, 8);

                            data.Add(target, patch);
                        }
                    }
            });

            return data;
        }

        void Store_Patch(DhtAddress source, ulong distance, byte[] data)
        {
            if (data.Length % PatchEntrySize != 0)
                return;

            int offset = 0;

            for (int i = 0; i < data.Length; i += PatchEntrySize)
            {
                ulong dhtid = BitConverter.ToUInt64(data, i);
                uint version = BitConverter.ToUInt32(data, i + 8);

                offset += PatchEntrySize;

                if (!Utilities.InBounds(Core.LocalDhtID, distance, dhtid))
                    continue;

                OpPlan plan = GetPlan(dhtid, false);

                if (plan != null && plan.Header != null)
                {
                    if (plan.Header.Version > version)
                    {
                        Store.Send_StoreReq(source, 0, new DataReq(null, plan.DhtID, ComponentID.Plan, plan.SignedHeader));
                        continue;
                    }

                    plan.Unique = false; // network has current or newer version

                    if (plan.Header.Version == version)
                        continue;

                    // else our version is old, download below
                }


                if (Network.Established)
                    Network.Searches.SendDirectRequest(source, dhtid, ComponentID.Plan, BitConverter.GetBytes(version));
                else
                    DownloadLater[dhtid] = version;
            }
        }

        private void StartSearch(ulong key, uint version)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { StartSearch(key, version); });
                return;
            }

            byte[] parameters = BitConverter.GetBytes(version);
            DhtSearch search = Core.OperationNet.Searches.Start(key, "Plan", ComponentID.Plan, parameters, new EndSearchHandler(EndSearch));

            if (search != null)
                search.TargetResults = 2;
        }

        void EndSearch(DhtSearch search)
        {
            foreach (SearchValue found in search.FoundValues)
                Store_Local(new DataReq(found.Sources, search.TargetID, ComponentID.Plan, found.Value));
        }

        List<byte[]> Search_Local(ulong key, byte[] parameters)
        {
            List<Byte[]> results = new List<byte[]>();

            uint minVersion = BitConverter.ToUInt32(parameters, 0);

            OpPlan plan = GetPlan(key, false);

            if (plan != null)
                if (plan.Header.Version >= minVersion)
                    results.Add(plan.SignedHeader);

            return results;
        }

        internal void SaveLocal()
        {
            if (Core.InvokeRequired)
            {
                // block until completed
                Core.RunInCoreBlocked(delegate() { SaveLocal(); });
                return;
            }


            try
            {
                OpPlan plan = GetPlan(Core.LocalDhtID, true);
                PlanHeader header = null;

                if (plan != null)
                    header = plan.Header;

                string oldFile = null;

                if (header != null)
                    oldFile = GetFilePath(header);
                else
                    header = new PlanHeader();


                header.Key = Core.User.Settings.KeyPublic;
                header.KeyID = Core.LocalDhtID; // set so keycheck works
                header.Version++;
                header.FileKey.GenerateKey();
                header.FileKey.IV = new byte[header.FileKey.IV.Length];

                string tempPath = Core.GetTempPath();
                FileStream tempFile = new FileStream(tempPath, FileMode.CreateNew);
                CryptoStream stream = new CryptoStream(tempFile, header.FileKey.CreateEncryptor(), CryptoStreamMode.Write);

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


                // finish building header
                Utilities.ShaHashFile(tempPath, ref header.FileHash, ref header.FileSize);

                // move file, overwrite if need be
                string finalPath = GetFilePath(header);
                File.Move(tempPath, finalPath);

                CachePlan(new SignedData(Core.Protocol, Core.User.Settings.KeyPair, header), header);

                SaveHeaders();

                if (oldFile != null && File.Exists(oldFile)) // delete after move to ensure a copy always exists (names different)
                    try { File.Delete(oldFile); }
                    catch { }

                // publish header
                plan = GetPlan(Core.LocalDhtID, true); // get newly loaded object

                if (plan == null)
                    return;

                Store.PublishNetwork(Core.LocalDhtID, ComponentID.Plan, plan.SignedHeader);

                Store.PublishDirect(Links.GetSuperLocs(), Core.LocalDhtID, ComponentID.Plan, plan.SignedHeader);
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

            try
            {
                string path = GetFilePath(plan.Header);

                if (!File.Exists(path))
                    return;

                plan.Blocks = new Dictionary<uint, List<PlanBlock>>();
                plan.GoalMap = new Dictionary<int, List<PlanGoal>>();
                plan.ItemMap = new Dictionary<int, List<PlanItem>>();

                List<int> myjobs = new List<int>();

                FileStream   file   = new FileStream(path, FileMode.Open);
                CryptoStream crypto = new CryptoStream(file, plan.Header.FileKey.CreateDecryptor(), CryptoStreamMode.Read);
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

        internal void Research(ulong key)
        {
            if (!Network.Routing.Responsive())
                return;

            // limit re-search to once per 30 secs
            DateTime timeout = default(DateTime);

            if (NextResearch.TryGetValue(key, out timeout))
                if (Core.TimeNow < timeout)
                    return;

            uint version = 0;
            OpPlan plan = GetPlan(key, false);
            if (plan != null)
                version = plan.Header.Version + 1;

            StartSearch(key, version);

            NextResearch[key] = Core.TimeNow.AddSeconds(30);
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
                        if (Links.LinkMap.SafeContainsKey(sub.Person) && !Links.IsLower(goal.Person, sub.Person, goal.Project))
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
    }

    internal class OpPlan
    {
        internal ulong    DhtID;
        internal ulong    DhtBounds = ulong.MaxValue;
        internal byte[]   Key;    // make sure reference is the same as main key list
        internal bool     Unique;

        internal PlanHeader Header;
        internal byte[] SignedHeader;

        internal bool Loaded; // true if blocks/goals loaded
        
        internal Dictionary<uint, List<PlanBlock>> Blocks = null;

        internal Dictionary<int, List<PlanGoal>> GoalMap = null;
        internal Dictionary<int, List<PlanItem>> ItemMap = null;

        internal OpPlan(byte[] key)
        {
            Key = key;
            DhtID = Utilities.KeytoID(key);
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
