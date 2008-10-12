using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Protocol.Special;


namespace RiseOp
{

    internal enum LoadModeType { Settings, AllCaches, LookupCache };

    internal enum AccessType { Public, Private, Secret };

    internal delegate void IconUpdateHandler();


	/// <summary>
	/// Summary description for KimProfile.
	/// </summary>
	internal class OpUser
	{
        internal OpCore Core;
        internal G2Protocol Protocol = new G2Protocol();

		internal string ProfilePath;
        internal string RootPath;
        internal string TempPath;

		internal byte[] PasswordKey;  // 32 bytes (256bit)
        internal byte[] PasswordSalt; // 4 btyes

        internal SettingsPacket Settings = new SettingsPacket();

        // gui look
        internal Bitmap OpIcon;
        internal Bitmap OpSplash;

        internal IconUpdateHandler GuiIconUpdate;
   

        // loading identity, or temp load for processing invite
        internal OpUser(string filepath, string password, OpCore core)
        {
            Core = core;

            // get password salt, first 16 bytes IV, next 4 is salt
            using (FileStream stream = File.OpenRead(filepath))
            {
                stream.Seek(16, SeekOrigin.Begin);

                PasswordSalt = new byte[4];
                stream.Read(PasswordSalt, 0, 4);
            }

            Init(filepath, password);
        }

        // used when creating new identity
		internal OpUser(string filepath, string password)
		{
            SetNewPassword(password);
        
            Init(filepath, password);
        }

        private void Init(string filepath, string password)
        {
			ProfilePath = filepath;


            if(PasswordKey == null)
                PasswordKey = Utilities.GetPasswordKey(password, PasswordSalt);
            
            RootPath = Path.GetDirectoryName(filepath);
            TempPath = RootPath + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + "0";

            // clear temp directory
            try
            {
                if (Directory.Exists(TempPath))
                    Directory.Delete(TempPath, true);
            }
            catch { }

            Directory.CreateDirectory(TempPath);

            Random rndGen = new Random(unchecked((int)DateTime.Now.Ticks));

			// default settings, set tcp/udp the same so forwarding is easier
            Settings.TcpPort = (ushort)rndGen.Next(5000, 9000);
            Settings.UdpPort = Settings.TcpPort;
		}

