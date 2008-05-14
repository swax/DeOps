using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

using RiseOp.Services.Location;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Comm;
using RiseOp.Implementation.Protocol.Net;


namespace RiseOp.Implementation.Transport
{
    internal enum SessionStatus { Connecting, Active, Closed };

    internal class RudpSession
    {
        internal OpCore Core;

        internal RudpSocket Comm;

        internal SessionStatus Status = SessionStatus.Connecting;

        internal ulong DhtID;
        internal ushort ClientID;
        int HashCode;

		// extra info
        internal string Name;

		// negotiation
		internal RijndaelManaged InboundEnc;
		internal RijndaelManaged OutboundEnc;

        bool ConnectAckSent;

        internal DateTime NegotiateTimeout;

        internal const int BUFF_SIZE = 8 * 1024;
		
        // sending
        int SendBlockSize = 16;
        bool SendBufferFlushed = true;

        internal byte[] SendBuffer;
        internal int SendBuffSize = 0;

        internal byte[] EncryptBuffer;
        internal int EncryptBuffSize;

        ICryptoTransform SendEncryptor;
        
        // receiving
        int RecvBlockSize = 16;
        
        byte[] ReceiveBuffer;
        int    RecvBuffSize = 0; 
        
        byte[] DecryptBuffer;
        int    DecryptBuffSize;

        ICryptoTransform RecvDecryptor;

		// active
        internal DateTime  Startup;


        //FileStream DebugWriter;
        //UTF8Encoding strEnc = new UTF8Encoding();
        internal RudpSession(OpCore core, ulong dhtID, ushort clientID, bool inbound)
        {
			Core     = core;

            DhtID = dhtID;
            ClientID = clientID;
            HashCode = Core.RndGen.Next();

            Comm = new RudpSocket(this, inbound);

            NegotiateTimeout = Core.TimeNow.AddSeconds(10);
            Startup = Core.TimeNow;

            Name = Core.Links.GetName(DhtID);

            //DebugWriter = new FileStream("Log " + Network.Profile.ScreenName + "-" + Buddy.ScreenName + "-" + Comm.PeerID.ToString() + ".txt", FileMode.CreateNew, FileAccess.Write);
		}

        internal void Connect()
        {
            if (Status != SessionStatus.Connecting)
                return;

            Comm.Connect();
        }

		internal void UpdateStatus(SessionStatus status)
		{
            if (Status == status)
                return;

			Status = status;

			Log("Status changed to " + status.ToString());

            if (status == SessionStatus.Closed)
            {
                lock(Core.CommMap)
                    Core.CommMap.Remove(Comm.PeerID);
            }

            if (Core.RudpControl.SessionUpdate != null)
                Core.RudpControl.SessionUpdate.Invoke(this);
		}	

		internal bool SendPacket(G2Packet packet, bool expedite)
		{
            if (Core.InvokeRequired)
                Debug.Assert(false);

            byte[] final = packet.Encode(Core.Protocol);

            if (Comm.State != RudpState.Connected)
                return false;

            PacketLogEntry logEntry = new PacketLogEntry(TransportProtocol.Rudp, DirectionType.Out, Comm.PrimaryAddress.Address, final);
            Core.OperationNet.LogPacket(logEntry);

            // dont worry about buffers, cause initial comm buffer is large enough to fit all negotiating packets
            if (SendEncryptor == null)
            {
                Comm.Send(final, final.Length);
                return true;
            }

            // goal - dont fill encrypt buffer because it will block stuff like pings during transfers
            // use as temp, return failed if no room

            if (SendBuffer == null)
                SendBuffer = new byte[BUFF_SIZE];

            if (EncryptBuffer == null)
                EncryptBuffer = new byte[BUFF_SIZE];

            // ensure enough space in encrypt buff for packet and expedite packets
            if (BUFF_SIZE - EncryptBuffSize < final.Length + 128)
                throw new Exception("Packet Dropped");

            // encode put into send buff
            lock (SendBuffer)
            {
                final.CopyTo(SendBuffer, SendBuffSize);
                SendBuffSize += final.Length;

                SendBufferFlushed = false;
            }

            return FlushSend(expedite); // return true if room in comm buffer
		}

