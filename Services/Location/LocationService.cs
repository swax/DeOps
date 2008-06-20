using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Transport;
using RiseOp.Services.Trust;


namespace RiseOp.Services.Location
{
    internal delegate void LocationUpdateHandler(LocationData location);
    internal delegate void LocationGuiUpdateHandler(ulong key);

    internal delegate byte[] GetLocationTagHandler();
    internal delegate void LocationTagReceivedHandler(DhtAddress address, ulong user, byte[] tag);


    internal class LocationService : OpService
    {
        public string Name { get { return "Location"; } }
        public uint ServiceID { get { return 2; } }

        OpCore Core;

        internal uint LocationVersion = 1;
        internal DateTime NextLocationUpdate;
        internal DateTime NextGlobalPublish;

        internal ClientInfo LocalLocation;
        internal ThreadedDictionary<ulong, ThreadedDictionary<ushort, ClientInfo>> LocationMap = new ThreadedDictionary<ulong, ThreadedDictionary<ushort, ClientInfo>>();

        Dictionary<ulong, DateTime> NextResearch = new Dictionary<ulong, DateTime>();

        internal LocationUpdateHandler LocationUpdate;
        internal LocationGuiUpdateHandler GuiUpdate;

        internal ServiceEvent<GetLocationTagHandler> GetTag = new ServiceEvent<GetLocationTagHandler>();
        internal ServiceEvent<LocationTagReceivedHandler> TagReceived = new ServiceEvent<LocationTagReceivedHandler>();

        int PruneLocations = 64;
        int MaxClientsperUser = 10;
        internal bool LocalAway;


        internal LocationService(OpCore core)
        {
            Core = core;
            Core.Locations = this;

            Core.SecondTimerEvent += new TimerHandler(Core_SecondTimer);
            Core.MinuteTimerEvent += new TimerHandler(Core_MinuteTimer);

            Core.Network.Store.StoreEvent[ServiceID, 0] += new StoreHandler(OperationStore_Local);
            Core.Network.Searches.SearchEvent[ServiceID, 0] += new SearchRequestHandler(OperationSearch_Local);


            if (Core.Sim != null)
            {
                PruneLocations     = 16;
            }
        }

       

        public void Dispose()
        {
            Core.SecondTimerEvent -= new TimerHandler(Core_SecondTimer);
            Core.SecondTimerEvent -= new TimerHandler(Core_MinuteTimer);

            Core.Network.Store.StoreEvent[ServiceID, 0] -= new StoreHandler(OperationStore_Local);
            Core.Network.Searches.SearchEvent[ServiceID, 0] -= new SearchRequestHandler(OperationSearch_Local);
        }

        void Core_SecondTimer()
        {
            OpCore global = Core.Context.Global;

            // global publish
            if ((Core.Context.Firewall == FirewallType.Open || Core.Network.UseGlobalProxies) &&
                global != null &&
                global.Network.Responsive &&
                Core.TimeNow > NextGlobalPublish)
                PublishGlobal();

            // operation publish
            if (Core.Network.Responsive && Core.TimeNow > NextLocationUpdate)
                UpdateLocation();

            // run code below every quarter second
            int second = Core.TimeNow.Second + 1; // get 1 - 60 value
            if (second % 15 != 0)
                return;


            // operation ttl 
            Dictionary<ulong, bool> affectedUsers = new Dictionary<ulong, bool>();
            List<ushort> deadClients = new List<ushort>();

            LocationMap.LockReading(delegate()
            {
                foreach (ThreadedDictionary<ushort, ClientInfo> clients in LocationMap.Values)
                {
                    deadClients.Clear();

                    clients.LockReading(delegate()
                    {
                        foreach (ClientInfo location in clients.Values)
                        {
                            if (second == 60)
                            {
                                if (location.TTL > 0)
                                    location.TTL--;

                                if (location.TTL == 0)
                                {
                                    deadClients.Add(location.ClientID);
                                    affectedUsers[location.Data.UserID] = true;
                                }
                            }

                            //crit hack - last 30 and 15 secs before loc destroyed do searches (working pretty good through...)
                            if (location.TTL == 1 && (second == 15 || second == 30))
                                StartSearch(location.Data.UserID, 0);
                        }
                    });

                    foreach (ushort dead in deadClients)
                        clients.SafeRemove(dead);
                }
            });

            LocationMap.LockWriting(delegate()
            {
                foreach (ulong id in affectedUsers.Keys)
                    if (LocationMap[id].SafeCount == 0)
                        LocationMap.Remove(id);
            });

            foreach (ulong id in affectedUsers.Keys)
                Core.RunInGuiThread(GuiUpdate, id);

        }

