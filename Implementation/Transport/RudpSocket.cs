using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using RiseOp.Services.Location;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Comm;
using RiseOp.Implementation.Protocol.Net;


namespace RiseOp.Implementation.Transport
{
    internal enum RudpState {Connecting, Connected, Closed};
	
	internal enum CloseReason
	{
		NORMAL_CLOSE	 = 0x0,
		YOU_CLOSED		 = 0x1,
		TIMEOUT			 = 0x2,
		LARGE_PACKET	 = 0x3,
		TOO_MANY_RESENDS = 0x4
	};

    internal class RudpSocket
    {
        OpCore Core;
        DhtNetwork Network;
        RudpSession Session;

		// properties
        internal RudpAddress PrimaryAddress;
        internal Dictionary<int, RudpAddress> AddressMap = new Dictionary<int, RudpAddress>();
        internal ushort    PeerID;
		internal ushort    RemotePeerID;

		internal RudpState State = RudpState.Connecting;
		internal bool   Listening;
		byte   CurrentSeq;

		MovingAvg AvgLatency   = new MovingAvg(10);
		MovingAvg AvgBytesSent = new MovingAvg(10);

		const int MAX_WINDOW_SIZE  = 25;
		internal const int SEND_BUFFER_SIZE = 64 * 1024;
		const int CHUNK_SIZE       = 1024;
		const int MAX_CHUNK_SIZE   = 1024;
		const int RECEIVE_BUFFER_SIZE = MAX_CHUNK_SIZE * MAX_WINDOW_SIZE;

		// connecting 
		bool	 SynAckSent;
		bool	 SynAckReceieved;

		// sending
        Queue<TrackPacket> SendPacketMap = new Queue<TrackPacket>();
        int		  SendWindowSize = 5;
        internal int SendBuffLength;
		internal bool RudpSendBlock;
		object    SendSection = new object();
        internal byte[] SendBuff = new byte[SEND_BUFFER_SIZE];
		DateTime  LastSend;
			
		// receiving
        SortedDictionary<byte, RudpPacket> RecvPacketMap = new SortedDictionary<byte, RudpPacket>(); // needs to be locked because used by timer and network threads
		byte	  HighestSeqRecvd;
		byte	  NextSeq;
		int		  RecvBuffLength;
		byte[]    RecvBuff = new byte[RECEIVE_BUFFER_SIZE];

		// acks
		Hashtable AckMap = Hashtable.Synchronized(new Hashtable());
		Queue	  AckOrder = Queue.Synchronized(new Queue());
		int InOrderAcks;
		int ReTransmits;


        internal RudpSocket(RudpSession session, bool listening)
        {
            Session = session;
            Core = session.Core;
            Network = Session.Network;

            Listening = listening; 
            
            PeerID = (ushort)Core.RndGen.Next(1, ushort.MaxValue);

            lock (Session.RudpControl.SocketMap)
                Session.RudpControl.SocketMap[PeerID] = this;
        }
   		
        internal void Connect()
		{
			AvgLatency.Input(500); // set retry for syn to half sec
            AvgLatency.Next();

			SendSyn();
		}

        internal void AddAddress(RudpAddress address)
        {
            if (!AddressMap.ContainsKey(address.GetHashCode()))
                AddressMap[address.GetHashCode()] = address;

            if (PrimaryAddress == null)
                PrimaryAddress = address;
        }

		internal int Send(byte[] buff, int buffLength)
		{
            if (State != RudpState.Connected)
                return -1;

            //Session.Log("Send " + buffLength + " bytes");

			// multiplied by 2 so room to expand and basically 2 second buffer
            int maxBuffSize = GetSendBuffSize();
		
            int copysize = 0;
			
            //int MaxBufferSize = SEND_BUFFER_SIZE;//m_SendWindowSize * CHUNK_SIZE;
            lock (SendSection)
            {
                if (SendBuffLength >= maxBuffSize)
                {
                    RudpSendBlock = true;
                    return -1;
                }

                int buffspace = maxBuffSize - SendBuffLength;
                copysize = buffspace >= buffLength ? buffLength : buffspace;

                Buffer.BlockCopy(buff, 0, SendBuff, SendBuffLength, copysize);
                SendBuffLength += copysize;

                if (copysize != buffLength)
                    RudpSendBlock = true;
            }

			return copysize;
		}

