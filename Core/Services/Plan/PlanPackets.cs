using System;
using System.Drawing;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using DeOps.Implementation.Protocol;


namespace DeOps.Services.Plan
{
    public class PlanPacket
    {
        public const byte Block  = 0x10;
        public const byte Goal   = 0x20;
        public const byte Item   = 0x30;
    }

    public class PlanBlock : G2Packet
    {
        const byte Packet_ProjectID = 0x10;
        const byte Packet_Title = 0x20;
        const byte Packet_StartTime = 0x30;
        const byte Packet_EndTime = 0x40;
        const byte Packet_Description = 0x60;
        const byte Packet_Unique = 0x70;
        //const byte Packet_Personal = 0x80;
        const byte Packet_Scope = 0x90;


        public uint ProjectID;
        public string Title = "";
        public DateTime StartTime;
        public DateTime EndTime;
        public string Description = "";
        public short Scope;
        public int Unique;


        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame block = protocol.WritePacket(null, PlanPacket.Block, null);

                protocol.WritePacket(block, Packet_ProjectID, BitConverter.GetBytes(ProjectID));
                protocol.WritePacket(block, Packet_Title, UTF8Encoding.UTF8.GetBytes(Title));
                protocol.WritePacket(block, Packet_StartTime, BitConverter.GetBytes(StartTime.ToBinary()));
                protocol.WritePacket(block, Packet_EndTime, BitConverter.GetBytes(EndTime.ToBinary()));
                protocol.WritePacket(block, Packet_Description, UTF8Encoding.UTF8.GetBytes(Description));
                protocol.WritePacket(block, Packet_Scope, BitConverter.GetBytes(Scope));
                protocol.WritePacket(block, Packet_Unique, BitConverter.GetBytes(Unique));

                return protocol.WriteFinish();
            }
        }

        public static PlanBlock Decode(byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!G2Protocol.ReadPacket(root))
                return null;

            if (root.Name != PlanPacket.Block)
                return null;

            return PlanBlock.Decode(root);
        }

        public static PlanBlock Decode(G2Header root)
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
                        block.Title = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_StartTime:
                        block.StartTime = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_EndTime:
                        block.EndTime = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_Description:
                        block.Description = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
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

    public class PlanGoal : G2Packet
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

        public int  Ident;
        public uint Project;

        public int BranchUp;
        public int BranchDown;

        public string Title = "";
        public DateTime End;
        public string Description = "";

        public ulong Person;

        public bool Archived;

        public int EstCompleted;
        public int EstTotal;


        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame goal = protocol.WritePacket(null, PlanPacket.Goal, null);

                protocol.WritePacket(goal, Packet_Ident, BitConverter.GetBytes(Ident));
                protocol.WritePacket(goal, Packet_Project, BitConverter.GetBytes(Project));

                protocol.WritePacket(goal, Packet_BranchUp, BitConverter.GetBytes(BranchUp));
                protocol.WritePacket(goal, Packet_BranchDown, BitConverter.GetBytes(BranchDown));

                protocol.WritePacket(goal, Packet_Title, UTF8Encoding.UTF8.GetBytes(Title));
                protocol.WritePacket(goal, Packet_End, BitConverter.GetBytes(End.ToBinary()));
                protocol.WritePacket(goal, Packet_Desc, UTF8Encoding.UTF8.GetBytes(Description));

                protocol.WritePacket(goal, Packet_Person, BitConverter.GetBytes(Person));

                protocol.WritePacket(goal, Packet_Archived, BitConverter.GetBytes(Archived));

                protocol.WritePacket(goal, Packet_EstCompleted, BitConverter.GetBytes(EstCompleted));
                protocol.WritePacket(goal, Packet_EstTotal, BitConverter.GetBytes(EstTotal));

                return protocol.WriteFinish();
            }
        }

        public static PlanGoal Decode(byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!G2Protocol.ReadPacket(root))
                return null;

            if (root.Name != PlanPacket.Goal)
                return null;

            return PlanGoal.Decode(root);
        }

        public static PlanGoal Decode(G2Header root)
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
                        goal.Title = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_End:
                        goal.End = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_Desc:
                        goal.Description = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
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

    public class PlanItem : G2Packet
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


        public int  Ident;
        public uint Project;
        public int  BranchUp;

        public string   Title = "";
         DateTime Start;
         DateTime End;
        public string   Description = "";

        public int HoursCompleted;
        public int HoursTotal;


        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame task = protocol.WritePacket(null, PlanPacket.Item, null);

                protocol.WritePacket(task, Packet_Ident, BitConverter.GetBytes(Ident));
                protocol.WritePacket(task, Packet_Project, BitConverter.GetBytes(Project));
                protocol.WritePacket(task, Packet_BranchUp, BitConverter.GetBytes(BranchUp));

                protocol.WritePacket(task, Packet_Title, UTF8Encoding.UTF8.GetBytes(Title));
                protocol.WritePacket(task, Packet_Start, BitConverter.GetBytes(Start.ToBinary()));
                protocol.WritePacket(task, Packet_End, BitConverter.GetBytes(End.ToBinary()));
                protocol.WritePacket(task, Packet_Desc, UTF8Encoding.UTF8.GetBytes(Description));

                protocol.WritePacket(task, Packet_HoursCompleted, BitConverter.GetBytes(HoursCompleted));
                protocol.WritePacket(task, Packet_HoursTotal, BitConverter.GetBytes(HoursTotal));

                return protocol.WriteFinish();
            }
        }

        public static PlanItem Decode(byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!G2Protocol.ReadPacket(root))
                return null;

            if (root.Name != PlanPacket.Item)
                return null;

            return PlanItem.Decode(root);
        }

        public static PlanItem Decode(G2Header root)
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
                        item.Title = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Start:
                        item.Start = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_End:
                        item.End = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_Desc:
                        item.Description = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
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