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
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Runtime.InteropServices;


using DeOps.Components;
using DeOps.Components.Chat;
using DeOps.Components.IM;
using DeOps.Components.Link;
using DeOps.Components.Location;
using DeOps.Components.Mail;
using DeOps.Components.Profile;
using DeOps.Components.Transfer;
using DeOps.Components.Board;
using DeOps.Components.Plan;
using DeOps.Components.Storage;

using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Comm;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Transport;

using DeOps.Interface;
using DeOps.Interface.Tools;
using DeOps.Interface.Views;

using DeOps.Simulator;


namespace DeOps.Implementation
{
	internal enum FirewallType { Blocked, NAT, Open };
    internal enum TransportProtocol { Tcp, Udp, Rudp };


    internal delegate void LoadHandler();
    internal delegate void TimerHandler();
    internal delegate void NewsUpdateHandler(string message, int component, int project);


	internal class OpCore
	{
        // super-classes
        internal LoaderForm  Loader;
        internal SimInstance Sim;

        // sub-classes
		internal Identity    User;
        internal G2Protocol  Protocol;
        internal DhtNetwork  GlobalNet;
        internal DhtNetwork  OperationNet;
        internal RudpHandler RudpControl;

        // components
        internal LinkControl     Links;
        internal LocationControl Locations;
        internal ProfileControl  Profiles;
        internal TransferControl Transfers;
        internal MailControl     Mail;
        internal BoardControl    Board;
        internal PlanControl     Plans;
        internal StorageControl  Storages;

        internal Dictionary<ushort, OpComponent> Components = new Dictionary<ushort, OpComponent>();


		// properties
		internal IPAddress    LocalIP;
        internal UInt64       LocalDhtID;
        internal FirewallType Firewall = FirewallType.Blocked;
		internal ushort       ClientID;
        internal DateTime     StartTime;
        internal DateTime     NextSaveCache;
        internal ulong        OpID;
        internal bool         Loading;

        internal Dictionary<ulong, byte[]> KeyMap = new Dictionary<ulong, byte[]>();

        internal Dictionary<ushort, RudpSocket> CommMap = new Dictionary<ushort, RudpSocket>();

        internal event LoadHandler  LoadEvent;
        internal event TimerHandler TimerEvent;
        internal event NewsUpdateHandler NewsUpdate;

		[DllImport("USER32.DLL", SetLastError=true)]
		private static extern bool GetLastInputInfo(ref LastInputInfo ii);

		internal int GmtOffset = System.TimeZone.CurrentTimeZone.GetUtcOffset( DateTime.Now ).Hours;

        // interfaces
        internal MainForm      GuiMain;
        internal TrayLock      GuiTray;
        internal ConsoleForm   GuiConsole;
        internal InternalsForm GuiInternal;

        // logs
        internal bool PauseLog;
        internal Queue ConsoleText = Queue.Synchronized(new Queue());
      

        // other
        internal Random RndGen = new Random(unchecked((int)DateTime.Now.Ticks));
        internal RNGCryptoServiceProvider StrongRndGen = new RNGCryptoServiceProvider();


        internal OpCore(LoaderForm loader, string path, string pass)
        {
            Loader = loader;

            Init(path, pass);
        }

        internal OpCore(SimInstance sim, string path, string pass)
        {
            Sim = sim;

            Init(path, pass);
        }

