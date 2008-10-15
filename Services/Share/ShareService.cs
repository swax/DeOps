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


namespace RiseOp.Services.Share
{
    internal class SharePacket
    {
        internal const byte File = 0x10;
        internal const byte PublicRequest = 0x20;
        internal const byte Collection = 0x30;
    }

    internal delegate void ShareFileUpdateHandler(SharedFile share);
    internal delegate void ShareCollectionUpdateHandler(ulong user);


    class ShareService : OpService
    {
        public string Name { get { return "Sharing"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.Share; } }

        OpCore Core;
        DhtNetwork Network;

        internal ShareCollection Local;
        internal ThreadedDictionary<ulong, ShareCollection> Collections = new ThreadedDictionary<ulong, ShareCollection>();

        Thread ProcessFilesHandle;
        LinkedList<SharedFile> ProcessingQueue = new LinkedList<SharedFile>();
        const int ProcessBufferSize = 1024 * 16;
        byte[] ProcessBuffer = new byte[ProcessBufferSize];


        Thread OpenFilesHandle;
        LinkedList<SharedFile> OpenQueue = new LinkedList<SharedFile>();
        int OpenBufferSize = 4096;
        byte[] OpenBuffer = new byte[4096]; // needs to be 4k to packet stream break/resume work

        bool KillThreads;        

        string SharePath;
        string DownloadPath;
        string PublicPath;

        internal event ShareFileUpdateHandler GuiFileUpdate;
        internal event ShareCollectionUpdateHandler GuiCollectionUpdate;

        const uint DataTypeShare = 0x01;
        const uint DataTypeLocation = 0x02;
        const uint DataTypePublic = 0x03;
        const uint DataTypeSession = 0x04; // used for sending file reqs, public reqs, and lists over rudp
        


        internal ShareService(OpCore core)
        {
            Core = core;
            Network = core.Network;

            string rootPath = Core.User.RootPath + Path.DirectorySeparatorChar +
                        "Data" + Path.DirectorySeparatorChar +
                        ServiceID.ToString() + Path.DirectorySeparatorChar;

            SharePath = rootPath + DataTypeShare.ToString() + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(SharePath);

            PublicPath = rootPath + DataTypePublic.ToString() + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(PublicPath);

            DownloadPath = Core.User.RootPath + Path.DirectorySeparatorChar + "Downloads" + Path.DirectorySeparatorChar;


            Core.SecondTimerEvent += new TimerHandler(Core_SecondTimer);
            Core.MinuteTimerEvent += new TimerHandler(Core_MinuteTimer);

            // data
            Network.RudpControl.SessionUpdate += new SessionUpdateHandler(Session_Update);
            Network.RudpControl.SessionData[ServiceID, DataTypeSession] += new SessionDataHandler(Session_Data);

            Core.Transfers.FileSearch[ServiceID, DataTypeShare] += new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[ServiceID, DataTypeShare] += new FileRequestHandler(Transfers_FileRequest);

            Core.Transfers.FileSearch[ServiceID, DataTypePublic] += new FileSearchHandler(Transfers_PublicSearch);
            Core.Transfers.FileRequest[ServiceID, DataTypePublic] += new FileRequestHandler(Transfers_PublicRequest);

            // location
            Network.Store.StoreEvent[ServiceID, DataTypeLocation] += new StoreHandler(Store_Locations);
            Network.Searches.SearchEvent[ServiceID, DataTypeLocation] += new SearchRequestHandler(Search_Locations);

            Local = new ShareCollection(Core.UserID);
            Collections.SafeAdd(Core.UserID, Local);

            LoadHeaders();
        }

        public void Dispose()
        {
            //crit - public shared directory can be deleted here, except for our local public header

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
            if (Core.Locations.ActiveClientCount(user) == 0)
                return;

            if (menuType == InterfaceMenuType.Internal)
                menus.Add(new MenuItemInfo("Data/Share", Res.ShareRes.Icon, new EventHandler(Menu_View)));

            if (menuType == InterfaceMenuType.External)
                menus.Add(new MenuItemInfo("Share", Res.ShareRes.Icon, new EventHandler(Menu_View)));

            if (menuType == InterfaceMenuType.Quick)
            {
                if (user == Core.UserID)
                    return;

                menus.Add(new MenuItemInfo("Send File", Res.ShareRes.sendfile, new EventHandler(QuickMenu_View)));
            }
        }

