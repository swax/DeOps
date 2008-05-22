using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Transport;
using RiseOp.Implementation.Protocol;


namespace RiseOp.Implementation.Protocol.Net
{
    internal class DhtClient
    {
        internal ulong userID;
        internal ushort ClientID;

        internal DhtClient()
        {
        }

        internal DhtClient(DhtClient copy)
        {
            userID = copy.userID;
            ClientID = copy.ClientID;
        }

        internal DhtClient(ulong dht, ushort client)
        {
            userID = dht;
            ClientID = client;
        }

        public override bool Equals(object obj)
        {
            DhtClient compare = obj as DhtClient;

            if (compare == null)
                return false ;

            return userID == compare.userID && ClientID == compare.ClientID;
        }

        public override int GetHashCode()
        {
            return userID.GetHashCode() ^ ClientID.GetHashCode();
        }
    }

    internal class DhtSource : DhtClient
	{
        internal const int BYTE_SIZE = 15;

		internal ushort TcpPort;
		internal ushort UdpPort;
        internal FirewallType Firewall;

        internal byte[] ToBytes()
        {
            byte[] bytes = new byte[BYTE_SIZE];

            BitConverter.GetBytes(userID).CopyTo(bytes, 0);
            BitConverter.GetBytes(ClientID).CopyTo(bytes, 8);
            BitConverter.GetBytes(TcpPort).CopyTo(bytes, 10);
            BitConverter.GetBytes(UdpPort).CopyTo(bytes, 12);
            bytes[14] = (byte)Firewall;

            return bytes;
        }

        internal static DhtSource FromBytes(byte[] data, int pos)
        {
            DhtSource source = new DhtSource();

            source.userID    = BitConverter.ToUInt64(data, pos);
            source.ClientID = BitConverter.ToUInt16(data, pos + 8);
            source.TcpPort  = BitConverter.ToUInt16(data, pos + 10);
            source.UdpPort  = BitConverter.ToUInt16(data, pos + 12);
            source.Firewall = (FirewallType)data[pos + 14];

            return source;
        }

        DhtClient GetDhtClient()
        {
            return new DhtClient(userID, ClientID);
        }
    }

    internal class DhtAddress : DhtClient
    {
        internal const int BYTE_SIZE = 16;

        internal IPAddress IP;
        internal ushort    UdpPort;

        internal DhtAddress()
        {
        }

        internal DhtAddress(ulong user, ushort client, IPAddress ip, ushort port)
        {
            userID   = user;
            ClientID = client;
            IP       = ip;
            UdpPort  = port;
        }

        internal DhtAddress(ulong user, IPEndPoint host)
        {
            userID = user;
            IP    = host.Address;
            UdpPort  = (ushort)host.Port;
        }

        internal DhtAddress(IPAddress ip, DhtSource source)
        {
            userID = source.userID;
            ClientID = source.ClientID;
            IP = ip;
            UdpPort = source.UdpPort;
        }

        internal IPEndPoint ToEndPoint()
        {
            return new IPEndPoint(IP, UdpPort);
        }

        internal byte[] ToBytes()
        {
            byte[] bytes = new byte[BYTE_SIZE];

            BitConverter.GetBytes(userID).CopyTo(bytes, 0);
            BitConverter.GetBytes(ClientID).CopyTo(bytes, 8);
            IP.GetAddressBytes().CopyTo(bytes, 10);
            BitConverter.GetBytes(UdpPort).CopyTo(bytes, 14);

            return bytes;
        }

        internal static DhtAddress FromBytes(byte[] data, int pos)
        {
            DhtAddress address = new DhtAddress();
            
            address.userID = BitConverter.ToUInt64(data, pos);
            address.ClientID = BitConverter.ToUInt16(data, pos + 8);
            address.IP = Utilities.BytestoIP(data, pos + 10);
            address.UdpPort = BitConverter.ToUInt16(data, pos + 14);

            return address;
        }

