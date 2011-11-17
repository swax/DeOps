using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;

using DeOps.Services;
using DeOps.Services.Location;


namespace DeOps.Implementation
{

    public class OpCache
    {
        const int MAX_CACHE = 200;


        OpCore Core;
        DhtNetwork Network;

        
        DateTime NextSave;
        RetryIntervals Retry;

        // web 
        public DateTime NextQueryAny;
        public DateTime NextPublishAny;
        public List<WebCache> WebCaches = new List<WebCache>(); // sorted by distance to self
        
        // ips
        public LinkedList<DhtContact> IPs = new LinkedList<DhtContact>();
        public Dictionary<int, LinkedListNode<DhtContact>> IPTable = new Dictionary<int, LinkedListNode<DhtContact>>();

        // lan
        public int BroadcastTimeout = 0;
        
        RetryIntervals LookupearchInterval;
        DateTime NextLookupSearch;     


        public OpCache(DhtNetwork network)
        {
            Core = network.Core;
            Network = network;

            Core.MinuteTimerEvent += Core_MinuteTimerEvent;

            Retry = new RetryIntervals(Core);
            LookupearchInterval = new RetryIntervals(Core);
            NextSave = Core.TimeNow.AddMinutes(1);
            NextPublishAny = Core.TimeNow.AddMinutes(30);


            if (Network.IsLookup ||
                (Core.User != null && Core.User.Settings.GlobalIM))
            {
                WebCache cache = new WebCache();
                cache.Address = "http://www.c0re.net/deops/cache/update.php";
                cache.AccessKey = Convert.FromBase64String("O+6IRs7GY1r/JIk+DFY/VK+i8pFTWhsDfNH9R3j3f9Q=");
                AddWebCache(cache);
            }
        }

        public void SecondTimer()
        {
            // ip cache
            lock (IPs)
                while (IPs.Count > MAX_CACHE)
                {
                    DhtContact entry = IPs.Last.Value;
                    IPTable.Remove(entry.CacheHash());
                    IPs.RemoveLast();
                }

            // save cache
            if (Core.TimeNow > NextSave)
            {
                if (Network.IsLookup)
                    Network.Lookup.Save(Core);
                else
                    Core.User.Save();

                NextSave = Core.TimeNow.AddMinutes(5);
            }


            // if unresponsive
            if (!Network.Responsive)
            {
                Retry.Timer();

                if (Network.IsLookup)
                    LoookupBootstrap();
                else
                    OpBootstrap();
            }

            // send broadcast in lan mode every 20 secs
            if (Network.LanMode)//&& !IsLookup) //crit re-enable?
            {
                // if disconnected from LAN, once reconnected, establishing should be < 20 secs
                if (BroadcastTimeout <= 0)
                {
                    Ping ping = new Ping();
                    ping.Source = Network.GetLocalSource();
                    Network.LanControl.SendTo(ping);

                    BroadcastTimeout = 20;
                }
                else
                    BroadcastTimeout--;
            }
        }

        void Core_MinuteTimerEvent()
        {
            if(Core.TimeNow > NextPublishAny)
                foreach(WebCache cache in WebCaches)
                    if (Core.TimeNow > cache.NextPublish)
                    {
                        // if fails, NextPublishAny won't be set, and next minute we will try to publish
                        // to another working cache

                        StartThread("WebCache Publish", WebPublish, cache);
                        break;
                    }

            // prune
                // remove largest difference between last tried and last seen
                // this way caches that were never seen are always removed first while ensuring they've been tried
                // if difference between last seen and last tried is more than 5 dayss, remove

            bool KeepRemoving = true;

            while (KeepRemoving)
            {
                KeepRemoving = false;

                WebCache unresponsive = GetMostUnresponsiveCache();

                if (unresponsive == null)
                    break;

                TimeSpan diff = new TimeSpan(unresponsive.LastTried.Ticks - unresponsive.LastSeen.Ticks);

                if (diff.Days > 5 || WebCaches.Count > 20)
                {
                    WebCaches.Remove(unresponsive);
                    KeepRemoving = true;
                }            
            }

            // check if any caches havent been tried yet, only check 1 per minute
            if(ThinkOnline && Core.Sim == null)
                foreach (WebCache cache in WebCaches)
                    if (cache.LastTried == default(DateTime))
                    {
                        StartThread("WebCache Ping", WebPing, cache);
                        break;
                    }
        }

