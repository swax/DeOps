using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Protocol.Special;

using DeOps.Services.Assist;
using DeOps.Services.Location;
using DeOps.Services.Transfer;


namespace DeOps.Services.Trust
{
    internal delegate void LinkUpdateHandler(OpTrust trust);
    internal delegate void LinkGuiUpdateHandler(ulong key);


    class TrustService : OpService
    {
        public string Name { get { return "Trust"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.Trust; } }

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
        byte[] LocalFileKey;

        internal LinkUpdateHandler LinkUpdate;
        internal LinkGuiUpdateHandler GuiUpdate;


        internal TrustService(OpCore core)
        {
            Core = core;
            Core.Trust = this;

            Store = Core.Network.Store;
            Network = Core.Network;

            Core.SecondTimerEvent += new TimerHandler(Core_SecondTimer);
            Core.MinuteTimerEvent += new TimerHandler(Core_MinuteTimer);
            Core.KeepDataCore += new KeepDataHandler(Core_KeepData);

            Cache = new VersionedCache(Network, ServiceID, DataTypeFile, false);

            Network.CoreStatusChange += new StatusChange(Network_StatusChange);

            // piggyback searching for uplink requests on cache file data
            Store.StoreEvent[ServiceID, DataTypeFile] += new StoreHandler(Store_Local);
            Network.Searches.SearchEvent[ServiceID, DataTypeFile] += new SearchRequestHandler(Search_Local);

            Core.Locations.KnowOnline += new KnowOnlineHandler(Location_KnowOnline);

            Cache.FileAquired += new FileAquiredHandler(Cache_FileAquired);
            Cache.FileRemoved += new FileRemovedHandler(Cache_FileRemoved);
            Cache.Load();

            ProjectNames.SafeAdd(0, Core.User.Settings.Operation);

            LinkPath = Core.User.RootPath + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + ServiceID.ToString();
            Directory.CreateDirectory(LinkPath);

            LocalFileKey = Core.User.Settings.FileKey;

            LoadUplinkReqs();


            LocalTrust = GetTrust(Core.UserID);


            if (LocalTrust == null)
            {
                LocalTrust = new OpTrust(new OpVersionedFile(Core.User.Settings.KeyPublic));
                TrustMap.SafeAdd(Core.UserID, LocalTrust);
            }

            if (!LocalTrust.Loaded)
            {
                LocalTrust.Name = Core.User.Settings.UserName;
                LocalTrust.AddProject(0, true); // operation

                SaveLocal();
            }
        }

        public void Dispose()
        {
            
            Core.SecondTimerEvent -= new TimerHandler(Core_SecondTimer);
            Core.MinuteTimerEvent -= new TimerHandler(Core_MinuteTimer);
            Core.KeepDataCore -= new KeepDataHandler(Core_KeepData);

            Network.CoreStatusChange -= new StatusChange(Network_StatusChange);

            Store.StoreEvent[ServiceID, DataTypeFile] -= new StoreHandler(Store_Local);
            Network.Searches.SearchEvent[ServiceID, DataTypeFile] -= new SearchRequestHandler(Search_Local);
            Core.Locations.KnowOnline -= new KnowOnlineHandler(Location_KnowOnline);

            Cache.FileAquired -= new FileAquiredHandler(Cache_FileAquired);
            Cache.FileRemoved -= new FileRemovedHandler(Cache_FileRemoved);
            Cache.Dispose();
        }