        internal bool FlushSend(bool expedite)
        {
            if (SendEncryptor == null)
                return false;

            lock (SendBuffer)
            {
                // add padding to send buff if expedidted to ensure all packets sent
                int remainder = SendBuffSize % SendBlockSize;
                if (!SendBufferFlushed && expedite && remainder > 0 && SendBuffSize < BUFF_SIZE - 32)
                {
                    int paddingNeeded = SendBlockSize - remainder;

                    if (paddingNeeded == 3)
                        paddingNeeded = 4;

                    // packet empty is 2 bytes, 1 byte extra if there is size info, cant pad 3 bytes :(
                    EncryptionUpdate eu = new EncryptionUpdate(false);
                    if(paddingNeeded > 3)
                        eu.Padding = new byte[paddingNeeded - 3];
                    byte[] final = eu.Encode(Core.Protocol);

                    final.CopyTo(SendBuffer, SendBuffSize);
                    SendBuffSize += final.Length;
                }

                // move data from send buff to encrypt buff
                int transformSize = SendBuffSize - (SendBuffSize % SendBlockSize);
                if (transformSize > 0 && transformSize < BUFF_SIZE - EncryptBuffSize)
                {
                    int transformed = SendEncryptor.TransformBlock(SendBuffer, 0, transformSize, EncryptBuffer, EncryptBuffSize);
                    Debug.Assert(transformSize == transformed);

                    EncryptBuffSize += transformed;
                    SendBuffSize -= transformed;

                    Buffer.BlockCopy(SendBuffer, transformed, SendBuffer, 0, SendBuffSize);
                }

                // send encrypt buff
                if(EncryptBuffSize > 0)
                {
                    int sent = Comm.Send(EncryptBuffer, EncryptBuffSize);

                    if (sent > 0)
                    {
                        EncryptBuffSize -= sent;
                        Buffer.BlockCopy(EncryptBuffer, sent, EncryptBuffer, 0, EncryptBuffSize);

                        if (expedite)
                            SendBufferFlushed = true;
                    }
                }
            }

            // return false if still data to be sent
            return EncryptBuffSize == 0;
        }

		internal void ReceivePacket(G2ReceivedPacket packet)
		{
			if(packet.Root.Name == CommPacket.Close)
			{
				Receive_Close(packet);
				return;
			}

            else if (packet.Root.Name == CommPacket.CryptPadding)
            { 
                // just padding 
            }

			else if(Status == SessionStatus.Connecting)
			{
                if (packet.Root.Name == CommPacket.SessionRequest)
					Receive_SessionRequest(packet);

                else if (packet.Root.Name == CommPacket.SessionAck)
					Receive_SessionAck(packet);

                else if (packet.Root.Name == CommPacket.KeyRequest)
					Receive_KeyRequest(packet);

                else if (packet.Root.Name == CommPacket.KeyAck)
					Receive_KeyAck(packet);

                else if (packet.Root.Name == CommPacket.CryptStart)
                {
                    InboundEnc.Padding = PaddingMode.None;
                    RecvDecryptor = InboundEnc.CreateDecryptor();
                }

				return;
			}
			
			else if(Status == SessionStatus.Active)
			{
                if (packet.Root.Name == CommPacket.Data)
                    ReceiveData(packet);

                else if (packet.Root.Name == CommPacket.ProxyUpdate)
                    Receive_ProxyUpdate(packet);

				return;
			}
							
		}

		internal void SecondTimer()
		{
			Comm.SecondTimer();

            if(Status == SessionStatus.Connecting)
			{
                if (Core.TimeNow > NegotiateTimeout)
                    Send_Close("Timeout");
			}
			
            if(Status == SessionStatus.Active)
			{
                if (FlushSend(true))
                    Core.Transfers.OnMoreData(this); // a hack for stalled transfers
			}
		}

