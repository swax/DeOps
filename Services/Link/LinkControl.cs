using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;

using DeOps.Components.Location;
using DeOps.Components.Transfer;


namespace DeOps.Components.Link
{
    internal delegate void LinkUpdateHandler(OpTrust trust);
    internal delegate void LinkGuiUpdateHandler(ulong key);
    internal delegate List<ulong> LinkGetFocusedHandler();


    class LinkControl : OpComponent
    {
        internal OpCore Core;
        internal DhtStore Store;
        internal DhtNetwork Network;

        internal OpTrust LocalTrust;

        internal ThreadedDictionary<ulong, OpTrust> TrustMap = new ThreadedDictionary<ulong, OpTrust>();
        internal ThreadedDictionary<uint, string> ProjectNames = new ThreadedDictionary<uint, string>();
        internal ThreadedDictionary<uint, List<OpLink>> ProjectRoots = new ThreadedDictionary<uint, List<OpLink>>();

        Dictionary<ulong, DateTime> NextResearch = new Dictionary<ulong, DateTime>();
        Dictionary<ulong, uint> DownloadLater = new Dictionary<ulong, uint>();

        internal string LinkPath;
        internal bool StructureKnown;
        internal int PruneSize = 100;

        bool RunSaveHeaders;
        RijndaelManaged LocalFileKey;

        internal LinkUpdateHandler LinkUpdate;
        internal LinkGuiUpdateHandler GuiUpdate;
        internal event LinkGetFocusedHandler GetFocused;


        internal LinkControl(OpCore core)
        {
            Core = core;
            Core.Links = this;

            Store = Core.OperationNet.Store;
            Network = Core.OperationNet;

            Core.TimerEvent += new TimerHandler(Core_Timer);
            Core.LoadEvent += new LoadHandler(Core_Load);

            Network.EstablishedEvent += new EstablishedHandler(Network_Established);

            Store.StoreEvent[ComponentID.Trust] = new StoreHandler(Store_Local);
            Store.ReplicateEvent[ComponentID.Trust] = new ReplicateHandler(Store_Replicate);
            Store.PatchEvent[ComponentID.Trust] = new PatchHandler(Store_Patch);

            Network.Searches.SearchEvent[ComponentID.Trust] = new SearchRequestHandler(Search_Local);

            if (Core.Sim != null)
                PruneSize = 25;
        }

        void Core_Load()
        {
            Core.Transfers.FileSearch[ComponentID.Trust] = new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[ComponentID.Trust] = new FileRequestHandler(Transfers_FileRequest);


            ProjectNames.SafeAdd(0, Core.User.Settings.Operation);

            LinkPath = Core.User.RootPath + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + ComponentID.Trust.ToString();
            Directory.CreateDirectory(LinkPath);

            LocalFileKey = Core.User.Settings.FileKey;

            LoadHeaders();


            LocalTrust = GetTrust(Core.LocalDhtID);


            if (LocalTrust == null)
            {
                LocalTrust = new OpTrust(Core.User.Settings.KeyPublic);
                TrustMap.SafeAdd(Core.LocalDhtID, LocalTrust);
            }

            if (!LocalTrust.Loaded)
            {
                LocalTrust.Name = Core.User.Settings.ScreenName;
                LocalTrust.AddProject(0, true); // operation

                SaveLocal();
            }

        }

        internal override List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong remoteKey, uint project)
        {
            if (menuType != InterfaceMenuType.Quick)
                return null;

            List<MenuItemInfo> menus = new List<MenuItemInfo>();

            bool unlink = false;

            OpLink remoteLink = GetLink(remoteKey, project);
            OpLink localLink = LocalTrust.GetLink(project);

            if (remoteLink == null || localLink == null)
                return menus;

            // linkup
            if (Core.LocalDhtID != remoteKey &&
                (localLink.Uplink == null || localLink.Uplink.DhtID != remoteKey)) // not already linked to
                menus.Add(new MenuItemInfo("Trust", LinkRes.link, new EventHandler(Menu_Linkup)));

            // confirm
            if (localLink.Downlinks.Contains(remoteLink))
            {
                unlink = true;

                if (!localLink.Confirmed.Contains(remoteKey)) // not already confirmed
                    menus.Add(new MenuItemInfo("Accept Trust", LinkRes.confirmlink, new EventHandler(Menu_ConfirmLink)));
            }

            // unlink
            if ((unlink && localLink.Confirmed.Contains(remoteKey)) ||
                (localLink.Uplink != null && localLink.Uplink.DhtID == remoteKey))
                menus.Add(new MenuItemInfo("Revoke Trust", LinkRes.unlink, new EventHandler(Menu_Unlink)));


            return menus;
        }

