using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using RiseOp.Services;
using RiseOp.Implementation.Transport;
using RiseOp.Implementation.Protocol.Net;

namespace RiseOp.Implementation.Dht
{
    internal delegate void EndSearchHandler(DhtSearch search);


	/// <summary>
	/// Summary description for DhtSearch.
	/// </summary>
	internal class DhtSearch
	{

        const int LOOKUP_SIZE = 8;
        const int SEARCH_ALPHA = 3;

        // super-classes
        OpCore Core;
		internal DhtNetwork Network;
        DhtSearchControl Searches;
        
		internal UInt64    TargetID;
		internal uint      SearchID;
        internal string    Name;
        internal uint      Service;
        internal uint      DataType;
        EndSearchHandler   EndSearch;
        internal int       TargetResults = 10;

        internal List<DhtLookup> LookupList = new List<DhtLookup>();

		internal bool   Finished;
		internal string FinishReason;
        
        internal bool   FoundProxy;
		internal DhtContact FoundContact;
        internal List<SearchValue> FoundValues = new List<SearchValue>();

		internal TcpConnect ProxyTcp;
        internal byte[] Parameters;
        internal object Carry;


        internal DhtSearch(DhtSearchControl control, UInt64 targetID, string name, uint service, uint datatype, EndSearchHandler endSearch)
		{
            Core      = control.Core;
            Network   = control.Network ;
            Searches  = control;
			TargetID  = targetID;
			Name      = name;
            Service   = service;
            DataType  = datatype;
            EndSearch = endSearch;

            SearchID = (uint) Core.RndGen.Next(1, int.MaxValue);
		}

		internal bool Activate()
		{
			// check if node tcp connected
            if (Service == Core.DhtServiceID && Network.TcpControl.ConnectionMap.ContainsKey(TargetID))
            {
                TcpConnect connection = Network.TcpControl.ConnectionMap[TargetID];

                Found(connection.GetContact(), false);
                return true;
            }

			// bootstrap search from routing
			foreach(DhtContact contact in Network.Routing.Find(TargetID, 8))
				Add(contact);

			// if natted send request to proxies for fresh nodes
            if (Core.Firewall == FirewallType.NAT)
                lock (Network.TcpControl.Connections)
                    foreach (TcpConnect connection in Network.TcpControl.Connections)
                    {
                        DhtAddress address = new DhtAddress(connection.DhtID, connection.RemoteIP, connection.UdpPort);
                        Searches.SendUdpRequest(address, TargetID, SearchID, Service, DataType, Parameters);
                    }			

			// if blocked send proxy search request to 1 proxy, record and wait
            if (Core.Firewall == FirewallType.Blocked)
			{
				// pick random proxy server
				ArrayList servers = new ArrayList();

                lock (Network.TcpControl.Connections)
                    foreach (TcpConnect connection in Network.TcpControl.Connections)
						if(connection.Proxy == ProxyType.Server)
							servers.Add(connection);

				if(servers.Count == 0)
					return false;

                ProxyTcp = (TcpConnect)servers[Core.RndGen.Next(servers.Count)];
                Send_ProxySearchRequest();
			}

			return true;
		}

        internal void Send_ProxySearchRequest()
        {
            SearchReq request = new SearchReq();

            request.Source    = Network.GetLocalSource();
            request.SearchID  = SearchID;
            request.TargetID  = TargetID;
            request.Service   = Service;
            request.DataType  = DataType;
            request.Parameters = Parameters;

            ProxyTcp.SendPacket(request);
        }


		internal void Add(DhtContact contact)
		{
			// never going to add self because filtered by routing.add

			if(Finished) // search over
				return;

			// go through lookup list, add if closer to target
			lock(LookupList)
			{
                bool added = false;

				foreach(DhtLookup lookup in LookupList)
				{	
					if(contact.DhtID == lookup.Contact.DhtID && contact.ClientID == lookup.Contact.ClientID)
						return;

					if((contact.DhtID ^ TargetID) < (lookup.Contact.DhtID ^ TargetID))
					{
						LookupList.Insert( LookupList.IndexOf(lookup), new DhtLookup(contact));
                        added = true;
						break;
					}
				}

                if(!added)
                    LookupList.Add(new DhtLookup(contact));

                while (LookupList.Count > LOOKUP_SIZE)
					LookupList.RemoveAt(LookupList.Count - 1);
			}
			

			// at end so we ensure this node is put into list and sent with proxy results
            if (Service == Core.DhtServiceID && contact.DhtID == TargetID)
			{
				Found(contact, false);
				return;
			}
		}

