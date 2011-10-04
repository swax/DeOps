namespace DeOps.Services.Board
{
    partial class BoardView
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader1 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader2 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader3 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BoardView));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.PostView = new DeOps.Interface.TLVex.TreeListViewEx();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.ArchiveButton = new System.Windows.Forms.ToolStripButton();
            this.RightSplitter = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel4 = new System.Windows.Forms.ToolStripLabel();
            this.PostButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.RefreshButton = new System.Windows.Forms.ToolStripButton();
            this.ButtonLow = new System.Windows.Forms.ToolStripButton();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.ButtonHigh = new System.Windows.Forms.ToolStripButton();
            this.toolStripLabel3 = new System.Windows.Forms.ToolStripLabel();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.PostHeader = new DeOps.Interface.Views.WebBrowserEx();
            this.PostBody = new DeOps.Interface.Views.RichTextBoxEx();
            this.PostImageList = new System.Windows.Forms.ImageList(this.components);
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.PostView);
            this.splitContainer1.Panel1.Controls.Add(this.toolStrip1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(511, 426);
            this.splitContainer1.SplitterDistance = 190;
            this.splitContainer1.TabIndex = 0;
            // 
            // PostView
            // 
            this.PostView.BackColor = System.Drawing.SystemColors.Window;
            this.PostView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            toggleColumnHeader1.Hovered = false;
            toggleColumnHeader1.Image = null;
            toggleColumnHeader1.Index = 0;
            toggleColumnHeader1.Pressed = false;
            toggleColumnHeader1.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Spring;
            toggleColumnHeader1.Selected = false;
            toggleColumnHeader1.Text = "Subject";
            toggleColumnHeader1.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader1.Visible = true;
            toggleColumnHeader1.Width = 269;
            toggleColumnHeader2.Hovered = false;
            toggleColumnHeader2.Image = null;
            toggleColumnHeader2.Index = 0;
            toggleColumnHeader2.Pressed = false;
            toggleColumnHeader2.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader2.Selected = false;
            toggleColumnHeader2.Text = "Author";
            toggleColumnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader2.Visible = true;
            toggleColumnHeader2.Width = 100;
            toggleColumnHeader3.Hovered = false;
            toggleColumnHeader3.Image = null;
            toggleColumnHeader3.Index = 0;
            toggleColumnHeader3.Pressed = false;
            toggleColumnHeader3.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader3.Selected = false;
            toggleColumnHeader3.Text = "Date";
            toggleColumnHeader3.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader3.Visible = true;
            toggleColumnHeader3.Width = 140;
            this.PostView.Columns.AddRange(new DeOps.Interface.TLVex.ToggleColumnHeader[] {
            toggleColumnHeader1,
            toggleColumnHeader2,
            toggleColumnHeader3});
            this.PostView.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.PostView.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.PostView.DisableHorizontalScroll = true;
            this.PostView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PostView.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.PostView.HeaderMenu = null;
            this.PostView.ItemHeight = 20;
            this.PostView.ItemMenu = null;
            this.PostView.LabelEdit = false;
            this.PostView.Location = new System.Drawing.Point(0, 25);
            this.PostView.Margin = new System.Windows.Forms.Padding(0);
            this.PostView.Name = "PostView";
            this.PostView.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.PostView.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.PostView.Size = new System.Drawing.Size(511, 165);
            this.PostView.SmallImageList = null;
            this.PostView.StateImageList = null;
            this.PostView.TabIndex = 1;
            this.PostView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PostView_MouseClick);
            this.PostView.SelectedItemChanged += new System.EventHandler(this.PostView_SelectedItemChanged);
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ArchiveButton,
            this.RightSplitter,
            this.toolStripLabel4,
            this.PostButton,
            this.toolStripLabel1,
            this.RefreshButton,
            this.ButtonLow,
            this.toolStripLabel2,
            this.ButtonHigh,
            this.toolStripLabel3});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.toolStrip1.Size = new System.Drawing.Size(511, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // ArchiveButton
            // 
            this.ArchiveButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.ArchiveButton.CheckOnClick = true;
            this.ArchiveButton.Image = ((System.Drawing.Image)(resources.GetObject("ArchiveButton.Image")));
            this.ArchiveButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ArchiveButton.Name = "ArchiveButton";
            this.ArchiveButton.Size = new System.Drawing.Size(69, 22);
            this.ArchiveButton.Text = "Archived";
            this.ArchiveButton.Click += new System.EventHandler(this.ArchiveButton_Click);
            // 
            // RightSplitter
            // 
            this.RightSplitter.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.RightSplitter.Name = "RightSplitter";
            this.RightSplitter.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripLabel4
            // 
            this.toolStripLabel4.Name = "toolStripLabel4";
            this.toolStripLabel4.Size = new System.Drawing.Size(10, 22);
            this.toolStripLabel4.Text = " ";
            // 
            // PostButton
            // 
            this.PostButton.Image = ((System.Drawing.Image)(resources.GetObject("PostButton.Image")));
            this.PostButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.PostButton.Name = "PostButton";
            this.PostButton.Size = new System.Drawing.Size(48, 22);
            this.PostButton.Text = "Post";
            this.PostButton.Click += new System.EventHandler(this.PostButton_Click);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(16, 22);
            this.toolStripLabel1.Text = "   ";
            // 
            // RefreshButton
            // 
            this.RefreshButton.Image = ((System.Drawing.Image)(resources.GetObject("RefreshButton.Image")));
            this.RefreshButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.RefreshButton.Name = "RefreshButton";
            this.RefreshButton.Size = new System.Drawing.Size(65, 22);
            this.RefreshButton.Text = "Refresh";
            this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
            // 
            // ButtonLow
            // 
            this.ButtonLow.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.ButtonLow.Checked = true;
            this.ButtonLow.CheckOnClick = true;
            this.ButtonLow.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ButtonLow.Image = ((System.Drawing.Image)(resources.GetObject("ButtonLow.Image")));
            this.ButtonLow.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ButtonLow.Name = "ButtonLow";
            this.ButtonLow.Size = new System.Drawing.Size(46, 22);
            this.ButtonLow.Text = "Low";
            this.ButtonLow.CheckedChanged += new System.EventHandler(this.ButtonLow_CheckedChanged);
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel2.AutoSize = false;
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(5, 22);
            // 
            // ButtonHigh
            // 
            this.ButtonHigh.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.ButtonHigh.Checked = true;
            this.ButtonHigh.CheckOnClick = true;
            this.ButtonHigh.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ButtonHigh.Image = ((System.Drawing.Image)(resources.GetObject("ButtonHigh.Image")));
            this.ButtonHigh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ButtonHigh.Name = "ButtonHigh";
            this.ButtonHigh.Size = new System.Drawing.Size(48, 22);
            this.ButtonHigh.Text = "High";
            this.ButtonHigh.CheckedChanged += new System.EventHandler(this.ButtonHigh_CheckedChanged);
            // 
            // toolStripLabel3
            // 
            this.toolStripLabel3.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel3.Name = "toolStripLabel3";
            this.toolStripLabel3.Size = new System.Drawing.Size(37, 22);
            this.toolStripLabel3.Text = "Show:";
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.PostHeader);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.PostBody);
            this.splitContainer2.Size = new System.Drawing.Size(511, 232);
            this.splitContainer2.SplitterDistance = 62;
            this.splitContainer2.SplitterWidth = 1;
            this.splitContainer2.TabIndex = 0;
            // 
            // PostHeader
            // 
            this.PostHeader.AllowWebBrowserDrop = false;
            this.PostHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PostHeader.IsWebBrowserContextMenuEnabled = false;
            this.PostHeader.Location = new System.Drawing.Point(0, 0);
            this.PostHeader.MinimumSize = new System.Drawing.Size(20, 20);
            this.PostHeader.Name = "PostHeader";
            this.PostHeader.ScriptErrorsSuppressed = true;
            this.PostHeader.ScrollBarsEnabled = false;
            this.PostHeader.Size = new System.Drawing.Size(511, 62);
            this.PostHeader.TabIndex = 0;
            this.PostHeader.WebBrowserShortcutsEnabled = false;
            this.PostHeader.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.PostHeader_Navigating);
            // 
            // PostBody
            // 
            this.PostBody.BackColor = System.Drawing.Color.White;
            this.PostBody.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.PostBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PostBody.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PostBody.Location = new System.Drawing.Point(0, 0);
            this.PostBody.Name = "PostBody";
            this.PostBody.ReadOnly = true;
            this.PostBody.Size = new System.Drawing.Size(511, 169);
            this.PostBody.TabIndex = 0;
            this.PostBody.Text = "";
            // 
            // PostImageList
            // 
            this.PostImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("PostImageList.ImageStream")));
            this.PostImageList.TransparentColor = System.Drawing.Color.Transparent;
            this.PostImageList.Images.SetKeyName(0, "post.png");
            this.PostImageList.Images.SetKeyName(1, "post_down.png");
            this.PostImageList.Images.SetKeyName(2, "post_up.png");
            this.PostImageList.Images.SetKeyName(3, "posthigh.png");
            this.PostImageList.Images.SetKeyName(4, "posthigh_down.png");
            this.PostImageList.Images.SetKeyName(5, "posthigh_up.png");
            this.PostImageList.Images.SetKeyName(6, "postlow.png");
            this.PostImageList.Images.SetKeyName(7, "postlow_down.png");
            this.PostImageList.Images.SetKeyName(8, "postlow_up.png");
            // 
            // BoardView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.Controls.Add(this.splitContainer1);
            this.Name = "BoardView";
            this.Size = new System.Drawing.Size(511, 426);
            this.Load += new System.EventHandler(this.BoardView_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private DeOps.Interface.Views.WebBrowserEx PostHeader;
        private DeOps.Interface.Views.RichTextBoxEx PostBody;
        private DeOps.Interface.TLVex.TreeListViewEx PostView;
        private System.Windows.Forms.ToolStripButton PostButton;
        private System.Windows.Forms.ToolStripButton ArchiveButton;
        private System.Windows.Forms.ToolStripSeparator RightSplitter;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripButton RefreshButton;
        private System.Windows.Forms.ImageList PostImageList;
        private System.Windows.Forms.ToolStripButton ButtonHigh;
        private System.Windows.Forms.ToolStripButton ButtonLow;
        private System.Windows.Forms.ToolStripLabel toolStripLabel3;
        private System.Windows.Forms.ToolStripLabel toolStripLabel4;
    }
}