        public override bool Equals(object obj)
        {
            DhtAddress check = obj as DhtAddress;

            if (check == null)
                return false;

            if (userID == check.userID && ClientID == check.ClientID && IP.Equals(check.IP) && UdpPort == check.UdpPort)
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return userID.GetHashCode() ^ ClientID.GetHashCode() ^ IP.GetHashCode() ^ UdpPort.GetHashCode();
        }

        public override string  ToString()
        {
            return IP.ToString() + ":" + ClientID.ToString() + ":" + UdpPort.ToString() + ":" + Utilities.IDtoBin(userID).Substring(0, 10);
        }


        internal static byte[] ToByteList(List<DhtAddress> list)
        {
            if (list == null || list.Count == 0)
                return null;

            byte[] result = new byte[BYTE_SIZE * list.Count];

            int offset = 0;
            foreach (DhtAddress address in list)
            {
                address.ToBytes().CopyTo(result, offset);
                offset += BYTE_SIZE;
            }

            return result;
        }

        internal static List<DhtAddress> FromByteList(byte[] data, int pos, int size)
        {
            if (data == null || (size % BYTE_SIZE != 0))
                return null;

            List<DhtAddress> results = new List<DhtAddress>();

            for (int i = pos; i < pos + size; i += BYTE_SIZE)
                results.Add(DhtAddress.FromBytes(data, i));

            return results;
        }
    }

    internal class NetworkPacket : G2Packet
	{
        const byte Packet_SourceID = 0x01;
        const byte Packet_ClientID = 0x02;
        const byte Packet_To       = 0x03;
        const byte Packet_From     = 0x04;

        // internal packet types
        internal const byte SearchRequest   = 0x10;
        internal const byte SearchAck       = 0x20;
        internal const byte StoreRequest    = 0x30;
        internal const byte Ping            = 0x40;
        internal const byte Pong            = 0x50;
        internal const byte Bye             = 0x60;
        internal const byte ProxyRequest    = 0x70;
        internal const byte ProxyAck        = 0x80;
        internal const byte CrawlRequest    = 0x90;
        internal const byte CrawlAck        = 0xA0;

        
        internal ulong      SourceID;
        internal ushort     ClientID;
        internal DhtAddress ToAddress;
        internal DhtAddress FromAddress;

        internal byte[]     InternalData;


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame gn = protocol.WritePacket(null, RootPacket.Network, InternalData);

                protocol.WritePacket(gn, Packet_SourceID, BitConverter.GetBytes(SourceID));
                protocol.WritePacket(gn, Packet_ClientID, BitConverter.GetBytes(ClientID));

                if (ToAddress != null)
                    protocol.WritePacket(gn, Packet_To, ToAddress.ToBytes());

                if (FromAddress != null)
                    protocol.WritePacket(gn, Packet_From, FromAddress.ToBytes());


