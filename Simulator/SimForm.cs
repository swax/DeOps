using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Transport;

using RiseOp.Services.Transfer;

using RiseOp.Interface;
using RiseOp.Interface.Tools;


namespace RiseOp.Simulator
{

    internal partial class SimForm : Form
    {
        internal InternetSim Sim;



        private ListViewColumnSorter lvwColumnSorter = new ListViewColumnSorter();

        internal Dictionary<ulong, NetView> NetViews = new Dictionary<ulong, NetView>();

        FileStream TimeFile;

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

            listInstances.ListViewItemSorter = lvwColumnSorter;

            Sim = new InternetSim(this);
        }

        private void ControlForm_Load(object sender, EventArgs e)
        {
            Sim.InstanceChange += new InstanceChangeHandler(OnInstanceChange);

            //Loaded = true;
            string name = SystemInformation.UserName;
            string comp = SystemInformation.ComputerName;
            
            // call home for alpha security
#if !DEBUG
            WebClient client  = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler( DownloadStringCallback);
            client.DownloadStringAsync(new Uri("http://www.c0re.net/deops/update.php?build=" + "alpha2" + "&comp=" + comp + "&name=" + name ));
#endif 
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

                browse.SelectedPath = Application.StartupPath;

                if (browse.ShowDialog(this) == DialogResult.OK)
                    LoadDirectory(browse.SelectedPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void LoadDirectory(string dirpath)
        {
            if (Sim.UseTimeFile)
            {
                TimeFile = new FileStream(dirpath + Path.DirectorySeparatorChar + "time.dat", FileMode.OpenOrCreate, FileAccess.ReadWrite);

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
                string[] paths = Directory.GetFiles(dir, "*.dop");

                foreach (string path in paths)
                    Sim.AddInstance(path);

                LoadProgress.Value = LoadProgress.Value + 1;
                Application.DoEvents();
            }

            LoadProgress.Visible = false;
        }

        private void SaveMenuItem_Click(object sender, EventArgs e)
        {
            Sim.Pause();

            foreach (SimInstance instance in Sim.Instances)
                instance.Core.User.Save();

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
            // add
            if (type == InstanceChangeType.Add)
            {
                listInstances.Items.Add(new ListInstanceItem(instance));
            }

            // refresh
            else if (type == InstanceChangeType.Refresh)
            {
                listInstances.Items.Clear();
                foreach(SimInstance inst in Sim.Instances)
                    listInstances.Items.Add(new ListInstanceItem(inst));
            }

            // update
            else if (type == InstanceChangeType.Update)
            {
                foreach (ListInstanceItem item in listInstances.Items)
                    if (item.Instance == instance)
                    {
                        item.Refresh();
                        break;
                    }
            }

            // remove
            else if (type == InstanceChangeType.Remove)
            {
                foreach (ListInstanceItem item in listInstances.Items)
                    if (item.Instance == instance)
                    {
                        listInstances.Items.Remove(item);
                        break;
                    }
            }
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

            ListInstanceItem item = listInstances.GetItemAt(e.X, e.Y) as ListInstanceItem;

            if (item == null)
                return;

            ContextMenu menu = new ContextMenu();

            if(item.Instance.Core == null)
                menu.MenuItems.Add(new MenuItem("Bring Online", new EventHandler(Click_Connect)));
            else
            {
                MenuItem global = new MenuItem("Global");
                global.MenuItems.Add(new MenuItem("Crawler", new EventHandler(Click_GlobalCrawler)));
                global.MenuItems.Add(new MenuItem("Graph", new EventHandler(Click_GlobalGraph)));
                global.MenuItems.Add(new MenuItem("Packets", new EventHandler(Click_GlobalPackets)));
                global.MenuItems.Add(new MenuItem("Search", new EventHandler(Click_GlobalSearch)));

                MenuItem operation = new MenuItem("Operation");
                operation.MenuItems.Add(new MenuItem("Crawler", new EventHandler(Click_OpCrawler)));
                operation.MenuItems.Add(new MenuItem("Graph", new EventHandler(Click_OpGraph)));
                operation.MenuItems.Add(new MenuItem("Packets", new EventHandler(Click_OpPackets)));
                operation.MenuItems.Add(new MenuItem("Search", new EventHandler(Click_OpSearch)));

                menu.MenuItems.Add(new MenuItem("Main", new EventHandler(Click_Main)));
                menu.MenuItems.Add(new MenuItem("Internal", new EventHandler(Click_Internal)));
                menu.MenuItems.Add(new MenuItem("Transfers", new EventHandler(Click_Transfers)));
                menu.MenuItems.Add(global);
                menu.MenuItems.Add(operation);
                menu.MenuItems.Add(new MenuItem("Console", new EventHandler(Click_Console)));
                menu.MenuItems.Add(new MenuItem("-"));
                menu.MenuItems.Add(new MenuItem("Bring Offline", new EventHandler(Click_Disconnect)));
            }

            menu.Show(listInstances, e.Location);
        }

        private void listInstances_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Click_Main(null, null);
        }

        private void Click_Main(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in listInstances.SelectedItems)
            {
                OpCore core = item.Instance.Core;

                if (core == null)
                    return;

                if (core.GuiMain == null)
                    core.GuiMain = new MainForm(core);

                core.GuiMain.Show();
                core.GuiMain.Activate();
            }
        }

