using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;

using DeOps.Services.Assist;
using DeOps.Services.Location;

namespace DeOps.Services.Assist
{
    internal delegate byte[] GetLocalSyncTagHandler();
    internal delegate void LocalSyncTagReceivedHandler(ulong user, byte[] tag);

    // gives any service the ability to store a little piece of data on the network for anything
    // usually its version number purpose of this is to ease the burdon of patching the local area
    // as services increase, patch size remains constant
    

    class LocalSync : OpService
    {
        public string Name { get { return "LocalSync"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.LocalSync; } }

        const uint DataTypeSync = 0x01;


        OpCore Core;
        DhtNetwork Network;
        DhtStore Store;

        internal VersionedCache Cache;

        bool GlobalIM;

        internal Dictionary<ulong, ServiceData> InRange = new Dictionary<ulong, ServiceData>();
        internal Dictionary<ulong, ServiceData> OutofRange = new Dictionary<ulong, ServiceData>();

        internal ServiceEvent<GetLocalSyncTagHandler> GetTag = new ServiceEvent<GetLocalSyncTagHandler>();
        internal ServiceEvent<LocalSyncTagReceivedHandler> TagReceived = new ServiceEvent<LocalSyncTagReceivedHandler>();

       
      
        internal LocalSync(OpCore core)
        {
            Core = core;
            Network = core.Network;
            Store = Network.Store;
            Core.Sync = this;

            GlobalIM = Core.User.Settings.GlobalIM;

            Network.CoreStatusChange += new StatusChange(Network_StatusChange);

            Core.Locations.GetTag[ServiceID, DataTypeSync] += new GetLocationTagHandler(Locations_GetTag);
            Core.Locations.TagReceived[ServiceID, DataTypeSync] += new LocationTagReceivedHandler(Locations_TagReceived);

            Store.ReplicateEvent[ServiceID, DataTypeSync] += new ReplicateHandler(Store_Replicate);

            Cache = new VersionedCache(Network, ServiceID, DataTypeSync, true);
            Cache.FileAquired += new FileAquiredHandler(Cache_FileAquired);
            Cache.FileRemoved += new FileRemovedHandler(Cache_FileRemoved);
            Cache.Load();

            // if sync file for ourselves does not exist create
            if (!Cache.FileMap.SafeContainsKey(Core.UserID))
                UpdateLocal();
        }

        public void Dispose()
        {
            Core.Locations.GetTag[ServiceID, DataTypeSync] -= new GetLocationTagHandler(Locations_GetTag);
            Core.Locations.TagReceived[ServiceID, DataTypeSync] -= new LocationTagReceivedHandler(Locations_TagReceived);

            Store.ReplicateEvent[ServiceID, DataTypeSync] -= new ReplicateHandler(Store_Replicate);

            Cache.FileAquired -= new FileAquiredHandler(Cache_FileAquired);
            Cache.FileRemoved -= new FileRemovedHandler(Cache_FileRemoved);
            Cache.Dispose();
        }

        void Network_StatusChange()
        {
            // check that we have the latest version of local sync'd files in range
            if (Network.Established)
                foreach (ulong user in InRange.Keys)
                    InvokeTags(user, InRange[user]);
        }

        internal void UpdateLocal()
        {
            ServiceData data = new ServiceData();
            data.Date = Core.TimeNow.ToUniversalTime();

            foreach (uint service in GetTag.HandlerMap.Keys)
                foreach (uint datatype in GetTag.HandlerMap[service].Keys)
                {
                    PatchTag tag = new PatchTag();

                    tag.Service = service;
                    tag.DataType = datatype;
                    tag.Tag = GetTag[service, datatype].Invoke();

                    if (tag.Tag != null)
                    {
                        Debug.Assert(tag.Tag.Length < 8);

                        if (tag.Tag.Length < 8)
                            data.Tags.Add(tag);
                    }
                }

            Cache.UpdateLocal("", null, data.Encode(Network.Protocol));
        }

        void InvokeTags(ulong user, ServiceData data)
        {
            foreach (PatchTag tag in data.Tags)
                if (TagReceived.Contains(tag.Service, tag.DataType))
                    TagReceived[tag.Service, tag.DataType].Invoke(user, tag.Tag);
        }

