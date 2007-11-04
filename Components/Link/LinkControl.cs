using System;
using System.Collections.Generic;
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
    internal delegate void LinkUpdateHandler(OpLink link);
    internal delegate void LinkGuiUpdateHandler(ulong key);
    internal delegate List<ulong> LinkGetFocusedHandler();


    class LinkControl : OpComponent
    {
        internal OpCore   Core;
        internal DhtStore Store;
        internal DhtNetwork Network;

        internal OpLink LocalLink;

        internal ThreadedDictionary<ulong, OpLink> LinkMap = new ThreadedDictionary<ulong, OpLink>();
        internal ThreadedDictionary<uint, string> ProjectNames = new ThreadedDictionary<uint, string>();
        internal ThreadedDictionary<uint, List<OpLink>> ProjectRoots = new ThreadedDictionary<uint, List<OpLink>>();
        
        Dictionary<ulong, DateTime> NextResearch = new Dictionary<ulong, DateTime>();
        Dictionary<ulong, uint> DownloadLater = new Dictionary<ulong, uint>();

        internal string LinkPath;
        internal bool   StructureKnown;
        internal int    PruneSize = 100;

        bool RunSaveHeaders;
        RijndaelManaged LocalFileKey;

        internal LinkUpdateHandler     LinkUpdate;
        internal LinkGuiUpdateHandler  GuiUpdate;
        internal event LinkGetFocusedHandler GetFocused;


        internal LinkControl(OpCore core)
        {
            Core = core;
            Core.Links = this;
            
            Store = Core.OperationNet.Store;
            Network = Core.OperationNet;

            Core.TimerEvent += new TimerHandler(Core_Timer);
            Core.LoadEvent  += new LoadHandler(Core_Load);

            Network.EstablishedEvent += new EstablishedHandler(Network_Established);

            Store.StoreEvent[ComponentID.Link]     = new StoreHandler(Store_Local);
            Store.ReplicateEvent[ComponentID.Link] = new ReplicateHandler(Store_Replicate);
            Store.PatchEvent[ComponentID.Link]     = new PatchHandler(Store_Patch);

            Network.Searches.SearchEvent[ComponentID.Link] = new SearchRequestHandler(Search_Local);

            if (Core.Sim != null)
                PruneSize = 25;     
        }

        void Core_Load()
        {
            Core.Transfers.FileSearch[ComponentID.Link] = new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[ComponentID.Link] = new FileRequestHandler(Transfers_FileRequest);


            ProjectNames.SafeAdd(0, Core.User.Settings.Operation);

            LinkPath = Core.User.RootPath + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + ComponentID.Link.ToString();    
            Directory.CreateDirectory(LinkPath);

            LocalFileKey = Core.User.Settings.FileKey;

            LoadHeaders();


            LocalLink = GetLink(Core.LocalDhtID);


            if (LocalLink == null)
            {
                LocalLink = new OpLink(Core.User.Settings.KeyPublic);
                LinkMap.SafeAdd(Core.LocalDhtID, LocalLink);
            }

            if (!LocalLink.Loaded)
            {
                LocalLink.Name = Core.User.Settings.ScreenName;
                LocalLink.AddProject(0); // operation

                SaveLocal();
            } 
            
        }

        internal override List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong key, uint proj)
        {
            if (menuType != InterfaceMenuType.Quick)
                return null;

            List<MenuItemInfo> menus = new List<MenuItemInfo>();

            bool unlink = false;

            OpLink link = GetLink(key);

            if (link == null)
                return menus;

            // linkup
            if (Core.LocalDhtID != key &&
                (!LocalLink.Uplink.ContainsKey(proj) || LocalLink.Uplink[proj].DhtID != key) ) // not already linked to
                menus.Add( new MenuItemInfo("Trust", LinkRes.link, new EventHandler(Menu_Linkup)));

            // confirm
            if (LocalLink.Downlinks.ContainsKey(proj) && LocalLink.Downlinks[proj].Contains(link))
            {
                unlink = true;

                if (!LocalLink.Confirmed.ContainsKey(proj) || !LocalLink.Confirmed[proj].Contains(key)) // not already confirmed
                    menus.Add(new MenuItemInfo("Accept Trust", LinkRes.confirmlink, new EventHandler(Menu_ConfirmLink)));
            }

            // unlink
            if ((unlink && LocalLink.Confirmed.ContainsKey(proj) && LocalLink.Confirmed[proj].Contains(key)) || 
                (LocalLink.Uplink.ContainsKey(proj) && LocalLink.Uplink[proj].DhtID == key))
                menus.Add(new MenuItemInfo("Revoke Trust", LinkRes.unlink, new EventHandler(Menu_Unlink)));


            return menus;
        }

        private void Menu_Linkup(object sender, EventArgs e)
        {
            if (!(sender is IViewParams) || Core.GuiMain == null)
                return;

            ulong key = ((IViewParams)sender).GetKey();
            uint project = ((IViewParams)sender).GetProject();


            // get user confirmation if nullifying previous uplink
            if (LocalLink.Uplink.ContainsKey(project))
            {
                string who = GetName(LocalLink.Uplink[project].DhtID);
                string message = "Transfer trust from " + who + " to " + GetName(key) + "?";

                if (MessageBox.Show(Core.GuiMain, message, "Confirm Trust", MessageBoxButtons.YesNo) == DialogResult.No)
                    return;
            }

            try
            {
                OpLink uplink = GetLink(key);

                if (uplink == null)
                    throw new Exception("Could not find Person");

                // check if self
                if (uplink == LocalLink)
                    throw new Exception("Cannot Trust in your Self");

                // check if already linked
                if (LocalLink.Uplink.ContainsKey(project) && LocalLink.Uplink[project] == uplink)
                    throw new Exception("Already Trusting " + GetName(key));

                //crit check for loop
                //if (LocalLink.SearchBranch(proj, uplink))
                //    throw new Exception("Cannot uplink to a downlinked node");

                LocalLink.AddProject(project);
                LocalLink.ResetUplink(project);
                LocalLink.Uplink[project] = uplink;

                SaveLocal();

                Core.RunInCoreAsync(delegate()
                {
                    LinkupRequest(uplink, project);
                });
               
            }
            catch (Exception ex)
            {
                MessageBox.Show(Core.GuiMain, ex.Message);
            }
        }

        private void LinkupRequest(OpLink uplink, uint project)
        {
            

            // create uplink request, publish
            UplinkRequest request = new UplinkRequest();
            request.LinkVersion = LocalLink.Header.Version;
            request.TargetVersion = uplink.Header.Version;
            request.Key = LocalLink.Key;
            request.KeyID = LocalLink.DhtID;
            request.Target = uplink.Key;
            request.TargetID = uplink.DhtID;

            byte[] signed = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, request);
            Store.PublishNetwork(request.TargetID, ComponentID.Link, signed);

            // store locally
            Process_UplinkReq(null, new SignedData(Core.Protocol, Core.User.Settings.KeyPair, request), request);

            // publish at neighbors so they are aware of request status
            List<LocationData> locations = new List<LocationData>();
            GetLocs(Core.LocalDhtID, project, 1, 1, locations);
            GetLocsBelow(Core.LocalDhtID, project, locations);
            Store.PublishDirect(locations, request.TargetID, ComponentID.Link, signed);
        }

        private void Menu_ConfirmLink(object sender, EventArgs e)
        {
            if (!(sender is IViewParams) || Core.GuiMain == null)
                return;

            ulong key = ((IViewParams)sender).GetKey();
            uint proj = ((IViewParams)sender).GetProject();

            try
            {
                OpLink link = GetLink(key);

                if(link == null)
                    throw new Exception("Could not find Person");

                if (!LocalLink.Downlinks.ContainsKey(proj) || !LocalLink.Downlinks[proj].Contains(link))
                    throw new Exception(GetName(key) + " does not trust you");

                if (!LocalLink.Confirmed.ContainsKey(proj))
                    LocalLink.Confirmed[proj] = new List<ulong>();

                if (!LocalLink.Confirmed[proj].Contains(link.DhtID))
                    LocalLink.Confirmed[proj].Add(link.DhtID);

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
            uint proj = ((IViewParams)sender).GetProject();

            try
            {
                OpLink link = GetLink(key);

                bool unlinkUp = false;
                bool unlinkDown = false;

                if(link == null)
                    throw new Exception("Could not find Person");

                if (LocalLink.Uplink.ContainsKey(proj) && LocalLink.Uplink[proj] == link)
                    unlinkUp = true;

                if (LocalLink.Downlinks.ContainsKey(proj) && LocalLink.Confirmed.ContainsKey(proj))
                    unlinkDown = true;

                if (!unlinkUp && !unlinkDown)
                    throw new Exception("Cannot unlink from node");

                // make sure old links are notified of change
                List<LocationData> locations = new List<LocationData>();
                
                // remove node as an uplink
                OpLink parent = null;

                if (unlinkUp)
                {
                    GetLocs(Core.LocalDhtID, proj, 1, 1, locations);

                    parent = LocalLink.Uplink[proj];
                    LocalLink.ResetUplink(proj);
                    LocalLink.Uplink.Remove(proj);
                }

                // remove node from downlinks
                if (unlinkDown)
                {
                    LocalLink.Confirmed[proj].Remove(link.DhtID);

                    // removal of uplink requests done when version is updated by updatelocal
                }

                // update
                SaveLocal();

                // notify old links of change
                Core.RunInCoreAsync(delegate()
                {
                    Store.PublishDirect(locations, Core.LocalDhtID, ComponentID.Link, LocalLink.SignedHeader);
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
            if(Core.TimeNow.Second != 0)
                return;

            List<ulong> removeLinks = new List<ulong>();

            LinkMap.LockReading(delegate()
            {
               if (LinkMap.Count > PruneSize && StructureKnown)
               {
                   List<ulong> focused = GetFocusedLinks();

                   foreach (OpLink link in LinkMap.Values)
                       // if not focused, linked, or cached - remove
                       if (!link.InLocalLinkTree &&
                           link.DhtID != Core.LocalDhtID &&
                           !focused.Contains(link.DhtID) &&
                           !Utilities.InBounds(link.DhtID, link.DhtBounds, Core.LocalDhtID))
                       {
                           removeLinks.Add(link.DhtID);
                       }
               }
            });

            if (removeLinks.Count > 0)
                LinkMap.LockWriting(delegate()
                {
                    while (removeLinks.Count > 0 && LinkMap.Count > PruneSize / 2)
                    {
                        // find furthest id
                        ulong furthest = Core.LocalDhtID;

                        foreach (ulong id in removeLinks)
                            if ((id ^ Core.LocalDhtID) > (furthest ^ Core.LocalDhtID))
                                furthest = id;

                        // remove
                        OpLink link = LinkMap[furthest];
                        link.Reset();

                        foreach (uint proj in link.Projects)
                            if (link.Downlinks.ContainsKey(proj))
                                foreach (OpLink downlink in link.Downlinks[proj])
                                    if (downlink.Uplink.ContainsKey(proj))
                                        if (downlink.Uplink[proj] == link)
                                            downlink.Uplink[proj] = new OpLink(link.Key); // place holder

                        if (link.Header != null)
                            try { File.Delete(GetFilePath(link.Header)); }
                            catch { }

                        LinkMap.Remove(furthest);
                        removeLinks.Remove(furthest);
                        RunSaveHeaders = true;
                    }
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
            LinkMap.LockReading(delegate()
            {
                foreach (OpLink link in LinkMap.Values)
                {
                    link.DhtBounds = Store.RecalcBounds(link.DhtID);

                    // republish objects that were not seen on the network during startup
                    if (link.Unique && Utilities.InBounds(Core.LocalDhtID, localBounds, link.DhtID))
                        Store.PublishNetwork(link.DhtID, ComponentID.Link, link.SignedHeader);
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

            LinkMap.LockReading(delegate()
            {
                foreach (OpLink link in LinkMap.Values)
                    link.InLocalLinkTree = false;


                // TraverseDown 2 from self
                foreach (uint id in LocalLink.Projects)
                {
                    MarkBranchLinked(LocalLink, id, 2);

                    // TraverseDown 1 from all parents above self
                    OpLink parent = LocalLink.GetHigher(id, false);

                    while (parent != null)
                    {
                        MarkBranchLinked(parent, id, 1);
                        parent = parent.GetHigher(id, false);
                    }
                    

                    // TraverseDown 2 from Roots
                    List<OpLink> roots = null;
                    if (ProjectRoots.SafeTryGetValue(id, out roots))
                        foreach (OpLink link in roots)
                        {
                            // structure known if node found with no uplinks, and a number of downlinks
                            if (id == 0 && link.Loaded && !link.Uplink.ContainsKey(0))
                                if (link.Downlinks.ContainsKey(id) && link.Downlinks[id].Count > 0 && LinkMap.Count > 8)
                                    StructureKnown = true;

                            MarkBranchLinked(link, id, 2);
                        }
                }
            });
        }

        void MarkBranchLinked(OpLink link, uint id, int depth)
        {
            link.InLocalLinkTree = true;

            if ( !link.Searched )
            {
                Core.Locations.StartSearch(link.DhtID, 0, false);

                link.Searched = true;
            }

            if (depth > 0 && link.Downlinks.ContainsKey(id))
                foreach (OpLink downlink in link.Downlinks[id])
                    MarkBranchLinked(downlink, id, depth - 1);
        }

        internal void Research(ulong key, uint proj, bool searchDownlinks)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { Research(key, proj, searchDownlinks); });
                return;
            }

            if (!Network.Routing.Responsive())
                return;

            List<ulong> searchList = new List<ulong>();
            
            searchList.Add(key);

            OpLink link = GetLink(key);

            if (link != null)
            {
                // process_linkdata should add confirmed ids to linkmap, but they are not in downlinks list
                // unless the file is loaded (only links that specify their uplink as node x are in node x's downlink list

                // searchDownlinks - true re-search downlinks, false only search ids that are NOT in downlinks or linkmap
       
                List<ulong> downlinks = new List<ulong>();
                
                if (link.Downlinks.ContainsKey(proj))
                    foreach (OpLink downlink in link.Downlinks[proj])
                    {
                        if (searchDownlinks)
                            searchList.Add(downlink.DhtID);

                        downlinks.Add(downlink.DhtID);
                    }

                if (link.Confirmed.ContainsKey(proj))
                    foreach (ulong id in link.Confirmed[proj])
                        if(!searchList.Contains(id))
                            if(searchDownlinks || (!LinkMap.SafeContainsKey(id) && !downlinks.Contains(id)))
                                searchList.Add(id);

                if (link.Requests.ContainsKey(proj))
                    foreach (UplinkRequest request in link.Requests[proj])
                        if (!searchList.Contains(request.KeyID))
                            if (searchDownlinks || (!LinkMap.SafeContainsKey(request.KeyID) && !downlinks.Contains(request.KeyID)))
                                searchList.Add(request.KeyID);
            }


            foreach (ulong id in searchList)
            {
                uint version = 0;
                if (link != null)
                    version = link.Header.Version + 1;

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

            DhtSearch search = Network.Searches.Start(key, "Link", ComponentID.Link, parameters, new EndSearchHandler(EndSearch));

            if (search != null)
                search.TargetResults = 2;
        }

        void EndSearch(DhtSearch search)
        {
            foreach (SearchValue found in search.FoundValues)
                Store_Local(new DataReq(found.Sources, search.TargetID, ComponentID.Link, found.Value));
        }

        List<byte[]> Search_Local(ulong key, byte[] parameters)
        {
            List<Byte[]> results = new List<byte[]>();

            uint minVersion = BitConverter.ToUInt32(parameters, 0);

            OpLink link = GetLink(key);

            if (link != null)
            {
                if (link.Loaded && link.Header.Version >= minVersion)
                    results.Add(link.SignedHeader);

                foreach (uint id in link.Requests.Keys)
                    foreach (UplinkRequest request in link.Requests[id])
                        if (request.TargetVersion > minVersion)
                            results.Add(request.Signed);
            }

            return results;
        }

        bool Transfers_FileSearch(ulong key, FileDetails details)
        {
            OpLink link = GetLink(key);

            if (link != null)
                if (link.Loaded && details.Size == link.Header.FileSize && Utilities.MemCompare(details.Hash, link.Header.FileHash))
                    return true;

            return false;
        }

        string Transfers_FileRequest(ulong key, FileDetails details)
        {
            OpLink link = GetLink(key);

            if (link != null)
                if (link.Loaded && details.Size == link.Header.FileSize && Utilities.MemCompare(details.Hash, link.Header.FileHash))
                    return GetFilePath(link.Header);

            return null;
        }

        internal void RoutingUpdate(DhtContact contact)
        {
            // find node if structure not known
            if (StructureKnown)
                return;

            OpLink link = GetLink(contact.DhtID);

            if(link == null)
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
                if (embedded.Name == LinkPacket.LinkHeader)
                    Process_LinkHeader(store, signed, LinkHeader.Decode(Core.Protocol, embedded));

                else if (embedded.Name == LinkPacket.UplinkReq)
                    Process_UplinkReq(store, signed, UplinkRequest.Decode(Core.Protocol, embedded));
            }
        }

        const int PatchEntrySize = 12;

        ReplicateData Store_Replicate(DhtContact contact, bool add)
        {
            if (!Network.Established)
                return null;


            ReplicateData data = new ReplicateData(ComponentID.Link, PatchEntrySize);
            
            byte[] patch = new byte[PatchEntrySize];

            LinkMap.LockReading(delegate()
            {
                foreach (OpLink link in LinkMap.Values)
                    if (link.Loaded && Utilities.InBounds(link.DhtID, link.DhtBounds, contact.DhtID))
                    {
                        // bounds is a distance value
                        DhtContact target = contact;
                        link.DhtBounds = Store.RecalcBounds(link.DhtID, add, ref target);

                        if (target != null)
                        {
                            BitConverter.GetBytes(link.DhtID).CopyTo(patch, 0);
                            BitConverter.GetBytes(link.Header.Version).CopyTo(patch, 8);

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

                OpLink link = GetLink(dhtid);

                if (link != null)
                    if(link.Loaded && link.Header != null)
                    {
                        if(link.Header.Version > version)
                        {
                            Store.Send_StoreReq(source, 0, new DataReq(null, link.DhtID, ComponentID.Link, link.SignedHeader));
                            continue;
                        }

                        link.Unique = false; // network has current or newer version

                        if (link.Header.Version == version)
                            continue;

                        // else our version is old, download below
                    }
                
                if (Network.Established)
                    Network.Searches.SendDirectRequest(source, dhtid, ComponentID.Link, BitConverter.GetBytes(version));
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
                LinkHeader header = LocalLink.Header;

                string oldFile = null;

                if(header != null)
                    oldFile = GetFilePath(header);
                else
                    header = new LinkHeader();
                    

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
                foreach (uint id in LocalLink.Projects)
                {
                    ProjectData project = new ProjectData();
                    project.ID = id;
                    project.Name = GetProjectName(id);

                    if (id == 0)
                        project.UserName = LocalLink.Name;

                    project.UserTitle = LocalLink.Title[id];

                    byte[] packet = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, project);
                    stream.Write(packet, 0, packet.Length);


                    // uplinks
                    if (LocalLink.Uplink.ContainsKey(id))
                    {
                        LinkData link = new LinkData(id, LocalLink.Uplink[id].Key, true);
                        packet = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, link);
                        stream.Write(packet, 0, packet.Length);
                    }

                    // downlinks
                    if (LocalLink.Confirmed.ContainsKey(id))
                        foreach (OpLink downlink in LocalLink.Downlinks[id])
                            if (LocalLink.Confirmed[id].Contains(downlink.DhtID))
                            {
                                LinkData link = new LinkData(id, downlink.Key, false);
                                packet = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, link);
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

                if(oldFile != null && File.Exists(oldFile)) // delete after move to ensure a copy always exists (names different)
                    try { File.Delete(oldFile); }
                    catch { }

                // publish header
                Store.PublishNetwork(Core.LocalDhtID, ComponentID.Link, LocalLink.SignedHeader);

                Store.PublishDirect(GetSuperLocs(), Core.LocalDhtID, ComponentID.Link, LocalLink.SignedHeader);
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

                LinkMap.LockReading(delegate()
                {
                    foreach (OpLink link in LinkMap.Values)
                        if (link.SignedHeader != null)
                        {
                            stream.Write(link.SignedHeader, 0, link.SignedHeader.Length);

                            foreach (uint id in link.Requests.Keys)
                                foreach (UplinkRequest request in link.Requests[id])
                                    stream.Write(request.Signed, 0, request.Signed.Length);
                        }
                });

                stream.FlushFinalBlock();
                stream.Close();


                string finalPath = LinkPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "headers");
                File.Delete(finalPath);
                File.Move(tempPath, finalPath);
            }
            catch(Exception ex)
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

                while( stream.ReadPacket(ref root) )
                    if (root.Name == DataPacket.SignedData)
                    {
                        SignedData signed = SignedData.Decode(Core.Protocol, root);
                        G2Header embedded = new G2Header(signed.Data);

                        // figure out data contained
                        if (Core.Protocol.ReadPacket(embedded))
                        {
                            if (embedded.Name == LinkPacket.LinkHeader)
                                Process_LinkHeader(null, signed, LinkHeader.Decode(Core.Protocol, embedded));

                            else if (embedded.Name == LinkPacket.UplinkReq)
                                Process_UplinkReq(null, signed, UplinkRequest.Decode(Core.Protocol, embedded));
                        }
                    }

                stream.Close();
            }
            catch(Exception ex)
            {
                Network.UpdateLog("Link", "Error loading links " + ex.Message);
            }
        }

        private void Process_LinkHeader(DataReq data, SignedData signed, LinkHeader header)
        {
            Core.IndexKey(header.KeyID, ref header.Key);


            OpLink current = GetLink(header.KeyID);

            // if link loaded
            if (current != null)
            {
                // lower version
                if (header.Version < current.Header.Version)
                {
                    if (data != null && data.Sources != null)
                        foreach(DhtAddress source in data.Sources)
                            Store.Send_StoreReq(source, data.LocalProxy, new DataReq(null, current.DhtID, ComponentID.Link, current.SignedHeader));

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
            Core.IndexKey(request.KeyID,    ref request.Key);
            Core.IndexKey(request.TargetID, ref request.Target);
            Utilities.CheckSignedData(request.Key, signed.Data, signed.Signature);

            OpLink requesterLink = GetLink(request.KeyID);

            if (requesterLink != null && requesterLink.Loaded && requesterLink.Header.Version > request.LinkVersion)
                return;

            // check if target in linkmap, if not add
            OpLink targetLink = GetLink(request.TargetID);

            if (targetLink == null)
            {
                targetLink = new OpLink(request.Target);
                LinkMap.SafeAdd(request.TargetID, targetLink);
            }

            if (targetLink.Loaded && targetLink.Header.Version > request.TargetVersion)
                return;

            request.Signed = signed.Encode(Core.Protocol); // so we can send it in results / save, later on

            // check for duplicate requests
            if (targetLink.Requests.ContainsKey(request.ProjectID))
            {
                foreach(UplinkRequest compare in targetLink.Requests[request.ProjectID])
                    if( Utilities.MemCompare(compare.Signed, request.Signed))
                        return;
            }
            else
                targetLink.AddProject(request.ProjectID);

            // add
            targetLink.Requests[request.ProjectID].Add(request);


            // if target is marked as linked or focused, update link of target and sender
            if (targetLink.Loaded && (targetLink.InLocalLinkTree || GetFocusedLinks().Contains(targetLink.DhtID)))
            {
                if (targetLink.Header.Version < request.TargetVersion)
                    StartSearch(targetLink.DhtID, request.TargetVersion);

                if (requesterLink == null)
                {
                    requesterLink = new OpLink(request.Key);
                    LinkMap.SafeAdd(request.KeyID, requesterLink); 
                }

                // once new version of requester's link file has been downloaded, interface will be updated
                if (!requesterLink.Loaded || (requesterLink.Header.Version < request.LinkVersion))
                    StartSearch(requesterLink.DhtID, request.LinkVersion);
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

        private void DownloadLinkFile(SignedData signed, LinkHeader header)
        {
            Utilities.CheckSignedData(header.Key, signed.Data, signed.Signature);

            FileDetails details = new FileDetails(ComponentID.Link, header.FileHash, header.FileSize, null);

            Core.Transfers.StartDownload(header.KeyID, details, new object[] { signed, header }, new EndDownloadHandler(EndDownload));
        }

        private void EndDownload(string path, object[] args)
        {
            SignedData signedHeader = (SignedData) args[0];
            LinkHeader header       = (LinkHeader) args[1];

            string finalpath = GetFilePath(header);

            if (File.Exists(finalpath))
                return;

            File.Move(path, finalpath);

            CacheLinkFile(signedHeader, header);
        }

        private void CacheLinkFile(SignedData signedHeader, LinkHeader header)
        {
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
                OpLink link = null;
                LinkMap.SafeTryGetValue(header.KeyID, out link);

                if (link == null)
                {
                    link = new OpLink(header.Key);
                    LinkMap.SafeAdd(header.KeyID, link);
                }


                // delete old file
                if (link.Header != null)
                {
                    if (header.Version < link.Header.Version)
                        return; // dont update with older version

                    string oldPath = GetFilePath(link.Header);
                    if (path != oldPath && File.Exists(oldPath))
                        try { File.Delete(oldPath); }
                        catch { }
                }

                // clean roots
                List<uint> removeList = new List<uint>();

                ProjectRoots.LockReading(delegate()
                {
                    foreach (uint project in ProjectRoots.Keys)
                    {
                        ProjectRoots[project].Remove(link);

                        if (ProjectRoots[project].Count == 0)
                            removeList.Add(project);
                    }
                });

                ProjectRoots.LockWriting(delegate()
               {
                   foreach (uint project in removeList)
                   {
                       //ProjectNames.Remove(id); // if we are only root, and leave project, but have downlinks, still need the name
                       ProjectRoots.Remove(project);
                   }
               });

                link.Reset();


                // load data from link file
                FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                CryptoStream crypto = new CryptoStream(file, header.FileKey.CreateDecryptor(), CryptoStreamMode.Read);
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
                            if (embedded.Name == LinkPacket.ProjectData)
                                Process_ProjectData(link, signed, ProjectData.Decode(Core.Protocol, embedded));

                            else if (embedded.Name == LinkPacket.LinkData)
                                Process_LinkData(link, signed, LinkData.Decode(Core.Protocol, embedded));
                        }
                    }

                stream.Close();

                // set as root if node has no uplinks
                foreach (uint project in link.Projects)
                    if (!link.Uplink.ContainsKey(project))
                        AddRoot(project, link);

                foreach (uint project in link.Downlinks.Keys) // add root for projects this node is not apart of
                    if (!link.Uplink.ContainsKey(project))
                        AddRoot(project, link);

                

                // set new header
                link.Header = header;
                link.SignedHeader = signedHeader.Encode(Core.Protocol);
                link.Loaded = true;
                link.Unique = Core.Loading;

                link.CheckRequestVersions();

                RunSaveHeaders = true;

                if(LinkUpdate != null)
                    LinkUpdate.Invoke(link);

                if (Core.NewsWorthy(link.DhtID, 0, false))
                    Core.MakeNews("Trust updated by " + GetName(link.DhtID), link.DhtID, 0, true, LinkRes.link, null);
            

                // update subs
                if (Network.Established)
                {
                    List<LocationData> locations = new List<LocationData>();

                    ProjectRoots.LockReading(delegate()
                    {
                        foreach (uint project in ProjectRoots.Keys)
                            if (Core.LocalDhtID == link.DhtID || IsHigher(link.DhtID, project))
                                GetLocsBelow(Core.LocalDhtID, project, locations);
                    });

                    Store.PublishDirect(locations, link.DhtID, ComponentID.Link, link.SignedHeader);
                }

                // update interface node
                Core.RunInGuiThread(GuiUpdate, link.DhtID);

                foreach (uint id in link.Downlinks.Keys)
                    foreach (OpLink downlink in link.Downlinks[id])
                        Core.RunInGuiThread(GuiUpdate, downlink.DhtID);

            }
            catch (Exception ex)
            {
                Network.UpdateLog("Link", "Error loading file " + ex.Message);
            }
        }

        private void AddRoot(uint id, OpLink link)
        {
            List<OpLink> roots = null;

            if (!ProjectRoots.SafeTryGetValue(id, out roots))
            {
                roots = new List<OpLink>();
                ProjectRoots.SafeAdd(id, roots);
            }

            if (!roots.Contains(link)) // possible it wasnt removed above because link might not be part of project locally but others think it does (uplink)
                roots.Add(link);
        }

        internal string GetFilePath(LinkHeader header)
        {
            return LinkPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, header.KeyID, header.FileHash);
        }
        
        private void Process_ProjectData(OpLink link, SignedData signed, ProjectData project)
        {
            Utilities.CheckSignedData(link.Key, signed.Data, signed.Signature);

            if (project.ID != 0 && !ProjectNames.SafeContainsKey(project.ID))
                ProjectNames.SafeAdd(project.ID, project.Name);

            link.AddProject(project.ID);

            if(project.ID == 0)
                link.Name = project.UserName;

            link.Title[project.ID] = project.UserTitle;            
        }

        private void Process_LinkData(OpLink link, SignedData signed, LinkData linkData)
        {
            Utilities.CheckSignedData(link.Key, signed.Data, signed.Signature);

            Core.IndexKey(linkData.TargetID, ref linkData.Target);

            uint id = linkData.Project;
            if (!link.Projects.Contains(id))
                return;

            OpLink targetLink = GetLink(linkData.TargetID);

            if (targetLink == null)
            {
                targetLink = new OpLink(linkData.Target);
                LinkMap.SafeAdd(linkData.TargetID, targetLink);
            }

            if (linkData.Uplink)
            {
                if (link.SearchBranch(id, targetLink))
                {
                    link.Error = "Uplink contained in local branch";
                    return;
                }

                link.Uplink[id] = targetLink;

                if (!targetLink.Downlinks.ContainsKey(id))
                    targetLink.Downlinks[id] = new List<OpLink>();

                targetLink.Downlinks[id].Add(link);

                if (!targetLink.Loaded && !StructureKnown)
                    StartSearch(targetLink.DhtID, 0);

                if (!targetLink.Uplink.ContainsKey(id))
                    AddRoot(id, targetLink);
            }

            else
            {
                link.Confirmed[id].Add(targetLink.DhtID);
            }
        }

        internal void CheckVersion(ulong key, uint version)
        {
            OpLink link = GetLink(key);

            if (link != null && link.Header != null)
                if (link.Header.Version < version)
                    StartSearch(key, version);
        }

        internal uint CreateProject(string name)
        {
            uint id = (uint)Core.RndGen.Next();

            ProjectNames.SafeAdd(id, name);
            LocalLink.AddProject(id);

            List<OpLink> roots = new List<OpLink>();
            roots.Add(LocalLink);
            ProjectRoots.SafeAdd(id, roots);
            
            SaveLocal();

            return id;
        }

        internal void JoinProject(uint project)
        {
            if (project == 0)
                return;

            LocalLink.AddProject(project);

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

            LocalLink.Projects.Remove(project);

            SaveLocal();

            // update links in old project of update
            Core.RunInCoreAsync(delegate()
            {
                Store.PublishDirect(locations, Core.LocalDhtID, ComponentID.Link, LocalLink.SignedHeader);
            });
        }

        internal string GetName(ulong id)
        {
            OpLink link = GetLink(id);
            
            if(link != null && link.Name.Trim() != "")
               return link.Name;

            string name = id.ToString();
            return (name.Length > 5) ? name.Substring(0, 5) : name;
        }

        internal string GetProjectName(uint id)
        {
            string name = null;
            if(ProjectNames.SafeTryGetValue(id, out name))
                if(name.Trim() != "")
                    return name;

            name = id.ToString();
            return (name.Length > 5) ? name.Substring(0, 5) : name;
        }

        internal void GetLocs(ulong id, uint project, int up, int depth, List<LocationData> locations)
        {
            OpLink link = GetLink(id);

            if (link == null)
                return;

            OpLink uplink = TraverseUp(link, project, up);

            if (uplink != null)
                GetLinkLocs(uplink, project, depth, locations);

            // if at top, get nodes around roots
            else
            {
                List<OpLink> roots = null;
                if (ProjectRoots.SafeTryGetValue(project, out roots))
                    foreach (OpLink root in roots)
                        GetLinkLocs(root, project, 1, locations);
            }
        }

        internal void GetLocsBelow(ulong id, uint project, List<LocationData> locations)
        {
            // this is a spam type function that finds all locations (online nodes) below
            // a certain link.  it stops traversing down when an online node is found in a branch
            // the online node will call this function to continue traversing data down the network
            // upon being updated with the data object sent by whoever is calling this function

            OpLink link = GetLink(id);

            if (link != null)
                if (link.Downlinks.ContainsKey(project))
                    foreach (OpLink child in link.Downlinks[project])
                        if( !AddLinkLocations(child, locations)) 
                            GetLocsBelow(child.DhtID, project,  locations);
        }

        private OpLink TraverseUp(OpLink link, uint project, int distance)
        {
            // needs to get unconfiremd ids so unconfirmed above / below are updated with link status

            if (distance == 0)
                return link;

            int traverse = 0;

            OpLink uplink = link.GetHigher(project, false);

            while (uplink != null)
            {
                traverse++;
                if (traverse == distance)
                    return uplink;

                uplink = uplink.GetHigher(project, false);
            }

            return null;
        }

        private void GetLinkLocs(OpLink parent, uint project, int depth, List<LocationData> locations)
        {
            AddLinkLocations(parent, locations);

            if (depth > 0 && parent.Downlinks.ContainsKey(project))
                foreach (OpLink child in parent.Downlinks[project])
                    GetLinkLocs(child, project, depth - 1, locations);
        }

        private bool AddLinkLocations(OpLink link, List<LocationData> locations)
        {
            bool online = false;

              Core.Locations.LocationMap.LockReading(delegate()
              {
                  if (Core.Locations.LocationMap.ContainsKey(link.DhtID))
                      foreach (LocInfo info in Core.Locations.LocationMap[link.DhtID].Values)
                          if (!info.Location.Global)
                          {
                              if (info.Location.KeyID == Core.LocalDhtID && info.Location.Source.ClientID == Core.ClientID)
                                  continue;

                              if (!locations.Contains(info.Location))
                                  locations.Add(info.Location);

                              online = true;
                          }
              });

            return online;
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
            OpLink local = GetLink(localID);

            if (local == null)
                return false;   

            OpLink uplink = local.GetHigher(project, confirmed);

            while (uplink != null)
            {
                if (uplink.DhtID == higherID)
                    return true;

                uplink = uplink.GetHigher(project, confirmed);
            }

            return false;
        }


        internal bool IsLower(ulong localID, ulong lowerID, uint project)
        {
            OpLink lower = GetLink(lowerID);

            if (lower == null)
                return false; 

            OpLink uplink = lower.GetHigher(project, true);

            while (uplink != null)
            {
                if (uplink.DhtID == localID)
                    return true;

                uplink = uplink.GetHigher(project, true);
            }

            return false;
        }

        internal List<LocationData> GetSuperLocs()
        {
            List<LocationData> locations = new List<LocationData>();

            ProjectRoots.LockReading(delegate()
            {
                foreach (uint project in ProjectRoots.Keys)
                    GetLocs(Core.LocalDhtID, project, 1, 1, locations);  // below done by cacheplan
            });

            return locations;
        }

        internal List<ulong> GetUplinkIDs(ulong id, uint project)
        {
            return GetUplinkIDs(id, project, true);
        }

        internal List<ulong> GetUnconfirmedUplinkIDs(ulong id, uint project)
        {
            return GetUplinkIDs(id, project, false);
        }

        private List<ulong> GetUplinkIDs(ulong id, uint project, bool confirmed)
        {
            // get uplinks from id, not including id, starting with directly above and ending with root

            List<ulong> list = new List<ulong>();

            OpLink link = GetLink(id);

            if (link == null)
                return list;

            OpLink uplink = link.GetHigher(project, confirmed);

            while (uplink != null)
            {
                list.Add(uplink.DhtID);

                uplink = uplink.GetHigher(project, confirmed);
            }

            return list;
        }

        internal List<ulong> GetAdjacentIDs(ulong id, uint project)
        {
            List<ulong> list = new List<ulong>();

            OpLink link = GetLink(id);

            if (link == null)
                return list;

            OpLink uplink = link.GetHigher(project, true);

            if (uplink == null)
                return list;

            foreach(OpLink sub in uplink.GetLowers(project, true))
                list.Add(sub.DhtID);

            list.Remove(id);

            return list;
        }

        internal List<ulong> GetDownlinkIDs(ulong id, uint project, int levels)
        {
            List<ulong> list = new List<ulong>();

            OpLink link = GetLink(id);

            if (link == null)
                return list;

            levels--;

            if (link.Confirmed.ContainsKey(project) && link.Downlinks.ContainsKey(project))
                foreach (OpLink downlink in link.Downlinks[project])
                    if (link.Confirmed[project].Contains(downlink.DhtID))
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

           OpLink link = GetLink(id);

            if (link != null)
                if (link.Confirmed.ContainsKey(project) && link.Downlinks.ContainsKey(project))
                    foreach (OpLink downlink in link.Downlinks[project])
                        if (link.Confirmed[project].Contains(downlink.DhtID))
                            count++;

            return count > 0;
        }

        internal bool IsAdjacent(ulong id, uint project)
        {
            OpLink higher = LocalLink.GetHigher(project, true);

            if (higher != null &&
                higher.Confirmed.ContainsKey(project) &&
                higher.Confirmed[project].Contains(id) &&
                higher.Downlinks.ContainsKey(project))
                foreach (OpLink downlink in higher.Downlinks[project])
                    if (downlink.DhtID == id)
                        return true;

            return false;
        }

        internal bool IsLowerDirect(ulong id, uint project)
        {
            if (LocalLink.Confirmed.ContainsKey(project) &&
                LocalLink.Confirmed[project].Contains(id) &&
                LocalLink.Downlinks.ContainsKey(project))
                foreach (OpLink downlink in LocalLink.Downlinks[project])
                    if (downlink.DhtID == id)
                        return true;

            return false;
        }

        internal bool IsHigherDirect(ulong id, uint project)
        {
            OpLink link = GetLink(id);

            if (link == null)
                return false;

            OpLink uplink = link.GetHigher(project, true);

            if (uplink == null)
                return false;

            return uplink.DhtID == id;
        }

        internal bool IsInScope(Dictionary<ulong, short> scope, ulong testID, uint project)
        {
            List<ulong> uplinks = GetUplinkIDs(testID, project);

            
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

        internal OpLink GetLink(ulong id)
        {
            OpLink link = null;

            LinkMap.SafeTryGetValue(id, out link);

            if (link == null || !link.Loaded)
                return null;

            return link;
        }


    }


    internal class OpLink
    {
        internal string   Name;
        internal ulong    DhtID;
        internal ulong    DhtBounds = ulong.MaxValue;
        internal byte[]   Key;    // make sure reference is the same as main key list
        internal bool     Loaded;
        internal bool     Unique; 
        internal string   Error;
        internal bool     InLocalLinkTree;
        internal bool     Searched;

        internal List<uint> Projects = new List<uint>();
        internal Dictionary<uint, string> Title = new Dictionary<uint, string>();

        internal Dictionary<uint, OpLink>       Uplink    = new Dictionary<uint, OpLink>();
        internal Dictionary<uint, List<OpLink>> Downlinks = new Dictionary<uint, List<OpLink>>();
        internal Dictionary<uint, List<ulong>>  Confirmed = new Dictionary<uint, List<ulong>>();
        internal Dictionary<uint, List<UplinkRequest>> Requests = new Dictionary<uint, List<UplinkRequest>>();

        internal LinkHeader Header;
        internal byte[] SignedHeader;


        internal OpLink(byte[] key)
        {
            Key = key;
            DhtID = Utilities.KeytoID(key);
        }

        internal void AddProject(uint id)
        {
            if (!Projects.Contains(id))
            {
                Projects.Add(id);
                Title[id] = "";
                Confirmed[id] = new List<ulong>();
                Requests[id] = new List<UplinkRequest>();
            }
            
            // nodes can have downlinks in projects they themselves are not part of

            if (!Downlinks.ContainsKey(id))
                Downlinks[id] = new List<OpLink>();
        }

        internal void Reset()
        {
            // find nodes we're uplinked to and remove ourselves from their downlink list
            foreach (uint id in Uplink.Keys)
                ResetUplink(id);


            // only clear downlinks that are no longer uplinked to us
            List<uint> removeIDs = new List<uint>();

            foreach (uint id in Downlinks.Keys)
            {
                List<OpLink> list = Downlinks[id];
                List<OpLink> remove = new List<OpLink>();

                foreach (OpLink downlink in Downlinks[id])
                    if (downlink.Uplink.ContainsKey(id))
                        if (downlink.Uplink[id] != this)
                            remove.Add(downlink);

                foreach (OpLink downlink in remove)
                    list.Remove(downlink);

                if (list.Count == 0)
                    removeIDs.Add(id);
            }

            foreach (uint id in removeIDs)
                Downlinks.Remove(id);
 
            Projects.Clear();
            Title.Clear();

            Uplink.Clear();
            Confirmed.Clear();

            Error = null;
        }

        internal void ResetUplink(uint id)
        {
            if (!Uplink.ContainsKey(id))
                return;

            if (Uplink[id].Downlinks.ContainsKey(id))
                Uplink[id].Downlinks[id].Remove(this);

            // uplink requests are invalidated on verion update also
            // not the local ones, but the ones this link issued to previous uplink
            if (Uplink[id].Requests.ContainsKey(id))
                foreach (UplinkRequest request in Uplink[id].Requests[id])
                    if (request.KeyID == DhtID)
                    {
                        Uplink[id].Requests[id].Remove(request);
                        break;
                    }
        }

        internal bool SearchBranch(uint proj, OpLink find)
        {
            if (Downlinks.ContainsKey(proj))
                foreach (OpLink link in Downlinks[proj])
                {
                    if (link == find)
                        return true;

                    if (link.SearchBranch(proj, find))
                        return true;
                }

            return false;
        }

        internal void CheckRequestVersions()
        {
            List<UplinkRequest> removeList = new List<UplinkRequest>();

            // check target
            foreach (uint id in Requests.Keys)
            {
                removeList.Clear();

                foreach (UplinkRequest request in Requests[id])
                    if (request.TargetVersion < Header.Version)
                        removeList.Add(request);

                foreach (UplinkRequest request in removeList)
                    Requests[id].Remove(request);
            }
        }

        internal OpLink GetHigher(uint project, bool confirmed)
        {
            if (!Uplink.ContainsKey(project))
                return null;

            if (!confirmed)
                return Uplink[project];

            OpLink uplink = Uplink[project];

             if (!uplink.Confirmed.ContainsKey(project))
                return null;

            // if we are one of the uplinks confirmed downlinks then return trusted uplink
            if (uplink.Confirmed[project].Contains(DhtID)) 
                return uplink;
             
            return null;
        }

        internal List<OpLink> GetLowers(uint project, bool confirmed)
        {
            List<OpLink> lowers = new List<OpLink>();

             if (Downlinks.ContainsKey(project))
                 if(!confirmed || Confirmed.ContainsKey(project))
                    foreach (OpLink downlink in Downlinks[project])
                        if (!confirmed || Confirmed[project].Contains(downlink.DhtID))
                            lowers.Add(downlink);

            return lowers;
        }
    }
}
