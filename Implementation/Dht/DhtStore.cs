using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Net;

using DeOps.Implementation;
using DeOps.Implementation.Transport;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;

using DeOps.Components;
using DeOps.Components.Location;


namespace DeOps.Implementation.Dht
{
    internal delegate void StoreHandler(DataReq data);
    internal delegate ReplicateData ReplicateHandler(DhtContact contact, bool add);
    internal delegate void PatchHandler(DhtAddress source, ulong distance, byte[] data);

    internal class DhtStore
    {
        //super-class
        OpCore Core;
        DhtNetwork Network; 
        
        //crit - if middle bucket had one entry would it still be replicated to? maybe should not use maxdistance
        internal ulong MaxDistance = ulong.MaxValue; 

        internal Dictionary<ushort, StoreHandler> StoreEvent = new Dictionary<ushort, StoreHandler>();
        internal Dictionary<ushort, ReplicateHandler> ReplicateEvent = new Dictionary<ushort, ReplicateHandler>();
        internal Dictionary<ushort, PatchHandler> PatchEvent = new Dictionary<ushort, PatchHandler>();


        internal DhtStore(DhtNetwork network)
        {
            Network = network;
            Core = Network.Core;
        }

        internal void PublishNetwork(ulong target, ushort component, byte[] data)
        {
            if (Core.InvokeRequired)
                Debug.Assert(false);

            string type = "Publish " + component.ToString();

            DhtSearch search = Network.Searches.Start(target, type, ComponentID.Node, null, new EndSearchHandler(EndPublishSearch));
            
            if(search != null)
                search.Carry = new DataReq(null, target, component, data);
        }

        void EndPublishSearch(DhtSearch search)
        {
            DataReq publish = (DataReq)search.Carry;

            // need to carry over componentid that wanted search also so store works

            foreach (DhtLookup node in search.LookupList)
                Send_StoreReq(node.Contact.ToDhtAddress(), 0, publish);
        }

        internal void PublishDirect(List<LocationData> locations, ulong target, ushort component, byte[] data)
        {
            if (Core.InvokeRequired)
                Debug.Assert(false);

            DataReq req = new DataReq(null, target, component, data);
            
            foreach (LocationData location in locations)
            {
                DhtAddress address = new DhtAddress(location.IP, location.Source);
                Send_StoreReq(address, 0, req);

                foreach (DhtAddress proxy in location.Proxies)
                    Send_StoreReq(proxy, 0, req);
            }
        }
        internal void Send_StoreReq(DhtAddress address, ulong proxyID, DataReq publish)
        {
            if (address == null)
                return;

            StoreReq store = new StoreReq();
            store.Source    = Network.GetLocalSource();
            store.Key       = publish.Target;
            store.Component = publish.Component;
            store.Data      = publish.Data;

            // if blocked send tcp with to tag
            if (Core.Firewall == FirewallType.Blocked || proxyID != 0)
            {
                store.ToAddress = address;

                if (proxyID == 0)
                    Network.TcpControl.ProxyPacket(address.DhtID, store);

                //crit doesnt work with multiple clients of same id
                else if (Network.TcpControl.ConnectionMap.ContainsKey(proxyID) &&
                    Network.TcpControl.ConnectionMap[proxyID].Proxy == ProxyType.Server)
                    Network.TcpControl.ConnectionMap[proxyID].SendPacket(store);
            }
            else
                Network.UdpControl.SendTo(address, store);
        }

        internal void Receive_StoreReq(G2ReceivedPacket packet)
        {
            StoreReq store = StoreReq.Decode(Core.Protocol, packet);

            if (store.Source.Firewall == FirewallType.Open )
                    // dont need to add to routing if nat/blocked because eventual routing ping by server will auto add
                    Network.Routing.Add(new DhtContact(store.Source, packet.Source.IP, Core.TimeNow));
            
            
            // forward to proxied nodes
            store.FromAddress = packet.Source;

            if (!Network.IsGlobal) // only replicate data to blocked nodes on operation network
                lock (Network.TcpControl.Connections)
                    foreach (TcpConnect connection in Network.TcpControl.Connections)
                        if (connection.State == TcpState.Connected &&
                            (connection.Proxy == ProxyType.ClientBlocked || connection.Proxy == ProxyType.ClientNAT))
                            if (packet.Tcp == null || packet.Tcp != connection)
                            {
                                if(packet.Tcp == null)
                                    store.FromAddress = packet.Source;
                                
                                connection.SendPacket(store);
                            }

            //crit delete?
            //if (!Core.Links.StructureKnown)
            //    if (!Core.Links.LinkMap.ContainsKey(store.Source.DhtID) || !Core.Links.LinkMap[store.Source.DhtID].Loaded)
            //        Core.Links.StartSearch(store.Source.DhtID, 0);

            // pass to components
            DataReq data = new DataReq(new List<DhtAddress>(), store.Key, store.Component, store.Data); //crit need to pass which tcp proxy received through
            data.Sources.Add( packet.Source);
            
            if(packet.Tcp != null)
                data.LocalProxy = packet.Tcp.DhtID;

            if(data.Component == 0)
                Receive_Patch(packet.Source, store.Data);

            else if (StoreEvent.ContainsKey(store.Component))
                StoreEvent[store.Component].Invoke(data);
        }

