using System;
using System.Net;
using System.Net.Sockets;

using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Transport;


namespace RiseOp.Implementation.Protocol
{
    internal class RootPacket
    {
        internal const byte Network = 0x10;
        internal const byte Comm = 0x20;
        internal const byte Padding = 0x30;
        internal const byte Tunnel = 0x40;
    }

	internal class G2Header
	{
		internal byte[] Data;

		internal int    PacketPos;
		internal int    PacketSize;

        internal byte   Name;
	
		internal bool   HasChildren;

		internal int    InternalPos;
		internal int    InternalSize;

		internal int    PayloadPos;
		internal int    PayloadSize;

		internal int    NextBytePos;
		internal int    NextBytesLeft;

		internal G2Header(byte[] data)
		{
			Data = data;
		}
	}

	internal class G2Packet
	{
		internal G2Packet()
		{
		}

		internal virtual byte[] Encode(G2Protocol protocol)
		{
            return null;
		}
	}

	internal class G2ReceivedPacket
	{
		internal G2Header    Root;
		internal DhtAddress	 Source;
		internal TcpConnect  Tcp;

        internal bool Tunneled { get { return Source.TunnelClient != null; } }
        internal bool ReceivedTcp { get { return Tcp != null; } }
        internal bool ReceivedUdp { get { return Tcp == null; } }
    }

	internal enum DirectionType {In, Out};
	
	internal class PacketLogEntry
	{
        internal DateTime Time;
        internal TransportProtocol Protocol;
		internal DirectionType Direction;
        internal DhtAddress    Address;
		internal byte[]        Data;

        internal PacketLogEntry(DateTime time, TransportProtocol protocol, DirectionType direction, DhtAddress address, byte[] data)
		{
            Time = time;
			Protocol  = protocol;
			Direction = direction;
			Address   = address;
			Data      = data;
		}

        public override string ToString()
        {
            return Address.ToString() + " " + Protocol.ToString() + " " + Direction.ToString();
        }
	}
}
