using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Transport;

using RiseOp.Services;
using RiseOp.Services.Location;
using RiseOp.Services.Transfer;


/* active shares are periodically published at fileID on network so that when source goes offline
 * more locations can be found
 */


namespace RiseOp.Services.Sharing
{
    internal class SharingPacket
    {
        internal const byte File = 0x10;
    }

    internal delegate void ShareUpdateHandler(OpShare share);

    class SharingService : OpService
    {
        public string Name { get { return "Sharing"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.Sharing; } }

        OpCore Core;
        DhtNetwork Network;

        internal ThreadedList<OpShare> ShareList = new ThreadedList<OpShare>();
        
        Thread ProcessFilesHandle; 
        Queue<OpShare> ProcessingQueue = new Queue<OpShare>();
        const int ProcessBufferSize = 1024 * 16;
        byte[] ProcessBuffer = new byte[ProcessBufferSize];


        Thread OpenFilesHandle;
        Queue<OpShare> OpenQueue = new Queue<OpShare>();
        int OpenBufferSize = 4096;
        byte[] OpenBuffer = new byte[4096]; // needs to be 4k to packet stream break/resume work

        bool KillThreads;        

        string SharePath;
        string DownloadPath;

        internal event ShareUpdateHandler GuiUpdate;

        const uint DataTypeShare = 0x01;
        const uint DataTypeLocation = 0x02;
        //const uint FileTypePublic = 0x03;


        internal SharingService(OpCore core)
        {
            Core = core;
            Network = core.Network;

            string rootPath = Core.User.RootPath + Path.DirectorySeparatorChar +
                        "Data" + Path.DirectorySeparatorChar +
                        ServiceID.ToString() + Path.DirectorySeparatorChar;

            SharePath = rootPath + DataTypeShare.ToString() + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(SharePath);

            DownloadPath = Core.User.RootPath + Path.DirectorySeparatorChar + "Downloads" + Path.DirectorySeparatorChar;


            Core.SecondTimerEvent += new TimerHandler(Core_SecondTimer);
            Core.MinuteTimerEvent += new TimerHandler(Core_MinuteTimer);

            // data
            Network.RudpControl.SessionUpdate += new SessionUpdateHandler(Session_Update);
            Network.RudpControl.SessionData[ServiceID, DataTypeShare] += new SessionDataHandler(Session_Data);

            Core.Transfers.FileSearch[ServiceID, DataTypeShare] += new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[ServiceID, DataTypeShare] += new FileRequestHandler(Transfers_FileRequest);

            // location
            Network.Store.StoreEvent[ServiceID, DataTypeLocation] += new StoreHandler(Store_Locations);
            Network.Searches.SearchEvent[ServiceID, DataTypeLocation] += new SearchRequestHandler(Search_Locations);


            LoadHeaders();
        }

        public void Dispose()
        {
            KillThreads = true;

            if (ProcessFilesHandle != null)
                Debug.Assert(ProcessFilesHandle.Join(5000));

            if(OpenFilesHandle != null)
                Debug.Assert(OpenFilesHandle.Join(5000));

            Core.SecondTimerEvent -= new TimerHandler(Core_SecondTimer);
            Core.MinuteTimerEvent -= new TimerHandler(Core_MinuteTimer);

            // file
            Network.RudpControl.SessionUpdate -= new SessionUpdateHandler(Session_Update);
            Network.RudpControl.SessionData[ServiceID, DataTypeShare] -= new SessionDataHandler(Session_Data);

            Core.Transfers.FileSearch[ServiceID, DataTypeShare] -= new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[ServiceID, DataTypeShare] -= new FileRequestHandler(Transfers_FileRequest);

            // location
            Network.Store.StoreEvent[ServiceID, DataTypeLocation] -= new StoreHandler(Store_Locations);
            Network.Searches.SearchEvent[ServiceID, DataTypeLocation] -= new SearchRequestHandler(Search_Locations);

        }

        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong user, uint project)
        {
            if (menuType == InterfaceMenuType.Internal)
                menus.Add(new MenuItemInfo("Data/Share", Res.ShareRes.Icon, new EventHandler(Menu_View)));

            if (menuType == InterfaceMenuType.Quick)
            {
                if (user == Core.UserID)
                    return;

                if (Core.Locations.ActiveClientCount(user) == 0)
                    return;

                menus.Add(new MenuItemInfo("Send File", Res.ShareRes.sendfile, new EventHandler(QuickMenu_View)));
            }
        }

