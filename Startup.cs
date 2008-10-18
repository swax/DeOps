using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

using Microsoft.Win32;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Interface;
using RiseOp.Simulator;
using RiseOp.Services.Share;


namespace RiseOp
{
    class Startup
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
       {
            Application.EnableVisualStyles();

            try
            {
                RiseOpContext context = new RiseOpContext(args);

                if(context.SingleInstance.First)
                    Application.Run(context);
            }
            catch(Exception ex)
            {
                ErrorReport report = new ErrorReport(ex);

                Application.Run(report);
            }
        }
    }

    internal class RiseOpContext : ApplicationContext
    {
        internal RiseOpMutex SingleInstance;

        internal OpCore Lookup;
        internal ThreadedList<OpCore> Cores = new ThreadedList<OpCore>();
        List<LoginForm> Logins = new List<LoginForm>();
        SimForm Simulator;

        internal SimInstance Sim;

        Timer FastTimer = new Timer();
        Timer SecondTimer = new Timer();

        string UpdatePath;
        Queue<string[]> NewInstances = new Queue<string[]>();

        internal Dictionary<uint, string> KnownServices = new Dictionary<uint, string>();

        internal BandwidthLog Bandwidth = new BandwidthLog(10);


        internal RiseOpContext(string[] args)
        {
            // if instance already running, signal it and exit
     
            SingleInstance = new RiseOpMutex(this, args);

            if (!SingleInstance.First)
                return;

            // open windows firewall
            Win32.AuthorizeApplication("RiseOp", Application.ExecutablePath, 
                NetFwTypeLib.NET_FW_SCOPE_.NET_FW_SCOPE_ALL, NetFwTypeLib.NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY);

            // register file types
            RegisterType();

            // start timers
            SecondTimer.Interval = 1000;
            SecondTimer.Tick += new EventHandler(SecondTimer_Tick);
            SecondTimer.Enabled = true ;

            FastTimer.Interval = 250;
            FastTimer.Tick += new EventHandler(FastTimer_Tick);
            FastTimer.Enabled = true;

            // check for updates
            new System.Threading.Thread(CheckForUpdates).Start();

            // display login form
            ShowLogin(args);
        }

        internal void SecondInstanceStarted(string[] args)
        {
            // pass to main thread
            lock (NewInstances)
                NewInstances.Enqueue(args);
        }

        internal RiseOpContext(SimInstance sim)
        {
            // starting up simulated context context->simulator->instances[]->context
            Sim = sim;
        }

        internal void CheckForUpdates()
        {
            try
            {
                string address = Utilities.WebDownloadString("http://www.riseop.com/update/check.php?version=" + Application.ProductVersion);

          
                if (address != null && address != "")
                {
                    string path = Application.StartupPath + Path.DirectorySeparatorChar + Path.GetFileName(address);

                    if (!File.Exists(path))
                    {
                        WebClient client = new WebClient();
                        client.DownloadFile(address, path);
                    }

                    UpdatePath = path;
                }


                #if !DEBUG      
                // call home for alpha security
                string build = Application.ProductName + "_" + Application.ProductVersion;
                string name = SystemInformation.UserName;
                string comp = SystemInformation.ComputerName;
                
                string x = Utilities.WebDownloadString("http://www.riseop.com/checkin.php?build=" + build + "&comp=" + comp + "&name=" + name);

                if (x == "**die**")
                    Application.Exit();
                
#endif

            }
            catch { }
        }

        void RegisterType()
        {
            // try to register file type
            try
            {
                RegistryKey type = Registry.ClassesRoot.CreateSubKey(".rop");
                type.SetValue("", "rop");

                RegistryKey root = Registry.ClassesRoot.CreateSubKey("rop");
                root.SetValue("", "RiseOp Identity");

                RegistryKey icon = root.CreateSubKey("DefaultIcon");
                icon.SetValue("", "\"" + Application.ExecutablePath + "\"");

                RegistryKey shell = root.CreateSubKey("shell");
                RegistryKey open = shell.CreateSubKey("open");
                RegistryKey command = open.CreateSubKey("command");
                command.SetValue("", "\"" + Application.ExecutablePath + "\" \"%1\"");
            }
            catch
            {
                //UpdateLog("Exception", "LoginForm::RegisterType: " + ex.Message);
            }

            // try to register protocol
            try
            {
                RegistryKey root = Registry.ClassesRoot.CreateSubKey("riseop");
                root.SetValue("", "URL:riseop Protocol");
                root.SetValue("URL Protocol", "");

                RegistryKey icon = root.CreateSubKey("DefaultIcon");
                icon.SetValue("", "\"" + Application.ExecutablePath + "\"");

                RegistryKey shell = root.CreateSubKey("shell");
                RegistryKey open = shell.CreateSubKey("open");
                RegistryKey command = open.CreateSubKey("command");
                command.SetValue("", "\"" + Application.ExecutablePath + "\" \"%1\"");
            }
            catch
            {
                //UpdateLog("Exception", "LoginForm::RegisterType: " + ex.Message);
            }
        }

        float FastestUploadSpeed = 10;

        internal void SecondTimer_Tick(object sender, EventArgs e)
        {
            // flag set, actual timer code run in thread per core

            if (Lookup != null)
                Lookup.SecondTimer();

            Cores.LockReading(delegate()
            {
                foreach (OpCore core in Cores)
                    core.SecondTimer();
            });

            // bandwidth
            Bandwidth.NextSecond();

            // fastest degrades over time, min is 10kb/s
            FastestUploadSpeed--;
            FastestUploadSpeed = Math.Max(Bandwidth.Average(Bandwidth.Out, 10), FastestUploadSpeed);
            FastestUploadSpeed = Math.Max(10, FastestUploadSpeed);

            AssignUploadSlots();
        }

        internal void AssignUploadSlots()
        {
            int activeTransfers = 0;
            OpCore next = null;

            Cores.LockReading(delegate()
            {
                foreach (OpCore core in Cores)
                {
                    activeTransfers += core.Transfers.ActiveUploads;

                    if (next == null || core.Transfers.NeedUploadWeight > next.Transfers.NeedUploadWeight)
                        next = core;
                }
            });

            // max number of active transfers 15
            if (next == null || activeTransfers >= 15)
                return;

            // allocate a min of 5kb/s per transfer
            // allow a min of 2 transfers
            // if more than 10kb/s free, after accounting for upload speed allow another transfer
            // goal push transfers down to around 5kb/s, 30 secs to finish 256kb chunk
            if (activeTransfers < 2 || FastestUploadSpeed - 5 * activeTransfers > 10)
            {
                next.Transfers.NeedUploadWeight = 0; // do here so that if core is crashed/throwing exceptions - other cores can still u/l

                next.RunInCoreAsync(delegate()
                {
                    next.Transfers.StartUpload();
                });
            }
        }

        internal void FastTimer_Tick(object sender, EventArgs e)
        {
            // process new instance
            string[] loginArgs = null;

            lock (NewInstances)
                if (NewInstances.Count > 0)
                    loginArgs = NewInstances.Dequeue();

            if (loginArgs != null)
                ShowLogin(loginArgs);

            // run update
            if (UpdatePath != null)
            {
                string path = UpdatePath;  // prevent cascading updates as message box is displayed
                UpdatePath = null;

                if (MessageBox.Show("A new version of RiseOp is available. Install it now?", "RiseOp", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start(path); // launch installer without risking core exit screwups

                    try
                    {
                        Cores.LockReading(delegate()
                        {
                            while (Cores.Count > 0)
                                Cores[0].Exit();
                        });
                    }
                    catch { }

                    ExitThread();
                }
            }
        }

        internal void ShowLogin(string[] args)
        {
            // either c:\... or riseop://...
            string arg = "";
            if (args != null && args.Length > 0)
                arg = args[0];

            // find if login exists with same arg
            foreach(LoginForm login in Logins)
                if (login.Arg == arg)
                {
                    login.WindowState = FormWindowState.Normal;
                    login.Activate(); // bring windows on top of other apps
                    return;
                }

            // try pre-process link
            if (arg.StartsWith(@"riseop://"))
                if(PreProcessLink(arg)) // internal links
                    return;

            LoginForm form = new LoginForm(this, arg);
            form.FormClosed += new FormClosedEventHandler(Window_FormClosed);
            form.Show();
            form.Activate();

            Logins.Add(form);

            // do here because process link can close form and we want all the events already hooked up for it
            if (arg.StartsWith(@"riseop://"))
                if (!form.ProcessLink()) // new op links
                    MessageBox.Show("Could not process link:\n" + arg);
        }

        private bool PreProcessLink(string arg) 
        {
            try
            {
                arg = arg.TrimEnd('/'); // copy so modifications arent permanent

                if (arg.Contains("/file/"))
                {
                    FileLink link = FileLink.Decode(arg, null);

                    OpCore core = FindCore(link.PublicOpID);

                    if (core != null)
                    {
                        ShareService share = core.GetService(RiseOp.Services.ServiceIDs.Share) as ShareService;
                        share.DownloadLink(arg);

                        if (core.GuiMain != null)
                            if (!core.GuiMain.ShowExistingView(typeof(SharingView)))
                                core.ShowExternal(new SharingView(core, core.UserID)); 

                        return true;
                    }
                }

                else if (arg.Contains("/ident/"))
                {
                    IdentityLink link = IdentityLink.Decode(arg);

                    OpCore target = FindCore(link.PublicOpID);

                    if (target != null)
                    {
                        RiseOp.Services.Buddy.BuddyView.AddBuddyDialog(target, arg);
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private OpCore FindCore(byte[] pubOpID)
        {
            OpCore found = null;

            Cores.LockReading(() =>
            {
                foreach (OpCore core in Cores)
                    if (Utilities.MemCompare(core.User.Settings.PublicOpID, pubOpID))
                    {
                        found = core;
                        break;
                    }
            });

            return found;
        }

        internal void ShowCore(OpCore core)
        {
            AddCore(core);

            core.ShowMainView();
        }

        internal void ShowSimulator()
        {
            if (Simulator != null)
            {
                Simulator.BringToFront();
                return;
            }

            Simulator = new SimForm();
            Simulator.FormClosed += new FormClosedEventHandler(Window_FormClosed);
            Simulator.Show();
        }

        void Window_FormClosed(object sender, FormClosedEventArgs e)
        {
            foreach (LoginForm form in Logins)
                if (sender == form)
                {
                    Logins.Remove(form);
                    break;
                }

            if (sender == Simulator)
                Simulator = null;

            CheckExit();
        }

        internal void AddCore(OpCore core)
        {
            Cores.SafeAdd(core);

            CheckLookup();
        }

        void CheckLookup()
        {
            // called from gui thread

            bool runLookup = false;

            Cores.LockReading(delegate()
            {
                foreach (OpCore core in Cores)
                    if (core.User.Settings.OpAccess != AccessType.Secret ||
                        core.User.Settings.GlobalIM)
                        runLookup = true;

                // if public cores exist, sign into global
                if (runLookup && Lookup == null)
                {
                    Lookup = new OpCore(this);
                }

                // else destroy global context
                if (!runLookup && Lookup != null)
                {
                    Lookup.Exit();
                    Lookup = null;
                }
            });
        }

        internal void RemoveCore(OpCore removed)
        {
            if (removed == Lookup)
                return;

            Cores.LockWriting(delegate()
            {
                foreach (OpCore core in Cores)
                    if (core == removed)
                    {
                        Cores.Remove(core);
                        break;
                    }
            });

            CheckLookup();
            CheckExit();
        }

        private void CheckExit()
        {
            // if context running inside a simulator dont exit thread
            if (Sim != null)
                return;

            if (Logins.Count == 0 && Cores.SafeCount == 0)
            {
                if (Lookup != null)
                    Lookup.Exit();

                if (Sim == null) // context not running inside a simulation
                {
                    if(Simulator == null) // simulation interface closed
                        ExitThread();
                }
                else
                    Sim.Internet.ExitInstance(Sim);
            }
        }
    }

}
