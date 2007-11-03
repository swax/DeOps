using System;
using System.Collections.Generic;
using System.Text;

using DeOps.Implementation.Protocol;


namespace DeOps.Components.IM
{
    internal class IMPacket
    {
        internal const byte Alive = 0x10;
        internal const byte Message = 0x20;
    }

    internal class MessageData : G2Packet
    {
        const byte Packet_Text = 0x10;

        internal string Text;


        internal MessageData()
        {
        }

        internal MessageData(string text)
        {
            Text = text;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame msg = protocol.WritePacket(null, IMPacket.Message, null);

                protocol.WritePacket(msg, Packet_Text, protocol.UTF.GetBytes(Text));

                return protocol.WriteFinish();
            }
        }

        internal static MessageData Decode(G2Protocol protocol, G2Header root)
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
                        msg.Text = protocol.UTF.GetString(child.Data, child.PayloadPos, child.PayloadSize);
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
