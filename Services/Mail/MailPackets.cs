using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using RiseOp.Implementation.Protocol;


namespace RiseOp.Services.Mail
{
    internal class MailPacket
    {
        internal const byte MailHeader = 0x10;
        internal const byte MailInfo = 0x20;
        internal const byte MailDest = 0x30;
        internal const byte MailFile = 0x40;

        internal const byte Ack = 0x50;
    }

    // 20,000 emails stored locally, the header is ~10MB for 1024bit RSA, 2.5MB for 128bit ECC

    internal class MailHeader : G2Packet
    {
        const byte Packet_Source   = 0x10;
        const byte Packet_Target   = 0x20;
        const byte Packet_FileKey  = 0x30;
        const byte Packet_FileHash = 0x40;
        const byte Packet_FileSize = 0x50;
        const byte Packet_LocalKey = 0x60;
        const byte Packet_SourceVersion = 0x70;
        const byte Packet_TargetVersion = 0x80; 
        const byte Packet_MailID        = 0x90;
        const byte Packet_FileStart     = 0xA0;
        const byte Packet_Read          = 0xB0;
        const byte Packet_Received      = 0xC0;

        internal byte[] Source;
        internal byte[] Target; 
        internal byte[] FileKey; // signed with targets public key
        internal byte[] FileHash;
        internal long  FileSize;
        internal uint SourceVersion;
        internal uint TargetVersion;
        internal byte[] MailID;

        internal ulong SourceID;
        internal ulong TargetID;

        // only saved in inbox file, **not put out on netork
        internal RijndaelManaged LocalKey = new RijndaelManaged();
        internal ulong    FileStart;
        internal bool     Read;
        internal DateTime Received;


        internal MailHeader()
        {
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            // should not be called
            throw new Exception("Mail header encode called");
        }

        internal byte[] Encode(G2Protocol protocol, bool local)
        {
            lock (protocol.WriteSection)
            {
                G2Frame header = protocol.WritePacket(null, MailPacket.MailHeader, null);

                protocol.WritePacket(header, Packet_Source, Source);
                protocol.WritePacket(header, Packet_Target, Target);
                protocol.WritePacket(header, Packet_FileKey, FileKey);
                protocol.WritePacket(header, Packet_FileHash, FileHash);
                protocol.WritePacket(header, Packet_FileSize, BitConverter.GetBytes(FileSize));
                protocol.WritePacket(header, Packet_SourceVersion, BitConverter.GetBytes(SourceVersion));
                protocol.WritePacket(header, Packet_TargetVersion, BitConverter.GetBytes(TargetVersion));
                protocol.WritePacket(header, Packet_MailID, MailID);

                if (local)
                {
                    protocol.WritePacket(header, Packet_LocalKey, LocalKey.Key);
                    protocol.WritePacket(header, Packet_FileStart, BitConverter.GetBytes(FileStart));
                    protocol.WritePacket(header, Packet_Read, BitConverter.GetBytes(Read));
                    protocol.WritePacket(header, Packet_Received, BitConverter.GetBytes(Received.ToBinary()));
                }

                return protocol.WriteFinish();
            }
        }

        internal static MailHeader Decode(G2Protocol protocol, G2Header root)
        {
            MailHeader header = new MailHeader();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Source:
                        header.Source = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        header.SourceID = Utilities.KeytoID(header.Source);
                        break;

                    case Packet_Target:
                        header.Target = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        header.TargetID = Utilities.KeytoID(header.Target);
                        break;

                    case Packet_FileKey:
                        header.FileKey = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                    case Packet_FileHash:
                        header.FileHash = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_FileSize:
                        header.FileSize = BitConverter.ToInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_SourceVersion:
                        header.SourceVersion = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_TargetVersion:
                        header.TargetVersion = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_MailID:
                        header.MailID = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_LocalKey:
                        header.LocalKey.Key = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        header.LocalKey.IV = new byte[header.LocalKey.IV.Length];
                        break;

                    case Packet_FileStart:
                        header.FileStart = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Read:
                        header.Read = BitConverter.ToBoolean(child.Data, child.PayloadPos);
                        break;

                    case Packet_Received:
                        header.Received = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;
                }
            }

