using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;

using RiseOp.Services;
using RiseOp.Services.Chat;
using RiseOp.Services.IM;
using RiseOp.Services.Location;
using RiseOp.Services.Mail;
using RiseOp.Services.Transfer;
using RiseOp.Services.Trust;

using RiseOp.Interface.Tools;
using RiseOp.Interface.TLVex;
using RiseOp.Interface.Views;


namespace RiseOp.Interface
{
    internal delegate void ShowExternalHandler(ViewShell view);
    internal delegate void ShowInternalHandler(ViewShell view);


    internal partial class MainForm : CustomIconForm
    {
        internal OpCore Core;
        internal TrustService Trust;

        internal ShowExternalHandler ShowExternal;
        internal ShowInternalHandler ShowInternal;

        internal uint SelectedProject;

        ToolStripButton ProjectButton;
        uint ProjectButtonID;

        internal ViewShell InternalView;
        internal List<ExternalView> ExternalViews = new List<ExternalView>();

        Font BoldFont = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

        // news
        int NewsSequence;
        Queue<NewsItem> NewsPending = new Queue<NewsItem>();
        Queue<NewsItem> NewsRecent = new Queue<NewsItem>();
        SolidBrush NewsBrush = new SolidBrush(Color.FromArgb(0, Color.White));
        Rectangle NewsArea;
        bool NewsHideUpdates;

        // bottom status panel
        StringBuilder StatusHtml = new StringBuilder(4096);

        const string NetworkPage =
             @"<html>
                <head>
	                <style type=""text/css"">
	                <!--
	                    body { margin: 0; }
	                    p    { font-size: 8.25pt; font-family: Tahoma }
	                -->
	                </style>

                    <script>
                        function SetElement(id, text)
                        {
                            document.getElementById(id).innerHTML = text;
                        }
                    </script>
                </head>
                <body bgcolor=WhiteSmoke>
	                <table width=100% cellpadding=4>
	                    <tr><td bgcolor=green><p><b><font color=#ffffff>Network Status</font></b></p></td></tr>
	                </table>
                    <table callpadding=3>    
                        <tr><td><p><b>Global:</b></p></td><td><p><span id='global'><?=global?></span></p></td></tr>
	                    <tr><td><p><b>Network:</b></p></td><td><p><span id='operation'><?=operation?></span></p></td></tr>
	                    <tr><td><p><b>Firewall:</b></p></td><td><p><span id='firewall'><?=firewall?></span></p></td></tr>
                    </table>
                </body>
                </html>";

        const string NodePage =
                @"<html>
                <head>
	                <style type=""text/css"">
	                <!--
	                    body { margin: 0 }
	                    p    { font-size: 8.25pt; font-family: Tahoma }
                        A:link {text-decoration: none; color: black}
                        A:visited {text-decoration: none; color: black}
                        A:active {text-decoration: none; color: black}
                        A:hover {text-decoration: underline; color: black}
	                -->
	                </style>

                    <script>
                        function SetElement(id, text)
                        {
                            document.getElementById(id).innerHTML = text;
                        }
                    </script>
                </head>
                <body bgcolor=WhiteSmoke>
	                <table width=100% cellpadding=4>
	                    <tr><td bgcolor=MediumSlateBlue><p><font color=#ffffff><span id='name'><?=name?></span></font></p></td></tr>
	                </table>

                    <span id='content'><?=content?></span>

                    
                </body>
                </html>";

        // add gmt
        // add away status
        // add online status
        // add edit link

        internal MainForm(OpCore core) : base(core)
        {
            InitializeComponent();
            
            Core = core;
            Trust = Core.Trust;

            ShowExternal += new ShowExternalHandler(OnShowExternal);
            ShowInternal += new ShowInternalHandler(OnShowInternal);

            Core.NewsUpdate += new NewsUpdateHandler(Core_NewsUpdate);
            Trust.GuiUpdate  += new LinkGuiUpdateHandler(Trust_Update);
            Core.Locations.GuiUpdate += new LocationGuiUpdateHandler(Location_Update);
            Core.GetFocusedGui += new GetFocusedHandler(Core_GetFocused);

            CommandTree.SelectedLink = Core.UserID;

            TopToolStrip.Renderer = new ToolStripProfessionalRenderer(new OpusColorTable());
            NavStrip.Renderer = new ToolStripProfessionalRenderer(new NavColorTable());
            SideToolStrip.Renderer = new ToolStripProfessionalRenderer(new OpusColorTable());
            SideNavStrip.Renderer = new ToolStripProfessionalRenderer(new NavColorTable());
            
            SideNavStrip.Visible = false;
            CommandTree.Top = 0;
            CommandTree.Height = CommandSplit.Panel1.Height;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            UpdateTitle();
          
            CommandTree.Init(Trust);
            CommandTree.ShowProject(0);

            OnSelectChange(Core.UserID, CommandTree.Project);
            UpdateCommandPanel();

            if (SideMode)
            {
                SideButton.Checked = true;
                Left = Screen.PrimaryScreen.WorkingArea.Width - Width;
            }
        }

        private void UpdateTitle()
        {
            Text = Core.User.GetTitle();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Trace.WriteLine("Main Closing " + Thread.CurrentThread.ManagedThreadId.ToString());

            CommandTree.Fin();

            while (ExternalViews.Count > 0)
                if( !ExternalViews[0].SafeClose())
                {
                    e.Cancel = true;
                    return;
                }

            if (!CleanInternal())
            {
                e.Cancel = true;
                return;
            }

            ShowExternal -= new ShowExternalHandler(OnShowExternal);
            ShowInternal -= new ShowInternalHandler(OnShowInternal);

            Core.NewsUpdate -= new NewsUpdateHandler(Core_NewsUpdate);
            Trust.GuiUpdate -= new LinkGuiUpdateHandler(Trust_Update);
            Core.Locations.GuiUpdate -= new LocationGuiUpdateHandler(Location_Update);
            Core.GetFocusedGui -= new GetFocusedHandler(Core_GetFocused);

            Core.GuiMain = null;

            if(LockForm)
            {
                LockForm = false;
                return;
            }

            if (Core.Sim == null)
                Core.Exit();
        }

        bool LockForm;
        

        private void LockButton_Click(object sender, EventArgs e)
        {
            LockForm = true;

            Close();

            Core.GuiTray = new TrayLock(Core, SideMode);
        }

