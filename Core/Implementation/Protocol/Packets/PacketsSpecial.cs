using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol.Net;
using DeOps.Services.Location;


namespace DeOps.Implementation.Protocol.Special
{
    public class TunnelPacket : G2Packet
    {
        const byte Packet_Source = 0x10;
        const byte Packet_Target = 0x20;
        const byte Packet_SourceServer = 0x30;
        const byte Packet_TargetServer = 0x40;


        public TunnelAddress Source;
        public TunnelAddress Target;
        public DhtAddress SourceServer;
        public DhtAddress TargetServer;

        public byte[] Payload;


        public override byte[] Encode(G2Protocol protocol)
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

        public static TunnelPacket Decode(G2Header root)
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

    public class InvitePacket
    {
        public const byte Info = 0x10;
        public const byte Contact = 0x20;
        public const byte WebCache = 0x30;
    }

    public class OneWayInvite : G2Packet
    {
        const byte Packet_UserName  = 0x10;
        const byte Packet_OpName    = 0x20;
        const byte Packet_OpAccess  = 0x30;
        const byte Packet_OpID      = 0x40;


        public string UserName;
        public string OpName;
        public AccessType OpAccess;
        public byte[] OpID;
        public List<DhtContact> Contacts = new List<DhtContact>();


        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame invite = protocol.WritePacket(null, InvitePacket.Info, null);

                protocol.WritePacket(invite, Packet_UserName, UTF8Encoding.UTF8.GetBytes(UserName));
                protocol.WritePacket(invite, Packet_OpName, UTF8Encoding.UTF8.GetBytes(OpName));
                protocol.WritePacket(invite, Packet_OpAccess, BitConverter.GetBytes((byte)OpAccess));
                protocol.WritePacket(invite, Packet_OpID, OpID);

                return protocol.WriteFinish();
            }
        }

        public static OneWayInvite Decode(G2Header root)
        {
            OneWayInvite invite = new OneWayInvite();

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_UserName:
                        invite.UserName = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_OpName:
                        invite.OpName = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_OpAccess:
                        invite.OpAccess = (AccessType)child.Data[child.PayloadPos];
                        break;

                    case Packet_OpID:
                        invite.OpID = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return invite;
        }
    }

    public class FilePacket
    {
        public const byte SubHash = 0x10;
    }

    public class SubHashPacket : G2Packet
    {
        const byte Packet_ChunkSize  = 0x10;
        const byte Packet_TotalCount = 0x20;
        const byte Packet_SubHashes  = 0x30;

        public int    ChunkSize;  // in KB, 128kb chunks
        public int    TotalCount;
        public byte[] SubHashes;

        // 100 chunks per packet - 10*200 = 2,000kb packets

        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame subhash = protocol.WritePacket(null, FilePacket.SubHash, null);

                protocol.WritePacket(subhash, Packet_ChunkSize, BitConverter.GetBytes(ChunkSize));
                protocol.WritePacket(subhash, Packet_TotalCount, BitConverter.GetBytes(TotalCount));
                protocol.WritePacket(subhash, Packet_SubHashes, SubHashes);

                return protocol.WriteFinish();
            }
        }

        public static SubHashPacket Decode(G2Header root)
        {
            SubHashPacket subhash = new SubHashPacket();

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_ChunkSize:
                        subhash.ChunkSize = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_TotalCount:
                        subhash.TotalCount = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_SubHashes:
                        subhash.SubHashes = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;                     
                }
            }

            return subhash;
        }
    }

    public class LargeDataPacket : G2Packet
    {
        const byte Packet_Size= 0x10;
        const byte Packet_Hash = 0x20;

        byte Name;

        public int Size;
        public byte[] Hash;
        public byte[] Data;


        public LargeDataPacket(byte name, int size, byte[] hash)
        {
            Name = name;
            Size = size;
            Hash = hash;
        }

        public LargeDataPacket(byte name, byte[] data)
        {
            Name = name;
            Data = data;
        }

        public override byte[] Encode(G2Protocol protocol)
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

        public static bool Decode(G2Header root, byte[] destination, ref int pos)
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

        public static LargeDataPacket Decode(G2Header root)
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

        public static void Write(PacketStream stream, byte name, byte[] data)
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

        public static byte[] Read(LargeDataPacket start, PacketStream stream, byte name)
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
