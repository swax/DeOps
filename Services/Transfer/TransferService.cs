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
        public uint ServiceID { get { return 3; } }

        internal OpCore Core;
        DhtNetwork Network;

        int ConcurrentDownloads = 15;
        internal LinkedList<OpTransfer> Pending = new LinkedList<OpTransfer>();
        internal List<OpTransfer> Active = new List<OpTransfer>(); // local incomplete transfers
     
        // all transfers complete and incomplete and inactive partials
        internal int ActiveUploads;
        internal int NeedUploadWeight;
        internal Dictionary<DhtClient, UploadPeer> UploadPeers = new Dictionary<DhtClient, UploadPeer>(); // routing, info - peers that have requested an upload (ping)
        internal Dictionary<ulong, OpTransfer> Transfers = new Dictionary<ulong, OpTransfer>(); // file id, transfer

        internal ServiceEvent<FileSearchHandler> FileSearch = new ServiceEvent<FileSearchHandler>();
        internal ServiceEvent<FileRequestHandler> FileRequest = new ServiceEvent<FileRequestHandler>();
        
        internal string TransferPath;

        LinkedList<DateTime> RecentPings = new LinkedList<DateTime>();

        //-------------------------

        internal Dictionary<int, FileDownload> DownloadMap = new Dictionary<int, FileDownload>();
        internal Dictionary<RudpSession, List<FileUpload>> UploadMap = new Dictionary<RudpSession, List<FileUpload>>();


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

          

            // create and clear transfer dir
            try
            {
                TransferPath = Core.User.RootPath + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + ServiceID.ToString();
                Directory.CreateDirectory(TransferPath);

                //crit - load partials, but dont begin transferring them, keep on standby
                /*
                string[] files = Directory.GetFiles(TransferPath);

                foreach (string filepath in files)
                    try { File.Delete(filepath); }
                    catch { }*/
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
        }

        internal void StartDownload( ulong target, FileDetails details, object[] args, EndDownloadHandler endEvent)
        {
            if (Core.InvokeRequired)
                Debug.Assert(false);

            // files isolated (use different enc keys) between services to keep compartmentalization strong

            foreach (OpTransfer transfer in Transfers.Values)
                if (transfer.Details.Equals(details))
                    return;

            OpTransfer pending = new OpTransfer(Core, target, details, TransferStatus.Empty, args, endEvent);

            Debug.Assert(!File.Exists(pending.Destination));

            Pending.AddLast(pending);

            return ;
        }

        //crit used??
        internal void AddSource(int id, ulong key)
        {
            if (Core.InvokeRequired)
                Debug.Assert(false);

            if (!DownloadMap.ContainsKey(id))
                return;

            List<ClientInfo> clients = Core.Locations.GetClients(key);

            foreach (ClientInfo info in clients)
                DownloadMap[id].AddSource(info.Data);
        }

        internal void CancelDownload(uint id, byte[] hash, long size)
        {
            if (Core.InvokeRequired)
                Debug.Assert(false);

            FileDownload target = null;

            foreach (FileDownload download in DownloadMap.Values)
                if (download.Details.Service == id &&
                   download.Details.Size == size &&
                   Utilities.MemCompare(download.Details.Hash, hash))
                {
                    target = download;
                    break;
                }

            if (target != null)
            {
                /*if (Pending.Contains(target.ID))
                    Pending.Remove(target.ID);

                if (Active.Contains(target.ID))
                    Active.Remove(target.ID);*/

                DownloadMap.Remove(target.ID);
            }
        }

        void Core_SecondTimer()
        {
            if (!Network.Established)
                return;

            // move downloads from pending to active
            if (Active.Count < ConcurrentDownloads && Pending.Count > 0)
            {
                OpTransfer transfer = Pending.First.Value;
                Pending.RemoveFirst();
                Active.Add(transfer);

                Transfers[transfer.FileID] = transfer;
             
                byte[] parameters = transfer.Details.Encode(Network.Protocol);

                DhtSearch search = Core.Network.Searches.Start(transfer.Target, "Transfer", ServiceID, 0, parameters, new EndSearchHandler(EndSearch));

                if (search != null)
                {
                    transfer.Searching = true;
                    search.Carry = transfer;
                }
            }


            // only ping 8 closest peers in peer list
            // max peer list at 16 closest
            // only ping if transfer is incomplete
            // remove dead peers / transfers
            ulong localID = Core.Network.Routing.LocalRoutingID;
            Dictionary<ulong, List<OpTransfer>> pingLocs = new Dictionary<ulong, List<OpTransfer>>(); // routing id, file id lsit

            // send pings, expire old peers/transfers
            foreach (OpTransfer transfer in Transfers.Values)
            {
                // remove dead peers
                foreach (DhtClient client in (from peer in transfer.Peers.Values
                                      where Core.TimeNow > peer.Timeout && peer.Attempts > 2
                                      select peer.Client).ToArray())
                    transfer.RemovePeer(this, client);
                    


                // trim peers to 16 closest, remove furthest from local
                if (transfer.Peers.Count > 16)
                {
                    foreach(DhtClient furthest in (from p in transfer.Peers.Keys
                                                 orderby p.RoutingID ^ localID descending 
                                                 select p).Take(transfer.Peers.Count - 16).ToArray())
                        transfer.RemovePeer(this, furthest);
                }

                // imcomplete transfers ping their sources to let them know to send us data
                if (transfer.Status != TransferStatus.Complete)
                {
                    // ping 8 closest peers, the rest are in standby (they ping us to stay in our peer list)
                    foreach (TransferPeer peer in (from peer in transfer.Peers.Values
                                                   where Core.TimeNow > peer.NextPing
                                                   orderby peer.RoutingID ^ localID
                                                   select peer).Take(8))
                        SendPing(transfer, peer);
                }
                // completed rely on remotes to send pings to keep transfer loaded
            }

            // remove empty transfers with 0 peers, imcompletes hang around and are pruned seperately
            foreach (OpTransfer transfer in (from t in Transfers.Values
                                             where t.Peers.Count == 0 && (t.Searching == false && t.Status == TransferStatus.Empty) || t.Status == TransferStatus.Complete
                                             select t).ToArray())
            {
                Transfers.Remove(transfer.FileID);
                Active.Remove(transfer);
            }

            
            // lert context that we need to upload a piece
            if (Transfers.Count > 0)
                NeedUploadWeight++;

            ActiveUploads = UploadPeers.Values.Count(p => p.Active);
            

            // on complete - if local complete, remove complete peers



            /*foreach (int id in Active)
            {
                FileDownload download = DownloadMap[id];

                if (download.Status == DownloadStatus.None)
                {
                    // try connect
                    if (download.NextAttempt > 0)
                        download.NextAttempt--;
                    else
                        Connect(DownloadMap[id]);

                    // check if done
                    if (!download.Searching &&
                        download.Sessions.Count == 0 && // ensure that removed
                        download.Attempted.Count == download.Sources.Count &&
                        download.NextAttempt == 0)
                        download.Status = DownloadStatus.Failed;
                }
            }*/


            // run code below every 5 secs
            if (Core.TimeNow.Second % 5 != 0)
                return;


            //remove dead uploads
            List<RudpSession> removeSessions = new List<RudpSession>();

            foreach (RudpSession session in UploadMap.Keys)
            {
                List<FileUpload> doneList = new List<FileUpload>();
                List<FileUpload> uploadList = UploadMap[session];

                foreach (FileUpload upload in uploadList)
                    if (upload.Done)
                        doneList.Add(upload);

                foreach (FileUpload upload in doneList)
                    uploadList.Remove(upload);

                if (uploadList.Count == 0)
                    removeSessions.Add(session);
            }

            foreach (RudpSession session in removeSessions)
                UploadMap.Remove(session);

            // remove dead downloads
            List<int> removeList = new List<int>();

            /*foreach (int id in Active)
                if (DownloadMap[id].Status == DownloadStatus.Failed ||
                    DownloadMap[id].Status == DownloadStatus.Done)
                    removeList.Add(id);*/

            foreach (int id in removeList)
            {
                try
                {
                    DownloadMap[id].CloseStream();
                    File.Delete(DownloadMap[id].Destination);
                }
                catch { }

                //Active.Remove(id);
                DownloadMap.Remove(id);
            }
        }

        internal void StartUpload()
        {
            // this method ensures that if one host has multiple files queued they are completed sequentially

            // pref long waited host in top 8 not completed, local incomplete, most completed remote file, something we have a piece for, select rarest (when transfer begins)
             ulong localID = Core.Network.Routing.LocalRoutingID;
            List<TransferPeer> allPeers = new List<TransferPeer>();

            // pref 8 closest incomplete nodes of each transfer
            foreach(OpTransfer transfer in Transfers.Values)
                if(transfer.Status != TransferStatus.Empty)
                    allPeers.AddRange( (from p in transfer.Peers.Values
                                        where p.Status != TransferStatus.Complete 
                                        orderby p.RoutingID ^ localID
                                        select p).Take(8) );

            allPeers = (from p in allPeers
                        where !UploadPeers[p.Client].Active && p.CanSendPeice() 
                        select p).ToList();

            allPeers = (from p in allPeers // the order of these orderbys is very important
                        orderby p.BytesUntilFinished // pref file closest to completion
                        orderby UploadPeers[p.Client].LastAttempt // pref peer thats been waiting the longest
                        orderby (int)p.Transfer.Status // prefsending local incomplete over complete
                        select p).Take(1).ToList();


            if (allPeers.Count == 0)
                return;

            TransferPeer selected = allPeers[0];
            UploadPeer upload = UploadPeers[selected.Client];

            upload.LastAttempt = Core.TimeNow;
            upload.Active = true;

            // if connected to source
            RudpSession session = Network.RudpControl.GetActiveSession(selected.Client);

            if (session != null)
                Send_Request(session, selected);
            else
                Network.RudpControl.Connect(selected.Client);
        }

        void LightComm_ReceiveData(DhtClient client, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (G2Protocol.ReadPacket(root))
            {
                if (root.Name == TransferPacket.Ping)
                    ReceivePing(client, TransferPing.Decode(root));

                if (root.Name == TransferPacket.Pong)
                    ReceivePong(client, TransferPong.Decode(root));
            }

        }

        private void SendPing(OpTransfer transfer, TransferPeer peer)
        {
            peer.Attempts++;
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
            //ping.Status = transfer.Status;


            if (peer.FirstPing)
            {
                ping.RequestAlts = true;
                peer.FirstPing = false;
            }
            // transfer status
            // bifield change

            

            Network.LightComm.SendPacket(peer.Client, ServiceID, 0, ping);
        }

        void ReceivePing(DhtClient client, TransferPing ping)
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

                if(path != null && File.Exists(path))
                {
                    transfer = new OpTransfer(Core, ping.Target, ping.Details, TransferStatus.Complete, null, null);
                   Transfers[fileID] = transfer;
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
            }

            else
            {
                // add remote as peer
                TransferPeer peer = transfer.AddPeer(client);
                peer.LastSeen = Core.TimeNow; // attempts used for outgoing pings

                if(!UploadPeers.ContainsKey(client))
                    UploadPeers[client] = new UploadPeer(client);
                
                if(!UploadPeers[client].Transfers.ContainsKey(fileID))
                    UploadPeers[client].Transfers[fileID] = peer;

                // we want a target of 20 pings per minute ( 1 every 3 seconds)
                // pings per minute = RecentPings.count / (Core.TimeNow - RecentPings.Last).ToMinutes()
                float pingsPerMinute = (float) RecentPings.Count / (float) (Core.TimeNow - RecentPings.Last.Value).Minutes;
                pong.Timeout = (int)(60.0 * pingsPerMinute / 20.0); // 20 is target rate, so if we have 40ppm, multiplier is 2, timeout 120seconds
                pong.Timeout = Math.Max(60, pong.Timeout); // use 60 as lowest timeout


                //if haven't sent alts - send random 3 alts (upon first contact will send more alts)
                if (ping.RequestAlts)
                {
                     // select 3 random peers foreach peer get top 3 addresses from lightComm
                    foreach (TransferPeer alt in (from p in transfer.Peers.Values
                                                  where p != peer 
                                                  orderby Core.RndGen.Next() 
                                                  select p).Take(3).ToArray())
                    {
                        if (Network.LightComm.Clients.ContainsKey(alt.Client))
                            pong.Alts[alt.Client] = (from loc in Network.LightComm.Clients[alt.Client].Addresses
                                                        select loc.Address).Take(3).ToList();
                    }
                }
           
                //crit - mark peer's bitfield out-of-date

            }

            Network.LightComm.SendPacket(client, ServiceID, 0, pong);
        }

        void ReceivePong(DhtClient client, TransferPong pong)
        {
            if (!Transfers.ContainsKey(pong.FileID))
                return;

            OpTransfer transfer = Transfers[pong.FileID];

            TransferPeer peer = transfer.AddPeer(client);
            peer.LastSeen = Core.TimeNow; // attempts used for outgoing pings

            if (pong.Error)
            {
                transfer.Peers.Remove(client);
                return;
            }

            peer.PingTimeout = pong.Timeout;
            peer.NextPing = Core.TimeNow.AddSeconds(pong.Timeout);
            peer.Attempts = 0;

		    // process alts (add locations)
            foreach(DhtClient alt in pong.Alts.Keys)
            {
                transfer.AddPeer(alt);

                foreach (DhtAddress address in pong.Alts[alt])
                    Network.LightComm.Update(alt, address);
            }
     
        }


        public void SimTest()
        {
        }

        public void SimCleanup()
        {
        }

        public List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong user, uint project)
        {
            return null;
        }

        internal string GetStatus(uint id, byte[] hash, long size)
        {
            FileDownload target = null;

            foreach (FileDownload download in DownloadMap.Values)
                if (download.Details.Service == id &&
                   download.Details.Size == size &&
                   Utilities.MemCompare(download.Details.Hash, hash))
                {
                    target = download;
                    break;
                }

            if(target == null)
                return null;

            // pending
            //if(Pending.Contains(target.ID))
            //    return "Pending";

            //if (Active.Contains(target.ID))
            {
                // transferring
                if (target.Status == DownloadStatus.Transferring)
                {
                    long percent = target.FilePos * 100 / target.Details.Size;

                    return "Downloading, " + percent.ToString() + "% Completed";
                }

                // searching
                if (target.Searching)
                    return "Searching";

                return "Connecting";
            }

            return null;
        }

        void Session_KeepActive(Dictionary<ulong, bool> active)
        {
            foreach (FileDownload transfer in DownloadMap.Values)
                foreach (RudpSession session in transfer.Sessions)
                    active[session.UserID] = true;

            foreach (RudpSession session in UploadMap.Keys)
                active[session.UserID] = true;
        }

        void Search_Local(ulong key, byte[] parameters, List<byte[]> results)
        {
            if (parameters == null)
            {
                Core.Network.UpdateLog("Transfers", "Search Recieved with null parameters");
                return;
            }

            FileDetails details = FileDetails.Decode(parameters);

            if (details == null || Core.Locations.LocalLocation == null)
                return;

            // reply with loc info if a component has the file
            if(FileSearch.Contains(details.Service, details.DataType))
                if (FileSearch[details.Service, details.DataType].Invoke(key, details))
                {
                    results.Add(Core.Locations.LocalLocation.Data.EncodeLight(Network.Protocol));
                    return;
                }

            // search if file is partial - only reply if we have at least one piece and know someone with 100% of original
            foreach (OpTransfer transfer in Transfers.Values)
                if (transfer.Status != TransferStatus.Empty && 
                    transfer.Peers.Values.Count(p => p.Status == TransferStatus.Complete) > 0 && 
                    transfer.Details.Equals(details))
                {
                    results.Add(Core.Locations.LocalLocation.Data.EncodeLight(Network.Protocol));
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
            
            // if no results mark as dead
            //if (transfer.Sources.Count == 0)
            //    transfer.Status = DownloadStatus.Failed;
        }

        private void Connect(FileDownload download)
        {
            for(int i = 0; i < download.Sources.Count; i++)
                if (!download.Attempted.Contains(i))
                {
                    // if connected to source
                    if (Network.RudpControl.IsConnected(download.Sources[i]))
                        Send_Request(Network.RudpControl.GetActiveSession(download.Sources[i]), download);
                    else
                        Network.RudpControl.Connect(download.Sources[i]);

                    download.Attempted.Add(i);
                    download.NextAttempt = 5;
                    return;
                }
        }

        void Session_Update(RudpSession session)
        {
            // uploads
            if (session.Status == SessionStatus.Closed)
                if (UploadMap.ContainsKey(session))
                    UploadMap.Remove(session);

            // downloads
            /*foreach (int id in Active)
            {
                FileDownload download = DownloadMap[id];

                // active
                if(session.Status == SessionStatus.Active)
                    if ( download.Status != DownloadStatus.Done && 
                        !download.Sessions.Contains(session) &&
                        download.Sessions.Count == 0)// only allow 1 d/l session at a time for now
                            foreach(LocationData source in download.Sources)
                                if (source.UserID == session.UserID && source.Source.ClientID == session.ClientID)
                                {
                                    download.Log("Request sent to " + session.Name);
                                    Send_Request(session, download);
                                    break;
                                }

                // closed
                if (session.Status == SessionStatus.Closed && download.Sessions.Contains(session))
                {
                    download.Log("Session to " + session.Name + " closed");
                    download.Sessions.Remove(session);
                    
                    if(download.Status != DownloadStatus.Done)
                        download.Status = DownloadStatus.None;
                }
            }*/
        }

        void Send_Request(RudpSession session, TransferPeer transfer)
        {
            int x = 0;
            // if need updated bitfield ask for it

            // select rarest piece, or if last piece not sent, send that first
        }

        void Send_Request(RudpSession session, FileDownload download)
        {
            download.RequestSent++;

            download.Status = DownloadStatus.Transferring;

            if(!download.Sessions.Contains(session))
                download.Sessions.Add(session);

            TransferRequest request = new TransferRequest(download, Network.Protocol);
            session.SendData(ServiceID, 0, request, true);
        }

        void Session_Data(RudpSession session, byte[] data)
        {
            G2Header root = new G2Header(data);

            if(G2Protocol.ReadPacket(root))
            {
                switch(root.Name)
                {
                    case TransferPacket.Request:
                        Process_Request(session, TransferRequest.Decode(root));
                        break;

                    case TransferPacket.Ack:
                        Process_Ack(session, TransferAck.Decode(root));
                        break;

                    case TransferPacket.Data:
                        Process_Data(session, TransferData.Decode(root));
                        break;
                }
            }
        }

        private void Process_Request(RudpSession session, TransferRequest request)
        {
            FileDetails details = FileDetails.Decode(request.Details);

            // ask component for path to file
            string path = null;

            if (FileRequest.Contains(details.Service, details.DataType))
                path = FileRequest[details.Service, details.DataType].Invoke(request.Target, details);


            // build ack
            TransferAck ack = new TransferAck();
            ack.TransferID = request.TransferID;


            if (!UploadMap.ContainsKey(session))
                UploadMap[session] = new List<FileUpload>();

            // invalidate previous requests with same transferid
            foreach (FileUpload upload in UploadMap[session])
                if (upload.Request.TransferID == request.TransferID)
                    upload.Done = true;

            if (request.StartByte == details.Size)
                return;

            // load file
            if(path != null && File.Exists(path))
            {
                FileUpload upload = new FileUpload(session, request, details, path);
                
                if(upload.ReadStream != null)
                {
                    UploadMap[session].Add(upload);
                    ack.Accept = true;
                }
            }
            
            // send ack
            session.SendData(ServiceID, 0, ack, true);


            OnMoreData(session);
        }

        internal void OnMoreData(RudpSession session)
        {
            if(!UploadMap.ContainsKey(session))
                return;

            if(session.SendBuffLow())
                return;

            List<FileUpload> files = UploadMap[session];

            // if we're sending a big list of files over session 
            // simulaneously we want them all going at the same speed
            int  start = Core.RndGen.Next(files.Count);
            bool sending = true;
            
            while(sending)
            {
                sending = false;
                
                for (int i = start; i < files.Count; i++)
                {
                    FileUpload upload = files[i];

                    if (!upload.Done)
                    {
                        if (session.SendBuffLow()) // sets blocking if true
                            return;

                        sending = true;
                        upload.ReadNext();
                        TransferData data = new TransferData(upload);

                        upload.CheckDone();

                        if (!session.SendData(ServiceID, 0, data, false))
                            return; // no room in comm buffer for more sends
                    }
                }

                start = 0;
            }
        }

        private void Process_Ack(RudpSession session, TransferAck ack)
        {
            // find transfer
            if (!DownloadMap.ContainsKey(ack.TransferID))
                return;

            FileDownload download = DownloadMap[ack.TransferID];
            download.Log("Ack received from " + session.Name);

            if (!download.Sessions.Contains(session))
                return;

            if (!ack.Accept)
            {
                download.Sessions.Remove(session);
                download.Status = DownloadStatus.None;
                return;
            }
            
            // setup file
            try
            {
                if (download.WriteStream == null)
                {
                    download.Log("Stream created");
                    download.WriteStream = new FileStream(download.Destination, FileMode.Create, FileAccess.Write);
                }
            }
            catch
            {
                download.Sessions.Remove(session);
                download.Status = DownloadStatus.None;
                return;
            }
        }

        private void Process_Data(RudpSession session, TransferData data)
        {
            // find transfer
            if (!DownloadMap.ContainsKey(data.TransferID))
                return;

            FileDownload download = DownloadMap[data.TransferID];

            download.ProcessCalled++;

            if (!download.Sessions.Contains(session))
                return;

            try
            {
                if (data.StartByte == download.FilePos)
                {
                    download.Log("Data received, pos " + download.FilePos.ToString() + ", size " + data.Data.Length.ToString() + "  from " + session.Name);
                    download.WriteStream.Write(data.Data, 0, data.Data.Length);
                    download.FilePos += data.Data.Length;
                }
                // else, offset, maybe two connections or somethan
                else if(download.Status == DownloadStatus.Transferring)
                {
                    Send_Request(session, download);
                }

            }
            catch
            {
                download.Log("Error stream closed");
                download.CloseStream();  
                download.Sessions.Remove(session);
                download.Status = DownloadStatus.None;
                return;
            }

            // if complete
            if (download.FilePos == download.Details.Size && download.Status == DownloadStatus.Transferring)
            {
                

                // hash file
                try
                {
                    download.Log("Completed, stream closed");
                    download.CloseStream();

                    FileStream file = new FileStream(download.Destination, FileMode.Open);

                    SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();

                    if (file.Length != download.Details.Size ||
                        !Utilities.MemCompare(download.Details.Hash, sha.ComputeHash(file)))
                    {
                        File.Delete(download.Destination);
                        file.Close();
                        throw new Exception("File itegrity check failed");
                    }

                    download.Log("Hash successful");
                    file.Close();
                }
                catch
                {
                    download.CloseStream();
                    download.Sessions.Remove(session);
                    download.Status = DownloadStatus.Failed;
                    return;
                }

                // call finish event
                download.Log("Status done");
                download.EndEvent.Invoke(download.Destination, download.Args);

                download.Status = DownloadStatus.Done;
            }
        }
    }

    enum DownloadStatus { None, Transferring, Failed, Done };

    enum TransferStatus { Empty, Incomplete, Complete }; // order important used for selecting next piece to send

    internal class OpTransfer
    {
        internal ulong FileID;

        internal ulong Target; // where file is located
        internal FileDetails Details;
        internal object[] Args;
        internal EndDownloadHandler EndEvent;

        internal bool Searching;
        internal Dictionary<DhtClient, TransferPeer> Peers = new Dictionary<DhtClient, TransferPeer>(); // routing id, peer

        internal string Destination;

        internal BitArray LocalBitfield;
        internal TransferStatus Status;


        internal OpTransfer(OpCore core, ulong target, FileDetails details, TransferStatus status, object[] args, EndDownloadHandler endEvent)
        {
            Target = target;
            Details = details;
            Status = status;
            Args = args;
            EndEvent = endEvent;

            Destination = core.Transfers.TransferPath;
            Destination += Path.DirectorySeparatorChar + Utilities.CryptFilename(core, (ulong)Details.Size, Details.Hash);

            FileID = GetFileID(Details);
        }

        internal TransferPeer AddPeer(DhtClient client)
        {
            // if local complete, dont keep remote completes in mesh because a remotes own search will find completed sources
            // incomplete hosts will also return completed sources

            if(Peers.ContainsKey(client))
                return Peers[client];

            TransferPeer peer = new TransferPeer(this, client);

            Peers[client] = peer;

            return peer;
        }

        static internal ulong GetFileID(FileDetails details)
        {
            long key = BitConverter.ToInt64(details.Hash, 0);

            return (ulong)(key ^ details.Size);
        }

        internal void RemovePeer(TransferService service, DhtClient client)
        {
            Peers.Remove(client);

            if (!service.UploadPeers.ContainsKey(client))
                return;
            
            service.UploadPeers[client].Transfers.Remove(FileID);

            if (service.UploadPeers[client].Transfers.Count == 0)
                service.UploadPeers.Remove(client);
        }
    }

    internal class TransferPeer
    {
        internal OpTransfer Transfer;
        internal ulong RoutingID;
        internal DhtClient Client;

        internal TransferStatus Status;
        internal BitArray RemoteBitfield;
        internal long BytesUntilFinished;
        internal bool BitfieldOutofDate;

        internal bool FirstPing = true;
        internal DateTime NextPing; // next time a ping can be sent
        internal DateTime LastSeen; // last time ping was received

        internal int PingTimeout = 60;

        internal DateTime Timeout
        {
            get { return LastSeen.AddSeconds(PingTimeout + 30); }
        }

        internal int Attempts;


        internal TransferPeer(OpTransfer transfer, DhtClient client)
        {
            Transfer = transfer;
            Client = client;
            RoutingID = client.RoutingID;
        }

        internal bool CanSendPeice()
        {
 
            if (Transfer.Status == TransferStatus.Empty)
                return false;

            // if bitfield out of date, we just use last known bitfield, compared with our current status
            // on connect to peer we'll request an updated field

            // find the pieces each bit field has and the other needs
            BitArray needs = Transfer.LocalBitfield.Xor(RemoteBitfield); 

            // just the pieces we can send over
            needs = needs.And(Transfer.LocalBitfield); 

            return !needs.IsEmpty();
        }


    }

    internal class UploadPeer
    {
        internal DhtClient Client;
        internal bool Active;
        internal DateTime LastAttempt;

        internal Dictionary<ulong, TransferPeer> Transfers = new Dictionary<ulong, TransferPeer>();


        internal UploadPeer(DhtClient client)
        {
            Client = client;
        }
    }

    internal class FileDownload
    {
        internal DownloadStatus Status;
        internal List<LocationData> Sources = new List<LocationData>();
        internal List<int> Attempted = new List<int>();
        internal int NextAttempt;
        internal bool Searching;
        internal List<RudpSession> Sessions = new List<RudpSession>();

        internal int ID;
        internal ulong Target;
        internal FileDetails Details;
        internal object[] Args;
        internal EndDownloadHandler EndEvent;
        
        internal string Destination;
        internal FileStream WriteStream;
        internal long FilePos;

        internal int ProcessCalled;
        internal int RequestSent;

        List<string> History = new List<string>();


        internal FileDownload(OpCore core, int id, ulong target, FileDetails details, object[] args, EndDownloadHandler endEvent)
        {
            ID = id;
            Target = target;
            Details = details;
            Args = args;
            EndEvent = endEvent;

            Destination = core.Transfers.TransferPath;
            Destination += Path.DirectorySeparatorChar + Utilities.CryptFilename(core, (ulong)Details.Size, Details.Hash);
        }

        internal void AddSource(LocationData location)
        {
            foreach (LocationData source in Sources)
                if (source.UserID == location.UserID && source.Source.ClientID == location.Source.ClientID)
                    return;

            Sources.Add(location);
        }

        internal void Log(string entry)
        {
            History.Add(entry);
        }

        internal void CloseStream()
        {
            try
            {
                if (WriteStream != null)
                    WriteStream.Close();
            }
            catch { }

            WriteStream = null;
        }
    }

    internal class FileUpload
    {
        internal const int READ_SIZE = 4096;

        internal RudpSession     Session;
        internal TransferRequest Request;
        internal FileDetails     Details;
        internal string          Path;
        internal FileStream      ReadStream;
        internal bool            Done;

        internal byte[] Buff = new byte[READ_SIZE];
        internal int    BuffSize;
        internal long  FilePos;

        internal FileUpload(RudpSession session, TransferRequest request, FileDetails details, string path)
        {
            Session = session;
            Request = request;
            Details = details;
            Path = path;

            try
            {
                ReadStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                FilePos = request.StartByte;
                ReadStream.Seek((long)FilePos, SeekOrigin.Begin);
            }
            catch 
            {
                Done = true;
            }
        }

        internal void ReadNext()
        {
            BuffSize = 0;

            try
            {
                BuffSize = ReadStream.Read(Buff, 0, READ_SIZE);
                FilePos += BuffSize;
            }
            catch 
            {
                Done = true;
            }
        }

        internal void Rollback()
        {
            if (ReadStream == null)
                return;

            try
            {
                FilePos -= BuffSize;
                ReadStream.Seek(-BuffSize, SeekOrigin.Current);
                BuffSize = 0;
            }
            catch { }
        }

        internal void CheckDone()
        {
            if (FilePos == Details.Size)
            {
                Done = true;
                ReadStream.Close();
                ReadStream = null;
            }
        }
    }
}