		internal void SecondTimer()
		{
			if(Finished) // search over
				return;

			if(ProxyTcp != null && ProxyTcp.Proxy == ProxyType.Server) // search being handled by proxy server
				return;

            // get searching count
            int searching = 0;
            foreach (DhtLookup lookup in LookupList)
                if (lookup.Status == LookupStatus.Searching)
                    searching++;

            // iterate through lookup nodes
            bool alldone = true;
			lock(LookupList)
                foreach (DhtLookup lookup in LookupList)
                {
					if(lookup.Status == LookupStatus.Done)
					    continue;

                    alldone = false;

                    // if searching
                    if (lookup.Status == LookupStatus.Searching)
                    {
                        lookup.Age++;

                        // research after 3 seconds
                        if (lookup.Age == 3)
                        {
                            //Log("Sending Request to " + lookup.Contact.Address.ToString() + " (" + Utilities.IDtoBin(lookup.Contact.DhtID) + ")");
                            Network.Searches.SendUdpRequest(lookup.Contact.ToDhtAddress(), TargetID, SearchID, Service, DataType, Parameters);
                        }

                        // drop after 6
                        if (lookup.Age >= 6)
                            lookup.Status = LookupStatus.Done;
                    }

                    // start search if room available
                    if (lookup.Status == LookupStatus.None && searching < SEARCH_ALPHA)
                    {
                        //Log("Sending Request to " + lookup.Contact.Address.ToString() + " (" + Utilities.IDtoBin(lookup.Contact.DhtID) + ")");
                        Network.Searches.SendUdpRequest(lookup.Contact.ToDhtAddress(), TargetID, SearchID, Service, DataType, Parameters);

                        lookup.Status = LookupStatus.Searching;
                    }
				}


			// set search over if nothing more
            if (alldone)
                FinishSearch(LookupList.Count.ToString() + " Search Points Exhausted");
		}


		internal void FinishSearch(string reason)
		{
			Finished     = true;
			FinishReason = reason;

            if (ProxyTcp != null)
            {
                if (ProxyTcp.Proxy == ProxyType.ClientBlocked)
                {
                    SearchAck ack = new SearchAck();
                    ack.Source = Network.GetLocalSource();
                    ack.SearchID = SearchID;

                    foreach (DhtLookup lookup in LookupList)
                        ack.ContactList.Add(lookup.Contact);
                }

                SearchReq req = new SearchReq();
                req.SearchID = SearchID;
                req.EndProxySearch = true;

                ProxyTcp.SendPacket(req);
            }

            if(EndSearch != null)
                EndSearch.Invoke(this);
		}

		internal void Found(DhtContact contact, bool proxied)
		{
			FoundContact = contact;

			if( !proxied )
				FinishSearch("Found");

			else
			{
				FoundProxy = true;
                FinishSearch("Found Proxy");
			}
		}

        internal void Found(byte[] value, DhtAddress source)
        {
            foreach (SearchValue found in FoundValues)
                if(Utilities.MemCompare(found.Value, value))
                {
                    found.AddSource(source);
                    return;
                }

            if(FoundValues.Count > TargetResults)
                return;

            FoundValues.Add( new SearchValue(value, source) );
        }

		internal void Log(string message)
		{
            int id = (int)SearchID % 1000;
            string entry = id.ToString() + ":" + Utilities.IDtoBin(TargetID);

            if(ProxyTcp != null)
                entry += "(proxied)";

            entry += ": " + message;

            Network.UpdateLog("Search - " + Name, entry); 
		}

    }

	internal enum LookupStatus {None, Searching, Done};

	internal class DhtLookup
	{
		internal LookupStatus Status;
		internal DhtContact Contact;
		internal int Age;

		internal DhtLookup(DhtContact contact)
		{
			Contact = contact;
			Status  = LookupStatus.None;
		}
	}

    internal class SearchValue
    {
        internal byte[] Value;

        internal List<DhtAddress> Sources = new List<DhtAddress>();

        internal SearchValue(byte[] value, DhtAddress source)
        {
            Value = value;
            Sources.Add(source);
        }

        internal void AddSource(DhtAddress add)
        {
            foreach (DhtAddress source in Sources)
                if( source.Equals(add))
                    return;

            Sources.Add(add);
        }
    }
}