        internal void Menu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            SharingView view = new SharingView(Core, node.GetUser());

            Core.InvokeView(node.IsExternal(), view);

            GetPublicList(node.GetUser());
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

       internal void SaveHeaders()
        {
            // save public shared
            try
            {
                Local.Key = Utilities.GenerateKey(Core.StrongRndGen, 256);

                string tempPath = Core.GetTempPath();
                using (IVCryptoStream crypto = IVCryptoStream.Save(tempPath, Local.Key))
                {
                    PacketStream stream = new PacketStream(crypto, Network.Protocol, FileAccess.Write);

                    Local.Files.LockReading(delegate()
                    {
                        foreach (SharedFile file in Local.Files)
                            if (!file.Ignore && 
                                file.Hash != null && 
                                file.ClientID == Core.Network.Local.ClientID &&
                                file.Public)
                            {
                                file.SaveLocal = false;
                                stream.WritePacket(file);
                            }
                    });

                    crypto.FlushFinalBlock();
                }

                Utilities.HashTagFile(tempPath, Core.Network.Protocol, ref Local.Hash, ref Local.Size);

                string finalPath = GetPublicPath(Local);
                File.Copy(tempPath, finalPath, true);
                File.Delete(tempPath);
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Share", "Error saving public: " + ex.Message);
            }


            // save private/public shared - this is also whats loaded on startup
            try
            {
                string tempPath = Core.GetTempPath();
                using (IVCryptoStream crypto = IVCryptoStream.Save(tempPath, Core.User.Settings.FileKey))
                {
                    PacketStream stream = new PacketStream(crypto, Network.Protocol, FileAccess.Write);

                    // save all files
                    Local.Files.LockReading(delegate()
                    {
                        foreach (SharedFile file in Local.Files)
                            if (!file.Ignore &&
                                file.Hash != null &&
                                file.ClientID == Core.Network.Local.ClientID)
                            {
                                file.SaveLocal = true;
                                stream.WritePacket(file);
                            }
                    });

                    // save our public header
                    if(Local.Hash != null)
                        stream.WritePacket(Local);

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
                        if (root.Name == SharePacket.Collection)
                        {
                            ShareCollection copy = ShareCollection.Decode(root, Core.UserID);
                            //crit - ensure public path exists before loading up
                            Local.Hash = copy.Hash;
                            Local.Size = copy.Size;
                            Local.Key = copy.Key;
                        }
                        else if (root.Name == SharePacket.File)
                        {
                            SharedFile file = SharedFile.Decode(root, Core.Network.Local.ClientID);

                            if (file.SystemPath != null && !File.Exists(GetFilePath(file)))
                                file.SystemPath = null;

                            file.FileID = OpTransfer.GetFileID(ServiceID, file.Hash, file.Size);

                            if (File.Exists(GetFilePath(file)))
                            {
                                file.Completed = true;
                                file.Sources.Add(Core.Network.Local);
                            }

                            // incomplete, ensure partial file is saved into next run if need be
                            else
                            {
                                foreach (OpTransfer partial in Core.Transfers.Partials.Where(p => p.FileID == file.FileID))
                                    partial.SavePartial = true;
                            }

                            file.FileStatus = file.Completed ? "Secured" : "Incomplete";

                            Local.Files.SafeAdd(file);

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

        void Core_SecondTimer()
        {
            // interface has its own timer that updates automatically
            // done because transfers isnt multi-threaded

            Local.Files.LockReading(() =>
            {
                foreach (SharedFile file in Local.Files)
                {
                    if (file.Hash == null) // still processing
                        continue;

                    file.TransferStatus = "";

                    if (Core.Transfers.Pending.Any(t => t.FileID == file.FileID))
                        file.TransferStatus = "Download Pending";

                    bool active = false;

                    OpTransfer transfer;
                    if (Core.Transfers.Transfers.TryGetValue(file.FileID, out transfer))
                    {
                        if (transfer.Searching)
                            file.TransferStatus = "Searching..."; // allow to be re-assigned

                        if (transfer.Status != TransferStatus.Complete)
                        {
                            long progress = transfer.GetProgress() * 100 / transfer.Details.Size;

                            file.TransferStatus = "Downloading " + progress + "% at " + Utilities.ByteSizetoString((long)transfer.Bandwidth.InAvg()) + "/s";

                            if (progress > 0)
                                active = true;
                        }

                        else if (Core.Transfers.UploadPeers.Values.Where(u => u.Active != null && u.Active.Transfer.FileID == file.FileID).Count() > 0)
                        {
                            file.TransferStatus = "Uploading at " + Utilities.ByteSizetoString((long)transfer.Bandwidth.OutAvg()) + "/s";
                            active = true;
                        }
                    }

                    file.TransferActive = active;

                    if (active && Core.TimeNow > file.NextPublish)
                    {
                        Core.Network.Store.PublishNetwork(file.FileID, ServiceID, DataTypeLocation, Core.Locations.LocalClient.Data.EncodeLight(Network.Protocol));
                        file.NextPublish = Core.TimeNow.AddHours(1);
                    }

                }
            });
        }

        void Core_MinuteTimer()
        {
            foreach (CachedLocation loc in CachedLocations.Where(l => Core.TimeNow > l.Expires).ToArray())
                CachedLocations.Remove(loc);
        }
    
        internal string GetFilePath(SharedFile file)
        {
            if (file.Hash == null)
                return "";

            return SharePath + Utilities.CryptFilename(Core, Core.UserID, file.Hash);
        }

        private string GetPublicPath(ShareCollection collection)
        {
            return PublicPath + Utilities.CryptFilename(Core, collection.UserID, collection.Hash);
        }




        internal void LoadFile(string path)
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
            SharedFile file = new SharedFile(Core.Network.Local.ClientID);
            file.Name = Path.GetFileName(path);
            file.SystemPath = path;
            
            file.Completed = true;
            file.Sources.Add(Core.Network.Local);
            file.FileStatus = "Processing...";

            AddTargets(file.ToRequest, user, client);

            // so user can see hash progress
            Local.Files.SafeAdd(file);
            Core.RunInGuiThread(GuiFileUpdate, file);

            ProcessFileShare(file);
        }

        internal void AddTargets(List<DhtClient> targets, ulong user, ushort client)
        {
            if (user == Core.UserID && client == Core.Network.Local.ClientID)
                return;

            // check for dupes
            if (targets.Any(t => t.UserID == user && t.ClientID == client))
                return;

            if (user == 0)
                return;

            // client 0 means all known clients
            if (client == 0)
                targets.AddRange( from loc in Core.Locations.GetClients(user)
                                  where loc.UserID != Core.UserID || loc.ClientID != Core.Network.Local.ClientID // or is right, not and
                                  select new DhtClient(loc));
            else
                targets.Add(new DhtClient(user, client));
        }

        private void ProcessFileShare(SharedFile file)
        {
            // enqueue file for processing
            lock (ProcessingQueue)
                ProcessingQueue.AddLast(file);

            // hashing
            if (ProcessFilesHandle == null || !ProcessFilesHandle.IsAlive)
            {
                ProcessFilesHandle = new Thread(ProcessFiles);
                ProcessFilesHandle.Start();
            }  
        }

        void ProcessFiles()
        {
            SharedFile file = null;

            // while files on processing list
            while (ProcessingQueue.Count > 0 && !KillThreads)
            {
                lock (ProcessingQueue)
                {
                    file = ProcessingQueue.First.Value;
                    ProcessingQueue.RemoveFirst();
                }

                try
                {
                    // copied from storage service

                    // hash file fast - used to gen key/iv
                   file.FileStatus = "Identifying...";
                    byte[] internalHash = null;
                    long internalSize = 0;
                    Utilities.Md5HashFile(file.SystemPath, ref internalHash, ref internalSize);

                    // dont bother find dupe, becaues dupe might be incomplete, in which case we want to add 
                    // completed file to our shared, have timer find dupes, and if both have the same file path that exists, remove one

                    // file key is opID and internal hash xor'd so that files won't be duplicated on the network
                    file.FileKey = new byte[32];
                    Core.User.Settings.OpKey.CopyTo(file.FileKey, 0);
                    for (int i = 0; i < internalHash.Length; i++)
                        file.FileKey[i] ^= internalHash[i];

                    // iv needs to be the same for ident files to gen same file hash
                    byte[] iv = new MD5CryptoServiceProvider().ComputeHash(file.FileKey);

                    // encrypt file to temp dir
                    file.FileStatus = "Securing...";
                    string tempPath = Core.GetTempPath();
                    using (IVCryptoStream stream = IVCryptoStream.Save(tempPath, file.FileKey, iv))
                    {
                        using (FileStream localfile = File.OpenRead(file.SystemPath))
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
                    file.FileStatus = "Tagging...";
                    Utilities.HashTagFile(tempPath, Network.Protocol, ref file.Hash, ref file.Size);
                    file.FileID = OpTransfer.GetFileID(ServiceID, file.Hash, file.Size);

                    // move to official path
                    string path = GetFilePath(file);
                    if (!File.Exists(path))
                        File.Move(tempPath, path);

                    file.FileStatus = "Secured";

                    // run in core thread -> save, send request to user
                    Core.RunInCoreAsync(() =>
                    {
                        SaveHeaders();

                        foreach(DhtClient target in file.ToRequest.ToArray()) // to array because collection will be modified when sending request
                            TrySendRequest(file, target);
                    });
                }
                catch (Exception ex)
                {
                    Network.UpdateLog("Sharing", "Error: " + ex.Message);
                }
            }

            ProcessFilesHandle = null;
        }



        void Session_Update(RudpSession session)
        {
            DhtClient client = new DhtClient(session.UserID, session.ClientID);

            if (session.Status == SessionStatus.Active)
            {
                Local.Files.LockReading(() =>
                {
                    foreach (SharedFile file in Local.Files.Where(s => s.ToRequest.Any(c => c.UserID == session.UserID && c.ClientID == session.ClientID)))
                        SendFileRequest(session, file);
                });

                ShareCollection collection;
                if (Collections.SafeTryGetValue(session.UserID, out collection))
                    if (collection.ToRequest.Any(t => t.ClientID == session.ClientID))
                        SendPublicRequest(session, collection);
            }
        }
       
        void Session_Data(RudpSession session, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (G2Protocol.ReadPacket(root))
            {
                switch (root.Name)
                {
                    case SharePacket.File:
                        ReceiveFileRequest(session, SharedFile.Decode(root, Core.Network.Local.ClientID));
                        break;

                    case SharePacket.PublicRequest:
                        ReceivePublicRequest(session);
                        break;

                    case SharePacket.Collection:
                        ReceivePublicDetails(session, ShareCollection.Decode(root, session.UserID));
                        break;
                }
            }
        }



        internal void GetPublicList(ulong user)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => GetPublicList(user));
                return;
            }

            ShareCollection collection;
            if (!Collections.SafeTryGetValue(user, out collection))
            {
                collection = new ShareCollection(user);
                Collections.SafeAdd(user, collection);
            }

            AddTargets(collection.ToRequest, user, 0);

            foreach (DhtClient target in collection.ToRequest)
            {
                RudpSession session = Network.RudpControl.GetActiveSession(target);

                if (session == null)
                {
                    Network.RudpControl.Connect(target);
                    collection.Status = "Connecting to " + Core.GetName(target.UserID);
                }
                else
                    SendPublicRequest(session, collection);
            }
        }

        private void SendPublicRequest(RudpSession session, ShareCollection collection)
        {
            collection.Status = "Requesting List";

            session.SendData(ServiceID, DataTypeSession, new PublicShareRequest(), true);
        }

        private void ReceivePublicRequest(RudpSession session)
        {
            // if in global im, only allow if on buddies list
            if (Core.User.Settings.GlobalIM)
                if (!Core.Buddies.BuddyList.SafeContainsKey(session.UserID))
                    return;

            if (Core.Buddies.IgnoreList.SafeContainsKey(session.UserID))
                return;

            if(Local.Hash != null)
                session.SendData(ServiceID, DataTypeSession, Local, true);
        }

        private void ReceivePublicDetails(RudpSession session, ShareCollection file)
        {
            ShareCollection collection;
            if (!Collections.SafeTryGetValue(session.UserID, out collection))
                return;

            collection.Key = file.Key;
            collection.Size = file.Size;
            collection.Hash = file.Hash;

            foreach (DhtClient done in collection.ToRequest.Where(t => t.ClientID == session.ClientID).ToArray())
                collection.ToRequest.Remove(done);


            FileDetails details = new FileDetails(ServiceID, DataTypePublic, file.Hash, file.Size, null);
            object[] args = new object[] { collection, (object) session.ClientID };


            DhtClient client = new DhtClient(session.UserID, session.ClientID);
            OpTransfer transfer = Core.Transfers.StartDownload(client.UserID, details, args, new EndDownloadHandler(CollectionDownloadFinished));
            transfer.AddPeer(client);
            transfer.DoSearch = false;

            collection.Status = "Starting List Download";
        }

        bool Transfers_PublicSearch(ulong key, FileDetails details)
        {
            // shouldnt be called, transfer starts directly
            Debug.Assert(false);

            return false;
        }

        string Transfers_PublicRequest(ulong key, FileDetails details)
        {
            ShareCollection collection;
            if (!Collections.SafeTryGetValue(key, out collection))
                return null;

            if (collection.Size != details.Size || !Utilities.MemCompare(collection.Hash, details.Hash))
                return null;

            return GetPublicPath(collection);
        }

        internal void CollectionDownloadFinished(string path, object[] args)
        {
            ShareCollection collection = args[0] as ShareCollection;
            ushort client = (ushort) args[1];

            collection.Files.LockWriting(() =>
            {
                foreach (SharedFile file in collection.Files.Where(c => c.ClientID == client).ToArray())
                    collection.Files.Remove(file);
            });

            try
            {
                string finalpath = GetPublicPath(collection);

                File.Delete(finalpath);
                File.Copy(path, finalpath);

                using (TaggedStream tagged = new TaggedStream(finalpath, Network.Protocol))
                using (IVCryptoStream crypto = IVCryptoStream.Load(tagged, collection.Key))
                {
                    PacketStream stream = new PacketStream(crypto, Network.Protocol, FileAccess.Read);

                    G2Header root = null;

                    collection.Files.LockWriting(() =>
                    {
                        while (stream.ReadPacket(ref root))
                            if (root.Name == SharePacket.File)
                            {
                                SharedFile file = SharedFile.Decode(root, client);

                                // dont add dupes from diff client ids
                                if (collection.Files.Any(f => f.Size == file.Size && Utilities.MemCompare(f.Hash, file.Hash)))
                                    continue;

                                file.FileID = OpTransfer.GetFileID(ServiceID, file.Hash, file.Size);
                                collection.Files.SafeAdd(file);
                            }
                    });
                }
            }
            catch (Exception ex)
            {
                Core.Network.UpdateLog("Mail", "Error loading local mail " + ex.Message);
            }

            collection.Status = collection.Files.SafeCount + " Files Shared";

            Core.RunInGuiThread(GuiCollectionUpdate, collection.UserID);
         }




        internal void TrySendRequest(SharedFile file, DhtClient target)
        {
            RudpSession session = Network.RudpControl.GetActiveSession(target);

            if (session == null)
            {
                Network.RudpControl.Connect(target);
                file.TransferStatus = "Connecting to " + Core.GetName(target.UserID);
            }
            else
                SendFileRequest(session, file);
        }

        private void SendFileRequest(RudpSession session, SharedFile file)
        {
            foreach (DhtClient taraget in file.ToRequest.Where(c => c.UserID == session.UserID && c.ClientID == session.ClientID).ToArray())
                file.ToRequest.Remove(taraget);

            file.SaveLocal = false;
            session.SendData(ServiceID, DataTypeSession, file, true);

            file.TransferStatus = "Request sent to " + Core.GetName(session.UserID);
        }

        private void ReceiveFileRequest(RudpSession session, SharedFile file)
        {
            // if in global im, only allow if on buddies list
            if (Core.User.Settings.GlobalIM)
                if (!Core.Buddies.BuddyList.SafeContainsKey(session.UserID))
                    return;

            if (Core.Buddies.IgnoreList.SafeContainsKey(session.UserID))
                return;

            DhtClient client = new DhtClient(session.UserID, session.ClientID);


            // check if file hash already on sharelist, if it is ignore
            bool alertUser = true;

            Local.Files.LockReading(() =>
            {
                SharedFile existing = Local.Files.Where(s => Utilities.MemCompare(s.Hash, file.Hash)).FirstOrDefault();

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

            file.Ignore = true; // turned off once accepted, allowing this item to be saved to header
            file.FileID = OpTransfer.GetFileID(ServiceID, file.Hash, file.Size);
            
            Local.Files.SafeAdd(file);
            Core.RunInGuiThread(GuiFileUpdate, file);

            file.TransferStatus =  "Request received from " + Core.GetName(session.UserID);
           
            Core.RunInGuiThread((System.Windows.Forms.MethodInvoker) delegate()
            {
                new AcceptFileForm(Core, client, file).ShowDialog();
            });
        }

        private OpTransfer StartTransfer(DhtClient client, SharedFile file)
        {
            FileDetails details = new FileDetails(ServiceID, DataTypeShare, file.Hash, file.Size, null);
            object[] args = new object[] { file };

            OpTransfer transfer = Core.Transfers.StartDownload(client.UserID, details, args, new EndDownloadHandler(FileDownloadFinished));

            transfer.AddPeer(client);

            file.TransferStatus =  "Starting download from " + Core.GetName(client.UserID);

            return transfer;
        }

        internal void AcceptRequest(DhtClient client, SharedFile file)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => AcceptRequest(client, file));
                return;
            }

            file.Ignore = false;

            StartTransfer(client, file);

            ResearchFile(file);

            SaveHeaders();
        }

