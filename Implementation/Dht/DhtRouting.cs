using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

using RiseOp.Simulator;
using RiseOp.Services;
using RiseOp.Implementation.Protocol.Net;

namespace RiseOp.Implementation.Dht
{
	internal class DhtRouting
	{
        int BucketLimit = 63;
        internal int ContactsPerBucket = 16;

        int HighLowCacheCount = 4;
        int XorCacheCount = 8;

  
        // super-classes
        internal OpCore Core;
		internal DhtNetwork Network;


        ulong LocalRoutingID;

		internal int CurrentBucket;
        internal List<DhtBucket> BucketList = new List<DhtBucket>();

        internal Dictionary<ulong, DhtContact> ContactMap = new Dictionary<ulong, DhtContact>();

        internal bool Responsive;
        int NetworkTimeout = 60; // seconds
        internal DateTime LastUpdated    = new DateTime(0);
		internal DateTime NextSelfSearch = new DateTime(0);

        internal ulong XorBound = ulong.MaxValue;
        internal ulong HighBound = ulong.MaxValue;
        internal ulong LowBound = 0;

        internal List<DhtContact> XorContacts = new List<DhtContact>();
        internal List<DhtContact> HighContacts = new List<DhtContact>();
        internal List<DhtContact> LowContacts = new List<DhtContact>();


		internal DhtRouting(DhtNetwork network)
		{
            Core = network.Core;
			Network = network;

			LastUpdated = new DateTime(0);

            if (Core.Sim != null)
            {
                BucketLimit = 25;
                ContactsPerBucket = 8;
            }

            LocalRoutingID = Core.LocalDhtID ^ Core.ClientID;

			BucketList.Add( new DhtBucket(this, 0, true) );
		}

