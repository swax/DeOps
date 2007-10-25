using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Components.Link;
using DeOps.Components.Location;
using DeOps.Components.Transfer;


namespace DeOps.Components.Storage
{
    internal delegate List<ulong> StorageGetFocusedHandler();
    internal delegate void StorageUpdateHandler(OpStorage storage);
    internal delegate void WorkingUpdateHandler(uint project, string dir, ulong uid, WorkingChange action);


    class StorageControl : OpComponent
    {
        internal OpCore Core;
        internal G2Protocol Protocol;
        internal DhtNetwork Network;
        internal DhtStore Store;
        internal LinkControl Links;

        internal string StoragePath;
        internal OpStorage LocalStorage;
        internal RijndaelManaged LocalFileKey;

        bool RunSaveHeaders;
        bool SavingLocal;
        internal StorageUpdateHandler StorageUpdate;
        internal event StorageGetFocusedHandler GetFocused;

        internal Dictionary<ulong, OpStorage> StorageMap = new Dictionary<ulong, OpStorage>();
        internal Dictionary<ulong, OpFile> FileMap = new Dictionary<ulong, OpFile>();
        internal Dictionary<ulong, OpFile> InternalFileMap = new Dictionary<ulong, OpFile>();// used to bring together files encrypted with different keys

        Dictionary<ulong, DateTime> NextResearch = new Dictionary<ulong, DateTime>();
        Dictionary<ulong, uint> DownloadLater = new Dictionary<ulong, uint>();

        int PruneSize = 100;

        int FileBufferSize = 4096;
        byte[] FileBuffer = new byte[4096]; // needs to be 4k to packet stream break/resume work


        // working
        internal Dictionary<uint, WorkingStorage> Working = new Dictionary<uint, WorkingStorage>();

        internal WorkingUpdateHandler WorkingFileUpdate;
        internal WorkingUpdateHandler WorkingFolderUpdate;

    
        Thread HashThreadHandle;
        internal Queue<HashPack> HashQueue = new Queue<HashPack>();


        internal StorageControl(OpCore core)
        {
            Core = core;
            Protocol = core.Protocol;
            Core.Storages = this;
            Network = core.OperationNet;
            Store = Network.Store;

            Core.LoadEvent += new LoadHandler(Core_Load);
            Core.ExitEvent += new ExitHandler(Core_Exit);
            Core.TimerEvent += new TimerHandler(Core_Timer);

            Network.EstablishedEvent += new EstablishedHandler(Network_Established);

            Store.StoreEvent[ComponentID.Storage] = new StoreHandler(Store_Local);
            Store.ReplicateEvent[ComponentID.Storage] = new ReplicateHandler(Store_Replicate);
            Store.PatchEvent[ComponentID.Storage] = new PatchHandler(Store_Patch);

            Network.Searches.SearchEvent[ComponentID.Storage] = new SearchRequestHandler(Search_Local);

            if (Core.Sim != null)
                PruneSize = 25;  
        }
        
        void Core_Load()
        {
            Links = Core.Links;

            Core.Transfers.FileSearch[ComponentID.Storage] = new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[ComponentID.Storage] = new FileRequestHandler(Transfers_FileRequest);

            Core.Links.LinkUpdate += new LinkUpdateHandler(Links_LinkUpdate);

            StoragePath = Core.User.RootPath + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + ComponentID.Storage.ToString();
            Directory.CreateDirectory(StoragePath);
            Directory.CreateDirectory(StoragePath + Path.DirectorySeparatorChar + "0");
            Directory.CreateDirectory(StoragePath + Path.DirectorySeparatorChar + "1");

            // clear resource files so that updates of these files work
            string resPath = StoragePath + Path.DirectorySeparatorChar + "2";
            if (Directory.Exists(resPath))
                Directory.Delete(resPath, true);

            LocalFileKey = Core.User.Settings.FileKey;

            LoadHeaders();


            lock (StorageMap)
            {
                if (!StorageMap.ContainsKey(Core.LocalDhtID))
                {
                    StorageMap[Core.LocalDhtID] = new OpStorage(Core.User.Settings.KeyPublic);
                    SaveLocal(0);
                }

                LocalStorage = StorageMap[Core.LocalDhtID];
            }
            
            // load working headers
            foreach (uint project in Links.LocalLink.Projects)
            {
                LoadHeaderFile(GetWorkingPath(project), LocalStorage, false, true);
                Working[project] = new WorkingStorage(this, project);

                bool doSave = false;
                foreach (ulong higher in Links.GetUplinkIDs(Core.LocalDhtID, project))
                    if(Working[project].RefreshHigherChanges(higher))
                        doSave = true ;

                Working[project].AutoIntegrate(doSave);
            }
        }

        void Core_Exit()
        {
      
            if(HashingActive())
            {
                HashStatus status = new HashStatus(this);
                status.ShowDialog();
            }

            // lock down working
            List<LockError> errors = new List<LockError>();

            foreach (WorkingStorage working in Working.Values)
            {
                working.LockAll(errors);

                if(working.Modified)
                    working.SaveWorking();
            }
            Working.Clear();

            // delete completely folders made for other user's storages
            foreach (uint project in Links.ProjectRoots.Keys)
            {
                string path = Core.User.RootPath + Path.DirectorySeparatorChar + Links.ProjectNames[project] + " Storage";
                string local = Links.GetName(Core.LocalDhtID);

                if(Directory.Exists(path))
                    foreach (string dir in Directory.GetDirectories(path))
                        if (Path.GetFileName(dir) != local)
                        {
                            try
                            {
                                Directory.Delete(dir, true);
                            }
                            catch (Exception ex)
                            {
                                errors.Add(new LockError(dir, "", false, LockErrorType.Blocked));
                            }
                        }
            }

            // security warning: could not secure these files
            if (errors.Count > 0)
            {
                string message = "Security Warning: Not able to delete these files, please do it manually\n";

                foreach (LockError error in errors)
                    if (error.Type == LockErrorType.Blocked)
                        message += error.Path;

                MessageBox.Show(message, "De-Ops");
            }
        }