        int GetSendBuffSize()
        {
            int maxBuffSize = AvgBytesSent.GetAverage() * 2;

            maxBuffSize = maxBuffSize < 4096 ? 4096 : maxBuffSize;
            maxBuffSize = maxBuffSize > SEND_BUFFER_SIZE ? SEND_BUFFER_SIZE : maxBuffSize;

            return maxBuffSize;
        }

        internal bool SendBuffLow()
        {
            // if outstanding bytes are more than 3/4 of the max buffer size, we are low
            if (SendBuffLength > GetSendBuffSize() * 3 / 4)
            {
                RudpSendBlock = true;
                return true;
            }

            return false;
        }

		internal int Receive(byte[] buff, int buffOffset, int buffLen)
		{
			if(RecvBuffLength > buffLen)
                return FinishReceive(buff, buffOffset, buffLen);

            lock (RecvPacketMap)
            {
                ArrayList removeList = new ArrayList();

                // copy data from packets
                // while next element of map equals next in sequence
                foreach (byte seq in RecvPacketMap.Keys) // read keys because they are sorted
                {
                    RudpPacket packet = RecvPacketMap[seq];

                    // deal with reading in order at 0xFF to zero boundry
                    if (NextSeq > 0xFF - 25 && packet.Sequence < 25)
                        continue;

                    if (packet.Sequence != NextSeq)
                        break;

                    if (packet.PacketType == RudpPacketType.Data)
                    {
                        int dataSize = packet.Payload.Length;

                        if (dataSize > MAX_CHUNK_SIZE)
                        {
                            Session.Log("Too Large Packet Received Size " + packet.Payload.Length + ", Type Data");
                            RudpClose(CloseReason.LARGE_PACKET);
                            return -1;
                        }

                        // copy data
                        packet.Payload.CopyTo(RecvBuff, RecvBuffLength);
                        RecvBuffLength += packet.Payload.Length;

                        //Session.Log("Data Recv, Seq " + packet.Sequence.ToString() + ", ID " + packet.PeerID.ToString());
                    }
                    else
                        break;

                    HighestSeqRecvd = packet.Sequence;

                    removeList.Add(packet.Sequence);

                    NextSeq++;

                    if (RecvBuffLength > buffLen)
                        break;
                }

                foreach (byte seq in removeList)
                    RecvPacketMap.Remove(seq);
            }

			//Log("Reliable Receive " + NumtoStr(copysize) + ", " + NumtoStr(m_RecvBuffLength) + " left");

            return FinishReceive(buff, buffOffset, buffLen);
		}

		int FinishReceive(byte[] buff, int buffOffset, int buffLen)
		{
			// copy extra data from recv buffer
			int copysize = (RecvBuffLength > buffLen) ? buffLen : RecvBuffLength;
            Buffer.BlockCopy(RecvBuff, 0, buff, buffOffset, copysize);

			if(copysize != RecvBuffLength)
				Buffer.BlockCopy(RecvBuff, copysize, RecvBuff, 0, RecvBuffLength - copysize);

			RecvBuffLength -= copysize;
	
			return copysize;
		}

		internal void Close()
		{
			if(State != RudpState.Connected)
				return;

			SendFin(CloseReason.NORMAL_CLOSE);
			State = RudpState.Closed;

			return;
		}

		void SetConnected()
		{
			if(Listening)
                Session.OnAccept();
			else
                Session.OnConnect();
		}

