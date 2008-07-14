using System;
using System.Collections.Generic;
using System.Text;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Transport;


namespace RiseOp.Services.Location
{
    class GlobalService : OpService 
    {
        public string Name { get { return "Global"; } }
        public uint ServiceID { get { return 2; } }

        OpCore Core;
        DhtNetwork Network;

        internal ThreadedDictionary<ulong, LinkedList<CryptLoc>> GlobalIndex = new ThreadedDictionary<ulong, LinkedList<CryptLoc>>();
        
        int PruneGlobalKeys = 64;
        int PruneGlobalEntries = 16;


        internal GlobalService(OpCore core)
        {
            Core = core;
            Network = core.Network;

            Network.Store.StoreEvent[ServiceID, 0] += new StoreHandler(GlobalStore_Local);
            Network.Store.ReplicateEvent[ServiceID, 0] += new ReplicateHandler(GlobalStore_Replicate);
            Network.Store.PatchEvent[ServiceID, 0] += new PatchHandler(GlobalStore_Patch);
            Network.Searches.SearchEvent[ServiceID, 0] += new SearchRequestHandler(GlobalSearch_Local);

            Core.SecondTimerEvent += new TimerHandler(Core_SecondTimer);
            Core.MinuteTimerEvent += new TimerHandler(Core_MinuteTimer);


            if (Core.Sim != null)
            {
                PruneGlobalKeys = 16;
                PruneGlobalEntries = 4;
            }
        }

        public void Dispose()
        {
            Core.SecondTimerEvent -= new TimerHandler(Core_SecondTimer);
            Core.SecondTimerEvent -= new TimerHandler(Core_MinuteTimer);

            Network.Store.StoreEvent[ServiceID, 0] -= new StoreHandler(GlobalStore_Local);
            Network.Store.ReplicateEvent[ServiceID, 0] -= new ReplicateHandler(GlobalStore_Replicate);
            Network.Store.PatchEvent[ServiceID, 0] -= new PatchHandler(GlobalStore_Patch);
            Network.Searches.SearchEvent[ServiceID, 0] -= new SearchRequestHandler(GlobalSearch_Local);
        }


        public List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong user, uint project)
        {
            return null;
        }

        void Core_SecondTimer()
        {
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
                        ulong furthest = Network.Local.UserID;

                        foreach (ulong id in GlobalIndex.Keys)
                            if ((id ^ Network.Local.UserID) > (furthest ^ Network.Local.UserID))
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
        }

        void Core_MinuteTimer()
        {
            // global ttl, once per minute
            List<ulong> removeIDs = new List<ulong>();

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
        }

        public void SimTest()
        {
        }

        public void SimCleanup()
        {
        }

        internal void Publish(ulong opID, byte[] data)
        {
            // called from operation thread

            Core.RunInCoreAsync(delegate()
            {
                Network.Store.PublishNetwork(opID, ServiceID, 0, data);

                GlobalStore_Local(new DataReq(null, opID, ServiceID, 0, data));
            });

        }

        void GlobalSearch_Local(ulong key, byte[] parameters, List<byte[]> results)
        {
            GlobalIndex.LockReading(delegate()
            {
                if (GlobalIndex.ContainsKey(key))
                    foreach (CryptLoc loc in GlobalIndex[key])
                        results.Add(loc.Encode(Network.Protocol));
            });
        }

        internal void GlobalStore_Local(DataReq crypt)
        {
            CryptLoc newLoc = CryptLoc.Decode(crypt.Data);

            if (newLoc == null)
                return;

            // check if data is for our operation, if it is use it
            Core.Context.Cores.LockReading(delegate()
            {
                foreach (OpCore opCore in Core.Context.Cores)
                    if (crypt.Target == opCore.Network.OpID)
                    {
                        DataReq store = new DataReq(null, opCore.Network.OpID, ServiceID, 0, newLoc.Data);

                        if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
                            store.Data = Utilities.DecryptBytes(store.Data, store.Data.Length, opCore.Network.OriginalCrypt.Key);

                        store.Sources = null; // dont pass global sources to operation store 

                        opCore.RunInCoreAsync(delegate()
                        {
                            opCore.Locations.OperationStore_Local(store);
                        });
                    }
            });

            // index location 
            LinkedList<CryptLoc> locations = null;

            if (GlobalIndex.SafeTryGetValue(crypt.Target, out locations))
            {
                foreach (CryptLoc location in locations)
                    if (Utilities.MemCompare(crypt.Data, location.Data))
                    {
                        if (newLoc.TTL > location.TTL)
                            location.TTL = newLoc.TTL;

                        return;
                    }
            }
            else
            {
                locations = new LinkedList<CryptLoc>();
                GlobalIndex.SafeAdd(crypt.Target, locations);
            }

            locations.AddFirst(newLoc);
        }

        List<byte[]> GlobalStore_Replicate(DhtContact contact)
        {
            //crit
            // just send little piece of first 8 bytes, if remote doesnt have it, it is requested through params with those 8 bytes

            return null;
        }

        void GlobalStore_Patch(DhtAddress source, byte[] data)
        {

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

                GlobalStore_Local(store);
            }
        }
    }
}
