using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using RiseOp.Services;
using RiseOp.Services.Location;
using RiseOp.Services.Transfer;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;


namespace RiseOp.Services.Assist
{
    internal delegate void FileAquiredHandler(OpVersionedFile file);
    internal delegate void FileRemovedHandler(OpVersionedFile file);


    class VersionedCache : IDisposable
    {
        OpCore Core;
        DhtNetwork Network;
        internal DhtStore Store;

        bool Loading;
        internal ushort Service;
        internal ushort DataType;

        RijndaelManaged LocalKey;
        string CachePath;

        internal ThreadedDictionary<ulong, OpVersionedFile> FileMap = new ThreadedDictionary<ulong, OpVersionedFile>();
       
        bool RunSaveHeaders;
        int PruneSize = 100;
        Dictionary<ulong, DateTime> NextResearch = new Dictionary<ulong, DateTime>();
        Dictionary<ulong, uint> DownloadLater = new Dictionary<ulong, uint>();


        internal FileAquiredHandler FileAquired;
        internal FileRemovedHandler FileRemoved;


        internal VersionedCache(DhtNetwork network, ushort service, ushort type)
        {
            Core    = network.Core;
            Network = network;
            Store   = network.Store;

            Service = service;
            DataType = type;

            CachePath = Core.User.RootPath + Path.DirectorySeparatorChar +
                        "Data" + Path.DirectorySeparatorChar +
                        Service.ToString() + Path.DirectorySeparatorChar +
                        DataType.ToString();

            Directory.CreateDirectory(CachePath);

            LocalKey = Core.User.Settings.FileKey;

            Core.SecondTimerEvent += new TimerHandler(Core_SecondTimer);
            Core.MinuteTimerEvent += new TimerHandler(Core_MinuteTimer);

            Network.EstablishedEvent += new EstablishedHandler(Network_Established);

            Store.StoreEvent[Service, DataType] += new StoreHandler(Store_Local);
            Store.ReplicateEvent[Service, DataType] += new ReplicateHandler(Store_Replicate);
            Store.PatchEvent[Service, DataType] += new PatchHandler(Store_Patch);

            Network.Searches.SearchEvent[Service, DataType] += new SearchRequestHandler(Search_Local);

            Core.Transfers.FileSearch[Service, DataType] += new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[Service, DataType] += new FileRequestHandler(Transfers_FileRequest);

            Core.Locations.GetTag[Service, DataType] += new GetTagHandler(Locations_GetTag);
            Core.Locations.TagReceived[Service, DataType] += new TagReceivedHandler(Locations_TagReceived);

            if (Core.Sim != null)
                PruneSize = 25;
        }

        public void Load()
        {
            Loading = true;

            LoadHeaders();

            Loading = false;
        }

        public void Dispose()
        {
            Core.SecondTimerEvent -= new TimerHandler(Core_SecondTimer);
            Core.MinuteTimerEvent -= new TimerHandler(Core_MinuteTimer);

            Network.EstablishedEvent -= new EstablishedHandler(Network_Established);

            Store.StoreEvent[Service, DataType] -= new StoreHandler(Store_Local);
            Store.ReplicateEvent[Service, DataType] -= new ReplicateHandler(Store_Replicate);
            Store.PatchEvent[Service, DataType] -= new PatchHandler(Store_Patch);

            Network.Searches.SearchEvent[Service, DataType] -= new SearchRequestHandler(Search_Local);

            Core.Transfers.FileSearch[Service, DataType] -= new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[Service, DataType] -= new FileRequestHandler(Transfers_FileRequest);

            Core.Locations.GetTag[Service, DataType] -= new GetTagHandler(Locations_GetTag);
            Core.Locations.TagReceived[Service, DataType] -= new TagReceivedHandler(Locations_TagReceived);

        }

        void Core_SecondTimer()
        {
            // clean download later map
            if (!Network.Established)
                Utilities.PruneMap(DownloadLater, Core.LocalDhtID, PruneSize);

            // save headers
            if (RunSaveHeaders)
                SaveHeaders();
        }
        
