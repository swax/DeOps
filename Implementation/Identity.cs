/********************************************************************************

	De-Ops: Decentralized Operations
	Copyright (C) 2006 John Marshall Group, Inc.

	By contributing code you grant John Marshall Group an unlimited, non-exclusive
	license to your contribution.

	For support, questions, commercial use, etc...
	E-Mail: swabby@c0re.net

********************************************************************************/

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

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Protocol.File;


namespace DeOps
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

        internal List<IPCacheEntry> GlobalCache = new List<IPCacheEntry>();
        internal List<IPCacheEntry> OpCache = new List<IPCacheEntry>();


        internal Identity(string filepath, string password, OpCore core)
        {
            Core = core;
            Protocol = core.Protocol;

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
            TempPath = RootPath + "\\0";
            Directory.CreateDirectory(TempPath);

			// default settings
            Settings.GlobalPortTcp = NextRandPort();
            Settings.GlobalPortUdp = NextRandPort();
            Settings.OpPortTcp = NextRandPort();
            Settings.OpPortUdp = NextRandPort();

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
                PacketStream stream = new PacketStream(decStream, Core.Protocol, FileAccess.Read);

                G2Header root = null;

                while (stream.ReadPacket(ref root))
                {
                    // cached ips
                    if (root.Name == RootPacket.File)
                    {
                        FilePacket file = FilePacket.Decode(Protocol, root);
                        G2Header embedded = new G2Header(file.Embedded);

                        if (Protocol.ReadPacket(embedded))
                        {
                            if (loadMode == LoadModeType.Settings)
                                if (embedded.Name == FilePacket.Settings)
                                    Settings = SettingsPacket.Decode(Protocol, embedded);

                            if (loadMode == LoadModeType.Cache && Core != null)
                            {
                                if (embedded.Name == FilePacket.GlobalCache)
                                    LoadCache(embedded, Core.GlobalNet);
                                if (embedded.Name == FilePacket.OperationCache)
                                    LoadCache(embedded, Core.OperationNet);
                            }
                        }
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

        void LoadCache(G2Header root, DhtNetwork network)
        {
            CachePacket packet = CachePacket.Decode(Protocol, root);

            if (packet.IPs.Length == 0)
                return;

            if (packet.IPs.Length % IPCacheEntry.BYTE_SIZE != 0)
                return;

            int offset = 0;
            while(offset < packet.IPs.Length)
            {
                network.AddCacheEntry(IPCacheEntry.FromBytes(packet.IPs, offset));
                offset += IPCacheEntry.BYTE_SIZE;
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
                FileStream writeStream = new FileStream(ProfilePath, FileMode.Create);
                CryptoStream encStream = new CryptoStream(writeStream, Password.CreateEncryptor(), CryptoStreamMode.Write);

                byte[] packet = Settings.Encode(Protocol);

                encStream.Write(packet, 0, packet.Length);

                // cache
                if (Core != null)
                {
                    SaveCache(encStream, Core.GlobalNet.IPCache, FilePacket.GlobalCache);
                    SaveCache(encStream, Core.OperationNet.IPCache, FilePacket.OperationCache);
                }


                encStream.FlushFinalBlock();
                encStream.Close();
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

        void SaveCache(CryptoStream encStream, LinkedList<IPCacheEntry> source, byte type)
        {
            // convert source to bytes
            int offset = 0;
            byte[] IPs = new byte[IPCacheEntry.BYTE_SIZE * source.Count];

            
            lock (source)
                foreach (IPCacheEntry host in source)
                {
                    host.ToBytes().CopyTo(IPs, offset);
                    offset += IPCacheEntry.BYTE_SIZE;
                }

            // make packets
            CachePacket cache = new CachePacket(type);

            offset = 0;
            int chunkSize = IPCacheEntry.BYTE_SIZE * 64;

            while(offset < IPs.Length)
            {
                int copySize = (IPs.Length-offset) < chunkSize ? (IPs.Length-offset) : chunkSize;

                cache.IPs = Utilities.ExtractBytes(IPs, offset, copySize);
                offset += copySize;

                byte[] packet = cache.Encode(Protocol);
                encStream.Write(packet, 0, packet.Length);
            }

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
}
