/********************************************************************************

	De-Ops: Decentralized Operations
	Copyright (C) 2006 John Marshall Group, Inc.

	By contributing code you grant John Marshall Group an unlimited, non-exclusive
	license to your contribution.

	For support, questions, commercial use, etc...
	E-Mail: swabby@c0re.net

********************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using DeOps.Components;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Comm;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Transport;
using DeOps.Components.Location;
using DeOps.Interface.Tools;


namespace DeOps.Implementation.Dht
{
    internal delegate void EstablishedHandler();


    internal class DhtNetwork
    {
        const int MAX_CACHE = 200;

        // super-class
        internal OpCore Core; 

        // sub-class
        internal TcpHandler TcpControl;
        internal UdpHandler UdpControl;
        internal DhtRouting Routing;
        internal DhtStore   Store;
        internal DhtSearchControl Searches;

        internal LinkedList<IPCacheEntry> IPCache = new LinkedList<IPCacheEntry>();
        internal Dictionary<int, LinkedListNode<IPCacheEntry>> IPTable = new Dictionary<int, LinkedListNode<IPCacheEntry>>();

        internal bool IsGlobal;
        internal DateTime BootstrapTimeout;

        internal bool Established;
        internal int FireEstablished; // timeout until established is called
        internal event EstablishedHandler EstablishedEvent; // operation only

        internal RijndaelManaged OriginalCrypt;
        internal RijndaelManaged AugmentedCrypt;

        // log
        internal Queue<PacketLogEntry> LoggedPackets = new Queue<PacketLogEntry>();
        internal Dictionary<string, Queue<string>> LogTable = new Dictionary<string, Queue<string>>();

        // gui
        internal PacketsForm GuiPackets;
        internal CrawlerForm GuiCrawler;
        internal GraphForm GuiGraph;


        internal DhtNetwork(OpCore core, bool isGlobal)
        {
            Core = core;
            IsGlobal = isGlobal;

            // load ip cache, addlast so in same order it was saved in
            List<IPCacheEntry> cache = IsGlobal ? Core.User.GlobalCache : Core.User.OpCache;
            lock(IPCache)
                foreach (IPCacheEntry entry in cache)
                    IPTable.Add(entry.GetHashCode(), IPCache.AddLast(entry));

            // load encryption
            if (IsGlobal)
            {
                OriginalCrypt = new RijndaelManaged();
                OriginalCrypt.Key = new byte[] {0x33,0xf6,0x89,0xf3,0xd2,0xf5,0xae,0xc2,
                                            0x49,0x59,0xe6,0xbb,0xe2,0xc6,0x3c,0xc8,
                                            0x5e,0x63,0x0c,0x7a,0xb9,0x08,0x18,0xd4,
                                            0xf9,0x73,0x9f,0x52,0xd6,0xf4,0x34,0x0e};
            }
            else
                OriginalCrypt = Core.User.Settings.OpKey;


            AugmentedCrypt = new RijndaelManaged();
            AugmentedCrypt.Key = (byte[]) OriginalCrypt.Key.Clone();


            TcpControl  = new TcpHandler(this);
            UdpControl  = new UdpHandler(this);
            Routing     = new DhtRouting(this);
            Store       = new DhtStore(this);
            Searches    = new DhtSearchControl(this);
        }

        internal void SecondTimer()
        {
            // timers
            TcpControl.SecondTimer();
            Routing.SecondTimer();
            Searches.SecondTimer();

            // if unresponsive
            if (!Routing.Responsive())
                DoBootstrap(); 
            
            // ip cache
            lock (IPCache)
                while (IPCache.Count > MAX_CACHE)
                {
                    IPCacheEntry entry = IPCache.Last.Value;
                    IPTable.Remove(entry.GetHashCode());
                    IPCache.RemoveLast();
                }

            // established in dht
            if (FireEstablished > 0)
            {
                FireEstablished--;

                if (FireEstablished == 0)
                {
                    Established = true;

                    if (IsGlobal)
                        return;

                    if (EstablishedEvent != null)
                        EstablishedEvent.Invoke();
                }
            }
        }

        internal void AddCacheEntry(IPCacheEntry entry)
        {
            lock (IPCache)
            {
                if (IPTable.ContainsKey(entry.GetHashCode()))
                    IPCache.Remove(IPTable[entry.GetHashCode()]);

                IPTable[entry.GetHashCode()] = IPCache.AddFirst(entry);
            }
        }

        internal void UpdateLog(string type, string message)
        {
            lock (LogTable)
            {
                Queue<string> targetLog = null;

                if (LogTable.ContainsKey(type))
                    targetLog = LogTable[type];
                else
                    LogTable[type] = targetLog = new Queue<string>();

                targetLog.Enqueue(message);

                int logsize = (Core.Sim == null) ? 500 : 100;

                while (targetLog.Count > logsize)
                    targetLog.Dequeue();
            }
        }

        void DoBootstrap()
        {
            bool NoDelay = (IPCache.Count == 0 || Core.TimeNow > Core.StartTime.AddSeconds(10));

            // give a few seconds at startup to try to connect to Dht networks from the cache
            if (Core.TimeNow > BootstrapTimeout && NoDelay)
            {
                BootstrapTimeout = Core.TimeNow.AddMinutes(30);
                
                // global use web cache
                if (IsGlobal)
                {
                    if (Core.Sim == null)
                    {
                        Thread dlThread = new Thread(new ThreadStart(DownloadCache));
                        dlThread.Start();
                    }
                    else
                        Core.Sim.Internet.DownloadCache(this);
                }

                // else find operation nodes through global net
                else if (Core.GlobalNet != null)
                    Core.Locations.StartSearch(Core.OpID, 0, true);
            }


            // send pings to nodes in cache, responses will startup the routing system
            int pings = 0;

            lock (IPCache)
                foreach (IPCacheEntry entry in IPCache)
                {
                    if (Core.TimeNow < entry.NextTry)
                        continue;

                    Send_Ping(entry.Address);

                    entry.NextTry = Core.TimeNow.AddMinutes(10);

                    pings++;
                    if (pings >= 10)
                        break;
                }
                    

            // if blocked and go through cache and mark as tcp tried
            if (Core.Firewall == FirewallType.Blocked)

                lock (IPCache)
                    foreach (IPCacheEntry entry in IPCache)
                    {
                        if (Core.TimeNow < entry.NextTryTcp)
                            continue;

                        TcpControl.MakeOutbound(entry.Address, entry.TcpPort);

                        entry.NextTryTcp = Core.TimeNow.AddMinutes(10);
                        break;
                    }
        }

        void DownloadCache()
        {
            try
            {
                //crit - revise
               /* WebClient cacheSite = new System.Net.WebClient();

                UpdateLog("Network", "Requesting web cache...");
                Stream webStream = cacheSite.OpenRead("http://kim.c0re.net/cache.net");
                StreamReader cacheStream = new StreamReader(webStream);

                int entries = 0;
                string line = cacheStream.ReadLine();

                while (line != null)
                {
                    string[] addr = line.Split(':');

                    AddCacheEntry( new IPCacheEntry(IPAddress.Parse(addr[0]), Convert.ToUInt16(addr[1]), Convert.ToUInt16(addr[2])));
                    entries++;

                    line = cacheStream.ReadLine();
                }

                UpdateLog("Network", entries.ToString() + " entries read from web cache");

                cacheStream.Close();*/
            }
            catch (Exception ex)
            {
                UpdateLog("Exception", "KimCore::DownloadCache: " + ex.Message);
            }
        }

        internal void FirewallChangedtoOpen()
        {
            //close proxy connects
            lock (TcpControl.Connections)
                foreach (TcpConnect connection in TcpControl.Connections)
                    if (connection.State == TcpState.Connected) // close everything, even unset proxies
                        connection.CleanClose("Firewall changed to Open");
        }

        internal void FirewallChangedtoNAT()
        {
            //update proxy connects
            lock (TcpControl.Connections)
                foreach (TcpConnect connection in TcpControl.Connections)
                    if (connection.Proxy == ProxyType.Server)
                    {
                        ProxyReq request = new ProxyReq();
                        request.SenderID = Core.LocalDhtID;
                        request.Type = ProxyType.ClientNAT;
                        connection.SendPacket(request);
                    }
        }

        internal DhtSource GetLocalSource()
        {
            DhtSource source = new DhtSource();

            source.DhtID    = Core.LocalDhtID;
            source.ClientID = Core.ClientID;
            source.TcpPort  = TcpControl.ListenPort;
            source.UdpPort  = UdpControl.ListenPort;
            source.Firewall = Core.Firewall;

            return source;
        }

        internal DhtContact GetLocalContact()
        {
            return new DhtContact(Core.LocalDhtID, Core.ClientID, Core.LocalIP, TcpControl.ListenPort, UdpControl.ListenPort, Core.TimeNow);
        }



        internal void ReceivePacket(G2ReceivedPacket packet)
        {
            // Network packet
            if (packet.Root.Name == RootPacket.Network)
            {
                NetworkPacket netPacket = NetworkPacket.Decode(Core.Protocol, packet.Root);

                G2ReceivedPacket embedded = new G2ReceivedPacket();
                embedded.Tcp    = packet.Tcp;
                embedded.Source = packet.Source;
                embedded.Source.DhtID = netPacket.SourceID;
                embedded.Root   = new G2Header(netPacket.InternalData);

                // from - received from proxy server
                if (netPacket.FromAddress != null)
                {
                    if (packet.Tcp == null)
                        throw new Exception("From tag set on packet received udp");
                    if (packet.Tcp.Proxy != ProxyType.Server)
                        throw new Exception("From tag (" + netPacket.FromAddress.ToString() + ") set on packet not received from server (" + packet.Tcp.ToString() + ")");

                    embedded.Source = netPacket.FromAddress;
                }

                // to - received from proxied node
                if (netPacket.ToAddress != null)
                {
                    if (packet.Tcp == null)
                        throw new Exception("To tag set on packet received udp");
                    if (packet.Tcp.Proxy == ProxyType.Server || packet.Tcp.Proxy == ProxyType.Unset)
                        throw new Exception("To tag set on packet received from server");

                    if (netPacket.ToAddress.DhtID != Core.LocalDhtID)
                    {
                        DhtAddress address = netPacket.ToAddress;
                        netPacket.ToAddress = null;

                        //crit doesnt work for multiple clients with same dht id
                        if (TcpControl.ConnectionMap.ContainsKey(address.DhtID) &&
                            TcpControl.ConnectionMap[address.DhtID].State == TcpState.Connected)
                            TcpControl.ConnectionMap[address.DhtID].SendPacket(netPacket);
                        else
                            UdpControl.SendTo(address, netPacket);
                        
                        return;
                    }
                }

                // process
                if(Core.Protocol.ReadPacket(embedded.Root))
                    ReceiveNetworkPacket(embedded);
            }

            // Communication Packet
            if (packet.Root.Name == RootPacket.Comm)
            {
                RudpPacket commPacket = RudpPacket.Decode(Core.Protocol, packet);

                packet.Source.DhtID = commPacket.SenderID;

                // Also Forward to appropriate node
                //crit work for multiple clients
                if(TcpControl.ConnectionMap.ContainsKey(commPacket.TargetID))
                {
                    TcpConnect connection = TcpControl.ConnectionMap[commPacket.TargetID];

                    if (connection.State != TcpState.Connected)
                        return;

                    // strip TO flag, add from address
                    commPacket.ToEndPoint = null;
                    commPacket.FromEndPoint = packet.Source;

                    commPacket.SenderID = Core.LocalDhtID;
                    connection.SendPacket(commPacket);
                    return;
                }

                // forward udp if TO flag marked
                if (packet.Tcp != null && commPacket.ToEndPoint != null)
                {
                    DhtAddress address = commPacket.ToEndPoint;

                    commPacket.ToEndPoint = null; // strip TO flag

                    commPacket.SenderID = Core.LocalDhtID;
                    UdpControl.SendTo(address, commPacket);
                }

                // For local host
                if (commPacket.TargetID == Core.LocalDhtID)
                {
                    if (packet.Tcp != null && commPacket.FromEndPoint != null)
                        packet.Source = commPacket.FromEndPoint;

                    ReceiveCommPacket(packet, commPacket);
                    return;
                }  
            }
        }

        internal void ReceiveCommPacket(G2ReceivedPacket raw, RudpPacket packet)
        {
            try
            {
                // if a socket already set up
                lock (Core.CommMap)
                    if (Core.CommMap.ContainsKey(packet.PeerID))
                    {
                        Core.CommMap[packet.PeerID].RudpReceive(raw, packet);
                        return;
                    }

                // if starting new session
                if (packet.PacketType != RudpPacketType.Syn)
                    return;

                RudpSyn syn = new RudpSyn(packet.Payload);

                // prevent connection from self
                if (syn.SenderID == Core.LocalDhtID && syn.ClientID == Core.ClientID)
                    return;

                // get node
                /*OpNode node = null;
                
                // if node known
                if (Core.OperationNet.Store.Index.ContainsKey(syn.SenderID))
                    node = Core.OperationNet.Store.Index[syn.SenderID];

                // else create node
                else
                {
                    node = new OpNode(syn.SenderID, Core.Command);
                    Core.OperationNet.Store.Index[syn.SenderID] = node;
                }*/


                // find connecting session with same or unknown client id
                if (Core.RudpControl.SessionMap.ContainsKey(syn.SenderID))
                    foreach (RudpSession session in Core.RudpControl.SessionMap[syn.SenderID])
                    {
                        if (session.ClientID == syn.ClientID)
                        {
                            // if session id zero or matches forward
                            if ((session.Comm.State == RudpState.Connecting && session.Comm.RemotePeerID == 0) ||
                                (session.Comm.State != RudpState.Closed && session.Comm.RemotePeerID == syn.ConnID)) // duplicate syn
                            {
                                session.Comm.RudpReceive(raw, packet);
                            }
                            else
                                session.Log("Session request denied (already active)");

                            return;
                        }
                    }

                //crit check if this is the peer id of a failed connection attempt
                /*if (buddy.LastPeerIDs.Contains(syn.ConnID))
                {
                    buddy.Log("Session denied due to recent peer id");
                    return;
                }*/


                // if clientid not in session, create new session
                RudpSession newSession = new RudpSession(Core, syn.SenderID, syn.ClientID, true);

                if (!Core.RudpControl.SessionMap.ContainsKey(syn.SenderID))
                    Core.RudpControl.SessionMap[syn.SenderID] = new List<RudpSession>();

                Core.RudpControl.SessionMap[syn.SenderID].Add(newSession);

                
                // add addresses and connect
                RudpAddress address = new RudpAddress(Core, raw.Source, IsGlobal);
                address.LocalProxyID = raw.Tcp != null ? raw.Tcp.DhtID : 0;
                newSession.Comm.AddAddress(address);
                newSession.Connect();
                
                newSession.Comm.RudpReceive(raw, packet);
                
                
                UpdateLog("RUDP", "Inbound session accepted to ClientID " + syn.ClientID.ToString());
            }
            catch (Exception ex)
            {
                UpdateLog("Exception", "KimCore::ReceiveCommPacket: " + ex.Message);
            }
        }

        internal void Send_Ping(DhtAddress address)
        {
            Ping ping = new Ping();
            ping.Source = GetLocalSource();
            ping.RemoteIP = address.IP;

            UdpControl.SendTo(address, ping);   
        }

        internal void ReceiveNetworkPacket(G2ReceivedPacket packet)
        {
            // Search request
            if (packet.Root.Name == NetworkPacket.SearchRequest)
                Searches.ReceiveRequest(packet);

            // Search ack
            else if (packet.Root.Name == NetworkPacket.SearchAck)
                Searches.ReceiveAck(packet);

            // Ping
            else if (packet.Root.Name == NetworkPacket.Ping)
                Receive_Ping(packet);

            // Pong
            else if (packet.Root.Name == NetworkPacket.Pong)
                Receive_Pong(packet);

            // Store
            else if (packet.Root.Name == NetworkPacket.StoreRequest)
                Store.Receive_StoreReq(packet);

            // Proxy request
            else if (packet.Root.Name == NetworkPacket.ProxyRequest)
                Receive_ProxyRequest(packet);

            // Proxy ack
            else if (packet.Root.Name == NetworkPacket.ProxyAck)
                Receive_ProxyAck(packet);

            // Bye
            else if (packet.Root.Name == NetworkPacket.Bye && packet.Tcp != null)
                TcpControl.Receive_Bye(packet);

            // Crawl Request
            else if (packet.Root.Name == NetworkPacket.CrawlRequest)
                Receive_CrawlRequest(packet);

            // Crawl Ack
            else if (packet.Root.Name == NetworkPacket.CrawlAck)
                Receive_CrawlAck(packet);

            // unknown packet
            else
            {
                UpdateLog("Exception", "Uknown packet type " + packet.Root.Name.ToString());
            }
        }

        void Receive_Ping(G2ReceivedPacket packet)
        {
            Ping ping = Ping.Decode(Core.Protocol, packet);
            
            // setup pong reply
            if(ping.RemoteIP != null)
                Core.LocalIP = ping.RemoteIP;

            Pong pong = new Pong();
            pong.Source = GetLocalSource();
            pong.RemoteIP = packet.Source.IP;

            if (ping.RemoteIP != null)
                pong.RemoteIP = packet.Source.IP;

            // if received udp
            if (packet.Tcp == null)
            {
                if (ping.Source.DhtID == Core.LocalDhtID && ping.Source.ClientID == Core.ClientID) // loop back
                    return;

                Core.SetFirewallType(FirewallType.NAT);

                // if firewall flag not set add to routing
                if (ping.Source.Firewall == FirewallType.Open)
                    Routing.Add(new DhtContact(ping.Source, packet.Source.IP, Core.TimeNow));
                
                UdpControl.SendTo(packet.Source, pong);
            }

            // received tcp
            else
            {
                if (ping.Source.DhtID == Core.LocalDhtID && ping.Source.ClientID == Core.ClientID) // loopback
                {
                    packet.Tcp.CleanClose("Loopback connection");
                    return;
                }

                if (ping.Source.Firewall == FirewallType.Open)
                    Routing.Add(new DhtContact(ping.Source, packet.Source.IP, Core.TimeNow));

                if (!packet.Tcp.Outbound)
                    Core.SetFirewallType(FirewallType.Open); // received incoming tcp means we are not firewalled
                // done here to prevent setting open for loopback tcp connection

                if (packet.Tcp.DhtID == 0)
                {
                    packet.Tcp.DhtID = ping.Source.DhtID;
                    packet.Tcp.ClientID = ping.Source.ClientID;
                    packet.Tcp.TcpPort = ping.Source.TcpPort;
                    packet.Tcp.UdpPort = ping.Source.UdpPort;
                    TcpControl.ConnectionMap[ping.Source.DhtID] = packet.Tcp;
                }

                // if requesting a firewall check and havent checked yet
                if (ping.Source.Firewall != FirewallType.Open && !packet.Tcp.CheckedFirewall)
                {
                    TcpControl.MakeOutbound(packet.Source, ping.Source.TcpPort);
                    packet.Tcp.CheckedFirewall = true;
                }

                packet.Tcp.SendPacket(pong);

                // dont send close if proxies maxxed yet, because their id might be closer than current proxies
            }
        }

        void Receive_Pong(G2ReceivedPacket packet)
        {
            Pong pong = Pong.Decode(Core.Protocol, packet);

            if (pong.RemoteIP != null)
                Core.LocalIP = pong.RemoteIP;

            // if received udp
            if (packet.Tcp == null)
            {
                Core.SetFirewallType(FirewallType.NAT);

                // send bootstrap if Dht cache dead
                if (!Routing.Responsive())
                    Searches.SendUdpRequest(packet.Source, Core.LocalDhtID, 0, ComponentID.Node, null);

                // add to routing
                // on startup, especially in sim everyone starts blocked so pong source firewall is not set right, but still needs to go into routing
                if (pong.Source.Firewall == FirewallType.Open || Routing.BucketList.Count == 1)// !Routing.Responsive()) //crit hack
                    Routing.Add(new DhtContact(pong.Source, packet.Source.IP, Core.TimeNow));

                // forward to proxied nodes
                if (Core.Firewall == FirewallType.Open)
                {
                    pong.FromAddress = packet.Source;
                    pong.RemoteIP = null;

                    lock (TcpControl.Connections)
                        foreach (TcpConnect connection in TcpControl.Connections)
                            if (connection.State == TcpState.Connected &&
                                (connection.Proxy == ProxyType.ClientBlocked || connection.Proxy == ProxyType.ClientNAT))
                                connection.SendPacket(pong);
                }
            }

            // if received tcp
            else
            {
                if (pong.Source.Firewall == FirewallType.Open)
                    Routing.Add(new DhtContact(pong.Source, packet.Source.IP, Core.TimeNow));

                // if firewalled
                if (packet.Tcp.Proxy == ProxyType.Unset && pong.Source.DhtID == packet.Tcp.DhtID)
                {

                    if (Core.Firewall != FirewallType.Open && TcpControl.AcceptProxy(ProxyType.Server, pong.Source.DhtID))
                    {
                        // send proxy request
                        ProxyReq request = new ProxyReq();
                        request.SenderID = Core.LocalDhtID;
                        request.Type = (Core.Firewall == FirewallType.Blocked) ? ProxyType.ClientBlocked : ProxyType.ClientNAT;
                        packet.Tcp.SendPacket(request);
                    }

                    // else ping/pong done, end connect
                    else
                    	packet.Tcp.CleanClose("Not in need of a proxy");
                }
            }
        }

        internal void Receive_ProxyRequest(G2ReceivedPacket packet)
        {
            ProxyReq request = ProxyReq.Decode(Core.Protocol, packet);

            ProxyAck ack = new ProxyAck();
            ack.Source = GetLocalSource();

            // check if there is space for type required
            if (Core.Firewall == FirewallType.Open  && TcpControl.AcceptProxy(request.Type, ack.Source.DhtID))
            {
                ack.Accept = true;
            }
            else if (packet.Tcp != null)
            {
                packet.Tcp.CleanClose("Couldn't accept proxy request");
                return;
            }
                


            // always send some contacts along so node can find closer proxy
            ack.ContactList = Routing.Find(request.SenderID, 8);


            // received request tcp
            if (packet.Tcp == null)
                UdpControl.SendTo(packet.Source, ack);

            // received request tcp
            else
            {
                packet.Tcp.Proxy = request.Type;
                packet.Tcp.SendPacket(ack);

                // check if a proxy needs to be disconnected now because overflow
                TcpControl.CheckProxies();
            }
        }

        internal void Receive_ProxyAck(G2ReceivedPacket packet)
        {
            ProxyAck ack = ProxyAck.Decode(Core.Protocol, packet);

            // update routing
            if (packet.Tcp == null && ack.Source.Firewall == FirewallType.Open)
                Routing.Add(new DhtContact(ack.Source, packet.Source.IP, Core.TimeNow));

            foreach (DhtContact contact in ack.ContactList)
                Routing.Add(contact);


            // dont do proxy if we're not firewalled or remote host didnt accept
            if (Core.Firewall == FirewallType.Open || !ack.Accept)
            {
                if (packet.Tcp != null)
                    packet.Tcp.CleanClose("Proxy request rejected");

                return;
            }

            // received ack udp
            if (packet.Tcp == null)
            {
                if(!TcpControl.ConnectionMap.ContainsKey(ack.Source.DhtID))
                    TcpControl.MakeOutbound(packet.Source, ack.Source.TcpPort);
                
            }

            // received ack tcp
            else
            {
                packet.Tcp.Proxy = ProxyType.Server;

                TcpControl.CheckProxies();
            }
        }

        internal void Send_CrawlRequest(DhtAddress address)
        {
            CrawlReq req = new CrawlReq();

            req.Source = GetLocalSource();
            req.TargetID = address.DhtID;

            UdpControl.SendTo(address, req);
        }

        internal void Receive_CrawlRequest(G2ReceivedPacket packet)
        {
            CrawlReq req = CrawlReq.Decode(Core.Protocol, packet);

            // Not meant for local host, forward along
            if (req.TargetID != Core.LocalDhtID)
            {
                // Forward to appropriate node
                if (TcpControl.ConnectionMap.ContainsKey(req.TargetID))
                {
                    TcpConnect connection = TcpControl.ConnectionMap[req.TargetID];

                    // add so receiving host knows where to send response too
                    req.FromAddress = packet.Source;

                    connection.SendPacket(req);
                    return;
                }

                return;
            }


            // Send Ack Reply
            Send_CrawlAck(req, packet);

        }

        internal void Send_CrawlAck(CrawlReq req, G2ReceivedPacket packet)
        {
            CrawlAck ack = new CrawlAck();

            ack.Source  = GetLocalSource();
            ack.Version = System.Windows.Forms.Application.ProductVersion;
            ack.Uptime  = (Core.TimeNow - Core.StartTime).Seconds;
            ack.Depth   = Routing.BucketList.Count;

            // proxies as contact list, also need firewall type
            lock (TcpControl.Connections)
                foreach (TcpConnect connection in TcpControl.Connections)
                    if (connection.State == TcpState.Connected && connection.Proxy != ProxyType.Unset)
                        ack.ProxyList.Add(new DhtContact(connection, connection.RemoteIP, Core.TimeNow));

            if (packet.Tcp != null)
            {
                ack.ToAddress = packet.Source;
                packet.Tcp.SendPacket(ack);
            }
            else
                UdpControl.SendTo(packet.Source, ack);
        }

        internal void Receive_CrawlAck(G2ReceivedPacket packet)
        {
            CrawlAck ack = CrawlAck.Decode(Core.Protocol, packet);

            if (GuiCrawler != null)
                GuiCrawler.BeginInvoke(GuiCrawler.CrawlAck, ack, packet);

        }

        internal void LogPacket(PacketLogEntry logEntry)
        {
            if (Core.PauseLog)
                return;

            lock (LoggedPackets)
            {
                LoggedPackets.Enqueue(logEntry);

                while (LoggedPackets.Count > 50)
                    LoggedPackets.Dequeue();
            }

            if (GuiPackets != null)
                GuiPackets.BeginInvoke(GuiPackets.UpdateLog, logEntry);


            // log in console
            string message = logEntry.Protocol.ToString();
            message += (logEntry.Direction == DirectionType.In) ? " in from " : " out to ";
            message += logEntry.Address.ToString();

            G2ReceivedPacket packet = new G2ReceivedPacket();
            packet.Root = new G2Header(logEntry.Data);

            if(Core.Protocol.ReadPacket(packet.Root))
                message += ", type " + packet.Root.Name;
        }
    }


    internal class IPCacheEntry
    {
        internal static int BYTE_SIZE = 16;

        internal DhtAddress Address = new DhtAddress();
        internal ushort     TcpPort;
        internal DateTime   NextTry    = new DateTime(0);
        internal DateTime   NextTryTcp = new DateTime(0);

        IPCacheEntry()
        {
        }

        internal IPCacheEntry(DhtAddress address, ushort tcpPort)
        {
            Address = address;
            TcpPort = tcpPort;
        }

        internal IPCacheEntry(DhtContact contact)
        {
            Address.IP    = contact.Address;
            Address.DhtID = contact.DhtID;
            Address.UdpPort  = contact.UdpPort;
            TcpPort       = contact.TcpPort;
        }

        public override int GetHashCode()
        {
            return Address.GetHashCode() ^ TcpPort.GetHashCode();
        }

        public override string ToString()
        {
            return Address.IP.ToString() + ":" + TcpPort.ToString() + ":" + Address.UdpPort.ToString();
        }

        internal byte[] ToBytes()
        {
            byte[] bytes = new byte[BYTE_SIZE];

            Address.ToBytes().CopyTo(bytes, 0);
            BitConverter.GetBytes(TcpPort).CopyTo(bytes, 14);

            return bytes;
        }

        internal static IPCacheEntry FromBytes(byte[] data, int pos)
        {
            IPCacheEntry entry = new IPCacheEntry();

            entry.Address = DhtAddress.FromBytes(data, pos);
            entry.TcpPort = BitConverter.ToUInt16(data, pos + 14);

            return entry;
        }
    }

}

