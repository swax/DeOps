using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;

using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;


namespace DeOps.Implementation.Dht
{
	/// <summary>
	/// Summary description for DhtBucket.
	/// </summary>
	internal class DhtBucket
	{
        internal Dictionary<ulong, List<DhtContact>> ContactMap = new Dictionary<ulong, List<DhtContact>>();

        internal List<DhtContact> ContactList = new List<DhtContact>();

		internal int  Depth;
		internal bool Last;

		DhtRouting Routing;

		// dont refresh immediately because searches/self search will take care of it
		internal DateTime NextRefresh;


		internal DhtBucket(DhtRouting routing, int depth, bool lastBucket)
		{
			Depth      = depth;
			Routing    = routing;
			Last = lastBucket;

            NextRefresh = Routing.Core.TimeNow.AddMinutes(15);
		}
	
		internal UInt64 GetRandomBucketID()
		{
			UInt64 randomID = Utilities.StrongRandUInt64(Routing.Core.StrongRndGen);

			UInt64 localID = Routing.Network.Local.UserID;

            // ex.. Dht id 00000
            // depth 0, 1...
            // depth 1, 01...
            // depth 2, 001...
            // depth 3 (last), 000...

			// set depth number of bits to same as LocalDhtID
			for(int x = 0; x < Depth; x++)
				Utilities.SetBit(ref randomID, x, Utilities.GetBit(localID, x)); 

            // if this is the last bucket, keep id the same in the final bit
            bool finalBit = Utilities.GetBit(localID, Depth);
            Utilities.SetBit(ref randomID, Depth, Last ? finalBit : !finalBit);

			return randomID;
		}

		internal bool Add(DhtContact newContact)
		{
            // duplicate already checked for in routing.add

            // check if bucket full
            if (ContactList.Count >= Routing.ContactsPerBucket)
                return false;

            // else good to go
            ContactList.Add(newContact);
            Routing.ContactMap[newContact.RoutingID] = newContact;

            return true;
		}
	}

	internal class DhtContact : DhtAddress
	{
        new const int PAYLOAD_SIZE = 14;

        const byte Packet_IP = 0x10;
        const byte Packet_Server = 0x20;
        const byte Packet_Client = 0x30;


        internal ushort    TcpPort;

        internal DateTime  LastSeen;
        internal int       Attempts;
        internal DateTime  NextTry;
        internal DateTime  NextTryIP;
        internal DateTime  NextTryProxy; // required because attempts more spaced out
        internal ushort    Ident;

        internal DhtContact() { }

        internal DhtContact(UInt64 user, ushort client, IPAddress address, ushort tcpPort, ushort udpPort)
		{
			UserID     = user;
			ClientID  = client;
			IP   = address;
			TcpPort   = tcpPort;
			UdpPort   = udpPort;
		}

        internal DhtContact(DhtAddress address)
        {
            UserID = address.UserID;
            ClientID = address.ClientID;
            IP = address.IP;
            UdpPort = address.UdpPort;
        }


        // used to add global proxies
        internal DhtContact(DhtSource opHost, IPAddress opIP, TunnelAddress client, DhtAddress server)
        {
            UserID = opHost.UserID;
            ClientID = opHost.ClientID;
            IP = opIP;
            TcpPort = opHost.TcpPort;
            UdpPort = opHost.UdpPort;

            TunnelServer = new DhtAddress(server.UserID, server.ClientID, server.IP, server.UdpPort);
            TunnelClient = client;
        }

        internal DhtContact(DhtSource source, IPAddress ip)
        {
            UserID = source.UserID;
            ClientID = source.ClientID;
            IP = ip;
            TcpPort = source.TcpPort;
            UdpPort = source.UdpPort;
        }

        public override string ToString()
		{
            return IP.ToString() + ":" + TcpPort.ToString() + ":" + UdpPort.ToString(); ;
		}

        internal new void WritePacket(G2Protocol protocol, G2Frame root, byte name)
        {
            byte[] payload = new byte[PAYLOAD_SIZE];

            BitConverter.GetBytes(UserID).CopyTo(payload, 0);
            BitConverter.GetBytes(ClientID).CopyTo(payload, 8);
            BitConverter.GetBytes(UdpPort).CopyTo(payload, 10);
            BitConverter.GetBytes(TcpPort).CopyTo(payload, 12);

            G2Frame address = protocol.WritePacket(root, name, payload);

            protocol.WritePacket(address, Packet_IP, IP.GetAddressBytes());

            if (TunnelServer != null)
                TunnelServer.WritePacket(protocol, address, Packet_Server);

            if (TunnelClient != null)
                protocol.WritePacket(address, Packet_Client, TunnelClient.ToBytes());
        }

        internal byte[] Encode(G2Protocol protocol, byte name)
        {
            lock (protocol.WriteSection)
            {
                WritePacket(protocol, null, name);

                return protocol.WriteFinish();
            }
        }


        internal static new DhtContact ReadPacket(G2Header root)
        {
            // read payload
            DhtContact contact = new DhtContact();

            contact.UserID = BitConverter.ToUInt64(root.Data, root.PayloadPos);
            contact.ClientID = BitConverter.ToUInt16(root.Data, root.PayloadPos + 8);
            contact.UdpPort = BitConverter.ToUInt16(root.Data, root.PayloadPos + 10);
            contact.TcpPort = BitConverter.ToUInt16(root.Data, root.PayloadPos + 12);

            // read packets
            G2Protocol.ResetPacket(root);

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_IP:
                        contact.IP = new IPAddress(Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize));
                        break;

                    case Packet_Server:
                        contact.TunnelServer = DhtAddress.ReadPacket(child);
                        break;

                    case Packet_Client:
                        contact.TunnelClient = TunnelAddress.FromBytes(child.Data, child.PayloadPos);
                        break;
                }
            }

            return contact;
        }

        internal void Alive(DateTime latest)
        {
            if (latest > LastSeen)
            {
                LastSeen = latest;
                Attempts = 0;
            }
        }
    }
}
