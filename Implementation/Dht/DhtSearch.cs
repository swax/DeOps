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
			// bootstrap search from routing
			foreach(DhtContact contact in Network.Routing.Find(TargetID, 8))
				Add(contact);

            List<TcpConnect> sockets = null;

            // if open send search to proxied nodes just for good measure, probably helps on very small networks
            if (Core.Firewall == FirewallType.Open)
                sockets = Network.TcpControl.ProxyClients;

            // if natted send request to proxies for fresh nodes
            if(Core.Firewall == FirewallType.NAT)
                sockets = Network.TcpControl.ProxyServers;

            if(sockets != null)
                foreach (TcpConnect socket in sockets)
                {
                    DhtAddress address = new DhtAddress(socket.userID, socket.ClientID, socket.RemoteIP, socket.UdpPort);
                    Searches.SendDirectRequest(address, TargetID, Service, DataType, Parameters);

                    DhtLookup host = Add(socket.GetContact());
                    if (host != null)
                        host.Status = LookupStatus.Done;
                }					

			// if blocked send proxy search request to 1 proxy, record and wait
            if (Core.Firewall == FirewallType.Blocked)
			{
				// pick random proxy server
                if (Network.TcpControl.ProxyServers.Count == 0)
					return false;

                ProxyTcp = Network.TcpControl.ProxyServers[Core.RndGen.Next(Network.TcpControl.ProxyServers.Count)];

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


        internal DhtLookup Add(DhtContact contact)
		{
            DhtLookup added = null;

            if (contact.userID == Network.LocalUserID && contact.ClientID == Network.ClientID)
                return null;

			if(Finished) // search over
                return null;

			// go through lookup list, add if closer to target
			foreach(DhtLookup lookup in LookupList)
			{	
				if(contact.userID == lookup.Contact.userID && contact.ClientID == lookup.Contact.ClientID)
					return lookup;

				if((contact.userID ^ TargetID) < (lookup.Contact.userID ^ TargetID))
				{
                    added = new DhtLookup(contact);
                    LookupList.Insert(LookupList.IndexOf(lookup), added);
					break;
				}
			}

            if (added == null)
            {
                added = new DhtLookup(contact);
                LookupList.Add(added);
            }

            while (LookupList.Count > LOOKUP_SIZE)
				LookupList.RemoveAt(LookupList.Count - 1);
		
	
			// at end so we ensure this node is put into list and sent with proxy results
            if (Service == Core.DhtServiceID && contact.userID == TargetID)
				Found(contact, false);

            return added;
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

            foreach (DhtLookup lookup in LookupList)
            {
                if (lookup.Status == LookupStatus.Done)
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