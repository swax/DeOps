using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Diagnostics;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Protocol.File;


namespace RiseOp
{

    internal enum LoadModeType { Settings, Cache };

    internal enum AccessType { Public, Private, Secret };

	/// <summary>
	/// Summary description for KimProfile.
	/// </summary>
	internal class Identity
	{
        internal OpCore Core;
        internal G2Protocol Protocol;

		internal string ProfilePath;
        internal string RootPath;
        internal string TempPath;
		internal RijndaelManaged Password;

		Random RndGen = new Random(unchecked((int)DateTime.Now.Ticks));

        internal SettingsPacket Settings = new SettingsPacket();

        internal List<DhtContact> GlobalCache = new List<DhtContact>();
        internal List<DhtContact> OpCache = new List<DhtContact>();


        internal Identity(string filepath, string password, OpCore core)
        {
            Core = core;
            Protocol = Core.GuiProtocol;

            Init(filepath, password);
        }

		internal Identity(string filepath, string password, G2Protocol protocol)
		{
            Protocol    = protocol;

            Init(filepath, password);
        }

        private void Init(string filepath, string password)
        {
			ProfilePath = filepath;
			Password    = Utilities.PasswordtoRijndael(password);

            RootPath = Path.GetDirectoryName(filepath);
            TempPath = RootPath + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + "0";
            Directory.CreateDirectory(TempPath);

			// default settings
            Settings.GlobalPortTcp = NextRandPort();
            Settings.GlobalPortUdp = NextRandPort();
            Settings.OpPortTcp = NextRandPort();
            Settings.OpPortUdp = NextRandPort();

            byte[] id = new byte[8];
            RndGen.NextBytes(id);
            Settings.GlobalID = BitConverter.ToUInt64(id, 0);
		}

        List<ushort> UsedPorts = new List<ushort>();

        ushort NextRandPort()
        {
            while (true)
            {
                ushort num = (ushort)RndGen.Next(5000, 9000);

                if (UsedPorts.Contains(num))
                    continue;

                UsedPorts.Add(num);
                return num;
            }
        }

		internal void Load(LoadModeType loadMode)
		{
			FileStream readStream = null;

			try
			{
                readStream = new FileStream(ProfilePath, FileMode.Open);
                CryptoStream decStream = new CryptoStream(readStream, Password.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(decStream, Protocol, FileAccess.Read);

                G2Header root = null;

                while (stream.ReadPacket(ref root))
                {
                    if(loadMode == LoadModeType.Settings)
                        if(root.Name ==  IdentityPacket.Settings)
                        {
                            Settings = SettingsPacket.Decode(root);
                            break;
                        }

                    if (loadMode == LoadModeType.Cache)
                    {
                        if (root.Name == IdentityPacket.GlobalCache)
                            if (Core.GlobalNet != null)
                                Core.GlobalNet.AddCacheEntry(ContactPacket.Decode(root).Contact);

                        if (root.Name == IdentityPacket.OperationCache)
                            Core.OperationNet.AddCacheEntry(ContactPacket.Decode(root).Contact);
                    }
                }

                stream.Close();
            }
			catch(Exception ex)
			{
				if(readStream != null)
					readStream.Close();

				throw ex;
			}
		}

        internal void Save()
        {
            string backupPath = ProfilePath.Replace(".dop", ".bak");

			if( !File.Exists(backupPath) && File.Exists(ProfilePath))
				File.Copy(ProfilePath, backupPath, true);

            try
            {
                // Attach to crypto stream and write file
                FileStream file = new FileStream(ProfilePath, FileMode.Create);
                CryptoStream crypto = new CryptoStream(file, Password.CreateEncryptor(), CryptoStreamMode.Write);
                PacketStream stream = new PacketStream(crypto, Protocol, FileAccess.Write);

                stream.WritePacket(Settings);

                if (Core != null)
                {
                    if (Core.GlobalNet != null)
                        SaveCache(stream, Core.GlobalNet.IPCache, IdentityPacket.GlobalCache);

                    SaveCache(stream, Core.OperationNet.IPCache, IdentityPacket.OperationCache);
                }

                stream.Close();
            }

            catch (Exception ex)
            {
                if (Core != null)
                    Core.ConsoleLog("Exception KimProfile::Save() " + ex.Message);
                else
                    System.Windows.Forms.MessageBox.Show("Profile Save Error:\n" + ex.Message + "\nBackup Restored");

                // restore backup
                if (File.Exists(backupPath))
                    File.Copy(backupPath, ProfilePath, true);
            }

            File.Delete(backupPath);
        }

        private void SaveCache(PacketStream stream, LinkedList<DhtContact> cache, byte type)
        {
            lock (cache)
                foreach (DhtContact entry in cache)
                    if (entry.GlobalProxy == 0) 
                        stream.WritePacket(new ContactPacket(type, entry));
        }

        internal void SaveInvite(string path)
        {
            /*FileStream writeStream = new FileStream(path, FileMode.Create);
            XmlTextWriter xmlWriter = new XmlTextWriter(writeStream, Encoding.UTF8);

            xmlWriter.WriteStartElement("invite");

            xmlWriter.WriteElementString("Operation", Operation);
            xmlWriter.WriteElementString("OpKey", Utilities.CryptType(OpKey) + "/" + Utilities.BytestoHex(OpKey.Key));
            xmlWriter.WriteElementString("OpAccess", Enum.GetName(typeof(AccessType), OpAccess));

            xmlWriter.WriteEndElement();
            xmlWriter.Flush();

            xmlWriter.Close();
            writeStream.Close();*/
        }
    }