        void Core_Timer()
        {
            if (RunSaveHeaders)
                SaveHeaders();

            // hashing
            if (HashQueue.Count > 0 && (HashThreadHandle == null || !HashThreadHandle.IsAlive))
            {
                HashThreadHandle = new Thread(new ThreadStart(HashThread));
                HashThreadHandle.Start();
            }

            // clean download later map
            if (!Network.Established)
                Utilities.PruneMap(DownloadLater, Core.LocalDhtID, PruneSize);


            // every 10 seconds
            if (Core.TimeNow.Second % 9 == 0) 
                foreach(WorkingStorage working in Working.Values)
                    if (working.PeriodicSave)
                    {
                        working.SaveWorking();
                        working.PeriodicSave = false;
                    }


            // do below once per minute
            if (Core.TimeNow.Second != 0)
                return;


            List<ulong> focused = new List<ulong>();

            if (GetFocused != null)
                foreach (StorageGetFocusedHandler handler in GetFocused.GetInvocationList())
                    foreach (ulong id in handler.Invoke())
                        if (!focused.Contains(id))
                            focused.Add(id);

            // prune
            List<ulong> removeIDs = new List<ulong>();

            if (StorageMap.Count > PruneSize)
            {
                foreach (OpStorage storage in StorageMap.Values)
                    if (storage.DhtID != Core.LocalDhtID &&
                        !Core.Links.LinkMap.ContainsKey(storage.DhtID) && // dont remove nodes in our local hierarchy
                        !focused.Contains(storage.DhtID) &&
                        !Utilities.InBounds(storage.DhtID, storage.DhtBounds, Core.LocalDhtID))
                        removeIDs.Add(storage.DhtID);

                while (removeIDs.Count > 0 && StorageMap.Count > PruneSize / 2)
                {
                    ulong furthest = Core.LocalDhtID;
                    OpStorage storage = StorageMap[furthest];

                    foreach (ulong id in removeIDs)
                        if ((id ^ Core.LocalDhtID) > (furthest ^ Core.LocalDhtID))
                            furthest = id;

                    UnloadHeaderFile(GetFilePath(storage.Header), storage.Header.FileKey);

                    if (storage.Header != null)
                        try { File.Delete(GetFilePath(storage.Header)); }
                        catch { }

                    StorageMap.Remove(furthest);
                    removeIDs.Remove(furthest);
                    RunSaveHeaders = true;
                }
            }

            // clean research map
            removeIDs.Clear();

            foreach (KeyValuePair<ulong, DateTime> pair in NextResearch)
                if (Core.TimeNow > pair.Value)
                    removeIDs.Add(pair.Key);

            foreach (ulong id in removeIDs)
                NextResearch.Remove(id);


            // clear de-reffed files
            foreach (KeyValuePair<ulong, OpFile> pair in FileMap)
                if (pair.Value.References == 0)
                    File.Delete(GetFilePath(pair.Key)); //crit test
        }

        internal override List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong key, uint proj)
        {
            List<MenuItemInfo> menus = new List<MenuItemInfo>();

            if (menuType == InterfaceMenuType.Internal)
                menus.Add(new MenuItemInfo("Data/Storage", StorageRes.Icon, new EventHandler(Menu_View)));

            if (menuType == InterfaceMenuType.External)
                menus.Add(new MenuItemInfo("Storage", StorageRes.Icon, new EventHandler(Menu_View)));


            return menus;
        }

        internal void Menu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            StorageView view = new StorageView(this, node.GetKey(), node.GetProject());

