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
using System.Xml;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Protocol.Special;
using DeOps.Services.Update;
using System.Xml.Serialization;


namespace DeOps
{

    public enum LoadModeType { Settings, AllCaches, LookupCache };

    public enum AccessType { Public, Private, Secret };


	/// <summary>
	/// Summary description for KimProfile.
	/// </summary>
	public class OpUser
	{
        public OpCore Core;
        public G2Protocol Protocol = new G2Protocol();

		public string ProfilePath;
        public string RootPath;
        public string TempPath;

		public byte[] PasswordKey;  // 32 bytes (256bit)
        public byte[] PasswordSalt; // 4 btyes

        public SettingsPacket Settings = new SettingsPacket();

        // gui look
        public Bitmap OpIcon;
        public Bitmap OpSplash;

        public Action GuiIconUpdate;
   

        // loading identity, or temp load for processing invite
        public OpUser(string filepath, string password, OpCore core)
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
		public OpUser(string filepath, string password)
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

		public void Load(LoadModeType loadMode)
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

                                if (root.Name == IdentityPacket.UserInfo && Core != null && 
                                    (Core.Sim == null || !Core.Sim.Internet.FreshStart))
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
                                    lookup.Network.Cache.AddWebCache(WebCache.Decode(root));
                            }

                            if (loadMode == LoadModeType.AllCaches)
                            {
                                if (root.Name == IdentityPacket.OpCachedIP)
                                    Core.Network.Cache.AddContact(CachedIP.Decode(root).Contact);

                                if (root.Name == IdentityPacket.OpCachedWeb)
                                    Core.Network.Cache.AddWebCache(WebCache.Decode(root));
                            }
                        }
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

        public void Save()
        {
            if (Core != null && Core.InvokeRequired)
            {
                Debug.Assert(false);
                Core.RunInCoreAsync(() => Save());
                return;
            }

            string backupPath = ProfilePath.Replace(".dop", ".bak");

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
                    Core.UserMessage("Profile Save Error:\n" + ex.Message + "\nBackup Restored");

                // restore backup
                if (File.Exists(backupPath))
                    File.Copy(backupPath, ProfilePath, true);
            }

            File.Delete(backupPath);
        }

        public static void CreateNew(string path, string opName, string userName, string password, AccessType access, byte[] opKey, bool globalIM)
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
                user.Settings.OpKey = sha256.ComputeHash(UTF8Encoding.UTF8.GetBytes(opName.ToLowerInvariant()));
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

        public void SetNewPassword(string password)
        {
            PasswordSalt = new byte[4];
            RNGCryptoServiceProvider rnd = new RNGCryptoServiceProvider();
            rnd.GetBytes(PasswordSalt);

            PasswordKey = Utilities.GetPasswordKey(password, PasswordSalt);

            // ensure save called soon after this function
        }

        public bool VerifyPassword(string password)
        {
            byte[] key = Utilities.GetPasswordKey(password, PasswordSalt);

            return Utilities.MemCompare(PasswordKey, key);
        }

        public string GetTitle()
        {
            return Settings.Operation + " - " + Settings.UserName;
        }

        public void IconUpdate()
        {
            Core.RunInGuiThread(GuiIconUpdate);
        }

        public Icon GetOpIcon()
        {
            if (OpIcon != null)
                return Icon.FromHandle(OpIcon.GetHicon());

            else
                return Core.Context.DefaultIcon;
        }
    }

    public class IconPacket : G2Packet
    {
        byte Name;
        public Bitmap OpIcon;

        public IconPacket(byte name, Bitmap icon)
        {
            Name = name;
            OpIcon = icon;
        }

        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                MemoryStream stream = new MemoryStream();
                OpIcon.Save(stream, ImageFormat.Png);

                protocol.WritePacket(null, Name, stream.ToArray());

                return protocol.WriteFinish();
            }
        }

        public static IconPacket Decode(G2Header root)
        {
            if (G2Protocol.ReadPayload(root))
            {
                byte[] array = Utilities.ExtractBytes(root.Data, root.PayloadPos, root.PayloadSize);

                return new IconPacket(root.Name, (Bitmap) Bitmap.FromStream(new MemoryStream(array)));
            }

            return new IconPacket(root.Name, null);
        }
    }

    public class CachedIP : G2Packet
    {
        public const byte Packet_Contact = 0x10;
        public const byte Packet_LastSeen = 0x20;


        public byte Name;
        public DateTime LastSeen;
        public DhtContact Contact;


        public CachedIP() { }

        public CachedIP(byte name, DhtContact contact)
        {
            Name = name;
            LastSeen = contact.LastSeen;
            Contact = contact;
        }

        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame saved = protocol.WritePacket(null, Name, null);

                Contact.WritePacket(protocol, saved, Packet_Contact);

                protocol.WritePacket(saved, Packet_LastSeen, BitConverter.GetBytes(LastSeen.ToBinary()));

                return protocol.WriteFinish();
            }
        }

        public static CachedIP Decode(G2Header root)
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

    public class IdentityPacket
    {
        public const byte OperationSettings  = 0x10;
        public const byte LookupSettings     = 0x20;

        public const byte LookupCachedIP   = 0x30;
        public const byte OpCachedIP = 0x40;

        public const byte LookupCachedWeb = 0x50;
        public const byte OpCachedWeb = 0x60;

        public const byte Icon    = 0x70;
        public const byte Splash  = 0x80;

        public const byte UserInfo = 0x90;

        public const byte Update = 0xA0;
    }

    public class UserInfo : G2Packet
    {
        const byte Packet_Name = 0x10;

        public string Name;

        public byte[] Key;
        public ulong ID;


        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame user = protocol.WritePacket(null, IdentityPacket.UserInfo, Key);

                protocol.WritePacket(user, Packet_Name, UTF8Encoding.UTF8.GetBytes(Name));

                return protocol.WriteFinish();
            }
        }

        public static UserInfo Decode(G2Header root)
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

    public class SettingsPacket : G2Packet
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
        const byte Packet_Invisible     = 0xD0;

        const byte Key_D        = 0x10;
        const byte Key_DP       = 0x20;
        const byte Key_DQ       = 0x30;
        const byte Key_Exponent = 0x40;
        const byte Key_InverseQ = 0x50;
        const byte Key_Modulus  = 0x60;
        const byte Key_P        = 0x70;
        const byte Key_Q        = 0x80;


        // general
        public string Operation;
        public string UserName;
        public string Location = "";
        public string AwayMessage = "";
        public bool Invisible;

        // network
        public ushort TcpPort;
        public ushort UdpPort;

        // private
        public byte[] OpKey;
        public AccessType OpAccess;
        public SecurityLevel Security;
        public bool GlobalIM;

        public RSACryptoServiceProvider KeyPair = new RSACryptoServiceProvider();
        public byte[] KeyPublic;

        public byte[] FileKey;

        // derived from OpKey
        // the invite key is how we match the invite to the op of the invitee's public key
        // different than regular opID so that invite link / public link does not compramise
        // dht position of op on lookup network
        public byte[] PublicOpID;
 
         

        public SettingsPacket()
        {
        }

        public override byte[] Encode(G2Protocol protocol)
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

                if (GlobalIM) protocol.WritePacket(settings, Packet_GlobalIM, null);
                if (Invisible) protocol.WritePacket(settings, Packet_Invisible, null);    

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

        public static SettingsPacket Decode(G2Header root)
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

                if (child.Name == Packet_Invisible)
                {
                    settings.Invisible = true;
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

                        byte[] pubID = new MD5CryptoServiceProvider().ComputeHash(settings.OpKey);
                        settings.PublicOpID = Utilities.ExtractBytes(pubID, 0, 8);                        
                        break;

                    case Packet_FileKey:
                        settings.FileKey = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_OpAccess:
                        settings.OpAccess = (AccessType)child.Data[child.PayloadPos];
                        break;

                    case Packet_SecurityLevel:
                        settings.Security = (SecurityLevel)BitConverter.ToInt32(child.Data, child.PayloadPos);
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
    public class LookupSettings
    {
        [Serializable]
        public class PortsConfig
        {
            public ulong UserID;
            public ushort Tcp;
            public ushort Udp;
        }

        public PortsConfig Ports;
        public string StartupPath;
        public byte[] BootstrapKey;
        public string BootstrapPath;
        public string UpdatePath;
        public string PortsConfigPath;


        public LookupSettings(string startupPath)
        {
            StartupPath = startupPath;
            BootstrapKey = new SHA256Managed().ComputeHash(UTF8Encoding.UTF8.GetBytes("bootstrap"));
            BootstrapPath = Path.Combine(startupPath, "bootstrap.dat");
            UpdatePath = Path.Combine(startupPath, "update.dat");
            PortsConfigPath = Path.Combine(startupPath, "lookup.xml");
        }

        public void Load(DhtNetwork network)
        {
            // if the user has multiple ops, the lookup network is setup with the same settings
            // so it is easy to find and predictable for other's bootstrapping
            // we put it in local settings so we safe-guard these settings from moving to other computers
            // and having dupe dht lookup ids on the network
            try
            {
                var serializer = new XmlSerializer(typeof(PortsConfig));

                using (var reader = new StreamReader(PortsConfigPath))
                    Ports = (PortsConfig)serializer.Deserialize(reader);
            }
            catch 
            {
                Ports = new PortsConfig();
            }

            if (Ports.UserID == 0 || network.Core.Sim != null)
                Ports.UserID = Utilities.StrongRandUInt64(network.Core.StrongRndGen);

            // keep tcp/udp the same by default
            if (Ports.Tcp == 0 || network.Core.Sim != null)
                Ports.Tcp = (ushort)network.Core.RndGen.Next(3000, 15000);

            if (Ports.Udp == 0 || network.Core.Sim != null)
                Ports.Udp = Ports.Tcp;
   

            // dont want instances saving and loading same lookup file
            if (network.Core.Sim == null && File.Exists(BootstrapPath))
            { 
                try
                {
                    using (IVCryptoStream crypto = IVCryptoStream.Load(BootstrapPath, BootstrapKey))
                    {
                        PacketStream stream = new PacketStream(crypto, network.Protocol, FileAccess.Read);

                        G2Header root = null;

                        while (stream.ReadPacket(ref root))
                        {
                            if (root.Name == IdentityPacket.LookupCachedIP)
                                network.Cache.AddContact(CachedIP.Decode(root).Contact);

                            if (root.Name == IdentityPacket.LookupCachedWeb)
                                network.Cache.AddWebCache(WebCache.Decode(root));
                        }
                    }
                }
                catch (Exception ex)
                {
                    network.UpdateLog("Exception", "LookupSettings::Load " + ex.Message);
                }
            }
        }

        public void Save(OpCore core)
        {
            Debug.Assert(core.Network.IsLookup);

            try
            {
                var serializer = new XmlSerializer(typeof(PortsConfig));

                using (var writer = new StreamWriter(PortsConfigPath))
                    serializer.Serialize(writer, Ports);
            }
            catch { }

            if (core.Sim != null)
                return;
      
            try
            {
                // Attach to crypto stream and write file
                using (IVCryptoStream crypto = IVCryptoStream.Save(BootstrapPath, BootstrapKey))
                {
                    PacketStream stream = new PacketStream(crypto, core.Network.Protocol, FileAccess.Write);

                    if(core.Context.SignedUpdate != null)
                        stream.WritePacket(core.Context.SignedUpdate);

                    core.Network.Cache.SaveIPs(stream);
                    core.Network.Cache.SaveWeb(stream);
                }
            }

            catch (Exception ex)
            {
                core.Network.UpdateLog("Exception", "LookupSettings::Save " + ex.Message);
            }
        }

        public void WriteUpdateInfo(OpCore core)
        {
            // non lookup core, embedding update packet
            Debug.Assert(!core.Network.IsLookup);

            string temp = Path.Combine(StartupPath, "temp.dat");

            try
            {
                using (IVCryptoStream inCrypto = IVCryptoStream.Load(BootstrapPath, BootstrapKey))
                using (IVCryptoStream outCrypto = IVCryptoStream.Save(temp, BootstrapKey))
                {
                    byte[] update = core.Context.SignedUpdate.Encode(core.Network.Protocol);
                    outCrypto.Write(update, 0, update.Length);

                    PacketStream inStream = new PacketStream(inCrypto, core.Network.Protocol, FileAccess.Read);

                    G2Header root = null;

                    while (inStream.ReadPacket(ref root))
                        if (root.Name != IdentityPacket.Update)
                            outCrypto.Write(root.Data, root.PacketPos, root.PacketSize);
                }

                File.Copy(temp, BootstrapPath, true);
                File.Delete(temp);
            }

            catch (Exception ex)
            {
                core.Network.UpdateLog("Exception", "WriteUpdateInfo::" + ex.Message);
            }
        }

        public UpdateInfo ReadUpdateInfo()
        {
            if (!File.Exists(BootstrapPath))
                return null;

            try
            {
                using (IVCryptoStream crypto = IVCryptoStream.Load(BootstrapPath, BootstrapKey))
                {
                    PacketStream stream = new PacketStream(crypto, new G2Protocol(), FileAccess.Read);

                    G2Header root = null;

                    while (stream.ReadPacket(ref root))
                        if (root.Name == IdentityPacket.Update)
                            return UpdateInfo.Decode(root);
                }
            }
            catch { }

            return null;
        }
    }
}
