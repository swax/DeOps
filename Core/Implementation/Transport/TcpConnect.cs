using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Security.Cryptography;
using System.Diagnostics;

using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Dht;

using DeOps.Simulator;


namespace DeOps.Implementation.Transport
{
	public enum ProxyType { Unset, Server, ClientBlocked, ClientNAT };
    public enum TcpState { Connecting, Connected, Closed };

	public class TcpConnect : DhtSource
	{
		public OpCore     Core;
        public DhtNetwork Network;
        public TcpHandler TcpControl;

		// client info
		public IPAddress   RemoteIP;
		
		// socket info
		public Socket	TcpSocket = null;
        public TcpState State = TcpState.Connecting;
		public int		Age;
		public bool		CheckedFirewall;
		public bool		Outbound;
		public string	ByeMessage;
	
        
		// bandwidth
		public int BytesReceivedinSec;
		public int BytesSentinSec;
		
		int SecondsDead;

		// proxying
		public ProxyType    Proxy;

        const int BUFF_SIZE = 16 * 1024;

		// sending
		ICryptoTransform Encryptor;
        byte[]   SendBuffer;
		int      SendBuffSize;
        byte[] FinalSendBuffer;
        int    FinalSendBuffSize;

		
        // receiving
		ICryptoTransform Decryptor;
        public byte[] RecvBuffer;
        public int    RecvBuffSize;
        public byte[] FinalRecvBuffer;
        public int    FinalRecvBuffSize;

        // bandwidth
        public BandwidthLog Bandwidth;

        int TotalIn; //crit - delete


        // inbound
        public TcpConnect(TcpHandler control)
		{
            TcpControl = control;
            Network    = TcpControl.Network;
            Core       = TcpControl.Core;

            Bandwidth = new BandwidthLog(Core.RecordBandwidthSeconds);
		}

        // outbound
        public TcpConnect(TcpHandler control, DhtAddress address, ushort tcpPort)
		{
            Debug.Assert(address.UserID != 0);

            TcpControl = control;
            Network = TcpControl.Network;
            Core = TcpControl.Core;

            Bandwidth = new BandwidthLog(Core.RecordBandwidthSeconds);

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

		public void SecondTimer()
		{
            if (State == TcpState.Closed)
                return;

			// update bandwidth
			SecondsDead = (BytesReceivedinSec > 0) ? 0 : SecondsDead + 1;

			BytesSentinSec     = 0;
			BytesReceivedinSec = 0;

            Core.Context.Bandwidth.InPerSec += Bandwidth.InPerSec;
            Core.Context.Bandwidth.OutPerSec += Bandwidth.OutPerSec;
            Bandwidth.NextSecond();

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
            if (Age == 15 && !Network.IsLookup)
            {
                Network.Store.Replicate(new DhtContact(this, RemoteIP));
            }

            // new global proxy
            if (Proxy == ProxyType.Server)
            {
                if (Age == 5)
                {
                    // announce to rudp connections new proxy if blocked/nat, or using a global proxy
                    if (Network.IsLookup)
                    {
                        Core.Context.Cores.LockReading(delegate()
                        {
                            foreach (OpCore core in Core.Context.Cores)
                                if (core.Network.UseLookupProxies)
                                    core.Network.RudpControl.AnnounceProxy(this);
                        });
                    }
                    else
                        Network.RudpControl.AnnounceProxy(this);

                }
                else if (Age == 15)
                {
                    if (!Network.IsLookup)
                        Core.Locations.UpdateLocation();

                    if (Network.UseLookupProxies)
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

		public void Socket_Connect(IAsyncResult asyncResult)
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

        public void OnConnect()
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
            RijndaelManaged crypt = new RijndaelManaged();
            crypt.Key = Network.GetAugmentedKey(UserID);
            crypt.Padding = PaddingMode.None;

            if (UserID == Network.Local.UserID)
                Debug.Assert(Utilities.MemCompare(crypt.Key, Network.LocalAugmentedKey));

            Encryptor = crypt.CreateEncryptor();

            crypt.IV.CopyTo(FinalSendBuffer, 0);
            FinalSendBuffSize = crypt.IV.Length;
        }

		public void SetConnected()
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

        public void CleanClose(string reason)
        {
            CleanClose(reason, false);
        }

		public void CleanClose(string reason, bool reconnect)
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

		public void Disconnect()
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

		public int SendPacket(G2Packet packet)
		{
            if (Core.InvokeRequired)
                Debug.Assert(false);

            if (State != TcpState.Connected)
                return 0;

            // usually when an inbound connection (dont know remote userId) is determined to be a loopback, we close the connection
            // even before the userId is set, if the userId is not set then the encryptor cant be init'd to send the 'close' packet
            if (UserID == 0)
                return 0;

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
                PacketLogEntry logEntry = new PacketLogEntry(Core.TimeNow, TransportProtocol.Tcp, DirectionType.Out, new DhtAddress(RemoteIP, this), encoded);
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
                            return 0;

                        int tranformed = Encryptor.TransformBlock(SendBuffer, 0, tryTransform, FinalSendBuffer, FinalSendBuffSize);
                        if (tranformed == 0)
                            return 0;

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
            
                // record bandwidth
                return encoded.Length;
			}
			catch(Exception ex)
			{
				LogException("SendPacket", ex.Message);
			}

            return 0;
		}

		public void TrySend()
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

                        Bandwidth.OutPerSec += bytesSent;

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
            }
            catch (Exception ex)
            {
                LogException("Socket_Receive:1", ex.Message);
                Disconnect();
            }

            try
            {
                if (State == TcpState.Connected)
                    TcpSocket.BeginReceive(RecvBuffer, RecvBuffSize, RecvBuffer.Length, SocketFlags.None, new AsyncCallback(Socket_Receive), TcpSocket);
            }
            catch (Exception ex)
            {
                LogException("Socket_Receive:2", ex.Message);
                Disconnect();
            }
        }

        public void OnReceive(int length)
        {
            if (State != TcpState.Connected)
                return;

            if (length <= 0)
            {
                Disconnect();
                return;
            }
            TotalIn += length;

            Bandwidth.InPerSec += length;
            BytesReceivedinSec += length;
            RecvBuffSize += length;  

            // transfer to final recv buffer
            if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
            {
                //create decryptor
                if (Decryptor == null)
                {
                    int ivlen = 16;

                    if (RecvBuffSize < ivlen)
                        return;

                    RijndaelManaged crypt = new RijndaelManaged();
                    crypt.Key = Network.LocalAugmentedKey;
                    crypt.IV = Utilities.ExtractBytes(RecvBuffer, 0, ivlen);
                    crypt.Padding = PaddingMode.None;

                    Decryptor = crypt.CreateDecryptor();
                
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

        G2ReceivedPacket LastPacket; //crit delete

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
               
                // extract data from final recv buffer so it can be referenced without being overwritten by this thread
                byte[] extracted = Utilities.ExtractBytes(packet.Root.Data, packet.Root.PacketPos, packet.Root.PacketSize);
                packet.Root = new G2Header(extracted);
                G2Protocol.ReadPacket(packet.Root);

                LastPacket = packet;

                PacketLogEntry logEntry = new PacketLogEntry(Core.TimeNow, TransportProtocol.Tcp, DirectionType.In, packet.Source, packet.Root.Data);
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

		public DhtContact GetContact()
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
