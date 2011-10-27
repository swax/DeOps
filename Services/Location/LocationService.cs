using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Transport;


namespace DeOps.Services.Location
{
    internal delegate void LocationUpdateHandler(LocationData location);
    internal delegate void LocationGuiUpdateHandler(ulong key);
    internal delegate void KnowOnlineHandler(List<ulong> users);

    internal delegate byte[] GetLocationTagHandler();
    internal delegate void LocationTagReceivedHandler(DhtAddress address, ulong user, byte[] tag);

    /*
     * The old location system published location every 3 mins, and everyone interested would search every 3 mins for
     * the updated location info.  This was done to prevent flooding of the host itself, as popular location info would
     * be replicated.
     * 
     * Location info still needs to be published periodically (a firewalled host that cant find a proxy close to his
     * own userID).  But the entire network doesnt need to periodically search.  Once a user loc is found, it is pinged
     * and that future loc updates are done direclty between the two.
     */
    
    internal class LocationService : OpService
    {
        public string Name { get { return "Location"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.Location; } }

        OpCore Core;
        DhtNetwork Network;

        internal uint LocationVersion = 1;
        internal DateTime NextGlobalPublish;

        internal ClientInfo LocalClient;
        internal ThreadedDictionary<ulong, ClientInfo> Clients = new ThreadedDictionary<ulong, ClientInfo>();

        Dictionary<ulong, DateTime> NextResearch = new Dictionary<ulong, DateTime>();

        internal LocationUpdateHandler LocationUpdate;
        internal LocationGuiUpdateHandler GuiUpdate;

        internal KnowOnlineHandler KnowOnline;
        List<ulong> LocalPings = new List<ulong>(); // local users who we are interested in pinging

        LinkedList<DateTime> RecentPings = new LinkedList<DateTime>();
        Dictionary<DhtClient, DateTime> NotifyUsers = new Dictionary<DhtClient, DateTime>(); // users who are interested in our updates, and when that interest expires
        LinkedList<DhtClient> PendingNotifications = new LinkedList<DhtClient>();


        internal ServiceEvent<GetLocationTagHandler> GetTag = new ServiceEvent<GetLocationTagHandler>();
        internal ServiceEvent<LocationTagReceivedHandler> TagReceived = new ServiceEvent<LocationTagReceivedHandler>();

        int PruneLocations = 64;
        int MaxClientsperUser = 10;
        internal bool LocalAway;


        internal LocationService(OpCore core)
        {
            Core = core;
            Network = core.Network;

            Core.Locations = this;

            Core.SecondTimerEvent += new TimerHandler(Core_SecondTimer);
            Core.MinuteTimerEvent += new TimerHandler(Core_MinuteTimer);

            Network.CoreStatusChange += new StatusChange(Network_StatusChange);

            Network.Store.StoreEvent[ServiceID, 0] += new StoreHandler(Store_Local);
            Network.Searches.SearchEvent[ServiceID, 0] += new SearchRequestHandler(Search_Local);

            Network.LightComm.Data[ServiceID, 0] += new LightDataHandler(LightComm_ReceiveData);

            Network.Store.ReplicateEvent[ServiceID, 0] += new ReplicateHandler(Store_Replicate);

            UpdateLocation();

            if (Core.Sim != null)
            {
                PruneLocations = 16;
            }
        }

        public void Dispose()
        {
            Core.SecondTimerEvent -= new TimerHandler(Core_SecondTimer);
            Core.SecondTimerEvent -= new TimerHandler(Core_MinuteTimer);

            Network.CoreStatusChange -= new StatusChange(Network_StatusChange);

            Network.Store.StoreEvent[ServiceID, 0] -= new StoreHandler(Store_Local);
            Network.Searches.SearchEvent[ServiceID, 0] -= new SearchRequestHandler(Search_Local);

            Network.LightComm.Data[ServiceID, 0] -= new LightDataHandler(LightComm_ReceiveData);

            Network.Store.ReplicateEvent[ServiceID, 0] -= new ReplicateHandler(Store_Replicate);

            // shotgun udp, let everyone know we're going offline
            GoingOffline();
        }



