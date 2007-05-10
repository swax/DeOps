using System;
using System.Collections.Generic;
using System.Text;

using DeOps.Components;
using DeOps.Components.Location;
using DeOps.Implementation.Protocol.Net;

namespace DeOps.Implementation.Transport
{
    internal delegate void SessionUpdateHandler(RudpSession session);
    internal delegate void SessionDataHandler(RudpSession session, byte[] data);


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


        internal RudpHandler(OpCore core)
        {
            Core = core;
        }

        internal void SecondTimer()
        {
            List<ulong> removeKeys = new List<ulong>();
            List<RudpSession> removeSessions = new List<RudpSession>();

            // remove dead sessions
            lock (SessionMap)
            {
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
            }

            // every 10 secs check for sessions no longer in use
            if (Core.TimeNow.Second % 10 != 0)
                return;

            ActiveSessions active = new ActiveSessions();

            foreach (OpComponent component in Core.Components.Values)
                component.GetActiveSessions(ref active);

            foreach(List<RudpSession> list in SessionMap.Values)
                foreach(RudpSession session in list)
                    if(session.Status == SessionStatus.Active && !active.Contains(session))
                        session.Send_Close("Not Active");

        }

        internal void Connect(LocationData location)
        {
            if (location.KeyID == Core.LocalDhtID && location.Source.ClientID == Core.ClientID)
                return;

            if (IsConnected(location))
                return;

            lock (SessionMap)
            {
                if (!SessionMap.ContainsKey(location.KeyID))
                    SessionMap[location.KeyID] = new List<RudpSession>();

                RudpSession session = new RudpSession(Core, location.KeyID, location.Source.ClientID, false);
                SessionMap[location.KeyID].Add(session);

                session.Comm.AddAddress(new RudpAddress(Core, new DhtAddress(location.IP, location.Source), location.Global));

                foreach (DhtAddress address in location.Proxies)
                    session.Comm.AddAddress(new RudpAddress(Core, address, location.Global));

                session.Connect();
            }
        }

        internal RudpSession GetSession(LocationData location)
        {
            lock (SessionMap)
                if (SessionMap.ContainsKey(location.KeyID))
                    foreach (RudpSession session in SessionMap[location.KeyID])
                        if (session.ClientID == location.Source.ClientID)
                            return session;

            return null;
        }

        internal bool IsConnected(LocationData location)
        {
            RudpSession session = GetSession(location);

            if (session != null)
                if (session.Comm.State == RudpState.Connected)
                    return true;

            return false;
        }

        internal bool IsConnected(ulong id)
        {
            lock (SessionMap)
                if (SessionMap.ContainsKey(id))
                    foreach (RudpSession session in SessionMap[id])
                        if (session.Status == SessionStatus.Active)
                            return true;

            return false;
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
