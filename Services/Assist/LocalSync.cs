using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;

using RiseOp.Services.Assist;
using RiseOp.Services.Location;

namespace RiseOp.Services.Assist
{
    internal delegate byte[] GetLocalSyncTagHandler();
    internal delegate void LocalSyncTagReceivedHandler(ulong user, byte[] tag);


    class LocalSync : OpService
    {
        public string Name { get { return "LocalSync"; } }
        public ushort ServiceID { get { return 11; } }

        OpCore Core;
        DhtNetwork Network;

        enum DataType { SyncObject = 1 };

        internal VersionedCache Cache;

        internal ServiceEvent<GetLocalSyncTagHandler> GetTag = new ServiceEvent<GetLocalSyncTagHandler>();
        internal ServiceEvent<LocalSyncTagReceivedHandler> TagReceived = new ServiceEvent<LocalSyncTagReceivedHandler>();

      
        internal LocalSync(OpCore core)
        {
            Core = core;
            Network = core.OperationNet;
            Core.Sync = this;

            Core.Locations.GetTag[ServiceID, (ushort) DataType.SyncObject] += new GetLocationTagHandler(Locations_GetTag);
            Core.Locations.TagReceived[ServiceID, (ushort) DataType.SyncObject] += new LocationTagReceivedHandler(Locations_TagReceived);


            Cache = new VersionedCache(Network, ServiceID, (ushort)DataType.SyncObject, false);
            Cache.FileAquired += new FileAquiredHandler(Cache_FileAquired);

            // if sync file for ourselves does not exist create
            if (!Cache.FileMap.SafeContainsKey(Core.LocalDhtID))
                UpdateLocal();
        }

        public void Dispose()
        {
            Core.Locations.GetTag[ServiceID, (ushort)DataType.SyncObject] -= new GetLocationTagHandler(Locations_GetTag);
            Core.Locations.TagReceived[ServiceID, (ushort)DataType.SyncObject] -= new LocationTagReceivedHandler(Locations_TagReceived);

            Cache.FileAquired -= new FileAquiredHandler(Cache_FileAquired);
            Cache.Dispose();
        }

        public List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong user, uint project)
        {
            return null;
        }

        internal void UpdateLocal()
        {
            ServiceData data = new ServiceData();
            data.Date = Core.TimeNow.ToUniversalTime();

            foreach (ushort service in GetTag.HandlerMap.Keys)
                foreach (ushort datatype in GetTag.HandlerMap[service].Keys)
                {
                    LocationTag tag = new LocationTag();

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

            Cache.UpdateLocal("", null, data.Encode(Core.Protocol));
        }

        void Cache_FileAquired(OpVersionedFile file)
        {
            ServiceData data = ServiceData.Decode(Core.Protocol, file.Header.Extra);

            foreach (LocationTag tag in data.Tags)
                if (TagReceived.Contains(tag.Service, tag.DataType))
                    TagReceived[tag.Service, tag.DataType].Invoke(file.DhtID, tag.Tag);
        }

        byte[] Locations_GetTag()
        {
            OpVersionedFile file = Cache.GetFile(Core.LocalDhtID);

            return (file != null) ? BitConverter.GetBytes(file.Header.Version) : null;
        }

        void Locations_TagReceived(ulong user, byte[] tag)
        {
            if (tag.Length < 4)
                return;

            OpVersionedFile file = Cache.GetFile(user);

            if (file != null)
            {
                uint version = BitConverter.ToUInt32(tag, 0);

                if (version > file.Header.Version)
                    foreach (ClientInfo client in Core.Locations.GetClients(user))
                        Network.Searches.SendDirectRequest(new DhtAddress(client.Data.IP, client.Data.Source), user, ServiceID, (ushort) DataType.SyncObject, BitConverter.GetBytes(version));
            }
        }


        internal void Research(ulong user)
        {
            Cache.Research(user);
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
        internal List<LocationTag> Tags = new List<LocationTag>();


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame data = protocol.WritePacket(null, SyncPacket.ServiceData, null);

                protocol.WritePacket(data, Packet_Date, BitConverter.GetBytes(Date.ToBinary()));

                foreach (LocationTag tag in Tags)
                    protocol.WritePacket(data, Packet_Tag, tag.ToBytes());

                return protocol.WriteFinish();
            }
        }

        internal static ServiceData Decode(G2Protocol protocol, byte[] data)
        {
            G2Header root = new G2Header(data);

            protocol.ReadPacket(root);

            if (root.Name != LocPacket.LocationData)
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
                        packet.Tags.Add(LocationTag.FromBytes(child.Data, child.PayloadPos, child.PayloadSize));
                        break;
                }
            }

            return packet;
        }
    }
}
