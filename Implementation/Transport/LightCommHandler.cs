using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Comm;
using RiseOp.Implementation.Protocol.Net;

using RiseOp.Services.Location;


namespace RiseOp.Implementation.Transport
{
    internal delegate void LightDataHandler(DhtClient client, byte[] data);


    // a 'light' reliable udp between users
    // a user is a collection of addresses, a message sent to a user through lightComm
    // will automatically try various addresses to send the packet without the service
    // having to write complicated netcode for preferencing addresses, trying, and timeouts, etc..

    // only 1 packet outstanding to an address at a time
    // next packet not sent until ack received, or timeout

    internal class LightCommHandler
    {
        OpCore Core;
        DhtNetwork Network;

        Dictionary<ulong, LightClient> Active = new Dictionary<ulong, LightClient>();
        internal Dictionary<ulong, LightClient> Clients = new Dictionary<ulong, LightClient>();

        internal ServiceEvent<LightDataHandler> Data = new ServiceEvent<LightDataHandler>();


        internal LightCommHandler(DhtNetwork network)
        {
            Network = network;
            Core = network.Core;
        }

        internal void SecondTimer()
        {
            foreach (LightClient client in Active.Values)
                client.TrySend(Network);

            foreach (LightClient client in Active.Values.Where(c => c.Packets.Count == 0).ToArray())
                Active.Remove(client.RoutingID);

            // each minute clean locations
            if (Core.TimeNow.Second == 0)
            {
                if(Clients.Count > 50)
                    foreach (LightClient old in Clients.Values.OrderBy(c => c.LastSeen).ToArray())
                    {
                        if (Clients.Count <= 50)
                            break;

                        if (Active.ContainsKey(old.RoutingID))
                            continue;

                        if (old.LastSeen.AddMinutes(5) > Core.TimeNow)
                            continue;

                        Clients.Remove(old.RoutingID);
                    }
            }
        }

        internal void Update(LocationData location)
        {
            DhtClient client = new DhtClient(location.Source);

            if (!Clients.ContainsKey(client.RoutingID))
                Clients[client.RoutingID] = new LightClient(client);

            LightClient light = Clients[client.RoutingID];

           light.AddAddress(Core, new DhtAddress(location.IP, location.Source), false);

            foreach (DhtAddress address in location.Proxies)
                light.AddAddress(Core, address, false);

            foreach (DhtAddress server in location.TunnelServers)
                light.AddAddress(Core, new DhtContact(location.Source, location.IP, location.TunnelClient, server), false);
        }

        internal void Update(DhtClient client, DhtAddress address)
        {
            // clients can have different userids than their address (proxied)

            if (!Clients.ContainsKey(client.RoutingID))
                Clients[client.RoutingID] = new LightClient(client);

            Clients[client.RoutingID].AddAddress(Core, address, false);
        }

        internal void SendPacket(DhtClient client, uint service, int type, G2Packet packet)
        {
            SendPacket(client, service, type, packet, false);
        }

        internal void SendPacket(DhtClient client, uint service, int type, G2Packet packet, bool expedite)
        {
            RudpPacket comm     = new RudpPacket();
            comm.SenderID       = Network.Local.UserID;
            comm.SenderClient   = Network.Local.ClientID;
            comm.TargetID       = client.UserID;
            comm.TargetClient   = client.ClientID;
            comm.PacketType     = RudpPacketType.Light;
            comm.Payload        = RudpLight.Encode(service, type, packet.Encode(Network.Protocol));
            comm.PeerID         = (ushort)Core.RndGen.Next(ushort.MaxValue); // used to ack
            comm.Sequence       = 0;


            if (!Clients.ContainsKey(client.RoutingID))
                return;

            LightClient target = Clients[client.RoutingID];

            if (expedite)
            {
                target.NextTry = Core.TimeNow;
                target.Packets.AddFirst(new Tuple<uint, RudpPacket>(service, comm));
                target.TrySend(Network);
                return;
            }

            Active[client.RoutingID] = target;

            target.Packets.AddLast(new Tuple<uint, RudpPacket>(service, comm));
            while (target.Packets.Count > 15)
            {
                //crit - log to console? Debug.Assert(false);
                target.Packets.RemoveFirst();
            }

            target.TrySend(Network);
        }

        internal void ReceivePacket(G2ReceivedPacket raw, RudpPacket packet)
        {
            DhtClient client = new DhtClient(raw.Source);

            if (!Clients.ContainsKey(client.RoutingID))
                Clients[client.RoutingID] = new LightClient(client);

            LightClient light = Clients[client.RoutingID];
            light.LastSeen = Core.TimeNow;

            light.AddAddress(Core, new RudpAddress(raw.Source), true);
            
            if (raw.ReceivedTcp) // add this second so sending ack through tcp proxy is perferred
                light.AddAddress(Core, new RudpAddress(raw.Source, raw.Tcp), true);

            
            if (packet.PacketType == RudpPacketType.LightAck)
                ReceiveAck( raw, light, packet);
            else
            {
                RudpLight info = new RudpLight(packet.Payload);

                if (Core.ServiceBandwidth.ContainsKey(info.Service))
                    Core.ServiceBandwidth[info.Service].InPerSec += raw.Root.Data.Length;

                if (Data.Contains(info.Service, info.Type))
                    Data[info.Service, info.Type].Invoke(client, info.Data);

                SendAck(light, packet, info.Service);
            }
        }

