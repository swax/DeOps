using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Transport;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Components.Transfer;

/* files
 *      mail folder
 *          outbound file (mail headers)
 *          inbound file (mail headers)
 *          inbound/outbound mail files
 *      cache folder
 *          cache file
 *              mail headers
 *              acks
 *              pending headers (including local pending header)
 *          mail/pending files (including local pending file)
 * */

/* entry removal
 *      mail map
 *          ID not in source's pending mail list
 *          ID in target's pending ack list
 *      ack map
 *          ID not in target's pending mail list
 *          ID not in source's pending ack list
 *      pending mail
 *          Ack received from target
 *          ID in targets pending ack list
 *      pending ack
 *          ID not in targets pending mail list
 * */

/* re-publish (make sure pending lists of targets updated first)
 *      mail
 *          search for ID returns nothing
 *      ack 
 *          search for ID returns nothing
 * */


namespace DeOps.Components.Mail
{
    internal delegate void MailUpdateHandler(bool inbox, LocalMail message);


    class MailControl : OpComponent
    {
        internal OpCore Core;
        G2Protocol Protocol;
        internal DhtNetwork Network;
        internal DhtStore Store;

        internal string MailPath;
        internal string CachePath;
        RijndaelManaged LocalFileKey;
        RijndaelManaged MailIDKey;

        internal Dictionary<ulong, List<CachedMail>> MailMap = new Dictionary<ulong, List<CachedMail>>();
        internal Dictionary<ulong, List<CachedAck>>  AckMap  = new Dictionary<ulong, List<CachedAck>>();
        internal Dictionary<ulong, CachedPending> PendingMap = new Dictionary<ulong, CachedPending>();

        internal List<LocalMail> Inbox;
        internal List<LocalMail> Outbox;

        CachedPending LocalPending;
        internal Dictionary<ulong, List<ulong>>  PendingMail = new Dictionary<ulong, List<ulong>>();
        internal Dictionary<ulong, List<byte[]>> PendingAcks = new Dictionary<ulong, List<byte[]>>();

        bool RunSaveHeaders;
        Dictionary<ulong, List<MailIdent>> DownloadLater = new Dictionary<ulong,List<MailIdent>>();

        internal MailUpdateHandler MailUpdate;

        int PruneSize = 100;


        internal MailControl(OpCore core)
        {
            Core = core;
            Protocol = Core.Protocol;
            Core.Mail = this;
            Network = core.OperationNet;
            Store = Network.Store;

            Core.LoadEvent += new LoadHandler(Core_Load);
            Core.TimerEvent += new TimerHandler(Core_Timer);

            Network.EstablishedEvent += new EstablishedHandler(Network_Established);

            Store.StoreEvent[ComponentID.Mail]     = new StoreHandler(Store_Local);
            Store.ReplicateEvent[ComponentID.Mail] = new ReplicateHandler(Store_Replicate);
            Store.PatchEvent[ComponentID.Mail]     = new PatchHandler(Store_Patch);
            
            Network.Searches.SearchEvent[ComponentID.Mail] = new SearchRequestHandler(Search_Local);

            if (Core.Sim != null)
                PruneSize = 25;
        }

        void Core_Load()
        {
            Core.Transfers.FileSearch[ComponentID.Mail] = new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[ComponentID.Mail] = new FileRequestHandler(Transfers_FileRequest);


            LocalFileKey = Core.User.Settings.FileKey;

            MailIDKey = new RijndaelManaged();
            MailIDKey.Key = LocalFileKey.Key;
            MailIDKey.IV = LocalFileKey.IV;
            MailIDKey.Padding = PaddingMode.None;

            MailPath = Core.User.RootPath + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + ComponentID.Mail.ToString();
            CachePath = MailPath + Path.DirectorySeparatorChar + "1";

            Directory.CreateDirectory(MailPath);
            Directory.CreateDirectory(CachePath);

            LoadHeaders();

            lock (PendingMap)
                if (!PendingMap.ContainsKey(Core.LocalDhtID))
                {
                    PendingMap[Core.LocalDhtID] = new CachedPending();
                    LocalPending = PendingMap[Core.LocalDhtID];
                    
                    SavePending();
                    SaveHeaders();
                }
        }

        void Core_Timer()
        {
            //crit periodically do search for unacked list so maybe mail/acks can be purged

            // unload outbound / inbound data if no interfaces connected


            if (RunSaveHeaders)
                SaveHeaders();


            // clean download later map
            if (!Network.Established)
                PruneMap(DownloadLater);


            // do below once per minute
            if (Core.TimeNow.Second != 0)
                return;


            // prune mail 
            if (MailMap.Count > PruneSize)
            {
                List<ulong> removeIds = new List<ulong>();

                foreach (List<CachedMail> list in MailMap.Values)
                {
                    if (list.Count > PruneSize) // no other way to ident, cant remove by age
                        list.RemoveRange(0, list.Count - PruneSize);

                    foreach (CachedMail mail in list)
                        if (!Utilities.InBounds(mail.Header.TargetID, mail.DhtBounds, Core.LocalDhtID))
                        {
                            removeIds.Add(mail.Header.TargetID);
                            break;
                        }
                }

                while (removeIds.Count > 0 && MailMap.Count > PruneSize / 2)
                {
                    ulong furthest = Core.LocalDhtID;
                    List<CachedMail> mails = MailMap[furthest];

                    foreach (ulong id in removeIds)
                        if ((id ^ Core.LocalDhtID) > (furthest ^ Core.LocalDhtID))
                            furthest = id;

                    foreach(CachedMail mail in mails)
                        if(mail.Header != null)
                            try { File.Delete(GetCachePath(mail.Header)); }
                            catch { }

                    MailMap.Remove(furthest);
                    removeIds.Remove(furthest);
                    RunSaveHeaders = true;
                }
            }

            // prune acks 
            if (AckMap.Count > PruneSize)
            {
                List<ulong> removeIds = new List<ulong>();

                foreach (List<CachedAck> list in AckMap.Values)
                {
                    if (list.Count > PruneSize) // no other way to ident, cant remove by age
                        list.RemoveRange(0, list.Count - PruneSize);

                    foreach (CachedAck cached in list)
                        if (!Utilities.InBounds(cached.Ack.TargetID, cached.DhtBounds, Core.LocalDhtID))
                        {
                            removeIds.Add(cached.Ack.TargetID);
                            break;
                        }
                }

                while (removeIds.Count > 0 && AckMap.Count > PruneSize / 2)
                {
                    ulong furthest = Core.LocalDhtID;

                    foreach (ulong id in removeIds)
                        if ((id ^ Core.LocalDhtID) > (furthest ^ Core.LocalDhtID))
                            furthest = id;

                    AckMap.Remove(furthest);
                    removeIds.Remove(furthest);
                    RunSaveHeaders = true;
                }
            }

            // prune pending
            if (PendingMap.Count > PruneSize)
            {
                List<ulong> removeIds = new List<ulong>();

                foreach (CachedPending pending in PendingMap.Values)
                    if (pending.Header.KeyID != Core.LocalDhtID && 
                        !Utilities.InBounds(pending.Header.KeyID, pending.DhtBounds, Core.LocalDhtID))
                        removeIds.Add(pending.Header.KeyID);

                while (removeIds.Count > 0 && PendingMap.Count > PruneSize / 2)
                {
                    ulong furthest = Core.LocalDhtID;
                    CachedPending pending = PendingMap[furthest];

                    foreach (ulong id in removeIds)
                        if ((id ^ Core.LocalDhtID) > (furthest ^ Core.LocalDhtID))
                            furthest = id;

                    if (pending.Header != null)
                        try { File.Delete(GetFilePath(pending.Header)); }
                        catch { }


                    PendingMap.Remove(furthest);
                    removeIds.Remove(furthest);
                    RunSaveHeaders = true;
                }
            }
        }

