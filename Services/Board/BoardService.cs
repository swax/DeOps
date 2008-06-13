using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;

using RiseOp.Services.Trust;
using RiseOp.Services.Transfer;
using RiseOp.Services.Location;


namespace RiseOp.Services.Board
{
    internal enum ScopeType {All, High, Low };
    internal enum BoardSearch { Threads, Time, Post };

    internal delegate void PostUpdateHandler(OpPost post);


    internal class BoardService : OpService
    {
        public string Name { get { return "Board"; } }
        public uint ServiceID { get { return 8; } }

        internal OpCore Core;
        internal G2Protocol Protocol;
        internal DhtNetwork Network;
        internal DhtStore Store;
        TrustService Links;

        bool Loading = true;
        internal string BoardPath;
        RijndaelManaged LocalFileKey;

        int PruneSize = 64;

        internal List<ulong> SaveHeaders = new List<ulong>();
        internal ThreadedDictionary<ulong, OpBoard> BoardMap = new ThreadedDictionary<ulong, OpBoard>();    
        internal ThreadedDictionary<ulong, List<BoardView>> WindowMap = new ThreadedDictionary<ulong, List<BoardView>>();


        ThreadedDictionary<int, ushort> SavedReplyCount = new ThreadedDictionary<int, ushort>();
        ThreadedDictionary<ulong, List<PostUID>> DownloadLater = new ThreadedDictionary<ulong, List<PostUID>>();

        internal PostUpdateHandler PostUpdate;


        internal BoardService(OpCore core )
        {
            Core       = core;
            Network = Core.Network;
            Protocol = Network.Protocol;
            Store    = Network.Store;
            Links = Core.Links;

            Core.SecondTimerEvent += new TimerHandler(Core_SecondTimer);
            Core.MinuteTimerEvent += new TimerHandler(Core_MinuteTimer);

            Network.StatusChange += new StatusChange(Network_StatusChange);

            Store.StoreEvent[ServiceID, 0] += new StoreHandler(Store_Local);
            Store.ReplicateEvent[ServiceID, 0] += new ReplicateHandler(Store_Replicate);
            Store.PatchEvent[ServiceID, 0] += new PatchHandler(Store_Patch);

            Network.Searches.SearchEvent[ServiceID, 0] += new SearchRequestHandler(Search_Local);

            Core.Transfers.FileSearch[ServiceID, 0] += new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[ServiceID, 0] += new FileRequestHandler(Transfers_FileRequest);

            if (Core.Sim != null)
                PruneSize = 16;

            LocalFileKey = Core.User.Settings.FileKey;

            BoardPath = Core.User.RootPath + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + ServiceID.ToString();

            if (!Directory.Exists(BoardPath))
                Directory.CreateDirectory(BoardPath);


            // get available board header targets
            string[] directories = Directory.GetDirectories(BoardPath);

            SortedDictionary<ulong, ulong> targets = new SortedDictionary<ulong, ulong>(); // key distance to self, value target

            foreach (string path in directories)
            {
                string dir = Path.GetFileName(path); // gets dir name

                ulong id = BitConverter.ToUInt64(Utilities.HextoBytes(dir), 0);

                targets[Core.UserID ^ id] = id;
            }

            // load closest targets
            int loaded = 0;
            foreach (ulong id in targets.Values)
            {
                LoadHeader(id);

                loaded++;
                if (loaded == PruneSize)
                    break;
            }

            Loading = false;
        }

        public void Dispose()
        {
            Core.SecondTimerEvent -= new TimerHandler(Core_SecondTimer);
            Core.MinuteTimerEvent -= new TimerHandler(Core_MinuteTimer);

            Network.StatusChange -= new StatusChange(Network_StatusChange);

            Store.StoreEvent[ServiceID, 0] -= new StoreHandler(Store_Local);
            Store.ReplicateEvent[ServiceID, 0] -= new ReplicateHandler(Store_Replicate);
            Store.PatchEvent[ServiceID, 0] -= new PatchHandler(Store_Patch);

            Network.Searches.SearchEvent[ServiceID, 0] -= new SearchRequestHandler(Search_Local);

            Core.Transfers.FileSearch[ServiceID, 0] -= new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[ServiceID, 0] -= new FileRequestHandler(Transfers_FileRequest);

        }

        void Core_SecondTimer()
        {
            // save headers, timeout 10 secs
            if (Core.TimeNow.Second % 9 == 0)
                lock (SaveHeaders)
                {
                    foreach (ulong id in SaveHeaders)
                        SaveHeader(id);

                    SaveHeaders.Clear();
                }

            // clean download later map
            if (!Network.Established)
                PruneMap(DownloadLater);
        }

