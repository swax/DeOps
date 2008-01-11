using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;

using RiseOp.Implementation.Protocol.Net;

namespace RiseOp.Implementation.Dht
{
	/// <summary>
	/// Summary description for DhtBucket.
	/// </summary>
	internal class DhtBucket
	{
		internal static int MAX_CONTACTS = 16;

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

			UInt64 localID = Routing.Core.LocalDhtID;

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

		internal bool Add(DhtContact newContact, bool merge)
		{
            // if already here update last seen
            foreach (DhtContact contact in ContactList)
                if (contact.Equals(newContact))
                {
                    if (newContact.LastSeen > contact.LastSeen)
                    {
                        contact.LastSeen = newContact.LastSeen;
                        contact.Attempts = 0;
                    }

                    return true;
                }

            // check if bucket full
            if (ContactList.Count >= MAX_CONTACTS)
                return false;

            // else good to go
            ContactList.Add(newContact);

            
            // signal data replication
            if (!merge)
            {
                Routing.Network.Store.RoutingAdd(newContact);

                if (!Routing.Network.IsGlobal)
                    Routing.Core.Links.RoutingUpdate(newContact);
            }

            return true;
		}
	}

	internal class DhtContact
	{
        const int BYTE_SIZE = 18;

		internal UInt64    DhtID;
        internal ushort    ClientID;
        internal IPAddress Address;
        internal ushort    TcpPort;
        internal ushort    UdpPort;
        internal DateTime  LastSeen;
        internal DateTime  NextTry;
        internal int       Attempts;
        internal DateTime  NextTryProxy; // required because attempts more spaced out

        internal DhtContact(UInt64 Dhtid, ushort clientID, IPAddress address, ushort tcpPort, ushort udpPort, DateTime lastSeen)
		{
			DhtID     = Dhtid;
			ClientID  = clientID;
			Address   = address;
			TcpPort   = tcpPort;
			UdpPort   = udpPort;
			LastSeen  = lastSeen;
			NextTry   = new DateTime(0);
			NextTryProxy = new DateTime(0);
		}

        internal DhtContact(DhtSource Dht, IPAddress address, DateTime lastSeen)
        {
            DhtID = Dht.DhtID;
            ClientID = Dht.ClientID;
            Address = address;
            TcpPort = Dht.TcpPort;
            UdpPort = Dht.UdpPort;
            LastSeen = lastSeen;
            NextTry = new DateTime(0);
            NextTryProxy = new DateTime(0);
        }

        internal bool Equals(DhtContact compare)
		{
			if( DhtID    == compare.DhtID    &&
				ClientID == compare.ClientID && 
				TcpPort  == compare.TcpPort  &&
				UdpPort  == compare.UdpPort  &&
				Address.Equals(compare.Address))
				return true;

			return false;
		}

        internal DhtAddress ToDhtAddress()
        {
            return new DhtAddress(DhtID, Address, UdpPort);
        }

        public override string ToString()
		{
			return Address.ToString() + ":" + TcpPort.ToString() + ":" + UdpPort.ToString();
		}

        internal byte[] ToBytes()
        {
            byte[] buffer = new byte[BYTE_SIZE];

            BitConverter.GetBytes(DhtID).CopyTo(buffer, 0);
            BitConverter.GetBytes(ClientID).CopyTo(buffer, 8);
            Address.GetAddressBytes().CopyTo(buffer, 10);
            BitConverter.GetBytes(TcpPort).CopyTo(buffer, 14);
            BitConverter.GetBytes(UdpPort).CopyTo(buffer, 16);

            return buffer;
        }

        internal static DhtContact FromBytes(byte[] data, int pos)
        {
            UInt64      Dhtid       = BitConverter.ToUInt64(data, pos);
            ushort      clientID    = BitConverter.ToUInt16(data, pos + 8);
            IPAddress   address     = Utilities.BytestoIP(data, pos + 10);
            ushort      tcpport     = BitConverter.ToUInt16(data, pos + 14);
            ushort      udpport     = BitConverter.ToUInt16(data, pos + 16);

            DhtContact contact = new DhtContact(Dhtid, clientID, address, tcpport, udpport, new DateTime(0));

            return contact;
        }

        internal static byte[] ToByteList(List<DhtContact> list)
        {
            if (list == null || list.Count == 0)
                return null;

            byte[] result = new byte[BYTE_SIZE * list.Count];

            int offset = 0;
            foreach (DhtContact contact in list)
            {
                contact.ToBytes().CopyTo(result, offset);
                offset += BYTE_SIZE;
            }

            return result;
        }

        internal static List<DhtContact> FromByteList(byte[] data, int pos, int size)
        {
            if (data == null || (size % BYTE_SIZE != 0))
                return null;

            List<DhtContact> results = new List<DhtContact>();

            for (int i = pos; i < pos + size; i += BYTE_SIZE)
                results.Add(DhtContact.FromBytes(data, i));

            return results;
        }
    }
}