        internal void LinkupTo(ulong user, uint project)
        {
            LocalTrust.AddProject(project, true);

            OpLink localLink = LocalTrust.GetLink(project);

            if (localLink == null)
                return;

            // get user confirmation if nullifying previous uplink
            if (localLink.Uplink != null)
            {
                string who = Core.GetName(localLink.Uplink.UserID);
                string message = "Transfer trust from " + who + " to " + Core.GetName(user) + "?";

                if ( !Core.UserConfirm(message, "Confirm Trust"))
                    return;
            }
            else if ( !Core.UserConfirm("Are you sure you want to trust " + Core.GetName(user) + "?", "Trust") )
                return;

            try
            {
                OpLink remoteLink = GetLink(user, project);

                if (remoteLink == null)
                    throw new Exception("Could not find Person");

                // check if self
                if (remoteLink == localLink)
                    throw new Exception("Cannot Trust in your Self");

                // check if already linked
                if (localLink.Uplink != null && localLink.Uplink == remoteLink)
                    throw new Exception("Already Trusting " + Core.GetName(user));

                //check for loop
                if (IsHigher(remoteLink.UserID, Core.UserID, project, false))
                {
                    string who = Core.GetName(remoteLink.UserID);
                    string message = "Trusting " + who + " will create a loop. Is this your intention?";

                    if (!Core.UserConfirm(message, "Loop Warning"))
                        return;
                }

                if (!Core.UserVerifyPass(ThreatLevel.Medium))
                    return;

                LocalTrust.AddProject(project, true);
                localLink.ResetUplink();
                localLink.Uplink = remoteLink;

                SaveLocal();

                Core.RunInCoreAsync(() => LinkupRequest(remoteLink));
            }
            catch (Exception ex)
            {
                Core.UserMessage(ex.Message);
            }
        }

        void Network_StatusChange()
        {
            if (!Network.Established)
                return;

            TrustMap.LockReading(delegate()
            {
                foreach (OpTrust trust in TrustMap.Values)
                    trust.Searched = false;
            });
        }

        private void LinkupRequest(OpLink remoteLink)
        {
            // create uplink request, publish
            UplinkRequest request = new UplinkRequest();
            request.ProjectID = remoteLink.Project;
            request.LinkVersion = LocalTrust.File.Header.Version;
            request.TargetVersion = remoteLink.Trust.File.Header.Version;
            request.Key = LocalTrust.File.Key;
            request.KeyID = LocalTrust.UserID;
            request.Target = remoteLink.Trust.File.Key;
            request.TargetID = remoteLink.UserID;

            byte[] signed = SignedData.Encode(Network.Protocol, Core.User.Settings.KeyPair, request);

            if(Network.Established)
                Store.PublishNetwork(request.TargetID, ServiceID, DataTypeFile, signed);

            // store locally
            Process_UplinkReq(null, new SignedData(Network.Protocol, Core.User.Settings.KeyPair, request), request);

            // publish at neighbors so they are aware of request status
            List<LocationData> locations = new List<LocationData>();
            GetLocs(Core.UserID, remoteLink.Project, 1, 1, locations);
            GetLocsBelow(Core.UserID, remoteLink.Project, locations);
            Store.PublishDirect(locations, request.TargetID, ServiceID, DataTypeFile, signed);
        }

        internal void AcceptTrust(ulong user, uint project)
        {
            try
            {
                if ( !Core.UserConfirm("Are you sure you want to accept trust from " + Core.GetName(user) + "?", "Accept Trust"))
                    return;

                OpLink remoteLink = GetLink(user, project);
                OpLink localLink = LocalTrust.GetLink(project);

                if (remoteLink == null || localLink == null)
                    throw new Exception("Could not find Person");

                if (!localLink.Downlinks.Contains(remoteLink))
                    throw new Exception(Core.GetName(user) + " does not trust you");

                if (!Core.UserVerifyPass(ThreatLevel.Medium))
                    return;


                if (!localLink.Confirmed.Contains(remoteLink.UserID))
                    localLink.Confirmed.Add(remoteLink.UserID);

                SaveLocal();
            }
            catch (Exception ex)
            {
                Core.UserMessage(ex.Message);
            }
        }

