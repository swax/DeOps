using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

using RiseOp.Simulator;
using RiseOp.Services;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;

namespace RiseOp.Implementation.Dht
{
	internal class DhtRouting
	{
        int BucketLimit = 63;
        internal int ContactsPerBucket = 16;

        // super-classes
        internal OpCore Core;
		internal DhtNetwork Network;


        internal ulong LocalRoutingID;

        internal List<DhtBucket> BucketList = new List<DhtBucket>();

        internal Dictionary<ulong, DhtContact> ContactMap = new Dictionary<ulong, DhtContact>();

        int NetworkTimeout = 15; // seconds
        int AttemptTimeout = 5; // seconds
        internal bool DhtResponsive;
		internal DateTime NextSelfSearch = new DateTime(0);

        internal DhtBound NearXor = new DhtBound(ulong.MaxValue, 8);
        internal DhtBound NearHigh = new DhtBound(ulong.MaxValue, 4);
        internal DhtBound NearLow = new DhtBound(0, 4);


		internal DhtRouting(DhtNetwork network)
		{
            Core = network.Core;
			Network = network;

            if (Core.Sim != null)
                ContactsPerBucket = 8;

            LocalRoutingID = Network.Local.UserID ^ Network.Local.ClientID;

			BucketList.Add( new DhtBucket(this, 0, true) );
		}

        internal void SecondTimer()
        {
            // if not connected, cache is frozen until re-connected
            // ideally for disconnects around 10 mins, most of cache will still be valid upon reconnect
            if (!Network.Responsive)
                return;


            // hourly self search, even if dht not enabled so we can find closer proxies for example
            if (Core.TimeNow > NextSelfSearch)
            {
                // if behind nat this is how we ensure we are at closest proxy
                // slightly off to avoid proxy host from returning with a found
                Network.Searches.Start(LocalRoutingID + 1, "Self", Core.DhtServiceID, 0, null, null);
                NextSelfSearch = Core.TimeNow.AddHours(1);
            }

            // refresh highest lowest bucket
            if (DhtEnabled)
                foreach (DhtBucket bucket in BucketList)
                {
                    // if a node has a lot of dupes logged on there will be 20 empty buckets all the way down
                    // to where clientIDs start getting split up, don't research on those

                    // if bucket not low, then it will not refresh, but once low state is triggered  
                    // a re-search is almost always immediately done to bring it back up
                    if (bucket.ContactList.Count != 0 &&
                        bucket.ContactList.Count < ContactsPerBucket / 2 &&
                        Core.Firewall == FirewallType.Open &&
                        Core.TimeNow > bucket.NextRefresh)
                    {
                        // search on random id in bucket
                        Network.Searches.Start(bucket.GetRandomBucketID(), "Low Bucket", Core.DhtServiceID, 0, null, null);
                        bucket.NextRefresh = Core.TimeNow.AddMinutes(15);

                        break;
                    }
                }

            /*
                est network size = bucket size * 2^(number of buckets - 1)
		        est cache contacts = bucket size * number of buckets
		        ex: net of 2M = 2^21 = 2^4 * 2^(18-1)
			        contacts = 16 * 18 = 288, 1 refresh per sec = ~5 mins for full cache validation
			*/
           
            // if dhtEnabled - continually ping hosts - get oldest and ping it, remove timed out
            // if not (proxied) - passively collect hosts from server - time out non-refreshed
            // if not connected - don't remove hosts from routing

            // get youngest (freshest) and oldest contact
            DhtContact oldest = null;
            DhtContact youngest = null;
            List<DhtContact> timedOut = new List<DhtContact>();
            int passiveTimeout = ContactMap.Count + 3 * AttemptTimeout; // the timeout upper bound is the routing table size + attempt wait * 3

            foreach (DhtContact contact in ContactMap.Values)
            {
                if (youngest == null || contact.LastSeen > youngest.LastSeen)
                    youngest = contact;


                if (DhtEnabled)
                {
                    if (Core.TimeNow > contact.NextTry && (oldest == null || contact.LastSeen < oldest.LastSeen))
                        if (contact.Attempts < 2)
                            oldest = contact;
                        else
                            timedOut.Add(contact); // move than two tries, time out
                }

                else if(Core.TimeNow > contact.LastSeen.AddSeconds(passiveTimeout))
                    timedOut.Add(contact);
            }

            foreach (DhtContact contact in timedOut)
                RemoveContact(contact);

            
            // stagger cache pings, so once every second
			// find oldest can attempt, send ping, remove expired
            if (oldest != null && Core.TimeNow > oldest.NextTry)
            {
                Network.Send_Ping(oldest);

                oldest.Attempts++;

                // allow 10 (2*AttemptTimeout) unique nodes to be tried before disconnect
                // (others should be pinging us as well if connected)
                oldest.NextTry = Core.TimeNow.AddSeconds(AttemptTimeout);
            }


            // know if disconnected within 15 secs for any network size
			// find youngest, if more than 15 secs old, we are disconnected
            // in this time 15 unique contacts should have been pinged
            SetResponsive(youngest != null && youngest.LastSeen.AddSeconds(NetworkTimeout) > Core.TimeNow);
        }