        void StartThread(string name, ParameterizedThreadStart start, object arg)
        {
            Thread thread = new Thread(start);
            thread.Name = name;
            thread.Start(arg);
        }

        private WebCache GetMostUnresponsiveCache()
        {
            WebCache unresponsive = null;
            long biggestDiff = 0;

            foreach(WebCache cache in WebCaches)
                if (cache.LastTried.Ticks - cache.LastSeen.Ticks >= biggestDiff) // >= so last removed first, as well as 0 diff hosts
                {
                    unresponsive = cache;
                    biggestDiff = cache.LastTried.Ticks - cache.LastSeen.Ticks;
                }

            return unresponsive;
        }

        public void AddContact(DhtContact entry)
        {
            lock (IPs)
            {
                if (IPTable.ContainsKey(entry.CacheHash()))
                {
                    entry = IPTable[entry.CacheHash()].Value; // replace entry with dupe to maintain next try info
                    IPs.Remove(entry);
                }

                // sort nodes based on last seen
                LinkedListNode<DhtContact> node = null;

                for (node = IPs.First; node != null; node = node.Next)
                    if (entry.LastSeen > node.Value.LastSeen)
                        break;

                IPTable[entry.CacheHash()] = (node != null) ? IPs.AddBefore(node, entry) : IPs.AddLast(entry);
            }
        }


        const int OnlineConfirmed = 3;
        int OnlineSuccess = OnlineConfirmed;
        DateTime NextOnlineCheck;
        bool ThinkOnline { get { return OnlineSuccess == OnlineConfirmed; } }

        void LoookupBootstrap()
        {
            // only called if network not responsive

            // try website BootstrapTimeout at 1 2 5 10 15 30 / 30 / 30 intervals 
            // reset increment when disconnected
            // dont try web cache for first 10 seconds


            // ensure that if re-connected at anytime then re-connect to network is fast
            // (only called by global network, and only when it's not responsive)
            if (Core.Sim == null)
            {
                // ThinkOnline state changed to connected then retry timers reset
                if (Core.TimeNow > NextOnlineCheck)
                    if (OnlineSuccess > 0)
                    {
                        // check online status by pinging google/yahoo/microsoft every 60 secs
                        LookupPingCheck();
                        NextOnlineCheck = Core.TimeNow.AddSeconds(60);
                    }

                    // if think offline
                    else if (OnlineSuccess == 0)
                    {
                        // try google/yahoo/microsoft every 5 secs
                        LookupPingCheck();
                        NextOnlineCheck = Core.TimeNow.AddSeconds(10);
                    }
            }

            TryWebCache();

            TryIPCache();
        }

        void OpBootstrap()
        {
            OpCore lookup = Core.Context.Lookup;

            // find operation nodes through global net at expanding intervals
            // called from operation network's bootstrap
            if (lookup != null && 
                Core.User.Settings.OpAccess != AccessType.Secret && 
                lookup.Network.Responsive)
            {
                LookupearchInterval.Timer();

                if (Core.TimeNow > NextLookupSearch)
                {
                    NextLookupSearch = LookupearchInterval.NextTry;

                    Network.UpdateLog("general", "Looking for " + Core.User.Settings.Operation);

                    lookup.RunInCoreAsync(delegate()
                    {
                        LookupService service = lookup.GetService(ServiceIDs.Lookup) as LookupService;
                        service.LookupCache.Search(Network.OpID, null, Search_FoundLookup);
                    });
                }
            }

            TryWebCache();

            TryIPCache();
        }