        private bool CleanInternal()
        {
            if (InternalView != null)
            {
                if (!InternalView.Fin())
                    return false;

                InternalView.Dispose();
            }

            InternalPanel.Controls.Clear();
            
            return true;
        }


        void OnShowExternal(ViewShell view)
        {
            ExternalView external = new ExternalView(this, view);

            ExternalViews.Add(external);

            if(InternalView == null || !InternalView.BlockReinit)
                view.Init();

            external.Show();
        }

        void OnShowInternal(ViewShell view)
        {
            if (!CleanInternal())
                return;

            view.Dock = DockStyle.Fill;

            InternalPanel.Visible = false;
            InternalPanel.Controls.Add(view);
            InternalView = view;

            UpdateNavBar();

            InternalView.Init();
            InternalPanel.Visible = true;
        }

        void Trust_Update(ulong key)
        {
            OpTrust trust = Trust.GetTrust(key);

            if (trust == null)
                return;

            if (key == Core.UserID)
                UpdateTitle();

            if (!Trust.ProjectRoots.SafeContainsKey(CommandTree.Project))
            {
                if (ProjectButton.Checked)
                    OperationButton.Checked = true;

                SideToolStrip.Items.Remove(ProjectButton);
                ProjectButton = null;
            }

            UpdateNavBar();

            if(key == CurrentStatusID)
                UpdateCommandPanel();
        }

        void Location_Update(ulong key)
        {
            if (key == CurrentStatusID)
                UpdateCommandPanel();
        }

        LinkNode GetSelected()
        {
            if (CommandTree.SelectedNodes.Count == 0)
                return null;

            TreeListNode node = CommandTree.SelectedNodes[0];

            if (node.GetType() != typeof(LinkNode))
                return null;

            return (LinkNode)node;
        }


        void UpdateCommandPanel()
        {
            if (GetSelected() == null)
                ShowNetworkStatus();

            else
                ShowNodeStatus();
        }

        enum StatusModeType {None, Network, Node };
        StatusModeType CurrentStatusMode = StatusModeType.None;
        ulong CurrentStatusID;

        private void ShowNetworkStatus()
        {
            StatusModeType mode = StatusModeType.Network;
            CurrentStatusID = 0;

            string global = "";
            string operation = "";

            if (Core.Context.Global == null)
                global = "Disconnected";
            else if (Core.Context.Global.Network.Responsive)
                global = "Connected";
            else
                global = "Connecting";

            if (Core.Network.Responsive)
                operation = "Connected";
            else
                operation = "Connecting";


            List<string[]> tuples = new List<string[]>();
            tuples.Add(new string[] { "global", global });
            tuples.Add(new string[] { "operation", operation });
            tuples.Add(new string[] { "firewall", Core.GetFirewallString() });

            
            if (CurrentStatusMode != mode)
            {
                CurrentStatusMode = mode;

                StatusHtml.Length = 0;
                StatusHtml.Append(NetworkPage);

                foreach (string[] tuple in tuples)
                    StatusHtml.Replace("<?=" + tuple[0] + "?>", tuple[1]);

                SetStatus(StatusHtml.ToString());
            }
            else
            {
                foreach (string[] tuple in tuples)
                    StatusBrowser.Document.InvokeScript("SetElement", new String[] { tuple[0], tuple[1] });
            }
        }


        private void ShowNodeStatus()
        {
            LinkNode node = GetSelected();

            if (node == null)
            {
                ShowNetworkStatus();
                return;
            }

            StatusModeType mode = StatusModeType.Node;
            
            OpLink link = node.Link;
            CurrentStatusID = link.UserID;

            List<Tuple<string, string>> tuples = new List<Tuple<string, string>>();

            string name = "";
            string content = "";

            // if loop root
            if (link.IsLoopRoot)
            {
                name = "<b>Trust Loop</b>";

                content = @"<table callpadding=3>
                            <tr><td>
                            <p><b>Order:</b><br>";

                string order = "";
                

                string confirmed = "";

                if (link.Downlinks.Count > 0)
                {
                    foreach (OpLink downlink in link.Downlinks)
                    {
                        confirmed = downlink.GetHigher(true) == null ? "(unconfirmed)" : "";

                        if (downlink.UserID == Core.UserID)
                            order += " &nbsp&nbsp&nbsp <b>" + Trust.GetName(downlink.UserID) + "</b> <i>trusts ";
                        else
                            order += " &nbsp&nbsp&nbsp " + Trust.GetName(downlink.UserID) + " <i>trusts ";

                        order += confirmed + "</i><br>";
                    }

                    order += " &nbsp&nbsp&nbsp " + Trust.GetName(link.Downlinks[0].UserID) + "<br>";
                }

                content += order + 
                            @"</p>
                            </tr></td>
                            </table>";
            }
            else
            {
                // name
                name = "<b>" + Trust.GetName(link.UserID) + "</b>";

                if (link.UserID == Core.UserID)
                    name += "  &nbsp&nbsp  (<a href='edit:local'><font color=white>edit</font></a>)";

                // title
                string title = link.Title;

                if (title != "")
                    tuples.Add(new Tuple<string, string>("Title: ", title));

                // projects
                string projects = "";
                foreach (uint id in link.Trust.Links.Keys)
                    if (id != 0)
                        projects += "<a href='project:" + id.ToString() + "'>" + Trust.GetProjectName(id) + "</a>, ";
                projects = projects.TrimEnd(new char[] { ' ', ',' });

                if (projects != "")
                    tuples.Add(new Tuple<string, string>("Projects: ", projects));

                //Locations:
                List<Tuple<string, string>> locations = new List<Tuple<string, string>>();

                //    Home: Online
                //    Office: Away - At Home
                //    Mobile: Online, Local Time 2:30pm
                //    Server: Last Seen 10/2/2007

                bool online = false;

                 List<ClientInfo> clients = Core.Locations.GetClients(link.UserID);

                foreach (ClientInfo info in clients)
                    {
                        string status = "";

                        if (info.Data.Away)
                            status += "Away " + info.Data.AwayMessage;
                        else
                            status += "Online";

                        if (info.Data.GmtOffset != System.TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Minutes)
                            status += ", Local Time " + Core.TimeNow.ToUniversalTime().AddMinutes(info.Data.GmtOffset).ToString("t");

                        // last seen stuff here

                        online = true;
                        locations.Add(new Tuple<string, string>(Core.Locations.GetLocationName(link.UserID, info.ClientID), status));
                    }


                //crit - get last update from localSync
                /*if (locations.Count == 0)
                {
                    ClientInfo latest = clients[0];

                    foreach (ClientInfo info in clients)
                        if (info.Data.Date > latest.Data.Date)
                            latest = info;

                    locations.Add(new Tuple<string, string>("Last Seen", latest.Data.Date.ToLocalTime().ToString()));
                }*/

                content = GenerateContent(tuples, locations, online);
            }

            // display
            if (CurrentStatusMode != mode)
            {
                CurrentStatusMode = mode;

                StatusHtml.Length = 0;
                StatusHtml.Append(NodePage);

                StatusHtml.Replace("<?=name?>", name);
                StatusHtml.Replace("<?=content?>", content);

                SetStatus(StatusHtml.ToString());
            }
            else
            {
                StatusBrowser.Document.InvokeScript("SetElement", new String[] { "name", name });
                StatusBrowser.Document.InvokeScript("SetElement", new String[] { "content", content  });
            }
        }

