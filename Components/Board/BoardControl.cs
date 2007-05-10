using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;

using DeOps.Components.Link;
using DeOps.Components.Transfer;
using DeOps.Components.Location;


namespace DeOps.Components.Board
{
    internal enum ScopeType {All, High, Low };
    internal enum BoardSearch { Threads, Time, Post };

    internal delegate void PostUpdateHandler(OpPost post);


    internal class BoardControl : OpComponent
    {
        internal OpCore Core;
        internal G2Protocol Protocol;
        internal DhtNetwork Network;
        internal DhtStore Store;
        LinkControl Links;

        internal string BoardPath;
        RijndaelManaged LocalFileKey;

        int PruneSize = 64;

        internal List<ulong> SaveHeaders = new List<ulong>();
        internal Dictionary<ulong, OpBoard> BoardMap = new Dictionary<ulong, OpBoard>();    
        internal Dictionary<ulong, List<BoardView>> WindowMap = new Dictionary<ulong, List<BoardView>>();
       
        Dictionary<int, ushort> SavedReplyCount = new Dictionary<int, ushort>();
        Dictionary<ulong, List<PostUID>> DownloadLater = new Dictionary<ulong, List<PostUID>>();

        internal PostUpdateHandler PostUpdate;


        internal BoardControl(OpCore core )
        {
            Core       = core;
            Core.Board = this;

            Protocol = Core.Protocol;
            Network  = Core.OperationNet;
            Store    = Network.Store;

            Core.LoadEvent += new LoadHandler(Core_Load);
            Core.TimerEvent += new TimerHandler(Core_Timer);

            Network.EstablishedEvent += new EstablishedHandler(Network_Established);

            Store.StoreEvent[ComponentID.Board] = new StoreHandler(Store_Local);
            Store.ReplicateEvent[ComponentID.Board] = new ReplicateHandler(Store_Replicate);
            Store.PatchEvent[ComponentID.Board] = new PatchHandler(Store_Patch);

            Network.Searches.SearchEvent[ComponentID.Board] = new SearchRequestHandler(Search_Local);

            if (Core.Sim != null)
                PruneSize = 32;
        }

        void Core_Load()
        {
            Links = Core.Links;
            Core.Transfers.FileSearch[ComponentID.Board] = new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[ComponentID.Board] = new FileRequestHandler(Transfers_FileRequest);

            LocalFileKey = Core.User.Settings.FileKey;

            BoardPath = Core.User.RootPath + "\\" + ComponentID.Board.ToString();

            if(!Directory.Exists(BoardPath))
                Directory.CreateDirectory(BoardPath);


            // get available board header targets
            string[] directories = Directory.GetDirectories(BoardPath);

            SortedDictionary<ulong, ulong> targets = new SortedDictionary<ulong, ulong>(); // key distance to self, value target

            foreach (string path in directories)
            {
                string dir = Path.GetFileName(path); // gets dir name

                ulong id = BitConverter.ToUInt64(Utilities.HextoBytes(dir), 0);

                targets[Core.LocalDhtID ^ id] = id;
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
        }

        void Core_Timer()
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
                

            // do below once per minute
            if (Core.TimeNow.Second != 0)
                return;

            // prune loaded boards
            if (BoardMap.Count > PruneSize)
            {
                List<ulong> removeBoards = new List<ulong>();

                List<ulong> localRegion = new List<ulong>();
                foreach (uint project in Core.Links.LocalLink.Projects)
                    localRegion.AddRange(GetBoardRegion(Core.LocalDhtID, project, ScopeType.All));

                foreach(OpBoard board in BoardMap.Values)
                    if (board.DhtID != Core.LocalDhtID &&
                        !Utilities.InBounds(board.DhtID, board.DhtBounds, Core.LocalDhtID) &&
                        !WindowMap.ContainsKey(board.DhtID) &&
                        !localRegion.Contains(board.DhtID))
                    {
                        removeBoards.Add(board.DhtID);
                    }

                while (removeBoards.Count > 0 && BoardMap.Count > PruneSize / 2)
                {
                    // find furthest id
                    ulong furthest = Core.LocalDhtID;

                    foreach (ulong id in removeBoards)
                        if ((id ^ Core.LocalDhtID) > (furthest ^ Core.LocalDhtID))
                            furthest = id;

                    // remove
                    OpBoard board = BoardMap[furthest];

                    try 
                    {
                        string dir = BoardPath + "\\" + Utilities.CryptFilename(LocalFileKey, board.DhtID.ToString());
                        string[] files = Directory.GetFiles(dir);

                        foreach(string path in files)
                            File.Delete(path);

                        Directory.Delete(dir);
                    }
                    catch { }

                    BoardMap.Remove(furthest);
                    removeBoards.Remove(furthest);
                }
            }
        }

