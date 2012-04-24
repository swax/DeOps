using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using DeOps.Services;
using DeOps.Services.Location;
using DeOps.Services.Transfer;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;


namespace DeOps.Services.Assist
{
    public delegate void FileAquiredHandler(OpVersionedFile file);
    public delegate void FileRemovedHandler(OpVersionedFile file);

    // A versioned cache is used for a single file you want to keep cached around the local user's ID
    // So it is persistant on the network.  Newer versions of files replace older versions.
    // LocalSync is used to provide simultaneous replication of all versioned files

    // on Global IM local sync is cached by your buddies, not DHT area

    public class VersionedCache : IDisposable
    {
        OpCore Core;
        DhtNetwork Network;
        public DhtStore Store;

        public uint Service;
        public uint DataType;

        byte[] LocalKey;
        string CachePath;
        string HeaderPath;

        public ThreadedDictionary<ulong, OpVersionedFile> FileMap = new ThreadedDictionary<ulong, OpVersionedFile>();

        bool LocalSync;
        bool GlobalIM;

        bool RunSaveHeaders;
        int PruneSize = 64;
        Dictionary<ulong, DateTime> NextResearch = new Dictionary<ulong, DateTime>();
        Dictionary<ulong, uint> DownloadLater = new Dictionary<ulong, uint>();


        public FileAquiredHandler FileAquired;
        public FileRemovedHandler FileRemoved;


        public VersionedCache(DhtNetwork network, uint service, uint type, bool localSync)
        {
            Core    = network.Core;
            Network = network;
            Store   = network.Store;

            Service = service;
            DataType = type;

            LocalSync = localSync;
            GlobalIM = Core.User.Settings.GlobalIM;

            CachePath = Core.User.RootPath + Path.DirectorySeparatorChar +
                        "Data" + Path.DirectorySeparatorChar +
                        Service.ToString() + Path.DirectorySeparatorChar +
                        DataType.ToString();

            HeaderPath = CachePath + Path.DirectorySeparatorChar + Utilities.CryptFilename(Core, "VersionedFileHeaders");
                

            Directory.CreateDirectory(CachePath);

            LocalKey = Core.User.Settings.FileKey;

            Core.SecondTimerEvent += Core_SecondTimer;
            Core.MinuteTimerEvent += Core_MinuteTimer;

            Network.CoreStatusChange += new StatusChange(Network_StatusChange);
 
            Store.StoreEvent[Service, DataType] += new StoreHandler(Store_Local);

            // if local sync used then it will handle all replication
            if (LocalSync && !GlobalIM)
            {
                Store.ReplicateEvent[Service, DataType] += new ReplicateHandler(Store_Replicate);
                Store.PatchEvent[Service, DataType] += new PatchHandler(Store_Patch);
            }

            Network.Searches.SearchEvent[Service, DataType] += new SearchRequestHandler(Search_Local);

            Core.Transfers.FileSearch[Service, DataType] += new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[Service, DataType] += new FileRequestHandler(Transfers_FileRequest);

            if (!LocalSync)
            {
                Core.Sync.GetTag[Service, DataType] += new GetLocalSyncTagHandler(LocalSync_GetTag);
                Core.Sync.TagReceived[Service, DataType] += new LocalSyncTagReceivedHandler(LocalSync_TagReceived);
            }

            if (Core.Sim != null)
                PruneSize = 16;
        }

        public void Load()
        {
            LoadHeaders();
        }

        public void Dispose()
        {
            Core.SecondTimerEvent -= Core_SecondTimer;
            Core.MinuteTimerEvent -= Core_MinuteTimer;

            Network.CoreStatusChange -= new StatusChange(Network_StatusChange);

            Store.StoreEvent[Service, DataType] -= new StoreHandler(Store_Local);

            if (LocalSync && !GlobalIM)
            {
                Store.ReplicateEvent[Service, DataType] -= new ReplicateHandler(Store_Replicate);
                Store.PatchEvent[Service, DataType] -= new PatchHandler(Store_Patch);
            }

            Network.Searches.SearchEvent[Service, DataType] -= new SearchRequestHandler(Search_Local);

            Core.Transfers.FileSearch[Service, DataType] -= new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[Service, DataType] -= new FileRequestHandler(Transfers_FileRequest);

            if (!LocalSync)
            {
                Core.Sync.GetTag[Service, DataType] -= new GetLocalSyncTagHandler(LocalSync_GetTag);
                Core.Sync.TagReceived[Service, DataType] -= new LocalSyncTagReceivedHandler(LocalSync_TagReceived);
            }
        }