        string GenerateContent(List<Tuple<string, string>> tuples, List<Tuple<string, string>> locations, bool online)
        {
            string content = "<table callpadding=3>  ";

            foreach (Tuple<string, string> tuple in tuples)
                content += "<tr><td><p><b>" + tuple.First + "</b></p></td> <td><p>" + tuple.Second + "</p></td></tr>";

            if (locations == null)
                return content + "</table>";

            // locations
            string ifonline = online ? "Locations" : "Offline";

            content += "<tr><td colspan=2><p><b>" + ifonline + "</b><br>";
            foreach (Tuple<string, string> tuple in locations)
                content += "&nbsp&nbsp&nbsp <b>" + tuple.First + ":</b> " + tuple.Second + "<br>";
            content += "</p></td></tr>";

            return content + "</table>";
        }

        private void SetStatus(string html)
        {
            Debug.Assert(!html.Contains("<?"));

            // prevents clicking sound when browser navigates
            StatusBrowser.Hide();
            StatusBrowser.DocumentText = html;
            StatusBrowser.Show();
        }


        private void CommandTree_MouseClick(object sender, MouseEventArgs e)
        {
            // this gets right click to select item
            TreeListNode clicked = CommandTree.GetNodeAt(e.Location) as TreeListNode;

            if (clicked == null)
                return;

            // project menu
            if (clicked == CommandTree.ProjectNode && e.Button == MouseButtons.Right)
            {
                ContextMenuStripEx manage = new ContextMenuStripEx();

                FillManageMenu(manage.Items, CommandTree.Project);

                manage.Show(CommandTree, e.Location);

                return;
            }


            if (clicked.GetType() != typeof(LinkNode))
                return;

            LinkNode item = clicked as LinkNode;



            // right click menu
            if (e.Button != MouseButtons.Right)
                return;

            // menu
            ContextMenuStripEx treeMenu = new ContextMenuStripEx();

            // select
            if (!SideMode)
                treeMenu.Items.Add("Select", InterfaceRes.star, TreeMenu_Select);

            // views
            List<ToolStripMenuItem> quickMenus = new List<ToolStripMenuItem>();
            List<ToolStripMenuItem> extMenus = new List<ToolStripMenuItem>();

            foreach (OpService service in Core.ServiceMap.Values)
            {
                if (service is TrustService)
                    continue;

                // quick
                List<MenuItemInfo> menuList = service.GetMenuInfo(InterfaceMenuType.Quick, item.Link.UserID, CommandTree.Project);

                if (menuList != null && menuList.Count > 0)
                    foreach (MenuItemInfo info in menuList)
                        quickMenus.Add(new OpMenuItem(item.Link.UserID, CommandTree.Project, info.Path, info));

                // external
                menuList = service.GetMenuInfo(InterfaceMenuType.External, item.Link.UserID, CommandTree.Project);

                if (menuList != null && menuList.Count > 0)
                    foreach (MenuItemInfo info in menuList)
                        extMenus.Add(new OpMenuItem(item.Link.UserID, CommandTree.Project, info.Path, info));
            }

            if (quickMenus.Count > 0 || extMenus.Count > 0)
                if (treeMenu.Items.Count > 0)
                    treeMenu.Items.Add("-");

            foreach (ToolStripMenuItem menu in quickMenus)
                treeMenu.Items.Add(menu);

            if (extMenus.Count > 0)
            {
                ToolStripMenuItem viewItem = new ToolStripMenuItem("Views", InterfaceRes.views);

                foreach (ToolStripMenuItem menu in extMenus)
                    viewItem.DropDownItems.Add(menu);

                treeMenu.Items.Add(viewItem);
            }

            // link
            if (CommandTree.TreeMode == CommandTreeMode.Operation)
            {
                List<ToolStripMenuItem> linkMenus = new List<ToolStripMenuItem>();

                List<MenuItemInfo> menuList = Trust.GetMenuInfo(InterfaceMenuType.Quick, item.Link.UserID, CommandTree.Project);

                if (menuList != null && menuList.Count > 0)
                    foreach (MenuItemInfo info in menuList)
                        linkMenus.Add(new OpMenuItem(item.Link.UserID, CommandTree.Project, info.Path, info));

                if (linkMenus.Count > 0)
                {
                    if (treeMenu.Items.Count > 0)
                        treeMenu.Items.Add("-");

                    foreach (ToolStripMenuItem menu in linkMenus)
                        treeMenu.Items.Add(menu);
                }
            }

            // show
            if (treeMenu.Items.Count > 0)
                treeMenu.Show(CommandTree, e.Location);
        }

