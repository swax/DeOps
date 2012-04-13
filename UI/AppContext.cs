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
using DeOps.Interface;
using DeOps.Services.Share;
using DeOps.Services.Buddy;
using System.Diagnostics;
using DeOps.Interface.Views;
using DeOps.Services;
using System.Drawing;
using DeOps.Services.Board;
using DeOps.Services.Chat;
using DeOps.Services.IM;
using DeOps.Services.Mail;
using DeOps.Services.Plan;
using DeOps.Services.Profile;
using DeOps.Services.Storage;
using DeOps.Services.Trust;
using DeOps.Interface.Tools;
using System.Net;

namespace DeOps
{
    public class AppContext : ApplicationContext
    {
        public bool StartSuccess;
        public DeOpsMutex SingleInstance;
        public DeOpsContext Context;
        public ThreadedList<CoreUI> CoreUIs = new ThreadedList<CoreUI>();
        public AppSettings Settings;

        SimForm Simulator;

        List<LoginForm> Logins = new List<LoginForm>();

        Queue<string[]> NewInstances = new Queue<string[]>();

        Timer FastTimer = new Timer();

        //string UpdatePath; // news page used to alert users of non-autoupdates


        public AppContext(string[] args)
        {
            // if instance already running, signal it and exit

            SingleInstance = new DeOpsMutex(this, args);

            if (!SingleInstance.First)
                return;

            // open windows firewall
            //Win32.AuthorizeApplication("DeOps", Application.ExecutablePath,
            //    NetFwTypeLib.NET_FW_SCOPE_.NET_FW_SCOPE_ALL, NetFwTypeLib.NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY);
            
            //CheckForUpdates();

            // register file types
            //RegisterType();

            // upgrade properties if we need to 
            Settings = AppSettings.Load(Path.Combine(Application.StartupPath, "app.xml"));

            FastTimer.Interval = 250;
            FastTimer.Tick += new EventHandler(FastTimer_Tick);
            FastTimer.Enabled = true;

            Context = new DeOpsContext(Application.StartupPath, InterfaceRes.deops);
            Context.ShowLogin += ShowLogin;
            Context.NotifyUpdateReady += NotifyUpdateReady;

            if (Context.CanUpdate() && NotifyUpdateReady(Context.LookupConfig))
                return;

            // display login form
            ShowLogin(args);

            StartSuccess = true;
        }

        public void SecondInstanceStarted(string[] args)
        {
            // pass to main thread
            lock (NewInstances)
                NewInstances.Enqueue(args);
        }

        public void ShowLogin(string[] args)
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
                if (PreProcessLink(arg)) // public links
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

        public void FastTimer_Tick(object sender, EventArgs e)
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

                    var ui = FindCoreUI(link.PublicOpID);

                    if (ui != null)
                    {
                        ShareService share = ui.Core.GetService(DeOps.Services.ServiceIDs.Share) as ShareService;
                        share.DownloadLink(arg);

                        if (ui.GuiMain != null && !ui.GuiMain.ShowExistingView(typeof(SharingView)))
                            ui.ShowView(new SharingView(ui.Core, ui.Core.UserID), true);

                        return true;
                    }
                }