        internal void SecondTimer()
        {
            if (Core.TimeNow > NextSelfSearch)
            {
                // if behind nat this is how we ensure we are at closest proxy
                // slightly off to avoid proxy host from returning with a found
                Network.Searches.Start(LocalRoutingID + 1, "Self", Core.DhtServiceID, 0, null, new EndSearchHandler(EndSelfSearch));
                NextSelfSearch = Core.TimeNow.AddHours(1);
            }

            // get current bucket
            DhtBucket bucket = null;

            lock (BucketList)
                if (CurrentBucket < BucketList.Count)
                {
                    bucket = BucketList[CurrentBucket];
                    CurrentBucket++;
                }
                else
                {
                    bucket = BucketList[0];
                    CurrentBucket = 1;
                }

            bool bucketLow = bucket.ContactList.Count < ContactsPerBucket / 2;

            // if bucket low
            if (bucketLow &&
                Core.Firewall == FirewallType.Open &&
                Core.TimeNow > bucket.NextRefresh)
            {
                // get random id in bucket
                Network.Searches.Start(bucket.GetRandomBucketID(), "Low Bucket", Core.DhtServiceID, 0, null, null);
                bucket.NextRefresh = Core.TimeNow.AddMinutes(15);
            }

            List<DhtContact> replicate = new List<DhtContact>();


            // alert app of new bounds? yes need to activate caching to new nodes in bounds as network shrinks 

            bool refreshXor = false;
            bool refreshHigh = false;
            bool refreshLow = false;

            // check xor bounds
            DhtContact removed = CheckContactList(bucket.ContactList);

            if (removed != null)
            {
                CheckMerge();
                XorContacts.Remove(removed);

                refreshXor = ((LocalRoutingID ^ removed.RoutingID) < XorBound);
                refreshHigh = HighContacts.Remove(removed);
                refreshLow = LowContacts.Remove(removed);
            }

            // check low bounds
            if (Core.TimeNow.Second == 0 || Core.TimeNow.Second == 20 || Core.TimeNow.Second == 40)
            {
                removed = CheckContactList(LowContacts);

                if (removed != null)
                {
                    refreshLow = true;
                    refreshXor = RemoveFromBuckets(removed);
                }
            }

            // check high bounds
            if (Core.TimeNow.Second == 10 || Core.TimeNow.Second == 30 || Core.TimeNow.Second == 50)
            {
                removed = CheckContactList(HighContacts);
                if (removed != null)
                {
                    refreshHigh = true;
                    refreshXor = RemoveFromBuckets(removed);
                }
            }


            if (refreshXor)
            {
                XorContacts = Find(LocalRoutingID, XorCacheCount);

                // set bound to furthest contact in range
                if (XorContacts.Count == XorCacheCount)
                {
                    DhtContact furthest = XorContacts[XorCacheCount - 1];

                    XorBound = LocalRoutingID ^ furthest.RoutingID;

                    // ensure node being replicated to hasnt already been replicated to through another list
                    if (!HighContacts.Contains(furthest) &&
                        !LowContacts.Contains(furthest) && 
                        !replicate.Contains(furthest))
                        replicate.Add(furthest);
                }
                else
                    XorBound = ulong.MaxValue;
            }

            // node removed from closest, so there isnt anything in buckets closer than lower bound
            // so find node next closest to lower bound
            if (refreshLow)
            {
                DhtContact closest = null;
                ulong bound = LowContacts.Count > 0 ? LowContacts[0].RoutingID : LocalRoutingID;

                foreach (DhtBucket x in BucketList)
                    foreach (DhtContact contact in x.ContactList)
                        if (closest == null ||
                            (closest.RoutingID < contact.RoutingID && contact.RoutingID < bound))
                        {
                            closest = contact;
                        }

                if (closest != null && !LowContacts.Contains(closest))
                {
                    LowContacts.Insert(0, closest);

                    if (!XorContacts.Contains(closest) &&
                        !HighContacts.Contains(closest) && 
                        !replicate.Contains(closest))
                        replicate.Add(closest);
                }

                if (LowContacts.Count < HighLowCacheCount)
                    LowBound = 0;
                else
                    LowBound = LowContacts[0].RoutingID;
            }

            // high - get next highest
            if (refreshHigh)
            {
                DhtContact closest = null;
                ulong bound = HighContacts.Count > 0 ? HighContacts[HighContacts.Count - 1].RoutingID : LocalRoutingID;

                foreach (DhtBucket x in BucketList)
                    foreach (DhtContact contact in x.ContactList)
                        if (closest == null ||
                            (bound < contact.RoutingID && contact.RoutingID < closest.RoutingID))
                        {
                            closest = contact;
                        }

                if (closest != null && !HighContacts.Contains(closest))
                {
                    HighContacts.Insert(HighContacts.Count, closest);

                    if (!XorContacts.Contains(closest) &&
                        !LowContacts.Contains(closest) && 
                        !replicate.Contains(closest))
                        replicate.Add(closest);
                }

                if (HighContacts.Count < HighLowCacheCount)
                    HighBound = ulong.MaxValue;
                else
                    HighBound = HighContacts[HighContacts.Count - 1].RoutingID;

            }

            foreach (DhtContact contact in replicate)
                Network.Store.Replicate(contact);

            // if no updates in a minute, we're disconnected
            if (Network.Established && LastUpdated.AddSeconds(NetworkTimeout) < Core.TimeNow)
                SetResponsive(false);
        }

        private bool RemoveFromBuckets(DhtContact removed)
        {
            foreach (DhtBucket bucket in BucketList)
                foreach (DhtContact contact in bucket.ContactList)
                    if (contact == removed)
                    {
                        bucket.ContactList.Remove(contact);
                        return true;
                    }

            XorContacts.Remove(removed);

            return false;
        }

        private bool InBucket(DhtContact find)
        {
            foreach (DhtBucket bucket in BucketList)
                foreach (DhtContact contact in bucket.ContactList)
                    if (contact == find)
                        return true;

            return false;
        }