		internal void RudpReceive(G2ReceivedPacket raw, RudpPacket packet, bool global)
		{
			// check if packet meant for another socket
			if(packet.PeerID != 0 && packet.PeerID != PeerID)
				return;	

			// check for errors
			string error = null;

			if(packet.Sequence > HighestSeqRecvd && State == RudpState.Closed) // accept only packets that came before the fin
				error = "Packet Received while in Close State ID " + packet.PeerID.ToString() + ", Type " + packet.PacketType.ToString();

            else if (packet.Payload.Length > 4096)
            {
                error = "Too Large Packet Received Size " + packet.Payload.Length.ToString() + ", Type " + packet.PacketType.ToString();
                RudpClose(CloseReason.LARGE_PACKET);
            }

			if( error != null )
			{
                Session.Log(error);
				return;
			}


            // add proxied and direct addresses
            // proxied first because that packet has highest chance of being received, no need for retry
            // also allows quick udp hole punching
            if (raw.Tcp != null)
                AddAddress(new RudpAddress(raw.Source, raw.Tcp));

            AddAddress( new RudpAddress(raw.Source) );

            // received syn, ident 5, from 1001 over tcp
            /*string log = "Received " + packet.PacketType.ToString();
            if (packet.Ident != 0) log += " ID:" + packet.Ident.ToString();
            log += " from " + raw.Source.ToString();

            if (raw.Tcp != null)
                log += " tcp";
            else
                log += " udp";
            Session.Log(log);*/
            

			// try to clear up bufffer, helps if full, better than calling this on each return statement
			ManageRecvWindow();

            // if ACK, PING, or PONG
            if (packet.PacketType == RudpPacketType.Ack || packet.PacketType == RudpPacketType.Ping || packet.PacketType == RudpPacketType.Pong)
            {
                if (packet.PacketType == RudpPacketType.Ack)
                    ReceiveAck(packet);

                if (packet.PacketType == RudpPacketType.Ping)
                    ReceivePing(packet);

                if (packet.PacketType == RudpPacketType.Pong)
                    ReceivePong(packet);

                return;
            }

			// if SYN, DATA or FIN packet

			// stop acking so remote host catches up
            if (packet.Sequence > HighestSeqRecvd + MAX_WINDOW_SIZE || RecvPacketMap.Count > MAX_WINDOW_SIZE)
			{
                //Session.Log("Error Packet Overflow");
				return;
			}

			// Send Ack - cant combine if statements doesnt work
			if( AckMap.Contains(packet.Sequence) )
			{
                //Session.Log("Error Packet Seq " + packet.Sequence.ToString() + " Already Received");
				SendAck(packet);
				return;
			}
	
			// insert into recv map
            lock (RecvPacketMap)
            {
                RecvPacketMap[packet.Sequence] = packet;
            }

			ManageRecvWindow();

			// ack down here so highest received is iterated, syns send own acks
            if (packet.PacketType != RudpPacketType.Syn)
			    SendAck(packet);
		}



		void SendSyn()
		{
			RudpPacket syn = new RudpPacket();

            lock (SendSection) // ensure queued in right order with right current seq
            {
                syn.TargetID = Session.UserID;
                syn.TargetClient = Session.ClientID;
                syn.PeerID = 0;
                syn.PacketType = RudpPacketType.Syn;
                syn.Sequence = CurrentSeq++;
                syn.Payload = RudpSyn.Encode(1, Network.LocalUserID, Network.ClientID, PeerID);

                SendPacketMap.Enqueue(new TrackPacket(syn));
            }

			ManageSendWindow();
		}

		void ReceiveSyn(RudpPacket packet)
		{
			RudpSyn syn = new RudpSyn(packet.Payload);

			if(RemotePeerID == 0)
				RemotePeerID = syn.ConnID;

            //Session.Log("Syn Recv, Seq " + packet.Sequence.ToString() + ", ID " + syn.ConnID.ToString());

			SendAck(packet); // send ack here also because peerID now set

			SynAckSent = true;

			if(SynAckSent && SynAckReceieved)
			{
                Session.Log("Connected (recv syn)");
				State = RudpState.Connected;
				SetConnected();
			}
		}

		void SendAck(RudpPacket packet)
		{
			RudpPacket ack = new RudpPacket();

            ack.TargetID = Session.UserID;
            ack.TargetClient = Session.ClientID;
			ack.PeerID   = RemotePeerID;
			ack.PacketType     = RudpPacketType.Ack;
			ack.Sequence = packet.Sequence;
			ack.Payload  = RudpAck.Encode(HighestSeqRecvd, (byte) (MAX_WINDOW_SIZE - RecvPacketMap.Count));
            ack.Ident = packet.Ident;

            //Session.Log("Ack Sent, Seq " + ack.Sequence.ToString() + ", ID " + ack.PeerID.ToString() + ", highest " + HighestSeqRecvd.ToString());

			if( !AckMap.Contains(ack.Sequence) )
			{	
				AckMap[ack.Sequence] = true;
				AckOrder.Enqueue(ack.Sequence);
			}

			while(AckMap.Count > MAX_WINDOW_SIZE * 2)
				AckMap.Remove( AckOrder.Dequeue() );

			SendTracked( new TrackPacket(ack) );
		}