        void Search_FoundLookup(byte[] data, object arg)
        {
            try
            {
                if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
                    data = Utilities.DecryptBytes(data, data.Length, Network.OpCrypt.Key);

                LocationData loc = LocationData.Decode(data);

                if (loc != null)
                    Core.Locations.AddRoutingData(loc);
            }
            catch { }
        }

        private void TryWebCache()
        {
            bool AllowWebTry = (IPs.Count == 0 || Core.TimeNow > Retry.Start.AddSeconds(5));


            // give a few seconds at startup to try to connect to Dht networks from the cache
            if (Core.TimeNow > NextQueryAny && AllowWebTry)
            {
                // if not connected to global use web cache
                if (Core.Sim == null)
                {
                     foreach (WebCache cache in WebCaches)
                         if (Core.TimeNow > cache.NextQuery)
                             StartThread("WebCache Query", WebQuery, cache);
                }

                // only can d/l from global cache in sim, or a secret network with private web cache
                else if (Network.IsLookup || Core.User.Settings.OpAccess == AccessType.Secret)
                {
                    NextQueryAny = Retry.NextTry;
                    Core.Sim.Internet.DownloadCache(Network);
                }
            }
        }

        void WebQuery(object parameter)
        {
            WebCache cache = parameter as WebCache;

            if (ThinkOnline)
                cache.LastTried = Core.TimeNow;

            cache.NextQuery = Retry.NextTry;

            Network.UpdateLog("general", "Trying webcache");

            string response = MakeWebCacheRequest(cache, "query:" + Network.OpID.ToString());
            
            // check for empty or error
            if (response.StartsWith("error:"))
            {
                Network.UpdateLog("WebCache", "Query: " + cache.Address + ": " + response);
                return;
            }
            else
                Network.UpdateLog("general", "Webcache success");

            // else success
            cache.LastSeen = Core.TimeNow;

            NextQueryAny = Core.TimeNow.AddSeconds(10);  // cache.nextry will keep any individual cache from being tried too often 

            // if cache is low this will be restarted
            Core.RunInCoreAsync(delegate() { WebQueryResponse(cache, response); });
        }

        private void WebQueryResponse(WebCache cache, string response)
        {
            double timeout = 0;
            bool low = false;


            foreach (string line in response.Split('\n'))
            {
                // add to node cache
                if (line.StartsWith("node:"))
                {
                    string[] parts = line.Substring(5).Split('/');
                    
                    DhtContact contact = new DhtContact(ulong.Parse(parts[0]), 0, IPAddress.Parse(parts[1]), ushort.Parse(parts[2]), ushort.Parse(parts[3]));
                    contact.LastSeen = Core.TimeNow; // set this so its put at front of list, and is the first to be attempted

                    AddContact(contact);
                }

                // for use with publishing back to cache, dont set our local IP with it in case on
                // network with LAN nodes and web cache active, dont want to reset until we find nodes on external network
                else if (line.StartsWith("remoteip:"))
                    cache.RemoteIP = IPAddress.Parse(line.Substring(9));

                // set next publish time
                else if (line.StartsWith("timeout:"))
                    timeout = double.Parse(line.Substring(8));

                else if (line.StartsWith("load:low"))
                    low = true;
            }

            cache.NextPublish = Core.TimeNow.AddMinutes(Math.Max(timeout, 15)); // dont change to min

            // if cache low auto publish to low cache so that network can be initialized
            if (low)
                new Thread(WebPublish).Start(cache);
                // more caches will be queried by the bootstrap if network not responsive
        }