        private void PruneMap(Dictionary<ulong, List<MailIdent>> map)
        {
            if (map.Count < PruneSize)
                return;

            List<ulong> removeIDs = new List<ulong>();

            while (map.Count > 0 && map.Count > PruneSize)
            {
                ulong furthest = Core.LocalDhtID;

                // get furthest id
                foreach (ulong id in map.Keys)
                    if ((id ^ Core.LocalDhtID) > (furthest ^ Core.LocalDhtID))
                        furthest = id;

                // remove one 
                map.Remove(furthest);
            }  
        }

        void Network_Established()
        {
            ulong localBounds = Store.RecalcBounds(Core.LocalDhtID);

            // set bounds for pending
            foreach (ulong key in PendingMap.Keys)
            {
                CachedPending pending = PendingMap[key];
                pending.DhtBounds = Store.RecalcBounds(key);

                // republish objects that were not seen on the network during startup
                if (pending.Unique && Utilities.InBounds(Core.LocalDhtID, localBounds, key))
                    Store.PublishNetwork(key, ComponentID.Mail, pending.SignedHeader);
            }

            // set bounds for acks
            foreach (ulong key in AckMap.Keys)
            {
                ulong bounds = Store.RecalcBounds(key);

                foreach (CachedAck ack in AckMap[key])
                {
                    ack.DhtBounds = bounds;

                    if (ack.Unique && Utilities.InBounds(Core.LocalDhtID, localBounds, key))
                        Store.PublishNetwork(key, ComponentID.Mail, ack.SignedAck);
                }
            }

            // set bounds for mail
            foreach (ulong key in MailMap.Keys)
            {
                ulong bounds = Store.RecalcBounds(key);

                foreach (CachedMail mail in MailMap[key])
                {
                    mail.DhtBounds = bounds;

                    if (mail.Unique && Utilities.InBounds(Core.LocalDhtID, localBounds, key))
                        Store.PublishNetwork(key, ComponentID.Mail, mail.SignedHeader);
                }
            }

            // only download those objects in our local area
            foreach (ulong key in DownloadLater.Keys)
                if (Utilities.InBounds(Core.LocalDhtID, localBounds, key))
                    foreach (MailIdent ident in DownloadLater[key])
                        StartSearch(key, ident.Encode());

            DownloadLater.Clear();
        }

        void SaveHeaders()
        {
            RunSaveHeaders = false;

            try
            {
                string tempPath = Core.GetTempPath();
                FileStream file = new FileStream(tempPath, FileMode.Create);
                CryptoStream stream = new CryptoStream(file, LocalFileKey.CreateEncryptor(), CryptoStreamMode.Write);

                // mail headers
                foreach (List<CachedMail> list in MailMap.Values)
                    foreach (CachedMail mail in list)
                        stream.Write(mail.SignedHeader, 0, mail.SignedHeader.Length);

                // acks
                foreach (List<CachedAck> list in AckMap.Values)
                    foreach (CachedAck ack in list)
                        stream.Write(ack.SignedAck, 0, ack.SignedAck.Length);

                // unacked
                lock (PendingMap)
                    foreach (CachedPending pending in PendingMap.Values)
                        stream.Write(pending.SignedHeader, 0, pending.SignedHeader.Length);

                stream.FlushFinalBlock();
                stream.Close();


                string finalPath = CachePath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "headers");
                File.Delete(finalPath);
                File.Move(tempPath, finalPath);
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Mail", "Error saving headers " + ex.Message);
            }
        }

