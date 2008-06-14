using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
using RiseOp.Implementation.Protocol.Special;
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

        internal LinkedList<DhtContact> IPCache = new LinkedList<DhtContact>();
        internal Dictionary<int, LinkedListNode<DhtContact>> IPTable = new Dictionary<int, LinkedListNode<DhtContact>>();

        internal bool IsGlobal;
        internal GlobalSettings GlobalConfig;

        internal DhtClient Local;
        internal ulong  OpID;

        internal bool Established;
        internal bool Responsive;
        internal int  FireStatusChange; // timeout until established is called
        internal StatusChange StatusChange; // operation only
        
        RetryIntervals    Retry;
        internal DateTime NextWebcacheTry;
        RetryIntervals    GlobalSearchInterval;
        DateTime          NextGlobalSearch;
        DateTime          NextSaveCache;

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


        internal DhtNetwork(OpCore core, bool global)
        {
            Core = core;
            IsGlobal = global;

            if (IsGlobal)
                GlobalConfig = GlobalSettings.Load(this);

            Local = new DhtClient();
            Local.UserID = IsGlobal ? GlobalConfig.UserID : Utilities.KeytoID(Core.User.Settings.KeyPair.ExportParameters(false));
            Local.ClientID = (ushort) Core.RndGen.Next(1, ushort.MaxValue);

            OpID = IsGlobal ? 0 : Utilities.KeytoID(Core.User.Settings.OpKey.Key);

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
            NextSaveCache = Core.TimeNow.AddMinutes(1);
        }

        internal void SecondTimer()
        {
            // timers
            TcpControl.SecondTimer();
            RudpControl.SecondTimer();   
            Routing.SecondTimer();
            Searches.SecondTimer();

            if(!IsGlobal)
                CheckGlobalProxyMode();


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
                    DhtContact entry = IPCache.Last.Value;
                    IPTable.Remove(entry.GetHashCode());
                    IPCache.RemoveLast();
                }

            // save cache
            if (Core.TimeNow > NextSaveCache)
            {
                if(IsGlobal)
                    GlobalConfig.Save(Core);
                else
                    Core.User.Save();

                NextSaveCache = Core.TimeNow.AddMinutes(5);
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

        internal void AddCacheEntry(DhtContact entry)
        {
             lock (IPCache)
            {
                if (IPTable.ContainsKey(entry.GetHashCode()))
                    IPCache.Remove(IPTable[entry.GetHashCode()]);

                // sort nodes based on last seen
                LinkedListNode<DhtContact> node = null;

                for (node = IPCache.First; node != null; node = node.Next)
                    if (entry.LastSeen > node.Value.LastSeen)
                        break;

                IPTable[entry.GetHashCode()] = (node != null) ? IPCache.AddBefore(node, entry) : IPCache.AddLast(entry);
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
            OpCore global = Core.Context.Global;

            // find operation nodes through global net at expanding intervals
            // called from operation network's bootstrap
            if (global != null && global.Network.Responsive)
            {
                GlobalSearchInterval.Timer();

                if (Core.TimeNow > NextGlobalSearch)
                {
                    NextGlobalSearch = GlobalSearchInterval.NextTry;

                    global.RunInCoreAsync(delegate()
                    {
                        GlobalService service = (GlobalService) global.ServiceMap[2];
                        service.StartSearch(OpID, 0);
                    });
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
                foreach (DhtContact entry in IPCache)
                {
                    if (Core.TimeNow < entry.NextTry)
                        continue;

                    Send_Ping(entry);

                    entry.NextTry = Retry.NextTry;

                    pings++;
                    if (pings >= 10)
                        break;
                }


            // if blocked and go through cache and mark as tcp tried
            // 1 outbound tcp per second, 10 min retry
            if (Core.Context.Firewall == FirewallType.Blocked)
                lock (IPCache)
                    foreach (DhtContact entry in IPCache)
                    {
                        if (Core.TimeNow < entry.NextTryProxy)
                            continue;

                        TcpControl.MakeOutbound(entry, entry.TcpPort, "ip cache");

                        entry.NextTryProxy = Retry.NextTry;
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

                    foreach (DhtContact entry in IPCache)
                    {
                        entry.NextTry = new DateTime(0);
                        entry.NextTryProxy = new DateTime(0);
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
            bool connected = (Routing.DhtEnabled && Routing.DhtResponsive) || 
                            TcpControl.ProxyServers.Count > 0 || TcpControl.ProxyClients.Count > 0;


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
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { FirewallChangedtoOpen(); });
                return;
            }

            //close proxy connects
            lock (TcpControl.SocketList)
                foreach (TcpConnect connection in TcpControl.SocketList)
                    if (connection.State == TcpState.Connected) // close everything, even unset proxies
                        connection.CleanClose("Firewall changed to Open", true);
        }

        internal void FirewallChangedtoNAT()
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { FirewallChangedtoNAT(); });
                return;
            }

            //update proxy connects
            lock (TcpControl.SocketList)
                foreach (TcpConnect connection in TcpControl.SocketList)
                    if (connection.Proxy == ProxyType.Server)
                    {
                        ProxyReq request = new ProxyReq();
                        request.SenderID = Local.UserID;
                        request.Type = ProxyType.ClientNAT;
                        connection.SendPacket(request);
                    }
        }

        internal DhtSource GetLocalSource()
        {
            DhtSource source = new DhtSource();

            source.UserID    = Local.UserID;
            source.ClientID = Local.ClientID;
            source.TcpPort  = TcpControl.ListenPort;
            source.UdpPort  = UdpControl.ListenPort;
            source.Firewall = Core.Context.Firewall;

            return source;
        }

        internal DhtContact GetLocalContact()
        {
            return new DhtContact(Local.UserID, Local.ClientID, Core.Context.LocalIP, TcpControl.ListenPort, UdpControl.ListenPort);
        }

        internal void IncomingPacket(G2ReceivedPacket packet)
        {
            if (Core.Sim == null || Core.Sim.Internet.TestCoreThread)
            {
                lock (IncomingPackets)
                    if (IncomingPackets.Count < 100)
                        IncomingPackets.Enqueue(new PacketCopy(packet));

                Core.ProcessEvent.Set();
            }
            else
            {
                try
                {
                    ReceivePacket(packet);
                }
                catch (Exception ex)
                {
                    UpdateLog("Exception", "DhtNetwork::IncomingPacket: " + ex.Message);
                }
            }
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
                embedded.Source.UserID = netPacket.SourceID;
                embedded.Source.ClientID = netPacket.ClientID;
                embedded.Root   = new G2Header(netPacket.InternalData);

                // from - received from proxy server
                if (netPacket.FromAddress != null)
                {
                    if (packet.ReceivedUdp)
                        throw new Exception("From tag set on packet received udp");
                    if (packet.Tcp.Proxy != ProxyType.Server)
                        throw new Exception("From tag (" + netPacket.FromAddress.ToString() + ") set on packet not received from server (" + packet.Tcp.ToString() + ")");

                    embedded.Source = new DhtContact(netPacket.FromAddress);
                }

                // to - received from proxied node, and not for us
                if (netPacket.ToAddress != null &&
                    !(netPacket.ToAddress.UserID == Local.UserID && netPacket.ToAddress.ClientID == Local.ClientID))
                {
                    if (packet.ReceivedUdp)
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

            // Tunnel Packet
            else if (packet.Root.Name == RootPacket.Tunnel )
            {
                // can only tunnel over global network
                if (!IsGlobal)
                    return;

                PacketLogEntry logEntry = new PacketLogEntry(TransportProtocol.Tunnel, DirectionType.In, packet.Source, packet.Root.Data);
                LogPacket(logEntry);
                
                TunnelPacket tunnel = TunnelPacket.Decode(packet.Root);

                // handle locally
                if ( tunnel.Target.Equals(Local))
                {
                    ReceiveTunnelPacket(packet, tunnel);
                }
                else if(tunnel.TargetServer != null)
                {
                    TcpConnect direct = TcpControl.GetProxy(tunnel.Target);
  
                    // if directly connected add from and forwared
                    if (direct != null)
                        direct.SendPacket(tunnel);

                    // only forward udp if received over tcp from a proxied host
                    else if (tunnel.TargetServer != null && packet.ReceivedTcp && packet.Tcp.Proxy != ProxyType.Server)
                        UdpControl.SendTo(tunnel.TargetServer, tunnel);
                }
            }

            // Communication Packet
            else if (packet.Root.Name == RootPacket.Comm)
            {
                RudpPacket commPacket = RudpPacket.Decode(packet);

                packet.Source.UserID = commPacket.SenderID;
                packet.Source.ClientID = commPacket.SenderClient;

                // For local host
                if (commPacket.TargetID == Local.UserID && commPacket.TargetClient == Local.ClientID)
                {
                    if (packet.ReceivedTcp && commPacket.FromEndPoint != null)
                        packet.Source = new DhtContact(commPacket.FromEndPoint);

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

                    commPacket.SenderID = Local.UserID;
                    commPacket.SenderClient = Local.ClientID;
                    socket.SendPacket(commPacket);
                    return;
                }

                // forward udp if TO flag marked
                if (packet.ReceivedTcp && commPacket.ToEndPoint != null)
                {
                    DhtAddress address = commPacket.ToEndPoint;

                    commPacket.ToEndPoint = null; // strip TO flag

                    commPacket.SenderID = Local.UserID;
                    commPacket.SenderClient = Local.ClientID;
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
                if (syn.SenderID == Local.UserID && syn.ClientID == Local.ClientID)
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
            else if (packet.Root.Name == NetworkPacket.Bye && packet.ReceivedTcp)
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

        // nodes in global proxy mode are psuedo-open, instead of udp they send tunneled packets
        // tunnel packets include routing information to the global target as well as
        // the encrytped operation packet embedded in the payload
        internal void SendTunnelPacket(DhtAddress contact, G2Packet embed)
        {
            Debug.Assert(contact.TunnelClient != null && contact.TunnelServer != null);
            Debug.Assert(Core.Context.Global != null);
            Debug.Assert(!IsGlobal);
            Debug.Assert(Core.User.Settings.OpAccess != AccessType.Secret);

            if (IsGlobal ||
                Core.Context.Global == null || 
                Core.User.Settings.OpAccess == AccessType.Secret)
                return;

            OpCore global = Core.Context.Global;

            // tunnel packet through global network
            byte[] encoded = embed.Encode(Protocol);

            PacketLogEntry logEntry = new PacketLogEntry(TransportProtocol.Tunnel, DirectionType.Out, contact, encoded);
            LogPacket(logEntry);

            TunnelPacket packet = new TunnelPacket();

            // encrypt, turn off encryption during simulation
            if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
            {
                lock (AugmentedCrypt)
                {
                    BitConverter.GetBytes(contact.UserID).CopyTo(AugmentedCrypt.Key, 0);
                    packet.Payload = Utilities.EncryptBytes(encoded, AugmentedCrypt.Key);
                }
            }
            else
                packet.Payload = encoded;

            packet.Source = new TunnelAddress(global.Network.Local, Core.TunnelID);
            packet.Target = contact.TunnelClient;

 
            // if not open send proxied through local global proxy
            // NAT as well because receiver would need to send all responses through same local global proxy
            // for NATd host to get replies
            if (Core.Context.Firewall != FirewallType.Open)
            {
                packet.TargetServer = contact.TunnelServer;

                global.RunInCoreAsync(delegate()
                {
                    TcpConnect server = global.Network.TcpControl.GetProxy(packet.TargetServer) ?? // direct path
                                        global.Network.TcpControl.GetProxyServer(contact.IP) ?? // reRoute through same server
                                        global.Network.TcpControl.GetRandomProxy(); // random proxy

                    if (server != null)
                    {
                        packet.SourceServer = new DhtAddress(server.RemoteIP, server);
                        server.SendPacket(packet);
                    }
                });
            }
            // else we are open, send op ip address in the souce server
            else
            {
                packet.SourceServer = new DhtAddress(Core.Context.LocalIP, global.Network.GetLocalSource());

                global.RunInCoreAsync(delegate()
                {
                    global.Network.UdpControl.SendTo(contact.TunnelServer, packet);
                });
            }
        }

        void ReceiveTunnelPacket(G2ReceivedPacket raw, TunnelPacket tunnel)
        {
            Debug.Assert(IsGlobal);

            if (!IsGlobal)
                return;

            // decrypt internal packet
            if (Core.Sim == null || Core.Sim.Internet.TestEncryption) // turn off encryption during simulation
            {
                if (tunnel.Payload.Length < AugmentedCrypt.IV.Length)
                    throw new Exception("Not enough data received for IV");

                lock (AugmentedCrypt)
                {
                    BitConverter.GetBytes(Local.UserID).CopyTo(AugmentedCrypt.Key, 0);
                    tunnel.Payload = Utilities.DecryptBytes(tunnel.Payload, tunnel.Payload.Length, AugmentedCrypt.Key);
                }
            }

            G2ReceivedPacket opPacket = new G2ReceivedPacket();
            opPacket.Root = new G2Header(tunnel.Payload);

            // set source information
            if (G2Protocol.ReadPacket(opPacket.Root))
            {
                opPacket.Source = new DhtAddress();

                // used to add direct op contact if source firewall is open
                // or re-routing through same global proxy
                opPacket.Source.IP = raw.Source.IP; 

                // op user/client set by net/comm processing

                opPacket.Source.TunnelClient = tunnel.Source;
                opPacket.Source.TunnelServer = tunnel.SourceServer;

                Core.Context.Cores.LockReading(delegate()
                {
                    foreach (OpCore core in Core.Context.Cores)
                        if (core.TunnelID == tunnel.Target.TunnelID)
                            core.RunInCoreAsync(delegate()
                            {
                                PacketLogEntry logEntry = new PacketLogEntry(TransportProtocol.Tunnel, DirectionType.In, opPacket.Source, opPacket.Root.Data);
                                core.Network.LogPacket(logEntry);

                                core.Network.IncomingPacket(opPacket);
                            });
                });
            }
        }

        internal void Send_Ping(DhtContact contact)
        {
            Ping ping = new Ping();
            ping.Source = GetLocalSource();
            ping.RemoteIP = contact.IP;
            ping.Ident = contact.Ident = (ushort)Core.RndGen.Next(ushort.MaxValue);

            // always send ping udp, tcp pings are sent manually
            SendPacket(contact, ping);       
        }

        internal void SendPacket(DhtAddress contact, G2Packet packet)
        {
            if (contact.TunnelServer != null)
                SendTunnelPacket(contact, packet);
            else
                UdpControl.SendTo(contact, packet);
        }

        void Receive_Ping(G2ReceivedPacket packet)
        {
            Ping ping = Ping.Decode(packet);
            
            // set local IP
            if (ping.RemoteIP != null && !packet.Tunneled)
                Core.Context.LocalIP = ping.RemoteIP;


            // check loop back
            if (ping.Source != null && Local.Equals(ping.Source))
            {
                if (packet.ReceivedTcp)
                    packet.Tcp.CleanClose("Loopback connection");

                return;
            }

            // dont send back pong if received tunneled and no longer need to use global proxies
            // remote would only send tunneled ping if UseGlobalProxies published info on network
            // let our global address expire from remote's routing table
            if (packet.Tunneled && !UseGlobalProxies)
                return;

            // setup pong reply
            Pong pong = new Pong();

            if (ping.Source != null)
                pong.Source = GetLocalSource();

            if (ping.RemoteIP != null)
                pong.RemoteIP = packet.Source.IP;

            // received tcp
            if (packet.ReceivedTcp)
            {
                if (ping.Source == null)
                {
                    packet.Tcp.SendPacket(pong);
                    return;
                }

                if (ping.Source.Firewall == FirewallType.Open)
                    Routing.Add(new DhtContact(ping.Source, packet.Source.IP));

                // received incoming tcp means we are not firewalled
                if (!packet.Tcp.Outbound)
                    // done here to prevent setting open for loopback tcp connection
                    Core.Context.SetFirewallType(FirewallType.Open);

                // check if already connected
                if (packet.Tcp.Proxy == ProxyType.Unset && TcpControl.GetProxy(ping.Source) != null)
                {
                    packet.Tcp.CleanClose("Dupelicate Connection");
                    return;
                }

                packet.Tcp.UserID = ping.Source.UserID;
                packet.Tcp.ClientID = ping.Source.ClientID;
                packet.Tcp.TcpPort = ping.Source.TcpPort;
                packet.Tcp.UdpPort = ping.Source.UdpPort;

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

            // ping received udp or tunneled
            else
            {
                // received udp traffic, we must be behind a NAT at least
                if (!packet.Tunneled)
                    Core.Context.SetFirewallType(FirewallType.NAT);

                Routing.TryAdd(packet, ping.Source);

                // send pong
                SendPacket(packet.Source, pong);
            }
        }

        void Receive_Pong(G2ReceivedPacket packet)
        {
            Pong pong = Pong.Decode(packet);

            if (pong.RemoteIP != null && !packet.Tunneled)
                Core.Context.LocalIP = pong.RemoteIP;


            // if received tcp
            if (packet.ReceivedTcp)
            {
                // if regular interval pong 
                if (pong.Source == null)
                {
                    // keep routing entry fresh so connect state remains
                    if (packet.Tcp.Proxy == ProxyType.Server)
                        Routing.Add(new DhtContact(packet.Tcp, packet.Tcp.RemoteIP), true);
                }

                // else connect pong with source info
                else
                {
                    // usually a proxied pong from somewhere else to keep our routing fresh
                    if (pong.Source.Firewall == FirewallType.Open)
                        Routing.Add(new DhtContact(pong.Source, packet.Source.IP), true);

                    // pong's direct flag ensures that tcp connection info (especially client ID) is not set with 
                    //   information from a pong routed through the remote host, but from the host we're directly connected to
                    if (pong.Direct)
                    {
                        packet.Tcp.UserID = pong.Source.UserID;
                        packet.Tcp.ClientID = pong.Source.ClientID;
                        packet.Tcp.TcpPort = pong.Source.TcpPort;
                        packet.Tcp.UdpPort = pong.Source.UdpPort;

                        // if firewalled
                        if (packet.Tcp.Outbound && packet.Tcp.Proxy == ProxyType.Unset)
                        {
                            if (Core.Context.Firewall != FirewallType.Open && TcpControl.AcceptProxy(ProxyType.Server, pong.Source.UserID))
                            {
                                // send proxy request
                                ProxyReq request = new ProxyReq();
                                request.SenderID = Local.UserID;
                                request.Type = (Core.Context.Firewall == FirewallType.Blocked) ? ProxyType.ClientBlocked : ProxyType.ClientNAT;
                                packet.Tcp.SendPacket(request);
                            }

                            // else ping/pong done, end connect
                            else
                                packet.Tcp.CleanClose("Not in need of a proxy");
                        }
                    }
                }
            }

            // pong received udp or tunneled
            else
            {
                if (!packet.Tunneled)
                    Core.Context.SetFirewallType(FirewallType.NAT);

                // send bootstrap request for nodes if network not responsive
                // do tcp connect because if 2 nodes on network then one needs to find out they're open
                if (!Responsive)
                {
                    Searches.SendRequest(packet.Source, Local.UserID, 0, Core.DhtServiceID, 0, null);

                    if (!packet.Tunneled) // ip isnt set correctly on tunneled, and if tunneled then global active and host tested anyways
                        TcpControl.MakeOutbound(packet.Source, pong.Source.TcpPort, "pong bootstrap");
                }

                // add to routing
                // on startup, especially in sim everyone starts blocked so pong source firewall is not set right, but still needs to go into routing
                Routing.TryAdd(packet, pong.Source, true);

                // forward to proxied nodes, so that their routing tables are up to date, so they can publish easily
                if (Core.Context.Firewall == FirewallType.Open)
                {
                    pong.FromAddress = packet.Source;
                    pong.RemoteIP = null;
                    pong.Direct = false;

                    foreach (TcpConnect connection in TcpControl.ProxyClients)
                        connection.SendPacket(pong);
                }
            }
        }

        internal void Receive_ProxyRequest(G2ReceivedPacket packet)
        {
            ProxyReq request = ProxyReq.Decode(packet);

            ProxyAck ack = new ProxyAck();
            ack.Source = GetLocalSource();

            // check if there is space for type required
            if (Core.Context.Firewall == FirewallType.Open  && TcpControl.AcceptProxy(request.Type, ack.Source.UserID))
            {
                ack.Accept = true;
            }
            else if (packet.ReceivedTcp)
            {
                packet.Tcp.CleanClose("Couldn't accept proxy request");
                return;
            }
                


            // always send some contacts along so node can find closer proxy
            ack.ContactList = Routing.Find(request.SenderID, 8);


            // received request tcp
            if (packet.ReceivedUdp)
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
            if (packet.ReceivedUdp && ack.Source.Firewall == FirewallType.Open)
                Routing.Add(new DhtContact(ack.Source, packet.Source.IP));

            foreach (DhtContact contact in ack.ContactList)
                Routing.Add(contact);


            // dont do proxy if we're not firewalled or remote host didnt accept
            if (Core.Context.Firewall == FirewallType.Open || !ack.Accept)
            {
                if (packet.ReceivedTcp)
                    packet.Tcp.CleanClose("Proxy request rejected");

                return;
            }

            // received ack udp
            if (packet.ReceivedUdp)
            {
                if(!TcpControl.ProxyMap.ContainsKey(ack.Source.UserID))
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

        internal void Send_CrawlRequest(DhtAddress address, DhtClient target)
        {
            CrawlRequest request = new CrawlRequest();

            request.Target = target;

            SendPacket(address, request);
        }

        internal void Receive_CrawlRequest(G2ReceivedPacket packet)
        {
            CrawlRequest request = CrawlRequest.Decode(packet);


            if (Local.Equals(request.Target))
            {
                Send_CrawlAck(request, packet);
            }

            // Forward to appropriate node
            else
            {
                TcpConnect client = TcpControl.GetProxy(request.Target);

                if (client != null)
                {
                    request.FromAddress = packet.Source; // add so receiving host knows where to send response too

                    client.SendPacket(request);
                }
            }
        }

        internal void Send_CrawlAck(CrawlRequest req, G2ReceivedPacket packet)
        {
            CrawlAck ack = new CrawlAck();

            ack.Source  = GetLocalSource();
            ack.Version = System.Windows.Forms.Application.ProductVersion;
            ack.Uptime  = (Core.TimeNow - Core.StartTime).Seconds;


            foreach (TcpConnect connection in TcpControl.ProxyServers)
                ack.ProxyServers.Add(new DhtContact(connection, connection.RemoteIP));

            foreach (TcpConnect connection in TcpControl.ProxyClients)
                ack.ProxyClients.Add(new DhtContact(connection, connection.RemoteIP));


            if (packet.ReceivedTcp)
            {
                ack.ToAddress = packet.Source;
                packet.Tcp.SendPacket(ack);
            }
            else
                SendPacket(packet.Source, ack);
        }

        internal void Receive_CrawlAck(G2ReceivedPacket packet)
        {
            CrawlAck ack = CrawlAck.Decode(packet);

            if (GuiCrawler != null)
                GuiCrawler.BeginInvoke(GuiCrawler.CrawlAck, ack, packet);
            
        }

        internal void UpdateLog(string type, string message)
        {
            if (Core.Sim != null && !Core.Sim.Internet.Logging)
                return;

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

        internal void LogPacket(PacketLogEntry logEntry)
        {
            if (Core.Sim != null && !Core.Sim.Internet.Logging)
                return;

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
        }

        internal bool UseGlobalProxies;

        internal void CheckGlobalProxyMode()
        {
            Debug.Assert(!IsGlobal);

            // if blocked/NATed connected to global but not the op, then we are in global proxy mode
            // global proxy mode removed once connection to op is established
            // global proxies are published with location data so that communication can be tunneled
            // hosts in global proxy mode are psuedo-open meaning they act similarly to open hosts in that 
            // they are added to the routing table and they conduct search/store like an open node

            OpCore global = Core.Context.Global;

            bool useProxies = (Core.Context.Firewall != FirewallType.Open &&
                                Core.TimeNow > Core.StartTime.AddSeconds(15) &&
                                global != null && global.Network.TcpControl.ProxyServers.Count > 0 &&
                                TcpControl.ProxyServers.Count == 0);


            // if no state change return
            if (useProxies == UseGlobalProxies)
                return;

            UseGlobalProxies = useProxies;

            if (UseGlobalProxies)
            {
                // socket will handle publishing after 15 secs, location timer handles re-publishing
            }
            else
            {
                // global proxies should remove themselves from routing by timing out
            }
        }
    }

    internal class PacketCopy
    {
        internal G2ReceivedPacket Packet;

        internal PacketCopy(G2ReceivedPacket packet)
        {
            Packet = packet;
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