        void WebPublish(object parameter)
        {
            WebCache cache = parameter as WebCache;

            if (ThinkOnline)
                cache.LastTried = Core.TimeNow;

            cache.NextPublish = Core.TimeNow.AddMinutes(60); // default value if failed

            Network.UpdateLog("general", "Publishing to webcache");

            // remote ip is used for cache/network initialization, when no one has a node determined IP

            string request = "publish:" + Network.OpID + "/" +
                               Core.UserID + "/" +
                               (cache.RemoteIP != null ? cache.RemoteIP : Core.LocalIP) + "/" +
                               Network.TcpControl.ListenPort + "/" +
                               Network.UdpControl.ListenPort;

            string response = MakeWebCacheRequest(cache, request);

            // check for empty or error
            if (response.StartsWith("error:"))
            {
                Network.UpdateLog("WebCache", "Publish: " + cache.Address + ": " + response);
                return;
            }

            // else success
            cache.LastSeen = Core.TimeNow;

            NextPublishAny = Core.TimeNow.AddMinutes(30); // timeout for trying ANY web cache

            // if cache is low this will be restarted
            Core.RunInCoreAsync(delegate() { WebPublishResponse(cache, response); });
        }

        private void WebPublishResponse(WebCache cache, string response)
        {
            bool low = false;
            double timeout = 0;

            foreach (string line in response.Split('\n'))
            {
                if (line.StartsWith("timeout:"))
                    timeout = double.Parse(line.Substring(8));

                else if (line.StartsWith("load:low"))
                    low = true;
            }

            cache.NextPublish = Core.TimeNow.AddMinutes(Math.Max(timeout, 15)); // dont change to math.min!

            // if this cache is low, immediately try to re-publish at an active cache
            if (low)
                NextPublishAny = Core.TimeNow;
        }

        void WebPing(object parameter)
        {
            WebCache cache = parameter as WebCache;

            if (ThinkOnline)
                cache.LastTried = Core.TimeNow;

            string response = MakeWebCacheRequest(cache, "ping:" + Network.OpID.ToString());

            if (response.StartsWith("pong"))
                cache.LastSeen = Core.TimeNow;

            else
                Network.UpdateLog("WebCache", "Ping: " + cache.Address + ": " + response);
        }

        public string MakeWebCacheRequest(WebCache cache, string request)
        {
            try
            {
                RijndaelManaged crypt = new RijndaelManaged();
                crypt.BlockSize = 256;
                crypt.Padding = PaddingMode.Zeros;
                crypt.GenerateIV();
                crypt.Key = cache.AccessKey;

                byte[] data = ASCIIEncoding.ASCII.GetBytes(request);
                byte[] encrypted = crypt.CreateEncryptor().TransformFinalBlock(data, 0, data.Length);


                // php decode requests
                byte[] ivEnc = Utilities.CombineArrays(crypt.IV, encrypted);
                string get = Convert.ToBase64String(ivEnc);

                string response = Utilities.WebDownloadString(cache.Address + "?get=" + Uri.EscapeDataString(get));

                if (response == null || response == "")
                    throw new Exception("Access key not accepted");

                // php encode response
                byte[] decoded = Convert.FromBase64String(response);

                // decode response
                crypt.IV = Utilities.ExtractBytes(decoded, 0, 32);

                data = Utilities.ExtractBytes(decoded, 32, decoded.Length - 32);
                byte[] decrypted = crypt.CreateDecryptor().TransformFinalBlock(data, 0, data.Length);

                response = ASCIIEncoding.ASCII.GetString(decrypted);
                response = response.Trim('\0');

                return response;
            }
            catch (Exception ex)
            {
                return "error: " + ex.Message;
            }
        }

