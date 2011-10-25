using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Diagnostics;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Transport;

namespace DeOps.Simulator
{
    internal enum InstanceChangeType { Add, Remove, Update, Refresh };
    internal delegate void InstanceChangeHandler(SimInstance instance, InstanceChangeType type);
    internal delegate void UpdateViewHandler();

    internal class InternetSim
    {
        internal ThreadedList<SimInstance> Instances = new ThreadedList<SimInstance>();

        Random RndGen = new Random(unchecked((int)DateTime.Now.Ticks));

        internal InstanceChangeHandler InstanceChange;
        internal UpdateViewHandler UpdateView;

        // Sim
        internal Dictionary<IPEndPoint, DhtNetwork> TcpEndPoints = new Dictionary<IPEndPoint, DhtNetwork>();
        internal Dictionary<IPEndPoint, DhtNetwork> UdpEndPoints = new Dictionary<IPEndPoint, DhtNetwork>();
        Dictionary<IPAddress, SimInstance> SimMap = new Dictionary<IPAddress, SimInstance>();

        internal List<SimPacket>[] OutPackets = new List<SimPacket>[Environment.ProcessorCount];
        internal List<SimPacket>[] InPackets = new List<SimPacket>[Environment.ProcessorCount];

        internal Dictionary<ulong, string> UserNames = new Dictionary<ulong, string>();
        internal Dictionary<ulong, string> OpNames = new Dictionary<ulong, string>();

        internal ThreadedDictionary<TcpConnect, TcpConnect> TcpSourcetoDest = new ThreadedDictionary<TcpConnect, TcpConnect>();

        internal DateTime StartTime;
        internal DateTime TimeNow;

        internal int WebCacheHits;

        internal string LoadedPath;

        // thread
        internal Thread RunThread;
        internal bool Paused;
        bool   Step;
        internal bool   Shutdown;
        
        // settings
        internal int SleepTime = 250; // 1000 is realtime, 1000 / x = target secs to simulate per real sec

        internal bool LoadOnline = true;
        internal bool TestEncryption = false;
        internal bool FreshStart = false; // all service files deleted
        internal bool ClearIPCache = true;
        internal bool TestTcpFullBuffer = false;
        internal bool UseTimeFile = true; // keep time consistant between sim runs
        internal bool TestCoreThread = false; // sleepTime needs to be set with this so packets have time to process ayncronously
        internal bool Logging = true; // saves memory with large sim runs
        internal bool LAN = false; // cant sim few nodes on lan connected to internet, but can do net of all lan or of alll internet

        bool Flux        = false;
        //int  FluxIn      = 1;
        //int  FluxOut     = 0;

        int PercentNAT = 30;
        int PercentBlocked = 30;

        int InstanceCount = 1;

        // pump threads
        internal Thread[] PumpThreads = new Thread[Environment.ProcessorCount];
        AutoResetEvent[] PumpStart = new AutoResetEvent[Environment.ProcessorCount];
        AutoResetEvent[] PumpEnd = new AutoResetEvent[Environment.ProcessorCount];

        int CurrentPump = 0;
        int PumpsPerSec = 8;
        int NextAffinity = 0;


        internal InternetSim()
        {
            StartTime = new DateTime(2006, 1, 1, 0, 0, 0);
            TimeNow = StartTime;

            for (int i = 0; i < OutPackets.Length; i++)
            {
                OutPackets[i] = new List<SimPacket>();
                InPackets[i] = new List<SimPacket>();
            }
        }

