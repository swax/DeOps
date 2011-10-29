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
	internal class UdpHandler
	{
        const int MAX_UDP_SIZE = 1500;

        // super-class
        OpCore Core;
		DhtNetwork Network;

		Socket UdpSocket;

		internal ushort ListenPort;
	
		byte[] ReceiveBuff = new byte[4096];
		byte[] InflateBuff = new byte[4096];

		BufferData SendData = new BufferData( new byte[4096] );

        internal BandwidthLog Bandwidth;
        

        internal UdpHandler(DhtNetwork network)
		{
            Network = network;
            Core = network.Core;

            Bandwidth = new BandwidthLog(Core.RecordBandwidthSeconds);

            Initialize();
        }

        internal void Initialize()
        {
            ListenPort = Network.IsLookup ? Network.Lookup.Ports.Udp : Core.User.Settings.UdpPort;
            
            if (Core.Sim != null)
                return;

			UdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                uint IOC_IN = 0x80000000;
                uint IOC_VENDOR = 0x18000000;
                uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

                UdpSocket.IOControl((int)SIO_UDP_CONNRESET, new byte[] { 0 }, null);
            }
            catch
            {
                // will fail in mono
            }

			// listen
			bool bound    = false;
			int  attempts = 0;
			while( !bound && attempts < 5)
			{
				try
				{
					UdpSocket.Bind( new IPEndPoint( System.Net.IPAddress.Any, ListenPort) );
					bound = true;
					
					EndPoint tempSender = (EndPoint) new IPEndPoint(IPAddress.Any, 0);
					UdpSocket.BeginReceiveFrom(ReceiveBuff, 0, ReceiveBuff.Length, SocketFlags.None, ref tempSender, new AsyncCallback(UdpSocket_Receive), UdpSocket);
				
					Network.UpdateLog("Network", "Listening for UDP on port " + ListenPort.ToString());

				}
				catch(Exception ex)
				{ 
					Network.UpdateLog("Exception", "UdpHandler::UdpHandler: " + ex.Message);
			
					attempts++; 
					ListenPort++;
				}
			}
		}

		internal void Shutdown()
		{
			try
			{
				Socket oldSocket = UdpSocket; // do this to prevent listen exception
				UdpSocket = null;

				if(oldSocket != null)
					oldSocket.Close();
			}
			catch(Exception ex)
			{
				Network.UpdateLog("Exception", "UdpHandler::Shudown: " + ex.Message);
			}
		}

        internal void SecondTimer()
        {
            Core.Context.Bandwidth.InPerSec += Bandwidth.InPerSec;
            Core.Context.Bandwidth.OutPerSec += Bandwidth.OutPerSec;

            Bandwidth.NextSecond();
        }

		internal int SendTo(DhtAddress address, G2Packet packet)
		{
            if (Core.InvokeRequired)
                Debug.Assert(false);

            Debug.Assert(address.UdpPort != 0);

            if (packet is NetworkPacket)
            {
                ((NetworkPacket)packet).SourceID = Network.Local.UserID;
                ((NetworkPacket)packet).ClientID = Network.Local.ClientID;
            }

            byte[] encoded = packet.Encode(Network.Protocol);

            PacketLogEntry logEntry = new PacketLogEntry(Core.TimeNow, TransportProtocol.Udp, DirectionType.Out, address, encoded);
            Network.LogPacket(logEntry);

            byte[] final = null;

            // encrypt, turn off encryption during simulation
            if (Core.Sim == null || Core.Sim.Internet.TestEncryption)
                final = Utilities.EncryptBytes(encoded, Network.GetAugmentedKey(address.UserID));
            else
                final = encoded;

            
            // send
            try
			{
                if (Core.Sim != null)
                {
                    Core.Sim.Internet.SendPacket(SimPacketType.Udp, Network, final, address.ToEndPoint(), null);
                }
                else
                {
                    if (UdpSocket == null)
                        return 0;

                    if (encoded.Length > MAX_UDP_SIZE)
                        throw new Exception("Packet larger than " + MAX_UDP_SIZE.ToString() + " bytes");

                    UdpSocket.BeginSendTo(final, 0, final.Length, SocketFlags.None, address.ToEndPoint(), new AsyncCallback(UdpSocket_SendTo), UdpSocket);
                }

                // record bandwidth
                Bandwidth.OutPerSec += final.Length;
                return final.Length;
            }
			catch(Exception ex)
			{ 
				Network.UpdateLog("Exception", "UdpHandler::SendTo: " + ex.Message);
			}

            return 0;
		}

		void UdpSocket_SendTo(IAsyncResult asyncResult)
		{
			if(UdpSocket == null)
				return;

			try
			{
				int bytesSent = UdpSocket.EndSendTo(asyncResult);
			}
			catch(Exception ex)
			{ 
				Network.UpdateLog("Exception", "UdpHandler::UdpSocket_SendTo: " + ex.Message);
			}
		}

		void UdpSocket_Receive (IAsyncResult asyncResult)
		{
			if(UdpSocket == null)
				return;
		
			try
			{
				EndPoint sender = (EndPoint) new IPEndPoint(IPAddress.Any, 0);
				int recvLen = UdpSocket.EndReceiveFrom(asyncResult, ref sender);

                OnReceive(ReceiveBuff, recvLen, (IPEndPoint)sender);
			}
			catch(Exception ex)
			{ 
				Network.UpdateLog("Exception", "UdpHandler::UdpSocket_Receive:1: " + ex.Message);
			}
			

            //crit - may have been fixed by io control - check release exception log on larger network
			// calling a sendto to a good host but unreachable port causes exceptions that stack the more sentto's you call
			// endreceivefrom will throw and so will begin until begin has been called enough to makeup for the unreachable hosts
			// if this loop is exited without a successful call to beginreceive from, inbound udb is game over
			int attempts = 0;
			while(attempts < 100)
			{
				try
				{
					EndPoint sender = (EndPoint) new IPEndPoint(IPAddress.Any, 0);
					UdpSocket.BeginReceiveFrom(ReceiveBuff, 0, ReceiveBuff.Length, SocketFlags.None, ref sender, new AsyncCallback(UdpSocket_Receive), UdpSocket);
					break;
				}
				catch(Exception ex)
				{ 
					Network.UpdateLog("Exception", "UdpHandler::UdpSocket_Receive:2: " + ex.Message + ", attempt " + attempts.ToString());
					attempts++;
				}
			}
		}

        internal void OnReceive(byte[] buff, int length, IPEndPoint sender)
        {
            // record bandwidth - done here so caught in/out of sim
            Bandwidth.InPerSec += length;

            bool copied = false;
            byte[] finalBuff = buff;

            if (Core.Sim == null || Core.Sim.Internet.TestEncryption) // turn off encryption during simulation
            {
                if (length < 16)
                    throw new Exception("Not enough data received for IV");

                finalBuff = Utilities.DecryptBytes(buff, length, Network.LocalAugmentedKey);
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
                packet.Source = new DhtContact(0, 0, sender.Address, 0, (ushort)sender.Port);

                byte[] packetData = copied ? buff : Utilities.ExtractBytes(packet.Root.Data, packet.Root.PacketPos, packet.Root.PacketSize);

                PacketLogEntry logEntry = new PacketLogEntry(Core.TimeNow, TransportProtocol.Udp, DirectionType.In, packet.Source, packetData);
				Network.LogPacket(logEntry);


                Network.IncomingPacket(packet);
			}
		}
	}
}