        internal bool IsCached(ulong id)
        {
            return (id ^ Core.LocalDhtID) < MaxDistance;
        }

        internal void RoutingUpdate(int depth)
        {
            // Dhtid 00000
            //          prefix  routing
            // depth 1  none    1...
            // depth 2  none    01...
            // depth 3  0       001..
            // dept  4  00      000..

            int prefix = 0;

            if (depth > 2)
                prefix = depth - 2;

            MaxDistance = ulong.MaxValue >> prefix;
        }

        internal void RoutingAdd(DhtContact contact)
        {
            /* This will need to be re-analyzed, there are 2 ways to patch per node, and per key.
             * Per Key: Foreach key, find the 8 closest nodes, if this added node is one of the closest, include data
             * associated with key in the patch.
             * Per Node: Find the closest 8 nodes to the new contact, make the furthest nodes id the max bounds
             * send patch keys with in those bounds.
             * Per Node takes less processing power but I think its not as accurate as per key
             * orr.. 
             * *** I can just check if added is of of the top 8 local nodes, if it is I replicate my
             * keys with in the top 8 range to that node
             *
             * figure out how to dynamically find 8 closest nodes to each key in caches shouldreplicate()? 
             *
             */

            // dont replicate to nodes outside our max caching bounds
            if ((contact.DhtID ^ Core.LocalDhtID) > MaxDistance)
                return;


            Replicate(contact, true);
        }

        internal void RoutingDelete(DhtContact contact)
        {
            // basically contact gets deleted, send patch to the new furthest replicate node

            // dont replicate to nodes outside our max caching bounds
            if ((contact.DhtID ^ Core.LocalDhtID) > MaxDistance)
                return;


            Replicate(contact, false);
        }

        internal void Replicate(DhtContact contact, bool add)
        {
            /* tricky stuff, for add or delete first new bounds are set
             * for an add patch info is sent to the added contact
             * for a delete patch info is sent to multiple targets that where close to the deleted
             *    targets depend on the specific key of the specific data
             */

            Dictionary<ulong, DhtContact> ContactMap = new Dictionary<ulong, DhtContact>();
            Dictionary<ulong, Dictionary<ushort, List<byte[]>>> DataMap = new Dictionary<ulong, Dictionary<ushort, List<byte[]>>>();

            // get data that needs to be replicated from components
            // structure as so
            //      contact
            //          component [] 
            //              patch data []
            foreach (ushort component in ReplicateEvent.Keys)
            {
                ReplicateData data = ReplicateEvent[component].Invoke(contact, false);

                if(data != null)
                    foreach (DhtContact target in data.TargetMap.Values)
                    {
                        if (!ContactMap.ContainsKey(target.DhtID))
                            ContactMap[target.DhtID] = target;

                        if (!DataMap.ContainsKey(target.DhtID))
                            DataMap[target.DhtID] = new Dictionary<ushort, List<byte[]>>();

                        DataMap[target.DhtID][component] = data.GetTargetData(target.DhtID);
                    }
            }

            ulong proxyID = 0;
            if (Network.TcpControl.ConnectionMap.ContainsKey(contact.DhtID))
                if (Network.TcpControl.ConnectionMap[contact.DhtID].ClientID == contact.ClientID)
                    proxyID = contact.DhtID;

            foreach (ulong id in DataMap.Keys)
            {
                PatchPacket packet = new PatchPacket();

                int totalSize = 0;
                
                foreach(ushort component in DataMap[id].Keys)
                {
                    List<byte[]> list = DataMap[id][component];

                    foreach (byte[] data in list)
                        {
                            if (data.Length + totalSize > 1200)
                            {
                                if (packet.PatchData.Count > 0)
                                    Send_StoreReq(ContactMap[id].ToDhtAddress(), proxyID, new DataReq(null, id, 0, packet.Encode(Core.Protocol)));

                                packet.PatchData.Clear();
                                totalSize = 0;
                            }

                            packet.PatchData.Add(new KeyValuePair<ushort, byte[]>(component, data));
                            totalSize += data.Length;
                        }
                }

                if (packet.PatchData.Count > 0)
                    Send_StoreReq(ContactMap[id].ToDhtAddress(), proxyID, new DataReq(null, id, 0, packet.Encode(Core.Protocol)));
            }
        }

        private void Receive_Patch(DhtAddress source, byte[] data)
        {
            // get max distance
            ulong localBounds = RecalcBounds(Core.LocalDhtID);

            // invoke patch
            G2Header root = new G2Header(data);

            if (Core.Protocol.ReadPacket(root))
                if (root.Name == StorePacket.Patch)
                {
                    PatchPacket packet = PatchPacket.Decode(Core.Protocol, root);

                    if (packet == null)
                        return;

                    foreach (KeyValuePair<ushort, byte[]> pair in packet.PatchData)
                        if (PatchEvent.ContainsKey(pair.Key))
                            PatchEvent[pair.Key].Invoke(source, localBounds, pair.Value);
                }
        }