		void ReceiveAck(RudpPacket packet)
		{
			int latency = 0;
			int retries = -1;

            // find original packet that was sent
            TrackPacket sent = null;

            lock (SendSection)
            {
                foreach (TrackPacket tracked in SendPacketMap)
                    if (tracked.Packet.Sequence == packet.Sequence)
                    {
                        sent = tracked;
                        break;
                    }
            }

            // mark packet as acked
            if (sent != null)
            {
                sent.Target.LastAck = Core.TimeNow;

			    // connect handshake
                if (State == RudpState.Connecting && sent.Packet.PacketType == RudpPacketType.Syn)
			    {
				    SynAckReceieved = true;

                    SetPrimaryAddress(packet.Ident);

				    if(SynAckSent && SynAckReceieved)
				    {
                        Session.Log("Connected (recv ack)");
					    State = RudpState.Connected;
					    SetConnected();

                        // if data packets received before final connection ack, process them now
                        ManageRecvWindow(); 
				    }
			    }

                if (!sent.Acked)
			    {
				    InOrderAcks++;

                    if (sent.Retries == 0)
				    {
					    latency = (int) sent.TimeEllapsed(Core);
					    latency = latency < 5 ? 5 : latency;
					    AvgLatency.Input(latency);
					    AvgLatency.Next();
				    }
			    }

			    retries = sent.Retries;

			    sent.Acked = true;
		    }

            RudpAck ack = new RudpAck(packet.Payload);
            //Session.Log("Ack Recv, Seq " + packet.Sequence.ToString() + ", ID " + packet.PeerID.ToString() + ", highest " + ack.Start.ToString() + ", retries " + retries.ToString() + ", latency " + latency.ToString());

            lock (SendSection)
            {
                // ack possibly un-acked packets
                foreach (TrackPacket tracked in SendPacketMap)
                {
                    // ack if start past the zero boundry with sequences behind
                    if (tracked.Packet.Sequence > 0xFF - 25 && ack.Start < 25)
                        tracked.Acked = true;

                    // break start before boundry and sequences ahead are not recvd/acked yet
                    if (ack.Start > 0xFF - 25 && tracked.Packet.Sequence < 25)
                        break;

                    // normal acking procedure
                    else if (ack.Start >= tracked.Packet.Sequence)
                        tracked.Acked = true;

                    else
                        break;
                }

                // remove acked, only remove packets from front
                while (SendPacketMap.Count > 0 && SendPacketMap.Peek().Acked)
                {
                    // calculate receive speed of remote host by rate they ack
                    AvgBytesSent.Input(SendPacketMap.Peek().Packet.Payload.Length);

                    SendPacketMap.Dequeue();
                }
            }

			// increase window if packets removed from beginning of buffer
			//if(packetsRemoved && SendWindowSize < 25)
			// 	SendWindowSize++;


			ManageSendWindow();
		}

		void SendPing(RudpAddress address)
		{
			RudpPacket ping = new RudpPacket();

            ping.TargetID = Session.UserID;
            ping.TargetClient = Session.ClientID;
            ping.PeerID = RemotePeerID;
            ping.PacketType = RudpPacketType.Ping;
            ping.Sequence = 0;
            ping.Payload = BitConverter.GetBytes(address.Ident);

            //Session.Log("Keep Alive Sent, Seq " + alive.Sequence.ToString() + ", ID " + alive.PeerID.ToString());
            TrackPacket tracked = new TrackPacket(ping);
            tracked.Target = address;
            tracked.SpecialTarget = true;

            SendTracked(tracked);
		}

        void ReceivePing(RudpPacket ping)
        {
            RudpPacket pong = new RudpPacket();

            pong.TargetID = Session.UserID;
            pong.TargetClient = Session.ClientID;
            pong.PeerID = RemotePeerID;
            pong.PacketType = RudpPacketType.Pong;
            pong.Sequence = 0;
            pong.Payload = ping.Payload;

            SendTracked(new TrackPacket(pong));
        }