        void Core_MinuteTimer()
        {

            // prune op locs
            List<ulong> removeIDs = new List<ulong>();
            List<ushort> removeClients = new List<ushort>();

            LocationMap.LockReading(delegate()
            {
                foreach (ulong id in LocationMap.Keys)
                {
                    if (LocationMap.Count > PruneLocations &&
                        id != Core.UserID &&
                        !Core.Focused.SafeContainsKey(id) &&
                        !Core.Network.Routing.InCacheArea(id))
                        removeIDs.Add(id);
                }
            });

            if (removeIDs.Count > 0)
                LocationMap.LockWriting(delegate()
                {
                    while (removeIDs.Count > 0 && LocationMap.Count > PruneLocations / 2)
                    {
                        ulong furthest = Core.UserID;

                        foreach (ulong id in removeIDs)
                            if ((id ^ Core.UserID) > (furthest ^ Core.UserID))
                                furthest = id;

                        LocationMap.Remove(furthest);
                        Core.RunInGuiThread(GuiUpdate, furthest);
                        removeIDs.Remove(furthest);
                    }
                });

   
            // clean research map
            removeIDs.Clear();

            foreach (KeyValuePair<ulong, DateTime> pair in NextResearch)
                if (Core.TimeNow > pair.Value)
                    removeIDs.Add(pair.Key);

            if (removeIDs.Count > 0)
                foreach (ulong id in removeIDs)
                    NextResearch.Remove(id);
        }

        public List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong user, uint project)
        {
            return null;
        }

        internal void PublishGlobal()
        {
            // should be auto-set like a second after tcp connect
            // this isnt called until 15s after tcp connect
            if (Core.Context.LocalIP == null)
                return;

            // set next publish time 55 mins
            NextGlobalPublish = Core.TimeNow.AddMinutes(55);

            LocationData location = GetLocalLocation();


            // location packet is encrypted inside global loc packet
            // this embedded has OP TTL, while wrapper (CryptLoc) has global TTL

            byte[] data = SignedData.Encode(Core.Network.Protocol, Core.Profile.Settings.KeyPair, location);

            if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
                data = Utilities.EncryptBytes(data, Core.Network.OriginalCrypt.Key);

            data = new CryptLoc(LocationData.GLOBAL_TTL, data).Encode(Core.Network.Protocol);

            GlobalService service = (GlobalService) Core.Context.Global.ServiceMap[2];
            service.Publish(Core.Network.OpID, data);
        }

