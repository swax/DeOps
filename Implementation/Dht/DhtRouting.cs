/********************************************************************************

	De-Ops: Decentralized Operations
	Copyright (C) 2006 John Marshall Group, Inc.

	By contributing code you grant John Marshall Group an unlimited, non-exclusive
	license to your contribution.

	For support, questions, commercial use, etc...
	E-Mail: swabby@c0re.net

********************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

using DeOps.Simulator;
using DeOps.Services;
using DeOps.Implementation.Protocol.Net;

namespace DeOps.Implementation.Dht
{
	internal class DhtRouting
	{
        static int MAX_BUCKETS = 50;

        // super-classes
        internal OpCore Core;
		internal DhtNetwork Network; 


		internal int CurrentBucket;
        internal List<DhtBucket> BucketList = new List<DhtBucket>();

		internal DateTime LastUpdated    = new DateTime(0);
		internal DateTime NextSelfSearch = new DateTime(0);


		internal DhtRouting(DhtNetwork network)
		{
            Core = network.Core;
			Network = network;

			LastUpdated = new DateTime(0);

			BucketList.Add( new DhtBucket(this, 0, true) );
		}

		internal void SecondTimer()
		{
            if (Core.TimeNow > NextSelfSearch)
			{
				// if behind nat this is how we ensure we are at closest proxy
				// slightly off to avoid proxy host from returning with a found
                Network.Searches.Start(Core.LocalDhtID + 1, "Self", Core.DhtServiceID, 0, null, new EndSearchHandler(EndSelfSearch));
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

			bool bucketLow = bucket.ContactList.Count < DhtBucket.MAX_CONTACTS / 2;

			// if bucket low, dont remove contacts
			if( bucketLow &&
                Core.Firewall == FirewallType.Open &&
                Core.TimeNow > bucket.NextRefresh)
			{
				// get random id in bucket
                Network.Searches.Start(bucket.GetRandomBucketID(), "Low Bucket", Core.DhtServiceID, 0, null, null);
                bucket.NextRefresh = Core.TimeNow.AddMinutes(15);
			}

			// get oldest pingable contact
            DateTime oldestTime = Core.TimeNow;
			DhtContact oldContact = null;


			// if havent seen for more than a minute and time is greater than nexttry
			foreach(DhtContact contact in bucket.ContactList)
                if (contact.LastSeen.AddMinutes(1) < Core.TimeNow &&
                    contact.LastSeen < oldestTime &&
                    contact.NextTry < Core.TimeNow)
				{	
					oldContact = contact;
                    oldestTime = contact.LastSeen;
				}

			if(oldContact != null)
			{
				// if less than 2 attempts
				if(oldContact.Attempts < 2)
				{
					// if not firewalled proxy refreshes nodes, let old ones timeout
                    if (Core.Firewall == FirewallType.Open)
						Network.Send_Ping(oldContact.ToDhtAddress());
					
					oldContact.Attempts++;
                    oldContact.NextTry = Core.TimeNow.AddSeconds(30);
				}

				// else remove contact if bucket more than half full
				else
				{
                    bucket.ContactList.Remove(oldContact);
					CheckMerge(); // update max distance for caching before calling routingdelete
                    
                    Network.Store.RoutingDelete(oldContact);
				}
			}
		}

        internal void EndSelfSearch(DhtSearch search)
        {
            Network.FireEstablished = 10; // a little buffer time for local nodes to send patch files
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

			if(newContact.LastSeen > LastUpdated)
				LastUpdated = newContact.LastSeen;

            // add to ip cache
            if (newContact.LastSeen.AddMinutes(1) > Core.TimeNow)
                Network.AddCacheEntry(new IPCacheEntry(newContact));

			// add to searches
            foreach (DhtSearch search in Network.Searches.Active)
				search.Add(newContact);

			// add to buckets
			int depth = 0;
			int pos   = 1;

            List<DhtContact> moveContacts = null;

			lock(BucketList)
				foreach(DhtBucket bucket in BucketList)
				{
					// if not the last bucket
					if( !bucket.Last )
						// if this is not the contacts place on tree
                        if (Utilities.GetBit(Core.LocalDhtID, depth) == Utilities.GetBit(newContact.DhtID, depth))
						{
							depth++;
							pos++;
							continue;
						}
					
					
					// if cant add contact
					if( !bucket.Add(newContact, false) )
					{
						if(BucketList.Count > MAX_BUCKETS)
							return;
						
						// split bucket if last and try add again
                        if (bucket.Last)
						{
							// save contacts from bucket and reset
                            moveContacts = bucket.ContactList;
                            bucket.ContactList = new List<DhtContact>();

                            // create new bucket
                            bucket.Last = false;
							BucketList.Add(new DhtBucket(this, bucket.Depth + 1, true) );

                            // reaching here means dhtbucket::add was never called successfully, it gets recalled after the move
                            Network.Store.RoutingUpdate(BucketList.Count); 
							break;
						}

                        // else contact dropped
					}

					break;
				}

			// split should not recurse anymore than once
			if(moveContacts != null)
			{
				foreach(DhtContact contact in moveContacts)
					Add(contact);

				Add(newContact);
			}

            if(Network.GuiGraph != null)
                Network.GuiGraph.BeginInvoke(Network.GuiGraph.UpdateGraph);
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
                if (nexttoLast.ContactList.Count + lastBucket.ContactList.Count < DhtBucket.MAX_CONTACTS)
				{
					nexttoLast.Last = true;

                    BucketList.Remove(lastBucket);
                    Network.Store.RoutingUpdate(BucketList.Count); // do first, so add replicates to new contacts

                    foreach (DhtContact contact in lastBucket.ContactList)
                        nexttoLast.Add(contact, true);
				}
			}
		}

		internal List<DhtContact> Find(UInt64 targetID, int resultMax)
		{
			int  depth       = 0;
			int  pos         = 1;
			bool needExtra   = false;
			
			DhtBucket prevBucket = null;

            // buckets aren't sorted, so just cant grab contacts till max is reached
            // need to grab all contacts from bucket then sort for closest to target


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

            return results;
		}

		internal bool Responsive()
		{
            return (LastUpdated.AddMinutes(3) > Core.TimeNow);
		}
	}
}
