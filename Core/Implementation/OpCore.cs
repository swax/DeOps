using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using DeOps.Services;
using DeOps.Services.Assist;
using DeOps.Services.Board;
using DeOps.Services.Buddy;
using DeOps.Services.Chat;
using DeOps.Services.IM;
using DeOps.Services.Location;
using DeOps.Services.Mail;
using DeOps.Services.Plan;
using DeOps.Services.Profile;
using DeOps.Services.Share;
using DeOps.Services.Storage;
using DeOps.Services.Transfer;
using DeOps.Services.Trust;
using DeOps.Services.Update;
using DeOps.Services.Voice;

using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Comm;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Protocol.Special;
using DeOps.Implementation.Transport;

using DeOps.Simulator;

using DeOpsCore.Utilities;

namespace DeOps.Implementation
{
	public enum FirewallType { Blocked, NAT, Open };
    public enum TransportProtocol { Tcp, Udp, LAN, Rudp, Tunnel };

    public delegate void NewsUpdateHandler(uint serviceID, string message, ulong userID, uint project, bool showRemote);
    public delegate void KeepDataHandler();

    public delegate List<MenuItemInfo> MenuRequestHandler(InterfaceMenuType menuType, ulong key, uint proj);


    [DebuggerDisplay("{User.Settings.UserName}")]
	public class OpCore
	{
        // super-classes
        public DeOpsContext Context;
        public SimInstance Sim;

        // sub-classes
		public OpUser    User; // null on lookup network
        public DhtNetwork  Network;

        // services
        public TrustService    Trust;
        public LocationService Locations;
        public BuddyService    Buddies;
        public TransferService Transfers;
        public LocalSync       Sync;
        public UpdateService   Update;


        public ushort DhtServiceID = 0;
        public Dictionary<uint, OpService> ServiceMap = new Dictionary<uint, OpService>();

        public int RecordBandwidthSeconds = 5;
        public Dictionary<uint, BandwidthLog> ServiceBandwidth = new Dictionary<uint, BandwidthLog>();
        
        // properties
        public IPAddress LocalIP = IPAddress.Parse("127.0.0.1");
        public FirewallType Firewall = FirewallType.Blocked;

        public UInt64       UserID { get { return Network.Local.UserID; } }
        public ushort       TunnelID;
        public DateTime     StartTime;

        int KeyMax = 128;
        public ThreadedDictionary<ulong, string> NameMap = new ThreadedDictionary<ulong, string>();
        public Dictionary<ulong, byte[]> KeyMap = new Dictionary<ulong, byte[]>();

        // events
        public event Action<OpCore> Exited;
        public event Action SecondTimerEvent;

        int MinuteCounter; // random so all of network doesnt burst at once
        public event Action MinuteTimerEvent;
        public event NewsUpdateHandler NewsUpdate;

        public event KeepDataHandler KeepDataGui; // event for gui thread
        public event KeepDataHandler KeepDataCore; // event for core thread

        public event Func<string, string, bool> ShowConfirm;
        public event Action<string> ShowMessage;
        public event Func<ThreatLevel, bool> VerifyPass;

        public event Action<Delegate, object[]> RunInGui;
        public event Action<string> UpdateConsole;

        // only safe to use this from core_minuteTimer because updated 2 secs before it
        // every min keep data is updated and then pruning is done on public data structures
        // local data that is in use by views or we are caching and shouldn't be removed by the pruning process
        public ThreadedDictionary<ulong, bool> KeepData = new ThreadedDictionary<ulong, bool>();

        public G2Protocol    GuiProtocol; // used for encoding from the gui thread
        public bool DebugWindowsActive;

        // logs
        public bool PauseLog;
        public Queue ConsoleText = Queue.Synchronized(new Queue());
      

        // other
        public Random RndGen = new Random();
        public RNGCryptoServiceProvider StrongRndGen = new RNGCryptoServiceProvider();
        public LipsumGenerator TextGen = new LipsumGenerator();

        // threading
        Thread CoreThread;
        bool   CoreRunning = true;
        bool   RunTimer;
        public AutoResetEvent ProcessEvent = new AutoResetEvent(false);
        public Queue<AsyncCoreFunction> CoreMessages = new Queue<AsyncCoreFunction>();


