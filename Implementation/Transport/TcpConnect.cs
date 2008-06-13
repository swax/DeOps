using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Security.Cryptography;
using System.Diagnostics;

using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Dht;

using RiseOp.Simulator;


namespace RiseOp.Implementation.Transport
{
	internal enum ProxyType { Unset, Server, ClientBlocked, ClientNAT };
    internal enum TcpState { Connecting, Connected, Closed };

	internal class TcpConnect : DhtSource
	{
		internal OpCore     Core;
        internal DhtNetwork Network;
        internal TcpHandler TcpControl;

		// client info
		internal IPAddress   RemoteIP;
		
		// socket info
		internal Socket	TcpSocket = null;
        internal TcpState State = TcpState.Connecting;
		internal int		Age;
		internal bool		CheckedFirewall;
		internal bool		Outbound;
		internal string	ByeMessage;
	
        
		// bandwidth
		internal int BytesReceivedinSec;
		internal int BytesSentinSec;
		
		int SecondsDead;

		// proxying
		internal ProxyType    Proxy;

        const int BUFF_SIZE = 8 * 1024;

		// sending
		ICryptoTransform Encryptor;
        byte[]   SendBuffer;
		int      SendBuffSize;
        byte[] FinalSendBuffer;
        int    FinalSendBuffSize;

		
        // receiving
		ICryptoTransform Decryptor;
        internal byte[] RecvBuffer;
        internal int    RecvBuffSize;
        internal byte[] FinalRecvBuffer;
        internal int    FinalRecvBuffSize;

        // inbound
        internal TcpConnect(TcpHandler control)
		{
            TcpControl = control;
            Network    = TcpControl.Network;
            Core       = TcpControl.Core;
		}

