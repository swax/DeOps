using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

using DeOps.Services;
using DeOps.Services.Transfer;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;




namespace DeOps.Services.Update
{
    internal class UpdateService : OpService
    {
        public string Name { get { return "Update"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.Update; } }

        OpCore Core;

        LookupSettings LookupConfig;

        internal UpdateService(OpCore core)
        {
            Core = core;

            Core.Update = this;

            LookupConfig = core.Context.LookupConfig;

            // gen key pair
            /*RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();

            string pub = rsa.ToXmlString(false);
            
            byte[] priv = UTF8Encoding.UTF8.GetBytes(rsa.ToXmlString(true));
            byte[] pass = Utilities.GetPasswordKey("password", new byte[] { 0x7A, 0x0D });

            string safe = Convert.ToBase64String(Utilities.EncryptBytes(priv, pass));


            // check
            byte[] check = Convert.FromBase64String(safe);
            byte[] test = Utilities.DecryptBytes(check, check.Length, pass);
            string priv2 = UTF8Encoding.UTF8.GetString(test);

            bool good = Utilities.MemCompare(priv, test);*/

#if DEBUG
           //SignNewUpdate();
#endif

            Core.Network.Searches.SearchEvent[ServiceID, 0] += new SearchRequestHandler(Search_Local);

            Core.Transfers.FileSearch[ServiceID, 0] += new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[ServiceID, 0] += new FileRequestHandler(Transfers_FileRequest);
        }

        public void SimTest()
        {
            return;
        }

        public void SimCleanup()
        {
            return;
        }

        public void Dispose()
        {
            Core.Network.Searches.SearchEvent[ServiceID, 0] -= new SearchRequestHandler(Search_Local);

            Core.Transfers.FileSearch[ServiceID, 0] -= new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[ServiceID, 0] -= new FileRequestHandler(Transfers_FileRequest);
        }

        /*
#if DEBUG
        void SignNewUpdate()
        {
            // Network update only uses sequential version
            // release betas with higher sequential version
            // auto-update requires a signed sequential version, so betas can be safely relesaed

            Core.UserMessage("Signing Update");

            try
            {
                UpdateInfo info = new UpdateInfo();

                info.Name = "DeOps_1.1.3.exe";
                info.DottedVersion = "1.1.3";

                // want to prevent infinite update loop, ensure the seq verison in the intaller, and the
                // signed seq version in the update are equal
                info.SequentialVersion = Core.Context.LocalSeqVersion;

                info.Notes = "";
                info.Notes += "Fixed stupid bug\r\n";
    
                RijndaelManaged crypt = new RijndaelManaged();
                crypt.Key = Utilities.GenerateKey(Core.StrongRndGen, 256);
                info.Key = crypt.Key;

                string source = "..\\Protected\\DeOps.exe";
                string final = Path.Combine(Application.StartupPath, "update.dat");

                string tempPath = Core.GetTempPath();
                Utilities.EncryptTagFile(source, tempPath, crypt, Core.Network.Protocol, ref info.Hash, ref info.Size);

                // function to encrypt given file
                using (FileStream stream = File.OpenRead(tempPath))
                {
                    info.Beginning = new byte[64];
                    stream.Read(info.Beginning, 0, info.Beginning.Length);
                }

                // test
                //Utilities.DecryptTagFile(tempPath, "..\\Protected\\check.exe", crypt.Key, null);

                File.Copy(tempPath, final, true);
                File.Delete(tempPath);

                // sign
                info.SignUpdate(Core.Network.Protocol);

                // test
                byte[] test = info.Encode(Core.Network.Protocol);
                Debug.Assert(test.Length < 1024);

                G2Header root = new G2Header(test);
                Debug.Assert(G2Protocol.ReadPacket(root));
                Debug.Assert(UpdateInfo.Decode(root) != null);

                // set
                Core.Context.SignedUpdate = info;

                // write bootstrap
                LookupConfig.WriteUpdateInfo(Core);

                Core.UserMessage("Sign Update Success");

                Process.Start("explorer.exe", Application.StartupPath);
                Debug.Assert(false);
            }
            catch (Exception ex)
            {
                Core.UserMessage(ex.Message);
            }

            Application.Exit();
        }
#endif*/


