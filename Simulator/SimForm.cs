using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;

using Microsoft.Win32;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Transport;

using DeOps.Services;
using DeOps.Services.Transfer;

using DeOps.Interface;
using DeOps.Interface.Tools;


namespace DeOps.Simulator
{
    delegate void RunServiceMethod(OpService service);


    internal partial class SimForm : CustomIconForm
    {
        internal InternetSim Sim;

        private ListViewColumnSorter lvwColumnSorter = new ListViewColumnSorter();

        internal Dictionary<ulong, NetView> NetViews = new Dictionary<ulong, NetView>();

        FileStream TimeFile;

        public int UiThreadId;

        //bool Loaded;
        //string DelayLoadPath;


        internal SimForm()
        {
            Construct();
        }

        internal SimForm(string path)
        {
            Construct();

            //DelayLoadPath = path;
        }

        void Construct()
        {
            InitializeComponent();

            ListInstances.ListViewItemSorter = lvwColumnSorter;

            UiThreadId = Thread.CurrentThread.ManagedThreadId;

            Sim = new InternetSim(Application.StartupPath, InterfaceRes.deops);

        }

        private void ControlForm_Load(object sender, EventArgs e)
        {
            Sim.InstanceChange += new InstanceChangeHandler(OnInstanceChange);
        }

