using System;
using System.Collections.Generic;
using System.Text;

using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;


namespace RiseOp.Services.Transfer
{
    internal class TransferPacket
    {
        internal const byte Params = 0x10;
        internal const byte Request = 0x20;
        internal const byte Ack = 0x30;
        internal const byte Data = 0x40;

        internal const byte Ping = 0x50;
        internal const byte Pong = 0x60;
    }

    internal class FileDetails : G2Packet
    {
        const byte Packet_Service = 0x10;
        const byte Packet_DataType = 0x20;
        const byte Packet_Hash  = 0x30;
        const byte Packet_Size  = 0x40;
        const byte Packet_Extra = 0x50;

        internal uint Service;
        internal uint DataType;
        internal byte[] Hash;
        internal long   Size;
        internal byte[] Extra;


        internal FileDetails()
        {
        }

        internal FileDetails(uint service, uint datatype, byte[] hash, long size, byte[] extra)
        {
            Service = service;
            DataType = datatype;
            Hash = hash;
            Size = size;
            Extra = extra;
        }

        public override bool Equals(object obj)
        {
            FileDetails compare = obj as FileDetails;

            if (obj == null)
                return false;

            if (compare.Service == Service &&
                compare.DataType == DataType &&
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

                protocol.WritePacket(packet, Packet_Service, CompactNum.GetBytes(Service));
                protocol.WritePacket(packet, Packet_DataType, CompactNum.GetBytes(DataType));
                protocol.WritePacket(packet, Packet_Hash, Hash);
                protocol.WritePacket(packet, Packet_Size, CompactNum.GetBytes(Size));
                protocol.WritePacket(packet, Packet_Extra, Extra);

                return protocol.WriteFinish();
            }
        }

        internal static FileDetails Decode(byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!G2Protocol.ReadPacket(root))
                return null;

            if (root.Name != TransferPacket.Params)
                return null;

            return FileDetails.Decode(root);
        }

