using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Transport;
using DeOps.Components.Location;

namespace DeOps.Components.Transfer
{
    internal delegate void EndDownloadHandler(string path, object[] args);
    internal delegate bool FileSearchHandler(ulong key, FileDetails details);
    internal delegate string FileRequestHandler(ulong key, FileDetails details);


    internal class TransferControl : OpComponent
    {
        internal OpCore Core;

        int ConcurrentDownloads = 5;

        internal List<int> Active = new List<int>();
        internal LinkedList<int> Pending = new LinkedList<int>();

        internal Dictionary<int, FileDownload> DownloadMap = new Dictionary<int, FileDownload>();
        internal Dictionary<RudpSession, List<FileUpload>> UploadMap = new Dictionary<RudpSession, List<FileUpload>>();
         
        internal Dictionary<ushort, FileSearchHandler>   FileSearch  = new Dictionary<ushort, FileSearchHandler>();
        internal Dictionary<ushort, FileRequestHandler>  FileRequest = new Dictionary<ushort, FileRequestHandler>();

        internal string TransferPath;


        internal TransferControl(OpCore core)
        {
            Core = core;
            Core.Transfers = this;

            Core.LoadEvent += new LoadHandler(Core_Load);
            Core.TimerEvent += new TimerHandler(Core_Timer);

            Core.OperationNet.Searches.SearchEvent[ComponentID.Transfer] = new SearchRequestHandler(Search_Local);

            Core.RudpControl.SessionUpdate += new SessionUpdateHandler(Session_Update);
            Core.RudpControl.SessionData[ComponentID.Transfer] = new SessionDataHandler(Session_Data);
        }

        internal void Core_Load()
        {
            // create and clear transfer dir
            try
            {
                TransferPath = Core.User.RootPath + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + ComponentID.Transfer.ToString();
                Directory.CreateDirectory(TransferPath);

                string[] files = Directory.GetFiles(TransferPath);

                foreach (string filepath in files)
                    try { File.Delete(filepath); }
                    catch { }
            }
            catch { }
        }

        internal int StartDownload( ulong target, FileDetails details, object[] args, EndDownloadHandler endEvent)
        {
            // search for already started downloads
            lock (DownloadMap)
                foreach (int key in DownloadMap.Keys)
                    if (DownloadMap[key].Details.Equals(details))
                        return key;

            // add to download pending
            int id = Core.RndGen.Next();
            DownloadMap[id] = new FileDownload(Core, id, target, details, args, endEvent);

            Pending.AddLast(id);

            return id;
        }

        internal void AddSource(int id, ulong key)
        {
            if (!DownloadMap.ContainsKey(id))
                return;

            Core.Locations.LocationMap.LockReading(delegate()
            {
                if (Core.Locations.LocationMap.ContainsKey(key))
                    foreach (LocInfo info in Core.Locations.LocationMap[key].Values)
                        if (!info.Location.Global) //crit works when proxying over global?
                            DownloadMap[id].AddSource(info.Location);
            });
        }

        internal void CancelDownload(ushort id, byte[] hash, long size)
        {
            FileDownload target = null;

            lock (DownloadMap)
                foreach (FileDownload download in DownloadMap.Values)
                    if (download.Details.Component == id &&
                       download.Details.Size == size &&
                       Utilities.MemCompare(download.Details.Hash, hash))
                    {
                        target = download;
                        break;
                    }

            if(target != null)
                lock (DownloadMap)
                {
                    if (Pending.Contains(target.ID))
                        Pending.Remove(target.ID);

                    if (Active.Contains(target.ID))
                        Active.Remove(target.ID);
                    
                    DownloadMap.Remove(target.ID);
                }
        }

        void Core_Timer()
        {
            // move downloads from pending to active
            if (Active.Count < ConcurrentDownloads && Pending.Count > 0)
            {
                int id = Pending.First.Value;
                Pending.RemoveFirst();
                Active.Add(id);

                FileDownload transfer = DownloadMap[id];

                byte[] parameters = transfer.Details.Encode(Core.Protocol);

                DhtSearch search = Core.OperationNet.Searches.Start(transfer.Target, "Transfer", ComponentID.Transfer, parameters, new EndSearchHandler(EndSearch));
                
                if (search != null)
                {
                    transfer.Searching = true;
                    search.Carry = transfer;
                }
            }

            foreach (int id in Active)
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
            }


            // run code below every 5 secs
            if(Core.TimeNow.Second % 5 != 0)
                return;


            //remove dead uploads
            lock (UploadMap)
            {
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
            }

            // remove dead downloads
            lock (DownloadMap)
            {
                List<int> removeList = new List<int>();

                foreach (int id in Active)
                    if (DownloadMap[id].Status == DownloadStatus.Failed ||
                        DownloadMap[id].Status == DownloadStatus.Done)
                        removeList.Add(id);

                foreach (int id in removeList)
                {
                    try
                    {
                        DownloadMap[id].CloseStream();
                        File.Delete(DownloadMap[id].Destination);
                    }
                    catch { }

                    Active.Remove(id);
                    DownloadMap.Remove(id);
                }
            }
        }


