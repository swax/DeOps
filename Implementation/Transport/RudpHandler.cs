using System;
using System.Collections.Generic;
using System.Text;

using DeOps.Services;
using DeOps.Services.Location;
using DeOps.Implementation.Protocol.Net;

namespace DeOps.Implementation.Transport
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

        internal OpCore Core;

        internal Dictionary<ulong, List<RudpSession>> SessionMap = new Dictionary<ulong, List<RudpSession>>();

        internal SessionUpdateHandler SessionUpdate;
        internal Dictionary<ushort,SessionDataHandler> SessionData = new Dictionary<ushort,SessionDataHandler>();
        internal KeepActiveHandler KeepActive;

        internal RudpHandler(OpCore core)
        {
            Core = core;
        }

        internal void SecondTimer()
        {
            List<ulong> removeKeys = new List<ulong>();
            List<RudpSession> removeSessions = new List<RudpSession>();

            // remove dead sessions
            foreach (List<RudpSession> list in SessionMap.Values)
            {
                removeSessions.Clear();

                foreach (RudpSession session in list)
                    if (session.Status != SessionStatus.Closed)
                        session.SecondTimer();
                    else
                        removeSessions.Add(session);


                foreach (RudpSession session in removeSessions)
                    list.Remove(session);

                if (list.Count == 0)
                    removeKeys.Add(removeSessions[0].DhtID);
            }

            foreach (ulong key in removeKeys)
                SessionMap.Remove(key);

            // every 10 secs check for sessions no longer in use
            if (Core.TimeNow.Second % 10 != 0)
                return;

            Dictionary<ulong, bool> active = new Dictionary<ulong, bool>();

            if (KeepActive != null)
                foreach (KeepActiveHandler handler in KeepActive.GetInvocationList())
                    handler.Invoke(active);

            foreach(ulong key in SessionMap.Keys)
                if(!active.ContainsKey(key))
                    foreach (RudpSession session in SessionMap[key])
                        if(session.Status == SessionStatus.Active)
                            session.Send_Close("Not Active");

        }

        internal void Connect(LocationData location)
        {
            if (location.KeyID == Core.LocalDhtID && location.Source.ClientID == Core.ClientID)
                return;

            if (IsConnected(location))
                return;

            if (!SessionMap.ContainsKey(location.KeyID))
                SessionMap[location.KeyID] = new List<RudpSession>();

            RudpSession session = new RudpSession(Core, location.KeyID, location.Source.ClientID, false);
            SessionMap[location.KeyID].Add(session);

            session.Comm.AddAddress(new RudpAddress(Core, new DhtAddress(location.IP, location.Source), location.Global));

            foreach (DhtAddress address in location.Proxies)
                session.Comm.AddAddress(new RudpAddress(Core, address, location.Global));

            session.Connect(); 
        }

        internal RudpSession GetActiveSession(LocationData location)
        {
            return GetActiveSession(location.KeyID, location.Source.ClientID);
        }

        internal RudpSession GetActiveSession(ulong key, ushort client)
        {
            foreach (RudpSession session in GetActiveSessions(key))
                if (session.ClientID == client)
                    return session;

            return null;
        }

        internal List<RudpSession> GetActiveSessions(ulong key, bool onlyActive)
        {
            List<RudpSession> sessions = new List<RudpSession>();

            if (SessionMap.ContainsKey(key))
                foreach (RudpSession session in SessionMap[key])
                    if (!onlyActive || session.Status == SessionStatus.Active)
                        sessions.Add(session);

            return sessions;
        }

        internal List<RudpSession> GetActiveSessions(ulong key)
        {
            return GetActiveSessions(key, true);
        }

        internal bool IsConnected(LocationData location)
        {
            return ( GetActiveSession(location) != null );
        }

        internal bool IsConnected(ulong id)
        {
            return (GetActiveSessions(id).Count > 0);
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
            Add(rudp.DhtID, rudp.ClientID);
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
            if (AllSessions.Contains(rudp.DhtID))
                return true;

            if (!Sessions.ContainsKey(rudp.DhtID))
                return false;

            if (Sessions[rudp.DhtID].Contains(rudp.ClientID))
                return true;

            return false;
        }
    }
}