        internal void StartInstance(string path)
        {
            SimInstance instance = new SimInstance(this, InstanceCount++);

            instance.Context = new DeOpsContext(instance);

            // ip
            byte[] ipbytes = new byte[4] { (byte)RndGen.Next(99), (byte)RndGen.Next(99), (byte)RndGen.Next(99), (byte)RndGen.Next(99) };

            if (LAN)
                ipbytes[0] = 10;
            else if (Utilities.IsLocalIP(new IPAddress(ipbytes)))
                ipbytes[0] = (byte)RndGen.Next(30, 70); // make non lan ip
            
            instance.RealIP = new IPAddress(ipbytes);

            // firewall
            instance.RealFirewall = FirewallType.Open;
            int num = RndGen.Next(100);

            if (num < PercentBlocked)
                instance.RealFirewall = FirewallType.Blocked;

            else if (num < PercentBlocked + PercentNAT)
                instance.RealFirewall = FirewallType.NAT;

            // processor
            instance.ThreadIndex = NextAffinity++;
            if (NextAffinity >= Environment.ProcessorCount)
                NextAffinity = 0;

            // hook instance into maps
            SimMap[instance.RealIP] = instance;

            if (LoadOnline)
                Login(instance, path);

            Instances.SafeAdd(instance);
            
            InstanceChange(instance, InstanceChangeType.Add);
        }

        internal void DoFlux()
        {
            // run flux once per minute
            /*if (TimeNow.Second % 5 != 0) 
                return;

            // influx
            for (int i = 0; i < FluxIn && Offline.Count > 0; i++)
                BringOnline(Offline[RndGen.Next(Offline.Count)]);
         
            // outflux
            for (int i = 0; i < FluxOut && Online.Count > 0; i++)
                BringOffline(Online[RndGen.Next(Online.Count)]);*/
        }

        internal void Login(SimInstance instance, string path)
        {
            string filename = Path.GetFileNameWithoutExtension(path);
            string[] parts = filename.Split('-');
            string op = parts[0].Trim();
            string name = parts[1].Trim();

            instance.Name = name;
            instance.Ops.Add(op);

            OpCore core = null;

            string pass = name.Split(' ')[0].ToLower(); // lowercase firstname
            core = new OpCore(instance.Context, path, pass);


            instance.LastPath = path;
            instance.Context.AddCore(core);

            if(InstanceChange != null)
                InstanceChange(instance, InstanceChangeType.Update);
        }

        internal void RegisterAddress(OpCore core)
        {
            if (ClearIPCache)
            {
                core.Network.Cache.IPs.Clear();
                core.Network.Cache.IPTable.Clear();
            }

            if (core.Network.IsLookup)
            {
                AddAddress(new IPEndPoint(core.Sim.RealIP, core.Network.LookupConfig.TcpPort), core.Network, true);
                AddAddress(new IPEndPoint(core.Sim.RealIP, core.Network.LookupConfig.UdpPort), core.Network, false);
            }
            else
            {
                AddAddress(new IPEndPoint(core.Sim.RealIP, core.User.Settings.TcpPort), core.Network, true);
                AddAddress(new IPEndPoint(core.Sim.RealIP, core.User.Settings.UdpPort), core.Network, false);

                UserNames[core.UserID] = core.User.Settings.UserName;
                OpNames[core.Network.OpID] = core.User.Settings.Operation;
            }
        }

        internal void Logout(OpCore core)
        {
            if (core.GuiMain != null)
                core.GuiMain.Close();

            core.Exit();
      
            if(InstanceChange != null)
                InstanceChange(core.Sim, InstanceChangeType.Update);
        }

        internal void UnregisterAddress(OpCore core)
        {
            if (core.Network.IsLookup)
            {
                TcpEndPoints.Remove(new IPEndPoint(core.Sim.RealIP, core.Network.LookupConfig.TcpPort));
                UdpEndPoints.Remove(new IPEndPoint(core.Sim.RealIP, core.Network.LookupConfig.UdpPort));
            }
            else
            {
                TcpEndPoints.Remove(new IPEndPoint(core.Sim.RealIP, core.User.Settings.TcpPort));
                UdpEndPoints.Remove(new IPEndPoint(core.Sim.RealIP, core.User.Settings.UdpPort));
            }
        }


        internal void ExitInstance(SimInstance instance)
        {
            SimMap.Remove(instance.RealIP);
        }

        internal void AddAddress(IPEndPoint address, DhtNetwork network, bool tcp)
        {
            Dictionary<IPEndPoint, DhtNetwork> endpoints = tcp ? TcpEndPoints : UdpEndPoints;

            lock (endpoints)
            {
                if (endpoints.ContainsKey(address))
                    Debug.Assert(false);
                else
                    endpoints[address] = network;
            }
        }

