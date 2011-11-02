using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Transport;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;

using DeOps.Services.Location;
using DeOps.Services.Share;
using DeOps.Services.Trust;


namespace DeOps.Services.IM
{
    public delegate void IM_MessageHandler(ulong id, InstantMessage message);
    public delegate void IM_StatusHandler(ulong id);
    public delegate void CreateViewHandler(ulong id);

    public class IMService : OpService
    {
        public string Name { get { return "IM"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.IM; } }

        const int SessionTimeout = 10;

        public OpCore Core;
        public DhtNetwork Network;
        public LocationService Locations;

        public ThreadedDictionary<ulong, IMStatus> IMMap = new ThreadedDictionary<ulong, IMStatus>();
        public List<ulong> ActiveUsers = new List<ulong>();

        public IM_MessageHandler MessageUpdate;
        public IM_StatusHandler StatusUpdate;
        public CreateViewHandler CreateView;


        public IMService(OpCore core)
        {
            Core = core;
            Network = Core.Network;
            Locations = core.Locations;

            Core.SecondTimerEvent += Core_SecondTimer;
            Core.KeepDataCore += new KeepDataHandler(Core_KeepData);

            Network.RudpControl.SessionUpdate += new SessionUpdateHandler(Session_Update);
            Network.RudpControl.SessionData[ServiceID, 0] += new SessionDataHandler(Session_Data);
            Network.RudpControl.KeepActive += new KeepActiveHandler(Session_KeepActive);

            Core.Locations.LocationUpdate += new LocationUpdateHandler(Location_Update);
            Core.Locations.KnowOnline += new KnowOnlineHandler(Location_KnowOnline);

            if (Core.Trust != null)
                Core.Trust.LinkUpdate += new LinkUpdateHandler(Trust_Update);
        }

        public void Dispose()
        {
            if (MessageUpdate != null)
                throw new Exception("IM Events not fin'd");

            Core.SecondTimerEvent -= Core_SecondTimer;
            Core.KeepDataCore -= new KeepDataHandler(Core_KeepData);

            Network.RudpControl.SessionUpdate -= new SessionUpdateHandler(Session_Update);
            Network.RudpControl.SessionData[ServiceID, 0] -= new SessionDataHandler(Session_Data);
            Network.RudpControl.KeepActive -= new KeepActiveHandler(Session_KeepActive);

            Core.Locations.LocationUpdate -= new LocationUpdateHandler(Location_Update);
            Core.Locations.KnowOnline -= new KnowOnlineHandler(Location_KnowOnline);

            if (Core.Trust != null)
                Core.Trust.LinkUpdate -= new LinkUpdateHandler(Trust_Update);
        }

        void Core_SecondTimer()
        {
            // need keep alives because someone else might have IM window open while we have it closed

            // send keep alives every x secs
            if (Core.TimeNow.Second % SessionTimeout == 0)
            {
                foreach (var userID in ActiveUsers)
                {
                    IMStatus status = null;

                    if (IMMap.SafeTryGetValue(userID, out status))
                        foreach(ushort client in status.TTL.Keys)
                            if (status.TTL[client].Value > 0)
                            {
                                RudpSession session = Network.RudpControl.GetActiveSession(userID, client);

                                if (session != null)
                                {
                                    status.TTL[client].Value = SessionTimeout * 2;
                                    session.SendData(ServiceID, 0, new IMKeepAlive());
                                }
                            }
                }
            }

            // timeout sessions
            IMMap.LockReading(delegate()
            {
                foreach(IMStatus status in IMMap.Values)
                    foreach (BoxInt ttl in status.TTL.Values)
                        if (ttl.Value > 0)
                            ttl.Value--;
            });
        }

        public void SimTest()
        {
        }

        public void SimCleanup()
        {
        }

        void Core_KeepData()
        {
            IMMap.LockReading(delegate()
            {
                foreach (ulong user in IMMap.Keys )
                    Core.KeepData.SafeAdd(user, true);
            });
        }

        void Location_KnowOnline(List<ulong> users)
        {
            IMMap.LockReading(() => users.AddRange(IMMap.Keys));
        }

