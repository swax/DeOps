using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace RiseOp.Implementation.Protocol.File
{
    internal class FilePacket : G2Packet
    {
        internal const byte Settings = 0x10;
        internal const byte GlobalCache = 0x20;
        internal const byte OperationCache = 0x30;

        // embedded
        internal byte[] Embedded;

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                // network packet
                G2Frame file = protocol.WritePacket(null, RootPacket.File, Embedded);

                return protocol.WriteFinish();
            }
        }

        internal static FilePacket Decode(G2Header root)
        {
            FilePacket file = new FilePacket();

            if (G2Protocol.ReadPayload(root))
                file.Embedded = Utilities.ExtractBytes(root.Data, root.PayloadPos, root.PayloadSize);

            G2Protocol.ResetPacket(root);

            return file;
        }
    }

    internal class SettingsPacket : G2Packet
    {
        const byte Packet_Operation     = 0x10;
        const byte Packet_ScreenName    = 0x20;
        const byte Packet_GlobalID      = 0x30;
        const byte Packet_GlobalPortTcp = 0x40;
        const byte Packet_GlobalPortUdp = 0x50;
        const byte Packet_OpPortTcp     = 0x60;
        const byte Packet_OpPortUdp     = 0x70;
        const byte Packet_OpKey         = 0x80;
        const byte Packet_OpAccess      = 0x90;
        const byte Packet_KeyPair       = 0xA0;
        const byte Packet_Location      = 0xB0;
        const byte Packet_FileKey       = 0xC0;
        const byte Packet_AwayMsg       = 0xD0;

        const byte Key_D        = 0x10;
        const byte Key_DP       = 0x20;
        const byte Key_DQ       = 0x30;
        const byte Key_Exponent = 0x40;
        const byte Key_InverseQ = 0x50;
        const byte Key_Modulus  = 0x60;
        const byte Key_P        = 0x70;
        const byte Key_Q        = 0x80;


        // general
        internal string Operation;
        internal string ScreenName;
        internal string Location = "";
        internal string AwayMessage = "";

        // network
        internal ulong GlobalID;

        internal ushort GlobalPortTcp;
        internal ushort GlobalPortUdp;
        internal ushort OpPortTcp;
        internal ushort OpPortUdp;
        

        // private
        internal RijndaelManaged OpKey = new RijndaelManaged();
        internal AccessType      OpAccess;

        internal RSACryptoServiceProvider KeyPair = new RSACryptoServiceProvider();
        internal byte[] KeyPublic;

        internal RijndaelManaged FileKey = new RijndaelManaged();


        internal SettingsPacket()
        {
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame settings = protocol.WritePacket(null, FilePacket.Settings, null);

                protocol.WritePacket(settings, Packet_Operation,     UTF8Encoding.UTF8.GetBytes(Operation));
                protocol.WritePacket(settings, Packet_ScreenName,    UTF8Encoding.UTF8.GetBytes(ScreenName));
                protocol.WritePacket(settings, Packet_GlobalID,      BitConverter.GetBytes(GlobalID));
                protocol.WritePacket(settings, Packet_GlobalPortTcp, BitConverter.GetBytes(GlobalPortTcp));
                protocol.WritePacket(settings, Packet_GlobalPortUdp, BitConverter.GetBytes(GlobalPortUdp));
                protocol.WritePacket(settings, Packet_OpPortTcp,     BitConverter.GetBytes(OpPortTcp));
                protocol.WritePacket(settings, Packet_OpPortUdp,     BitConverter.GetBytes(OpPortUdp));
                protocol.WritePacket(settings, Packet_Location,      UTF8Encoding.UTF8.GetBytes(Location));
                protocol.WritePacket(settings, Packet_AwayMsg,      UTF8Encoding.UTF8.GetBytes(AwayMessage));

                protocol.WritePacket(settings, Packet_FileKey,  FileKey.Key);
                protocol.WritePacket(settings, Packet_OpKey,    OpKey.Key);
                protocol.WritePacket(settings, Packet_OpAccess, BitConverter.GetBytes((byte)OpAccess));

                RSAParameters rsa = KeyPair.ExportParameters(true);
                G2Frame key = protocol.WritePacket(settings, Packet_KeyPair, null);    
                    protocol.WritePacket(key, Key_D,        rsa.D);
                    protocol.WritePacket(key, Key_DP,       rsa.DP);
                    protocol.WritePacket(key, Key_DQ,       rsa.DQ);
                    protocol.WritePacket(key, Key_Exponent, rsa.Exponent);
                    protocol.WritePacket(key, Key_InverseQ, rsa.InverseQ);
                    protocol.WritePacket(key, Key_Modulus,  rsa.Modulus);
                    protocol.WritePacket(key, Key_P,        rsa.P);
                    protocol.WritePacket(key, Key_Q,        rsa.Q);

                FilePacket file = new FilePacket();
                file.Embedded = protocol.WriteFinish();

                return file.Encode(protocol);
            }
        }

        internal static SettingsPacket Decode(G2Header root)
        {
            SettingsPacket settings = new SettingsPacket();

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (child.Name == Packet_KeyPair)
                {
                    DecodeKey(child, settings);
                    continue;
                }

                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Operation:
                        settings.Operation = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_ScreenName:
                        settings.ScreenName = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_GlobalID:
                        settings.GlobalID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_GlobalPortTcp:
                        settings.GlobalPortTcp = BitConverter.ToUInt16(child.Data, child.PayloadPos);
                        break;

                    case Packet_GlobalPortUdp:
                        settings.GlobalPortUdp = BitConverter.ToUInt16(child.Data, child.PayloadPos);
                        break;

                    case Packet_OpPortTcp:
                        settings.OpPortTcp = BitConverter.ToUInt16(child.Data, child.PayloadPos);
                        break;

                    case Packet_OpPortUdp:
                        settings.OpPortUdp = BitConverter.ToUInt16(child.Data, child.PayloadPos);
                        break;

                    case Packet_OpKey:
                        settings.OpKey.Key = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_FileKey:
                        settings.FileKey.Key = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        settings.FileKey.IV  = new byte[settings.FileKey.IV.Length]; // set zeros
                        break;

                    case Packet_OpAccess:
                        settings.OpAccess =  (AccessType) child.Data[child.PayloadPos];
                        break;

                    case Packet_Location:
                        settings.Location = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_AwayMsg:
                        settings.AwayMessage = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return settings;
        }

        private static void DecodeKey(G2Header child, SettingsPacket settings)
        {
            G2Header key = new G2Header(child.Data);

            RSAParameters rsa = new RSAParameters();

            while (G2Protocol.ReadNextChild(child, key) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(key))
                    continue;

                switch (key.Name)
                {
                    case Key_D:
                        rsa.D = Utilities.ExtractBytes(key.Data, key.PayloadPos, key.PayloadSize);
                        break;

                    case Key_DP:
                        rsa.DP = Utilities.ExtractBytes(key.Data, key.PayloadPos, key.PayloadSize);
                        break;

                    case Key_DQ:
                        rsa.DQ = Utilities.ExtractBytes(key.Data, key.PayloadPos, key.PayloadSize);
                        break;

                    case Key_Exponent:
                        rsa.Exponent = Utilities.ExtractBytes(key.Data, key.PayloadPos, key.PayloadSize);
                        break;

                    case Key_InverseQ:
                        rsa.InverseQ = Utilities.ExtractBytes(key.Data, key.PayloadPos, key.PayloadSize);
                        break;

                    case Key_Modulus:
                        rsa.Modulus = Utilities.ExtractBytes(key.Data, key.PayloadPos, key.PayloadSize);
                        break;

                    case Key_P:
                        rsa.P = Utilities.ExtractBytes(key.Data, key.PayloadPos, key.PayloadSize);
                        break;

                    case Key_Q:
                        rsa.Q = Utilities.ExtractBytes(key.Data, key.PayloadPos, key.PayloadSize);
                        break;
                }       
            }

            settings.KeyPair.ImportParameters(rsa);
            settings.KeyPublic = rsa.Modulus;
        }
    }

    internal class CachePacket : G2Packet
    {
        internal byte[] IPs;
        byte PacketType;
      
        internal CachePacket(byte type)
        {
            PacketType = type;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                protocol.WritePacket(null, PacketType, IPs);

                FilePacket file = new FilePacket();
                file.Embedded = protocol.WriteFinish();

                return file.Encode(protocol);
            }
        }

        internal static CachePacket Decode(G2Header root)
        {
            CachePacket cache = new CachePacket(FilePacket.GlobalCache); // type doesnt matter here

            if (G2Protocol.ReadPayload(root))
                cache.IPs = Utilities.ExtractBytes(root.Data, root.PayloadPos, root.PayloadSize);

            G2Protocol.ResetPacket(root);

            return cache;
        }
    }

}