            Core.InvokeView(node.IsExternal(), view);
        }

        internal void CallFolderUpdate(uint project, string dir, ulong uid, WorkingChange action)
        {
            if (WorkingFolderUpdate != null)
                Core.InvokeInterface(WorkingFolderUpdate, project, dir, uid, action);
        }

        internal void CallFileUpdate(uint project, string dir, ulong uid, WorkingChange action)
        {
            if (WorkingFileUpdate != null)
                Core.InvokeInterface(WorkingFileUpdate, project, dir, uid, action);
        }
        
        private void LoadHeaders()
        {
            try
            {
                string path = StoragePath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "headers");

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
                            if (embedded.Name == StoragePacket.Header)
                                Process_StorageHeader(null, signed, StorageHeader.Decode(Core.Protocol, embedded));
                        }
                    }

                stream.Close();
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Storage", "Error loading headers " + ex.Message);
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

                lock (StorageMap)
                    foreach (OpStorage storage in StorageMap.Values)
                        if (storage.SignedHeader != null)
                            stream.Write(storage.SignedHeader, 0, storage.SignedHeader.Length);

                stream.FlushFinalBlock();
                stream.Close();


                string finalPath = StoragePath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "headers");
                File.Delete(finalPath);
                File.Move(tempPath, finalPath);
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Storage", "Error saving headers " + ex.Message);
            }
        }

        internal void SaveLocal(uint project)
        {
            try
            {
                StorageHeader header = null;
                if (StorageMap.ContainsKey(Core.LocalDhtID))
                    header = StorageMap[Core.LocalDhtID].Header;

                string oldFile = null;
                PacketStream oldStream = null;

                if (header != null)
                    oldFile = GetFilePath(header);
                else
                    header = new StorageHeader();


                if (oldFile != null)
                {
                    FileStream file = new FileStream(oldFile, FileMode.Open, FileAccess.Read);
                    CryptoStream crypto = new CryptoStream(file, header.FileKey.CreateDecryptor(), CryptoStreamMode.Read);
                    oldStream = new PacketStream(crypto, Protocol, FileAccess.Read);
                }


                header.Key = Core.User.Settings.KeyPublic;
                header.KeyID = Core.LocalDhtID; // set so keycheck works
                header.Version++;
                header.Date = Core.TimeNow;
                header.FileKey.GenerateKey();
                header.FileKey.IV = new byte[header.FileKey.IV.Length];

                string tempPath = Core.GetTempPath();
                FileStream tempFile = new FileStream(tempPath, FileMode.CreateNew);
                CryptoStream stream = new CryptoStream(tempFile, header.FileKey.CreateEncryptor(), CryptoStreamMode.Write);

                // write loaded projects
                WorkingStorage working = null;
                if (Working.ContainsKey(project))
                    working = Working[project];

                if(working != null)
                {
                    Protocol.WriteToFile(new StorageRoot(working.ProjectID), stream);
                    working.WriteWorkingFile(stream, working.RootFolder, true);

                    working.Modified = false;

                    try { File.Delete(GetWorkingPath(project)); }
                    catch { }

                }

                // open old file and copy entries, except for working
                if (oldStream != null)
                {
                    bool write = false;
                    G2Header g2header = null;

                    while (oldStream.ReadPacket(ref g2header))
                    {
                        if (g2header.Name == StoragePacket.Root)
                        {
                            StorageRoot root = StorageRoot.Decode(Protocol, g2header);

                            write = (root.ProjectID != project);
                        }

                        //copy packet right to new file
                        if (write) //crit test
                            stream.Write(g2header.Data, g2header.PacketPos, g2header.PacketSize); 
                    }

                    oldStream.Close();
                }

                stream.FlushFinalBlock();
                stream.Close();


                // finish building header
                Utilities.ShaHashFile(tempPath, ref header.FileHash, ref header.FileSize);

                // move file, overwrite if need be
                string finalPath = GetFilePath(header);
                File.Move(tempPath, finalPath);

                SavingLocal = true; // prevents auto-integrate from re-calling saveLocal
                CacheStorage(new SignedData(Core.Protocol, Core.User.Settings.KeyPair, header), header);
                SavingLocal = false;

                SaveHeaders();

                if (oldFile != null && File.Exists(oldFile)) // delete after move to ensure a copy always exists (names different)
                    try { File.Delete(oldFile); }
                    catch { }

                // publish header
                Store.PublishNetwork(Core.LocalDhtID, ComponentID.Storage, StorageMap[Core.LocalDhtID].SignedHeader);

                Store.PublishDirect(Links.GetSuperLocs(), Core.LocalDhtID, ComponentID.Storage, StorageMap[Core.LocalDhtID].SignedHeader);

            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Storage", "Error updating local " + ex.Message);
            }
        }

        private void Process_StorageHeader(DataReq data, SignedData signed, StorageHeader header)
        {
            Core.IndexKey(header.KeyID, ref header.Key);
            Utilities.CheckSignedData(header.Key, signed.Data, signed.Signature);


            // if link loaded
            if (StorageMap.ContainsKey(header.KeyID))
            {
                OpStorage current = StorageMap[header.KeyID];

                // lower version
                if (header.Version < current.Header.Version)
                {
                    if (data != null && data.Sources != null)
                        foreach (DhtAddress source in data.Sources)
                            Store.Send_StoreReq(source, data.LocalProxy, new DataReq(null, current.DhtID, ComponentID.Storage, current.SignedHeader));

                    return;
                }

                // higher version
                else if (header.Version > current.Header.Version)
                {
                    CacheStorage(signed, header);
                }
            }

            // else load file, set new header after file loaded
            else
                CacheStorage(signed, header);
        }

        private void DownloadStorage(SignedData signed, StorageHeader header)
        {
            FileDetails details = new FileDetails(ComponentID.Storage, header.FileHash, header.FileSize, BitConverter.GetBytes(StoragePacket.Header));

            Core.Transfers.StartDownload(header.KeyID, details, new object[] { signed, header }, new EndDownloadHandler(EndDownload));
        }

        private void EndDownload(string path, object[] args)
        {
            SignedData signedHeader = (SignedData)args[0];
            StorageHeader header = (StorageHeader)args[1];

            try
            {
                File.Move(path, GetFilePath(header));
            }
            catch 
            { 
                return;
            }

            CacheStorage(signedHeader, header);
        }

        private void CacheStorage(SignedData signedHeader, StorageHeader header)
        {
            try
            {
                // check if file exists           
                string path = GetFilePath(header);
                if (!File.Exists(path))
                {
                    DownloadStorage(signedHeader, header);
                    return;
                }

                // get storage
                if (!StorageMap.ContainsKey(header.KeyID))
                {
                    lock (StorageMap)
                        StorageMap[header.KeyID] = new OpStorage(header.Key);
                }

                OpStorage storage = StorageMap[header.KeyID];

                // delete old file
                if (storage.Header != null)
                {
                    if (header.Version < storage.Header.Version)
                        return; // dont update with older version

                    string oldPath = GetFilePath(storage.Header);
                    UnloadHeaderFile(oldPath, storage.Header.FileKey);

                    if (path != oldPath && File.Exists(oldPath))
                        try { File.Delete(oldPath); }
                        catch { }
                }

                // set new header
                storage.Header = header;
                storage.SignedHeader = signedHeader.Encode(Core.Protocol);
                storage.Unique = Core.Loading;

                RunSaveHeaders = true;

                LoadHeaderFile(path, storage, false, false);

                // record changes of higher nodes for auto-integration purposes
                foreach (uint project in Links.ProjectRoots.Keys)
                    if (Core.LocalDhtID == storage.DhtID || Links.IsHigher(storage.DhtID, project))
                        // doesnt get called on startup because working not initialized before headers are loaded
                        if (Working.ContainsKey(project))
                        {
                            bool doSave = Working[project].RefreshHigherChanges(storage.DhtID);

                            if (!Core.Loading && !SavingLocal)
                                Working[project].AutoIntegrate(doSave);
                        }

                // update subs
                if (Network.Established)
                {
                    List<LocationData> locations = new List<LocationData>();
                    foreach (uint project in Links.ProjectRoots.Keys)
                        if (storage.DhtID == Core.LocalDhtID || Links.IsHigher(storage.DhtID, project))
                            Links.GetLocsBelow(Core.LocalDhtID, project, locations);

                    Store.PublishDirect(locations, storage.DhtID, ComponentID.Storage, storage.SignedHeader);
                }

                if (StorageUpdate != null)
                    Core.InvokeInterface(StorageUpdate, storage);

                if (Core.NewsWorthy(storage.DhtID, 0, false))
                    Core.MakeNews("Storage updated by " + Links.GetName(storage.DhtID), storage.DhtID, 0, false, StorageRes.Icon, Menu_View);
         
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Storage", "Error caching storage " + ex.Message);
            }
        }

        void Links_LinkUpdate(OpLink link)
        {
            // update working projects (add)
            if (link.DhtID == Core.LocalDhtID)
                foreach (uint project in Links.LocalLink.Projects)
                    if (!Working.ContainsKey(project))
                    {
                        LoadHeaderFile(GetWorkingPath(project), LocalStorage, false, true);
                        Working[project] = new WorkingStorage(this, project);
                    }


            // remove all higher changes, reload with new highers (cause link changed
            foreach (WorkingStorage working in Working.Values )
                if (Core.LocalDhtID == link.DhtID || Links.IsHigher(link.DhtID, working.ProjectID))
                {
                    working.RemoveAllHigherChanges();

                    foreach (ulong uplink in Links.GetUplinkIDs(Core.LocalDhtID, working.ProjectID))
                        working.RefreshHigherChanges(uplink);
                }
        }

        bool Transfers_FileSearch(ulong key, FileDetails details)
        {
            if (details.Extra.Length == 0)
                return false;

            if (details.Extra[0] == StoragePacket.Header)
            {
                lock (StorageMap)
                    if (StorageMap.ContainsKey(key))
                    {
                        OpStorage storage = StorageMap[key];

                        if (details.Size == storage.Header.FileSize && Utilities.MemCompare(details.Hash, storage.Header.FileHash))
                            return true;
                    }
            }

            if (details.Extra[0] == StoragePacket.File)
            {
                ulong hashID = BitConverter.ToUInt64(details.Hash, 0);

                if (FileMap.ContainsKey(hashID))
                {
                    OpFile file = FileMap[hashID];

                    if (details.Size == file.Size && Utilities.MemCompare(details.Hash, file.Hash))
                        return true;
                }
            }

            return false;
        }

        string Transfers_FileRequest(ulong key, FileDetails details)
        {
             if (details.Extra.Length == 0)
                 return null;

            if (details.Extra[0] == StoragePacket.Header)
            {
                lock (StorageMap)
                    if (StorageMap.ContainsKey(key))
                    {
                        OpStorage storage = StorageMap[key];

                        if (details.Size == storage.Header.FileSize && Utilities.MemCompare(details.Hash, storage.Header.FileHash))
                            return GetFilePath(storage.Header);
                    }
            }

            if (details.Extra[0] == StoragePacket.File)
            {
                ulong hashID = BitConverter.ToUInt64(details.Hash, 0);

                if (FileMap.ContainsKey(hashID))
                {
                    OpFile file = FileMap[hashID];

                    if (details.Size == file.Size && Utilities.MemCompare(details.Hash, file.Hash))
                        return GetFilePath(hashID);
                }
            }

            return null;
        }

        void Store_Local(DataReq store)
        {
            // getting published to - search results - patch

            SignedData signed = SignedData.Decode(Core.Protocol, store.Data);
            StorageHeader header = StorageHeader.Decode(Core.Protocol, signed.Data);

            Process_StorageHeader(null, signed, header);
        }

        const int PatchEntrySize = 12;

        ReplicateData Store_Replicate(DhtContact contact, bool add)
        {
            if (!Network.Established)
                return null;

            ReplicateData data = new ReplicateData(ComponentID.Storage, PatchEntrySize);

            byte[] patch = new byte[PatchEntrySize];

            lock (StorageMap)
                foreach (OpStorage storage in StorageMap.Values)
                    if (Utilities.InBounds(storage.DhtID, storage.DhtBounds, contact.DhtID))
                    {
                        DhtContact target = contact;
                        storage.DhtBounds = Store.RecalcBounds(storage.DhtID, add, ref target);

                        if (target != null)
                        {
                            BitConverter.GetBytes(storage.DhtID).CopyTo(patch, 0);
                            BitConverter.GetBytes(storage.Header.Version).CopyTo(patch, 8);

                            data.Add(target, patch);
                        }
                    }

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

                if (StorageMap.ContainsKey(dhtid))
                {
                    OpStorage storage = StorageMap[dhtid];

                    if (storage.Header != null)
                    {
                        if (storage.Header.Version > version)
                        {
                            Store.Send_StoreReq(source, 0, new DataReq(null, storage.DhtID, ComponentID.Storage, storage.SignedHeader));
                            continue;
                        }

                        storage.Unique = false; // network has current or newer version

                        if (storage.Header.Version == version)
                            continue;

                        // else our version is old, download below
                    }
                }

                if (Network.Established)
                    Network.Searches.SendDirectRequest(source, dhtid, ComponentID.Storage, BitConverter.GetBytes(version));
                else
                    DownloadLater[dhtid] = version;
            }
        }

        private void StartSearch(ulong key, uint version)
        {
            byte[] parameters = BitConverter.GetBytes(version);
            DhtSearch search = Core.OperationNet.Searches.Start(key, "Storage", ComponentID.Storage, parameters, new EndSearchHandler(EndSearch));

            if (search != null)
                search.TargetResults = 2;
        }

        void EndSearch(DhtSearch search)
        {
            foreach (SearchValue found in search.FoundValues)
                Store_Local(new DataReq(found.Sources, search.TargetID, ComponentID.Storage, found.Value));
        }

        List<byte[]> Search_Local(ulong key, byte[] parameters)
        {
            List<Byte[]> results = new List<byte[]>();

            uint minVersion = BitConverter.ToUInt32(parameters, 0);

            lock (StorageMap)
                if (StorageMap.ContainsKey(key))
                {
                    OpStorage storage = StorageMap[key];

                    if (storage.Header.Version >= minVersion)
                        results.Add(storage.SignedHeader);
                }

            return results;
        }

        void Network_Established()
        {
            ulong localBounds = Store.RecalcBounds(Core.LocalDhtID);

            // set bounds for objects
            foreach (OpStorage storage in StorageMap.Values)
            {
                storage.DhtBounds = Store.RecalcBounds(storage.DhtID);

                
                if (Utilities.InBounds(Core.LocalDhtID, localBounds, storage.DhtID))
                {
                    // republish objects that were not seen on the network during startup
                    if (storage.Unique)
                        Store.PublishNetwork(storage.DhtID, ComponentID.Storage, storage.SignedHeader);

                    // trigger download of files now in cache range
                    LoadHeaderFile(GetFilePath(storage.Header), storage, true, false);
                }
            }


            // only download those objects in our local area
            foreach (KeyValuePair<ulong, uint> pair in DownloadLater)
                if (Utilities.InBounds(Core.LocalDhtID, localBounds, pair.Key))
                    StartSearch(pair.Key, pair.Value);

            DownloadLater.Clear();


            // delete loose files not in map - do here because now files in cache range are marked as reffed
            foreach (string filepath in Directory.GetFiles(StoragePath + Path.DirectorySeparatorChar + "0"))
            {
                byte[] hash = Utilities.HextoBytes(Path.GetFileName(filepath));

                if (hash == null)
                {
                    File.Delete(filepath);
                    continue;
                }

                ICryptoTransform transform = LocalFileKey.CreateDecryptor(); //crit moved outside?
                byte[] id = transform.TransformFinalBlock(hash, 0, hash.Length);

                if (!FileMap.ContainsKey(BitConverter.ToUInt64(id, 0)))
                    File.Delete(filepath);
            }
        }

        internal void Research(ulong key)
        {
            if (!Network.Routing.Responsive())
                return;

            // limit re-search to once per 30 secs
            if (NextResearch.ContainsKey(key))
                if (Core.TimeNow < NextResearch[key])
                    return;

            uint version = 0;
            if (StorageMap.ContainsKey(key))
                version = StorageMap[key].Header.Version + 1;

            StartSearch(key, version);

            NextResearch[key] = Core.TimeNow.AddSeconds(30);
        }


        internal string GetFilePath(StorageHeader header)
        {
            return StoragePath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, header.KeyID, header.FileHash);
        }

        internal string GetFilePath(ulong hashID)
        {
            ICryptoTransform transform = LocalFileKey.CreateEncryptor();

            byte[] hash = BitConverter.GetBytes(hashID);

            return StoragePath + Path.DirectorySeparatorChar + "0" + Path.DirectorySeparatorChar + Utilities.BytestoHex(transform.TransformFinalBlock(hash, 0, hash.Length));
        }

        internal string GetWorkingPath(uint project)
        {
            return StoragePath + Path.DirectorySeparatorChar + "1" + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "working:" + project.ToString());
        }

        private void LoadHeaderFile(string path, OpStorage storage, bool reload, bool working)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                bool cached = Utilities.InBounds(storage.DhtID, storage.DhtBounds, Core.LocalDhtID);
                bool local = false;

                RijndaelManaged key = working ? LocalFileKey : storage.Header.FileKey; 

                FileStream filex = new FileStream(path, FileMode.Open);
                CryptoStream crypto = new CryptoStream(filex, key.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Protocol, FileAccess.Read);

                G2Header header = null;

                ulong currentUID = 0;

                while (stream.ReadPacket(ref header))
                {
                    if (!working && header.Name == StoragePacket.Root)
                    {
                        StorageRoot packet = StorageRoot.Decode(Protocol, header);

                        local = GetHigherRegion(storage.DhtID, packet.ProjectID).Contains(Core.LocalDhtID) ||
                            Links.GetDownlinkIDs(storage.DhtID, packet.ProjectID, 1).Contains(Core.LocalDhtID);
                    }

                    if (header.Name == StoragePacket.File)
                    {
                        StorageFile packet = StorageFile.Decode(Protocol, header);

                        if (packet == null)
                            continue;

                        bool historyFile = true;
                        if(packet.UID != currentUID)
                        {
                            historyFile = false;
                            currentUID = packet.UID;
                        }

                        if (!FileMap.ContainsKey(packet.HashID))
                            FileMap[packet.HashID] = new OpFile(packet);

                        OpFile file = FileMap[packet.HashID];
                        InternalFileMap[packet.InternalHashID] = file;

                        if(!reload)
                            file.References++;
                        
                        if (!working) // if one ref is public, then whole file is marked public
                            file.Working = false;

                        if (packet.HashID == 0 || packet.InternalHash == null)
                        {
                            Debug.Assert(false);
                            continue;
                        }

                        file.Downloaded = File.Exists(GetFilePath(packet.HashID));

                        if (!file.Downloaded)
                        {
                            // if in local range only store latest 
                            if (local && !historyFile)
                                DownloadFile(storage.DhtID, packet);

                            // if storage is in cache range, download all files
                            else if (Network.Established && cached)
                                DownloadFile(storage.DhtID, packet);
                        }

                        // on link update, if in local range, get latest files
                        // (handled by location update, when it sees a new version of storage component is available)                 
                    }
                }

                stream.Close();
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Storage", "Error loading files " + ex.Message);
            }
        }

        internal void DownloadFile(ulong id, StorageFile file)
        {
            // if file still processing return
            if (file.Hash == null)
                return;

            FileDetails details = new FileDetails(ComponentID.Storage, file.Hash, file.Size, BitConverter.GetBytes(StoragePacket.File));

            Core.Transfers.StartDownload(id, details, new object[] { file }, new EndDownloadHandler(EndDownloadFile));
        }

        private void EndDownloadFile(string path, object[] args)
        {
            StorageFile file = (StorageFile) args[0];

            try
            {
                File.Move(path, GetFilePath(file.HashID));
            }
            catch { return; }

            if (FileMap.ContainsKey(file.HashID))
                FileMap[file.HashID].Downloaded = true;

            // interface list box would be watching if file is transferring, will catch completed update
        }

        private void UnloadHeaderFile(string path, RijndaelManaged key)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                FileStream filex = new FileStream(path, FileMode.Open);
                CryptoStream crypto = new CryptoStream(filex, key.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Protocol, FileAccess.Read);

                G2Header header = null;

                while (stream.ReadPacket(ref header))
                {
                    if (header.Name == StoragePacket.File)
                    {
                        StorageFile packet = StorageFile.Decode(Protocol, header);

                        if (packet == null)
                            continue;

                        if (!FileMap.ContainsKey(packet.HashID))
                            continue;

                        FileMap[packet.HashID].DeRef();
                    }
                }

                stream.Close();
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Storage", "Error loading files " + ex.Message);
            }
        }

        internal void MarkforHash(LocalFile file, string path, uint project, string dir)
        {
            HashPack pack = new HashPack(file, path, project, dir);

            lock (HashQueue)
                if (!HashQueue.Contains(pack))
                 
                {
                    file.Info.Size = new FileInfo(path).Length; // set so we can get hash status

                    // will be reset if file data modified
                    /*file.Info.Size = 0;
                    file.Info.Hash = null;
                    file.Info.HashID = 0;
                    file.Info.FileKey = new RijndaelManaged();
                    file.Info.FileKey.IV = new byte[file.Info.FileKey.IV.Length]; // zero out

                    file.Info.InternalSize = 0;
                    file.Info.InternalHash = null;
                    file.Info.InternalHashID = 0;*/

                    HashQueue.Enqueue(pack);
                }

            lock (HashQueue)
                Monitor.Pulse(HashQueue);
        }

        const int HashBufferSize = 1024 * 16;
        byte[] HashBuffer = new byte[HashBufferSize];
        bool HashRetry = false;

        void HashThread()
        {
            while (HashQueue.Count > 0)
            {
                HashPack pack = null;

                lock (HashQueue)
                    if (HashQueue.Count > 0)
                        pack = HashQueue.Peek();

                if (pack == null)
                    continue;

                // three steps, hash file, encrypt file, hash encrypted file
                try
                {
                    OpFile file = null;
                    StorageFile info = pack.File.Info.Clone();


                    // remove old references from local file
                    if (!HashRetry)
                        if (FileMap.ContainsKey(pack.File.Info.HashID))
                            FileMap[pack.File.Info.HashID].DeRef(); //crit test

                    if (!File.Exists(pack.Path))
                    {
                        lock(HashQueue) HashQueue.Dequeue();
                        HashRetry = false;

                        continue;
                    }

                    // do internal hash
                    Utilities.ShaHashFile(pack.Path, ref info.InternalHash, ref info.InternalSize);
                    info.InternalHashID = BitConverter.ToUInt64(info.InternalHash, 0);

                    // if file exists in internal map, use key for that file
                    if (InternalFileMap.ContainsKey(info.InternalHashID))
                    {
                        file = InternalFileMap[info.InternalHashID];
                        file.References++;

                        info.Size = file.Size;
                        info.FileKey.Key = file.Key;

                        info.Hash = file.Hash;
                        info.HashID = file.HashID;

                        if (!Utilities.MemCompare(file.Hash, pack.File.Info.Hash))
                            ReviseFile(pack, info);

                        lock (HashQueue) HashQueue.Dequeue();
                        HashRetry = false;

                        continue;
                    }

                    // encrypt file to temp dir
                    string tempPath = Core.GetTempPath();
                    FileStream tempFile = new FileStream(tempPath, FileMode.CreateNew);
                    CryptoStream stream = new CryptoStream(tempFile, info.FileKey.CreateEncryptor(), CryptoStreamMode.Write);

                    FileStream localfile = new FileStream(pack.Path, FileMode.Open, FileAccess.Read);

                    int read = HashBufferSize;
                    while (read == HashBufferSize)
                    {
                        read = localfile.Read(HashBuffer, 0, HashBufferSize);
                        stream.Write(HashBuffer, 0, read);
                    }

                    localfile.Close();
                    stream.FlushFinalBlock();
                    stream.Close();

                    // hash temp file
                    Utilities.ShaHashFile(tempPath, ref info.Hash, ref info.Size);
                    info.HashID = BitConverter.ToUInt64(info.Hash, 0);

                    // move to official path
                    string path = GetFilePath(info.HashID);
                    if (!File.Exists(path))
                        File.Move(tempPath, path);

                    // insert into file map - create new because internal for hash above was not in map already
                    file = new OpFile(info);
                    file.References++;
                    FileMap[info.HashID] = file;
                    InternalFileMap[info.InternalHashID] = file;

                    // if hash is different than previous mark as modified
                    if (!Utilities.MemCompare(file.Hash, pack.File.Info.Hash))
                        ReviseFile(pack, info);


                    lock (HashQueue) HashQueue.Dequeue(); // try to hash until finished without exception (wait for access to file)
                    HashRetry = false; // make sure we only deref once per file
                }
                catch (Exception ex)
                {
                    HashRetry = true;

                    // rotate file to back of queue
                    lock (HashQueue)
                        if (HashQueue.Count > 1)
                            HashQueue.Enqueue(HashQueue.Dequeue());

                    Core.OperationNet.UpdateLog("Storage", "Hash thread: " + ex.Message);
                    continue; // file might not exist anymore, name changed, etc..
                }
            }
        }

        private void ReviseFile(HashPack pack, StorageFile info)
        {
            if (Working.ContainsKey(pack.Project))
                Working[pack.Project].ReadyChange(pack.File, info);

            CallFileUpdate(pack.Project, pack.Dir, info.UID, WorkingChange.Updated);
        }

        internal string GetRootPath(ulong user, uint project)
        {
            return Core.User.RootPath + Path.DirectorySeparatorChar + Links.ProjectNames[project] + " Storage" + Path.DirectorySeparatorChar + Links.GetName(user);
        }

        internal WorkingStorage Discard(uint project)
        {
            if (!Working.ContainsKey(project))
                return null;

            // LockAll() to prevent unlocked discarded changes from conflicting with previous versions of
            // files when they are unlocked again by the user
            List<LockError> errors = new List<LockError>();
            Working[project].LockAll(errors);
            Working.Remove(project);

            // call unload on working
            string path = GetWorkingPath(project);
            UnloadHeaderFile(path, LocalFileKey);

            // delete working file
            try { File.Delete(path); }
            catch { };
                 
            //loadworking
            Working[project] = new WorkingStorage(this, project);

            return Working[project];
        }

        internal bool FileExists(StorageFile file)
        {
            if (FileMap.ContainsKey(file.HashID) &&
                File.Exists(GetFilePath(file.HashID)))
                return true;

            return false;
        }

        internal string FileStatus(StorageFile file)
        {
            // returns null if file not being handled by transfer component

            return Core.Transfers.GetStatus(ComponentID.Storage, file.Hash, file.Size);
        }

        internal bool IsFileUnlocked(ulong dht, uint project, string path, StorageFile file, bool history)
        {
            string finalpath = GetRootPath(dht, project) + path;

            if (history)
                finalpath += Path.DirectorySeparatorChar + ".history" + Path.DirectorySeparatorChar + GetHistoryName(file);
            else
                finalpath += Path.DirectorySeparatorChar + file.Name;

            return File.Exists(finalpath);
        }

        internal bool IsHistoryUnlocked(ulong dht, uint project, string path, LinkedList<StorageItem> archived)
        {
            string finalpath = GetRootPath(dht, project) + path + Path.DirectorySeparatorChar + ".history" + Path.DirectorySeparatorChar;

            if(Directory.Exists(finalpath))
                foreach (StorageFile file in archived)
                    if (File.Exists(finalpath + GetHistoryName(file)))
                        return true;

            return false;
        }

        private string GetHistoryName(StorageFile file)
        {
            string name = file.Name;

            int pos = name.LastIndexOf('.');
            if (pos == -1)
                pos = name.Length;


            string tag = "unhashed";
            if(file.InternalHash != null)
                tag = Utilities.BytestoHex(file.InternalHash, 0, 3, false);

            name = name.Insert(pos, "-" + tag);

            return name;
        }


        internal string UnlockFile(ulong dht, uint project, string path, StorageFile file, bool history, List<LockError> errors)
        {
            // path needs to include name, because for things like history files name is diff than file.Info

            string finalpath = GetRootPath(dht, project) + path;

            finalpath += history ? Path.DirectorySeparatorChar + ".history" + Path.DirectorySeparatorChar : Path.DirectorySeparatorChar.ToString();

            if (!CreateFolder(finalpath, errors, false))
                return null;
            
            finalpath += history ? GetHistoryName(file) : file.Name;


            // file not in storage
            if(!FileMap.ContainsKey(file.HashID) || !File.Exists(GetFilePath(file.HashID)))
            {
                errors.Add(new LockError(finalpath, "", true, LockErrorType.Missing));
                return null;
            }

            // check if already unlocked
            if (File.Exists(finalpath) && file.IsFlagged(StorageFlags.Unlocked))
                return finalpath;

            // file already exists
            if(File.Exists(finalpath))
            {
               
                // ask user about local
                if (dht == Core.LocalDhtID)
                {
                    errors.Add(new LockError(finalpath, "", true, LockErrorType.Existing, file, history));

                    return null;
                }

                // overwrite remote
                else
                {
                    try
                    {
                        File.Delete(finalpath);
                    }
                    catch
                    {
                        // not an existing error, dont want to give user option to 'use' the old remote file
                        errors.Add(new LockError(finalpath, "", true, LockErrorType.Unexpected, file, history));
                        return null;
                    }
                }
            }


            // extract file
            try
            {
                string tempPath = Core.GetTempPath();
                FileStream tempFile = new FileStream(tempPath, FileMode.CreateNew);

                FileStream encFile = new FileStream(GetFilePath(file.HashID), FileMode.Open, FileAccess.Read);
                CryptoStream stream = new CryptoStream(encFile, file.FileKey.CreateDecryptor(), CryptoStreamMode.Read);

                int read = FileBufferSize;
                while (read == FileBufferSize)
                {
                    read = stream.Read(FileBuffer, 0, FileBufferSize);
                    tempFile.Write(FileBuffer, 0, read);
                }

                tempFile.Close();
                stream.Close();

                // move to official path
                File.Move(tempPath, finalpath);
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Storage", "UnlockFile: " + ex.Message);

                errors.Add(new LockError(finalpath, "", true, LockErrorType.Unexpected, file, history));
                return null;
            }
        

            file.SetFlag(StorageFlags.Unlocked);

            if (dht != Core.LocalDhtID)
            {
                //FileInfo info = new FileInfo(finalpath);
                //info.IsReadOnly = true;
            }

            // local
            else if (Working.ContainsKey(project) )
            {
                // let caller trigger event because certain ops unlock multiple files

                // set watch on root path
                Working[project].StartWatchers();
            }

            return finalpath;
        }

        internal void LockFileCompletely(ulong dht, uint project, string path, LinkedList<StorageItem> archived, List<LockError> errors)
        {
            if (archived.Count == 0)
                return;

            StorageFile main = (StorageFile) archived.First.Value;
            
            string dirpath = GetRootPath(dht, project) + path;

            // delete main file
            string finalpath = dirpath + Path.DirectorySeparatorChar + main.Name;

            if (File.Exists(finalpath))
                if (DeleteFile(finalpath, errors, false))
                    main.RemoveFlag(StorageFlags.Unlocked);

            // delete archived file
            finalpath = dirpath + Path.DirectorySeparatorChar + ".history" + Path.DirectorySeparatorChar;

            if (Directory.Exists(finalpath))
            {
                List<string> stillLocked = new List<string>();
            
                foreach (StorageFile file in archived)
                {
                    string historyPath = finalpath + GetHistoryName(file);

                    if (File.Exists(historyPath))
                        if (DeleteFile(historyPath, errors, false))
                            file.RemoveFlag(StorageFlags.Unlocked);
                        else
                            stillLocked.Add(historyPath);
                }

                // delete history folder
                DeleteFolder(finalpath, errors, stillLocked);
            }
        }


        internal bool DeleteFile(string path, List<LockError> errors, bool temp)
        {
            try
            {
                File.Delete(path);
            }
            catch(Exception ex)
            {
                errors.Add(new LockError(path, ex.Message, true, temp ? LockErrorType.Temp : LockErrorType.Blocked ));
                return false;
            }

            return true;
        }

        internal void DeleteFolder(string path, List<LockError> errors, List<string> stillLocked)
        {
            try
            {
                if (Directory.GetDirectories(path).Length > 0 || Directory.GetFiles(path).Length > 0)
                {
                    foreach (string directory in Directory.GetDirectories(path))
                        if (stillLocked != null && !stillLocked.Contains(directory))
                            errors.Add(new LockError(directory, "", false, LockErrorType.Temp));

                    foreach (string file in Directory.GetFiles(path))
                        if (stillLocked != null && !stillLocked.Contains(file))
                            errors.Add(new LockError(file, "", true, LockErrorType.Temp));
                }
                else
                {
                    foreach (WorkingStorage working in Working.Values)
                        if (path == working.RootPath)
                            working.StopWatchers();

                    Directory.Delete(path, true);
                }
            }
            catch (Exception ex)
            {
                errors.Add(new LockError(path, ex.Message, false,  LockErrorType.Blocked));
            }
        }

        internal bool CreateFolder(string path, List<LockError> errors, bool subs)
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                LockError error = new LockError(path, ex.Message, true, LockErrorType.Unexpected);
                error.Subs = subs;
                errors.Add(error);

                return false;
            }

            return true;
        }

        internal void LockFile(ulong dht, uint project, string path, StorageFile file, bool history)
        {
            string finalpath = GetRootPath(dht, project) + path;

            if (history)
                finalpath += Path.DirectorySeparatorChar + ".history" + Path.DirectorySeparatorChar + GetHistoryName(file);
            else
                finalpath += Path.DirectorySeparatorChar + file.Name;

            try
            {
                if (File.Exists(finalpath))
                    File.Delete(finalpath);

                file.RemoveFlag(StorageFlags.Unlocked);
            }
            catch { }
        }


        internal StorageActions ItemDiff(StorageItem item, StorageItem original)
        {
            StorageActions actions = StorageActions.None;

            if (original == null)
                return StorageActions.Created;

            if (item.Name != original.Name)
                actions = actions | StorageActions.Renamed;

            if (ScopeChanged(item.Scope, original.Scope))
                actions = actions | StorageActions.Scoped;

            if (item.IsFlagged(StorageFlags.Archived) && !original.IsFlagged(StorageFlags.Archived))
                actions = actions | StorageActions.Deleted;

            if (!item.IsFlagged(StorageFlags.Archived) && original.IsFlagged(StorageFlags.Archived))
                actions = actions | StorageActions.Restored;

            if (item.GetType() == typeof(StorageFile))
                if (!Utilities.MemCompare(((StorageFile)item).InternalHash, ((StorageFile)original).InternalHash))
                    actions = actions | StorageActions.Modified;


            return actions;
        }

        internal bool ScopeChanged(Dictionary<ulong, short> a, Dictionary<ulong, short> b)
        {
            if (a.Count != b.Count)
                return true;

            foreach (ulong id in a.Keys)
            {
                if (!b.ContainsKey(id))
                    return true;

                if (a[id] != b[id])
                    return true;
            }

            return false;
        }


        internal List<ulong> GetHigherRegion(ulong id, uint project)
        {
            List<ulong> highers = Links.GetUplinkIDs(id, project);

            highers.AddRange(Links.GetAdjacentIDs(id, project));

            highers.Remove(id); // remove target


            return highers;
        }

        internal bool HashingActive()
        {
            if (HashQueue.Count > 0 || 
                (HashThreadHandle != null && HashThreadHandle.IsAlive))
                return true;

            return false;
        }
    }

    internal class OpStorage
    {
        internal ulong DhtID;
        internal ulong DhtBounds = ulong.MaxValue;
        internal byte[] Key;    // make sure reference is the same as main key list
        internal bool Unique;

        internal StorageHeader Header;
        internal byte[] SignedHeader;


        internal OpStorage(byte[] key)
        {
            Key = key;
            DhtID = Utilities.KeytoID(key);
        }
    }

    internal class OpFile
    {
        internal long Size;
        internal ulong HashID;
        internal byte[] Key;
        internal byte[] Hash;
        internal int References;
        internal bool Working;
        internal bool Downloaded;

        internal OpFile(StorageFile file)
        {
            HashID = file.HashID;
            Hash = file.Hash;
            Size = file.Size;
            Key = file.FileKey.Key;
            Working = true;
        }

        internal void DeRef()
        {
            if (References > 0)
                References--;
        }
    }


    internal class HashPack
    {
        internal LocalFile File;
        internal string Path;
        internal string Dir;
        internal uint Project;
        

        internal HashPack(LocalFile file, string path, uint project, string dir)
        {
            File = file;
            Path = path;
            Project = project;
            Dir = dir;
        }

        public override bool Equals(object obj)
        {
            HashPack pack = obj as HashPack;

            if(obj == null)
                return false;

            return (string.Compare(Path, pack.Path, true) == 0);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