        internal void NewVersion(uint version, ulong user)
        {
            // if need to get new signed info
            if (Core.Context.SignedUpdate == null || Core.Context.SignedUpdate.SequentialVersion < version)
            {
                byte[] parameters = CompactNum.GetBytes(version);
                
                Core.Network.Searches.Start(user, "Update Search", ServiceID, 0, parameters, Search_Found);
            }

            // else if just need file
            else if (!Core.Context.SignedUpdate.Loaded)
                StartDownload(user);
        }

        void Search_Local(ulong key, byte[] parameters, List<byte[]> results)
        {
            uint version = CompactNum.ToUInt32(parameters, 0, parameters.Length);

            // if local version equal to or greater, send back signed packet
            if (Core.Context.SignedUpdate == null ||
                !Core.Context.SignedUpdate.Loaded ||
                Core.Context.SignedUpdate.SequentialVersion < version)
                return;

            results.Add(Core.Context.SignedUpdate.Encode(Core.Network.Protocol));
        }

        void Search_Found(DhtSearch search, DhtAddress source, byte[] data)
        {
            G2Header root = new G2Header(data);
            if (!G2Protocol.ReadPacket(root))
                return;

            UpdateInfo info = UpdateInfo.Decode(root); // verifies signature
            if (info == null)
                return;

            if (Core.Context.SignedUpdate == null || Core.Context.SignedUpdate.SequentialVersion < info.SequentialVersion)
            {
                Core.Context.SignedUpdate = info;
                LookupConfig.WriteUpdateInfo(Core);
            }

            // version less than what we have
            else if (Core.Context.SignedUpdate.SequentialVersion > info.SequentialVersion)
                return;

            // version remote has already loaded
            if (Core.Context.SignedUpdate.Loaded)
                return;

            // same sources will be hit as file download search progresses
            StartDownload(search.TargetID);
        }

        private void StartDownload(ulong target)
        {
            FileDetails details = new FileDetails(ServiceID, 0, Core.Context.SignedUpdate.Hash, Core.Context.SignedUpdate.Size, null);

            Core.Transfers.StartDownload(target, details, LookupConfig.UpdatePath, new EndDownloadHandler(DownloadFinished), null);
        }

        bool Transfers_FileSearch(ulong key, FileDetails details)
        {
            UpdateInfo signed = Core.Context.SignedUpdate;

            return (signed != null && signed.Loaded &&
                    signed.Size == details.Size && Utilities.MemCompare(signed.Hash, details.Hash));
        }

        string Transfers_FileRequest(ulong key, FileDetails details)
        {
            UpdateInfo signed = Core.Context.SignedUpdate;

            if (signed != null && signed.Loaded &&
                signed.Size == details.Size && Utilities.MemCompare(signed.Hash, details.Hash))
            {
                string sharePath = Core.User.TempPath + Path.DirectorySeparatorChar + signed.TempName;

                // cannot share from deops root directory because when new update.dat is downloaded the old one
                // cant be replaced if it is locked up by the transfer service

                if (!File.Exists(sharePath))
                    File.Copy(LookupConfig.UpdatePath, sharePath, true);

                return sharePath;
            }

            return null;
        }

        void DownloadFinished(object[] args)
        {
            bool success = false;

            // check size and beginning - ensure new signed update wasnt received before transfer finished
            using (FileStream stream = File.OpenRead(LookupConfig.UpdatePath))
            {
                UpdateInfo signed = Core.Context.SignedUpdate;

                byte[] check = new byte[signed.Beginning.Length];
                stream.Read(check, 0, check.Length);

                if (stream.Length == signed.Size &&
                    Utilities.MemCompare(check, signed.Beginning))
                {
                    signed.Loaded = true;

                    // signed already written to bootstrap
                    success = true; 
                }
            }

            if (success && Core.Context.CanUpdate())
                Core.RunInGuiThread(Core.Context.NotifyUpdateReady);
        }

