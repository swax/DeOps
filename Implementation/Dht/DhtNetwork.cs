using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using RiseOp.Services;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Comm;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Transport;
using RiseOp.Services.Location;
using RiseOp.Interface.Tools;


namespace RiseOp.Implementation.Dht
{
    internal delegate void StatusChange();


    internal class DhtNetwork
    {
        const int MAX_CACHE = 200;

        // super-class
        internal OpCore Core; 

        // sub-class
        internal G2Protocol Protocol;
        internal TcpHandler TcpControl;
        internal UdpHandler UdpControl;
        internal RudpHandler RudpControl;
        internal DhtRouting Routing;
        internal DhtStore   Store;
        internal DhtSearchControl Searches;

        internal LinkedList<IPCacheEntry> IPCache = new LinkedList<IPCacheEntry>();
        internal Dictionary<int, LinkedListNode<IPCacheEntry>> IPTable = new Dictionary<int, LinkedListNode<IPCacheEntry>>();

        internal bool   IsGlobal;
        internal ulong  LocalUserID;
        internal ushort ClientID;
        internal ulong  OpID;

        internal bool Established;
        internal bool Responsive;
        internal int  FireStatusChange; // timeout until established is called
        internal StatusChange StatusChange; // operation only
        
        RetryIntervals    Retry;
        internal DateTime NextWebcacheTry;
        RetryIntervals    GlobalSearchInterval;
        DateTime          NextGlobalSearch;

        internal RijndaelManaged OriginalCrypt;
        internal RijndaelManaged AugmentedCrypt;

        // log
        internal Queue<PacketCopy> IncomingPackets = new Queue<PacketCopy>();
        internal Queue<PacketLogEntry> LoggedPackets = new Queue<PacketLogEntry>();
        internal Dictionary<string, Queue<string>> LogTable = new Dictionary<string, Queue<string>>();

        // gui
        internal PacketsForm GuiPackets;
        internal CrawlerForm GuiCrawler;
        internal GraphForm   GuiGraph;


        internal DhtNetwork(OpCore core, bool isGlobal)
        {
            Core = core;
            IsGlobal = isGlobal;

            LocalUserID = IsGlobal ? Core.User.Settings.GlobalID : Utilities.KeytoID(Core.User.Settings.KeyPair.ExportParameters(false));
            ClientID = (ushort) new Random().Next(1, ushort.MaxValue);
            OpID = IsGlobal ? 0 : Utilities.KeytoID(Core.User.Settings.OpKey.Key);

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

            Protocol = new G2Protocol();
            TcpControl  = new TcpHandler(this);
            UdpControl  = new UdpHandler(this);
            RudpControl = new RudpHandler(this);
            Routing     = new DhtRouting(this);
            Store       = new DhtStore(this);
            Searches    = new DhtSearchControl(this);

            Retry = new RetryIntervals(Core);
            GlobalSearchInterval = new RetryIntervals(Core);
        }

