using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Transport;

using DeOps.Services;
using DeOps.Services.Assist;
using DeOps.Services.Location;
using DeOps.Services.Transfer;

using DeOps.Utility;

/* active shares are periodically published at fileID on network so that when source goes offline
 * more locations can be found
 */


namespace DeOps.Services.Share
{
    public class SharePacket
    {
        public const byte File = 0x10;
        public const byte PublicRequest = 0x20;
        public const byte Collection = 0x30;
    }

    public delegate void ShareFileUpdateHandler(SharedFile share);
    public delegate void ShareCollectionUpdateHandler(ulong user);
    public delegate void FileProcessedHandler(SharedFile share, object arg);


    public class ShareService : OpService
    {
        public string Name { get { return "Sharing"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.Share; } }

        OpCore Core;
        DhtNetwork Network;

        public ShareCollection Local;
        public ThreadedDictionary<ulong, ShareCollection> Collections = new ThreadedDictionary<ulong, ShareCollection>();

        WorkerQueue ProcessFiles = new WorkerQueue("Share Process");
        WorkerQueue OpenFiles = new WorkerQueue("Share Open");

        string SharePath;
        string HeaderPath;
        string DownloadPath;
        string PublicPath;

        public event ShareFileUpdateHandler GuiFileUpdate;
        public event ShareCollectionUpdateHandler GuiCollectionUpdate;

        const uint DataTypeShare = 0x01;
        const uint DataTypeLocation = 0x02;
        const uint DataTypePublic = 0x03;
        const uint DataTypeSession = 0x04; // used for sending file reqs, public reqs, and lists over rudp

        TempCache TempLocation;

        public bool RunSave;


        public ShareService(OpCore core)
        {
            Core = core;
            Network = core.Network;

            string rootPath = Core.User.RootPath + Path.DirectorySeparatorChar +
                        "Data" + Path.DirectorySeparatorChar +
                        ServiceID.ToString() + Path.DirectorySeparatorChar;

            SharePath = rootPath + DataTypeShare.ToString() + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(SharePath);

            HeaderPath = SharePath + Utilities.CryptFilename(Core, "ShareHeaders");

            PublicPath = rootPath + DataTypePublic.ToString() + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(PublicPath);

            DownloadPath = Core.User.RootPath + Path.DirectorySeparatorChar + "Downloads" + Path.DirectorySeparatorChar;


            Core.SecondTimerEvent += Core_SecondTimer;
         
            // data
            Network.RudpControl.SessionUpdate += new SessionUpdateHandler(Session_Update);
            Network.RudpControl.SessionData[ServiceID, DataTypeSession] += new SessionDataHandler(Session_Data);

            Core.Transfers.FileSearch[ServiceID, DataTypeShare] += new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[ServiceID, DataTypeShare] += new FileRequestHandler(Transfers_FileRequest);

            Core.Transfers.FileSearch[ServiceID, DataTypePublic] += new FileSearchHandler(Transfers_PublicSearch);
            Core.Transfers.FileRequest[ServiceID, DataTypePublic] += new FileRequestHandler(Transfers_PublicRequest);

            // location
            TempLocation = new TempCache(Network, ServiceID, DataTypeLocation);
      
            Local = new ShareCollection(Core.UserID);
            Collections.SafeAdd(Core.UserID, Local);

            LoadHeaders();
        }

        public void Dispose()
        {
            //crit - public shared directory can be deleted here, except for our local public header

            ProcessFiles.Dispose();
            OpenFiles.Dispose();

            Core.SecondTimerEvent -= Core_SecondTimer;
 
            // file
            Network.RudpControl.SessionUpdate -= new SessionUpdateHandler(Session_Update);
            Network.RudpControl.SessionData[ServiceID, DataTypeShare] -= new SessionDataHandler(Session_Data);

            Core.Transfers.FileSearch[ServiceID, DataTypeShare] -= new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[ServiceID, DataTypeShare] -= new FileRequestHandler(Transfers_FileRequest);

            TempLocation.Dispose();
        }

        public void SimTest()
        {
            
        }

        public void SimCleanup()
        {
        }