        void Core_SecondTimer()
        {
            // clean download later map
            if (!Network.Established)
                Utilities.PruneMap(DownloadLater, Core.UserID, PruneSize);

            // save headers
            if (RunSaveHeaders)
                SaveHeaders();

            // clean research map every 10 seconds
            if (Core.TimeNow.Second % 9 == 0)
            {
                foreach (ulong user in NextResearch.Keys.Where(u => Core.TimeNow > NextResearch[u]).ToArray())
                    NextResearch.Remove(user);
            }
        }

        void Core_MinuteTimer()
        {
            // prune
            List<OpVersionedFile> remove = null;

            if (FileMap.SafeCount > PruneSize)
                FileMap.LockReading(delegate()
                {
                    // local cache region is in KeepData and consists of the closest dht nodes that are part of our
                    // same trust hierarchy (so that we don't cache or flood the network with old data of people who've left)
                    remove = (from file in FileMap.Values
                              where !Core.KeepData.SafeContainsKey(file.UserID)
                              orderby file.UserID ^ Core.UserID descending
                              select file).Take(FileMap.Count - PruneSize).ToList();
                                      
                });

            if (remove == null)
                return;

            foreach (OpVersionedFile file in remove)
            {
                if (FileRemoved != null)
                    FileRemoved.Invoke(file);

                if (file.Header != null && file.Header.FileHash != null) // local sync doesnt use files
                    try { File.Delete(GetFilePath(file.Header)); }
                    catch { }

                FileMap.SafeRemove(file.UserID);

                RunSaveHeaders = true;
            }
        }

        void Network_StatusChange()
        {
            if (Network.Established)
            {
                // republish objects that were not seen on the network during startup
                // only if local sync doesnt do this for us
                if (LocalSync && !GlobalIM)
                    FileMap.LockReading(delegate()
                    {
                        foreach (OpVersionedFile vfile in FileMap.Values)
                            if (vfile.Unique && Network.Routing.InCacheArea(vfile.UserID))
                                Store.PublishNetwork(vfile.UserID, Service, DataType, vfile.SignedHeader);
                    });

                // only download those objects in our local area
                foreach (KeyValuePair<ulong, uint> pair in DownloadLater)
                    if (Network.Routing.InCacheArea(pair.Key))
                        StartSearch(pair.Key, pair.Value);

                DownloadLater.Clear();
            }

            // disconnected, reset cache to unique
            else if(!Network.Responsive)
            {
                FileMap.LockReading(delegate()
                {
                    foreach (OpVersionedFile vfile in FileMap.Values)
                        vfile.Unique = true;
                });
            }
        }

        public void LoadHeaders()
        {
            List<string> goodPaths = new List<string>();

            try
            {
                goodPaths.Add(HeaderPath);

                if (!File.Exists(HeaderPath))
                    return;

                using (IVCryptoStream crypto = IVCryptoStream.Load(HeaderPath, LocalKey))
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
                                if (embedded.Name == DataPacket.VersionedFile)
                                {
                                    VersionedFileHeader header = VersionedFileHeader.Decode(embedded);
                                    
                                    if(header.FileHash != null)
                                        goodPaths.Add(GetFilePath(header));
                                    
                                    Process_VersionedFile(null, signed, header);
                                }
                        }
                }