        private void FillManageMenu(ToolStripItemCollection items, uint project)
        {
            // settings
            ToolStripMenuItem settings = new ToolStripMenuItem("Settings", InterfaceRes.settings);

            settings.DropDownItems.Add(new ManageItem("User", null, delegate() { new RiseOp.Interface.Settings.User(this).ShowDialog(this); }));
            settings.DropDownItems.Add(new ManageItem("Operation", null, delegate() { new RiseOp.Interface.Settings.Operation(this).ShowDialog(this); }));
            settings.DropDownItems.Add(new ManageItem("Connecting", null, delegate() { new RiseOp.Interface.Settings.Connecting(this).ShowDialog(this); }));

            items.Add(settings);

            // invite
            items.Add(new ManageItem("Invite", ChatRes.invite, delegate()
            {
                if (Core.User.Settings.OpAccess == AccessType.Public)
                    MessageBox.Show(this, "Give out this link to invite others \r\n \r\n riseop://" + Core.User.Settings.Operation, "RiseOp");
                else
                {
                    InviteForm form = new InviteForm(Core);
                    form.ShowDialog(this);
                }
            }));

            // tools
            ToolStripMenuItem tools = new ToolStripMenuItem("Tools", InterfaceRes.tools);

            tools.DropDownItems.Add(new ManageItem("Crawler", null, delegate() { CrawlerForm.Show(Core.Network); }));

            // global - crawler/graph/packets/search
            if (Core.Context.Global != null)
            {
                ToolStripMenuItem global = new ToolStripMenuItem("Global", null);

                DhtNetwork globalNetwork = Core.Context.Global.Network;

                global.DropDownItems.Add(new ManageItem("Crawler", null, delegate() { CrawlerForm.Show(globalNetwork); }));
                global.DropDownItems.Add(new ManageItem("Graph", null, delegate() { GraphForm.Show(globalNetwork); }));
                global.DropDownItems.Add(new ManageItem("Packets", null, delegate() { PacketsForm.Show(globalNetwork); }));
                global.DropDownItems.Add(new ManageItem("Search", null, delegate() { SearchForm.Show(globalNetwork); }));

                tools.DropDownItems.Add(global);
            }

            tools.DropDownItems.Add(new ManageItem("Graph", null, delegate()  { GraphForm.Show(Core.Network); }));
            tools.DropDownItems.Add(new ManageItem("Internals", null, delegate() { InternalsForm.Show(Core); }));
            tools.DropDownItems.Add(new ManageItem("Packets", null, delegate() { PacketsForm.Show(Core.Network); }));
            tools.DropDownItems.Add(new ManageItem("Search", null, delegate() { SearchForm.Show(Core.Network); }));        
            tools.DropDownItems.Add(new ManageItem("Transfers", null, delegate() { new TransferView(Core.Transfers).Show(); }));

            items.Add(tools);


            // split
            items.Add(new ToolStripSeparator());

            // proect options
            if (project != 0)
            {
                if (Trust.LocalTrust.Links.ContainsKey(project))
                    items.Add("Leave Project", IMRes.redled, new EventHandler(OnProjectLeave));
                else
                    items.Add("Join Project", IMRes.greenled, new EventHandler(OnProjectJoin));
            }

            // main options
            else
            {
                items.Add(new ManageItem("Sign On", IMRes.greenled, delegate()
                {
                    Core.Context.ShowLogin(null);
                }));

                items.Add(new ManageItem("Sign Off", IMRes.redled, delegate()
                {
                    Core.Context.ShowLogin(null);
                    Close();
                }));
            }
        }


        void TreeMenu_Select(object sender, EventArgs e)
        {

            LinkNode item = GetSelected();

            if (item == null)
                return;

            OnSelectChange(item.Link.UserID, CommandTree.Project);
        }

        private void CommandTree_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            LinkNode item = GetSelected();

            if (item == null)
                return;

            OpMenuItem info = new OpMenuItem(item.Link.UserID, 0);

            if (Core.Locations.LocationMap.SafeContainsKey(info.UserID))
            {
                IMService IM = Core.GetService("IM") as IMService;

                if (IM != null)
                    IM.QuickMenu_View(info, null);
            }
            else
            {
                MailService Mail = Core.GetService("Mail") as MailService;

                if (Mail != null)
                    Mail.QuickMenu_View(info, null);
            }
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd,int wMsg, bool wParam, int lParam);


        void SetRedraw(Control ctl, bool lfDraw)
        {
            int WM_SETREDRAW  = 0x000B;

            SendMessage(ctl.Handle, WM_SETREDRAW, lfDraw, 0);
            if (lfDraw)
            {
                ctl.Invalidate();
                ctl.Refresh();
            }
        }

        void OnSelectChange(ulong id, uint project)
        {
            SuspendLayout();

            OpTrust trust = Trust.GetTrust(id);

            if (trust == null)
            {
                trust = Trust.LocalTrust;
                id = Core.UserID;
            }

            if (!trust.Links.ContainsKey(project))
                project = 0;

            // bold new and set
            SelectedProject = project;

            CommandTree.SelectLink(id, project);


            // setup toolbar with menu items for user
            HomeButton.Visible = (id != Core.UserID);
            ManageButton.Visible = (id == Core.UserID);

            ManageButton.DropDownItems.Clear();
            FillManageMenu(ManageButton.DropDownItems, 0);

            PlanButton.DropDownItems.Clear();
            CommButton.DropDownItems.Clear();
            DataButton.DropDownItems.Clear();

            foreach (OpService service in Core.ServiceMap.Values)
            {
                List<MenuItemInfo> menuList = service.GetMenuInfo(InterfaceMenuType.Internal, id, project);

                if (menuList == null || menuList.Count == 0)
                    continue;

                foreach (MenuItemInfo info in menuList)
                {
                    string[] parts = info.Path.Split(new char[] { '/' });

                    if (parts.Length < 2)
                        continue;
                    
                    if (parts[0] == PlanButton.Text)
                        PlanButton.DropDownItems.Add(new OpStripItem(id, project, false, parts[1], info));

                    else if (parts[0] == CommButton.Text)
                        CommButton.DropDownItems.Add(new OpStripItem(id, project, false, parts[1], info));

                    else if (parts[0] == DataButton.Text)
                        DataButton.DropDownItems.Add(new OpStripItem(id, project, false, parts[1], info));
                }
            }

            // setup nav bar - add components
            UpdateNavBar();


            // find previous component in drop down, activate click on it
            string previous = InternalView != null? InternalView.GetTitle(true) : "Profile";

            if (!SelectService(previous))
                SelectService("Profile");

            ResumeLayout();
        }

        private bool SelectService(string service)
        {
            foreach (ToolStripMenuItem item in ComponentNavButton.DropDownItems)
                if (item.Text == service)
                {
                    item.PerformClick();
                    return true;
                }

            return false;
        }