        internal void Start()
        {
            if (RunThread == null || !RunThread.IsAlive)
            {
                RunThread = new Thread(Run);
                RunThread.Name = "Sim Thread";
                RunThread.Start();
            }

            Paused = false;
        }

        internal void DoStep()
        {
            Step = true;
            Paused = true;
            
            if (RunThread == null)
            {
                RunThread = new Thread(Run);
                RunThread.Name = "Sim Thread";
                RunThread.Start();
            }              
        }

        internal void Pause()
        {
            if (RunThread != null)
                Paused = true;
        }

        void PumpThread(int index)
        {
            while (true)
            {
                PumpStart[index].WaitOne();

                if (Shutdown)
                    return;


                // send packets
                foreach (SimPacket packet in InPackets[index])
                {
                    switch (packet.Type)
                    {
                        case SimPacketType.Udp:
                            packet.Dest.Core.Sim.BytesRecvd += (ulong)packet.Packet.Length;
                            packet.Dest.UdpControl.OnReceive(packet.Packet, packet.Packet.Length, packet.Source);
                            break;
                        case SimPacketType.TcpConnect:
                            TcpConnect socket = packet.Dest.TcpControl.OnAccept(null, packet.Source);

                            if (socket != null)
                            {
                                TcpSourcetoDest.SafeAdd(packet.Tcp, socket);
                                TcpSourcetoDest.SafeAdd(socket, packet.Tcp);

                                packet.Tcp.OnConnect();
                            }

                            break;
                        case SimPacketType.Tcp:
                            TcpConnect dest;
                            if (TcpSourcetoDest.SafeTryGetValue(packet.Tcp, out dest))
                            {
                                dest.Core.Sim.BytesRecvd += (ulong)packet.Packet.Length;

                                packet.Packet.CopyTo(dest.RecvBuffer, dest.RecvBuffSize);
                                dest.OnReceive(packet.Packet.Length);
                            }
                            break;
                        case SimPacketType.TcpClose:
                            TcpConnect destClose;
                            if (TcpSourcetoDest.SafeTryGetValue(packet.Tcp, out destClose))
                            {
                                destClose.OnReceive(0);

                                TcpSourcetoDest.SafeRemove(packet.Tcp);
                                TcpSourcetoDest.SafeRemove(destClose);
                            }
                            break;
                    }
                }

                // send messages from gui
                if (!TestCoreThread)
                    Instances.SafeForEach(instance =>
                    {
                        if (instance.ThreadIndex != index)
                            return;

                        Action<OpCore> runMessages = core =>
                        {
                            // process invoked functions, dequeue quickly to continue processing
                            while (core.CoreMessages.Count > 0)
                            {
                                AsyncCoreFunction function = null;

                                lock (core.CoreMessages)
                                    function = core.CoreMessages.Dequeue();

                                if (function != null)
                                {
                                    function.Result = function.Method.DynamicInvoke(function.Args);
                                    function.Completed = true;
                                    function.Processed.Set();
                                }
                            }
                        };

                        runMessages(instance.Context.Lookup);

                        instance.Context.Cores.SafeForEach(c => runMessages(c));
                    });

                // instance timer
                if (CurrentPump == 0) // stepping would cause second timer to run every 250ms without this
                    Instances.SafeForEach(instance =>
                    {
                        if (instance.ThreadIndex == index)
                            instance.Context.SecondTimer_Tick(null);
                    });
  
                PumpEnd[index].Set();
            }
        }


