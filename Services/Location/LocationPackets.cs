using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Protocol.Packets;

namespace RiseOp.Services.Location
{
    internal class LocationPacket
    {
        internal const byte Data = 0x10;
        internal const byte Ping = 0x20;
        internal const byte Notify = 0x30;

        internal const byte CryptLoc = 0x40;
    }

    internal class LocationData : G2Packet
    {
        internal const int GLOBAL_TTL = 60;
        internal const int OP_TTL = 6; // published to 8 closest so ~1 loc update per minute


        const byte Packet_Key       = 0x10;
        const byte Packet_Source    = 0x20;
        const byte Packet_IP        = 0x30;
        const byte Packet_Proxies   = 0x40;
        const byte Packet_Name      = 0x50;
        const byte Packet_Place     = 0x60;
        const byte Packet_Version   = 0x70;
        const byte Packet_GMTOffset = 0x80;
        const byte Packet_Away      = 0x90;
        const byte Packet_AwayMsg   = 0xA0;
        const byte Packet_Tag       = 0xB0;
        const byte Packet_TunnelClient  = 0xC0;
        const byte Packet_TunnelServers = 0xD0;
        const byte Packet_License = 0xE0;


        internal byte[] Key;
        internal DhtSource Source;
        internal IPAddress IP;
        internal List<DhtAddress> Proxies = new List<DhtAddress>();
        internal string Name;
        internal string Place = "";
        internal uint Version;
        internal List<PatchTag> Tags = new List<PatchTag>();

        internal TunnelAddress TunnelClient;
        internal List<DhtAddress> TunnelServers = new List<DhtAddress>();

        internal int GmtOffset;
        internal bool Away;
        internal string AwayMessage = "";

        internal LightLicense License;

        internal ulong UserID;
        internal ulong RoutingID
        {
            get { return UserID ^ Source.ClientID; }
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            byte[] license = null;
            if(License != null)
                license = License.Encode(protocol);

            lock (protocol.WriteSection)
            {
                G2Frame loc = protocol.WritePacket(null, LocationPacket.Data, null);

                protocol.WritePacket(loc, Packet_Key, Key);
                Source.WritePacket(protocol, loc, Packet_Source);
                protocol.WritePacket(loc, Packet_IP, IP.GetAddressBytes());
                protocol.WritePacket(loc, Packet_Name, UTF8Encoding.UTF8.GetBytes(Name));
                protocol.WritePacket(loc, Packet_Place, UTF8Encoding.UTF8.GetBytes(Place));
                protocol.WritePacket(loc, Packet_Version, CompactNum.GetBytes(Version));
                protocol.WritePacket(loc, Packet_GMTOffset, BitConverter.GetBytes(GmtOffset));
                protocol.WritePacket(loc, Packet_Away, BitConverter.GetBytes(Away));
                protocol.WritePacket(loc, Packet_AwayMsg, UTF8Encoding.UTF8.GetBytes(AwayMessage));
                    
                foreach(DhtAddress proxy in Proxies)
                    proxy.WritePacket(protocol, loc, Packet_Proxies);

                foreach(PatchTag tag in Tags)
                    protocol.WritePacket(loc, Packet_Tag, tag.ToBytes());

                if (TunnelClient != null)
                    protocol.WritePacket(loc, Packet_TunnelClient, TunnelClient.ToBytes());

                foreach (DhtAddress server in TunnelServers)
                    server.WritePacket(protocol, loc, Packet_TunnelServers);

                if(license != null)
                    protocol.WritePacket(loc, Packet_License, license);

                return protocol.WriteFinish();
            }
        }

        internal byte[] EncodeLight(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame loc = protocol.WritePacket(null, LocationPacket.Data, null);

                Source.WritePacket(protocol, loc, Packet_Source);
                protocol.WritePacket(loc, Packet_IP, IP.GetAddressBytes());

                foreach (DhtAddress proxy in Proxies)
                    proxy.WritePacket(protocol, loc, Packet_Proxies);

                if (TunnelClient != null)
                    protocol.WritePacket(loc, Packet_TunnelClient, TunnelClient.ToBytes());

                foreach (DhtAddress server in TunnelServers)
                    server.WritePacket(protocol, loc, Packet_TunnelServers);

                return protocol.WriteFinish();
            }
        }

        internal static LocationData Decode(byte[] data)
        {
            G2Header root = new G2Header(data);

            G2Protocol.ReadPacket(root);

            if (root.Name != LocationPacket.Data)
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
                        loc.Source = DhtSource.ReadPacket(child);
                        loc.UserID = loc.Source.UserID; // encode light doesnt send full key
                        break;

                    case Packet_IP:
                        loc.IP = new IPAddress(Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize));
                        break;

                    case Packet_Proxies:
                        loc.Proxies.Add( DhtAddress.ReadPacket(child) );
                        break;

                    case Packet_Name:
                        loc.Name = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Place:
                        loc.Place = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
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

                    case Packet_TunnelClient:
                        loc.TunnelClient = TunnelAddress.FromBytes(child.Data, child.PayloadPos);
                        break;

                    case Packet_TunnelServers:
                        loc.TunnelServers.Add(DhtAddress.ReadPacket(child));
                        break;

                    case Packet_License:
                        loc.License = LightLicense.Decode(Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize));
                        break;
                }
            }

            return loc;
        }
    }

    internal class LocationPing : G2Packet
    {
        const byte Packet_RemoteVersion = 0x10;


        internal uint RemoteVersion;


        internal LocationPing()
        {
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame ping = protocol.WritePacket(null, LocationPacket.Ping, null);

                protocol.WritePacket(ping, Packet_RemoteVersion, CompactNum.GetBytes(RemoteVersion));

                return protocol.WriteFinish();
            }
        }

        internal static LocationPing Decode(G2Header root)
        {
            LocationPing ping = new LocationPing();

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_RemoteVersion:
                        ping.RemoteVersion = CompactNum.ToUInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return ping;
        }
    }

    internal class LocationNotify : G2Packet
    {
        const byte Packet_Timeout = 0x10;
        const byte Packet_SignedLocation = 0x20;
        const byte Packet_GoingOffline = 0x30;


        internal int Timeout;
        internal byte[] SignedLocation;
        internal bool GoingOffline;


        internal LocationNotify()
        {
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame notify = protocol.WritePacket(null, LocationPacket.Notify, SignedLocation);

                protocol.WritePacket(notify, Packet_Timeout, CompactNum.GetBytes(Timeout));

                if(GoingOffline)
                    protocol.WritePacket(notify, Packet_GoingOffline, null);

                return protocol.WriteFinish();
            }
        }

        internal static LocationNotify Decode(G2Header root)
        {
            LocationNotify notify = new LocationNotify();

            if (G2Protocol.ReadPayload(root))
                notify.SignedLocation = Utilities.ExtractBytes(root.Data, root.PayloadPos, root.PayloadSize);

            G2Protocol.ResetPacket(root);

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (child.Name == Packet_GoingOffline)
                {
                    notify.GoingOffline = true;
                    continue;
                }

                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Timeout:
                        notify.Timeout = CompactNum.ToInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return notify;
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
                G2Frame wrap = protocol.WritePacket(null, LocationPacket.CryptLoc, null);

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

            if (root.Name != LocationPacket.CryptLoc)
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