        void Core_GetFocused()
        {
            // return links in nav bar
            foreach (ToolStripItem item in PersonNavButton.DropDownItems)
            {
                PersonNavItem person = item as PersonNavItem;

                if (person != null)
                    Core.Focused.SafeAdd(person.UserID, true);
            }
        }


        private void UpdateNavBar()
        {
            PersonNavButton.DropDownItems.Clear();
            ProjectNavButton.DropDownItems.Clear();
            ComponentNavButton.DropDownItems.Clear();

            OpLink link = Trust.GetLink(CommandTree.SelectedLink, SelectedProject);

            if (link != null)
            {
                if (link.UserID == Core.UserID)
                    PersonNavButton.Text = "My";
                else
                    PersonNavButton.Text = Trust.GetName(link.UserID) + "'s";

                PersonNavItem self = null;
                
                // add higher and subs of higher
                OpLink higher = link.GetHigher(false);
                if (higher != null)
                {
                    PersonNavButton.DropDownItems.Add(new PersonNavItem(Trust.GetName(higher.UserID), higher.UserID, this, PersonNav_Clicked));

                    List<ulong> adjacentIDs = Trust.GetDownlinkIDs(higher.UserID, SelectedProject, 1);
                    foreach (ulong id in adjacentIDs)
                    {
                        PersonNavItem item = new PersonNavItem("   " + Trust.GetName(id), id, this, PersonNav_Clicked);
                        if (id == CommandTree.SelectedLink)
                        {
                            item.Font = BoldFont;
                            self = item;
                        }

                        PersonNavButton.DropDownItems.Add(item);
                    }
                }

                string childspacing = (self == null) ? "   " : "      ";

                // if self not added yet, add
                if (self == null)
                {
                    PersonNavItem item = new PersonNavItem(Trust.GetName(link.UserID), link.UserID, this, PersonNav_Clicked);
                    item.Font = BoldFont;
                    self = item;
                    PersonNavButton.DropDownItems.Add(item);
                }

                // add downlinks of self
                List<ulong> downlinkIDs = Trust.GetDownlinkIDs(CommandTree.SelectedLink, SelectedProject, 1);
                foreach (ulong id in downlinkIDs)
                {
                    PersonNavItem item = new PersonNavItem(childspacing + Trust.GetName(id), id, this, PersonNav_Clicked);

                    int index = PersonNavButton.DropDownItems.IndexOf(self);
                    PersonNavButton.DropDownItems.Insert(index+1, item);
                }
            }
            else
            {
                PersonNavButton.Text = "Unknown";
            }

            PersonNavButton.DropDownItems.Add("-");
            PersonNavButton.DropDownItems.Add("Browse...", null, new EventHandler(PersonNavBrowse_Clicked));


            // set person's projects
            ProjectNavButton.Text = Trust.GetProjectName(SelectedProject);

            if (link != null)
                foreach (uint project in link.Trust.Links.Keys)
                {
                    string name = Trust.GetProjectName(project);

                    string spacing = (project == 0) ? "" : "   ";

                    ProjectNavButton.DropDownItems.Add(new ProjectNavItem(spacing + name, project, ProjectNav_Clicked));
                }


            // set person's components
            if (InternalView != null)
                ComponentNavButton.Text = InternalView.GetTitle(true);

            foreach (OpService service in Core.ServiceMap.Values)
            {
                List<MenuItemInfo> menuList = service.GetMenuInfo(InterfaceMenuType.Internal, CommandTree.SelectedLink, SelectedProject);

                if (menuList == null || menuList.Count == 0)
                    continue;

                foreach (MenuItemInfo info in menuList)
                    ComponentNavButton.DropDownItems.Add(new ServiceNavItem(info, CommandTree.SelectedLink, SelectedProject, info.ClickEvent));
            }
            
        }

        private void PersonNav_Clicked(object sender, EventArgs e)
        {
            PersonNavItem item = sender as PersonNavItem;

            if (item == null)
                return;

            OnSelectChange(item.UserID, SelectedProject);
        }

        private void PersonNavBrowse_Clicked(object sender, EventArgs e)
        {
            AddLinks add = new AddLinks(Core.Trust, SelectedProject);
            add.Text = "Select Person";
            add.AddButton.Text = "Select";
            add.MultiSelect = false;

            if (add.ShowDialog(this) == DialogResult.OK)
                OnSelectChange(add.Person, add.ProjectID);
        }

        private void ProjectNav_Clicked(object sender, EventArgs e)
        {
            ProjectNavItem item = sender as ProjectNavItem;

            if (item == null)
                return;

            OnSelectChange(CommandTree.SelectedLink, item.ProjectID);
        }
        
        private void OperationButton_CheckedChanged(object sender, EventArgs e)
        {
            // if checked, uncheck other and display
            if (OperationButton.Checked)
            {
                OnlineButton.Checked = false;

                if (ProjectButton != null)
                    ProjectButton.Checked = false;

                MainSplit.Panel1Collapsed = false;

                CommandTree.ShowProject(0);
            }

            // if not check, check if online checked, if not hide
            else
            {
                if (!OnlineButton.Checked)
                    if (ProjectButton == null || !ProjectButton.Checked)
                    {
                        if (SideMode)
                            OperationButton.Checked = true;
                        else
                            MainSplit.Panel1Collapsed = true;
                    }
            }
        }

        private void OnlineButton_CheckedChanged(object sender, EventArgs e)
        {
            // if checked, uncheck other and display
            if (OnlineButton.Checked)
            {
                OperationButton.Checked = false;

                if (ProjectButton != null)
                    ProjectButton.Checked = false;

                MainSplit.Panel1Collapsed = false;

                CommandTree.ShowOnline();
            }

            // if not check, check if online checked, if not hide
            else
            {
                if (!OperationButton.Checked)
                    if (ProjectButton == null || !ProjectButton.Checked)
                    {
                        if (SideMode)
                            OnlineButton.Checked = true;
                        else
                            MainSplit.Panel1Collapsed = true;
                    }
            }
        }

        private void HomeButton_Click(object sender, EventArgs e)
        {
            OnSelectChange(Core.UserID, SelectedProject);
        }