        internal void Menu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            SharingView view = new SharingView(Core);

            Core.InvokeView(node.IsExternal(), view);
        }


        internal void QuickMenu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            new SendFileForm(Core, node.GetUser(), 0).ShowDialog();
        }

        public void SimTest()
        {
            
        }

        public void SimCleanup()
        {
            
        }

        internal void ShareFile(string path)
        {
            SendFile(path, 0, 0);
        }

        internal void SendFile(string path, ulong user, ushort client)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => SendFile(path, user, client));
                return;
            }

            // add to share list
            OpShare share = new OpShare();
            share.Name = Path.GetFileName(path);
            share.SystemPath = path;
            
            share.Completed = true;
            share.Sources.Add(Core.Network.Local);
            share.FileStatus = "Processing...";

            AddTargets(share, user, client);

            // so user can see hash progress
            ShareList.SafeAdd(share);
            Core.RunInGuiThread(GuiUpdate, share);

            ProcessFileShare(share);
        }

        internal void AddTargets(OpShare share, ulong user, ushort client)
        {
            if (user == 0)
                return;

            if (client == 0)
                share.ToRequest.AddRange(Core.Locations.GetClients(user).Select(c => new DhtClient(c)));
            else
                share.ToRequest.Add(new DhtClient(user, client));
        }

        private void ProcessFileShare(OpShare share)
        {
            // enqueue file for processing
            lock (ProcessingQueue)
                ProcessingQueue.Enqueue(share);

            // hashing
            if (ProcessFilesHandle == null || !ProcessFilesHandle.IsAlive)
            {
                ProcessFilesHandle = new Thread(ProcessFiles);
                ProcessFilesHandle.Start();
            }  
        }

        void ProcessFiles()
        {
            OpShare share = null;

            // while files on processing list
            while (ProcessingQueue.Count > 0 && !KillThreads)
            {
                lock (ProcessingQueue)
                    share = ProcessingQueue.Dequeue();

                try
                {
                    // copied from storage service

                    // hash file fast - used to gen key/iv
                   share.FileStatus = "Identifying...";
                    byte[] internalHash = null;
                    long internalSize = 0;
                    Utilities.Md5HashFile(share.SystemPath, ref internalHash, ref internalSize);

                    // dont bother find dupe, becaues dupe might be incomplete, in which case we want to add 
                    // completed file to our shared, have timer find dupes, and if both have the same file path that exists, remove one

                    // file key is opID and internal hash xor'd so that files won't be duplicated on the network
                    share.FileKey = new byte[32];
                    Core.User.Settings.OpKey.CopyTo(share.FileKey, 0);
                    for (int i = 0; i < internalHash.Length; i++)
                        share.FileKey[i] ^= internalHash[i];

                    // iv needs to be the same for ident files to gen same file hash
                    byte[] iv = new MD5CryptoServiceProvider().ComputeHash(share.FileKey);

                    // encrypt file to temp dir
                    share.FileStatus = "Securing...";
                    string tempPath = Core.GetTempPath();
                    using (IVCryptoStream stream = IVCryptoStream.Save(tempPath, share.FileKey, iv))
                    {
                        using (FileStream localfile = File.OpenRead(share.SystemPath))
                        {
                            int read = ProcessBufferSize;
                            while (read == ProcessBufferSize)
                            {
                                read = localfile.Read(ProcessBuffer, 0, ProcessBufferSize);
                                stream.Write(ProcessBuffer, 0, read);
                            }
                        }

                        stream.FlushFinalBlock();
                    }

                    // hash temp file
                    share.FileStatus = "Tagging...";
                    Utilities.HashTagFile(tempPath, Network.Protocol, ref share.Hash, ref share.Size);
                    share.FileID = OpTransfer.GetFileID(ServiceID, share.Hash, share.Size);

                    // move to official path
                    string path = GetFilePath(share);
                    if (!File.Exists(path))
                        File.Move(tempPath, path);

                    share.FileStatus = "Secured";

                    // run in core thread -> save, send request to user
                    Core.RunInCoreAsync(() =>
                    {
                        SaveHeaders();

                        foreach(DhtClient target in share.ToRequest.ToArray()) // to array because collection will be modified when sending request
                            TrySendRequest(share, target);
                    });
                }
                catch (Exception ex)
                {
                    Network.UpdateLog("Sharing", "Error: " + ex.Message);
                }
            }

            ProcessFilesHandle = null;
        }

        private void SaveHeaders()
        {
            try
            {
                string tempPath = Core.GetTempPath();
                using (IVCryptoStream crypto = IVCryptoStream.Save(tempPath, Core.User.Settings.FileKey))
                {
                    PacketStream stream = new PacketStream(crypto, Network.Protocol, FileAccess.Write);

                    ShareList.LockReading(delegate()
                    {
                        foreach (OpShare share in ShareList)
                            if (!share.Ignore && share.Hash != null)
                            {
                                share.SaveSystemPath = true;
                                stream.WritePacket(share);
                            }
                    });

                    crypto.FlushFinalBlock();
                }

                string finalPath = SharePath + Utilities.CryptFilename(Core, "ShareHeaders");
                File.Copy(tempPath, finalPath, true);
                File.Delete(tempPath);
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Share", "Error saving headers: " + ex.Message);
            }
        }

        private void LoadHeaders()
        {
            //crit - check for unused files and delete

            try
            {
                string path = SharePath + Utilities.CryptFilename(Core, "ShareHeaders");

                if (!File.Exists(path))
                    return;

                using (IVCryptoStream crypto = IVCryptoStream.Load(path, Core.User.Settings.FileKey))
                {
                    PacketStream stream = new PacketStream(crypto, Network.Protocol, FileAccess.Read);

                    G2Header root = null;

                    
                    while (stream.ReadPacket(ref root))
                        if (root.Name == SharingPacket.File)
                        {
                            OpShare share = OpShare.Decode(root);

                            if (share.SystemPath != null && !File.Exists(GetFilePath(share)))
                                share.SystemPath = null;

                            share.FileID = OpTransfer.GetFileID(ServiceID, share.Hash, share.Size);

                            if (File.Exists(GetFilePath(share)))
                            {
                                share.Completed = true;
                                share.Sources.Add(Core.Network.Local);
                            }

                            // incomplete, ensure partial file is saved into next run if need be
                            else
                            {
                                foreach (OpTransfer partial in Core.Transfers.Partials.Where(p => p.FileID == share.FileID))
                                    partial.SavePartial = true;
                            }

                            share.FileStatus = share.Completed ? "Secured" : "Incomplete";

                            ShareList.SafeAdd(share);

                            // unhashed files aren't saved anymore
                            /* if app previous closed without hashing share, hash now
                            if (share.Hash == null && share.SystemPath != null &&
                                File.Exists(share.SystemPath))
                                ProcessFileShare(share);*/
                        }
                }
            }
            catch (Exception ex)
            {
                Network.UpdateLog("VersionedFile", "Error loading data " + ex.Message);
            }
        }


        private string GetFilePath(OpShare share)
        {
            return SharePath + Utilities.CryptFilename(Core, Core.UserID, share.Hash);
        }

        internal void TrySendRequest(OpShare share, DhtClient target)
        {
            RudpSession session = Network.RudpControl.GetActiveSession(target);

            if (session == null)
            {
                Network.RudpControl.Connect(target);
                share.TransferStatus = "Connecting to " + Core.GetName(target.UserID);
            }
            else
                SendRequest(session, share);
        }

        void Session_Update(RudpSession session)
        {
            DhtClient client = new DhtClient(session.UserID, session.ClientID);

            if (session.Status == SessionStatus.Active)
                ShareList.LockReading(() =>
                {
                    foreach (OpShare share in ShareList.Where(s => s.ToRequest.Any(c => c.UserID == session.UserID && c.ClientID == session.ClientID)))
                        SendRequest(session, share);
                });
        }

        private void SendRequest(RudpSession session, OpShare share)
        {
            foreach (DhtClient taraget in share.ToRequest.Where(t => t.UserID == session.UserID && t.ClientID == session.ClientID).ToArray())
                share.ToRequest.Remove(taraget);

            share.SaveSystemPath = false;
            session.SendData(ServiceID, DataTypeShare, share, true);

            share.TransferStatus = "Request sent to " + Core.GetName(session.UserID);
        }

        void Session_Data(RudpSession session, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (G2Protocol.ReadPacket(root))
            {
                switch (root.Name)
                {
                    case SharingPacket.File:
                        ReceiveRequest(session, OpShare.Decode(root));
                        break;
                }
            }
        }

        private void ReceiveRequest(RudpSession session, OpShare share)
        {        
            DhtClient client = new DhtClient(session.UserID, session.ClientID);


            // check if file hash already on sharelist, if it is ignore
            bool alertUser = true;

            ShareList.LockReading(() =>
            {
                OpShare existing = ShareList.Where(s => Utilities.MemCompare(s.Hash, share.Hash)).FirstOrDefault();

                // transfer exists, but this is from another source, or we started up and someone is trying
                // to resend file to us, which this auto adds the new source
                if (existing != null)
                {
                    if (!existing.Ignore)
                        StartTransfer(client, existing);

                    alertUser = false;
                }
            });

            if (!alertUser)
                return;

            share.Ignore = true; // turned off once accepted, allowing this item to be saved to header
            share.FileID = OpTransfer.GetFileID(ServiceID, share.Hash, share.Size);
            
            ShareList.SafeAdd(share);
            Core.RunInGuiThread(GuiUpdate, share);

             share.TransferStatus =  "Request received from " + Core.GetName(session.UserID);
           
            Core.RunInGuiThread((System.Windows.Forms.MethodInvoker) delegate()
            {
                new AcceptFileForm(Core, client, share).ShowDialog();
            });
        }

        private OpTransfer StartTransfer(DhtClient client, OpShare share)
        {
            FileDetails details = new FileDetails(ServiceID, DataTypeShare, share.Hash, share.Size, null);
            object[] args = new object[] { share };

            OpTransfer transfer = Core.Transfers.StartDownload(client.UserID, details, args, new EndDownloadHandler(DownloadFinished));

            transfer.AddPeer(client);

            share.TransferStatus =  "Starting download from " + Core.GetName(client.UserID);

            return transfer;
        }

        internal void AcceptRequest(DhtClient client, OpShare share)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => AcceptRequest(client, share));
                return;
            }

            share.Ignore = false;

            StartTransfer(client, share);

            ReSearchShare(share);

            SaveHeaders();
        }

        internal void DownloadFinished(string path, object[] args)
        {
            OpShare share = args[0] as OpShare;

            File.Copy(path, GetFilePath(share));

            share.Completed = true;
            share.Sources.Add(Core.Network.Local);

            share.FileStatus = "Download Finished";
            
            Core.RunInGuiThread(GuiUpdate, share);

            Core.MakeNews(share.Name + " Finished Downloading", Core.UserID, 0, false, Res.ShareRes.Icon, Menu_View);
        }


        bool Transfers_FileSearch(ulong key, FileDetails details)
        {
            bool found = false;

            ShareList.LockReading(() =>
            {
                if (ShareList.Any(s => s.Size == details.Size && Utilities.MemCompare(s.Hash, details.Hash)))
                    found = true;
            });

            return found;
        }

        string Transfers_FileRequest(ulong key, FileDetails details)
        {
            OpShare share = null;

            ShareList.LockReading(() =>
            {
                share = ShareList.Where(s => s.Size == details.Size && Utilities.MemCompare(s.Hash, details.Hash)).FirstOrDefault();
            });


            if (share != null && share.Completed)
                return GetFilePath(share);


            return null;
        }


        void Core_SecondTimer()
        {
            // interface has its own timer that updates automatically
            // done because transfers isnt multi-threaded

            ShareList.LockReading(() =>
            {
                foreach (OpShare share in ShareList)
                {
                    if(share.Hash == null) // still processing
                        continue;

                    share.TransferStatus = "";

                    if (Core.Transfers.Pending.Any(t => t.FileID == share.FileID))
                        share.TransferStatus = "Download Pending";

                    bool active = false;

                    OpTransfer transfer;
                    if (Core.Transfers.Transfers.TryGetValue(share.FileID, out transfer))
                    {
                        if (transfer.Searching)
                            share.TransferStatus = "Searching..."; // allow to be re-assigned

                        if (transfer.Status != TransferStatus.Complete)
                        {
                            long progress = transfer.GetProgress() * 100 / transfer.Details.Size;

                            share.TransferStatus = "Downloading " + progress + "%...";

                            if (progress > 0)
                                active = true;
                        }

                        else if (Core.Transfers.UploadPeers.Values.Where(u => u.Active != null && u.Active.Transfer.FileID == share.FileID).Count() > 0)
                        {
                            share.TransferStatus = "Uploading...";
                            active = true;
                        }
                    }

                    share.TransferActive = active;

                    if (active && Core.TimeNow > share.NextPublish)
                    {
                        Core.Network.Store.PublishNetwork(share.FileID, ServiceID, DataTypeLocation, Core.Locations.LocalClient.Data.EncodeLight(Network.Protocol));
                        share.NextPublish = Core.TimeNow.AddHours(1);
                    }

                }
            });
        }

        void Core_MinuteTimer()
        {
            foreach (CachedLocation loc in CachedLocations.Where(l => Core.TimeNow > l.Expires).ToArray())
                CachedLocations.Remove(loc);
        }


        internal void OpenFile(OpShare share)
        {
            if (!File.Exists(GetFilePath(share)))
                return;

            // check if already exists, if it does open
            if (share.SystemPath != null && File.Exists(share.SystemPath))
            {
                Process.Start(share.SystemPath);
            }

            lock (OpenQueue)
            {
                if (OpenQueue.Contains(share))
                    return;

                OpenQueue.Enqueue(share);
            }

            // hashing
            if (OpenFilesHandle == null || !OpenFilesHandle.IsAlive)
            {
                OpenFilesHandle = new Thread(OpenFiles);
                OpenFilesHandle.Start();
            }  
        }

        void OpenFiles()
        {
            OpShare share = null;

            // while files on open list
            while (OpenQueue.Count > 0 && !KillThreads)
            {
                lock (OpenQueue)
                    share = OpenQueue.Dequeue();

                try
                {
                    if(!Directory.Exists(DownloadPath))
                        Directory.CreateDirectory(DownloadPath);

                    string finalpath = DownloadPath + share.Name;

                    int i = 1;
                    while(File.Exists(finalpath))
                        finalpath = DownloadPath + "(" + (i++) + ") " + share.Name;


                    // decrypt file to temp dir
                    share.FileStatus = "Unsecuring...";

                    string tempPath = Core.GetTempPath();

                    using (FileStream tempFile = new FileStream(tempPath, FileMode.CreateNew))
                    using (TaggedStream encFile = new TaggedStream(GetFilePath(share), Network.Protocol))
                    using (IVCryptoStream stream = IVCryptoStream.Load(encFile, share.FileKey))
                    {
                        int read = OpenBufferSize;
                        while (read == OpenBufferSize)
                        {
                            read = stream.Read(OpenBuffer, 0, OpenBufferSize);
                            tempFile.Write(OpenBuffer, 0, read);
                        }
                    }

                    // move to official path
                    File.Move(tempPath, finalpath);

                    share.SystemPath = finalpath;

                    share.FileStatus = "File in Downloads Folder";

                    Process.Start(finalpath);

                    Core.RunInCoreAsync(() => SaveHeaders());
                }
                catch (Exception ex)
                {
                    Network.UpdateLog("Sharing", "Error: " + ex.Message);
                }
            }

            OpenFilesHandle = null;
        }

        internal string GetFileLink(OpShare share)
        {
            // riseop://op/file/filename/opid~size~hash~key/targetlist
            string link = "riseop://" + HttpUtility.UrlEncode(Core.User.Settings.Operation) + 
                            "/file/" + HttpUtility.UrlEncode(share.Name) + "/";

            byte[] endtag = Core.User.Settings.InviteKey; // 8
            endtag = Utilities.CombineArrays(endtag, BitConverter.GetBytes(share.Size)); // 8
            endtag = Utilities.CombineArrays(endtag, share.Hash); // 20
            endtag = Utilities.CombineArrays(endtag, share.FileKey); // 32

            link += Utilities.ToBase64String(endtag) + "/";

            byte[] sources = null;

            foreach (DhtClient client in share.Sources)
                sources = (sources == null) ? client.ToBytes() : Utilities.CombineArrays(sources, client.ToBytes());

            link += Utilities.ToBase64String(sources);

            return link;
            
        }

        internal void DownloadLink(string link)
        {
            if(!link.StartsWith("riseop://"))
                throw new Exception("Invalid Link");

            string[] parts = link.Substring(9).Split('/');

            if (parts.Length < 4)
                return;

            if (parts[1] != "file")
                throw new Exception("Invalid Link");

            OpShare share = new OpShare();

            share.Name = HttpUtility.UrlDecode(parts[2]);

            byte[] endtag = Utilities.FromBase64String(parts[3]);

            if(endtag.Length < (8 + 8 + 20 + 32))
                throw new Exception("Invalid Link");

            byte[] inviteKey = Utilities.ExtractBytes(endtag, 0, 8);

            if(!Utilities.MemCompare(inviteKey, Core.User.Settings.InviteKey))
                throw new Exception("File Link is not for this Op");

            share.Size = BitConverter.ToInt64(Utilities.ExtractBytes(endtag, 8, 8), 0);
            share.Hash = Utilities.ExtractBytes(endtag, 8 + 8, 20);
            share.FileKey = Utilities.ExtractBytes(endtag, 8 + 8 + 20, 32);
            share.FileID = OpTransfer.GetFileID(ServiceID, share.Hash, share.Size);

            OpShare existing = null;

            ShareList.LockReading(() =>
                existing = ShareList.Where(s => Utilities.MemCompare(s.Hash, share.Hash)).FirstOrDefault());

            // just add new targets if we already have this file
            if (existing != null)
                share = existing;
            else
                ShareList.SafeAdd(share);

            if (parts.Length >= 5)
            {
                byte[] sources = Utilities.FromBase64String(parts[4]);

                for (int i = 0; i < sources.Length; i += 10)
                    share.Sources.Add(DhtClient.FromBytes(sources, i));
            }

            if (share.Sources.Count > 0)
                Core.RunInCoreAsync(() => StartTransfer(share.Sources[0], share));

            share.FileStatus = "Incomplete";

            Core.RunInGuiThread(GuiUpdate, share);
        }

        internal void RemoveShare(OpShare share)
        {
            try
            {
                File.Delete(GetFilePath(share));
            }
            catch { }

            ShareList.SafeRemove(share);

            Core.RunInGuiThread(GuiUpdate, share);
        }

        internal void ReSearchShare(OpShare share)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => ReSearchShare(share));
                return;
            }

            DhtSearch search = Core.Network.Searches.Start(share.FileID, "Share Search: " + share.Name, ServiceID, DataTypeLocation, null, new EndSearchHandler(EndLocationSearch));
            search.Carry = share;
        }

        void EndLocationSearch(DhtSearch search)
        {
            if (search.FoundValues.Count == 0)
                return;

            OpShare share = search.Carry as OpShare;

            if (share.Completed)
                return;

            OpTransfer transfer = null;

            // add locations to running transfer
            foreach (byte[] result in search.FoundValues.Select(v => v.Value))
            {
                LocationData loc = LocationData.Decode(result);
                DhtClient client = new DhtClient(loc.UserID, loc.Source.ClientID);

                if (transfer == null)
                    transfer = StartTransfer(client, share);

                Core.Network.LightComm.Update(loc);
                transfer.AddPeer(client);
            }
        }

        List<CachedLocation> CachedLocations = new List<CachedLocation>();

        void Store_Locations(DataReq store)
        {
            // location being published to hashid so others can get sources


            CachedLocation loc = CachedLocations.Where(l => l.FileID == store.Target && Utilities.MemCompare(store.Data, store.Data)).FirstOrDefault();
            
            if(loc != null)
            {
                loc.Expires = Core.TimeNow.AddHours(1);
                return;
            }

            loc = new CachedLocation() { FileID = store.Target, Data = store.Data, Expires = Core.TimeNow.AddHours(1)};
            CachedLocations.Add(loc);
        }


        void Search_Locations(ulong key, byte[] parameters, List<byte[]> results)
        {
            // return 3 random locations

            results.AddRange((from l in CachedLocations
                              where l.FileID == key
                              orderby Core.RndGen.Next()
                              select l.Data).Take(3));
        }
    }

    internal class CachedLocation
    {
        internal  ulong FileID;
        internal DateTime Expires;
        internal byte[] Data;
    }

    internal class OpShare : G2Packet
    {
        const byte Packet_Name = 0x10;
        const byte Packet_Hash = 0x20;
        const byte Packet_Size = 0x30;
        const byte Packet_FileKey = 0x40;
        const byte Packet_SystemPath = 0x50;


        internal string Name;
        internal byte[] Hash;
        internal long Size;
        internal byte[] FileKey;

        internal ulong FileID;
        internal string SystemPath;
        internal bool SaveSystemPath;
        internal List<DhtClient> ToRequest = new List<DhtClient>();
        internal List<DhtClient> Sources = new List<DhtClient>();
        internal bool Completed;
        internal bool Ignore;
        internal DateTime NextPublish;
        internal bool TransferActive;

        internal string FileStatus = "";
        internal string TransferStatus = "";

        public override string ToString()
        {
            return Name;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame root = protocol.WritePacket(null, SharingPacket.File, null);

                protocol.WritePacket(root, Packet_Name, UTF8Encoding.UTF8.GetBytes(Name));
                protocol.WritePacket(root, Packet_Hash, Hash);
                protocol.WritePacket(root, Packet_Size, CompactNum.GetBytes(Size));
                protocol.WritePacket(root, Packet_FileKey, FileKey);

                if(SaveSystemPath && SystemPath != null)
                    protocol.WritePacket(root, Packet_SystemPath, UTF8Encoding.UTF8.GetBytes(SystemPath));

                return protocol.WriteFinish();
            }
        }

        internal static OpShare Decode(G2Header header)
        {
            OpShare root = new OpShare();
            G2Header child = new G2Header(header.Data);

            while (G2Protocol.ReadNextChild(header, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Name:
                        root.Name = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Hash:
                        root.Hash = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Size:
                        root.Size = CompactNum.ToInt64(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_FileKey:
                        root.FileKey = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_SystemPath:
                        root.SystemPath = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return root;
        }
    }
}
