using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using RiseOp.Services;
using RiseOp.Services.Assist;
using RiseOp.Services.Board;
using RiseOp.Services.Chat;
using RiseOp.Services.IM;
using RiseOp.Services.Location;
using RiseOp.Services.Mail;
using RiseOp.Services.Plan;
using RiseOp.Services.Profile;
using RiseOp.Services.Storage;
using RiseOp.Services.Transfer;
using RiseOp.Services.Trust;

using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Comm;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Protocol.Special;
using RiseOp.Implementation.Transport;

using RiseOp.Interface;
using RiseOp.Interface.Tools;
using RiseOp.Interface.Views;

using RiseOp.Simulator;


namespace RiseOp.Implementation
{
	internal enum FirewallType { Blocked, NAT, Open };
    internal enum TransportProtocol { Tcp, Udp, Rudp, Tunnel };


    internal delegate void LoadHandler();
    internal delegate void ExitHandler();
    internal delegate void TimerHandler();
    internal delegate void NewsUpdateHandler(NewsItemInfo info);
    internal delegate void GetFocusedHandler();

    internal delegate List<MenuItemInfo> MenuRequestHandler(InterfaceMenuType menuType, ulong key, uint proj);


    [DebuggerDisplay("{User.Settings.UserName}")]
	internal class OpCore
	{
        // super-classes
        internal RiseOpContext Context;
        internal SimInstance Sim;

        // sub-classes
		internal Identity    User;
        internal DhtNetwork  Network;

        // services
        internal TrustService    Links;
        internal LocationService Locations;
        internal GlobalService   GlobalLoc;
        internal TransferService Transfers;
        internal LocalSync       Sync;


        internal ushort DhtServiceID = 0;
        internal Dictionary<uint, OpService> ServiceMap = new Dictionary<uint, OpService>();


		// properties
        internal UInt64       UserID { get { return Network.Local.UserID; } }
        internal ushort       TunnelID;
        internal DateTime     StartTime;

        internal Dictionary<ulong, byte[]> KeyMap = new Dictionary<ulong, byte[]>();

        // events
        internal event TimerHandler SecondTimerEvent;

        int MinutePoint; // random so all of network doesnt burst at once
        internal event TimerHandler MinuteTimerEvent;
        internal event NewsUpdateHandler NewsUpdate;

        internal event GetFocusedHandler GetFocusedGui;
        internal event GetFocusedHandler GetFocusedCore;
        // only safe to use this from core_minuteTimer because updated 2 secs before it
        internal ThreadedDictionary<ulong, bool> Focused = new ThreadedDictionary<ulong, bool>();

        // interfaces
        internal MainForm      GuiMain;
        internal TrayLock      GuiTray;
        internal ConsoleForm   GuiConsole;
        internal InternalsForm GuiInternal;
        internal G2Protocol    GuiProtocol;


        // logs
        internal bool PauseLog;
        internal Queue ConsoleText = Queue.Synchronized(new Queue());
      

        // other
        internal Random RndGen = new Random(unchecked((int)DateTime.Now.Ticks));
        internal RNGCryptoServiceProvider StrongRndGen = new RNGCryptoServiceProvider();

        // threading
        Thread CoreThread;
        bool   CoreRunning = true;
        bool   RunTimer;
        internal AutoResetEvent ProcessEvent = new AutoResetEvent(false);
        Queue<AsyncCoreFunction> CoreMessages = new Queue<AsyncCoreFunction>();

      

