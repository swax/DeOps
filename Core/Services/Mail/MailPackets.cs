using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using DeOps.Implementation.Protocol;


namespace DeOps.Services.Mail
{
    public class MailPacket
    {
        public const byte MailHeader = 0x10;
        public const byte MailInfo = 0x20;
        public const byte MailDest = 0x30;
        public const byte MailFile = 0x40;

        public const byte Ack = 0x50;
    }

    // 20,000 emails stored locally, the header is ~10MB for 1024bit RSA, 2.5MB for 128bit ECC

    public class MailHeader : G2Packet
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
        const byte Packet_ThreadID      = 0xD0;


        public byte[] Source;
        public byte[] Target; 
        public byte[] FileKey; // signed with targets public key
        public byte[] FileHash;
        public long   FileSize;
        public uint   SourceVersion;
        public uint   TargetVersion;
        public byte[] MailID;
        public int    ThreadID;

        public ulong SourceID;
        public ulong TargetID;

        // only saved in inbox file, **not put out on netork
        public byte[]   LocalKey;
        public long     FileStart;
        public bool     Read;
        public DateTime Received;


        public MailHeader()
        {
        }

        public override byte[] Encode(G2Protocol protocol)
        {
            // should not be called
            throw new Exception("Mail header encode called");
        }

        public byte[] Encode(G2Protocol protocol, bool local)
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
                protocol.WritePacket(header, Packet_ThreadID, BitConverter.GetBytes(ThreadID));

                if (local)
                {
                    protocol.WritePacket(header, Packet_LocalKey, LocalKey);
                    protocol.WritePacket(header, Packet_FileStart, BitConverter.GetBytes(FileStart));
                    protocol.WritePacket(header, Packet_Read, BitConverter.GetBytes(Read));
                    protocol.WritePacket(header, Packet_Received, BitConverter.GetBytes(Received.ToBinary()));
                }

                return protocol.WriteFinish();
            }
        }

        public static MailHeader Decode(G2Header root)
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

                    case Packet_ThreadID:
                        header.ThreadID = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_LocalKey:
                        header.LocalKey = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_FileStart:
                        header.FileStart = BitConverter.ToInt64(child.Data, child.PayloadPos);
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

    public class MailInfo : G2Packet
    {
        const byte Packet_Subject = 0x10;
        const byte Packet_Format = 0x20;
        const byte Packet_Date = 0x30;
        const byte Packet_Attachments = 0x40;
        const byte Packet_Unique = 0x50;
        const byte Packet_Quip = 0x60;


        public string Subject;
        public TextFormat Format;
        public string Quip;
        public DateTime Date;
        public bool Attachments;
        public byte[] Unique = new byte[16];


        public MailInfo() { }

        public MailInfo(string subject, TextFormat format, string quip, DateTime date, bool attachements)
        {
            Subject = subject;
            Format = format;
            Quip = quip;
            Date = date;
            Attachments = attachements;

            RNGCryptoServiceProvider rnd = new RNGCryptoServiceProvider();
            rnd.GetBytes(Unique);
        }

        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame info = protocol.WritePacket(null, MailPacket.MailInfo, null);

                protocol.WritePacket(info, Packet_Subject, UTF8Encoding.UTF8.GetBytes(Subject));
                protocol.WritePacket(info, Packet_Format, CompactNum.GetBytes((int)Format));
                protocol.WritePacket(info, Packet_Quip, UTF8Encoding.UTF8.GetBytes(Quip));
                protocol.WritePacket(info, Packet_Date, BitConverter.GetBytes(Date.ToBinary()));
                protocol.WritePacket(info, Packet_Attachments, BitConverter.GetBytes(Attachments));
                protocol.WritePacket(info, Packet_Unique, Unique);

                return protocol.WriteFinish();
            }
        }

        public static MailInfo Decode(G2Header root)
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
                        info.Subject = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Format:
                        info.Format = (TextFormat)CompactNum.ToInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Quip:
                        info.Quip = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Date:
                        info.Date = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_Attachments:
                        info.Attachments = BitConverter.ToBoolean(child.Data, child.PayloadPos);
                        break;

                    case Packet_Unique:
                        info.Unique = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return info;
        }
    }

    public class MailDestination : G2Packet
    {
        const byte Packet_Key = 0x10;
        const byte Packet_CC = 0x20;

        public byte[] Key;
        public bool CC;

        public ulong KeyID;

        public MailDestination()
        {
        }

        public MailDestination(byte[] key, bool cc)
        {
            Key = key;
            CC = cc;
        }

        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame dest = protocol.WritePacket(null, MailPacket.MailDest, null);

                protocol.WritePacket(dest, Packet_Key, Key);
                protocol.WritePacket(dest, Packet_CC, BitConverter.GetBytes(CC));

                return protocol.WriteFinish();
            }
        }

        public static MailDestination Decode(G2Header root)
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


    public class MailFile : G2Packet
    {
        const byte Packet_Name = 0x10;
        const byte Packet_Size = 0x20;

        public string Name;
        public long Size;


        public MailFile()
        {
        }

        public MailFile(string name, long size)
        {
            Name = name;
            Size = size;
        }

        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame file = protocol.WritePacket(null, MailPacket.MailFile, null);

                protocol.WritePacket(file, Packet_Name, UTF8Encoding.UTF8.GetBytes(Name));
                protocol.WritePacket(file, Packet_Size, CompactNum.GetBytes(Size));

                return protocol.WriteFinish();
            }
        }

        public static MailFile Decode(G2Header root)
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
                        file.Name = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Size:
                        file.Size = CompactNum.ToInt64(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return file;
        }
    }

    public class MailAck : G2Packet
    {
        const byte Packet_MailID  = 0x10;
        const byte Packet_Source  = 0x20;
        const byte Packet_Target  = 0x30;
        const byte Packet_TargetVersion = 0x40;
        const byte Packet_SourceVersion = 0x50;

        public byte[] MailID;
        public byte[] Source;
        public byte[] Target;
        public uint TargetVersion;
        public uint SourceVersion;

        public ulong SourceID;
        public ulong TargetID;

        public override byte[] Encode(G2Protocol protocol)
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

        public static MailAck Decode(G2Header root)
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
