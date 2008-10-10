using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Linq;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Protocol.Special;
using RiseOp.Implementation.Transport;
using RiseOp.Services.Location;


namespace RiseOp.Services.Transfer
{
    internal delegate void EndDownloadHandler(string path, object[] args);
    internal delegate bool FileSearchHandler(ulong key, FileDetails details);
    internal delegate string FileRequestHandler(ulong key, FileDetails details);


    internal class TransferService : OpService
    {
        public string Name { get { return "Transfer"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.Transfer; } }

        internal OpCore Core;
        DhtNetwork Network;

        int ConcurrentDownloads = 12;
        internal LinkedList<OpTransfer> Partials = new LinkedList<OpTransfer>(); // 0 peer incomplete
        internal LinkedList<OpTransfer> Pending = new LinkedList<OpTransfer>(); // untried or >0 peer incomplete
     
        // all transfers complete and incomplete and inactive partials
        internal int ActiveUploads;
        internal int NeedUploadWeight;

        // users who we've accepted downloads from, only exists for length of connection
        internal Dictionary<ulong, DownloadPeer> DownloadPeers = new Dictionary<ulong, DownloadPeer>(); // routing, dl
        
        // state of uploads to differnt peers in our transfer map, enforces 1 upload per client (even if queued for multiple files)
        internal Dictionary<ulong, UploadPeer> UploadPeers = new Dictionary<ulong, UploadPeer>(); // routing, info - peers that have requested an upload (ping)
        
        // downloads/uploads are all treated the same (a transfer) which exists as long as a file is wanted
        // only files in this list are (active) we actively are distributing them to peers
        internal Dictionary<ulong, OpTransfer> Transfers = new Dictionary<ulong, OpTransfer>(); // file id, transfer

        internal ServiceEvent<FileSearchHandler> FileSearch = new ServiceEvent<FileSearchHandler>();
        internal ServiceEvent<FileRequestHandler> FileRequest = new ServiceEvent<FileRequestHandler>();
        
        internal string TransferPath;

        LinkedList<DateTime> RecentPings = new LinkedList<DateTime>();

        internal SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();

        string PartialHeaderPath = "";

        internal bool Logging = false; //crit - turn off


        internal TransferService(OpCore core)
        {
            Core = core;
            Network = Core.Network;
            Core.Transfers = this;

            Core.SecondTimerEvent += new TimerHandler(Core_SecondTimer);

            Network.Searches.SearchEvent[ServiceID, 0] += new SearchRequestHandler(Search_Local);

            Network.RudpControl.SessionUpdate += new SessionUpdateHandler(Session_Update);
            Network.RudpControl.SessionData[ServiceID, 0] += new SessionDataHandler(Session_Data);
            Network.RudpControl.KeepActive += new KeepActiveHandler(Session_KeepActive);

            Network.LightComm.Data[ServiceID, 0] += new LightDataHandler(LightComm_ReceiveData);


            if(core.Sim != null)
                ConcurrentDownloads = 6;
          

            // create and clear transfer dir
            try
            {
                TransferPath = Core.User.RootPath + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + ServiceID.ToString();
                Directory.CreateDirectory(TransferPath);

                PartialHeaderPath = TransferPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(Core, "PartialFileHeaders");

                LoadPartials();

                // remove lingering files that are not either a partial or the partial info header
                string[] files = Directory.GetFiles(TransferPath);

                foreach (string path in files)
                    if (path.CompareTo(PartialHeaderPath) == 0)
                        continue;
                    else if (Partials.Count(p => path.CompareTo(p.FilePath) == 0) > 0)
                        continue;
                    else
                    {
                        try { File.Delete(path); }
                        catch { }
                    }
            }
            catch { }
        }

        public void Dispose()
        {
            Core.SecondTimerEvent -= new TimerHandler(Core_SecondTimer);

            Core.Network.Searches.SearchEvent[ServiceID, 0] -= new SearchRequestHandler(Search_Local);

            Network.RudpControl.SessionUpdate -= new SessionUpdateHandler(Session_Update);
            Network.RudpControl.SessionData[ServiceID, 0] -= new SessionDataHandler(Session_Data);
            Network.RudpControl.KeepActive -= new KeepActiveHandler(Session_KeepActive);

            Network.LightComm.Data[ServiceID, 0] -= new LightDataHandler(LightComm_ReceiveData);

            foreach (OpTransfer transfer in Pending.Concat(Partials).Concat(Transfers.Values))
                transfer.Dispose();
        }

        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong user, uint project)
        {
        }

        public void SimTest()
        {
        }

        public void SimCleanup()
        {
        }

        internal OpTransfer StartDownload( ulong target, FileDetails details, object[] args, EndDownloadHandler endEvent)
        {
            if (Core.InvokeRequired)
                Debug.Assert(false);

            // files isolated (use different enc keys) between services to keep compartmentalization strong

            ulong id = OpTransfer.GetFileID(details);

            // if file in partials list, move to pending
            var partials = Partials.Where(t => t.FileID == id).ToArray();

            if (partials.Length > 0)
            {
                OpTransfer transfer = partials[0];

                transfer.SavePartial = true;
                transfer.Args = args;
                transfer.EndEvent = endEvent;

                if(transfer.LocalBitfield[transfer.LocalBitfield.Length -1])
                    transfer.LoadSubhashes(); // if we have the last piece, load sub hashes

                Partials.Remove(transfer);
                Pending.AddLast(transfer);

                return transfer;
            }


            // if file already added to pending or transfers list, return
            OpTransfer existing = Pending.Where(p => p.FileID == id).Concat(
                                  Transfers.Values.Where(t => t.FileID == id)).FirstOrDefault();

            if (existing != null)
                return existing;


            string path = TransferPath + Path.DirectorySeparatorChar + 
                          Utilities.CryptFilename(Core, id, details.Hash);

            OpTransfer pending = new OpTransfer(this, path, target, details, TransferStatus.Empty, args, endEvent);

            Pending.AddLast(pending);

            return pending;
        }

