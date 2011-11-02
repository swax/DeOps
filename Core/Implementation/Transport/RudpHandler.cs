using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using DeOps.Services;
using DeOps.Services.Location;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol.Net;

namespace DeOps.Implementation.Transport
{
    public delegate void SessionUpdateHandler(RudpSession session);
    public delegate void SessionDataHandler(RudpSession session, byte[] data);
    public delegate void KeepActiveHandler(Dictionary<ulong, bool> active);


    public class RudpHandler
    {
        // maintain connections to those needed by components (timer)
        // keep up to date with location changes / additions (locationdata index)
        // notify components on connection changes (event)
        // remove connections when components no longer interested in node (timer)

        public DhtNetwork Network;

        public Dictionary<ulong, RudpSession> SessionMap = new Dictionary<ulong, RudpSession>();
        public Dictionary<ushort, RudpSocket> SocketMap = new Dictionary<ushort, RudpSocket>();

        public SessionUpdateHandler SessionUpdate;
        public ServiceEvent<SessionDataHandler> SessionData = new ServiceEvent<SessionDataHandler>();
        public KeepActiveHandler KeepActive;

        public RudpHandler(DhtNetwork network)
        {
            Network = network;
        }

        public void SecondTimer()
        {
            foreach (RudpSession session in SessionMap.Values)
                session.SecondTimer();

            foreach (RudpSession session in SessionMap.Values.Where(s => s.Comm.State == RudpState.Closed).ToArray())
                RemoveSession(session);

            // every 10 secs check for sessions no longer in use
            if (Network.Core.TimeNow.Second % 10 != 0)
                return;

            Dictionary<ulong, bool> active = new Dictionary<ulong, bool>();

            if (KeepActive != null)
                foreach (KeepActiveHandler handler in KeepActive.GetInvocationList())
                    handler.Invoke(active);

            foreach (RudpSession session in SessionMap.Values)
                if (session.Status == SessionStatus.Active)
                    if (active.ContainsKey(session.UserID))
                        session.Lingering = 0;
                    else if (session.Lingering > 1) // ~10 secs to linger
                        session.Send_Close("Not Active");
                    else
                        session.Lingering++;
        }

        public void RemoveSession(RudpSession session)
        {
            lock (SocketMap)
                if (SocketMap.ContainsKey(session.Comm.PeerID))
                    SocketMap.Remove(session.Comm.PeerID);

            SessionMap.Remove(session.RoutingID);
        }

        public bool Connect(DhtClient client)
        {
            if (client.UserID == Network.Local.UserID && client.ClientID == Network.Local.ClientID)
                return false;

            // sessionmap and socketmap both need to have the same # of entries
            // if a session is fin or closed, we need to wait for fins to complete before re-assigning entry
            if(SessionMap.ContainsKey(client.RoutingID))
                return SessionMap[client.RoutingID].Status != SessionStatus.Closed;

            RudpSession session = new RudpSession(this, client.UserID, client.ClientID, false);
            SessionMap[client.RoutingID] = session;


            if (Network.LightComm.Clients.ContainsKey(client.RoutingID))
                foreach (RudpAddress address in Network.LightComm.Clients[client.RoutingID].Addresses)
                    session.Comm.AddAddress(address);

            session.Connect();

            return true; // indicates that we will eventually notify caller with close, so caller can clean up
        }

        public bool Connect(LocationData location)
        {
            if (location.UserID == Network.Local.UserID && location.Source.ClientID == Network.Local.ClientID)
                return false;

            ulong id = location.UserID ^ location.Source.ClientID;

            if (SessionMap.ContainsKey(id))
                return SessionMap[id].Status != SessionStatus.Closed;

            RudpSession session = new RudpSession(this, location.UserID, location.Source.ClientID, false);
            SessionMap[id] = session;

            session.Comm.AddAddress(new RudpAddress(new DhtAddress(location.IP, location.Source)));

            foreach (DhtAddress address in location.Proxies)
                session.Comm.AddAddress(new RudpAddress(address));

            foreach (DhtAddress server in location.TunnelServers)
                session.Comm.AddAddress(new RudpAddress(new DhtContact(location.Source, location.IP, location.TunnelClient, server)));

            session.Connect();

            return true;
        }

        public List<RudpSession> GetActiveSessions(ulong key)
        {
            return (from session in SessionMap.Values
                    where session.Status == SessionStatus.Active &&
                          session.UserID == key
                    select session).ToList();
        }

        public RudpSession GetActiveSession(DhtClient client)
        {
            return GetActiveSession(client.UserID, client.ClientID);
        }

        public RudpSession GetActiveSession(ulong key, ushort client)
        {
            return (from session in SessionMap.Values
                    where session.Status == SessionStatus.Active &&
                          session.UserID == key && session.ClientID == client
                    select session).FirstOrDefault();
        }

        public bool IsConnectingOrActive(ulong key, ushort client)
        {
            int notClosed = (from session in SessionMap.Values
                             where session.Status != SessionStatus.Closed &&
                                   session.UserID == key && session.ClientID == client
                             select session).Count();

            return (notClosed > 0);
        }

        public bool IsConnected(ulong id)
        {
            int active = (from session in SessionMap.Values
                          where session.Status == SessionStatus.Active &&
                                session.UserID == id
                          select session).Count();

            return (active > 0);
        }

        public void AnnounceProxy(TcpConnect tcp)
        {
            Debug.Assert(!Network.IsLookup);

            // function run bewteen cores
            if (Network.Core.InvokeRequired)
            {
                Network.Core.RunInCoreAsync(delegate() { AnnounceProxy(tcp); });
                return;
            }

            foreach (RudpSession session in SessionMap.Values)
                if (session.Status == SessionStatus.Active)
                    session.Send_ProxyUpdate(tcp);
        }


        public void Shutdown()
        {
            foreach (RudpSession session in SessionMap.Values)
                if (session.Status != SessionStatus.Closed)
                    session.Send_Close("Going Offline");
        }
    }

    public class ActiveSessions
    {
        List<ulong> AllSessions = new List<ulong>();
        Dictionary<ulong, List<ushort>> Sessions = new Dictionary<ulong, List<ushort>>();

        public ActiveSessions()
        {
        }

        public void Add(RudpSession rudp)
        {
            Add(rudp.UserID, rudp.ClientID);
        }

        public void Add(ulong key, ushort id)
        {
            if (!Sessions.ContainsKey(key))
                Sessions[key] = new List<ushort>();

            if (!Sessions[key].Contains(id))
                Sessions[key].Add(id);
        }

        public void Add(ulong key)
        {
            if (AllSessions.Contains(key))
                return;

            AllSessions.Add(key);
        }

        public bool Contains(RudpSession rudp)
        {
            if (AllSessions.Contains(rudp.UserID))
                return true;

            if (!Sessions.ContainsKey(rudp.UserID))
                return false;

            if (Sessions[rudp.UserID].Contains(rudp.ClientID))
                return true;

            return false;
        }
    }
}