        // initializing operation network
        public OpCore(DeOpsContext context, string userPath, string pass)
        {
            Context = context;
            Sim = context.Sim;

            StartTime = TimeNow;
            GuiProtocol = new G2Protocol();

            User = new OpUser(userPath, pass, this);
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

            if (Sim != null) KeyMax = 32;

            Context.KnownServices[DhtServiceID] = "Dht";
            ServiceBandwidth[DhtServiceID] = new BandwidthLog(RecordBandwidthSeconds);

            // permanent - order is important here
            AddService(new TransferService(this));
            AddService(new LocationService(this));
            AddService(new LocalSync(this));
            AddService(new BuddyService(this));
            AddService(new UpdateService(this));

            if (!User.Settings.GlobalIM)
                AddService(new TrustService(this));


            // optional
            AddService(new IMService(this));
            AddService(new ChatService(this));
            AddService(new ShareService(this));

            if (Type.GetType("Mono.Runtime") == null)
                AddService(new VoiceService(this));
            
            if (!User.Settings.GlobalIM)
            { 
                AddService(new ProfileService(this));
                AddService(new MailService(this));
                AddService(new BoardService(this));
                AddService(new PlanService(this));
                AddService(new StorageService(this));
            }

            if (Sim != null)
                Sim.Internet.RegisterAddress(this);

            CoreThread = new Thread(RunCore);
            CoreThread.Name = User.Settings.Operation + " Thread";

            if (Sim == null || Sim.Internet.TestCoreThread)
                CoreThread.Start();

#if DEBUG
            DebugWindowsActive = true;
#endif
        }

        // initializing lookup network (from the settings of a loaded operation)
        public OpCore(DeOpsContext context)
        {
            Context = context;
            Sim = context.Sim;

            StartTime = TimeNow;
            GuiProtocol = new G2Protocol();

            Network = new DhtNetwork(this, true);

            // for each core, re-load the lookup cache items
            Context.Cores.LockReading(() =>
            {
                foreach (OpCore core in Context.Cores)
                    core.User.Load(LoadModeType.LookupCache);
            });

            ServiceBandwidth[DhtServiceID] = new BandwidthLog(RecordBandwidthSeconds);

            // get cache from all loaded cores
            AddService(new LookupService(this));

            if (Sim != null)
                Sim.Internet.RegisterAddress(this);
            
            CoreThread = new Thread(RunCore);
            CoreThread.Name = "Lookup Thread";

            if (Sim == null || Sim.Internet.TestCoreThread)
                CoreThread.Start();
        }

        private void AddService(OpService service)
        {
            if (ServiceMap.ContainsKey(service.ServiceID))
                throw new Exception("Duplicate Service Added");

            ServiceMap[service.ServiceID] = service;

            ServiceBandwidth[service.ServiceID] = new BandwidthLog(RecordBandwidthSeconds);

            Context.KnownServices[service.ServiceID] = service.Name;
        }

        public string GetServiceName(uint id)
        {
            if (id == 0)
                return "DHT";

            if (ServiceMap.ContainsKey(id))
                return ServiceMap[id].Name;

            return id.ToString();
        }

        public OpService GetService(uint id)
        {
            if (ServiceMap.ContainsKey(id))
                return ServiceMap[id];

            return null;
        }