        internal static UpdateInfo LoadUpdate(LookupSettings config)
        {
            UpdateInfo info = config.ReadUpdateInfo();

            if (info == null)
                return null;

            if (!File.Exists(config.UpdatePath))
                return null;

            using (FileStream stream = new FileStream(config.UpdatePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] check = new byte[info.Beginning.Length];
                stream.Read(check, 0, check.Length);

                if (stream.Length == info.Size &&
                    Utilities.MemCompare(check, info.Beginning))
                {
                    info.Loaded = true;
                }
            }

            return info;
        }
    }

    internal class UpdateInfo : G2Packet
    {
        const byte Packet_Signature = 0x10;

        const byte Packet_Name = 0x20;
        const byte Packet_Key = 0x30;
        const byte Packet_Size = 0x40;
        const byte Packet_Hash = 0x50;
        const byte Packet_Notes = 0x60;
        const byte Packet_Beginning = 0x70;
        const byte Packet_DottedVersion = 0x80;
        const byte Packet_SequentialVersion = 0x90;

        
        internal string Name;
        internal byte[] Key;
        internal long   Size;
        internal byte[] Hash;
        internal string Notes;

        internal byte[] Beginning;

        internal string DottedVersion;
        internal uint SequentialVersion;


        internal byte[] Embedded;
        internal byte[] Signature;

        internal bool Loaded;
        internal string TempName;

/*#if DEBUG
        internal void SignUpdate(G2Protocol protocol)
        {
            // public/private key
            byte[] safeKey = Convert.FromBase64String("GEFrq6zCbCVN3APcabDmKFUt/VHswKV0RKc4uRgy5XrrdQPgOXNhhJCnO0bVFREeAuURFJKFsLcZbwSXTUL1xKZeukavEdCpGj1IjuUjkixDk/HEeNfwT5nZm8wUB/bWEEUC++WeziQ6KORbx0J5SzNG6um+jM39pll3TP1jwq0EHUKGnAVs1xEbglBphMvPBjRIWDzVK97xMJhVMc7JHRolR3juFNJgIu6G0qW2TVFWFGHP3k3W/E/G58RhATR2jpYBhlJP2+QUACyBMYMGde4hf14wxuGizYyrGFuhEqxMjGcJNZFAlwxS6ccjo/ShQEYLu04yxkaav2Q5PmRUrCg6n6LSVJFCTr0u6V/jEJ8da9GIUITDay9PoyCUS/979x0l5acfH+M8QU9q7532nlVA2IBzYTuvuvsq3oWQJ23Xb8LFKICIrwifQ9gDXC52rpZ+nCOI44b6ii9iJigL93/88V798xP/kG0qKp/Kns8bePuUGTg43yVfPdXB5SeW03xgzJ1IrQOqudaEVKdtR6UPHeGmYkE5XYp4jkoNqEbGg1QsMbaiS00iwTRsDuM5dJlpw/H8vg9LiPjOAGFkj83LJTgLOKabgoZKTHS07kY8Xiy3n/59M2Xfhvx+y1+YxYCwLg5LnsIodZcuH5N6m6obWZLMddXhvjE32xb+7JiSfngidxorVXVrKnYWiD/JMXP0HA7WsQYMOztsJGLooiJ0mxuoE71aetwyCKnSxFHPcGPTTQijdHMOo5Fv6mfpdfHr+EmhRZRDRKkaKc4OWqZCzwfyiZ1AuvV9tWRbEJLcdMnDPIW4haEMZTqmHFRWoEBUip1VS9BFQrYdMZtZ2XpDXqNviWYr688wH+0m//Zw64J+rAYc4TKM3c0HMhVuI6YTUaQsiiVDMlIHiljiAt++oi+pLFNQF4kplL8cW7WhXU0JevsQTdk2MFrAML/a0c+dwunnTN1lsGBR6dLMY8vDbVtrnKwTxoJPtqKDoVHUVTWjoqbRb+HaJic1tB3CrOyvORax7nj9zjVSpdkkdgZ8+90+5mEwg+itPnCzEKCQwmrUrLKnEV1Bij719gcqsQ8NVY2lAPR4v3RAQSHbcqlnlYBsJ2YDy93Ihugc+686AXQKjE4X2D8n5UrRLhuLk0ZtUUbP3HKMKcrTyxsSMo6i4Q5eydqGgZSSzyit6IViMHxg1FHjxeGoFMULqc7igyeu1J3PhG9pq2Q+JjwYUmArHuEBBscXgJsaDmmO+ks=");

            // password protected
            GetTextDialog getpass = new GetTextDialog("Sign Update", "Enter private key password", "");
            getpass.ResultBox.UseSystemPasswordChar = true;
            getpass.ShowDialog(); 
            byte[] password = Utilities.GetPasswordKey(getpass.ResultBox.Text, new byte[] { 0x32, 0x12 });

            // decrypt - turn into xml
            safeKey = Utilities.DecryptBytes(safeKey, safeKey.Length, password);

            RSACryptoServiceProvider JMG_KEY = new RSACryptoServiceProvider() ;
            JMG_KEY.FromXmlString(UTF8Encoding.UTF8.GetString(safeKey));

            lock (protocol.WriteSection)
            {
                G2Frame inside = protocol.WritePacket(null, 1, null);

                protocol.WritePacket(inside, Packet_Name, UTF8Encoding.UTF8.GetBytes(Name));
                protocol.WritePacket(inside, Packet_Key, Key);
                protocol.WritePacket(inside, Packet_Size, CompactNum.GetBytes(Size));
                protocol.WritePacket(inside, Packet_Hash, Hash);
                protocol.WritePacket(inside, Packet_Notes, UTF8Encoding.UTF8.GetBytes(Notes));
                protocol.WritePacket(inside, Packet_Beginning, Beginning);
                protocol.WritePacket(inside, Packet_DottedVersion, UTF8Encoding.UTF8.GetBytes(DottedVersion));
                protocol.WritePacket(inside, Packet_SequentialVersion, CompactNum.GetBytes(SequentialVersion));

                Embedded = protocol.WriteFinish();
            }

            Signature = JMG_KEY.SignData(Embedded, new SHA1CryptoServiceProvider());
        }
#endif*/