                return protocol.WriteFinish();
            }
        }

		internal static NetworkPacket Decode(G2Header root)
		{

            NetworkPacket gn = new NetworkPacket();

            if (G2Protocol.ReadPayload(root))
                gn.InternalData = Utilities.ExtractBytes(root.Data, root.PayloadPos, root.PayloadSize);

            G2Protocol.ResetPacket(root);


			G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_SourceID:
                        gn.SourceID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_ClientID:
                        gn.ClientID = BitConverter.ToUInt16(child.Data, child.PayloadPos);
                        break;

                    case Packet_To:
                        gn.ToAddress = DhtAddress.FromBytes(child.Data, child.PayloadPos);
                        break;

                    case Packet_From:
                        gn.FromAddress = DhtAddress.FromBytes(child.Data, child.PayloadPos);
                        break;
                }
            }

			return gn;
		}
	}

    internal class SearchReq : NetworkPacket
	{
        const byte Packet_Source     = 0x10;
        const byte Packet_Nodes      = 0x20;
        const byte Packet_SearchID   = 0x30;
        const byte Packet_Target     = 0x40;
        const byte Packet_Service    = 0x50;
        const byte Packet_DataType   = 0x60;
        const byte Packet_Parameters = 0x70;
        const byte Packet_EndSearch  = 0x80;


		internal DhtSource Source = new DhtSource();
		internal bool      Nodes  = true;
        internal uint      SearchID;
		internal UInt64    TargetID;
        internal uint      Service;
        internal uint      DataType;
        internal byte[]    Parameters;
        internal bool      EndProxySearch;

		internal override byte[] Encode(G2Protocol protocol)
		{
            lock (protocol.WriteSection)
            {
                G2Frame req = protocol.WritePacket(null, NetworkPacket.SearchRequest, null);

                protocol.WritePacket(req, Packet_Source,    Source.ToBytes());
                protocol.WritePacket(req, Packet_Nodes, BitConverter.GetBytes(Nodes));
                protocol.WritePacket(req, Packet_SearchID,  BitConverter.GetBytes(SearchID));
                protocol.WritePacket(req, Packet_Target,    BitConverter.GetBytes(TargetID));
                protocol.WritePacket(req, Packet_Service, CompactNum.GetBytes(Service));
                protocol.WritePacket(req, Packet_DataType, CompactNum.GetBytes(DataType));
                protocol.WritePacket(req, Packet_Parameters, Parameters);

                if (EndProxySearch)
                    protocol.WritePacket(req, Packet_EndSearch, BitConverter.GetBytes(true));

                InternalData = protocol.WriteFinish();

                return base.Encode(protocol);
            }
		}

		internal static SearchReq Decode(G2ReceivedPacket packet)
		{
            SearchReq req = new SearchReq();

			G2Header child = new G2Header(packet.Root.Data);
	
			while( G2Protocol.ReadNextChild(packet.Root, child) == G2ReadResult.PACKET_GOOD )
			{
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Source:
                        req.Source = DhtSource.FromBytes(child.Data, child.PayloadPos);
                        break;

                    case Packet_SearchID:
                        req.SearchID = BitConverter.ToUInt32(child.Data, child.PayloadPos); 
                        break;

                    case Packet_Target:
                        req.TargetID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Service:
                        req.Service = CompactNum.ToUInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_DataType:
                        req.DataType = CompactNum.ToUInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Parameters:
                        req.Parameters = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Nodes:
                        req.Nodes = BitConverter.ToBoolean(child.Data, child.PayloadPos);
                        break;

                    case Packet_EndSearch:
                        req.EndProxySearch = true;
                        break;
                }      
			}

            return req;
		}	
	}

    internal class SearchAck : NetworkPacket
	{
        const byte Packet_Source   = 0x10;
        const byte Packet_SearchID = 0x20;
        const byte Packet_Proxied  = 0x30;
        const byte Packet_Contacts = 0x40;
        const byte Packet_Values   = 0x50;
        const byte Packet_Service = 0x60;


		internal DhtSource Source = new DhtSource();
		internal uint SearchID;
		internal bool Proxied;
        internal uint Service;
        internal List<DhtContact> ContactList = new List<DhtContact>();
        internal List<byte[]> ValueList = new List<byte[]>();

		internal override byte[] Encode(G2Protocol protocol)
		{
            lock (protocol.WriteSection)
            {
                G2Frame ack = protocol.WritePacket(null, NetworkPacket.SearchAck, null);

                protocol.WritePacket(ack, Packet_Source, Source.ToBytes());

                protocol.WritePacket(ack, Packet_SearchID, BitConverter.GetBytes(SearchID));
                protocol.WritePacket(ack, Packet_Service, CompactNum.GetBytes(Service));

                if (Proxied)
                    protocol.WritePacket(ack, Packet_Proxied, null);

                if (ContactList.Count > 0)
                    protocol.WritePacket(ack, Packet_Contacts, DhtContact.ToByteList(ContactList));

                foreach (byte[] value in ValueList)
                    protocol.WritePacket(ack, Packet_Values, value);

                InternalData = protocol.WriteFinish();

                return base.Encode(protocol);
            }
		}

		internal static SearchAck Decode(G2ReceivedPacket packet)
		{
            SearchAck ack = new SearchAck();

			G2Header child = new G2Header(packet.Root.Data);
	
			while( G2Protocol.ReadNextChild(packet.Root, child) == G2ReadResult.PACKET_GOOD )
			{
                if (child.Name == Packet_Proxied)
                {
                    ack.Proxied = true;
                    continue;
                }

                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Source:
                        ack.Source = DhtSource.FromBytes(child.Data, child.PayloadPos);
                        break;

                    case Packet_SearchID:
                        ack.SearchID = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Contacts:
                        ack.ContactList = DhtContact.FromByteList(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Service:
                        ack.Service = CompactNum.ToUInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Values:
                        ack.ValueList.Add(Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize));
                        break;
                }                      
            }

			return ack;
		}	
	}

    internal class StoreReq : NetworkPacket
    {
        const byte Packet_Source = 0x10;
        const byte Packet_Key = 0x20;
        const byte Packet_Service = 0x30;
        const byte Packet_DataType = 0x40;
        const byte Packet_Data = 0x50;
        const byte Packet_TTL = 0x60;


        internal DhtSource  Source = new DhtSource();
        internal UInt64 Key;
        internal uint Service;
        internal uint DataType;
        internal byte[] Data;
        internal ushort TTL = ushort.MaxValue;


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame req = protocol.WritePacket(null, NetworkPacket.StoreRequest, null);

                protocol.WritePacket(req, Packet_Source, Source.ToBytes());

                protocol.WritePacket(req, Packet_Key, BitConverter.GetBytes(Key));
                protocol.WritePacket(req, Packet_Service, CompactNum.GetBytes(Service));
                protocol.WritePacket(req, Packet_DataType, CompactNum.GetBytes(DataType));
                protocol.WritePacket(req, Packet_Data, Data);

                protocol.WritePacket(req, Packet_TTL, BitConverter.GetBytes(TTL));

                InternalData = protocol.WriteFinish();

                return base.Encode(protocol);
            }
        }

        internal static StoreReq Decode(G2ReceivedPacket packet)
        {
            StoreReq req = new StoreReq();

            G2Header child = new G2Header(packet.Root.Data);

            while (G2Protocol.ReadNextChild(packet.Root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Source:
                        req.Source = DhtSource.FromBytes(child.Data, child.PayloadPos);
                        break;

                    case Packet_Key:
                        req.Key = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Service:
                        req.Service = CompactNum.ToUInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_DataType:
                        req.DataType = CompactNum.ToUInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Data:
                        req.Data = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_TTL:
                        req.TTL = BitConverter.ToUInt16(child.Data, child.PayloadPos);
                        break;
                }
            }

            return req;
        }
    }

    internal class Ping : NetworkPacket
	{
        const byte Packet_Source   = 0x10;
        const byte Packet_RemoteIP = 0x20;


        internal DhtSource Source;
        internal IPAddress RemoteIP;


		internal override byte[] Encode(G2Protocol protocol)
		{
            lock (protocol.WriteSection)
            {
                // ping packet
                G2Frame pi = protocol.WritePacket(null, NetworkPacket.Ping, null);

                if(Source != null)
                    protocol.WritePacket(pi, Packet_Source, Source.ToBytes());

                if (RemoteIP != null)
                    protocol.WritePacket(pi, Packet_RemoteIP, RemoteIP.GetAddressBytes());

                // network packet
                InternalData = protocol.WriteFinish();

                return base.Encode(protocol);
            }
		}

		internal static Ping Decode(G2ReceivedPacket packet)
		{
			Ping pi = new Ping();

			G2Header child = new G2Header(packet.Root.Data);

			while( G2Protocol.ReadNextChild(packet.Root, child) == G2ReadResult.PACKET_GOOD )
			{
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Source:
                        pi.Source = DhtSource.FromBytes(child.Data, child.PayloadPos);
                        break;

                    case Packet_RemoteIP:
                        pi.RemoteIP = Utilities.BytestoIP(child.Data, child.PayloadPos);
                        break;
                }
			}

			return pi;
		}
	}

    internal class Pong : NetworkPacket
	{
        const byte Packet_Source   = 0x10;
        const byte Packet_RemoteIP = 0x20;
        const byte Packet_Direct   = 0x30;

		internal DhtSource Source;
		internal IPAddress RemoteIP;
        internal bool Direct;


		internal override byte[] Encode(G2Protocol protocol)
		{
            lock (protocol.WriteSection)
            {
                G2Frame po = protocol.WritePacket(null, NetworkPacket.Pong, null);

                if(Source != null)
                    protocol.WritePacket(po, Packet_Source, Source.ToBytes());

                if (RemoteIP != null)
                    protocol.WritePacket(po, Packet_RemoteIP, RemoteIP.GetAddressBytes());

                if(Direct)
                    protocol.WritePacket(po, Packet_Direct, null);

                // network packet
                InternalData = protocol.WriteFinish();

                return base.Encode(protocol);
            }
		}

		internal static Pong Decode(G2ReceivedPacket packet)
		{
			Pong po = new Pong();

			G2Header child = new G2Header(packet.Root.Data);

			while( G2Protocol.ReadNextChild(packet.Root, child) == G2ReadResult.PACKET_GOOD )
			{
                if (!G2Protocol.ReadPayload(child))
                {
                    if(child.Name == Packet_Direct)
                        po.Direct = true;

                    continue;
                }

                switch (child.Name)
                {
                    case Packet_Source:
                        po.Source = DhtSource.FromBytes(child.Data, child.PayloadPos);
                        break;

                    case Packet_RemoteIP:
                        po.RemoteIP = Utilities.BytestoIP(child.Data, child.PayloadPos);
                        break;
                }			
			}

			return po;
		}
	}

	internal class Bye : NetworkPacket
	{
        const byte Packet_Source    = 0x10;
        const byte Packet_Contacts  = 0x20;
        const byte Packet_Message   = 0x30;
        const byte Packet_Reconnect = 0x40;

		internal UInt64     SenderID;
        internal List<DhtContact> ContactList = new List<DhtContact>();
		internal string     Message;
        internal bool       Reconnect;


		internal override byte[] Encode(G2Protocol protocol)
		{
            lock (protocol.WriteSection)
            {
                G2Frame bye = protocol.WritePacket(null, NetworkPacket.Bye, null);

                protocol.WritePacket(bye, Packet_Source, BitConverter.GetBytes(SenderID));

                if (ContactList.Count > 0)
                    protocol.WritePacket(bye, Packet_Contacts, DhtContact.ToByteList(ContactList));

                if (Message != null)
                    protocol.WritePacket(bye, Packet_Message, UTF8Encoding.UTF8.GetBytes(Message));

                if (Reconnect)
                    protocol.WritePacket(bye, Packet_Reconnect, null);

                // network packet
                InternalData = protocol.WriteFinish();

                return base.Encode(protocol);
            }
		}

		internal static Bye Decode(G2ReceivedPacket packet)
		{
			Bye bye = new Bye();

			G2Header child = new G2Header(packet.Root.Data);
	
			while( G2Protocol.ReadNextChild(packet.Root, child) == G2ReadResult.PACKET_GOOD )
			{
                if (!G2Protocol.ReadPayload(child))
                {
                    if (child.Name == Packet_Reconnect)
                        bye.Reconnect = true;

                    continue;
                }

                switch (child.Name)
                {
                    case Packet_Source:
                        bye.SenderID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Contacts:
                        bye.ContactList = DhtContact.FromByteList(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Message:
                        bye.Message = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
			}

			return bye;
		}
	}

    internal class ProxyReq : NetworkPacket
	{
        const byte Packet_Source  = 0x10;
        const byte Packet_Blocked = 0x20;
        const byte Packet_NAT = 0x30;


		internal UInt64    SenderID;
		internal ProxyType Type;


		internal override byte[] Encode(G2Protocol protocol)
		{
            lock (protocol.WriteSection)
            {
                G2Frame pr = protocol.WritePacket(null, NetworkPacket.ProxyRequest, null);

                protocol.WritePacket(pr, Packet_Source, BitConverter.GetBytes(SenderID));

                if (Type == ProxyType.ClientBlocked)
                    protocol.WritePacket(pr, Packet_Blocked, null);
                else if (Type == ProxyType.ClientNAT)
                    protocol.WritePacket(pr, Packet_NAT, null);

                // network packet
                InternalData = protocol.WriteFinish();

                return base.Encode(protocol);
            }
		}

		internal static ProxyReq Decode(G2ReceivedPacket packet)
		{
			ProxyReq pr = new ProxyReq();

			G2Header child = new G2Header(packet.Root.Data);

			while( G2Protocol.ReadNextChild(packet.Root, child) == G2ReadResult.PACKET_GOOD )
			{
                switch (child.Name)
                {
                    case Packet_Source:
                        if (G2Protocol.ReadPayload(child))
                            pr.SenderID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Blocked:
                        pr.Type = ProxyType.ClientBlocked;
                        break;

                    case Packet_NAT:
                        pr.Type = ProxyType.ClientNAT;
                        break;
                }
			}

			return pr;
		}
	}

    internal class ProxyAck : NetworkPacket
	{
        const byte Packet_Source = 0x10;
        const byte Packet_Accept = 0x20;
        const byte Packet_Contacts = 0x30;


		internal DhtSource Source = new DhtSource();
		internal bool      Accept;
        internal List<DhtContact> ContactList = new List<DhtContact>();


		internal override byte[] Encode(G2Protocol protocol)
		{
            lock (protocol.WriteSection)
            {
                G2Frame pa = protocol.WritePacket(null, NetworkPacket.ProxyAck, null);

                protocol.WritePacket(pa, Packet_Source, Source.ToBytes());

                if (Accept)
                    protocol.WritePacket(pa, Packet_Accept, null);

                if (ContactList.Count > 0)
                    protocol.WritePacket(pa, Packet_Contacts, DhtContact.ToByteList(ContactList));

                // network packet
                InternalData = protocol.WriteFinish();

                return base.Encode(protocol);
            }
		}

		internal static ProxyAck Decode(G2ReceivedPacket packet)
		{
			ProxyAck pa = new ProxyAck();

			G2Header child = new G2Header(packet.Root.Data);

			while( G2Protocol.ReadNextChild(packet.Root, child) == G2ReadResult.PACKET_GOOD )
			{
                switch (child.Name)
                {
                    case Packet_Source:
                        if (G2Protocol.ReadPayload(child))
                            pa.Source = DhtSource.FromBytes(child.Data, child.PayloadPos);
                        break;

                    case Packet_Accept:
                        pa.Accept = true;
                        break;

                    case Packet_Contacts:
                        if (G2Protocol.ReadPayload(child))
                            pa.ContactList = DhtContact.FromByteList(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
			}

			return pa;
		}
	}

    internal class CrawlReq : NetworkPacket
	{
        const byte Packet_Source = 0x10;
        const byte Packet_Target = 0x20;
        const byte Packet_From   = 0x30;


		internal DhtSource Source = new DhtSource();
		internal UInt64 TargetID;

		internal override byte[] Encode(G2Protocol protocol)
		{
            lock (protocol.WriteSection)
            {
                G2Frame crwlr = protocol.WritePacket(null, NetworkPacket.CrawlRequest, null);

                protocol.WritePacket(crwlr, Packet_Source, Source.ToBytes());
                protocol.WritePacket(crwlr, Packet_Target, BitConverter.GetBytes(TargetID));

                // network packet
                InternalData = protocol.WriteFinish();

                return base.Encode(protocol);
            }
		}

		internal static CrawlReq Decode(G2ReceivedPacket packet)
		{
			CrawlReq crwlr = new CrawlReq();

			G2Header child = new G2Header(packet.Root.Data);

			while( G2Protocol.ReadNextChild(packet.Root, child) == G2ReadResult.PACKET_GOOD )
			{
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Source:
                        crwlr.Source = DhtSource.FromBytes(child.Data, child.PayloadPos);
                        break;

                    case Packet_Target:
                        crwlr.TargetID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;
                }
			}

			return crwlr;
		}
	}

    internal class CrawlAck : NetworkPacket
	{
        const byte Packet_Source   = 0x10;
        const byte Packet_Version = 0x20;
        const byte Packet_Uptime = 0x30;
        const byte Packet_Depth = 0x40;
        const byte Packet_Proxies = 0x50;


		internal DhtSource	Source = new DhtSource();
		internal string		Version;
		internal int			Uptime;
		internal int			Depth;
        internal List<DhtContact> ProxyList = new List<DhtContact>();
		

		internal override byte[] Encode(G2Protocol protocol)
		{
            lock (protocol.WriteSection)
            {
                G2Frame crwla = protocol.WritePacket(null, NetworkPacket.CrawlAck, null);

                protocol.WritePacket(crwla, Packet_Source, Source.ToBytes());
                protocol.WritePacket(crwla, Packet_Version, UTF8Encoding.UTF8.GetBytes(Version));
                protocol.WritePacket(crwla, Packet_Uptime, BitConverter.GetBytes(Uptime));
                protocol.WritePacket(crwla, Packet_Depth, BitConverter.GetBytes(Depth));

                if (ProxyList.Count > 0)
                    protocol.WritePacket(crwla, Packet_Proxies, DhtContact.ToByteList(ProxyList));

                // network packet
                InternalData = protocol.WriteFinish();

                return base.Encode(protocol);
            }
		}

		internal static CrawlAck Decode(G2ReceivedPacket packet)
		{
			CrawlAck crwla = new CrawlAck();

			G2Header child = new G2Header(packet.Root.Data);

			while( G2Protocol.ReadNextChild(packet.Root, child) == G2ReadResult.PACKET_GOOD )
			{
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Source:
                        crwla.Source = DhtSource.FromBytes(child.Data, child.PayloadPos);
                        break;

                    case Packet_Version:
                        crwla.Version = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Uptime:
                        crwla.Uptime = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Depth:
                        crwla.Depth = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Proxies:
                        crwla.ProxyList = DhtContact.FromByteList(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }						
			}

			return crwla;
		}
	}


    internal class CryptPadding : G2Packet
    {
        internal byte[] Filler;


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                protocol.WritePacket(null, RootPacket.CryptPadding, Filler);
                return protocol.WriteFinish();
            }
        }

        internal static CryptPadding Decode(G2ReceivedPacket packet)
        {
            CryptPadding padding = new CryptPadding();
            return padding;
        }
    }

    /*internal class TestPacket : G2Packet
    {
        internal string Message;
        internal int    Num;


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame earth = protocol.WritePacket(null, "Earth", UTF8Encoding.UTF8.GetBytes("Our Home"));
                G2Frame america = protocol.WritePacket(earth, "America", UTF8Encoding.UTF8.GetBytes("Where I live"));
                G2Frame nh = protocol.WritePacket(america, "NH", UTF8Encoding.UTF8.GetBytes("Home"));
                protocol.WritePacket(nh, "Nashua", null);
                protocol.WritePacket(nh, "Concord", UTF8Encoding.UTF8.GetBytes("Capitol"));
                protocol.WritePacket(america, "Mass", UTF8Encoding.UTF8.GetBytes("Where I go to school"));
                G2Frame europe = protocol.WritePacket(earth, "Europe", UTF8Encoding.UTF8.GetBytes("Across the ocean"));
                protocol.WritePacket(europe, "London", UTF8Encoding.UTF8.GetBytes("in england"));
                protocol.WritePacket(europe, "Paris", UTF8Encoding.UTF8.GetBytes("in france"));

                return protocol.WriteFinish();
            }
        }

        internal static TestPacket Decode(G2ReceivedPacket packet)
        {
            TestPacket test = new TestPacket();

            G2Header child = new G2Header(packet.Root.Data);

            while( G2Protocol.ReadNextChild(packet.Root, child) == G2ReadResult.PACKET_GOOD )
            {
				
            }

            return test;
        }
    }*/


}
