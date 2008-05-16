using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;

using RiseOp.Services.Assist;
using RiseOp.Services.Location;
using RiseOp.Services.Transfer;


namespace RiseOp.Services.Trust
{
    internal delegate void LinkUpdateHandler(OpTrust trust);
    internal delegate void LinkGuiUpdateHandler(ulong key);


    class TrustService : OpService
    {
        public string Name { get { return "Trust"; } }
        public uint ServiceID { get { return 1; } }

        const uint DataTypeFile = 0x01;


        internal OpCore Core;
        internal DhtStore Store;
        internal DhtNetwork Network;

        internal OpTrust LocalTrust;

        internal ThreadedDictionary<ulong, OpTrust> TrustMap = new ThreadedDictionary<ulong, OpTrust>();
        internal ThreadedDictionary<uint, string> ProjectNames = new ThreadedDictionary<uint, string>();
        internal ThreadedDictionary<uint, ThreadedList<OpLink>> ProjectRoots = new ThreadedDictionary<uint, ThreadedList<OpLink>>();

        internal VersionedCache Cache;

        internal string LinkPath;

        bool RunSaveUplinks;
        RijndaelManaged LocalFileKey;

        internal LinkUpdateHandler LinkUpdate;
        internal LinkGuiUpdateHandler GuiUpdate;


        internal TrustService(OpCore core)
        {
            Core = core;
            Core.Links = this;

            Store = Core.OperationNet.Store;
            Network = Core.OperationNet;

            Core.SecondTimerEvent += new TimerHandler(Core_SecondTimer);
            Core.MinuteTimerEvent += new TimerHandler(Core_MinuteTimer);
            Core.GetFocusedCore += new GetFocusedHandler(Core_GetFocusedCore);

            Cache = new VersionedCache(Network, ServiceID, DataTypeFile, true);

            // piggyback searching for uplink requests on cache file data
            Store.StoreEvent[ServiceID, DataTypeFile] += new StoreHandler(Store_Local);
            Network.Searches.SearchEvent[ServiceID, DataTypeFile] += new SearchRequestHandler(Search_Local);

            Cache.FileAquired += new FileAquiredHandler(Cache_FileAquired);
            Cache.FileRemoved += new FileRemovedHandler(Cache_FileRemoved);
            Cache.Load();

            ProjectNames.SafeAdd(0, Core.User.Settings.Operation);

            LinkPath = Core.User.RootPath + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + ServiceID.ToString();
            Directory.CreateDirectory(LinkPath);

            LocalFileKey = Core.User.Settings.FileKey;

            LoadUplinkReqs();


            LocalTrust = GetTrust(Core.LocalDhtID);


            if (LocalTrust == null)
            {
                LocalTrust = new OpTrust(new OpVersionedFile(Core.User.Settings.KeyPublic));
                TrustMap.SafeAdd(Core.LocalDhtID, LocalTrust);
            }

            if (!LocalTrust.Loaded)
            {
                LocalTrust.Name = Core.User.Settings.ScreenName;
                LocalTrust.AddProject(0, true); // operation

                SaveLocal();
            }
        }

        public void Dispose()
        {
            Core.SecondTimerEvent -= new TimerHandler(Core_SecondTimer);
            Core.MinuteTimerEvent -= new TimerHandler(Core_MinuteTimer);
            Core.GetFocusedCore -= new GetFocusedHandler(Core_GetFocusedCore);

            Network.Searches.SearchEvent[ServiceID, DataTypeFile] -= new SearchRequestHandler(Search_Local);

            Cache.FileAquired -= new FileAquiredHandler(Cache_FileAquired);
            Cache.FileRemoved -= new FileRemovedHandler(Cache_FileRemoved);
            Cache.Dispose();
        }

        public List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong user, uint project)
        {
            if (menuType != InterfaceMenuType.Quick)
                return null;

            List<MenuItemInfo> menus = new List<MenuItemInfo>();

            bool unlink = false;

            OpLink remoteLink = GetLink(user, project);
            OpLink localLink = LocalTrust.GetLink(project);

            if (remoteLink == null)
                return menus;

            // linkup
            if (Core.LocalDhtID != user &&
                (localLink == null || 
                 localLink.Uplink == null || 
                 localLink.Uplink.DhtID != user)) // not already linked to
                menus.Add(new MenuItemInfo("Trust", LinkRes.link, new EventHandler(Menu_Linkup)));

            if (localLink == null)
                return menus;

            // confirm
            if (localLink.Downlinks.Contains(remoteLink))
            {
                unlink = true;

                if (!localLink.Confirmed.Contains(user)) // not already confirmed
                    menus.Add(new MenuItemInfo("Accept Trust", LinkRes.confirmlink, new EventHandler(Menu_ConfirmLink)));
            }

            // unlink
            if ((unlink && localLink.Confirmed.Contains(user)) ||
                (localLink.Uplink != null && localLink.Uplink.DhtID == user))
                menus.Add(new MenuItemInfo("Revoke Trust", LinkRes.unlink, new EventHandler(Menu_Unlink)));


            return menus;
        }

