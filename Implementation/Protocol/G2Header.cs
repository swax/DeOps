/********************************************************************************

	De-Ops: Decentralized Operations
	Copyright (C) 2006 John Marshall Group, Inc.

	By contributing code you grant John Marshall Group an unlimited, non-exclusive
	license to your contribution.

	For support, questions, commercial use, etc...
	E-Mail: swabby@c0re.net

********************************************************************************/

using System;
using System.Net;
using System.Net.Sockets;

using DeOps.Implementation.Transport;
using DeOps.Implementation.Protocol.Net;

namespace DeOps.Implementation.Protocol
{
    internal class RootPacket
    {
        internal const byte Network = 0x10;
        internal const byte Comm = 0x20;
        internal const byte CryptPadding = 0x30;
        internal const byte File = 0x40;
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
	}

	internal enum DirectionType {In, Out};
	
	internal class PacketLogEntry
	{
        internal TransportProtocol Protocol;
		internal DirectionType Direction;
        internal DhtAddress    Address;
		internal byte[]        Data;

        internal PacketLogEntry(TransportProtocol protocol, DirectionType direction, DhtAddress address, byte[] data)
		{
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
