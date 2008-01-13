using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using RiseOp.Implementation.Protocol;


namespace RiseOp.Services.Profile
{
    internal class ProfilePacket
    {
        internal const byte Attachment = 0x20;
        internal const byte Field = 0x30;
    }

    internal class ProfileAttachment : G2Packet
    {
        const byte Packet_Name = 0x10;
        const byte Packet_Size = 0x20;

        internal string Name;
        internal long Size;


        internal ProfileAttachment()
        {
        }

        internal ProfileAttachment(string name, long size)
        {
            Name = name;
            Size = size;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame header = protocol.WritePacket(null, ProfilePacket.Attachment, null);

                protocol.WritePacket(header, Packet_Name, protocol.UTF.GetBytes(Name));
                protocol.WritePacket(header, Packet_Size, BitConverter.GetBytes(Size));

                return protocol.WriteFinish();
            }
        }

        internal static ProfileAttachment Decode(G2Protocol protocol, G2Header root)
        {
            ProfileAttachment file = new ProfileAttachment();
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
