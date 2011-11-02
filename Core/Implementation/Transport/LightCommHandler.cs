using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Comm;
using DeOps.Implementation.Protocol.Net;

using DeOps.Services.Location;


namespace DeOps.Implementation.Transport
{
    public delegate void LightDataHandler(DhtClient client, byte[] data);


    // a 'light' reliable udp between users
    // a user is a collection of addresses, a message sent to a user through lightComm
    // will automatically try various addresses to send the packet without the service
    // having to write complicated netcode for preferencing addresses, trying, and timeouts, etc..
    
    // not as secure as rudp because only opkey is used, no public encryption
    // rudp has a fast unreliable way of sending packets if more security is needed

    // only 1 packet outstanding to an address at a time
    // next packet not sent until ack received, or timeout

    public class LightCommHandler
    {
        OpCore Core;
        DhtNetwork Network;

        Dictionary<ulong, LightClient> Active = new Dictionary<ulong, LightClient>();
        public Dictionary<ulong, LightClient> Clients = new Dictionary<ulong, LightClient>();

        public ServiceEvent<LightDataHandler> Data = new ServiceEvent<LightDataHandler>();


        public LightCommHandler(DhtNetwork network)
        {
            Network = network;
            Core = network.Core;
        }

        public void SecondTimer()
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

        public void Update(LocationData location)
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

        public void Update(DhtClient client, DhtAddress address)
        {
            // clients can have different userids than their address (proxied)

            if (!Clients.ContainsKey(client.RoutingID))
                Clients[client.RoutingID] = new LightClient(client);

            Clients[client.RoutingID].AddAddress(Core, address, false);
        }

        public void SendReliable(DhtClient client, uint service, int type, G2Packet packet)
        {
            SendReliable(client, service, type, packet, false);
        }

        public void SendReliable(DhtClient client, uint service, int type, G2Packet packet, bool expedite)
        {
            if (!Clients.ContainsKey(client.RoutingID))
                return;

            RudpPacket comm = CreateRudpPacket(client, service, type, packet, true);

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
            while (target.Packets.Count > 30)
            {
                //crit - log to console? Debug.Assert(false);
                target.Packets.RemoveFirst();
            }

            target.TrySend(Network);
        }

        RudpPacket CreateRudpPacket(DhtClient client, uint service, int type, G2Packet packet, bool reliable)
        {
            RudpPacket comm = new RudpPacket();
            comm.SenderID = Network.Local.UserID;
            comm.SenderClient = Network.Local.ClientID;
            comm.TargetID = client.UserID;
            comm.TargetClient = client.ClientID;
            comm.PacketType = RudpPacketType.Light;
            comm.Payload = RudpLight.Encode(service, type, packet.Encode(Network.Protocol));

            if (reliable)
            {
                comm.PeerID = (ushort)Core.RndGen.Next(ushort.MaxValue); // used to ack
                comm.Sequence = 1;
            }

            return comm;
        }

        /*public void SendUnreliable(RudpAddress address, uint service, int type, G2Packet packet)
        {
            // insecure, rudp provides this same method which is more secure, if a rudp connection is already established

            RudpPacket wrap = CreateRudpPacket(address.Address, service, type, packet, false);

            int sentBytes = LightClient.SendtoAddress(Core.Network, address, wrap);

            Core.ServiceBandwidth[service].OutPerSec += sentBytes;
        }*/

        public void ReceivePacket(G2ReceivedPacket raw, RudpPacket packet)
        {
            DhtClient client = new DhtClient(packet.SenderID, packet.SenderClient);

            if (!Clients.ContainsKey(client.RoutingID))
                Clients[client.RoutingID] = new LightClient(client);

            LightClient light = Clients[client.RoutingID];
            light.LastSeen = Core.TimeNow;

            // either direct, or node's proxy
            light.AddAddress(Core, new RudpAddress(raw.Source), true);
            
            if (raw.ReceivedTcp) // add this second so sending ack through tcp proxy is perferred
                light.AddAddress(Core, new RudpAddress(raw.Source, raw.Tcp), true);

            
            if (packet.PacketType == RudpPacketType.LightAck)
                ReceiveAck( raw, light, packet);

            else if(packet.PacketType == RudpPacketType.Light)
            {
                RudpLight info = new RudpLight(packet.Payload);

                if (Core.ServiceBandwidth.ContainsKey(info.Service))
                    Core.ServiceBandwidth[info.Service].InPerSec += raw.Root.Data.Length;

                if (Data.Contains(info.Service, info.Type))
                    Data[info.Service, info.Type].Invoke(client, info.Data);

                if(packet.Sequence == 1) // reliable packet
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
            int sentBytes = LightClient.SendtoAddress(Network, light.Addresses.First.Value, comm);

            Core.ServiceBandwidth[service].OutPerSec += sentBytes;

            // on resend packet from remote we receive it through different proxy, so that address
            // is moved to the front of the list automatically and our ack takes that direction
        }

        void ReceiveAck(G2ReceivedPacket raw, LightClient client, RudpPacket packet)
        {
            // remove acked packet
            foreach(Tuple<uint, RudpPacket> tuple in client.Packets)
                if (tuple.Param2.PeerID == packet.PeerID)
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

        public List<DhtAddress> GetAddresses(ulong id)
        {
            if (Clients.ContainsKey(id))
                return Clients[id].Addresses.Select(a => a.Address).Take(3).ToList();

            return null;
        }
    }

    public class LightClient
    {
        public DhtClient Client;
        public ulong RoutingID;

        public LinkedList<RudpAddress> Addresses = new LinkedList<RudpAddress>();
        public LinkedList<Tuple<uint, RudpPacket>> Packets = new LinkedList<Tuple<uint, RudpPacket>>(); // service, packet

        public DateTime LastSeen;
        public DateTime NextTry;
        public int Attempts;


        public LightClient(DhtClient client)
        {
            Client = new DhtClient(client);
            RoutingID = Client.RoutingID;
        }

        public void AddAddress(OpCore core, DhtAddress address, bool moveFront)
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

        public void AddAddress(OpCore core, RudpAddress address, bool moveFront)
        {
            Debug.Assert(address.Address.UdpPort != 0);

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

        public void TrySend(DhtNetwork network)
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
            RudpPacket packet = tuple.Param2;
            packet.Ident = target.Ident;

            int sentBytes = SendtoAddress(network, target, packet);

            network.Core.ServiceBandwidth[tuple.Param1].OutPerSec += sentBytes;
        }

        public static int SendtoAddress(DhtNetwork network, RudpAddress target, RudpPacket packet)
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
                packet.ToAddress = target.Address;

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
