using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Security.Cryptography;
using System.Net;

using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Transport;

/* Built in Service IDs
 * Dht          0
 * Trust        1
 * Location     2
 * Transfer     3
 * Profile      4
 * IM           5
 * Chat         6
 * Mail         7
 * Board        8
 * Plan         9
 * Storage     10
 * LocalSync   11
 * Global      12
 */

namespace RiseOp.Services
{
    public enum InterfaceMenuType { Internal, External, Settings, Quick };

    public class MenuItemInfo
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
        internal ulong UserID;
        internal uint ProjectID;
        internal bool ShowRemote;
        internal Icon Symbol;
        internal EventHandler ClickEvent;

        internal NewsItemInfo(string message, ulong id, uint project, bool showRemote, Icon symbol, EventHandler onClick)
        {
            Message = message;
            UserID = id;
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

     public interface OpService : IDisposable 
    {
         List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong user, uint project);

         string Name { get; }
         uint ServiceID { get; }

         void SimTest();
         void SimCleanup();
    }

    internal class DataPacket
    {
        internal const byte SignedData = 0x10;
        internal const byte VersionedFile = 0x20;
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

        internal static SignedData Decode(byte[] data)
        {
            G2Header root = new G2Header(data);

            if (!G2Protocol.ReadPacket(root))
                return null;

            if (root.Name != DataPacket.SignedData)
                return null;

            return SignedData.Decode(root);
        }

        internal static SignedData Decode(G2Header root)
        {
            SignedData signed = new SignedData();
            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
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