        internal void CancelDownload(uint service, byte[] hash, long size)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreBlocked(delegate() { CancelDownload(service, hash, size); });
                return;
            }

            ulong id = OpTransfer.GetFileID(service, hash, size);

            foreach (OpTransfer pending in (from p in Pending 
                                             where p.FileID == id 
                                             select p).ToArray())
            {
                Pending.Remove(pending);
                return;
            }

            if (!Transfers.ContainsKey(id))
                return;

            OpTransfer transfer = Transfers[id];
            transfer.SavePartial = false;
            Transfers.Remove(id);
            
            transfer.Dispose();
        }

        internal string GetDownloadStatus(uint service, byte[] hash, long size)
        {
            ulong id = OpTransfer.GetFileID(service, hash, size);

            if ((from p in Pending where p.FileID == id select p).Count() > 0)
                return "Pending";

            if (!Transfers.ContainsKey(id))
                return null;

            OpTransfer transfer = Transfers[id];

            if (transfer.Status == TransferStatus.Complete)
                return null; 

            int active = 0;
            foreach (RemotePeer peer in transfer.Peers.Values)
                if (DownloadPeers.ContainsKey(peer.RoutingID))
                    if (DownloadPeers[peer.RoutingID].Requests.ContainsKey(transfer.FileID))
                        active++;

            long progress = transfer.GetProgress() * 100 / transfer.Details.Size;

            if (progress == 0 && transfer.Searching)
                return "Searching...";

            if (progress == 0 && transfer.Peers.Count == 0)
                return "No sources found";

            string text = "";

            if (active == 0)
            {
                text = progress + "% complete, " + transfer.Peers.Count + " source";
                return (transfer.Peers.Count != 1) ? text + "s" : text;
            }

            text = "Downloading, " + progress + "% complete from " + active + " of " + transfer.Peers.Count + " source";
            return (transfer.Peers.Count != 1) ? text + "s" : text;
        }

        void Core_SecondTimer()
        {
            if (!Network.Established)
                return;

            int active = (from t in Transfers.Values
                          where t.Status != TransferStatus.Complete
                          select t).Count();

            // move downloads from pending to active
            if (active < ConcurrentDownloads && Pending.Count > 0)
            {
                OpTransfer transfer = Pending.First.Value;
                Pending.RemoveFirst();

                transfer.LastDataReceived = Core.TimeNow; // reset
                foreach (RemotePeer peer in transfer.Peers.Values)
                {
                    peer.LastSeen = Core.TimeNow; // prevent a pending going back to active after a while from having its peers auto-deleted
                    peer.AddUploadEntry(); // re-add peer to potential upload list
                }

                Transfers[transfer.FileID] = transfer;
             


                byte[] parameters = transfer.Details.Encode(Network.Protocol);

                DhtSearch search = Core.Network.Searches.Start(transfer.Target, "Transfer", ServiceID, 0, parameters, new EndSearchHandler(EndSearch));

                if (search != null)
                {
                    transfer.Searching = true;
                    search.Carry = transfer;
                }
            }

            // only ping if transfer is incomplete
            // remove dead peers / transfers
            ulong localID = Core.Network.Routing.LocalRoutingID;
            Dictionary<ulong, List<OpTransfer>> pingLocs = new Dictionary<ulong, List<OpTransfer>>(); // routing id, file id lsit

            // send pings, expire old peers/transfers
            foreach (OpTransfer transfer in Transfers.Values)
            {
                // remove dead peers
                foreach (ulong id in (from peer in transfer.Peers.Values
                                      where Core.TimeNow > peer.Timeout &&
                                            !peer.Active()
                                      select peer.RoutingID).ToArray())
                    transfer.RemovePeer(id);
                    

                // if we are completed, we don't need completed sources in our mesh 
                // (hangs mesh as well, peer wont be removed and hence transfer wont be removed)
                if(transfer.Status == TransferStatus.Complete)
                    foreach (ulong id in (from peer in transfer.Peers.Values
                                          where peer.Status == TransferStatus.Complete &&
                                                !peer.Active()
                                          select peer.RoutingID).ToArray())
                        transfer.RemovePeer(id);

                // trim peers to 16 closest, remove furthest from local
                if (transfer.Peers.Count > OpTransfer.MaxPeers)
                {
                    // remove furthest peer, not in the routing table and not active
                    var x = (from p in transfer.Peers.Values
                             orderby p.RoutingID ^ localID descending // furthest first
                             where !transfer.RoutingTable.Contains(p) &&
                                   !p.Active()
                             select p.RoutingID).Take(transfer.Peers.Count - OpTransfer.MaxPeers).ToArray();

                    foreach (ulong id in x)
                        transfer.RemovePeer(id);
                 
                    // transfer keeps a mini-dht so that entire download mesh is always pinging each other
                    // uploads still preference closer, so replication is faster, but further references aren't lost
                    // if peers were just closest, mesh would become desynched

                    // the original problem (mesh desynch)
                    // will still persist with 64 size network of all same user ID
                }

                // imcomplete transfers ping their sources to let them know to send us data
                if (transfer.Status != TransferStatus.Complete)
                {
                    // ping peer most over due
                    RemotePeer peer = (from p in transfer.Peers.Values
                                       where Core.TimeNow > p.NextPing
                                       orderby Core.TimeNow.Subtract(p.NextPing) descending
                                       select p).FirstOrDefault();

                    if (peer != null)
                        Send_Ping(transfer, peer);
                }

                // completed rely on remotes to send pings to keep transfer loaded
            }


            // 0 peers, and incomplete move to partials list
            foreach (OpTransfer transfer in (from t in Transfers.Values
                                             where   t.Peers.Count == 0 && 
                                                    !t.Searching &&
                                                     t.Status == TransferStatus.Incomplete
                                             select t).ToArray())
            {
                MoveTransferTo(Partials, transfer);
            }

            // remove dead empty and complete transfers with 0 peers (no one interested)
            foreach (OpTransfer transfer in (from t in Transfers.Values
                                             where t.Peers.Count == 0 && 
                                                   (( !t.Searching && t.Status == TransferStatus.Empty) || t.Status == TransferStatus.Complete)
                                             select t).ToArray())
            {
                transfer.Dispose();
                Transfers.Remove(transfer.FileID);
            }

            // move stalled downloads back to end of partials list (remove download / uploads)
            if (active >= ConcurrentDownloads && Pending.Count > 0)
                foreach (OpTransfer stalled in (from t in Transfers.Values
                                                where t.Status == TransferStatus.Incomplete &&
                                                      Core.TimeNow > t.LastDataReceived.AddMinutes(3)
                                                select t).ToArray())
                {
                    MoveTransferTo(Pending, stalled);
                }

            // clean download peers
            foreach (ulong id in (from download in DownloadPeers.Values
                                  where download.Requests.Count == 0
                                  select download.Client.RoutingID).ToArray())
                DownloadPeers.Remove(id);

            
            // let context know that we need to upload a piece
            if (Transfers.Count > 0)
                NeedUploadWeight++;

            ActiveUploads = UploadPeers.Values.Count(p => p.Active != null);


            // save partials every 20 seconds
            if (Core.TimeNow.Second % 15 == 0)
                SavePartials();
        }

        private void MoveTransferTo(LinkedList<OpTransfer> list, OpTransfer transfer)
        {
            Transfers.Remove(transfer.FileID);

            // remove peers from transfers, but dont delete them
            foreach (RemotePeer peer in transfer.Peers.Values)
                peer.Clean(); // removes from up/down list

            list.AddLast(transfer);
        }

        void SavePartials()
        {
            try
            {
                string tempPath = Core.GetTempPath();
                using (IVCryptoStream crypto = IVCryptoStream.Save(tempPath, Core.User.Settings.FileKey))
                {
                    PacketStream stream = new PacketStream(crypto, Network.Protocol, FileAccess.Write);

                    // write each incomplete transfer, from pending and transfers
                    Func<OpTransfer, bool> shouldSave = (t => t.SavePartial && t.Status == TransferStatus.Incomplete);

                    var save = Pending.Where(shouldSave).
                        Concat(Transfers.Values.Where(shouldSave)).
                        Concat(Partials.Where(shouldSave)); // sharing service will perserve certain partials

                    foreach (OpTransfer transfer in save)
                        stream.WritePacket(new TransferPartial(transfer));

                    crypto.FlushFinalBlock();
                }

                File.Copy(tempPath, PartialHeaderPath, true);
                File.Delete(tempPath);
            }
            catch (Exception ex)
            {
                Core.Network.UpdateLog("Transfers", "Error saving partials " + ex.Message);
            }
        }

        void LoadPartials()
        {
            // if transfer found in start download
            // set args and endevent for transfer (if endevent is null, TEST)
            // load sub hashes

            // how do clean up commands in others services not delete the primary header file

            // after 10 mintues of network established, set save partial of those loaded from file to false

            try
            {
                if (!File.Exists(PartialHeaderPath))
                    return;

                using (IVCryptoStream crypto = IVCryptoStream.Load(PartialHeaderPath, Core.User.Settings.FileKey))
                {
                    PacketStream stream = new PacketStream(crypto, Network.Protocol, FileAccess.Read);

                    G2Header root = null;

                    while (stream.ReadPacket(ref root))
                        if (root.Name == TransferPacket.Partial)
                        {
                            TransferPartial partial = TransferPartial.Decode(root);

                            ulong id = OpTransfer.GetFileID(partial.Details);

                            string path = TransferPath + Path.DirectorySeparatorChar +
                                        Utilities.CryptFilename(Core, id, partial.Details.Hash);

                            if (!File.Exists(path))
                                continue;

                            OpTransfer transfer = new OpTransfer(this, path, partial.Target, partial.Details, TransferStatus.Incomplete, null, null);

                            transfer.Created = partial.Created;
                            transfer.InternalSize = partial.InternalSize;
                            transfer.ChunkSize = partial.ChunkSize;
                            transfer.LocalBitfield = partial.Bitfield;
                            transfer.SavePartial = false; // reset when this current instance utilizes partial file

                            // subhashes loaded when partial goes to pending

                            Partials.AddLast(transfer);
                        }
                }
            }
            catch (Exception ex)
            {
                Core.Network.UpdateLog("Transfers", "Error loading partials " + ex.Message);
            }
        }

        void Search_Local(ulong key, byte[] parameters, List<byte[]> results)
        {
            if (parameters == null)
            {
                Core.Network.UpdateLog("Transfers", "Search Recieved with null parameters");
                return;
            }

            FileDetails details = FileDetails.Decode(parameters);

            if (details == null || Core.Locations.LocalClient == null)
                return;

            // reply with loc info if a component has the file
            if(FileSearch.Contains(details.Service, details.DataType))
                if (FileSearch[details.Service, details.DataType].Invoke(key, details))
                {
                    results.Add(Core.Locations.LocalClient.Data.EncodeLight(Network.Protocol));
                    return;
                }

            // search if file is partial - only reply if we have at least one piece and know someone with 100% of original
            foreach (OpTransfer transfer in Transfers.Values)
                if (transfer.Status != TransferStatus.Empty && 
                    transfer.Peers.Values.Count(p => p.Status == TransferStatus.Complete) > 0 && 
                    transfer.Details.Equals(details))
                {
                    results.Add(Core.Locations.LocalClient.Data.EncodeLight(Network.Protocol));
                    return;
                }
        }

        void EndSearch(DhtSearch search)
        {
            OpTransfer transfer = search.Carry as OpTransfer;

            if (transfer == null)
                return;

            transfer.Searching = false;

            foreach (SearchValue found in search.FoundValues)
            {
                try
                {
                    LocationData location = LocationData.Decode(found.Value);
                    // dont core.indexkey because key not sent in light location

                    Network.LightComm.Update(location); // primes all loc's addresses

                    transfer.AddPeer(location.Source);
                }
                catch (Exception ex)
                {
                    Core.Network.UpdateLog("Transfer", "Search Results error " + ex.Message);
                }
            }
            
        }

        internal void StartUpload()
        {
            // this method ensures that if one host has multiple files queued they are completed sequentially

            // pref long waited host in top 8 not completed, local incomplete, most completed remote file, something we have a piece for, select rarest (when transfer begins)
            ulong localID = Core.Network.Routing.LocalRoutingID;
            List<RemotePeer> allPeers = new List<RemotePeer>();

            // pref 8 closest incomplete nodes of each transfer
            foreach (OpTransfer transfer in Transfers.Values)
                if (transfer.Status != TransferStatus.Empty) // cant upload piece if we dont have any
                    allPeers.AddRange((from p in transfer.Peers.Values
                                       where p.Status != TransferStatus.Complete // dont upload to someone who alredy has the whole file
                                       orderby p.RoutingID ^ localID
                                       select p).Take(8)); // only transfer to 8 closest in mesh that need a piece

            allPeers = (from p in allPeers
                        where UploadPeers[p.RoutingID].Active == null && p.CanSendPeice()
                        select p).ToList();

            RemotePeer selected = (from p in allPeers // the order of these orderbys is very important
                                   orderby p.BlocksUntilFinished // pref file closest to completion
                                   orderby UploadPeers[p.RoutingID].LastAttempt // pref peer thats been waiting the longest
                                   //orderby (int)p.Transfer.Status // pref sending local incomplete over complete
                                   select p).FirstOrDefault();


            if (selected == null)
                return;


            Debug.Assert(selected.UploadData == false);
            Debug.Assert(selected.Transfer.GetProgress() != 0);

            UploadPeer upload = UploadPeers[selected.RoutingID];
            Debug.Assert(upload.Active == null);
            upload.Active = selected;
            upload.LastAttempt = Core.TimeNow;
            selected.LastError = null;

            // if connected to source
            RudpSession session = Network.RudpControl.GetActiveSession(upload.Active.Client);

            if (session != null)
                Send_Request(session, upload.Active);

            else if (!Network.RudpControl.Connect(upload.Active.Client))
                upload.Active = null;
        }

        void StopUpload(RemotePeer peer, bool deletePeer, string reason)
        {
            ulong id = peer.RoutingID;

            if (UploadPeers.ContainsKey(id) &&
                    UploadPeers[id].Active == peer)
            {
                UploadPeers[id].Active = null;
                ActiveUploads--;
            }

            peer.UploadAttempts = 0;
            peer.LastRequest = null;
            peer.LastError = reason;
            peer.UploadData = false;
            peer.CurrentPos = 0;

            if (Logging) peer.DebugUpload += "Upload Stopped\r\n";

            // removes from upload list as well
            if (deletePeer)
                peer.Transfer.RemovePeer(id);

            Core.Context.AssignUploadSlots();
        }

        void LightComm_ReceiveData(DhtClient client, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (G2Protocol.ReadPacket(root))
            {
                if (root.Name == TransferPacket.Ping)
                    Receive_Ping(client, TransferPing.Decode(root));

                if (root.Name == TransferPacket.Pong)
                    Receive_Pong(client, TransferPong.Decode(root));
            }

        }

        void Session_Data(RudpSession session, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (G2Protocol.ReadPacket(root))
            {
                switch (root.Name)
                {
                    case TransferPacket.Request:
                        Receive_Request(session, TransferRequest.Decode(root));
                        break;

                    case TransferPacket.Ack:
                        Receive_Ack(session, TransferAck.Decode(root));
                        break;

                    case TransferPacket.Data:
                        Receive_Data(session, TransferData.Decode(root));
                        break;

                    case TransferPacket.Stop:
                        Receive_Stop(session, TransferStop.Decode(root));
                        break;

                    case TransferPacket.Pong:
                        TransferPong pong = TransferPong.Decode(root);
                        OpTransfer transfer;
                        if (Transfers.TryGetValue(pong.FileID, out transfer) && transfer.LocalBitfield == null)
                        {
                            transfer.InternalSize = pong.InternalSize;
                            transfer.ChunkSize = pong.ChunkSize;
                            transfer.LocalBitfield = new BitArray(pong.BitCount);
                        }
                        break;
                }
            }
        }

        void Session_Update(RudpSession session)
        {
            DhtClient client = new DhtClient(session.UserID, session.ClientID);

            if (session.Status == SessionStatus.Closed && DownloadPeers.ContainsKey(client.RoutingID))
            {
                if (Logging)
                    foreach (ulong fileID in DownloadPeers[client.RoutingID].Requests.Keys)
                        if (Transfers.ContainsKey(fileID))
                            if (Transfers[fileID].Peers.ContainsKey(client.RoutingID))
                                Transfers[fileID].Peers[client.RoutingID].DebugDownload += "Closed\r\n";

                DownloadPeers.Remove(client.RoutingID);
            }

            UploadPeer upload;
            if (UploadPeers.TryGetValue(client.RoutingID, out upload) && upload.Active != null)
            {

                // if session closed, remove from active list, check if we should transfer to someone else
                if (session.Status == SessionStatus.Closed)
                {
                    if (Logging) upload.Active.DebugUpload += "Closed\r\n";
                    StopUpload(upload.Active, false, "Session: " + session.CloseMsg);
                }

                // a node goes active when it is decided that we want to send that node a piece
                else if (session.Status == SessionStatus.Active)
                {
                    if (Logging) upload.Active.DebugUpload += "Connected\r\n";
                    Send_Request(session, upload.Active);
                }
            }
        }

        void Session_KeepActive(Dictionary<ulong, bool> active)
        {
            foreach(UploadPeer upload in UploadPeers.Values)
                if(upload.Active != null)
                    active[upload.Client.UserID] = true;

            foreach(DownloadPeer download in DownloadPeers.Values)
                active[download.Client.UserID] = true;
        }

        private void Send_Ping(OpTransfer transfer, RemotePeer peer)
        {
            peer.NextPing = Core.TimeNow.AddSeconds(peer.PingTimeout);

            // why decided to not group pings to the same location -
            // send smaller ping packets, if host unreachable, less bandwidth used to find out
            // done so transfer pings aren't held up, more likely to get through
            // move overhead for single host we're transferring multiple from, but rare we transfer multiple from same
            // simplifies code, simplier is better
            // ping throttle used so host gets less big pings, more controllable little pings

            TransferPing ping = new TransferPing();
            ping.Target = transfer.Target;
            ping.Details = transfer.Details;
            ping.Status = transfer.Status;

            int missingDepth = -1;
            for(int i = 0; i < transfer.RoutingTable.Length; i++)
                if (transfer.RoutingTable[i] == null)
                {
                    missingDepth = i;
                    break;
                }

            ping.MissingDepth = missingDepth;

            if (peer.FirstPing)
            {
                ping.RequestAlts = true;
                peer.FirstPing = false;
            }

            if (peer.LocalBitfieldUpdated)
            {
                ping.BitfieldUpdated = true;
                peer.LocalBitfieldUpdated = false;
            }

            if (transfer.InternalSize == 0)
                ping.RequestInfo = true;


            Network.LightComm.SendPacket(peer.Client, ServiceID, 0, ping);
        }

        void Receive_Ping(DhtClient client, TransferPing ping)
        {
            // remote must be incomplete to recv ping, this is basically an upload request


            ulong fileID = OpTransfer.GetFileID(ping.Details);


            OpTransfer transfer = null;

            // check if loaded
            if (Transfers.ContainsKey(fileID))
                transfer = Transfers[fileID];

            else
            {
                // ask component for path to file
                string path = null;

                if (FileRequest.Contains(ping.Details.Service, ping.Details.DataType))
                    path = FileRequest[ping.Details.Service, ping.Details.DataType].Invoke(ping.Target, ping.Details);

                if (path != null && File.Exists(path))
                {
                    transfer = new OpTransfer(this, path, ping.Target, ping.Details, TransferStatus.Complete, null, null);

                    try
                    {
                        using (FileStream file = File.OpenRead(path))
                        {
                            file.Seek(-8, SeekOrigin.End);

                            byte[] sizeBytes = new byte[8];
                            file.Read(sizeBytes, 0, sizeBytes.Length);
                            transfer.InternalSize = BitConverter.ToInt64(sizeBytes, 0);

                            // we have the whole file already, we don't need to load sub-hashes because
                            // there's nothing we need to verify, load up the internal size, chunk size, and
                            // bit count to send to the client
                            file.Seek(transfer.InternalSize, SeekOrigin.Begin);

                            PacketStream stream = new PacketStream(file, Network.Protocol, FileAccess.Read);

                            G2Header root = null;
                            while (stream.ReadPacket(ref root))
                                if (root.Name == FilePacket.SubHash)
                                {
                                    SubHashPacket info = SubHashPacket.Decode(root);

                                    transfer.ChunkSize = info.ChunkSize;

                                    transfer.LocalBitfield = new BitArray(info.TotalCount + 1); // 1 for the subhash area
                                    transfer.LocalBitfield.SetAll(true); // we have all pieces

                                    break; // dont need to load actual hashes
                                }

                        }

                        Transfers[fileID] = transfer;
                    }
                    catch (Exception ex)
                    {
                        transfer = null;
                        Network.UpdateLog("Transfers", "Error loading file: " + ex.Message);
                    }
                }
            }


            RecentPings.AddFirst(Core.TimeNow);
            while (RecentPings.Count > 30)
                RecentPings.RemoveLast();


            TransferPong pong = new TransferPong();
            pong.FileID = fileID;

            if (transfer == null)
            {
                pong.Error = true;
                Network.LightComm.SendPacket(client, ServiceID, 0, pong);
                return;
            }


            pong.Status = transfer.Status;

            // add remote as peer
            RemotePeer peer = transfer.AddPeer(client);
            peer.Status = ping.Status;

            if (ping.BitfieldUpdated)
                peer.RemoteBitfieldUpdated = true;

            // we want a target of 20 pings per minute ( 1 every 3 seconds)
            // pings per minute = RecentPings.count / (Core.TimeNow - RecentPings.Last).ToMinutes()
            float pingsPerMinute = (float)RecentPings.Count / (float)(Core.TimeNow - RecentPings.Last.Value).Minutes;
            pong.Timeout = (int)(60.0 * pingsPerMinute / 20.0); // 20 is target rate, so if we have 40ppm, multiplier is 2, timeout 120seconds
            pong.Timeout = Math.Max(60, pong.Timeout); // use 60 as lowest timeout


            peer.Uninitialized = ping.RequestInfo;
            if (peer.Uninitialized && transfer.LocalBitfield != null)
            {
                pong.InternalSize = transfer.InternalSize;
                pong.ChunkSize = transfer.ChunkSize;
                pong.BitCount = transfer.LocalBitfield.Count;
            }


            //if haven't sent alts - send random 3 alts (upon first contact will send more alts)
            if (ping.RequestAlts)
            {
                // select 3 random peers foreach peer get top 3 addresses from lightComm
                foreach (RemotePeer alt in (from p in transfer.Peers.Values
                                            where p != peer
                                            orderby Core.RndGen.Next()
                                            select p).Take(3).ToArray())
                {
                    List<DhtAddress> addresses = Network.LightComm.GetAddresses(alt.RoutingID);

                    if (addresses != null)
                        pong.Alts[alt.Client] = addresses;
                }
            }

            RemotePeer altPeer = null;

            // if remote is missing a spot in it't table, see if we have it
            if (ping.MissingDepth >= 0)
            {
                // for example if user is missing depth 0, we flip the 0 bit and
                // find any matches with 0 as the first bit - other side of the network

                ulong match = Utilities.FlipBit(client.RoutingID, ping.MissingDepth);

                altPeer = (from p in transfer.Peers.Values
                           where p != peer &&
                                 IsDepthMatched(match, p.RoutingID, ping.MissingDepth)
                           orderby Core.RndGen.Next()
                           select p).FirstOrDefault();

                //if (altPeer != null)
                //    TestMatchDepth(client.RoutingID, altPeer.RoutingID, ping.MissingDepth); //crit - comment out
            }

            // didnt find an alt to bucket pref, just take a random one
            if (altPeer == null && transfer.Peers.Count > 1)
            {
                int rndIndex = Core.RndGen.Next(transfer.Peers.Count);

                foreach (RemotePeer p in transfer.Peers.Values)
                    if (p != peer)
                        if (rndIndex == 0)
                        {
                            altPeer = p;
                            break;
                        }
                        else
                            rndIndex--;
            }

            // add alt
            if (altPeer != null && !pong.Alts.ContainsKey(altPeer.Client))
            {
                List<DhtAddress> addresses = Network.LightComm.GetAddresses(altPeer.RoutingID);

                if (addresses != null)
                    pong.Alts[altPeer.Client] = addresses;
            }

            Network.LightComm.SendPacket(client, ServiceID, 0, pong);
        }

        void TestMatchDepth(ulong remoteID, ulong alt, int check)
        {
            string remoteStr = Utilities.IDtoBin(remoteID);
            string altStr = Utilities.IDtoBin(alt);

            int index = 0;

            for (int x = 0; x < 64; x++)
                if (Utilities.GetBit(remoteID, x) != Utilities.GetBit(alt, x))
                    break;
                else
                    index++;

            Debug.Assert(index == check);
        }

        private bool IsDepthMatched(ulong match, ulong check, int pos)
        {
            pos = 63 - pos;

            return (match >> pos == check >> pos);
        }

        void Receive_Pong(DhtClient client, TransferPong pong)
        {
            if (!Transfers.ContainsKey(pong.FileID))
                return;

            OpTransfer transfer = Transfers[pong.FileID];

            RemotePeer peer = transfer.AddPeer(client);

            if (pong.Error)
            {
                transfer.RemovePeer(peer.RoutingID);
                return;
            }

            peer.PingTimeout = pong.Timeout;
            
            peer.NextPing = Core.TimeNow.AddSeconds(pong.Timeout);

            if (pong.InternalSize != 0 && peer.Transfer.InternalSize == 0)
            {
                peer.Transfer.InternalSize = pong.InternalSize;
                peer.Transfer.ChunkSize = pong.ChunkSize;
                peer.Transfer.LocalBitfield = new BitArray(pong.BitCount);
            }

            peer.Status = pong.Status; // set this  here to ensure bitcount is set

		    // process alts (add locations)
            foreach(DhtClient alt in pong.Alts.Keys)
            {
                transfer.AddPeer(alt);

                foreach (DhtAddress address in pong.Alts[alt])
                    Network.LightComm.Update(alt, address);
            }
     
        }

        void Send_Request(RudpSession session, RemotePeer peer)
        {

            Debug.Assert(!peer.UploadData);

            // select piece we're going to upload
            OpTransfer transfer = peer.Transfer;


            // if peer pinged us not knowning anything and hasnt specified they know anything since
            // send a pong first rudp to init the peer
            if (peer.Uninitialized)
            {
                TransferPong pong = new TransferPong();
                pong.FileID = transfer.FileID;
                pong.InternalSize = transfer.InternalSize;
                pong.ChunkSize = transfer.ChunkSize;
                pong.BitCount = transfer.LocalBitfield.Count;
                peer.Uninitialized = false;
                session.SendData(ServiceID, 0, pong, true);
            }

            int bits = transfer.LocalBitfield.Length;

            if (peer.RemoteBitfield == null)
            {
                peer.RemoteBitfield = new BitArray(bits);
                peer.RemoteBitfieldUpdated = true;
            }

            // generate of each bit and its popularity
            ChunkBit[] chunks = new ChunkBit[bits];
            for (int i = 0; i < bits; i++)
                chunks[i].Index = i;

            // chunk is popular for each peer that has it
            foreach (RemotePeer other in transfer.Peers.Values)
                if (other.RemoteBitfield != null)
                    for (int i = 0; i < bits; i++)
                        if (other.RemoteBitfield[i] == true)
                            chunks[i].Popularity++;

            // also count current upload requests to popularity count
            foreach (int index in from u in UploadPeers.Values
                                  where u.Active != null && 
                                        u.Active.Transfer.FileID == transfer.FileID &&
                                        u.Active.LastRequest != null
                                  select u.Active.LastRequest.ChunkIndex)
                chunks[index].Popularity++;

            // from the least popular pieces, select a random one
            var selected = (from chunk in chunks
                            where transfer.LocalBitfield[chunk.Index] &&
                                  !peer.RemoteBitfield[chunk.Index] // we have the piece and remote doesnt
                            orderby Core.RndGen.Next()
                            orderby chunk.Popularity
                            select chunk).Take(1).ToArray();

            if (selected.Length == 0)
            {
                StopUpload(peer, false, "No pieces to send");
                return;
            }

            int selectedIndex = selected[0].Index;


            // if we have the last piece and remote doesn't send that first
            if (transfer.LocalBitfield[bits - 1] == true &&
                peer.RemoteBitfield[bits - 1] == false)
                selectedIndex = bits - 1;


            // setup start, end byte for request
            TransferRequest request = new TransferRequest();
            request.FileID = transfer.FileID;
            request.ChunkIndex = selectedIndex;

            if (peer.RemoteBitfieldUpdated)
            {
                request.GetBitfield = true;
                peer.RemoteBitfieldUpdated = false;
            }

            if (request.ChunkIndex == bits - 1)
            {
                request.StartByte = transfer.InternalSize;
                request.EndByte = transfer.Details.Size;
            }

            else
            {
                request.StartByte = request.ChunkIndex * transfer.ChunkSize * 1024;
                request.EndByte = request.StartByte + transfer.ChunkSize * 1024;

                if (request.EndByte > transfer.InternalSize)
                    request.EndByte = transfer.InternalSize;
            }

            Debug.Assert(UploadPeers[peer.RoutingID].Active != null);

            peer.LastRequest = request;

            if (Logging) peer.DebugUpload += "Request Sent for " + selectedIndex + "\r\n";

            //crit peer must be acknowledged by x or upload is de-activated

            session.SendData(ServiceID, 0, request, true);
        }

        // sender wants to give us data
        private void Receive_Request(RudpSession session, TransferRequest request)
        {
            TransferAck ack = new TransferAck();
            ack.FileID = request.FileID;

            if (!Transfers.ContainsKey(request.FileID) ||
                Transfers[request.FileID].Status == TransferStatus.Complete)
            {
                ack.Error = true;
                session.SendData(ServiceID, 0, ack, true);
                return;
            }

            OpTransfer transfer = Transfers[request.FileID];
            DhtClient client = new DhtClient(session.UserID, session.ClientID);
            RemotePeer peer = transfer.AddPeer(client);

            if (Logging) peer.DebugDownload += "Request Received for " + request.ChunkIndex + "\r\n";

            if (transfer.LocalBitfield == null)
            {
                ack.Uninitialized = true;
                ack.StartByte = -1;
                session.SendData(ServiceID, 0, ack, true);
                return;
            }

            if(request.ChunkIndex >= transfer.LocalBitfield.Length ||
                peer.Warnings > 3)
            {
                ack.Error = true;
                session.SendData(ServiceID, 0, ack, true);
                return;
            }

            
            int lastIndex = transfer.LocalBitfield.Length - 1; 

            // allow the same piece to be transmitted simultaneously
            // pieces are so small, conflicts will be short lived, transfers will end quicker

            // if we already have the piece, send our bitfield
            // or we are still missing the last piece (get that asap so we can verify other pieces)
            if (transfer.LocalBitfield[request.ChunkIndex] ||
                (request.ChunkIndex != lastIndex && !transfer.LocalBitfield[lastIndex]))
            {
                ack.StartByte = -1;
                ack.Bitfield = transfer.LocalBitfield.ToBytes();
            }

            // else we need the piece
            else
            {
                ack.StartByte = request.StartByte;

                if (request.GetBitfield)
                    ack.Bitfield = transfer.LocalBitfield.ToBytes();

                //crit - verify startbyte / chunk index are correct 
                // function - given index get startbyte

                DownloadPeer download;
                if (!DownloadPeers.TryGetValue(client.RoutingID, out download))
                    download = new DownloadPeer(client);

                DownloadPeers[client.RoutingID] = download;

                download.Requests[ack.FileID] = request;

                if (Logging) peer.DebugDownload += "Request Accepted for " + request.ChunkIndex + "\r\n";
            }

            session.SendData(ServiceID, 0, ack, true);
        }

        // sender confirming or denying the range we want to send
        private void Receive_Ack(RudpSession session, TransferAck ack)
        {
            DhtClient client = new DhtClient(session.UserID, session.ClientID);

            ulong id = client.RoutingID;

            // handle local error
            if (!UploadPeers.ContainsKey(id) ||
                UploadPeers[id].Active == null ||
                UploadPeers[id].Active.Transfer.FileID != ack.FileID)
                return;

   
            RemotePeer peer = UploadPeers[id].Active;
            OpTransfer transfer = peer.Transfer;

            peer.DataError = false;

            if (Logging) peer.DebugUpload += "Ack Received for " + ack.StartByte + ", req was " + peer.LastRequest.StartByte + "\r\n";

            // handle remote error - remote removed transfer for ex
            if (ack.Error)
            {
                StopUpload(peer, true, "Ack Error");
                return;
            }

            peer.LastSeen = Core.TimeNow;

            // update remote's bitfield
            if (ack.Bitfield != null)
            {
                peer.RemoteBitfield = Utilities.ToBitArray(ack.Bitfield, peer.RemoteBitfield.Length);
                peer.RemoteBitfieldUpdated = false;
            }

            if (ack.Uninitialized)
            {
                peer.Uninitialized = true;
                Send_Request(session, peer);
                return;
            }

            Debug.Assert(ack.StartByte == -1 || ack.StartByte == peer.LastRequest.StartByte);
            Debug.Assert(peer.LastRequest != null);
            if(ack.StartByte != -1 && ack.StartByte != peer.LastRequest.StartByte)
                return;

            // check if remote wants a diff piece
            if (ack.StartByte == -1)
            {
                // only try this 3 times, then move on
                if (peer.UploadAttempts >= 3)
                {
                    StopUpload(peer, false, "Request attempt limit");
                    return;
                }

                // call send_request again 
                // remote bitfield has been updated, but not if remote is currently getting the piece from someone else
                
                peer.UploadData = false;
                peer.UploadAttempts++;

                peer.RemoteHasChunk(peer.LastRequest.ChunkIndex);

                if (peer.Status == TransferStatus.Complete)
                    StopUpload(peer, true, "Peer Completed");
                else
                    Send_Request(session, peer);
            }

            // confirmed startbyte matches current request, start sending
            else
            {
                peer.UploadAttempts = 0;
                peer.CurrentPos = peer.LastRequest.StartByte;
                peer.UploadData = true;

                if (Logging) peer.DebugUpload += "Ack Accepts start " + ack.StartByte + ", chunk " + peer.LastRequest.ChunkIndex + "\r\n";

                Send_Data(session);
            }
        }

        internal void Send_Data(RudpSession session)
        {
            if (session.SendBuffLow())
                return;

            DhtClient client = new DhtClient(session.UserID, session.ClientID);
            ulong id = client.RoutingID;

            // handle local error
            if (!UploadPeers.ContainsKey(id) ||
                UploadPeers[id].Active == null ||
                !UploadPeers[id].Active.UploadData)
                return;

            RemotePeer peer = UploadPeers[id].Active;

            Debug.Assert(peer.CurrentPos < peer.LastRequest.EndByte);

            while (peer.CurrentPos < peer.LastRequest.EndByte)
            {
                if (session.SendBuffLow()) // sets blocking if true
                    return;

                // get block ready
                TransferData data = new TransferData();
                data.FileID     = peer.Transfer.FileID;
                data.StartByte  = peer.CurrentPos;
                data.Block      = peer.Transfer.ReadBlock(peer.CurrentPos, peer.LastRequest.EndByte);

                if (data.Block == null || data.Block.Length == 0)
                {
                    //crit Debug.Assert(false);
                    StopUpload(peer, false, "Data error");
                    return;
                }

                // send data
                if (session.SendData(ServiceID, 0, data, false))
                    peer.CurrentPos += data.Block.Length;

                else // no room in comm buffer for more sends
                    return;


                // check upload completion
                if (peer.CurrentPos >= peer.LastRequest.EndByte)
                {
                    Debug.Assert(peer.CurrentPos == peer.LastRequest.EndByte);

                    peer.RemoteHasChunk(peer.LastRequest.ChunkIndex);

                    if (Logging) peer.DebugUpload += "Finished Sending chunk " + peer.LastRequest.ChunkIndex + "\r\n";

                    StopUpload(peer, false, "Finished Sending"); // assigns next upload, sends request
                    return; // loop again will cause exception
                }
            }
        }

        private void Receive_Data(RudpSession session, TransferData data)
        {
            // find download
            DhtClient client = new DhtClient(session.UserID, session.ClientID);

            if(!Transfers.ContainsKey(data.FileID))
            {
                Send_Stop(session, data, false);
                return;
            }

            // may have already received piece from someone else, signal retry
            if (!DownloadPeers.ContainsKey(client.RoutingID) ||
                !DownloadPeers[client.RoutingID].Requests.ContainsKey(data.FileID))
            {
                Send_Stop(session, data, true);
                return;
            }

            DownloadPeer download = DownloadPeers[client.RoutingID];
            TransferRequest request = DownloadPeers[client.RoutingID].Requests[data.FileID];

            // ensure data in range
            if (data.StartByte < request.StartByte ||
                data.Block == null ||
                data.StartByte + data.Block.Length > request.EndByte)
            {
                Send_Stop(session, data, true);
                return;
            }

            
            OpTransfer transfer = Transfers[data.FileID];
            
            if(transfer.LocalBitfield == null)
            {
                Send_Stop(session, data, true);
                return;
            }

            // write data
            if (!transfer.WriteBlock(data.StartByte, data.Block))
            {
                Send_Stop(session, data, false);
                return;
            }

            transfer.LastDataReceived = Core.TimeNow;

            request.CurrentPos = data.StartByte + data.Block.Length;

            // if not completed, return
            if (request.CurrentPos != request.EndByte)
                return;

            download.Requests.Remove(data.FileID); // peer needs to send another request to start
            
            RemotePeer peer = transfer.AddPeer(client);
            
            // on chunk completed
            bool success = false;

            if (request.ChunkIndex == transfer.LocalBitfield.Length - 1)
                success = transfer.LoadSubhashes();
            else
                success = transfer.VerifyChunk(request.ChunkIndex);


            if (success)
            {
                transfer.LocalBitfield.Set(request.ChunkIndex, true);

                if (Logging) peer.DebugDownload += "Finished Receiving chunk " + request.ChunkIndex + "\r\n";


                // kill concurrent transfers
                foreach (DownloadPeer dl in (from d in DownloadPeers.Values
                                             where d.Requests.ContainsKey(data.FileID) &&
                                                   d.Requests[data.FileID].ChunkIndex == request.ChunkIndex
                                             select d).ToArray())
                {
                    dl.Requests.Remove(data.FileID);
                }

            }

            // after 3 bad chunks, requests are denied
            else
            {
                peer.Warnings++;
                // download removed, and remote completed so next action is for remote to send req if remote chooses to
                return;
            }
           

            // set status
            transfer.Status = TransferStatus.Incomplete; // we have at least one piece done
            foreach(RemotePeer remote in transfer.Peers.Values)
                remote.LocalBitfieldUpdated = true;


            // on all complete
            if (transfer.LocalBitfield.AreAllSet(true))
            {
                // dont dispose, could still be transferring

                if (!transfer.CheckCompletion())
                {
                    // remove peers, delete transfer, delete file
                    Debug.Assert(false);

                    Transfers.Remove(transfer.FileID);
                    transfer.SavePartial = false;
                    transfer.Dispose();

                    return;
                }

                transfer.Status = TransferStatus.Complete;

               
                // those invoked should copy file, not move it, transfer control will clean itself up
                transfer.EndEvent.Invoke(transfer.FilePath, transfer.Args);
            }
        }

        private void Send_Stop(RudpSession session, TransferData data, bool retry)
        {
            TransferStop stop = new TransferStop();
            stop.FileID = data.FileID;
            stop.StartByte = data.StartByte;
            stop.Retry = retry;

            session.SendData(ServiceID, 0, stop, true);
        }

        void Receive_Stop(RudpSession session, TransferStop stop)
        {
            // 1 request should result in 1 ack, if desynched problems happen
            // stop packet signals we dont need the data packets your sending, try again

            DhtClient client = new DhtClient(session.UserID, session.ClientID);

            ulong id = client.RoutingID;

            // handle local error
            if (!UploadPeers.ContainsKey(id) ||
                UploadPeers[id].Active == null ||
                UploadPeers[id].Active.Transfer.FileID != stop.FileID)
                return;

            RemotePeer peer = UploadPeers[id].Active;

            // if we already sent a new request, and this stop is for a previous chunk, ignore
            if (peer.LastRequest == null ||
                stop.StartByte < peer.LastRequest.StartByte || peer.LastRequest.EndByte < stop.StartByte)
                return;

            // reset dataError only when ack comes in (signaling all bad data packet stops have been received)
            if (peer.DataError)
                return;

            peer.DataError = true;

            bool dontRetry = !stop.Retry;

            StopUpload(peer, dontRetry, "Halted");

            Core.Context.AssignUploadSlots();
        }
    }

    enum TransferStatus { Empty, Incomplete, Complete }; // order important used for selecting next piece to send

    internal class OpTransfer : IDisposable
    {
        internal TransferService Control;
        internal ulong FileID;

        internal DateTime Created;
        internal ulong Target; // where file is located
        internal FileDetails Details;
        internal object[] Args;
        internal EndDownloadHandler EndEvent;

        internal bool Searching;
        internal bool SavePartial = true;

        internal const int MaxPeers = 16;
        internal Dictionary<ulong, RemotePeer> Peers = new Dictionary<ulong, RemotePeer>(); // routing id, peer
        internal RemotePeer[] RoutingTable = new RemotePeer[MaxPeers - 4];


        internal BitArray LocalBitfield;
        string DebugBitfield
        {
            get
            {
                if (LocalBitfield == null)
                    return "empty";

                string field = "";
                for (int i = 0; i < LocalBitfield.Length; i++)
                    field += LocalBitfield[i] ? "1" : "0";

                return field;
            }
        }
        internal TransferStatus Status;

        internal DateTime LastDataReceived;

        // file
        internal long InternalSize;
        internal int ChunkSize;

        internal string FilePath;
        internal FileStream LocalFile;
        byte[] ReadBuffer;
        byte[] Subhashes;
        byte[] VerifyBuffer;

        string DebugLog = "";


        internal OpTransfer(TransferService service, string path, ulong target, FileDetails details, TransferStatus status, object[] args, EndDownloadHandler endEvent)
        {
            // take path as parameter, if it exists open it and get bitfield/chunksize
            // else, create the file to the specified size (bitfield size?)
            // dont initialize / create file until transfer starts, same with initializing sub-hash data filed
            // both for up/down transfers

            // need way to get bit count quickly, and transmit it efficiently
            Control = service;
            Created = service.Core.TimeNow;
            Target = target;
            Details = details;
            Status = status;
            Args = args;
            EndEvent = endEvent;

            FileID = GetFileID(Details);
            FilePath = path;
        }

        static internal ulong GetFileID(FileDetails details)
        {
            return GetFileID(details.Service, details.Hash, details.Size);
        }

        static internal ulong GetFileID(uint service, byte[] hash, long size)
        {
            long key = BitConverter.ToInt64(hash, 0);

            return (ulong)(key ^ size ^ service);
        }

        internal RemotePeer AddPeer(DhtClient client)
        {
            Debug.Assert(!client.Equals(Control.Core.Network.Local));
            if (client.Equals(Control.Core.Network.Local))
                return null;

            // if local complete, dont keep remote completes in mesh because a remotes own search will find completed sources
            // incomplete hosts will also return completed sources

            ulong id = client.RoutingID;

            RemotePeer peer;
            if (!Peers.TryGetValue(id, out peer))
            {
                peer = new RemotePeer(this, client);
                if (Control.Logging) DebugLog += "Peer Added " + id + "\r\n";
            }

            peer.LastSeen = Control.Core.TimeNow;

            Peers[id] = peer;

            if (peer.DhtIndex < RoutingTable.Length && RoutingTable[peer.DhtIndex] == null)
                RoutingTable[peer.DhtIndex] = peer;

            return peer;
        }



        internal void RemovePeer(ulong id)
        {
            if (!Peers.ContainsKey(id))
                return;

            RemotePeer peer = Peers[id];
            peer.Clean();

            Peers.Remove(id);

            if (RoutingTable[peer.DhtIndex] == peer)
            {
                RoutingTable[peer.DhtIndex] = null;

                // check if theres another peer with same index to take place
                //Peers.
            }

            if (Control.Logging) DebugLog += "Peer Removed " + id + "\r\n";
        }

        internal byte[] ReadBlock(long start, long end)
        {
            Debug.Assert(end > start);

            try
            {
                //crit - if this throws exception we might want to notify peers not to try back, all failed

                LoadFile();
                
                if(ReadBuffer == null)
                    ReadBuffer = new byte[4096];

                LocalFile.Seek(start, SeekOrigin.Begin);

                int readSize = (int) ((end - start < 4096) ? (end - start) : 4096);

                int bytesRead = LocalFile.Read(ReadBuffer, 0, readSize);

                if (bytesRead > 0)
                    return Utilities.ExtractBytes(ReadBuffer, 0, bytesRead);
                else
                    return null;
            }
            catch { }

            return null;
        }

        private void LoadFile()
        {
            if (LocalFile != null)
                return;

            if (Status == TransferStatus.Complete)
                LocalFile = File.OpenRead(FilePath);
            else
                LocalFile = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

            if (LocalFile.Length != Details.Size)
                LocalFile.SetLength(Details.Size);
        }

        public void Dispose()
        {
            foreach (ulong id in (from p in Peers.Values select p.RoutingID).ToArray())
                RemovePeer(id);

            if (LocalFile != null)
            {
                LocalFile.Close();
                LocalFile = null;
            }

            // if file not in transfer directory, dont delete it 
            if (!FilePath.StartsWith(Control.TransferPath))
                return;

            // if file not incomplete or save partial is false delete 
            if(Status != TransferStatus.Incomplete || !SavePartial)
                try { File.Delete(FilePath); }
                catch { }

            // otherwise partial is saved to be tried on the next run
        }

        internal bool WriteBlock(long start, byte[] block)
        {
            try
            {
                LoadFile();

                LocalFile.Seek(start, SeekOrigin.Begin);

                LocalFile.Write(block, 0, block.Length);

                return true;
            }
            catch { }

            return false;
        }

        internal bool LoadSubhashes()
        {
            DhtNetwork network = Control.Core.Network;

            LoadFile();

            try
            {
                LocalFile.Seek(InternalSize, SeekOrigin.Begin);

                PacketStream stream = new PacketStream(LocalFile, network.Protocol, FileAccess.Read);

                int copyIndex = 0;

                G2Header root = null;
                while (stream.ReadPacket(ref root))
                    if (root.Name == FilePacket.SubHash)
                    {
                        SubHashPacket info = SubHashPacket.Decode(root);

                        if (Subhashes == null)
                            Subhashes = new byte[info.TotalCount * 20];

                        ChunkSize = info.ChunkSize;

                        info.SubHashes.CopyTo(Subhashes, copyIndex);
                        copyIndex += info.SubHashes.Length;
                    }

                Debug.Assert(Subhashes != null);

                return Subhashes != null;
            }
            catch { }

            return false;
        }

        internal bool VerifyChunk(int index)
        { 
            if (LocalFile == null)
                return false;

            Debug.Assert(Subhashes != null);
            if (Subhashes == null)
                return false;

           if( VerifyBuffer == null)
               VerifyBuffer = new byte[ChunkSize * 1024];

            long start = ChunkSize * 1024 * index;

            try
            {
                LocalFile.Seek(start, SeekOrigin.Begin);

                int read = VerifyBuffer.Length;
                if(index == LocalBitfield.Length - 2) // last bit isnt sub-hashed, thats for storing sub-hashes
                    read = (int) (InternalSize % VerifyBuffer.Length);

                read = LocalFile.Read(VerifyBuffer, 0, read);

                byte[] check = Utilities.ExtractBytes(Subhashes, index * 20, 20);

                return Utilities.MemCompare(check, Control.sha1.ComputeHash(VerifyBuffer, 0, read));
            }
            catch { }

            return false;
        }

        internal bool CheckCompletion()
        {
             if (LocalFile == null)
                return false;

            LocalFile.Seek(0, SeekOrigin.Begin);

            byte[] check = Control.sha1.ComputeHash(LocalFile);

            // dont dispose, could still be transferring

            return Utilities.MemCompare(check, Details.Hash);
        }

        internal long GetProgress()
        {
            if(LocalBitfield == null)
                return 0;

            long completed = 0;

            for(int i = 0; i < LocalBitfield.Length; i++)
                if (LocalBitfield[i])
                {
                    if (i == LocalBitfield.Length - 1)
                        completed += Details.Size - InternalSize;

                    else if (i == LocalBitfield.Length - 2)
                        completed += InternalSize % (ChunkSize * 1024);

                    else
                        completed += ChunkSize * 1024;
                }

            return completed;
        }
    }

    internal class RemotePeer
    {
        internal OpTransfer Transfer;
        internal ulong RoutingID;
        internal DhtClient Client;

        internal bool FirstPing = true;
        internal DateTime NextPing; // next time a ping can be sent
        internal DateTime LastSeen; // last time ping was received

        internal int PingTimeout = 60;

        internal DateTime Timeout
        {
            get { return LastSeen.AddSeconds(PingTimeout + 30); }
        }

        internal string DebugDownload = "";
        internal string DebugUpload = "";

        internal bool DataError;

        // file
       
        internal BitArray RemoteBitfield;

        string DebugBitfield
        {
            get
            {
                if (RemoteBitfield == null)
                    return "empty";

                string field = "";
                for (int i = 0; i < RemoteBitfield.Length; i++)
                    field += RemoteBitfield[i] ? "1" : "0";

                return field;
            }
        }

        internal int BlocksUntilFinished
        {
            get
            {
                if (RemoteBitfield == null)
                    return int.MaxValue;

                int blocks = 0;
                for (int i = 0; i < RemoteBitfield.Length; i++)
                    if (!RemoteBitfield[i])
                        blocks++;

                return blocks;
            }
        }

        internal bool LocalBitfieldUpdated;
        internal bool RemoteBitfieldUpdated;
        internal bool Uninitialized;

        internal TransferRequest LastRequest;
        internal string LastError;
        internal int UploadAttempts;
        internal bool UploadData;
        internal long CurrentPos;
        internal int Warnings;

        internal int DhtIndex;

        TransferStatus _Status;
        internal TransferStatus Status
        {
            get { return _Status; }
            set
            {
                _Status = value;

                if(Transfer.LocalBitfield == null)
                    return;

                if(RemoteBitfield == null)
                    RemoteBitfield = new BitArray(Transfer.LocalBitfield.Length);

                if (_Status == TransferStatus.Empty)
                    RemoteBitfield.SetAll(false);

                if (_Status == TransferStatus.Complete)
                    RemoteBitfield.SetAll(true);
            }
        }

        internal RemotePeer(OpTransfer transfer, DhtClient client)
        {
            Transfer = transfer;
            Client = new DhtClient(client); // if its a sub-class hashing gets messed
            RoutingID = client.RoutingID;
            DhtIndex = transfer.Control.Core.Network.Routing.GetBucketIndex(RoutingID);

            AddUploadEntry();
        }

        internal void AddUploadEntry()
        {
            Dictionary<ulong, UploadPeer> uploads = Transfer.Control.UploadPeers;

            if (!uploads.ContainsKey(RoutingID))
            {
                uploads[RoutingID] = new UploadPeer(Client);

                // init last attempt to now, so newly added host isn't preferenced over
                // hosts that have been waiting longer
                uploads[RoutingID].LastAttempt = Transfer.Control.Core.TimeNow;
            }

            if (!uploads[RoutingID].Transfers.ContainsKey(Transfer.FileID))
                uploads[RoutingID].Transfers[Transfer.FileID] = this;
        }

        internal bool CanSendPeice()
        {
            if (Transfer.Status == TransferStatus.Empty)
                return false;

            if (Transfer.LocalBitfield != null && RemoteBitfield == null)
                return true; // on connect we'll get remote's bitfield

            if (Transfer.LocalBitfield == null || RemoteBitfield == null)
                return false;

            // if bitfield out of date, we just use last known bitfield, compared with our current status
            // on connect to peer we'll request an updated field

            // xor local and remote together to get the pieces each sides needs
            // and with local to get just what we have
            // bit array xor / and operations modify the original field!!

            BitArray canSend = new BitArray(RemoteBitfield.Length);

            for (int i = 0; i < RemoteBitfield.Length; i++)
                canSend[i] = (RemoteBitfield[i] ^ Transfer.LocalBitfield[i]) & Transfer.LocalBitfield[i];

            // return true as long as there is one piece we can send
            return !canSend.AreAllSet(false);
        }



        internal void RemoteHasChunk(int index)
        {
            RemoteBitfield.Set(index, true);
            
            Status = TransferStatus.Incomplete;

            if (RemoteBitfield.AreAllSet(true))
                Status = TransferStatus.Complete;
        }

        internal void Clean()
        {
            ulong fileID = Transfer.FileID;
            TransferService service = Transfer.Control;

            // remove upload entry
            if (!service.UploadPeers.ContainsKey(RoutingID))
                return;

            UploadPeer upload = service.UploadPeers[RoutingID];

            if (upload.Active != null && upload.Active.Transfer.FileID == fileID)
                upload.Active = null;

            upload.Transfers.Remove(fileID);

            if (upload.Transfers.Count == 0)
                service.UploadPeers.Remove(RoutingID);

            // remove download entry
            if (service.DownloadPeers.ContainsKey(RoutingID) &&
                service.DownloadPeers[RoutingID].Requests.ContainsKey(fileID))
                service.DownloadPeers[RoutingID].Requests.Remove(fileID);
        }

        internal bool Active()
        {
            // return true if this is the active upload
            Dictionary<ulong, UploadPeer> uploads = Transfer.Control.UploadPeers;

            if (uploads.ContainsKey(RoutingID))
                if (uploads[RoutingID].Active == this)
                    return true;


            // return true if in download list
            Dictionary<ulong, DownloadPeer> downloads = Transfer.Control.DownloadPeers;
            if (downloads.ContainsKey(RoutingID))
                if (downloads[RoutingID].Requests.ContainsKey(Transfer.FileID))
                    return true;

            return false;
        }
    }

    internal class UploadPeer
    {
        internal DhtClient  Client;
        internal RemotePeer Active;
        internal DateTime   LastAttempt;
        internal Dictionary<ulong, RemotePeer> Transfers = new Dictionary<ulong, RemotePeer>();


        internal UploadPeer(DhtClient client)
        {
            Client = client;
        }
    }

    internal class DownloadPeer
    {
        internal DhtClient Client;
        internal Dictionary<ulong, TransferRequest> Requests = new Dictionary<ulong, TransferRequest>();


        internal DownloadPeer(DhtClient client)
        {
            Client = client;
        }
    }

    internal struct ChunkBit
    {
        internal int Index;
        internal int Popularity;
    }
    
}
