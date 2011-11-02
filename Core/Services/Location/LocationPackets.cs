using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Protocol.Packets;

namespace DeOps.Services.Location
{
    public class LocationPacket
    {
        public const byte Data = 0x10;
        public const byte Ping = 0x20;
        public const byte Notify = 0x30;
    }

    public class LocationData : G2Packet
    {
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


        public byte[] Key;
        public DhtSource Source;
        public IPAddress IP;
        public List<DhtAddress> Proxies = new List<DhtAddress>();
        public string Name;
        public string Place = "";
        public uint Version;
        public List<PatchTag> Tags = new List<PatchTag>();

        public TunnelAddress TunnelClient;
        public List<DhtAddress> TunnelServers = new List<DhtAddress>();

        public int GmtOffset;
        public bool Away;
        public string AwayMessage = "";

        public LightLicense License;

        public ulong UserID;
        public ulong RoutingID
        {
            get { return UserID ^ Source.ClientID; }
        }

        public override byte[] Encode(G2Protocol protocol)
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

        public byte[] EncodeLight(G2Protocol protocol)
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

        public static LocationData Decode(byte[] data)
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

    public class LocationPing : G2Packet
    {
        const byte Packet_RemoteVersion = 0x10;


        public uint RemoteVersion;


        public LocationPing()
        {
        }

        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame ping = protocol.WritePacket(null, LocationPacket.Ping, null);

                protocol.WritePacket(ping, Packet_RemoteVersion, CompactNum.GetBytes(RemoteVersion));

                return protocol.WriteFinish();
            }
        }

        public static LocationPing Decode(G2Header root)
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

    public class LocationNotify : G2Packet
    {
        const byte Packet_Timeout = 0x10;
        const byte Packet_SignedLocation = 0x20;
        const byte Packet_GoingOffline = 0x30;


        public int Timeout;
        public byte[] SignedLocation;
        public bool GoingOffline;


        public LocationNotify()
        {
        }

        public override byte[] Encode(G2Protocol protocol)
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

        public static LocationNotify Decode(G2Header root)
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
}
