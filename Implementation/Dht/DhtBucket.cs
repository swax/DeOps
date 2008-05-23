using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;

using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;


namespace RiseOp.Implementation.Dht
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
			byte[] eightBytes = new byte[8];
			Routing.Core.StrongRndGen.GetBytes(eightBytes);
			UInt64 randomID = BitConverter.ToUInt64(eightBytes, 0);

			UInt64 localID = Routing.Network.LocalUserID;

            // ex.. Dht id 00000
            // depth 0, 1...
            // depth 1, 01...
            // depth 2, 001...
            // depth 3 (last), 000...

			// set depth number of bits to same as LocalDhtID
			for(int x = 0; x < Depth; x++)
				Utilities.SetBit(ref randomID, x, Utilities.GetBit(localID, x)); 

			if( !Last )
				Utilities.SetBit(ref randomID, Depth, Utilities.GetBit(localID, Depth) ^ 0x1);

			return randomID;
		}

		internal bool Add(DhtContact newContact)
		{
            // if already here update last seen
            foreach (DhtContact contact in ContactList)
                if (contact.Equals(newContact))
                    return true;

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
        
        // RoutingID: slightly mod the user's lower bits so that dhtid is unique (max 64k uniques)
        // needed so that 1000 of the same user are online, the routing table still works
        // high/low/xor cache area is still fair and balanced
        internal ulong RoutingID
        {
            get { return UserID ^ ClientID; }
        }

        new const int PAYLOAD_SIZE = 14;

        const byte Packet_IP = 0x01;
        const byte Packet_Global = 0x02;


        internal ushort    TcpPort;

        internal DateTime  LastSeen;
        internal int       Attempts;
        internal DateTime NextTry;
        internal DateTime  NextTryProxy; // required because attempts more spaced out


        internal DhtContact() { }

        internal DhtContact(UInt64 user, ushort client, IPAddress address, ushort tcpPort, ushort udpPort, DateTime lastSeen)
		{
			UserID     = user;
			ClientID  = client;
			IP   = address;
			TcpPort   = tcpPort;
			UdpPort   = udpPort;
			LastSeen  = lastSeen;
		}

        // used to add global proxies
        internal DhtContact(DhtAddress address, DateTime lastSeen)
        {
            UserID = address.UserID;
            ClientID = address.ClientID;
            IP = address.IP;
            TcpPort = 0;
            UdpPort = address.UdpPort;
            LastSeen = lastSeen;
            GlobalProxy = address.GlobalProxy;
        }

        internal DhtContact(DhtSource Dht, IPAddress address, DateTime lastSeen)
        {
            UserID = Dht.UserID;
            ClientID = Dht.ClientID;
            IP = address;
            TcpPort = Dht.TcpPort;
            UdpPort = Dht.UdpPort;
            LastSeen = lastSeen;
        }

        internal bool Equals(DhtContact compare)
		{
			if( UserID    == compare.UserID    &&
				ClientID == compare.ClientID && 
				TcpPort  == compare.TcpPort  &&
				UdpPort  == compare.UdpPort  &&
				IP.Equals(compare.IP) &&
                GlobalProxy == compare.GlobalProxy)
				return true;

			return false;
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

            if (GlobalProxy > 0)
                protocol.WritePacket(address, Packet_Global, BitConverter.GetBytes(GlobalProxy));
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

                    case Packet_Global:
                        contact.GlobalProxy = BitConverter.ToUInt64(child.Data, child.PayloadPos);
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