        private void ProjectsButton_DropDownOpening(object sender, EventArgs e)
        {
            ProjectsButton.DropDownItems.Clear();

            ProjectsButton.DropDownItems.Add(new ToolStripMenuItem("New...", null, new EventHandler(ProjectMenu_New)));

            Trust.ProjectNames.LockReading(delegate()
            {
                foreach (uint id in Trust.ProjectNames.Keys)
                    if (id != 0 && Trust.ProjectRoots.SafeContainsKey(id))
                        ProjectsButton.DropDownItems.Add(new ProjectItem(Trust.ProjectNames[id], id, new EventHandler(ProjectMenu_Click)));
            });

            if (ProjectButton != null && Trust.LocalTrust.InProject(CommandTree.Project))
            {
                ProjectsButton.DropDownItems.Add(new ToolStripSeparator());
                ProjectsButton.DropDownItems.Add(new ToolStripMenuItem("Leave " + ProjectButton.Text, null, new EventHandler(OnProjectLeave)));
            }
        }

        private void ProjectMenu_New(object sender, EventArgs e)
        {
            NewProjectForm form = new NewProjectForm(Core);

            if (form.ShowDialog(this) == DialogResult.OK)
            {
                ProjectItem item = new ProjectItem("", form.ProjectID, null);
                ProjectMenu_Click(item, e);
            }
        }

        private void ProjectMenu_Click(object sender, EventArgs e)
        {
            ProjectItem item = sender as ProjectItem;

            if (item == null)
                return;

            UpdateProjectButton(item.ProjectID);
        }

        private void UpdateProjectButton(uint id)
        {
            ProjectButtonID = id;

            // destroy any current project button
            if (ProjectButton != null)
                SideToolStrip.Items.Remove(ProjectButton);

            // create button for project
            ProjectButton = new ToolStripButton(Trust.GetProjectName(ProjectButtonID), null, new EventHandler(ShowProject));
            ProjectButton.TextDirection = ToolStripTextDirection.Vertical90;
            ProjectButton.CheckOnClick = true;
            ProjectButton.Checked = true;
            SideToolStrip.Items.Add(ProjectButton);

            // click button
            ShowProject(ProjectButton, null);
        }


        private void ShowProject(object sender, EventArgs e)
        {
            ToolStripButton button = sender as ToolStripButton;

            if (sender == null)
                return;

            // check project exists
            if (!Trust.ProjectRoots.SafeContainsKey(ProjectButtonID))
            {
                OperationButton.Checked = true;
                SideToolStrip.Items.Remove(ProjectButton);
                ProjectButton = null;
            }

            // if checked, uncheck other and display
            if (button.Checked)
            {
                OperationButton.Checked = false;
                OnlineButton.Checked = false;
                MainSplit.Panel1Collapsed = false;

                CommandTree.ShowProject(ProjectButtonID);
            }

            // if not check, check if online checked, if not hide
            else
            {
                if (!OperationButton.Checked && !OnlineButton.Checked)
                {
                    if (SideMode)
                        ProjectButton.Checked = true;
                    else
                        MainSplit.Panel1Collapsed = true;
                }
            }
        }


        internal bool SideMode;
        int Panel2Width;

        private void SideButton_CheckedChanged(object sender, EventArgs e)
        {
            if (SideButton.Checked)
            {
                Panel2Width = MainSplit.Panel2.Width;

                MainSplit.Panel1Collapsed = false;
                MainSplit.Panel2Collapsed = true;

                Width -= Panel2Width;
                Left += Panel2Width;

                SideNavStrip.Visible = true;
                CommandTree.Top = 23;
                CommandTree.Height = CommandSplit.Panel1.Height - 23;

                SideMode = true;

                OnSelectChange(Core.UserID, 0);
            }

            else
            {
                Left -= Panel2Width;

                Width += Panel2Width;

                MainSplit.Panel2Collapsed = false;

                SideNavStrip.Visible = false;

                CommandTree.Top = 0;
                CommandTree.Height = CommandSplit.Panel1.Height;


                SideMode = false;
            }
        }

        private void OnProjectLeave(object sender, EventArgs e)
        {
            if (CommandTree.Project != 0)
                Trust.LeaveProject(CommandTree.Project);

            // if no roots, remove button change projectid to 0
            if (!Trust.ProjectRoots.SafeContainsKey(CommandTree.Project))
            {
                SideToolStrip.Items.Remove(ProjectButton);
                ProjectButton = null;
                OperationButton.Checked = true;
            }
        }

        private void OnProjectJoin(object sender, EventArgs e)
        {
            if (CommandTree.Project != 0)
                Trust.JoinProject(CommandTree.Project);
        }

        private void StatusBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            string url = e.Url.OriginalString;

            string[] parts = url.Split(new char[] {':'});

            if(parts.Length < 2)
                return;

            if (parts[0] == "edit")
            {
                EditMenu_Click(null, null);
                e.Cancel = true;
                return;
            }

