using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Implementation.Transport;
using RiseOp.Implementation.Protocol;
using RiseOp.Services.Trust;
using RiseOp.Services.Location;


namespace RiseOp.Services.IM
{
    internal delegate void IM_MessageHandler(ulong dhtid, InstantMessage message);
    internal delegate void IM_StatusHandler(ulong dhtid);

    internal class IMService : OpService
    {
        public string Name { get { return "IM"; } }
        public ushort ServiceID { get { return 5; } }

        const int SessionTimeout = 10;

        internal OpCore Core;
        internal TrustService Links;
        internal LocationService Locations;

        internal ThreadedDictionary<ulong, IMStatus> IMMap = new ThreadedDictionary<ulong, IMStatus>();

        internal IM_MessageHandler MessageUpdate;
        internal IM_StatusHandler StatusUpdate;


        internal IMService(OpCore core)
        {
            Core = core;
            Links = core.Links;
            Locations = core.Locations;

            Core.SecondTimerEvent += new TimerHandler(Core_SecondTimer);
            Core.GetFocusedCore += new GetFocusedHandler(Core_GetFocusedCore);

            Core.RudpControl.SessionUpdate += new SessionUpdateHandler(Session_Update);
            Core.RudpControl.SessionData[ServiceID, 0] += new SessionDataHandler(Session_Data);
            Core.RudpControl.KeepActive += new KeepActiveHandler(Session_KeepActive);

            Core.Links.LinkUpdate += new LinkUpdateHandler(Link_Update);
            Core.Locations.LocationUpdate += new LocationUpdateHandler(Location_Update);
        }

        public void Dispose()
        {
            if (MessageUpdate != null)
                throw new Exception("IM Events not fin'd");

            Core.SecondTimerEvent -= new TimerHandler(Core_SecondTimer);
            Core.GetFocusedCore -= new GetFocusedHandler(Core_GetFocusedCore);

            Core.RudpControl.SessionUpdate -= new SessionUpdateHandler(Session_Update);
            Core.RudpControl.SessionData[ServiceID, 0] -= new SessionDataHandler(Session_Data);
            Core.RudpControl.KeepActive -= new KeepActiveHandler(Session_KeepActive);

            Core.Links.LinkUpdate -= new LinkUpdateHandler(Link_Update);
            Core.Locations.LocationUpdate -= new LocationUpdateHandler(Location_Update);
        }

        void Core_SecondTimer()
        {
            // need keep alives because someone else might have IM window open while we have it closed

            // send keep alives every x secs
            if (Core.TimeNow.Second % SessionTimeout == 0)
            {
                List<IM_View> views = GetViews();

                foreach (IM_View view in views)
                {
                    IMStatus status = null;

                    if (IMMap.SafeTryGetValue(view.DhtID, out status))
                        foreach(ushort client in status.TTL.Keys)
                            if (status.TTL[client].Value > 0)
                            {
                                RudpSession session = Core.RudpControl.GetActiveSession(view.DhtID, client);

                                if (session != null)
                                {
                                    status.TTL[client].Value = SessionTimeout * 2;
                                    session.SendData(ServiceID, 0, new IMKeepAlive(), true);
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

        void Core_GetFocusedCore()
        {
            IMMap.LockReading(delegate()
            {
                foreach (ulong user in IMMap.Keys )
                    Core.Focused.SafeAdd(user, true);
            });
        }

        //crit not thread locked/protected
        private List<IM_View> GetViews()
        {
            List<IM_View> views = new List<IM_View>();

            if (MessageUpdate != null)
                foreach (Delegate func in MessageUpdate.GetInvocationList())
                    if (func.Target is IM_View)
                        views.Add((IM_View)func.Target);

            return views;
        }

        public List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong user, uint project)
        {
            List<MenuItemInfo> menus = new List<MenuItemInfo>();

            if (menuType == InterfaceMenuType.Quick)
            {
                if (user == Core.LocalDhtID)
                    return null;

                if (!Core.Locations.LocationMap.SafeContainsKey(user))
                    return null;

                menus.Add(new MenuItemInfo("Send IM", IMRes.Icon, new EventHandler(QuickMenu_View)));
            }

            return menus;
        }

        internal void QuickMenu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            // if window already exists to node, show it
            IM_View view = FindView(node.GetKey());

            if(view != null && view.External != null)
                view.External.BringToFront();

            // else create new window
            else
            {
                view = CreateView(node.GetKey());

                Core.RunInCoreAsync(delegate() { Connect(node.GetKey()); });
            }
        }

        private void Connect(ulong key)
        {
            Debug.Assert(!Core.InvokeRequired);

            IMStatus status = null;
            if(!IMMap.SafeTryGetValue(key, out status))
            {
                status = new IMStatus(key);
                IMMap.SafeAdd(key, status);
            }

            foreach (ClientInfo loc in Core.Locations.GetClients(key))
                Core.RudpControl.Connect(loc.Data);

            Update(status);
        }

        private void Update(IMStatus status)
        {
            ulong key = status.DhtID;

            // connected to jonn smith @home, @work
            // connecting to john smith
            // disconnected from john smith

            string places = "";


            status.Connected = false;
            status.Connecting = false;
            status.Away = false;
            string awayMessage = "";
            int activeCount = 0;

            foreach (RudpSession session in Core.RudpControl.GetActiveSessions(key))
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
                status.Text = Core.Links.GetName(key) + " is Away ";

                if (activeCount > 1)
                    status.Text += places.TrimEnd(',');
                else
                    status.Text += awayMessage;
            }

            else if (status.Connected)
            {
                status.Text = "Connected to " + Core.Links.GetName(key);
                
                if (activeCount > 1)
                    status.Text += places.TrimEnd(',');
            }

            else if(status.Connecting)
                status.Text = "Connecting to " + Core.Links.GetName(key);

            else
                status.Text = "Disconnected from " + Core.Links.GetName(key);


            Core.RunInGuiThread(StatusUpdate, status.DhtID);
        }

        private IM_View FindView(ulong key)
        {
            List<IM_View> views = GetViews();

            foreach (IM_View view in views)
                if (view.DhtID == key)
                    return view;

            return null;
        }

        private IM_View CreateView(ulong key)
        {
            if (Core.GuiMain == null)
                return null;

            if (Links.GetTrust(key) == null)
                Links.Research(key, 0, false);

            if (Locations.GetClients(key).Count == 0)
                Locations.Research(key);

            IM_View view = new IM_View(this, key);

            Core.InvokeView(true, view);

            return view;
        }

        internal void Link_Update(OpTrust trust)
        {
            if (FindView(trust.DhtID) == null)
                return;

            IMStatus status = null;
            if (IMMap.SafeTryGetValue(trust.DhtID, out status))
                Update(status);
        }

        internal void Location_Update(LocationData location)
        {
            if (FindView(location.KeyID) == null)
                return;

            IMStatus status = null;
            if (!IMMap.SafeTryGetValue(location.KeyID, out status))
                return;

            Core.RudpControl.Connect(location);

            Update(status);
        }

        internal void Session_Update(RudpSession session)
        {
            if (FindView(session.DhtID) == null)
                return;

            IMStatus status = null;
            if (!IMMap.SafeTryGetValue(session.DhtID, out status))
                return;


            if (session.Status == SessionStatus.Active)
            {
                // needs to be set here as well be cause we don't receive a keep alive from remote host on connect
                status.SetTTL(session.ClientID, SessionTimeout * 2);

                session.SendData(ServiceID, 0, new IMKeepAlive(), true);
            }

            Update(status);
        }

        internal void SendMessage(ulong key, string text)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { SendMessage(key, text); });
                return;
            }