        private void TryIPCache()
        {
            // send pings to nodes in cache, responses will startup the routing system
            // 10 udp pings per second, 10 min retry
            int pings = 0;

            lock (IPs)
                foreach (DhtContact entry in IPs)
                {
                    if (Core.TimeNow < entry.NextTryIP)
                        continue;

                    Network.UpdateLog("general", "Trying peer " + entry.IP);

                    Network.Send_Ping(entry);

                    entry.NextTryIP = Retry.NextTry;

                    pings++;
                    if (pings >= 10)
                        break;
                }


            // if blocked and go through cache and mark as tcp tried
            // 1 outbound tcp per second, 10 min retry
            if (Core.Firewall == FirewallType.Blocked)
                lock (IPs)
                    foreach (DhtContact entry in IPs)
                    {
                        if (Core.TimeNow < entry.NextTryProxy)
                            continue;

                        Network.UpdateLog("general", "Trying direct " + entry.IP);

                        Network.TcpControl.MakeOutbound(entry, entry.TcpPort, "ip cache");

                        entry.NextTryProxy = Retry.NextTry;
                        break;
                    }
        }

        string[] TestSites = new string[] { "www.google.com", 
                                            "www.yahoo.com", 
                                            "www.youtube.com", 
                                            "www.myspace.com"};

        private void LookupPingCheck()
        {
            System.Net.NetworkInformation.Ping pingSender = new System.Net.NetworkInformation.Ping();

            // Create an event handler for ping complete
            pingSender.PingCompleted += new System.Net.NetworkInformation.PingCompletedEventHandler(Ping_Complete);

            Network.UpdateLog("general", "Checking if online...");

            // Send the ping asynchronously
            string site = TestSites[Core.RndGen.Next(TestSites.Length)];
            pingSender.SendAsync(site, 5000, null);
        }

        private void Ping_Complete(object sender, System.Net.NetworkInformation.PingCompletedEventArgs e)
        {
            if (e.Reply != null && e.Reply.Status == System.Net.NetworkInformation.IPStatus.Success)
            {
                // if previously thought we were offline and now looks like reconnected
                if (OnlineSuccess == 0)
                {
                    Reset();

                    foreach (DhtContact entry in IPs)
                    {
                        entry.NextTryIP = new DateTime(0);
                        entry.NextTryProxy = new DateTime(0);
                    }
                }

                if (OnlineSuccess < OnlineConfirmed)
                    OnlineSuccess++;

                if(OnlineSuccess == OnlineConfirmed)
                    Network.UpdateLog("general", "Online check success");
            }

            // not success, try another random site, quickly (3 times) then retry will be 1 min
            else if (OnlineSuccess > 0)
            {
                OnlineSuccess--;

                LookupPingCheck();
            }

        }

        public void Reset()
        {
            Retry.Reset();
            LookupearchInterval.Reset();
            NextQueryAny = new DateTime(0);
            BroadcastTimeout = 0;

            foreach (WebCache cache in WebCaches)
            {
                cache.NextQuery = Core.TimeNow;
                cache.NextPublish = Core.TimeNow.AddMinutes(60);
            }
        }


        public void SaveIPs(PacketStream stream)
        {
            byte type = Network.IsLookup ? IdentityPacket.LookupCachedIP : IdentityPacket.OpCachedIP;

            lock (IPs)
                foreach (DhtContact entry in IPs)
                    if (entry.TunnelClient == null)
                        stream.WritePacket(new CachedIP(type, entry));
        }

        public void SaveWeb(PacketStream stream)
        {
            // randomize
            List<WebCache> source = new List<WebCache>(WebCaches);
            List<WebCache> randomized = new List<WebCache>();

            while (source.Count > 0)
            {
                WebCache pick = source[Core.RndGen.Next(source.Count)];
                randomized.Add(pick);
                source.Remove(pick);
            }

            foreach (WebCache cache in randomized)
                stream.WritePacket(cache);
        }

        public void AddWebCache(WebCache add)
        {
            // look for exact match
            foreach (WebCache cache in WebCaches)
                if (cache.Equals(add))
                    return;

            // cache is pinged in minute timer
            add.NextPublish = Core.TimeNow.AddMinutes(60); // default timeout to publish is 1 hour
            add.PacketType = Network.IsLookup ? IdentityPacket.LookupCachedWeb : IdentityPacket.OpCachedWeb;

            WebCaches.Add(add);
        }