       public void SaveHeaders()
        {
            // save public shared lists
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
                            if (file.Hash != null && 
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
                            if (file.Hash != null &&
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

                File.Copy(tempPath, HeaderPath, true);
                File.Delete(tempPath);
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Share", "Error saving headers: " + ex.Message);
            }
        }

        private void LoadHeaders()
        {
            List<string> goodPaths = new List<string>();
            
            // load shared file lists

            try
            {
                goodPaths.Add(HeaderPath);

                if (!File.Exists(HeaderPath))
                    return;

                using (IVCryptoStream crypto = IVCryptoStream.Load(HeaderPath, Core.User.Settings.FileKey))
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

                // clears most of files in direcotry, others shared public lists are not persisted between runs
                foreach (string testPath in Directory.GetFiles(SharePath))
                    if (!goodPaths.Contains(testPath))
                        try { File.Delete(testPath); }
                        catch { }
            }
            catch (Exception ex)
            {
                Network.UpdateLog("VersionedFile", "Error loading data " + ex.Message);
            }
        }

        void Core_SecondTimer()
        {
            if (RunSave)
            {
                SaveHeaders();
                RunSave = false;
            }

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

                        else if (transfer.Verifying)
                            file.TransferStatus = "Verifying...";

                        else if (Core.Transfers.UploadPeers.Values.Where(u => u.Active != null && u.Active.Transfer.FileID == file.FileID).Count() > 0)
                        {
                            file.TransferStatus = "Uploading at " + Utilities.ByteSizetoString((long)transfer.Bandwidth.OutAvg()) + "/s";
                            active = true;
                        }
                    }

                    file.TransferActive = active;

                    if (active && Core.TimeNow > file.NextPublish)
                    {
                        TempLocation.Publish(file.FileID, Core.Locations.LocalClient.Data.EncodeLight(Network.Protocol));                     
                        file.NextPublish = Core.TimeNow.AddHours(1);
                    }

                }
            });
        }
    
        public string GetFilePath(SharedFile file)
        {
            if (file.Hash == null)
                return "";

            return SharePath + Utilities.CryptFilename(Core, Core.UserID, file.Hash);
        }

        private string GetPublicPath(ShareCollection collection)
        {
            return PublicPath + Utilities.CryptFilename(Core, collection.UserID, collection.Hash);
        }

        public void LoadFile(string path)
        {
            SendFile(path, null);
        }

        public void SendFile(string path, Tuple<FileProcessedHandler, object> processed)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => SendFile(path, processed));
                return;
            }

            // add to share list
            SharedFile file = new SharedFile(Core.Network.Local.ClientID);
            file.Name = Path.GetFileName(path);
            file.SystemPath = path;
            
            file.Completed = true;
            file.Sources.Add(Core.Network.Local);
            file.FileStatus = "Processing...";
            file.Processed = processed;

            // so user can see hash progress
            Local.Files.SafeAdd(file);
            Core.RunInGuiThread(GuiFileUpdate, file);

            ProcessFiles.Enqueue(() => ProcessFile(file));
        }

        public void AddTargets(List<DhtClient> targets, ulong user, ushort client)
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

        void ProcessFile(SharedFile file)
        {
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

                RijndaelManaged crypt = Utilities.CommonFileKey(Core.User.Settings.OpKey, internalHash);
                file.FileKey = crypt.Key;

                // encrypt file to temp dir
                file.FileStatus = "Securing...";
                string tempPath = Core.GetTempPath();
                Utilities.EncryptTagFile(file.SystemPath, tempPath, crypt, Network.Protocol, ref file.Hash, ref file.Size);
                file.FileID = OpTransfer.GetFileID(ServiceID, file.Hash, file.Size);

                // move to official path
                string path = GetFilePath(file);
                if (!File.Exists(path))
                    File.Move(tempPath, path);

                file.FileStatus = "Secured";

                // run in core thread -> save, send request to user
                if (file.Processed != null)
                    Core.RunInCoreAsync(() => file.Processed.Param1.Invoke(file, file.Processed.Param2));

                RunSave = true;
            }
            catch(Exception ex)
            {
                file.FileStatus = "Error: " + ex.Message;
            }
        }



        void Session_Update(RudpSession session)
        {
            DhtClient client = new DhtClient(session.UserID, session.ClientID);

            if (session.Status == SessionStatus.Active)
            {
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
                    case SharePacket.PublicRequest:
                        ReceivePublicRequest(session);
                        break;

                    case SharePacket.Collection:
                        ReceivePublicDetails(session, ShareCollection.Decode(root, session.UserID));
                        break;
                }
            }
        }



        public void GetPublicList(ulong user)
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

            session.SendData(ServiceID, DataTypeSession, new PublicShareRequest());
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
                session.SendData(ServiceID, DataTypeSession, Local);
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
            OpTransfer transfer = Core.Transfers.StartDownload(client.UserID, details, GetPublicPath(collection), new EndDownloadHandler(CollectionDownloadFinished), args);
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

        public void CollectionDownloadFinished(object[] args)
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

        private OpTransfer StartTransfer(DhtClient client, SharedFile file)
        {
            FileDetails details = new FileDetails(ServiceID, DataTypeShare, file.Hash, file.Size, null);
            object[] args = new object[] { file };

            OpTransfer transfer = Core.Transfers.StartDownload(client.UserID, details, GetFilePath(file), new EndDownloadHandler(FileDownloadFinished), args);

            transfer.AddPeer(client);

            file.TransferStatus =  "Starting download from " + Core.GetName(client.UserID);

            return transfer;
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

        public void FileDownloadFinished(object[] args)
        {
            SharedFile file = args[0] as SharedFile;

            file.Completed = true;
            file.Sources.Add(Core.Network.Local);

            file.FileStatus = "Download Finished";
            
            Core.RunInGuiThread(GuiFileUpdate, file);

            Core.MakeNews(ServiceIDs.Share, file.Name + " Finished Downloading", Core.UserID, 0, false);
        }

        public string GetFileLink(ulong user, SharedFile file)
        {
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


            FileLink link = new FileLink()
            {
                OpName = Core.User.Settings.Operation,
                FileName = file.Name,
                PublicOpID = Core.User.Settings.PublicOpID,
                Size = file.Size,
                Hash = file.Hash,
                Key = file.FileKey,
                Sources = sources
            };

            return link.Encode(Core);
        }

        public void DownloadLink(string text)
        {
            FileLink link = FileLink.Decode(text, Core);

            SharedFile file = new SharedFile(Core.Network.Local.ClientID);

            file.Name = link.FileName;

            if (!Utilities.MemCompare(link.PublicOpID, Core.User.Settings.PublicOpID))
                throw new Exception("File Link is not for this Op");

            file.Size = link.Size;
            file.Hash = link.Hash;
            file.FileKey = link.Key;
            file.FileID = OpTransfer.GetFileID(ServiceID, file.Hash, file.Size);

            if(link.Sources != null)
                for (int i = 0; i < link.Sources.Length; i += 10)
                    file.Sources.Add(DhtClient.FromBytes(link.Sources, i));

            DownloadFile(file);
        }

        public void DownloadFile(ulong user, SharedFile file)
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
                if (existing.Completed)
                    return;

                existing.Sources = existing.Sources.Union(file.Sources).ToList();
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
                if (file.Sources.Count > 0)
                {
                    OpTransfer transfer = StartTransfer(file.Sources[0], file);

                    file.Sources.ForEach(s => transfer.AddPeer(s));
                }

                TempLocation.Search(file.FileID, file, Search_FoundLocation);
            });

            RunSave = true;

            file.FileStatus = "Incomplete";

            Core.RunInGuiThread(GuiFileUpdate, file);
        }

        public void OpenFile(ulong user, SharedFile file)
        {
            if (!File.Exists(GetFilePath(file)))
                return;

            // check if already exists, if it does open
            if (file.SystemPath != null && File.Exists(file.SystemPath))
            {
                Process.Start(file.SystemPath);
                return;
            }

            OpenFiles.Enqueue(() => OpenFile(file));
        }

        void OpenFile(SharedFile file)
        {
            if (!Directory.Exists(DownloadPath))
                Directory.CreateDirectory(DownloadPath);

            string finalpath = DownloadPath + file.Name;

            int i = 1;
            while (File.Exists(finalpath))
                finalpath = DownloadPath + "(" + (i++) + ") " + file.Name;


            // decrypt file to temp dir
            file.FileStatus = "Unsecuring...";
            Utilities.DecryptTagFile(GetFilePath(file), finalpath, file.FileKey, Core);

            file.SystemPath = finalpath;
            file.FileStatus = "File in Downloads Folder";

            Process.Start(finalpath);

            RunSave = true;
        }

        public void RemoveFile(SharedFile file)
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

            RunSave = true;

            Core.RunInGuiThread(GuiFileUpdate, file);
        }

        void Search_FoundLocation(byte[] data, object arg)
        {
            SharedFile file = arg as SharedFile;

            if (file.Completed)
                return;

            OpTransfer transfer = null;

            // add locations to running transfer
            LocationData loc = LocationData.Decode(data);
            DhtClient client = new DhtClient(loc.UserID, loc.Source.ClientID);

            if (transfer == null)
                transfer = StartTransfer(client, file);

            Core.Network.LightComm.Update(loc);
            transfer.AddPeer(client);
        }
    }

    public class FileLink
    {
        public string OpName;
        public string FileName;
        public byte[] PublicOpID;
        public long Size;
        public byte[] Hash;
        public byte[] Key;
        public byte[] Sources;

        public string Encode(OpCore core)
        {
            //  deops://opName/file/name/size/opId~size~hash~key/sources

            string link = "deops://" + HttpUtility.UrlEncode(OpName) +
                            "/file/" + HttpUtility.UrlEncode(FileName) + "/" + 
                            HttpUtility.UrlEncode(Utilities.ByteSizetoString(Size)) + "/";

            byte[] cryptTag = BitConverter.GetBytes(Size);  // 8
            cryptTag = Utilities.CombineArrays(cryptTag, Hash); // 20
            cryptTag = Utilities.CombineArrays(cryptTag, Key); // 32

            cryptTag = Utilities.EncryptBytes(cryptTag, core.Network.OpCrypt.Key);
            byte[] endtag = Utilities.CombineArrays(PublicOpID, cryptTag);

            link += Utilities.ToBase64String(endtag) + "/";

            if (Sources != null)
                link += Utilities.ToBase64String(Utilities.EncryptBytes(Sources, core.Network.OpCrypt.Key));

            return link;
        }

        public static FileLink Decode(string text, OpCore core)
        {
            FileLink link = new FileLink();

            if (!text.StartsWith("deops://"))
                throw new Exception("Invalid Link");

            string[] parts = text.Substring(8).Split('/');

            if (parts.Length < 5)
                throw new Exception("Invalid Link");

            if (parts[1] != "file")
                throw new Exception("Invalid Link");

            link.FileName = HttpUtility.UrlDecode(parts[2]);

            byte[] endtag = Utilities.FromBase64String(parts[4]);

            if (endtag.Length < (8 + 8 + 20 + 32))
                throw new Exception("Invalid Link");

            link.PublicOpID = Utilities.ExtractBytes(endtag, 0, 8);

            byte[] cryptTag = Utilities.ExtractBytes(endtag, 8, endtag.Length - 8);

            if (core != null)
            {
                cryptTag = Utilities.DecryptBytes(cryptTag, cryptTag.Length, core.Network.OpCrypt.Key);

                link.Size = BitConverter.ToInt64(Utilities.ExtractBytes(cryptTag, 0, 8), 0);
                link.Hash = Utilities.ExtractBytes(cryptTag, 8, 20);
                link.Key = Utilities.ExtractBytes(cryptTag, 8 + 20, 32);

                if (parts.Length >= 6)
                {
                    link.Sources = Utilities.FromBase64String(parts[5]);

                    if (link.Sources != null)
                        link.Sources = Utilities.DecryptBytes(link.Sources, link.Sources.Length, core.Network.OpCrypt.Key);
                }
            }

            return link;
        }
    }

    public class ShareCollection : G2Packet
    {  
        const byte Packet_Hash = 0x10;
        const byte Packet_Size = 0x20;
        const byte Packet_Key = 0x30;

        // public file info
        public byte[] Hash;
        public long Size;
        public byte[] Key;


        public ulong UserID;
        public string Status;
        public List<DhtClient> ToRequest = new List<DhtClient>();
        public ThreadedList<SharedFile> Files = new ThreadedList<SharedFile>();


        public ShareCollection(ulong user)
        {
            UserID = user;
        }

        public override byte[] Encode(G2Protocol protocol)
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

        public static ShareCollection Decode(G2Header header, ulong user)
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

    public class SharedFile : G2Packet
    {
        const byte Packet_Name = 0x10;
        const byte Packet_Hash = 0x20;
        const byte Packet_Size = 0x30;
        const byte Packet_FileKey = 0x40;
        const byte Packet_SystemPath = 0x50;
        const byte Packet_Public = 0x60;


        public string Name;
        public byte[] Hash;
        public long Size;
        public byte[] FileKey;

        public ulong FileID;
        public ushort ClientID;

        public bool SaveLocal;
        public string SystemPath;
        public bool Public;

        public List<DhtClient> Sources = new List<DhtClient>();
        public bool Completed;
        public DateTime NextPublish;
        public bool TransferActive;

        public string FileStatus = "";
        public string TransferStatus = "";
        public Tuple<FileProcessedHandler, object> Processed;


        public SharedFile(ushort client)
        {
            ClientID = client;
        }

        public SharedFile(SharedFile copy, ushort client)
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

        public override byte[] Encode(G2Protocol protocol)
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

        public static SharedFile Decode(G2Header header, ushort client)
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



    public class PublicShareRequest : G2Packet
    {
        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame root = protocol.WritePacket(null, SharePacket.PublicRequest, null);
                return protocol.WriteFinish();
            }
        }

        public static PublicShareRequest Decode(G2Header header)
        {
            return new PublicShareRequest();
        }
    }
}
