using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using RiseOp.Services;
using RiseOp.Services.Location;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol.Net;

namespace RiseOp.Implementation.Transport
{
    internal delegate void SessionUpdateHandler(RudpSession session);
    internal delegate void SessionDataHandler(RudpSession session, byte[] data);
    internal delegate void KeepActiveHandler(Dictionary<ulong, bool> active);


    internal class RudpHandler
    {
        // maintain connections to those needed by components (timer)
        // keep up to date with location changes / additions (locationdata index)
        // notify components on connection changes (event)
        // remove connections when components no longer interested in node (timer)

        internal DhtNetwork Network;

        internal Dictionary<ulong, RudpSession> SessionMap = new Dictionary<ulong, RudpSession>();
        internal Dictionary<ushort, RudpSocket> SocketMap = new Dictionary<ushort, RudpSocket>();

        internal SessionUpdateHandler SessionUpdate;
        internal ServiceEvent<SessionDataHandler> SessionData = new ServiceEvent<SessionDataHandler>();
        internal KeepActiveHandler KeepActive;

        internal RudpHandler(DhtNetwork network)
        {
            Network = network;
        }

        internal void SecondTimer()
        {
            List<ulong> closed = new List<ulong>();

            foreach (RudpSession session in SessionMap.Values)
            {
                session.SecondTimer();

                if (session.Comm.State == RudpState.Closed)
                {
                    lock (SocketMap)
                        if (SocketMap.ContainsKey(session.Comm.PeerID))
                            SocketMap.Remove(session.Comm.PeerID);

                    closed.Add(session.RoutingID);
                }
            }

            foreach (ulong id in closed)
                SessionMap.Remove(id);


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

        internal bool Connect(DhtClient client)
        {
            if (client.UserID == Network.Local.UserID && client.ClientID == Network.Local.ClientID)
                return false;

            //if (IsConnectingOrActive(client.UserID, client.ClientID))
            //    return;

            //crit
            if(SessionMap.ContainsKey(client.RoutingID))
                return false;

            RudpSession session = new RudpSession(this, client.UserID, client.ClientID, false);
            SessionMap[client.RoutingID] = session;


            if (Network.LightComm.Clients.ContainsKey(client.RoutingID))
                foreach (RudpAddress address in Network.LightComm.Clients[client.RoutingID].Addresses)
                    session.Comm.AddAddress(address);

            session.Connect();

            return true; // indicates that we will eventually notify caller with close, so caller can clean up
        }

        internal void Connect(LocationData location)
        {
            if (location.UserID == Network.Local.UserID && location.Source.ClientID == Network.Local.ClientID)
                return;

            //crit
            //if (IsConnectingOrActive(location.UserID, location.Source.ClientID))
            //    return;

            ulong id = location.UserID ^ location.Source.ClientID;

            if (SessionMap.ContainsKey(id))
                return;

            RudpSession session = new RudpSession(this, location.UserID, location.Source.ClientID, false);
            SessionMap[id] = session;

            session.Comm.AddAddress(new RudpAddress(new DhtAddress(location.IP, location.Source)));

            foreach (DhtAddress address in location.Proxies)
                session.Comm.AddAddress(new RudpAddress(address));

            foreach (DhtAddress server in location.TunnelServers)
                session.Comm.AddAddress(new RudpAddress(new DhtContact(location.Source, location.IP, location.TunnelClient, server)));

            session.Connect(); 
        }

        internal List<RudpSession> GetActiveSessions(ulong key)
        {
            return (from session in SessionMap.Values
                    where session.Status == SessionStatus.Active &&
                          session.UserID == key
                    select session).ToList();
        }

        internal RudpSession GetActiveSession(DhtClient client)
        {
            return GetActiveSession(client.UserID, client.ClientID);
        }

        internal RudpSession GetActiveSession(ulong key, ushort client)
        {
            return (from session in SessionMap.Values
                    where session.Status == SessionStatus.Active &&
                          session.UserID == key && session.ClientID == client
                    select session).ElementAtOrDefault(0);
        }

        internal bool IsConnectingOrActive(ulong key, ushort client)
        {
            int notClosed = (from session in SessionMap.Values
                             where session.Status != SessionStatus.Closed &&
                                   session.UserID == key && session.ClientID == client
                             select session).Count();

            return (notClosed > 0);
        }

        internal bool IsConnected(ulong id)
        {
            int active = (from session in SessionMap.Values
                          where session.Status == SessionStatus.Active &&
                                session.UserID == id
                          select session).Count();

            return (active > 0);
        }

        internal void AnnounceProxy(TcpConnect tcp)
        {
            Debug.Assert(!Network.IsGlobal);

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


        internal void Shutdown()
        {
            foreach (RudpSession session in SessionMap.Values)
                if (session.Status != SessionStatus.Closed)
                    session.Send_Close("Going Offline");
        }
    }

    internal class ActiveSessions
    {
        List<ulong> AllSessions = new List<ulong>();
        Dictionary<ulong, List<ushort>> Sessions = new Dictionary<ulong, List<ushort>>();

        internal ActiveSessions()
        {
        }

        internal void Add(RudpSession rudp)
        {
            Add(rudp.UserID, rudp.ClientID);
        }

        internal void Add(ulong key, ushort id)
        {
            if (!Sessions.ContainsKey(key))
                Sessions[key] = new List<ushort>();

            if (!Sessions[key].Contains(id))
                Sessions[key].Add(id);
        }

        internal void Add(ulong key)
        {
            if (AllSessions.Contains(key))
                return;

            AllSessions.Add(key);
        }

        internal bool Contains(RudpSession rudp)
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