		internal void Send_KeyRequest(SessionRequest request)
		{
            // generate inbound key
            InboundEnc = new RijndaelManaged();
            InboundEnc.GenerateKey();
            InboundEnc.GenerateIV();
            
            // make packet
            KeyRequest keyRequest = new KeyRequest();
            keyRequest.Encryption = Utilities.CryptType(InboundEnc);
            keyRequest.Key = InboundEnc.Key;
            keyRequest.IV = InboundEnc.IV;

            Log("Key Request Sent");

			SendPacket(keyRequest, true);
		}

		internal void Receive_KeyRequest(G2ReceivedPacket embeddedPacket)
		{
			KeyRequest request = KeyRequest.Decode(Core.Protocol, embeddedPacket);

            OutboundEnc = new RijndaelManaged();
            OutboundEnc.Key = request.Key;
            OutboundEnc.IV = request.IV;

            StartEncryption();
			Send_KeyAck();
		}

		internal void Send_KeyAck()
		{
			KeyAck keyAck       = new KeyAck();
			keyAck.SenderPubKey = Core.User.Settings.KeyPair.ExportParameters(false);
            keyAck.Name         = Core.User.Settings.ScreenName;

			Log("Key Ack Sent");

			SendPacket(keyAck, true);
		}

		internal void Receive_KeyAck(G2ReceivedPacket embeddedPacket)
		{
			KeyAck keyAck = KeyAck.Decode(Core.Protocol, embeddedPacket);
            Name = keyAck.Name;

            Log("Key Ack Received");

            Core.IndexKey(DhtID, ref keyAck.SenderPubKey.Modulus);

            // send session request with encrypted current key
            Send_SessionRequest();
            Send_SessionAck();
            ConnectAckSent = true;

            // receiving session gets, verifies sender can encrypt with public key and goes alriiight alriight
		}

		internal void Send_SessionRequest()
		{
			// build session request, call send packet
			SessionRequest request = new SessionRequest();

            // generate inbound key, inbound known if keyack completing
            if (InboundEnc == null)
            {
                InboundEnc = new RijndaelManaged();
                InboundEnc.GenerateKey();
                InboundEnc.GenerateIV();
            }

            // encode session key with remote hosts internal key (should be 48 bytes, 16 + 32)
            byte[] sessionKey = new byte[InboundEnc.Key.Length + InboundEnc.IV.Length];
            InboundEnc.Key.CopyTo(sessionKey, 0);
            InboundEnc.IV.CopyTo(sessionKey, InboundEnc.Key.Length);
            request.EncryptedKey = Utilities.KeytoRsa(Core.KeyMap[DhtID]).Encrypt(sessionKey, false);

            Log("Session Request Sent");

            SendPacket(request, true);
		}

		internal void Receive_SessionRequest(G2ReceivedPacket embeddedPacket)
		{
			SessionRequest request = SessionRequest.Decode(Core.Protocol, embeddedPacket);

            Log("Session Request Received");

            byte[] sessionKey = Core.User.Settings.KeyPair.Decrypt(request.EncryptedKey, false);

            // new connection
            if (OutboundEnc == null)
            {
                OutboundEnc = new RijndaelManaged();
                OutboundEnc.Key = Utilities.ExtractBytes(sessionKey, 0, 32);
                OutboundEnc.IV = Utilities.ExtractBytes(sessionKey, 32, 16);
            }

            // if key request
            else
            {
                if(Utilities.MemCompare(OutboundEnc.Key, Utilities.ExtractBytes(sessionKey, 0, 32)) == false ||
                    Utilities.MemCompare(OutboundEnc.IV, Utilities.ExtractBytes(sessionKey, 32, 16)) == false)
                {
                    Send_Close("Verification after key request failed");
                    return;
                }

                Send_SessionAck();
                ConnectAckSent = true;
                return;
            }

			// if internal key null
			if(!Core.KeyMap.ContainsKey(DhtID))
			{
                StartEncryption();
				Send_KeyRequest(request);
				return;
			}

            if (Comm.Listening)
                Send_SessionRequest();

            StartEncryption();
            Send_SessionAck();

            ConnectAckSent = true;
		}

        private void StartEncryption()
        {
            SendPacket( new EncryptionUpdate(true), false ); // dont expedite because very next packet is expedited

            OutboundEnc.Padding = PaddingMode.None;
            SendEncryptor = OutboundEnc.CreateEncryptor();

            Log("Encryption Started");
        }

