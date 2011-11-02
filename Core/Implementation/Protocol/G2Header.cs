using System;
using System.Net;
using System.Net.Sockets;

using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Transport;


namespace DeOps.Implementation.Protocol
{
    public class RootPacket
    {
        public const byte Network = 0x10;
        public const byte Comm = 0x20;
        public const byte Padding = 0x30;
        public const byte Tunnel = 0x40;
    }

	public class G2ReceivedPacket
	{
		public G2Header    Root;
		public DhtAddress	 Source;
		public TcpConnect  Tcp;

        public bool Tunneled { get { return Source.TunnelClient != null; } }
        public bool ReceivedTcp { get { return Tcp != null; } }
        public bool ReceivedUdp { get { return Tcp == null; } }
    }

	public enum DirectionType {In, Out};
	
	public class PacketLogEntry
	{
        public DateTime Time;
        public TransportProtocol Protocol;
		public DirectionType Direction;
        public DhtAddress    Address;
		public byte[]        Data;

        public PacketLogEntry(DateTime time, TransportProtocol protocol, DirectionType direction, DhtAddress address, byte[] data)
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