        // initializing operation network
        internal OpCore(RiseOpContext context, string path, string pass)
        {
            Context = context;
            Sim = context.Sim;

            StartTime = TimeNow;
            MinutePoint = RndGen.Next(2, 59);
            GuiProtocol = new G2Protocol();

            ConsoleLog("RiseOp " + Application.ProductVersion);

            User = new Identity(path, pass, this);
            User.Load(LoadModeType.Settings);

            Network = new DhtNetwork(this, false);

            TunnelID = (ushort)RndGen.Next(1, ushort.MaxValue);

            Test test = new Test(); // should be empty unless running a test    

            User.Load(LoadModeType.AllCaches);

            // delete data dirs if frsh start indicated
            if (Sim != null && Sim.Internet.FreshStart)
                for (int service = 1; service < 20; service++ ) // 0 is temp folder, cleared on startup
                {
                    string dirpath = User.RootPath + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + service.ToString();
                    if (Directory.Exists(dirpath))
                        Directory.Delete(dirpath, true);
                }

            // permanent - order is important here
            AddService(new TransferService(this));
            AddService(new LocationService(this));
            AddService(new LocalSync(this));
            AddService(new TrustService(this));
 

            // optional
            AddService(new IMService(this));
            AddService(new ChatService(this));
            AddService(new ProfileService(this));
            AddService(new MailService(this));
            AddService(new BoardService(this));
            AddService(new PlanService(this));
            AddService(new StorageService(this));

            if (Sim != null)
                Sim.Internet.RegisterAddress(this);

            CoreThread = new Thread(RunCore);
            
            if (Sim == null || Sim.Internet.TestCoreThread)
                CoreThread.Start();
        }

        // initializing global network (from the settings of a loaded operation)
        internal OpCore(RiseOpContext context)
        {
            Context = context;
            Sim = context.Sim;

            StartTime = TimeNow;
            MinutePoint = RndGen.Next(2, 59);
            GuiProtocol = new G2Protocol();

            Network = new DhtNetwork(this, true);

            // for each core, re-load the global cache items
            Context.Cores.LockReading(delegate()
            {
                foreach (OpCore core in Context.Cores)
                    core.User.Load(LoadModeType.GlobalCache);
            });

            // get cache from all loaded cores
            AddService(new GlobalService(this));

            if (Sim != null)
                Sim.Internet.RegisterAddress(this);
            
            CoreThread = new Thread(RunCore);

            if (Sim == null || Sim.Internet.TestCoreThread)
                CoreThread.Start();
        }

        private void AddService(OpService service)
        {
            if (ServiceMap.ContainsKey(service.ServiceID))
                throw new Exception("Duplicate Service Added");

            ServiceMap[service.ServiceID] = service;
        }

        private void RemoveService(uint id)
        {
            if (!ServiceMap.ContainsKey(id))
                return;

            ServiceMap[id].Dispose();

            ServiceMap.Remove(id);
        }

        internal string GetServiceName(uint id)
        {
            if (id == 0)
                return "DHT";

            if (ServiceMap.ContainsKey(id))
                return ServiceMap[id].Name;

            return id.ToString();
        }

        internal OpService GetService(string name)
        {
            foreach (OpService service in ServiceMap.Values)
                if (service.Name == name)
                    return service;

            return null;
        }