        void Run()
        {
            for(int i = 0; i < PumpThreads.Length; i++)
            {
                PumpStart[i] = new AutoResetEvent(false);
                PumpEnd[i] = new AutoResetEvent(false);

                int index = i;

                PumpThreads[i] = new Thread(() => PumpThread(index)); // re-assign i so it is preserved
                PumpThreads[i].Name = "Pump Thread: " + i.ToString();
                PumpThreads[i].Start();
            }


            while (true)
            {
                if (Shutdown)
                {
                    PumpStart.ForEach(e => e.Set());
                    return;
                }

                if (Paused && !Step)
                {
                    Thread.Sleep(250);
                    continue;
                }

                // load users
                if (Flux)
                    DoFlux();


                // pump packets, 4 times (250ms latency
                while (CurrentPump < PumpsPerSec)
                {
                    for (int i = 0; i < OutPackets.Length; i++)
                        lock(OutPackets[i])
                        {
                            InPackets[i] = OutPackets[i];
                            OutPackets[i] = new List<SimPacket>();
                        }

                    // tell all pump threads to go and wait for them to complete
                    PumpStart.ForEach(e => e.Set());

                    AutoResetEvent.WaitAll(PumpEnd);


                    TimeNow = TimeNow.AddMilliseconds(1000 / PumpsPerSec);

                    if (UpdateView != null) 
                        UpdateView();

                    CurrentPump++;

                    if (Step)
                    {
                        Step = false;
                        break;
                    }
                }

                CurrentPump = 0;

                // if run sim slow
                if (SleepTime > 0)
                    Thread.Sleep(SleepTime);
            }
        }


        /* object TimerLock = new object();
         object[] PacketsLock = new object[4] { new object(), new object(), new object(), new object() };

         ManualResetEvent[] WaitHandles = new ManualResetEvent[5] {  new ManualResetEvent(true), new ManualResetEvent(true), 
                                                                     new ManualResetEvent(true), new ManualResetEvent(true), 
                                                                     new ManualResetEvent(true) };

        
         void OldRun()
         {
             int pumps = 4;
             List<SimPacket> tempList = new List<SimPacket>();

             Thread timerThread = new Thread(RunTimer);
             timerThread.Start();

             Thread[] packetsThread = new Thread[4];
             for (int i = 0; i < 4; i++)
             {
                 packetsThread[i] = new Thread(RunPackets);
                 packetsThread[i].Start(i);
             }

             Thread.Sleep(1000); // lets new threads reach wait(


             while (true && !Shutdown)
             {
                 if (Paused && !Step)
                 {
                     Thread.Sleep(250);
                     continue;
                 }

                 // load users
                 if (Flux)
                     DoFlux();


                 // instance timer
                 lock (TimerLock)
                 {
                     WaitHandles[0].Reset();
                     Monitor.Pulse(TimerLock);
                 } 

                 // pump packets, 4 times (250ms latency
                 for (int i = 0; i < pumps; i++)
                 {
                     // clear out buffer by switching with in buffer
                     lock (PacketHandle)
                     {
                         tempList = InPackets;
                         InPackets = OutPackets;
                         OutPackets = tempList;
                     }

                     for (int index = 0; index < 4; index++)
                         lock (PacketsLock[index])
                         {
                             WaitHandles[1 + index].Reset();
                             Monitor.Pulse(PacketsLock[index]);
                         }

              
                     AutoResetEvent.WaitAll(WaitHandles);

                     InPackets.Clear();

                     TimeNow = TimeNow.AddMilliseconds(1000 / pumps);

                     if(UpdateView != null)
                        UpdateView();

                     if (Step || Shutdown)
                     {
                         Step = false;
                         break;
                     }
                 }

                 // if run sim slow
                 if (SleepTime > 0)
                     Thread.Sleep(SleepTime);
             }


             lock(TimerLock)
                 Monitor.Pulse(TimerLock);

             for(int i = 0; i < 4; i++)
                 lock(PacketsLock[i])
                     Monitor.Pulse(PacketsLock[i]);
         }

         void RunTimer()
         {
             while (true && !Shutdown)
             {
                 lock (TimerLock)
                 {
                     Monitor.Wait(TimerLock);

                     lock (Instances)
                         foreach (SimInstance instance in Instances)
                             instance.Context.SecondTimer_Tick(null, null);

                     WaitHandles[0].Set();
                 }
             }
         }

         void RunPackets(object val)
         {
             int index = (int)val;

             while (true && !Shutdown)
             {
                 lock (PacketsLock[index])
                 {
                     Monitor.Wait(PacketsLock[index]);

                     // send packets
                     foreach (SimPacket packet in InPackets)
                     {
                         // 0 - global udp
                         // 1 - global tcp
                         // 2 - op udp
                         // 3 - op tcp

                         if ((index == 0 && packet.Type == SimPacketType.Udp && packet.Dest.IsGlobal) ||
                             (index == 1 && packet.Type != SimPacketType.Udp && packet.Dest.IsGlobal) ||
                             (index == 2 && packet.Type == SimPacketType.Udp && !packet.Dest.IsGlobal) ||
                             (index == 3 && packet.Type != SimPacketType.Udp && !packet.Dest.IsGlobal))
                         {

                             switch (packet.Type)
                             {
                                 case SimPacketType.Udp:
                                     packet.Dest.Core.Sim.BytesRecvd += (ulong)packet.Packet.Length;
                                     packet.Dest.UdpControl.OnReceive(packet.Packet, packet.Packet.Length, packet.Source);
                                     break;
                                 case SimPacketType.TcpConnect:
                                     TcpConnect socket = packet.Dest.TcpControl.OnAccept(null, packet.Source);

                                     if (socket != null)
                                     {
                                         TcpSourcetoDest[packet.Tcp] = socket;
                                         TcpSourcetoDest[socket] = packet.Tcp;

                                         packet.Tcp.OnConnect();
                                     }

                                     break;
                                 case SimPacketType.Tcp:
                                     if (TcpSourcetoDest.ContainsKey(packet.Tcp))
                                     {
                                         TcpConnect dest = TcpSourcetoDest[packet.Tcp];

                                         dest.Core.Sim.BytesRecvd += (ulong)packet.Packet.Length;

                                         packet.Packet.CopyTo(dest.RecvBuffer, dest.RecvBuffSize);
                                         dest.OnReceive(packet.Packet.Length);
                                     }
                                     break;
                                 case SimPacketType.TcpClose:
                                     if (TcpSourcetoDest.ContainsKey(packet.Tcp))
                                     {
                                         TcpConnect dest = TcpSourcetoDest[packet.Tcp];
                                         dest.OnReceive(0);

                                         TcpSourcetoDest.Remove(packet.Tcp);
                                         TcpSourcetoDest.Remove(dest);
                                     }
                                     break;
                             }
                         
                         }
                     }

                     WaitHandles[1 + index].Set();
                 }
             }
         }
         */

