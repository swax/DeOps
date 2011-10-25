using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

using DeOps.Implementation.Protocol;
using DeOps.Implementation;
using DeOps.Services.Update;
using DeOps.Simulator;
using DeOps.Implementation.Protocol.Packets;
using DeOps.Interface;
using DeOps.Services.Share;
using DeOps.Services.Buddy;
using System.Diagnostics;

namespace DeOps
{
    public class WinContext : ApplicationContext
    {
        public bool StartSuccess;
        public DeOpsMutex SingleInstance;
        public DeOpsContext Context;

        SimForm Simulator;

        internal SimInstance Sim;

        internal FullLicense License;

        List<LoginForm> Logins = new List<LoginForm>();

        Queue<string[]> NewInstances = new Queue<string[]>();

        Timer FastTimer = new Timer();


        public WinContext(string[] args)
        {
            // if instance already running, signal it and exit

            SingleInstance = new DeOpsMutex(this, args);

            if (!SingleInstance.First)
                return;

            // open windows firewall
            //Win32.AuthorizeApplication("DeOps", Application.ExecutablePath,
            //    NetFwTypeLib.NET_FW_SCOPE_.NET_FW_SCOPE_ALL, NetFwTypeLib.NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY);

            // register file types
            RegisterType();


            // create directories if need be
            try { Directory.CreateDirectory(ApplicationEx.CommonAppDataPath()); }
            catch { }
            try { Directory.CreateDirectory(ApplicationEx.UserAppDataPath()); }
            catch { }


            // upgrade properties if we need to 
            if (Properties.Settings.Default.NeedUpgrade)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.NeedUpgrade = false;
            }

            FastTimer.Interval = 250;
            FastTimer.Tick += new EventHandler(FastTimer_Tick);
            FastTimer.Enabled = true;

            Context = new DeOpsContext();
            Context.ShowLogin = ShowLogin;
            Context.NotifyUpdateReady = NotifyUpdateReady;
            Context.CheckExit = CheckExit;

            LoadLicense(ref License, ref Context.LicenseProof);

            if (Context.CanUpdate() && NotifyUpdateReady())
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

        internal void ShowLogin(string[] args)
        {
            // either c:\... or deops://...
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
            if (arg.StartsWith(@"deops://"))
                if (PreProcessLink(arg)) // internal links
                    return;

            LoginForm form = new LoginForm(this, arg);
            form.FormClosed += new FormClosedEventHandler(Window_FormClosed);
            form.Show();
            form.Activate();

            Logins.Add(form);

            // do here because process link can close form and we want all the events already hooked up for it
            if (arg.StartsWith(@"deops://"))
                if (!form.ProcessLink()) // new op links
                    MessageBox.Show("Could not process link:\n" + arg);
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

                if (MessageBox.Show("A new version of DeOps is available. Install it now?", "DeOps", MessageBoxButtons.YesNo) == DialogResult.Yes)
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

        private bool PreProcessLink(string arg)
        {
            try
            {
                arg = arg.TrimEnd('/'); // copy so modifications arent permanent

                if (arg.Contains("/file/"))
                {
                    FileLink link = FileLink.Decode(arg, null);

                    OpCore core = Context.FindCore(link.PublicOpID);

                    if (core != null)
                    {
                        ShareService share = core.GetService(DeOps.Services.ServiceIDs.Share) as ShareService;
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

                    OpCore target = Context.FindCore(link.PublicOpID);

                    if (target != null)
                    {
                        BuddyView.AddBuddyDialog(target, arg);
                        return true;
                    }
                }
            }
            catch { }

            return false;
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

        internal bool NotifyUpdateReady()
        {
            var signedUpdate = Context.SignedUpdate;

            UpdateForm form = new UpdateForm(signedUpdate);

            if (form.ShowDialog() != DialogResult.OK)
                return false;

            try
            {
                string finalpath = ApplicationEx.CommonAppDataPath() + Path.DirectorySeparatorChar + signedUpdate.Name;

                Utilities.DecryptTagFile(LookupSettings.UpdatePath, finalpath, signedUpdate.Key, null);

                try
                {
                    Process.Start("UpdateOp.exe", "\"" + finalpath + "\"");

                    // try to close interfaces
                    Context.Cores.LockReading(() => Context.Cores.ToList().ForEach(c => c.Exit()));

                    Logins.ForEach(l => l.Close());

                    CheckExit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                return true;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Update Error: " + ex.Message);
            }

            return false;
        }

        private void CheckExit()
        {
            // if context running inside a simulator dont exit thread
            if (Sim != null)
                return;

            if (Logins.Count == 0 && Context.Cores.SafeCount == 0)
            {
                if (Context.Lookup != null)
                    Context.Lookup.Exit();

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

        /*internal void CheckForUpdates()
{
    try
    {
        string address = Utilities.WebDownloadString("http://www.c0re.net/deops/update/check.php?version=" + Application.ProductVersion);


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
                
        string x = Utilities.WebDownloadString("http://www.c0re.net/deops/checkin.php?build=" + build + "&comp=" + comp + "&name=" + name);

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
            // HKCR ".dop" "" "dop"
            // HKCR "dop"  ""   "DeOps Identity"
            // HKCR "dop\DefaultIcon" "" "$\"Application.ExecutablePath$\""
            // HKCR "dop\shell\open\command" "" "$\"Application.ExecutablePath$\" $\"%1$\""

            /*try
            {
                RegistryKey type = Registry.ClassesRoot.CreateSubKey(".dop");
                type.SetValue("", "dop");

                RegistryKey root = Registry.ClassesRoot.CreateSubKey("dop");
                root.SetValue("", "DeOps Identity");

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
            // HKCR "deops" "" "URL:deops Protocol"
            // HKCR "deops" "URL Protocol" ""
            // HKCR "deops\DefaultIcon" "" "$\"Application.ExecutablePath$\""
            // HKCR "deops\shell\open\command" "" "$\"Application.ExecutablePath$\" $\"%1$\""

            /* try
             {
                 RegistryKey root = Registry.ClassesRoot.CreateSubKey("deops");
                 root.SetValue("", "URL:deops Protocol");
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
    }
}
