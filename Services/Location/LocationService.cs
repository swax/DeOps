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

    internal delegate byte[] GetTagHandler();
    internal delegate void TagReceivedHandler(ulong user, byte[] tag);


    internal class LocationService : OpService
    {
        public string Name { get { return "Location"; } }
        public ushort ServiceID { get { return 2; } }

        OpCore Core;

        internal uint LocationVersion = 1;
        internal DateTime NextLocationUpdate;
        internal DateTime NextGlobalPublish;

        bool Loading = true;
        bool RunSaveLocs;
        RijndaelManaged LocalKey;
        string LocationPath;

        internal ClientInfo LocalLocation;
        internal ThreadedDictionary<ulong, LinkedList<CryptLoc>> GlobalIndex = new ThreadedDictionary<ulong, LinkedList<CryptLoc>>();
        internal ThreadedDictionary<ulong, ThreadedDictionary<ushort, ClientInfo>> LocationMap = new ThreadedDictionary<ulong, ThreadedDictionary<ushort, ClientInfo>>();

        Dictionary<ulong, DateTime> NextResearch = new Dictionary<ulong, DateTime>();

        internal LocationUpdateHandler LocationUpdate;
        internal LocationGuiUpdateHandler GuiUpdate;

        internal ServiceEvent<GetTagHandler> GetTag = new ServiceEvent<GetTagHandler>();
        internal ServiceEvent<TagReceivedHandler> TagReceived = new ServiceEvent<TagReceivedHandler>();


        int PruneGlobalKeys    = 100;
        int PruneGlobalEntries = 20;
        int PruneLocations = 100;

        internal bool LocalAway;


        internal LocationService(OpCore core)
        {
            Core = core;
            Core.Locations = this;

            Core.SecondTimerEvent += new TimerHandler(Core_SecondTimer);
            Core.MinuteTimerEvent += new TimerHandler(Core_MinuteTimer);

            Core.GlobalNet.Store.StoreEvent[ServiceID, 0] += new StoreHandler(GlobalStore_Local);
            Core.GlobalNet.Store.ReplicateEvent[ServiceID, 0] += new ReplicateHandler(GlobalStore_Replicate);
            Core.GlobalNet.Store.PatchEvent[ServiceID, 0] += new PatchHandler(GlobalStore_Patch);
            Core.GlobalNet.Searches.SearchEvent[ServiceID, 0] += new SearchRequestHandler(GlobalSearch_Local);

            Core.OperationNet.Store.StoreEvent[ServiceID, 0] += new StoreHandler(OperationStore_Local);
            Core.OperationNet.Searches.SearchEvent[ServiceID, 0] += new SearchRequestHandler(OperationSearch_Local);

            // should be published auto anyways, on bootstrap, or firewall/proxy update
            NextGlobalPublish = Core.TimeNow.AddMinutes(1);

            if (Core.Sim != null)
            {
                PruneGlobalKeys    = 50;
                PruneGlobalEntries = 10;
                PruneLocations = 25;
            }

            LocalKey = Core.User.Settings.FileKey;

            LocationPath = Core.User.RootPath + Path.DirectorySeparatorChar +
                        "Data" + Path.DirectorySeparatorChar +
                        ServiceID.ToString();

            Directory.CreateDirectory(LocationPath);

            LoadLocations();
            Loading = false;
        }

       

        public void Dispose()
        {
            Core.SecondTimerEvent -= new TimerHandler(Core_SecondTimer);
            Core.SecondTimerEvent -= new TimerHandler(Core_MinuteTimer);

            Core.GlobalNet.Store.StoreEvent[ServiceID, 0] -= new StoreHandler(GlobalStore_Local);
            Core.GlobalNet.Store.ReplicateEvent[ServiceID, 0] -= new ReplicateHandler(GlobalStore_Replicate);
            Core.GlobalNet.Store.PatchEvent[ServiceID, 0] -= new PatchHandler(GlobalStore_Patch);
            Core.GlobalNet.Searches.SearchEvent[ServiceID, 0] -= new SearchRequestHandler(GlobalSearch_Local);

            Core.OperationNet.Store.StoreEvent[ServiceID, 0] -= new StoreHandler(OperationStore_Local);
            Core.OperationNet.Searches.SearchEvent[ServiceID, 0] -= new SearchRequestHandler(OperationSearch_Local);

        }

        private void LoadLocations()
        {
            try
            {
                string path = LocationPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalKey, "LocHeaders");

                if (!File.Exists(path))
                    return;

                FileStream file = new FileStream(path, FileMode.Open);
                CryptoStream crypto = new CryptoStream(file, LocalKey.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Core.Protocol, FileAccess.Read);

                G2Header root = null;

                while (stream.ReadPacket(ref root))
                    if (root.Name == DataPacket.SignedData)
                    {
                        SignedData signed = SignedData.Decode(Core.Protocol, root);
                        G2Header embedded = new G2Header(signed.Data);

                        // figure out data contained
                        if (Core.Protocol.ReadPacket(embedded))
                            if (embedded.Name == LocPacket.LocationData)
                                Process_LocationData(null, signed, LocationData.Decode(Core.Protocol, signed.Data));
                    }

                stream.Close();
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Locations", "Error loading data " + ex.Message);
            }
        }

        private void SaveLocations()
        {
            try
            {
                string tempPath = Core.GetTempPath();
                FileStream file = new FileStream(tempPath, FileMode.Create);
                CryptoStream stream = new CryptoStream(file, LocalKey.CreateEncryptor(), CryptoStreamMode.Write);

                LocationMap.LockReading(delegate()
                {
                    foreach (ThreadedDictionary<ushort, ClientInfo> map in LocationMap.Values)
                        map.LockReading(delegate()
                        {
                            foreach(ClientInfo loc in map.Values)
                                stream.Write(loc.SignedData, 0, loc.SignedData.Length);
                        });
                });

                stream.FlushFinalBlock();
                stream.Close();


                string finalPath = LocationPath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalKey, "LocHeaders");
                File.Delete(finalPath);
                File.Move(tempPath, finalPath);
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Locations", "Error saving data " + ex.Message);
            }
        }


        void Core_SecondTimer()
        {
            // global publish
            if (Core.GlobalNet != null && Core.TimeNow > NextGlobalPublish)
                PublishGlobal();

            // operation publish
            if (Core.OperationNet.Routing.Responsive() && Core.TimeNow > NextLocationUpdate)
                UpdateLocation();

            // run code below every quarter second
            int second = Core.TimeNow.Second + 1; // get 1 - 60 value
            if (second % 15 != 0)
                return;

            // prune global keys
            if (GlobalIndex.SafeCount > PruneGlobalKeys)
                GlobalIndex.LockWriting(delegate()
                {
                    while (GlobalIndex.Count > PruneGlobalKeys / 2)
                    {
                        ulong furthest = Core.LocalDhtID;

                        foreach (ulong id in GlobalIndex.Keys)
                            if ((id ^ Core.LocalDhtID) > (furthest ^ Core.LocalDhtID))
                                furthest = id;

                        GlobalIndex.Remove(furthest);
                    }
                });

            // prune global entries
            GlobalIndex.LockReading(delegate()
            {
                foreach (LinkedList<CryptLoc> list in GlobalIndex.Values)
                    if (list.Count > PruneGlobalEntries)
                        while (list.Count > PruneGlobalEntries / 2)
                            list.RemoveLast();
            });

            
       
            // operation ttl (similar as above, but not the same)
            if (second % 15 == 0)
            {
                List<ulong> inactivated = new List<ulong>();

                LocationMap.LockReading(delegate()
                {
                     foreach (ThreadedDictionary<ushort, ClientInfo> clients in LocationMap.Values)
                     {
                         clients.LockReading(delegate()
                         {
                             foreach (ClientInfo location in clients.Values)
                                 if(location.Active)
                                 {
                                     if (second == 60)
                                     {
                                         if (location.TTL > 0)
                                             location.TTL--;

                                         if (location.TTL == 0)
                                         {
                                             location.Active = false;
                                             inactivated.Add(location.Data.KeyID);
                                         }
                                     }

                                     //crit hack - last 30 and 15 secs before loc destroyed do searches (working pretty good through...)
                                     if (location.TTL == 1 && (second == 15 || second == 30))
                                         StartSearch(location.Data.KeyID, 0, false);
                                 }
                         });
                     }
                });

                foreach (ulong id in inactivated)
                    Core.RunInGuiThread(GuiUpdate, id);
            }
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
                        id != Core.LocalDhtID &&
                        !Core.Focused.SafeContainsKey(id) &&
                        !Utilities.InBounds(id, Core.OperationNet.Store.RecalcBounds(id), Core.LocalDhtID)) //crit update later to dhtbounds
                        removeIDs.Add(id);

                    // if more than one location and location hasnt been seen for a day - remove
                    ThreadedDictionary<ushort, ClientInfo> clients;
                    if(LocationMap.TryGetValue(id, out clients))
                        if (clients.SafeCount > 1)
                        {
                            removeClients.Clear();

                            clients.LockReading(delegate()
                            {
                                foreach (ClientInfo location in clients.Values)
                                    if (location.Data.Date < Core.TimeNow.ToUniversalTime().AddDays(-1))
                                        removeClients.Add(location.ClientID);
                            });

                            foreach (ushort client in removeClients)
                                clients.SafeRemove(client);
                        }
                }
            });

            if (removeIDs.Count > 0)
                LocationMap.LockWriting(delegate()
                {
                    while (removeIDs.Count > 0 && LocationMap.Count > PruneLocations / 2)
                    {
                        ulong furthest = Core.LocalDhtID;

                        foreach (ulong id in removeIDs)
                            if ((id ^ Core.LocalDhtID) > (furthest ^ Core.LocalDhtID))
                                furthest = id;

                        LocationMap.Remove(furthest);
                        Core.RunInGuiThread(GuiUpdate, furthest);
                        removeIDs.Remove(furthest);
                    }
                });

            
            if (RunSaveLocs)
            {
                SaveLocations();
                RunSaveLocs = false;
            }


            // global ttl, once per minute
            removeIDs.Clear();

            GlobalIndex.LockReading(delegate()
            {
                List<CryptLoc> removeList = new List<CryptLoc>();

                foreach (ulong key in GlobalIndex.Keys)
                {
                    removeList.Clear();

                    foreach (CryptLoc loc in GlobalIndex[key])
                    {
                        if (loc.TTL > 0)
                            loc.TTL--;

                        if (loc.TTL == 0)
                            removeList.Add(loc);
                    }

                    foreach (CryptLoc loc in removeList)
                        GlobalIndex[key].Remove(loc);

                    if (GlobalIndex[key].Count == 0)
                        removeIDs.Add(key);
                }


            });

            GlobalIndex.LockWriting(delegate()
            {
                foreach (ulong key in removeIDs)
                    GlobalIndex.Remove(key);
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

        internal void UpdateLocation()
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreBlocked(delegate() { UpdateLocation(); });
                return;
            }

            // do next update a minute before current update expires
            NextLocationUpdate = Core.TimeNow.AddMinutes(LocationData.OP_TTL - 1);

            LocationData location = new LocationData();
            location.Key    = Core.User.Settings.KeyPublic;
            location.IP     = Core.LocalIP;
            location.Source = Core.OperationNet.GetLocalSource();
            location.TTL    = LocationData.OP_TTL;
            
            foreach (TcpConnect connect in Core.OperationNet.TcpControl.Connections)
                if (connect.Proxy == ProxyType.Server)
                    location.Proxies.Add(new DhtAddress(connect.RemoteIP, connect));

            location.Place = Core.User.Settings.Location;
            location.GmtOffset = System.TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Minutes;
            location.Away = LocalAway;
            location.AwayMessage = LocalAway ? Core.User.Settings.AwayMessage : "";
            location.Date = Core.TimeNow.ToUniversalTime(); 

            location.Version  = LocationVersion++;

            foreach(ushort service in GetTag.HandlerMap.Keys)
                foreach (ushort datatype in GetTag.HandlerMap[service].Keys)
                {
                    LocationTag tag = new LocationTag();

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

            byte[] signed = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, location);

            Core.OperationNet.Store.PublishNetwork(Core.LocalDhtID, ServiceID, 0, signed);

            OperationStore_Local(new DataReq(null, Core.LocalDhtID, ServiceID, 0, signed));
        }

        internal void StartSearch(ulong id, uint version, bool global)
        {
            DhtNetwork network = global ? Core.GlobalNet : Core.OperationNet;

            byte[] parameters = BitConverter.GetBytes(version);

            DhtSearch search = network.Searches.Start(id, "Location", ServiceID, 0, parameters, new EndSearchHandler(EndSearch));

            if (search != null)
                search.TargetResults = 2;
        }

        internal void EndSearch(DhtSearch search)
        {
            foreach (SearchValue found in search.FoundValues)
            {
                DataReq store = new DataReq(found.Sources, search.TargetID, ServiceID, 0, found.Value);

                if (search.Network == Core.GlobalNet)
                {
                    if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
                        store.Data = Utilities.DecryptBytes(store.Data, store.Data.Length, Core.OperationNet.OriginalCrypt);

                    store.Sources = null; // dont pass global sources to operation store 
                }

                OperationStore_Local(store);
            }
        }

        void GlobalSearch_Local(ulong key, byte[] parameters, List<byte[]> results)
        {
            GlobalIndex.LockReading(delegate()
            {
                if (GlobalIndex.ContainsKey(key))
                    foreach (CryptLoc loc in GlobalIndex[key])
                        results.Add(loc.Data);
            });
        }

        void OperationSearch_Local(ulong key, byte[] parameters, List<byte[]> results)
        {
            uint minVersion = BitConverter.ToUInt32(parameters, 0);

            List<ClientInfo> clients = GetClients(key);

            foreach (ClientInfo info in clients)
                if (info.Data.Version >= minVersion)
                    results.Add(info.SignedData);
        }

        void GlobalStore_Local(DataReq location)
        {
            CryptLoc newLoc = CryptLoc.Decode(Core.Protocol, location.Data);

            if (newLoc == null)
                return;

            // check for duplicates

            LinkedList<CryptLoc> locs = null;

            if (GlobalIndex.SafeTryGetValue(location.Target, out locs))
            {
                foreach (CryptLoc loc in locs)
                       if (Utilities.MemCompare(location.Data, loc.Data))
                       {
                           if (newLoc.TTL > loc.TTL)
                               loc.TTL = newLoc.TTL;

                           return;
                       }
            }
            else
            {
                locs = new LinkedList<CryptLoc>();
                GlobalIndex.SafeAdd(location.Target, locs);
            }

            locs.AddFirst(newLoc);
        }

        void OperationStore_Local(DataReq store)
        {
            // getting published to - search results - patch

            SignedData signed = SignedData.Decode(Core.Protocol, store.Data);

            if (signed == null)
                return;

            G2Header embedded = new G2Header(signed.Data);

            // figure out data contained
            if (Core.Protocol.ReadPacket(embedded))
                if (embedded.Name == LocPacket.LocationData)
                {
                    LocationData location = LocationData.Decode(Core.Protocol, signed.Data);

                    if (Utilities.CheckSignedData(location.Key, signed.Data, signed.Signature))
                        Process_LocationData(store, signed, location);
                }
        }

        private void Process_LocationData(DataReq data, SignedData signed, LocationData location)
        {
            Core.IndexKey(location.KeyID, ref location.Key);

            ClientInfo current = GetLocationInfo(location.KeyID, location.Source.ClientID);

            // check location version
            if (current != null)
            {
                if (location.Version == current.Data.Version)
                    return;

                else if (location.Version < current.Data.Version)
                {
                    if (data != null && data.Sources != null)
                        foreach (DhtAddress source in data.Sources)
                            Core.OperationNet.Store.Send_StoreReq(source, data.LocalProxy, new DataReq(null, current.Data.KeyID, ServiceID, 0, current.SignedData));

                    return;
                }
            }


            // notify components of new versions
            foreach (LocationTag tag in location.Tags)
                if(TagReceived.Contains(tag.Service, tag.DataType))
                    TagReceived[tag.Service, tag.DataType].Invoke(location.KeyID, tag.Tag);


            // add location
            if (current == null)
            {
                ThreadedDictionary<ushort, ClientInfo> locations = null;

                if (!LocationMap.SafeTryGetValue(location.KeyID, out locations))
                {
                    locations = new ThreadedDictionary<ushort, ClientInfo>();
                    LocationMap.SafeAdd(location.KeyID, locations);
                }

                current = new ClientInfo(location.Source.ClientID);
                locations.SafeAdd(location.Source.ClientID, current);
            }

            current.Data = location;
            current.SignedData = signed.Encode(Core.Protocol);

            if (current.Data.KeyID == Core.LocalDhtID && current.Data.Source.ClientID == Core.ClientID)
                LocalLocation = current;

            if (Loading) // return if this operation came from a file, not the network
            {
                Core.OperationNet.AddCacheEntry(new IPCacheEntry(new DhtContact(location.Source, location.IP, location.Date)));
                return;
            }

            current.TTL = location.TTL;
            current.Active = true;

            RunSaveLocs = true;
            
            // if open and not global, add to routing
            if (location.Source.Firewall == FirewallType.Open && !location.Global)
                Core.OperationNet.Routing.Add(new DhtContact(location.Source, location.IP, Core.TimeNow));

            if (LocationUpdate != null)
                LocationUpdate.Invoke(current.Data);

            Core.RunInGuiThread(GuiUpdate, current.Data.KeyID);
        }

        internal void PublishGlobal()
        {
            if (Core.LocalIP == null)
                return;

            // run when global proxy changes
            // run when firewall mode changes to open

            // only publish to global if operation routing is low, or we are open
            if (Core.OperationNet.Routing.BucketList.Count > 1 && Core.Firewall != FirewallType.Open)
                return;

            // set next publish time 55 mins
            NextGlobalPublish = Core.TimeNow.AddMinutes(55);


            LocationData location = new LocationData();
            location.Key = Core.User.Settings.KeyPublic;
            location.IP = Core.LocalIP;

            // if we're open publish our operation contact info on global
            if (Core.Firewall == FirewallType.Open)
            {
                location.Source = Core.OperationNet.GetLocalSource();
            }

            // else if operation network small and we're not open, publish proxy info on global
            else if (Core.OperationNet.Routing.BucketList.Count == 1)
            {
                location.Global = true;
                location.Source = Core.GlobalNet.GetLocalSource();

                foreach (TcpConnect connect in Core.GlobalNet.TcpControl.Connections)
                    if (connect.Proxy == ProxyType.Server)
                        location.Proxies.Add(new DhtAddress(connect.RemoteIP, connect));
            }

            // else no need to publish ourselves on global
            else
                return;

            location.Place = Core.User.Settings.Location;
            location.Version = LocationVersion++;
            location.TTL = LocationData.GLOBAL_TTL; // set expire 1 hour

            byte[] data = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, location);

            if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
                data = Utilities.EncryptBytes(data, Core.OperationNet.OriginalCrypt);

            data = new CryptLoc(60 * 60, data).Encode(Core.Protocol);

            Core.GlobalNet.Store.PublishNetwork(Core.OpID, ServiceID, 0, data);

            GlobalStore_Local(new DataReq(null, Core.OpID, ServiceID, 0, data));
        }


        ReplicateData GlobalStore_Replicate(DhtContact contact, bool add)
        {
            //crit
            // just send little piece of first 8 bytes, if remote doesnt have it, it is requested through params with those 8 bytes

            return null;
        }

        void GlobalStore_Patch(DhtAddress source, ulong distance, byte[] data)
        {

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

            if (!Core.OperationNet.Routing.Responsive())
                return;

            // limit re-search to once per 30 secs
            DateTime timeout = default(DateTime);

            if (NextResearch.TryGetValue(user, out timeout))
                if (Core.TimeNow < timeout)
                    return;

            StartSearch(user, 0, false);

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
                        if (location.Active)
                            count++;
                });
            return count;
        }

        internal List<ClientInfo> GetClients(ulong user)
        {
            //crit needs to change when global proxying implemented

            List<ClientInfo> results = new List<ClientInfo>();
            
            ThreadedDictionary<ushort, ClientInfo> clients = null;

            if(!LocationMap.SafeTryGetValue(user, out clients))
                return results;

            clients.LockReading(delegate()
            {
                foreach (ClientInfo info in clients.Values)
                    if (!info.Data.Global)
                        results.Add(info);
            });

            return results;
        }
    }

    internal class ClientInfo
    {
        internal LocationData Data;

        internal byte[] SignedData;

        internal bool Active;
        internal uint TTL;
        internal ushort ClientID;

        internal ClientInfo(ushort id)
        {
            ClientID = id;
        }
    }


    internal class LocationTag
    {
        internal ushort Service;
        internal ushort DataType;
        internal byte[] Tag;

        internal byte[] ToBytes()
        {
            byte[] data = new byte[2 + 2 + Tag.Length];

            BitConverter.GetBytes(Service).CopyTo(data, 0);
            BitConverter.GetBytes(DataType).CopyTo(data, 2);
            Tag.CopyTo(data, 4);

            return data;
        }

        internal static LocationTag FromBytes(byte[] data, int pos, int size)
        {
            LocationTag tag = new LocationTag();

            tag.Service = BitConverter.ToUInt16(data, pos);
            tag.DataType = BitConverter.ToUInt16(data, pos + 2);
            tag.Tag = Utilities.ExtractBytes(data, pos + 4, size - 4);

            return tag;
        }
    }
}