        bool Transfers_FileSearch(ulong key, FileDetails details)
        {
            bool found = false;

            Local.Files.LockReading(() =>
            {
                if (Local.Files.Any(f => f.Size == details.Size && Utilities.MemCompare(f.Hash, details.Hash)))
                    found = true;
            });

            return found;
        }

        string Transfers_FileRequest(ulong key, FileDetails details)
        {
            SharedFile share = null;

            Local.Files.LockReading(() =>
            {
                share = Local.Files.Where(f => f.Size == details.Size && Utilities.MemCompare(f.Hash, details.Hash)).FirstOrDefault();
            });


            if (share != null && share.Completed)
                return GetFilePath(share);


            return null;
        }

        internal void FileDownloadFinished(string path, object[] args)
        {
            SharedFile file = args[0] as SharedFile;

            File.Copy(path, GetFilePath(file));

            file.Completed = true;
            file.Sources.Add(Core.Network.Local);

            file.FileStatus = "Download Finished";
            
            Core.RunInGuiThread(GuiFileUpdate, file);

            Core.MakeNews(file.Name + " Finished Downloading", Core.UserID, 0, false, Res.ShareRes.Icon, Menu_View);
        }





        internal string GetFileLink(ulong user, SharedFile file)
        {
            // riseop://op/file/filename/opid~size~hash~key/targetlist
            string link = "riseop://" + HttpUtility.UrlEncode(Core.User.Settings.Operation) + 
                            "/file/" + HttpUtility.UrlEncode(file.Name) + "/";

            byte[] endtag = Core.User.Settings.InviteKey; // 8
            endtag = Utilities.CombineArrays(endtag, BitConverter.GetBytes(file.Size)); // 8
            endtag = Utilities.CombineArrays(endtag, file.Hash); // 20
            endtag = Utilities.CombineArrays(endtag, file.FileKey); // 32

            link += Utilities.ToBase64String(endtag) + "/";

            byte[] sources = null;


            // if local shared file, get the sources we know of
            if (user == Core.UserID && file.ClientID == Core.Network.Local.ClientID)
            {
                foreach (DhtClient client in file.Sources)
                    sources = (sources == null) ? client.ToBytes() : Utilities.CombineArrays(sources, client.ToBytes());
            }
            
            // else getting link from remote share, so add it's address as a location
            else
                sources = new DhtClient(user, file.ClientID).ToBytes();


            if(sources != null)
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

            SharedFile file = new SharedFile(Core.Network.Local.ClientID);

            file.Name = HttpUtility.UrlDecode(parts[2]);

            byte[] endtag = Utilities.FromBase64String(parts[3]);

            if(endtag.Length < (8 + 8 + 20 + 32))
                throw new Exception("Invalid Link");

            byte[] inviteKey = Utilities.ExtractBytes(endtag, 0, 8);

            if(!Utilities.MemCompare(inviteKey, Core.User.Settings.InviteKey))
                throw new Exception("File Link is not for this Op");

            file.Size = BitConverter.ToInt64(Utilities.ExtractBytes(endtag, 8, 8), 0);
            file.Hash = Utilities.ExtractBytes(endtag, 8 + 8, 20);
            file.FileKey = Utilities.ExtractBytes(endtag, 8 + 8 + 20, 32);
            file.FileID = OpTransfer.GetFileID(ServiceID, file.Hash, file.Size);

            if (parts.Length >= 5)
            {
                byte[] sources = Utilities.FromBase64String(parts[4]);

                for (int i = 0; i < sources.Length; i += 10)
                    file.Sources.Add(DhtClient.FromBytes(sources, i));
            }

            DownloadFile(file);
        }