        internal string GetStatus(ushort id, byte[] hash, long size)
        {
            FileDownload target = null;

            lock (DownloadMap)
                foreach (FileDownload download in DownloadMap.Values)
                    if (download.Details.Component == id &&
                       download.Details.Size == size &&
                       Utilities.MemCompare(download.Details.Hash, hash))
                    {
                        target = download;
                        break;
                    }

            if(target == null)
                return null;

            // pending
            if(Pending.Contains(target.ID))
                return "Pending";

            if (Active.Contains(target.ID))
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

        internal override void GetActiveSessions( ActiveSessions active)
        {
            foreach (FileDownload transfer in DownloadMap.Values)
                foreach (RudpSession session in transfer.Sessions)
                    active.Add(session);

            foreach (RudpSession session in UploadMap.Keys)
                active.Add(session);
        }

        List<byte[]> Search_Local(ulong key, byte[] parameters)
        {
            if (parameters == null)
            {
                Core.OperationNet.UpdateLog("Transfers", "Search Recieved with null parameters");
                return null;
            }

            FileDetails details = FileDetails.Decode(Core.Protocol, parameters);

            if (details == null || Core.Locations.LocalLocation == null)
                return null;

            // reply with loc info if a component has the file
            if(FileSearch.ContainsKey(details.Component))
                if (FileSearch[details.Component].Invoke(key, details))
                {
                    List<Byte[]> results = new List<byte[]>();
                    results.Add(Core.Locations.LocalLocation.Location.Encode(Core.Protocol));
                    return results;
                }

            return null;
        }

        void EndSearch(DhtSearch search)
        {
            FileDownload download = search.Carry as FileDownload;

            if(download == null)
                return;

            download.Searching = false;

            foreach (SearchValue found in search.FoundValues)
            {
                try
                {
                    LocationData location = LocationData.Decode(Core.Protocol, found.Value);
                    
                    Core.IndexKey(location.KeyID, ref location.Key);
                    download.AddSource(location);
                }
                catch (Exception ex)
                {
                    Core.OperationNet.UpdateLog("Transfer", "Search Results error " + ex.Message);
                }
            }
            
            // if no results mark as dead
            if (download.Sources.Count == 0)
                download.Status = DownloadStatus.Failed;
        }

        private void Connect(FileDownload download)
        {
            for(int i = 0; i < download.Sources.Count; i++)
                if (!download.Attempted.Contains(i))
                {
                    // if connected to source
                    if(Core.RudpControl.IsConnected(download.Sources[i]))
                        Send_Request( Core.RudpControl.GetSession(download.Sources[i]), download);
                    else
                        Core.RudpControl.Connect(download.Sources[i]);

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
            
            foreach (int id in Active)
            {
                FileDownload download = DownloadMap[id];

                // active
                if(session.Status == SessionStatus.Active)
                    if ( download.Status != DownloadStatus.Done && !download.Sessions.Contains(session))
                            foreach(LocationData source in download.Sources)
                                if (source.KeyID == session.DhtID && source.Source.ClientID == session.ClientID)
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
            }
        }

        void Send_Request(RudpSession session, FileDownload download)
        {
            download.RequestSent++;

            download.Status = DownloadStatus.Transferring;

            if(!download.Sessions.Contains(session))
                download.Sessions.Add(session);

            TransferRequest request = new TransferRequest(download, Core.Protocol);
            session.SendData(ComponentID.Transfer, request, true);
        }

        void Session_Data(RudpSession session, byte[] data)
        {
            G2Header root = new G2Header(data);

            if(Core.Protocol.ReadPacket(root))
            {
                switch(root.Name)
                {
                    case TransferPacket.Request:
                        Process_Request(session, TransferRequest.Decode(Core.Protocol, root));
                        break;

                    case TransferPacket.Ack:
                        Process_Ack(session, TransferAck.Decode(Core.Protocol, root));
                        break;

                    case TransferPacket.Data:
                        Process_Data(session, TransferData.Decode(Core.Protocol, root));
                        break;
                }
            }
        }

        private void Process_Request(RudpSession session, TransferRequest request)
        {
            FileDetails details = FileDetails.Decode(Core.Protocol, request.Details);

            // ask component for path to file
            string path = null;

            if (FileRequest.ContainsKey(details.Component))
                path = FileRequest[details.Component].Invoke(request.Target, details);


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
            session.SendData(ComponentID.Transfer, ack, true);


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

                        if (!session.SendData(ComponentID.Transfer, data, false))
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
                    download.Log("Data received, pos " + download.FilePos.ToString() + ", size " + data.Data.Length.ToString());
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
            Destination += Path.DirectorySeparatorChar + Utilities.CryptFilename(core.User.Settings.FileKey, (ulong)Details.Size, Details.Hash);
        }

        internal void AddSource(LocationData location)
        {
            foreach (LocationData source in Sources)
                if (source.KeyID == location.KeyID && source.Source.ClientID == location.Source.ClientID)
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
        internal const int READ_SIZE = 2048;

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
