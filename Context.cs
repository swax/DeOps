using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Packets;

using RiseOp.Interface;
using RiseOp.Simulator;

using RiseOp.Services.Share;
using RiseOp.Services.Update;

// v1.0.0 s1
// v1.0.1 s2
// v1.0.2 s3
// v1.0.3 s4
// v1.0.4 s5
// v1.0.5 s6
// v1.0.6 s7
// v1.0.7 s8
// v1.0.8 s9
// v1.0.9
// v1.1.0 s10
// v1.1.1 s11
// v1.1.2 s12
// v1.1.3 s13

namespace RiseOp
{
    internal class RiseOpContext : ApplicationContext
    {
        internal bool StartSuccess;
        RiseOpMutex SingleInstance;
         
        internal OpCore Lookup;
        internal ThreadedList<OpCore> Cores = new ThreadedList<OpCore>();
        List<LoginForm> Logins = new List<LoginForm>();
        SimForm Simulator;

        internal SimInstance Sim;

        Timer FastTimer = new Timer();
        Timer SecondTimer = new Timer();

        internal FullLicense License;
        internal LightLicense LicenseProof;

        //string UpdatePath; // news page used to alert users of non-autoupdates

        Queue<string[]> NewInstances = new Queue<string[]>();

        internal UpdateInfo SignedUpdate;
        internal uint LocalSeqVersion = 13;

        internal BandwidthLog Bandwidth = new BandwidthLog(10);
        internal Dictionary<uint, string> KnownServices = new Dictionary<uint, string>();

        internal System.Threading.Thread ContextThread;


        internal RiseOpContext(string[] args)
        {
            // if instance already running, signal it and exit

            SingleInstance = new RiseOpMutex(this, args);

            if (!SingleInstance.First)
                return;

            ContextThread = System.Threading.Thread.CurrentThread;

            // upgrade properties if we need to 
            if (Properties.Settings.Default.NeedUpgrade)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.NeedUpgrade = false;
            }

            // open windows firewall
            //Win32.AuthorizeApplication("RiseOp", Application.ExecutablePath,
            //    NetFwTypeLib.NET_FW_SCOPE_.NET_FW_SCOPE_ALL, NetFwTypeLib.NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY);

            // register file types
            RegisterType();

            // start timers
            SecondTimer.Interval = 1000;
            SecondTimer.Tick += new EventHandler(SecondTimer_Tick);
            SecondTimer.Enabled = true;

            FastTimer.Interval = 250;
            FastTimer.Tick += new EventHandler(FastTimer_Tick);
            FastTimer.Enabled = true;

            // create directories if need be
            try { Directory.CreateDirectory(ApplicationEx.CommonAppDataPath()); }
            catch { }
            try { Directory.CreateDirectory(ApplicationEx.UserAppDataPath()); }
            catch { }

            // check for updates - update through network, use news page to notify user of updates
            //new System.Threading.Thread(CheckForUpdates).Start();
            SignedUpdate = UpdateService.LoadUpdate();

            LoadLicense(ref License, ref LicenseProof);

            if (CanUpdate() && NotifyUpdateReady())
                return;

            // display login form
            ShowLogin(args);

            StartSuccess = true;
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

        /*internal void CheckForUpdates()
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
            */

                /* call home for alpha security
                string build = Application.ProductName + "_" + Application.ProductVersion;
                string name = SystemInformation.UserName;
                string comp = SystemInformation.ComputerName;
                
                string x = Utilities.WebDownloadString("http://www.riseop.com/checkin.php?build=" + build + "&comp=" + comp + "&name=" + name);

                if (x == "**die**")
                    Application.Exit();*/
            /*
            }
            catch { }
        }*/