        private void PruneMap(Dictionary<ulong, List<PostUID>> map)
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

            // set bounds for objects
            foreach (OpBoard board in BoardMap.Values)
            {
                board.DhtBounds = Store.RecalcBounds(board.DhtID);

                // republish objects that were not seen on the network during startup
                foreach(OpPost post in board.Posts.Values)
                    if (post.Unique && Utilities.InBounds(Core.LocalDhtID, localBounds, board.DhtID))
                        Store.PublishNetwork(board.DhtID, ComponentID.Board, post.SignedHeader);
            }


            // only download those objects in our local area
            foreach(ulong key in DownloadLater.Keys)
                if (Utilities.InBounds(Core.LocalDhtID, localBounds, key))
                    foreach(PostUID uid in DownloadLater[key])
                        PostSearch(key, uid, 0);
            
            DownloadLater.Clear();
        }

        private void LoadHeader(ulong id)
        {
            try
            {
                string path = GetTargetDir(id) + "\\" + Utilities.CryptFilename(LocalFileKey, "headers" + id.ToString());

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
                            if (embedded.Name == BoardPacket.PostHeader)
                                Process_PostHeader(null, signed, PostHeader.Decode(Core.Protocol, embedded));
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
            try
            {
                string tempPath = Core.GetTempPath();
                FileStream file = new FileStream(tempPath, FileMode.Create);
                CryptoStream stream = new CryptoStream(file, LocalFileKey.CreateEncryptor(), CryptoStreamMode.Write);

                lock (BoardMap)
                    if (BoardMap.ContainsKey(id))
                        foreach (OpPost post in BoardMap[id].Posts.Values)
                            stream.Write(post.SignedHeader, 0, post.SignedHeader.Length);

                stream.FlushFinalBlock();
                stream.Close();


                string finalPath = GetTargetDir(id) + "\\" + Utilities.CryptFilename(LocalFileKey, "headers" + id.ToString());
                File.Delete(finalPath);
                File.Move(tempPath, finalPath);
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Board", "Error saving board headers " + id.ToString() + " " + ex.Message);
            }
        }

        internal override List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong key, uint proj)
        {
            List<MenuItemInfo> menus = new List<MenuItemInfo>();

            if (menuType == InterfaceMenuType.Quick)
                return null;

            if (menuType == InterfaceMenuType.Internal)
                menus.Add(new MenuItemInfo("Comm/Board", new EventHandler(InternalMenu_View)));

            if (menuType == InterfaceMenuType.External)
                menus.Add(new MenuItemInfo("Board", new EventHandler(ExternalMenu_View)));

            return menus;
        }

        void InternalMenu_View(object sender, EventArgs args)
        {
            IContainsNode node = sender as IContainsNode;

            if (node == null)
                return;

            BoardView view = new BoardView(this, node.GetKey(), node.GetProject());

            Core.InvokeInterface(Core.GuiMain.ShowInternal, view);
        }

        void ExternalMenu_View(object sender, EventArgs args)
        {
            IContainsNode node = sender as IContainsNode;

            if (node == null)
                return;

            BoardView view = new BoardView(this, node.GetKey(), node.GetProject());

            Core.InvokeInterface(Core.GuiMain.ShowExternal, view );
        }

