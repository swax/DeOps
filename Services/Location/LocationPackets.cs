using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;

namespace RiseOp.Services.Location
{
    internal class LocPacket
    {
        internal const byte LocationData = 0x10;
        internal const byte CryptLoc = 0x20;
    }

    internal class LocationData : G2Packet
    {
        internal const int GLOBAL_TTL = 60;
        internal const int OP_TTL = 4;


        const byte Packet_Key = 0x10;
        const byte Packet_Source = 0x20;
        const byte Packet_Global = 0x30;
        const byte Packet_IP = 0x40;
        const byte Packet_Proxies = 0x50;
        const byte Packet_Place = 0x60;
        const byte Packet_Version = 0x70;
        const byte Packet_TTL = 0xA0;
        const byte Packet_GMTOffset = 0xB0;
        const byte Packet_Away = 0xC0;
        const byte Packet_AwayMsg = 0xD0;
        const byte Packet_Tag = 0xE0;

        internal byte[] Key;
        internal DhtSource Source;
        internal bool Global;
        internal IPAddress IP;
        internal List<DhtAddress> Proxies = new List<DhtAddress>();
        internal string Place = "";
        internal uint TTL;
        internal uint Version;
        internal List<PatchTag> Tags = new List<PatchTag>();

        internal int GmtOffset;
        internal bool Away;
        internal string AwayMessage = "";


        internal ulong UserID;

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame loc = protocol.WritePacket(null, LocPacket.LocationData, null);

                protocol.WritePacket(loc, Packet_Key, Key);
                protocol.WritePacket(loc, Packet_Source, Source.ToBytes());
                protocol.WritePacket(loc, Packet_Global, BitConverter.GetBytes(Global));
                protocol.WritePacket(loc, Packet_IP, IP.GetAddressBytes());
                protocol.WritePacket(loc, Packet_Proxies, DhtAddress.ToByteList(Proxies));
                protocol.WritePacket(loc, Packet_Place, UTF8Encoding.UTF8.GetBytes(Place));
                protocol.WritePacket(loc, Packet_TTL, BitConverter.GetBytes(TTL));
                protocol.WritePacket(loc, Packet_Version, CompactNum.GetBytes(Version));
                protocol.WritePacket(loc, Packet_GMTOffset, BitConverter.GetBytes(GmtOffset));
                protocol.WritePacket(loc, Packet_Away, BitConverter.GetBytes(Away));
                protocol.WritePacket(loc, Packet_AwayMsg, UTF8Encoding.UTF8.GetBytes(AwayMessage));
                    
                foreach(PatchTag tag in Tags)
                    protocol.WritePacket(loc, Packet_Tag, tag.ToBytes());

                return protocol.WriteFinish();
            }
        }

        internal static LocationData Decode(byte[] data)
        {
            G2Header root = new G2Header(data);

            G2Protocol.ReadPacket(root);

            if (root.Name != LocPacket.LocationData)
                return null;

            LocationData loc = new LocationData();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Key:
                        loc.Key = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        loc.UserID = Utilities.KeytoID(loc.Key);
                        break;

                    case Packet_Source:
                        loc.Source = DhtSource.FromBytes(child.Data, child.PayloadPos);
                        break;

                    case Packet_Global:
                        loc.Global = BitConverter.ToBoolean(child.Data, child.PayloadPos);
                        break;

                    case Packet_IP:
                        loc.IP = Utilities.BytestoIP(child.Data, child.PayloadPos);
                        break;

                    case Packet_Proxies:
                        loc.Proxies = DhtAddress.FromByteList(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Place:
                        loc.Place = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_TTL:
                        loc.TTL = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Version:
                        loc.Version = CompactNum.ToUInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_GMTOffset:
                        loc.GmtOffset = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Away:
                        loc.Away = BitConverter.ToBoolean(child.Data, child.PayloadPos);
                        break;

                    case Packet_AwayMsg:
                        loc.AwayMessage = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Tag:
                        loc.Tags.Add(PatchTag.FromBytes(child.Data, child.PayloadPos, child.PayloadSize));
                        break;
                }
            }

            return loc;
        }
    }

    internal class CryptLoc : G2Packet
    {
        const byte Packet_TTL = 0x10;
        const byte Packet_Data = 0x20;

        internal uint TTL;
        internal byte[] Data;


        internal CryptLoc()
        {
        }

        internal CryptLoc(uint ttl, byte[] data)
        {
            TTL = ttl;
            Data = data;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame wrap = protocol.WritePacket(null, LocPacket.CryptLoc, null);

                protocol.WritePacket(wrap, Packet_TTL, BitConverter.GetBytes(TTL));
                protocol.WritePacket(wrap, Packet_Data, Data);

                return protocol.WriteFinish();
            }
        }

        internal static CryptLoc Decode(byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!G2Protocol.ReadPacket(root))
                return null;

            if (root.Name != LocPacket.CryptLoc)
                return null;

            return CryptLoc.Decode(root);
        }

        internal static CryptLoc Decode(G2Header root)
        {
            CryptLoc wrap = new CryptLoc();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_TTL:
                        wrap.TTL = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Data:
                        wrap.Data = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return wrap;
        }
    }
}