		internal void Load(LoadModeType loadMode)
		{
            RijndaelManaged Password = new RijndaelManaged();
            Password.Key = PasswordKey;

            byte[] iv = new byte[16];
            byte[] salt = new byte[4];

            OpCore lookup = null;
            if (Core != null)
                lookup = Core.Context.Lookup;

			try
			{
                using (TaggedStream file = new TaggedStream(ProfilePath, Protocol, ProcessSplash)) // tagged with splash
                {
                    // first 16 bytes IV, next 4 bytes is salt
                    file.Read(iv, 0, 16);
                    file.Read(salt, 0, 4);
                    Password.IV = iv;

                    using (CryptoStream crypto = new CryptoStream(file, Password.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        PacketStream stream = new PacketStream(crypto, Protocol, FileAccess.Read);

                        G2Header root = null;
                        while (stream.ReadPacket(ref root))
                        {
                            if (loadMode == LoadModeType.Settings)
                            {
                                if (root.Name == IdentityPacket.OperationSettings)
                                    Settings = SettingsPacket.Decode(root);

                                if (root.Name == IdentityPacket.UserInfo && Core != null)
                                    Core.IndexInfo(UserInfo.Decode(root));

                                // save icon to identity file because only root node saves icon/splash to link file
                                // to minimize link file size, but allow user to set custom icon/splash if there are not overrides
                                if (root.Name == IdentityPacket.Icon)
                                    OpIcon = IconPacket.Decode(root).OpIcon;
                            }

                            if (lookup != null && (loadMode == LoadModeType.AllCaches || loadMode == LoadModeType.LookupCache))
                            {
                                if (root.Name == IdentityPacket.LookupCachedIP)
                                    lookup.Network.Cache.AddContact(CachedIP.Decode(root).Contact);

                                if (root.Name == IdentityPacket.LookupCachedWeb)
                                    lookup.Network.Cache.AddCache(WebCache.Decode(root));
                            }

                            if (loadMode == LoadModeType.AllCaches)
                            {
                                if (root.Name == IdentityPacket.OpCachedIP)
                                    Core.Network.Cache.AddContact(CachedIP.Decode(root).Contact);

                                if (root.Name == IdentityPacket.OpCachedWeb)
                                    Core.Network.Cache.AddCache(WebCache.Decode(root));
                            }
                        }

                        Utilities.ReadtoEnd(crypto);
                    }
                }
            }
			catch(Exception ex)
			{
				throw ex;
			}
		}

        void ProcessSplash(PacketStream stream)
        {
            G2Header root = null;
            if (stream.ReadPacket(ref root))
                if (root.Name == IdentityPacket.Splash)
                {
                    LargeDataPacket start = LargeDataPacket.Decode(root);
                    if (start.Size > 0)
                    {
                        byte[] data = LargeDataPacket.Read(start, stream, IdentityPacket.Splash);
                        OpSplash = (Bitmap)Bitmap.FromStream(new MemoryStream(data));
                    }
                }
        }

        internal void Save()
        {
            if (Core != null && Core.InvokeRequired)
            {
                Debug.Assert(false);
                Core.RunInCoreAsync(() => Save());
                return;
            }

            string backupPath = ProfilePath.Replace(".rop", ".bak");

			if( !File.Exists(backupPath) && File.Exists(ProfilePath))
				File.Copy(ProfilePath, backupPath, true);

            RijndaelManaged Password = new RijndaelManaged();
            Password.Key = PasswordKey;

            try
            {
                // Attach to crypto stream and write file
                string tempPath = TempPath + Path.DirectorySeparatorChar + "firstsave";
                if (Core != null)
                    tempPath = Core.GetTempPath();

                using (FileStream file = new FileStream(tempPath, FileMode.Create))
                {
                    // write encrypted part of file
                    Password.GenerateIV();
                    file.Write(Password.IV, 0, Password.IV.Length);
                    file.Write(PasswordSalt, 0, PasswordSalt.Length);

                    using (CryptoStream crypto = new CryptoStream(file, Password.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        PacketStream stream = new PacketStream(crypto, Protocol, FileAccess.Write);

                        stream.WritePacket(Settings);

                        if (Core != null)
                        {
                            if (Core.Context.Lookup != null)
                            {
                                Core.Context.Lookup.Network.Cache.SaveIPs(stream);
                                Core.Context.Lookup.Network.Cache.SaveWeb(stream);
                            }

                            Core.Network.Cache.SaveIPs(stream);
                            Core.Network.Cache.SaveWeb(stream);

                            Core.SaveKeyIndex(stream);
                        }

                        if (OpIcon != null)
                            stream.WritePacket(new IconPacket(IdentityPacket.Icon, OpIcon));
                    }
                }

                // write unencrypted splash
                using (FileStream file = new FileStream(tempPath, FileMode.Open))
                {
                    file.Seek(0, SeekOrigin.End);

                    long startpos = file.Position;

                    PacketStream stream = new PacketStream(file, Protocol, FileAccess.Write);

                    // get right splash image (only used for startup logo, main setting is in link file)
                    if (OpSplash != null)
                    {
                        MemoryStream mem = new MemoryStream();
                        OpSplash.Save(mem, ImageFormat.Jpeg);
                        LargeDataPacket.Write(stream, IdentityPacket.Splash, mem.ToArray());
                    }
                    else
                        LargeDataPacket.Write(stream, IdentityPacket.Splash, null);

                    file.WriteByte(0); // end packet stream

                    byte[] last = BitConverter.GetBytes(startpos);
                    file.Write(last, 0, last.Length);
                }


                File.Copy(tempPath, ProfilePath, true);
                File.Delete(tempPath);
            }

            catch (Exception ex)
            {
                if (Core != null)
                    Core.ConsoleLog("Exception Identity::Save() " + ex.Message);
                else
                    System.Windows.Forms.MessageBox.Show("Profile Save Error:\n" + ex.Message + "\nBackup Restored");

                // restore backup
                if (File.Exists(backupPath))
                    File.Copy(backupPath, ProfilePath, true);
            }

            File.Delete(backupPath);
        }

        internal static void CreateNew(string path, string opName, string userName, string password, AccessType access, byte[] opKey, bool globalIM)
        {
            OpUser user = new OpUser(path, password);
            user.Settings.Operation = opName;
            user.Settings.UserName = userName;
            user.Settings.KeyPair = new RSACryptoServiceProvider(1024);
            user.Settings.FileKey = Utilities.GenerateKey(new RNGCryptoServiceProvider(), 256);
            user.Settings.OpAccess = access;
            user.Settings.Security = SecurityLevel.Medium;
            user.Settings.GlobalIM = globalIM;

            // joining/creating public
            if (access == AccessType.Public)
            {
                // 256 bit rijn

                SHA256Managed sha256 = new SHA256Managed();
                user.Settings.OpKey = sha256.ComputeHash(UTF8Encoding.UTF8.GetBytes(opName));
                user.Settings.Security = SecurityLevel.Low;
            }

            // invite to private/secret
            else if (opKey != null)
                user.Settings.OpKey = opKey;

            // creating private/secret
            else if (globalIM)
                user.Settings.OpKey = DhtNetwork.GlobalIMKey;

            else
                user.Settings.OpKey = Utilities.GenerateKey(new RNGCryptoServiceProvider(), 256);

            user.Save();

            // throws exception on failure
        }

        internal void SetNewPassword(string password)
        {
            PasswordSalt = new byte[4];
            RNGCryptoServiceProvider rnd = new RNGCryptoServiceProvider();
            rnd.GetBytes(PasswordSalt);

            PasswordKey = Utilities.GetPasswordKey(password, PasswordSalt);

            // ensure save called soon after this function
        }

        internal bool VerifyPassword(string password)
        {
            byte[] key = Utilities.GetPasswordKey(password, PasswordSalt);

            return Utilities.MemCompare(PasswordKey, key);
        }

        internal string GetTitle()
        {
            return Settings.Operation + " - " + Settings.UserName;
        }

        internal void IconUpdate()
        {
            Core.RunInGuiThread(GuiIconUpdate);
        }

        internal Icon GetOpIcon()
        {
            if (OpIcon != null)
                return Icon.FromHandle(OpIcon.GetHicon());

            else
                return Interface.InterfaceRes.riseop;
        }
    }

    internal class IconPacket : G2Packet
    {
        byte Name;
        internal Bitmap OpIcon;

        internal IconPacket(byte name, Bitmap icon)
        {
            Name = name;
            OpIcon = icon;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                MemoryStream stream = new MemoryStream();
                OpIcon.Save(stream, ImageFormat.Png);

                protocol.WritePacket(null, Name, stream.ToArray());

                return protocol.WriteFinish();
            }
        }

        internal static IconPacket Decode(G2Header root)
        {
            if (G2Protocol.ReadPayload(root))
            {
                byte[] array = Utilities.ExtractBytes(root.Data, root.PayloadPos, root.PayloadSize);

                return new IconPacket(root.Name, (Bitmap) Bitmap.FromStream(new MemoryStream(array)));
            }

            return new IconPacket(root.Name, null);
        }
    }

