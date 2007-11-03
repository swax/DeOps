using System;
using System.Drawing;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using DeOps.Implementation.Protocol;


namespace DeOps.Components.Plan
{
    internal class PlanPacket
    {
        internal const byte Header = 0x10;
        internal const byte Block  = 0x20;
        internal const byte Goal   = 0x30;
        internal const byte Item   = 0x40;
    }

    internal class PlanHeader : G2Packet
    {
        const byte Packet_Key = 0x10;
        const byte Packet_Version = 0x20;
        const byte Packet_FileHash = 0x30;
        const byte Packet_FileSize = 0x40;
        const byte Packet_FileKey = 0x50;


        internal byte[] Key;
        internal uint Version;
        internal byte[] FileHash;
        internal long FileSize;
        internal RijndaelManaged FileKey = new RijndaelManaged();

        internal ulong KeyID;


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame header = protocol.WritePacket(null, PlanPacket.Header, null);

                protocol.WritePacket(header, Packet_Key, Key);
                protocol.WritePacket(header, Packet_Version, BitConverter.GetBytes(Version));
                protocol.WritePacket(header, Packet_FileHash, FileHash);
                protocol.WritePacket(header, Packet_FileSize, BitConverter.GetBytes(FileSize));
                protocol.WritePacket(header, Packet_FileKey, FileKey.Key);

