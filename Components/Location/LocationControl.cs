using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Transport;
using DeOps.Components.Link;


namespace DeOps.Components.Location
{
    internal delegate void LocationUpdateHandler(LocationData location);
    internal delegate void LocationGuiUpdateHandler(ulong key);

    
    internal class LocationControl : OpComponent
    {
        OpCore Core;

        internal uint LocationVersion = 1;
        internal DateTime NextLocationUpdate;
        internal DateTime NextGlobalPublish;

        internal LocInfo LocalLocation;
        internal ThreadedDictionary<ulong, LinkedList<CryptLoc>> GlobalIndex = new ThreadedDictionary<ulong, LinkedList<CryptLoc>>();
        internal ThreadedDictionary<ulong, ThreadedDictionary<ushort, LocInfo>> LocationMap = new ThreadedDictionary<ulong, ThreadedDictionary<ushort, LocInfo>>();

        Dictionary<ulong, DateTime> NextResearch = new Dictionary<ulong, DateTime>();

        internal LocationUpdateHandler LocationUpdate;
        internal LocationGuiUpdateHandler GuiUpdate;

        int PruneGlobalKeys    = 100;
        int PruneGlobalEntries = 20;
       // int PruneLocations     = 100;


        internal LocationControl(OpCore core)
        {
            Core = core;
            Core.Locations = this;

            Core.TimerEvent += new TimerHandler(Core_Timer);
            
            Core.GlobalNet.Store.StoreEvent[ComponentID.Location] = new StoreHandler(GlobalStore_Local);
            Core.GlobalNet.Store.ReplicateEvent[ComponentID.Location] = new ReplicateHandler(GlobalStore_Replicate);
            Core.GlobalNet.Store.PatchEvent[ComponentID.Location] = new PatchHandler(GlobalStore_Patch);
            Core.GlobalNet.Searches.SearchEvent[ComponentID.Location] = new SearchRequestHandler(GlobalSearch_Local);

            Core.OperationNet.Store.StoreEvent[ComponentID.Location] = new StoreHandler(OperationStore_Local);
            Core.OperationNet.Searches.SearchEvent[ComponentID.Location] = new SearchRequestHandler(OperationSearch_Local);

            // should be published auto anyways, on bootstrap, or firewall/proxy update
            NextGlobalPublish = Core.TimeNow.AddMinutes(1);

            if (Core.Sim != null)
            {
                PruneGlobalKeys    = 50;
                PruneGlobalEntries = 10;
                //PruneLocations     = 25;
            }
        }

        void Core_Timer()
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

            //crit op locs need pruning?
            /* prune op locations
            if (LocationMap.Count > PruneLocations)
            {
                List<ulong> removeLocs = new List<ulong>();

                foreach (ulong id in LocationMap.Keys)
                    if (!Core.Links.LinkMap.ContainsKey(id) &&
                        !Utilities.InBounds(Core.LocalDhtID, Core.OperationNet.Store.MaxDistance, id)) //crit update later to dhtbounds
                        removeLocs.Add(id);


                while (removeLocs.Count > 0 && LocationMap.Count > PruneLocations / 2)
                {
                    ulong furthest = Core.LocalDhtID;

                    foreach (ulong id in removeLocs)
                        if ((id ^ Core.LocalDhtID) > (furthest ^ Core.LocalDhtID))
                            furthest = id;

                    if (furthest != Core.LocalDhtID)
                    {
                        LocationMap.Remove(furthest);
                        Core.InvokeInterface(GuiUpdate, furthest);
                        removeLocs.Remove(furthest);
                    }
                }
            }*/

