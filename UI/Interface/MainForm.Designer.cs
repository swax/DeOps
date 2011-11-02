namespace DeOps.Interface
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.MainSplit = new System.Windows.Forms.SplitContainer();
            this.CommandSplit = new System.Windows.Forms.SplitContainer();
            this.BuddyList = new DeOps.Services.Buddy.BuddyView();
            this.SideNavStrip = new System.Windows.Forms.ToolStrip();
            this.SideViewsButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.SideHelpButton = new System.Windows.Forms.ToolStripButton();
            this.SideSearchButton = new System.Windows.Forms.ToolStripButton();
            this.SideNewsButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.CommandTree = new DeOps.Services.Trust.LinkTree();
            this.SelectionInfo = new DeOps.Interface.StatusPanel();
            this.NavStrip = new System.Windows.Forms.ToolStrip();
            this.PersonNavButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.ProjectNavButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.ComponentNavButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.PopoutButton = new System.Windows.Forms.ToolStripButton();
            this.NewsButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.InternalPanel = new System.Windows.Forms.Panel();
            this.TopToolStrip = new System.Windows.Forms.ToolStrip();
            this.HomeButton = new System.Windows.Forms.ToolStripButton();
            this.ManageButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.inviteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.signOnToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.signOffToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.PlanButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.calanderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.personalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CommButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.mailToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.chatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.HelpInfoButton = new System.Windows.Forms.ToolStripButton();
            this.SearchButton = new System.Windows.Forms.ToolStripButton();
            this.SearchBox = new System.Windows.Forms.ToolStripTextBox();
            this.DataButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.commonToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.personalToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.SideToolStrip = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.OperationButton = new System.Windows.Forms.ToolStripButton();
            this.BuddiesButton = new System.Windows.Forms.ToolStripButton();
            this.ProjectsButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.SideButton = new System.Windows.Forms.ToolStripButton();
            this.LockButton = new System.Windows.Forms.ToolStripButton();
            this.NetworkButton = new System.Windows.Forms.ToolStripButton();
            this.TreeImageList = new System.Windows.Forms.ImageList(this.components);
            this.NewsTimer = new System.Windows.Forms.Timer(this.components);
            this.MainSplit.Panel1.SuspendLayout();
            this.MainSplit.Panel2.SuspendLayout();
            this.MainSplit.SuspendLayout();
            this.CommandSplit.Panel1.SuspendLayout();
            this.CommandSplit.Panel2.SuspendLayout();
            this.CommandSplit.SuspendLayout();
            this.SideNavStrip.SuspendLayout();
            this.NavStrip.SuspendLayout();
            this.TopToolStrip.SuspendLayout();
            this.SideToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainSplit
            // 
            this.MainSplit.BackColor = System.Drawing.Color.WhiteSmoke;
            this.MainSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainSplit.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.MainSplit.Location = new System.Drawing.Point(27, 0);
            this.MainSplit.Name = "MainSplit";
            // 
            // MainSplit.Panel1
            // 
            this.MainSplit.Panel1.Controls.Add(this.CommandSplit);
            // 
            // MainSplit.Panel2
            // 
            this.MainSplit.Panel2.BackColor = System.Drawing.Color.White;
            this.MainSplit.Panel2.Controls.Add(this.NavStrip);
            this.MainSplit.Panel2.Controls.Add(this.InternalPanel);
            this.MainSplit.Panel2.Controls.Add(this.TopToolStrip);
            this.MainSplit.Size = new System.Drawing.Size(741, 447);
            this.MainSplit.SplitterDistance = 171;
            this.MainSplit.SplitterWidth = 2;
            this.MainSplit.TabIndex = 1;
            // 
            // CommandSplit
            // 
            this.CommandSplit.BackColor = System.Drawing.Color.White;
            this.CommandSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CommandSplit.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.CommandSplit.ForeColor = System.Drawing.Color.White;
            this.CommandSplit.Location = new System.Drawing.Point(0, 0);
            this.CommandSplit.Name = "CommandSplit";
            this.CommandSplit.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // CommandSplit.Panel1
            // 
            this.CommandSplit.Panel1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.CommandSplit.Panel1.Controls.Add(this.BuddyList);
            this.CommandSplit.Panel1.Controls.Add(this.SideNavStrip);
            this.CommandSplit.Panel1.Controls.Add(this.CommandTree);
            // 
            // CommandSplit.Panel2
            // 
            this.CommandSplit.Panel2.BackColor = System.Drawing.Color.WhiteSmoke;
            this.CommandSplit.Panel2.Controls.Add(this.SelectionInfo);
            this.CommandSplit.Size = new System.Drawing.Size(171, 447);
            this.CommandSplit.SplitterDistance = 324;
            this.CommandSplit.SplitterWidth = 2;
            this.CommandSplit.TabIndex = 4;
            // 
            // BuddyList
            // 
            this.BuddyList.AllowDrop = true;
            this.BuddyList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.BuddyList.BackColor = System.Drawing.SystemColors.Window;
            this.BuddyList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.BuddyList.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.BuddyList.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.BuddyList.DisableHorizontalScroll = true;
            this.BuddyList.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.BuddyList.HeaderMenu = null;
            this.BuddyList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.BuddyList.ItemMenu = null;
            this.BuddyList.LabelEdit = false;
            this.BuddyList.Location = new System.Drawing.Point(0, 25);
            this.BuddyList.MultiSelect = true;
            this.BuddyList.Name = "BuddyList";
            this.BuddyList.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.BuddyList.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.BuddyList.Size = new System.Drawing.Size(171, 296);
            this.BuddyList.SmallImageList = null;
            this.BuddyList.StateImageList = null;
            this.BuddyList.TabIndex = 0;
            this.BuddyList.Visible = false;
            // 
            // SideNavStrip
            // 
            this.SideNavStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.SideNavStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SideViewsButton,
            this.SideHelpButton,
            this.SideSearchButton,
            this.SideNewsButton});
            this.SideNavStrip.Location = new System.Drawing.Point(0, 0);
            this.SideNavStrip.Name = "SideNavStrip";
            this.SideNavStrip.Size = new System.Drawing.Size(171, 25);
            this.SideNavStrip.TabIndex = 1;
            this.SideNavStrip.Paint += new System.Windows.Forms.PaintEventHandler(this.SideModeStrip_Paint);
            this.SideNavStrip.MouseClick += new System.Windows.Forms.MouseEventHandler(this.SideModeStrip_MouseClick);
            this.SideNavStrip.MouseMove += new System.Windows.Forms.MouseEventHandler(this.SideModeStrip_MouseMove);
            // 
            // SideViewsButton
            // 
            this.SideViewsButton.Image = ((System.Drawing.Image)(resources.GetObject("SideViewsButton.Image")));
            this.SideViewsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.SideViewsButton.Name = "SideViewsButton";
            this.SideViewsButton.Size = new System.Drawing.Size(63, 22);
            this.SideViewsButton.Text = "Views";
            this.SideViewsButton.DropDownOpening += new System.EventHandler(this.SideViewsButton_DropDownOpening);
            // 
            // SideHelpButton
            // 
            this.SideHelpButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.SideHelpButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.SideHelpButton.Image = ((System.Drawing.Image)(resources.GetObject("SideHelpButton.Image")));
            this.SideHelpButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.SideHelpButton.Name = "SideHelpButton";
            this.SideHelpButton.Size = new System.Drawing.Size(23, 22);
            this.SideHelpButton.Text = "Help";
            this.SideHelpButton.Click += new System.EventHandler(this.SideHelpButton_Click);
            // 
            // SideSearchButton
            // 
            this.SideSearchButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.SideSearchButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.SideSearchButton.Image = ((System.Drawing.Image)(resources.GetObject("SideSearchButton.Image")));
            this.SideSearchButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.SideSearchButton.Name = "SideSearchButton";
            this.SideSearchButton.Size = new System.Drawing.Size(23, 22);
            this.SideSearchButton.Text = "toolStripButton1";
            this.SideSearchButton.Visible = false;
            // 
            // SideNewsButton
            // 
            this.SideNewsButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.SideNewsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.SideNewsButton.Image = ((System.Drawing.Image)(resources.GetObject("SideNewsButton.Image")));
            this.SideNewsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.SideNewsButton.Name = "SideNewsButton";
            this.SideNewsButton.Size = new System.Drawing.Size(29, 22);
            this.SideNewsButton.Text = "News";
            this.SideNewsButton.DropDownOpening += new System.EventHandler(this.SideNewsButton_DropDownOpening);
            // 
            // CommandTree
            // 
            this.CommandTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.CommandTree.BackColor = System.Drawing.SystemColors.Window;
            this.CommandTree.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.CommandTree.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.CommandTree.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.CommandTree.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.CommandTree.HeaderMenu = null;
            this.CommandTree.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.CommandTree.ItemHeight = 20;
            this.CommandTree.ItemMenu = null;
            this.CommandTree.LabelEdit = false;
            this.CommandTree.Location = new System.Drawing.Point(0, 25);
            this.CommandTree.MouseActivte = true;
            this.CommandTree.Name = "CommandTree";
            this.CommandTree.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.CommandTree.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.CommandTree.ShowLines = true;
            this.CommandTree.Size = new System.Drawing.Size(171, 296);
            this.CommandTree.SmallImageList = null;
            this.CommandTree.StateImageList = null;
            this.CommandTree.TabIndex = 0;
            this.CommandTree.MouseClick += new System.Windows.Forms.MouseEventHandler(this.CommandTree_MouseClick);
            this.CommandTree.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.CommandTree_MouseDoubleClick);
            this.CommandTree.SelectedItemChanged += new System.EventHandler(this.CommandTree_SelectedItemChanged);
            // 
            // SelectionInfo
            // 
            this.SelectionInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SelectionInfo.Location = new System.Drawing.Point(0, 0);
            this.SelectionInfo.Name = "SelectionInfo";
            this.SelectionInfo.Size = new System.Drawing.Size(171, 121);
            this.SelectionInfo.TabIndex = 0;
            // 
            // NavStrip
            // 
            this.NavStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.NavStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PersonNavButton,
            this.ProjectNavButton,
            this.ComponentNavButton,
            this.PopoutButton,
            this.NewsButton});
            this.NavStrip.Location = new System.Drawing.Point(0, 31);
            this.NavStrip.Name = "NavStrip";
            this.NavStrip.ShowItemToolTips = false;
            this.NavStrip.Size = new System.Drawing.Size(568, 25);
            this.NavStrip.TabIndex = 3;
            this.NavStrip.Paint += new System.Windows.Forms.PaintEventHandler(this.NavStrip_Paint);
            this.NavStrip.MouseClick += new System.Windows.Forms.MouseEventHandler(this.NavStrip_MouseClick);
            this.NavStrip.MouseMove += new System.Windows.Forms.MouseEventHandler(this.NavStrip_MouseMove);
            // 
            // PersonNavButton
            // 
            this.PersonNavButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.PersonNavButton.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PersonNavButton.ForeColor = System.Drawing.Color.White;
            this.PersonNavButton.Image = ((System.Drawing.Image)(resources.GetObject("PersonNavButton.Image")));
            this.PersonNavButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.PersonNavButton.Name = "PersonNavButton";
            this.PersonNavButton.Size = new System.Drawing.Size(59, 22);
            this.PersonNavButton.Text = "Person";
            // 
            // ProjectNavButton
            // 
            this.ProjectNavButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ProjectNavButton.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ProjectNavButton.ForeColor = System.Drawing.Color.White;
            this.ProjectNavButton.Image = ((System.Drawing.Image)(resources.GetObject("ProjectNavButton.Image")));
            this.ProjectNavButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ProjectNavButton.Name = "ProjectNavButton";
            this.ProjectNavButton.Size = new System.Drawing.Size(61, 22);
            this.ProjectNavButton.Text = "Project";
            // 
            // ComponentNavButton
            // 
            this.ComponentNavButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ComponentNavButton.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ComponentNavButton.ForeColor = System.Drawing.Color.White;
            this.ComponentNavButton.Image = ((System.Drawing.Image)(resources.GetObject("ComponentNavButton.Image")));
            this.ComponentNavButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ComponentNavButton.Name = "ComponentNavButton";
            this.ComponentNavButton.Size = new System.Drawing.Size(46, 22);
            this.ComponentNavButton.Text = "View";
            // 
            // PopoutButton
            // 
            this.PopoutButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.PopoutButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.PopoutButton.Image = ((System.Drawing.Image)(resources.GetObject("PopoutButton.Image")));
            this.PopoutButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.PopoutButton.Name = "PopoutButton";
            this.PopoutButton.Size = new System.Drawing.Size(23, 22);
            this.PopoutButton.Text = "Popout Window";
            this.PopoutButton.Click += new System.EventHandler(this.PopoutButton_Click);
            // 
            // NewsButton
            // 
            this.NewsButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.NewsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.NewsButton.Image = ((System.Drawing.Image)(resources.GetObject("NewsButton.Image")));
            this.NewsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.NewsButton.Name = "NewsButton";
            this.NewsButton.Size = new System.Drawing.Size(29, 22);
            this.NewsButton.Text = "Recent News";
            this.NewsButton.DropDownOpening += new System.EventHandler(this.NewsButton_DropDownOpening);
            // 
            // InternalPanel
            // 
            this.InternalPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.InternalPanel.BackColor = System.Drawing.Color.WhiteSmoke;
            this.InternalPanel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.InternalPanel.Location = new System.Drawing.Point(0, 56);
            this.InternalPanel.Name = "InternalPanel";
            this.InternalPanel.Size = new System.Drawing.Size(566, 391);
            this.InternalPanel.TabIndex = 2;
            // 
            // TopToolStrip
            // 
            this.TopToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.TopToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.HomeButton,
            this.ManageButton,
            this.ToolSeparator,
            this.PlanButton,
            this.CommButton,
            this.HelpInfoButton,
            this.SearchButton,
            this.SearchBox,
            this.DataButton});
            this.TopToolStrip.Location = new System.Drawing.Point(0, 0);
            this.TopToolStrip.Name = "TopToolStrip";
            this.TopToolStrip.ShowItemToolTips = false;
            this.TopToolStrip.Size = new System.Drawing.Size(568, 31);
            this.TopToolStrip.TabIndex = 0;
            this.TopToolStrip.Text = "MainToolstrip";
            // 
            // HomeButton
            // 
            this.HomeButton.AutoToolTip = false;
            this.HomeButton.Image = ((System.Drawing.Image)(resources.GetObject("HomeButton.Image")));
            this.HomeButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.HomeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.HomeButton.Name = "HomeButton";
            this.HomeButton.Size = new System.Drawing.Size(79, 28);
            this.HomeButton.Text = "My Home";
            this.HomeButton.Click += new System.EventHandler(this.HomeButton_Click);
            // 
            // ManageButton
            // 
            this.ManageButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.settingsToolStripMenuItem,
            this.inviteToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.toolStripMenuItem1,
            this.signOnToolStripMenuItem,
            this.signOffToolStripMenuItem});
            this.ManageButton.Image = ((System.Drawing.Image)(resources.GetObject("ManageButton.Image")));
            this.ManageButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ManageButton.Name = "ManageButton";
            this.ManageButton.Size = new System.Drawing.Size(74, 28);
            this.ManageButton.Text = "Manage";
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.settingsToolStripMenuItem.Text = "Settings";
            // 
            // inviteToolStripMenuItem
            // 
            this.inviteToolStripMenuItem.Name = "inviteToolStripMenuItem";
            this.inviteToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.inviteToolStripMenuItem.Text = "Invite";
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(121, 6);
            // 
            // signOnToolStripMenuItem
            // 
            this.signOnToolStripMenuItem.Name = "signOnToolStripMenuItem";
            this.signOnToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.signOnToolStripMenuItem.Text = "Sign On";
            // 
            // signOffToolStripMenuItem
            // 
            this.signOffToolStripMenuItem.Name = "signOffToolStripMenuItem";
            this.signOffToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.signOffToolStripMenuItem.Text = "Sign Off";
            // 
            // ToolSeparator
            // 
            this.ToolSeparator.Name = "ToolSeparator";
            this.ToolSeparator.Size = new System.Drawing.Size(6, 31);
            // 
            // PlanButton
            // 
            this.PlanButton.AutoToolTip = false;
            this.PlanButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.calanderToolStripMenuItem,
            this.personalToolStripMenuItem});
            this.PlanButton.Image = ((System.Drawing.Image)(resources.GetObject("PlanButton.Image")));
            this.PlanButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.PlanButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.PlanButton.Name = "PlanButton";
            this.PlanButton.Size = new System.Drawing.Size(69, 28);
            this.PlanButton.Text = "Plans";
            this.PlanButton.TextDirection = System.Windows.Forms.ToolStripTextDirection.Horizontal;
            // 
            // calanderToolStripMenuItem
            // 
            this.calanderToolStripMenuItem.Name = "calanderToolStripMenuItem";
            this.calanderToolStripMenuItem.Size = new System.Drawing.Size(128, 22);
            this.calanderToolStripMenuItem.Text = "Calandar";
            // 
            // personalToolStripMenuItem
            // 
            this.personalToolStripMenuItem.Name = "personalToolStripMenuItem";
            this.personalToolStripMenuItem.Size = new System.Drawing.Size(128, 22);
            this.personalToolStripMenuItem.Text = "Personal";
            // 
            // CommButton
            // 
            this.CommButton.AutoToolTip = false;
            this.CommButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mailToolStripMenuItem,
            this.chatToolStripMenuItem});
            this.CommButton.Image = ((System.Drawing.Image)(resources.GetObject("CommButton.Image")));
            this.CommButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.CommButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.CommButton.Name = "CommButton";
            this.CommButton.Size = new System.Drawing.Size(73, 28);
            this.CommButton.Text = "Comm";
            // 
            // mailToolStripMenuItem
            // 
            this.mailToolStripMenuItem.Name = "mailToolStripMenuItem";
            this.mailToolStripMenuItem.Size = new System.Drawing.Size(108, 22);
            this.mailToolStripMenuItem.Text = "Mail";
            // 
            // chatToolStripMenuItem
            // 
            this.chatToolStripMenuItem.Name = "chatToolStripMenuItem";
            this.chatToolStripMenuItem.Size = new System.Drawing.Size(108, 22);
            this.chatToolStripMenuItem.Text = "Chat";
            // 
            // HelpInfoButton
            // 
            this.HelpInfoButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.HelpInfoButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.HelpInfoButton.Image = ((System.Drawing.Image)(resources.GetObject("HelpInfoButton.Image")));
            this.HelpInfoButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.HelpInfoButton.Name = "HelpInfoButton";
            this.HelpInfoButton.Size = new System.Drawing.Size(23, 28);
            this.HelpInfoButton.Text = "Help";
            this.HelpInfoButton.Click += new System.EventHandler(this.HelpButton_Click);
            // 
            // SearchButton
            // 
            this.SearchButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.SearchButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.SearchButton.Enabled = false;
            this.SearchButton.Image = ((System.Drawing.Image)(resources.GetObject("SearchButton.Image")));
            this.SearchButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.SearchButton.Name = "SearchButton";
            this.SearchButton.Size = new System.Drawing.Size(23, 28);
            this.SearchButton.Text = "Search";
            this.SearchButton.Click += new System.EventHandler(this.SearchButton_Click);
            // 
            // SearchBox
            // 
            this.SearchBox.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.SearchBox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.SearchBox.Enabled = false;
            this.SearchBox.Name = "SearchBox";
            this.SearchBox.Size = new System.Drawing.Size(100, 31);
            // 
            // DataButton
            // 
            this.DataButton.AutoToolTip = false;
            this.DataButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.commonToolStripMenuItem1,
            this.personalToolStripMenuItem2});
            this.DataButton.Image = ((System.Drawing.Image)(resources.GetObject("DataButton.Image")));
            this.DataButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.DataButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.DataButton.Name = "DataButton";
            this.DataButton.Size = new System.Drawing.Size(67, 28);
            this.DataButton.Text = "Data";
            // 
            // commonToolStripMenuItem1
            // 
            this.commonToolStripMenuItem1.Name = "commonToolStripMenuItem1";
            this.commonToolStripMenuItem1.Size = new System.Drawing.Size(126, 22);
            this.commonToolStripMenuItem1.Text = "Common";
            // 
            // personalToolStripMenuItem2
            // 
            this.personalToolStripMenuItem2.Name = "personalToolStripMenuItem2";
            this.personalToolStripMenuItem2.Size = new System.Drawing.Size(126, 22);
            this.personalToolStripMenuItem2.Text = "Personal";
            // 
            // SideToolStrip
            // 
            this.SideToolStrip.Dock = System.Windows.Forms.DockStyle.Left;
            this.SideToolStrip.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SideToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.SideToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel2,
            this.OperationButton,
            this.BuddiesButton,
            this.ProjectsButton,
            this.SideButton,
            this.LockButton,
            this.NetworkButton});
            this.SideToolStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
            this.SideToolStrip.Location = new System.Drawing.Point(0, 0);
            this.SideToolStrip.Name = "SideToolStrip";
            this.SideToolStrip.Padding = new System.Windows.Forms.Padding(3, 0, 1, 0);
            this.SideToolStrip.Size = new System.Drawing.Size(27, 447);
            this.SideToolStrip.TabIndex = 3;
            this.SideToolStrip.TextDirection = System.Windows.Forms.ToolStripTextDirection.Vertical270;
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.None;
            this.toolStripLabel2.Margin = new System.Windows.Forms.Padding(0, 25, 0, 2);
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(18, 0);
            // 
            // OperationButton
            // 
            this.OperationButton.Checked = true;
            this.OperationButton.CheckOnClick = true;
            this.OperationButton.CheckState = System.Windows.Forms.CheckState.Checked;
            this.OperationButton.ForeColor = System.Drawing.Color.Black;
            this.OperationButton.Image = ((System.Drawing.Image)(resources.GetObject("OperationButton.Image")));
            this.OperationButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.OperationButton.Name = "OperationButton";
            this.OperationButton.Size = new System.Drawing.Size(18, 83);
            this.OperationButton.Text = "Operation";
            this.OperationButton.TextDirection = System.Windows.Forms.ToolStripTextDirection.Vertical90;
            this.OperationButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.OperationButton.CheckedChanged += new System.EventHandler(this.OperationButton_CheckedChanged);
            // 
            // BuddiesButton
            // 
            this.BuddiesButton.CheckOnClick = true;
            this.BuddiesButton.ForeColor = System.Drawing.Color.Black;
            this.BuddiesButton.Image = ((System.Drawing.Image)(resources.GetObject("BuddiesButton.Image")));
            this.BuddiesButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.BuddiesButton.Name = "BuddiesButton";
            this.BuddiesButton.Size = new System.Drawing.Size(18, 71);
            this.BuddiesButton.Text = "Buddies";
            this.BuddiesButton.TextDirection = System.Windows.Forms.ToolStripTextDirection.Vertical90;
            this.BuddiesButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.BuddiesButton.CheckedChanged += new System.EventHandler(this.OnlineButton_CheckedChanged);
            // 
            // ProjectsButton
            // 
            this.ProjectsButton.ForeColor = System.Drawing.Color.Black;
            this.ProjectsButton.Image = ((System.Drawing.Image)(resources.GetObject("ProjectsButton.Image")));
            this.ProjectsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ProjectsButton.Name = "ProjectsButton";
            this.ProjectsButton.Size = new System.Drawing.Size(18, 81);
            this.ProjectsButton.Text = "Projects";
            this.ProjectsButton.TextDirection = System.Windows.Forms.ToolStripTextDirection.Vertical90;
            this.ProjectsButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.ProjectsButton.DropDownOpening += new System.EventHandler(this.ProjectsButton_DropDownOpening);
            // 
            // SideButton
            // 
            this.SideButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.SideButton.CheckOnClick = true;
            this.SideButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.SideButton.Image = ((System.Drawing.Image)(resources.GetObject("SideButton.Image")));
            this.SideButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.SideButton.Name = "SideButton";
            this.SideButton.Size = new System.Drawing.Size(18, 20);
            this.SideButton.Text = "Toggle Sidebar";
            this.SideButton.CheckedChanged += new System.EventHandler(this.SideButton_CheckedChanged);
            // 
            // LockButton
            // 
            this.LockButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.LockButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.LockButton.Image = ((System.Drawing.Image)(resources.GetObject("LockButton.Image")));
            this.LockButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.LockButton.Name = "LockButton";
            this.LockButton.Size = new System.Drawing.Size(18, 20);
            this.LockButton.Text = "Lockdown to Tray";
            this.LockButton.Click += new System.EventHandler(this.LockButton_Click);
            // 
            // NetworkButton
            // 
            this.NetworkButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.NetworkButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.NetworkButton.Image = ((System.Drawing.Image)(resources.GetObject("NetworkButton.Image")));
            this.NetworkButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.NetworkButton.Name = "NetworkButton";
            this.NetworkButton.Size = new System.Drawing.Size(18, 20);
            this.NetworkButton.Text = "Network Info";
            this.NetworkButton.Click += new System.EventHandler(this.NetworkButton_Click);
            // 
            // TreeImageList
            // 
            this.TreeImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("TreeImageList.ImageStream")));
            this.TreeImageList.TransparentColor = System.Drawing.Color.White;
            this.TreeImageList.Images.SetKeyName(0, "link_confirmed.ico");
            this.TreeImageList.Images.SetKeyName(1, "link_denied.ico");
            this.TreeImageList.Images.SetKeyName(2, "link_pending.ico");
            // 
            // NewsTimer
            // 
            this.NewsTimer.Enabled = true;
            this.NewsTimer.Tick += new System.EventHandler(this.NewsTimer_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(768, 447);
            this.Controls.Add(this.MainSplit);
            this.Controls.Add(this.SideToolStrip);
            this.DoubleBuffered = true;
            this.Name = "MainForm";
            this.Text = "DeOps";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.MainSplit.Panel1.ResumeLayout(false);
            this.MainSplit.Panel2.ResumeLayout(false);
            this.MainSplit.Panel2.PerformLayout();
            this.MainSplit.ResumeLayout(false);
            this.CommandSplit.Panel1.ResumeLayout(false);
            this.CommandSplit.Panel1.PerformLayout();
            this.CommandSplit.Panel2.ResumeLayout(false);
            this.CommandSplit.ResumeLayout(false);
            this.SideNavStrip.ResumeLayout(false);
            this.SideNavStrip.PerformLayout();
            this.NavStrip.ResumeLayout(false);
            this.NavStrip.PerformLayout();
            this.TopToolStrip.ResumeLayout(false);
            this.TopToolStrip.PerformLayout();
            this.SideToolStrip.ResumeLayout(false);
            this.SideToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer MainSplit;
        private System.Windows.Forms.ImageList TreeImageList;
        private System.Windows.Forms.ToolStrip TopToolStrip;
        private System.Windows.Forms.ToolStripButton HomeButton;
        private System.Windows.Forms.ToolStripSeparator ToolSeparator;
        private System.Windows.Forms.ToolStripTextBox SearchBox;
        private System.Windows.Forms.SplitContainer CommandSplit;
        private System.Windows.Forms.ToolStrip SideToolStrip;
        private System.Windows.Forms.ToolStripButton OperationButton;
        private System.Windows.Forms.ToolStripButton BuddiesButton;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.Panel InternalPanel;
        private System.Windows.Forms.ToolStripDropDownButton ProjectsButton;
        private System.Windows.Forms.ToolStripButton SideButton;
        private System.Windows.Forms.ToolStripDropDownButton CommButton;
        private System.Windows.Forms.ToolStripMenuItem mailToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem chatToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton PlanButton;
        private System.Windows.Forms.ToolStripMenuItem calanderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem personalToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton DataButton;
        private System.Windows.Forms.ToolStripMenuItem commonToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem personalToolStripMenuItem2;
        private DeOps.Services.Trust.LinkTree CommandTree;
        private System.Windows.Forms.ToolStrip NavStrip;
        private System.Windows.Forms.ToolStripDropDownButton PersonNavButton;
        private System.Windows.Forms.ToolStripDropDownButton ProjectNavButton;
        private System.Windows.Forms.ToolStripDropDownButton ComponentNavButton;
        private System.Windows.Forms.ToolStripButton PopoutButton;
        private System.Windows.Forms.ToolStripButton LockButton;
        private System.Windows.Forms.Timer NewsTimer;
        private System.Windows.Forms.ToolStripDropDownButton NewsButton;
        private System.Windows.Forms.ToolStrip SideNavStrip;
        private System.Windows.Forms.ToolStripDropDownButton SideViewsButton;
        private System.Windows.Forms.ToolStripDropDownButton SideNewsButton;
        private System.Windows.Forms.ToolStripDropDownButton ManageButton;
        private System.Windows.Forms.ToolStripButton SearchButton;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem inviteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem signOnToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem signOffToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton SideSearchButton;
        private DeOps.Services.Buddy.BuddyView BuddyList;
        private StatusPanel SelectionInfo;
        private System.Windows.Forms.ToolStripButton HelpInfoButton;
        private System.Windows.Forms.ToolStripButton NetworkButton;
        private System.Windows.Forms.ToolStripButton SideHelpButton;
    }
}