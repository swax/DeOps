using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;


namespace RiseOp.Services.Assist
{
    // provides temporary caching of data at a target spot on the network

    internal class TempCache
    {
        OpCore Core;
        DhtNetwork Network;

        uint ServiceID;
        uint DataType;

        int MaxEntries = 512;

        List<TempData> CachedData = new List<TempData>();


        internal TempCache(DhtNetwork network, uint service, uint type)
        {
            Core = network.Core;
            Network = network;

            ServiceID = service;
            DataType = type;

            Core.MinuteTimerEvent += new TimerHandler(Core_MinuteTimer);

            Network.Store.StoreEvent[ServiceID, DataType] += new StoreHandler(Store_Data);
            Network.Searches.SearchEvent[ServiceID, DataType] += new SearchRequestHandler(Search_Data);

        }

        public void Dispose()
        {
            Core.MinuteTimerEvent -= new TimerHandler(Core_MinuteTimer);

            Network.Store.StoreEvent[ServiceID, DataType] -= new StoreHandler(Store_Data);
            Network.Searches.SearchEvent[ServiceID, DataType] -= new SearchRequestHandler(Search_Data);
        }


        void Core_MinuteTimer()
        {
            // remove expired entries
            CachedData.
                Where(l => Core.TimeNow > l.Expires).
                ToList().
                ForEach(d => CachedData.Remove(d));

            // if max reached, remove furthest
            if (CachedData.Count > MaxEntries)
                CachedData.
                    OrderByDescending(d => d.TargetID ^ Network.Local.UserID).
                    Take(CachedData.Count - MaxEntries).
                    ToList().
                    ForEach(d => CachedData.Remove(d));
        }

        void Store_Data(DataReq store)
        {
            // location being published to hashid so others can get sources

            TempData loc = CachedData.Where(l => l.TargetID == store.Target && Utilities.MemCompare(store.Data, store.Data)).FirstOrDefault();

            if (loc != null)
            {
                loc.Expires = Core.TimeNow.AddHours(1);
                return;
            }

            loc = new TempData()
            {
                TargetID = store.Target,
                Data = store.Data,
                Expires = Core.TimeNow.AddHours(1)
            };

            CachedData.Add(loc);
        }

        void Search_Data(ulong key, byte[] parameters, List<byte[]> results)
        {
            // return 3 random data entries for target

            results.AddRange((from l in CachedData
                              where l.TargetID == key
                              orderby Core.RndGen.Next()
                              select l.Data).Take(3));
        }

        internal void Publish(ulong target, byte[] data)
        {
            Store_Data(new DataReq(null, target, ServiceID, DataType, data));
            
            Network.Store.PublishNetwork(target, ServiceID, DataType, data);
        }

        internal void Search(ulong target, object carry, EndSearchHandler endEvent)
        {
            DhtSearch search = Network.Searches.Start(target, "Temp Search", ServiceID, DataType, null, endEvent);
            search.Carry = carry;
        }
    }



    internal class TempData
    {
        internal ulong TargetID;
        internal DateTime Expires;
        internal byte[] Data;
    }
}