        void Core_MinuteTimer()
        {
            // prune
            List<ulong> removeIDs = new List<ulong>();

            if (FileMap.SafeCount > PruneSize)
                FileMap.LockReading(delegate()
                {
                    if (FileMap.Count > PruneSize)
                        foreach (OpVersionedFile vfile in FileMap.Values)
                            if (vfile.DhtID != Core.LocalDhtID &&
                                !Core.Focused.SafeContainsKey(vfile.DhtID) &&
                                !Utilities.InBounds(vfile.DhtID, vfile.DhtBounds, Core.LocalDhtID))
                                removeIDs.Add(vfile.DhtID);
                });

            if (removeIDs.Count > 0)
                FileMap.LockWriting(delegate()
                {
                    while (removeIDs.Count > 0 && FileMap.Count > PruneSize / 2)
                    {
                        ulong furthest = Core.LocalDhtID;
                        OpVersionedFile vfile = FileMap[furthest];

                        foreach (ulong id in removeIDs)
                            if ((id ^ Core.LocalDhtID) > (furthest ^ Core.LocalDhtID))
                                furthest = id;

                        FileRemoved.Invoke(vfile);

                        if (vfile.Header != null)
                            try { File.Delete(GetFilePath(vfile.Header)); }
                            catch { }

                        FileMap.Remove(furthest);
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
            FileMap.LockReading(delegate()
            {
                foreach (OpVersionedFile vfile in FileMap.Values)
                {
                    vfile.DhtBounds = Store.RecalcBounds(vfile.DhtID);

                    // republish objects that were not seen on the network during startup
                    if (vfile.Unique && Utilities.InBounds(Core.LocalDhtID, localBounds, vfile.DhtID))
                        Store.PublishNetwork(vfile.DhtID, Service, DataType, vfile.SignedHeader);
                }
            });

            // only download those objects in our local area
            foreach (KeyValuePair<ulong, uint> pair in DownloadLater)
                if (Utilities.InBounds(Core.LocalDhtID, localBounds, pair.Key))
                    StartSearch(pair.Key, pair.Value);

            DownloadLater.Clear();
        }

        internal void LoadHeaders()
        {
            try
            {
                string path = CachePath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalKey, "VersionedFileHeaders");

                if (!File.Exists(path))
                    return;

                FileStream file = new FileStream(path, FileMode.Open);
                CryptoStream crypto = new CryptoStream(file, LocalKey.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Core.Protocol, FileAccess.Read);

                G2Header root = null;

                while (stream.ReadPacket(ref root))
                    if (root.Name == DataPacket.SignedData)
                    {
                        SignedData signed = SignedData.Decode(Core.Protocol, root);
                        G2Header embedded = new G2Header(signed.Data);


                        // figure out data contained
                        if (Core.Protocol.ReadPacket(embedded))
                            if (embedded.Name == DataPacket.VersionedFile)
                                Process_VersionedFile(null, signed, VersionedFileHeader.Decode(Core.Protocol, embedded));
                    }

                stream.Close();
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("VersionedFile", "Error loading data " + ex.Message);
            }
        }

        internal void SaveHeaders()
        {
            RunSaveHeaders = false;

            try
            {
                string tempPath = Core.GetTempPath();
                FileStream file = new FileStream(tempPath, FileMode.Create);
                CryptoStream stream = new CryptoStream(file, LocalKey.CreateEncryptor(), CryptoStreamMode.Write);

                FileMap.LockReading(delegate()
                {
                    foreach (OpVersionedFile vfile in FileMap.Values)
                        if (vfile.SignedHeader != null)
                            stream.Write(vfile.SignedHeader, 0, vfile.SignedHeader.Length);
                });

                stream.FlushFinalBlock();
                stream.Close();


                string finalPath = CachePath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalKey, "VersionedFileHeaders");
                File.Delete(finalPath);
                File.Move(tempPath, finalPath);
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("VersionedFile", "Error saving data " + ex.Message);
            }
        }

        internal OpVersionedFile UpdateLocal(string tempPath, RijndaelManaged key, byte[] extra)
        {
            OpVersionedFile vfile = null;

            if (Core.InvokeRequired)
            {
                // block until completed
                Core.RunInCoreBlocked(delegate() { vfile = UpdateLocal(tempPath, key, extra); });
                return vfile;
            }

            vfile = GetFile(Core.LocalDhtID);
            VersionedFileHeader header = null;

            if (vfile != null)
                header = vfile.Header;

            string oldFile = null;

            if (header != null)
                oldFile = GetFilePath(header);
            else
                header = new VersionedFileHeader();


            header.Key = Core.User.Settings.KeyPublic;
            header.KeyID = Core.LocalDhtID; // set so keycheck works
            header.Version++;
            header.FileKey = key;
            header.Extra = extra;


            // finish building header
            Utilities.ShaHashFile(tempPath, ref header.FileHash, ref header.FileSize);

            // move file, overwrite if need be
            string finalPath = GetFilePath(header);
            File.Move(tempPath, finalPath);

            CacheFile(new SignedData(Core.Protocol, Core.User.Settings.KeyPair, header), header);

            SaveHeaders();

            if (oldFile != null && File.Exists(oldFile)) // delete after move to ensure a copy always exists (names different)
                try { File.Delete(oldFile); }
                catch { }

            // publish header
            vfile = GetFile(Core.LocalDhtID); // get newly loaded object

            if (vfile == null)
                return null;

            Store.PublishNetwork(Core.LocalDhtID, Service, DataType, vfile.SignedHeader);

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
                    if (data != null && data.Sources != null)
                        foreach (DhtAddress source in data.Sources)
                            Store.Send_StoreReq(source, data.LocalProxy, new DataReq(null, current.DhtID, Service, DataType, current.SignedHeader));

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

        internal OpVersionedFile GetFile(ulong id)
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
                string path = GetFilePath(header);
                if (!File.Exists(path))
                {
                    Download(signedHeader, header);
                    return;
                }

                // get file
                OpVersionedFile prevFile = GetFile(header.KeyID);

                OpVersionedFile newFile = new OpVersionedFile(header.Key);


                // set new header
                newFile.Header = header;
                newFile.SignedHeader = signedHeader.Encode(Core.Protocol);
                newFile.Unique = Loading;

                FileMap.SafeAdd(header.KeyID, newFile);

                RunSaveHeaders = true;

                FileAquired.Invoke(newFile);


                // delete old file - do after aquired event so invoked (storage) can perform clean up operation
                if (prevFile != null)
                {
                    if (header.Version < prevFile.Header.Version)
                        return; // dont update with older version

                    string oldPath = GetFilePath(prevFile.Header);
                    if (path != oldPath && File.Exists(oldPath))
                        try { File.Delete(oldPath); }
                        catch { }
                }
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("VersionedFile", "Error caching data " + ex.Message);
            }
        }