        void Network_StatusChange()
        {
            if (!Network.Responsive)
                return;

            if(!Core.User.Settings.Invisible)
                GoingOnline();
        }

        List<byte[]> Store_Replicate(DhtContact contact)
        {
            DataReq req = new DataReq(null, Core.UserID, ServiceID, 0, LocalClient.SignedData); 

            // only replicating to open nodes directly
            Network.Store.Send_StoreReq(contact, null, req);

            return null;
        }

        void Core_SecondTimer()
        {
            OpCore global = Core.Context.Lookup;

            // global publish - so others can find entry to the op
            if (Core.User.Settings.OpAccess != AccessType.Secret && 
                global != null &&
                global.Network.Responsive &&
                (global.Firewall == FirewallType.Open || Network.UseLookupProxies) && 
                Core.TimeNow > NextGlobalPublish)
                PublishGlobal();

            
            if (!Network.Established)
                return;


            // run code below every quarter second
            if (Core.TimeNow.Second % 15 != 0)
                return;


            // keep local client from being pinged, or removed
            LocalClient.LastSeen = Core.TimeNow.AddMinutes(1);
            LocalClient.NextPing = Core.TimeNow.AddMinutes(1); 


            // remove expired clients - either from not notifying us, or we've lost interest and stopped pinging
            Clients.LockWriting(delegate()
            {
                foreach (ClientInfo client in Clients.Values.Where(c => Core.TimeNow > c.Timeout).ToArray())
                {
                    Clients.Remove(client.RoutingID);
                    SignalUpdate(client, false);
                }
            });


            // get form services users that we should keep tabs on
            LocalPings.Clear();
            KnowOnline.Invoke(LocalPings);


            // ping clients that we are locally caching, or we have interest in
            Clients.LockReading(delegate()
            {
                foreach (ClientInfo client in (from c in Clients.Values
                                               where Core.TimeNow > c.NextPing &&
                                                     (Network.Routing.InCacheArea(c.UserID) || LocalPings.Contains(c.UserID))
                                               select c).ToArray())
                    Send_Ping(client);

            });


            // remove users no longer interested in our upates
            foreach (DhtClient expired in (from id in NotifyUsers.Keys
                                           where Core.TimeNow > NotifyUsers[id]
                                           select id).ToArray())
            {
                NotifyUsers.Remove(expired);

                if (PendingNotifications.Contains(expired))
                    PendingNotifications.Remove(expired);
            }


            // send 2 per second, if new update, start over again
            foreach (DhtClient client in PendingNotifications.Take(2).ToArray())
            {
                LocationNotify notify = new LocationNotify();
                notify.Timeout = CurrentTimeout;
                notify.SignedLocation = LocalClient.SignedData;
                Network.LightComm.SendReliable(client, ServiceID, 0, notify);

                PendingNotifications.Remove(client);
            }
        }

        void Core_MinuteTimer()
        {
            if(Clients.SafeCount < PruneLocations)
                return;

            // second timer handles dead locs

            // minute timer handles pruning of too many locs


            Clients.LockWriting(delegate()
            {
                var remove = (from c in Clients.Values
                             where c.UserID != Core.UserID &&
                                    !Network.Routing.InCacheArea(c.RoutingID) &&
                                    !Core.KeepData.SafeContainsKey(c.UserID) &&
                                    !LocalPings.Contains(c.UserID)
                             orderby c.RoutingID ^ Network.Routing.LocalRoutingID descending
                             select c).Take(Clients.Count - PruneLocations).ToArray();

                foreach (ClientInfo client in remove)
                {
                    Clients.Remove(client.RoutingID);
                    SignalUpdate(client, false);
                }
            });

   
            // clean research map
            foreach(ulong user in NextResearch.Keys.Where(u => Core.TimeNow > NextResearch[u]).ToArray())
                NextResearch.Remove(user);
        }

        public void SimTest()
        {
        }

        public void SimCleanup()
        {
        }