        void Init(string path, string pass)
        {
            StartTime = TimeNow;
            NextSaveCache = TimeNow.AddMinutes(1);

            ConsoleLog("De-Ops " + Application.ProductVersion);

            Protocol = new G2Protocol();
            RudpControl = new RudpHandler(this);
            
            User = new Identity(path, pass, this);
            User.Load(LoadModeType.Settings);

            LocalDhtID = Utilities.KeytoID(User.Settings.KeyPair.ExportParameters(false));
            ClientID   = (ushort)RndGen.Next(1, ushort.MaxValue);
            OpID       = Utilities.KeytoID(User.Settings.OpKey.Key);
            
            OperationNet = new DhtNetwork(this, false);

            if (User.Settings.OpAccess != AccessType.Secret)
                GlobalNet = new DhtNetwork(this, true);


            Components[ComponentID.Link]     = new LinkControl(this);
            Components[ComponentID.Location] = new LocationControl(this);
            Components[ComponentID.Transfer] = new TransferControl(this);
            Components[ComponentID.Profile]  = new ProfileControl(this);
            Components[ComponentID.IM]       = new IMControl(this);
            Components[ComponentID.Chat]     = new ChatControl(this);
            Components[ComponentID.Mail]     = new MailControl(this);
            Components[ComponentID.Board]    = new BoardControl(this);
            Components[ComponentID.Plan]     = new PlanControl(this);
            Components[ComponentID.Storage]  = new StorageControl(this);

            User.Load(LoadModeType.Cache);

            if (Sim != null && Sim.Internet.FreshStart)
                foreach (ushort id in Components.Keys)
                {
                    string dirpath = User.RootPath + "\\Data\\" + id.ToString();
                    if (Directory.Exists(dirpath))
                        Directory.Delete(dirpath, true);
                }

            // trigger components to load
            Loading = true;
                LoadEvent.Invoke();
            Loading = false;

            Test test = new Test(); // should be empty unless running a test            
        }

		internal void SignOff()
		{
            //crit - reimplement signing off 
            /*
            if (Login != null)
                Login.Core = null;

            User.Save();

			lock(BuddyMap.SyncRoot)
				foreach(KimBuddy buddy in BuddyMap.Values)
					foreach(KimSession session in buddy.Sessions)
						if(session.Status == SessionStatus.Active)
							session.Send_Close("Signing Off");
			
			TcpControl.Shutdown();
			UdpControl.Shutdown();
            */
		}

		bool InTimer;

		internal void SecondTimer()
		{
            if (InTimer)
            {
                ConsoleLog("Timer pile-up");
                return;
            }

			InTimer = true; // this isnt mfc, prevent timer pile up

			try
			{
                RudpControl.SecondTimer();
                
                // networks
				GlobalNet.SecondTimer();
                OperationNet.SecondTimer();

                TimerEvent.Invoke();


                // save cache
                if (TimeNow > NextSaveCache)
                {
                    User.Save();
                    NextSaveCache = TimeNow.AddMinutes(5);
                }
			}
			catch(Exception ex)
			{
				ConsoleLog("Exception KimCore::SecondTimer_Tick: " + ex.Message);
			}
		
			InTimer = false;
		}

		internal void SetFirewallType(FirewallType type)
		{
			// check if already set
			if( Firewall == type)
				return;


			// if client previously blocked, cancel any current searches through proxy
			if(Firewall == FirewallType.Blocked)
				lock(GlobalNet.Searches.Active)
                    foreach (DhtSearch search in GlobalNet.Searches.Active)
						search.ProxyTcp = null;


			if(type == FirewallType.Open)
			{
                Firewall = FirewallType.Open; // do first, otherwise publish will fail
                
                OperationNet.FirewallChangedtoOpen();

                if (GlobalNet != null)
                {
                    GlobalNet.FirewallChangedtoOpen();

                    Locations.PublishGlobal();
                }

				ConsoleLog("Network Firewall status changed to Open");

                return;
			}

			if(type == FirewallType.NAT && Firewall != FirewallType.Open)
			{
                Firewall = FirewallType.NAT;
                
                OperationNet.FirewallChangedtoNAT();

                if (GlobalNet != null)
                    GlobalNet.FirewallChangedtoNAT();

				ConsoleLog("Network Firewall status changed to NAT");
				return;
			}

			if(type == FirewallType.Blocked)
			{
				// why is this being set (forced)
				//Debug.Assert(false);
			}
		}