        void Core_MinuteTimer()
        {
            List<ulong> removeBoards = new List<ulong>();

               
            // prune loaded boards
            BoardMap.LockReading(delegate()
            {
                if (BoardMap.Count > PruneSize)
                {
                    List<ulong> localRegion = new List<ulong>();
                    foreach (uint project in Core.Links.LocalTrust.Links.Keys )
                        localRegion.AddRange(GetBoardRegion(Core.UserID, project, ScopeType.All));

                    foreach (OpBoard board in BoardMap.Values)
                        if (board.UserID != Core.UserID &&
                            !Network.Routing.InCacheArea(board.UserID) &&
                            !Core.Focused.SafeContainsKey(board.UserID) &&
                            !WindowMap.SafeContainsKey(board.UserID) &&
                            !localRegion.Contains(board.UserID))
                        {
                            removeBoards.Add(board.UserID);
                        }
                }
            });

            if (removeBoards.Count > 0)
                BoardMap.LockWriting(delegate()
                {
                    while (removeBoards.Count > 0 && BoardMap.Count > PruneSize / 2)
                    {
                        // find furthest id
                        ulong furthest = Core.UserID;

                        foreach (ulong id in removeBoards)
                            if ((id ^ Core.UserID) > (furthest ^ Core.UserID))
                                furthest = id;

                        // remove
                        try
                        {
                            string dir = BoardPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, furthest.ToString());
                            string[] files = Directory.GetFiles(dir);

                            foreach (string path in files)
                                File.Delete(path);

                            Directory.Delete(dir);
                        }
                        catch { }

                        BoardMap.Remove(furthest);
                        removeBoards.Remove(furthest);
                    }
                });
            
        }

        private void PruneMap(ThreadedDictionary<ulong, List<PostUID>> map)
        {
            if (map.SafeCount < PruneSize)
                return;

            List<ulong> removeIDs = new List<ulong>();

            map.LockWriting(delegate()
            {
                while (map.Count > 0 && map.Count > PruneSize)
                {
                    ulong furthest = Core.UserID;

                    // get furthest id
                    foreach (ulong id in map.Keys)
                        if ((id ^ Core.UserID) > (furthest ^ Core.UserID))
                            furthest = id;

                    // remove one 
                    map.Remove(furthest);
                }
            });
        }

        void Network_StatusChange()
        {
            if (Network.Established)
            {
                // republish objects that were not seen on the network during startup
                BoardMap.LockReading(delegate()
                {
                    foreach (OpBoard board in BoardMap.Values)
                        board.Posts.LockReading(delegate()
                        {
                            foreach (OpPost post in board.Posts.Values)
                                if (post.Unique && Network.Routing.InCacheArea(board.UserID))
                                    Store.PublishNetwork(board.UserID, ServiceID, 0, post.SignedHeader);
                        });
                });

                // only download those objects in our local area
                DownloadLater.LockWriting(delegate()
                {
                    foreach (ulong key in DownloadLater.Keys)
                        if (Network.Routing.InCacheArea(key))
                            foreach (PostUID uid in DownloadLater[key])
                                PostSearch(key, uid, 0);

                    DownloadLater.Clear();
                });
            }

            // disconnected, reset cache to unique
            else
            {
                BoardMap.LockReading(delegate()
                {
                    foreach (OpBoard board in BoardMap.Values)
                        board.Posts.LockReading(delegate()
                        {
                            foreach (OpPost post in board.Posts.Values)
                                post.Unique = true;
                        });
                });
            }
        }