        private void Menu_Linkup(object sender, EventArgs e)
        {
            if (!(sender is IViewParams) || Core.GuiMain == null)
                return;

            ulong remoteKey = ((IViewParams)sender).GetKey();
            uint project = ((IViewParams)sender).GetProject();

            LocalTrust.AddProject(project, true);

            OpLink localLink = LocalTrust.GetLink(project);

            if (localLink == null)
                return;

            // get user confirmation if nullifying previous uplink
            if (localLink.Uplink != null)
            {
                string who = GetName(localLink.Uplink.DhtID);
                string message = "Transfer trust from " + who + " to " + GetName(remoteKey) + "?";

                if (MessageBox.Show(Core.GuiMain, message, "Confirm Trust", MessageBoxButtons.YesNo) == DialogResult.No)
                    return;
            }

            try
            {
                OpLink remoteLink = GetLink(remoteKey, project);

                if (remoteLink == null)
                    throw new Exception("Could not find Person");

                // check if self
                if (remoteLink == localLink)
                    throw new Exception("Cannot Trust in your Self");

                // check if already linked
                if (localLink.Uplink != null && localLink.Uplink == remoteLink)
                    throw new Exception("Already Trusting " + GetName(remoteKey));

                //check for loop
                if (IsHigher(remoteLink.DhtID, Core.LocalDhtID, project, false))
                {
                    string who = GetName(remoteLink.DhtID);
                    string message = "Trusting " + who + " will create a loop. Is this your intention?";

                    if (MessageBox.Show(Core.GuiMain, message, "Loop Warning", MessageBoxButtons.YesNo) == DialogResult.No)
                        return;
                }

                LocalTrust.AddProject(project, true);
                localLink.ResetUplink();
                localLink.Uplink = remoteLink;

                SaveLocal();

                Core.RunInCoreAsync(delegate()
                {
                    LinkupRequest(remoteLink);
                });

            }
            catch (Exception ex)
            {
                MessageBox.Show(Core.GuiMain, ex.Message);
            }
        }

        private void LinkupRequest(OpLink remoteLink)
        {
            // create uplink request, publish
            UplinkRequest request = new UplinkRequest();
            request.ProjectID = remoteLink.Project;
            request.LinkVersion = LocalTrust.File.Header.Version;
            request.TargetVersion = remoteLink.Trust.File.Header.Version;
            request.Key = LocalTrust.File.Key;
            request.KeyID = LocalTrust.DhtID;
            request.Target = remoteLink.Trust.File.Key;
            request.TargetID = remoteLink.DhtID;

            byte[] signed = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, request);

            if(Network.Established)
                Store.PublishNetwork(request.TargetID, ServiceID, DataTypeFile, signed);

            // store locally
            Process_UplinkReq(null, new SignedData(Core.Protocol, Core.User.Settings.KeyPair, request), request);

