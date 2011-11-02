using System;
using System.Collections.Generic;
using System.Text;

using DeOps.Implementation.Protocol;


namespace DeOps.Services.Chat
{
    public class ChatPacket
    {
        public const byte Data = 0x10;
        public const byte Status = 0x20;
        public const byte Invite = 0x30;
        public const byte Who = 0x40;
    }

    public class ChatText : G2Packet
    {
        const byte Packet_ID = 0x10;
        const byte Packet_Kind = 0x20;
        const byte Packet_Text = 0x30;
        const byte Packet_Format = 0x40;
        const byte Packet_RoomID = 0x50;

        public uint ProjectID;
        public RoomKind Kind;
        public string Text = "";
        public TextFormat Format;
        public ulong RoomID;


        public ChatText()
        {
        }

        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame chat = protocol.WritePacket(null, ChatPacket.Data, null);

                protocol.WritePacket(chat, Packet_ID, BitConverter.GetBytes(ProjectID));
                protocol.WritePacket(chat, Packet_Kind, BitConverter.GetBytes((int)Kind));
                protocol.WritePacket(chat, Packet_RoomID, BitConverter.GetBytes(RoomID));

                if (Text.Length > 0)
                {
                    protocol.WritePacket(chat, Packet_Text, UTF8Encoding.UTF8.GetBytes(Text));
                    protocol.WritePacket(chat, Packet_Format, CompactNum.GetBytes((int)Format));
                }

                return protocol.WriteFinish();
            }
        }

        public static ChatText Decode(G2Header root)
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
                        chat.ProjectID = BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Kind:
                        chat.Kind = (RoomKind) BitConverter.ToUInt32(child.Data, child.PayloadPos);
                        break;

                    case Packet_Text:
                        chat.Text = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Format:
                        chat.Format = (TextFormat)CompactNum.ToInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_RoomID:
                        chat.RoomID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;
                }
            }

            return chat;
        }
    }

    public class ChatStatus : G2Packet
    {
        const byte Packet_Active = 0x10;

        public List<ulong> ActiveRooms = new List<ulong>();


        public override byte[] Encode(G2Protocol protocol)
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


        public static ChatStatus Decode(G2Header root)
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



    public class ChatInvite : G2Packet
    {
        const byte Packet_RoomID = 0x10;
        const byte Packet_Title = 0x20;
        const byte Packet_SignedInvite = 0x30;
        const byte Packet_Host = 0x40;


        public ulong RoomID;
        public string Title;
        public byte[] Host;
        public byte[] SignedInvite;

        public ChatInvite()
        {
        }

        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame invite = protocol.WritePacket(null, ChatPacket.Invite , null);

                protocol.WritePacket(invite, Packet_RoomID, BitConverter.GetBytes(RoomID));

                if (Host != null)
                    protocol.WritePacket(invite, Packet_Host, Host);

                if(SignedInvite != null)
                    protocol.WritePacket(invite, Packet_SignedInvite, SignedInvite);

                if (Title.Length > 0)
                    protocol.WritePacket(invite, Packet_Title, UTF8Encoding.UTF8.GetBytes(Title));

                return protocol.WriteFinish();
            }
        }

        public static ChatInvite Decode(G2Header root)
        {
            ChatInvite invite = new ChatInvite();

            G2Protocol.ResetPacket(root);

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_RoomID:
                        invite.RoomID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Title:
                        invite.Title = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Host:
                        invite.Host = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_SignedInvite:
                        invite.SignedInvite = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return invite;
        }
    }


    public class ChatWho : G2Packet
    {
        const byte Packet_Request = 0x10;
        const byte Packet_RoomID  = 0x20;
        const byte Packet_Members = 0x30;


        public bool Request;
        public ulong RoomID;
        public List<ulong> Members = new List<ulong>();


        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame who = protocol.WritePacket(null, ChatPacket.Who, null);

                protocol.WritePacket(who, Packet_Request, BitConverter.GetBytes(Request));
                protocol.WritePacket(who, Packet_RoomID, BitConverter.GetBytes(RoomID));


                int offset = 0;
                byte[] buffer = new byte[Members.Count * 8];

                foreach (UInt64 id in Members)
                {
                    BitConverter.GetBytes(id).CopyTo(buffer, offset);
                    offset += 8;
                }

                protocol.WritePacket(who, Packet_Members, buffer);

                return protocol.WriteFinish();
            }
        }


        public static ChatWho Decode(G2Header root)
        {
            ChatWho who = new ChatWho();

            G2Protocol.ResetPacket(root);

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Request:
                        who.Request = BitConverter.ToBoolean(child.Data, child.PayloadPos);
                        break;

                    case Packet_RoomID:
                        who.RoomID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_Members:
                        if (child.PayloadSize % 8 == 0)
                        {
                            int offset = 0;

                            while (offset < child.PayloadSize)
                            {
                                UInt64 id = BitConverter.ToUInt64(child.Data, child.PayloadPos + offset);
                                who.Members.Add(id);

                                offset += 8;
                            }
                        }
                        break;
                }
            }

            return who;
        }
    }
}
