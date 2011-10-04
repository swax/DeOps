using System;
using System.Net;
using System.Net.Sockets;

using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Transport;


namespace DeOps.Implementation.Protocol
{
    internal class RootPacket
    {
        internal const byte Network = 0x10;
        internal const byte Comm = 0x20;
        internal const byte Padding = 0x30;
        internal const byte Tunnel = 0x40;
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