            // publish at neighbors so they are aware of request status
            List<LocationData> locations = new List<LocationData>();
            GetLocs(Core.LocalDhtID, remoteLink.Project, 1, 1, locations);
            GetLocsBelow(Core.LocalDhtID, remoteLink.Project, locations);
            Store.PublishDirect(locations, request.TargetID, ServiceID, DataTypeFile, signed);
        }

        private void Menu_ConfirmLink(object sender, EventArgs e)
        {
            if (!(sender is IViewParams) || Core.GuiMain == null)
                return;

            ulong key = ((IViewParams)sender).GetKey();
            uint project = ((IViewParams)sender).GetProject();

            try
            {
                OpLink remoteLink = GetLink(key, project);
                OpLink localLink = LocalTrust.GetLink(project);

                if (remoteLink == null || localLink == null)
                    throw new Exception("Could not find Person");

                if (!localLink.Downlinks.Contains(remoteLink))
                    throw new Exception(GetName(key) + " does not trust you");

                if (!localLink.Confirmed.Contains(remoteLink.DhtID))
                    localLink.Confirmed.Add(remoteLink.DhtID);

                SaveLocal();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Core.GuiMain, ex.Message);
            }

        }

        private void Menu_Unlink(object sender, EventArgs e)
        {
            if (!(sender is IViewParams) || Core.GuiMain == null)
                return;

            ulong key = ((IViewParams)sender).GetKey();
            uint project = ((IViewParams)sender).GetProject();

            try
            {
                bool unlinkUp = false;
                bool unlinkDown = false;

                OpLink remoteLink = GetLink(key, project);
                OpLink localLink = LocalTrust.GetLink(project);

                if (remoteLink == null || localLink == null)
                    throw new Exception("Could not find Person");

                if (localLink.Uplink != null && localLink.Uplink == remoteLink)
                    unlinkUp = true;

                if (localLink.Confirmed.Contains(remoteLink.DhtID))
                    unlinkDown = true;

                if (!unlinkUp && !unlinkDown)
                    throw new Exception("Cannot unlink from node");

                // make sure old links are notified of change
                List<LocationData> locations = new List<LocationData>();

                // remove node as an uplink
                OpLink parent = null;

                if (unlinkUp)
                {
                    GetLocs(Core.LocalDhtID, project, 1, 1, locations);

                    parent = localLink.Uplink;
                    localLink.ResetUplink();
                    localLink.Uplink = null;
                }

                // remove node from downlinks
                if (unlinkDown)
                {
                    localLink.Confirmed.Remove(remoteLink.DhtID);

                    // removal of uplink requests done when version is updated by updatelocal
                }

                // update
                SaveLocal();

                // notify old links of change
                Core.RunInCoreAsync(delegate()
                {
                    OpVersionedFile file = Cache.GetFile(Core.LocalDhtID);

                    Store.PublishDirect(locations, Core.LocalDhtID, ServiceID, 0, file.SignedHeader);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(Core.GuiMain, ex.Message);
            }
        }

        void Core_SecondTimer()
        {
            //crit remove projects no longer referenced, call for projects refresh
            // location updates are done for nodes in link map that are focused or linked
            // node comes online how to know to search for it, every 10 mins?

            // do only once per second if needed
            // branches only change when a profile is updated
            if (RunSaveUplinks)
            {
                RefreshLinked();
                SaveUplinkReqs();
            }
        }

        void Core_MinuteTimer()
        {
            // clean roots
            List<uint> removeList = new List<uint>();

            ProjectRoots.LockReading(delegate()
            {
                foreach (uint project in ProjectRoots.Keys)
                    if (ProjectRoots[project].SafeCount == 0)
                        removeList.Add(project);
            });

            if (removeList.Count > 0)
                ProjectRoots.LockWriting(delegate()
               {
                   foreach (uint project in removeList)
                       ProjectRoots.Remove(project);
                   //ProjectNames.Remove(id); // if we are only root, and leave project, but have downlinks, still need the name
               });
        }


        void Core_GetFocusedCore()
        {
            RefreshLinked();

            TrustMap.LockReading(delegate()
            {
                foreach (OpTrust trust in TrustMap.Values)
                    if (trust.InLocalLinkTree)
                        Core.Focused.SafeAdd(trust.DhtID, true);

                    // if in bounds, set highers of node to focused
                    // because if highers removed, they will just be re-added when inbounds link cache is refreshed
                    else if (Network.Routing.InCacheArea(trust.DhtID))
                        foreach(OpLink link in trust.Links.Values)
                            foreach(ulong id in link.GetHighers())
                                Core.Focused.SafeAdd(id, true);
            });



            //crit needs to update live tree as well
        }

        void Cache_FileRemoved(OpVersionedFile file)
        {
            OpTrust trust = GetTrust(file.DhtID);

            if (trust == null)
                return;

            ProjectRoots.LockReading(delegate()
            {
                foreach (uint project in trust.Links.Keys)
                    if(ProjectRoots.ContainsKey(project))
                        ProjectRoots[project].SafeRemove(trust.Links[project]);
            });

            trust.Reset();
            trust.Loaded = false;
            TrustMap.SafeRemove(file.DhtID);
            
            // alert services/gui
            if (LinkUpdate != null)
                LinkUpdate.Invoke(trust);

            Core.RunInGuiThread(GuiUpdate, trust.DhtID);
        }

        void RefreshLinked()
        {
            // unmark all nodes

            TrustMap.LockReading(delegate()
            {
                foreach (OpTrust trust in TrustMap.Values)
                    trust.InLocalLinkTree = false;


                // TraverseDown 2 from self
                foreach (OpLink link in LocalTrust.Links.Values)
                {
                    uint project = link.Project;

                    MarkBranchLinked(link, 2);

                    // TraverseDown 1 from all parents above self
                    List<ulong> uplinks = GetUplinkIDs(LocalTrust.DhtID, project, false);

                    foreach (ulong id in uplinks)
                    {
                        OpLink uplink = GetLink(id, project);

                        if (uplink != null)
                            MarkBranchLinked(uplink, 1);
                    }

                    // TraverseDown 2 from Roots
                    // dont keep focused on every untrusted node, will overwhelm us
                    // other processes will keep right about of contacts and delete furthest
                    /*List<OpLink> roots = null;
                    if (ProjectRoots.SafeTryGetValue(project, out roots))
                        foreach (OpLink root in roots)
                        {
                            // structure known if node found with no uplinks, and a number of downlinks
                            if (project == 0 && root.Trust.Loaded && root.Uplink == null)
                                if (root.Downlinks.Count > 0 && TrustMap.Count > 8)
                                    StructureKnown = true;

                            MarkBranchLinked(root, 2);
                        }*/
                }
            });
        }

        void MarkBranchLinked(OpLink link, int depth)
        {
            link.Trust.InLocalLinkTree = true;

            if (!link.Trust.Searched)
            {
                Core.Locations.StartSearch(link.DhtID, 0, false);

                link.Trust.Searched = true;
            }

            if (depth > 0)
                foreach (OpLink downlink in link.Downlinks)
                    MarkBranchLinked(downlink, depth - 1);
        }

        internal void Research(ulong key, uint project, bool searchDownlinks)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { Research(key, project, searchDownlinks); });
                return;
            }

            if (!Network.Responsive)
                return;

            List<ulong> searchList = new List<ulong>();

            searchList.Add(key);

            OpLink link = GetLink(key, project);

            if (link != null)
            {
                // process_linkdata should add confirmed ids to linkmap, but they are not in downlinks list
                // unless the file is loaded (only links that specify their uplink as node x are in node x's downlink list

                // searchDownlinks - true re-search downlinks, false only search ids that are NOT in downlinks or linkmap

                List<ulong> downlinks = new List<ulong>();

                foreach (OpLink downlink in link.Downlinks)
                {
                    if (searchDownlinks)
                        searchList.Add(downlink.DhtID);

                    downlinks.Add(downlink.DhtID);
                }

                foreach (ulong id in link.Confirmed)
                    if (!searchList.Contains(id))
                        if (searchDownlinks || (!TrustMap.SafeContainsKey(id) && !downlinks.Contains(id)))
                            searchList.Add(id);

                foreach (UplinkRequest request in link.Requests)
                    if (!searchList.Contains(request.KeyID))
                        if (searchDownlinks || (!TrustMap.SafeContainsKey(request.KeyID) && !downlinks.Contains(request.KeyID)))
                            searchList.Add(request.KeyID);
            }


            foreach (ulong id in searchList)
                Cache.Research(id);
        }

        void Search_Local(ulong key, byte[] parameters, List<byte[]> results)
        {
            uint minVersion = BitConverter.ToUInt32(parameters, 0);

            OpTrust trust = GetTrust(key);

            if (trust != null)
                foreach (OpLink link in trust.Links.Values)
                    foreach (UplinkRequest request in link.Requests)
                        if (request.TargetVersion > minVersion)
                            results.Add(request.Signed);
        }

        internal void RoutingUpdate(DhtContact contact)
        {
            //*** if enough nodes in local neighborhood that are untrusted then they need to figure out
            // among themselves what the deal is, because entire network could be clueless people
            // and this line of code will flood the network to aimless searches

            // find node if structure not known
            //if (StructureKnown)
            //    return;

            //OpTrust trust = GetTrust(contact.DhtID);

            //if (trust == null)
            //    Cache.Research(contact.DhtID);
        }

        void Store_Local(DataReq store)
        {
            // getting published to - search results - patch

            SignedData signed = SignedData.Decode(Core.Protocol, store.Data);

            if (signed == null)
                return;

            G2Header embedded = new G2Header(signed.Data);

            // figure out data contained
            if (Core.Protocol.ReadPacket(embedded))
                if (embedded.Name == TrustPacket.UplinkReq)
                    Process_UplinkReq(store, signed, UplinkRequest.Decode(Core.Protocol, embedded));
        }

        internal void SaveLocal()
        {
            try
            {
                RijndaelManaged key = new RijndaelManaged();
                key.GenerateKey();
                key.IV = new byte[key.IV.Length]; 

                // create new link file in temp dir
                string tempPath = Core.GetTempPath();
                FileStream tempFile = new FileStream(tempPath, FileMode.CreateNew);
                CryptoStream stream = new CryptoStream(tempFile, key.CreateEncryptor(), CryptoStreamMode.Write);


                // project packets
                foreach (OpLink link in LocalTrust.Links.Values)
                    if (link.Active)
                    {
                        ProjectData project = new ProjectData();
                        project.ID = link.Project;
                        project.Name = GetProjectName(link.Project);

                        if (link.Project == 0)
                            project.UserName = LocalTrust.Name;

                        project.UserTitle = link.Title;

                        byte[] packet = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, project);
                        stream.Write(packet, 0, packet.Length);


                        // uplinks
                        if (link.Uplink != null)
                        {
                            LinkData data = new LinkData(link.Project, link.Uplink.Trust.File.Key, true);
                            packet = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, data);
                            stream.Write(packet, 0, packet.Length);
                        }

                        // downlinks
                        foreach (OpLink downlink in link.Downlinks)
                            if (link.Confirmed.Contains(downlink.DhtID))
                            {
                                LinkData data = new LinkData(link.Project, downlink.Trust.File.Key, false);
                                packet = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, data);
                                stream.Write(packet, 0, packet.Length);
                            }
                    }

                stream.WriteByte(0); // signal last packet

                stream.FlushFinalBlock();
                stream.Close();

                OpVersionedFile file = Cache.UpdateLocal(tempPath, key, null);

                Store.PublishDirect(GetLocsAbove(), Core.LocalDhtID, ServiceID, DataTypeFile, file.SignedHeader);

                SaveUplinkReqs();
            }
            catch (Exception ex)
            {
                Network.UpdateLog("LinkControl", "Error updating local " + ex.Message);
            }
        }

        void SaveUplinkReqs()
        {
            RunSaveUplinks = false;

            try
            {
                string tempPath = Core.GetTempPath();
                FileStream file = new FileStream(tempPath, FileMode.Create);
                CryptoStream stream = new CryptoStream(file, LocalFileKey.CreateEncryptor(), CryptoStreamMode.Write);

                TrustMap.LockReading(delegate()
                {
                    foreach (OpTrust trust in TrustMap.Values)
                        foreach (OpLink link in trust.Links.Values)
                            foreach (UplinkRequest request in link.Requests)
                                stream.Write(request.Signed, 0, request.Signed.Length);
                });

                stream.FlushFinalBlock();
                stream.Close();


                string finalPath = LinkPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "uplinks");
                File.Delete(finalPath);
                File.Move(tempPath, finalPath);
            }
            catch (Exception ex)
            {
                Network.UpdateLog("LinkControl", "Error saving links " + ex.Message);
            }
        }

        private void LoadUplinkReqs()
        {
            try
            {
                string path = LinkPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "uplinks");

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
                            if (embedded.Name == TrustPacket.UplinkReq)
                                Process_UplinkReq(null, signed, UplinkRequest.Decode(Core.Protocol, embedded));
                    }

                stream.Close();
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Link", "Error loading links " + ex.Message);
            }
        }

        private void Process_UplinkReq(DataReq data, SignedData signed, UplinkRequest request)
        {
            Core.IndexKey(request.KeyID, ref request.Key);
            Core.IndexKey(request.TargetID, ref request.Target);

            if (!Utilities.CheckSignedData(request.Key, signed.Data, signed.Signature))
                return;

            OpTrust requesterTrust = GetTrust(request.KeyID);

            if (requesterTrust != null && requesterTrust.Loaded && requesterTrust.File.Header.Version > request.LinkVersion)
                return;

            // check if target in linkmap, if not add
            OpTrust targetTrust = GetTrust(request.TargetID);

            if (targetTrust == null)
            {
                targetTrust = new OpTrust(new OpVersionedFile(request.Target) );
                TrustMap.SafeAdd(request.TargetID, targetTrust);
            }

            if (targetTrust.Loaded && targetTrust.File.Header.Version > request.TargetVersion)
                return;

            request.Signed = signed.Encode(Core.Protocol); // so we can send it in results / save, later on

            // check for duplicate requests
            OpLink targetLink = targetTrust.GetLink(request.ProjectID);

            if (targetLink != null)
            {
                foreach (UplinkRequest compare in targetLink.Requests)
                    if (Utilities.MemCompare(compare.Signed, request.Signed))
                        return;
            }
            else
            {
                targetTrust.AddProject(request.ProjectID, true);
                targetLink = targetTrust.GetLink(request.ProjectID);
            }

            // add
            targetLink.Requests.Add(request);


            // if target is marked as linked or focused, update link of target and sender
            if (targetTrust.Loaded && (targetTrust.InLocalLinkTree || Core.Focused.SafeContainsKey(targetTrust.DhtID)))
            {
                if (targetTrust.File.Header.Version < request.TargetVersion)
                    Cache.Research(targetTrust.DhtID);

                if (requesterTrust == null)
                {
                    requesterTrust = new OpTrust(new OpVersionedFile(request.Key));
                    TrustMap.SafeAdd(request.KeyID, requesterTrust);
                }

                // once new version of requester's link file has been downloaded, interface will be updated
                if (!requesterTrust.Loaded || (requesterTrust.File.Header.Version < request.LinkVersion))
                    Cache.Research(requesterTrust.DhtID);
            }

            RunSaveUplinks = true;
        }

        private void Cache_FileAquired(OpVersionedFile cachefile)
        {

            try
            {

                // get link directly, even if in unloaded state we need the same reference
                OpTrust trust = null;
                TrustMap.SafeTryGetValue(cachefile.DhtID, out trust);

                if (trust == null)
                {
                    trust = new OpTrust(cachefile);
                    TrustMap.SafeAdd(cachefile.DhtID, trust);
                }
                else
                    trust.File = cachefile;

                // clean roots, if link has loopID, remove loop node entirely, it will be recreated if needed later
                ProjectRoots.LockReading(delegate()
                {
                    foreach (uint project in ProjectRoots.Keys)
                    {
                        OpLink link = trust.GetLink(project);

                        if (link == null)
                            continue;

                        ThreadedList<OpLink> roots = ProjectRoots[project];

                        roots.SafeRemove(link);

                        // remove loop node
                        if (link.LoopRoot != null)
                            roots.LockReading(delegate()
                            {
                                foreach (OpLink root in roots)
                                    if (root.DhtID == link.LoopRoot.DhtID)
                                    {
                                        roots.SafeRemove(root); // root is a loop node

                                        // remove associations with loop node
                                        foreach (OpLink downlink in root.Downlinks)
                                            downlink.LoopRoot = null;

                                        break;
                                    }
                            });
                    }
                });

                trust.Reset();


                // load data from link file
                TaggedStream file = new TaggedStream(Cache.GetFilePath(cachefile.Header));
                CryptoStream crypto = new CryptoStream(file, cachefile.Header.FileKey.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Core.Protocol, FileAccess.Read);

                G2Header packetRoot = null;

                while (stream.ReadPacket(ref packetRoot))
                    if (packetRoot.Name == DataPacket.SignedData)
                    {
                        SignedData signed = SignedData.Decode(Core.Protocol, packetRoot);
                        G2Header embedded = new G2Header(signed.Data);

                        // figure out data contained
                        if (Core.Protocol.ReadPacket(embedded))
                        {
                            if (embedded.Name == TrustPacket.ProjectData)
                                Process_ProjectData(trust, signed, ProjectData.Decode(Core.Protocol, embedded));

                            else if (embedded.Name == TrustPacket.LinkData)
                                Process_LinkData(trust, signed, LinkData.Decode(Core.Protocol, embedded));
                        }
                    }

                stream.Close();

                // set new header
                trust.Loaded = true;

                // set as root if node has no uplinks
                foreach (OpLink link in trust.Links.Values)
                    if (link.Uplink == null)
                        AddRoot(link);

                // add root for projects this node is not apart of - above code should do this fine
                /* foreach (uint project in trust.Downlinks.Keys)
                    if (!trust.Projects.Contains(project) && !trust.Uplink.ContainsKey(project))
                        AddRoot(project, trust);*/

                // if loop created, create new loop node with unique ID, assign all nodes in loop the ID and add as downlinks
                foreach (OpLink link in trust.Links.Values)
                    if (IsLooped(link))
                    {
                        uint project = link.Project;

                        OpLink loop = new OpTrust(project, (ulong)Core.RndGen.Next()).GetLink(project);
                        loop.IsLoopRoot = true;

                        List<ulong> uplinks = GetUnconfirmedUplinkIDs(trust.DhtID, project);
                        uplinks.Add(trust.DhtID);

                        foreach (ulong uplink in uplinks)
                        {
                            OpLink member = GetLink(uplink, project);

                            if (member == null)
                                continue;

                            member.LoopRoot = loop;

                            loop.Downlinks.Add(member);
                            loop.Confirmed.Add(member.DhtID); //needed for getlowers
                        }

                        AddRoot(loop);
                    }


                trust.CheckRequestVersions();


                if (LinkUpdate != null)
                    LinkUpdate.Invoke(trust);

                if (Core.NewsWorthy(trust.DhtID, 0, false))
                    Core.MakeNews("Trust updated by " + GetName(trust.DhtID), trust.DhtID, 0, true, LinkRes.link, null);


                // update subs
                if (Network.Established)
                {
                    List<LocationData> locations = new List<LocationData>();

                    ProjectRoots.LockReading(delegate()
                    {
                        foreach (uint project in ProjectRoots.Keys)
                            if (Core.LocalDhtID == trust.DhtID || IsHigher(trust.DhtID, project))
                                GetLocsBelow(Core.LocalDhtID, project, locations);
                    });

                    Store.PublishDirect(locations, trust.DhtID, ServiceID, 0, cachefile.SignedHeader);
                }

                // update interface node
                Core.RunInGuiThread(GuiUpdate, trust.DhtID);

                foreach (OpLink link in trust.Links.Values)
                    foreach (OpLink downlink in link.Downlinks)
                        Core.RunInGuiThread(GuiUpdate, downlink.DhtID);

            }
            catch (Exception ex)
            {
                Network.UpdateLog("Link", "Error loading file " + ex.Message);
            }
        }

        private void AddRoot(OpLink link)
        {
            ThreadedList<OpLink> roots = null;

            if (!ProjectRoots.SafeTryGetValue(link.Project, out roots))
            {
                roots = new ThreadedList<OpLink>();
                ProjectRoots.SafeAdd(link.Project, roots);
            }

            if (!roots.SafeContains(link)) // possible it wasnt removed above because link might not be part of project locally but others think it does (uplink)
                roots.SafeAdd(link);
        }

        private void Process_ProjectData(OpTrust trust, SignedData signed, ProjectData project)
        {
            if (!Utilities.CheckSignedData(trust.File.Key, signed.Data, signed.Signature))
                return;

            if (project.ID != 0 && !ProjectNames.SafeContainsKey(project.ID))
                ProjectNames.SafeAdd(project.ID, project.Name);

            trust.AddProject(project.ID, true);

            if (project.ID == 0)
                trust.Name = project.UserName;

            OpLink link = trust.GetLink(project.ID);

            link.Title = project.UserTitle;
        }

        private void Process_LinkData(OpTrust trust, SignedData signed, LinkData linkData)
        {
            if (!Utilities.CheckSignedData(trust.File.Key, signed.Data, signed.Signature))
                return;

            Core.IndexKey(linkData.TargetID, ref linkData.Target);

            uint project = linkData.Project;

            OpLink localLink = trust.GetLink(project);

            if (localLink == null)
                return;

            OpTrust targetTrust = GetTrust(linkData.TargetID, false);

            if (targetTrust == null)
            {
                targetTrust = new OpTrust(new OpVersionedFile(linkData.Target));
                TrustMap.SafeAdd(linkData.TargetID, targetTrust);
            }

            targetTrust.AddProject(project, false);
            OpLink targetLink = targetTrust.GetLink(project);

            if (linkData.Uplink)
            {
                localLink.Uplink = targetLink;

                targetLink.Downlinks.Add(localLink);

                // always know a link's trust structure to the top
                if (!targetTrust.Loaded)
                    Cache.Research(targetTrust.DhtID);

                if (targetLink.Uplink == null)
                    AddRoot(targetLink);
            }

            else
            {
                localLink.Confirmed.Add(targetLink.DhtID);
            }
        }

        internal uint CreateProject(string name)
        {
            uint id = (uint)Core.RndGen.Next();

            ProjectNames.SafeAdd(id, name);
            LocalTrust.AddProject(id, true);

            ThreadedList<OpLink> roots = new ThreadedList<OpLink>();
            roots.SafeAdd(LocalTrust.GetLink(id));
            ProjectRoots.SafeAdd(id, roots);

            SaveLocal();

            return id;
        }

        internal void JoinProject(uint project)
        {
            if (project == 0)
                return;

            LocalTrust.AddProject(project, true);

            SaveLocal();
        }

        internal void LeaveProject(uint project)
        {
            if (project == 0)
                return;

            // update local peers we are leaving
            List<LocationData> locations = new List<LocationData>();
            GetLocs(Core.LocalDhtID, project, 1, 1, locations);
            GetLocsBelow(Core.LocalDhtID, project, locations);

            LocalTrust.RemoveProject(project);

            SaveLocal();

            // update links in old project of update
            Core.RunInCoreAsync(delegate()
            {
                OpVersionedFile file = Cache.GetFile(Core.LocalDhtID);
                Store.PublishDirect(locations, Core.LocalDhtID, ServiceID, 0, file.SignedHeader);
            });
        }

        internal string GetName(ulong id)
        {
            OpTrust trust = GetTrust(id);

            if (trust != null && trust.Name.Trim() != "")
                return trust.Name;

            string name = id.ToString();
            return (name.Length > 5) ? name.Substring(0, 5) : name;
        }

        internal string GetProjectName(uint id)
        {
            string name = null;
            if (ProjectNames.SafeTryGetValue(id, out name))
                if (name.Trim() != "")
                    return name;

            name = id.ToString();
            return (name.Length > 5) ? name.Substring(0, 5) : name;
        }

        internal void GetLocs(ulong id, uint project, int up, int depth, List<LocationData> locations)
        {
            OpLink link = GetLink(id, project);

            if (link == null)
                return;

            OpLink uplink = TraverseUp(link, up);

            if (uplink != null)
                GetLinkLocs(uplink, depth, locations);

            // if at top, get nodes around roots
            else
            {
                ThreadedList<OpLink> roots = null;
                if (ProjectRoots.SafeTryGetValue(project, out roots))
                    roots.LockReading(delegate()
                    {
                        foreach (OpLink root in roots)
                            GetLinkLocs(root, 1, locations);
                    });
            }
        }

        internal void GetLocsBelow(ulong id, uint project, List<LocationData> locations)
        {
            GetLocsBelow(id, id, project, locations);
        }

        internal void GetLocsBelow(ulong id, ulong root, uint project, List<LocationData> locations)    
        {
            // this is a spam type function that finds all locations (online nodes) below
            // a certain link.  it stops traversing down when an online node is found in a branch
            // the online node will call this function to continue traversing data down the network
            // upon being updated with the data object sent by whoever is calling this function

            // root prevents problem with trust loops

            OpLink link = GetLink(id, project);

            if (link != null)
                foreach (OpLink child in link.Downlinks)
                    if (child.DhtID != root && !AddLinkLocations(child, locations))
                        GetLocsBelow(child.DhtID, project, locations);
        }
        private void GetLinkLocs(OpLink parent, int depth, List<LocationData> locations)
        {
            AddLinkLocations(parent, locations);

            if (depth > 0)
                foreach (OpLink child in parent.Downlinks)
                    if (!parent.IsLoopedTo(child))
                        GetLinkLocs(child, depth - 1, locations);
        }

        private bool AddLinkLocations(OpLink link, List<LocationData> locations)
        {
            List<ClientInfo> clients = Core.Locations.GetClients(link.DhtID);

            foreach (ClientInfo info in clients)
            {
                if (info.Data.DhtID == Core.LocalDhtID && info.Data.Source.ClientID == Core.ClientID)
                    continue;

                if (!locations.Contains(info.Data))
                    locations.Add(info.Data);
            }

            return (locations.Count > 0);
        }


        internal List<LocationData> GetLocsAbove()
        {
            List<LocationData> locations = new List<LocationData>();

            ProjectRoots.LockReading(delegate()
            {
                foreach (uint project in ProjectRoots.Keys)
                    GetLocs(Core.LocalDhtID, project, 1, 1, locations);  // below done by cacheplan
            });

            return locations;
        }

        private OpLink TraverseUp(OpLink link, int distance)
        {
            // needs to get unconfiremd ids so unconfirmed above / below are updated with link status

            if (distance == 0)
                return link;

            int traverse = 0;

            OpLink uplink = link.GetHigher(false);

            while (uplink != null)
            {
                traverse++;
                if (traverse == distance)
                    return uplink;

                uplink = uplink.GetHigher(false);
            }

            return null;
        }


        internal bool IsHigher(ulong key, uint project)
        {
            return IsHigher(Core.LocalDhtID, key, project, true);
        }

        internal bool IsUnconfirmedHigher(ulong key, uint project)
        {
            return IsHigher(Core.LocalDhtID, key, project, false);
        }

        internal bool IsHigher(ulong localID, ulong key, uint project)
        {
            return IsHigher(localID, key, project, true);
        }

        internal bool IsUnconfirmedHigher(ulong localID, ulong key, uint project)
        {
            return IsHigher(localID, key, project, false);
        }

        private bool IsHigher(ulong localID, ulong higherID, uint project, bool confirmed)
        {
            OpTrust local = GetTrust(localID);

            if (local == null)
                return false;

            List<ulong> uplinks = GetUplinkIDs(localID, project, confirmed);

            if (uplinks.Count == 0)
                return false;

            if (uplinks.Contains(higherID))
                return true;

            // check if higher ID being checked is the loop root ID
            OpLink highest = GetLink(uplinks[uplinks.Count - 1], project);

            if (highest != null && 
                highest.LoopRoot != null && 
                highest.LoopRoot.DhtID == higherID)
                return true;

            return false;
        }


        internal bool IsLower(ulong localID, ulong lowerID, uint project)
        {
            List<ulong> uplinks = GetUplinkIDs(lowerID, project, true);

            if (uplinks.Contains(localID))
                return true;

            return false;
        }

        internal bool IsLooped(OpLink local)
        {
            // this function is the same as getUplinkIDs with minor mods
            List<ulong> list = new List<ulong>();

            OpLink uplink = local.GetHigher(false);

            while (uplink != null)
            {
                // if loop lead back to self, link is in loop
                if (uplink == local)
                    return true;

                // if there is a loop higher up, but link is not in it, return
                if (list.Contains(uplink.DhtID))
                    return false;

                list.Add(uplink.DhtID);

                uplink = uplink.GetHigher(false);
            }

            return false;
        }

        internal List<ulong> GetUplinkIDs(ulong id, uint project)
        {
            return GetUplinkIDs(id, project, true);
        }

        internal List<ulong> GetUnconfirmedUplinkIDs(ulong id, uint project)
        {
            return GetUplinkIDs(id, project, false);
        }

        private List<ulong> GetUplinkIDs(ulong local, uint project, bool confirmed)
        {
            // get uplinks from id, not including id, starting with directly above and ending with root

            List<ulong> list = new List<ulong>();

            OpLink link = GetLink(local, project);

            if (link == null)
                return list;

            OpLink uplink = link.GetHigher(confirmed);

            while (uplink != null)
            {
                // if looping, return
                if (uplink.DhtID == local || list.Contains(uplink.DhtID))
                    return list;

                list.Add(uplink.DhtID);

                uplink = uplink.GetHigher(confirmed);
            }

            return list;
        }

        internal List<ulong> GetAutoInheritIDs(ulong local, uint project)
        {
            // get uplinks from local, including first id in loop, but no more

            OpLink link = GetLink(local, project);

            if (link == null)
                return new List<ulong>();

            return link.GetHighers();
        }

        internal List<ulong> GetAdjacentIDs(ulong id, uint project)
        {
            List<ulong> list = new List<ulong>();

            OpLink link = GetLink(id, project);

            if (link == null)
                return list;

            OpLink uplink = link.GetHigher(true);

            if (uplink == null || link.IsLoopedTo(uplink))
                return list;

            foreach (OpLink sub in uplink.GetLowers(true))
                list.Add(sub.DhtID);

            list.Remove(id);

            return list;
        }

        internal List<ulong> GetDownlinkIDs(ulong id, uint project, int levels)
        {
            List<ulong> list = new List<ulong>();

            OpLink link = GetLink(id, project);

            if (link == null)
                return list;

            levels--;

            foreach (OpLink downlink in link.Downlinks)
                if (!link.IsLoopedTo(downlink))
                    if (link.Confirmed.Contains(downlink.DhtID))
                    {
                        list.Add(downlink.DhtID);

                        if (levels > 0)
                            list.AddRange(GetDownlinkIDs(downlink.DhtID, project, levels));
                    }

            return list;
        }

        internal bool HasSubs(ulong id, uint project)
        {
            int count = 0;

            OpLink link = GetLink(id, project);

            if (link != null)
                foreach (OpLink downlink in link.Downlinks)
                    if (!link.IsLoopedTo(downlink))
                        if (link.Confirmed.Contains(downlink.DhtID))
                            count++;

            return count > 0;
        }

        internal bool IsAdjacent(ulong id, uint project)
        {
            OpLink link = LocalTrust.GetLink(project);

            if (link == null)
                return false;

            OpLink higher = link.GetHigher(true);

            if (higher != null && higher.Confirmed.Contains(id))
                foreach (OpLink downlink in higher.Downlinks)
                    if (!higher.IsLoopedTo(downlink))
                        if (downlink.DhtID == id)
                            return true;

            return false;
        }

        internal bool IsLowerDirect(ulong id, uint project)
        {
            OpLink link = LocalTrust.GetLink(project);

            if (link != null && link.Confirmed.Contains(id))
                foreach (OpLink downlink in link.Downlinks)
                    if (!link.IsLoopedTo(downlink))
                        if (downlink.DhtID == id)
                            return true;

            return false;
        }

        internal bool IsHigherDirect(ulong id, uint project)
        {
            OpLink link = LocalTrust.GetLink(project);

            if (link == null)
                return false;

            OpLink uplink = link.GetHigher(true);

            if (uplink == null || link.IsLoopedTo(uplink))
                return false;

            return uplink.DhtID == id;
        }

        internal bool IsInScope(Dictionary<ulong, short> scope, ulong testID, uint project)
        {
            // get inherit ids because scope ranges dont work in loops
            List<ulong> uplinks = GetAutoInheritIDs(testID, project);


            // loop through all the scope permissions
            foreach (ulong id in scope.Keys)
            {
                if (id == testID)
                    return true;

                if (uplinks.Contains(id))
                {
                    if (scope[id] == -1) // everyone below
                        return true;

                    // i+1 is the levels above test, scope[id] is the # of sub-levels the scope includes
                    // so if node is 2 levels above (i=2) had a scope[id] >= 2, test would be allowed, otherwise not
                    int i = uplinks.IndexOf(id) + 1;

                    if (i - scope[id] <= 0)
                        return true;
                }
            }

            return false;
        }

        OpTrust GetTrust(ulong id, bool loaded)
        {
            OpTrust trust = null;

            if (TrustMap.SafeTryGetValue(id, out trust))
                if (!loaded || trust.Loaded)
                    return trust;

            return null;
        }

        internal OpTrust GetTrust(ulong id)
        {
            return GetTrust(id, true);
        }

        internal OpLink GetLink(ulong id, uint project)
        {
            OpTrust trust = GetTrust(id, true);

            if (trust == null)
                return null;

            return trust.GetLink(project);
        }
    }

    [DebuggerDisplay("{Name}")]
    internal class OpTrust
    {
        internal string Name = "Unknown";
        
        internal bool Loaded;
        internal ulong LoopID;

        internal bool InLocalLinkTree;
        internal bool Searched;

        internal Dictionary<uint, OpLink> Links = new Dictionary<uint, OpLink>();

        internal OpVersionedFile File;


        internal OpTrust(OpVersionedFile file)
        {
            File = file;
        }

        internal ulong DhtID
        {
            get
            {
                return (File == null) ? LoopID : File.DhtID;
            }
        }

        // loop root object should now be OpLink
        internal OpTrust(uint project, ulong loopID)
        {
            Loaded = true;
            Name = "Trust Loop";
            LoopID = loopID;
            AddProject(project, true);
        }

        internal bool InProject(uint project)
        {
            OpLink link = GetLink(project);

            return link != null && link.Active;
        }

        internal void AddProject(uint project, bool active)
        {
            OpLink link = GetLink(project);

            if (link == null)
            {
                link = new OpLink(this, project);
                Links.Add(project, link);
            }

            // setting project to non-active has to be done manually
            if(!link.Active) // only allow false->true, not true->false
                link.Active = active;
        }

        internal void RemoveProject(uint project)
        {
            OpLink link = GetLink(project);

            if (link == null)
                return;

            link.Active = false;
            link.Uplink = null;
            link.Title = "";
            link.Confirmed.Clear();

            // downlinks remain so structure can still be seen
        }

        internal OpLink GetLink(uint project)
        {
            OpLink link = null;

            Links.TryGetValue(project, out link);

            return link;
        }

        internal void Reset()
        {
            List<uint> remove = new List<uint>();

            foreach (OpLink link in Links.Values)
            {
                link.Reset();

                if(link.Downlinks.Count == 0)
                    remove.Add(link.Project);
            }

            foreach (uint project in remove)
                Links.Remove(project);
        }

        internal void CheckRequestVersions()
        {
            List<UplinkRequest> removeList = new List<UplinkRequest>();

            // check target
            foreach (OpLink link in Links.Values)
            {
                removeList.Clear();

                foreach (UplinkRequest request in link.Requests)
                    if (request.TargetVersion < link.Trust.File.Header.Version)
                        removeList.Add(request);

                foreach (UplinkRequest request in removeList)
                    link.Requests.Remove(request);
            }
        }


    }

    [DebuggerDisplay("{Trust.Name}")]
    internal class OpLink
    {
        internal OpTrust Trust;
        internal uint Project;
        internal bool Active;
        internal string Title = "";

        internal bool IsLoopRoot;
        internal OpLink LoopRoot;

        internal OpLink Uplink;
        internal List<OpLink> Downlinks = new List<OpLink>();
        internal List<ulong> Confirmed = new List<ulong>();
        internal List<UplinkRequest> Requests = new List<UplinkRequest>();

        internal ulong DhtID
        {
            get
            {
                return Trust.DhtID;
            }
        }

        internal OpLink(OpTrust trust, uint project)
        {
            Trust = trust;
            Project = project;
        }

        internal void Reset()
        {
            // find nodes we're uplinked to and remove ourselves from their downlink list
            ResetUplink();

            // only clear downlinks that are no longer uplinked to us
            List<OpLink> remove = new List<OpLink>();

            foreach (OpLink downlink in Downlinks)
                if (downlink.Uplink == null || downlink.Uplink != this)
                    remove.Add(downlink);

            foreach (OpLink downlink in remove)
                Downlinks.Remove(downlink);

            Active = false;
            Title = "";

            Uplink = null;
            Confirmed.Clear();
        }

        internal bool IsLoopedTo(OpLink test)
        {
            if (test.LoopRoot != null && test.LoopRoot == LoopRoot)
                return true;

            return false;
        }

        internal OpLink GetHigher(bool confirmed)
        {
            if (Uplink == null)
                return null;

            if (!confirmed)
                return Uplink;

            // if we are one of the uplinks confirmed downlinks then return trusted uplink
            if (Uplink.Confirmed.Contains(DhtID))
                return Uplink;

            return null;
        }

        internal List<OpLink> GetLowers(bool confirmed)
        {
            List<OpLink> lowers = new List<OpLink>();

            foreach (OpLink downlink in Downlinks)
                if (IsLoopRoot || !IsLoopedTo(downlink)) // chat uses getlowers on looproot
                    if (!confirmed || Confirmed.Contains(downlink.DhtID))
                        lowers.Add(downlink);

            return lowers;
        }

        internal void ResetUplink()
        {
            if (Uplink != null)
                Uplink.RemoveDownlink(this);
        }

        private void RemoveDownlink(OpLink downlink)
        {
            if (Downlinks.Contains(downlink))
                Downlinks.Remove(downlink);

            // uplink requests are invalidated on verion update also
            // not the local ones, but the ones this link issued to previous uplink
            foreach (UplinkRequest request in Requests)
                if (request.KeyID == downlink.DhtID)
                {
                    Requests.Remove(request);
                    break;
                }
        }


        internal List<ulong> GetHighers()
        {
            List<ulong> list = new List<ulong>();

            if (LoopRoot != null)
                return list;

            OpLink uplink = GetHigher(true);

            while (uplink != null)
            {
                list.Add(uplink.DhtID);

                uplink = uplink.GetHigher(true);

                if (uplink != null && uplink.LoopRoot != null)
                    return list;
            }

            return list;
        }
    }
}