            // global ttl, once per minute
            if (second == 60)
            {

                List<ulong> removeKeys = new List<ulong>();

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
                            removeKeys.Add(key);
                    }


                });

                GlobalIndex.LockWriting(delegate()
                 {
                     foreach (ulong key in removeKeys)
                         GlobalIndex.Remove(key);
                 });
            }

            // operation ttl (similar as above, but not the same)
            if (second % 15 == 0)
            {
                List<ulong> removeKeys = new List<ulong>();

                LocationMap.LockReading(delegate()
                {
                     List<ushort> removeClients = new List<ushort>();

                     foreach (ulong key in LocationMap.Keys)
                     {
                         removeClients.Clear();

                         ThreadedDictionary<ushort, LocInfo> clients = null;
                         LocationMap.SafeTryGetValue(key, out clients);

                         if (clients != null)
                             clients.LockReading(delegate()
                             {
                                 foreach (ushort id in clients.Keys)
                                 {
                                     if (second == 60)
                                     {
                                         if (clients[id].TTL > 0)
                                             clients[id].TTL--;

                                         if (clients[id].TTL == 0)
                                             removeClients.Add(id);
                                     }

                                     //crit hack - last 30 and 15 secs before loc destroyed do searches (working pretty good through...)
                                     if (clients[id].TTL == 1 &&
                                         (second == 15 || second == 30))
                                         if (Core.Links.LinkMap.SafeContainsKey(key))
                                             StartSearch(key, 0, false);
                                 }
                             });

                         foreach (ushort id in removeClients)
                             clients.SafeRemove(id);

                         if (clients.SafeCount == 0)
                             removeKeys.Add(key);
                     }
                });

                foreach (ulong key in removeKeys)
                 {
                     LocationMap.SafeRemove(key);

                     Core.RunInGuiThread(GuiUpdate, key);
                 }
            }

            // clean research map
            if (second == 60)
            {
                List<ulong> removeIDs = new List<ulong>();

                foreach (KeyValuePair<ulong, DateTime> pair in NextResearch)
                    if (Core.TimeNow > pair.Value)
                        removeIDs.Add(pair.Key);

                if (removeIDs.Count > 0)
                    foreach (ulong id in removeIDs)
                        NextResearch.Remove(id);
            }
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
            location.Away = Core.Away;
            location.AwayMessage = Core.Away ? Core.User.Settings.AwayMessage : "";

            location.Version  = LocationVersion++;
            if( Core.Profiles != null)
                location.ProfileVersion = Core.Profiles.LocalProfile.Header.Version;
            location.LinkVersion = Core.Links.LocalLink.Header.Version;


            byte[] signed = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, location);

            Core.OperationNet.Store.PublishNetwork(Core.LocalDhtID, ComponentID.Location, signed);

            OperationStore_Local(new DataReq(null, Core.LocalDhtID, ComponentID.Location, signed));
        }

        internal void StartSearch(ulong id, uint version, bool global)
        {
            DhtNetwork network = global ? Core.GlobalNet : Core.OperationNet;

            byte[] parameters = BitConverter.GetBytes(version); 

            DhtSearch search = network.Searches.Start(id, "Location", ComponentID.Location, parameters, new EndSearchHandler(EndSearch));

            if (search != null)
                search.TargetResults = 2;
        }

        internal void EndSearch(DhtSearch search)
        {
            foreach (SearchValue found in search.FoundValues)
            {
                DataReq location = new DataReq(found.Sources, search.TargetID, ComponentID.Location, found.Value);

                if (search.Network == Core.GlobalNet)
                {
                    if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
                        location.Data = Utilities.DecryptBytes(location.Data, location.Data.Length, Core.OperationNet.OriginalCrypt);

                    location.Sources = null; // dont pass global sources to operation store 
                }

                OperationStore_Local(location);
            }
        }

        List<byte[]> GlobalSearch_Local(ulong key, byte[] parameters)
        {
            List<Byte[]> results = new List<byte[]>();

            GlobalIndex.LockReading(delegate()
            {
                if (GlobalIndex.ContainsKey(key))
                    foreach (CryptLoc loc in GlobalIndex[key])
                        results.Add(loc.Data);
            });

            return results;
        }

        List<byte[]> OperationSearch_Local(ulong key, byte[] parameters)
        {
            List<Byte[]> results = new List<byte[]>();

            uint minVersion = BitConverter.ToUInt32(parameters, 0);

            List<LocInfo> clients = GetClients(key);

            foreach (LocInfo info in clients)
                if (!info.Location.Global && info.Location.Version >= minVersion)
                    results.Add(info.SignedData);

            return results;
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

        void OperationStore_Local(DataReq data)
        {
            SignedData signed = SignedData.Decode(Core.Protocol, data.Data);
            LocationData location = LocationData.Decode(Core.Protocol, signed.Data);

            Core.IndexKey(location.KeyID, ref location.Key);

            if (!Utilities.CheckSignedData(location.Key, signed.Data, signed.Signature))
                return;

            LocInfo current = GetLocationInfo(location.KeyID, location.Source.ClientID);

            // check location version
            if (current != null)
            {
                if (location.Version == current.Location.Version)
                    return;

                else if (location.Version < current.Location.Version)
                {
                    if (data != null && data.Sources != null)
                        foreach (DhtAddress source in data.Sources)
                            Core.OperationNet.Store.Send_StoreReq(source, data.LocalProxy, new DataReq(null, current.Location.KeyID, ComponentID.Location, current.SignedData));

                    return;
                }
            }

            // version checks
            if(Core.Profiles != null)
                Core.Profiles.CheckVersion(location.KeyID, location.ProfileVersion);

            Core.Links.CheckVersion(location.KeyID, location.LinkVersion);


            // add location
            if (current == null)
            {
                ThreadedDictionary<ushort, LocInfo> locations = null;

                if (!LocationMap.SafeTryGetValue(location.KeyID, out locations))
                {
                    locations = new ThreadedDictionary<ushort, LocInfo>();
                    LocationMap.SafeAdd(location.KeyID, locations);
                }

                current = new LocInfo(location.Source.ClientID);
                current.Cached = Core.OperationNet.Store.IsCached(location.KeyID);
                locations.SafeAdd(location.Source.ClientID, current);
            }

            current.Location   = location;
            current.SignedData = data.Data;
            current.TTL        = location.TTL;

            if (current.Location.KeyID == Core.LocalDhtID && current.Location.Source.ClientID == Core.ClientID)
                LocalLocation = current;

            // if open and not global, add to routing
            if (location.Source.Firewall == FirewallType.Open && !location.Global)
                Core.OperationNet.Routing.Add(new DhtContact(location.Source, location.IP, Core.TimeNow));

            if(LocationUpdate != null)
                LocationUpdate.Invoke(current.Location);
            
            Core.RunInGuiThread(GuiUpdate, current.Location.KeyID);
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
            location.LinkVersion = Core.Links.LocalLink.Header.Version;

            if(Core.Profiles != null)
                location.ProfileVersion = Core.Profiles.LocalProfile.Header.Version;

            location.TTL = LocationData.GLOBAL_TTL; // set expire 1 hour

            byte[] data = SignedData.Encode(Core.Protocol, Core.User.Settings.KeyPair, location);

            if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
                data = Utilities.EncryptBytes(data, Core.OperationNet.OriginalCrypt);

            data = new CryptLoc(60 * 60, data).Encode(Core.Protocol);

            Core.GlobalNet.Store.PublishNetwork(Core.OpID, ComponentID.Location, data);

            GlobalStore_Local(new DataReq(null, Core.OpID, ComponentID.Location, data));
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

        internal LocInfo GetLocationInfo(ulong key, ushort client)
        {
            ThreadedDictionary<ushort, LocInfo> locations = null;

            if (LocationMap.SafeTryGetValue(key, out locations))
            {
                LocInfo info = null;
                if (locations.SafeTryGetValue(client, out info))
                    return info;
            }

            return null;
        }

        internal string GetLocationName(ulong key, ushort id)
        {
            LocInfo current = Core.Locations.GetLocationInfo(key, id);

            if(current == null)
                return id.ToString();

            LocationData data = current.Location;

            if (data == null || data.Place == null || data.Place == "")
                return data.IP.ToString();

            return data.Place;
        }

        internal void Research(ulong key)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { Research(key); });
                return;
            }

            if (!Core.OperationNet.Routing.Responsive())
                return;

            // limit re-search to once per 30 secs
            DateTime timeout = default(DateTime);

            if (NextResearch.TryGetValue(key, out timeout))
                if (Core.TimeNow < timeout)
                    return;

            StartSearch(key, 0, false);

            NextResearch[key] = Core.TimeNow.AddSeconds(30);
        }

        internal int ClientCount(ulong id)
        {
            ThreadedDictionary<ushort, LocInfo> locations = null;

            if (LocationMap.SafeTryGetValue(id, out locations))
                return locations.SafeCount;

            return 0;
        }

        internal List<LocInfo> GetClients(ulong id)
        {
            List<LocInfo> results = new List<LocInfo>();
            
            ThreadedDictionary<ushort, LocInfo> clients = null;

            if(!LocationMap.SafeTryGetValue(id, out clients))
                return results;

            clients.LockReading(delegate()
            {
                foreach (LocInfo info in clients.Values)
                    results.Add(info);
            });

            return results;
        }
    }

    internal class LocInfo
    {
        internal LocationData Location;

        internal byte[] SignedData;

        internal uint TTL;
        internal bool Cached;
        internal ushort ClientID;

        internal LocInfo(ushort id)
        {
            ClientID = id;
        }
    }
}