        internal ulong RecalcBounds(ulong id)
        {
            List<DhtContact> closest = Network.Routing.Find(id, 8);

            if (closest.Count < 8)
                return ulong.MaxValue;

            return closest[7].DhtID ^ id;
        }

        internal ulong RecalcBounds(ulong id, bool add, ref DhtContact contact)
        {
            List<DhtContact> closest = Network.Routing.Find(id, 8);

            // add false means caller is re-calcing from contact delete operation, return next closest contact

            if (closest.Count < 8)
            {
                if (!add)
                    contact = null;

                return ulong.MaxValue;
            }

            if (!add)
                contact = closest[7];

            return closest[7].DhtID ^ id;
        }
    }

    internal class ReplicateData
    {
        ushort Component;

        int MaxSize = 1024;
        int EntrySize;

        internal Dictionary<ulong, DhtContact> TargetMap = new Dictionary<ulong,DhtContact>();
        internal Dictionary<ulong, List<byte[]>> Patches = new Dictionary<ulong, List<byte[]>>();
        internal Dictionary<ulong, int> Offsets = new Dictionary<ulong, int>();


        internal ReplicateData(ushort component, int entrySize)
        {
            Component = component;

            MaxSize = MaxSize - (MaxSize % entrySize);
            EntrySize = entrySize;
        }

        internal void Add(DhtContact target, byte[] data)
        {
            ulong id = target.DhtID;

            if (data.Length != EntrySize)
                throw new Exception("Data added to replication patch is not correct size " + data.Length.ToString() + " vs " + EntrySize.ToString());

            if (!TargetMap.ContainsKey(id))
                TargetMap[id] = target;

            // get patch for contact
            if (!Patches.ContainsKey(id))
            {
                Patches[id] = new List<byte[]>();
                Patches[id].Add(new byte[MaxSize]);
            }

            if (!Offsets.ContainsKey(id))
                Offsets[id] = 0;
            
            // add data to contact's data list
            List<byte[]> DataList = Patches[id];
            int offset = Offsets[id];

            data.CopyTo(DataList[DataList.Count - 1], offset);
            Offsets[id] = offset + EntrySize; // patch value is offset

            // if max reached create new data entry
            if (offset == MaxSize)
            {
                DataList.Add(new byte[MaxSize]);
                Offsets[id] = 0;
            }
        }

        internal List<byte[]> GetTargetData(ulong id)
        {
            if (!Patches.ContainsKey(id))
                throw new Exception("Target not found in patch map");

            List<byte[]> source = Patches[id];
            List<byte[]> final = new List<byte[]>();

            for (int i = 0; i < source.Count; i++)
                // if not last entry
                if (i != source.Count - 1)
                    final.Add(source[i]);
   
                // if last have to extract from offset
                else
                    final.Add(Utilities.ExtractBytes(source[i], 0, Offsets[id]));

            return final;
        }
    }

    internal class DataReq
    {
        internal List<DhtAddress> Sources;
        internal ulong LocalProxy;

        internal ulong  Target;
        internal ushort Component;
        internal byte[] Data;

        internal DataReq(List<DhtAddress> sources, ulong target, ushort component, byte[] data)
        {
            Sources   = sources;
            Target    = target;
            Component = component;
            Data      = data;
        }
    }

    internal class StorePacket
    {
        internal const byte Patch = 0x10;
    }

    internal class PatchPacket : G2Packet
    {
        const byte Packet_Component = 0x10;
        const byte Packet_Data = 0x20;

        internal List<KeyValuePair<ushort, byte[]>> PatchData = new List<KeyValuePair<ushort,byte[]>>();

        internal PatchPacket()
        {

        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame patch = protocol.WritePacket(null, StorePacket.Patch, null);

                foreach (KeyValuePair<ushort, byte[]> pair in PatchData)
                {
                    G2Frame data = protocol.WritePacket(patch, Packet_Component, BitConverter.GetBytes(pair.Key));

                    protocol.WritePacket(data, Packet_Data, pair.Value);
                }

                return protocol.WriteFinish();
            }
        }

        internal static PatchPacket Decode(G2Protocol protocol, G2Header root)
        {
            PatchPacket patch = new PatchPacket();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (child.Name == Packet_Component && G2Protocol.ReadPayload(child))
                {
                    ushort component = BitConverter.ToUInt16(child.Data, child.PayloadPos);
                    byte[] data = null;

                    G2Protocol.ResetPacket(child);

                    G2Header embedded = new G2Header(child.Data);
                    if (G2Protocol.ReadNextChild(child, embedded) == G2ReadResult.PACKET_GOOD)
                        if(embedded.Name == Packet_Data && G2Protocol.ReadPayload(embedded))
                            data = Utilities.ExtractBytes(embedded.Data, embedded.PayloadPos, embedded.PayloadSize);

                    if(data != null)
                        patch.PatchData.Add(new KeyValuePair<ushort, byte[]>(component, data));
                }
            }

            return patch;
        }
    }
}