        internal void DownloadFile(ulong user, SharedFile file)
        {
            // donwloading from a different user, make a copy of their share, and activate download
            // copies are checked for

            if (File.Exists(GetFilePath(file)))
                return;

            SharedFile localCopy = new SharedFile(file, Core.Network.Local.ClientID);
            localCopy.Sources.Add(new DhtClient(user, file.ClientID));

            DownloadFile(localCopy);
        }

        void DownloadFile(SharedFile file)
        {
            SharedFile existing = null;

            Local.Files.LockReading(() =>
                existing = Local.Files.Where(s => Utilities.MemCompare(s.Hash, file.Hash)).FirstOrDefault());

            // just add new targets if we already have this file
            if (existing != null)
            {
                existing.Sources.AddRange(file.Sources);
                file = existing;
            }
            else
                Local.Files.SafeAdd(file);

            // if downloading form another client of self
            if (file.ClientID != Core.Network.Local.ClientID)
            {
                file.ClientID = Core.Network.Local.ClientID; 
                Core.RunInGuiThread(GuiCollectionUpdate, Core.UserID);
            }


            Core.RunInCoreAsync(() =>
            {
                SaveHeaders();

                if (file.Sources.Count > 0)
                    StartTransfer(file.Sources[0], file);
            });

            ResearchFile(file);

            file.FileStatus = "Incomplete";

            Core.RunInGuiThread(GuiFileUpdate, file);
        }