        internal void PostMessage(ulong id, uint project, uint parent, ScopeType scope, string subject, string message, List<AttachedFile> files, OpPost edit)
        {
            // post header
            PostHeader header = new PostHeader();
            
            header.Source = Core.User.Settings.KeyPublic;
            header.SourceID = Core.LocalDhtID;

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

            byte[] msgBytes = Core.Protocol.UTF.GetBytes(message);
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

            stream.FlushFinalBlock();
            stream.Close();

            // finish building header
            Utilities.ShaHashFile(tempPath, ref header.FileHash, ref header.FileSize);

            string finalPath = GetPostPath(header);
            File.Move(tempPath, finalPath);

            CachePost(new SignedData(Protocol, Core.User.Settings.KeyPair, header), header);

            // publish to network and local region of target
            Network.Store.PublishNetwork(header.TargetID, ComponentID.Board, GetPost(header).SignedHeader);

            List<LocationData> locations = new List<LocationData>();
            Links.GetLocs(header.TargetID, project, 1, 1, locations);
            Links.GetLocs(header.TargetID, project, 0, 1, locations);

            Store.PublishDirect(locations, header.TargetID, ComponentID.Board, GetPost(header).SignedHeader);


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

            return targetDir + "\\" + Utilities.CryptFilename(LocalFileKey, header.TargetID, ident);
        }

