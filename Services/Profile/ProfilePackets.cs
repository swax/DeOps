using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using DeOps.Implementation.Protocol;


namespace DeOps.Services.Profile
{
    internal class ProfilePacket
    {
        internal const byte Header = 0x10;
        internal const byte File = 0x20;
        internal const byte Field = 0x30;
    }

    internal class ProfileHeader : G2Packet
    {
        const byte Packet_Key = 0x10;
        const byte Packet_Version = 0x20;
        const byte Packet_FileHash = 0x30;
        const byte Packet_FileSize = 0x40;
        const byte Packet_FileKey = 0x50;
        const byte Packet_EmbeddedStart = 0x60;


        internal byte[] Key;
        internal uint Version;
        internal byte[] FileHash;
        internal long FileSize;
        internal long EmbeddedStart;
        internal RijndaelManaged FileKey = new RijndaelManaged();

        internal ulong KeyID;


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame header = protocol.WritePacket(null, ProfilePacket.Header, null);

                protocol.WritePacket(header, Packet_Key, Key);
                protocol.WritePacket(header, Packet_Version, BitConverter.GetBytes(Version));
                protocol.WritePacket(header, Packet_FileHash, FileHash);
                protocol.WritePacket(header, Packet_FileSize, BitConverter.GetBytes(FileSize));
                protocol.WritePacket(header, Packet_EmbeddedStart, BitConverter.GetBytes(EmbeddedStart));
                protocol.WritePacket(header, Packet_FileKey, FileKey.Key);

                return protocol.WriteFinish();
            }
        }

        internal static ProfileHeader Decode(G2Protocol protocol, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!protocol.ReadPacket(root))
                return null;

            if (root.Name != ProfilePacket.Header)
                return null;

            return ProfileHeader.Decode(protocol, root);
        }

        internal static ProfileHeader Decode(G2Protocol protocol, G2Header root)
        {
            ProfileHeader header = new ProfileHeader();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Key:
                        header.Key = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        header.KeyID = Utilities.KeytoID(header.Key);
                        break;

                    case Packet_Version:
                        header.Version = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_FileHash:
                        header.FileHash = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_FileSize:
                        header.FileSize = BitConverter.ToInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_EmbeddedStart:
                        header.EmbeddedStart = BitConverter.ToInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_FileKey:
                        header.FileKey.Key = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        header.FileKey.IV = new byte[header.FileKey.IV.Length];
                        break;
                }
            }

            return header;
        }
    }

    internal class ProfileFile : G2Packet
    {
        const byte Packet_Name = 0x10;
        const byte Packet_Size = 0x20;

        internal string Name;
        internal long Size;


        internal ProfileFile()
        {
        }

        internal ProfileFile(string name, long size)
        {
            Name = name;
            Size = size;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame header = protocol.WritePacket(null, ProfilePacket.File, null);

                protocol.WritePacket(header, Packet_Name, protocol.UTF.GetBytes(Name));
                protocol.WritePacket(header, Packet_Size, BitConverter.GetBytes(Size));

                return protocol.WriteFinish();
            }
        }

        internal static ProfileFile Decode(G2Protocol protocol, G2Header root)
        {
            ProfileFile file = new ProfileFile();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Name:
                        file.Name = protocol.UTF.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Size:
                        file.Size = BitConverter.ToInt64(child.Data, child.PayloadPos);
                        break;
                }
            }

            return file;
        }
    }


    internal enum ProfileFieldType : byte {Text, File};

    internal class ProfileField : G2Packet
    {
        const byte Packet_Type = 0x10;
        const byte Packet_Name = 0x20;
        const byte Packet_Value = 0x30;


        internal ProfileFieldType FieldType;
        internal string Name;
        internal byte[] Value;


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame header = protocol.WritePacket(null, ProfilePacket.Field, null);

                protocol.WritePacket(header, Packet_Type, BitConverter.GetBytes((byte)FieldType));
                protocol.WritePacket(header, Packet_Name, protocol.UTF.GetBytes(Name));
                protocol.WritePacket(header, Packet_Value, Value);

                return protocol.WriteFinish();
            }
        }

        internal static ProfileField Decode(G2Protocol protocol, G2Header root)
        {
            ProfileField field = new ProfileField();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Type:
                        field.FieldType = (ProfileFieldType)child.Data[child.PayloadPos];
                        break;

                    case Packet_Name:
                        field.Name = protocol.UTF.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Value:
                        field.Value = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return field;
        }
    }
}