        void RunCore()
        {
            // timer / network events are brought into this thread so that locking between network/core/components is minimized
            // so only place we need to be real careful is at the core/gui interface

            bool keepGoing = false;


            while (CoreRunning && Context.ContextThread.IsAlive)
            {
                if (!keepGoing)
                    ProcessEvent.WaitOne(1000, false); // if context crashes this will release us

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
                            Network.ReceivePacket(Network.IncomingPackets.Dequeue());

                            keepGoing = true;
                        }
                }
                catch (Exception ex)
                {
                    Network.UpdateLog("Core Thread", ex.Message + "\n" + ex.StackTrace);
                }
            }
        }

		public void SecondTimer()
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

                if (SecondTimerEvent != null)
                    SecondTimerEvent.Invoke();

                // service bandwidth
                foreach (BandwidthLog buffer in ServiceBandwidth.Values)
                    buffer.NextSecond();

                // before minute timer give gui 2 secs to tell us of nodes it doesnt want removed
                if (KeepDataCore != null && MinuteCounter == 58)
                {
                    KeepData.SafeClear();

                    KeepDataCore.Invoke();
                    RunInGuiThread(KeepDataGui);
                }


                MinuteCounter++;

                if (MinuteCounter == 60)
                {
                    MinuteCounter = 0;
                    
                    if(MinuteTimerEvent != null)
                        MinuteTimerEvent.Invoke();

                    // prune keys from keymap - dont remove focused, remove furthest first
                    if(KeyMap.Count > KeyMax)
                        foreach (ulong user in (from id in KeyMap.Keys
                                                where !KeepData.SafeContainsKey(id) &&
                                                      !Network.RudpControl.SessionMap.Values.Any(socket => socket.UserID == id) &&
                                                      !Network.TcpControl.SocketList.Any(socket => socket.UserID == id)
                                                orderby Network.Local.UserID ^ id descending
                                                select id).Take(KeyMap.Count - KeyMax).ToArray())
                        {
                            KeyMap.Remove(user);
                            if (NameMap.SafeContainsKey(user))
                                NameMap.SafeRemove(user);
                        }
                }
			}
			catch(Exception ex)
			{
				ConsoleLog("Exception OpCore::SecondTimer_Tick: " + ex.Message);
			}
		}

		public void ConsoleCommand(string command)
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
					int count = Convert.ToInt32(commands[1]);

					for(int i = 0; i < count; i++)
					{
                        RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

                        UInt64 kid = Utilities.StrongRandUInt64(rng);

						// create random contact
						DhtContact contact = new DhtContact(kid, 7, new IPAddress(7), 7, 7);
						
						// add to routing
						Network.Routing.Add(contact);
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

        // firewall set at core level so that networks can exist on internet and on public LANs simultaneously
        public void SetFirewallType(FirewallType type)
        {
            // check if already set
            if (Firewall == type)
                return;


            // if client previously blocked, cancel any current searches through proxy
            if (Firewall == FirewallType.Blocked)
                lock (Network.Searches.Active)
                    foreach (DhtSearch search in Network.Searches.Active)
                        search.ProxyTcp = null;

            Firewall = type;

            if (type == FirewallType.Open)
                Network.FirewallChangedtoOpen();

            if (type == FirewallType.NAT)
                Network.FirewallChangedtoNAT();

            if (type == FirewallType.Blocked)
            {

            }

            string message = "Firewall changed to " + type.ToString();
            Network.UpdateLog("Network", message);
            Network.UpdateLog("general", message);
        }

        /*public struct LastInputInfo
        {
            public int Size;
            public int Time;
        }*/


        /*public int GetIdleTime()
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

        public void ConsoleLog( string message)
        {
            ConsoleText.Enqueue(message);

            while (ConsoleText.Count > 500)
                ConsoleText.Dequeue();

            RunInGuiThread(UpdateConsole, message);
        }

        public DateTime TimeNow
        {
            get
            {
                if (Sim == null)
                    return DateTime.Now;

                return Sim.Internet.TimeNow;
            }
        }

        public void IndexKey(ulong id, ref byte[] key)
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

        public void IndexName(ulong user, string name)
        {
            Debug.Assert(name != null && name != "");
            if (name == null || name.Trim() == "") 
                return;

            if (NameMap.SafeContainsKey(user))
                return;

            NameMap.SafeAdd(user, name);
        }

        // ensure that key/name associations persist between runs, done so remote people dont change their name and try to play with
        // us, once we make an association with a key, we change that name on our terms, also prevents key spoofing with dupe
        // user ids
        public void SaveKeyIndex(PacketStream stream)
        {
            NameMap.LockReading(() =>
            {
                foreach (ulong user in KeyMap.Keys)
                    if (NameMap.ContainsKey(user))
                    {
                        Debug.Assert(NameMap[user] != null);
                        if (NameMap[user] != null)
                            stream.WritePacket(new UserInfo() { Name = NameMap[user], Key = KeyMap[user] });
                    }
            });
        }

        public void IndexInfo(UserInfo info)
        {
            Debug.Assert(info.Name != null);
            if (info.Name == null)
                return;

            KeyMap[info.ID] = info.Key;
            NameMap.SafeAdd(info.ID, info.Name);
        }

        public string GetName(ulong user)
        {
            string name;
            if (NameMap.SafeTryGetValue(user, out name))
                return name;

            name = user.ToString();

            return (name.Length > 5) ? name.Substring(0, 5) : name;
        }

        // used for debugging - Queue<Delegate> LastEvents = new Queue<Delegate>();

        public void RunInGuiThread(Delegate method, params object[] args)
        {
            if (RunInGui != null)
                RunInGui(method, args);
        }

        public string GetTempPath()
        {
            string path = "";

            while (true)
            {
                byte[] rnd = new byte[16];
                RndGen.NextBytes(rnd);

                path = User.TempPath + Path.DirectorySeparatorChar + Utilities.ToBase64String(rnd);

                if ( !File.Exists(path) )
                    break;
            }

            return path;
        }


        public bool NewsWorthy(ulong id, uint project, bool localRegionOnly)
        {
            //crit - if in buddy list, if non-local self
            //should really be done per compontnt (board only cares about local, mail doesnt care at all, neither does chat)
    
            // if not self, higher, adjacent or lower direct then true
            if (id == UserID || Trust.LocalTrust == null)
                return false;

            if(!localRegionOnly && Trust.IsHigher(id, project))
                return true;
            
            if(localRegionOnly && Trust.IsHigherDirect(id, project))
                return true;

            if(Trust.IsAdjacent(id, project))
                return true;

            if (Trust.IsLowerDirect(id, project))
                return true;

            return false;

        }

        public void MakeNews(uint service, string message, ulong userID, uint project, bool showRemote)
        {
            // use self id because point of news is alerting user to changes in their *own* interfaces
            RunInGuiThread(NewsUpdate, service, message, userID, project, showRemote);
        }

        public void Exit()
        {
            // if main interface not closed (triggered from auto-update) then properly close main window
            // let user save files etc..
            // do this before shutting down services so they're still available to do clean up
            if (Exited != null)
                Exited(this);


            CoreRunning = false;

            if(CoreThread != null && CoreThread.IsAlive)
            {
                ProcessEvent.Set();
                CoreThread.Join();
            }
            CoreThread = null;

            try
            {
                if (Network.IsLookup)
                    Network.Lookup.Save(this);
                else
                    User.Save();


                foreach (OpService service in ServiceMap.Values)
                    service.Dispose();

                Network.UPnPControl.Shutdown();
                Network.RudpControl.Shutdown();
                Network.UdpControl.Shutdown();
                Network.LanControl.Shutdown();
                Network.TcpControl.Shutdown();


                ServiceMap.Clear();
                ServiceBandwidth.Clear();


                if (Sim != null)
                    Sim.Internet.UnregisterAddress(this);
            }
            catch
            {
                Debug.Assert(false);
            }
        }


        public bool InvokeRequired
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

                    // if not testing individual core thread, its the pump thread that controls the core thread
                    if (!Sim.Internet.TestCoreThread)
                        return Sim.Internet.PumpThreads[Sim.ThreadIndex].ManagedThreadId != Thread.CurrentThread.ManagedThreadId;
                }

                return CoreThread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId ;
            }
        }

        // be careful if calling this with loop objects, reference will be changed by the time async executes
        public void RunInCoreAsync(Action code)
        {
            RunInCoreThread(code, null);
        }

        public void RunInCoreBlocked(Action code)
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

            lock (CoreMessages)
                if (CoreMessages.Count < 100)
                    CoreMessages.Enqueue(function);
      
            ProcessEvent.Set();

            return function;
        }

        public void ResizeBandwidthRecord(int seconds)
        {
            if(InvokeRequired)
            {
                RunInCoreAsync(delegate() { ResizeBandwidthRecord(seconds); } );
                return;
            }

            // services
            foreach (BandwidthLog log in ServiceBandwidth.Values)
                log.Resize(seconds);

            // transport
            foreach (TcpConnect tcp in Network.TcpControl.SocketList)
                tcp.Bandwidth.Resize(seconds);

            Network.UdpControl.Bandwidth.Resize(seconds);

            foreach (RudpSession session in Network.RudpControl.SessionMap.Values)
                session.Comm.Bandwidth.Resize(seconds);

            RecordBandwidthSeconds = seconds; // do this last to ensure all buffers set
        }

        public void RenameUser(ulong user, string name)
        {
            if (InvokeRequired)
            {
                RunInCoreAsync(() => RenameUser(user, name));
                return;
            }

            NameMap.SafeAdd(user, name);

            // update services with new name
            if (Trust != null)
            {
                if (user == UserID)
                {
                    Trust.LocalTrust.Name = name;
                    Trust.SaveLocal();
                }

                RunInGuiThread(Trust.GuiUpdate, user);
            }

            OpBuddy buddy;
            if(Buddies.BuddyList.SafeTryGetValue(user, out buddy))
            {
                buddy.Name = name;
                Buddies.SaveLocal();

                RunInGuiThread(Buddies.GuiUpdate);
            }
        }

        public string GetIdentity(ulong user)
        {
            if (!KeyMap.ContainsKey(user))
                return "User Public Key Unknown";

            IdentityLink link = new IdentityLink()
            {
                Name = GetName(user),
                OpName = User.Settings.Operation,
                PublicOpID = User.Settings.PublicOpID,
                PublicKey = KeyMap[user]
            };

            string test = link.Encode();
            IdentityLink check = IdentityLink.Decode(test);

            Debug.Assert(Utilities.MemCompare(link.PublicOpID, check.PublicOpID) &&
                         Utilities.MemCompare(link.PublicKey, check.PublicKey));

            return link.Encode();
        }

        public string GetMyAddress()
        {
            return CreateBootstrapLink(UserID, LocalIP, Network.TcpControl.ListenPort, Network.UdpControl.ListenPort);
        }

        public string CreateBootstrapLink(DhtContact contact)
        {
            return CreateBootstrapLink(contact.UserID, contact.IP, contact.TcpPort, contact.UdpPort);
        }

        public string CreateBootstrapLink(ulong userid, IPAddress ip, ushort tcpPort, ushort udpPort)
        {
            // deops://opname/bootstrap/pubOpId:userId/ip:tcp:udp

            string link = string.Format("deops://{0}/bootstrap/{1}:{2}/{3}:{4}:{5}",
                                        (User != null) ? WebUtility.UrlEncode(User.Settings.Operation) : "lookup",
                                        (User != null) ? Utilities.BytestoHex(User.Settings.PublicOpID) : "0",
                                        Utilities.BytestoHex(BitConverter.GetBytes(userid)),
                                        ip,
                                        tcpPort,
                                        udpPort);

            return link;
        }

        public string GenerateInvite(string pubLink, out string name)
        {
            IdentityLink ident = IdentityLink.Decode(pubLink);

            // dont check opID, because invites come from different ops

            name = ident.Name;
            
            // deops://firesoft/invite/person@GlobalIM/originalopID~invitedata {op key web caches ips}

            string link = string.Format("deops://{0}/invite/{1}@{2}/",
                            WebUtility.UrlEncode(User.Settings.Operation),
                            WebUtility.UrlEncode(ident.Name),
                            WebUtility.UrlEncode(ident.OpName));

            // encode invite info in user's public key
            byte[] data = new byte[4096];
            MemoryStream mem = new MemoryStream(data);
            PacketStream stream = new PacketStream(mem, GuiProtocol, FileAccess.Write);

            // write invite
            OneWayInvite invite = new OneWayInvite();
            invite.UserName = ident.Name;
            invite.OpName = User.Settings.Operation;
            invite.OpAccess = User.Settings.OpAccess;
            invite.OpID = User.Settings.OpKey;

            stream.WritePacket(invite);

            // write some contacts
            foreach (DhtContact contact in Network.Routing.GetCacheArea())
            {
                byte[] bytes = contact.Encode(GuiProtocol, InvitePacket.Contact);
                mem.Write(bytes, 0, bytes.Length);
            }

            // write web caches
            foreach (WebCache cache in Network.Cache.GetLastSeen(3))
                stream.WritePacket(new WebCache(cache, InvitePacket.WebCache));

            mem.WriteByte(0); // end packets

            byte[] packets = Utilities.ExtractBytes(data, 0, (int)mem.Position);
            byte[] encrypted = Utilities.KeytoRsa(ident.PublicKey).Encrypt(packets,false);

            // ensure that this link is opened from the original operation remote's public key came from
            byte[] final = Utilities.CombineArrays(ident.PublicOpID, encrypted);

            return link + Utilities.BytestoHex(final);
        }

        public static InvitePackage OpenInvite(byte[] decrypted, G2Protocol protocol)
        {
            
            // if we get down here, opening invite was success

            MemoryStream mem = new MemoryStream(decrypted);
            PacketStream stream = new PacketStream(mem, protocol, FileAccess.Read);

            InvitePackage package = new InvitePackage();

            G2Header root = null;
            while (stream.ReadPacket(ref root))
            {
                if (root.Name == InvitePacket.Info)
                    package.Info = OneWayInvite.Decode(root);

                if (root.Name == InvitePacket.Contact)
                    package.Contacts.Add(DhtContact.ReadPacket(root));

                if (root.Name == InvitePacket.WebCache)
                    package.Caches.Add(WebCache.Decode(root));
            }

            return package;
        }

        public void ProcessInvite(InvitePackage invite)
        {
            // add nodes to ipcache in processing
            foreach (DhtContact contact in invite.Contacts)
                Network.Cache.AddContact(contact);

            foreach (WebCache cache in invite.Caches)
                Network.Cache.AddWebCache(cache);
        }

        public bool UserConfirm(string message, string title)
        {
            if (ShowConfirm != null)
                return ShowConfirm(message, title);

            return false;
        }

        public void UserMessage(string message)
        {
            if (ShowMessage != null)
                ShowMessage(message);
        }

        public bool UserVerifyPass(ThreatLevel threatLevel)
        {
            if (VerifyPass != null)
                return VerifyPass(threatLevel);

            return false;
        }
    }

    public class IdentityLink
    {
        public string Name;
        public string OpName;
        public byte[] PublicOpID;
        public byte[] PublicKey;

        // deops://opname/ident/name/opid:publickey

        public string Encode()
        {
            string link = string.Format("deops://{0}/identity/{1}/{2}:{3}",
                                        WebUtility.UrlEncode(OpName),
                                        WebUtility.UrlEncode(Name),
                                        Utilities.BytestoHex(PublicOpID),
                                        Utilities.BytestoHex(PublicKey));

            return link;
        }

        public static IdentityLink Decode(string link)
        {
            if (link.StartsWith("deops://"))
                link = link.Substring(8);
            else
                throw new Exception("Invalid Link");

            string[] mainParts = link.Split('/', ':');
            if (mainParts.Length < 4 || mainParts[1] != "identity")
                throw new Exception("Invalid Link");

            IdentityLink ident = new IdentityLink();

            ident.OpName = WebUtility.UrlDecode(mainParts[0]);
            ident.Name = WebUtility.UrlDecode(mainParts[2]);
            ident.PublicOpID = Utilities.HextoBytes(mainParts[3]);
            ident.PublicKey = Utilities.HextoBytes(mainParts[4]);

            return ident;
        }
    }


    /* signe identity link - doesnt work because we need to get *other* people's links all the time, even drag/drop needs to gen ident links
    public class IdentityLink
    {
        public string Name;
        public string OpName;
        public byte[] OpID;
        public byte[] PublicKey;

        // deops://opname/ident/name/opid~publickey/sig


        public string Encode(OpCore core)
        {
            string link = "deops://" + OpName + "/ident/" + Name + "/";

            byte[] totalKey = Utilities.CombineArrays(OpID, PublicKey);
            link += Utilities.ToBase64String(totalKey);

            byte[] sig = core.User.Settings.KeyPair.SignData(UTF8Encoding.UTF8.GetBytes(link), new SHA1CryptoServiceProvider());
            link += "/" + Utilities.ToBase64String(sig);

            return link;
        }

        public static IdentityLink Decode(string link)
        {
            if (!link.StartsWith("deops://"))
               throw new Exception("Invalid Link");

            string[] mainParts = link.Substring(8).Split('/');

            if (mainParts.Length < 5 || mainParts[1] != "ident")
                throw new Exception("Invalid Link");

            IdentityLink ident = new IdentityLink();

            ident.Name = mainParts[2];
            ident.OpName = mainParts[0];

            byte[] totalKey = Utilities.FromBase64String(mainParts[3]);
            ident.OpID = Utilities.ExtractBytes(totalKey, 0, 8);
            ident.PublicKey = Utilities.ExtractBytes(totalKey, 8, totalKey.Length - 8);

            // check signature
            byte[] sig = Utilities.FromBase64String(mainParts[4]);

            string check = link.Substring(0, link.LastIndexOf('/'));

            if (!Utilities.CheckSignedData(ident.PublicKey, UTF8Encoding.UTF8.GetBytes(check), sig))
                throw new Exception("Link Integrity Check Failed");

            return ident;
        }
    }*/

    public class InvitePackage
    {
        public OneWayInvite Info;
        public List<DhtContact> Contacts = new List<DhtContact>();
        public List<WebCache> Caches = new List<WebCache>();

        public InvitePackage()
        {
        }
    }

    public class AsyncCoreFunction
    {
        public Delegate Method;
        public object[] Args;
        public object   Result;

        public bool Completed;
        public ManualResetEvent Processed = new ManualResetEvent(false);


        public AsyncCoreFunction(Delegate method, params object[] args)
        {
            Method = method;
            Args = args;
        }
    }
}