        internal void PublishGlobal()
        {
            Debug.Assert(Core.User.Settings.OpAccess != AccessType.Secret);


            // should be auto-set like a second after tcp connect
            // this isnt called until 15s after tcp connect
            if (Core.Context.Lookup.LocalIP == null)
                return;

            // set next publish time 55 mins
            NextGlobalPublish = Core.TimeNow.AddMinutes(55);

            LocationData location = GetLocalLocation();

            // network iniitialized with ip/firewall set to default, use global values
            location.IP = Core.Context.Lookup.LocalIP;
            location.Source.Firewall = Core.Context.Lookup.Firewall;


            // location packet is encrypted and published on lookup network at op id dht position

            byte[] data = location.EncodeLight(Network.Protocol);

            if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
                data = Utilities.EncryptBytes(data, Network.OpCrypt.Key);

            Core.Context.Lookup.RunInCoreAsync(delegate()
            {
                LookupService service = Core.Context.Lookup.GetService(Services.ServiceIDs.Lookup) as LookupService;
                service.LookupCache.Publish(Network.OpID, data);
            });
        }

        internal void UpdateLocation()
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreBlocked(delegate() { UpdateLocation(); });
                return;
            }

            LocationData location = GetLocalLocation();
            
            byte[] signed = SignedData.Encode(Network.Protocol, Core.User.Settings.KeyPair, location);

            Store_Local(new DataReq(null, Core.UserID, ServiceID, 0, signed));

