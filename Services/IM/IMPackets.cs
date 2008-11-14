using System;
using System.Collections.Generic;
using System.Text;

using RiseOp.Implementation.Protocol;


namespace RiseOp.Services.IM
{
    internal class IMPacket
    {
        internal const byte Alive = 0x10;
        internal const byte Message = 0x20;
    }

    internal class MessageData : G2Packet
    {
        const byte Packet_Text = 0x10;
        const byte Packet_Format = 0x20;
        const byte Packet_TargetID = 0x30;


        internal string Text;
        internal TextFormat Format;
        internal ulong TargetID; // used to sync duplicate clients


        internal MessageData()
        {
        }

        internal MessageData(string text, TextFormat format)
        {
            Text = text;
            Format = format;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame msg = protocol.WritePacket(null, IMPacket.Message, null);

                protocol.WritePacket(msg, Packet_Text, UTF8Encoding.UTF8.GetBytes(Text));
                protocol.WritePacket(msg, Packet_Format, CompactNum.GetBytes((int)Format));

                if(TargetID != 0)
                    protocol.WritePacket(msg, Packet_TargetID, BitConverter.GetBytes(TargetID));

                return protocol.WriteFinish();
            }
        }

        internal static MessageData Decode(G2Header root)
        {
            MessageData msg = new MessageData();

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Text:
                        msg.Text = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Format:
                        msg.Format = (TextFormat)CompactNum.ToInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_TargetID:
                        msg.TargetID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;
                }
            }

            return msg;
        }
    }

    internal class IMKeepAlive : G2Packet
    {
        internal IMKeepAlive()
        {
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                protocol.WritePacket(null, IMPacket.Alive, null);

                return protocol.WriteFinish();
            }
        }
    }
}