        private DhtContact CheckContactList(List<DhtContact> contacts)
        {
            // get oldest pingable contact
            DateTime oldestTime = Core.TimeNow;
            DhtContact oldContact = null;


            // if havent seen for more than a minute and time is greater than nexttry
            foreach (DhtContact contact in contacts)
                if (contact.LastSeen.AddMinutes(1) < Core.TimeNow &&
                    contact.LastSeen < oldestTime &&
                    contact.NextTry < Core.TimeNow)
                {
                    oldContact = contact;
                    oldestTime = contact.LastSeen;
                }

            if (oldContact != null)
            {
                // if less than 2 attempts
                if (oldContact.Attempts < 2)
                {
                    // if not firewalled proxy refreshes nodes, let old ones timeout
                    if (Core.Firewall == FirewallType.Open)
                        Network.Send_Ping(oldContact.ToDhtAddress());

                    oldContact.Attempts++;
                    oldContact.NextTry = Core.TimeNow.AddSeconds(30);
                }

                // else remove contact
                else
                {
                    contacts.Remove(oldContact);

                    if (ContactMap.ContainsKey(oldContact.RoutingID))
                        ContactMap.Remove(oldContact.RoutingID);

                    return oldContact;
                }
            }

            return null;
        }

        internal bool InCacheArea(ulong user)
        {
            // modify lower bits so user on xor/high/low routing ID specific client boundary wont be rejected 

            if (user == Core.LocalDhtID)
                return true;

            // xor is primary
            ulong xor = XorBound & ushort.MaxValue;
            if ((Core.LocalDhtID ^ user) <= xor)
                return true;

            // high/low is backup, a continuous cache, to keep data from being lost by grouped nodes in xor
            user = user & ushort.MaxValue;
            if (LowBound <= user && user <= Core.LocalDhtID)
                return true;

            user = user & ushort.MinValue;
            if (Core.LocalDhtID <= user && user <= HighBound)
                return true;

            return false;
        }

        internal IEnumerable<DhtContact> GetCacheArea()
        {
            Dictionary<ulong, DhtContact> map = new Dictionary<ulong,DhtContact>();

            // can replace these with delegate

            foreach (DhtContact contact in XorContacts)
                if (!map.ContainsKey(contact.RoutingID))
                    map.Add(contact.RoutingID, contact);

            foreach (DhtContact contact in LowContacts)
                if (!map.ContainsKey(contact.RoutingID))
                    map.Add(contact.RoutingID, contact);

            foreach (DhtContact contact in HighContacts)
                if (!map.ContainsKey(contact.RoutingID))
                    map.Add(contact.RoutingID, contact);

            return map.Values;
        }

        internal void EndSelfSearch(DhtSearch search)
        {
            // if not already established (an hourly self re-search)
            if (!Network.Established)
            {
                Network.FireStatusChange = 10; // a little buffer time for local nodes to send patch files
            }
        }

		internal void Add(DhtContact newContact)
		{
			if(newContact.DhtID == 0)
			{
                Network.UpdateLog("Routing", "Zero add attempt");
				return;
			}

            if (newContact.DhtID == Core.LocalDhtID && newContact.ClientID == Core.ClientID)
			{
                // happens because nodes will include ourselves in returnes to search requests
                //Network.UpdateLog("Routing", "Self add attempt");
				return;
			}

            if (newContact.ClientID == 0)
                return;

            
            // test to check if non open hosts being added to routing table through simulation
            if (Core.Sim != null)
            {
                IPEndPoint address = new IPEndPoint(newContact.Address, newContact.UdpPort);

                if (Core.Sim.Internet.AddressMap.ContainsKey(address))
                {
                    DhtNetwork checkNet = Core.Sim.Internet.AddressMap[address];

                    if (checkNet.Core.LocalDhtID != newContact.DhtID ||
                        checkNet.Core.ClientID != newContact.ClientID ||
                        checkNet.TcpControl.ListenPort != newContact.TcpPort ||
                        checkNet.Core.Sim.RealFirewall != FirewallType.Open)
                        throw new Exception("Routing add mismatch");
                }
            }

            if (newContact.LastSeen > LastUpdated)
            {
                LastUpdated = newContact.LastSeen;

                if (!Responsive)
                    SetResponsive(true);
            }

            // add to ip cache
            if (newContact.LastSeen.AddMinutes(1) > Core.TimeNow)
                Network.AddCacheEntry(new IPCacheEntry(newContact));

			// add to searches
            foreach (DhtSearch search in Network.Searches.Active)
				search.Add(newContact);


            // if contact already in bucket/high/low list
            if (ContactMap.ContainsKey(newContact.RoutingID))
            {
                ContactMap[newContact.RoutingID].Alive(newContact.LastSeen);
                return;
            }

            AddtoBucket(newContact);
			
            bool replicate = false;

            // check/set xor bound, cant combine below because both need to run
            if (CheckXor(newContact))
                replicate = true;

            // check if should be added to high/low
            if (CheckHighLow(newContact))
                replicate = true;

            if (replicate)
                Network.Store.Replicate(newContact);
        

            if(Network.GuiGraph != null)
                Network.GuiGraph.BeginInvoke(Network.GuiGraph.UpdateGraph);
		}