        void LoadHeaders()
        {
            try
            {
                string path = CachePath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "headers");

                if (!File.Exists(path))
                    return;

                FileStream file = new FileStream(path, FileMode.Open);
                CryptoStream crypto = new CryptoStream(file, LocalFileKey.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Core.Protocol, FileAccess.Read);

                G2Header root = null;

                while (stream.ReadPacket(ref root))
                    if (root.Name == DataPacket.SignedData)
                    {
                        SignedData signed = SignedData.Decode(Core.Protocol, root);
                        G2Header embedded = new G2Header(signed.Data);

                        // figure out data contained
                        if (Core.Protocol.ReadPacket(embedded))
                        {
                            if (embedded.Name == MailPacket.MailHeader)
                                Process_MailHeader(null, signed, MailHeader.Decode(Core.Protocol, embedded));

                            else if (embedded.Name == MailPacket.Ack)
                                Process_MailAck(null, signed, MailAck.Decode(Core.Protocol, embedded), true);

                            if (embedded.Name == MailPacket.Pending)
                                Process_PendingHeader(null, signed, PendingHeader.Decode(Core.Protocol, embedded));
                        }
                    }

                stream.Close();
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Mail", "Error loading headers " + ex.Message);
            }
        }

        internal void SaveLocalHeaders(List<LocalMail> mailList, string name)
        {
            try
            {
                string tempPath = Core.GetTempPath();
                FileStream file = new FileStream(tempPath, FileMode.Create);
                CryptoStream stream = new CryptoStream(file, LocalFileKey.CreateEncryptor(), CryptoStreamMode.Write);

                lock (mailList)
                    foreach (LocalMail local in mailList)
                    {
                        byte[] encoded = local.Header.Encode(Core.Protocol, true);
                        stream.Write(encoded, 0, encoded.Length);
                    }

                stream.FlushFinalBlock();
                stream.Close();


                string finalPath = MailPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, name);
                File.Delete(finalPath);
                File.Move(tempPath, finalPath);
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Mail", "Error saving " + name + " " + ex.Message);
            }
        }

        internal void LoadLocalHeaders(ref List<LocalMail> mailList, string name)
        {
            mailList = new List<LocalMail>();
            List<MailHeader> headers = new List<MailHeader>();
            
            try
            {
                string path = MailPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, name);

                if (!File.Exists(path))
                    return;

                FileStream file = new FileStream(path, FileMode.Open);
                CryptoStream crypto = new CryptoStream(file, LocalFileKey.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Core.Protocol, FileAccess.Read);

                G2Header root = null;

                while (stream.ReadPacket(ref root))
                    if (root.Name == MailPacket.MailHeader)
                    {
                        MailHeader header = MailHeader.Decode(Core.Protocol, root);

                        if (header == null)
                            continue;

                        Core.IndexKey(header.SourceID, ref header.Source);

                        if(header.Target != null)
                            Core.IndexKey(header.TargetID, ref header.Target);

                        headers.Add(header);
                    }

                stream.Close();
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Mail", "Error loading " + name + " " + ex.Message);
            }

            // load mail files that headers point to
            foreach (MailHeader header in headers)
            {
                LocalMail local = LoadLocalMail(header);

                if (local != null)
                    mailList.Add(local);
            }
        }

        private LocalMail LoadLocalMail(MailHeader header)
        {
            LocalMail local = new LocalMail();
            local.Header = header;
            local.From = header.SourceID;

            try
            {
                string path = GetLocalPath(header);

                if (!File.Exists(path))
                    return null;

                FileStream   file   = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                CryptoStream crypto = new CryptoStream(file, header.LocalKey.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Core.Protocol, FileAccess.Read);

                G2Header root = null;

                while (stream.ReadPacket(ref root))
                {
                    if (root.Name == MailPacket.MailInfo)
                        local.Info = MailInfo.Decode(Core.Protocol, root);

                    else if (root.Name == MailPacket.MailDest)
                    {
                        MailDestination dest = MailDestination.Decode(Core.Protocol, root);
                        Core.IndexKey(dest.KeyID, ref dest.Key);

                        if (dest.CC)
                            local.CC.Add(dest.KeyID);
                        else
                            local.To.Add(dest.KeyID);
                    }

                    else if (root.Name == MailPacket.MailFile)
                        local.Attached.Add( MailFile.Decode(Core.Protocol, root));
                }

                stream.Close();
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Mail", "Error loading local mail " + ex.Message);
            }

            if (local.Info != null)
                return local;

            return null;
        }

        internal override List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong key, uint proj)
        {
            List<MenuItemInfo> menus = new List<MenuItemInfo>();

            if (menuType == InterfaceMenuType.Quick)
            {
                if (key == Core.LocalDhtID)
                    return null;

                menus.Add(new MenuItemInfo("Send Mail", MailRes.SendMail, new EventHandler(QuickMenu_View)));
                return menus;
            }

            if (key != Core.LocalDhtID)
                return null;

            if (menuType == InterfaceMenuType.Internal)
                menus.Add(new MenuItemInfo("Comm/Mail", MailRes.Icon, new EventHandler(Menu_View)));

            if (menuType == InterfaceMenuType.External)
                menus.Add(new MenuItemInfo("Mail", MailRes.Icon, new EventHandler(Menu_View)));


            return menus;
        }

        internal void QuickMenu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            ulong key = 0;

            if (node != null)
                key = node.GetKey();

            // if window already exists to node, show it
            ComposeMail compose = new ComposeMail(this, key);

            Core.InvokeView(true, compose);
        }

        void Menu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            if (node.GetKey() != Core.LocalDhtID)
                return;

            MailView view = new MailView(this);

            Core.InvokeView(node.IsExternal(), view);
        }

        internal void SendMail(List<ulong> to, List<AttachedFile> files, string subject, string body)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreBlocked(delegate() { SendMail(to, files, subject, body); });
                return;
            }

            // exception handled by compose mail interface
            MailHeader header = new MailHeader();
            header.Source = Core.User.Settings.KeyPublic;
            header.SourceID = Core.LocalDhtID;
            header.LocalKey.GenerateKey();
            header.LocalKey.IV = new byte[header.LocalKey.IV.Length];
            header.Read = true;

            // setup temp file
            string tempPath = Core.GetTempPath();
            FileStream tempFile = new FileStream(tempPath, FileMode.CreateNew);
            CryptoStream stream = new CryptoStream(tempFile, header.LocalKey.CreateEncryptor(), CryptoStreamMode.Write);
            int written = 0;

            // build mail file
            written += Protocol.WriteToFile(new MailInfo(subject, Core.TimeNow.ToUniversalTime(), files.Count > 0), stream);

            foreach (ulong id in to)
                written += Protocol.WriteToFile(new MailDestination(Core.KeyMap[id], false), stream);

            byte[] bodyBytes = Core.Protocol.UTF.GetBytes(body);
            written += Protocol.WriteToFile(new MailFile("body", bodyBytes.Length), stream);

            foreach (AttachedFile attached in files)
                written += Protocol.WriteToFile(new MailFile(attached.Name, attached.Size), stream);

            stream.WriteByte(0); // end packets
            header.FileStart = (ulong)written + 1;

            // write files
            stream.Write(bodyBytes, 0, bodyBytes.Length);

            if (files != null)
            {
                int buffSize = 4096;
                byte[] buffer = new byte[buffSize];

                foreach (AttachedFile attached in files)
                {
                    FileStream embed = new FileStream(attached.FilePath, FileMode.Open, FileAccess.Read);

                    int read = buffSize;
                    while (read == buffSize)
                    {
                        read = embed.Read(buffer, 0, buffSize);
                        stream.Write(buffer, 0, read);
                    }

                    embed.Close();
                }
            }

            stream.FlushFinalBlock();
            stream.Close();


            // finish building header
            Utilities.ShaHashFile(tempPath, ref header.FileHash, ref header.FileSize);


            // move file, overwrite if need be, local id used so filename is the same for all targets
            string finalPath = MailPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, Core.LocalDhtID, header.FileHash);
            File.Move(tempPath, finalPath);

            // write header to outbound file
            if (Outbox == null)
                LoadLocalHeaders(ref Outbox, "outbox");

            LocalMail message = LoadLocalMail(header);

            if (message != null)
            {
                Outbox.Add(message);
                SaveLocalHeaders(Outbox, "outbox");

                if (MailUpdate != null)
                    Core.RunInGuiThread(MailUpdate, false, message);
            }

            // write headers to outbound cache file
            List<ulong> targets = new List<ulong>();
            targets.AddRange(to);

            // add targets as unacked
            ulong hashID = BitConverter.ToUInt64(header.FileHash, 0);

            List<KeyValuePair<ulong, byte[]>> publishTargets = new List<KeyValuePair<ulong, byte[]>>();
            foreach (ulong id in targets)
            {
                if (!PendingMail.ContainsKey(hashID))
                    PendingMail[hashID] = new List<ulong>();

                PendingMail[hashID].Add(id);
            }
            
            SavePending();
            header.SourceVersion = LocalPending.Header.Version;

            // publish to targets
            foreach (ulong id in targets)
            {
                header.Target = Core.KeyMap[id];
                header.TargetID = id;
                header.FileKey = EncodeFileKey(Utilities.KeytoRsa(Core.KeyMap[id]), header.LocalKey, header.FileStart);
                header.MailID  = GetMailID(hashID, id);

                if (PendingMap.ContainsKey(id))
                    header.TargetVersion = PendingMap[id].Header.Version;

                byte[] signed = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, header.Encode(Core.Protocol, false) );

                Core.OperationNet.Store.PublishNetwork(id, ComponentID.Mail, signed);

                Store_Local(new DataReq(null, id, ComponentID.Mail, signed)); // cant direct process_header, because header var is being modified
            }

            SaveHeaders();
        }

        byte[] EncodeFileKey(RSACryptoServiceProvider rsa, RijndaelManaged crypt, ulong fileStart)
        {
            byte[] buffer = new byte[crypt.Key.Length + 8];

            crypt.Key.CopyTo(buffer, 0);
            BitConverter.GetBytes(fileStart).CopyTo(buffer, crypt.Key.Length);

            return rsa.Encrypt(buffer, false);
        }

        void DecodeFileKey(byte[] enocoded, ref RijndaelManaged fileKey, ref ulong fileStart)
        {
            byte[] decoded = Core.User.Settings.KeyPair.Decrypt(enocoded, false);

            if (decoded.Length < fileKey.Key.Length + 8)
                return;

            fileKey.IV = new byte[fileKey.IV.Length];
            fileKey.Key = Utilities.ExtractBytes(decoded, 0, fileKey.Key.Length);

            fileStart = BitConverter.ToUInt64(decoded, fileKey.Key.Length);
        }

        internal byte[] GetMailID(ulong hashID, ulong dhtID)
        {
            byte[] buffer = new byte[16];

            BitConverter.GetBytes(hashID).CopyTo(buffer, 0);
            BitConverter.GetBytes(dhtID).CopyTo(buffer, 8);

            ICryptoTransform transform = MailIDKey.CreateEncryptor();
            buffer = transform.TransformFinalBlock(buffer, 0, buffer.Length);

            return buffer;
        }

        // only unacked are searched for in mail, mail/acks are replicated
        private void StartSearch(ulong key, uint version)
        {
            byte[] parameters = new MailIdent(MailPacket.Pending, key, BitConverter.GetBytes(version)).Encode();

            StartSearch(key, parameters);
        }

        private void StartSearch(ulong key, byte[] parameters)
        {
            DhtSearch search = Core.OperationNet.Searches.Start(key, "Mail", ComponentID.Mail, parameters, new EndSearchHandler(EndSearch));

            if (search != null)
                search.TargetResults = 2;
        }

        void EndSearch(DhtSearch search)
        {
            foreach (SearchValue found in search.FoundValues)
                Store_Local(new DataReq(found.Sources, search.TargetID, ComponentID.Mail, found.Value));
        }

        List<byte[]> Search_Local(ulong key, byte[] parameters)
        {
            List<Byte[]> results = new List<byte[]>();

            MailIdent ident = MailIdent.Decode(parameters, 0);

            // unacked
            if (ident.Packet == MailPacket.Pending)
            {
                // cant do find unacked because thats version specific
                if (PendingMap.ContainsKey(key))
                {
                    CachedPending pending = PendingMap[key];
                    
                    uint minVersion = BitConverter.ToUInt32(ident.Data, 0);

                    if (pending.Header.Version >= minVersion)
                        results.Add(pending.SignedHeader);
                }
            }

            // ack 
            if (ident.Packet == MailPacket.Ack)
            {
                CachedAck cached = FindAck(ident);

                if (cached != null)
                    results.Add(cached.SignedAck);
            }

            // mail
            if (ident.Packet == MailPacket.MailHeader)
            {
                CachedMail cached = FindMail(ident);

                if (cached != null)
                    results.Add(cached.SignedHeader);
            }

            return results;
        }

        bool Transfers_FileSearch(ulong key, FileDetails details)
        {
            lock (MailMap)
                if (MailMap.ContainsKey(key))
                {
                    List<CachedMail> list = MailMap[key];

                    foreach (CachedMail mail in list)
                        if (details.Size == mail.Header.FileSize && Utilities.MemCompare(details.Hash, mail.Header.FileHash))
                            return true;
                }

            lock (PendingMap)
                if (PendingMap.ContainsKey(key))
                {
                    CachedPending pending = PendingMap[key];

                    if (details.Size == pending.Header.FileSize && Utilities.MemCompare(details.Hash, pending.Header.FileHash))
                        return true;
                }

            return false;
        }

        string Transfers_FileRequest(ulong key, FileDetails details)
        {
            lock (MailMap)
                if (MailMap.ContainsKey(key))
                {
                    List<CachedMail> list = MailMap[key];

                    foreach (CachedMail mail in list)
                        if (details.Size == mail.Header.FileSize && Utilities.MemCompare(details.Hash, mail.Header.FileHash))
                        {
                            string path = GetCachePath(mail.Header);
                            if (File.Exists(path))
                                return path;

                            path = GetLocalPath(mail.Header);
                            if (File.Exists(path))
                                return path;
                        }
                }

            lock (PendingMap)
                if (PendingMap.ContainsKey(key))
                {
                    CachedPending pending = PendingMap[key];

                    if (details.Size == pending.Header.FileSize && Utilities.MemCompare(details.Hash, pending.Header.FileHash))
                        return GetFilePath(pending.Header);
                }

            return null;
        }

        void Store_Local(DataReq store)
        {
            // getting published to - search results - patch

            SignedData signed = SignedData.Decode(Core.Protocol, store.Data);

            if (signed == null)
                return;

            G2Header embedded = new G2Header(signed.Data);

            // figure out data contained
            if (Core.Protocol.ReadPacket(embedded))
            {
                if (embedded.Name == MailPacket.MailHeader)
                    Process_MailHeader(store, signed, MailHeader.Decode(Core.Protocol, embedded));

                else if (embedded.Name == MailPacket.Ack)
                    Process_MailAck(store, signed, MailAck.Decode(Core.Protocol, embedded), false);

                else if (embedded.Name == MailPacket.Pending)
                    Process_PendingHeader(store, signed, PendingHeader.Decode(Core.Protocol, embedded));
            }
        }

        ReplicateData Store_Replicate(DhtContact contact, bool add)
        {
            // size 1+8+4=13
            // unack -- packet.unack / Dhtid    / version
            // mail  -- packet.mail  / targetid / mailid(4) 
            // ack   -- packet.ack   / targetid / mailid(4)

            if (!Network.Established)
                return null;


            ReplicateData data = new ReplicateData(ComponentID.Profile, MailIdent.SIZE);

            byte[] patch = new byte[MailIdent.SIZE];

            // unacked
            lock(PendingMap)
                foreach (CachedPending pending in PendingMap.Values)
                    if (Utilities.InBounds(pending.Header.KeyID, pending.DhtBounds, contact.DhtID)) 
                    {
                        DhtContact target = contact;
                        pending.DhtBounds = Store.RecalcBounds(pending.Header.KeyID, add, ref target);

                        if (target != null)
                            data.Add(target, new MailIdent(MailPacket.Pending, pending.Header.KeyID, BitConverter.GetBytes(pending.Header.Version)).Encode());
                    }

            // acks
            lock (AckMap)
                foreach (List<CachedAck> list in AckMap.Values)
                    foreach(CachedAck cached in list)
                        if (Utilities.InBounds(cached.Ack.TargetID, cached.DhtBounds, contact.DhtID)) 
                        {
                            DhtContact target = contact;
                            cached.DhtBounds = Store.RecalcBounds(cached.Ack.TargetID, add, ref target);

                            if (target != null)
                                data.Add(target, new MailIdent(MailPacket.Ack, cached.Ack.TargetID, Utilities.ExtractBytes(cached.Ack.MailID, 0, 4)).Encode());
                        }

            // mail
            lock (MailMap)
                foreach (List<CachedMail> list in MailMap.Values)
                    foreach (CachedMail cached in list)
                        if (Utilities.InBounds(cached.Header.TargetID, cached.DhtBounds, contact.DhtID)) 
                        {
                            DhtContact target = contact;
                            cached.DhtBounds = Store.RecalcBounds(cached.Header.TargetID, add, ref target);

                            if (target != null)
                                data.Add(target, new MailIdent(MailPacket.MailHeader, cached.Header.TargetID, Utilities.ExtractBytes(cached.Header.MailID, 0, 4)).Encode());                   
                        }

            return data;
        }

        void Store_Patch(DhtAddress source, ulong distance, byte[] data)
        {
            if (data.Length % MailIdent.SIZE != 0)
                return;

            int offset = 0;
            for (int i = 0; i < data.Length; i += MailIdent.SIZE)
            {
                MailIdent ident = MailIdent.Decode(data, i);
                offset += MailIdent.SIZE;

                if (!Utilities.InBounds(Core.LocalDhtID, distance, ident.DhtID))
                    continue;

                bool request = false;

                // unacked
                if (ident.Packet == MailPacket.Pending)
                {
                    CachedPending pending = FindPending(ident);

                    uint version = BitConverter.ToUInt32(ident.Data, 0);

                    if (pending == null)
                        request = true;
                    else
                    {
                       

                        if (pending.Header.Version > version) // we have new version
                        {
                            Store.Send_StoreReq(source, 0, new DataReq(null, ident.DhtID, ComponentID.Mail, pending.SignedHeader));
                            continue;
                        }

                        pending.Unique = false;

                        if (pending.Header.Version == version) // we have current version
                            continue;

                        if (pending.Header.Version < version) // we have older version
                            request = true;
                    }
                }
               
                // ack
                if (ident.Packet == MailPacket.Ack)
                {
                    CachedAck ack = FindAck(ident);

                    if (ack == null)
                        request = true;
                    else
                        ack.Unique = false;
                }

                // mail
                if (ident.Packet == MailPacket.MailHeader)
                {
                    CachedMail mail = FindMail(ident);
                    
                    if (mail == null)
                        request = true;
                    else
                        mail.Unique = false;
                }

                if (!request)
                    return;

                if (Network.Established)
                    Network.Searches.SendDirectRequest(source, ident.DhtID, ComponentID.Mail, ident.Encode());
                else
                {
                    if (!DownloadLater.ContainsKey(ident.DhtID))
                        DownloadLater[ident.DhtID] = new List<MailIdent>();

                    DownloadLater[ident.DhtID].Add(ident);
                }
            }
        }

        CachedMail FindMail(MailIdent ident)
        {
            lock (MailMap)
                if (MailMap.ContainsKey(ident.DhtID))
                    foreach (CachedMail cached in MailMap[ident.DhtID])
                        if (Utilities.MemCompare(cached.Header.MailID, 0, ident.Data, 0, 4))
                            return cached;

            return null;
        }

        CachedAck FindAck(MailIdent ident)
        {
            lock (AckMap)
                if (AckMap.ContainsKey(ident.DhtID))
                    foreach (CachedAck cached in AckMap[ident.DhtID])
                        if (Utilities.MemCompare(cached.Ack.MailID, 0, ident.Data, 0, 4))
                            return cached;

            return null;
        }

        private CachedPending FindPending(MailIdent ident)
        {
            lock (PendingMap)
                if (PendingMap.ContainsKey(ident.DhtID))
                    return PendingMap[ident.DhtID];

            return null;
        }

        private void Process_MailHeader(DataReq data, SignedData signed, MailHeader header)
        {
            Core.IndexKey(header.SourceID, ref header.Source);
            Core.IndexKey(header.TargetID, ref header.Target);

            // check if already cached
            if (MailMap.ContainsKey(header.TargetID))
                foreach (CachedMail cached in MailMap[header.TargetID])
                    if (Utilities.MemCompare(header.MailID, cached.Header.MailID))
                        return;

            bool cache = false;

            // source's pending file
            if (PendingMap.ContainsKey(header.SourceID))
            {
                CachedPending pending = PendingMap[header.SourceID];

                if (header.SourceVersion > pending.Header.Version)
                {
                    StartSearch(header.SourceID, header.SourceVersion);
                    cache = true;
                }
                else
                {
                    
                    if (IsMailPending(pending, header.MailID))
                        cache = true;
                    else
                        return; //ID not in source's pending mail list
                }
            }
            else
            {
                StartSearch(header.SourceID, header.SourceVersion);
                cache = true;
            }

            // check target's pending file
            if (PendingMap.ContainsKey(header.TargetID))
            {
                CachedPending pending = PendingMap[header.TargetID];

                if (header.TargetVersion > pending.Header.Version)
                {
                    StartSearch(header.TargetID, header.TargetVersion);
                    cache = true;
                }
                else
                {
                    if (IsAckPending(pending, header.MailID))
                        return;  //ID in target's pending ack list
                    else
                        cache = true;
                }
            }
            else
            {
                StartSearch(header.TargetID, header.TargetVersion);
                cache = true;
            }

            if(cache)
                CacheMail(signed, header);
        }

        private void Download_Mail(SignedData signed, MailHeader header)
        {
            Utilities.CheckSignedData(header.Source, signed.Data, signed.Signature);

            FileDetails details = new FileDetails(ComponentID.Mail, header.FileHash, header.FileSize, null);

            Core.Transfers.StartDownload(header.TargetID, details, new object[] { signed, header }, new EndDownloadHandler(EndDownload_Mail));
        }

        private void EndDownload_Mail(string path, object[] args)
        {
            SignedData signedHeader = (SignedData)args[0];
            MailHeader header = (MailHeader)args[1];

            try
            {
                File.Move(path, GetCachePath(header));
            }
            catch { return; }

            CacheMail(signedHeader, header);
        }

        private void CacheMail(SignedData signed, MailHeader header)
        {
            try
            {
                bool exists = false;

                // try cache path
                string path = GetCachePath(header);
                if (File.Exists(path))
                    exists = true;

                // try local path
                if (!exists)
                {
                    path = GetLocalPath(header);
                    if (File.Exists(path))
                        exists = true;
                }

                if(!exists)
                {
                    Download_Mail(signed, header);
                    return;
                }

                if(header.TargetID == Core.LocalDhtID)
                {
                    ReceiveMail(path, signed, header);
                    return;
                }

                if(!MailMap.ContainsKey(header.TargetID))
                    MailMap[header.TargetID] = new List<CachedMail>();

                CachedMail mail = new CachedMail() ;

                mail.Header = header;
                mail.SignedHeader = signed.Encode(Core.Protocol);
                mail.Unique = Core.Loading;

                MailMap[header.TargetID].Add(mail);

                RunSaveHeaders = true;
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Mail", "Error loading mail " + ex.Message);
            }
        }

        private void ReceiveMail(string path, SignedData signed, MailHeader header)
        {
            header.Received = Core.TimeNow.ToUniversalTime();

            // move file
            string localPath = GetLocalPath(header);
            string cachePath = GetCachePath(header);
           
            if (File.Exists(localPath))
                return;

            File.Move(cachePath, localPath);

            // add to inbound list
            if (Inbox == null)
                LoadLocalHeaders(ref Inbox, "inbox");

            DecodeFileKey(header.FileKey, ref header.LocalKey, ref header.FileStart);

            LocalMail message = LoadLocalMail(header);

            if (message != null)
            {
                Inbox.Add(message);
                
                SaveLocalHeaders(Inbox, "inbox");

                if (MailUpdate != null)
                    Core.RunInGuiThread(MailUpdate, true, message);
            }

            // publish ack
            MailAck ack  = new MailAck();
            ack.MailID   = header.MailID;
            ack.Source   = Core.User.Settings.KeyPublic;
            ack.SourceID = Core.LocalDhtID;
            ack.Target   = header.Source;
            ack.TargetID = header.TargetID; 
            ack.TargetVersion = header.SourceVersion;
            
            // update pending
            if (!PendingAcks.ContainsKey(ack.TargetID))
                PendingAcks[ack.TargetID] = new List<byte[]>();
            PendingAcks[ack.TargetID].Add(ack.MailID);

            ack.SourceVersion = LocalPending.Header.Version;
            
            SavePending(); // also publishes

            // send 
            byte[] signedAck = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, ack.Encode(Core.Protocol));
            Core.OperationNet.Store.PublishNetwork(header.SourceID, ComponentID.Mail, signedAck);
            Store_Local(new DataReq(null, header.SourceID, ComponentID.Mail, signedAck)); // cant direct process_header, because header var is being modified
            RunSaveHeaders = true;

            Core.MakeNews("Mail Received from " + Core.Links.GetName(message.From), message.From, 0, false, MailRes.Icon, Menu_View);
         
        }

        private bool IsMailPending(CachedPending pending, byte[] mailID)
        {
            try
            {
                string path = GetFilePath(pending.Header);

                FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                CryptoStream crypto = new CryptoStream(file, pending.Header.FileKey.CreateDecryptor(), CryptoStreamMode.Read);

                byte[] divider = new byte[16];
                byte[] buffer = new byte[4096];

                int read = buffer.Length;
                while (read == buffer.Length)
                {
                    read = crypto.Read(buffer, 0, buffer.Length);

                    if (read % 16 != 0)
                        return false;

                    for (int i = 0; i < read; i += 16)
                    {
                        if (Utilities.MemCompare(divider, 0, buffer, i, 16))
                            return false;

                        if (Utilities.MemCompare(mailID, 0, buffer, i, 16))
                        {
                            crypto.Close();
                            return true;
                        }
                    }
                }

                crypto.Close();
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Mail", "Error checking pending " + ex.Message);
            }

            return false;
        }

        private bool IsAckPending(CachedPending pending, byte[] mailID)
        {
            try
            {
                string path = GetFilePath(pending.Header);

                FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                CryptoStream crypto = new CryptoStream(file, pending.Header.FileKey.CreateDecryptor(), CryptoStreamMode.Read);

                bool dividerReached = false;
                byte[] divider = new byte[16];
                byte[] buffer = new byte[4096];

                int read = buffer.Length;
                while (read == buffer.Length)
                {
                    read = crypto.Read(buffer, 0, buffer.Length);

                    if (read % 16 != 0)
                        return false;

                    for (int i = 0; i < read; i += 16)
                    {
                        if (Utilities.MemCompare(divider, 0, buffer, i, 16))
                            dividerReached = true;

                        else if (dividerReached && Utilities.MemCompare(mailID, 0, buffer, i, 16))
                        {
                            crypto.Close();
                            return true;
                        }
                    }
                }

                crypto.Close();
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Mail", "Error checking pending " + ex.Message);
            }

            return false;
        }

        private void Process_MailAck(DataReq data, SignedData signed, MailAck ack, bool verified)
        {
            Core.IndexKey(ack.SourceID, ref ack.Source);
            Core.IndexKey(ack.TargetID, ref ack.Target);

            if(!verified)
                Utilities.CheckSignedData(ack.Source, signed.Data, signed.Signature);

            // check if local
            if(ack.TargetID == Core.LocalDhtID)
            {
                ReceiveAck(ack);
                return;
            }

            // check if already cached
            if (AckMap.ContainsKey(ack.TargetID))
                foreach (CachedAck cached in AckMap[ack.TargetID])
                    if (Utilities.MemCompare(ack.MailID, cached.Ack.MailID))
                        return;

            bool add = true;

            // check target pending file
            if (PendingMap.ContainsKey(ack.TargetID))
            {
                CachedPending pending = PendingMap[ack.TargetID];

                // if our version of the pending file is out of date
                if (ack.TargetVersion > pending.Header.Version)
                    StartSearch(ack.TargetID, ack.TargetVersion);

                // not pending, means its not in file, which means it IS acked
                else if (!IsMailPending(pending, ack.MailID))
                    add = false;
            }
            else
                StartSearch(ack.TargetID, ack.TargetVersion);

            // check source pending file, but dont search for it or anything, focus on the target, source is backup
            if (PendingMap.ContainsKey(ack.SourceID))
            {
                CachedPending pending = PendingMap[ack.SourceID];

                if (pending.Header.Version > ack.SourceVersion)
                    if (!IsAckPending(pending, ack.MailID))
                        add = false;
            }

            if(add)
            {
                if (!AckMap.ContainsKey(ack.TargetID))
                    AckMap[ack.TargetID] = new List<CachedAck>();

                AckMap[ack.TargetID].Add(new CachedAck(signed.Encode(Core.Protocol), ack, Core.Loading));
            }
        }

        private void ReceiveAck(MailAck ack)
        {
            ulong hashID, dhtID;
            DecodeMailID(ack.MailID, out hashID, out dhtID);

            if (dhtID != ack.SourceID)
                return;

            if (!PendingMail.ContainsKey(hashID))
                return;

            if (!PendingMail[hashID].Contains(dhtID))
                return;

            PendingMail[hashID].Remove(dhtID);

            if (PendingMail[hashID].Count == 0)
                PendingMail.Remove(hashID);

            SavePending(); // also publishes

            // update interface
            if(Outbox != null && MailUpdate != null)
                foreach(LocalMail message in Outbox)
                    if (Utilities.MemCompare(message.Header.MailID, ack.MailID))
                    {
                        Core.RunInGuiThread(MailUpdate, false, message);
                        break;
                    }
        }

        private void Process_PendingHeader(DataReq data, SignedData signed, PendingHeader header)
        {
            Core.IndexKey(header.KeyID, ref header.Key);
           
            // if link loaded
            if (PendingMap.ContainsKey(header.KeyID))
            {
                CachedPending current = PendingMap[header.KeyID];

                // if remote source's version old
                if (header.Version < current.Header.Version)
                {
                    if (data != null && data.Sources != null)
                        foreach (DhtAddress source in data.Sources)
                            Store.Send_StoreReq(source, data.LocalProxy, new DataReq(null, current.Header.KeyID, ComponentID.Mail, current.SignedHeader));

                    return;
                }

                // higher version
                else if (header.Version > current.Header.Version)
                {
                    CachePending(signed, header);
                }
            }

            // else load file, set new header after file loaded
            else
                CachePending(signed, header);    
        }

        private void Download_Pending(SignedData signed, PendingHeader header)
        {
            Utilities.CheckSignedData(header.Key, signed.Data, signed.Signature);

            FileDetails details = new FileDetails(ComponentID.Mail, header.FileHash, header.FileSize, null);

            Core.Transfers.StartDownload(header.KeyID, details, new object[] { signed, header }, new EndDownloadHandler(EndDownload_Pending));
        }

        private void EndDownload_Pending(string path, object[] args)
        {
            SignedData signedHeader = (SignedData)args[0];
            PendingHeader header = (PendingHeader)args[1];

            try
            {
                File.Move(path, GetFilePath(header));
            }
            catch { return; }

            CachePending(signedHeader, header);
        }

        private void CachePending(SignedData signed, PendingHeader header)
        {
            try
            {
                string path = "";
                if (header.FileHash != null)
                    path = GetFilePath(header);

                if (!File.Exists(path))
                {
                    Download_Pending(signed, header);
                    return;
                }

                if (!PendingMap.ContainsKey(header.KeyID))
                    PendingMap[header.KeyID] = new CachedPending();

                CachedPending pending = PendingMap[header.KeyID];

                List<byte[]> pendingMailIDs = new List<byte[]>();
                List<byte[]> pendingAckIDs = new List<byte[]>();


                // load pending file
                FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                CryptoStream crypto = new CryptoStream(file, header.FileKey.CreateDecryptor(), CryptoStreamMode.Read);

                bool dividerPassed = false;
                byte[] divider = new byte[16];
                byte[] buffer = new byte[4096];
                
                int read = buffer.Length;
                while (read == buffer.Length)
                {
                    read = crypto.Read(buffer, 0, buffer.Length);

                    if (read % 16 != 0)
                        throw new Exception("Bad read pending file");

                    for (int i = 0; i < read; i += 16)
                    {
                        byte[] id = Utilities.ExtractBytes(buffer, i, 16);

                        if (Utilities.MemCompare(id, divider))
                            dividerPassed = true;

                        else if (!dividerPassed)
                            pendingMailIDs.Add(id);

                        else
                            pendingAckIDs.Add(id);
                    }  
                }
                
                crypto.Close();


                // if the local pending file that we are loading
                if (header.KeyID == Core.LocalDhtID)
                {
                    // setup local mail pending
                    PendingMail.Clear();

                    foreach (byte[] id in pendingMailIDs)
                    {
                        ulong hashID, dhtID;
                        DecodeMailID(id, out hashID, out dhtID);

                        if (!PendingMail.ContainsKey(hashID))
                            PendingMail[hashID] = new List<ulong>();

                        PendingMail[hashID].Add(dhtID);
                    }

                    // setup local acks pending
                    PendingAcks.Clear();

                    List<ulong> targets = new List<ulong>();

                    string localpath = MailPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "acktargets");

                    if (File.Exists(localpath))
                    {
                        file = new FileStream(localpath, FileMode.Open);
                        crypto = new CryptoStream(file, LocalFileKey.CreateDecryptor(), CryptoStreamMode.Read);

                        read = buffer.Length;
                        while (read == buffer.Length)
                        {
                            read = crypto.Read(buffer, 0, buffer.Length);

                            if (read % 8 != 0)
                                throw new Exception("Bad read targets file");

                            for (int i = 0; i < read; i += 8)
                                targets.Add(BitConverter.ToUInt64(buffer, i));
                        }

                        crypto.Close();

                        for (int i = 0; i < targets.Count; i++)
                        {
                            if (!PendingAcks.ContainsKey(targets[i]))
                                PendingAcks[targets[i]] = new List<byte[]>();

                            PendingAcks[targets[i]].Add(pendingAckIDs[i]);
                        }
                    }
                }   


                // delete old file
                if (pending.Header != null)
                {
                    string oldPath = GetFilePath(pending.Header);
                    if (path != oldPath && File.Exists(oldPath))
                        try { File.Delete(oldPath); }
                        catch { }
                }

                // set new header
                pending.Header = header;
                pending.SignedHeader = signed.Encode(Core.Protocol);
                pending.Unique = Core.Loading;

                if(pending.Header.KeyID == Core.LocalDhtID)
                    LocalPending = pending;

                CheckCache(pending, pendingMailIDs, pendingAckIDs);
                

                RunSaveHeaders = true;
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Mail", "Error loading pending " + ex.Message);
            }
        }

        private void DecodeMailID(byte[] encrypted, out ulong hashID, out ulong dhtID)
        {
            ICryptoTransform transform = MailIDKey.CreateDecryptor();
            encrypted = transform.TransformFinalBlock(encrypted, 0, encrypted.Length);

            hashID = BitConverter.ToUInt64(encrypted, 0);
            dhtID  = BitConverter.ToUInt64(encrypted, 8);
        }

        private void CheckCache(CachedPending pending, List<byte[]> mailIDs, List<byte[]> ackIDs)
        {
/* entry removal
 *      mail map
 *          ID not in source's pending mail list X
 *          ID in target's pending ack list X
 *      ack map
 *          ID not in target's pending mail list X
 *          ID not in source's pending ack list X
 * 
 *      pending mail
 *          Ack received from target
 *          ID in targets pending ack list
 *      pending ack
 *          ID not in targets pending mail list
 * */

            PendingHeader header = pending.Header;

            List<string> removePaths = new List<string>();

            // check mail
            List<CachedMail> removeMails = new List<CachedMail>();    
            List<ulong> removeKeys = new List<ulong>();
            
            lock (MailMap)
            {
                foreach (ulong key in MailMap.Keys)
                {
                    List<CachedMail> list = MailMap[key];
                    removeMails.Clear();

                    foreach (CachedMail mail in list)
                    {
                        // ID not in source's pending mail list
                        if (mail.Header.SourceID == header.KeyID &&
                            header.Version >= mail.Header.SourceVersion &&
                            !IDinList(mailIDs, mail.Header.MailID))
                            removeMails.Add(mail);
                        

                        // ID in target's pending ack list
                        if (mail.Header.TargetID == pending.Header.KeyID &&
                            header.Version >= mail.Header.TargetVersion &&
                            IDinList(ackIDs, mail.Header.MailID))
                            removeMails.Add(mail);
                    }

                    foreach (CachedMail mail in removeMails)
                    {
                        removePaths.Add(GetCachePath(mail.Header));
                        list.Remove(mail);
                    }

                    if (list.Count == 0)
                        removeKeys.Add(key);
                }

                foreach (ulong key in removeKeys)
                    MailMap.Remove(key);
            }

            List<CachedAck> removeAcks = new List<CachedAck>();
            removeKeys.Clear();
            
            // check acks
            lock (AckMap)
            {              
                foreach (ulong key in AckMap.Keys)
                {
                    List<CachedAck> list = AckMap[key];
                    removeAcks.Clear();

                    foreach (CachedAck cached in list)
                    {
                        // ID not in target's pending mail list
                        if (cached.Ack.TargetID == header.KeyID &&
                            header.Version >= cached.Ack.TargetVersion &&
                            !IDinList(mailIDs, cached.Ack.MailID))
                            removeAcks.Add(cached);

                        // ID not in source's pending ack list
                        if (cached.Ack.SourceID == header.KeyID &&
                            header.Version >= cached.Ack.SourceVersion &&
                            !IDinList(ackIDs, cached.Ack.MailID))
                            removeAcks.Add(cached);
                    }
                    foreach (CachedAck ack in removeAcks)
                        list.Remove(ack);

                    if (list.Count == 0)
                        removeKeys.Add(key);
                }

                foreach (ulong key in removeKeys)
                    AckMap.Remove(key);
            }

            // mark pending changed and re-save pending
            bool resavePending = false;

            // pending mail, ID in target's pending ack list
            List<ulong> removeTargets = new List<ulong>();
            removeKeys.Clear();

            foreach (ulong hashID in PendingMail.Keys)
            {
                removeTargets.Clear();

                foreach (ulong target in PendingMail[hashID])
                    if (header.KeyID == target)
                    {
                        byte[] mailID = GetMailID(hashID, target);

                        if (IDinList(ackIDs, mailID))
                        {
                            removeTargets.Add(target); // dont have to remove file cause its local
                            resavePending = true;
                        }
                    }

                foreach (ulong target in removeTargets)
                    PendingMail[hashID].Remove(target);

                if (PendingMail[hashID].Count == 0)
                    removeKeys.Add(hashID);
            }

            foreach (ulong hashID in removeKeys)
                PendingMail.Remove(hashID);

            // pending ack, ID not in targets pending mail list
            foreach (ulong target in PendingAcks.Keys)
                if (target == header.KeyID)
                {
                    List<byte[]> removeIDs = new List<byte[]>();

                    foreach (byte[] mailID in PendingAcks[target])
                        if (!IDinList(mailIDs, mailID))
                        {
                            removeIDs.Add(mailID);
                            resavePending = true;
                        }

                    foreach (byte[] mailID in removeIDs)
                        PendingAcks[target].Remove(mailID);

                    if (PendingAcks[target].Count == 0)
                        PendingAcks.Remove(target);

                    break;
                }
                     

            // remove files
            foreach(string path in removePaths)
            {
                try
                {
                    File.Delete(path);
                }
                catch {}
            }

            if (resavePending)
                SavePending();
        }

        private bool IDinList(List<byte[]> ids, byte[] find)
        {
            foreach (byte[] id in ids)
                if (Utilities.MemCompare(id, find))
                    return true;

            return false;
        }

        private void SavePending()
        {
            // pending file is shared over the network, the first part is pending mail IDs
            // the next part is pending ack IDs, because IDs are encrypted ack side needs to seperately
            // store the target ID of the ack locally, local host is only able to decrypt pending mail IDs

            try
            {
                PendingHeader header = LocalPending.Header;

                string oldFile = null;

                if (header != null)
                    oldFile = GetFilePath(header);
                else
                    header = new PendingHeader();

                header.Key = Core.User.Settings.KeyPublic;
                header.KeyID = Core.LocalDhtID; // set so keycheck works
                header.Version++;
                header.FileKey.GenerateKey();
                header.FileKey.IV = new byte[header.FileKey.IV.Length];


                string tempPath = Core.GetTempPath();
                FileStream tempFile = new FileStream(tempPath, FileMode.CreateNew);
                CryptoStream stream = new CryptoStream(tempFile, header.FileKey.CreateEncryptor(), CryptoStreamMode.Write);

                byte[] buffer = null;

                // write pending mail
                foreach(ulong hashID in PendingMail.Keys)
                    foreach (ulong dhtID in PendingMail[hashID])
                    {
                        buffer = GetMailID(hashID, dhtID);
                        stream.Write(buffer, 0, buffer.Length);
                    }

                // divider
                buffer = new byte[16];
                stream.Write(buffer, 0, buffer.Length);

                // write pending acks
                foreach (ulong target in PendingAcks.Keys)
                    foreach (byte[] mailID in PendingAcks[target])
                        stream.Write(mailID, 0, mailID.Length);

                stream.FlushFinalBlock();
                stream.Close();


                // finish building header
                Utilities.ShaHashFile(tempPath, ref header.FileHash, ref header.FileSize);


                // move file, overwrite if need be
                string finalPath = GetFilePath(header);
                File.Move(tempPath, finalPath);
   
                if (oldFile != null && File.Exists(oldFile))
                    try { File.Delete(oldFile); }
                    catch { }


                // save pending ack targets in local file
                string localpath = MailPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "acktargets");

                if (PendingAcks.Count > 0)
                {
                    FileStream file = new FileStream(localpath, FileMode.Create);
                    stream = new CryptoStream(file, LocalFileKey.CreateEncryptor(), CryptoStreamMode.Write);

                    foreach (ulong target in PendingAcks.Keys)
                        stream.Write(BitConverter.GetBytes(target), 0, 8);

                    stream.FlushFinalBlock();
                    stream.Close();
                }
                else if (File.Exists(localpath))
                    File.Delete(localpath);

                // re-load
                CachePending(new SignedData(Core.Protocol, Core.User.Settings.KeyPair, header), header);

                // publish header
                Core.OperationNet.Store.PublishNetwork(Core.LocalDhtID, ComponentID.Mail, LocalPending.SignedHeader);

            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Profile", "Error saving pending " + ex.Message);
            }
        }
        
        internal string GetFilePath(PendingHeader header)
        {
            return CachePath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, header.KeyID, header.FileHash);
        }

        internal string GetCachePath(MailHeader header)
        {
            return CachePath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, header.SourceID, header.FileHash);
        }

        internal string GetLocalPath(MailHeader header)
        {
            return MailPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, header.SourceID, header.FileHash);
        }

        internal void Reply(LocalMail message, string body)
        {
            ComposeMail compose = new ComposeMail(this, message.Header.SourceID);

            string subject = message.Info.Subject;
            if (!subject.StartsWith("RE: "))
                subject = "RE: " + subject;

            compose.SubjectTextBox.Text = subject;

            string header = "\n\n-----Original Message-----\n";
            header += "From: " + Core.Links.GetName(message.Header.SourceID) + "\n";
            header += "Sent: " + Utilities.FormatTime(message.Info.Date) + "\n";
            header += "To: " + GetNames(message.To) + "\n";

            if(message.CC.Count > 0)
                header += "CC: " + GetNames(message.CC) + "\n";

            header += "Subject: " + message.Info.Subject + "\n\n";

            compose.MessageBody.InputBox.AppendText(header);

            compose.MessageBody.InputBox.Select(compose.MessageBody.InputBox.TextLength, 0);
            compose.MessageBody.InputBox.SelectedRtf = body;

            compose.MessageBody.InputBox.Select(0, 0);

            
            Core.RunInGuiThread(Core.GuiMain.ShowExternal, compose);
        }

        internal void Forward(LocalMail message, string body)
        {
            ComposeMail compose = new ComposeMail(this, 0);


            string subject = message.Info.Subject;
            if (!subject.StartsWith("FW: "))
                subject = "FW: " + subject;

            compose.SubjectTextBox.Text = subject;

            //crit attach files


            string header = "\n\n-----Original Message-----\n";
            header += "From: " + Core.Links.GetName(message.Header.SourceID) + "\n";
            header += "Sent: " + Utilities.FormatTime(message.Info.Date) + "\n";
            header += "To: " + GetNames(message.To) + "\n";

            if (message.CC.Count > 0)
                header += "CC: " + GetNames(message.CC) + "\n";

            header += "Subject: " + message.Info.Subject + "\n\n";

            compose.MessageBody.InputBox.AppendText(header);

            compose.MessageBody.InputBox.Select(compose.MessageBody.InputBox.TextLength, 0);
            compose.MessageBody.InputBox.SelectedRtf = body;

            compose.MessageBody.InputBox.Select(0, 0);

            Core.RunInGuiThread(Core.GuiMain.ShowExternal, compose);
        }

        internal void DeleteLocal(LocalMail message, bool inbox)
        {
            File.Delete(GetLocalPath(message.Header));
            
            if (inbox)
            {
                Inbox.Remove(message);
                SaveLocalHeaders(Inbox, "inbox");
            }

            else
            {
                Outbox.Remove(message);
                SaveLocalHeaders(Outbox, "outbox");
            }
        }

        internal string GetNames(List<ulong> list)
        {
            string names = "";

            foreach (ulong id in list)
                names += Core.Links.GetName(id) + ", ";

            names = names.TrimEnd(new char[] { ' ', ',' });

            return names;
        }
    }

    internal class CachedPending
    {
        internal ulong DhtBounds = ulong.MaxValue;
        internal bool  Unique;

        internal PendingHeader Header;
        internal byte[]        SignedHeader;
    }

    internal class CachedMail
    {
        internal MailHeader Header;
        internal byte[]     SignedHeader;
        internal ulong      DhtBounds = ulong.MaxValue;
        internal bool       Unique;
    }

    internal class CachedAck
    {
        internal MailAck Ack;
        internal byte[]  SignedAck;
        internal ulong   DhtBounds = ulong.MaxValue;
        internal bool    Unique;

        internal CachedAck(byte[] signed, MailAck ack, bool unique)
        {
            SignedAck = signed;
            Ack = ack;
            Unique = unique;
        }
    }

    internal class MailIdent
    {
        internal const int SIZE = 13;

        internal byte   Packet;
        internal ulong  DhtID;
        internal byte[] Data;

        internal MailIdent()
        { 
        }

        internal MailIdent(byte packet, ulong dhtid, byte[] data)
        {
            Packet = packet;
            DhtID  = dhtid;
            Data   = data;
        }

        internal byte[] Encode()
        {
            byte[] encoded = new byte[SIZE];

            encoded[0] = Packet;
            BitConverter.GetBytes(DhtID).CopyTo(encoded, 1);
            Data.CopyTo(encoded, 9);

            return encoded;
        }

        internal static MailIdent Decode(byte[] data, int offset)
        {
            MailIdent ident = new MailIdent();

            ident.Packet = data[offset];
            ident.DhtID  = BitConverter.ToUInt64(data, offset + 1);
            ident.Data   = Utilities.ExtractBytes(data, offset + 9, 4);

            return ident;
        }
    }

    internal class LocalMail
    {
        internal MailInfo Info;

        internal ulong From;
        internal List<ulong> To = new List<ulong>();
        internal List<ulong> CC = new List<ulong>();

        internal List<MailFile> Attached = new List<MailFile>();

        internal MailHeader Header;
    }
}
