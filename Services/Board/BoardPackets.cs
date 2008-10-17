using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using RiseOp.Implementation.Protocol;


namespace RiseOp.Services.Board
{
    internal class BoardPacket
    {
        internal const byte PostHeader = 0x10;
        internal const byte PostInfo = 0x20;
        internal const byte PostFile = 0x30;
    }

    internal class PostHeader : G2Packet
    {
        const byte Packet_Source    = 0x10;
        const byte Packet_Target    = 0x20;
        const byte Packet_ProjectID = 0x30;
        const byte Packet_PostID    = 0x40;
        const byte Packet_ParentID  = 0x50;
        const byte Packet_Time      = 0x60;
        const byte Packet_Scope     = 0x70;
        const byte Packet_FileKey   = 0x80;
        const byte Packet_FileHash  = 0x90;
        const byte Packet_FileSize  = 0xA0;
        const byte Packet_FileStart = 0xB0;
        const byte Packet_EditTime  = 0xC0;
        const byte Packet_Version   = 0xD0;
        const byte Packet_Archived  = 0xE0;

        internal byte[] Source;
        internal byte[] Target;

        internal uint ProjectID;
        internal uint PostID;
        internal uint ParentID;
        internal DateTime Time;
        internal ScopeType Scope;
        internal bool Archived;

        internal ushort Version;
        internal DateTime EditTime;
        
        internal byte[] FileKey;
        internal byte[] FileHash;
        internal long  FileSize;
        internal long  FileStart;

        internal ulong SourceID;
        internal ulong TargetID;

        // cant write local info (read) because post is re-transmitted

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame header = protocol.WritePacket(null, BoardPacket.PostHeader, null);

                protocol.WritePacket(header, Packet_Source, Source);
                protocol.WritePacket(header, Packet_Target, Target);

                protocol.WritePacket(header, Packet_ProjectID, BitConverter.GetBytes(ProjectID));
                protocol.WritePacket(header, Packet_PostID, BitConverter.GetBytes(PostID));
                protocol.WritePacket(header, Packet_ParentID, BitConverter.GetBytes(ParentID)); 
                protocol.WritePacket(header, Packet_Time, BitConverter.GetBytes(Time.ToBinary()));
                protocol.WritePacket(header, Packet_Scope, new byte[] { (byte)Scope });

                protocol.WritePacket(header, Packet_Version, BitConverter.GetBytes(Version));
                protocol.WritePacket(header, Packet_EditTime, BitConverter.GetBytes(EditTime.ToBinary()));
                protocol.WritePacket(header, Packet_Archived, BitConverter.GetBytes(Archived));
                
                protocol.WritePacket(header, Packet_FileKey, FileKey);
                protocol.WritePacket(header, Packet_FileHash, FileHash);
                protocol.WritePacket(header, Packet_FileSize, BitConverter.GetBytes(FileSize));
                protocol.WritePacket(header, Packet_FileStart, BitConverter.GetBytes(FileStart));

                return protocol.WriteFinish();
            }
        }

        internal static PostHeader Decode(G2Header root)
        {
            PostHeader header = new PostHeader();
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

                    case Packet_ProjectID:
                        header.ProjectID = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_PostID:
                        header.PostID = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_ParentID:
                        header.ParentID = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Time:
                        header.Time = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_EditTime:
                        header.EditTime = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_Archived:
                        header.Archived = BitConverter.ToBoolean(child.Data, child.PayloadPos);
                        break;

                    case Packet_Version:
                        header.Version = BitConverter.ToUInt16(child.Data, child.PayloadPos);
                        break;

                    case Packet_Scope:
                        header.Scope = (ScopeType)child.Data[child.PayloadPos];
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

                    case Packet_FileStart:
                        header.FileStart = BitConverter.ToInt64(child.Data, child.PayloadPos);
                        break;
                }
            }

            return header;
        }

        internal PostHeader Copy()
        {
            PostHeader copy = new PostHeader();

            copy.Source = Source;
            copy.Target = Target;

            copy.ProjectID = ProjectID;
            copy.PostID = PostID;
            copy.ParentID = ParentID;
            copy.Time = Time;
            copy.Scope = Scope;
            copy.Archived = Archived;

            copy.Version = Version;
            copy.EditTime = EditTime;

            copy.FileKey = FileKey;
            copy.FileHash = FileHash;
            copy.FileSize = FileSize;
            copy.FileStart = FileStart;

            copy.SourceID = SourceID;
            copy.TargetID = TargetID;

            return copy;
        }
    }


    internal class PostInfo : G2Packet
    {
        const byte Packet_Subject = 0x10;
        const byte Packet_Format = 0x20;
        const byte Packet_Unique = 0x30;
        const byte Packet_Quip = 0x40;


        internal string     Subject;
        internal TextFormat Format;
        internal string Quip;
        int Unique; // ensures file hash is unique

        
        internal PostInfo()
        {
        }

        internal PostInfo(string subject, TextFormat format, string quip, Random gen)
        {
            Subject = subject;
            Quip = quip;
            Unique = gen.Next();
            Format = format;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame info = protocol.WritePacket(null, BoardPacket.PostInfo, null);

                protocol.WritePacket(info, Packet_Subject, UTF8Encoding.UTF8.GetBytes(Subject));
                protocol.WritePacket(info, Packet_Format, CompactNum.GetBytes((int)Format));
                protocol.WritePacket(info, Packet_Quip, UTF8Encoding.UTF8.GetBytes(Quip));
                protocol.WritePacket(info, Packet_Unique, BitConverter.GetBytes(Unique));

                return protocol.WriteFinish();
            }
        }

        internal static PostInfo Decode(G2Header root)
        {
            PostInfo info = new PostInfo();
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

                    case Packet_Unique:
                        info.Unique = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;
                }
            }

            return info;
        }
    }

    internal class PostFile : G2Packet
    {
        const byte Packet_Name = 0x10;
        const byte Packet_Size = 0x20;

        internal string Name;
        internal long Size;


        internal PostFile()
        {
        }

        internal PostFile(string name, long size)
        {
            Name = name;
            Size = size;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame file = protocol.WritePacket(null, BoardPacket.PostFile, null);

                protocol.WritePacket(file, Packet_Name, UTF8Encoding.UTF8.GetBytes(Name));
                protocol.WritePacket(file, Packet_Size, CompactNum.GetBytes(Size));

                return protocol.WriteFinish();
            }
        }

        internal static PostFile Decode(G2Header root)
        {
            PostFile file = new PostFile();
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
}