        // outbound
        internal TcpConnect(TcpHandler control, DhtAddress address, ushort tcpPort)
		{
            TcpControl = control;
            Network = TcpControl.Network;
            Core = TcpControl.Core;

            Outbound = true;

            RemoteIP = address.IP;
            TcpPort  = tcpPort;
            UdpPort  = address.UdpPort;
            UserID    = address.UserID;

            try
            {
                IPEndPoint endpoint = new IPEndPoint(RemoteIP, TcpPort);

                if (Core.Sim != null)
                {
                    Core.Sim.Internet.SendPacket(SimPacketType.TcpConnect, Network, null, endpoint, this);
                    return;
                }

                TcpSocket = new Socket(address.IP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                TcpSocket.BeginConnect((EndPoint)endpoint, new AsyncCallback(Socket_Connect), TcpSocket);
            }
            catch (Exception ex)
            {
                LogException("TcpSocket", ex.Message);
                Disconnect();
            }
		}

		internal void SecondTimer()
		{
            if (State == TcpState.Closed)
                return;

			// update bandwidth
			SecondsDead = (BytesReceivedinSec > 0) ? 0 : SecondsDead + 1;

			BytesSentinSec     = 0;
			BytesReceivedinSec = 0;

            if (Age < 60)
                Age++;

			// if proxy not set after 10 secs disconnect
			if(Age > 10 && Proxy == ProxyType.Unset)
			{
                if (State == TcpState.Connecting)
                    CleanClose("Timed out");
                else
				    CleanClose("No proxy request");

				return;
			}

            // replicate
            if (Age == 15 && !Network.IsGlobal)
            {
                Network.Store.Replicate(new DhtContact(this, RemoteIP));
            }

            // new global proxy
            if (Proxy == ProxyType.Server)
            {
                if (Age == 5)
                {
                    // announce to rudp connections new proxy if blocked/nat, or using a global proxy
                    if (Network.IsGlobal)
                    {
                        Core.Context.Cores.LockReading(delegate()
                        {
                            foreach (OpCore core in Core.Context.Cores)
                                if (core.Network.UseGlobalProxies)
                                    core.Network.RudpControl.AnnounceProxy(this);
                        });
                    }
                    else
                        Network.RudpControl.AnnounceProxy(this);

                }
                else if (Age == 15)
                {
                    if (!Network.IsGlobal)
                        Core.Locations.UpdateLocation();

                    if (Network.UseGlobalProxies)
                        Core.Locations.PublishGlobal();
                }
            }
            // new proxied host
            else if(Age == 15)
            {
                // proxied replicates to server naturally by adding server to routing table
                // server replicates to proxy here
                Network.Store.Replicate(new DhtContact(this, RemoteIP));
            }

			// send ping if dead for x secs
            if (SecondsDead > 30 && SecondsDead % 5 == 0)
			{
				SendPacket(new Ping());
			}
			else if(SecondsDead > 60)
			{
				CleanClose("Minute dead");
				return;
			}

            // flush send buffer
            TrySend();
		}

		internal void Socket_Connect(IAsyncResult asyncResult)
		{
			try
			{
				TcpSocket.EndConnect(asyncResult);

                OnConnect();
			}
			catch(Exception ex)
			{
				LogException("Socket_Connect", ex.Message);
				Disconnect();
			}
		}

        internal void OnConnect()
        {
            Network.UpdateLog("Tcp", "Connected to " + ToString());

            SetConnected();

            if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
                CreateEncryptor();

            Ping ping = new Ping();
            ping.Source = Network.GetLocalSource();
            ping.RemoteIP = RemoteIP;

            Core.RunInCoreAsync(delegate() 
            {
                SendPacket(ping);

                // if we made connection to the node its not firewalled
                if (Outbound)
                    Network.Routing.Add(new DhtContact(this, RemoteIP));
            });
        }

        private void CreateEncryptor()
        {
            lock (Network.AugmentedCrypt)
            {
                Network.AugmentedCrypt.GenerateIV();
                Network.AugmentedCrypt.Padding = PaddingMode.None;
                BitConverter.GetBytes(UserID).CopyTo(Network.AugmentedCrypt.Key, 0);

                Encryptor = Network.AugmentedCrypt.CreateEncryptor();

                Network.AugmentedCrypt.IV.CopyTo(FinalSendBuffer, 0);
                FinalSendBuffSize = Network.AugmentedCrypt.IV.Length;
            }
        }

		internal void SetConnected()
		{
			SendBuffer    = new byte[BUFF_SIZE];
            RecvBuffer = new byte[BUFF_SIZE];
            FinalRecvBuffer = new byte[BUFF_SIZE];
            FinalSendBuffer = new byte[BUFF_SIZE];

            State = TcpState.Connected;

            if (Core.Sim != null)
                return;

			try
			{
				TcpSocket.BeginReceive( RecvBuffer, RecvBuffSize, RecvBuffer.Length, SocketFlags.None, new AsyncCallback(Socket_Receive), TcpSocket);
			}
			catch(Exception ex)
			{
				LogException("SetConnected", ex.Message);
				Disconnect();
			}
		}

        internal void CleanClose(string reason)
        {
            CleanClose(reason, false);
        }

		internal void CleanClose(string reason, bool reconnect)
		{
            if (State == TcpState.Connecting)
                ByeMessage = reason;

            if (State == TcpState.Connected)
            {
                ByeMessage = reason;

                Bye bye = new Bye();

                bye.SenderID = Network.Local.UserID;
                bye.ContactList = Network.Routing.Find(UserID, 8);
                bye.Message = reason;
                bye.Reconnect = reconnect;

                SendPacket(bye);

                Network.UpdateLog("Tcp", "Closing connection to " + ToString() + " " + reason);
            }

            State = TcpState.Closed;
		}

		internal void Disconnect()
		{
            if (State != TcpState.Closed)
            {
                try
                {
                    if (Core.Sim == null)
                        TcpSocket.Close();
                    else
                        Core.Sim.Internet.SendPacket(SimPacketType.TcpClose, Network, null, new IPEndPoint(RemoteIP, TcpPort), this);
                }
                catch (Exception ex)
                {
                    LogException("Disconnect", ex.Message);
                }
            }

            State = TcpState.Closed; 
        }

		internal void SendPacket(G2Packet packet)
		{
            if (Core.InvokeRequired)
                Debug.Assert(false);

            if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
                if(Encryptor == null)
                    CreateEncryptor();

			try
			{
                if (packet is NetworkPacket)
                {
                    ((NetworkPacket)packet).SourceID = Network.Local.UserID;
                    ((NetworkPacket)packet).ClientID = Network.Local.ClientID;
                }

				byte[] encoded = packet.Encode(Network.Protocol);
                PacketLogEntry logEntry = new PacketLogEntry(TransportProtocol.Tcp, DirectionType.Out, new DhtAddress(RemoteIP, this), encoded);
                Network.LogPacket(logEntry);

                lock(FinalSendBuffer)
                {
                    // fill up final buffer, keep encrypt buffer clear
                    if (BUFF_SIZE - FinalSendBuffSize < encoded.Length + 128)
                        throw new Exception("SendBuff Full"); //crit check packet log

                    // encrypt, turn off encryption during simulation
                    if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
                    {
                        encoded.CopyTo(SendBuffer, SendBuffSize);
                        SendBuffSize += encoded.Length;
                        
                        int remainder = SendBuffSize % Encryptor.InputBlockSize;
                        if (remainder > 0)
                        {
                            CryptPadding padding = new CryptPadding();

                            int fillerNeeded = Encryptor.InputBlockSize - remainder;

                            if (fillerNeeded > 2)
                                padding.Filler = new byte[fillerNeeded - 2];

                            encoded = padding.Encode(Network.Protocol);
                            encoded.CopyTo(SendBuffer, SendBuffSize);
                            SendBuffSize += encoded.Length;
                        }

                        int tryTransform = SendBuffSize - (SendBuffSize % Encryptor.InputBlockSize);
                        if (tryTransform == 0)
                            return;

                        int tranformed = Encryptor.TransformBlock(SendBuffer, 0, tryTransform, FinalSendBuffer, FinalSendBuffSize);
                        if (tranformed == 0)
                            return;

                        FinalSendBuffSize += tranformed;
                        SendBuffSize -= tranformed;
                        Buffer.BlockCopy(SendBuffer, tranformed, SendBuffer, 0, SendBuffSize);
                    }
                    else
                    {
                        encoded.CopyTo(FinalSendBuffer, FinalSendBuffSize);
                        FinalSendBuffSize += encoded.Length;
                    }
                }
                   
                TrySend();
               
			}
			catch(Exception ex)
			{
				LogException("SendPacket", ex.Message);
			}
		}

		internal void TrySend()
		{
            if (FinalSendBuffSize == 0)
                return;

			try
			{
                lock (FinalSendBuffer)
                {
                    int bytesSent = 0;

                    //Core.UpdateConsole("Begin Send " + SendBufferSize.ToString());

                    if (Core.Sim == null)
                    {
                        TcpSocket.Blocking = false;
                        bytesSent = TcpSocket.Send(FinalSendBuffer, FinalSendBuffSize, SocketFlags.None);
                    }
                    else
                    {
                        bytesSent = Core.Sim.Internet.SendPacket(SimPacketType.Tcp, Network, Utilities.ExtractBytes(FinalSendBuffer, 0, FinalSendBuffSize), new IPEndPoint(RemoteIP, TcpPort), this);

                        if (bytesSent < 0) // simulator tcp disconnected
                        {
                            LogException("TrySend", "Disconnected");
                            Disconnect();
                            return;
                        }
                    }

                    if (bytesSent > 0)
                    {
                        FinalSendBuffSize -= bytesSent;
                        BytesSentinSec += bytesSent;

                        if (FinalSendBuffSize < 0)
                            throw new Exception("Tcp SendBuff size less than zero");

                        // realign send buffer
                        if (FinalSendBuffSize > 0)
                            lock (FinalSendBuffer)
                                Buffer.BlockCopy(FinalSendBuffer, bytesSent, FinalSendBuffer, 0, FinalSendBuffSize);
                    }
                }
			}

			catch(Exception ex)
			{ 
				LogException("TrySend", ex.Message);
				Disconnect();
			}
		}


		void Socket_Receive(IAsyncResult asyncResult)
		{
			try
			{
				int recvLength = TcpSocket.EndReceive(asyncResult);
                //Core.UpdateConsole(recvLength.ToString() + " received");

                if (recvLength <= 0)
                {
                    Disconnect();
                    return;
                }

                OnReceive(recvLength);
               
				if(State == TcpState.Connected)
                    TcpSocket.BeginReceive(RecvBuffer, RecvBuffSize, RecvBuffer.Length, SocketFlags.None, new AsyncCallback(Socket_Receive), TcpSocket);
			}
			catch(Exception ex)
			{ 
				LogException("Socket_Receive", ex.Message);
                Disconnect();
			}	
		}

        internal void OnReceive(int length)
        {
            if (State != TcpState.Connected)
                return;

            if (length <= 0)
            {
                Disconnect();
                return;
            }
            
            BytesReceivedinSec += length;
            RecvBuffSize += length;  

            // transfer to final recv buffer
            if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
            {
                //create decryptor
                if (Decryptor == null)
                {
                    int ivlen = Network.AugmentedCrypt.IV.Length;

                    if (RecvBuffSize < ivlen)
                        return;

                    lock (Network.AugmentedCrypt)
                    {
                        Network.AugmentedCrypt.IV = Utilities.ExtractBytes(RecvBuffer, 0, ivlen);
                        Network.AugmentedCrypt.Padding = PaddingMode.None;
                        BitConverter.GetBytes(Network.Local.UserID).CopyTo(Network.AugmentedCrypt.Key, 0);

                        Decryptor = Network.AugmentedCrypt.CreateDecryptor();
                    }

                    RecvBuffSize -= ivlen;

                    if (RecvBuffSize == 0)
                        return;
                        
                    Buffer.BlockCopy(RecvBuffer, ivlen, RecvBuffer, 0, RecvBuffSize);      
                }

                // decrypt
                int tryTransform = RecvBuffSize - (RecvBuffSize % Decryptor.InputBlockSize);
                if (tryTransform == 0)
                    return;

                int transformed = Decryptor.TransformBlock(RecvBuffer, 0, tryTransform, FinalRecvBuffer, FinalRecvBuffSize);
                if (transformed == 0)
                    return;

                FinalRecvBuffSize += transformed;
                RecvBuffSize -= transformed;
                Buffer.BlockCopy(RecvBuffer, transformed, RecvBuffer, 0, RecvBuffSize);      
            }
            else
            {
                int copysize = RecvBuffSize;
                if (FinalRecvBuffSize + RecvBuffSize > FinalRecvBuffer.Length)
                    copysize = FinalRecvBuffer.Length - FinalRecvBuffSize;

                Buffer.BlockCopy(RecvBuffer, 0, FinalRecvBuffer, FinalRecvBuffSize, copysize);
                FinalRecvBuffSize += copysize;
                RecvBuffSize -= copysize;

                if (RecvBuffSize > 0)
                    Buffer.BlockCopy(RecvBuffer, copysize, RecvBuffer, 0, RecvBuffSize);
            }
            
            ReceivePackets();
        }

		void ReceivePackets()
		{
			int Start  = 0;
			G2ReadResult streamStatus = G2ReadResult.PACKET_GOOD;

			while(streamStatus == G2ReadResult.PACKET_GOOD)
			{
				G2ReceivedPacket packet = new G2ReceivedPacket();
				packet.Root = new G2Header(FinalRecvBuffer);

                streamStatus = G2Protocol.ReadNextPacket(packet.Root, ref Start, ref FinalRecvBuffSize);

				if( streamStatus != G2ReadResult.PACKET_GOOD )
					break;

				packet.Tcp       = this;
				packet.Source = new DhtContact(this, RemoteIP);

                byte[] packetData = Utilities.ExtractBytes(packet.Root.Data, packet.Root.PacketPos, packet.Root.PacketSize);
                PacketLogEntry logEntry = new PacketLogEntry(TransportProtocol.Tcp, DirectionType.In, packet.Source, packetData);
                Network.LogPacket(logEntry);

                Network.IncomingPacket(packet);
			}

            // re-align buffer
            if (Start > 0 && FinalRecvBuffSize > 0)
			{
                Buffer.BlockCopy(FinalRecvBuffer, Start, FinalRecvBuffer, 0, FinalRecvBuffSize);
				//Network.UpdateConsole(PacketBytesReady.ToString() + " bytes moved to front of receive buffer");
			}
		}

		void LogException(string where, string message)
		{
            Network.UpdateLog("Exception", "TcpConnect(" + ToString() + ")::" + where + ": " + message);
		}

		internal DhtContact GetContact()
		{
			return new DhtContact(UserID, ClientID, RemoteIP, TcpPort, UdpPort);
		}

        public override string ToString()
		{
			return RemoteIP.ToString() + ":" + TcpPort.ToString();
		}

        // the simulator uses tcpConnects in a dictionay, this prevents it from using the base hash and overlapping
        // tcpConnect instances
        object UniqueIdentifier = new object();

        public override int GetHashCode()
        {
            return UniqueIdentifier.GetHashCode();
        }
	}
}