        void ReceivePong(RudpPacket pong)
        {
            if (pong.Payload != null)
            {
                uint ident = BitConverter.ToUInt32(pong.Payload, 0);

                SetPrimaryAddress(ident);
            }
        }

        private void SetPrimaryAddress(uint ident)
        {
            foreach (RudpAddress address in AddressMap.Values)
                if (address.Ident == ident)
                {
                    address.LastAck = Core.TimeNow;

                    // if primary address needs to be reset, replace with this one 
                    // (which would be first/fastest received)
                    if (PrimaryAddress.Reset)
                    {
                        PrimaryAddress.Reset = false;
                        PrimaryAddress = address;
                    }

                    break;
                }
            
        }

		void SendFin(CloseReason reason)
		{
            RudpPacket fin = new RudpPacket();

            lock (SendSection) // ensure queued in right order with right current seq
            {
                fin.TargetID = Session.UserID;
                fin.TargetClient = Session.ClientID;
                fin.PeerID = RemotePeerID;
                fin.PacketType = RudpPacketType.Fin;
                fin.Sequence = CurrentSeq++;
                fin.Payload = new byte[1] { (byte)reason };

                SendPacketMap.Enqueue(new TrackPacket(fin));
            }

			//Session.Log("Fin Sent, Seq " + fin.Sequence.ToString() + ", ID " + fin.PeerID.ToString() + ", Reason " + reason.ToString());

			ManageSendWindow();
		}

		void ReceiveFin(RudpPacket packet)
		{
			if(packet.Payload.Length < 1)
				return;

			//Session.Log("Fin Recv, Seq " + packet.Sequence.ToString() + ", ID " + packet.PeerID.ToString() + ", Reason " + packet.Payload[0].ToString());
	
			if(State == RudpState.Closed)
				return;

			RudpClose(CloseReason.YOU_CLOSED);
		}

		void ManageSendWindow()
		{
            ArrayList retransmit = new ArrayList();
            
            int rtt = AvgLatency.GetAverage();
            int outstanding = 0;
            
            lock (SendSection)
            {
                // iter through send window
                foreach (TrackPacket packet in SendPacketMap)
                {
                    if (packet.Acked)
                        continue;

                    else if (packet.TimeSent == 0)
                        retransmit.Add(packet);

                    // connecting so must be a syn packet
                    else if (State == RudpState.Connecting)
                    {
                        if (packet.TimeEllapsed(Core) > 1000 * 2) // dont combine with above cause then next else if would always run
                            retransmit.Add(packet);
                    }

                    // send packets that havent been sent yet, and ones that need to be retransmitted
                    else if (packet.TimeEllapsed(Core) > rtt * 2)
                        retransmit.Add(packet);

                    // mark as outstanding
                    else
                        outstanding++;
                }
            }

            // re-transmit packets
            foreach (TrackPacket track in retransmit)
                if (outstanding < SendWindowSize)
                {
                    //Session.Log("Re-Send ID " + track.Packet.PeerID.ToString() +
                    //            ", Type " + track.Packet.Type.ToString() +
                    //            ", Seq " + track.Packet.Sequence.ToString() + ", Retries " + track.Retries.ToString() +
                    //            ", Passed " + track.TimeEllapsed().ToString() + " ms");

                    track.Retries++;
                    ReTransmits++;

                    SendTracked(track);

                    outstanding++;
                }
                else
                    break;


            lock (SendSection)
            {
                // send number of packets so that outstanding equals window size
                while (outstanding < SendWindowSize && SendBuffLength > 0 && SendPacketMap.Count < MAX_WINDOW_SIZE)
                {
                    int buffLen = (SendBuffLength > CHUNK_SIZE) ? CHUNK_SIZE : SendBuffLength;

                    RudpPacket data = new RudpPacket();
                    data.TargetID = Session.UserID;
                    data.TargetClient = Session.ClientID;
                    data.PeerID = RemotePeerID;
                    data.PacketType = RudpPacketType.Data;
                    data.Sequence = CurrentSeq++;
                    data.Payload = Utilities.ExtractBytes(SendBuff, 0, buffLen);


                    // move next data on deck for next send
                    if (SendBuffLength > buffLen)
                        Buffer.BlockCopy(SendBuff, buffLen, SendBuff, 0, SendBuffLength - buffLen);
                    SendBuffLength -= buffLen;

                    TrackPacket track = new TrackPacket(data);
                    SendPacketMap.Enqueue(track);

                    //Session.Log("Data Sent, Seq " + data.Sequence.ToString() + ", ID " + data.PeerID.ToString() + ", Size " + buffLen.ToString());

                    SendTracked(track);

                    outstanding++;
                }
            }

			// if we can take more data call onsend
			if(SendBuffLength == 0 && RudpSendBlock)
			{
                //Session.Log("OnSend Called");
				RudpSendBlock = false;
				Session.OnSend();
			}
		}