        private void Click_Console(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in listInstances.SelectedItems)
            {
                OpCore core = item.Instance.Core;

                if (core.GuiConsole == null)
                    core.GuiConsole = new ConsoleForm(core);

                core.GuiConsole.Show();
                core.GuiConsole.Activate();
            }
        }

        private void Click_Internal(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in listInstances.SelectedItems)
            {
                OpCore core = item.Instance.Core;

                
                if (core.GuiInternal == null)
                    core.GuiInternal = new InternalsForm(core);

                core.GuiInternal.Show();
                core.GuiInternal.Activate();
            }
        }

        private void Click_Transfers(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in listInstances.SelectedItems)
            {
                OpCore core = item.Instance.Core;

                TransferView view = new TransferView(core.Transfers);
                view.Show(this);
            }
        }

        private void Click_GlobalCrawler(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in listInstances.SelectedItems)
            {
                OpCore core = item.Instance.Core;

                DhtNetwork network = core.GlobalNet;

                if (network.GuiCrawler == null)
                    network.GuiCrawler = new CrawlerForm("Global", network);

                network.GuiCrawler.Show();
                network.GuiCrawler.Activate();
            }
        }

        private void Click_GlobalGraph(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in listInstances.SelectedItems)
            {
                OpCore core = item.Instance.Core;

                DhtNetwork network = core.GlobalNet;

                if (network.GuiGraph == null)
                    network.GuiGraph = new GraphForm("Global", network);

                network.GuiGraph.Show();
                network.GuiGraph.Activate();
            }
        }

        private void Click_GlobalPackets(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in listInstances.SelectedItems)
            {
                OpCore core = item.Instance.Core;

                DhtNetwork network = core.GlobalNet;

                if (network.GuiPackets == null)
                    network.GuiPackets = new PacketsForm("Global", network);

                network.GuiPackets.Show();
                network.GuiPackets.Activate();
            }
        }

        private void Click_GlobalSearch(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in listInstances.SelectedItems)
            {
                SearchForm form = new SearchForm("Global", this, item.Instance.Core.GlobalNet);

                form.Show();
            }
        }

        private void Click_OpCrawler(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in listInstances.SelectedItems)
            {
                OpCore core = item.Instance.Core;

                DhtNetwork network = core.OperationNet;

                if (network.GuiCrawler == null)
                    network.GuiCrawler = new CrawlerForm("Operation", network);

                network.GuiCrawler.Show();
                network.GuiCrawler.Activate();
            }
        }

        private void Click_OpGraph(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in listInstances.SelectedItems)
            {
                OpCore core = item.Instance.Core;

                DhtNetwork network = core.OperationNet;

                if (network.GuiGraph == null)
                    network.GuiGraph = new GraphForm("Operation", network);

                network.GuiGraph.Show();
                network.GuiGraph.Activate();
            }
        }

        private void Click_OpPackets(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in listInstances.SelectedItems)
            {
                OpCore core = item.Instance.Core;

                DhtNetwork network = core.OperationNet;

                if (network.GuiPackets == null)
                    network.GuiPackets = new PacketsForm("Operation", network);

                network.GuiPackets.Show();
                network.GuiPackets.Activate();
            }
        }

        private void Click_OpSearch(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in listInstances.SelectedItems)
            {
                SearchForm form = new SearchForm("Operation", this, item.Instance.Core.OperationNet);
                form.Show();
            }
        }

        private void Click_Connect(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in listInstances.SelectedItems)
                if(item.Instance.Core == null)
                    Sim.BringOnline(item.Instance);
        }

        private void Click_Disconnect(object sender, EventArgs e)
        {
            foreach (ListInstanceItem item in listInstances.SelectedItems)
                if(item.Instance.Core != null)
                    Sim.BringOffline(item.Instance);
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
            listInstances.Sort();
        }

        private void LinkUpdate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            lock(Sim.Instances)
                foreach (SimInstance instance in Sim.Instances)
                    OnInstanceChange(instance, InstanceChangeType.Update);

            LabelInstances.Text = Sim.Instances.Count.ToString() + " Instances";
        }

        private void ViewMenu_DropDownOpening(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;

            if (item == null || Sim == null) 
                return;

            item.DropDownItems.Clear();
            item.DropDownItems.Add(new ViewMenuItem("Global", 0, new EventHandler(ViewMenu_OnClick)));

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

            text = "Fresh Start: ";
            text += Sim.FreshStart ? "Yes" : "No";
            FreshStartMenuItem.Text = text;

            text = "Load Online: ";
            text += Sim.LoadOnline ? "Yes" : "No";
            LoadOnlineMenuItem.Text = text;
        }

        private void EncryptionMenuItem_Click(object sender, EventArgs e)
        {
            Sim.TestEncryption = !Sim.TestEncryption;
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
            Sim = new InternetSim(this);
            Sim.InstanceChange += new InstanceChangeHandler(OnInstanceChange);

            OnInstanceChange(null, InstanceChangeType.Refresh);
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

        internal ListInstanceItem(SimInstance instance)
        {
            Instance = instance;

            Refresh();
        }

        internal void Refresh()
        {
            int SUBITEM_COUNT = 9;

            while (SubItems.Count < SUBITEM_COUNT)
                SubItems.Add("");

            if (Instance.Core == null)
            {
                Text = Path.GetFileNameWithoutExtension(Instance.Path);
                ForeColor = Color.Gray;

                SubItems[1].Text = ""; SubItems[2].Text = ""; SubItems[3].Text = ""; SubItems[4].Text = "";
                SubItems[5].Text = ""; SubItems[6].Text = ""; SubItems[7].Text = ""; 

                return;
            }


            Text = Instance.Core.User.Settings.ScreenName;
            ForeColor = Color.Black;
            
            // 0 user
            // 1 op
            // 2 Dht id
            // 3 client id
            // 4 firewall
            // 5 alerts
            // 6 proxies
            // 7 Notes
            // 8 bandwidth

            // alerts...
            string alerts = "";
            
            // firewall incorrect
            if (Instance.RealFirewall != Instance.Core.Firewall)
                alerts += "Firewall, ";

            // ip incorrect
            if (Instance.Core.LocalIP != null && !Instance.RealIP.Equals(Instance.Core.LocalIP))
                alerts += "IP Mismatch, ";

            // routing unresponsive global/op
            if(Instance.Core.GlobalNet != null)
                if(!Instance.Core.GlobalNet.Responsive)
                    alerts += "Global Routing, ";

            if (!Instance.Core.OperationNet.Responsive)
                alerts += "Op Routing, ";

            // not proxied global/op
            if (Instance.RealFirewall != FirewallType.Open)
            {
                if (Instance.Core.GlobalNet != null)
                    if (Instance.Core.GlobalNet.TcpControl.ProxyMap.Count == 0)
                        alerts += "Global Proxy, ";

                if (Instance.Core.OperationNet.TcpControl.ProxyMap.Count == 0)
                    alerts += "Op Proxy, ";
            }

            // locations
            if (Instance.Core.Locations.LocationMap.SafeCount <= 1)
                alerts += "Locs, ";

            
            SubItems[1].Text = Instance.Core.User.Settings.Operation;
            SubItems[2].Text = Utilities.IDtoBin(Instance.Core.OperationNet.LocalUserID);
            SubItems[3].Text = Instance.RealIP.ToString() + "/" + Instance.Core.OperationNet.ClientID.ToString(); 
            SubItems[4].Text = Instance.RealFirewall.ToString();
            SubItems[5].Text = alerts;

            if(Instance.Core.GlobalNet != null)
            {
                SubItems[6].Text = ProxySummary(Instance.Core.GlobalNet.TcpControl);
                SubItems[7].Text = NotesSummary(Instance.Core);
            }

            SubItems[8].Text = Instance.BytesRecvd.ToString() + " in / " + Instance.BytesSent.ToString() + " out";
        }

        private string ProxySummary(TcpHandler control)
        {
            StringBuilder summary = new StringBuilder();

            lock(control.SocketList)
                foreach(TcpConnect connect in control.SocketList)
                    if(connect.State == TcpState.Connected)
                        if (connect.Proxy == ProxyType.ClientBlocked || connect.Proxy == ProxyType.ClientNAT)
                            if(Instance.Internet.UserNames.ContainsKey(connect.userID))
                                summary.Append(Instance.Internet.UserNames[connect.userID] + ", ");

            return summary.ToString();
        }

        private string NotesSummary(OpCore core)
        {
            //crit change store to other column at end total searches and transfers to detect backlogs


            StringBuilder summary = new StringBuilder();

            summary.Append(core.Links.TrustMap.SafeCount.ToString() + " links, ");

            summary.Append(core.Locations.LocationMap.SafeCount.ToString() + " locs, ");

            summary.Append(core.OperationNet.Searches.Pending.Count.ToString() + " searches, ");

            summary.Append(core.Transfers.Pending.Count.ToString() + " transfers");

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