            IMStatus status = null;
            if (!IMMap.SafeTryGetValue(key, out status))
                return;

            ProcessMessage(status, new InstantMessage(Core, text, false));

            if(!Core.RudpControl.IsConnected(key))
            {
                // run direct, dont log
                Core.RunInGuiThread(MessageUpdate, key, new InstantMessage(Core, "Could not send message, client disconnected", true));
                return;
            }

            MessageData message = new MessageData(text);

            foreach (RudpSession session in Core.RudpControl.GetActiveSessions(key))
                session.SendData(ServiceID, 0, message, true);
        }

        void Session_Data(RudpSession session, byte[] data)
        {
            IMStatus status = null;
            if (!IMMap.SafeTryGetValue(session.DhtID, out status))
            {
                status = new IMStatus(session.DhtID);
                IMMap.SafeAdd(session.DhtID, status);
            }

            

            G2Header root = new G2Header(data);

            if (Core.Protocol.ReadPacket(root))
            {
                if (root.Name == IMPacket.Message)
                {
                    InstantMessage im = new InstantMessage(Core, session, MessageData.Decode(Core.Protocol, root));

                    ProcessMessage(status, im);
                }

                if (root.Name == IMPacket.Alive)
                    status.SetTTL(session.ClientID, SessionTimeout * 2);
            }

        }

        internal void ProcessMessage(IMStatus status, InstantMessage message)
        {
            // log message - locks both dictionary and embedded list form reading
            status.MessageLog.SafeAdd(message);

            // update interface
            if (Core.GuiMain == null)
                return;

            Core.RunInGuiThread( (MethodInvoker) delegate()
            {
                IM_View view = FindView(status.DhtID);

                if (view == null)
                    CreateView(status.DhtID);
                else
                    MessageUpdate(status.DhtID, message);
            });

            Update(status);
        }

        void Session_KeepActive(Dictionary<ulong, bool> active)
        {
            IMMap.LockReading(delegate()
            {
                foreach(IMStatus status in IMMap.Values)
                     foreach(ushort client in status.TTL.Keys)
                         if (status.TTL[client].Value > 0)
                         {
                             active[status.DhtID] = true;
                             break;
                         }  
            });
        }
    }



    internal class InstantMessage
    {
        internal ulong    Source;
        internal ushort   ClientID;
        internal DateTime TimeStamp;
        internal string   Text;
        internal bool System;
        // local / system message
        internal InstantMessage(OpCore core, string text, bool system)
        {
            Source = core.LocalDhtID;
            ClientID = core.ClientID;
            TimeStamp = core.TimeNow;
            Text = text;
            System = system;
        }

        internal InstantMessage(OpCore core, RudpSession session, MessageData message)
        {
            Source = session.DhtID;
            ClientID = session.ClientID;
            TimeStamp = core.TimeNow;
            Text = message.Text;
        }
    }

    internal class IMStatus
    {
        internal ulong DhtID;
        internal Dictionary<ushort, BoxInt> TTL = new Dictionary<ushort, BoxInt>();
  
        internal string Text = "";
        internal bool Connected;
        internal bool Connecting;
        internal bool Away;

        internal ThreadedList<InstantMessage> MessageLog = new ThreadedList<InstantMessage>();
        
        internal IMStatus(ulong id)
        {
            DhtID = id;
        }

        internal void SetTTL(ushort client, int ttl)
        {
            if (!TTL.ContainsKey(client))
                TTL[client] = new BoxInt();

            TTL[client].Value = ttl;
        }
    }

    internal class BoxInt
    {
        internal int Value;
    }
}