        void SendAck(LightClient light, RudpPacket packet, uint service)
        {
            RudpPacket comm = new RudpPacket();
            comm.SenderID = Network.Local.UserID;
            comm.SenderClient = Network.Local.ClientID;
            comm.TargetID = light.Client.UserID;
            comm.TargetClient = light.Client.ClientID;
            comm.PacketType = RudpPacketType.LightAck;
            comm.PeerID = packet.PeerID; // so remote knows which packet we're acking
            comm.Ident = packet.Ident; // so remote knows which address is good
            comm.Sequence = 0;

            // send ack to first address, addresses moved to front on receive packet
            int sentBytes = light.SendtoAddress(Network, light.Addresses.First.Value, comm);

            Core.ServiceBandwidth[service].OutPerSec += sentBytes;

            // on resend packet from remote we receive it through different proxy, so that address
            // is moved to the front of the list automatically and our ack takes that direction
        }

        void ReceiveAck(G2ReceivedPacket raw, LightClient client, RudpPacket packet)
        {
            // add sources, dont promote to front, we move packet ident to front
            client.AddAddress(Core, new RudpAddress(raw.Source), false);

            if (raw.ReceivedTcp)
                client.AddAddress(Core, new RudpAddress(raw.Source, raw.Tcp), false);


            // remove acked packet
            foreach(Tuple<uint, RudpPacket> tuple in client.Packets)
                if(tuple.Second.PeerID == packet.PeerID)
                {
                    client.Packets.Remove(tuple);
                    client.Attempts = 0;
                    break;
                }

            // read ack ident and move to top
            foreach(RudpAddress address in client.Addresses)
                if (address.Ident == packet.Ident)
                {
                    client.Addresses.Remove(address);
                    client.Addresses.AddFirst(address);
                    address.LastAck = Core.TimeNow;
                    break;
                }

            client.NextTry = Core.TimeNow;

            // receieved ack, try to send next packet immediately
            client.TrySend(Network);
        }

        internal List<DhtAddress> GetAddresses(ulong id)
        {
            if (Clients.ContainsKey(id))
                return Clients[id].Addresses.Select(a => a.Address).Take(3).ToList();

            return null;
        }
    }

    internal class LightClient
    {
        internal DhtClient Client;
        internal ulong RoutingID;

        internal LinkedList<RudpAddress> Addresses = new LinkedList<RudpAddress>();
        internal LinkedList<Tuple<uint, RudpPacket>> Packets = new LinkedList<Tuple<uint, RudpPacket>>(); // service, packet

        internal DateTime LastSeen;
        internal DateTime NextTry;
        internal int Attempts;


        internal LightClient(DhtClient client)
        {
            Client = new DhtClient(client);
            RoutingID = Client.RoutingID;
        }

        internal void AddAddress(OpCore core, DhtAddress address, bool moveFront)
        {
            AddAddress(core, new RudpAddress(address), moveFront);

            // limit 5, remove oldest addresses, but not untried ones
            if(Addresses.Count > 5)
                foreach(RudpAddress old in (from a in Addresses orderby a.LastAck select a).ToArray())
                {
                    if (Addresses.Count <= 5)
                        break;

                    if(old.LastAck == default(DateTime))
                        continue;

                    Addresses.Remove(old);
                }
        }

        internal void AddAddress(OpCore core, RudpAddress address, bool moveFront)
        {
            foreach (RudpAddress check in Addresses)
                if (check.GetHashCode() == address.GetHashCode())
                {
                    if (moveFront)
                    {
                        Addresses.Remove(check);
                        Addresses.AddFirst(check);
                    }

                    return;
                }

            address.Ident = (uint) core.RndGen.Next();
            Addresses.AddLast(address);
        }

        internal void TrySend(DhtNetwork network)
        {
            // check if stuff in queue
            if (Packets.Count == 0 ||
                Addresses.Count == 0 ||
                network.Core.TimeNow < NextTry)
                return;

            Attempts++;
            if (Attempts >= Addresses.Count * 2) // every address known tried twice
            {
                Attempts = 0;
                Packets.RemoveFirst();
                return;
            }

            RudpAddress target = Addresses.First.Value;
            Addresses.RemoveFirst();
            Addresses.AddLast(target);

            NextTry = network.Core.TimeNow.AddSeconds(3);

            Tuple<uint, RudpPacket> tuple = Packets.First.Value;
            RudpPacket packet = tuple.Second;
            packet.Ident = target.Ident;

            int sentBytes = SendtoAddress(network, target, packet);

            network.Core.ServiceBandwidth[tuple.First].OutPerSec += sentBytes;
        }

        internal int SendtoAddress(DhtNetwork network, RudpAddress target, RudpPacket packet)
        {
            Debug.Assert(packet.Payload != null || packet.PacketType == RudpPacketType.LightAck);

            int sentBytes = 0;

            // same code used in rudpSocket
            if (network.Core.Firewall != FirewallType.Blocked && target.LocalProxy == null)
            {
                sentBytes = network.SendPacket(target.Address, packet);
            }

            else if (target.Address.TunnelClient != null)
                sentBytes = network.SendTunnelPacket(target.Address, packet);

            else
            {
                packet.ToEndPoint = target.Address;

                TcpConnect proxy = network.TcpControl.GetProxy(target.LocalProxy);

                if (proxy != null)
                    sentBytes = proxy.SendPacket(packet);
                else
                    sentBytes = network.TcpControl.SendRandomProxy(packet);
            }

            return sentBytes;
        }
    }
}