        void Cache_FileAquired(OpVersionedFile file)
        {
            ServiceData data = ServiceData.Decode(file.Header.Extra);
            
            if(GlobalIM ) // cant check here if in buddy list because on localSync load, buddy list is null
                InRange[file.UserID] = data;

            if (Network.Routing.InCacheArea(file.UserID))
                InRange[file.UserID] = data;
            else
                OutofRange[file.UserID] = data;

            InvokeTags(file.UserID, data);
        }

        void Cache_FileRemoved(OpVersionedFile file)
        {
            if(InRange.ContainsKey(file.UserID))
                InRange.Remove(file.UserID);

            if (OutofRange.ContainsKey(file.UserID))
                OutofRange.Remove(file.UserID);
        }

        List<byte[]> Store_Replicate(DhtContact contact)
        {
            if (GlobalIM) // cache area doesnt change with network in global IM mode
                return null;

            // indicates cache area has changed, move contacts between out and in range

            // move in to out
            List<ulong> remove = new List<ulong>();

            foreach(ulong user in InRange.Keys)
                if (!Network.Routing.InCacheArea(user))
                {
                    OutofRange[user] = InRange[user];
                    remove.Add(user);
                }

            foreach (ulong key in remove)
                InRange.Remove(key);

            // move out to in
            remove.Clear();

            foreach (ulong user in OutofRange.Keys)
                if (Network.Routing.InCacheArea(user))
                {
                    InRange[user] = OutofRange[user];
                    remove.Add(user);
                }

            foreach (ulong key in remove)
                OutofRange.Remove(key);

            // invoke tags on data moving in range so all services are cached
            foreach (ulong key in remove)
                InvokeTags(key, InRange[key]);

            return null;
        }

        byte[] Locations_GetTag()
        {
            OpVersionedFile file = Cache.GetFile(Core.UserID);

            return (file != null) ? CompactNum.GetBytes(file.Header.Version) : null;
        }

        void Locations_TagReceived(DhtAddress address, ulong user, byte[] tag)
        {
            // if user not cached, we only active search their info if in local cache area

            if (tag.Length == 0)
                return;

            uint version = 0;

            OpVersionedFile file = Cache.GetFile(user);

            if (file != null)
            {
                version = CompactNum.ToUInt32(tag, 0, tag.Length);

                if (version < file.Header.Version)
                    Store.Send_StoreReq(address, null, new DataReq(null, file.UserID, ServiceID, DataTypeSync, file.SignedHeader));
            }

            // get new version of local sync file
            if ( (file != null && version > file.Header.Version) ||

                 (file == null && ( ( !GlobalIM && Network.Routing.InCacheArea(user)) ||
                                    (  GlobalIM && Core.Buddies.BuddyList.SafeContainsKey(user)) )))
            {
                Cache.Research(user);
            }

            // ensure we have the lastest versions of the user's services
            if (file != null)
                CheckTags(file.UserID);
        }

        private void CheckTags(ulong user)
        {
            if (InRange.ContainsKey(user))
                InvokeTags(user, InRange[user]);

            else if (OutofRange.ContainsKey(user))
                InvokeTags(user, OutofRange[user]);
        }


        internal void Research(ulong user)
        {
            Cache.Research(user);
        }


        public void SimTest()
        {
        }

        public void SimCleanup()
        {
        }
    }

    internal class SyncPacket
    {
        internal const byte ServiceData = 0x10;
    }

    internal class ServiceData : G2Packet
    {
        const byte Packet_Date = 0xE0;
        const byte Packet_Tag = 0xF0;

        internal DateTime Date;
        internal List<PatchTag> Tags = new List<PatchTag>();


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame data = protocol.WritePacket(null, SyncPacket.ServiceData, null);

                protocol.WritePacket(data, Packet_Date, BitConverter.GetBytes(Date.ToBinary()));

                foreach (PatchTag tag in Tags)
                    protocol.WritePacket(data, Packet_Tag, tag.ToBytes());

                return protocol.WriteFinish();
            }
        }

        internal static ServiceData Decode(byte[] data)
        {
            G2Header root = new G2Header(data);

            G2Protocol.ReadPacket(root);

            if (root.Name != LocationPacket.Data)
                return null;

            ServiceData packet = new ServiceData();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Date:
                        packet.Date = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_Tag:
                        packet.Tags.Add(PatchTag.FromBytes(child.Data, child.PayloadPos, child.PayloadSize));
                        break;
                }
            }

            return packet;
        }
    }
}