        internal void OpenFile(ulong user, SharedFile file)
        {
            if (!File.Exists(GetFilePath(file)))
                return;

            // check if already exists, if it does open
            if (file.SystemPath != null && File.Exists(file.SystemPath))
            {
                Process.Start(file.SystemPath);
                return;
            }

            lock (OpenQueue)
            {
                if (OpenQueue.Contains(file))
                    return;

                OpenQueue.AddLast(file);
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
            SharedFile file = null;

            // while files on open list
            while (OpenQueue.Count > 0 && !KillThreads)
            {
                lock (OpenQueue)
                {
                    file = OpenQueue.First.Value;
                    OpenQueue.RemoveFirst();
                }

                try
                {
                    if(!Directory.Exists(DownloadPath))
                        Directory.CreateDirectory(DownloadPath);

                    string finalpath = DownloadPath + file.Name;

                    int i = 1;
                    while(File.Exists(finalpath))
                        finalpath = DownloadPath + "(" + (i++) + ") " + file.Name;


                    // decrypt file to temp dir
                    file.FileStatus = "Unsecuring...";

                    string tempPath = Core.GetTempPath();

                    using (FileStream tempFile = new FileStream(tempPath, FileMode.CreateNew))
                    using (TaggedStream encFile = new TaggedStream(GetFilePath(file), Network.Protocol))
                    using (IVCryptoStream stream = IVCryptoStream.Load(encFile, file.FileKey))
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

                    file.SystemPath = finalpath;

                    file.FileStatus = "File in Downloads Folder";

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

        internal void RemoveFile(SharedFile file)
        {
            if (file.Hash != null)
            { 
                // stop any current transfer of file
                Core.Transfers.CancelDownload(ServiceID, file.Hash, file.Size);

                try
                {
                    File.Delete(GetFilePath(file));
                }
                catch { }
            }

            Local.Files.SafeRemove(file);

            lock (ProcessingQueue)
                if (ProcessingQueue.Contains(file))
                    ProcessingQueue.Remove(file);

            lock(OpenQueue)
                if (OpenQueue.Contains(file))
                    OpenQueue.Remove(file);

            Core.RunInGuiThread(GuiFileUpdate, file);
        }

        internal void ResearchFile(SharedFile file)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => ResearchFile(file));
                return;
            }

            DhtSearch search = Core.Network.Searches.Start(file.FileID, "Share Search: " + file.Name, ServiceID, DataTypeLocation, null, new EndSearchHandler(EndLocationSearch));
            search.Carry = file;
        }



        void EndLocationSearch(DhtSearch search)
        {
            if (search.FoundValues.Count == 0)
                return;

            SharedFile file = search.Carry as SharedFile;

            if (file.Completed)
                return;

            OpTransfer transfer = null;

            // add locations to running transfer
            foreach (byte[] result in search.FoundValues.Select(v => v.Value))
            {
                LocationData loc = LocationData.Decode(result);
                DhtClient client = new DhtClient(loc.UserID, loc.Source.ClientID);

                if (transfer == null)
                    transfer = StartTransfer(client, file);

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

    internal class ShareCollection : G2Packet
    {  
        const byte Packet_Hash = 0x10;
        const byte Packet_Size = 0x20;
        const byte Packet_Key = 0x30;

        // public file info
        internal byte[] Hash;
        internal long Size;
        internal byte[] Key;


        internal ulong UserID;
        internal string Status;
        internal List<DhtClient> ToRequest = new List<DhtClient>();
        internal ThreadedList<SharedFile> Files = new ThreadedList<SharedFile>();


        internal ShareCollection(ulong user)
        {
            UserID = user;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame root = protocol.WritePacket(null, SharePacket.Collection, null);

                protocol.WritePacket(root, Packet_Hash, Hash);
                protocol.WritePacket(root, Packet_Size, CompactNum.GetBytes(Size));
                protocol.WritePacket(root, Packet_Key,  Key);

                return protocol.WriteFinish();
            }
        }

        internal static ShareCollection Decode(G2Header header, ulong user)
        {
            ShareCollection root = new ShareCollection(user);
            G2Header child = new G2Header(header.Data);

            while (G2Protocol.ReadNextChild(header, child) == G2ReadResult.PACKET_GOOD)
            {

                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Hash:
                        root.Hash = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Size:
                        root.Size = CompactNum.ToInt64(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Key:
                        root.Key = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return root;
        }
    }

    internal class SharedFile : G2Packet
    {
        const byte Packet_Name = 0x10;
        const byte Packet_Hash = 0x20;
        const byte Packet_Size = 0x30;
        const byte Packet_FileKey = 0x40;
        const byte Packet_SystemPath = 0x50;
        const byte Packet_Public = 0x60;


        internal string Name;
        internal byte[] Hash;
        internal long Size;
        internal byte[] FileKey;

        internal ulong FileID;
        internal ushort ClientID;

        internal bool SaveLocal;
        internal string SystemPath;
        internal bool Public;

        internal List<DhtClient> ToRequest = new List<DhtClient>();
        internal List<DhtClient> Sources = new List<DhtClient>();
        internal bool Completed;
        internal bool Ignore;
        internal DateTime NextPublish;
        internal bool TransferActive;

        internal string FileStatus = "";
        internal string TransferStatus = "";

        internal SharedFile(ushort client)
        {
            ClientID = client;
        }

        internal SharedFile(SharedFile copy, ushort client)
        {
            ClientID = client;
            Name = copy.Name;
            Hash = copy.Hash;
            Size = copy.Size;
            FileKey = copy.FileKey;
            FileID = OpTransfer.GetFileID((uint)ServiceIDs.Share, Hash, Size);
        }

        public override string ToString()
        {
            return Name;
        }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame root = protocol.WritePacket(null, SharePacket.File, null);

                protocol.WritePacket(root, Packet_Name, UTF8Encoding.UTF8.GetBytes(Name));
                protocol.WritePacket(root, Packet_Hash, Hash);
                protocol.WritePacket(root, Packet_Size, CompactNum.GetBytes(Size));
                protocol.WritePacket(root, Packet_FileKey, FileKey);

                if (SaveLocal)
                {
                    if (SystemPath != null)
                        protocol.WritePacket(root, Packet_SystemPath, UTF8Encoding.UTF8.GetBytes(SystemPath));

                    if (Public)
                        protocol.WritePacket(root, Packet_Public, null);
                }

                return protocol.WriteFinish();
            }
        }

        internal static SharedFile Decode(G2Header header, ushort client)
        {
            SharedFile root = new SharedFile(client);
            G2Header child = new G2Header(header.Data);

            while (G2Protocol.ReadNextChild(header, child) == G2ReadResult.PACKET_GOOD)
            {
                if (child.Name == Packet_Public)
                    root.Public = true;

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

    internal class CachedLocation
    {
        internal ulong FileID;
        internal DateTime Expires;
        internal byte[] Data;
    }

    internal class PublicShareRequest : G2Packet
    {
        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame root = protocol.WritePacket(null, SharePacket.PublicRequest, null);
                return protocol.WriteFinish();
            }
        }

        internal static PublicShareRequest Decode(G2Header header)
        {
            return new PublicShareRequest();
        }
    }
}