        internal static FileDetails Decode(G2Header root)
        {
            FileDetails packet = new FileDetails();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Service:
                        packet.Service = CompactNum.ToUInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_DataType:
                        packet.DataType = CompactNum.ToUInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Hash:
                        packet.Hash = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Size:
                        packet.Size = CompactNum.ToInt64(child.Data, child.PayloadPos, child.PayloadSize);
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
                protocol.WritePacket(tr, Packet_Start, CompactNum.GetBytes(StartByte));

                return protocol.WriteFinish();
            }
        }

        internal static TransferRequest Decode(G2Header root)
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
                        tr.StartByte = CompactNum.ToInt64(child.Data, child.PayloadPos, child.PayloadSize);
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
        internal long StartByte;


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
                protocol.WritePacket(ta, Packet_Start, CompactNum.GetBytes(StartByte));

                return protocol.WriteFinish();
            }
        }

        internal static TransferAck Decode(G2Header root)
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
                        ta.StartByte = CompactNum.ToInt64(child.Data, child.PayloadPos, child.PayloadSize);
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
                protocol.WritePacket(td, Packet_Start, CompactNum.GetBytes(StartByte));

                return protocol.WriteFinish();
            }
        }

        internal static TransferData Decode(G2Header root)
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
                        td.StartByte = CompactNum.ToInt64(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return td;
        }
    }

    internal class TransferPing : G2Packet
    {
        const byte Packet_Target      = 0x10;
        const byte Packet_Details     = 0x20;
        const byte Packet_Status      = 0x30;
        const byte Packet_RequestInfo = 0x40;
        const byte Packet_RequestAlts = 0x50;
        const byte Packet_BitfieldUpdated = 0x60;


        internal ulong Target; // where file is key'd
        internal FileDetails Details;
        internal TransferStatus Status;
        internal bool RequestInfo; 
        internal bool RequestAlts;
        internal bool BitfieldUpdated;


        internal TransferPing()
        {
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            byte[] details = Details.Encode(protocol); // prevent protocol conflict

            lock (protocol.WriteSection)
            {
                G2Frame ping = protocol.WritePacket(null, TransferPacket.Ping, null);

                protocol.WritePacket(ping, Packet_Target, BitConverter.GetBytes(Target));
                protocol.WritePacket(ping, Packet_Details, details);
                protocol.WritePacket(ping, Packet_Status, BitConverter.GetBytes((int)Status));

                if (RequestInfo)
                    protocol.WritePacket(ping, Packet_RequestInfo, null);

                if(RequestAlts)
                    protocol.WritePacket(ping, Packet_RequestAlts, null);

                if(BitfieldUpdated)
                    protocol.WritePacket(ping, Packet_BitfieldUpdated, null);

                return protocol.WriteFinish();
            }
        }

        internal static TransferPing Decode(G2Header root)
        {
            TransferPing ping = new TransferPing();

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (child.Name == Packet_RequestInfo)
                    ping.RequestInfo = true;

                if(child.Name == Packet_RequestAlts)
                    ping.RequestAlts = true;

                if (child.Name == Packet_BitfieldUpdated)
                    ping.BitfieldUpdated = true;


                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Target:
                        ping.Target = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Details:
                        ping.Details = FileDetails.Decode(Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize));
                        break;

                    case Packet_Status:
                        ping.Status = (TransferStatus) BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;
                }
            }

            return ping;
        }
    }

    internal class TransferPong : G2Packet
    {
        const byte Packet_FileID    = 0x10;
        const byte Packet_Error     = 0x20;
        const byte Packet_Timeout   = 0x30;
        const byte Packet_Status    = 0x40;
        const byte Packet_AltClient = 0x50;
        const byte Packet_AltAddress    = 0x60;
        const byte Packet_InternalSize  = 0x70;
        const byte Packet_ChunkSize     = 0x80;
        const byte Packet_BitCount      = 0x90;


        internal ulong FileID;
        internal bool Error;
        internal int Timeout;
        internal TransferStatus Status;

        internal long InternalSize;
        internal int ChunkSize;
        internal int BitCount;

        internal Dictionary<DhtClient, List<DhtAddress>> Alts = new Dictionary<DhtClient, List<DhtAddress>>();


        internal TransferPong()
        {
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame pong = protocol.WritePacket(null, TransferPacket.Pong, null);

                protocol.WritePacket(pong, Packet_FileID, BitConverter.GetBytes(FileID));
                protocol.WritePacket(pong, Packet_Error, null);
                protocol.WritePacket(pong, Packet_Timeout, BitConverter.GetBytes(Timeout));
                protocol.WritePacket(pong, Packet_Status, BitConverter.GetBytes((int)Status));

                if (InternalSize != 0)
                {
                    protocol.WritePacket(pong, Packet_InternalSize, BitConverter.GetBytes(InternalSize));
                    protocol.WritePacket(pong, Packet_ChunkSize, BitConverter.GetBytes(ChunkSize));
                    protocol.WritePacket(pong, Packet_BitCount, BitConverter.GetBytes(BitCount));
                }

                foreach (DhtClient client in Alts.Keys)
                {
                    G2Frame alt = protocol.WritePacket(pong, Packet_AltClient, client.ToBytes());

                    foreach (DhtAddress address in Alts[client])
                        address.WritePacket(protocol, alt, Packet_AltAddress);
                }

                return protocol.WriteFinish();
            }
        }

        internal static TransferPong Decode(G2Header root)
        {
            TransferPong pong = new TransferPong();

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if(child.Name == Packet_Error)
                    pong.Error = true;

                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_FileID:
                        pong.FileID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Timeout:
                        pong.Timeout = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Status:
                        pong.Status = (TransferStatus)BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_InternalSize:
                        pong.InternalSize = BitConverter.ToInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_ChunkSize:
                        pong.ChunkSize = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_BitCount:
                        pong.BitCount = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_AltClient:
                        DhtClient client = DhtClient.FromBytes(child.Data, child.PayloadPos);
                        pong.Alts[client] = new List<DhtAddress>();

                        G2Protocol.ResetPacket(child);
                        
                        G2Header sub = new G2Header(child.Data);

                        while (G2Protocol.ReadNextChild(child, sub) == G2ReadResult.PACKET_GOOD)
                            if (G2Protocol.ReadPayload(sub))
                                if (sub.Name == Packet_AltAddress)
                                    pong.Alts[client].Add(DhtAddress.ReadPacket(sub));

                        break;
                }
            }

            return pong;
        }
    }

    internal class TransferRequest2 : G2Packet
    {
        const byte Packet_FileID     = 0x10;
        const byte Packet_ChunkIndex = 0x20;
        const byte Packet_StartByte  = 0x30;
        const byte Packet_EndByte    = 0x40;
        const byte Packet_GetBitfield = 0x50;


        internal ulong FileID;
        internal int   ChunkIndex;
        internal long  StartByte;
        internal long  EndByte;
        internal bool GetBitfield;


        internal TransferRequest2()
        {
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame tr = protocol.WritePacket(null, TransferPacket.Request, null);

                protocol.WritePacket(tr, Packet_FileID, BitConverter.GetBytes(FileID));
                protocol.WritePacket(tr, Packet_ChunkIndex, BitConverter.GetBytes(ChunkIndex));
                protocol.WritePacket(tr, Packet_StartByte, BitConverter.GetBytes(StartByte));
                protocol.WritePacket(tr, Packet_EndByte, BitConverter.GetBytes(EndByte));

                if(GetBitfield)
                    protocol.WritePacket(tr, Packet_GetBitfield, null);

                return protocol.WriteFinish();
            }
        }

        internal static TransferRequest2 Decode(G2Header root)
        {
            TransferRequest2 tr = new TransferRequest2();

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (child.Name == Packet_GetBitfield)
                    tr.GetBitfield = true;

                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_FileID:
                        tr.FileID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_ChunkIndex:
                        tr.ChunkIndex = BitConverter.ToInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_StartByte:
                        tr.StartByte = BitConverter.ToInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_EndByte:
                        tr.EndByte = BitConverter.ToInt64(child.Data, child.PayloadPos);
                        break;
                }
            }

            return tr;
        }
    }

    internal class TransferAck2 : G2Packet
    {
        const byte Packet_FileID = 0x10;
        const byte Packet_Error = 0x20;
        const byte Packet_StartByte = 0x30;
        const byte Packet_Bitfield = 0x20;


        internal ulong FileID;
        internal bool Error;
        internal long StartByte;
        internal byte[] Bitfield;


        internal TransferAck2()
        {
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame ack = protocol.WritePacket(null, TransferPacket.Ack, null);

                protocol.WritePacket(ack, Packet_FileID, BitConverter.GetBytes(FileID));
                protocol.WritePacket(ack, Packet_Error, null);
                protocol.WritePacket(ack, Packet_StartByte, BitConverter.GetBytes(StartByte));

                if(Bitfield != null)
                    protocol.WritePacket(ack, Packet_Bitfield, Bitfield);

                return protocol.WriteFinish();
            }
        }

        internal static TransferAck2 Decode(G2Header root)
        {
            TransferAck2 ack = new TransferAck2();

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (child.Name == Packet_Error)
                    ack.Error = true;

                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_FileID:
                        ack.FileID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_StartByte:
                        ack.StartByte = BitConverter.ToInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Bitfield:
                        ack.Bitfield = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return ack;
        }
    }
}
