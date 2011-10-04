using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;

using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Simulator;

namespace DeOps.Implementation.Transport
{
	/// <summary>
	/// Summary description for UdpHandler.
	/// </summary>
	internal class LanHandler
	{
        // super-class
        OpCore Core;
		DhtNetwork Network;

		Socket LanSocket;

		internal ushort ListenPort;
	
		byte[] ReceiveBuff = new byte[4096];
		byte[] InflateBuff = new byte[4096];

		BufferData SendData = new BufferData( new byte[4096] );

        const int MAX_UDP_SIZE = 1500;


        internal LanHandler(DhtNetwork network)
		{
            Network = network;
            Core = network.Core;

            byte[] hash = new SHA1Managed().ComputeHash(Network.OpCrypt.Key);

            ListenPort = BitConverter.ToUInt16(hash, 0);

            if (ListenPort < 2000)
                ListenPort += 2000;

            if (Core.Sim != null)
                return;

			LanSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            LanSocket.EnableBroadcast = true;

			// listen - cant retry because listen port must be the same for everyone
			try
			{
				LanSocket.Bind( new IPEndPoint( System.Net.IPAddress.Any, ListenPort) );
	
				EndPoint tempSender = (EndPoint) new IPEndPoint(IPAddress.Any, 0);
				LanSocket.BeginReceiveFrom(ReceiveBuff, 0, ReceiveBuff.Length, SocketFlags.None, ref tempSender, new AsyncCallback(UdpSocket_Receive), LanSocket);
			
				Network.UpdateLog("Network", "Listening for LAN on port " + ListenPort.ToString());

			}
			catch(Exception ex)
			{ 
				Network.UpdateLog("Exception", "LanHandler::LanHandler: " + ex.Message);
			}
		}

		internal void Shutdown()
		{
			try
			{
				Socket oldSocket = LanSocket; // do this to prevent listen exception
				LanSocket = null;

				if(oldSocket != null)
					oldSocket.Close();
			}
			catch(Exception ex)
			{
                Network.UpdateLog("Exception", "LanHandler::Shudown: " + ex.Message);
			}
		}

		internal void SendTo(G2Packet packet)
		{
            if (Core.InvokeRequired)
                Debug.Assert(false);

      
            if (packet is NetworkPacket)
            {
                ((NetworkPacket)packet).SourceID = Network.Local.UserID;
                ((NetworkPacket)packet).ClientID = Network.Local.ClientID;
            }

            byte[] encoded = packet.Encode(Network.Protocol);

            PacketLogEntry logEntry = new PacketLogEntry(Core.TimeNow, TransportProtocol.LAN, DirectionType.Out, null, encoded);
            Network.LogPacket(logEntry);

            byte[] final = null;

            // encrypt, turn off encryption during simulation
            if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
            {
                final = Utilities.EncryptBytes(encoded, Network.OpCrypt.Key);
            }
            else
                final = encoded;

            // send
            try
			{
                if (Core.Sim != null)
                {
                    //Core.Sim.Internet.SendPacket(SimPacketType.Udp, Network, final, address.ToEndPoint(), null);
                    return;
                }

                if (LanSocket == null)
                    return;

                if (encoded.Length> MAX_UDP_SIZE)
					throw new Exception("Packet larger than " + MAX_UDP_SIZE.ToString() + " bytes");

                EndPoint tempSender = (EndPoint)new IPEndPoint(IPAddress.Broadcast, ListenPort);
                LanSocket.BeginSendTo(final, 0, final.Length, SocketFlags.None, tempSender, new AsyncCallback(UdpSocket_SendTo), LanSocket);
			}
			catch(Exception ex)
			{
                Network.UpdateLog("Exception", "LanHandler::SendTo: " + ex.Message);
			}
		}

		void UdpSocket_SendTo(IAsyncResult asyncResult)
		{
			if(LanSocket == null)
				return;

			try
			{
				int bytesSent = LanSocket.EndSendTo(asyncResult);
			}
			catch(Exception ex)
			{
                Network.UpdateLog("Exception", "LanHandler::UdpSocket_SendTo: " + ex.Message);
			}
		}

		void UdpSocket_Receive (IAsyncResult asyncResult)
		{
			if(LanSocket == null)
				return;
		
			try
			{
				EndPoint sender = (EndPoint) new IPEndPoint(IPAddress.Any, 0);
				int recvLen = LanSocket.EndReceiveFrom(asyncResult, ref sender);
                
                OnReceive(ReceiveBuff, recvLen, (IPEndPoint)sender);
			}
			catch(Exception ex)
			{
                Network.UpdateLog("Exception", "LanHandler::UdpSocket_Receive:1: " + ex.Message);
			}
			

            //crit
			// calling a sendto to a good host but unreachable port causes exceptions that stack the more sentto's you call
			// endreceivefrom will throw and so will begin until begin has been called enough to makeup for the unreachable hosts
			// if this loop is exited without a successful call to beginreceive from, inbound udb is game over
			int attempts = 0;
			while(attempts < 100)
			{
				try
				{
					EndPoint sender = (EndPoint) new IPEndPoint(IPAddress.Any, 0);
					LanSocket.BeginReceiveFrom(ReceiveBuff, 0, ReceiveBuff.Length, SocketFlags.None, ref sender, new AsyncCallback(UdpSocket_Receive), LanSocket);
					break;
				}
				catch(Exception ex)
				{
                    Network.UpdateLog("Exception", "LanHandler::UdpSocket_Receive:2: " + ex.Message + ", attempt " + attempts.ToString());
					attempts++;
				}
			}
		}

        internal void OnReceive(byte[] buff, int length, IPEndPoint sender)
        {
            bool copied = false;
            byte[] finalBuff = buff;

            if (Core.Sim == null || Core.Sim.Internet.TestEncryption) // turn off encryption during simulation
            {
                if (length < Network.OpCrypt.IV.Length)
                    throw new Exception("Not enough data received for IV");

                finalBuff = Utilities.DecryptBytes(buff, length, Network.OpCrypt.Key);
                length = finalBuff.Length;
                copied = true;           
            }

            ParsePacket(finalBuff, length, sender, copied);
        }

		void ParsePacket(byte[] buff, int length, IPEndPoint sender, bool copied)
		{
			G2ReceivedPacket packet = new G2ReceivedPacket();
            packet.Root = new G2Header(buff);

            if(G2Protocol.ReadPacket(packet.Root))
            {
                packet.Source = new DhtContact(0, 0, sender.Address, 0, 0);

                byte[] packetData = copied ? buff : Utilities.ExtractBytes(packet.Root.Data, packet.Root.PacketPos, packet.Root.PacketSize);

                PacketLogEntry logEntry = new PacketLogEntry(Core.TimeNow, TransportProtocol.LAN, DirectionType.In, packet.Source, packetData);
				Network.LogPacket(logEntry);


                Network.IncomingPacket(packet);
			}
		}
	}
}
