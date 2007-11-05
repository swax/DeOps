using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Implementation.Transport;
using DeOps.Implementation.Protocol;
using DeOps.Components.Link;
using DeOps.Components.Location;


namespace DeOps.Components.IM
{
    internal delegate void IM_UpdateHandler(ulong dhtid, InstantMessage message);

    internal class IMControl : OpComponent
    {
        const int SessionTimeout = 10;

        internal OpCore Core;

        List<ushort> ConnectedClients = new List<ushort>();
        Dictionary<ulong, TtlObj> SessionMap = new Dictionary<ulong, TtlObj>();
        internal ThreadedDictionary<ulong, List<InstantMessage>> MessageLog = new ThreadedDictionary<ulong, List<InstantMessage>>();

        internal IM_UpdateHandler IM_Update;


        internal IMControl(OpCore core)
        {
            Core = core;

            Core.LoadEvent  += new LoadHandler(Core_Load);
            Core.ExitEvent += new ExitHandler(Core_Exit);
            Core.TimerEvent += new TimerHandler(Core_Timer);
            
            Core.RudpControl.SessionUpdate    += new SessionUpdateHandler(Session_Update);
            Core.RudpControl.SessionData[ComponentID.IM] = new SessionDataHandler(Session_Data);
        }

        void Core_Load()
        {
            Core.Links.LinkUpdate += new LinkUpdateHandler(Link_Update);
            Core.Locations.LocationUpdate += new LocationUpdateHandler(Location_Update);
        }

        void Core_Timer()
        {
            // need keep alives because we might have IM window open while other party has it closed

            // send keep alives every x secs
            if (Core.TimeNow.Second % SessionTimeout == 0)
            {
                List<IM_View> views = GetViews();

                foreach (IM_View view in views)
                    if (Core.RudpControl.SessionMap.ContainsKey(view.DhtID))
                        foreach (RudpSession session in Core.RudpControl.SessionMap[view.DhtID])
                        {
                            SessionMap[view.DhtID] = new TtlObj(SessionTimeout);
                            session.SendData(ComponentID.IM, new IMKeepAlive(), true);
                        }
            }

            // timeout sessions
            List<ulong> removeKeys = new List<ulong>();

            foreach (ulong id in SessionMap.Keys)
            {
                if (SessionMap[id].Ttl == 0)
                    removeKeys.Add(id);
                else if (SessionMap[id].Ttl > 0)
                    SessionMap[id].Ttl--;
            }

            foreach (ulong id in removeKeys)
                SessionMap.Remove(id);
        }

        private List<IM_View> GetViews()
        {
            List<IM_View> views = new List<IM_View>();

            if (IM_Update != null)
                foreach (Delegate func in IM_Update.GetInvocationList())
                    if (func.Target is IM_View)
                        views.Add((IM_View)func.Target);

            return views;
        }

        void Core_Exit()
        {
            if (IM_Update != null)
                throw new Exception("IM Events not fin'd");
        }

        internal override List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong key, uint proj)
        {
            List<MenuItemInfo> menus = new List<MenuItemInfo>();

            if (menuType == InterfaceMenuType.Quick)
            {
                if (key == Core.LocalDhtID)
                    return null;

                if (!Core.Locations.LocationMap.SafeContainsKey(key))
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

            if(view != null)
                view.BringToFront();

            // else create new window
            else
            {
                view = CreateView(node.GetKey());

                Connect(node.GetKey());
            }
        }

        private void Connect(ulong key)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { Connect(key); });
                return;
            }

            List<LocInfo> clients = Core.Locations.GetClients(key);

            foreach (LocInfo loc in clients)
            {
                if (Core.RudpControl.IsConnected(loc.Location))
                    ProcessMessage(key, new InstantMessage(Core, "Connected " + loc.Location.Place, true));

                else
                {
                    Core.RudpControl.Connect(loc.Location);
                    ProcessMessage(key, new InstantMessage(Core, "Connecting " + loc.Location.Place, true));
                }
            }
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

            IM_View view = new IM_View(this, key);

            Core.InvokeView(true, view);

            return view;
        }

        internal void Link_Update(OpLink link)
        {
            if (FindView(link.DhtID) == null)
                return;

            Core.RunInGuiThread(IM_Update, link.DhtID, null);
        }

        internal void Location_Update(LocationData location)
        {
            if (FindView(location.KeyID) == null)
                return;

            Core.RudpControl.Connect(location);
        }

        internal void Session_Update(RudpSession session)
        {
            if (FindView(session.DhtID) == null)
                return;

            LocationData location = Core.Locations.GetLocationInfo(session.DhtID, session.ClientID).Location ;

            string place = "";
            if (location != null)
                place = location.Place;

            if (session.Status == SessionStatus.Active)
            {
                if (!ConnectedClients.Contains(session.ClientID))
                {
                    ProcessMessage(session.DhtID, new InstantMessage(Core, "Connected " + place, true));
                    ConnectedClients.Add(session.ClientID);
                }

                session.SendData(ComponentID.IM, new IMKeepAlive(), true);
                SessionMap[session.DhtID] = new TtlObj(SessionTimeout);
            }

            else if (session.Status == SessionStatus.Closed && ConnectedClients.Contains(session.ClientID))
            {
                ProcessMessage(session.DhtID, new InstantMessage(Core, "Disconnected " + place, true));
                ConnectedClients.Remove(session.ClientID);
            }
        }

        internal void SendMessage(ulong key, string text)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { SendMessage(key, text); });
                return;
            }

            ProcessMessage(key, new InstantMessage(Core, text, false));

            if (!Core.RudpControl.SessionMap.ContainsKey(key))
            {
                ProcessMessage(key, new InstantMessage(Core, "Message not sent (not connected)", true));
                return;
            }

            MessageData message = new MessageData(text);

            if (Core.RudpControl.SessionMap.ContainsKey(key))
                foreach (RudpSession session in Core.RudpControl.SessionMap[key])
                    session.SendData(ComponentID.IM, message, true);
        }

        void Session_Data(RudpSession session, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (Core.Protocol.ReadPacket(root))
            {
                if (root.Name == IMPacket.Message)
                {
                    InstantMessage im = new InstantMessage(Core, session, MessageData.Decode(Core.Protocol, root));

                    ProcessMessage(session.DhtID, im);
                }

                if (root.Name == IMPacket.Alive)
                    SessionMap[session.DhtID] = new TtlObj(SessionTimeout * 2);
            }

        }

        internal void ProcessMessage(ulong key, InstantMessage message)
        {
            // log message - locks both dictionary and embedded list form reading
            MessageLog.LockWriting(delegate()
            {
                if (!MessageLog.SafeContainsKey(key))
                    MessageLog.SafeAdd(key,  new List<InstantMessage>());

                MessageLog[key].Add(message);
            });

            // update interface
            if (Core.GuiMain == null)
                return;

            IM_View view = FindView(key);

            if (view == null)
                CreateView(key);
            else
                Core.RunInGuiThread(IM_Update, key, message);
        }

        internal override void GetActiveSessions( ActiveSessions active)
        {
            foreach(ulong id in SessionMap.Keys)
                if (Core.RudpControl.SessionMap.ContainsKey(id))
                    foreach (RudpSession session in Core.RudpControl.SessionMap[id])
                        active.Add(session);
        }
    }



    internal class InstantMessage
    {
        internal ulong    Source;
        internal ushort   ClientID;
        internal DateTime TimeStamp;
        internal string   Text;
        internal bool     System;

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

    internal class TtlObj
    {
        internal int Ttl;

        internal TtlObj(int ttl)
        {
            Ttl = ttl;
        }
    }
}