                else if (arg.Contains("/ident/"))
                {
                    IdentityLink link = IdentityLink.Decode(arg);

                    var target = FindCoreUI(link.PublicOpID);

                    if (target != null)
                    {
                        BuddyView.AddBuddyDialog(target.Core, arg);
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        public CoreUI FindCoreUI(byte[] pubOpID)
        {
            CoreUI found = null;

            CoreUIs.LockReading(() =>
            {
                foreach (var ui in CoreUIs)
                    if (Utilities.MemCompare(ui.Core.User.Settings.PublicOpID, pubOpID))
                    {
                        found = ui;
                        break;
                    }
            });

            return found;
        }

        public void ShowSimulator()
        {
            if (Simulator != null)
            {
                Simulator.BringToFront();
                return;
            }

            Simulator = new SimForm(this);
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

        public bool NotifyUpdateReady(LookupSettings config)
        {
            var signedUpdate = Context.SignedUpdate;

            UpdateForm form = new UpdateForm(signedUpdate);

            if (form.ShowDialog() != DialogResult.OK)
                return false;

            try
            {
                string finalpath = Application.StartupPath + Path.DirectorySeparatorChar + signedUpdate.Name;

                Utilities.DecryptTagFile(config.UpdatePath, finalpath, signedUpdate.Key, null);

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

        public CoreUI LoadCore(string userPath, string pass)
        {
            var core = Context.LoadCore(userPath, pass);
            core.Exited += RemoveCore;

            var ui = new CoreUI(core);
            CoreUIs.SafeAdd(ui);
            ui.ShowMainView();

            return ui;
        }

        public void RemoveCore(OpCore removed)
        {
            CoreUI removeUI = null;

            CoreUIs.SafeForEach(ui =>
            {
                if (ui.Core == removed)
                    removeUI = ui;
            });

            if (removeUI != null)
            {
                if (removeUI.GuiMain != null)
                    removeUI.GuiMain.Close();

                CoreUIs.SafeRemove(removeUI);
            }

            CheckExit();
        }

        private void CheckExit()
        {
            // if context running inside a simulator dont exit thread
            if (Context.Sim != null)
                return;

            if (Logins.Count == 0 && Context.Cores.SafeCount == 0)
            {
                if (Context.Sim == null) // context not running inside a simulation
                {
                    Settings.Save();
                    Context.Dispose();

                    if (Simulator == null) // simulation interface closed
                        ExitThread();
                }
                else
                    Context.Sim.Internet.ExitInstance(Context.Sim);
            }
        }

        /*
        public void CheckForUpdates()
        {

            try
            {
                var web = new WebClient();
                web.DownloadStringCompleted += CheckForUpdates_CacheDownloaded;
                web.DownloadStringAsync(new Uri("http://www.c0re.net/deops/update/check.php?version=" + Application.ProductVersion));
            }
            catch { }
        }

        void CheckForUpdates_CacheDownloaded(object sender, DownloadStringCompletedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Result))
                return;

            var updateUrl = e.Result;

            string path = Application.StartupPath + Path.DirectorySeparatorChar + Path.GetFileName(updateUrl);

            if (!File.Exists(path))
            {
                WebClient client = new WebClient();
                client.DownloadFile(updateUrl, path);
            }

            UpdatePath = path;
        }
            
        
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
             }
        }*/
    }

    public class CoreUI
    {
        public OpCore Core;
        public HostsExternalViews GuiMain;
        public TrayLock GuiTray;
        public InternalsForm GuiInternal;
        public ConsoleForm GuiConsole;

        public Dictionary<uint, IServiceUI> Services = new Dictionary<uint, IServiceUI>();

        public Action<ViewShell, bool> ShowView;


        public CoreUI(OpCore core)
        {
            Core = core;

            // load menus for loaded services
            foreach (var service in Core.ServiceMap.Values)
            {
                var id = service.ServiceID;

                if (id == ServiceIDs.Board)
                    Services[id] = new BoardUI(this, service);

                if (id == ServiceIDs.Buddy)
                    Services[id] = new BuddyUI(this, service);

                if (id == ServiceIDs.Chat)
                    Services[id] = new ChatUI(this, service);

                if (id == ServiceIDs.IM)
                    Services[id] = new IMUI(this, service);

                if (id == ServiceIDs.Mail)
                    Services[id] = new MailUI(this, service);

                if (id == ServiceIDs.Plan)
                    Services[id] = new PlanUI(this, service);

                if (id == ServiceIDs.Profile)
                    Services[id] = new ProfileUI(this, service);

                if (id == ServiceIDs.Share)
                    Services[id] = new ShareUI(this, service);

                if (id == ServiceIDs.Storage)
                    Services[id] = new StorageUI(this, service);

                if (id == ServiceIDs.Trust)
                    Services[id] = new TrustUI(this, service);
            }

            Core.RunInGui += Core_RunInGui;
            Core.UpdateConsole += Core_UpdateConsole;
            Core.ShowConfirm += Core_ShowConfirm;
            Core.ShowMessage += Core_ShowMessage;
            Core.VerifyPass += Core_VerifyPass;

            Core_UpdateConsole("DeOps " + Application.ProductVersion);
        }

        public void ShowMainView(bool sideMode = false)
        {
            if (Core.User.Settings.GlobalIM)
                GuiMain = new IMForm(this);
            else
                GuiMain = new MainForm(this, sideMode);

            GuiMain.Show();
        }

        public IServiceUI GetService(uint id)
        {
            if (Services.ContainsKey(id))
                return Services[id];

            return null;
        }

        public void Core_RunInGui(Delegate method, params object[] args)
        {
            if (method == null || GuiMain == null)
                return;

            //LastEvents.Enqueue(method);
            //while (LastEvents.Count > 10)
            //    LastEvents.Dequeue();

            GuiMain.BeginInvoke(method, args);
        }

        public void Core_UpdateConsole(string message)
        {
            if (GuiConsole != null)
                GuiConsole.UpdateConsole(message);
        }

        public bool Core_ShowConfirm(string message, string title)
        {
            var result = MessageBox.Show(message, title, MessageBoxButtons.YesNo);

            return (result == DialogResult.Yes);
        }

        public void Core_ShowMessage(string message)
        {
            MessageBox.Show(GuiMain, message);
        }

        public bool Core_VerifyPass(ThreatLevel threat)
        {
            return GuiUtils.VerifyPassphrase(Core, ThreatLevel.Medium);
        }
    }

    public interface IServiceUI
    {
        void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong user, uint project);

        void GetNewsAction(ref Icon symbol, ref EventHandler onClick);
    }
}
