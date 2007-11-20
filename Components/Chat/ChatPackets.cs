using System;
using System.Collections.Generic;
using System.Text;

using DeOps.Implementation.Protocol;


namespace DeOps.Components.Chat
{
    internal class ChatPacket
    {
        internal const byte Data = 0x10;
        internal const byte Status = 0x20;
    }

    //internal enum ChatPacketType { Invite, Join, Leave, Message, Who, Unknown };

    internal class ChatText : G2Packet
    {
        const byte Packet_ID = 0x10;
        //const byte Packet_Type = 0x20;
       // const byte Packet_User = 0x30;
        const byte Packet_Text = 0x50;


        internal uint RoomID;
        //internal ChatPacketType Type;
        //internal UInt64 UserID;
        //internal List<ulong> UserIDs = new List<ulong>();
        internal string Text = "";

        internal ChatText()
        {
            //Type = type;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame chat = protocol.WritePacket(null, ChatPacket.Data, null);

                protocol.WritePacket(chat, Packet_ID, BitConverter.GetBytes(RoomID));
                //protocol.WritePacket(chat, Packet_Type, protocol.UTF.GetBytes(TypeToString(Type)));

                /*if (UserID != 0 && !UserIDs.Contains(UserID))
                    UserIDs.Add(UserID);

                int offset = 0;
                byte[] buffer = new byte[UserIDs.Count * 8];

                foreach (UInt64 id in UserIDs)
                {
                    BitConverter.GetBytes(id).CopyTo(buffer, offset);
                    offset += 8;
                }

                protocol.WritePacket(chat, Packet_User, buffer);
*/
                if (Text.Length > 0)
                    protocol.WritePacket(chat, Packet_Text, protocol.UTF.GetBytes(Text));
                

                return protocol.WriteFinish();
            }
        }

        internal static ChatText Decode(G2Protocol protocol, G2Header root)
        {
            ChatText chat = new ChatText();

            G2Protocol.ResetPacket(root);

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_ID:
                        chat.RoomID = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    /*case Packet_Type:
                        chat.Type = StringToType(protocol.UTF.GetString(child.Data, child.PayloadPos, child.PayloadSize));
                        break;

                    case Packet_User:
                        if (child.PayloadSize % 8 == 0)
                        {
                            int offset = 0;

                            while (offset < child.PayloadSize)
                            {
                                UInt64 id = BitConverter.ToUInt64(child.Data, child.PayloadPos + offset);
                                chat.UserIDs.Add(id);

                                offset += 8;
                            }

                            if (chat.UserIDs.Count > 0)
                                chat.UserID = (UInt64)chat.UserIDs[0];
                        }
                        break;*/

                    case Packet_Text:
                        chat.Text = protocol.UTF.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return chat;
        }

        /*static string TypeToString(ChatPacketType type)
        {
            switch (type)
            {
                case ChatPacketType.Invite:
                    return "INV";
                case ChatPacketType.Join:
                    return "JOIN";
                case ChatPacketType.Leave:
                    return "LEAVE";
                case ChatPacketType.Message:
                    return "MSG";
                case ChatPacketType.Who:
                    return "WHO";
            }

            return "UNKN";
        }

        static ChatPacketType StringToType(string type)
        {
            switch (type)
            {
                case "INV":
                    return ChatPacketType.Invite;
                case "JOIN":
                    return ChatPacketType.Join;
                case "LEAVE":
                    return ChatPacketType.Leave;
                case "MSG":
                    return ChatPacketType.Message;
                case "WHO":
                    return ChatPacketType.Who;
            }

            return ChatPacketType.Unknown;
        }*/
    }

    internal class ChatStatus : G2Packet
    {
        const byte Packet_Active = 0x10;

        internal List<ulong> ActiveRooms = new List<ulong>();


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame status = protocol.WritePacket(null, ChatPacket.Status, null);

                int offset = 0;
                byte[] buffer = new byte[ActiveRooms.Count * 8];

                foreach (UInt64 id in ActiveRooms)
                {
                    BitConverter.GetBytes(id).CopyTo(buffer, offset);
                    offset += 8;
                }

                protocol.WritePacket(status, Packet_Active, buffer);

                return protocol.WriteFinish();
            }
        }


        internal static ChatStatus Decode(G2Protocol protocol, G2Header root)
        {
            ChatStatus status = new ChatStatus();

            G2Protocol.ResetPacket(root);

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            { 
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Active:
                        if (child.PayloadSize % 8 == 0)
                        {
                            int offset = 0;

                            while (offset < child.PayloadSize)
                            {
                                UInt64 id = BitConverter.ToUInt64(child.Data, child.PayloadPos + offset);
                                status.ActiveRooms.Add(id);

                                offset += 8;
                            }
                        }
                        break;
                }
            }

            return status;
        }
    }
}
