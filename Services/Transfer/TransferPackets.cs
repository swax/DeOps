using System;
using System.Collections.Generic;
using System.Text;

using DeOps.Implementation.Protocol;


namespace DeOps.Services.Transfer
{
    internal class TransferPacket
    {
        internal const byte Params = 0x10;
        internal const byte Request = 0x20;
        internal const byte Ack = 0x30;
        internal const byte Data = 0x40;
    }

    internal class FileDetails : G2Packet
    {
        const byte Packet_Component = 0x10;
        const byte Packet_Hash  = 0x20;
        const byte Packet_Size  = 0x30;
        const byte Packet_Extra = 0x40;

        internal ushort Component;
        internal byte[] Hash;
        internal long   Size;
        internal byte[] Extra;


        internal FileDetails()
        {
        }

        internal FileDetails(ushort component, byte[] hash, long size, byte[] extra)
        {
            Component = component;
            Hash = hash;
            Size = size;
            Extra = extra;
        }

        public override bool Equals(object obj)
        {
            FileDetails compare = obj as FileDetails;

            if (obj == null)
                return false;

            if (compare.Component == Component &&
                Utilities.MemCompare(compare.Hash, Hash) &&
                compare.Size == Size &&
                Utilities.MemCompare(compare.Extra, Extra))
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame packet = protocol.WritePacket(null, TransferPacket.Params, null);

                protocol.WritePacket(packet, Packet_Component, BitConverter.GetBytes(Component));
                protocol.WritePacket(packet, Packet_Hash, Hash);
                protocol.WritePacket(packet, Packet_Size, BitConverter.GetBytes(Size));
                protocol.WritePacket(packet, Packet_Extra, Extra);

                return protocol.WriteFinish();
            }
        }

        internal static FileDetails Decode(G2Protocol protocol, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!protocol.ReadPacket(root))
                return null;

            if (root.Name != TransferPacket.Params)
                return null;

            return FileDetails.Decode(protocol, root);
        }

        internal static FileDetails Decode(G2Protocol protocol, G2Header root)
        {
            FileDetails packet = new FileDetails();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Component:
                        packet.Component = BitConverter.ToUInt16(child.Data, child.PayloadPos);
                        break;

                    case Packet_Hash:
                        packet.Hash = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Size:
                        packet.Size = BitConverter.ToInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Extra:
                        packet.Extra = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return packet;
        }
    }

    internal class TransferRequest : G2Packet
    {
        const byte Packet_ID = 0x10;
        const byte Packet_Target = 0x20;
        const byte Packet_Details = 0x30;
        const byte Packet_Start = 0x40;

        internal int TransferID;
        internal ulong Target;
        internal byte[] Details;
        internal long StartByte;

        internal TransferRequest()
        {
        }

        internal TransferRequest(FileDownload file, G2Protocol protocol)
        {
            TransferID = file.ID;
            Target = file.Target;
            Details = file.Details.Encode(protocol);
            StartByte  = file.FilePos;

        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame tr = protocol.WritePacket(null, TransferPacket.Request, null);

                protocol.WritePacket(tr, Packet_ID, BitConverter.GetBytes(TransferID));
                protocol.WritePacket(tr, Packet_Target, BitConverter.GetBytes(Target));
                protocol.WritePacket(tr, Packet_Details, Details);
                protocol.WritePacket(tr, Packet_Start, BitConverter.GetBytes(StartByte));

                return protocol.WriteFinish();
            }
        }

        internal static TransferRequest Decode(G2Protocol protocol, G2Header root)
        {
            TransferRequest tr = new TransferRequest();

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_ID:
                        tr.TransferID = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Target:
                        tr.Target = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Details:
                        tr.Details = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Start:
                        tr.StartByte = BitConverter.ToInt64(child.Data, child.PayloadPos);
                        break;
                }
            }

            return tr;
        }
    }

    internal class TransferAck : G2Packet
    {
        const byte Packet_ID = 0x10;
        const byte Packet_Accept = 0x20;
        const byte Packet_Start = 0x30;


        internal int TransferID;
        internal bool Accept;
        internal ulong StartByte;


        internal TransferAck()
        {
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame ta = protocol.WritePacket(null, TransferPacket.Ack, null);

                protocol.WritePacket(ta, Packet_ID, BitConverter.GetBytes(TransferID));
                protocol.WritePacket(ta, Packet_Accept, BitConverter.GetBytes(Accept));
                protocol.WritePacket(ta, Packet_Start, BitConverter.GetBytes(StartByte));

                return protocol.WriteFinish();
            }
        }

        internal static TransferAck Decode(G2Protocol protocol, G2Header root)
        {
            TransferAck ta = new TransferAck();

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_ID:
                        ta.TransferID = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Accept:
                        ta.Accept = BitConverter.ToBoolean(child.Data, child.PayloadPos);
                        break;

                    case Packet_Start:
                        ta.StartByte = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;
                }
            }

            return ta;
        }
    }

    internal class TransferData : G2Packet
    {
        const byte Packet_ID = 0x10;
        const byte Packet_Start = 0x20;


        internal int TransferID;
        internal long StartByte;
        internal byte[] Data;


        internal TransferData()
        {
        }

        internal TransferData(FileUpload upload)
        {
            TransferID = upload.Request.TransferID;
            StartByte = upload.FilePos - upload.BuffSize;

            if (upload.BuffSize == FileUpload.READ_SIZE)
                Data = upload.Buff;
            else
                Data = Utilities.ExtractBytes(upload.Buff, 0, upload.BuffSize);
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame td = protocol.WritePacket(null, TransferPacket.Data, Data);

                protocol.WritePacket(td, Packet_ID, BitConverter.GetBytes(TransferID));
                protocol.WritePacket(td, Packet_Start, BitConverter.GetBytes(StartByte));

                return protocol.WriteFinish();
            }
        }

        internal static TransferData Decode(G2Protocol protocol, G2Header root)
        {
            TransferData td = new TransferData();

            if (G2Protocol.ReadPayload(root))
                td.Data = Utilities.ExtractBytes(root.Data, root.PayloadPos, root.PayloadSize);

            G2Protocol.ResetPacket(root);

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_ID:
                        td.TransferID = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Start:
                        td.StartByte = BitConverter.ToInt64(child.Data, child.PayloadPos);
                        break;
                }
            }

            return td;
        }
    }
}