		internal void Send_SessionAck()
		{
			SessionAck ack = new SessionAck();

            Log("Session Ack Sent");

			SendPacket(ack, true);
		}

		internal void Receive_SessionAck(G2ReceivedPacket embeddedPacket)
		{
			SessionAck ack = SessionAck.Decode(Core.Protocol, embeddedPacket);

            Log("Session Ack Received");

            if( AlreadyActive() )
            {
                Send_Close("Already Active");
                return;
            }

            if( !ConnectAckSent )
            {
                Send_Close("Ack not Received");
                return;
            }

            UpdateStatus(SessionStatus.Active);
		}

        internal bool SendData(uint service, uint datatype, G2Packet packet, bool expedite)
        {
            CommData data = new CommData(service, datatype, packet.Encode(Core.Protocol));

            return SendPacket(data, expedite);
        }

        internal void ReceiveData(G2ReceivedPacket embeddedPacket)
        {
            // 0 is network packet?

            CommData data = CommData.Decode(Core.Protocol, embeddedPacket);

            if (data != null)
                if (Core.RudpControl.SessionData.Contains(data.Service, data.DataType))
                    Core.RudpControl.SessionData[data.Service, data.DataType].Invoke(this, data.Data);
        }

		internal void Send_Close(string reason)
		{
			Log("Sending Close (" + reason + ")");

			CommClose close = new CommClose();
			close.Reason     = reason;

            SendPacket(close, true);
            Comm.Close(); 

			UpdateStatus(SessionStatus.Closed);
		}
		
		internal void Receive_Close(G2ReceivedPacket embeddedPacket)
		{
			CommClose close = CommClose.Decode(Core.Protocol, embeddedPacket);
			
			Log("Received Close (" + close.Reason + ")");

			UpdateStatus(SessionStatus.Closed);
		}

        internal void Send_ProxyUpdate(TcpConnect tcp)
        {
            ProxyUpdate update = new ProxyUpdate();

            update.Global = tcp.Network.IsGlobal;
            update.Proxy = new DhtAddress(tcp.RemoteIP, tcp);

            Log("Sent Proxy Update (" + update.Proxy + ")");

            SendPacket(update, true);
        }

        internal void Receive_ProxyUpdate(G2ReceivedPacket embeddedPacket)
        {
            ProxyUpdate update = ProxyUpdate.Decode(Core.Protocol, embeddedPacket);

            Comm.AddAddress(new RudpAddress(Core, update.Proxy, update.Global));

            if(embeddedPacket.Tcp != null)
                Comm.AddAddress(new RudpAddress(Core, update.Proxy, update.Global, embeddedPacket.Tcp.DhtID));

            Log("Received Proxy Update (" + update.Proxy + ")");
        }

		internal bool AlreadyActive()
		{
			foreach(RudpSession session in Core.RudpControl.SessionMap[DhtID])
				if(session != this && session.ClientID == ClientID && session.Status == SessionStatus.Active)
					return true;

			return false;
		}

		internal void Log(string entry)
		{
            string name = Comm.PeerID.ToString();

            if (Name != null)
                name = Name;

            
            //PeerID 10:23:250 : 
            string prefix = Comm.PeerID.ToString() + " ";
            prefix += Core.TimeNow.Minute.ToString() + ":" + Core.TimeNow.Second.ToString();

            if (Core.TimeNow.Millisecond == 0)
                prefix += ":00";
            else
                prefix += ":" + Core.TimeNow.Millisecond.ToString().Substring(0, 2);

            Core.OperationNet.UpdateLog("RUDP " + name, prefix + ": " + entry);

            //byte[] data = strEnc.GetBytes(Comm.PeerID.ToString() + ": " + entry + "\r\n");
            //DebugWriter.Write(data, 0, data.Length);
		}

		internal void OnConnect()
		{
            Log("OnConnect");

            // it can take a while to get the rudp session up
            // especially between two blocked hosts
            NegotiateTimeout = Core.TimeNow.AddSeconds(10);
            
            Send_SessionRequest();
		}