        internal void LoadHeader(ulong id)
        {
            try
            {
                string path = GetTargetDir(id) + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "headers" + id.ToString());

                if (!File.Exists(path))
                    return;

                FileStream file = new FileStream(path, FileMode.Open);
                CryptoStream crypto = new CryptoStream(file, LocalFileKey.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Network.Protocol, FileAccess.Read);

                G2Header root = null;

                while (stream.ReadPacket(ref root))
                    if (root.Name == DataPacket.SignedData)
                    {
                        SignedData signed = SignedData.Decode(root);
                        G2Header embedded = new G2Header(signed.Data);

                        // figure out data contained
                        if (G2Protocol.ReadPacket(embedded))
                            if (embedded.Name == BoardPacket.PostHeader)
                                Process_PostHeader(null, signed, PostHeader.Decode(embedded));
                    }

                stream.Close();
            }
            catch(Exception ex)
            {
                Network.UpdateLog("Board", "Could not load header " + id.ToString() + ": " + ex.Message);
            }
        }

        private void SaveHeader(ulong id)
        {
            OpBoard board = GetBoard(id);

            if (board == null)
                return;

            try
            {
                string tempPath = Core.GetTempPath();
                FileStream file = new FileStream(tempPath, FileMode.Create);
                CryptoStream stream = new CryptoStream(file, LocalFileKey.CreateEncryptor(), CryptoStreamMode.Write);

                board.Posts.LockReading(delegate()
                {
                    foreach (OpPost post in board.Posts.Values)
                        stream.Write(post.SignedHeader, 0, post.SignedHeader.Length);
                });

                stream.FlushFinalBlock();
                stream.Close();


                string finalPath = GetTargetDir(id) + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "headers" + id.ToString());
                File.Delete(finalPath);
                File.Move(tempPath, finalPath);
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Board", "Error saving board headers " + id.ToString() + " " + ex.Message);
            }
        }

        public List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong user, uint project)
        {
            List<MenuItemInfo> menus = new List<MenuItemInfo>();

            if (menuType == InterfaceMenuType.Quick)
                return null;

            if (menuType == InterfaceMenuType.Internal)
                menus.Add(new MenuItemInfo("Comm/Board", BoardRes.Icon, new EventHandler(Menu_View)));

            if (menuType == InterfaceMenuType.External)
                menus.Add(new MenuItemInfo("Board", BoardRes.Icon, new EventHandler(Menu_View)));

            return menus;
        }

        void Menu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            BoardView view = new BoardView(this, node.GetKey(), node.GetProject());

            Core.InvokeView(node.IsExternal(), view);
        }

        internal void PostEdit(OpPost edit)
        {
            // used for archive/restore
            PostHeader copy = edit.Header.Copy();

            copy.Version++;
            copy.EditTime = Core.TimeNow.ToUniversalTime();

            FinishPost(copy);
        }

        internal void PostMessage(ulong id, uint project, uint parent, ScopeType scope, string subject, string message, List<AttachedFile> files, OpPost edit)
        {
            // post header
            PostHeader header = new PostHeader();
            
            header.Source = Core.User.Settings.KeyPublic;
            header.SourceID = Core.UserID;

            header.Target = Core.KeyMap[id];
            header.TargetID = id;

            header.ParentID = parent;
            header.ProjectID = project;
            
            header.Scope = scope;

            if (edit == null)
            {
                header.Time = Core.TimeNow.ToUniversalTime();
                
                byte[] rnd = new byte[4];
                Core.RndGen.NextBytes(rnd);
                header.PostID = BitConverter.ToUInt32(rnd, 0);
            }
            else
            {
                header.PostID = edit.Header.PostID;
                header.Version = (ushort) (edit.Header.Version + 1);
                header.Time = edit.Header.Time;
                header.EditTime = Core.TimeNow.ToUniversalTime();
            }

            header.FileKey.GenerateKey();
            header.FileKey.IV = new byte[header.FileKey.IV.Length];

            // setup temp file
            string tempPath = Core.GetTempPath();
            FileStream tempFile = new FileStream(tempPath, FileMode.CreateNew);
            CryptoStream stream = new CryptoStream(tempFile, header.FileKey.CreateEncryptor(), CryptoStreamMode.Write);
            int written = 0;

            // write post file
            written += Protocol.WriteToFile(new PostInfo(subject, Core.RndGen), stream);

            byte[] msgBytes = UTF8Encoding.UTF8.GetBytes(message);
            written += Protocol.WriteToFile(new PostFile("body", msgBytes.Length), stream);

            foreach (AttachedFile attached in files)
                written += Protocol.WriteToFile(new PostFile(attached.Name, attached.Size), stream);

            stream.WriteByte(0); // end packets
            header.FileStart = (long)written + 1;

            // write files
            stream.Write(msgBytes, 0, msgBytes.Length);

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

            stream.WriteByte(0); // signal last packet

            stream.FlushFinalBlock();
            stream.Close();

            // finish building header
            Utilities.HashTagFile(tempPath, ref header.FileHash, ref header.FileSize);

            string finalPath = GetPostPath(header);
            File.Move(tempPath, finalPath);

            FinishPost(header);
        }

        void FinishPost(PostHeader header)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreBlocked(delegate() { FinishPost(header); });
                return;
            }

            CachePost(new SignedData(Protocol, Core.User.Settings.KeyPair, header), header);

            // publish to network and local region of target
            OpPost post = GetPost(header);

            if (Network.Established)
                Network.Store.PublishNetwork(header.TargetID, ServiceID, 0, GetPost(header).SignedHeader);
            else if (post != null)
                post.Unique = true; // publish when connected
                

            List<LocationData> locations = new List<LocationData>();
            Links.GetLocs(header.TargetID, header.ProjectID, 1, 1, locations);
            Links.GetLocs(header.TargetID, header.ProjectID, 0, 1, locations);

            Store.PublishDirect(locations, header.TargetID, ServiceID, 0, GetPost(header).SignedHeader);


            // save right off, dont wait for timer, or sim to be on
            SaveHeader(header.TargetID);
        }


        internal string GetPostPath(PostHeader header)
        {
            string targetDir = GetTargetDir(header.TargetID);

            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            byte[] ident = new byte[PostUID.SIZE + header.FileHash.Length];
            new PostUID(header).ToBytes().CopyTo(ident, 0);
            header.FileHash.CopyTo(ident, PostUID.SIZE);

            return targetDir + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, header.TargetID, ident);
        }

        internal string GetTargetDir(ulong id)
        {
            return BoardPath + Path.DirectorySeparatorChar + Utilities.BytestoHex(BitConverter.GetBytes(id));
        }

        void Store_Local(DataReq store)
        {
            // getting published to - search results - patch

            SignedData signed = SignedData.Decode(store.Data);

            if (signed == null)
                return;

            G2Header embedded = new G2Header(signed.Data);

            // figure out data contained
            if (G2Protocol.ReadPacket(embedded))
            {
                if (embedded.Name == BoardPacket.PostHeader)
                    Process_PostHeader(store, signed, PostHeader.Decode(embedded));
            }
        }

        private void Process_PostHeader(DataReq data, SignedData signed, PostHeader header)
        {
            Core.IndexKey(header.SourceID, ref header.Source);
            Core.IndexKey(header.TargetID, ref header.Target);
            
            PostUID uid = new PostUID(header);

            OpPost current = GetPost(header.TargetID, uid);

            // if link loaded
            if (current != null)
            {
                // lower version, send update
                if (header.Version < current.Header.Version)
                {
                    if (data != null && data.Sources != null)
                        foreach (DhtAddress source in data.Sources)
                            Store.Send_StoreReq(source, data.LocalProxy, new DataReq(null, header.TargetID, ServiceID, 0, current.SignedHeader));

                    return;
                }

                // higher version
                else if (header.Version > current.Header.Version)
                {
                    CachePost(signed, header);
                }

                // equal version do nothing
            }

            // else load file, set new header after file loaded
            else
                CachePost(signed, header); 
      
        }

        private void CachePost(SignedData signedHeader, PostHeader header)
        {
            if (Core.InvokeRequired)
                Debug.Assert(false);

            if (header.ParentID == 0 && header.SourceID != header.TargetID)
            {
                Network.UpdateLog("Board", "Post made to board other than source's");
                return;
            }

            
            if (!File.Exists(GetPostPath(header)))
            {
                DownloadPost(signedHeader, header);
                return;
            }

            // check if current version loaded
            OpPost post = GetPost(header);
           
            if (post != null && post.Header.Version >= header.Version )
            {
                UpdateGui(post);
                return;
            }

            // put into map
            OpBoard board = GetBoard(header.TargetID);

            if (board == null)
            {
                board = new OpBoard(header.Target);
                BoardMap.SafeAdd(header.TargetID, board);
            }

            PostUID uid = new PostUID(header);

            post = new OpPost();
            post.Header = header;
            post.SignedHeader = signedHeader.Encode(Network.Protocol);
            post.Ident = header.TargetID.GetHashCode() ^ uid.GetHashCode();
            post.Unique = Loading;

            // remove previous version of file, if its different
            OpPost prevPost = board.GetPost(uid);

            if (prevPost != null && GetPostPath(prevPost.Header) != GetPostPath(header))
                try { File.Delete(GetPostPath(prevPost.Header)); }
                catch { }

            board.Posts.SafeAdd(uid,  post);

            // update replies
            if (post.Header.ParentID == 0)
                board.UpdateReplies(post);
            else
            {
                PostUID parentUid = new PostUID(board.UserID, post.Header.ProjectID, post.Header.ParentID);
                OpPost parentPost = board.GetPost(parentUid);

                if (parentPost != null)
                {
                    board.UpdateReplies(parentPost);
                    UpdateGui(post);
                }
            }

            lock (SaveHeaders)
                if (!SaveHeaders.Contains(header.TargetID))
                    SaveHeaders.Add(header.TargetID);

            ushort replies = 0;
            if (SavedReplyCount.SafeTryGetValue(post.Ident, out replies))
            {
                post.Replies = replies;
                SavedReplyCount.SafeRemove(post.Ident);
            }

            UpdateGui(post);

            if (Core.NewsWorthy(header.TargetID, header.ProjectID, true))
                Core.MakeNews("Board updated by " + Links.GetName(header.SourceID), header.SourceID, 0, false, BoardRes.Icon, Menu_View);
         
        }

        void DownloadPost(SignedData signed, PostHeader header)
        {
            if (!Utilities.CheckSignedData(header.Source, signed.Data, signed.Signature))
                return;

            FileDetails details = new FileDetails(ServiceID, 0, header.FileHash, header.FileSize, new PostUID(header).ToBytes());

            Core.Transfers.StartDownload(header.TargetID, details, new object[] { signed, header }, new EndDownloadHandler(EndDownload));
        }

        private void EndDownload(string path, object[] args)
        {
            SignedData signedHeader = (SignedData)args[0];
            PostHeader header = (PostHeader)args[1];

            try
            {
                File.Move(path, GetPostPath(header));
            }
            catch { return; }

            CachePost(signedHeader, header);
        }

        bool Transfers_FileSearch(ulong key, FileDetails details)
        {
            if (details.Extra == null || details.Extra.Length < 8)
                return false;

            OpPost post = GetPost(key, PostUID.FromBytes(details.Extra, 0));

            if (post != null && details.Size == post.Header.FileSize && Utilities.MemCompare(details.Hash, post.Header.FileHash))
                return true;

            return false;
        }

        string Transfers_FileRequest(ulong key, FileDetails details)
        {
            if (details.Extra == null || details.Extra.Length < 8)
                return null;

            OpPost post = GetPost(key, PostUID.FromBytes(details.Extra, 0));

            if (post != null && details.Size == post.Header.FileSize && Utilities.MemCompare(details.Hash, post.Header.FileHash))
                return GetPostPath(post.Header);

            return null;
        }

        internal OpPost GetPost(PostHeader header)
        {
            return GetPost(header.TargetID, new PostUID(header));
        }

        internal OpPost GetPost(ulong target, PostUID uid)
        {
            OpBoard board = GetBoard(target);

            if (board == null)
                return null;

            return board.GetPost(uid);
        }


        int PatchEntrySize = 8 + PostUID.SIZE + 2;

        List<byte[]> Store_Replicate(DhtContact contact)
        {
            if (!Network.Established)
                return null;


            List<byte[]> patches = new List<byte[]>();

            
            BoardMap.LockReading(delegate()
            {
                foreach (OpBoard board in BoardMap.Values)
                    if (Network.Routing.InCacheArea(board.UserID))
                    {
                        
                        board.Posts.LockReading(delegate()
                        {
                            foreach (PostUID uid in board.Posts.Keys)
                            {
                                byte[] patch = new byte[PatchEntrySize];

                                BitConverter.GetBytes(board.UserID).CopyTo(patch, 0);

                                uid.ToBytes().CopyTo(patch, 8);
                                BitConverter.GetBytes(board.Posts[uid].Header.Version).CopyTo(patch, 24);

                                patches.Add( patch);
                            }
                        });

                    }
            });

            return patches;
        }

        void Store_Patch(DhtAddress source, byte[] data)
        {
            if (data.Length % PatchEntrySize != 0)
                return;

            int offset = 0;

            for (int i = 0; i < data.Length; i += PatchEntrySize)
            {
                ulong user = BitConverter.ToUInt64(data, i);
                PostUID uid = PostUID.FromBytes(data, i + 8);
                ushort version = BitConverter.ToUInt16(data, i + 24);

                offset += PatchEntrySize;

                if (!Network.Routing.InCacheArea(user))
                    continue;

                OpPost post = GetPost(user, uid);

                if (post != null)
                {
                    // remote version is lower, send update
                    if (post.Header.Version > version)
                    {
                        Store.Send_StoreReq(source, null, new DataReq(null, user, ServiceID, 0, post.SignedHeader));
                        continue;
                    }
                        
                    // version equal,  pass
                    post.Unique = false; // network has current or newer version

                    if (post.Header.Version == version)
                        continue;

                    // else our version is old, download below
                }

                // download cause we dont have it or its a higher version 
                if (Network.Established)
                    Network.Searches.SendDirectRequest(source, user, ServiceID, 0, uid.ToBytes());
                else
                {
                    List<PostUID> list = null;
                    if (!DownloadLater.SafeTryGetValue(user, out list))
                    {
                        list = new List<PostUID>();
                        DownloadLater.SafeAdd(user, list);
                    }

                    list.Add(uid);
                }
            }
        }



        

        const int TheadSearch_ParamsSize = 9;   // type/project/parent  1 + 4 + 4
        const int TheadSearch_ResultsSize = 20; // UID/version/replies 16 + 2 + 2

        internal void ThreadSearch(ulong target, uint project, uint parent)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { ThreadSearch(target, project, parent); });
                return;
            }

            byte[] parameters = new byte[TheadSearch_ParamsSize];
            parameters[0] = (byte) BoardSearch.Threads;
            BitConverter.GetBytes(project).CopyTo(parameters, 1);
            BitConverter.GetBytes(parent).CopyTo(parameters, 5);

            DhtSearch search = Network.Searches.Start(target, "Board:Thread", ServiceID, 0, parameters, new EndSearchHandler(EndThreadSearch));

            if (search == null)
                return;

            search.TargetResults = 50;
        }

        void EndThreadSearch(DhtSearch search)
        {
            foreach (SearchValue found in search.FoundValues)
            {
                if (found.Value.Length < TheadSearch_ResultsSize)
                    continue;

                PostUID uid = PostUID.FromBytes(found.Value, 0);
                ushort version = BitConverter.ToUInt16(found.Value, 16);
                ushort replies = BitConverter.ToUInt16(found.Value, 18);

                OpPost post = GetPost(search.TargetID, uid);

                if (post != null)
                {
                    if (post.Replies < replies)
                    {
                        post.Replies = replies;
                        UpdateGui(post);
                    }

                    // if we have current version, pass, else download
                    if(post.Header.Version >= version)
                        continue;
                }

                PostSearch(search.TargetID, uid, version);

                // if parent save replies value
                if (replies != 0)
                {
                    int hash = search.TargetID.GetHashCode() ^ uid.GetHashCode();

                    ushort savedReplies = 0;
                    if (SavedReplyCount.SafeTryGetValue(hash, out savedReplies))
                        if (savedReplies < replies)
                            SavedReplyCount.SafeAdd(hash, replies);
                }
            }
        }

        const int TimeSearch_ParamsSize = 13;   // type/project/time 1 + 4 + 8
        const int TimeSearch_ResultsSize = 18;  // UID/version 16 + 2

        internal void TimeSearch(ulong target, uint project, DateTime time)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { TimeSearch(target, project, time); });
                return;
            }


            byte[] parameters = new byte[TimeSearch_ParamsSize];
            parameters[0] = (byte)BoardSearch.Time;
            BitConverter.GetBytes(project).CopyTo(parameters, 1);
            BitConverter.GetBytes(time.ToBinary()).CopyTo(parameters, 5);

            DhtSearch search = Network.Searches.Start(target, "Board:Time", ServiceID, 0, parameters, new EndSearchHandler(EndTimeSearch));

            if (search == null)
                return;

            search.TargetResults = 50;
        }

        void EndTimeSearch(DhtSearch search)
        {
            OpBoard board = GetBoard(search.TargetID);
            
            foreach (SearchValue found in search.FoundValues)
            {
                if (found.Value.Length < TheadSearch_ResultsSize)
                    continue;

                PostUID uid = PostUID.FromBytes(found.Value, 0);
                ushort version = BitConverter.ToUInt16(found.Value, 16);

                OpPost post = GetPost(search.TargetID, uid);

                if (post != null)
                    if (post.Header.Version >= version)
                        continue;

                PostSearch(search.TargetID, uid, version);
            }
        }

        const int PostSearch_ParamsSize = 19;   // type/uid/version  1 + 16 + 2

        private void PostSearch(ulong target, PostUID uid, ushort version)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { PostSearch(target, uid, version); });
                return;
            }

            byte[] parameters = new byte[PostSearch_ParamsSize];
            parameters[0] = (byte)BoardSearch.Post;
            uid.ToBytes().CopyTo(parameters, 1);
            BitConverter.GetBytes(version).CopyTo(parameters, 17);

            DhtSearch search = Core.Network.Searches.Start(target, "Board:Post", ServiceID, 0, parameters, new EndSearchHandler(EndPostSearch));

            if (search == null)
                return;

            search.TargetResults = 2;
        }

        void EndPostSearch(DhtSearch search)
        {
            foreach (SearchValue found in search.FoundValues)
                Store_Local(new DataReq(found.Sources, search.TargetID, ServiceID, 0, found.Value));
        }

        void Search_Local(ulong key, byte[] parameters, List<byte[]> results)
        {
            OpBoard board = GetBoard(key);

            if (board == null || parameters == null)
                return;

            // thread search
            if (parameters[0] == (byte)BoardSearch.Threads)
            {
                if (parameters.Length < TheadSearch_ParamsSize)
                    return;

                uint project = BitConverter.ToUInt32(parameters, 1);
                uint parent = BitConverter.ToUInt32(parameters, 5);

                board.Posts.LockReading(delegate()
                {
                    foreach (OpPost post in board.Posts.Values)
                        if (post.Header.ProjectID == project)
                            if ((parent == 0 && post.Header.ParentID == 0) || // searching for top level threads
                                (parent == post.Header.ParentID)) // searching for posts under particular thread
                            {
                                byte[] result = new byte[TheadSearch_ResultsSize];
                                new PostUID(post.Header).ToBytes().CopyTo(result, 0);
                                BitConverter.GetBytes(post.Header.Version).CopyTo(result, 16);
                                BitConverter.GetBytes(post.Replies).CopyTo(result, 18);

                                results.Add(result);
                            }
                });
            }

            // time search
            else if (parameters[0] == (byte)BoardSearch.Time)
            {
                if (parameters.Length < TimeSearch_ParamsSize)
                    return;

                uint project = BitConverter.ToUInt32(parameters, 1);
                DateTime time = DateTime.FromBinary(BitConverter.ToInt64(parameters, 5));

                board.Posts.LockReading(delegate()
                {
                    foreach (OpPost post in board.Posts.Values)
                        if (post.Header.ProjectID == project && post.Header.Time > time)
                        {
                            byte[] result = new byte[TimeSearch_ResultsSize];
                            new PostUID(post.Header).ToBytes().CopyTo(result, 0);
                            BitConverter.GetBytes(post.Header.Version).CopyTo(result, 16);

                            results.Add(result);
                        }
                });
            }

            // post search
            else if (parameters[0] == (byte)BoardSearch.Post)
            {
                if (parameters.Length < PostSearch_ParamsSize)
                    return;

                PostUID uid = PostUID.FromBytes(parameters, 1);
                ushort version = BitConverter.ToUInt16(parameters, 17);

                OpPost post = GetPost(key, uid);
                if (post != null)
                    if (post.Header.Version == version)
                        results.Add(post.SignedHeader);
            }
        }

        internal string GetPostInfo(OpPost post)
        {
            if (post.Info == null)
                try
                {
                    string path = GetPostPath(post.Header);
                    if (!File.Exists(path))
                        return "";

                    post.Attached = new List<PostFile>();

                    TaggedStream file = new TaggedStream(path);
                    CryptoStream crypto = new CryptoStream(file, post.Header.FileKey.CreateDecryptor(), CryptoStreamMode.Read);
                    PacketStream stream = new PacketStream(crypto, Network.Protocol, FileAccess.Read);

                    G2Header root = null;

                    while (stream.ReadPacket(ref root))
                    {
                        if (root.Name == BoardPacket.PostInfo)
                            post.Info = PostInfo.Decode(root);

                        else if (root.Name == BoardPacket.PostFile)
                            post.Attached.Add(PostFile.Decode(root));
                    }

                    stream.Close();
                }
                catch (Exception ex)
                {
                    Network.UpdateLog("Board", "Could not load post " + post.Header.SourceID.ToString() + ": " + ex.Message);
                }

            
            return (post.Info != null) ? post.Info.Subject : "";
        }

        internal OpBoard GetBoard(ulong key)
        {
            OpBoard board = null;

            BoardMap.SafeTryGetValue(key, out board);

            return board;
        }


        internal void LoadView(BoardView view, ulong id)
        {
            WindowMap.LockWriting(delegate()
            {
                if (!WindowMap.ContainsKey(id))
                    WindowMap[id] = new List<BoardView>();

                WindowMap[id].Add(view);
            });
        }

        internal void UnloadView(BoardView view, ulong id)
        {
            WindowMap.LockWriting(delegate()
           {
               if (!WindowMap.ContainsKey(id))
                   return;

               WindowMap[id].Remove(view);

               if (WindowMap[id].Count == 0)
                   WindowMap.Remove(id);
           });
        }

        internal List<ulong> GetBoardRegion(ulong id, uint project, ScopeType scope)
        {
            List<ulong> targets = new List<ulong>();

            targets.Add(id); // need to include self in high and low scopes, for re-searching, onlinkupdate purposes

            OpLink link = Links.GetLink(id, project);

            if (link == null)
                return targets;


            // higher
            if (scope != ScopeType.Low)
            {
                // if we're in loop, get our 'adjacents' who are really uplinks
                if (link.LoopRoot != null)
                {
                    targets.AddRange( Links.GetUplinkIDs(id, project) );
                }

                // get parent and children of parent
                else
                {
                    OpLink parent = link.GetHigher(true);

                    if (parent != null)
                    {
                        targets.Add(parent.UserID);

                        targets.AddRange(Links.GetDownlinkIDs(parent.UserID, project, 1));

                        targets.Remove(id); // remove self
                    }
                }
            }

            // lower - get children of self
            if (scope != ScopeType.High)
                targets.AddRange(Links.GetDownlinkIDs(id, project, 1));


            return targets;
        }

        internal void LoadRegion(ulong id, uint project)
        {
            // get all boards in local region
            List<ulong> targets = GetBoardRegion(id, project, ScopeType.All);


            foreach (ulong target in targets)
            {
                OpBoard board = GetBoard(target);

                if (board == null)
                {
                    LoadHeader(target); // updateinterface called in processheader
                    continue;
                }

                // call update for all posts
                board.Posts.LockReading(delegate()
                {
                    foreach (OpPost post in board.Posts.Values)
                        if (post.Header.ProjectID == project && post.Header.ParentID == 0)
                            UpdateGui(post);
                });
            }


            // searches
            foreach (ulong target in targets)
                SearchBoard(target, project);
        }

        internal void SearchBoard(ulong target, uint project)
        {
            bool fullSearch = true;

            OpBoard board = GetBoard(target);

            DateTime refresh = default(DateTime);
            if (board != null)
                if (board.LastRefresh.SafeTryGetValue(project, out refresh))
                    fullSearch = false;


            if (fullSearch)
                ThreadSearch(target, project, 0); 

            // search for all theads posted since refresh, with an hour buffer
            else
                TimeSearch(target, project, refresh.AddHours(-1));


            if (board != null)
                board.LastRefresh.SafeAdd(project, Core.TimeNow);
        }

        internal void LoadThread(OpPost parent)
        {
            OpBoard board = GetBoard(parent.Header.TargetID);

            if (board == null)
                return;

            // have all replies fire an update
            board.Posts.LockReading(delegate()
            {
               foreach (OpPost post in board.Posts.Values)
                   if (post.Header.ProjectID == parent.Header.ProjectID &&
                       post.Header.ParentID == parent.Header.PostID)
                       UpdateGui(post);
            });


            // do search for thread
            ThreadSearch(board.UserID, parent.Header.ProjectID, parent.Header.PostID);
        }


        internal void UpdateGui(OpPost post)
        {
            if (PostUpdate != null)
                Core.RunInGuiThread(PostUpdate, post);
        }
    }

    internal class PostUID
    {
        internal const int SIZE = 16;

        internal ulong SenderID;
        internal uint  ProjectID;
        internal uint  PostID;

        internal PostUID()
        {
        }

        internal PostUID(PostHeader header)
        {
            SenderID = header.SourceID ;
            ProjectID = header.ProjectID;
            PostID = header.PostID;
        }

        internal PostUID(ulong sender, uint project, uint post)
        {
            SenderID = sender;
            ProjectID = project;
            PostID = post;
        }

        internal byte[] ToBytes()
        {
            byte[] data = new byte[SIZE];

            BitConverter.GetBytes(SenderID).CopyTo(data, 0);
            BitConverter.GetBytes(ProjectID).CopyTo(data, 8);
            BitConverter.GetBytes(PostID).CopyTo(data, 12);

            return data;
        }

        internal static PostUID FromBytes(byte[] data, int offset)
        {
            PostUID uid = new PostUID();

            uid.SenderID = BitConverter.ToUInt64(data, offset + 0);
            uid.ProjectID = BitConverter.ToUInt32(data, offset + 8);
            uid.PostID = BitConverter.ToUInt32(data, offset + 12);

            return uid;
        }

        public override string ToString()
        {
            return Utilities.BytestoHex(ToBytes());
        }

        public override int GetHashCode()
        {
            return SenderID.GetHashCode() ^ ProjectID.GetHashCode() ^ PostID.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            PostUID compare = obj as PostUID;
            if (compare == null)
                return false;

            if (SenderID == compare.SenderID && ProjectID == compare.ProjectID && PostID == compare.PostID)
                return true;

            return false;
        }
    }

    internal class OpBoard
    {
        internal ulong UserID;
        internal byte[] Key;    // make sure reference is the same as main key list

        internal ThreadedDictionary<PostUID, OpPost> Posts = new ThreadedDictionary<PostUID, OpPost>();
        internal ThreadedDictionary<uint, DateTime> LastRefresh = new ThreadedDictionary<uint, DateTime>();
        

        internal OpBoard(byte[] key)
        {
            Key = key;
            UserID = Utilities.KeytoID(key);
        }

        internal OpPost GetPost(PostUID uid)
        {
            OpPost post = null;

            Posts.SafeTryGetValue(uid, out post);

            return post;
        }

        internal void UpdateReplies(OpPost parent)
        {
            // count replies to post, if greater than variable set, overwrite

            ushort replies = 0;

            Posts.LockReading(delegate()
            {
                foreach (OpPost post in Posts.Values)
                    if (post.Header.ParentID == parent.Header.PostID)
                        replies++;
            });

            if (replies > parent.Replies)
                parent.Replies = replies;
        }

    }

    internal class OpPost
    {
        internal int Ident;
        internal bool Unique;

        internal PostHeader Header;
        internal byte[] SignedHeader;

        internal PostInfo Info;
        internal List<PostFile> Attached;

        internal ushort Replies; // only parent uses this variable
    }
}
