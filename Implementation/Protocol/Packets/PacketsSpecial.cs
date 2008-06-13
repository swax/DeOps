using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Services.Location;


namespace RiseOp.Implementation.Protocol.Special
{
    internal class TunnelPacket : G2Packet
    {
        const byte Packet_Source = 0x10;
        const byte Packet_Target = 0x20;
        const byte Packet_SourceServer = 0x30;
        const byte Packet_TargetServer = 0x40;


        internal TunnelAddress Source;
        internal TunnelAddress Target;
        internal DhtAddress SourceServer;
        internal DhtAddress TargetServer;

        internal byte[] Payload;


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame tunnel = protocol.WritePacket(null, RootPacket.Tunnel, Payload);

                protocol.WritePacket(tunnel, Packet_Source, Source.ToBytes());
                protocol.WritePacket(tunnel, Packet_Target, Target.ToBytes());

                if (SourceServer != null)
                    SourceServer.WritePacket(protocol, tunnel, Packet_SourceServer);

                if (TargetServer != null)
                    TargetServer.WritePacket(protocol, tunnel, Packet_TargetServer);

                return protocol.WriteFinish();
            }
        }

        internal static TunnelPacket Decode(G2Header root)
        {
            TunnelPacket tunnel = new TunnelPacket();

            if (G2Protocol.ReadPayload(root))
                tunnel.Payload = Utilities.ExtractBytes(root.Data, root.PayloadPos, root.PayloadSize);

            G2Protocol.ResetPacket(root);


            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Source:
                        tunnel.Source = TunnelAddress.FromBytes(child.Data, child.PayloadPos);
                        break;

                    case Packet_Target:
                        tunnel.Target = TunnelAddress.FromBytes(child.Data, child.PayloadPos);
                        break;

                    case Packet_SourceServer:
                        tunnel.SourceServer = DhtAddress.ReadPacket(child);
                        break;

                    case Packet_TargetServer:
                        tunnel.TargetServer = DhtAddress.ReadPacket(child);
                        break;
                }
            }

            return tunnel;
        }
    }

    internal class OneWayInvite : G2Packet
    {
        const byte Packet_OpName    = 0x10;
        const byte Packet_OpAccess  = 0x20;
        const byte Packet_OpID      = 0x30;
        const byte Packet_Contact   = 0x40;


        internal string OpName;
        internal AccessType OpAccess;
        internal byte[] OpID;
        internal List<DhtContact> Contacts = new List<DhtContact>();


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame invite = protocol.WritePacket(null, RootPacket.Invite, null);

                protocol.WritePacket(invite, Packet_OpName, UTF8Encoding.UTF8.GetBytes(OpName));
                protocol.WritePacket(invite, Packet_OpAccess, BitConverter.GetBytes((byte)OpAccess));
                protocol.WritePacket(invite, Packet_OpID, OpID);

                foreach (DhtContact contact in Contacts)
                    contact.WritePacket(protocol, invite, Packet_Contact);

                return protocol.WriteFinish();
            }
        }

        internal static OneWayInvite Decode(byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!G2Protocol.ReadPacket(root))
                return null;

            if (root.Name != RootPacket.Invite)
                return null;

            OneWayInvite invite = new OneWayInvite();

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_OpName:
                        invite.OpName = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;


                    case Packet_OpAccess:
                        invite.OpAccess = (AccessType)child.Data[child.PayloadPos];
                        break;

                    case Packet_OpID:
                        invite.OpID = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                        
                    case Packet_Contact:
                        invite.Contacts.Add(DhtContact.ReadPacket(child));
                        break;

                }

            }

            return invite;
        }
    }

    internal class TwoWayInvite : G2Packet
    {
        const byte Packet_OpName  = 0x10;
        const byte Packet_OpAccess = 0x20;
        const byte Packet_Location = 0x30;
        const byte Packet_InviteKey = 0x40;


        internal string OpName;
        internal AccessType OpAccess;
        internal LocationData Location;
        internal byte[] InviteKey;

        
        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame invite = protocol.WritePacket(null, RootPacket.Invite, null);

                protocol.WritePacket(invite, Packet_OpName, UTF8Encoding.UTF8.GetBytes(OpName));
                protocol.WritePacket(invite, Packet_OpAccess, BitConverter.GetBytes((byte)OpAccess));
                protocol.WritePacket(invite, Packet_Location, Location.Encode(new G2Protocol()));
                protocol.WritePacket(invite, Packet_InviteKey, InviteKey);

                return protocol.WriteFinish();
            }
        }

        internal static TwoWayInvite Decode(byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!G2Protocol.ReadPacket(root))
                return null;

            if (root.Name != RootPacket.Invite)
                return null;

            TwoWayInvite invite = new TwoWayInvite();

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_OpName:
                        invite.OpName = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_OpAccess:
                        invite.OpAccess = (AccessType)child.Data[child.PayloadPos];
                        break;

                    case Packet_Location:
                        invite.Location = LocationData.Decode(Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize));
                        break;

                    case Packet_InviteKey:
                        invite.InviteKey = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }

            }

            return invite;
        }
    }
}