        internal void DownloadCache(DhtNetwork network)
        {
            WebCacheHits++;

            List<DhtNetwork> open = new List<DhtNetwork>();

            // find matching networks that are potential cache entries
            Instances.LockReading(() =>
            {
                foreach (DeOpsContext context in from i in Instances
                                                  where i.RealFirewall == FirewallType.Open && i != network.Core.Sim
                                                  select i.Context)
                {
                    if (context.Lookup != null && context.Lookup.Network.OpID == network.OpID)
                        open.Add(context.Lookup.Network);

                    context.Cores.LockReading(() =>
                        open.AddRange(from c in context.Cores where c.Network.OpID == network.OpID select c.Network));
                }
            });

            // give back 3 random cache entries
            foreach (DhtNetwork net in open.OrderBy(n => RndGen.Next()).Take(3))
            {
                DhtContact contact = net.GetLocalContact();
                contact.IP = net.Core.Sim.RealIP;
                contact.LastSeen = net.Core.TimeNow;
                network.Cache.AddContact(contact);
            }
        }

        internal int SendPacket(SimPacketType type, DhtNetwork network, byte[] packet, System.Net.IPEndPoint target, TcpConnect tcp)
        {
            if (type == SimPacketType.Tcp)
                if (!TcpEndPoints.ContainsKey(target))
                {
                    //this is what actually happens -> throw new Exception("Disconnected");
                    return -1;
                }

            if( !UdpEndPoints.ContainsKey(target) || 
                !SimMap.ContainsKey(target.Address) ||
                !SimMap.ContainsKey(network.Core.Sim.RealIP))
                return 0;

            DhtNetwork targetNet = (type == SimPacketType.Tcp) ? TcpEndPoints[target] : UdpEndPoints[target];
            if (network.IsLookup != targetNet.IsLookup)
                Debug.Assert(false);

            IPEndPoint source = new IPEndPoint(network.Core.Sim.RealIP, 0);
            source.Port = (type == SimPacketType.Udp) ? network.UdpControl.ListenPort : network.TcpControl.ListenPort;

            SimInstance sourceInstance = SimMap[source.Address];
            SimInstance destInstance   = SimMap[target.Address];

            if(packet != null)
                sourceInstance.BytesSent += (ulong)packet.Length; 

            // tcp connection must be present to send tcp
            if (type == SimPacketType.Tcp)
                if( !TcpSourcetoDest.SafeContainsKey(tcp))
                {
                    //this is what actually happens -> throw new Exception("Disconnected");
                    return -1;
                }

            // add destination to nat table
            if (type == SimPacketType.Udp && sourceInstance.RealFirewall == FirewallType.NAT)
                sourceInstance.NatTable[target] = true;

            // if destination blocked drop udp / tcp connect requests
            if (destInstance.RealFirewall == FirewallType.Blocked)
            {
                if (type == SimPacketType.Udp || type == SimPacketType.TcpConnect)
                    return 0;
            }

            // if destination natted drop udp (unless in nat table) / tcp connect requests
            else if (destInstance.RealFirewall == FirewallType.NAT)
            {
                if (type == SimPacketType.TcpConnect)
                    return 0;

                if (type == SimPacketType.Udp && !destInstance.NatTable.ContainsKey(source) )
                    return 0;
            }

            // randomly test tcp send buffer full
            if(TestTcpFullBuffer)
                if (type == SimPacketType.Tcp && packet.Length > 4 && RndGen.Next(2) == 1)
                {
                    int newlength = packet.Length / 2;
                    byte[] newpacket = new byte[newlength];
                    Buffer.BlockCopy(packet, 0, newpacket, 0, newlength);
                    packet = newpacket;
                }

            if (packet != null && packet.Length == 0)
                Debug.Assert(false, "Empty Packet");

            int index = destInstance.ThreadIndex;
            lock (OutPackets[index])
                OutPackets[index].Add(new SimPacket(type, source, packet, targetNet, tcp, network.Local.UserID));

            if (packet == null)
                return 0;

            return packet.Length;
        }