        void RegisterType()
        {
            // taken out and added to nullsoft installer because as MS specifies, all HKLM entries
            // should be done by the installer, also app running in vista has no access to HKLM without
            // admin privlidges


            // register file type
            // HKCR ".rop" "" "rop"
            // HKCR "rop"  ""   "RiseOp Identity"
            // HKCR "rop\DefaultIcon" "" "$\"Application.ExecutablePath$\""
            // HKCR "rop\shell\open\command" "" "$\"Application.ExecutablePath$\" $\"%1$\""
            
            /*try
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
            }*/


            // register protocol
            // HKCR "riseop" "" "URL:riseop Protocol"
            // HKCR "riseop" "URL Protocol" ""
            // HKCR "riseop\DefaultIcon" "" "$\"Application.ExecutablePath$\""
            // HKCR "riseop\shell\open\command" "" "$\"Application.ExecutablePath$\" $\"%1$\""
            
           /* try
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
            }*/
        }

        float FastestUploadSpeed = 10;

        internal void SecondTimer_Tick(object sender, EventArgs e)
        {
            // flag set, actual timer code run in thread per core

            if (Lookup != null)
                Lookup.SecondTimer();

            Cores.SafeForEach(c => c.SecondTimer());

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

            Cores.SafeForEach(core =>
            {
                activeTransfers += core.Transfers.ActiveUploads;

                if (next == null || core.Transfers.NeedUploadWeight > next.Transfers.NeedUploadWeight)
                    next = core;
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

                next.RunInCoreAsync(() => next.Transfers.StartUpload());
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

            /* run update
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
            }*/
        }

        internal void ShowLogin(string[] args)
        {
            // either c:\... or riseop://...
            string arg = "";
            if (args != null && args.Length > 0)
                arg = args[0];

            // find if login exists with same arg
            foreach (LoginForm login in Logins)
                if (login.Arg == arg)
                {
                    login.WindowState = FormWindowState.Normal;
                    login.Activate(); // bring windows on top of other apps
                    return;
                }

            // try pre-process link
            if (arg.StartsWith(@"riseop://"))
                if (PreProcessLink(arg)) // internal links
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
                    Properties.Settings.Default.Save();

                    if (Simulator == null) // simulation interface closed
                        ExitThread();
                }
                else
                    Sim.Internet.ExitInstance(Sim);
            }
        }

        internal bool CanUpdate()
        {
            if (SignedUpdate == null)
                return false; // nothing to update with

            return (SignedUpdate.Loaded && SignedUpdate.SequentialVersion > LocalSeqVersion);             
        }

        internal bool NotifyUpdateReady()
        {
            UpdateForm form = new UpdateForm(SignedUpdate);

            if (form.ShowDialog() != DialogResult.OK)
                return false;

            try
            {
                string finalpath = ApplicationEx.CommonAppDataPath() + Path.DirectorySeparatorChar + SignedUpdate.Name;

                Utilities.DecryptTagFile(LookupSettings.UpdatePath, finalpath, SignedUpdate.Key, null);

                try
                {
                    Process.Start("UpdateOp.exe", "\"" + finalpath + "\"");

                    // try to close interfaces
                    Cores.LockReading(() => Cores.ToList().ForEach(c => c.Exit()));

                    Logins.ForEach(l => l.Close());

                    CheckExit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                return true;

            }
            catch(Exception ex)
            {
                MessageBox.Show("Update Error: " + ex.Message);
            }

            return false;
        }

        internal static void LoadLicense(ref FullLicense full, ref LightLicense light)
        {
            try
            {
                DirectoryInfo info = new DirectoryInfo(Application.StartupPath);
                FileInfo[] files = info.GetFiles("license-*.dat");

                if (files.Length == 0)
                    return;

                byte[] licenseKey = Convert.FromBase64String("4mdBmbUIjh2p6sff42O9AfYdWKZfVUgeK6vOv514XVw=");

                using (IVCryptoStream crypto = IVCryptoStream.Load(files[0].FullName, licenseKey))
                {
                    PacketStream stream = new PacketStream(crypto, new G2Protocol(), FileAccess.Read);

                    G2Header root = null;

                    while (stream.ReadPacket(ref root))
                        if (root.Name == LicensePacket.Full)
                            full = FullLicense.Decode(root);
                        else if (root.Name == LicensePacket.Light)
                            light = LightLicense.Decode(root);
                }
            }
            catch
            {
            }
        }
    }
}