        internal void UnlinkFrom(ulong user, uint project)
        {
            try
            {
                bool unlinkUp = false;
                bool unlinkDown = false;

                OpLink remoteLink = GetLink(user, project);
                OpLink localLink = LocalTrust.GetLink(project);

                if (remoteLink == null || localLink == null)
                    throw new Exception("Could not find Person");

                if (localLink.Uplink != null && localLink.Uplink == remoteLink)
                    unlinkUp = true;

                if (localLink.Confirmed.Contains(remoteLink.UserID))
                    unlinkDown = true;

                if (!unlinkUp && !unlinkDown)
                    throw new Exception("Cannot untrust person");

                if( !Core.UserVerifyPass(ThreatLevel.Medium))
                    return;

                if ( !Core.UserConfirm("Are you sure you want to untrust " + Core.GetName(user) + "?", "Untrust"))
                    return;

                // make sure old links are notified of change
                List<LocationData> locations = new List<LocationData>();

                // remove node as an uplink
                OpLink parent = null;

                if (unlinkUp)
                {
                    GetLocs(Core.UserID, project, 1, 1, locations);

                    parent = localLink.Uplink;
                    localLink.ResetUplink();
                    localLink.Uplink = null;
                }

                // remove node from downlinks
                if (unlinkDown)
                {
                    localLink.Confirmed.Remove(remoteLink.UserID);

                    // removal of uplink requests done when version is updated by updatelocal
                }

                // update
                SaveLocal();

                // notify old links of change
                Core.RunInCoreAsync(delegate()
                {
                    OpVersionedFile file = Cache.GetFile(Core.UserID);

                    Store.PublishDirect(locations, Core.UserID, ServiceID, 0, file.SignedHeader);
                });
            }
            catch (Exception ex)
            {
                Core.UserMessage(ex.Message);
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

        public void SimTest()
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => SimTest());
                return;
            }

            OpLink localLink = LocalTrust.Links[0];

            if (localLink.Uplink == null)
            {
                // linkup to random untrusted nodes
                OpTrust randTrust = null;

                TrustMap.LockReading(delegate()
                {
                    randTrust = (from t in TrustMap.Values
                                 where t.Loaded &&
                                       LocalTrust.Name.CompareTo(t.Name) > 0 && // avoids loops by linking to 'higher' name
                                       t.Links[0].GetHighest().Trust.Loaded // avoid trusting someone who is hidded to us
                                 orderby Core.RndGen.Next()
                                 select t).FirstOrDefault();
                });

                if (randTrust != null)
                {
                    OpLink uplink = randTrust.Links[0];

                    localLink.ResetUplink();
                    localLink.Uplink = uplink;

                    SaveLocal();

                    LinkupRequest(uplink);
                }
            }

            // if unconfirmed nodes, accept them
            bool change = false;

            foreach (OpLink downlink in localLink.Downlinks)
                if (!localLink.Confirmed.Contains(downlink.UserID))
                {
                    change = true;
                    localLink.Confirmed.Add(downlink.UserID);
                }

            if (change)
                SaveLocal();
        }

