using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Implementation.Dht;

using DeOps.Services;
using DeOps.Services.Buddy;
using DeOps.Services.Chat;
using DeOps.Services.IM;
using DeOps.Services.Location;
using DeOps.Services.Mail;
using DeOps.Services.Transfer;
using DeOps.Services.Trust;

using DeOps.Interface.Info;
using DeOps.Interface.Setup;
using DeOps.Interface.Tools;
using DeOps.Interface.TLVex;
using DeOps.Interface.Views;
using DeOps.Services.Board;
using DeOps.Services.Plan;
using DeOps.Services.Profile;
using DeOps.Services.Share;
using DeOps.Services.Storage;


namespace DeOps.Interface
{
    public partial class MainForm : HostsExternalViews
    {
        public CoreUI UI;
        public OpCore Core;
        public TrustService Trust;

        public uint SelectedProject;

        ToolStripButton ProjectButton;
        uint ProjectButtonID;

        public ViewShell InternalView;

        Font BoldFont = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

        // news
        int NewsSequence;
        Queue<NewsItem> NewsPending = new Queue<NewsItem>();
        Queue<NewsItem> NewsRecent = new Queue<NewsItem>();
        SolidBrush NewsBrush = new SolidBrush(Color.FromArgb(0, Color.White));
        Rectangle NewsArea;
        bool NewsHideUpdates;

        
        public MainForm(CoreUI ui, bool sideMode) : base(ui.Core)
        {
            InitializeComponent();

            UI = ui;
            Core = ui.Core;
            Trust = Core.Trust;

            UI.ShowView += ShowView;

            Core.NewsUpdate += new NewsUpdateHandler(Core_NewsUpdate);
            Core.KeepDataGui += new KeepDataHandler(Core_KeepData);
            Trust.GuiUpdate  += new LinkGuiUpdateHandler(Trust_Update);
            
            CommandTree.SelectedLink = Core.UserID;
            CommandTree.SearchOnline = true;

            GuiUtils.SetupToolstrip(TopToolStrip, new OpusColorTable());
            GuiUtils.SetupToolstrip(NavStrip, new NavColorTable());
            GuiUtils.SetupToolstrip(SideToolStrip, new OpusColorTable());
            GuiUtils.SetupToolstrip(SideNavStrip, new NavColorTable());

            GuiUtils.FixMonoDropDownOpening(SideViewsButton, SideViewsButton_DropDownOpening);
            GuiUtils.FixMonoDropDownOpening(SideNewsButton, SideNewsButton_DropDownOpening);
            GuiUtils.FixMonoDropDownOpening(NewsButton, NewsButton_DropDownOpening);
            GuiUtils.FixMonoDropDownOpening(ProjectsButton, ProjectsButton_DropDownOpening);

            if (GuiUtils.IsRunningOnMono())
            {
                foreach (ToolStripItem item in SideToolStrip.Items)
                {
                    //item.DisplayStyle = ToolStripItemDisplayStyle.Text;
                    //item.TextDirection = ToolStripTextDirection.Horizontal;
                    //item.Alignment = ToolStripItemAlignment.Left;
                }

                NetworkButton.Text = "Network";
                SideButton.Text = "Sidebar";
                LockButton.Text = "Lockdown";

                NewsButton.ForeColor = Color.White;
                PopoutButton.ForeColor = Color.White;

                SideViewsButton.ForeColor = Color.White;
                SideNewsButton.ForeColor = Color.White;
                SideHelpButton.ForeColor = Color.White;
            }
            
            SideNavStrip.Visible = sideMode;
            SideMode = sideMode;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            UpdateTitle();

            CommandTree.Init(Trust);
            CommandTree.ShowProject(0);

            BuddyList.Init(UI, Core.Buddies, SelectionInfo, true);

            SelectionInfo.Init(UI);

            OnSelectChange(Core.UserID, CommandTree.Project);
            UpdateStatusPanel();

            CommandTree.Top = 0;
            BuddyList.Top = 0;

            // reize trees to ensure they take up the whole space
            CommandTree.Size = new Size(CommandSplit.Panel1.Width, CommandSplit.Panel1.Height);
            BuddyList.Size = CommandTree.Size;

            // resize inner panel as well
            InternalPanel.Location = new Point(0, NavStrip.Bottom);
            InternalPanel.Height = MainSplit.Panel2.Height - NavStrip.Bottom;

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

            while (ExternalViews.Count > 0)
                // safe close removes entry from external views
                if (!ExternalViews[0].SafeClose())
                {
                    e.Cancel = true;
                    return;
                }

            if (!CleanInternal())
            {
                e.Cancel = true;
                return;
            }

            UI.ShowView -= ShowView;

            Core.NewsUpdate -= new NewsUpdateHandler(Core_NewsUpdate);
            Core.KeepDataGui -= new KeepDataHandler(Core_KeepData);
            Trust.GuiUpdate -= new LinkGuiUpdateHandler(Trust_Update);

            UI.GuiMain = null;

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

            UI.GuiTray = new TrayLock(UI, SideMode);
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

        public void ShowView(ViewShell view, bool external)
        {
            if (external)
                ShowExternal(view);
            else
                ShowInternal(view);
        }

        void ShowExternal(ViewShell view)
        {
            ExternalView external = new ExternalView(this, ExternalViews, view);

            ExternalViews.Add(external);

            if(InternalView == null || !InternalView.BlockReinit)
                view.Init();

            external.Show();
        }

        void ShowInternal(ViewShell view)
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

            if (ProjectButton != null)
            {
                ProjectButton.Text = Core.Trust.GetProjectName(ProjectButtonID);

                if (!Trust.ProjectRoots.SafeContainsKey(CommandTree.Project))
                {
                    if (ProjectButton.Checked)
                        OperationButton.Checked = true;

                    SideToolStrip.Items.Remove(ProjectButton);
                    ProjectButton = null;
                }
            }

            UpdateNavBar();
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


        void UpdateStatusPanel()
        {
            if (CommandTree.SelectedNodes.Count == 0)
            {
                SelectionInfo.ShowNetwork();
                return;
            }

            TreeListNode node = CommandTree.SelectedNodes[0];

            if (node.GetType() == typeof(LinkNode))
            {
                OpLink link = ((LinkNode)node).Link;
                SelectionInfo.ShowUser(link.UserID, link.Project);
            }

            else if (node.GetType() == typeof(ProjectNode))
            {
                ProjectNode project = node as ProjectNode;
                SelectionInfo.ShowProject(project.ID);
            }

            else
                SelectionInfo.ShowNetwork();
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
                ContextMenuStripEx menu = new ContextMenuStripEx();

                if (CommandTree.Project != 0)
                {
                    if (Trust.LocalTrust.Links.ContainsKey(CommandTree.Project))
                        menu.Items.Add("Leave Project", InterfaceRes.project_remove, new EventHandler(OnProjectLeave));
                    else
                        menu.Items.Add("Join Project", InterfaceRes.project_add, new EventHandler(OnProjectJoin));
                }

                // FillManageMenu(manage.Items, CommandTree.Project);

                if (menu.Items.Count > 0)
                    menu.Show(CommandTree, e.Location);

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
            List<MenuItemInfo> quickMenus = new List<MenuItemInfo>();
            List<MenuItemInfo> extMenus = new List<MenuItemInfo>();

            foreach (IServiceUI service in UI.Services.Values)
            {
                if (service is TrustUI || service is BuddyUI)
                    continue;

                service.GetMenuInfo(InterfaceMenuType.Quick, quickMenus, item.Link.UserID, CommandTree.Project);

                service.GetMenuInfo(InterfaceMenuType.External, extMenus, item.Link.UserID, CommandTree.Project);
            }

            if (quickMenus.Count > 0 || extMenus.Count > 0)
                if (treeMenu.Items.Count > 0)
                    treeMenu.Items.Add("-");

            foreach (MenuItemInfo info in quickMenus)
                treeMenu.Items.Add(new OpMenuItem(item.Link.UserID, CommandTree.Project, info.Path, info));

            if (extMenus.Count > 0)
            {
                ToolStripMenuItem viewItem = new ToolStripMenuItem("Views", InterfaceRes.views);

                foreach (MenuItemInfo info in extMenus)
                    viewItem.DropDownItems.SortedAdd(new OpMenuItem(item.Link.UserID, CommandTree.Project, info.Path, info));

                treeMenu.Items.Add(viewItem);
            }

            // add trust/buddy menu at bottom under separator
            quickMenus.Clear();

            UI.Services[ServiceIDs.Trust].GetMenuInfo(InterfaceMenuType.Quick, quickMenus, item.Link.UserID, CommandTree.Project);
            UI.Services[ServiceIDs.Buddy].GetMenuInfo(InterfaceMenuType.Quick, quickMenus, item.Link.UserID, CommandTree.Project);

            if (quickMenus.Count > 0)
            {
                if (treeMenu.Items.Count > 0)
                    treeMenu.Items.Add("-");

                foreach (MenuItemInfo info in quickMenus)
                    treeMenu.Items.Add(new OpMenuItem(item.Link.UserID, CommandTree.Project, info.Path, info));
            }

            // show
            if (treeMenu.Items.Count > 0)
                treeMenu.Show(CommandTree, e.Location);
        }

        public static void FillManageMenu(CoreUI ui, ToolStripItemCollection items)
        {
            OpCore core = ui.Core;

            items.Add(new ManageItem("My Identity", BuddyRes.buddy_who, () => new IdentityForm(core, core.UserID).Show(ui.GuiMain)));

            // invite
            if(!core.User.Settings.GlobalIM)
                items.Add(new ManageItem("Invite", ChatRes.invite, delegate()
                {
                    if (core.User.Settings.OpAccess == AccessType.Public)
                        MessageBox.Show("Give out this link to invite others \r\n \r\n deops://" + core.User.Settings.Operation, "DeOps");
                    else
                        new InviteForm(core).Show(ui.GuiMain);
                }));

            // settings
            ToolStripMenuItem settings = new ToolStripMenuItem("Settings", InterfaceRes.settings);

            settings.DropDownItems.Add(new ManageItem("User", null, () => new DeOps.Interface.Settings.User(core).Show(ui.GuiMain)));
            
            if(!core.User.Settings.GlobalIM)
                settings.DropDownItems.Add(new ManageItem("Organization", null, () => new DeOps.Interface.Settings.Operation(core).Show(ui.GuiMain)));

            settings.DropDownItems.Add(new ManageItem("Connection", null, () => new DeOps.Interface.Settings.Connection(core).Show(ui.GuiMain)));
            settings.DropDownItems.Add(new ManageItem("Ignore", null, () => new DeOps.Interface.Settings.IgnoreForm(ui).Show(ui.GuiMain)));

            items.Add(settings);

            // tools
            ToolStripMenuItem tools = new ToolStripMenuItem("Tools", InterfaceRes.tools);

            tools.DropDownItems.Add(new ManageItem("Bandwidth", null, () => BandwidthForm.Show(core.Context)));

            if (core.DebugWindowsActive)
            {
                tools.DropDownItems.Add(new ManageItem("Crawler", null, () => CrawlerForm.Show(core.Network)));

                // global - crawler/graph/packets/search
                if (core.Context.Lookup != null)
                {
                    ToolStripMenuItem global = new ToolStripMenuItem("Lookup", null);

                    DhtNetwork globalNetwork = core.Context.Lookup.Network;

                    global.DropDownItems.Add(new ManageItem("Crawler", null, () => CrawlerForm.Show(globalNetwork)));
                    global.DropDownItems.Add(new ManageItem("Graph", null, () => GraphForm.Show(globalNetwork)));
                    global.DropDownItems.Add(new ManageItem("Packets", null, () => PacketsForm.Show(globalNetwork)));
                    global.DropDownItems.Add(new ManageItem("Search", null, () => SearchForm.Show(globalNetwork)));

                    tools.DropDownItems.Add(global);
                }

                tools.DropDownItems.Add(new ManageItem("Graph", null, () => GraphForm.Show(core.Network)));
                tools.DropDownItems.Add(new ManageItem("Internals", null, () => InternalsForm.Show(ui)));
                tools.DropDownItems.Add(new ManageItem("Packets", null, () => PacketsForm.Show(core.Network)));
                tools.DropDownItems.Add(new ManageItem("Search", null, () => SearchForm.Show(core.Network)));
            }

            tools.DropDownItems.Add(new ManageItem("Transfers", null, () => TransferView.Show(core.Network)));

            items.Add(tools);

            // split
            items.Add(new ToolStripSeparator());

            // main options
            items.Add(new ManageItem("Sign On", IMRes.greenled, delegate()
            {
                core.Context.RaiseLogin(null);
            }));

            items.Add(new ManageItem("Sign Off", IMRes.redled, delegate()
            {
                core.Context.RaiseLogin(null);
                ui.GuiMain.Close();
            }));
        }


        void TreeMenu_Select(object sender, EventArgs e)
        {
            // puts internaal view in remote user's point of view

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

            if (Core.Locations.ActiveClientCount(info.UserID) > 0)
            {
                var im = UI.GetService(ServiceIDs.IM) as IMUI;
                if (im != null)
                    im.OpenIMWindow(info.UserID);
            }
            else
            {
                var mail = UI.GetService(ServiceIDs.Mail) as MailUI;
                if (mail != null)
                    mail.OpenComposeWindow(info.UserID);
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
            FillManageMenu(UI, ManageButton.DropDownItems);

            PlanButton.DropDownItems.Clear();
            CommButton.DropDownItems.Clear();
            DataButton.DropDownItems.Clear();

            List<MenuItemInfo> menuList = new List<MenuItemInfo>();

            foreach (var service in UI.Services.Values)
                service.GetMenuInfo(InterfaceMenuType.Internal, menuList, id, project);

            foreach (MenuItemInfo info in menuList)
            {
                string[] parts = info.Path.Split(new char[] { '/' });

                if (parts.Length < 2)
                    continue;

                if (parts[0] == PlanButton.Text)
                    PlanButton.DropDownItems.SortedAdd(new OpStripItem(id, project, false, parts[1], info));

                else if (parts[0] == CommButton.Text)
                    CommButton.DropDownItems.SortedAdd(new OpStripItem(id, project, false, parts[1], info));

                else if (parts[0] == DataButton.Text)
                    DataButton.DropDownItems.SortedAdd(new OpStripItem(id, project, false, parts[1], info));
            }


            // setup nav bar - add components
            UpdateNavBar();


            // find previous component in drop down, activate click on it
            if(InternalView == null)
                ShowInternal(new Info.InfoView(Core, false, true));

            /*string previous = InternalView != null ? InternalView.GetTitle(true) : "Profile";

            if (!SelectService(previous))
                SelectService("Profile");*/

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


        void Core_KeepData()
        {
            // return links in nav bar
            foreach (ToolStripItem item in PersonNavButton.DropDownItems)
            {
                PersonNavItem person = item as PersonNavItem;

                if (person != null)
                    Core.KeepData.SafeAdd(person.UserID, true);
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
                    PersonNavButton.Text = Core.GetName(link.UserID) + "'s";

                PersonNavItem self = null;

                // add higher and subs of higher
                OpLink higher = link.GetHigher(false);
                if (higher != null)
                {
                    PersonNavButton.DropDownItems.Add(new PersonNavItem(Core.GetName(higher.UserID), higher.UserID, this, PersonNav_Clicked));

                    List<ulong> adjacentIDs = Trust.GetDownlinkIDs(higher.UserID, SelectedProject, 1);
                    foreach (ulong id in adjacentIDs)
                    {
                        PersonNavItem item = new PersonNavItem("   " + Core.GetName(id), id, this, PersonNav_Clicked);
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
                    PersonNavItem item = new PersonNavItem(Core.GetName(link.UserID), link.UserID, this, PersonNav_Clicked);
                    item.Font = BoldFont;
                    self = item;
                    PersonNavButton.DropDownItems.Add(item);
                }

                // add downlinks of self
                List<ulong> downlinkIDs = Trust.GetDownlinkIDs(CommandTree.SelectedLink, SelectedProject, 1);
                foreach (ulong id in downlinkIDs)
                {
                    PersonNavItem item = new PersonNavItem(childspacing + Core.GetName(id), id, this, PersonNav_Clicked);

                    int index = PersonNavButton.DropDownItems.IndexOf(self);
                    PersonNavButton.DropDownItems.Insert(index + 1, item);
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

            List<MenuItemInfo> menuList = new List<MenuItemInfo>();

            foreach (var service in UI.Services.Values)
                service.GetMenuInfo(InterfaceMenuType.Internal, menuList, CommandTree.SelectedLink, SelectedProject);

            foreach (MenuItemInfo info in menuList)
                ComponentNavButton.DropDownItems.SortedAdd(new ServiceNavItem(info, CommandTree.SelectedLink, SelectedProject, info.ClickEvent));
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
            AddUsersDialog add = new AddUsersDialog(UI, SelectedProject);
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
            CommandTree.Visible = OperationButton.Checked;

            // if checked, uncheck other and display
            if (OperationButton.Checked)
            {
                BuddiesButton.Checked = false;

                if (ProjectButton != null)
                    ProjectButton.Checked = false;

                MainSplit.Panel1Collapsed = false;

                CommandTree.ShowProject(0);
            }

            // if not check, check if online checked, if not hide
            else
            {
                if (!BuddiesButton.Checked)
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
            BuddyList.Visible = BuddiesButton.Checked;

            // if checked, uncheck other and display
            if (BuddiesButton.Checked)
            {
                OperationButton.Checked = false;

                if (ProjectButton != null)
                    ProjectButton.Checked = false;

                MainSplit.Panel1Collapsed = false;
            }

            // if not check, check if online checked, if not hide
            else
            {
                if (!OperationButton.Checked)
                    if (ProjectButton == null || !ProjectButton.Checked)
                    {
                        if (SideMode)
                            BuddiesButton.Checked = true;
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

            ProjectsButton.DropDownItems.Add(new ToolStripMenuItem("New...", InterfaceRes.project_add, new EventHandler(ProjectMenu_New)));

            Trust.ProjectNames.LockReading(delegate()
            {
                foreach (uint id in Trust.ProjectNames.Keys)
                    if (id != 0 && Trust.ProjectRoots.SafeContainsKey(id))
                        ProjectsButton.DropDownItems.Add(new ProjectItem(Trust.ProjectNames[id], id, new EventHandler(ProjectMenu_Click)));
            });

            if (ProjectButton != null && Trust.LocalTrust.InProject(CommandTree.Project))
            {
                ProjectsButton.DropDownItems.Add(new ToolStripSeparator());
                ProjectsButton.DropDownItems.Add(new ToolStripMenuItem("Leave " + ProjectButton.Text, InterfaceRes.project_remove, new EventHandler(OnProjectLeave)));
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
            ProjectButton.TextDirection = GuiUtils.IsRunningOnMono() ? ToolStripTextDirection.Horizontal : ToolStripTextDirection.Vertical90;
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
                BuddiesButton.Checked = false;
                MainSplit.Panel1Collapsed = false;

                CommandTree.ShowProject(ProjectButtonID);

                CommandTree.Visible = true;
                BuddyList.Visible = false;
            }

            // if not check, check if online checked, if not hide
            else
            {
                if (!OperationButton.Checked && !BuddiesButton.Checked)
                {
                    if (SideMode)
                        ProjectButton.Checked = true;
                    else
                        MainSplit.Panel1Collapsed = true;
                }
            }
        }


        public bool SideMode;
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

                BuddyList.Top = 23;
                BuddyList.Height = CommandSplit.Panel1.Height - 23;

                SideMode = true;

                Rectangle screen = Screen.GetWorkingArea(this);
                Location = new Point(screen.Width - Width, Location.Y); 


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
                
                BuddyList.Top = 0;  
                BuddyList.Height = CommandSplit.Panel1.Height;

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



        private void CommandTree_SelectedItemChanged(object sender, EventArgs e)
        {
            UpdateStatusPanel();
        }

        private void PopoutButton_Click(object sender, EventArgs e)
        {
            if(InternalView == null) // no controls loaded
                return;

            SuspendLayout();
            InternalPanel.Controls.Clear();

            InternalView.BlockReinit = true;
            ShowExternal(InternalView);
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

        void Core_NewsUpdate(uint serviceID, string message, ulong userID, uint projectID, bool showRemote)
        {
            if (!UI.Services.ContainsKey(serviceID))
                return;

            Icon symbol = null;
            EventHandler onClick = null;
            UI.Services[serviceID].GetNewsAction(ref symbol, ref onClick);

            NewsItem item = new NewsItem(serviceID, message, userID, projectID, showRemote, symbol, onClick, Core.UserID); // pop out external view if in messenger mode
            item.Text = Core.TimeNow.ToString("h:mm ") + message;

            // set color
            if (Trust.IsLowerDirect(userID, projectID))
            {
                item.DisplayColor = Color.LightBlue;
                item.ForeColor = Color.Blue;
            }
            else if (Trust.IsHigher(userID, projectID))
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
            int reqWidth = (int)e.Graphics.MeasureString(NewsPending.Peek().Message, BoldFont).Width;

            if (width < reqWidth)
            {
                ActiveNewsButton().Image = InterfaceRes.news_hot;
                return;
            }

            // draw text
            x = x + width / 2 - reqWidth / 2;
            e.Graphics.DrawString(NewsPending.Peek().Message, BoldFont, NewsBrush, x, 5);

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
            if (NewsArea.Contains(e.Location) && NewsPending.Count > 0 && NewsPending.Peek().ClickEvent != null)
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
                NewsPending.Peek().ClickEvent.Invoke(NewsPending.Peek(), null);
            }
        }

#endregion

        private void SideViewsButton_DropDownOpening(object sender, EventArgs e)
        {
            SideViewsButton.DropDownItems.Clear();

            // Manage
            ToolStripMenuItem manage = new ToolStripMenuItem("Manage", InterfaceRes.manage);
            FillManageMenu(UI, manage.DropDownItems);
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

            List<MenuItemInfo> menuList = new List<MenuItemInfo>();

            foreach (var service in UI.Services.Values)
                service.GetMenuInfo(InterfaceMenuType.Internal, menuList, Core.UserID, project);

            foreach (MenuItemInfo info in menuList)
            {
                string[] parts = info.Path.Split(new char[] { '/' });

                if (parts.Length < 2)
                    continue;

                if (parts[0] == PlanButton.Text)
                    plansItem.DropDownItems.SortedAdd(new OpStripItem(Core.UserID, project, true, parts[1], info));

                else if (parts[0] == CommButton.Text)
                    commItem.DropDownItems.SortedAdd(new OpStripItem(Core.UserID, project, true, parts[1], info));

                else if (parts[0] == DataButton.Text)
                    dataItem.DropDownItems.SortedAdd(new OpStripItem(Core.UserID, project, true, parts[1], info));
            }

            collection.Add(plansItem);
            collection.Add(commItem);
            collection.Add(dataItem);
        }

        public void ShowProject(uint project)
        {
            UpdateProjectButton(project);
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            
        }

        private void HelpButton_Click(object sender, EventArgs e)
        {
            ShowInternal(new Info.InfoView(Core, true, false));
        }

        private void NetworkButton_Click(object sender, EventArgs e)
        {
            InfoView view = new InfoView(Core, false, false);

            if (SideMode)
                ShowExternal(view);
            else
                ShowInternal(view);
        }

        private void SideHelpButton_Click(object sender, EventArgs e)
        {
            ShowExternal(new InfoView(Core, true, false));
        }
    }

    class NewsItem : ToolStripMenuItem, IViewParams
    {
        public bool ShowRemote;
        public ulong UserID;
        public uint ProjectID;
        public Color DisplayColor;
        public bool External;
        public string Message;
        public EventHandler ClickEvent;
        ulong LocalID;

        public NewsItem(uint serviceID, string message, ulong userID, uint project, bool showRemote, Icon symbol, EventHandler onClick, ulong localid)
            : base(message, symbol != null ? symbol.ToBitmap() : null, onClick)
        {
            Message = message;
            ShowRemote = showRemote;
            UserID = userID;
            ProjectID = project;
            LocalID = localid;
            ClickEvent = onClick;
        }

        public ulong GetUser()
        {
            return ShowRemote ? UserID : LocalID;
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

    class OpStripItem : ToolStripMenuItem, IViewParams
    {
        public ulong UserID;
        public uint ProjectID;
        public MenuItemInfo Info;
        bool External;

        public OpStripItem(ulong key, uint id, bool external, string text, MenuItemInfo info)
            : base(text, null, info.ClickEvent )
        {
            UserID = key;
            ProjectID = id;
            Info = info;
            External = external;

            Image = Info.Symbol;
        }

        public ulong GetUser()
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
        public uint ProjectID;

        public ProjectItem(string text, uint id, EventHandler onClick)
            : base(text, null, onClick)
        {
            ProjectID = id;
        }
    }

    public class OpMenuItem : ToolStripMenuItem, IViewParams
    {
        public ulong UserID;
        public uint ProjectID;
        public MenuItemInfo Info;

        public OpMenuItem(ulong key, uint id)
        {
            UserID = key;
            ProjectID = id;
        }

        public OpMenuItem(ulong key, uint id, string text, MenuItemInfo info)
            : base(text, null, info.ClickEvent)
        {
            UserID = key;
            ProjectID = id;
            Info = info;

            if(info.Symbol != null)
                Image = info.Symbol;
        }

        public ulong GetUser()
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
        public ulong UserID;

        public PersonNavItem(string name, ulong id, MainForm form, EventHandler onClick)
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
        public uint ProjectID;

        public ProjectNavItem(string name, uint project, EventHandler onClick)
            : base(name, null, onClick)
        {
            ProjectID = project;

            Font = new System.Drawing.Font("Tahoma", 8.25F);
        }
    }

    class ManageItem : ToolStripMenuItem
    {
        MethodInvoker Code;


        public ManageItem(string text, Image image, MethodInvoker code)
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

        public ServiceNavItem(MenuItemInfo info, ulong id, uint project, EventHandler onClick)
            : base("", null, onClick)
        {

            UserID = id;
            ProjectID = project;

            string[] parts = info.Path.Split(new char[] { '/' });

            if (parts.Length == 2)
                Text = parts[1];

            Font = new System.Drawing.Font("Tahoma", 8.25F);

            if (info.Symbol != null)
                Image = info.Symbol;
        }

        public ulong GetUser()
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
