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



    internal class FilePacket
    {
        internal const byte SubHash = 0x10;
    }

    internal class SubHashPacket : G2Packet
    {
        const byte Packet_HashResKB = 0x10;
        const byte Packet_SubHashes = 0x20;

        internal int HashResKB;
        internal byte[] SubHashes;

        // 200 chunks per packet - 20*200 = 4000kb packets

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame subhash = protocol.WritePacket(null, FilePacket.SubHash, null);

                protocol.WritePacket(subhash, Packet_HashResKB, BitConverter.GetBytes(HashResKB));
                protocol.WritePacket(subhash, Packet_SubHashes, SubHashes);

                return protocol.WriteFinish();
            }
        }

        internal static SubHashPacket Decode(G2Header root)
        {
            SubHashPacket subhash = new SubHashPacket();

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_HashResKB:
                        subhash.HashResKB = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_SubHashes:
                        subhash.SubHashes = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;                     
                }
            }

            return subhash;
        }
    }

    internal class LargeDataPacket : G2Packet
    {
        const byte Packet_Size= 0x10;
        const byte Packet_Hash = 0x20;

        byte Name;

        internal int Size;
        internal byte[] Hash;
        internal byte[] Data;


        internal LargeDataPacket(byte name, int size, byte[] hash)
        {
            Name = name;
            Size = size;
            Hash = hash;
        }

        internal LargeDataPacket(byte name, byte[] data)
        {
            Name = name;
            Data = data;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                // first packet is size
                if (Size > 0)
                {
                    G2Frame split = protocol.WritePacket(null, Name, null);
                    protocol.WritePacket(split, Packet_Size, BitConverter.GetBytes(Size));

                    if(Hash != null)
                        protocol.WritePacket(split, Packet_Hash, Hash);
                }
                // following packets are data
                else
                    protocol.WritePacket(null, Name, Data);

                return protocol.WriteFinish();
            }
        }

        internal static bool Decode(G2Header root, byte[] destination, ref int pos)
        {
            // data packet
            if (G2Protocol.ReadPayload(root))
            {
                Buffer.BlockCopy(root.Data, root.PayloadPos, destination, pos, root.PayloadSize);
                pos += root.PayloadSize;
                return true;
            }

            return false;
        }

        internal static LargeDataPacket Decode(G2Header root)
        {
            LargeDataPacket packet = new LargeDataPacket(root.Name, 0, null);

            // size packet
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                if (child.Name == Packet_Size)
                    packet.Size = BitConverter.ToInt32(child.Data, child.PayloadPos);

                if (child.Name == Packet_Hash)
                    packet.Hash = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
            }

            return packet;
        }

        internal static void Write(PacketStream stream, byte name, byte[] data)
        {
            // empty file
            if (data == null)
            {
                stream.WritePacket(new LargeDataPacket(name, 0, null));
                return;
            }

            // write header packet
            byte[] hash = new SHA1Managed().ComputeHash(data);

            stream.WritePacket(new LargeDataPacket(name, data.Length, hash));

            int pos = 0;
            int left = data.Length;

            while (left > 0)
            {
                int amount = (left > 2000) ? 2000 : left;

                left -= amount;

                stream.WritePacket(new LargeDataPacket(name, Utilities.ExtractBytes(data, pos, amount)));

                pos += amount;
            }

        }

        internal static byte[] Read(LargeDataPacket start, PacketStream stream, byte name)
        {
            byte[] data = new byte[start.Size];
            int pos = 0;

            G2Header root = null;

            while (stream.ReadPacket(ref root))
                if (root.Name == name)
                {
                    if (!LargeDataPacket.Decode(root, data, ref pos))
                        break;

                    // done, break
                    if (pos == start.Size)
                        break;

                }
                // unknown packet, break
                else
                    break;

            if (start != null && data != null)
                if(Utilities.MemCompare(start.Hash, new SHA1Managed().ComputeHash(data)))
                    return data;

            return null;
        }
    }
}
