using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Security.Cryptography;
using System.Net;

using DeOps.Implementation.Protocol;
using DeOps.Implementation.Transport;


namespace DeOps.Services
{
    // Built-in Service IDs
    public static class ServiceIDs
    {
        public static uint Dht = 0;
        public static uint Trust = 1;
        public static uint Location = 2;
        public static uint Transfer = 3;
        public static uint Profile = 4;
        public static uint IM = 5;
        public static uint Chat = 6;
        public static uint Mail = 7;
        public static uint Board = 8;
        public static uint Plan = 9;
        public static uint Storage = 10;
        public static uint LocalSync = 11;
        public static uint Lookup = 12;
        public static uint Buddy = 13;
        public static uint Share = 14;
        public static uint Update = 15;
        public static uint Voice = 16;
    }
    
    public enum InterfaceMenuType { Internal, External, Settings, Quick };

    public class MenuItemInfo
    {
        internal string Path;
        internal Image Symbol;
        internal EventHandler ClickEvent;

        internal MenuItemInfo(string path, Icon symbol, EventHandler onClick)
        {
            Path = path;
            Symbol = symbol.ToBitmap();
            ClickEvent = onClick;
        }

        internal MenuItemInfo(string path, Image symbol, EventHandler onClick)
        {
            Path = path;
            Symbol = symbol;
            ClickEvent = onClick;
        }
    }

    internal interface IViewParams
    {
        ulong GetUser();
        uint  GetProject();
        bool  IsExternal();
    }

    public interface OpService : IDisposable
    {
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
