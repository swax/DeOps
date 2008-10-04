using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Transport;


namespace RiseOp.Services.Location
{
    class LookupService : OpService 
    {
        public string Name { get { return "Lookup"; } }
        public uint ServiceID { get { return 12; } }

        OpCore Core;
        DhtNetwork Network;

        internal ThreadedDictionary<ulong, LinkedList<CryptLoc>> LookupIndex = new ThreadedDictionary<ulong, LinkedList<CryptLoc>>();
        
        int PruneLookupKeys = 64;
        int PruneLookupEntries = 16;


        internal LookupService(OpCore core)
        {
            Core = core;
            Network = core.Network;

            Network.Store.StoreEvent[ServiceID, 0] += new StoreHandler(Store_Local);
            Network.Store.ReplicateEvent[ServiceID, 0] += new ReplicateHandler(Store_Replicate);
            Network.Store.PatchEvent[ServiceID, 0] += new PatchHandler(Store_Patch);
            Network.Searches.SearchEvent[ServiceID, 0] += new SearchRequestHandler(Search_Local);

            Core.SecondTimerEvent += new TimerHandler(Core_SecondTimer);
            Core.MinuteTimerEvent += new TimerHandler(Core_MinuteTimer);


            if (Core.Sim != null)
            {
                PruneLookupKeys = 16;
                PruneLookupEntries = 4;
            }
        }

        public void Dispose()
        {
            Core.SecondTimerEvent -= new TimerHandler(Core_SecondTimer);
            Core.SecondTimerEvent -= new TimerHandler(Core_MinuteTimer);

            Network.Store.StoreEvent[ServiceID, 0] -= new StoreHandler(Store_Local);
            Network.Store.ReplicateEvent[ServiceID, 0] -= new ReplicateHandler(Store_Replicate);
            Network.Store.PatchEvent[ServiceID, 0] -= new PatchHandler(Store_Patch);
            Network.Searches.SearchEvent[ServiceID, 0] -= new SearchRequestHandler(Search_Local);
        }


        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong user, uint project)
        {
        }

        void Core_SecondTimer()
        {
            // run code below every quarter second
            int second = Core.TimeNow.Second + 1; // get 1 - 60 value
            if (second % 15 != 0)
                return;

            // prune lookup keys
            if (LookupIndex.SafeCount > PruneLookupKeys)
                LookupIndex.LockWriting(delegate()
                {
                    while (LookupIndex.Count > PruneLookupKeys / 2)
                    {
                        ulong furthest = Network.Local.UserID;

                        foreach (ulong id in LookupIndex.Keys)
                            if ((id ^ Network.Local.UserID) > (furthest ^ Network.Local.UserID))
                                furthest = id;

                        LookupIndex.Remove(furthest);
                    }
                });

            // prune lookup entries
            LookupIndex.LockReading(delegate()
            {
                foreach (LinkedList<CryptLoc> list in LookupIndex.Values)
                    if (list.Count > PruneLookupEntries)
                        while (list.Count > PruneLookupEntries / 2)
                            list.RemoveLast();
            });
        }

        void Core_MinuteTimer()
        {
            // lookup ttl, once per minute
            List<ulong> removeIDs = new List<ulong>();

            LookupIndex.LockReading(delegate()
            {
                List<CryptLoc> removeList = new List<CryptLoc>();

                foreach (ulong key in LookupIndex.Keys)
                {
                    removeList.Clear();

                    foreach (CryptLoc loc in LookupIndex[key])
                    {
                        if (loc.TTL > 0)
                            loc.TTL--;

                        if (loc.TTL == 0)
                            removeList.Add(loc);
                    }

                    foreach (CryptLoc loc in removeList)
                        LookupIndex[key].Remove(loc);

                    if (LookupIndex[key].Count == 0)
                        removeIDs.Add(key);
                }


            });

            LookupIndex.LockWriting(delegate()
            {
                foreach (ulong key in removeIDs)
                    LookupIndex.Remove(key);
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

                Store_Local(new DataReq(null, opID, ServiceID, 0, data));
            });

        }

        void Search_Local(ulong key, byte[] parameters, List<byte[]> results)
        {
            LookupIndex.LockReading(delegate()
            {
                if (LookupIndex.ContainsKey(key))
                    foreach (CryptLoc loc in LookupIndex[key])
                        results.Add(loc.Encode(Network.Protocol));
            });
        }

        internal void Store_Local(DataReq crypt)
        {
            CryptLoc newLoc = CryptLoc.Decode(crypt.Data);

            if (newLoc == null)
                return;

            // check if data is for our operation, if it is use it
            Core.Context.Cores.LockReading(delegate()
            {
                foreach (OpCore opCore in Core.Context.Cores)
                    if (crypt.Target == opCore.Network.OpID && opCore.User.Settings.OpAccess != AccessType.Secret)
                    {
                        DataReq store = new DataReq(null, opCore.Network.OpID, ServiceID, 0, newLoc.Data);

                        if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
                            store.Data = Utilities.DecryptBytes(store.Data, store.Data.Length, opCore.Network.OriginalCrypt.Key);

                        store.Sources = null; // dont pass lookup sources to operation store 

                        OpCore staticCore = opCore; // if use opCore, foreach will change ref
                        staticCore.RunInCoreAsync( ()=>
                            staticCore.Locations.OperationStore_Local(store));
                    }
            });

            // index location 
            LinkedList<CryptLoc> locations = null;

            if (LookupIndex.SafeTryGetValue(crypt.Target, out locations))
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
                LookupIndex.SafeAdd(crypt.Target, locations);
            }

            locations.AddFirst(newLoc);
        }

        List<byte[]> Store_Replicate(DhtContact contact)
        {
            //crit
            // just send little piece of first 8 bytes, if remote doesnt have it, it is requested through params with those 8 bytes

            return null;
        }

        void Store_Patch(DhtAddress source, byte[] data)
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

                Store_Local(store);
            }
        }
    }
}
