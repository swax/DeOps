using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Net;

using RiseOp.Implementation;
using RiseOp.Implementation.Transport;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;

using RiseOp.Services;
using RiseOp.Services.Location;


namespace RiseOp.Implementation.Dht
{
    internal delegate void StoreHandler(DataReq data);
    internal delegate List<byte[]> ReplicateHandler(DhtContact contact);
    internal delegate void PatchHandler(DhtAddress source, byte[] data);

    internal class DhtStore
    {
        //super-class
        OpCore Core;
        DhtNetwork Network; 

        internal ServiceEvent<StoreHandler> StoreEvent = new ServiceEvent<StoreHandler>();
        internal ServiceEvent<ReplicateHandler> ReplicateEvent = new ServiceEvent<ReplicateHandler>(); // this event doesnt support overloading
        internal ServiceEvent<PatchHandler> PatchEvent = new ServiceEvent<PatchHandler>();


        internal DhtStore(DhtNetwork network)
        {
            Network = network;
            Core = Network.Core;
        }

        internal void PublishNetwork(ulong target, uint service, uint datatype, byte[] data)
        {
            if (Core.InvokeRequired)
                Debug.Assert(false);

            string type = "Publish " + service.ToString();

            DataReq store = new DataReq(null, target, service, datatype, data);

            // find users closest to publish target
            if (target == Network.Local.UserID)
            {
                foreach (DhtContact closest in Network.Routing.GetCacheArea())
                    Send_StoreReq(closest, null, store);

                foreach (TcpConnect socket in Network.TcpControl.ProxyClients)
                    Send_StoreReq(new DhtAddress(socket.RemoteIP, socket), null, store);
            }
            else
            {
                DhtSearch search = Network.Searches.Start(target, type, 0, 0, null, new EndSearchHandler(EndPublishSearch));

                if (search != null)
                    search.Carry = store;
            }
        }

        void EndPublishSearch(DhtSearch search)
        {
            DataReq publish = (DataReq)search.Carry;

            // need to carry over componentid that wanted search also so store works

            foreach (DhtLookup node in search.LookupList)
                Send_StoreReq(node.Contact, null, publish);
        }