        public List<WebCache> GetLastSeen(int max)
        {
            List<WebCache> result = new List<WebCache>();

            while (result.Count < max)
            {
                WebCache add = null;
                long latest = 0;

                foreach(WebCache cache in WebCaches)
                    if (!result.Contains(cache) && cache.LastSeen.Ticks > latest)
                    {
                        add = cache;
                        latest = cache.LastSeen.Ticks;
                    }

                if (add != null)
                    result.Add(add);
                else
                    break;
            }

            return result;
        }

        public void AddWebCache(List<WebCache> caches)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { AddWebCache(caches); });
                return;
            }

            WebCaches.Clear();

            foreach (WebCache cache in caches)
                AddWebCache(cache);
        }
    }


    public class WebCache : G2Packet
    {
        public const byte Packet_Admin     = 0x10;
        public const byte Packet_Address   = 0x20;
        public const byte Packet_AccessKey = 0x30;
        public const byte Packet_LastSeen  = 0x40;
        public const byte Packet_LastTried = 0x50;

        // used to check if trusted or should publish in link file
        // dont worry about for now, a 'known' unsecure web cache does not compramise op security, op key is not known
        //public ulong Admin; 
        
        public string Address;
        public byte[] AccessKey;

        public DateTime LastSeen;
        public DateTime LastTried;

        public IPAddress RemoteIP;            
        public DateTime NextQuery;
        public DateTime NextPublish;

        public byte PacketType; 
        bool SaveTimeInfo;


        // local caches stored in profile
        public WebCache()
        {
        }

        // caches sent in trust / invite packets
        public WebCache(WebCache cache, byte type)
        {
            Address = cache.Address;
            AccessKey = cache.AccessKey;

            PacketType = type;
            SaveTimeInfo = false;
        }

        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame cache = protocol.WritePacket(null, PacketType, null);

                protocol.WritePacket(cache, Packet_Address, UTF8Encoding.UTF8.GetBytes(Address));
                protocol.WritePacket(cache, Packet_AccessKey, AccessKey);

                if (SaveTimeInfo)
                {
                    protocol.WritePacket(cache, Packet_LastSeen, BitConverter.GetBytes(LastSeen.ToBinary()));
                    protocol.WritePacket(cache, Packet_LastTried, BitConverter.GetBytes(LastTried.ToBinary()));
                }

                return protocol.WriteFinish();
            }
        }

        public static WebCache Decode(G2Header root)
        {
            WebCache cache = new WebCache();

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Address:
                        cache.Address = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_AccessKey:
                        cache.AccessKey = Utilities.ExtractBytes(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_LastSeen:
                        cache.LastSeen = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;

                    case Packet_LastTried:
                        cache.LastTried = DateTime.FromBinary(BitConverter.ToInt64(child.Data, child.PayloadPos));
                        break;
                }
            }

            return cache;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            WebCache check = obj as WebCache;

            if(check == null)
                return false;

            if (string.Compare(Address, check.Address) != 0)
                return false;

            if( !Utilities.MemCompare(AccessKey, check.AccessKey))
                return false;

            return true;
        } 
    }


    class RetryIntervals
    {
        OpCore Core;

        public DateTime Start;
        int Index = 0;
        DateTime LastIncrement;

        int[] Intervals = new int[] { 0, 1, 2, 5, 10, 15, 30 };


        public RetryIntervals(OpCore core)
        {
            Core = core;

            Reset();
        }

        public void Reset()
        {
            Start = Core.TimeNow;
            Index = 0;
            LastIncrement = new DateTime(0);
        }

        public DateTime NextTry
        {
            get
            {
                return Core.TimeNow.AddMinutes(Intervals[Index]);
            }
        }
    
        public void Timer()
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