        private void Menu_Linkup(object sender, EventArgs e)
        {
            if (!(sender is IViewParams) || Core.GuiMain == null)
                return;

            ulong remoteKey = ((IViewParams)sender).GetKey();
            uint project = ((IViewParams)sender).GetProject();

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
            request.LinkVersion = LocalTrust.Header.Version;
            request.TargetVersion = remoteLink.Trust.Header.Version;
            request.Key = LocalTrust.Key;
            request.KeyID = LocalTrust.DhtID;
            request.Target = remoteLink.Trust.Key;
            request.TargetID = remoteLink.DhtID;

            byte[] signed = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, request);
            Store.PublishNetwork(request.TargetID, ComponentID.Trust, signed);

            // store locally
            Process_UplinkReq(null, new SignedData(Core.Protocol, Core.User.Settings.KeyPair, request), request);

            // publish at neighbors so they are aware of request status
            List<LocationData> locations = new List<LocationData>();
            GetLocs(Core.LocalDhtID, remoteLink.Project, 1, 1, locations);
            GetLocsBelow(Core.LocalDhtID, remoteLink.Project, locations);
            Store.PublishDirect(locations, request.TargetID, ComponentID.Trust, signed);
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
                    Store.PublishDirect(locations, Core.LocalDhtID, ComponentID.Trust, LocalTrust.SignedHeader);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(Core.GuiMain, ex.Message);
            }
        }

        void Core_Timer()
        {
            //crit remove projects no longer referenced, call for projects refresh
            // location updates are done for nodes in link map that are focused or linked
            // node comes online how to know to search for it, every 10 mins?

            // do only once per second if needed
            // branches only change when a profile is updated
            if (RunSaveHeaders)
            {
                RefreshLinked();
                SaveHeaders();
            }

            // clean download later map
            if (!Network.Established)
                Utilities.PruneMap(DownloadLater, Core.LocalDhtID, PruneSize);


            // do below once per minute
            if (Core.TimeNow.Second != 0)
                return;

            List<ulong> removeLinks = new List<ulong>();

            TrustMap.LockReading(delegate()
            {
                if (TrustMap.Count > PruneSize && StructureKnown)
                {
                    List<ulong> focused = GetFocusedLinks();

                    foreach (OpTrust trust in TrustMap.Values)
                        // if not focused, linked, or cached - remove
                        if (!trust.InLocalLinkTree &&
                            trust.DhtID != Core.LocalDhtID &&
                            !focused.Contains(trust.DhtID) &&
                            !Utilities.InBounds(trust.DhtID, trust.DhtBounds, Core.LocalDhtID))
                        {
                            removeLinks.Add(trust.DhtID);
                        }
                }
            });

            if (removeLinks.Count > 0)
                TrustMap.LockWriting(delegate()
                {
                    while (removeLinks.Count > 0 && TrustMap.Count > PruneSize / 2)
                    {
                        // find furthest id
                        ulong furthest = Core.LocalDhtID;

                        foreach (ulong id in removeLinks)
                            if ((id ^ Core.LocalDhtID) > (furthest ^ Core.LocalDhtID))
                                furthest = id;

                        // remove
                        OpTrust trust = TrustMap[furthest];

                        trust.Reset();

                        /*foreach (uint project in link.Projects)
                            if (link.Downlinks.ContainsKey(proj))
                                foreach (OpTrustOld downlink in link.Downlinks[proj])
                                    if (downlink.Uplink.ContainsKey(proj))
                                        if (downlink.Uplink[proj] == link)
                                            downlink.Uplink[proj] = new OpTrustOld(link.Key); // place holder
                        */
                        if (trust.Header != null)
                            try { File.Delete(GetFilePath(trust.Header)); }
                            catch { }

                        trust.Loaded = false;
                        TrustMap.Remove(furthest);
                        removeLinks.Remove(furthest);
                        RunSaveHeaders = true;
                    }
                });

            // clean roots
            List<uint> removeList = new List<uint>();

            ProjectRoots.LockReading(delegate()
                {
                    foreach (uint project in ProjectRoots.Keys)
                        if (ProjectRoots[project].Count == 0)
                            removeList.Add(project);
                });

            if (removeList.Count > 0)
                ProjectRoots.LockWriting(delegate()
               {
                   foreach (uint project in removeList)
                       ProjectRoots.Remove(project);
                   //ProjectNames.Remove(id); // if we are only root, and leave project, but have downlinks, still need the name
               });

            // clean research map
            removeLinks.Clear();

            foreach (KeyValuePair<ulong, DateTime> pair in NextResearch)
                if (Core.TimeNow > pair.Value)
                    removeLinks.Add(pair.Key);

            if (removeLinks.Count > 0)
                foreach (ulong id in removeLinks)
                    NextResearch.Remove(id);
        }

        void Network_Established()
        {
            ulong localBounds = Store.RecalcBounds(Core.LocalDhtID);

            // set bounds for objects
            TrustMap.LockReading(delegate()
            {
                foreach (OpTrust trust in TrustMap.Values)
                {
                    trust.DhtBounds = Store.RecalcBounds(trust.DhtID);

                    // republish objects that were not seen on the network during startup
                    if (trust.Unique && Utilities.InBounds(Core.LocalDhtID, localBounds, trust.DhtID))
                        Store.PublishNetwork(trust.DhtID, ComponentID.Trust, trust.SignedHeader);
                }
            });

            // only download those objects in our local area
            foreach (KeyValuePair<ulong, uint> pair in DownloadLater)
                if (Utilities.InBounds(Core.LocalDhtID, localBounds, pair.Key))
                    StartSearch(pair.Key, pair.Value);

            DownloadLater.Clear();
        }

        void RefreshLinked()
        {
            StructureKnown = false;

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
                    List<OpLink> roots = null;
                    if (ProjectRoots.SafeTryGetValue(project, out roots))
                        foreach (OpLink root in roots)
                        {
                            // structure known if node found with no uplinks, and a number of downlinks
                            if (project == 0 && root.Trust.Loaded && root.Uplink == null)
                                if (root.Downlinks.Count > 0 && TrustMap.Count > 8)
                                    StructureKnown = true;

                            MarkBranchLinked(root, 2);
                        }
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

            if (!Network.Routing.Responsive())
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
            {
                uint version = 0;
                if (link != null)
                    version = link.Trust.Header.Version + 1;

                // limit re-search to once per 30 secs
                DateTime timeout = default(DateTime);

                if (NextResearch.TryGetValue(key, out timeout))
                    if (Core.TimeNow < timeout)
                        return;

                StartSearch(id, version);
                NextResearch[id] = Core.TimeNow.AddSeconds(30);
            }

            NextResearch[key] = Core.TimeNow.AddSeconds(30);
        }

        internal void StartSearch(ulong key, uint version)
        {
            if (Core.Loading) // prevents routingupdate, or loadheaders from triggering search on startup
                return;

            byte[] parameters = BitConverter.GetBytes(version);

            DhtSearch search = Network.Searches.Start(key, "Link", ComponentID.Trust, parameters, new EndSearchHandler(EndSearch));

            if (search != null)
                search.TargetResults = 2;
        }

        void EndSearch(DhtSearch search)
        {
            foreach (SearchValue found in search.FoundValues)
                Store_Local(new DataReq(found.Sources, search.TargetID, ComponentID.Trust, found.Value));
        }

        List<byte[]> Search_Local(ulong key, byte[] parameters)
        {
            List<Byte[]> results = new List<byte[]>();

            uint minVersion = BitConverter.ToUInt32(parameters, 0);

            OpTrust trust = GetTrust(key);

            if (trust != null)
            {
                if (trust.Loaded && trust.Header.Version >= minVersion)
                    results.Add(trust.SignedHeader);

                foreach (OpLink link in trust.Links.Values)
                    foreach (UplinkRequest request in link.Requests)
                        if (request.TargetVersion > minVersion)
                            results.Add(request.Signed);
            }

            return results;
        }

        bool Transfers_FileSearch(ulong key, FileDetails details)
        {
            OpTrust trust = GetTrust(key);

            if (trust != null)
                if (trust.Loaded && details.Size == trust.Header.FileSize && Utilities.MemCompare(details.Hash, trust.Header.FileHash))
                    return true;

            return false;
        }

        string Transfers_FileRequest(ulong key, FileDetails details)
        {
            OpTrust trust = GetTrust(key);

            if (trust != null)
                if (trust.Loaded && details.Size == trust.Header.FileSize && Utilities.MemCompare(details.Hash, trust.Header.FileHash))
                    return GetFilePath(trust.Header);

            return null;
        }

        internal void RoutingUpdate(DhtContact contact)
        {
            // find node if structure not known
            if (StructureKnown)
                return;

            OpTrust trust = GetTrust(contact.DhtID);

            if (trust == null)
                StartSearch(contact.DhtID, 0);
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
            {
                if (embedded.Name == TrustPacket.TrustHeader)
                    Process_LinkHeader(store, signed, TrustHeader.Decode(Core.Protocol, embedded));

                else if (embedded.Name == TrustPacket.UplinkReq)
                    Process_UplinkReq(store, signed, UplinkRequest.Decode(Core.Protocol, embedded));
            }
        }

        const int PatchEntrySize = 12;

        ReplicateData Store_Replicate(DhtContact contact, bool add)
        {
            if (!Network.Established)
                return null;


            ReplicateData data = new ReplicateData(ComponentID.Trust, PatchEntrySize);

            byte[] patch = new byte[PatchEntrySize];

            TrustMap.LockReading(delegate()
            {
                foreach (OpTrust trust in TrustMap.Values)
                    if (trust.Loaded && Utilities.InBounds(trust.DhtID, trust.DhtBounds, contact.DhtID))
                    {
                        // bounds is a distance value
                        DhtContact target = contact;
                        trust.DhtBounds = Store.RecalcBounds(trust.DhtID, add, ref target);

                        if (target != null)
                        {
                            BitConverter.GetBytes(trust.DhtID).CopyTo(patch, 0);
                            BitConverter.GetBytes(trust.Header.Version).CopyTo(patch, 8);

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

                OpTrust trust = GetTrust(dhtid);

                if (trust != null)
                    if (trust.Loaded && trust.Header != null)
                    {
                        if (trust.Header.Version > version)
                        {
                            Store.Send_StoreReq(source, 0, new DataReq(null, trust.DhtID, ComponentID.Trust, trust.SignedHeader));
                            continue;
                        }

                        trust.Unique = false; // network has current or newer version

                        if (trust.Header.Version == version)
                            continue;

                        // else our version is old, download below
                    }

                if (Network.Established)
                    Network.Searches.SendDirectRequest(source, dhtid, ComponentID.Trust, BitConverter.GetBytes(version));
                else
                    DownloadLater[dhtid] = version;
            }
        }

        internal void SaveLocal()
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreBlocked(delegate() { SaveLocal(); });
                return;
            }

            try
            {
                TrustHeader header = LocalTrust.Header;

                string oldFile = null;

                if (header != null)
                    oldFile = GetFilePath(header);
                else
                    header = new TrustHeader();


                header.Key = Core.User.Settings.KeyPublic;
                header.KeyID = Core.LocalDhtID; // set so keycheck works
                header.Version++;
                header.FileKey.GenerateKey();
                header.FileKey.IV = new byte[header.FileKey.IV.Length];

                // create new link file in temp dir
                string tempPath = Core.GetTempPath();
                FileStream tempFile = new FileStream(tempPath, FileMode.CreateNew);
                CryptoStream stream = new CryptoStream(tempFile, header.FileKey.CreateEncryptor(), CryptoStreamMode.Write);


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
                            LinkData data = new LinkData(link.Project, link.Uplink.Trust.Key, true);
                            packet = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, data);
                            stream.Write(packet, 0, packet.Length);
                        }

                        // downlinks
                        foreach (OpLink downlink in link.Downlinks)
                            if (link.Confirmed.Contains(downlink.DhtID))
                            {
                                LinkData data = new LinkData(link.Project, downlink.Trust.Key, false);
                                packet = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, data);
                                stream.Write(packet, 0, packet.Length);
                            }
                    }

                stream.FlushFinalBlock();
                stream.Close();


                // finish building header
                Utilities.ShaHashFile(tempPath, ref header.FileHash, ref header.FileSize);


                // move file, overwrite if need be
                string finalPath = GetFilePath(header);
                File.Move(tempPath, finalPath);

                CacheLinkFile(new SignedData(Core.Protocol, Core.User.Settings.KeyPair, header), header);

                SaveHeaders();

                if (oldFile != null && File.Exists(oldFile)) // delete after move to ensure a copy always exists (names different)
                    try { File.Delete(oldFile); }
                    catch { }

                // publish header
                Store.PublishNetwork(Core.LocalDhtID, ComponentID.Trust, LocalTrust.SignedHeader);

                Store.PublishDirect(GetLocsAbove(), Core.LocalDhtID, ComponentID.Trust, LocalTrust.SignedHeader);
            }
            catch (Exception ex)
            {
                Network.UpdateLog("LinkControl", "Error updating local " + ex.Message);
            }
        }

        void SaveHeaders()
        {
            RunSaveHeaders = false;

            try
            {
                string tempPath = Core.GetTempPath();
                FileStream file = new FileStream(tempPath, FileMode.Create);
                CryptoStream stream = new CryptoStream(file, LocalFileKey.CreateEncryptor(), CryptoStreamMode.Write);

                TrustMap.LockReading(delegate()
                {
                    foreach (OpTrust trust in TrustMap.Values)
                        if (trust.SignedHeader != null)
                        {
                            stream.Write(trust.SignedHeader, 0, trust.SignedHeader.Length);

                            foreach (OpLink link in trust.Links.Values)
                                foreach (UplinkRequest request in link.Requests)
                                    stream.Write(request.Signed, 0, request.Signed.Length);
                        }
                });

                stream.FlushFinalBlock();
                stream.Close();


                string finalPath = LinkPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "headers");
                File.Delete(finalPath);
                File.Move(tempPath, finalPath);
            }
            catch (Exception ex)
            {
                Network.UpdateLog("LinkControl", "Error saving links " + ex.Message);
            }
        }

        private void LoadHeaders()
        {
            try
            {
                string path = LinkPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "headers");

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
                        {
                            if (embedded.Name == TrustPacket.TrustHeader)
                                Process_LinkHeader(null, signed, TrustHeader.Decode(Core.Protocol, embedded));

                            else if (embedded.Name == TrustPacket.UplinkReq)
                                Process_UplinkReq(null, signed, UplinkRequest.Decode(Core.Protocol, embedded));
                        }
                    }

                stream.Close();
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Link", "Error loading links " + ex.Message);
            }
        }

        private void Process_LinkHeader(DataReq data, SignedData signed, TrustHeader header)
        {
            Core.IndexKey(header.KeyID, ref header.Key);


            OpTrust current = GetTrust(header.KeyID);

            // if link loaded
            if (current != null)
            {
                // lower version
                if (header.Version < current.Header.Version)
                {
                    if (data != null && data.Sources != null)
                        foreach (DhtAddress source in data.Sources)
                            Store.Send_StoreReq(source, data.LocalProxy, new DataReq(null, current.DhtID, ComponentID.Trust, current.SignedHeader));

                    return;
                }

                // higher version
                else if (header.Version > current.Header.Version)
                {
                    CacheLinkFile(signed, header);
                }
            }

            // else load file, set new header after file loaded
            else
                CacheLinkFile(signed, header);
        }

        private void Process_UplinkReq(DataReq data, SignedData signed, UplinkRequest request)
        {
            Core.IndexKey(request.KeyID, ref request.Key);
            Core.IndexKey(request.TargetID, ref request.Target);

            if (!Utilities.CheckSignedData(request.Key, signed.Data, signed.Signature))
                return;

            OpTrust requesterTrust = GetTrust(request.KeyID);

            if (requesterTrust != null && requesterTrust.Loaded && requesterTrust.Header.Version > request.LinkVersion)
                return;

            // check if target in linkmap, if not add
            OpTrust targetTrust = GetTrust(request.TargetID);

            if (targetTrust == null)
            {
                targetTrust = new OpTrust(request.Target);
                TrustMap.SafeAdd(request.TargetID, targetTrust);
            }

            if (targetTrust.Loaded && targetTrust.Header.Version > request.TargetVersion)
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
            if (targetTrust.Loaded && (targetTrust.InLocalLinkTree || GetFocusedLinks().Contains(targetTrust.DhtID)))
            {
                if (targetTrust.Header.Version < request.TargetVersion)
                    StartSearch(targetTrust.DhtID, request.TargetVersion);

                if (requesterTrust == null)
                {
                    requesterTrust = new OpTrust(request.Key);
                    TrustMap.SafeAdd(request.KeyID, requesterTrust);
                }

                // once new version of requester's link file has been downloaded, interface will be updated
                if (!requesterTrust.Loaded || (requesterTrust.Header.Version < request.LinkVersion))
                    StartSearch(requesterTrust.DhtID, request.LinkVersion);
            }
        }

        private List<ulong> GetFocusedLinks()
        {
            List<ulong> focused = new List<ulong>();

            if (GetFocused != null)
                foreach (LinkGetFocusedHandler handler in GetFocused.GetInvocationList())
                    foreach (ulong id in handler.Invoke())
                        if (!focused.Contains(id))
                            focused.Add(id);

            return focused;
        }

        private void DownloadLinkFile(SignedData signed, TrustHeader header)
        {
            if (!Utilities.CheckSignedData(header.Key, signed.Data, signed.Signature))
                return;

            FileDetails details = new FileDetails(ComponentID.Trust, header.FileHash, header.FileSize, null);

            Core.Transfers.StartDownload(header.KeyID, details, new object[] { signed, header }, new EndDownloadHandler(EndDownload));
        }

        private void EndDownload(string path, object[] args)
        {
            SignedData signedHeader = (SignedData)args[0];
            TrustHeader header = (TrustHeader)args[1];

            string finalpath = GetFilePath(header);

            if (File.Exists(finalpath))
                return;

            File.Move(path, finalpath);

            CacheLinkFile(signedHeader, header);
        }

        private void CacheLinkFile(SignedData signedHeader, TrustHeader header)
        {
            if (Core.InvokeRequired)
                Debug.Assert(false);

            try
            {
                // check if file exists           
                string path = GetFilePath(header);

                if (!File.Exists(path))
                {
                    DownloadLinkFile(signedHeader, header);
                    return;
                }

                // get link directly, even if in unloaded state we need the same reference
                OpTrust trust = null;
                TrustMap.SafeTryGetValue(header.KeyID, out trust);

                if (trust == null)
                {
                    trust = new OpTrust(header.Key);
                    TrustMap.SafeAdd(header.KeyID, trust);
                }


                // delete old file
                if (trust.Header != null)
                {
                    if (header.Version < trust.Header.Version)
                        return; // dont update with older version

                    string oldPath = GetFilePath(trust.Header);
                    if (path != oldPath && File.Exists(oldPath))
                        try { File.Delete(oldPath); }
                        catch { }
                }

                // clean roots, if link has loopID, remove loop node entirely, it will be recreated if needed later
                ProjectRoots.LockReading(delegate()
                {
                    foreach (uint project in ProjectRoots.Keys)
                    {
                        OpLink link = trust.GetLink(project);

                        if (link == null)
                            continue;

                        ProjectRoots[project].Remove(link);

                        // remove loop node
                        if (link.LoopRoot != null)
                            foreach (OpLink root in ProjectRoots[project])
                                if (root.DhtID == link.LoopRoot.DhtID)
                                {
                                    ProjectRoots[project].Remove(root); // root is a loop node

                                    // remove associations with loop node
                                    foreach (OpLink downlink in root.Downlinks)
                                        downlink.LoopRoot = null;

                                    break;
                                }
                    }
                });

                trust.Reset();


                // load data from link file
                FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                CryptoStream crypto = new CryptoStream(file, header.FileKey.CreateDecryptor(), CryptoStreamMode.Read);
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
                trust.Header = header;
                trust.SignedHeader = signedHeader.Encode(Core.Protocol);
                trust.Loaded = true;
                trust.Unique = Core.Loading;

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

                RunSaveHeaders = true;

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

                    Store.PublishDirect(locations, trust.DhtID, ComponentID.Trust, trust.SignedHeader);
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
            List<OpLink> roots = null;

            if (!ProjectRoots.SafeTryGetValue(link.Project, out roots))
            {
                roots = new List<OpLink>();
                ProjectRoots.SafeAdd(link.Project, roots);
            }

            if (!roots.Contains(link)) // possible it wasnt removed above because link might not be part of project locally but others think it does (uplink)
                roots.Add(link);
        }

        internal string GetFilePath(TrustHeader header)
        {
            return LinkPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, header.KeyID, header.FileHash);
        }

        private void Process_ProjectData(OpTrust trust, SignedData signed, ProjectData project)
        {
            if (!Utilities.CheckSignedData(trust.Key, signed.Data, signed.Signature))
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
            if (!Utilities.CheckSignedData(trust.Key, signed.Data, signed.Signature))
                return;

            Core.IndexKey(linkData.TargetID, ref linkData.Target);

            uint project = linkData.Project;

            OpLink localLink = trust.GetLink(project);

            if (localLink == null)
                return;

            OpTrust targetTrust = GetTrust(linkData.TargetID, false);

            if (targetTrust == null)
            {
                targetTrust = new OpTrust(linkData.Target);
                TrustMap.SafeAdd(linkData.TargetID, targetTrust);
            }

            targetTrust.AddProject(project, false);
            OpLink targetLink = targetTrust.GetLink(project);

            if (linkData.Uplink)
            {
                localLink.Uplink = targetLink;

                targetLink.Downlinks.Add(localLink);

                if (!targetTrust.Loaded && !StructureKnown)
                    StartSearch(targetTrust.DhtID, 0);

                if (targetLink.Uplink == null)
                    AddRoot(targetLink);
            }

            else
            {
                localLink.Confirmed.Add(targetLink.DhtID);
            }
        }

        internal void CheckVersion(ulong key, uint version)
        {
            OpTrust trust = GetTrust(key);

            if (trust != null && trust.Header != null)
                if (trust.Header.Version < version)
                    StartSearch(key, version);
        }

        internal uint CreateProject(string name)
        {
            uint id = (uint)Core.RndGen.Next();

            ProjectNames.SafeAdd(id, name);
            LocalTrust.AddProject(id, true);

            List<OpLink> roots = new List<OpLink>();
            roots.Add(LocalTrust.GetLink(id));
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
                Store.PublishDirect(locations, Core.LocalDhtID, ComponentID.Trust, LocalTrust.SignedHeader);
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
                List<OpLink> roots = null;
                if (ProjectRoots.SafeTryGetValue(project, out roots))
                    foreach (OpLink root in roots)
                        GetLinkLocs(root, 1, locations);
            }
        }

        internal void GetLocsBelow(ulong id, uint project, List<LocationData> locations)
        {
            // this is a spam type function that finds all locations (online nodes) below
            // a certain link.  it stops traversing down when an online node is found in a branch
            // the online node will call this function to continue traversing data down the network
            // upon being updated with the data object sent by whoever is calling this function

            OpLink link = GetLink(id, project);

            if (link != null)
                foreach (OpLink child in link.Downlinks)
                    if (!AddLinkLocations(child, locations))
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
            bool online = false;

            List<LocInfo> clients = Core.Locations.GetClients(link.DhtID);

            foreach (LocInfo info in clients)
                if (!info.Location.Global)
                {
                    if (info.Location.KeyID == Core.LocalDhtID && info.Location.Source.ClientID == Core.ClientID)
                        continue;

                    if (!locations.Contains(info.Location))
                        locations.Add(info.Location);

                    online = true;
                }

            return online;
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

            List<ulong> list = new List<ulong>();

            OpLink link = GetLink(local, project);

            if (link == null || link.LoopRoot != null)
                return list;

            OpLink uplink = link.GetHigher(true);

            while (uplink != null)
            {
                list.Add(uplink.DhtID);

                uplink = uplink.GetHigher(true);

                if (uplink != null && uplink.LoopRoot != null)
                    return list;
            }

            return list;
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
        internal ulong DhtID;
        internal ulong DhtBounds = ulong.MaxValue;
        internal byte[] Key;    // make sure reference is the same as main key list
        internal bool Loaded;
        internal bool Unique;

        internal bool InLocalLinkTree;
        internal bool Searched;

        internal TrustHeader Header;
        internal byte[] SignedHeader;

        internal Dictionary<uint, OpLink> Links = new Dictionary<uint, OpLink>();
        
   
        internal OpTrust(byte[] key)
        {
            Key = key;
            DhtID = Utilities.KeytoID(key);
        }

        // loop root object should now be OpLink
        internal OpTrust(uint project, ulong loopID)
        {
            Loaded = true;
            Name = "Trust Loop";
            DhtID = loopID;
            AddProject(project, true);
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
            if (!link.Active) 
                link.Active = active;
        }

        internal void RemoveProject(uint project)
        {
            OpLink link = GetLink(project);

            if (link == null)
                link.Active = false;

            link.Uplink = null;
            link.Title = "";
            link.Confirmed.Clear();
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
                    if (request.TargetVersion < Header.Version)
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

    }
}