        internal string GetFilePath(VersionedFileHeader header)
        {
            return CachePath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalKey, header.KeyID, header.FileHash);
        }

        private void Download(SignedData signed, VersionedFileHeader header)
        {
            if (!Utilities.CheckSignedData(header.Key, signed.Data, signed.Signature))
                return;

            FileDetails details = new FileDetails(Service, DataType, header.FileHash, header.FileSize, null);

            Core.Transfers.StartDownload(header.KeyID, details, new object[] { signed, header }, new EndDownloadHandler(EndDownload));
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

        private void EndDownload(string path, object[] args)
        {
            SignedData signedHeader = (SignedData)args[0];
            VersionedFileHeader header = (VersionedFileHeader)args[1];

            try
            {
                File.Move(path, GetFilePath(header));
            }
            catch { return; }

            CacheFile(signedHeader, header);
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
                if (embedded.Name == DataPacket.VersionedFile)
                    Process_VersionedFile(null, signed, VersionedFileHeader.Decode(Core.Protocol, signed.Data));
        }

        const int PatchEntrySize = 12;

        ReplicateData Store_Replicate(DhtContact contact, bool add)
        {
            if (!Network.Established)
                return null;

            ReplicateData data = new ReplicateData(PatchEntrySize);

            byte[] patch = new byte[PatchEntrySize];

            FileMap.LockReading(delegate()
            {
                foreach (OpVersionedFile vfile in FileMap.Values)
                    if (Utilities.InBounds(vfile.DhtID, vfile.DhtBounds, contact.DhtID))
                    {
                        DhtContact target = contact;
                        vfile.DhtBounds = Store.RecalcBounds(vfile.DhtID, add, ref target);

                        if (target != null)
                        {
                            BitConverter.GetBytes(vfile.DhtID).CopyTo(patch, 0);
                            BitConverter.GetBytes(vfile.Header.Version).CopyTo(patch, 8);

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

                OpVersionedFile vfile = GetFile(dhtid);

                if (vfile != null && vfile.Header != null)
                {
                    if (vfile.Header.Version > version)
                    {
                        Store.Send_StoreReq(source, 0, new DataReq(null, vfile.DhtID, Service, DataType, vfile.SignedHeader));
                        continue;
                    }

                    vfile.Unique = false; // network has current or newer version

                    if (vfile.Header.Version == version)
                        continue;

                    // else our version is old, download below
                }


                if (Network.Established)
                    Network.Searches.SendDirectRequest(source, dhtid, Service, DataType, BitConverter.GetBytes(version));
                else
                    DownloadLater[dhtid] = version;
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
            OpVersionedFile file = GetFile(key);
            if (file != null)
                version = file.Header.Version + 1;

            StartSearch(key, version);

            NextResearch[key] = Core.TimeNow.AddSeconds(30);
        }

        private void StartSearch(ulong key, uint version)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { StartSearch(key, version); });
                return;
            }

            byte[] parameters = BitConverter.GetBytes(version);
            DhtSearch search = Core.OperationNet.Searches.Start(key, Core.GetServiceName(Service), Service, DataType, parameters, new EndSearchHandler(EndSearch));

            if (search != null)
                search.TargetResults = 2;
        }

        void Search_Local(ulong key, byte[] parameters, List<byte[]> results)
        {
            uint minVersion = BitConverter.ToUInt32(parameters, 0);

            OpVersionedFile vfile = GetFile(key);

            if (vfile != null)
                if (vfile.Header.Version >= minVersion)
                    results.Add(vfile.SignedHeader);
        }

        void EndSearch(DhtSearch search)
        {
            foreach (SearchValue found in search.FoundValues)
                Store_Local(new DataReq(found.Sources, search.TargetID, Service, DataType, found.Value));
        }

        byte[] Locations_GetTag()
        {
            OpVersionedFile file = GetFile(Core.LocalDhtID);

            return (file != null) ? BitConverter.GetBytes(file.Header.Version) : null;
        }

        void Locations_TagReceived(ulong user, byte[] tag)
        {
            if(tag.Length < 4)
                return;

            OpVersionedFile file = GetFile(user);

            if (file != null)
            {
                uint version = BitConverter.ToUInt32(tag, 0);

                if (version > file.Header.Version)
                    foreach (ClientInfo client in Core.Locations.GetClients(user))
                        if (client.Active)
                            Network.Searches.SendDirectRequest(new DhtAddress(client.Data.IP, client.Data.Source), user, Service, DataType, BitConverter.GetBytes(version));
            }
        }
    }

    internal class OpVersionedFile
    {
        internal ulong    DhtID;
        internal ulong    DhtBounds = ulong.MaxValue;
        internal byte[]   Key;    // make sure reference is the same as main key list (saves memory)
        internal bool     Unique;

        internal VersionedFileHeader Header;
        internal byte[] SignedHeader;

        internal OpVersionedFile(byte[] key)
        {
            Key = key;
            DhtID = Utilities.KeytoID(key);
        }
    }

    internal class VersionedFileHeader : G2Packet
    {
        const byte Packet_Key = 0x10;
        const byte Packet_Version = 0x20;
        const byte Packet_FileHash = 0x30;
        const byte Packet_FileSize = 0x40;
        const byte Packet_FileKey = 0x50;
        const byte Packet_Extra = 0x60;


        internal byte[] Key;
        internal uint Version;
        internal byte[] FileHash;
        internal long FileSize;
        internal RijndaelManaged FileKey = new RijndaelManaged();
        internal byte[] Extra;
        
        internal ulong KeyID;


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame header = protocol.WritePacket(null, DataPacket.VersionedFile, null);

                protocol.WritePacket(header, Packet_Key, Key);
                protocol.WritePacket(header, Packet_Version, BitConverter.GetBytes(Version));
                protocol.WritePacket(header, Packet_FileHash, FileHash);
                protocol.WritePacket(header, Packet_FileSize, BitConverter.GetBytes(FileSize));
                protocol.WritePacket(header, Packet_FileKey, FileKey.Key);
                protocol.WritePacket(header, Packet_Extra, Extra);

                return protocol.WriteFinish();
            }
        }

        internal static VersionedFileHeader Decode(G2Protocol protocol, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!protocol.ReadPacket(root))
                return null;

            if (root.Name != DataPacket.VersionedFile)
                return null;

            return VersionedFileHeader.Decode(protocol, root);
        }

        internal static VersionedFileHeader Decode(G2Protocol protocol, G2Header root)
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
                        header.Version = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_FileHash:
                        header.FileHash = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_FileSize:
                        header.FileSize = BitConverter.ToInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_FileKey:
                        header.FileKey.Key = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        header.FileKey.IV = new byte[header.FileKey.IV.Length];
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
