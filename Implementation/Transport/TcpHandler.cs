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
using System.Net.Sockets;

using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Dht;


namespace DeOps.Implementation.Transport
{
	/// <summary>
	/// Summary description for TcpHandler.
	/// </summary>
	internal class TcpHandler
	{
        // super-classes
		internal OpCore Core; 
        internal DhtNetwork Network;
		
		Socket ListenSocket;
		
		internal ushort ListenPort;

		internal List<TcpConnect> Connections = new List<TcpConnect>();
        internal Dictionary<ulong, TcpConnect> ConnectionMap = new Dictionary<ulong, TcpConnect>();

		DateTime LastProxyCheck = new DateTime(0);

		int MaxProxyServers = 2;
		int MaxProxyNATs    = 12;
		int MaxProxyBlocked = 6;


        internal TcpHandler(DhtNetwork network)
		{
            Network = network;
            Core = Network.Core;

            ListenPort = Network.IsGlobal ? Core.User.Settings.GlobalPortTcp : Core.User.Settings.OpPortTcp;

            if (Core.Sim != null)
                return;

			ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			bool bound    = false;
			int  attempts = 0;
			while( !bound && attempts < 5)
			{
				try
				{
					ListenSocket.Bind( new IPEndPoint( System.Net.IPAddress.Any, ListenPort) );
					bound = true;
					
					ListenSocket.Listen(10);
					ListenSocket.BeginAccept(new AsyncCallback(ListenSocket_Accept), ListenSocket);
					
					Network.UpdateLog("Core", "Listening for TCP on port " + ListenPort.ToString());
				}
				catch(Exception ex)
				{ 
					Network.UpdateLog("Exception", "TcpHandler::TcpHandler: " + ex.Message);

					attempts++; 
					ListenPort++;
				}
			}
		}

		internal void Shutdown()
		{
			try
			{
				Socket oldSocket = ListenSocket; // do this to prevent listen exception
				ListenSocket = null;
				
				if(oldSocket != null)
					oldSocket.Close();

				lock(Connections)
					foreach(TcpConnect connection in Connections)
						connection.CleanClose("Client shutting down");
			}
			catch(Exception ex)
			{
				Network.UpdateLog("Exception", "TcpHandler::Shudown: " + ex.Message);
			}
		}

		internal void SecondTimer()
		{
			// if firewalled find closer proxy
			if(Core.Firewall != FirewallType.Open)
				if(NeedProxies(ProxyType.Server) || Core.TimeNow > LastProxyCheck.AddSeconds(30))
				{
					ConnectProxy();
                    LastProxyCheck = Core.TimeNow;
				}

			// Run through socket connections
			ArrayList DeadSockets = new ArrayList();

			lock(Connections)
				foreach(TcpConnect connection in Connections)
				{
					connection.SecondTimer();
					
					// only let socket linger in connecting state for 10 secs
					if( connection.State == TcpState.Closed )
						DeadSockets.Add(connection);
				}

			foreach(TcpConnect connection in DeadSockets)
			{
				string message = "Connection to " + connection.ToString() + " Removed";
				if(connection.ByeMessage != null)
					message += ", Reason: " + connection.ByeMessage;

				Network.UpdateLog("Tcp", message);
				
				
				
				connection.TcpSocket = null;

                //crit problems with multiple clients
                // especially with firewall tests, multiple connections from same client id

                lock (Connections)
                {
                    Connections.Remove(connection);

                    if(ConnectionMap.ContainsKey(connection.DhtID))
					    ConnectionMap.Remove(connection.DhtID);

                    foreach (TcpConnect connect in Connections)
                        ConnectionMap[connect.DhtID] = connect;
                    
				}

                ArrayList removeList = new ArrayList();

				// iterate through searches
                lock (Network.Searches.Active)
                    foreach (DhtSearch search in Network.Searches.Active)
						// if proxytcp == connection
						if(search.ProxyTcp != null && search.ProxyTcp == connection)
						{
							// if proxytcp == client blocked kill search
							if(search.ProxyTcp.Proxy == ProxyType.ClientBlocked)
								search.FinishSearch("Proxied client disconnected");
							
							// else if proxy type is server add back to pending proxy list
							if(search.ProxyTcp.Proxy == ProxyType.Server)
							{
								removeList.Add(search);
                                Network.Searches.Pending.Add(search);
								search.Log("Back to Pending, TCP Disconnected");
							}

							search.ProxyTcp = null;
						}

                lock (Network.Searches.Active)
					foreach(DhtSearch search in removeList)
                        Network.Searches.Active.Remove(search);
			}
		}