		internal void OnAccept()
		{
			//wait for remote host to send session request
            NegotiateTimeout = Core.TimeNow.AddSeconds(10);
		}
        
		internal void OnReceive()
		{
            if(ReceiveBuffer == null)
                ReceiveBuffer = new byte[BUFF_SIZE];

            int recvd = Comm.Receive(ReceiveBuffer, RecvBuffSize, BUFF_SIZE - RecvBuffSize);

            if (recvd <= 0)
                return;

            int start = 0;
            RecvBuffSize += recvd;

            // get next packet
            G2ReadResult streamStatus = G2ReadResult.PACKET_GOOD;

            while (streamStatus == G2ReadResult.PACKET_GOOD)
            {
                G2ReceivedPacket packet = new G2ReceivedPacket();

                start = 0;

                // if encryption off
                if (RecvDecryptor == null)
                {
                    packet.Root = new G2Header(ReceiveBuffer);
                    streamStatus = G2Protocol.ReadNextPacket(packet.Root, ref start, ref RecvBuffSize);

                    if (streamStatus != G2ReadResult.PACKET_GOOD)
                        break;

                    PacketLogEntry logEntry = new PacketLogEntry(TransportProtocol.Rudp, DirectionType.In, Comm.PrimaryAddress.Address, Utilities.ExtractBytes(packet.Root.Data, packet.Root.PacketPos, packet.Root.PacketSize));
                    Core.OperationNet.LogPacket(logEntry);

                    ReceivePacket(packet);

                    if (start > 0 && RecvBuffSize > 0)
                        Buffer.BlockCopy(ReceiveBuffer, start, ReceiveBuffer, 0, RecvBuffSize);
                }

                // else if encryption on
                else 
                {
                    // if data needs to be decrypted from receive buffer
                    if (RecvBuffSize >= RecvBlockSize)
                    {
                        int transLength = RecvBuffSize - (RecvBuffSize % RecvBlockSize);
                        int spaceAvail  = BUFF_SIZE - DecryptBuffSize;

                        if(spaceAvail < transLength)
                            transLength = spaceAvail - (spaceAvail % RecvBlockSize);

                        if (transLength >= RecvBlockSize)
                        {
                            if (DecryptBuffer == null)
                                DecryptBuffer = new byte[BUFF_SIZE];

                            int transformed = RecvDecryptor.TransformBlock(ReceiveBuffer, 0, transLength, DecryptBuffer, DecryptBuffSize); 
                            Debug.Assert(transformed == transLength);

                            DecryptBuffSize += transformed;
                            RecvBuffSize -= transLength;

                            Buffer.BlockCopy(ReceiveBuffer, transLength, ReceiveBuffer, 0, RecvBuffSize);
                        }
                    }

                    // read packets from decrypt buffer
                    packet.Root = new G2Header(DecryptBuffer);
                    streamStatus = G2Protocol.ReadNextPacket(packet.Root, ref start, ref DecryptBuffSize);

                    if (streamStatus != G2ReadResult.PACKET_GOOD)
                        break;

                    PacketLogEntry logEntry = new PacketLogEntry(TransportProtocol.Rudp, DirectionType.In, Comm.PrimaryAddress.Address, Utilities.ExtractBytes(packet.Root.Data, packet.Root.PacketPos, packet.Root.PacketSize));
                    Core.OperationNet.LogPacket(logEntry);

                    ReceivePacket(packet);

                    if (start > 0 && DecryptBuffSize > 0)
                        Buffer.BlockCopy(DecryptBuffer, start, DecryptBuffer, 0, DecryptBuffSize);
                }
            }
		}

		internal void OnSend()
		{
            // try to flush remaining data
            if (!FlushSend(false))
                return;

            Core.Transfers.OnMoreData(this);
		}

		internal void OnClose()
		{
            Log("OnClose");

            UpdateStatus(SessionStatus.Closed);
		}


        public override int GetHashCode()
        {
            return HashCode;
        }

        internal bool SendBuffLow()
        {
            if (Comm != null)
                return Comm.SendBuffLow();

            return true;
        }


    }
}