        internal void Exit()
        {
            Shutdown = true;

            //foreach(ManualResetEvent handle in WaitHandles)
            //    handle.Set();

            Instances.SafeForEach(instance =>
            {
                instance.Context.Cores.LockReading(delegate()
                {
                    while (instance.Context.Cores.Count > 0)
                        Logout(instance.Context.Cores[0]);
                });
            });
        }
    }

    internal enum SimPacketType { Udp, Tcp, TcpConnect, TcpClose };

    internal class SimPacket
    {
        internal SimPacketType   Type;
        internal byte[]          Packet;
        internal IPEndPoint      Source;
        internal DhtNetwork      Dest;
        internal TcpConnect      Tcp;
        internal ulong           SenderID;

        internal SimPacket(SimPacketType type, IPEndPoint source, byte[] packet, DhtNetwork dest, TcpConnect tcp, ulong id)
        {
            Type = type;
            Source = source;
            Packet = packet;
            Dest = dest;
            Tcp = tcp;
            SenderID = id;
        }
    }

    internal class SimInstance
    {
        internal InternetSim Internet;

        internal DeOpsContext Context;

        internal string LastPath;

        internal string Name;
        internal List<string> Ops = new List<string>();

        internal int Index;
        internal int ThreadIndex;

        internal FirewallType RealFirewall;
        internal IPAddress RealIP;

        internal ulong BytesSent;
        internal ulong BytesRecvd;

        internal Dictionary<IPEndPoint, bool> NatTable = new Dictionary<IPEndPoint, bool>();

        internal SimInstance(InternetSim internet, int index)
        {
            Internet = internet;
            Index = index;
        }
    }

}
