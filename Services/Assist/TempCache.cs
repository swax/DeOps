using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;

namespace RiseOp.Services.Assist
{
    // provides temporary caching of data at a target spot on the network

    internal class TempCache
    {
        OpCore Core;
        DhtNetwork Network;

        uint ServiceID;
        uint DataType;

        int MaxEntries = 512;

        internal List<TempData> CachedData = new List<TempData>();


        internal TempCache(DhtNetwork network, uint service, uint type)
        {
            Core = network.Core;
            Network = network;

            ServiceID = service;
            DataType = type;

            Core.MinuteTimerEvent += new TimerHandler(Core_MinuteTimer);

            Network.Store.StoreEvent[ServiceID, DataType] += new StoreHandler(Store_Local);
            Network.Searches.SearchEvent[ServiceID, DataType] += new SearchRequestHandler(Search_Local);

            Network.Store.ReplicateEvent[ServiceID, DataType] += new ReplicateHandler(Store_Replicate);
            Network.Store.PatchEvent[ServiceID, DataType] += new PatchHandler(Store_Patch);

        }

        public void Dispose()
        {
            Core.MinuteTimerEvent -= new TimerHandler(Core_MinuteTimer);

            Network.Store.StoreEvent[ServiceID, DataType] -= new StoreHandler(Store_Local);
            Network.Searches.SearchEvent[ServiceID, DataType] -= new SearchRequestHandler(Search_Local);

            Network.Store.ReplicateEvent[ServiceID, DataType] -= new ReplicateHandler(Store_Replicate);
            Network.Store.PatchEvent[ServiceID, DataType] -= new PatchHandler(Store_Patch);
        }

        void Core_MinuteTimer()
        {
            CachedData.ForEach(d => d.TTL--);

            // remove expired entries
            CachedData.
                Where(d => d.TTL < 0).
                ToList().
                ForEach(d => CachedData.Remove(d));

            // if max reached, remove furthest
            if (CachedData.Count > MaxEntries)
                CachedData.
                    OrderByDescending(d => d.TargetID ^ Network.Local.UserID).
                    Take(CachedData.Count - MaxEntries).
                    ToList().
                    ForEach(d => CachedData.Remove(d));
        }

        void Store_Local(DataReq store)
        {
            // location being published to hashid so others can get sources

            TempData temp = TempData.Decode(store.Target, store.Data);

            if (temp == null)
                return;

            TempData dupe = CachedData.Where(l => l.TargetID == temp.TargetID && Utilities.MemCompare(l.Data, temp.Data)).FirstOrDefault();

            if (dupe != null)
            {
                if(temp.TTL > dupe.TTL)
                    dupe.TTL = temp.TTL;

                return;
            }

            CachedData.Add(temp);
        }

        void Search_Local(ulong key, byte[] parameters, List<byte[]> results)
        {
            // return 3 random data entries for target

            results.AddRange((from l in CachedData
                              where l.TargetID == key
                              orderby Core.RndGen.Next()
                              select l.Encode(Network.Protocol)).Take(3));
        }

        internal void Publish(ulong target, byte[] data)
        {
            TempData temp = new TempData(target)
            {
                TTL = 60,
                Data = data
            };

            byte[] encoded = temp.Encode(Network.Protocol);

            Store_Local(new DataReq(null, target, ServiceID, DataType, encoded));
            
            Network.Store.PublishNetwork(target, ServiceID, DataType, encoded);
        }

        internal void Search(ulong target, object hostArg, Action<byte[], object> hostFoundEvent)
        {
            DhtSearch search = Network.Searches.Start(target, "Temp Search", ServiceID, DataType, null, Search_FoundTemp);

            if (search != null)
                search.Carry = new object[] { hostArg, hostFoundEvent };
        }

        void Search_FoundTemp(DhtSearch search, DhtAddress source, byte[] data)
        {
            object[] carry = search.Carry as object[];

            object hostArg = carry[0];
            Action<byte[], object> hostFoundEvent = carry[1] as Action<byte[], object>;

            // strip temp data
            TempData temp = TempData.Decode(search.TargetID, data);

            if (temp == null)
                return;

            // fire host event with carry vars
            hostFoundEvent.Invoke(temp.Data, hostArg);
        }

        List<byte[]> Store_Replicate(DhtContact contact)
        {
            List<byte[]> patches = new List<byte[]>();

            foreach (TempData temp in CachedData)
                if (Network.Routing.InCacheArea(temp.TargetID))
                    patches.Add(BitConverter.GetBytes(temp.TargetID));

            return patches;
        }

        void Store_Patch(DhtAddress source, byte[] data)
        {
            if (!Network.Established || data.Length < 8)
                return;

            ulong user = BitConverter.ToUInt64(data, 0);

            if (!Network.Routing.InCacheArea(user))
                return;

            if (!CachedData.Any(d => d.TargetID == user))
                Network.Searches.SendDirectRequest(source, user, ServiceID, DataType, null);
        }
    }

    internal class TempPacket
    {
        internal const byte Data = 0x10;
    }

    internal class TempData : G2Packet
    {
        const byte Packet_TTL = 0x10;
        const byte Packet_Data = 0x20;


        internal ulong TargetID;
        internal int TTL;
        internal byte[] Data;


        internal TempData(ulong target)
        {
            TargetID = target;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame temp = protocol.WritePacket(null, TempPacket.Data, null);

                protocol.WritePacket(temp, Packet_TTL, CompactNum.GetBytes(TTL));
                protocol.WritePacket(temp, Packet_Data, Data);

                return protocol.WriteFinish();
            }
        }

        internal static TempData Decode(ulong target, byte[] data)
        {
            G2Header root = new G2Header(data);

            G2Protocol.ReadPacket(root);

            if (root.Name != TempPacket.Data)
                return null;

            TempData temp = new TempData(target);
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_TTL:
                        temp.TTL = CompactNum.ToInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Data:
                        temp.Data = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return temp;
        }
    }
}