        internal void UpdateLocation()
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreBlocked(delegate() { UpdateLocation(); });
                return;
            }

            // do next update a minute before current update expires
            NextLocationUpdate = Core.TimeNow.AddMinutes(LocationData.OP_TTL - 1);

            LocationData location = GetLocalLocation();
            
            byte[] signed = SignedData.Encode(Core.Network.Protocol, Core.Profile.Settings.KeyPair, location);

            Debug.Assert(location.TTL < 5);
            Core.Network.Store.PublishNetwork(Core.UserID, ServiceID, 0, signed);

            OperationStore_Local(new DataReq(null, Core.UserID, ServiceID, 0, signed));
        }

        internal void StartSearch(ulong id, uint version)
        {
            byte[] parameters = BitConverter.GetBytes(version);

            DhtSearch search = Core.Network.Searches.Start(id, "Location", ServiceID, 0, parameters, new EndSearchHandler(EndSearch));

            if (search != null)
                search.TargetResults = 2;
        }

        internal void EndSearch(DhtSearch search)
        {
            foreach (SearchValue found in search.FoundValues)
            {
                DataReq store = new DataReq(found.Sources, search.TargetID, ServiceID, 0, found.Value);

                OperationStore_Local(store);
            }
        }

        

        void OperationSearch_Local(ulong key, byte[] parameters, List<byte[]> results)
        {
            uint minVersion = BitConverter.ToUInt32(parameters, 0);

            List<ClientInfo> clients = GetClients(key);

            foreach (ClientInfo info in clients)
                if (info.Data.Version >= minVersion)
                    results.Add(info.SignedData);
        }

        internal void OperationStore_Local(DataReq store)
        {
            // getting published to - search results - patch

            SignedData signed = SignedData.Decode(store.Data);

            if (signed == null)
                return;

            G2Header embedded = new G2Header(signed.Data);

            // figure out data contained
            if (G2Protocol.ReadPacket(embedded))
                if (embedded.Name == LocPacket.LocationData)
                {
                    LocationData location = LocationData.Decode(signed.Data);

                    if (Utilities.CheckSignedData(location.Key, signed.Data, signed.Signature))
                        Process_LocationData(store, signed, location);
                }
        }

        private void Process_LocationData(DataReq data, SignedData signed, LocationData location)
        {
            Core.IndexKey(location.UserID, ref location.Key);

            Debug.Assert(location.UserID == location.Source.UserID);
            if (location.UserID != location.Source.UserID)
                return;


            ClientInfo current = GetLocationInfo(location.UserID, location.Source.ClientID);
           
            // check location version
            if (current != null)
            {
                if (location.Version == current.Data.Version)
                    return;

                else if (location.Version < current.Data.Version)
                {
                    if (data != null && data.Sources != null)
                        foreach (DhtAddress source in data.Sources)
                            Core.Network.Store.Send_StoreReq(source, data.LocalProxy, new DataReq(null, current.Data.UserID, ServiceID, 0, current.SignedData));

                    return;
                }
            }

            
            // notify components of new versions (usually just localsync service signed up for this)
            DhtAddress address = new DhtAddress(location.IP, location.Source);

            foreach (PatchTag tag in location.Tags)
                if(TagReceived.Contains(tag.Service, tag.DataType))
                    TagReceived[tag.Service, tag.DataType].Invoke(address, location.UserID, tag.Tag);


            // add location
            if (current == null)
            {
                ThreadedDictionary<ushort, ClientInfo> locations = null;

                if (!LocationMap.SafeTryGetValue(location.UserID, out locations))
                {
                    locations = new ThreadedDictionary<ushort, ClientInfo>();
                    LocationMap.SafeAdd(location.UserID, locations);
                }

                // if too many clients, and not us, return
                if (location.UserID != Core.UserID && locations.SafeCount > MaxClientsperUser)
                    return;

                current = new ClientInfo(location.Source.ClientID);
                locations.SafeAdd(location.Source.ClientID, current);
            }

            current.Data = location;
            current.SignedData = signed.Encode(Core.Network.Protocol);

            if (current.Data.UserID == Core.UserID && current.Data.Source.ClientID == Core.Network.Local.ClientID)
                LocalLocation = current;

            current.TTL = location.TTL;

            
            // if open and not global, add to routing
            if (location.Source.Firewall == FirewallType.Open)
                Core.Network.Routing.Add(new DhtContact(location.Source, location.IP));

            // add global proxies (they would only be included in location packet if source was not directly connected to OP
            // even if open add the GP because pinging them will let host know of an open node on the network to connect to
            foreach(DhtAddress server in location.TunnelServers)
                Core.Network.Routing.Add(new DhtContact(location.Source, location.IP, location.TunnelClient, server));
    
            if (LocationUpdate != null)
                LocationUpdate.Invoke(current.Data);

            Core.RunInGuiThread(GuiUpdate, current.Data.UserID);
        }

        internal LocationData GetLocalLocation()
        {
            LocationData location = new LocationData();

            location.Key = Core.Profile.Settings.KeyPublic;
            location.IP = Core.Context.LocalIP;
            location.TTL = LocationData.OP_TTL;

            location.Source = Core.Network.GetLocalSource();

            if (Core.Network.UseGlobalProxies && Core.Context.Firewall != FirewallType.Open)
            {
                location.TunnelClient = new TunnelAddress(Core.Context.Global.Network.Local, Core.TunnelID);

                foreach (TcpConnect socket in Core.Context.Global.Network.TcpControl.ProxyServers)
                    location.TunnelServers.Add(new DhtAddress(socket.RemoteIP, socket));
            }

            foreach (TcpConnect socket in Core.Network.TcpControl.ProxyServers)
                location.Proxies.Add(new DhtAddress(socket.RemoteIP, socket));

            location.Place = Core.Profile.Settings.Location;
            location.GmtOffset = System.TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Minutes;
            location.Away = LocalAway;
            location.AwayMessage = LocalAway ? Core.Profile.Settings.AwayMessage : "";

            location.Version = LocationVersion++;

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

        internal ClientInfo GetLocationInfo(ulong user, ushort client)
        {
            ThreadedDictionary<ushort, ClientInfo> locations = null;

            if (LocationMap.SafeTryGetValue(user, out locations))
            {
                ClientInfo info = null;
                if (locations.SafeTryGetValue(client, out info))
                    return info;
            }

            return null;
        }

        internal string GetLocationName(ulong user, ushort client)
        {
            ClientInfo current = Core.Locations.GetLocationInfo(user, client);

            if(current == null)
                return client.ToString();

            LocationData data = current.Data;

            if (data == null || data.Place == null || data.Place == "")
                return data.IP.ToString();

            return data.Place;
        }

        internal void Research(ulong user)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { Research(user); });
                return;
            }

            if (!Core.Network.Responsive)
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
            ThreadedDictionary<ushort, ClientInfo> locations = null;

            if (LocationMap.SafeTryGetValue(user, out locations))
                locations.LockReading(delegate()
                {
                    foreach (ClientInfo location in locations.Values)
                        count++;
                });

            return count;
        }

        internal List<ClientInfo> GetClients(ulong user)
        {
            List<ClientInfo> results = new List<ClientInfo>();
            
            ThreadedDictionary<ushort, ClientInfo> clients = null;

            if(!LocationMap.SafeTryGetValue(user, out clients))
                return results;

            clients.LockReading(delegate()
            {
                foreach (ClientInfo info in clients.Values)
                    results.Add(info);
            });

            return results;
        }
    }

    internal class ClientInfo
    {
        internal LocationData Data;

        internal byte[] SignedData;

        internal uint TTL;
        internal ushort ClientID;

        internal ClientInfo(ushort id)
        {
            ClientID = id;
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