        private void SetResponsive(bool responsive)
        {
            Responsive = responsive;

            if (Responsive)
            {
                Network.Searches.Start(LocalRoutingID + 1, "Self", Core.DhtServiceID, 0, null, new EndSearchHandler(EndSelfSearch));
                NextSelfSearch = Core.TimeNow.AddHours(1);
            }

            // network dead
            else
            {
                Network.Established = false;
                Network.StatusChange.Invoke();
            }
        }

        private void AddtoBucket(DhtContact newContact)
        {
            // add to buckets
            int depth = 0;
            int pos = 1;

            List<DhtContact> moveContacts = null;

            lock (BucketList)
                foreach (DhtBucket bucket in BucketList)
                {
                    // if not the last bucket
                    if (!bucket.Last)
                        // if this is not the contacts place on tree
                        if (Utilities.GetBit(LocalRoutingID, depth) == Utilities.GetBit(newContact.RoutingID, depth))
                        {
                            depth++;
                            pos++;
                            continue;
                        }


                    // if cant add contact
                    if (!bucket.Add(newContact))
                    {
                        if (BucketList.Count > BucketLimit)
                            return;

                        // split bucket if last and try add again
                        if (bucket.Last)
                        {
                            // save contacts from bucket and reset
                            moveContacts = bucket.ContactList;
                            bucket.ContactList = new List<DhtContact>();

                            // create new bucket
                            bucket.Last = false;
                            BucketList.Add(new DhtBucket(this, bucket.Depth + 1, true));

                            // reaching here means dhtbucket::add was never called successfully, it gets recalled after the move
                            //Network.Store.RoutingUpdate(BucketList.Count); 
                            break;
                        }

                        // else contact dropped
                    }

                    break;
                }

            // split should not recurse anymore than once
            if (moveContacts != null)
            {
                foreach (DhtContact contact in moveContacts)
                    AddtoBucket(contact);

                AddtoBucket(newContact);
            }

   
        }

        private bool CheckXor(DhtContact check)
        {
            if ((LocalRoutingID ^ check.RoutingID) <= XorBound)
            {
                XorContacts = Find(LocalRoutingID, XorCacheCount);

                // set bound to furthest contact in range
                if (XorContacts.Count == XorCacheCount)
                    XorBound = LocalRoutingID ^ XorContacts[XorCacheCount - 1].RoutingID;

                return true;
            }

            return false;
        }

        private bool CheckHighLow(DhtContact check)
        {
            // dont store self in high/low, buckets do that

            // we keep high/low nodes (not xor'd distance) because xor is absolute and
            // will cause a cluster of nodes to cache each other and ignore a node further away
            // then they are to each other, to ensure that far off node's stuff exists on network
            // we also set bounds high/low based on ID so node will always have someone to cache for it
            // nodes in high/low and xor ranges should mostly overlap

            // if another client of same user added, in bounds, replicate to it
            if (LowBound < check.RoutingID && check.RoutingID < LocalRoutingID)     
            {
                // sorted lowest to ourself
                int i = 0;
                for ( ; i < LowContacts.Count; i++)
                    if (check.RoutingID < LowContacts[i].RoutingID)
                        break;

                LowContacts.Insert(i, check);
                ContactMap[check.RoutingID] = check;

                if (LowContacts.Count > HighLowCacheCount)
                {
                    DhtContact remove = LowContacts[0];
                    LowContacts.Remove(remove);
                    if (!InBucket(remove))
                        ContactMap.Remove(remove.RoutingID);


                    LowBound = LowContacts[0].RoutingID;
                }

                return true;
            }


            if (LocalRoutingID < check.RoutingID && check.RoutingID < HighBound)
            {
                // sorted ourself to highest
                int i = 0;
                for (; i < HighContacts.Count; i++)
                    if (check.RoutingID < HighContacts[i].RoutingID)
                        break;

                HighContacts.Insert(i, check);
                ContactMap[check.RoutingID] = check;

                if (HighContacts.Count > HighLowCacheCount)
                {
                    DhtContact remove = HighContacts[HighContacts.Count - 1];
                    HighContacts.Remove(remove);
                    if ( !InBucket(remove) )
                        ContactMap.Remove(remove.RoutingID);

                    HighBound = HighContacts[HighContacts.Count - 1].RoutingID;
                }

                return true;
            }

            return false;
        }