		void ManageRecvWindow()
		{
			bool dataReceived = false;

            lock (RecvPacketMap)
            {
                ArrayList removeList = new ArrayList();

                foreach (byte seq in RecvPacketMap.Keys)
                {
                    RudpPacket packet = RecvPacketMap[seq];

                    // deal with reading in order at 0xFF to zero boundry
                    if (NextSeq > 0xFF - 25 && packet.Sequence < 25)
                        continue;

                    if (packet.Sequence != NextSeq)
                        break;

                    if (packet.PacketType == RudpPacketType.Syn)
                        ReceiveSyn(packet);

                    else if (packet.PacketType == RudpPacketType.Data)
                    {
                        dataReceived = true;
                        break;
                    }

                    else if (packet.PacketType == RudpPacketType.Fin)
                        ReceiveFin(packet);

                    HighestSeqRecvd = packet.Sequence;

                    removeList.Add(packet.Sequence);

                    NextSeq++;
                }


                foreach (byte seq in removeList)
                    RecvPacketMap.Remove(seq);
            }

			// if data waiting to be read
            // dont let data receive if still connecting (getting ahead of ourselves) need successful ack return path before we recv data
			if(State == RudpState.Connected && (RecvBuffLength > 0 || dataReceived))
                Session.OnReceive();
		}

		internal void SendTracked(TrackPacket tracked)
        {
            if (AddressMap.Count == 0)
                return;

            RudpAddress target = tracked.SpecialTarget ? tracked.Target : PrimaryAddress;

            LastSend = Core.TimeNow;
            tracked.TimeSent = Core.TimeNow.Ticks;
            tracked.Target = target;
            tracked.Packet.SenderID = Network.LocalUserID;
            tracked.Packet.SenderClient = Network.ClientID;


            if (tracked.Packet.PacketType != RudpPacketType.Syn)
                SendPacket(tracked, target);

            else
            {
                PrimaryAddress.Reset = true;

                foreach (RudpAddress address in AddressMap.Values)
                {
                    tracked.Packet.Ident = address.Ident;
                    tracked.Target = address;

                    SendPacket(tracked, address);
                }
            }
		}

        private void SendPacket(TrackPacket tracked, RudpAddress target)
        {
            // sending syn  to (tracked target) through (address target) udp / tcp

            /*string log = "Sending " + tracked.Packet.PacketType.ToString();
            if (tracked.Packet.Ident != 0) log  += ", ID " + tracked.Packet.Ident.ToString();
            log +=  " to " + Utilities.IDtoBin(tracked.Packet.TargetID).Substring(0, 10);
            log += " target address " + target.Address.ToString();*/

            DhtNetwork network = Core.OperationNet; //crit target.Global ? Core.GlobalNet : Core.OperationNet;

            if (network == null)
                return;

            if (Core.Firewall != FirewallType.Blocked && target.LocalProxy == null)
            {
                network.UdpControl.SendTo(target.Address, tracked.Packet);
                //log += " udp";
            }


            else
            {
                tracked.Packet.ToEndPoint = target.Address;

                TcpConnect proxy = network.TcpControl.GetProxy(target.LocalProxy);

                if (proxy != null)
                    proxy.SendPacket(tracked.Packet);
                else
                    network.TcpControl.SendRandomProxy(tracked.Packet);

                //log += " proxied by local tcp";
            }

            //Session.Log(log);
        }


		void RudpClose(CloseReason code)
		{
			SendFin(code);
			State = RudpState.Closed;
            Session.OnClose();
		}