            return header;
        }
    }

    internal class MailInfo : G2Packet
    {
        const byte Packet_Subject = 0x10;
        const byte Packet_Date = 0x20;
        const byte Packet_Attachments = 0x30;


        internal string Subject;
        internal DateTime Date;
        internal bool Attachments;


        internal MailInfo() { }

        internal MailInfo(string subject, DateTime date, bool attachements)
        {
            Subject = subject;
            Date = date;
            Attachments = attachements;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame info = protocol.WritePacket(null, MailPacket.MailInfo, null);

                protocol.WritePacket(info, Packet_Subject, protocol.UTF.GetBytes(Subject));
                protocol.WritePacket(info, Packet_Date, BitConverter.GetBytes(Date.ToBinary()));
                protocol.WritePacket(info, Packet_Attachments, BitConverter.GetBytes(Attachments));

                return protocol.WriteFinish();
            }
        }

        internal static MailInfo Decode(G2Protocol protocol, G2Header root)
        {
            MailInfo info = new MailInfo();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Subject:
                        info.Subject = protocol.UTF.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Date:
                        info.Date = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_Attachments:
                        info.Attachments = BitConverter.ToBoolean(child.Data, child.PayloadPos);
                        break;
                }
            }

            return info;
        }
    }

    internal class MailDestination : G2Packet
    {
        const byte Packet_Key = 0x10;
        const byte Packet_CC = 0x20;

        internal byte[] Key;
        internal bool CC;

        internal ulong KeyID;

        internal MailDestination()
        {
        }

        internal MailDestination(byte[] key, bool cc)
        {
            Key = key;
            CC = cc;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame dest = protocol.WritePacket(null, MailPacket.MailDest, null);

                protocol.WritePacket(dest, Packet_Key, Key);
                protocol.WritePacket(dest, Packet_CC, BitConverter.GetBytes(CC));

                return protocol.WriteFinish();
            }
        }

        internal static MailDestination Decode(G2Protocol protocol, G2Header root)
        {
            MailDestination dest = new MailDestination();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Key:
                        dest.Key = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        dest.KeyID = Utilities.KeytoID(dest.Key);
                        break;

                    case Packet_CC:
                        dest.CC = BitConverter.ToBoolean(child.Data, child.PayloadPos);
                        break;
                }
            }

            return dest;
        }
    }


    internal class MailFile : G2Packet
    {
        const byte Packet_Name = 0x10;
        const byte Packet_Size = 0x20;

        internal string Name;
        internal long Size;


        internal MailFile()
        {
        }

        internal MailFile(string name, long size)
        {
            Name = name;
            Size = size;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame file = protocol.WritePacket(null, MailPacket.MailFile, null);

                protocol.WritePacket(file, Packet_Name, protocol.UTF.GetBytes(Name));
                protocol.WritePacket(file, Packet_Size, CompactNum.GetBytes(Size));

                return protocol.WriteFinish();
            }
        }

        internal static MailFile Decode(G2Protocol protocol, G2Header root)
        {
            MailFile file = new MailFile();
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
                        file.Size = CompactNum.ToInt64(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return file;
        }
    }

    internal class MailAck : G2Packet
    {
        const byte Packet_MailID  = 0x10;
        const byte Packet_Source  = 0x20;
        const byte Packet_Target  = 0x30;
        const byte Packet_TargetVersion = 0x40;
        const byte Packet_SourceVersion = 0x50;

        internal byte[] MailID;
        internal byte[] Source;
        internal byte[] Target;
        internal uint TargetVersion;
        internal uint SourceVersion;

        internal ulong SourceID;
        internal ulong TargetID;

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame ack = protocol.WritePacket(null, MailPacket.Ack, null);

                protocol.WritePacket(ack, Packet_MailID, MailID);
                protocol.WritePacket(ack, Packet_Source, Source);
                protocol.WritePacket(ack, Packet_Target, Target);
                protocol.WritePacket(ack, Packet_TargetVersion, BitConverter.GetBytes(TargetVersion));
                protocol.WritePacket(ack, Packet_SourceVersion, BitConverter.GetBytes(SourceVersion));

                return protocol.WriteFinish();
            }
        }

        internal static MailAck Decode(G2Protocol protocol, G2Header root)
        {
            MailAck ack = new MailAck();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_MailID:
                        ack.MailID = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Source:
                        ack.Source = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        ack.SourceID = Utilities.KeytoID(ack.Source);
                        break;

                    case Packet_Target:
                        ack.Target = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        ack.TargetID = Utilities.KeytoID(ack.Target); 
                        break;

                    case Packet_TargetVersion:
                        ack.TargetVersion = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_SourceVersion:
                        ack.SourceVersion = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;
                }
            }

            return ack;
        }
    }
}