                // remove loose files
                foreach (string testPath in Directory.GetFiles(CachePath))
                    if (!goodPaths.Contains(testPath))
                        try { File.Delete(testPath); }
                        catch { }
            }
            catch (Exception ex)
            {
                Core.Network.UpdateLog("VersionedFile", "Error loading data " + ex.Message);
            }
        }

        public void SaveHeaders()
        {
            RunSaveHeaders = false;

            try
            {
                string tempPath = Core.GetTempPath();
                using (IVCryptoStream stream = IVCryptoStream.Save(tempPath, LocalKey))
                {
                    FileMap.LockReading(delegate()
                    {
                        foreach (OpVersionedFile vfile in FileMap.Values)
                            if (vfile.SignedHeader != null)
                                stream.Write(vfile.SignedHeader, 0, vfile.SignedHeader.Length);
                    });

                    stream.FlushFinalBlock();
                }

                File.Copy(tempPath, HeaderPath, true);
                File.Delete(tempPath);
            }
            catch (Exception ex)
            {
                Core.Network.UpdateLog("VersionedFile", "Error saving data " + ex.Message);
            }
        }

        public OpVersionedFile UpdateLocal(string tempPath, byte[] key, byte[] extra)
        {
            OpVersionedFile vfile = null;

            if (Core.InvokeRequired)
            {
                // block until completed
                Core.RunInCoreBlocked(delegate() { vfile = UpdateLocal(tempPath, key, extra); });
                return vfile;
            }

            vfile = GetFile(Core.UserID);

            VersionedFileHeader header = null;
            if (vfile != null)
                header = vfile.Header;

            string oldFile = null;
            if (header != null && header.FileHash != null)
                oldFile = GetFilePath(header);

            if (header == null)
                header = new VersionedFileHeader();


            header.Key = Core.User.Settings.KeyPublic;
            header.KeyID = Core.UserID; // set so keycheck works
            header.Version++;
            header.FileKey = key;
            header.Extra = extra;


            // finish building header
            if (key != null)
            {
                Utilities.HashTagFile(tempPath, Network.Protocol, ref header.FileHash, ref header.FileSize);

                // move file, overwrite if need be
                string finalPath = GetFilePath(header);
                File.Move(tempPath, finalPath);
            }

            CacheFile(new SignedData(Network.Protocol, Core.User.Settings.KeyPair, header), header);

            SaveHeaders();

            if (oldFile != null && File.Exists(oldFile)) // delete after move to ensure a copy always exists (names different)
                try { File.Delete(oldFile); }
                catch { }

            vfile = GetFile(Core.UserID);

            if (!LocalSync)
                Core.Sync.UpdateLocal(); // calls this same function for local sync which publishes

            else if (GlobalIM)
                Core.Locations.UpdateLocation();

            else if (Network.Established)
            {
                Store.PublishNetwork(Core.UserID, Service, DataType, vfile.SignedHeader);
                Core.Locations.UpdateLocation();
            }

            else
                vfile.Unique = true; // publish when connected

            return vfile;
        }

        private void Process_VersionedFile(DataReq data, SignedData signed, VersionedFileHeader header)
        {
            Core.IndexKey(header.KeyID, ref header.Key);

            OpVersionedFile current = GetFile(header.KeyID);

            // if link loaded
            if (current != null)
            {
                // lower version
                if (header.Version < current.Header.Version)
                {
                    if (data != null && data.Source != null)
                        Store.Send_StoreReq(data.Source, data.LocalProxy, new DataReq(null, current.UserID, Service, DataType, current.SignedHeader));

                    return;
                }

                // higher version
                else if (header.Version > current.Header.Version)
                {
                    CacheFile(signed, header);
                }
            }

            // else load file, set new header after file loaded
            else
                CacheFile(signed, header);
        }

        public OpVersionedFile GetFile(ulong id)
        {
            OpVersionedFile vfile = null;

            FileMap.SafeTryGetValue(id, out vfile);

            return vfile;
        }

        private void CacheFile(SignedData signedHeader, VersionedFileHeader header)
        {
            if (Core.InvokeRequired)
                Debug.Assert(false);

            try
            {
                // check if file exists           
                string path = "";
                if (header.FileHash != null)
                {
                    path = GetFilePath(header);
                    if (!File.Exists(path))
                    {
                        Download(signedHeader, header);
                        return;
                    }
                }

                // get file
                OpVersionedFile prevFile = GetFile(header.KeyID);

                if (prevFile != null)
                    if (header.Version < prevFile.Header.Version)
                        return; // dont update with older version

                OpVersionedFile newFile = new OpVersionedFile(header.Key);


                // set new header
                newFile.Header = header;
                newFile.SignedHeader = signedHeader.Encode(Network.Protocol);
                newFile.Unique = !Network.Established;

                FileMap.SafeAdd(header.KeyID, newFile);

                RunSaveHeaders = true;

                if(FileAquired != null)
                    FileAquired.Invoke(newFile);


                // delete old file - do after aquired event so invoked (storage) can perform clean up operation
                if (prevFile != null && prevFile.Header.FileHash != null)
                {
                    string oldPath = GetFilePath(prevFile.Header);
                    if (path != oldPath && File.Exists(oldPath))
                        try { File.Delete(oldPath); }
                        catch { }
                }
            }
            catch (Exception ex)
            {
                Core.Network.UpdateLog("VersionedFile", "Error caching data " + ex.Message);
            }
        }


        public string GetFilePath(VersionedFileHeader header)
        {
            return CachePath + Path.DirectorySeparatorChar + Utilities.CryptFilename(Core, header.KeyID, header.FileHash);
        }

        private void Download(SignedData signed, VersionedFileHeader header)
        {
            if (!Utilities.CheckSignedData(header.Key, signed.Data, signed.Signature))
            {
                Debug.Assert(false);
                return;
            }

            FileDetails details = new FileDetails(Service, DataType, header.FileHash, header.FileSize, null);

            Core.Transfers.StartDownload(header.KeyID, details, GetFilePath(header), new EndDownloadHandler(EndDownload), new object[] { signed, header } );
        }

        bool Transfers_FileSearch(ulong key, FileDetails details)
        {
            OpVersionedFile vfile = GetFile(key);

            if (vfile != null)
                if (details.Size == vfile.Header.FileSize && Utilities.MemCompare(details.Hash, vfile.Header.FileHash))
                    return true;

            return false;
        }

        string Transfers_FileRequest(ulong key, FileDetails details)
        {
            OpVersionedFile vfile = GetFile(key);

            if (vfile != null)
                if (details.Size == vfile.Header.FileSize && Utilities.MemCompare(details.Hash, vfile.Header.FileHash))
                    return GetFilePath(vfile.Header);


            return null;
        }

        private void EndDownload(object[] args)
        {
            SignedData signedHeader = args[0] as SignedData;
            VersionedFileHeader header = args[1] as VersionedFileHeader;

            CacheFile(signedHeader, header);
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
                if (embedded.Name == DataPacket.VersionedFile)
                    Process_VersionedFile(store, signed, VersionedFileHeader.Decode(signed.Data));
        }

        List<byte[]> Store_Replicate(DhtContact contact)
        {
            List<byte[]> patches = new List<byte[]>();

            FileMap.LockReading(delegate()
            {
                foreach (OpVersionedFile vfile in FileMap.Values)
                    if (Network.Routing.InCacheArea(vfile.UserID))
                    {

                        byte[] id = BitConverter.GetBytes(vfile.UserID);
                        byte[] ver = CompactNum.GetBytes(vfile.Header.Version);

                        byte[] patch = new byte[id.Length + ver.Length];
                        id.CopyTo(patch, 0);
                        ver.CopyTo(patch, 8);

                        patches.Add(patch);
                    }
            });

            return patches;
        }

        void Store_Patch(DhtAddress source, byte[] data)
        {
            if (data.Length < 9)
                return;

            ulong user = BitConverter.ToUInt64(data, 0);

            if (!Network.Routing.InCacheArea(user))
                return;

            uint version = CompactNum.ToUInt32(data, 8, data.Length - 8);

            OpVersionedFile vfile = GetFile(user);

            if (vfile != null && vfile.Header != null)
            {
                if (vfile.Header.Version > version)
                {
                    Store.Send_StoreReq(source, null, new DataReq(null, vfile.UserID, Service, DataType, vfile.SignedHeader));
                    return;
                }

                
                vfile.Unique = false; // network has current or newer version

                if (vfile.Header.Version == version)
                    return;

                // else our version is old, download below
            }


            if (Network.Established)
                Network.Searches.SendDirectRequest(source, user, Service, DataType, BitConverter.GetBytes(version));
            else
                DownloadLater[user] = version;
        }

        public void Research(ulong key)
        {
            if (!Network.Responsive)
                return;

            // limit re-search to once per 30 secs
            DateTime timeout = default(DateTime);

            if (NextResearch.TryGetValue(key, out timeout))
                if (Core.TimeNow < timeout)
                    return;

            uint version = 0;
            OpVersionedFile file = GetFile(key);
            if (file != null)
                version = file.Header.Version + 1;

            StartSearch(key, version);

            NextResearch[key] = Core.TimeNow.AddSeconds(3);
        }

        private void StartSearch(ulong user, uint version)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { StartSearch(user, version); });
                return;
            }

            byte[] parameters = BitConverter.GetBytes(version);
            DhtSearch search = Core.Network.Searches.Start(user, Core.GetServiceName(Service), Service, DataType, parameters, Search_Found);

            if (search != null)
                search.TargetResults = 2;


            // node is in our local cache area, so not flooding by directly connecting
            if ( (!GlobalIM && Network.Routing.InCacheArea(user)) || 
                 ( GlobalIM && Core.Buddies.BuddyList.SafeContainsKey(user)) )
                foreach (ClientInfo client in Core.Locations.GetClients(user))
                    if (client.Data.TunnelClient == null)
                    {
                        Network.Searches.SendDirectRequest(new DhtAddress(client.Data.IP, client.Data.Source), user, Service, DataType, BitConverter.GetBytes(version));
                    }
                    else
                    {
                        foreach (DhtAddress server in client.Data.TunnelServers)
                        {
                            DhtContact contact = new DhtContact(client.Data.Source, client.Data.IP, client.Data.TunnelClient, server);
                            Network.Searches.SendDirectRequest(contact, user, Service, DataType, BitConverter.GetBytes(version));
                        }
                    }
        }

        void Search_Local(ulong key, byte[] parameters, List<byte[]> results)
        {
            uint minVersion = BitConverter.ToUInt32(parameters, 0);

            OpVersionedFile vfile = GetFile(key);

            if (vfile != null)
                if (vfile.Header.Version >= minVersion)
                    results.Add(vfile.SignedHeader);
        }

        void Search_Found(DhtSearch search, DhtAddress source, byte[] data)
        {
            Store_Local(new DataReq(source, search.TargetID, Service, DataType, data));
        }

        byte[] LocalSync_GetTag()
        {
            OpVersionedFile file = GetFile(Core.UserID);

            return (file != null) ? CompactNum.GetBytes(file.Header.Version) : null;
        }

        void LocalSync_TagReceived(ulong user, byte[] tag)
        {
            if(tag.Length == 0)
                return;

            uint version = 0;

            OpVersionedFile file = GetFile(user);

            if (file != null)
            {
                version = CompactNum.ToUInt32(tag, 0, tag.Length);

                // version old, so we need the latest localSync file
                // wont cause loop because localsync's fileAquired will only fire on new version of localSync
                if (version < file.Header.Version)
                    Core.Sync.Research(user);
            }

            // if newer file on network, or this node is in our cache area, find it
            if ((file != null && version > file.Header.Version) ||
                
                (file == null && ( ( !GlobalIM && Network.Routing.InCacheArea(user)) ||
                                   (  GlobalIM && Core.Buddies.BuddyList.SafeContainsKey(user)) )))
            {
                StartSearch(user, version); // this could be called from a patch given to another user, direct connect not gauranteed
            }
        }
    }

    public class OpVersionedFile
    {
        public ulong    UserID;
        public byte[]   Key;    // make sure reference is the same as main key list (saves memory)
        public bool     Unique;

        public VersionedFileHeader Header;
        public byte[] SignedHeader;

        public OpVersionedFile(byte[] key)
        {
            Key = key;
            UserID = Utilities.KeytoID(key);
        }
    }

    public class VersionedFileHeader : G2Packet
    {
        const byte Packet_Key = 0x10;
        const byte Packet_Version = 0x20;
        const byte Packet_FileHash = 0x30;
        const byte Packet_FileSize = 0x40;
        const byte Packet_FileKey = 0x50;
        const byte Packet_Extra = 0x60;


        public byte[] Key;
        public uint Version;
        public byte[] FileHash;
        public long FileSize;
        public byte[] FileKey;
        public byte[] Extra;
        
        public ulong KeyID;


        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame header = protocol.WritePacket(null, DataPacket.VersionedFile, null);

                protocol.WritePacket(header, Packet_Key, Key);
                protocol.WritePacket(header, Packet_Version, CompactNum.GetBytes(Version));

                if (FileHash != null)
                {
                    protocol.WritePacket(header, Packet_FileHash, FileHash);
                    protocol.WritePacket(header, Packet_FileSize, CompactNum.GetBytes(FileSize));
                    protocol.WritePacket(header, Packet_FileKey, FileKey);
                }

                protocol.WritePacket(header, Packet_Extra, Extra);

                return protocol.WriteFinish();
            }
        }

        public static VersionedFileHeader Decode(byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!G2Protocol.ReadPacket(root))
                return null;

            if (root.Name != DataPacket.VersionedFile)
                return null;

            return VersionedFileHeader.Decode(root);
        }

        public static VersionedFileHeader Decode(G2Header root)
        {
            VersionedFileHeader header = new VersionedFileHeader();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Key:
                        header.Key = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        header.KeyID = Utilities.KeytoID(header.Key);
                        break;

                    case Packet_Version:
                        header.Version = CompactNum.ToUInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_FileHash:
                        header.FileHash = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_FileSize:
                        header.FileSize = CompactNum.ToInt64(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_FileKey:
                        header.FileKey = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Extra:
                        header.Extra = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return header;
        }
    }
}