        void DownloadStringCallback(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e.Result.Contains("**die**"))
                {
                    MessageBox.Show("Expired, contact JMG");
                    Close();
                }
            }
            catch
            {
            }
        }

        private void LoadMenuItem_Click(object sender, EventArgs e)
        {
            // choose folder to load from
            try
            {
                FolderBrowserDialog browse = new FolderBrowserDialog();

                string path = Properties.Settings.Default.LastSimPath;

                if (path == null)
                    path = Application.StartupPath;

                browse.SelectedPath = path;

                if (browse.ShowDialog(this) != DialogResult.OK)
                    return;
                   
                LoadDirectory(browse.SelectedPath);

                Properties.Settings.Default.LastSimPath = browse.SelectedPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void LoadDirectory(string dirpath)
        {
            Sim.LoadedPath = dirpath;

            if (Sim.UseTimeFile)
            {
                TimeFile = new FileStream(dirpath + Path.DirectorySeparatorChar + "time.dat", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

                if (TimeFile.Length >= 8)
                {
                    byte[] startTime = new byte[8];
                    TimeFile.Read(startTime, 0, 8);
                    Sim.TimeNow = DateTime.FromBinary(BitConverter.ToInt64(startTime, 0));
                    Sim.StartTime = Sim.TimeNow;
                }
            }

            string[] dirs = Directory.GetDirectories(dirpath);

            LoadProgress.Visible = true;
            LoadProgress.Maximum = dirs.Length;
            LoadProgress.Value = 0;

            foreach (string dir in dirs)
            {
                string[] paths = Directory.GetFiles(dir, "*.rop");

                foreach (string path in paths)
                {
                    string filename = Path.GetFileNameWithoutExtension(path);
                    string[] parts = filename.Split('-');
                    string op = parts[0].Trim();
                    string name = parts[1].Trim();

                    // if instance with same user name, who has not joined this operation - add to same instance
                    SimInstance instance = null;

                    Sim.Instances.LockReading(() =>
                        instance = Sim.Instances.Where(i => i.Name == name && !i.Ops.Contains(op)).FirstOrDefault());

                    if (instance != null)
                        Login(instance, path);
                    else
                        Sim.StartInstance(path);
                }
                LoadProgress.Value = LoadProgress.Value + 1;
                Application.DoEvents();
            }

            LoadProgress.Visible = false;
        }

        private void Login(SimInstance instance, string path)
        {
            try
            {
                Sim.Login(instance, path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, instance.Name + ": " + ex.Message);
                return;
            }
        }

        private void SaveMenuItem_Click(object sender, EventArgs e)
        {
            Sim.Pause();

            Sim.Instances.SafeForEach(instance =>
            {
                instance.Context.Cores.LockReading(delegate()
                {
                    foreach (OpCore core in instance.Context.Cores)
                        core.User.Save();
                });
            });

            MessageBox.Show(this, "Nodes Saved");
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }    

        private void buttonStart_Click(object sender, EventArgs e)
        {
            Sim.Start();
        }

        private void ButtonStep_Click(object sender, EventArgs e)
        {
            Sim.DoStep();
        }  

        private void buttonPause_Click(object sender, EventArgs e)
        {
            Sim.Pause();
        }

        void OnInstanceChange(SimInstance instance, InstanceChangeType type)
        {
            if (Thread.CurrentThread.ManagedThreadId != UiThreadId)
            {
                BeginInvoke(Sim.InstanceChange, instance, type);
                return;
            }

            // add
            if (type == InstanceChangeType.Add)
            {
                AddItem(instance);
            }

            // refresh
            else if (type == InstanceChangeType.Refresh)
            {
                ListInstances.Items.Clear();

                Sim.Instances.SafeForEach(i =>
                {
                    AddItem(i);
                });

                LabelInstances.Text = Sim.Instances.SafeCount.ToString() + " Instances";
            }

            // update
            else if (type == InstanceChangeType.Update)
            {
                foreach (ListInstanceItem item in ListInstances.Items)
                    if (item.Instance == instance)
                    {
                        item.Refresh();
                        break;
                    }
            }

            // remove
            else if (type == InstanceChangeType.Remove)
            {
                foreach (ListInstanceItem item in ListInstances.Items)
                    if (item.Instance == instance)
                    {
                        ListInstances.Items.Remove(item);
                        break;
                    }
            }
        }

        private void AddItem(SimInstance instance)
        {
            instance.Context.Cores.LockReading(delegate()
            {
                foreach (OpCore core in instance.Context.Cores)
                    ListInstances.Items.Add(new ListInstanceItem(core));

                if (instance.Context.Cores.Count == 0)
                    ListInstances.Items.Add(new ListInstanceItem(instance));
            });
        }

        DateTime LastSimTime = new DateTime(2006, 1, 1, 0, 0, 0);

        private void SecondTimer_Tick(object sender, EventArgs e)
        {
            TimeSpan total = Sim.TimeNow - Sim.StartTime;
            TimeSpan real = Sim.TimeNow - LastSimTime;

            LastSimTime = Sim.TimeNow;

 
            labelTime.Text = SpantoString(real);
            TimeLabel.Text = Sim.TimeNow.ToString();
            ElapsedLabel.Text = SpantoString(total);

            
            if (Sim.UseTimeFile && TimeFile != null)
            {
                TimeFile.Seek(0, SeekOrigin.Begin);
                TimeFile.Write(BitConverter.GetBytes(Sim.TimeNow.ToBinary()), 0, 8);
            }

            /*if (Loaded && DelayLoadPath != null)
            {
                string path = DelayLoadPath;
                DelayLoadPath = null; // done because doevents will refire
                LoadDirectory(Application.StartupPath + Path.DirectorySeparatorChar + path);
            }*/
        }

        string SpantoString(TimeSpan span)
        {
            string postfix = "";

            if (span.Milliseconds == 0)
                postfix += ".00";
            else
                postfix += "." + span.Milliseconds.ToString().Substring(0, 2);

            span = span.Subtract(new TimeSpan(0, 0, 0, 0, span.Milliseconds));

            return span.ToString() + postfix;

        }

        private void ControlForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Sim.Exit();

            Application.Exit();
        }

        private void listInstances_MouseClick(object sender, MouseEventArgs e)
        {
            // main
            // internal
            // global
                // crawler
                // graph
                // packets
                // search
            // operation
            // console
            // ---
            // Disconnect

            if (e.Button != MouseButtons.Right)
                return;

            ListInstanceItem item = ListInstances.GetItemAt(e.X, e.Y) as ListInstanceItem;

            if (item == null)
                return;

            ContextMenu menu = new ContextMenu();

            if(item.Core == null)
                menu.MenuItems.Add(new MenuItem("Login", new EventHandler(Click_Connect)));
            else
            {
                MenuItem global = null;

                if (item.Instance.Context.Lookup != null)
                {
                    global = new MenuItem("Lookup");
                    global.MenuItems.Add(new MenuItem("Crawler", new EventHandler(Click_GlobalCrawler)));
                    global.MenuItems.Add(new MenuItem("Graph", new EventHandler(Click_GlobalGraph)));
                    global.MenuItems.Add(new MenuItem("Packets", new EventHandler(Click_GlobalPackets)));
                    global.MenuItems.Add(new MenuItem("Search", new EventHandler(Click_GlobalSearch)));
                }

                MenuItem operation = new MenuItem("Operation");
                operation.MenuItems.Add(new MenuItem("Crawler", new EventHandler(Click_OpCrawler)));
                operation.MenuItems.Add(new MenuItem("Graph", new EventHandler(Click_OpGraph)));
                operation.MenuItems.Add(new MenuItem("Packets", new EventHandler(Click_OpPackets)));
                operation.MenuItems.Add(new MenuItem("Search", new EventHandler(Click_OpSearch)));

                MenuItem firewall = new MenuItem("Firewall");
                firewall.MenuItems.Add(new MenuItem("Open", new EventHandler(Click_FwOpen)));
                firewall.MenuItems.Add(new MenuItem("NAT", new EventHandler(Click_FwNAT)));
                firewall.MenuItems.Add(new MenuItem("Blocked", new EventHandler(Click_FwBlocked)));


                menu.MenuItems.Add(new MenuItem("Main", new EventHandler(Click_Main)));
                menu.MenuItems.Add(new MenuItem("Internal", new EventHandler(Click_Internal)));
                menu.MenuItems.Add(new MenuItem("Bandwidth", new EventHandler(Click_Bandwidth)));
                menu.MenuItems.Add(new MenuItem("Transfers", new EventHandler(Click_Transfers)));
                if(global != null) menu.MenuItems.Add(global);
                menu.MenuItems.Add(operation);
                menu.MenuItems.Add(firewall);
                menu.MenuItems.Add(new MenuItem("Console", new EventHandler(Click_Console)));
                menu.MenuItems.Add(new MenuItem("-"));
                menu.MenuItems.Add(new MenuItem("Logout", new EventHandler(Click_Disconnect)));
            }

            menu.Show(ListInstances, e.Location);
        }

        private void listInstances_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Click_Main(null, null);
        }

        private void Click_Main(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
            {
                OpCore core = item.Core;

                if (core == null)
                {
                    item.Instance.Context.RaiseLogin(null);
                    return;
                }

                if (item.UI.GuiMain == null)
                    item.UI.ShowMainView();

                item.UI.GuiMain.Activate();
            }
        }
        
        private void Click_FwOpen(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
            {
                item.Instance.RealFirewall = FirewallType.Open;
                OnInstanceChange(item.Instance, InstanceChangeType.Update);
            }
        }

        private void Click_FwNAT(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
            {
                item.Instance.RealFirewall = FirewallType.NAT;
                OnInstanceChange(item.Instance, InstanceChangeType.Update);
            }
        }

        private void Click_FwBlocked(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
            {
                item.Instance.RealFirewall = FirewallType.Blocked;
                OnInstanceChange(item.Instance, InstanceChangeType.Update);
            }
        }

        private void Click_Console(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
            {
                CoreUI ui = item.UI;

                if (ui.GuiConsole == null)
                    ui.GuiConsole = new ConsoleForm(ui);

                ui.GuiConsole.Show();
                ui.GuiConsole.Activate();
            }
        }

        private void Click_Internal(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
                InternalsForm.Show(item.UI);
        }

        private void Click_Bandwidth(object sender, EventArgs e)
        {
            List<OpCore> cores = new List<OpCore>();

            foreach (ListInstanceItem item in ListInstances.SelectedItems)
            {
                if (!cores.Contains(item.Core.Context.Lookup))
                    cores.Add(item.Core.Context.Lookup);
                
                cores.Add(item.Core);
            }

            new BandwidthForm(cores).Show();
        }

        private void Click_Transfers(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
                TransferView.Show(item.Core.Network);
        }

        private void Click_GlobalCrawler(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
                if (item.Core.Context.Lookup != null)
                    CrawlerForm.Show(item.Core.Context.Lookup.Network);
        }

        private void Click_GlobalGraph(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
                if (item.Core.Context.Lookup != null)
                    PacketsForm.Show(item.Core.Context.Lookup.Network);
        }

        private void Click_GlobalPackets(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
                if (item.Core.Context.Lookup != null)
                    PacketsForm.Show(item.Core.Context.Lookup.Network);
        }

        private void Click_GlobalSearch(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
                if (item.Core.Context.Lookup != null)
                    SearchForm.Show(item.Core.Context.Lookup.Network);
        }

        private void Click_OpCrawler(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
                CrawlerForm.Show(item.Core.Network);
        }

        private void Click_OpGraph(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
                GraphForm.Show(item.Core.Network);
        }

        private void Click_OpPackets(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
                PacketsForm.Show(item.Core.Network);
        }

        private void Click_OpSearch(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
                SearchForm.Show(item.Core.Network);
        }

        private void Click_Connect(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
                if(item.Core == null)
                    Login(item.Instance, item.Instance.LastPath);

            OnInstanceChange(null, InstanceChangeType.Refresh);
        }

        private void Click_Disconnect(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in ListInstances.SelectedItems)
                if (item.Core != null)
                {
                    OpCore core = item.Core;
                    item.Core = null; // remove list item reference

                    Sim.Logout(core);
                }
        }

        private void listInstances_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.ColumnToSort)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.OrderOfSort == SortOrder.Ascending)
                    lvwColumnSorter.OrderOfSort = SortOrder.Descending;
                else
                    lvwColumnSorter.OrderOfSort = SortOrder.Ascending;
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.ColumnToSort = e.Column;
                lvwColumnSorter.OrderOfSort = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            ListInstances.Sort();
        }

        private void LinkUpdate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

            OnInstanceChange(null, InstanceChangeType.Refresh);

            //Sim.Instances.SafeForEach(instance =>
            //        OnInstanceChange(instance, InstanceChangeType.Update);
        }

        private void ViewMenu_DropDownOpening(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;

            if (item == null || Sim == null) 
                return;

            item.DropDownItems.Clear();
            item.DropDownItems.Add(new ViewMenuItem("Lookup", 0, new EventHandler(ViewMenu_OnClick)));

            foreach(ulong id in Sim.OpNames.Keys)
                item.DropDownItems.Add(new ViewMenuItem(Sim.OpNames[id], id, new EventHandler(ViewMenu_OnClick)));
        }

        private void ViewMenu_OnClick(object sender, EventArgs e)
        {
            ViewMenuItem item = sender as ViewMenuItem;

            if (item == null || Sim == null)
                return;

            NetView view = null;

            if (!NetViews.ContainsKey(item.OpID))
            {
                view = new NetView(this, item.OpID);
                NetViews[item.OpID] = view;
            }
            else
                view = NetViews[item.OpID];

            view.Show();
            view.BringToFront();
        }

        private void GenerateUsersMenuItem_Click(object sender, EventArgs e)
        {
            GenerateUsers form = new GenerateUsers();
            form.ShowDialog();
        }

        private void OptionsMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            /*string enc = "Encryption: ";
            enc += Sim.TestEncryption ? "On" : "Off";
            EncryptionMenuItem.Text = enc;*/

            string text = "Speed: ";
            text += Sim.SleepTime.ToString() + "ms";
            SpeedMenuItem.Text = text;

            FreshStartMenuItem.Checked = Sim.FreshStart;
            LoadOnlineMenuItem.Checked = Sim.LoadOnline;
            LoggingMenu.Checked = Sim.Logging;
            LanMenu.Checked = Sim.LAN;
            EncryptionMenu.Checked = Sim.TestEncryption;
        }

        private void SpeedMenuItem_Click(object sender, EventArgs e)
        {
            GetTextDialog getText = new GetTextDialog("Options", "Enter # of ms to sleep between sim ticks (1000ms is real-time)", Sim.SleepTime.ToString());

            if (getText.ShowDialog() == DialogResult.OK)
                int.TryParse(getText.ResultBox.Text, out Sim.SleepTime);
        }

        private void FreshStartMenuItem_Click(object sender, EventArgs e)
        {
            Sim.FreshStart = !Sim.FreshStart;
        }

        private void CollectMenuItem_Click(object sender, EventArgs e)
        {
            GC.Collect();
        }

        private void LoadOnlineMenu_Click(object sender, EventArgs e)
        {
            Sim.LoadOnline = !Sim.LoadOnline;
        }
        
        private void LoggingMenu_Click(object sender, EventArgs e)
        {
            Sim.Logging = !Sim.Logging;
        }

        private void UnloadAllMenuItem_Click(object sender, EventArgs e)
        {
            // unload
            Sim.InstanceChange -= new InstanceChangeHandler(OnInstanceChange);

            if (TimeFile != null)
            {
                TimeFile.Dispose();
                TimeFile = null;
            }

            Sim.Exit();


            // re-init
            Sim = new InternetSim(Application.StartupPath, InterfaceRes.deops);
            Sim.InstanceChange += new InstanceChangeHandler(OnInstanceChange);

            OnInstanceChange(null, InstanceChangeType.Refresh);
        }

        private void LanMenu_Click(object sender, EventArgs e)
        {
            Sim.LAN = !Sim.LAN;
        }

        private void EncryptionMenu_Click(object sender, EventArgs e)
        {
            Sim.TestEncryption = !Sim.TestEncryption;
        }

        private void TestServicesMenu_Click(object sender, EventArgs e)
        {
            SelectServices form = new SelectServices(this, "Test Services", delegate(OpService service) { service.SimTest(); });

            form.Show();
        }

        private void CleanupServicesMenu_Click(object sender, EventArgs e)
        {
            SelectServices form = new SelectServices(this, "Cleanup Services", delegate(OpService service) { service.SimCleanup(); });

            form.Show();
        }


    }

    internal class ViewMenuItem : ToolStripMenuItem
    {
        internal ulong OpID;

        internal ViewMenuItem(string name, ulong id, EventHandler onClick)
            : base(name, null, onClick)
        {
            OpID = id;
        }

    }

    internal class ListInstanceItem : ListViewItem
    {
        internal SimInstance Instance;
        internal OpCore Core;
        internal CoreUI UI;

        internal ListInstanceItem(SimInstance instance)
        {
            // empty context
            Instance = instance;

            Refresh();
        }

        internal ListInstanceItem(OpCore core)
        {
            Core = core;
            UI = new CoreUI(core);
            Instance = core.Sim;

            Core.Exited += Core_Exited;

            Refresh();
        }

        void Core_Exited(OpCore core)
        {
            if (UI.GuiMain != null)
                UI.GuiMain.Close();
        }

        internal void Refresh()
        {
            int SUBITEM_COUNT = 11;

            while (SubItems.Count < SUBITEM_COUNT)
                SubItems.Add("");

            Text = Instance.Index.ToString();

            if (Core == null)
            {
                SubItems[1].Text = "Login: " + Path.GetFileNameWithoutExtension(Instance.LastPath);
                ForeColor = Color.Gray;

                for(int i = 2; i < SUBITEM_COUNT; i++)
                    SubItems[i].Text = "";

                return;
            }

            ForeColor = Color.Black;
            
            // 0 context index
            // 1 user
            // 2 op
            // 3 Dht id
            // 4 client id
            // 5 firewall
            // 6 alerts
            // 7 proxies
            // 8 Notes
            // 9 bytes in
            // 10 bytes out

            // alerts...
            string alerts = "";
            
            // firewall incorrect
            if (Instance.RealFirewall != Core.Firewall)
                alerts += "Firewall, ";

            // ip incorrect
            //if (Instance.Context.LocalIP != null && !Instance.RealIP.Equals(Instance.Context.LocalIP))
            //    alerts += "IP Mismatch, ";

            // routing unresponsive global/op
            if(Instance.Context.Lookup != null)
                if (!Instance.Context.Lookup.Network.Responsive)
                    alerts += "Lookup Routing, ";

            if (!Core.Network.Responsive)
                alerts += "Op Routing, ";

            // not proxied global/op
            if (Instance.RealFirewall != FirewallType.Open)
            {
                if (Instance.Context.Lookup != null)
                    if (Instance.Context.Lookup.Network.TcpControl.ProxyMap.Count == 0)
                        alerts += "Lookup Proxy, ";

                if (Core.Network.TcpControl.ProxyMap.Count == 0)
                    alerts += "Op Proxy, ";
            }

            // locations
            if (Core.Locations.Clients.SafeCount <= 1)
                alerts += "Locs, ";

            SubItems[1].Text = Core.User.Settings.UserName;
            SubItems[2].Text = Core.User.Settings.Operation;
            SubItems[3].Text = Utilities.IDtoBin(Core.UserID);
            SubItems[4].Text = Instance.RealIP.ToString() + "/" + Core.Network.Local.ClientID.ToString();
            SubItems[5].Text = Instance.RealFirewall.ToString();
            SubItems[6].Text = alerts;

            if (Instance.Context.Lookup != null)
            {
                SubItems[7].Text = ProxySummary(Instance.Context.Lookup.Network.TcpControl);
                SubItems[8].Text = NotesSummary();
            }

            SubItems[9].Text = Utilities.CommaIze(Instance.BytesRecvd);
            SubItems[10].Text = Utilities.CommaIze(Instance.BytesSent);
        }

        private string ProxySummary(TcpHandler control)
        {
            StringBuilder summary = new StringBuilder();

            lock (control.SocketList)
                foreach (TcpConnect connect in control.SocketList)
                    if (connect.State == TcpState.Connected &&
                        (connect.Proxy == ProxyType.ClientBlocked || connect.Proxy == ProxyType.ClientNAT) &&
                         Instance.Internet.UserNames.ContainsKey(connect.UserID))
                        summary.Append(Instance.Internet.UserNames[connect.UserID] + ", ");

            return summary.ToString();
        }

        private string NotesSummary()
        {
            //crit change store to other column at end total searches and transfers to detect backlogs


            StringBuilder summary = new StringBuilder();

            if(Core.Trust != null)
                summary.Append(Core.Trust.TrustMap.SafeCount.ToString() + " trust, ");

            summary.Append(Core.Locations.Clients.SafeCount.ToString() + " locs, ");

            summary.Append(Core.Network.Searches.Pending.Count.ToString() + " searches, ");

            summary.Append(Core.Transfers.Transfers.Count.ToString() + " transfers, ");

            summary.Append(Core.Network.RudpControl.SessionMap.Count.ToString() + " sessions");

            //foreach (ulong key in store.Index.Keys)
            //    foreach (StoreData data in store.Index[key])
            //        if (data.Kind == DataKind.Profile)
             //           summary.Append(((ProfileData)data.Packet).Name + ", ");

            //foreach (ulong key in store.Index.Keys)
            //    if(Instance.Internet.OpNames.ContainsKey(key))
            //        summary.Append(Instance.Internet.OpNames[key] + " " + store.Index[key].Count + ", ");

            return summary.ToString();
        }
    }
}