    internal class ContactPacket : G2Packet
    {
        internal byte Name;
        internal DhtContact Contact;


        internal ContactPacket() { }

        internal ContactPacket(byte name, DhtContact contact)
        {
            Name = name;
            Contact = contact;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                Contact.WritePacket(protocol, null, Name);
                return protocol.WriteFinish();
            }
        }

        internal static ContactPacket Decode(G2Header root)
        {
            ContactPacket wrap = new ContactPacket();

            wrap.Contact = DhtContact.ReadPacket(root);

            return wrap;
        }
    }

    internal class IdentityPacket
    {
        internal const byte Settings = 0x10;
        internal const byte GlobalCache = 0x20;
        internal const byte OperationCache = 0x30;
    }

    internal class SettingsPacket : G2Packet
    {
        const byte Packet_Operation = 0x10;
        const byte Packet_ScreenName = 0x20;
        const byte Packet_GlobalID = 0x30;
        const byte Packet_GlobalPortTcp = 0x40;
        const byte Packet_GlobalPortUdp = 0x50;
        const byte Packet_OpPortTcp = 0x60;
        const byte Packet_OpPortUdp = 0x70;
        const byte Packet_OpKey = 0x80;
        const byte Packet_OpAccess = 0x90;
        const byte Packet_KeyPair = 0xA0;
        const byte Packet_Location = 0xB0;
        const byte Packet_FileKey = 0xC0;
        const byte Packet_AwayMsg = 0xD0;

        const byte Key_D = 0x10;
        const byte Key_DP = 0x20;
        const byte Key_DQ = 0x30;
        const byte Key_Exponent = 0x40;
        const byte Key_InverseQ = 0x50;
        const byte Key_Modulus = 0x60;
        const byte Key_P = 0x70;
        const byte Key_Q = 0x80;


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
        internal AccessType OpAccess;

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
                G2Frame settings = protocol.WritePacket(null, IdentityPacket.Settings, null);

                protocol.WritePacket(settings, Packet_Operation, UTF8Encoding.UTF8.GetBytes(Operation));
                protocol.WritePacket(settings, Packet_ScreenName, UTF8Encoding.UTF8.GetBytes(ScreenName));
                protocol.WritePacket(settings, Packet_GlobalID, BitConverter.GetBytes(GlobalID));
                protocol.WritePacket(settings, Packet_GlobalPortTcp, BitConverter.GetBytes(GlobalPortTcp));
                protocol.WritePacket(settings, Packet_GlobalPortUdp, BitConverter.GetBytes(GlobalPortUdp));
                protocol.WritePacket(settings, Packet_OpPortTcp, BitConverter.GetBytes(OpPortTcp));
                protocol.WritePacket(settings, Packet_OpPortUdp, BitConverter.GetBytes(OpPortUdp));
                protocol.WritePacket(settings, Packet_Location, UTF8Encoding.UTF8.GetBytes(Location));
                protocol.WritePacket(settings, Packet_AwayMsg, UTF8Encoding.UTF8.GetBytes(AwayMessage));

                protocol.WritePacket(settings, Packet_FileKey, FileKey.Key);
                protocol.WritePacket(settings, Packet_OpKey, OpKey.Key);
                protocol.WritePacket(settings, Packet_OpAccess, BitConverter.GetBytes((byte)OpAccess));

                RSAParameters rsa = KeyPair.ExportParameters(true);
                G2Frame key = protocol.WritePacket(settings, Packet_KeyPair, null);
                protocol.WritePacket(key, Key_D, rsa.D);
                protocol.WritePacket(key, Key_DP, rsa.DP);
                protocol.WritePacket(key, Key_DQ, rsa.DQ);
                protocol.WritePacket(key, Key_Exponent, rsa.Exponent);
                protocol.WritePacket(key, Key_InverseQ, rsa.InverseQ);
                protocol.WritePacket(key, Key_Modulus, rsa.Modulus);
                protocol.WritePacket(key, Key_P, rsa.P);
                protocol.WritePacket(key, Key_Q, rsa.Q);

                return protocol.WriteFinish();
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
                        settings.FileKey.IV = new byte[settings.FileKey.IV.Length]; // set zeros
                        break;

                    case Packet_OpAccess:
                        settings.OpAccess = (AccessType)child.Data[child.PayloadPos];
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
}