		internal void ConsoleCommand(string command)
		{
            if (command == "clear")
                ConsoleText.Clear();
            if (command == "pause")
                PauseLog = !PauseLog;

            ConsoleLog("> " + command);

			try
			{
				string[] commands = command.Split(' ');

				if(commands.Length == 0)
					return;

				if(commands[0] == "testDht" && commands.Length == 2)
				{
					SHA1CryptoServiceProvider sha = new SHA1CryptoServiceProvider();
					byte[] hash = new byte[8];

					int count = Convert.ToInt32(commands[1]);

					for(int i = 0; i < count; i++)
					{
						//RSACryptoServiceProvider keys = new RSACryptoServiceProvider(1024);
						//RSAParameters rsaParams = keys.ExportParameters(false);
						//byte[] hash = sha.ComputeHash( rsaParams.Modulus );
						
						StrongRndGen.GetBytes(hash);
						UInt64 kid = BitConverter.ToUInt64(hash, 0);

						// create random contact
						DhtContact contact = new DhtContact(kid, 0, new IPAddress(0), 0, 0, TimeNow);
						
						// add to routing
						GlobalNet.Routing.Add(contact);
					}
				}

				if(commands[0] == "gc")
				{
					GC.Collect();
				}

				if(commands[0] == "killtcp")
				{
					/*ConsoleLog(TcpControl.Connections.Count.ToString() + " tcp sockets on list");

					lock(TcpControl.Connections.SyncRoot)
						foreach(TcpConnect connection in TcpControl.Connections)
							connection.CleanClose("Force Disconnect");*/
				}
				if(commands[0] == "fwstatus")
				{
					ConsoleLog("Status set to " + Firewall.ToString());
				}


				if(commands[0] == "fwset" && commands.Length > 1)
				{
					if(commands[1] == "open")
						SetFirewallType(FirewallType.Open);
					if(commands[1] == "nat")
						SetFirewallType(FirewallType.NAT);
					if(commands[1] == "blocked")
						SetFirewallType(FirewallType.Blocked);
				}

				if(commands[0] == "listening")
				{
					/*ConsoleLog("Listening for TCP on port " + TcpControl.ListenPort.ToString());
					ConsoleLog("Listening for UDP on port " + UdpControl.ListenPort.ToString());*/
				}

				if(commands[0] == "ping" && commands.Length > 0)
				{
					//string[] addr = commands[1].Split(':');

                    //GlobalNet.Send_Ping(IPAddress.Parse(addr[0]), Convert.ToUInt16(addr[1]));
				}

                if (commands[0] == "tcptest" && commands.Length > 0)
                {
                    string[] addr = commands[1].Split(':');

                    //TcpControl.MakeOutbound(IPAddress.Parse(addr[0]), Convert.ToUInt16(addr[1]),0);
                }
			}
			catch(Exception ex)
			{
				ConsoleLog("Exception " + ex.Message);
			}
		}

        internal struct LastInputInfo
        {
            internal int Size;
            internal int Time;
        }


        internal int GetIdleTime()
        {
            try
            {
                LastInputInfo info = new LastInputInfo();
                info.Size = System.Runtime.InteropServices.Marshal.SizeOf(info);

                if (GetLastInputInfo(ref info))
                {
                    // Got it, return idle time in minutes
                    return (Environment.TickCount - info.Time) / 1000 / 60;
                }
            }
            catch
            {
            }

            return 0;
        }

        internal void ConsoleLog( string message)
        {
            ConsoleText.Enqueue(message);

            while (ConsoleText.Count > 500)
                ConsoleText.Dequeue();

            if (GuiConsole != null)
                GuiConsole.BeginInvoke(GuiConsole.UpdateConsole, message);
        }

        internal DateTime TimeNow
        {
            get
            {
                if (Sim == null)
                    return DateTime.Now;

                return Sim.Internet.TimeNow;
            }
        }

        internal void IndexKey(ulong id, ref byte[] key)
        {
            if (KeyMap.ContainsKey(id))
            {
                if (Utilities.MemCompare(KeyMap[id], key))
                    key = KeyMap[id]; // save memory by using single key throughout app
                else
                    throw new Exception("ID/Key entry does not match checked pair");
            }
            else
            {
                if (id != Utilities.KeytoID(key))
                    throw new Exception("ID check failed");

                KeyMap[id] = key;
            }
        }

        internal void InvokeInterface(Delegate method, params object[] args)
        {
            if (method == null || GuiMain == null)
                return;

            GuiMain.BeginInvoke(method, args);
        }

        internal string GetTempPath()
        {
            string path = "";

            while (true)
            {
                byte[] rnd = new byte[16];
                RndGen.NextBytes(rnd);

                path = User.TempPath + "\\" + Utilities.BytestoHex(rnd);

                if ( !File.Exists(path) )
                    break;
            }

            return path;
        }



        internal void TestNewsUpdate()
        {
            for(int x = 1; x <= 10; x++)
                InvokeInterface(NewsUpdate, x.ToString(), 0, 0);
        }
    }
}