        public void SimCleanup()
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => SimCleanup());
                return;
            }
        }

        void Core_KeepData()
        {
            RefreshLinked();

            TrustMap.LockReading(delegate()
            {
                foreach (OpTrust trust in TrustMap.Values)
                    if (trust.InLocalLinkTree)
                        Core.KeepData.SafeAdd(trust.UserID, true);

                    // if in bounds, set highers of node to focused
                    // because if highers removed, they will just be re-added when inbounds link cache is refreshed
                    else if (Network.Routing.InCacheArea(trust.UserID))                 // for nodes in cache bounds
                        foreach (OpLink link in trust.Links.Values)                     // for each project node is apart of
                            if (LocalTrust.Links.ContainsKey(link.Project))             // if local host also part of project
                                if (link.GetHighest() == LocalTrust.Links[link.Project].GetHighest()) // if local host and remote are part of the same hierarchy
                                    foreach (ulong id in link.GetHighers())             // keep that node and all their higher's data
                                        Core.KeepData.SafeAdd(id, true);
            });
        }

        void Cache_FileRemoved(OpVersionedFile file)
        {
            OpTrust trust = GetTrust(file.UserID);

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
            TrustMap.SafeRemove(file.UserID);
            
            // alert services/gui
            if (LinkUpdate != null)
                LinkUpdate.Invoke(trust);

            Core.RunInGuiThread(GuiUpdate, trust.UserID);
        }

        void RefreshLinked()
        {
            TrustMap.LockReading(delegate()
            {
                // unmark all nodes
                foreach (OpTrust trust in TrustMap.Values)
                {
                    trust.InLocalLinkTree = false;
                    trust.PingUser = false;
                }

                Action<OpLink> shouldFocus = link => link.Trust.InLocalLinkTree = true;
                Action<OpLink> shouldPing = link => link.Trust.PingUser = true;

                // TraverseDown 2 from self for each project
                foreach (OpLink link in LocalTrust.Links.Values)
                {
                    uint project = link.Project;

                    DoToBranch(shouldFocus, link, 2);
                    DoToBranch(shouldPing, link, 1);

                    // TraverseDown 1 from all parents above self
                    List<ulong> uplinks = GetUplinkIDs(LocalTrust.UserID, project, false, false);

                    bool first = true;
                    foreach (ulong id in uplinks)
                    {
                        OpLink uplink = GetLink(id, project);

                        if (uplink != null)
                        {
                            DoToBranch(shouldFocus, uplink, 1);

                            // only ping (know if online) one higher from self, so top isnt overwhelmed with pings
                            if (first)
                                DoToBranch(shouldPing, uplink, 1);
                        }

                        first = false;
                    }
                }

                if(Network.Responsive)
                    foreach (OpTrust trust in TrustMap.Values.Where(t => !t.Searched))
                    {
                        Core.Locations.Research(trust.UserID);
                        trust.Searched = true;
                    }
            });
        }

        void DoToBranch(Action<OpLink> action, OpLink link, int depth)
        {
            action(link);

            if (depth > 0)
                foreach (OpLink downlink in link.Downlinks)
                    DoToBranch(action, downlink, depth - 1);
        }

        void Location_KnowOnline(List<ulong> users)
        {
            RefreshLinked();

            TrustMap.LockReading(() =>
                users.AddRange(from t in TrustMap.Values where t.PingUser select t.UserID));
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
                        searchList.Add(downlink.UserID);

                    downlinks.Add(downlink.UserID);
                }

                //crit - review
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

            SignedData signed = SignedData.Decode(store.Data);

            if (signed == null)
                return;

            G2Header embedded = new G2Header(signed.Data);

            // figure out data contained
            if (G2Protocol.ReadPacket(embedded))
                if (embedded.Name == TrustPacket.UplinkReq)
                    Process_UplinkReq(store, signed, UplinkRequest.Decode(embedded));
        }

        internal void SaveLocal()
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => SaveLocal());
                return;
            }

            try
            {
                // create new link file in temp dir
                string tempPath = Core.GetTempPath();
                byte[] key = Utilities.GenerateKey(Core.StrongRndGen, 256);
                using (IVCryptoStream stream = IVCryptoStream.Save(tempPath, key))
                {
                    // project packets
                    foreach (OpLink link in LocalTrust.Links.Values)
                        if (link.Active)
                        {
                            ProjectData project = new ProjectData();
                            project.ID = link.Project;
                            project.Name = GetProjectName(link.Project);

                            if (link.Project == 0)
                                project.UserName = Core.User.Settings.UserName;

                            byte[] packet = SignedData.Encode(Network.Protocol, Core.User.Settings.KeyPair, project);
                            stream.Write(packet, 0, packet.Length);


                            // uplinks
                            if (link.Uplink != null)
                            {
                                LinkData data = new LinkData(link.Project, link.Uplink.Trust.File.Key);
                                packet = SignedData.Encode(Network.Protocol, Core.User.Settings.KeyPair, data);
                                stream.Write(packet, 0, packet.Length);
                            }

                            // downlinks
                            foreach (OpLink downlink in link.Downlinks)
                                if (link.Confirmed.Contains(downlink.UserID))
                                {
                                    string title;
                                    link.Titles.TryGetValue(downlink.UserID, out title);

                                    LinkData data = new LinkData(link.Project, downlink.Trust.File.Key, title);
                                    packet = SignedData.Encode(Network.Protocol, Core.User.Settings.KeyPair, data);
                                    stream.Write(packet, 0, packet.Length);
                                }
                        }

                    PacketStream streamEx = new PacketStream(stream, Network.Protocol, FileAccess.Write);

                    // save top 5 web caches
                    foreach (WebCache cache in Network.Cache.GetLastSeen(5))
                        streamEx.WritePacket(new WebCache(cache, TrustPacket.WebCache));


                    // save inheritable settings only if they can be inherited
                    if (IsInheritNode(Core.UserID))
                    {
                        if (Core.User.OpIcon != null)
                            streamEx.WritePacket(new IconPacket(TrustPacket.Icon, Core.User.OpIcon));

                        if (Core.User.OpSplash != null)
                        {
                            MemoryStream splash = new MemoryStream();
                            Core.User.OpSplash.Save(splash, ImageFormat.Jpeg);
                            LargeDataPacket.Write(streamEx, TrustPacket.Splash, splash.ToArray());
                        }
                    }

                    stream.WriteByte(0); // signal last packet
                    stream.FlushFinalBlock();
                }

                OpVersionedFile file = Cache.UpdateLocal(tempPath, key, null);

                Store.PublishDirect(GetLocsAbove(), Core.UserID, ServiceID, DataTypeFile, file.SignedHeader);

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
                using (IVCryptoStream stream = IVCryptoStream.Save(tempPath, LocalFileKey))
                {
                    TrustMap.LockReading(delegate()
                    {
                        foreach (OpTrust trust in TrustMap.Values)
                            foreach (OpLink link in trust.Links.Values)
                                foreach (UplinkRequest request in link.Requests)
                                    stream.Write(request.Signed, 0, request.Signed.Length);
                    });

                    stream.FlushFinalBlock();
                }


                string finalPath = LinkPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(Core, "uplinks");
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
                string path = LinkPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(Core, "uplinks");

                if (!File.Exists(path))
                    return;

                using (IVCryptoStream crypto = IVCryptoStream.Load(path, LocalFileKey))
                {
                    PacketStream stream = new PacketStream(crypto, Network.Protocol, FileAccess.Read);

                    G2Header root = null;

                    while (stream.ReadPacket(ref root))
                        if (root.Name == DataPacket.SignedData)
                        {
                            SignedData signed = SignedData.Decode(root);
                            G2Header embedded = new G2Header(signed.Data);

                            // figure out data contained
                            if (G2Protocol.ReadPacket(embedded))
                                if (embedded.Name == TrustPacket.UplinkReq)
                                    Process_UplinkReq(null, signed, UplinkRequest.Decode(embedded));
                        }
                }
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

            request.Signed = signed.Encode(Network.Protocol); // so we can send it in results / save, later on

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
            if (targetTrust.Loaded && (targetTrust.InLocalLinkTree || Core.KeepData.SafeContainsKey(targetTrust.UserID)))
            {
                if (targetTrust.File.Header.Version < request.TargetVersion)
                    Cache.Research(targetTrust.UserID);

                if (requesterTrust == null)
                {
                    requesterTrust = new OpTrust(new OpVersionedFile(request.Key));
                    TrustMap.SafeAdd(request.KeyID, requesterTrust);
                }

                // once new version of requester's link file has been downloaded, interface will be updated
                if (!requesterTrust.Loaded || (requesterTrust.File.Header.Version < request.LinkVersion))
                    Cache.Research(requesterTrust.UserID);
            }

            RunSaveUplinks = true;
        }

        private void Cache_FileAquired(OpVersionedFile cachefile)
        {
            try
            {
                // get link directly, even if in unloaded state we need the same reference
                OpTrust trust = null;
                TrustMap.SafeTryGetValue(cachefile.UserID, out trust);

                if (trust == null)
                {
                    trust = new OpTrust(cachefile);
                    TrustMap.SafeAdd(cachefile.UserID, trust);
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
                                    if (root.UserID == link.LoopRoot.UserID)
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
                string inheritName = null; 
                string inheritOp = null;
                Bitmap inheritIcon = null;
                byte[] inheritSplash = null;

                using (TaggedStream file = new TaggedStream(Cache.GetFilePath(cachefile.Header), Network.Protocol))
                using (IVCryptoStream crypto = IVCryptoStream.Load(file, cachefile.Header.FileKey))
                {
                    PacketStream stream = new PacketStream(crypto, Network.Protocol, FileAccess.Read);

                    G2Header packetRoot = null;

                    while (stream.ReadPacket(ref packetRoot))
                    {
                        if (packetRoot.Name == DataPacket.SignedData)
                        {
                            SignedData signed = SignedData.Decode(packetRoot);
                            G2Header embedded = new G2Header(signed.Data);

                            // figure out data contained
                            if (G2Protocol.ReadPacket(embedded))
                            {
                                if (embedded.Name == TrustPacket.ProjectData)
                                {
                                    ProjectData project = ProjectData.Decode(embedded);
                                    Process_ProjectData(trust, signed, project);

                                    if (project.ID == 0)
                                    {
                                        inheritName = project.UserName;
                                        inheritOp = project.Name;
                                    }
                                }

                                else if (embedded.Name == TrustPacket.LinkData)
                                    Process_LinkData(trust, signed, LinkData.Decode(embedded));
                            }
                        }

                        else if (packetRoot.Name == TrustPacket.WebCache)
                            Network.Cache.AddCache(WebCache.Decode(packetRoot));

                        else if (packetRoot.Name == TrustPacket.Icon)
                            inheritIcon = IconPacket.Decode(packetRoot).OpIcon;

                        else if (packetRoot.Name == TrustPacket.Splash)
                        {
                            LargeDataPacket splash = LargeDataPacket.Decode(packetRoot);

                            if (splash.Size > 0)
                                inheritSplash = LargeDataPacket.Read(splash, stream, TrustPacket.Splash);
                        }
                    }
                }

                // set new header
                trust.Loaded = true;

                // set as root if node has no uplinks
                foreach (OpLink link in trust.Links.Values)
                    if (link.Uplink == null)
                        AddRoot(link);
                    // if uplink is unknown - process link data will search for the unknown parent
                

                // if loop created, create new loop node with unique ID, assign all nodes in loop the ID and add as downlinks
                foreach (OpLink link in trust.Links.Values)
                    if (IsLooped(link))
                    {
                        uint project = link.Project;

                        OpLink loop = new OpTrust(project, (ulong)Core.RndGen.Next()).GetLink(project);
                        loop.IsLoopRoot = true;

                        List<ulong> uplinks = GetUnconfirmedUplinkIDs(trust.UserID, project);
                        uplinks.Add(trust.UserID);

                        foreach (ulong uplink in uplinks)
                        {
                            OpLink member = GetLink(uplink, project);

                            if (member == null)
                                continue;

                            member.LoopRoot = loop;

                            loop.Downlinks.Add(member);
                            loop.Confirmed.Add(member.UserID); //needed for getlowers
                        }

                        AddRoot(loop);
                    }


                trust.CheckRequestVersions();


                if (LinkUpdate != null)
                    LinkUpdate.Invoke(trust);

                if (Core.NewsWorthy(trust.UserID, 0, false))
                    Core.MakeNews(ServiceIDs.Trust, "Trust updated by " + Core.GetName(trust.UserID), trust.UserID, 0, true);


                // update subs
                if (Network.Established)
                {
                    List<LocationData> locations = new List<LocationData>();

                    ProjectRoots.LockReading(delegate()
                    {
                        foreach (uint project in ProjectRoots.Keys)
                            if (Core.UserID == trust.UserID || IsHigher(trust.UserID, project))
                                GetLocsBelow(Core.UserID, project, locations);
                    });

                    Store.PublishDirect(locations, trust.UserID, ServiceID, 0, cachefile.SignedHeader);
                }

                // inherit local settings
                if(Core.UserID == trust.UserID)
                {
                    if (inheritName != null)
                        Core.User.Settings.UserName = inheritName;
                }

                // inherit settings from highest node, first node in loop
                if (IsInheritNode(trust.UserID))
                {
                    if (inheritOp != null)
                        Core.User.Settings.Operation = inheritOp;

                    if (inheritIcon != null)
                    {
                        Core.User.OpIcon = inheritIcon;
                        Core.User.IconUpdate();
                    }

                    if (inheritSplash != null)
                        Core.User.OpSplash = (Bitmap)Bitmap.FromStream(new MemoryStream(inheritSplash));
                    else
                        Core.User.OpSplash = null;
                }

                // update interface node
                Core.RunInGuiThread(GuiUpdate, trust.UserID);

                foreach (OpLink link in trust.Links.Values)
                    foreach (OpLink downlink in link.Downlinks)
                        Core.RunInGuiThread(GuiUpdate, downlink.UserID);

            }
            catch (Exception ex)
            {
                Network.UpdateLog("Link", "Error loading file " + ex.Message);
            }
        }

        internal bool IsInheritNode(ulong check)
        {
            List<ulong> highers = GetUplinkIDs(Core.UserID, 0, true, true);

            if (highers.Count > 0)
            {
                if (highers[highers.Count - 1] == check)
                    return true;
            }
            // else local is at the top
            if (Core.UserID == check)
                return true;

            return false;
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
            {
                trust.Name = project.UserName;
                Core.IndexName(trust.UserID, trust.Name);
            }

            OpLink link = trust.GetLink(project.ID);
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
                    Cache.Research(targetTrust.UserID);

                //if (targetLink.Uplink == null)
                //    AddRoot(targetLink);
            }

            else
            {
                if (linkData.Title != null)
                    localLink.Titles[targetLink.UserID] = linkData.Title;

                localLink.Confirmed.Add(targetLink.UserID);
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
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => JoinProject(project));
                return;
            }

            if (project == 0)
                return;

            LocalTrust.AddProject(project, true);

            SaveLocal();
        }

        internal void LeaveProject(uint project)
        {
            if (project == 0)
                return;

            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => LeaveProject(project));
                return;
            }

            // update local peers we are leaving
            List<LocationData> locations = new List<LocationData>();
            GetLocs(Core.UserID, project, 1, 1, locations);
            GetLocsBelow(Core.UserID, project, locations);

            LocalTrust.RemoveProject(project);

            SaveLocal();

            // update links in old project of update
            OpVersionedFile file = Cache.GetFile(Core.UserID);
            Store.PublishDirect(locations, Core.UserID, ServiceID, 0, file.SignedHeader);
        }

        internal void RenameProject(uint project, string name)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => RenameProject(project, name));
                return;
            }

            if (project == 0)
                Core.User.Settings.Operation = name;
            else
                ProjectNames.SafeAdd(project, name);


            Core.User.Save();
            SaveLocal();
        }

        internal string GetProjectName(uint id)
        {
            if (id == 0)
                return Core.User.Settings.Operation;

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
                    if (child.UserID != root && !AddLinkLocations(child, locations))
                        GetLocsBelow(child.UserID, root, project, locations);
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
            List<ClientInfo> clients = Core.Locations.GetClients(link.UserID);

            foreach (ClientInfo info in clients)
            {
                if (info.Data.UserID == Core.UserID && info.Data.Source.ClientID == Core.Network.Local.ClientID)
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
                    GetLocs(Core.UserID, project, 1, 1, locations);  // below done by cacheplan
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
            return IsHigher(Core.UserID, key, project, true);
        }

        internal bool IsUnconfirmedHigher(ulong key, uint project)
        {
            return IsHigher(Core.UserID, key, project, false);
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

            List<ulong> uplinks = GetUplinkIDs(localID, project, confirmed, false);

            if (uplinks.Count == 0)
                return false;

            if (uplinks.Contains(higherID))
                return true;

            // check if higher ID being checked is the loop root ID
            OpLink highest = GetLink(uplinks[uplinks.Count - 1], project);

            if (highest != null && 
                highest.LoopRoot != null && 
                highest.LoopRoot.UserID == higherID)
                return true;

            return false;
        }


        internal bool IsLower(ulong localID, ulong lowerID, uint project)
        {
            List<ulong> uplinks = GetUplinkIDs(lowerID, project, true, true);

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
                if (list.Contains(uplink.UserID))
                    return false;

                list.Add(uplink.UserID);

                uplink = uplink.GetHigher(false);
            }

            return false;
        }

        internal List<ulong> GetUplinkIDs(ulong id, uint project)
        {
            return GetUplinkIDs(id, project, true, false);
        }

        internal List<ulong> GetUnconfirmedUplinkIDs(ulong id, uint project)
        {
            return GetUplinkIDs(id, project, false, false);
        }

        private List<ulong> GetUplinkIDs(ulong local, uint project, bool confirmed, bool stopAtLoop)
        {
            // get uplinks from id, not including id, starting with directly above and ending with root

            List<ulong> list = new List<ulong>();

            OpLink link = GetLink(local, project);

            if (link == null || (stopAtLoop && link.InLoop) )
                return list;

            OpLink uplink = link.GetHigher(confirmed);

            while (uplink != null)
            {
                // if full loop traversed
                if (uplink.UserID == local || list.Contains(uplink.UserID))
                    return list;

                list.Add(uplink.UserID);

                // stop at loop means get first node in loop and return
                if (stopAtLoop && uplink.InLoop)
                    return list;

                uplink = uplink.GetHigher(confirmed);
            }

            return list;
        }

        internal List<ulong> GetAutoInheritIDs(ulong local, uint project)
        {
            // get uplinks from local, including first id in loop, but no more

            return GetUplinkIDs(local, project, true, true);
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
                list.Add(sub.UserID);

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
                    if (link.Confirmed.Contains(downlink.UserID))
                    {
                        list.Add(downlink.UserID);

                        if (levels > 0)
                            list.AddRange(GetDownlinkIDs(downlink.UserID, project, levels));
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
                        if (link.Confirmed.Contains(downlink.UserID))
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
                        if (downlink.UserID == id)
                            return true;

            return false;
        }

        internal bool IsLowerDirect(ulong id, uint project)
        {
            OpLink link = LocalTrust.GetLink(project);

            if (link != null && link.Confirmed.Contains(id))
                foreach (OpLink downlink in link.Downlinks)
                    if (!link.IsLoopedTo(downlink))
                        if (downlink.UserID == id)
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

            return uplink.UserID == id;
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

        internal OpTrust GetRandomTrust()
        {
            OpTrust result = null;

            TrustMap.LockReading(delegate()
            {
                result = (from trust in TrustMap.Values
                          where trust != LocalTrust && trust.Loaded
                          orderby Core.RndGen.Next()
                          select trust).FirstOrDefault();
            });

            return result;
        }

        internal void SetTitle(ulong user, uint project, string title)
        {
            OpLink link = GetLink(user, project);

            if(link == null)
                return;

            link.Titles[user] = title;

            if(title == "")
                link.Titles.Remove(user);

            SaveLocal();
        }
    }

    [DebuggerDisplay("{Name}")]
    internal class OpTrust
    {
        internal string Name = "Unknown";
        
        internal bool Loaded;
        internal ulong LoopID;

        internal bool InLocalLinkTree;
        internal bool PingUser;
        internal bool Searched;

        internal Dictionary<uint, OpLink> Links = new Dictionary<uint, OpLink>();

        internal OpVersionedFile File;


        internal OpTrust(OpVersionedFile file)
        {
            File = file;
        }

        internal ulong UserID
        {
            get
            {
                return (File == null) ? LoopID : File.UserID;
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
            link.Titles.Clear();
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

        // loop root is an empty node that has IsLoopRoot set to true, LoopRoot set to null
        // link in loop has LoopRoot set to adress of root node, IsLoopRoot false, InLoop resolves to true
        internal bool IsLoopRoot;
        internal OpLink LoopRoot;
        internal bool InLoop { get { return LoopRoot != null; } }

        internal OpLink Uplink;
        internal List<OpLink> Downlinks = new List<OpLink>();
        internal List<ulong> Confirmed = new List<ulong>();
        internal List<UplinkRequest> Requests = new List<UplinkRequest>();

        internal Dictionary<ulong, string> Titles = new Dictionary<ulong, string>();

        internal ulong UserID
        {
            get
            {
                return Trust.UserID;
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

            Uplink = null;
            Titles.Clear();
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
            if (Uplink.Confirmed.Contains(UserID))
                return Uplink;

            return null;
        }

        internal List<OpLink> GetLowers(bool confirmed)
        {
            List<OpLink> lowers = new List<OpLink>();

            foreach (OpLink downlink in Downlinks)
                if (IsLoopRoot || !IsLoopedTo(downlink)) // chat uses getlowers on looproot
                    if (!confirmed || Confirmed.Contains(downlink.UserID))
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
                if (request.KeyID == downlink.UserID)
                {
                    Requests.Remove(request);
                    break;
                }
        }

        // includes first node in loop only, not the entire loop
        internal List<ulong> GetHighers()
        {
            List<ulong> list = new List<ulong>();

            if (LoopRoot != null)
                return list;

            OpLink uplink = GetHigher(true);

            while (uplink != null)
            {
                list.Add(uplink.UserID);

                uplink = uplink.GetHigher(true);

                if (uplink != null && uplink.LoopRoot != null)
                    return list;
            }

            return list;
        }

        internal OpLink GetHighest()
        {
            // top is loop root
            if (LoopRoot != null)
                return LoopRoot;

            OpLink uplink = GetHigher(true);

            // recurse on higher
            if (uplink != null)
                return uplink.GetHighest();

            // else this is the top
            return this;
        }
    }
}