        private void SetResponsive(bool responsive)
        {
            if (DhtResponsive == responsive)
                return;

            // reset attempts when re-entering responsive mode
            if (responsive)
                foreach (DhtContact contact in ContactMap.Values)
                {
                    contact.Attempts = 0;
                    contact.NextTry = Core.TimeNow;
                }

            DhtResponsive = responsive;
        }

        private void RemoveContact(DhtContact target)
        {
            // alert app of new bounds? yes need to activate caching to new nodes in bounds as network shrinks 

            bool refreshBuckets = false;
            bool refreshXor = false;
            bool refreshHigh = false;
            bool refreshLow = false;


            if (ContactMap.ContainsKey(target.RoutingID))
                ContactMap.Remove(target.RoutingID);

            foreach (DhtBucket check in BucketList)
                if (check.ContactList.Contains(target))
                {
                    refreshBuckets = check.ContactList.Remove(target) ? true : refreshBuckets;
                    break;
                }

            refreshXor = NearXor.Contacts.Remove(target) ? true : refreshXor;
            refreshHigh = NearHigh.Contacts.Remove(target) ? true : refreshHigh;
            refreshLow = NearLow.Contacts.Remove(target) ? true : refreshLow;
        

            if (refreshBuckets)
                CheckMerge();


            // refesh lists that have been modified by getting next closest contacts
            List<DhtContact> replicate = new List<DhtContact>();

            if (refreshXor)
            {
                NearXor.Contacts = Find(LocalRoutingID, NearXor.Max);

                // set bound to furthest contact in range
                if (NearXor.Contacts.Count == NearXor.Max)
                {
                    DhtContact furthest = NearXor.Contacts[NearXor.Max - 1];

                    NearXor.SetBounds(LocalRoutingID ^ furthest.RoutingID,
                                  Network.Local.UserID ^ furthest.UserID);

                    // ensure node being replicated to hasnt already been replicated to through another list
                    if (!NearHigh.Contacts.Contains(furthest) &&
                        !NearLow.Contacts.Contains(furthest) &&
                        !replicate.Contains(furthest))
                        replicate.Add(furthest);
                }
                else
                    NearXor.SetBounds(ulong.MaxValue, ulong.MaxValue);
            }

            // node removed from closest, so there isnt anything in buckets closer than lower bound
            // so find node next closest to lower bound
            if (refreshLow)
            {
                DhtContact closest = null;
                ulong bound = NearLow.Contacts.Count > 0 ? NearLow.Contacts[0].RoutingID : LocalRoutingID;

                foreach (DhtBucket x in BucketList)
                    foreach (DhtContact contact in x.ContactList)
                        if (closest == null ||
                            (closest.RoutingID < contact.RoutingID && contact.RoutingID < bound))
                        {
                            closest = contact;
                        }

                if (closest != null && !NearLow.Contacts.Contains(closest))
                {
                    NearLow.Contacts.Insert(0, closest);

                    if (!NearXor.Contacts.Contains(closest) &&
                        !NearHigh.Contacts.Contains(closest) &&
                        !replicate.Contains(closest))
                        replicate.Add(closest);
                }

                if (NearLow.Contacts.Count < NearLow.Max)
                    NearLow.SetBounds(0, 0);
                else
                    NearLow.SetBounds(NearLow.Contacts[0].RoutingID, NearLow.Contacts[0].UserID);
            }

            // high - get next highest
            if (refreshHigh)
            {
                DhtContact closest = null;
                ulong bound = NearHigh.Contacts.Count > 0 ? NearHigh.Contacts[NearHigh.Contacts.Count - 1].RoutingID : LocalRoutingID;

                foreach (DhtBucket x in BucketList)
                    foreach (DhtContact contact in x.ContactList)
                        if (closest == null ||
                            (bound < contact.RoutingID && contact.RoutingID < closest.RoutingID))
                        {
                            closest = contact;
                        }

                if (closest != null && !NearHigh.Contacts.Contains(closest))
                {
                    NearHigh.Contacts.Insert(NearHigh.Contacts.Count, closest);

                    if (!NearXor.Contacts.Contains(closest) &&
                        !NearLow.Contacts.Contains(closest) &&
                        !replicate.Contains(closest))
                        replicate.Add(closest);
                }

                if (NearHigh.Contacts.Count < NearHigh.Max)
                    NearHigh.SetBounds(ulong.MaxValue, ulong.MaxValue);
                else
                    NearHigh.SetBounds(NearHigh.Contacts[NearHigh.Contacts.Count - 1].RoutingID,
                                     NearHigh.Contacts[NearHigh.Contacts.Count - 1].UserID);

            }

            foreach (DhtContact contact in replicate)
                Network.Store.Replicate(contact);
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

            NearXor.Contacts.Remove(removed);

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

        internal bool InCacheArea(ulong user)
        {
            // modify lower bits so user on xor/high/low routing ID specific client boundary wont be rejected 

            if (user == Network.Local.UserID)
                return true;

            // xor is primary
            if ((Network.Local.UserID ^ user) <= NearXor.UserBound)
                return true;

            // high/low is backup, a continuous cache, to keep data from being lost by grouped nodes in xor
            // boundaries are xor'd with client id these need to be modified to work with an user id

            if (NearLow.UserBound <= user && user <= Network.Local.UserID)
                return true;

            if (Network.Local.UserID <= user && user <= NearHigh.UserBound)
                return true;

            return false;
        }

        internal IEnumerable<DhtContact> GetCacheArea()
        {
            Dictionary<ulong, DhtContact> map = new Dictionary<ulong,DhtContact>();

            // can replace these with delegate

            foreach (DhtContact contact in NearXor.Contacts)
                if (!map.ContainsKey(contact.RoutingID))
                    map.Add(contact.RoutingID, contact);

            foreach (DhtContact contact in NearLow.Contacts)
                if (!map.ContainsKey(contact.RoutingID))
                    map.Add(contact.RoutingID, contact);

            foreach (DhtContact contact in NearHigh.Contacts)
                if (!map.ContainsKey(contact.RoutingID))
                    map.Add(contact.RoutingID, contact);

            return map.Values;
        }

        internal bool DhtEnabled
        {
            get
            {
                // dht enable if node is open, or psuedo-open as signaled by using global proxies
                return Core.Firewall == FirewallType.Open || Network.UseGlobalProxies;
            }
        }

        internal void TryAdd(G2ReceivedPacket packet, DhtSource source)
        {
            TryAdd(packet, source, false);
        }

        internal void TryAdd(G2ReceivedPacket packet, DhtSource source, bool pong)
        {
            // packet has IP and tunnel info
            // source has operational info

            // if firewall flag not set add to routing
            if (source.Firewall == FirewallType.Open)
                Add(new DhtContact(source, packet.Source.IP), pong);

            // if tunneled source doesnt have op reachable IP, must go over global
            else if (packet.Tunneled)
                Add(new DhtContact(source, packet.Source.IP, packet.Source.TunnelClient, packet.Source.TunnelServer), pong);

        }

        internal void Add(DhtContact newContact)
        {
            Add(newContact, false);
        }

		internal void Add(DhtContact newContact, bool pong)
		{
			if(newContact.UserID == 0)
			{
                Network.UpdateLog("Routing", "Zero add attempt");
				return;
			}

            if (newContact.UserID == Network.Local.UserID && newContact.ClientID == Network.Local.ClientID)
			{
                // happens because nodes will include ourselves in returnes to search requests
                //Network.UpdateLog("Routing", "Self add attempt");
				return;
			}

            if (newContact.ClientID == 0)
                return;

            
            // test to check if non open hosts being added to routing table through simulation
            if (Core.Sim != null && newContact.TunnelClient == null)
            {
                IPEndPoint address = new IPEndPoint(newContact.IP, newContact.UdpPort);

                if (Core.Sim.Internet.UdpEndPoints.ContainsKey(address))
                {
                    DhtNetwork checkNet = Core.Sim.Internet.UdpEndPoints[address];

                    if (checkNet.Local.UserID != newContact.UserID ||
                        checkNet.Local.ClientID != newContact.ClientID ||
                        checkNet.TcpControl.ListenPort != newContact.TcpPort ||
                        checkNet.Core.Sim.RealFirewall != FirewallType.Open)
                        throw new Exception("Routing add mismatch");
                }
            }

            // if dht enabled routing entries are set alive by pong
            // many things call add, like the location service, but don't want to falsely report being responsive
            // so only trigger dht responsive when a direct pong comes in
            if (!DhtEnabled || pong)
            {
                newContact.LastSeen = Core.TimeNow;

                if (ContactMap.ContainsKey(newContact.RoutingID))
                    ContactMap[newContact.RoutingID].Alive(Core.TimeNow);  

                SetResponsive(true);
            }
            
            Network.AddCacheEntry(newContact);
            
            if (ContactMap.ContainsKey(newContact.RoutingID))
            {
                DhtContact dupe = ContactMap[newContact.RoutingID];

                // tunnel may change from pong / location update etc.. reflect in routing
                // once host is open and in routing, prevent it from being maliciously set back to tunneled
                if (dupe.TunnelServer != null)
                {
                    dupe.TunnelServer = newContact.TunnelServer;
                    dupe.TunnelClient = newContact.TunnelClient;
                }

                return;
            }

            // add to searches
            foreach (DhtSearch search in Network.Searches.Active)
                search.Add(newContact);


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
            if ((LocalRoutingID ^ check.RoutingID) <= NearXor.RoutingBound)
            {
                NearXor.Contacts = Find(LocalRoutingID, NearXor.Max);

                // set bound to furthest contact in range
                if (NearXor.Contacts.Count == NearXor.Max)
                    NearXor.SetBounds( LocalRoutingID ^ NearXor.Contacts[NearXor.Max - 1].RoutingID,
                                    Network.Local.UserID ^ NearXor.Contacts[NearXor.Max - 1].UserID);

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
            if (NearLow.RoutingBound < check.RoutingID && check.RoutingID < LocalRoutingID)     
            {
                // sorted lowest to ourself
                int i = 0;
                for ( ; i < NearLow.Contacts.Count; i++)
                    if (check.RoutingID < NearLow.Contacts[i].RoutingID)
                        break;

                NearLow.Contacts.Insert(i, check);
                ContactMap[check.RoutingID] = check;

                if (NearLow.Contacts.Count > NearLow.Max)
                {
                    DhtContact remove = NearLow.Contacts[0];
                    NearLow.Contacts.Remove(remove);
                    if (!InBucket(remove))
                        ContactMap.Remove(remove.RoutingID);


                    NearLow.SetBounds( NearLow.Contacts[0].RoutingID, NearLow.Contacts[0].UserID);
                }

                return true;
            }


            if (LocalRoutingID < check.RoutingID && check.RoutingID < NearHigh.RoutingBound)
            {
                // sorted ourself to highest
                int i = 0;
                for (; i < NearHigh.Contacts.Count; i++)
                    if (check.RoutingID < NearHigh.Contacts[i].RoutingID)
                        break;

                NearHigh.Contacts.Insert(i, check);
                ContactMap[check.RoutingID] = check;

                if (NearHigh.Contacts.Count > NearHigh.Max)
                {
                    DhtContact remove = NearHigh.Contacts[NearHigh.Contacts.Count - 1];
                    NearHigh.Contacts.Remove(remove);
                    if ( !InBucket(remove) )
                        ContactMap.Remove(remove.RoutingID);

                    NearHigh.SetBounds( NearHigh.Contacts[NearHigh.Contacts.Count - 1].RoutingID, 
                        NearHigh.Contacts[NearHigh.Contacts.Count - 1].UserID);
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

    internal class DhtBound
    {
        internal ulong RoutingBound;
        internal ulong UserBound;

        internal List<DhtContact> Contacts = new List<DhtContact>();

        internal int Max;

        internal DhtBound(ulong bound, int max)
        {
            RoutingBound = bound;
            UserBound = bound;
            Max = max;
        }

        internal void SetBounds(ulong routing, ulong user)
        {
            RoutingBound = routing;
            UserBound = user;
        }
    }
}