        internal override byte[] Encode(G2Protocol protocol)
        {
            Debug.Assert(Signature != null);
            Debug.Assert(Embedded != null);

            lock (protocol.WriteSection)
            {
                G2Frame ouside = protocol.WritePacket(null, IdentityPacket.Update, Embedded);

                protocol.WritePacket(ouside, Packet_Signature, Signature);

                return protocol.WriteFinish();
            }
        }

        internal static UpdateInfo Decode(G2Header root)
        {
             // public key
            RSACryptoServiceProvider JMG_KEY = new RSACryptoServiceProvider() ;
            JMG_KEY.FromXmlString("<RSAKeyValue><Modulus>pTmHLSxyM9TDOM4tZzI5dld9JvPsHlHC/M5i0+Qtjid1DiefGAVubPToEhK9Im4Ohy37h5Ax6J3vt2pxLG4rnIDuKBJt70YH6W6XrJewQ6tid5BvVnNEzPUOIJHGMpOnyi0VjPpzZzWgp4JK6Yuh6LtsYwCyqIIJIBt9iQ/9XN0=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>");


            UpdateInfo info = new UpdateInfo();

            if (G2Protocol.ReadPayload(root))
                info.Embedded = Utilities.ExtractBytes(root.Data, root.PayloadPos, root.PayloadSize);

            G2Protocol.ResetPacket(root);

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
                if (G2Protocol.ReadPayload(child))
                    if(child.Name == Packet_Signature)
                        info.Signature = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);


            // verify signature
            byte[] jmgPublic = JMG_KEY.ExportParameters(false).Modulus;
            if (!Utilities.CheckSignedData(jmgPublic, info.Embedded, info.Signature))
                return null;


            root = new G2Header(info.Embedded);
            if (!G2Protocol.ReadPacket(root))
                return null;

            child = new G2Header(info.Embedded);
            
            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Name:
                        info.Name = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Key:
                        info.Key = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Size:
                        info.Size = CompactNum.ToInt64(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Hash:
                        info.Hash = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Notes:
                        info.Notes = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Beginning:
                        info.Beginning = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_DottedVersion:
                        info.DottedVersion = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_SequentialVersion:
                        info.SequentialVersion = CompactNum.ToUInt32(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            info.TempName = Utilities.ToBase64String(info.Hash);

            return info;
        }
    }

}