                return protocol.WriteFinish();
            }
        }

        internal static PlanHeader Decode(G2Protocol protocol, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!protocol.ReadPacket(root))
                return null;

            if (root.Name != PlanPacket.Header)
                return null;

            return PlanHeader.Decode(protocol, root);
        }

        internal static PlanHeader Decode(G2Protocol protocol, G2Header root)
        {
            PlanHeader header = new PlanHeader();
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

                    case Packet_FileKey:
                        header.FileKey.Key = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        header.FileKey.IV = new byte[header.FileKey.IV.Length];
                        break;
                }
            }

            return header;
        }
    }


    internal class PlanBlock : G2Packet
    {
        const byte Packet_ProjectID = 0x10;
        const byte Packet_Title = 0x20;
        const byte Packet_StartTime = 0x30;
        const byte Packet_EndTime = 0x40;
        const byte Packet_Description = 0x60;
        const byte Packet_Unique = 0x70;
        //const byte Packet_Personal = 0x80;
        const byte Packet_Scope = 0x90;


        internal uint ProjectID;
        internal string Title = "";
        internal DateTime StartTime;
        internal DateTime EndTime;
        internal string Description = "";
        internal short Scope;
        internal int Unique;


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame block = protocol.WritePacket(null, PlanPacket.Block, null);

                protocol.WritePacket(block, Packet_ProjectID, BitConverter.GetBytes(ProjectID));
                protocol.WritePacket(block, Packet_Title, protocol.UTF.GetBytes(Title));
                protocol.WritePacket(block, Packet_StartTime, BitConverter.GetBytes(StartTime.ToBinary()));
                protocol.WritePacket(block, Packet_EndTime, BitConverter.GetBytes(EndTime.ToBinary()));
                protocol.WritePacket(block, Packet_Description, protocol.UTF.GetBytes(Description));
                protocol.WritePacket(block, Packet_Scope, BitConverter.GetBytes(Scope));
                protocol.WritePacket(block, Packet_Unique, BitConverter.GetBytes(Unique));

                return protocol.WriteFinish();
            }
        }

        internal static PlanBlock Decode(G2Protocol protocol, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!protocol.ReadPacket(root))
                return null;

            if (root.Name != PlanPacket.Block)
                return null;

            return PlanBlock.Decode(protocol, root);
        }

        internal static PlanBlock Decode(G2Protocol protocol, G2Header root)
        {
            PlanBlock block = new PlanBlock();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_ProjectID:
                        block.ProjectID = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Title:
                        block.Title = protocol.UTF.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_StartTime:
                        block.StartTime = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_EndTime:
                        block.EndTime = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_Description:
                        block.Description = protocol.UTF.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Scope:
                        block.Scope = BitConverter.ToInt16(child.Data, child.PayloadPos);
                        break;

                    case Packet_Unique:
                        block.Unique = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;
                }
            }

            return block;
        }
    }

    internal class PlanGoal : G2Packet
    {
        const byte Packet_Ident         = 0x10;
        const byte Packet_Project       = 0x20;
        const byte Packet_BranchUp      = 0x30;
        const byte Packet_BranchDown    = 0x40;
        const byte Packet_Title         = 0x50;
        const byte Packet_End           = 0x60;
        const byte Packet_Desc          = 0x70;
        const byte Packet_Person        = 0x80;
        const byte Packet_Archived      = 0x90;
        const byte Packet_EstCompleted  = 0xA0;
        const byte Packet_EstTotal      = 0xB0;

        internal int  Ident;
        internal uint Project;

        internal int BranchUp;
        internal int BranchDown;

        internal string Title = "";
        internal DateTime End;
        internal string Description = "";

        internal ulong Person;

        internal bool Archived;

        internal int EstCompleted;
        internal int EstTotal;


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame goal = protocol.WritePacket(null, PlanPacket.Goal, null);

                protocol.WritePacket(goal, Packet_Ident, BitConverter.GetBytes(Ident));
                protocol.WritePacket(goal, Packet_Project, BitConverter.GetBytes(Project));

                protocol.WritePacket(goal, Packet_BranchUp, BitConverter.GetBytes(BranchUp));
                protocol.WritePacket(goal, Packet_BranchDown, BitConverter.GetBytes(BranchDown));

                protocol.WritePacket(goal, Packet_Title, protocol.UTF.GetBytes(Title));
                protocol.WritePacket(goal, Packet_End, BitConverter.GetBytes(End.ToBinary()));
                protocol.WritePacket(goal, Packet_Desc, protocol.UTF.GetBytes(Description));

                protocol.WritePacket(goal, Packet_Person, BitConverter.GetBytes(Person));

                protocol.WritePacket(goal, Packet_Archived, BitConverter.GetBytes(Archived));

                protocol.WritePacket(goal, Packet_EstCompleted, BitConverter.GetBytes(EstCompleted));
                protocol.WritePacket(goal, Packet_EstTotal, BitConverter.GetBytes(EstTotal));

                return protocol.WriteFinish();
            }
        }

        internal static PlanGoal Decode(G2Protocol protocol, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!protocol.ReadPacket(root))
                return null;

            if (root.Name != PlanPacket.Goal)
                return null;

            return PlanGoal.Decode(protocol, root);
        }

        internal static PlanGoal Decode(G2Protocol protocol, G2Header root)
        {
            PlanGoal goal = new PlanGoal();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Ident:
                        goal.Ident = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_BranchUp:
                        goal.BranchUp = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_BranchDown:
                        goal.BranchDown = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Project:
                        goal.Project = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Title:
                        goal.Title = protocol.UTF.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_End:
                        goal.End = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_Desc:
                        goal.Description = protocol.UTF.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Person:
                        goal.Person = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Archived:
                        goal.Archived = BitConverter.ToBoolean(child.Data, child.PayloadPos);
                        break;

                    case Packet_EstCompleted:
                        goal.EstCompleted = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_EstTotal:
                        goal.EstTotal = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;
                }
            }

            return goal;
        }
    }

    internal class PlanItem : G2Packet
    {
        const byte Packet_Ident     = 0x10;
        const byte Packet_Project   = 0x20;
        const byte Packet_BranchUp  = 0x30;
        const byte Packet_Title     = 0x40;
        const byte Packet_Start     = 0x50;
        const byte Packet_End       = 0x60;
        const byte Packet_Desc      = 0x70;
        const byte Packet_HoursCompleted = 0x80;
        const byte Packet_HoursTotal     = 0x90;


        internal int  Ident;
        internal uint Project;
        internal int  BranchUp;

        internal string   Title = "";
         DateTime Start;
         DateTime End;
        internal string   Description = "";

        internal int HoursCompleted;
        internal int HoursTotal;


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame task = protocol.WritePacket(null, PlanPacket.Item, null);

                protocol.WritePacket(task, Packet_Ident, BitConverter.GetBytes(Ident));
                protocol.WritePacket(task, Packet_Project, BitConverter.GetBytes(Project));
                protocol.WritePacket(task, Packet_BranchUp, BitConverter.GetBytes(BranchUp));

                protocol.WritePacket(task, Packet_Title, protocol.UTF.GetBytes(Title));
                protocol.WritePacket(task, Packet_Start, BitConverter.GetBytes(Start.ToBinary()));
                protocol.WritePacket(task, Packet_End, BitConverter.GetBytes(End.ToBinary()));
                protocol.WritePacket(task, Packet_Desc, protocol.UTF.GetBytes(Description));

                protocol.WritePacket(task, Packet_HoursCompleted, BitConverter.GetBytes(HoursCompleted));
                protocol.WritePacket(task, Packet_HoursTotal, BitConverter.GetBytes(HoursTotal));

                return protocol.WriteFinish();
            }
        }

        internal static PlanItem Decode(G2Protocol protocol, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!protocol.ReadPacket(root))
                return null;

            if (root.Name != PlanPacket.Item)
                return null;

            return PlanItem.Decode(protocol, root);
        }

        internal static PlanItem Decode(G2Protocol protocol, G2Header root)
        {
            PlanItem item = new PlanItem();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Ident:
                        item.Ident = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_BranchUp:
                        item.BranchUp = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Project:
                        item.Project = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Title:
                        item.Title = protocol.UTF.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Start:
                        item.Start = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_End:
                        item.End = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_Desc:
                        item.Description = protocol.UTF.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_HoursCompleted:
                        item.HoursCompleted = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_HoursTotal:
                        item.HoursTotal = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;
                }
            }

            return item;
        }
    }
}