		void ListenSocket_Accept(IAsyncResult asyncResult)
		{
			if(ListenSocket == null)
				return;

			try
			{
				Socket tempSocket = ListenSocket.EndAccept(asyncResult); // do first to catch

                OnAccept(tempSocket, (IPEndPoint) tempSocket.RemoteEndPoint);
			}
			catch(Exception ex)
			{
				Network.UpdateLog("Exception", "TcpHandler::ListenSocket_Accept:1: " + ex.Message);
			}

			// exception handling not combined because endreceive can fail legit, still need begin receive to run
			try
			{
				ListenSocket.BeginAccept(new AsyncCallback(ListenSocket_Accept), ListenSocket);
			}
			catch(Exception ex)
			{
				Network.UpdateLog("Exception", "TcpHandler::ListenSocket_Accept:2: " + ex.Message);
			}
		}

        internal TcpConnect OnAccept(Socket socket, IPEndPoint source)
        {
            TcpConnect inbound = new TcpConnect(this);

            inbound.TcpSocket = socket;
            inbound.RemoteIP = source.Address;
            inbound.TcpPort = (ushort)source.Port;  // zero if internet, actual value if sim
            inbound.SetConnected();

            lock (Connections) 
                Connections.Add(inbound);

            Network.UpdateLog("Tcp", "Accepted Connection from " + inbound.ToString());

            return inbound;
        }

		internal void MakeOutbound( DhtAddress address, ushort tcpPort)
		{
			try
			{
                TcpConnect outbound = new TcpConnect(this, address, tcpPort);
				Network.UpdateLog("Tcp", "Attempting Connection to " + address.ToString() + ":" + tcpPort.ToString());
				
                lock(Connections)
                    Connections.Add(outbound);
			}
			catch(Exception ex)
			{
				Network.UpdateLog("Exception", "TcpHandler::MakeOutbound: " + ex.Message);
			}
		}

		void ConnectProxy()
		{
			// Get cloest contacts and sort by distance to us
            List<DhtContact> contacts = Network.Routing.Find(Core.LocalDhtID, 8);

			DhtContact attempt = null;
			
			// no Dht contacts, use ip cache will be used to connect tcp/udp in DoBootstrap

			// find if any contacts in list are worth trying (will be skipped if set already)
            foreach (DhtContact contact in contacts)
            {
                if (ConnectionMap.ContainsKey(contact.DhtID))
                    continue;

                // if havent tried in 10 minutes
                if (Core.TimeNow > contact.NextTryProxy)
                {
                    if (Connections.Count == 0)
                        attempt = contact;

                    lock (Connections)
                        foreach (TcpConnect connection in Connections)
                            if (connection.Proxy == ProxyType.Server && contact.DhtID != connection.DhtID)
                                // if closer than at least 1 contact
                                if ((contact.DhtID ^ Core.LocalDhtID) < (connection.DhtID ^ Core.LocalDhtID))
                                {
                                    attempt = contact;
                                    break;
                                }

                    if (attempt != null)
                        break;
                }
            }

			if(attempt != null)
			{
				// take into account when making proxy request, disconnct furthest
				if(Core.Firewall == FirewallType.Blocked)
				{
					// continue attempted to test nat with pings which are small
                    Network.Send_Ping(attempt.ToDhtAddress());
					MakeOutbound( attempt.ToDhtAddress(), attempt.TcpPort);
				}

				// if natted do udp proxy request first before connect
				else if(Core.Firewall == FirewallType.NAT)
				{
					ProxyReq request = new ProxyReq();
                    request.SenderID = Core.LocalDhtID;
					request.Type     = ProxyType.ClientNAT;
					Network.UdpControl.SendTo( attempt.ToDhtAddress(), request);
				}

                attempt.NextTryProxy = Core.TimeNow.AddMinutes(10);
			}
		}

