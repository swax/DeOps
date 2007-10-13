using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Security.Cryptography;
using System.Net;

using DeOps.Implementation.Protocol;
using DeOps.Implementation.Transport;


namespace DeOps.Components
{
    internal enum InterfaceMenuType { Internal, External, Settings, Quick };

    internal class MenuItemInfo
    {
        internal string Path;
        internal Icon Symbol;
        internal EventHandler ClickEvent;

        internal MenuItemInfo(string path, Icon symbol, EventHandler onClick)
        {
            Path = path;
            Symbol = symbol;
            ClickEvent = onClick;
        }
    }

    internal class NewsItemInfo
    {
        internal string Message;
        internal ulong DhtID;
        internal uint ProjectID;
        internal bool ShowRemote;
        internal Icon Symbol;
        internal EventHandler ClickEvent;

        internal NewsItemInfo(string message, ulong id, uint project, bool showRemote, Icon symbol, EventHandler onClick)
        {
            Message = message;
            DhtID = id;
            ProjectID = project;
            Symbol = symbol;
            ShowRemote = showRemote;
            ClickEvent = onClick;
        }
    }

    internal interface IViewParams
    {
        ulong GetKey();
        uint  GetProject();
        bool  IsExternal();
    }


    internal class ComponentID
    {
        internal const ushort Node     =  0;
        internal const ushort Link     =  1;
        internal const ushort Location =  2;
        internal const ushort Transfer =  3;
        internal const ushort Profile  =  4;
        internal const ushort IM       =  5;
        internal const ushort Chat     =  6;
        internal const ushort Mail     =  7;
        internal const ushort Board    =  8;
        internal const ushort Plan     =  9;
        internal const ushort Storage  = 10;

        internal static string GetName(ushort id)
        {
            switch (id)
            {
                case Node:
                    return "Node";
                case Link:
                    return "Link";
                case Location:
                    return "Location";
                case Transfer:
                    return "Transfer";
                case Profile:
                    return "Profile";
                case IM:
                    return "IM";
                case Chat:
                    return "Chat";
                case Mail:
                    return "Mail";
                case Board:
                    return "Board";
                case Plan:
                    return "Plan";
                case Storage:
                    return "Storage";
            }

            return "Unknown";
        }
    }

    internal abstract class OpComponent
    {
        internal virtual List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong key, uint proj)
        {
            return null;
        }

        internal virtual void GetActiveSessions(ref ActiveSessions active)
        {

        }
    }

    internal class DataPacket
    {
        internal const byte SignedData = 0x10;
    }

    internal class SignedData : G2Packet
    {
        const byte Packet_Signature = 0x10;
        const byte Packet_Data = 0x20;


        internal byte[] Signature;
        internal byte[] Data;

        internal SignedData()
        {
        }

        internal SignedData(G2Protocol protocol, RSACryptoServiceProvider key, G2Packet packet)
        {
            Data = packet.Encode(protocol);

            Signature = key.SignData(Data, new SHA1CryptoServiceProvider());
        }

        internal static byte[] Encode(G2Protocol protocol, RSACryptoServiceProvider key, G2Packet packet)
        {
            byte[] data = packet.Encode(protocol);

            return Encode(protocol, key, data);
        }
          
        internal static byte[] Encode(G2Protocol protocol, RSACryptoServiceProvider key, byte[] data)
        {
            lock (protocol.WriteSection)
            {
                G2Frame signed = protocol.WritePacket(null, DataPacket.SignedData, null);

                protocol.WritePacket(signed, Packet_Signature, key.SignData(data, new SHA1CryptoServiceProvider()));
                protocol.WritePacket(signed, Packet_Data, data);

                return protocol.WriteFinish();
            }
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame signed = protocol.WritePacket(null, DataPacket.SignedData, null);

                protocol.WritePacket(signed, Packet_Signature, Signature);
                protocol.WritePacket(signed, Packet_Data, Data);

                return protocol.WriteFinish();
            }
        }

        internal static SignedData Decode(G2Protocol protocol, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!protocol.ReadPacket(root))
                return null;

            if (root.Name != DataPacket.SignedData)
                return null;

            return SignedData.Decode(protocol, root);
        }

        internal static SignedData Decode(G2Protocol protocol, G2Header root)
        {
            SignedData signed = new SignedData();
            G2Header child = new G2Header(root.Data);

            while (protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Signature:
                        signed.Signature = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Data:
                        signed.Data = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return signed;
        }

        bool VerifySignature(RSACryptoServiceProvider key)
        {
            return key.VerifyData(Data, new SHA1CryptoServiceProvider(), Data);
        }
    }
}