        internal string GetTargetDir(ulong id)
        {
            return BoardPath + "\\" + Utilities.BytestoHex(BitConverter.GetBytes(id));
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
                if (embedded.Name == BoardPacket.PostHeader)
                    Process_PostHeader(store, signed, PostHeader.Decode(Core.Protocol, embedded));
            }
        }

        private void Process_PostHeader(DataReq data, SignedData signed, PostHeader header)
        {
            Core.IndexKey(header.SourceID, ref header.Source);
            Core.IndexKey(header.TargetID, ref header.Target);
            Utilities.CheckSignedData(header.Source, signed.Data, signed.Signature);

            PostUID uid = new PostUID(header);

            // if link loaded
            if (BoardMap.ContainsKey(header.TargetID) && BoardMap[header.TargetID].Posts.ContainsKey(uid))
            {
                OpPost current = BoardMap[header.TargetID].Posts[uid];

                // lower version, send update
                if (header.Version < current.Header.Version)
                {
                    if (data != null && data.Sources != null)
                        foreach (DhtAddress source in data.Sources)
                            Store.Send_StoreReq(source, data.LocalProxy, new DataReq(null, header.TargetID, ComponentID.Board, current.SignedHeader));

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
                Core.InvokeInterface(PostUpdate, post);
                return;
            }

            // put into map
            if (!BoardMap.ContainsKey(header.TargetID))
                BoardMap[header.TargetID] = new OpBoard(header.Target);

            OpBoard board = BoardMap[header.TargetID];
            PostUID uid = new PostUID(header);

            post = new OpPost();
            post.Header = header;
            post.SignedHeader = signedHeader.Encode(Core.Protocol);
            post.Ident = header.TargetID.GetHashCode() ^ uid.GetHashCode();
            post.Unique = Core.Loading;

            // remove previous version of file
            
            if (board.Posts.ContainsKey(uid))
                try { File.Delete(GetPostPath(board.Posts[uid].Header)); }
                catch { }

            board.Posts[uid] = post;

            // update replies
            if (post.Header.ParentID == 0)
                board.UpdateReplies(post);
            else
            {
                PostUID parentUid = new PostUID(board.DhtID, post.Header.ProjectID, post.Header.ParentID);

                if (board.Posts.ContainsKey(parentUid))
                {
                    board.UpdateReplies(board.Posts[parentUid]);
                    Core.InvokeInterface(PostUpdate, board.Posts[parentUid]);
                }
            }

            lock (SaveHeaders)
                if (!SaveHeaders.Contains(header.TargetID))
                    SaveHeaders.Add(header.TargetID);

            lock (SavedReplyCount)
                if (SavedReplyCount.ContainsKey(post.Ident))
                {
                    post.Replies = SavedReplyCount[post.Ident];
                    SavedReplyCount.Remove(post.Ident);
                }

            Core.InvokeInterface(PostUpdate, post);
        }

        void DownloadPost(SignedData signed, PostHeader header)
        {
            FileDetails details = new FileDetails(ComponentID.Board, header.FileHash, header.FileSize, new PostUID(header).ToBytes());

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
            lock(BoardMap)
                if (BoardMap.ContainsKey(target))
                    if (BoardMap[target].Posts.ContainsKey(uid))
                        return BoardMap[target].Posts[uid];

            return null;
        }


        int PatchEntrySize = 8 + PostUID.SIZE + 2;

        ReplicateData Store_Replicate(DhtContact contact, bool add)
        {
            if (!Network.Established)
                return null;
            

            ReplicateData data = new ReplicateData(ComponentID.Board, PatchEntrySize);

            byte[] patch = new byte[PatchEntrySize];

            lock(BoardMap)
                foreach(OpBoard board in BoardMap.Values)
                    if(Utilities.InBounds(board.DhtID, board.DhtBounds, contact.DhtID))
                    {
                        // bounds is a distance value
                        DhtContact target = contact;
                        board.DhtBounds = Store.RecalcBounds(board.DhtID, add, ref target);

                        if (target != null)
                        {                      
                            BitConverter.GetBytes(board.DhtID).CopyTo(patch, 0);

                            foreach (PostUID uid in board.Posts.Keys)
                            {
                                uid.ToBytes().CopyTo(patch, 8);
                                BitConverter.GetBytes(board.Posts[uid].Header.Version).CopyTo(patch, 24);

                                data.Add(target, patch);
                            }
                        }
                    }

            return data;
        }

        void Store_Patch(DhtAddress source, ulong distance, byte[] data)
        {
            if (data.Length % PatchEntrySize != 0)
                return;

            int offset = 0;

            for (int i = 0; i < data.Length; i += PatchEntrySize)
            {
                ulong dhtid = BitConverter.ToUInt64(data, i);
                PostUID uid = PostUID.FromBytes(data, i + 8);
                ushort version = BitConverter.ToUInt16(data, i + 24);

                offset += PatchEntrySize;

                if (!Utilities.InBounds(Core.LocalDhtID, distance, dhtid))
                    continue;


                if (BoardMap.ContainsKey(dhtid) && BoardMap[dhtid].Posts.ContainsKey(uid))
                {
                    OpPost post = BoardMap[dhtid].Posts[uid];

                    // remote version is lower, send update
                    if (post.Header.Version > version)
                    {
                        Store.Send_StoreReq(source, 0, new DataReq(null, dhtid, ComponentID.Board, post.SignedHeader));
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
                    Network.Searches.SendDirectRequest(source, dhtid, ComponentID.Board, uid.ToBytes());
                else
                {
                    if (!DownloadLater.ContainsKey(dhtid))
                        DownloadLater[dhtid] = new List<PostUID>();

                    DownloadLater[dhtid].Add(uid);
                }
            }
        }

        internal void LoadView(BoardView view, ulong id)
        {
            if(!WindowMap.ContainsKey(id))
                WindowMap[id] = new List<BoardView>();

            WindowMap[id].Add(view);
        }

        internal void UnloadView(BoardView view, ulong id )
        {
            if(!WindowMap.ContainsKey(id))
                return;

            WindowMap[id].Remove(view);

            if(WindowMap[id].Count == 0)
                WindowMap.Remove(id);
        }

        internal void LoadRegion(ulong id, uint project)
        {
            // get all boards in local region
            List<ulong> targets = GetBoardRegion(id, project, ScopeType.All);


            foreach (ulong target in targets)
            {
                if (!BoardMap.ContainsKey(target))
                {
                    LoadHeader(target); // updateinterface called in processheader
                    continue;
                }

                // call update for all posts
                foreach (OpPost post in BoardMap[target].Posts.Values)
                    if (post.Header.ProjectID == project && post.Header.ParentID == 0)
                        Core.InvokeInterface(PostUpdate, post);
            }


            // searches
            foreach (ulong target in targets)
                SearchBoard(target, project);
        }

        internal void SearchBoard(ulong target, uint project)
        {
                bool fullSearch = true;

                if (BoardMap.ContainsKey(target))
                {
                    OpBoard board = BoardMap[target];

                    if(board.LastRefresh.ContainsKey(project))
                        fullSearch = false;
                }

                if (fullSearch)
                    ThreadSearch(target, project, 0);
                else // search for all theads posted since refresh, with an hour buffer
                    TimeSearch(target, project, BoardMap[target].LastRefresh[project].AddHours(-1));


                if(BoardMap.ContainsKey(target))
                    BoardMap[target].LastRefresh[project] = Core.TimeNow;
        }

        internal void LoadThread(OpPost parent)
        {
            if (!BoardMap.ContainsKey(parent.Header.TargetID))
                return;

            OpBoard board = BoardMap[parent.Header.TargetID];

            // have all replies fire an update
            foreach (OpPost post in board.Posts.Values)
                if (post.Header.ProjectID == parent.Header.ProjectID &&
                    post.Header.ParentID == parent.Header.PostID)
                    Core.InvokeInterface(PostUpdate, post);
            
            
            // do search for thread
            ThreadSearch(board.DhtID, parent.Header.ProjectID, parent.Header.PostID);
        }


        const int TheadSearch_ParamsSize = 9;   // type/project/parent  1 + 4 + 4
        const int TheadSearch_ResultsSize = 20; // UID/version/replies 16 + 2 + 2

        internal void ThreadSearch(ulong target, uint project, uint parent)
        {
            byte[] parameters = new byte[TheadSearch_ParamsSize];
            parameters[0] = (byte) BoardSearch.Threads;
            BitConverter.GetBytes(project).CopyTo(parameters, 1);
            BitConverter.GetBytes(parent).CopyTo(parameters, 5);
            
            DhtSearch search = Network.Searches.Start(target, "Board:Thread", ComponentID.Board, parameters, new EndSearchHandler(EndThreadSearch));

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
 
                if (BoardMap.ContainsKey(search.TargetID))
                    if (BoardMap[search.TargetID].Posts.ContainsKey(uid))
                    {
                        OpPost post = BoardMap[search.TargetID].Posts[uid];

                        if (post.Replies < replies)
                        {
                            post.Replies = replies;
                            Core.InvokeInterface(PostUpdate, post);
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

                    lock (SavedReplyCount)
                        if (!SavedReplyCount.ContainsKey(hash) || SavedReplyCount[hash] < replies)
                            SavedReplyCount[hash] = replies;
                }
            }
        }

        const int TimeSearch_ParamsSize = 13;   // type/project/time 1 + 4 + 8
        const int TimeSearch_ResultsSize = 18;  // UID/version 16 + 2

        private void TimeSearch(ulong target, uint project, DateTime time)
        {
            byte[] parameters = new byte[TimeSearch_ParamsSize];
            parameters[0] = (byte)BoardSearch.Time;
            BitConverter.GetBytes(project).CopyTo(parameters, 1);
            BitConverter.GetBytes(time.ToBinary()).CopyTo(parameters, 5);

            DhtSearch search = Network.Searches.Start(target, "Board:Time", ComponentID.Board, parameters, new EndSearchHandler(EndTimeSearch));

            if (search == null)
                return;

            search.TargetResults = 50;
        }

        void EndTimeSearch(DhtSearch search)
        {
            foreach (SearchValue found in search.FoundValues)
            {
                if (found.Value.Length < TheadSearch_ResultsSize)
                    continue;

                PostUID uid = PostUID.FromBytes(found.Value, 0);
                ushort version = BitConverter.ToUInt16(found.Value, 16);

                if (BoardMap.ContainsKey(search.TargetID))
                    if (BoardMap[search.TargetID].Posts.ContainsKey(uid))
                        if(BoardMap[search.TargetID].Posts[uid].Header.Version >= version)
                            continue;

                PostSearch(search.TargetID, uid, version);
            }
        }

        const int PostSearch_ParamsSize = 19;   // type/uid/version  1 + 16 + 2

        private void PostSearch(ulong target, PostUID uid, ushort version)
        {
            byte[] parameters = new byte[PostSearch_ParamsSize];
            parameters[0] = (byte)BoardSearch.Post;
            uid.ToBytes().CopyTo(parameters, 1);
            BitConverter.GetBytes(version).CopyTo(parameters, 17);
            
            DhtSearch search = Core.OperationNet.Searches.Start(target, "Board:Post", ComponentID.Board, parameters, new EndSearchHandler(EndPostSearch));

            if (search == null)
                return;

            search.TargetResults = 2;
        }

        void EndPostSearch(DhtSearch search)
        {
            foreach (SearchValue found in search.FoundValues)
                Store_Local(new DataReq(found.Sources, search.TargetID, ComponentID.Board, found.Value));
        }

        List<byte[]> Search_Local(ulong key, byte[] parameters)
        {
            List<Byte[]> results = new List<byte[]>();

            if (!BoardMap.ContainsKey(key) || parameters == null)
                return null;

            // thread search
            if (parameters[0] == (byte)BoardSearch.Threads)
            {
                if (parameters.Length < TheadSearch_ParamsSize)
                    return null;

                uint project = BitConverter.ToUInt32(parameters, 1);
                uint parent = BitConverter.ToUInt32(parameters, 5);

                foreach (OpPost post in BoardMap[key].Posts.Values)
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
            }

            // time search
            else if (parameters[0] == (byte)BoardSearch.Time)
            {
                if (parameters.Length < TimeSearch_ParamsSize)
                    return null;

                uint project = BitConverter.ToUInt32(parameters, 1);
                DateTime time = DateTime.FromBinary(BitConverter.ToInt64(parameters, 5));

                 foreach (OpPost post in BoardMap[key].Posts.Values)
                     if (post.Header.ProjectID == project && post.Header.Time > time)
                     {
                         byte[] result = new byte[TimeSearch_ResultsSize];
                         new PostUID(post.Header).ToBytes().CopyTo(result, 0);
                         BitConverter.GetBytes(post.Header.Version).CopyTo(result, 16);

                         results.Add(result);
                     }
            }

            // post search
            else if (parameters[0] == (byte)BoardSearch.Post)
            {
                if (parameters.Length < PostSearch_ParamsSize)
                    return null;

                PostUID uid = PostUID.FromBytes(parameters, 1);
                ushort version = BitConverter.ToUInt16(parameters, 17);

                if (BoardMap[key].Posts.ContainsKey(uid))
                    if(BoardMap[key].Posts[uid].Header.Version == version)
                        results.Add(BoardMap[key].Posts[uid].SignedHeader);
            }

            return results;
        }

        internal List<ulong> GetBoardRegion(ulong id, uint project, ScopeType scope)
        {
            List<ulong> targets = new List<ulong>();

            targets.Add(id); // need to include self in high and low scopes, for re-searching, onlinkupdate purposes

            if (!Core.Links.LinkMap.ContainsKey(id))
                return targets;

            OpLink link = Core.Links.LinkMap[id];

            // get parent and children of parent
            if(scope != ScopeType.Low)
                if (link.Uplink.ContainsKey(project))
                {
                    OpLink parent = link.Uplink[project];

                    if (parent.Projects.Contains(project) &&
                        parent.Confirmed[project].Contains(link.DhtID))
                    {
                        targets.Add(parent.DhtID);

                        if (parent.Downlinks.ContainsKey(project))
                            foreach (OpLink child in parent.Downlinks[project])
                                if (parent.Confirmed[project].Contains(child.DhtID) && child.DhtID != id)
                                    targets.Add(child.DhtID);
                    }
                }

            // get children of self
            if (scope != ScopeType.High) 
                if (link.Downlinks.ContainsKey(project))
                    foreach (OpLink child in link.Downlinks[project])
                        if(link.Confirmed[project].Contains(child.DhtID))
                            targets.Add(child.DhtID);

            return targets;
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

                    FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    CryptoStream crypto = new CryptoStream(file, post.Header.FileKey.CreateDecryptor(), CryptoStreamMode.Read);
                    PacketStream stream = new PacketStream(crypto, Core.Protocol, FileAccess.Read);

                    G2Header root = null;

                    while (stream.ReadPacket(ref root))
                    {
                        if (root.Name == BoardPacket.PostInfo)
                            post.Info = PostInfo.Decode(Core.Protocol, root);

                        else if (root.Name == BoardPacket.PostFile)
                            post.Attached.Add(PostFile.Decode(Core.Protocol, root));
                    }

                    stream.Close();
                }
                catch (Exception ex)
                {
                    Network.UpdateLog("Board", "Could not load post " + post.Header.SourceID.ToString() + ": " + ex.Message);
                }

            
            return (post.Info != null) ? post.Info.Subject : "";
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
        internal ulong DhtID;
        internal ulong DhtBounds = ulong.MaxValue;
        internal byte[] Key;    // make sure reference is the same as main key list

        internal Dictionary<PostUID, OpPost> Posts = new Dictionary<PostUID, OpPost>();
        internal Dictionary<uint, DateTime> LastRefresh = new Dictionary<uint, DateTime>();
        

        internal OpBoard(byte[] key)
        {
            Key = key;
            DhtID = Utilities.KeytoID(key);
        }

        internal void UpdateReplies(OpPost parent)
        {
            // count replies to post, if greater than variable set, overwrite

            ushort replies = 0;

            foreach (OpPost post in Posts.Values)
                if (post.Header.ParentID == parent.Header.PostID)
                    replies++;

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
