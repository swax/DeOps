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
            this.CommandTree = new DeOps.Interface.TLVex.TreeListViewEx();
            this.StatusBrowser = new System.Windows.Forms.WebBrowser();
            this.RightClickMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.EditMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.InternalView = new System.Windows.Forms.Panel();
            this.CurrentViewLabel = new System.Windows.Forms.Label();
            this.TopToolStrip = new System.Windows.Forms.ToolStrip();
            this.HomeButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.PlanButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.calanderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.personalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CommButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.mailToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.chatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripTextBox1 = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.DataButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.commonToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.personalToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.SideToolStrip = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.OperationButton = new System.Windows.Forms.ToolStripButton();
            this.OnlineButton = new System.Windows.Forms.ToolStripButton();
            this.ProjectsButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.SideButton = new System.Windows.Forms.ToolStripButton();
            this.TreeImageList = new System.Windows.Forms.ImageList(this.components);
            this.MainSplit.Panel1.SuspendLayout();
            this.MainSplit.Panel2.SuspendLayout();
            this.MainSplit.SuspendLayout();
            this.CommandSplit.Panel1.SuspendLayout();
            this.CommandSplit.Panel2.SuspendLayout();
            this.CommandSplit.SuspendLayout();
            this.RightClickMenu.SuspendLayout();
            this.TopToolStrip.SuspendLayout();
            this.SideToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainSplit
            // 
            this.MainSplit.BackColor = System.Drawing.SystemColors.Control;
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
            this.MainSplit.Panel2.BackColor = System.Drawing.SystemColors.Control;
            this.MainSplit.Panel2.Controls.Add(this.InternalView);
            this.MainSplit.Panel2.Controls.Add(this.CurrentViewLabel);
            this.MainSplit.Panel2.Controls.Add(this.TopToolStrip);
            this.MainSplit.Size = new System.Drawing.Size(674, 445);
            this.MainSplit.SplitterDistance = 171;
            this.MainSplit.TabIndex = 1;
            // 
            // CommandSplit
            // 
            this.CommandSplit.BackColor = System.Drawing.SystemColors.Control;
            this.CommandSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CommandSplit.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.CommandSplit.ForeColor = System.Drawing.Color.White;
            this.CommandSplit.Location = new System.Drawing.Point(0, 0);
            this.CommandSplit.Name = "CommandSplit";
            this.CommandSplit.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // CommandSplit.Panel1
            // 
            this.CommandSplit.Panel1.Controls.Add(this.CommandTree);
            // 
            // CommandSplit.Panel2
            // 
            this.CommandSplit.Panel2.BackColor = System.Drawing.SystemColors.ControlLight;
            this.CommandSplit.Panel2.Controls.Add(this.StatusBrowser);
            this.CommandSplit.Panel2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.CommandSplit.Size = new System.Drawing.Size(171, 445);
            this.CommandSplit.SplitterDistance = 277;
            this.CommandSplit.TabIndex = 4;
            // 
            // CommandTree
            // 
            this.CommandTree.BackColor = System.Drawing.SystemColors.Window;
            this.CommandTree.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.CommandTree.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.CommandTree.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.CommandTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.CommandTree.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.CommandTree.HeaderMenu = null;
            this.CommandTree.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.CommandTree.ItemHeight = 20;
            this.CommandTree.ItemMenu = null;
            this.CommandTree.LabelEdit = false;
            this.CommandTree.Location = new System.Drawing.Point(0, 0);
            this.CommandTree.MouseActivte = true;
            this.CommandTree.Name = "CommandTree";
            this.CommandTree.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.CommandTree.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.CommandTree.ShowLines = true;
            this.CommandTree.Size = new System.Drawing.Size(171, 277);
            this.CommandTree.SmallImageList = null;
            this.CommandTree.StateImageList = null;
            this.CommandTree.TabIndex = 0;
            this.CommandTree.Text = "treeListViewEx1";
            this.CommandTree.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.CommandTree_MouseDoubleClick);
            this.CommandTree.MouseClick += new System.Windows.Forms.MouseEventHandler(this.CommandTree_MouseClick);
            this.CommandTree.SelectedItemChanged += new System.EventHandler(this.CommandTree_SelectedItemChanged);
            // 
            // StatusBrowser
            // 
            this.StatusBrowser.AllowWebBrowserDrop = false;
            this.StatusBrowser.ContextMenuStrip = this.RightClickMenu;
            this.StatusBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StatusBrowser.IsWebBrowserContextMenuEnabled = false;
            this.StatusBrowser.Location = new System.Drawing.Point(0, 0);
            this.StatusBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.StatusBrowser.Name = "StatusBrowser";
            this.StatusBrowser.ScrollBarsEnabled = false;
            this.StatusBrowser.Size = new System.Drawing.Size(171, 164);
            this.StatusBrowser.TabIndex = 0;
            this.StatusBrowser.WebBrowserShortcutsEnabled = false;
            this.StatusBrowser.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.StatusBrowser_Navigating);
            // 
            // RightClickMenu
            // 
            this.RightClickMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.EditMenu});
            this.RightClickMenu.Name = "RightClickMenu";
            this.RightClickMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.RightClickMenu.Size = new System.Drawing.Size(116, 26);
            this.RightClickMenu.Opening += new System.ComponentModel.CancelEventHandler(this.RightClickMenu_Opening);
            // 
            // EditMenu
            // 
            this.EditMenu.Name = "EditMenu";
            this.EditMenu.Size = new System.Drawing.Size(115, 22);
            this.EditMenu.Text = "Edit...";
            this.EditMenu.Click += new System.EventHandler(this.EditMenu_Click);
            // 
            // InternalView
            // 
            this.InternalView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InternalView.Location = new System.Drawing.Point(0, 54);
            this.InternalView.Name = "InternalView";
            this.InternalView.Size = new System.Drawing.Size(499, 391);
            this.InternalView.TabIndex = 2;
            // 
            // CurrentViewLabel
            // 
            this.CurrentViewLabel.BackColor = System.Drawing.Color.CornflowerBlue;
            this.CurrentViewLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.CurrentViewLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.CurrentViewLabel.ForeColor = System.Drawing.Color.White;
            this.CurrentViewLabel.Location = new System.Drawing.Point(0, 31);
            this.CurrentViewLabel.Name = "CurrentViewLabel";
            this.CurrentViewLabel.Size = new System.Drawing.Size(499, 23);
            this.CurrentViewLabel.TabIndex = 1;
            this.CurrentViewLabel.Text = " Current View";
            this.CurrentViewLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TopToolStrip
            // 
            this.TopToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.TopToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.HomeButton,
            this.toolStripSeparator1,
            this.PlanButton,
            this.CommButton,
            this.toolStripTextBox1,
            this.toolStripLabel1,
            this.DataButton});
            this.TopToolStrip.Location = new System.Drawing.Point(0, 0);
            this.TopToolStrip.Name = "TopToolStrip";
            this.TopToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.TopToolStrip.Size = new System.Drawing.Size(499, 31);
            this.TopToolStrip.TabIndex = 0;
            this.TopToolStrip.Text = "MainToolstrip";
            // 
            // HomeButton
            // 
            this.HomeButton.Image = ((System.Drawing.Image)(resources.GetObject("HomeButton.Image")));
            this.HomeButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.HomeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.HomeButton.Name = "HomeButton";
            this.HomeButton.Size = new System.Drawing.Size(79, 28);
            this.HomeButton.Text = "My Home";
            this.HomeButton.Click += new System.EventHandler(this.HomeButton_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 31);
            // 
            // PlanButton
            // 
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
            // toolStripTextBox1
            // 
            this.toolStripTextBox1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripTextBox1.Name = "toolStripTextBox1";
            this.toolStripTextBox1.Size = new System.Drawing.Size(100, 31);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(46, 28);
            this.toolStripLabel1.Text = "Search";
            // 
            // DataButton
            // 
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
            this.SideToolStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.SideToolStrip.Dock = System.Windows.Forms.DockStyle.Left;
            this.SideToolStrip.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SideToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.SideToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel2,
            this.OperationButton,
            this.OnlineButton,
            this.ProjectsButton,
            this.SideButton});
            this.SideToolStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
            this.SideToolStrip.Location = new System.Drawing.Point(0, 0);
            this.SideToolStrip.Name = "SideToolStrip";
            this.SideToolStrip.Padding = new System.Windows.Forms.Padding(3, 0, 1, 0);
            this.SideToolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.SideToolStrip.Size = new System.Drawing.Size(27, 445);
            this.SideToolStrip.TabIndex = 3;
            this.SideToolStrip.TextDirection = System.Windows.Forms.ToolStripTextDirection.Vertical270;
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.None;
            this.toolStripLabel2.Margin = new System.Windows.Forms.Padding(0, 50, 0, 2);
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(18, 0);
            this.toolStripLabel2.Text = "toolStripLabel2";
            // 
            // OperationButton
            // 
            this.OperationButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.OperationButton.Checked = true;
            this.OperationButton.CheckOnClick = true;
            this.OperationButton.CheckState = System.Windows.Forms.CheckState.Checked;
            this.OperationButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.OperationButton.ForeColor = System.Drawing.Color.Black;
            this.OperationButton.Image = ((System.Drawing.Image)(resources.GetObject("OperationButton.Image")));
            this.OperationButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.OperationButton.Name = "OperationButton";
            this.OperationButton.Size = new System.Drawing.Size(18, 65);
            this.OperationButton.Text = "Structure";
            this.OperationButton.TextDirection = System.Windows.Forms.ToolStripTextDirection.Vertical90;
            this.OperationButton.CheckedChanged += new System.EventHandler(this.OperationButton_CheckedChanged);
            // 
            // OnlineButton
            // 
            this.OnlineButton.CheckOnClick = true;
            this.OnlineButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.OnlineButton.ForeColor = System.Drawing.Color.Black;
            this.OnlineButton.Image = ((System.Drawing.Image)(resources.GetObject("OnlineButton.Image")));
            this.OnlineButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.OnlineButton.Name = "OnlineButton";
            this.OnlineButton.Size = new System.Drawing.Size(18, 46);
            this.OnlineButton.Text = "Online";
            this.OnlineButton.TextDirection = System.Windows.Forms.ToolStripTextDirection.Vertical90;
            this.OnlineButton.CheckedChanged += new System.EventHandler(this.OnlineButton_CheckedChanged);
            // 
            // ProjectsButton
            // 
            this.ProjectsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ProjectsButton.ForeColor = System.Drawing.Color.Black;
            this.ProjectsButton.Image = ((System.Drawing.Image)(resources.GetObject("ProjectsButton.Image")));
            this.ProjectsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ProjectsButton.Name = "ProjectsButton";
            this.ProjectsButton.Size = new System.Drawing.Size(18, 65);
            this.ProjectsButton.Text = "Projects";
            this.ProjectsButton.TextDirection = System.Windows.Forms.ToolStripTextDirection.Vertical90;
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
            // TreeImageList
            // 
            this.TreeImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("TreeImageList.ImageStream")));
            this.TreeImageList.TransparentColor = System.Drawing.Color.White;
            this.TreeImageList.Images.SetKeyName(0, "link_confirmed.ico");
            this.TreeImageList.Images.SetKeyName(1, "link_denied.ico");
            this.TreeImageList.Images.SetKeyName(2, "link_pending.ico");
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(701, 445);
            this.Controls.Add(this.MainSplit);
            this.Controls.Add(this.SideToolStrip);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "De-Ops";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.MainSplit.Panel1.ResumeLayout(false);
            this.MainSplit.Panel2.ResumeLayout(false);
            this.MainSplit.Panel2.PerformLayout();
            this.MainSplit.ResumeLayout(false);
            this.CommandSplit.Panel1.ResumeLayout(false);
            this.CommandSplit.Panel2.ResumeLayout(false);
            this.CommandSplit.ResumeLayout(false);
            this.RightClickMenu.ResumeLayout(false);
            this.TopToolStrip.ResumeLayout(false);
            this.TopToolStrip.PerformLayout();
            this.SideToolStrip.ResumeLayout(false);
            this.SideToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer MainSplit;
        internal System.Windows.Forms.Label CurrentViewLabel;
        private System.Windows.Forms.ImageList TreeImageList;
        private System.Windows.Forms.ToolStrip TopToolStrip;
        private System.Windows.Forms.ToolStripButton HomeButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBox1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.SplitContainer CommandSplit;
        private System.Windows.Forms.ToolStrip SideToolStrip;
        private System.Windows.Forms.ToolStripButton OperationButton;
        private System.Windows.Forms.ToolStripButton OnlineButton;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.Panel InternalView;
        private System.Windows.Forms.ToolStripDropDownButton ProjectsButton;
        private System.Windows.Forms.ToolStripButton SideButton;
        private System.Windows.Forms.WebBrowser StatusBrowser;
        private System.Windows.Forms.ContextMenuStrip RightClickMenu;
        private System.Windows.Forms.ToolStripMenuItem EditMenu;
        private System.Windows.Forms.ToolStripDropDownButton CommButton;
        private System.Windows.Forms.ToolStripMenuItem mailToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem chatToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton PlanButton;
        private System.Windows.Forms.ToolStripMenuItem calanderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem personalToolStripMenuItem;
        private System.Windows.Forms.ToolStripDropDownButton DataButton;
        private System.Windows.Forms.ToolStripMenuItem commonToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem personalToolStripMenuItem2;
        private DeOps.Interface.TLVex.TreeListViewEx CommandTree;
    }
}