            if (parts[0] == "project")
            {
                UpdateProjectButton(uint.Parse(parts[1]));

                e.Cancel = true;
            }
        }

        private void RightClickMenu_Opening(object sender, CancelEventArgs e)
        {
            LinkNode item = GetSelected();

            if (item == null)
            {
                e.Cancel = true;
                return;
            }

            if (item.Link.UserID != Core.UserID)
            {
                e.Cancel = true;
                return;
            }
        }

        private void EditMenu_Click(object sender, EventArgs e)
        {
            EditLink edit = new EditLink(Core, CommandTree.Project);
            edit.ShowDialog(this);

            UpdateCommandPanel();
        }

        private void CommandTree_SelectedItemChanged(object sender, EventArgs e)
        {
            UpdateCommandPanel();
        }

        private void PopoutButton_Click(object sender, EventArgs e)
        {
            if(InternalView == null) // no controls loaded
                return;

            SuspendLayout();
            InternalPanel.Controls.Clear();

            InternalView.BlockReinit = true;
            OnShowExternal(InternalView);
            InternalView = null;

            OnSelectChange(CommandTree.SelectedLink, SelectedProject);
            ResumeLayout();
        }

        #region News

        private void NewsTimer_Tick(object sender, EventArgs e)
        {
            if (NewsPending.Count == 0)
                return;

            // sequence = 1/10s
            Color color = NewsPending.Peek().DisplayColor;
            int alpha = NewsBrush.Color.A;

            // 1/4s fade in
            if (NewsSequence < 3)
                alpha += 255 / 4;

            // 2s show
            else if (NewsSequence < 23)
                alpha = 255;

            // 1/4s fad out
            else if (NewsSequence < 26)
                alpha -= 255 / 4;

            // 1/2s hide
            else if (NewsSequence < 31)
            {
                alpha = 0;
            }
            else
            {
                NewsSequence = 0;
                NewsRecent.Enqueue(NewsPending.Dequeue());

                while (NewsRecent.Count > 15)
                    NewsRecent.Dequeue();
            }

            
            if (NewsBrush.Color.A != alpha || NewsBrush.Color != color)
                NewsBrush = new SolidBrush(Color.FromArgb(alpha, color));

            NewsSequence++;

            if (SideMode)
                SideNavStrip.Invalidate();
            else
                NavStrip.Invalidate();
        }

        void Core_NewsUpdate(NewsItemInfo info)
        {
            NewsItem item = new NewsItem(info, Core.UserID); // pop out external view if in messenger mode
            item.Text = Core.TimeNow.ToString("h:mm ") + info.Message;

            // set color
            if (Trust.IsLowerDirect(info.UserID, info.ProjectID))
            {
                item.DisplayColor = Color.LightBlue;
                item.ForeColor = Color.Blue;
            }
            else if (Trust.IsHigher(info.UserID, info.ProjectID))
            {
                item.DisplayColor = Color.FromArgb(255, 198, 198);
                item.ForeColor = Color.Red;
            }
            else
                item.DisplayColor = Color.White;


            Queue<NewsItem> queue = NewsHideUpdates ? NewsRecent : NewsPending;

            queue.Enqueue(item);

            while (queue.Count > 15) // prevent flooding
                queue.Dequeue();

            if (NewsHideUpdates)
                ActiveNewsButton().Image = InterfaceRes.news_hot;
        }

        private ToolStripDropDownButton ActiveNewsButton()
        {
            return SideMode ? SideNewsButton : NewsButton;
        }

        private void NewsButton_DropDownOpening(object sender, EventArgs e)
        {
            LoadNewsButton(NewsButton, false);
        }

        private void SideNewsButton_DropDownOpening(object sender, EventArgs e)
        {
            LoadNewsButton(SideNewsButton, true);
        }

        private void LoadNewsButton(ToolStripDropDownButton button, bool external)
        {
            button.DropDown.Items.Clear();

            foreach (NewsItem item in NewsPending)
            {
                item.External = external;
                button.DropDown.Items.Add(item);
            }

            if (NewsPending.Count > 0 && NewsRecent.Count > 0)
                button.DropDown.Items.Add("-");

            foreach (NewsItem item in NewsRecent)
            {
                item.External = external;
                button.DropDown.Items.Add(item);
            }

            if (NewsRecent.Count > 0)
                button.DropDown.Items.Add("-");

            ToolStripMenuItem hide = new ToolStripMenuItem("Hide News Updates", null, new EventHandler(NewsButton_HideUpdates));
            hide.Checked = NewsHideUpdates;
            button.DropDown.Items.Add(hide);

            button.Image = InterfaceRes.news;
        }

        private void NewsButton_HideUpdates(object sender, EventArgs e)
        {
            NewsHideUpdates = !NewsHideUpdates;

            if (NewsHideUpdates && NewsPending.Count > 0)
                while (NewsPending.Count != 0)
                    NewsRecent.Enqueue(NewsPending.Dequeue());

            if (SideMode)
                SideNavStrip.Invalidate();
            else
                NavStrip.Invalidate();
        }

        private void NavStrip_Paint(object sender, PaintEventArgs e)
        {
            PaintNewsStrip(e, ComponentNavButton, NewsButton);   
        }

        private void SideModeStrip_Paint(object sender, PaintEventArgs e)
        {
            PaintNewsStrip(e, SideViewsButton, SideNewsButton);
        }

        void PaintNewsStrip(PaintEventArgs e, ToolStripItem leftButton, ToolStripItem rightButton)
        {
            NewsArea = new Rectangle();

            if (NewsPending.Count == 0)
                return;

            // get bounds where we can put news text
            int x = leftButton.Bounds.X + leftButton.Bounds.Width + 4;
            int width = rightButton.Bounds.X - 4 - x;

            if (width < 0)
            {
                ActiveNewsButton().Image = InterfaceRes.news_hot;
                return;
            }

            // determine size of text
            int reqWidth = (int)e.Graphics.MeasureString(NewsPending.Peek().Info.Message, BoldFont).Width;

            if (width < reqWidth)
            {
                ActiveNewsButton().Image = InterfaceRes.news_hot;
                return;
            }

            // draw text
            x = x + width / 2 - reqWidth / 2;
            e.Graphics.DrawString(NewsPending.Peek().Info.Message, BoldFont, NewsBrush, x, 5);

            NewsArea = new Rectangle(x, 5, reqWidth, 9);
        }

        private void NavStrip_MouseMove(object sender, MouseEventArgs e)
        {
            NewsMouseMove(e);

        }

        private void SideModeStrip_MouseMove(object sender, MouseEventArgs e)
        {
            NewsMouseMove(e);
        }

        private void NewsMouseMove(MouseEventArgs e)
        {
            if (NewsArea.Contains(e.Location) && NewsPending.Count > 0 && NewsPending.Peek().Info.ClickEvent != null)
                Cursor.Current = Cursors.Hand;
            else
                Cursor.Current = Cursors.Arrow;
        }

        private void NavStrip_MouseClick(object sender, MouseEventArgs e)
        {
            NewsMouseClick(e, false);
           
        }

        private void SideModeStrip_MouseClick(object sender, MouseEventArgs e)
        {
            NewsMouseClick(e, true);
        }

        private void NewsMouseClick(MouseEventArgs e, bool external)
        {
            if (NewsArea.Contains(e.Location) && NewsPending.Peek() != null)
            {
                NewsPending.Peek().External = external; // in window
                NewsPending.Peek().Info.ClickEvent.Invoke(NewsPending.Peek(), null);
            }
        }