            // update oldest to newest (update oldest with new address/info before we ping timeout)
            PendingNotifications = new LinkedList<DhtClient>(NotifyUsers.Keys.OrderBy(c => NotifyUsers[c]));
        }

        internal void StartSearch(ulong id, uint version)
        {
            byte[] parameters = BitConverter.GetBytes(version);

            DhtSearch search = Network.Searches.Start(id, "Location", ServiceID, 0, parameters, Search_Found);

            if (search != null)
                search.TargetResults = 2;
        }

        void Search_Found(DhtSearch search, DhtAddress source, byte[] data)
        {
            DataReq store = new DataReq(source, search.TargetID, ServiceID, 0, data);

            Store_Local(store);
        }

        void Search_Local(ulong key, byte[] parameters, List<byte[]> results)
        {
            if (Core.User.Settings.Invisible)
                return;

            uint minVersion = BitConverter.ToUInt32(parameters, 0);

            List<ClientInfo> clients = GetClients(key);

            foreach (ClientInfo info in clients)
                if (info.Data.Version >= minVersion)
                    results.Add(info.SignedData);
        }

        internal void Store_Local(DataReq store)
        {
            // getting published to - search results - patch

            SignedData signed = SignedData.Decode(store.Data);

            if (signed == null)
                return;

            G2Header embedded = new G2Header(signed.Data);

            // figure out data contained
            if (G2Protocol.ReadPacket(embedded))
                if (embedded.Name == LocationPacket.Data)
                {
                    LocationData location = LocationData.Decode(signed.Data);

                    if (Utilities.CheckSignedData(location.Key, signed.Data, signed.Signature))
                        Process_LocationData(store, signed, location);
                    else
                        Debug.Assert(false);
                }
        }

        private void Process_LocationData(DataReq data, SignedData signed, LocationData location)
        {
            Core.IndexKey(location.UserID, ref location.Key);
 
            Debug.Assert(location.UserID == location.Source.UserID);
            if (location.UserID != location.Source.UserID)
                return;


            ClientInfo client = GetLocationInfo(location.UserID, location.Source.ClientID);
           
            // check location version
            if (client != null)
            {
                if (location.Version == client.Data.Version)
                    return;

                else if (location.Version < client.Data.Version)
                {
                    if (data != null && data.Source != null)
                        Network.Store.Send_StoreReq(data.Source, data.LocalProxy, new DataReq(null, client.Data.UserID, ServiceID, 0, client.SignedData));

                    return;
                }
            }

            Core.IndexName(location.UserID, location.Name);
        
            // notify components of new versions (usually just localsync service signed up for this)
            DhtAddress address = new DhtAddress(location.IP, location.Source);

            foreach (PatchTag tag in location.Tags)
                if(TagReceived.Contains(tag.Service, tag.DataType))
                    TagReceived[tag.Service, tag.DataType].Invoke(address, location.UserID, tag.Tag);


            // add location
            if (client == null)
            {
                // if too many clients, and not us, return
                if (location.UserID != Core.UserID && ActiveClientCount(location.UserID) > MaxClientsperUser)
                    return;

                client = new ClientInfo(location);

                Clients.SafeAdd(client.RoutingID, client);

                // dont need to worry about remote caching old locs indefinitely because if a loc is cached remotely
                // that means the remote is being continuall pinged, or else the loc would expire
                // if we're still interested in loc after a min, it will be pinged locally
            }

            client.Data = location;
            client.SignedData = signed.Encode(Network.Protocol);

            if (client.Data.UserID == Core.UserID && client.Data.Source.ClientID == Network.Local.ClientID)
                LocalClient = client;


            AddRoutingData(location);

            // only get down here if loc was new version in first place (recently published)
            // with live comm trickle down this prevents highers from being direct ping flooded to find their
            // online status
            client.LastSeen = Core.TimeNow;

            SignalUpdate(client, true);
        }

        internal void AddRoutingData(LocationData location) // a light loc can also be used here
        {
            // if open and not global, add to routing
            if (location.Source.Firewall == FirewallType.Open)
                Network.Routing.Add(new DhtContact(location.Source, location.IP));

            // add global proxies (they would only be included in location packet if source was not directly connected to OP
            // even if open add the GP because pinging them will let host know of an open node on the network to connect to
            foreach (DhtAddress server in location.TunnelServers)
                Network.Routing.Add(new DhtContact(location.Source, location.IP, location.TunnelClient, server));

            Network.LightComm.Update(location);

        }

        internal LocationData GetLocalLocation()
        {
            LocationData location = new LocationData();

            location.Key = Core.User.Settings.KeyPublic;
            location.IP = Core.LocalIP;
            location.Source = Network.GetLocalSource();

            if (Network.UseLookupProxies)
            {
                location.TunnelClient = new TunnelAddress(Core.Context.Lookup.Network.Local, Core.TunnelID);

                foreach (TcpConnect socket in Core.Context.Lookup.Network.TcpControl.ProxyServers)
                    location.TunnelServers.Add(new DhtAddress(socket.RemoteIP, socket));
            }

            foreach (TcpConnect socket in Network.TcpControl.ProxyServers)
                location.Proxies.Add(new DhtAddress(socket.RemoteIP, socket));

            location.Name = Core.User.Settings.UserName;
            location.Place = Core.User.Settings.Location;
            location.GmtOffset = (int)TimeZone.CurrentTimeZone.GetUtcOffset(Core.TimeNow).TotalMinutes;
            location.Away = LocalAway;
            location.AwayMessage = LocalAway ? Core.User.Settings.AwayMessage : "";

            location.Version = LocationVersion++;
            location.License = Core.Context.LicenseProof;

            foreach (uint service in GetTag.HandlerMap.Keys)
                foreach (uint datatype in GetTag.HandlerMap[service].Keys)
                {
                    PatchTag tag = new PatchTag();

                    tag.Service = service;
                    tag.DataType = datatype;
                    tag.Tag = GetTag[service, datatype].Invoke();

                    if (tag.Tag != null)
                    {
                        Debug.Assert(tag.Tag.Length < 8);

                        if (tag.Tag.Length < 8)
                            location.Tags.Add(tag);
                    }
                }

            return location;
        }

        void LightComm_ReceiveData(DhtClient client, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (G2Protocol.ReadPacket(root))
            {
                if (root.Name == LocationPacket.Ping)
                    Receive_Ping(client, LocationPing.Decode(root));

                if (root.Name == LocationPacket.Notify)
                    Receive_Notify(client, LocationNotify.Decode(root));
            }

        }

        private void Send_Ping(ClientInfo client)
        {
            client.NextPing = Core.TimeNow.AddSeconds(client.PingTimeout);

            LocationPing ping = new LocationPing();

            ping.RemoteVersion = client.Data.Version;

            Network.LightComm.SendReliable(client, ServiceID, 0, ping);
        }

        int CurrentTimeout = 60;

        void Receive_Ping(DhtClient client, LocationPing ping)
        {
            if (Core.User.Settings.Invisible)
                return;

            LocationNotify notify = new LocationNotify();

            RecentPings.AddFirst(Core.TimeNow);
            while (RecentPings.Count > 30)
                RecentPings.RemoveLast();

            // we want a target of 20 pings per minute ( 1 every 3 seconds)
            // pings per minute = RecentPings.count / (Core.TimeNow - RecentPings.Last).ToMinutes()
            float pingsPerMinute = (float)RecentPings.Count / (float)(Core.TimeNow - RecentPings.Last.Value).Minutes;
            notify.Timeout = (int)(60.0 * pingsPerMinute / 20.0); // 20 is target rate, so if we have 40ppm, multiplier is 2, timeout 120seconds
            notify.Timeout = Math.Max(60, notify.Timeout); // use 60 as lowest timeout
            CurrentTimeout = notify.Timeout;

            if (ping.RemoteVersion < LocalClient.Data.Version)
                notify.SignedLocation = LocalClient.SignedData;

            if (PendingNotifications.Contains(client))
                PendingNotifications.Remove(client);

            //put node on interested list
            NotifyUsers[client] = Core.TimeNow.AddSeconds(notify.Timeout + 15);


            // *** small security concern, notifies are not signed so they could be forged
            // signing vs unsigning is 144 vs 7 bytes, the bandwidth benefits outweigh forging
            // someone's online status at the moment
            // byte[] unsigned = notify.Encode(Network.Protocol);
            // byte[] signed = SignedData.Encode(Network.Protocol, Core.User.Settings.KeyPair, notify);


            Network.LightComm.SendReliable(client, ServiceID, 0, notify);
        }

        void Receive_Notify(DhtClient client, LocationNotify notify)
        {
            if (notify.SignedLocation != null)
                Store_Local(new DataReq(null, client.UserID, ServiceID, 0, notify.SignedLocation));
            

            ClientInfo info;
            if(!Clients.SafeTryGetValue(client.RoutingID, out info))
                return;

            if (notify.GoingOffline)
            {
                Clients.SafeRemove(client.RoutingID);
                SignalUpdate(info, false);
                return;
            }

            info.LastSeen = Core.TimeNow;
            info.PingTimeout = notify.Timeout;
            info.NextPing = Core.TimeNow.AddSeconds(notify.Timeout);
        }

        void SignalUpdate(ClientInfo client, bool online)
        {
            if (LocationUpdate != null)
                LocationUpdate.Invoke(client.Data);

            Core.RunInGuiThread(GuiUpdate, client.Data.UserID);
        }

        internal ClientInfo GetLocationInfo(ulong user, ushort client)
        {
            ClientInfo info = null;
         
            if (Clients.SafeTryGetValue(user ^ client, out info))
                return info;

            return null;
        }

        internal string GetLocationName(ulong user, ushort client)
        {
            ClientInfo current = Core.Locations.GetLocationInfo(user, client);

            if(current == null)
                return client.ToString();

            LocationData data = current.Data;

            // if no loc use instance xx
            if (data == null || data.Place == null || data.Place == "")
            {
                string site = client.ToString();
                return "Site " + site.Substring(site.Length - 2, 2);
                //return data.IP.ToString();
            }

            return data.Place;
        }

        internal void Research(ulong user)
        {
            // sets local interested, but will probably be cleared
            // searches for location, if still interested later, ping is sent for updated loc info
            // location return is enough to trigger yea im online!

            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { Research(user); });
                return;
            }

            if (!Network.Responsive)
                return;

            // limit re-search to once per 30 secs
            DateTime timeout = default(DateTime);

            if (NextResearch.TryGetValue(user, out timeout))
                if (Core.TimeNow < timeout)
                    return;

            StartSearch(user, 0);

            NextResearch[user] = Core.TimeNow.AddSeconds(30);
        }

        internal int ActiveClientCount(ulong user)
        {
            int count = 0;

            Clients.LockReading(delegate()
            {
                count = Clients.Values.Count(c => c.UserID == user); 
            });

            return count;
        }

        internal List<ClientInfo> GetClients(ulong user)
        {
            List<ClientInfo> results = new List<ClientInfo>();

            Clients.LockReading(delegate()
            {
                results = Clients.Values.Where(c => c.UserID == user).ToList();
            });

            return results;
        }

        internal void SetInvisble(bool mode)
        {
            if (Core.User.Settings.Invisible == mode)
                return;

            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => SetInvisble(mode));
                return;
            }

            Core.User.Settings.Invisible = mode;

            if (mode)
                GoingOffline();
            else
                GoingOnline();

            SignalUpdate(LocalClient, mode);

            Core.RunInCoreAsync(() => Core.User.Save());
        }

        private void GoingOnline()
        {
            // re-publish location on network
            // afterwards when new nodes come in range - replicate directly
            // local area pings us to keep their caches up to date
            // other nodes only ping us if they are locally interetested (they dont ping on getting a search result)

            UpdateLocation();

            // locs are published for the main benefit of firewalled hosts
            // they may have a proxy that is nowhere near their true dht position
            // dont need to re-publish, these hosts will continually ping us
            Network.Store.PublishNetwork(Core.UserID, ServiceID, 0, LocalClient.SignedData);
        }

        private void GoingOffline()
        {
            foreach (DhtClient client in NotifyUsers.Keys)
            {
                LocationNotify notify = new LocationNotify();
                notify.Timeout = CurrentTimeout;
                notify.GoingOffline = true;
                Network.LightComm.SendReliable(client, ServiceID, 0, notify, true);
            }

            NotifyUsers.Clear();
        }

        internal void SetAway(bool mode, string msg)
        {
            if (LocalAway == mode)
                return;

            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => SetAway(mode, msg));
                return;
            }

            LocalAway = mode;
            Core.User.Settings.AwayMessage = msg;

            Core.Locations.UpdateLocation(); // notify users of status change

            Core.User.Save();
        }
    }

    internal class ClientInfo : DhtClient
    {
        internal LocationData Data;
        internal byte[] SignedData;

        internal DateTime NextPing; // next time a ping can be sent
        internal DateTime LastSeen; // last time pong was received

        internal int PingTimeout = 60;

        internal DateTime Timeout
        {
            get { return LastSeen.AddSeconds(PingTimeout + 30); }
        }


        internal ClientInfo() { } // used only for InternalData debugging

        internal ClientInfo(LocationData data) :
            base(data.UserID, data.Source.ClientID)
        {

        }
    }


    internal class PatchTag
    {
        internal uint Service;
        internal uint DataType;
        internal byte[] Tag;

        internal byte[] ToBytes()
        {
            byte[] sByte = CompactNum.GetBytes(Service);
            byte[] dByte = CompactNum.GetBytes(DataType);
            
            byte control = (byte) (sByte.Length << 3);
            control |= (byte) dByte.Length;

            int size = 1 + sByte.Length + dByte.Length + Tag.Length;

            byte[] data = new byte[size];

            data[0] = control;
            sByte.CopyTo(data, 1);
            dByte.CopyTo(data, 1 + sByte.Length);
            Tag.CopyTo(data, 1 + sByte.Length + dByte.Length);

            return data;
        }

        internal static PatchTag FromBytes(byte[] data, int pos, int size)
        {
            PatchTag tag = new PatchTag();

            byte control = data[pos];
            int sLength = (control & 0x38) >> 3;
            int dLength = (control & 0x07);

            tag.Service = CompactNum.ToUInt32(data, pos + 1, sLength);
            tag.DataType = CompactNum.ToUInt32(data, pos + 1 + sLength, dLength);

            int dataPos = 1 + sLength + dLength;
            tag.Tag = Utilities.ExtractBytes(data, pos + dataPos, size - dataPos);

            return tag;
        }
    }
}