        /*crit not thread locked/protected
        private List<IM_View> GetViews()
        {
            List<IM_View> views = new List<IM_View>();

            if (MessageUpdate != null)
                foreach (Delegate func in MessageUpdate.GetInvocationList())
                    if (func.Target is IM_View)
                        views.Add((IM_View)func.Target);

            return views;
        }*/

        public void Connect(ulong user)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => Connect(user));
                return;
            }

            IMStatus status = OpenStatus(user);

            foreach (ClientInfo loc in Core.Locations.GetClients(user))
                Network.RudpControl.Connect(loc.Data);

            Update(status);
        }

        private void Update(IMStatus status)
        {
            ulong key = status.UserID;

            // connected to jonn smith @home, @work
            // connecting to john smith
            // disconnected from john smith

            string places = "";


            status.Connected = false;
            status.Connecting = false;
            status.Away = false;
            string awayMessage = "";
            int activeCount = 0;

            foreach(RudpSession session in Network.RudpControl.SessionMap.Values.Where(s => s.UserID == key))
            {
                if (session.Status == SessionStatus.Closed)
                    continue;

                status.Connecting = true;

                if (session.Status == SessionStatus.Active)
                {
                    status.Connected = true;

                    ClientInfo info = Locations.GetLocationInfo(key, session.ClientID);

                    awayMessage = "";
                    if (info != null)
                        if (info.Data.Away)
                        {
                            status.Away = true;
                            awayMessage = " " + info.Data.AwayMessage;
                        }

                    activeCount++;
                    places += " @" + Locations.GetLocationName(key, session.ClientID) + awayMessage + ",";
                }
            }

            if (status.Away)
            {
                status.Text = Core.GetName(key) + " is Away ";

                if (activeCount > 1)
                    status.Text += places.TrimEnd(',');
                else
                    status.Text += awayMessage;
            }

            else if (status.Connected)
            {
                status.Text = "Connected to " + Core.GetName(key);
                
                if (activeCount > 1)
                    status.Text += places.TrimEnd(',');
            }

            else if(status.Connecting)
                status.Text = "Connecting to " + Core.GetName(key);

            else
                status.Text = "Disconnected from " + Core.GetName(key);


            Core.RunInGuiThread(StatusUpdate, status.UserID);
        }

        public void Trust_Update(OpTrust trust)
        {
            if (!ActiveUsers.Contains(trust.UserID))
                return;

            IMStatus status = null;
            if (IMMap.SafeTryGetValue(trust.UserID, out status))
                Update(status);
        }

        public void Location_Update(LocationData location)
        {
            if (!ActiveUsers.Contains(location.UserID))
                return;

            IMStatus status = null;
            if (!IMMap.SafeTryGetValue(location.UserID, out status))
                return;

            Network.RudpControl.Connect(location);

            Update(status);
        }

        public void Session_Update(RudpSession session)
        {
            if (!ActiveUsers.Contains(session.UserID))
                return;

            IMStatus status = null;
            if (!IMMap.SafeTryGetValue(session.UserID, out status))
                return;


            if (session.Status == SessionStatus.Active)
            {
                // needs to be set here as well be cause we don't receive a keep alive from remote host on connect
                status.SetTTL(session.ClientID, SessionTimeout * 2);

                session.SendData(ServiceID, 0, new IMKeepAlive());
            }

            Update(status);
        }

        public void SendMessage(ulong key, string text, TextFormat format)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { SendMessage(key, text, format); });
                return;
            }

            if (text == "***debug***")
            {
                Core.DebugWindowsActive = true;
                text = "activated";
            }

            IMStatus status = null;
            if (!IMMap.SafeTryGetValue(key, out status))
                return;

           
            bool sent = false;
            MessageData message = new MessageData(text, format);

            foreach (RudpSession session in Network.RudpControl.GetActiveSessions(key))
            {
                sent = true; // only sent if target receies
                session.SendData(ServiceID, 0, message);
            }

            // send copies to other selves running
            message.TargetID = key;
            foreach (RudpSession session in Network.RudpControl.GetActiveSessions(Core.UserID))
                session.SendData(ServiceID, 0, message);

            ProcessMessage(status, new InstantMessage(Core, text, format) { Sent = sent });
        }

        void Session_Data(RudpSession session, byte[] data)
        {
            IMStatus status = OpenStatus(session.UserID);


            G2Header root = new G2Header(data);

            if (G2Protocol.ReadPacket(root))
            {
                if (root.Name == IMPacket.Message)
                {
                    MessageData message = MessageData.Decode(root);

                    if(message.TargetID != 0)
                    {
                        Debug.Assert(session.UserID == Core.UserID);
                        if(session.UserID != Core.UserID)
                            return;

                        status = OpenStatus(message.TargetID);
                    }

                    ProcessMessage(status, new InstantMessage(Core, session, message));
                }

                if (root.Name == IMPacket.Alive)
                    status.SetTTL(session.ClientID, SessionTimeout * 2);
            }
        }

        private IMStatus OpenStatus(ulong user)
        {
            IMStatus status;

            if (!IMMap.SafeTryGetValue(user, out status))
            {
                status = new IMStatus(user);
                IMMap.SafeAdd(user, status);
            }

            return status;
        }

        public void ProcessMessage(IMStatus status, InstantMessage message)
        {
            if (Core.Buddies.IgnoreList.SafeContainsKey(message.UserID))
                return;

            // log message - locks both dictionary and embedded list form reading
            status.MessageLog.SafeAdd(message);

            if(ActiveUsers.Contains(status.UserID))
                Core.RunInGuiThread(MessageUpdate, status.UserID, message);
            else
                Core.RunInGuiThread(CreateView, status.UserID);

            Update(status);
        }

        public void ReSearchUser(ulong userID)
        {
            if (Core.Trust != null && Core.Trust.GetTrust(userID) == null)
                Core.Trust.Research(userID, 0, false);

            if (Locations.GetClients(userID).Count == 0)
                Locations.Research(userID);
        }

        void Session_KeepActive(Dictionary<ulong, bool> active)
        {
            IMMap.LockReading(delegate()
            {
                foreach(IMStatus status in IMMap.Values)
                     foreach(ushort client in status.TTL.Keys)
                         if (status.TTL[client].Value > 0)
                         {
                             active[status.UserID] = true;
                             break;
                         }  
            });
        }

        public void Share_FileProcessed(SharedFile file, object arg)
        {
            ulong user = (ulong)arg;

            ShareService share = Core.GetService(ServiceIDs.Share) as ShareService;

            string message = "File: " + file.Name +
                ", Size: " + Utilities.ByteSizetoDecString(file.Size) +
                ", Download: " + share.GetFileLink(Core.UserID, file);

            SendMessage(user, message, TextFormat.Plain);
        }
    }



    public class InstantMessage : DhtClient
    {
        public DateTime TimeStamp;
        public string   Text;
        public TextFormat Format;
        public bool System;
        public bool Sent;

        // local / system message
        public InstantMessage(OpCore core, string text, TextFormat format)
        {
            UserID = core.UserID;
            ClientID = core.Network.Local.ClientID;
            TimeStamp = core.TimeNow;
            Text = text;
            Format = format;
            System = false;
        }

        public InstantMessage(OpCore core, RudpSession session, MessageData message)
        {
            UserID = session.UserID;
            ClientID = session.ClientID;
            TimeStamp = core.TimeNow;
            Text = message.Text;
            Format = message.Format;
        }
    }

    public class IMStatus
    {
        public ulong UserID;
        public Dictionary<ushort, BoxInt> TTL = new Dictionary<ushort, BoxInt>();
  
        public string Text = "";
        public bool Connected;
        public bool Connecting;
        public bool Away;

        public ThreadedList<InstantMessage> MessageLog = new ThreadedList<InstantMessage>();
        
        public IMStatus(ulong id)
        {
            UserID = id;
        }

        public void SetTTL(ushort client, int ttl)
        {
            if (!TTL.ContainsKey(client))
                TTL[client] = new BoxInt();

            TTL[client].Value = ttl;
        }
    }

    public class BoxInt
    {
        public int Value;
    }
}