#endregion

        private void SideViewsButton_DropDownOpening(object sender, EventArgs e)
        {
            SideViewsButton.DropDownItems.Clear();

            // Manage
            ToolStripMenuItem manage = new ToolStripMenuItem("Manage", InterfaceRes.manage);
            FillManageMenu(manage.DropDownItems, 0);
            SideViewsButton.DropDownItems.Add(manage);
            SideViewsButton.DropDownItems.Add(new ToolStripSeparator());

            // add command views
            AddSideViewMenus(SideViewsButton.DropDownItems, 0);

            // add project views
            if(Trust.LocalTrust.Links.Count > 1)
                SideViewsButton.DropDownItems.Add(new ToolStripSeparator());

            foreach(uint id in Trust.LocalTrust.Links.Keys )
                if(id != 0 && Trust.ProjectNames.SafeContainsKey(id))
                {
                    ToolStripMenuItem projectItem = new ToolStripMenuItem(Trust.GetProjectName(id));
                    AddSideViewMenus(projectItem.DropDownItems, id);
                    SideViewsButton.DropDownItems.Add(projectItem);
                }

        }

        private void AddSideViewMenus(ToolStripItemCollection collection, uint project)
        {
            ToolStripMenuItem plansItem = new ToolStripMenuItem("Plans", InterfaceRes.plans);
            ToolStripMenuItem commItem = new ToolStripMenuItem("Comm", InterfaceRes.comm);
            ToolStripMenuItem dataItem = new ToolStripMenuItem("Data", InterfaceRes.data);


            foreach (OpService service in Core.ServiceMap.Values)
            {
                List<MenuItemInfo> menuList = service.GetMenuInfo(InterfaceMenuType.Internal, Core.UserID, project);

                if (menuList == null || menuList.Count == 0)
                    continue;

                foreach (MenuItemInfo info in menuList)
                {
                    string[] parts = info.Path.Split(new char[] { '/' });

                    if (parts.Length < 2)
                        continue;

                    if (parts[0] == PlanButton.Text)
                        plansItem.DropDownItems.Add(new OpStripItem(Core.UserID, project, true, parts[1], info));

                    else if (parts[0] == CommButton.Text)
                        commItem.DropDownItems.Add(new OpStripItem(Core.UserID, project, true, parts[1], info));

                    else if (parts[0] == DataButton.Text)
                        dataItem.DropDownItems.Add(new OpStripItem(Core.UserID, project, true, parts[1], info));
                }
            }


            collection.Add(plansItem);
            collection.Add(commItem);
            collection.Add(dataItem);
        }

        private void SideSearchButton_Click(object sender, EventArgs e)
        {

        }

    }

    class NewsItem : ToolStripMenuItem, IViewParams
    {
        internal NewsItemInfo Info;
        internal Color DisplayColor;
        internal bool External;
        ulong LocalID;

        internal NewsItem(NewsItemInfo info, ulong localid)
            : base(info.Message, info.Symbol != null ? info.Symbol.ToBitmap() : null, info.ClickEvent)
        {
            Info = info;
            LocalID = localid;
        }

        public ulong GetKey()
        {
            return Info.ShowRemote ? Info.UserID : LocalID;
        }

        public uint GetProject()
        {
            return Info.ProjectID;
        }

        public bool IsExternal()
        {
            return External;
        }
    }

    class OpStripItem : ToolStripMenuItem, IViewParams
    {
        internal ulong UserID;
        internal uint ProjectID;
        internal MenuItemInfo Info;
        bool External;

        internal OpStripItem(ulong key, uint id, bool external, string text, MenuItemInfo info)
            : base(text, null, info.ClickEvent )
        {
            UserID = key;
            ProjectID = id;
            Info = info;
            External = external;

            Image = Info.Symbol.ToBitmap();
        }

        public ulong GetKey()
        {
            return UserID;
        }

        public uint GetProject()
        {
            return ProjectID;
        }

        public bool IsExternal()
        {
            return External;
        }
    }

    class ProjectItem : ToolStripMenuItem
    {
        internal uint ProjectID;

        internal ProjectItem(string text, uint id, EventHandler onClick)
            : base(text, null, onClick)
        {
            ProjectID = id;
        }
    }

    class OpMenuItem : ToolStripMenuItem, IViewParams
    {
        internal ulong UserID;
        internal uint ProjectID;
        internal MenuItemInfo Info;

        internal OpMenuItem(ulong key, uint id)
        {
            UserID = key;
            ProjectID = id;
        }

        internal OpMenuItem(ulong key, uint id, string text, MenuItemInfo info)
            : base(text, null, info.ClickEvent)
        {
            UserID = key;
            ProjectID = id;
            Info = info;

            if(info.Symbol != null)
                Image = info.Symbol.ToBitmap();
        }

        public ulong GetKey()
        {
            return UserID;
        }

        public uint GetProject()
        {
            return ProjectID;
        }

        public bool IsExternal()
        {
            return true;
        }
    }


    class PersonNavItem : ToolStripMenuItem
    {
        internal ulong UserID;

        internal PersonNavItem(string name, ulong id, MainForm form, EventHandler onClick)
            : base(name, null, onClick)
        {
            UserID = id;

            Font = new System.Drawing.Font("Tahoma", 8.25F);

            if (UserID == form.Core.UserID)
                Image = InterfaceRes.star;
        }
    }

    class ProjectNavItem : ToolStripMenuItem
    {
        internal uint ProjectID;

        internal ProjectNavItem(string name, uint project, EventHandler onClick)
            : base(name, null, onClick)
        {
            ProjectID = project;

            Font = new System.Drawing.Font("Tahoma", 8.25F);
        }
    }

    class ManageItem : ToolStripMenuItem
    {
        MethodInvoker Code;


        internal ManageItem(string text, Image image, MethodInvoker code)
            : base(text, image)
        {
            Code = code;

            this.Click += new EventHandler(ManageItem_OnClick);
        }

        private void ManageItem_OnClick(object sender, EventArgs e)
        {
            Code.Invoke();
        }


    }

    class ServiceNavItem : ToolStripMenuItem, IViewParams
    {
        ulong UserID;
        uint ProjectID;

        internal ServiceNavItem(MenuItemInfo info, ulong id, uint project, EventHandler onClick)
            : base("", null, onClick)
        {

            UserID = id;
            ProjectID = project;

            string[] parts = info.Path.Split(new char[] { '/' });

            if (parts.Length == 2)
                Text = parts[1];

            Font = new System.Drawing.Font("Tahoma", 8.25F);

            if (info.Symbol != null)
                Image = info.Symbol.ToBitmap();
        }

        public ulong GetKey()
        {
            return UserID;
        }

        public uint GetProject()
        {
            return ProjectID;
        }

        public bool IsExternal()
        {
            return false;
        }
    }
}