        internal void PublishDirect(List<LocationData> locations, ulong target, uint service, uint datatype, byte[] data)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { PublishDirect(locations, target, service, datatype, data); });
                return;
            }

            DataReq req = new DataReq(null, target, service, datatype, data);
            
            foreach (LocationData location in locations)
            {
                DhtAddress address = new DhtAddress(location.IP, location.Source);
                Send_StoreReq(address, null, req);

                foreach (DhtAddress proxy in location.Proxies)
                    Send_StoreReq(proxy, null, req);
            }
        }

        internal void Send_StoreReq(DhtAddress address, DhtClient localProxy, DataReq publish)
        {
            if (address == null)
                return;

            StoreReq store = new StoreReq();
            store.Source    = Network.GetLocalSource();
            store.Key       = publish.Target;
            store.Service   = publish.Service;
            store.DataType  = publish.DataType;
            store.Data      = publish.Data;

            int sentBytes = 0;
  
            TcpConnect direct = Network.TcpControl.GetProxy(address);

            if (direct != null)
                sentBytes = direct.SendPacket(store);

            else if (address.TunnelClient != null)
                sentBytes = Network.SendTunnelPacket(address, store);

            // if blocked send tcp with to tag
            else if (Core.Firewall == FirewallType.Blocked)
            {
                store.ToAddress = address;

                TcpConnect proxy = Network.TcpControl.GetProxy(localProxy);

                if (proxy != null)
                    sentBytes = proxy.SendPacket(store);
                else
                    sentBytes = Network.TcpControl.SendRandomProxy(store);
            }
            else
                sentBytes = Network.UdpControl.SendTo(address, store);

            Core.ServiceBandwidthOut[store.Service].Accumulated += sentBytes;
        }

        internal void Receive_StoreReq(G2ReceivedPacket packet)
        {
            StoreReq store = StoreReq.Decode(packet);

            if (Core.ServiceBandwidthIn.ContainsKey(store.Service))
                Core.ServiceBandwidthIn[store.Service].Accumulated += packet.Root.Data.Length;

            if (store.Source.Firewall == FirewallType.Open )
                    // dont need to add to routing if nat/blocked because eventual routing ping by server will auto add
                    Network.Routing.Add(new DhtContact(store.Source, packet.Source.IP));


            // forward to proxied nodes - only replicate data to blocked nodes on operation network
            if (!Network.IsGlobal)
                // when we go offline it will be these nodes that update their next proxy with stored info
                foreach (TcpConnect socket in Network.TcpControl.ProxyClients)
                    if (packet.Tcp != socket)
                    {
                        if (packet.ReceivedUdp)
                            store.FromAddress = packet.Source;

                        socket.SendPacket(store);
                    }

            // pass to components
            DataReq data = new DataReq(new List<DhtAddress>(), store.Key, store.Service, store.DataType, store.Data);
            data.Sources.Add( packet.Source);

            if (packet.ReceivedTcp && packet.Tcp.Proxy == ProxyType.Server)
                data.LocalProxy = new DhtClient(packet.Tcp);

            if(data.Service == 0)
                Receive_Patch(packet.Source, store.Data);

            else if (StoreEvent.Contains(store.Service, store.DataType))
                StoreEvent[store.Service, store.DataType].Invoke(data);
        }

        internal void Replicate(DhtContact contact)
        {
            // when new user comes into our cache area, we send them the data we have in our high/low/xor bounds

            // replicate is only for cached area
            // for remote user stuff that loads up with client, but now out of bounds, it is
            // republished by the uniqe modifier on data

            List<PatchTag> PatchList = new List<PatchTag>();

            // get data that needs to be replicated from components
            // structure as so
            //      contact
            //          service [] 
            //              datatype []
            //                  patch data []

            foreach (uint service in ReplicateEvent.HandlerMap.Keys)
                foreach (uint datatype in ReplicateEvent.HandlerMap[service].Keys)
                {
                    List<byte[]> patches = ReplicateEvent.HandlerMap[service][datatype].Invoke(contact);

                    if(patches != null)
                        foreach (byte[] data in patches)
                        {
                            PatchTag patch = new PatchTag();
                            patch.Service = service;
                            patch.DataType = datatype;
                            patch.Tag = data;

                            PatchList.Add(patch);
                        }
                }

            PatchPacket packet = new PatchPacket();

            int totalSize = 0;

            foreach (PatchTag patch in PatchList)
            {
                if (patch.Tag.Length + totalSize > 1000)
                {
                    if (packet.PatchData.Count > 0)
                        Send_StoreReq(contact, contact, new DataReq(null, contact.UserID, 0, 0, packet.Encode(Network.Protocol)));

                    packet.PatchData.Clear();
                    totalSize = 0;
                }

                packet.PatchData.Add(patch);
                totalSize += patch.Tag.Length;
            }

            if (packet.PatchData.Count > 0)
                Send_StoreReq(contact, contact, new DataReq(null, contact.UserID, 0, 0, packet.Encode(Network.Protocol)));

        }

        private void Receive_Patch(DhtAddress source, byte[] data)
        {
            // invoke patch
            G2Header root = new G2Header(data);

            if (G2Protocol.ReadPacket(root))
                if (root.Name == StorePacket.Patch)
                {
                    PatchPacket packet = PatchPacket.Decode(root);

                    if (packet == null)
                        return;

                    foreach (PatchTag patch in packet.PatchData)
                        if (PatchEvent.Contains(patch.Service, patch.DataType))
                            PatchEvent[patch.Service, patch.DataType].Invoke(source, patch.Tag);
                }
        }
    }

    internal class DataReq
    {
        internal List<DhtAddress> Sources;
        internal DhtClient LocalProxy;

        internal ulong  Target;
        internal uint Service;
        internal uint DataType;
        internal byte[] Data;

        internal DataReq(List<DhtAddress> sources, ulong target, uint service, uint datatype, byte[] data)
        {
            Sources   = sources;
            Target    = target;
            Service   = service;
            DataType  = datatype;
            Data      = data;
        }
    }

    internal class StorePacket
    {
        internal const byte Patch = 0x10;
    }

    internal class PatchPacket : G2Packet
    {
        const byte Packet_Patch = 0x01;

        internal List<PatchTag> PatchData = new List<PatchTag>();

        internal PatchPacket()
        {

        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame patch = protocol.WritePacket(null, StorePacket.Patch, null);

                foreach (PatchTag tag in PatchData)
                    protocol.WritePacket(patch, Packet_Patch, tag.ToBytes());

                return protocol.WriteFinish();
            }
        }

        internal static PatchPacket Decode(G2Header root)
        {
            PatchPacket patch = new PatchPacket();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
                if (child.Name == Packet_Patch && G2Protocol.ReadPayload(child))
                    patch.PatchData.Add((PatchTag) PatchTag.FromBytes(child.Data, child.PayloadPos, child.PayloadSize));

            return patch;
        }
    }
}