        DateTime NextCheckRoutes;

		internal void SecondTimer()
		{
			int packetLoss = 0;

			if(State == RudpState.Connected)
			{
				// manage send window
				packetLoss = 0;
		
				if(InOrderAcks > 0)
					packetLoss = ReTransmits * 100 / InOrderAcks;

				ReTransmits = 0;
				InOrderAcks = 0;

                //Session.Log("PL: " + packetLoss.ToString() + 
                //    ", SW: " + SendWindowSize.ToString() +
                //    ", SQ: " + SendPacketMap.Count.ToString() + 
                //    ", SB: " + SendBuffLength.ToString());

                if (packetLoss < 10 && SendWindowSize < MAX_WINDOW_SIZE)
					SendWindowSize++;
				if(packetLoss > 20 && SendWindowSize > 1)
					SendWindowSize /= 2;
		

				// if data waiting to be read
                if (State == RudpState.Connected && RecvBuffLength > 0)
                    Session.OnReceive();


                // dead - 5 secs send ping, 10 secs reset primary addr, 15 secs close

                DateTime lastRecv = PrimaryAddress.LastAck;

                // if nothing received for 15 seconds disconnect
                if (Core.TimeNow > lastRecv.AddSeconds(15))
                    RudpClose(CloseReason.TIMEOUT);

                // re-analyze alternate routes after 10 secs dead, or half min interval
                else if (Core.TimeNow > lastRecv.AddSeconds(10) || Core.TimeNow > NextCheckRoutes)
                {
                    // after connection this should immediately be called to find best route

                    CheckRoutes();

                    NextCheckRoutes = Core.TimeNow.AddSeconds(30);
                }

                // send keep alive if nothing received after 5 secs
                else if (Core.TimeNow > lastRecv.AddSeconds(5))
                {
                    SendPing(PrimaryAddress);
                }
			}

            // prune addressMap to last 6 addresses seen
            while (AddressMap.Count > 6)
            {
                RudpAddress lastSeen = null;

                foreach (RudpAddress address in AddressMap.Values)
                    if (lastSeen == null || address.LastAck < lastSeen.LastAck)
                        lastSeen = address;

                AddressMap.Remove(lastSeen.GetHashCode());
            }

			// re-send packets in out buffer
			if(State == RudpState.Connecting || State == RudpState.Connected)
			{	
				ManageSendWindow();
			}

			// update bandwidth rate used for determining internal send buffer
			AvgBytesSent.Next();
		}

        internal void CheckRoutes()
        {
            PrimaryAddress.Reset = true;

            foreach (RudpAddress address in AddressMap.Values)
                SendPing(address);
        }
	}

	internal class TrackPacket
	{
		internal RudpPacket   Packet;
		internal bool         Acked;
		internal int		    Retries;
        internal RudpAddress  Target;
        internal bool         SpecialTarget;
		internal long         TimeSent;

		internal TrackPacket(RudpPacket packet)
		{
			Packet = packet;
		}

        internal long TimeEllapsed(OpCore core)
        {
            if (TimeSent == 0)
                return 0;

            return (core.TimeNow.Ticks - TimeSent) / TimeSpan.TicksPerMillisecond;
        }
	}

    internal class RudpAddress
    {
        internal DhtAddress Address;
        internal DhtClient  LocalProxy; // NAT -> proxy -> host, NAT will only recv packets from specific proxy
        internal uint       Ident;
        internal DateTime   LastAck; // so not removed from address list when added
        internal bool       Reset;

        internal RudpAddress(DhtAddress address)
        {
            Init(address);
        }

        internal RudpAddress(DhtAddress address, TcpConnect proxy)
        {
            LocalProxy = new DhtClient(proxy);

            Init(address);
        }

        void Init(DhtAddress address)
        {
            Address = address;

            Ident = (uint) new Random().Next();
        }

        public override bool Equals(object obj)
        {
            RudpAddress check = obj as RudpAddress;

            if (check == null)
                return false;

            if (Address.Equals(check.Address) && LocalProxy.Equals(check.LocalProxy))
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            int hash = Address.GetHashCode();
            
            if(LocalProxy != null)
                hash = hash ^ LocalProxy.GetHashCode();

            return hash; 
        }
    }
}