    internal class CachedIP : G2Packet
    {
        internal const byte Packet_Contact = 0x10;
        internal const byte Packet_LastSeen = 0x20;


        internal byte Name;
        internal DateTime LastSeen;
        internal DhtContact Contact;


        internal CachedIP() { }

        internal CachedIP(byte name, DhtContact contact)
        {
            Name = name;
            LastSeen = contact.LastSeen;
            Contact = contact;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame saved = protocol.WritePacket(null, Name, null);

                Contact.WritePacket(protocol, saved, Packet_Contact);

                protocol.WritePacket(saved, Packet_LastSeen, BitConverter.GetBytes(LastSeen.ToBinary()));

                return protocol.WriteFinish();
            }
        }

        internal static CachedIP Decode(G2Header root)
        {
            CachedIP saved = new CachedIP();

			G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Contact:
                        saved.Contact = DhtContact.ReadPacket(child);
                        break;

                    case Packet_LastSeen:
                        saved.LastSeen = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;
                }
            }

            saved.Contact.LastSeen = saved.LastSeen;

            return saved;
        }
    }

    internal class IdentityPacket
    {
        internal const byte OperationSettings  = 0x10;
        internal const byte LookupSettings     = 0x20;

        internal const byte LookupCachedIP   = 0x30;
        internal const byte OpCachedIP = 0x40;

        internal const byte LookupCachedWeb = 0x50;
        internal const byte OpCachedWeb = 0x60;

        internal const byte Icon    = 0x70;
        internal const byte Splash  = 0x80;

        internal const byte UserInfo = 0x90;
    }

    internal class UserInfo : G2Packet
    {
        const byte Packet_Name = 0x10;

        internal string Name;

        internal byte[] Key;
        internal ulong ID;


        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame user = protocol.WritePacket(null, IdentityPacket.UserInfo, Key);

                protocol.WritePacket(user, Packet_Name, UTF8Encoding.UTF8.GetBytes(Name));

                return protocol.WriteFinish();
            }
        }

        internal static UserInfo Decode(G2Header root)
        {
            UserInfo user = new UserInfo();

            if (G2Protocol.ReadPayload(root))
            {
                user.Key = Utilities.ExtractBytes(root.Data, root.PayloadPos, root.PayloadSize);
                user.ID = Utilities.KeytoID(user.Key);    
            }

            G2Protocol.ResetPacket(root);

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Name:
                        user.Name = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return user;
        }
    }

    internal class SettingsPacket : G2Packet
    {
        const byte Packet_Operation     = 0x10;
        const byte Packet_UserName      = 0x20;
        const byte Packet_TcpPort       = 0x30;
        const byte Packet_UdpPort       = 0x40;
        const byte Packet_OpKey         = 0x50;
        const byte Packet_OpAccess      = 0x60;
        const byte Packet_SecurityLevel = 0x70;
        const byte Packet_KeyPair       = 0x80;
        const byte Packet_Location      = 0x90;
        const byte Packet_FileKey       = 0xA0;
        const byte Packet_AwayMsg       = 0xB0;
        const byte Packet_GlobalIM      = 0xC0;

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
        internal string UserName;
        internal string Location = "";
        internal string AwayMessage = "";

        // network
        internal ushort TcpPort;
        internal ushort UdpPort;

        // private
        internal byte[] OpKey;
        internal AccessType OpAccess;
        internal SecurityLevel Security;
        internal bool GlobalIM;

        internal RSACryptoServiceProvider KeyPair = new RSACryptoServiceProvider();
        internal byte[] KeyPublic;

        internal byte[] FileKey;

        // derived from OpKey
        // the invite key is how we match the invite to the op of the invitee's public key
        // different than regular opID so that invite link / public link does not compramise
        // dht position of op on lookup network
        internal byte[] InviteKey;
 
         

        internal SettingsPacket()
        {
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame settings = protocol.WritePacket(null, IdentityPacket.OperationSettings, null);

                protocol.WritePacket(settings, Packet_Operation, UTF8Encoding.UTF8.GetBytes(Operation));
                protocol.WritePacket(settings, Packet_UserName, UTF8Encoding.UTF8.GetBytes(UserName));              
                protocol.WritePacket(settings, Packet_TcpPort, BitConverter.GetBytes(TcpPort));
                protocol.WritePacket(settings, Packet_UdpPort, BitConverter.GetBytes(UdpPort));
                protocol.WritePacket(settings, Packet_Location, UTF8Encoding.UTF8.GetBytes(Location));
                protocol.WritePacket(settings, Packet_AwayMsg, UTF8Encoding.UTF8.GetBytes(AwayMessage));

                protocol.WritePacket(settings, Packet_FileKey, FileKey);
                protocol.WritePacket(settings, Packet_OpKey, OpKey);
                protocol.WritePacket(settings, Packet_OpAccess, BitConverter.GetBytes((byte)OpAccess));
                protocol.WritePacket(settings, Packet_SecurityLevel, BitConverter.GetBytes((int)Security));

                if(GlobalIM)
                    protocol.WritePacket(settings, Packet_GlobalIM, null);
                    

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

                if (child.Name == Packet_GlobalIM)
                {
                    settings.GlobalIM = true;
                    continue;
                }

                if (!G2Protocol.ReadPayload(child))
                    continue;


                switch (child.Name)
                {
                    case Packet_Operation:
                        settings.Operation = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_UserName:
                        settings.UserName = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_TcpPort:
                        settings.TcpPort = BitConverter.ToUInt16(child.Data, child.PayloadPos);
                        break;

                    case Packet_UdpPort:
                        settings.UdpPort = BitConverter.ToUInt16(child.Data, child.PayloadPos);
                        break;

                    case Packet_OpKey:
                        settings.OpKey = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);

                        byte[] invite = new MD5CryptoServiceProvider().ComputeHash(settings.OpKey);
                        settings.InviteKey = Utilities.ExtractBytes(invite, 0, 8);                        
                        break;

                    case Packet_FileKey:
                        settings.FileKey = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_OpAccess:
                        settings.OpAccess = (AccessType)child.Data[child.PayloadPos];
                        break;

                    case Packet_SecurityLevel:
                        settings.Security = (SecurityLevel) BitConverter.ToInt32(child.Data, child.PayloadPos);
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

    // save independently so all operations use same lookup settings for quick startup and lookup network stability
    internal class LookupSettings : G2Packet
    {
        const byte Packet_UserID = 0x10;
        const byte Packet_TcpPort = 0x20;
        const byte Packet_UdpPort = 0x30;

        internal ulong UserID;
        internal ushort TcpPort;
        internal ushort UdpPort;

        internal LookupSettings()
        {
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame settings = protocol.WritePacket(null, IdentityPacket.LookupSettings, null);

                protocol.WritePacket(settings, Packet_UserID, BitConverter.GetBytes(UserID));
                protocol.WritePacket(settings, Packet_TcpPort, BitConverter.GetBytes(TcpPort));
                protocol.WritePacket(settings, Packet_UdpPort, BitConverter.GetBytes(UdpPort));

                return protocol.WriteFinish();
            }
        }

        internal static LookupSettings Decode(G2Header root)
        {
            LookupSettings settings = new LookupSettings();

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_UserID:
                        settings.UserID = BitConverter.ToUInt64(child.Data, child.PayloadPos);
                        break;

                    case Packet_TcpPort:
                        settings.TcpPort = BitConverter.ToUInt16(child.Data, child.PayloadPos);
                        break;

                    case Packet_UdpPort:
                        settings.UdpPort = BitConverter.ToUInt16(child.Data, child.PayloadPos);
                        break;
                }
            }


            return settings;
        }

        internal static LookupSettings Load(DhtNetwork network)
        {
            // so that accross multiple ops, lookup access points are maintained more or less
            // also bootstrap file can be sent to others to help them out
            LookupSettings settings = null;

            string path = Application.StartupPath + Path.DirectorySeparatorChar + "bootstrap";

            // dont want instances saving and loading same lookup file
            if (network.Core.Sim == null && File.Exists(path))
            {
                
                byte[] key = new SHA256Managed().ComputeHash( UTF8Encoding.UTF8.GetBytes("bootstrap"));

                try
                {
                    using (IVCryptoStream crypto = IVCryptoStream.Load(path, key))
                    {
                        PacketStream stream = new PacketStream(crypto, network.Protocol, FileAccess.Read);

                        G2Header root = null;

                        while (stream.ReadPacket(ref root))
                        {
                            if (root.Name == IdentityPacket.LookupSettings)
                                settings = LookupSettings.Decode(root);

                            if (root.Name == IdentityPacket.LookupCachedIP)
                                network.Cache.AddContact(CachedIP.Decode(root).Contact);

                            if (root.Name == IdentityPacket.LookupCachedWeb)
                                network.Cache.AddCache(WebCache.Decode(root));

                        }
                    }
                }
                catch (Exception ex)
                {
                    network.UpdateLog("Exception", "LookupSettings::Load " + ex.Message);
                }
            }

            // file not found / loaded
            if (settings == null)
            {
                settings = new LookupSettings();

                settings.UserID = Utilities.StrongRandUInt64(network.Core.StrongRndGen);
                settings.TcpPort = (ushort)network.Core.RndGen.Next(5000, 9000);
                settings.UdpPort = settings.TcpPort;
            }

            return settings;
        }

        internal void Save(OpCore core)
        {
            Debug.Assert(core.Network.IsLookup);

            if (core.Sim != null)
                return;

            string path = Application.StartupPath + Path.DirectorySeparatorChar + "bootstrap";
            
            byte[] key = new SHA256Managed().ComputeHash(UTF8Encoding.UTF8.GetBytes("bootstrap"));

            try
            {
                // Attach to crypto stream and write file
                using (IVCryptoStream crypto = IVCryptoStream.Save(path, key))
                {
                    PacketStream stream = new PacketStream(crypto, core.Network.Protocol, FileAccess.Write);

                    stream.WritePacket(this);

                    core.Network.Cache.SaveIPs(stream);
                    core.Network.Cache.SaveWeb(stream);
                }
            }

            catch (Exception ex)
            {
                core.Network.UpdateLog("Exception", "LookupSettings::Save " + ex.Message);
            }

        }
    }

}