        void RunCore()
        {
            // timer / network events are brought into this thread so that locking between network/core/components is minimized
            // so only place we need to be real careful is at the core/gui interface

            bool keepGoing = false;
 

            while (CoreRunning)
            {
                if (!keepGoing)
                    ProcessEvent.WaitOne();

                keepGoing = false;

                try
                {
                    AsyncCoreFunction function = null;

                    // process invoked functions, dequeue quickly to continue processing
                    lock (CoreMessages)
                        if (CoreMessages.Count > 0)
                            function = CoreMessages.Dequeue();

                    if (function != null)
                    {
                        function.Result = function.Method.DynamicInvoke(function.Args);
                        function.Completed = true;
                        function.Processed.Set();

                        keepGoing = true;
                    }

                    // run timer, in packet loop so that if we're getting unbelievably flooded timer
                    // can still run and clear out component maps
                    if (RunTimer)
                    {
                        RunTimer = false;

                        SecondTimer();
                    }


                    // get the next packet off the queue without blocking the recv process
                    lock (Network.IncomingPackets)
                        if (Network.IncomingPackets.Count > 0)
                        {
                            Network.ReceivePacket(Network.IncomingPackets.Dequeue().Packet);

                            keepGoing = true;
                            break;
                        }
                }
                catch (Exception ex)
                {
                    Network.UpdateLog("Core Thread", ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

		internal void SecondTimer()
		{
            if (InvokeRequired)
            {
                RunTimer = true;
                ProcessEvent.Set();
                return;
            }

			try
			{
                // networks
                Network.SecondTimer();

                SecondTimerEvent.Invoke();

                // before minute timer give gui 2 secs to tell us of nodes it doesnt want removed
                if (GetFocusedCore != null && TimeNow.Second == MinutePoint - 2)
                {
                    Focused.SafeClear();

                    GetFocusedCore.Invoke();
                    RunInGuiThread(GetFocusedGui);
                }

                if (TimeNow.Second == MinutePoint)
                {
                    MinuteTimerEvent.Invoke();
                }
			}
			catch(Exception ex)
			{
				ConsoleLog("Exception KimCore::SecondTimer_Tick: " + ex.Message);
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
						/*RSACryptoServiceProvider keys = new RSACryptoServiceProvider(1024);
						//RSAParameters rsaParams = keys.ExportParameters(false);
						//byte[] hash = sha.ComputeHash( rsaParams.Modulus );
						
						StrongRndGen.GetBytes(hash);
						UInt64 kid = BitConverter.ToUInt64(hash, 0);

						// create random contact
						DhtContact contact = new DhtContact(kid, 0, new IPAddress(0), 0, 0);
						
						// add to routing
						GlobalNet.Routing.Add(contact);*/
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
                    ConsoleLog("Status set to " + Context.Firewall.ToString());
				}


				if(commands[0] == "fwset" && commands.Length > 1)
				{
					if(commands[1] == "open")
						Context.SetFirewallType(FirewallType.Open);
					if(commands[1] == "nat")
                        Context.SetFirewallType(FirewallType.NAT);
					if(commands[1] == "blocked")
                        Context.SetFirewallType(FirewallType.Blocked);
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

        /*internal struct LastInputInfo
        {
            internal int Size;
            internal int Time;
        }*/


        /*internal int GetIdleTime()
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
        }*/

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

        Queue<Delegate> LastEvents = new Queue<Delegate>();

        internal void RunInGuiThread(Delegate method, params object[] args)
        {
            if (method == null || GuiMain == null)
                return;

            LastEvents.Enqueue(method);
            while (LastEvents.Count > 10)
                LastEvents.Dequeue();


            GuiMain.BeginInvoke(method, args);
        }

        internal void InvokeView(bool external, ViewShell view)
        {
            if(external)
                RunInGuiThread(GuiMain.ShowExternal, view);
            else
                RunInGuiThread(GuiMain.ShowInternal, view);
        }

        internal string GetTempPath()
        {
            string path = "";

            while (true)
            {
                byte[] rnd = new byte[16];
                RndGen.NextBytes(rnd);

                path = User.TempPath + Path.DirectorySeparatorChar + Utilities.BytestoHex(rnd);

                if ( !File.Exists(path) )
                    break;
            }

            return path;
        }


        internal bool NewsWorthy(ulong id, uint project, bool localRegionOnly)
        {
            if (GuiMain == null)
                return false;

            //crit - if in buddy list, if non-local self
            //should really be done per compontnt (board only cares about local, mail doesnt care at all, neither does chat)
    
            // if not self, higher, adjacent or lower direct then true
            if (id == UserID)
                return false;

            if(!localRegionOnly && Links.IsHigher(id, project))
                return true;
            
            if(localRegionOnly && Links.IsHigherDirect(id, project))
                return true;

            if(Links.IsAdjacent(id, project))
                return true;

            if (Links.IsLowerDirect(id, project))
                return true;

            return false;

        }

        internal void MakeNews(string message, ulong id, uint project, bool showRemote, System.Drawing.Icon symbol, EventHandler onClick)
        {
            // use self id because point of news is alerting user to changes in their *own* interfaces
            RunInGuiThread(NewsUpdate, new NewsItemInfo(message, id, project, showRemote, symbol, onClick));
        }

        internal void Exit()
        {
            CoreRunning = false;

            if(CoreThread != null && CoreThread.IsAlive)
            {
                ProcessEvent.Set();
                CoreThread.Join();
                CoreThread = null;
            }

            //crit notify rudp / location signing off network

            if (Network.IsGlobal)
                Network.GlobalConfig.Save(this);
            else
                User.Save();

            foreach (OpService service in ServiceMap.Values)
                service.Dispose();

            ServiceMap.Clear();

            Network.TcpControl.Shutdown();
            Network.UdpControl.Shutdown();

            if (Sim != null)
                Sim.Internet.UnregisterAddress(this);

            Context.RemoveCore(this);
        }


        internal bool InvokeRequired
        {
            get 
            {
                if (CoreThread == null)
                    return false;

                // keep gui responsive if sim thread not active to process messages
                if (Sim != null && Sim.Internet.Paused && !Sim.Internet.TestCoreThread) 
                    return false;

                // in sim if not using core thread, then core thread is the sim thread
                if (Sim != null)
                {
                    if (Sim.Internet.RunThread == null) // core thread not started, run funtinos directly through
                        return false;

                    if (!Sim.Internet.TestCoreThread)
                        return Sim.Internet.RunThread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId;
                }

                return CoreThread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId ;
            }
        }

        internal void RunInCoreAsync(MethodInvoker code)
        {
            RunInCoreThread(code, null);
        }

        internal void RunInCoreBlocked(MethodInvoker code)
        {
            // if called from core thread, and blocked, this would result in a deadlock
            if (!InvokeRequired)
            {
                Debug.Assert(false);
                return;
            }

            RunInCoreThread(code, null).Processed.WaitOne();
        }

        AsyncCoreFunction RunInCoreThread(Delegate method, params object[] args)
        {
            AsyncCoreFunction function = new AsyncCoreFunction(method, args);

            if (Sim != null && !Sim.Internet.TestCoreThread)
            {
                lock (Sim.Internet.CoreMessages)
                    if (Sim.Internet.CoreMessages.Count < 100)
                        Sim.Internet.CoreMessages.Enqueue(function);
            }
            else
            {
                lock (CoreMessages)
                    if (CoreMessages.Count < 100)
                        CoreMessages.Enqueue(function);
            }

            ProcessEvent.Set();

            return function;
        }

        internal string CreateInvite(string password)
        {
            // generate invite packet
            OneWayInvite invite = new OneWayInvite();

            invite.OpName = User.Settings.Operation;
            invite.OpAccess = User.Settings.OpAccess;
            invite.OpID = User.Settings.OpKey.Key;
            invite.Contacts = new List<DhtContact>(Network.Routing.GetCacheArea());

            // encrypt packet with hashed password
            byte[] salt = new byte[4];
            RNGCryptoServiceProvider rnd = new RNGCryptoServiceProvider();
            rnd.GetBytes(salt);

            byte[] key = Utilities.GetPasswordKey(password, salt);

            byte[] encrypted = Utilities.EncryptBytes(invite.Encode(GuiProtocol), key);

            encrypted = Utilities.CombineArrays(salt, encrypted);

            // return base 64 link
            return "riseop://invite/" + Utilities.ToBase64String(encrypted);
        }

        internal static OneWayInvite OpenInvite(string link, string password)
        {
            byte[] encrypted = Utilities.FromBase64String(link);

            // decrypt packet with hashed password
            byte[] salt = Utilities.ExtractBytes(encrypted, 0, 4);
            encrypted = Utilities.ExtractBytes(encrypted, 4, encrypted.Length - 4);

            byte[] key = Utilities.GetPasswordKey(password, salt);

            byte[] decrypted = Utilities.DecryptBytes(encrypted, encrypted.Length, key);

            return OneWayInvite.Decode(decrypted);
        }

        internal void ProcessInvite(OneWayInvite invite)
        {
            // add nodes to ipcache in processing
            foreach (DhtContact contact in invite.Contacts)
                Network.AddCacheEntry(contact);
        }
    }

    internal class AsyncCoreFunction
    {
        internal Delegate Method;
        internal object[] Args;
        internal object   Result;

        internal bool Completed;
        internal ManualResetEvent Processed = new ManualResetEvent(false);


        internal AsyncCoreFunction(Delegate method, params object[] args)
        {
            Method = method;
            Args = args;
        }
    }
}
