using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Dht;


namespace RiseOp.Implementation.Transport
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

		internal List<TcpConnect> SocketList = new List<TcpConnect>();
        internal List<TcpConnect> ProxyServers = new List<TcpConnect>();
        internal List<TcpConnect> ProxyClients = new List<TcpConnect>();
        internal Dictionary<ulong, Dictionary<ushort, TcpConnect>> ConnectionMap = new Dictionary<ulong, Dictionary<ushort, TcpConnect>>();


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

				lock(SocketList)
					foreach(TcpConnect connection in SocketList)
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
			ArrayList deadSockets = new ArrayList();

			lock(SocketList)
				foreach(TcpConnect socket in SocketList)
				{
					socket.SecondTimer();
					
					// only let socket linger in connecting state for 10 secs
					if( socket.State == TcpState.Closed )
						deadSockets.Add(socket);
				}

			foreach(TcpConnect socket in deadSockets)
			{
				string message = "Connection to " + socket.ToString() + " Removed";
				if(socket.ByeMessage != null)
					message += ", Reason: " + socket.ByeMessage;

				Network.UpdateLog("Tcp", message);
								
				socket.TcpSocket = null;

   
                lock (SocketList)
                    SocketList.Remove(socket);

                if (ProxyServers.Contains(socket))
                    ProxyServers.Remove(socket);

                if (ProxyClients.Contains(socket))
                    ProxyClients.Remove(socket);

                if(ConnectionMap.ContainsKey(socket.DhtID))
                    foreach(ushort client in ConnectionMap[socket.DhtID].Keys)
                        if (ConnectionMap[socket.DhtID][client] == socket)
                        {
                            ConnectionMap[socket.DhtID].Remove(client);

                            if (ConnectionMap[socket.DhtID].Count == 0)
                                ConnectionMap.Remove(socket.DhtID);

                            break;
                        }

                ArrayList removeList = new ArrayList();

				// iterate through searches
                lock (Network.Searches.Active)
                    foreach (DhtSearch search in Network.Searches.Active)
						// if proxytcp == connection
						if(search.ProxyTcp != null && search.ProxyTcp == socket)
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

            // it's not until the host sends us traffic that we can send traffic back because we don't know
            // connecting node's dhtID (and hence encryption key) until ping is sent

            lock (SocketList) 
                SocketList.Add(inbound);

            Network.UpdateLog("Tcp", "Accepted Connection from " + inbound.ToString());

            return inbound;
        }

		internal void MakeOutbound( DhtAddress address, ushort tcpPort, string reason)
		{
			try
			{
                int connecting = 0;

                // check if already connected
                foreach (TcpConnect socket in SocketList)
                {
                    if (socket.State == TcpState.Connecting)
                        connecting++;

                    if (socket.State != TcpState.Closed && address.IP.Equals(socket.RemoteIP) && tcpPort == socket.TcpPort)
                        return;
                }

                if (connecting > 6)
                {
                    Debug.Assert(true);
                    return;
                }

                TcpConnect outbound = new TcpConnect(this, address, tcpPort);
				Network.UpdateLog("Tcp", "Attempting Connection to " + address.ToString() + ":" + tcpPort.ToString() + " (" + reason + ")");
				
                lock(SocketList)
                    SocketList.Add(outbound);
			}
			catch(Exception ex)
			{
				Network.UpdateLog("Exception", "TcpHandler::MakeOutbound: " + ex.Message);
			}
		}

		void ConnectProxy()
		{
			// Get cloest contacts and sort by distance to us
			DhtContact attempt = null;
			
			// no Dht contacts, use ip cache will be used to connect tcp/udp in DoBootstrap

			// find if any contacts in list are worth trying (will be skipped if set already)
            foreach (DhtContact contact in Network.Routing.NearXor.Contacts)
            {
                if (ConnectionMap.ContainsKey(contact.DhtID))
                    continue;

                // if havent tried in 10 minutes
                if (Core.TimeNow > contact.NextTryProxy)
                {
                    if (SocketList.Count == 0)
                        attempt = contact;

                    lock (SocketList)
                        foreach (TcpConnect connection in SocketList)
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
					MakeOutbound( attempt.ToDhtAddress(), attempt.TcpPort, "try proxy");
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

			lock(SocketList)
				foreach(TcpConnect connection in SocketList)
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
			lock(SocketList)
				foreach(TcpConnect connection in SocketList)
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

			lock(SocketList)
				foreach(TcpConnect connection in SocketList)
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

            // reconnect
            if (bye.Reconnect && NeedProxies(ProxyType.Server))
                MakeOutbound(packet.Source, packet.Tcp.TcpPort, "Reconnecting");
		}

        internal void SendRandomProxy(G2Packet packet)
        {
            if (ProxyServers.Count == 0)
                return;

            TcpConnect socket = ProxyServers[Core.RndGen.Next(ProxyServers.Count)];

            if(socket != null)
                socket.SendPacket(packet);
        }

        internal void AddConnection(TcpConnect socket)
        {
            //crit check not connected already as well, before we get to here of course

            if (!ConnectionMap.ContainsKey(socket.DhtID))
                ConnectionMap[socket.DhtID] = new Dictionary<ushort, TcpConnect>();

            Debug.Assert(!ConnectionMap[socket.DhtID].ContainsKey(socket.ClientID));

            ConnectionMap[socket.DhtID][socket.ClientID] = socket;

            if (socket.Proxy == ProxyType.Server)
                ProxyServers.Add(socket);
            else
                ProxyClients.Add(socket);
        }

        internal TcpConnect GetConnection(DhtClient ident)
        {
            if (ident == null)
                return null;

            return GetConnection(ident.DhtID, ident.ClientID);
        }

        internal TcpConnect GetConnection(ulong dhtid, ushort client)
        {
            if (ConnectionMap.ContainsKey(dhtid) &&
                ConnectionMap[dhtid].ContainsKey(client) &&
                ConnectionMap[dhtid][client].State == TcpState.Connected &&
                ConnectionMap[dhtid][client].Proxy != ProxyType.Unset)
                return ConnectionMap[dhtid][client];

            return null;
        }
    }
}