        internal void SecondTimer()
        {
            // timers
            TcpControl.SecondTimer();
            RudpControl.SecondTimer();   
            Routing.SecondTimer();
            Searches.SecondTimer();

            CheckConnectionStatus();

            // if unresponsive
            if (!Responsive)
            {
                Retry.Timer();

                if (IsGlobal)
                    GlobalBootstrap();
                else
                    OpBootstrap();
            }

            // ip cache
            lock (IPCache)
                while (IPCache.Count > MAX_CACHE)
                {
                    IPCacheEntry entry = IPCache.Last.Value;
                    IPTable.Remove(entry.GetHashCode());
                    IPCache.RemoveLast();
                }

            // established in dht
            if (FireStatusChange > 0)
            {
                FireStatusChange--;

                if (FireStatusChange == 0)
                {
                    Established = true;

                    if(StatusChange != null)
                        StatusChange.Invoke();
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

                targetLog.Enqueue(Core.TimeNow.ToString("HH:mm:ss:ff - ") + message);

                int logsize = (Core.Sim == null) ? 500 : 100;

                while (targetLog.Count > logsize)
                    targetLog.Dequeue();
            }
        }

        int ThinkOnline = 0;
        DateTime NextOnlineCheck;

        void GlobalBootstrap()
        {
            // only called if network not responsive

            // try website BootstrapTimeout at 1 2 5 10 15 30 / 30 / 30 intervals 
            // reset increment when disconnected
            // dont try web cache for first 10 seconds


            // ensure that if re-connected at anytime then re-connect to network is fast
            if (Core.Sim == null)
            {
                // ThinkOnline state changed to connected then retry timers reset
                if (Core.TimeNow > NextOnlineCheck)
                    if (ThinkOnline > 0)
                    {
                        // check online status by pinging google/yahoo/microsoft every 60 secs
                        PingCheck();
                        NextOnlineCheck = Core.TimeNow.AddSeconds(60);
                    }

                    // if think offline
                    else if (ThinkOnline == 0)
                    {
                        // try google/yahoo/microsoft every 5 secs
                        PingCheck();
                        NextOnlineCheck = Core.TimeNow.AddSeconds(5);
                    }
            }


            bool AllowWeb = (IPCache.Count == 0 || Core.TimeNow > Retry.Start.AddSeconds(10));


            // give a few seconds at startup to try to connect to Dht networks from the cache
            if (Core.TimeNow > NextWebcacheTry && AllowWeb)
            {
                NextWebcacheTry = Retry.NextTry;

                // if not connected to global use web cache
                if (Core.Sim == null)
                {
                    Thread dlThread = new Thread(new ThreadStart(DownloadCache));
                    dlThread.Start();
                }
                else
                    Core.Sim.Internet.DownloadCache(this);
            }

            TryFromCache();
        }

        void OpBootstrap()
        {
            // find operation nodes through global net at expanding intervals
            // called from operation network's bootstrap
            if (Core.GlobalNet != null && Core.GlobalNet.Responsive)
            {
                GlobalSearchInterval.Timer();

                if (Core.TimeNow > NextGlobalSearch)
                {
                    NextGlobalSearch = GlobalSearchInterval.NextTry;
                    Core.Locations.StartSearch(OpID, 0, true);
                }
            }

            TryFromCache();
        }

        private void TryFromCache()
        {
            // send pings to nodes in cache, responses will startup the routing system
            // 10 udp pings per second, 10 min retry
            int pings = 0;

            lock (IPCache)
                foreach (IPCacheEntry entry in IPCache)
                {
                    if (Core.TimeNow < entry.NextTry)
                        continue;

                    Send_Ping(entry.Address);

                    entry.NextTry = Retry.NextTry;

                    pings++;
                    if (pings >= 10)
                        break;
                }


            // if blocked and go through cache and mark as tcp tried
            // 1 outbound tcp per second, 10 min retry
            if (Core.Firewall == FirewallType.Blocked)
                lock (IPCache)
                    foreach (IPCacheEntry entry in IPCache)
                    {
                        if (Core.TimeNow < entry.NextTryTcp)
                            continue;

                        TcpControl.MakeOutbound(entry.Address, entry.TcpPort, "ip cache");

                        entry.NextTryTcp = Retry.NextTry;
                        break;
                    }
        }

        string[] TestSites = new string[] { "www.google.com", 
                                            "www.yahoo.com", 
                                            "www.youtube.com", 
                                            "www.myspace.com"};

        private void PingCheck()
        {
            System.Net.NetworkInformation.Ping pingSender = new System.Net.NetworkInformation.Ping();

            // Create an event handler for ping complete
            pingSender.PingCompleted += new System.Net.NetworkInformation.PingCompletedEventHandler(Ping_Complete);

            // Send the ping asynchronously
            string site = TestSites[Core.RndGen.Next(TestSites.Length)];
            pingSender.SendAsync(site, 5000, null);
        }

        private void Ping_Complete(object sender, System.Net.NetworkInformation.PingCompletedEventArgs e)
        {
            if (e.Reply != null && e.Reply.Status == System.Net.NetworkInformation.IPStatus.Success)
            {
                // if previously thought we were offline
                if (ThinkOnline == 0)
                {
                    Retry.Reset();
                    NextWebcacheTry = new DateTime(0);

                    foreach (IPCacheEntry entry in IPCache)
                    {
                        entry.NextTry = new DateTime(0);
                        entry.NextTryTcp = new DateTime(0);
                    }
                }

                if(ThinkOnline < 3)
                    ThinkOnline++;
            }

            // not success, try another random site
            else if (ThinkOnline > 0)
            {
                ThinkOnline--;

                PingCheck();
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

        internal void CheckConnectionStatus()
        {
            // dht responsiveness is only reliable if we can accept incoming connections, other wise we might be 
            // behind a NAT and in that case won't be able to receive traffic from anyone who has not sent us stuff
            bool connected = (Core.Firewall == FirewallType.Open && Routing.DhtResponsive) || 
                            TcpControl.ProxyServers.Count > 0 || TcpControl.ProxyClients.Count > 0;

            //crit check here for global proxy flag (if globally proxied and in ping contact with remote?

            if (connected == Responsive)
                return;

            // else set new value
            Responsive = connected;

            if (Responsive)
            {
                // done to fill up routing table down to self

                Searches.Start(Routing.LocalRoutingID + 1, "Self", Core.DhtServiceID, 0, null, new EndSearchHandler(EndSelfSearch));
                Routing.NextSelfSearch = Core.TimeNow.AddHours(1);

                // at end of self search, status change count down triggered
            }

            // network dead
            else
            {
                Established = false;

                Retry.Reset();
                GlobalSearchInterval.Reset();
                NextWebcacheTry = Core.TimeNow.AddMinutes(1); // only really reset when global network resets

                if (StatusChange != null)
                    StatusChange.Invoke();
            }
        }

        internal void EndSelfSearch(DhtSearch search)
        {
            // if not already established (an hourly self re-search)
            if (!Established)
            {
                // a little buffer time for local nodes to send patch files
                // so we dont start sending our own huge patch files
                FireStatusChange = 10;
            }
        }

        internal void FirewallChangedtoOpen()
        {
            //close proxy connects
            lock (TcpControl.SocketList)
                foreach (TcpConnect connection in TcpControl.SocketList)
                    if (connection.State == TcpState.Connected) // close everything, even unset proxies
                        connection.CleanClose("Firewall changed to Open", true);
        }

        internal void FirewallChangedtoNAT()
        {
            //update proxy connects
            lock (TcpControl.SocketList)
                foreach (TcpConnect connection in TcpControl.SocketList)
                    if (connection.Proxy == ProxyType.Server)
                    {
                        ProxyReq request = new ProxyReq();
                        request.SenderID = LocalUserID;
                        request.Type = ProxyType.ClientNAT;
                        connection.SendPacket(request);
                    }
        }

        internal DhtSource GetLocalSource()
        {
            DhtSource source = new DhtSource();

            source.userID    = LocalUserID;
            source.ClientID = ClientID;
            source.TcpPort  = TcpControl.ListenPort;
            source.UdpPort  = UdpControl.ListenPort;
            source.Firewall = Core.Firewall;

            return source;
        }

        internal DhtContact GetLocalContact()
        {
            return new DhtContact(LocalUserID, ClientID, Core.LocalIP, TcpControl.ListenPort, UdpControl.ListenPort, Core.TimeNow);
        }



        internal void ReceivePacket(G2ReceivedPacket packet)
        {
            // Network packet
            if (packet.Root.Name == RootPacket.Network)
            {
                NetworkPacket netPacket = NetworkPacket.Decode(packet.Root);

                G2ReceivedPacket embedded = new G2ReceivedPacket();
                embedded.Tcp    = packet.Tcp;
                embedded.Source = packet.Source;
                embedded.Source.userID = netPacket.SourceID;
                embedded.Source.ClientID = netPacket.ClientID;
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

                // to - received from proxied node, and not for us
                if (netPacket.ToAddress != null &&
                    !(netPacket.ToAddress.userID == LocalUserID && netPacket.ToAddress.ClientID == ClientID))
                {
                    if (packet.Tcp == null)
                        throw new Exception("To tag set on packet received udp");
                    if (packet.Tcp.Proxy == ProxyType.Server || packet.Tcp.Proxy == ProxyType.Unset)
                        throw new Exception("To tag set on packet received from server");

                    DhtAddress address = netPacket.ToAddress;
                    netPacket.ToAddress = null;

                    TcpConnect direct = TcpControl.GetProxy(address);

                    if (direct != null)
                        direct.SendPacket(netPacket);
                    else
                        UdpControl.SendTo(address, netPacket);
                    
                    return;
                }

                // process
                if(G2Protocol.ReadPacket(embedded.Root))
                    ReceiveNetworkPacket(embedded);
            }

            // Communication Packet
            if (packet.Root.Name == RootPacket.Comm)
            {
                RudpPacket commPacket = RudpPacket.Decode(packet);

                packet.Source.userID = commPacket.SenderID;
                packet.Source.ClientID = commPacket.SenderClient;

                // For local host
                if (commPacket.TargetID == LocalUserID && commPacket.TargetClient == ClientID)
                {
                    if (packet.Tcp != null && commPacket.FromEndPoint != null)
                        packet.Source = commPacket.FromEndPoint;

                    ReceiveCommPacket(packet, commPacket);
                    return;
                }  

                // Also Forward to appropriate node
                TcpConnect socket = TcpControl.GetProxy(commPacket.TargetID, commPacket.TargetClient);

                if (socket != null)
                {
                    // strip TO flag, add from address
                    commPacket.ToEndPoint = null;
                    commPacket.FromEndPoint = packet.Source;

                    commPacket.SenderID = LocalUserID;
                    commPacket.SenderClient = ClientID;
                    socket.SendPacket(commPacket);
                    return;
                }

                // forward udp if TO flag marked
                if (packet.Tcp != null && commPacket.ToEndPoint != null)
                {
                    DhtAddress address = commPacket.ToEndPoint;

                    commPacket.ToEndPoint = null; // strip TO flag

                    commPacket.SenderID = LocalUserID;
                    commPacket.SenderClient = ClientID;
                    UdpControl.SendTo(address, commPacket);
                }
            }
        }

        internal void ReceiveCommPacket(G2ReceivedPacket raw, RudpPacket packet)
        {
            try
            {
                // if a socket already set up
                lock (RudpControl.SocketMap)
                    if (RudpControl.SocketMap.ContainsKey(packet.PeerID))
                    {
                        RudpControl.SocketMap[packet.PeerID].RudpReceive(raw, packet, IsGlobal);
                        return;
                    }

                // if starting new session
                if (packet.PacketType != RudpPacketType.Syn)
                    return;

                RudpSyn syn = new RudpSyn(packet.Payload);

                // prevent connection from self
                if (syn.SenderID == LocalUserID && syn.ClientID == ClientID)
                    return;


                // find connecting session with same or unknown client id
                if (RudpControl.SessionMap.ContainsKey(syn.SenderID))
                    foreach (RudpSession session in RudpControl.SessionMap[syn.SenderID])
                    {
                        if (session.ClientID == syn.ClientID)
                        {
                            // if session id zero or matches forward
                            if ((session.Comm.State == RudpState.Connecting && session.Comm.RemotePeerID == 0) ||
                                (session.Comm.State != RudpState.Closed && session.Comm.RemotePeerID == syn.ConnID)) // duplicate syn
                            {
                                session.Comm.RudpReceive(raw, packet, IsGlobal);
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
                RudpSession newSession = new RudpSession(RudpControl, syn.SenderID, syn.ClientID, true);

                if (!RudpControl.SessionMap.ContainsKey(syn.SenderID))
                    RudpControl.SessionMap[syn.SenderID] = new List<RudpSession>();

                RudpControl.SessionMap[syn.SenderID].Add(newSession);
               
                // send ack before sending our own syn (connect)
                // ack tells remote which address is good so that our syn's ack comes back quickly
                newSession.Comm.RudpReceive(raw, packet, IsGlobal);
                
                newSession.Connect();
                

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
            Ping ping = Ping.Decode(packet);
            
            // set local IP
            if(ping.RemoteIP != null)
                Core.LocalIP = ping.RemoteIP;

            // setup pong reply
            Pong pong = new Pong();

            if(ping.Source != null)
                pong.Source = GetLocalSource();

            if (ping.RemoteIP != null)
                pong.RemoteIP = packet.Source.IP;

            // if received udp
            if (packet.Tcp == null)
            {
                // received udp traffic, we must be behind a NAT at least
                Core.SetFirewallType(FirewallType.NAT);

                if (ping.Source != null)
                {
                    if (ping.Source.userID == LocalUserID && ping.Source.ClientID == ClientID) // loop back
                        return;

                    // if firewall flag not set add to routing
                    if (ping.Source.Firewall == FirewallType.Open)
                        Routing.Add(new DhtContact(ping.Source, packet.Source.IP, Core.TimeNow));
                }

                UdpControl.SendTo(packet.Source, pong);
            }

            // received tcp
            else
            {
                if (ping.Source == null)
                {
                    packet.Tcp.SendPacket(pong);
                    return;
                }

                if (ping.Source.userID == LocalUserID && ping.Source.ClientID == ClientID) // loopback
                {
                    packet.Tcp.CleanClose("Loopback connection");
                    return;
                }

                if (ping.Source.Firewall == FirewallType.Open)
                    Routing.Add(new DhtContact(ping.Source, packet.Source.IP, Core.TimeNow));

                // received incoming tcp means we are not firewalled
                if (!packet.Tcp.Outbound)
                    // done here to prevent setting open for loopback tcp connection
                    Core.SetFirewallType(FirewallType.Open); 
                    
                // check if already connected
                if (packet.Tcp.Proxy == ProxyType.Unset && TcpControl.GetProxy(ping.Source) != null)
                {
                    packet.Tcp.CleanClose("Dupelicate Connection");
                    return;
                }

                packet.Tcp.userID    = ping.Source.userID;
                packet.Tcp.ClientID = ping.Source.ClientID;
                packet.Tcp.TcpPort  = ping.Source.TcpPort;
                packet.Tcp.UdpPort  = ping.Source.UdpPort;

                // if inbound connection, to our open host, and haven't checked fw yet
                if (!packet.Tcp.Outbound && 
                    ping.Source.Firewall != FirewallType.Open && 
                    !packet.Tcp.CheckedFirewall)
                {
                    TcpControl.MakeOutbound(packet.Source, ping.Source.TcpPort, "check firewall");
                    packet.Tcp.CheckedFirewall = true;
                }

                pong.Direct = true;
                packet.Tcp.SendPacket(pong);

                // dont send close if proxies maxxed yet, because their id might be closer than current proxies
            }
        }

        void Receive_Pong(G2ReceivedPacket packet)
        {
            Pong pong = Pong.Decode(packet);

            if (pong.RemoteIP != null)
                Core.LocalIP = pong.RemoteIP;

            // if received udp
            if (packet.Tcp == null)
            {
                Core.SetFirewallType(FirewallType.NAT);

                // send bootstrap request for nodes if network not responsive
                // do tcp connect because if 2 nodes on network then one needs to find out their open
                if (!Responsive)
                {
                    Searches.SendUdpRequest(packet.Source, LocalUserID, 0, Core.DhtServiceID, 0, null);
                    TcpControl.MakeOutbound(packet.Source, pong.Source.TcpPort, "pong bootstrap");
                }

                // add to routing
                // on startup, especially in sim everyone starts blocked so pong source firewall is not set right, but still needs to go into routing
                if (pong.Source.Firewall == FirewallType.Open)
                    Routing.Add(new DhtContact(pong.Source, packet.Source.IP, Core.TimeNow));

                // forward to proxied nodes, so that their routing tables are up to date, so they can publish easily
                if (Core.Firewall == FirewallType.Open)
                {
                    pong.FromAddress = packet.Source;
                    pong.RemoteIP = null;
                    pong.Direct = false;

                    foreach (TcpConnect connection in TcpControl.ProxyClients)
                        connection.SendPacket(pong);
                }
            }

            // if received tcp
            else if (packet.Tcp != null)
            {
                // if regular interval pong 
                if (pong.Source == null)
                {
                    // keep routing entry fresh so connect state remains
                    if (packet.Tcp.Proxy == ProxyType.Server)
                        Routing.Add(new DhtContact(packet.Tcp, packet.Tcp.RemoteIP, Core.TimeNow));
                }

                // else connect pong with source info
                else
                {
                    if (pong.Source.Firewall == FirewallType.Open)
                        Routing.Add(new DhtContact(pong.Source, packet.Source.IP, Core.TimeNow));

                    // pong's direct flag ensures that tcp connection info (especially client ID) is not set with 
                    //   information from a pong routed through the remote host, but from the host we're directly connected to
                    if (pong.Direct)
                    {
                        packet.Tcp.userID = pong.Source.userID;
                        packet.Tcp.ClientID = pong.Source.ClientID;
                        packet.Tcp.TcpPort = pong.Source.TcpPort;
                        packet.Tcp.UdpPort = pong.Source.UdpPort;

                        // if firewalled
                        if (packet.Tcp.Outbound && packet.Tcp.Proxy == ProxyType.Unset)
                        {
                            if (Core.Firewall != FirewallType.Open && TcpControl.AcceptProxy(ProxyType.Server, pong.Source.userID))
                            {
                                // send proxy request
                                ProxyReq request = new ProxyReq();
                                request.SenderID = LocalUserID;
                                request.Type = (Core.Firewall == FirewallType.Blocked) ? ProxyType.ClientBlocked : ProxyType.ClientNAT;
                                packet.Tcp.SendPacket(request);
                            }

                            // else ping/pong done, end connect
                            else
                                packet.Tcp.CleanClose("Not in need of a proxy");
                        }
                    }
                }
            }
        }

        internal void Receive_ProxyRequest(G2ReceivedPacket packet)
        {
            ProxyReq request = ProxyReq.Decode(packet);

            ProxyAck ack = new ProxyAck();
            ack.Source = GetLocalSource();

            // check if there is space for type required
            if (Core.Firewall == FirewallType.Open  && TcpControl.AcceptProxy(request.Type, ack.Source.userID))
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

                TcpControl.AddProxy(packet.Tcp);

                // check if a proxy needs to be disconnected now because overflow
                TcpControl.CheckProxies();
            }
        }

        internal void Receive_ProxyAck(G2ReceivedPacket packet)
        {
            ProxyAck ack = ProxyAck.Decode(packet);

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
                if(!TcpControl.ProxyMap.ContainsKey(ack.Source.userID))
                    TcpControl.MakeOutbound(packet.Source, ack.Source.TcpPort, "proxy ack recv");
            }

            // received ack tcp
            else
            {
                packet.Tcp.Proxy = ProxyType.Server;

                TcpControl.AddProxy(packet.Tcp);

                TcpControl.CheckProxies();

                // location and rudp connections updated after 20 seconds
            }
        }

        internal void Send_CrawlRequest(DhtAddress address)
        {
            CrawlReq req = new CrawlReq();

            req.Source = GetLocalSource();
            req.TargetID = address.userID;

            UdpControl.SendTo(address, req);
        }

        internal void Receive_CrawlRequest(G2ReceivedPacket packet)
        {
            CrawlReq req = CrawlReq.Decode(packet);

            // Not meant for local host, forward along
            if (req.TargetID != LocalUserID)
            {
                // Forward to appropriate node
                if (TcpControl.ProxyMap.ContainsKey(req.TargetID))
                {
                    /*TcpConnect connection = TcpControl.ConnectionMap[req.TargetID];

                    // add so receiving host knows where to send response too
                    req.FromAddress = packet.Source;

                    connection.SendPacket(req);*/
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
            lock (TcpControl.SocketList)
                foreach (TcpConnect connection in TcpControl.SocketList)
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
            CrawlAck ack = CrawlAck.Decode(packet);

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
            /*string message = logEntry.Protocol.ToString();
            message += (logEntry.Direction == DirectionType.In) ? " in from " : " out to ";
            message += logEntry.Address.ToString();

            G2ReceivedPacket packet = new G2ReceivedPacket();
            packet.Root = new G2Header(logEntry.Data);

            if(G2Protocol.ReadPacket(packet.Root))
                message += ", type " + packet.Root.Name;*/
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
            Address.userID = contact.userID;
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

    internal class PacketCopy
    {
        internal G2ReceivedPacket Packet;
        internal byte[] Data;
        internal bool Global;

        internal PacketCopy(G2ReceivedPacket packet, byte[] data, bool global)
        {
            Packet = packet;
            Data = data;
            Global = global;
        }
    }

    class RetryIntervals
    {
        OpCore Core;

        internal DateTime Start;
        int Index = 0;
        DateTime LastIncrement;

        int[] Intervals = new int[] { 0, 1, 2, 5, 10, 15, 30 };


        internal RetryIntervals(OpCore core)
        {
            Core = core;

            Reset();
        }

        internal void Reset()
        {
            Start = Core.TimeNow;
            Index = 0;
            LastIncrement = new DateTime(0);
        }

        internal DateTime NextTry
        {
            get
            {
                return Core.TimeNow.AddMinutes(Intervals[Index]);
            }
        }
    
        internal void Timer()
        {
            if (Core.TimeNow > LastIncrement.AddMinutes(Intervals[Index]))
            {
                LastIncrement = Core.TimeNow;
                
                if(Index < Intervals.Length - 1)
                    Index++;
            }
        }
    }
}

