using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Transport;

using RiseOp.Services.Location;


namespace RiseOp.Implementation.Dht
{
    internal delegate void SearchRequestHandler(ulong key, byte[] parameters, List<byte[]> values);


    class DhtSearchControl
    {
        const int MAX_SEARCHES = 5;

        //super-class
        internal OpCore Core;
        internal DhtNetwork Network;
        internal DhtRouting Routing;

        internal List<DhtSearch> Pending = new List<DhtSearch>();
        internal List<DhtSearch> Active = new List<DhtSearch>();

        internal ServiceEvent<SearchRequestHandler> SearchEvent = new ServiceEvent<SearchRequestHandler>();


        internal DhtSearchControl(DhtNetwork network)
        {
            Network = network;
            Core = Network.Core;
            Routing = network.Routing;
        }

        internal void SecondTimer()
        {
            // get active search count
            int searchCount = 0;
            lock (Active)
                foreach (DhtSearch search in Active)
                    if (search.ProxyTcp == null || search.ProxyTcp.Proxy == ProxyType.Server)
                        searchCount++;

  
            // if pending searches
            if (Network.Responsive) // only move from pending to active if network responsive
                while( searchCount < MAX_SEARCHES && Pending.Count > 0)
                {
                    DhtSearch move = Pending[0];
                    searchCount++; // do here to get out of loop

                    if (move.Activate())
                    {
                        move.Log("Active");
                        Active.Add(move);
                        Pending.Remove(move);
                    }
                }

            // pulse active searches
            List<DhtSearch> removeList = new List<DhtSearch>();

            lock (Active)
                foreach (DhtSearch search in Active)
                {
                    if (search.Finished)
                        removeList.Add(search);
                    else
                        search.SecondTimer();
                }

            // remove finished searches
            foreach (DhtSearch search in removeList)
            {
                if (Active.Contains(search))
                    lock (Active)
                        Active.Remove(search);

                if (Pending.Contains(search))
                    lock (Pending)
                        Pending.Remove(search);

                string log = "Finished";

                if (search.FoundValues.Count > 0)
                    log += ", " + search.FoundValues.Count.ToString() + " Values Found";

                if (search.FinishReason != null)
                    log += ", " + search.FinishReason;

                search.Log(log);
            }
        }

        delegate DhtSearch StartHandler(ulong key, string name, ushort component, byte[] parameters, EndSearchHandler endSearch);


        internal DhtSearch Start(ulong key, string name, uint service, uint datatype, byte[] parameters, EndSearchHandler endSearch)
        {
            if (Core.InvokeRequired)
                Debug.Assert(false);

            // transfer componenent does its own duplicate checks
            // also there can exist multiple transfers with with same trar

            if (service != Core.Transfers.ServiceID) 
            {
                foreach (DhtSearch pending in Pending)
                    if (pending.TargetID == key && pending.Service == service && Utilities.MemCompare(parameters, pending.Parameters))
                        return null;

                foreach (DhtSearch active in Active)
                    if (active.TargetID == key && active.Service == service && Utilities.MemCompare(parameters, active.Parameters))
                        return null;
            }

            DhtSearch search = new DhtSearch(this, key, name, service, datatype, endSearch);
            search.Parameters = parameters;

            search.Log("Pending");

            Pending.Add(search);

            return search;
        }

        internal void SendUdpRequest(DhtAddress address, UInt64 targetID, uint searchID, uint service, uint datatype, byte[] parameters)
        {
            SearchReq request = new SearchReq();

            request.Source     = Network.GetLocalSource();
            request.SearchID   = searchID;
            request.TargetID   = targetID;
            request.Service  = service;
            request.DataType = datatype;
            request.Parameters = parameters;

            Network.UdpControl.SendTo(address, request);
        }