		internal void CheckMerge()
		{
			if(BucketList.Count <= 1)
				return;

			lock(BucketList)
			{
				DhtBucket lastBucket = (DhtBucket) BucketList[BucketList.Count - 1];
				DhtBucket nexttoLast = (DhtBucket) BucketList[BucketList.Count - 2];

				// see if buckets can be merged
                if (nexttoLast.ContactList.Count + lastBucket.ContactList.Count < ContactsPerBucket)
				{
					nexttoLast.Last = true;

                    BucketList.Remove(lastBucket);

                    foreach (DhtContact contact in lastBucket.ContactList)
                        nexttoLast.Add(contact);
				}
			}
		}

		internal List<DhtContact> Find(UInt64 targetID, int resultMax)
		{
            // refactor for speed

            SortedList<ulong, DhtContact> closest = new SortedList<ulong, DhtContact>();

            foreach (DhtContact contact in ContactMap.Values)
            {                
                closest.Add(contact.RoutingID ^ targetID, contact);

                if (closest.Count > resultMax)
                    closest.RemoveAt(closest.Count - 1);
            }

            List<DhtContact> results = new List<DhtContact>();

            foreach (DhtContact contact in closest.Values)
                results.Add(contact);

            return results;


            /*
            // buckets aren't sorted, so just cant grab contacts till max is reached
            // need to grab all contacts from bucket then sort for closest to target

            int  depth       = 0;
			int  pos         = 1;
			bool needExtra   = false;
			
			DhtBucket prevBucket = null;

            SortedList<ulong, List<DhtContact>> closest = new SortedList<ulong, List<DhtContact>>();

			lock(BucketList)
            {
                foreach(DhtBucket bucket in BucketList)
				{
					// find right bucket to get contacts from
                    if(!bucket.Last)
                        if (needExtra == false && Utilities.GetBit(Core.LocalDhtID, depth) == Utilities.GetBit(targetID, depth))
					    {
						    prevBucket = bucket;
						    depth++;
						    pos++;
						    continue;
					    }

                    foreach (DhtContact contact in bucket.ContactList)
                    {
                        ulong distance = contact.DhtID ^ targetID;

                        if (!closest.ContainsKey(distance))
                            closest[distance] = new List<DhtContact>();

                        closest[distance].Add(contact);
                    }

                    if (closest.Count > resultMax || bucket.Last)
						break;

                    // if still need more contacts get them from next bucket down
					needExtra = true;
				}
			
			    // if still need more conacts get from bucket up from target
                if (closest.Count < resultMax && prevBucket != null)
                    foreach (DhtContact contact in prevBucket.ContactList)
                    {
                        ulong distance = contact.DhtID ^ targetID;

                        if (!closest.ContainsKey(distance))
                            closest[distance] = new List<DhtContact>();

                        closest[distance].Add(contact);
                    }
            }

            // just built and return
            List<DhtContact> results = new List<DhtContact>();

            foreach(List<DhtContact> list in closest.Values)
                foreach (DhtContact contact in list)
                {
                    results.Add(contact);

                    if (results.Count >= resultMax)
                        return results;
                }

            return results;*/
		}
    }
}