		internal bool NeedProxies(ProxyType type)
		{
			int count = 0;

			lock(Connections)
				foreach(TcpConnect connection in Connections)
					if(connection.Proxy == type)
						count++;

			// count of proxy servers connected to, (we are firewalled)
			if(type == ProxyType.Server && count < MaxProxyServers)
				return true;

			// count of proxy clients connected to, (we are open)
			if(type == ProxyType.ClientBlocked && count < MaxProxyBlocked)
					return true;

			if(type == ProxyType.ClientNAT && count < MaxProxyNATs)
				return true;

		
			return false;
		}

		internal bool ProxiesMaxed()
		{
			// we're not firewalled
			if( Core.Firewall == FirewallType.Open)
			{
				if( NeedProxies(ProxyType.ClientBlocked) )
					return false;

				if( NeedProxies(ProxyType.ClientNAT) )
					return false;
			}

			// we are firewalled
			else
			{
				if( NeedProxies(ProxyType.Server) )
					return false;
			}	

			return true;
		}

		internal bool AcceptProxy(ProxyType type, UInt64 targetID)
		{
			if( NeedProxies(type) )
				return true;

			// else go through proxies, determine if targetid is closer than proxies already hosted
			lock(Connections)
				foreach(TcpConnect connection in Connections)
					if(connection.Proxy == type)
						// if closer than at least 1 contact
                        if ((targetID ^ Core.LocalDhtID) < (connection.DhtID ^ Core.LocalDhtID) || targetID == connection.DhtID)
						{
							return true;
						}

			return false;
		}

		internal void CheckProxies()
		{
			CheckProxies(ProxyType.Server,        MaxProxyServers);
			CheckProxies(ProxyType.ClientNAT,     MaxProxyNATs);
			CheckProxies(ProxyType.ClientBlocked, MaxProxyBlocked);
		}

		void CheckProxies(ProxyType type, int max)
		{
			TcpConnect furthest = null;
			UInt64     distance = 0;
			int        count    = 0;

			lock(Connections)
				foreach(TcpConnect connection in Connections)
                    if (connection.Proxy == type)
                    {
                        count++;

                        if ((connection.DhtID ^ Core.LocalDhtID) > distance)
                        {
                            distance = connection.DhtID ^ Core.LocalDhtID;
                            furthest = connection;
                        }
                    }

			// greater than max, disconnect furthest
			if(count > max && furthest != null)
				furthest.CleanClose("Too many proxies, disconnecting furthest");
		}

		internal void Receive_Bye(G2ReceivedPacket packet)
		{
			Bye bye = Bye.Decode(Core.Protocol, packet);

			foreach(DhtContact contact in bye.ContactList)
                Network.Routing.Add(contact);

			string message = (bye.Message != null) ? bye.Message : "";
			
			packet.Tcp.ByeMessage = "Remote: " + message;

			Network.UpdateLog("Tcp", "Bye Received from " + packet.Tcp.ToString() + " " + message);
			
			// close connection
			packet.Tcp.Disconnect();
		}

        internal void ProxyPacket(ulong dhtID, G2Packet packet)
        {
            if (Network.TcpControl.Connections.Count == 0)
                return;

            TcpConnect tcp = null;

            if (ConnectionMap.ContainsKey(dhtID))
                tcp = ConnectionMap[dhtID];
            else
                tcp = Connections[Core.RndGen.Next(Connections.Count)];
           
            if (tcp.Proxy != ProxyType.Server)
                return; // should only be proxying packets if we're in blocked mode

            tcp.SendPacket(packet);
        }
    }
}