        internal void ReceiveRequest(G2ReceivedPacket packet)
        {
            SearchReq request = SearchReq.Decode(Core.Protocol, packet);

            // loopback
            if (request.Source.DhtID == Core.LocalDhtID && request.Source.ClientID == Core.ClientID)
                return;

            
            if (packet.Tcp != null && request.SearchID != 0 )
            {
                // request from blocked node
                if (packet.Tcp.Proxy == ProxyType.ClientBlocked)
                {
                    int proxySearches = 0;
                    lock (Active)
                        foreach (DhtSearch search in Active)
                            if (search.ProxyTcp == packet.Tcp)
                            {
                                proxySearches++;

                                if (request.EndProxySearch && search.SearchID == request.SearchID)
                                {
                                    search.FinishSearch("Proxied node finished search");
                                    return;
                                }
                            }

                    if (proxySearches < MAX_SEARCHES)
                    {
                        DhtSearch search = new DhtSearch(this, request.TargetID, "Proxy", request.Service, request.DataType, null);

                        search.Parameters = request.Parameters;
                        search.ProxyTcp = packet.Tcp;
                        search.SearchID = request.SearchID;
                        search.Activate();
                        Active.Add(search);
                        search.Log("Active - Proxy Search");
                    }
                   
                    // continue processing request and send local results
                }

                // request from proxy server
                if (packet.Tcp.Proxy == ProxyType.Server && request.EndProxySearch)
                {
                    lock (Active)
                        foreach (DhtSearch search in Active)
                            if (search.SearchID == request.SearchID)
                                if( !search.Finished )
                                    search.FinishSearch("Server finished search");
                }
            }

       
            if (request.Source.Firewall == FirewallType.Open)
                Routing.Add(new DhtContact(request.Source, packet.Source.IP, Core.TimeNow));


            // forward to proxied nodes
            foreach (TcpConnect socket in Network.TcpControl.ProxyClients)
                // prevents incoming udp from proxy and being forwarded to same host tcp
                if(socket != packet.Tcp && !(packet.Source.DhtID == socket.DhtID && packet.Source.ClientID == socket.ClientID))
                {
                    request.FromAddress = packet.Source;

                    socket.SendPacket(request);
                }

            // send ack
            SearchAck ack = new SearchAck();
            ack.Source = Network.GetLocalSource();
            ack.SearchID = request.SearchID;
            ack.Service = request.Service;

            // search for connected proxy
            if (Network.TcpControl.ConnectionMap.ContainsKey(request.TargetID))
                ack.Proxied = true;

            if(request.Nodes)
                ack.ContactList = Routing.Find(request.TargetID, 8);


            if (!SearchEvent.Contains(request.Service, request.DataType))
            {
                SendAck(packet, request, ack);
            }

            else
            {

                List<byte[]> results = new List<byte[]>();
                SearchEvent[request.Service, request.DataType].Invoke(request.TargetID, request.Parameters, results);

                // if nothing found, still send ack with closer contacts
                if (results == null || results.Count == 0)
                {
                    if(request.SearchID != 0)
                        SendAck(packet, request, ack);

                    return;
                }

                // if a direct search
                if (request.SearchID == 0)
                {
                    foreach(byte[] value in results)
                        Network.Store.Send_StoreReq(packet.Source, packet.Tcp, new DataReq(null, request.TargetID, request.Service, request.DataType, value));
                    
                    return;
                }

                // else send normal search results
                int totalSize = 0;

                foreach (byte[] data in results)
                {
                    if (data.Length + totalSize > 1200)
                    {
                        SendAck(packet, request, ack);

                        ack.ValueList.Clear();
                        ack.ContactList.Clear(); // dont send twice
                        totalSize = 0;
                    }

                    ack.ValueList.Add(data);
                    totalSize += data.Length;
                }

                if(totalSize > 0)
                    SendAck(packet, request, ack);
            }
        }

        private void SendAck(G2ReceivedPacket packet, SearchReq request, SearchAck ack)
        {
            // if request came in tcp, send back tcp - scenario happens in these situations
                // req u-> open t-> fw ack t-> open u-> remote
                // fw req t-> open ack t-> fw
                // fw1 req t-> open t-> fw2 ack t-> open t-> fw1

            if (packet.Tcp != null)
            {
                ack.ToAddress = packet.Source;
                packet.Tcp.SendPacket(ack);
            }
            else
                Network.UdpControl.SendTo(packet.Source, ack);
        }

        internal void ReceiveAck(G2ReceivedPacket packet)
        {
            SearchAck ack = SearchAck.Decode(Core.Protocol, packet);

            // loopback
            if (ack.Source.DhtID == Core.LocalDhtID && ack.Source.ClientID == Core.ClientID)
                return;

            // if response to crawl
            if (ack.SearchID == 0)
            {
                if (Network.GuiCrawler != null)
                    Network.GuiCrawler.BeginInvoke(Network.GuiCrawler.SearchAck, ack, packet);

                return;
            }

            // crit ackid and ack ip might not match if ack sent through proxy
            if (packet.Tcp == null && ack.Source.Firewall == FirewallType.Open && packet.Source.DhtID == ack.Source.DhtID)
                Routing.Add(new DhtContact(ack.Source, packet.Source.IP, Core.TimeNow));

            foreach (DhtContact contact in ack.ContactList)
                Routing.Add(contact); // function calls back into seach system, adding closer nodes

            // mark searches as done
            lock (Active)
                foreach (DhtSearch search in Active)
                    if (search.SearchID == ack.SearchID)
                    {
                        foreach (DhtLookup lookup in search.LookupList)
                            if (lookup.Contact.DhtID == ack.Source.DhtID && lookup.Contact.ClientID == ack.Source.ClientID)
                                lookup.Status = LookupStatus.Done;

                        if (search.ProxyTcp != null && search.ProxyTcp.Proxy == ProxyType.ClientBlocked)
                        {
                            ack.FromAddress = packet.Source;
                            search.ProxyTcp.SendPacket(ack);
                            return;
                        }

                        foreach (byte[] value in ack.ValueList)
                            search.Found(value, packet.Source);

                        if (ack.Proxied)
                            search.Found(new DhtContact(ack.Source, packet.Source.IP, Core.TimeNow), true);

                        if (!search.Finished && search.FoundValues.Count > search.TargetResults)
                            search.FinishSearch("Max Values Found");
                    }

        }

        internal void Stop(UInt64 id)
        {
            lock (Pending)
                foreach (DhtSearch search in Pending)
                    if (search.TargetID == id)
                    {
                        Pending.Remove(search);
                        break;
                    }

            lock (Active)
                foreach (DhtSearch search in Active)
                    if (search.TargetID == id)
                    {
                        Active.Remove(search);
                        search.Log("Stopped");
                        break;
                    }
        }


        internal void SendDirectRequest(DhtAddress dest, ulong target, uint service, uint datatype, byte[] parameters)
        {
            SearchReq request = new SearchReq();

            request.Source      = Network.GetLocalSource();
            request.TargetID    = target;
            request.Service     = service;
            request.DataType    = datatype;
            request.Parameters  = parameters;
            request.Nodes       = false;

            TcpConnect socket = Network.TcpControl.GetConnection(dest);

            if(socket != null)
                socket.SendPacket(request);

            else if(Core.Firewall == FirewallType.Blocked)
            {
                request.ToAddress = dest;
                Network.TcpControl.SendRandomProxy(request);
            }
            else
                Network.UdpControl.SendTo(dest, request);

            // if remote end has what we need they will send us a store request
        }
    }
}
