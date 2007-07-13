namespace DeOps.Components.Storage
{
    partial class StorageView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StorageView));
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader1 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader2 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader3 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.FoldersButton = new System.Windows.Forms.ToolStripButton();
            this.divLabel = new System.Windows.Forms.ToolStripLabel();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.DiffCombo = new System.Windows.Forms.ToolStripComboBox();
            this.GhostsButton = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.FolderTreeView = new DeOps.Interface.TLVex.TreeListViewEx();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.FileListView = new DeOps.Interface.TLVex.ContainerListViewEx();
            this.ChangesLabel = new System.Windows.Forms.Label();
            this.DiscardLink = new System.Windows.Forms.LinkLabel();
            this.SaveLink = new System.Windows.Forms.LinkLabel();
            this.SecondTimer = new System.Windows.Forms.Timer(this.components);
            this.RescanLabel = new System.Windows.Forms.Label();
            SelectedInfo = new InfoPanel();
            this.toolStrip1.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.FoldersButton,
            this.divLabel,
            this.toolStripLabel2,
            this.DiffCombo,
            this.GhostsButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(523, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(10, 22);
            this.toolStripLabel1.Text = " ";
            // 
            // FoldersButton
            // 
            this.FoldersButton.CheckOnClick = true;
            this.FoldersButton.Image = global::DeOps.Properties.Resources.folder;
            this.FoldersButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.FoldersButton.Name = "FoldersButton";
            this.FoldersButton.Size = new System.Drawing.Size(62, 22);
            this.FoldersButton.Text = "Folders";
            this.FoldersButton.Click += new System.EventHandler(this.FoldersButton_Click);
            // 
            // divLabel
            // 
            this.divLabel.Name = "divLabel";
            this.divLabel.Size = new System.Drawing.Size(22, 22);
            this.divLabel.Text = "     ";
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(63, 22);
            this.toolStripLabel2.Text = "Compare to";
            // 
            // DiffCombo
            // 
            this.DiffCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DiffCombo.Name = "DiffCombo";
            this.DiffCombo.Size = new System.Drawing.Size(121, 25);
            this.DiffCombo.SelectedIndexChanged += new System.EventHandler(this.DiffCombo_SelectedIndexChanged);
            // 
            // GhostsButton
            // 
            this.GhostsButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.GhostsButton.CheckOnClick = true;
            this.GhostsButton.Image = ((System.Drawing.Image)(resources.GetObject("GhostsButton.Image")));
            this.GhostsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.GhostsButton.Name = "GhostsButton";
            this.GhostsButton.Size = new System.Drawing.Size(60, 22);
            this.GhostsButton.Text = "Ghosts";
            this.GhostsButton.CheckedChanged += new System.EventHandler(this.GhostsButton_CheckedChanged);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.FolderTreeView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(523, 388);
            this.splitContainer1.SplitterDistance = 159;
            this.splitContainer1.TabIndex = 2;
            // 
            // FolderTreeView
            // 
            this.FolderTreeView.BackColor = System.Drawing.SystemColors.Window;
            this.FolderTreeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.FolderTreeView.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.FolderTreeView.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.FolderTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FolderTreeView.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.FolderTreeView.HeaderMenu = null;
            this.FolderTreeView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.FolderTreeView.ItemHeight = 20;
            this.FolderTreeView.ItemMenu = null;
            this.FolderTreeView.LabelEdit = false;
            this.FolderTreeView.Location = new System.Drawing.Point(0, 0);
            this.FolderTreeView.Name = "FolderTreeView";
            this.FolderTreeView.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.FolderTreeView.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.FolderTreeView.ShowLines = true;
            this.FolderTreeView.Size = new System.Drawing.Size(159, 388);
            this.FolderTreeView.SmallImageList = null;
            this.FolderTreeView.StateImageList = null;
            this.FolderTreeView.TabIndex = 0;
            this.FolderTreeView.Text = "FolderTeeView";
            this.FolderTreeView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.FolderTreeView_MouseClick);
            this.FolderTreeView.SelectedItemChanged += new System.EventHandler(this.FolderTreeView_SelectedItemChanged);
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.FileListView);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.SelectedInfo);
            this.splitContainer2.Size = new System.Drawing.Size(360, 388);
            this.splitContainer2.SplitterDistance = 280;
            this.splitContainer2.TabIndex = 0;
            // 
            // FileListView
            // 
            this.FileListView.AllowDrop = true;
            this.FileListView.BackColor = System.Drawing.SystemColors.Window;
            this.FileListView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            toggleColumnHeader1.Hovered = false;
            toggleColumnHeader1.Image = null;
            toggleColumnHeader1.Index = 0;
            toggleColumnHeader1.Pressed = false;
            toggleColumnHeader1.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Spring;
            toggleColumnHeader1.Selected = false;
            toggleColumnHeader1.Text = "Name";
            toggleColumnHeader1.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader1.Visible = true;
            toggleColumnHeader1.Width = 168;
            toggleColumnHeader2.Hovered = false;
            toggleColumnHeader2.Image = null;
            toggleColumnHeader2.Index = 0;
            toggleColumnHeader2.Pressed = false;
            toggleColumnHeader2.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader2.Selected = false;
            toggleColumnHeader2.Text = "Size";
            toggleColumnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            toggleColumnHeader2.Visible = true;
            toggleColumnHeader2.Width = 70;
            toggleColumnHeader3.Hovered = false;
            toggleColumnHeader3.Image = null;
            toggleColumnHeader3.Index = 0;
            toggleColumnHeader3.Pressed = false;
            toggleColumnHeader3.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader3.Selected = false;
            toggleColumnHeader3.Text = "Date Modified";
            toggleColumnHeader3.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader3.Visible = true;
            toggleColumnHeader3.Width = 120;
            this.FileListView.Columns.AddRange(new DeOps.Interface.TLVex.ToggleColumnHeader[] {
            toggleColumnHeader1,
            toggleColumnHeader2,
            toggleColumnHeader3});
            this.FileListView.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.FileListView.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.FileListView.DisableHorizontalScroll = true;
            this.FileListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FileListView.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.FileListView.HeaderMenu = null;
            this.FileListView.ItemMenu = null;
            this.FileListView.LabelEdit = false;
            this.FileListView.Location = new System.Drawing.Point(0, 0);
            this.FileListView.Name = "FileListView";
            this.FileListView.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.FileListView.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.FileListView.Size = new System.Drawing.Size(360, 280);
            this.FileListView.SmallImageList = null;
            this.FileListView.StateImageList = null;
            this.FileListView.TabIndex = 0;
            this.FileListView.Text = "FileListView";
            this.FileListView.QueryContinueDrag += new System.Windows.Forms.QueryContinueDragEventHandler(this.FileListView_QueryContinueDrag);
            this.FileListView.DragOver += new System.Windows.Forms.DragEventHandler(this.FileListView_DragOver);
            this.FileListView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.FileListView_MouseDown);
            this.FileListView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.FileListView_MouseMove);
            this.FileListView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.FileListView_MouseClick);
            this.FileListView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.FileListView_MouseDoubleClick);
            this.FileListView.DragDrop += new System.Windows.Forms.DragEventHandler(this.FileListView_DragDrop);
            this.FileListView.DragEnter += new System.Windows.Forms.DragEventHandler(this.FileListView_DragEnter);
            this.FileListView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.FileListView_MouseUp);
            this.FileListView.GiveFeedback += new System.Windows.Forms.GiveFeedbackEventHandler(this.FileListView_GiveFeedback);
            this.FileListView.SelectedIndexChanged += new System.EventHandler(this.FileListView_SelectedIndexChanged);
            this.FileListView.DragLeave += new System.EventHandler(this.FileListView_DragLeave);
            // 
            // SelectedInfo
            // 
            this.SelectedInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SelectedInfo.Location = new System.Drawing.Point(0, 0);
            this.SelectedInfo.Name = "SelectedInfo";
            this.SelectedInfo.Size = new System.Drawing.Size(360, 104);
            this.SelectedInfo.TabIndex = 0;
            // 
            // ChangesLabel
            // 
            this.ChangesLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ChangesLabel.AutoSize = true;
            this.ChangesLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ChangesLabel.Location = new System.Drawing.Point(376, 416);
            this.ChangesLabel.Name = "ChangesLabel";
            this.ChangesLabel.Size = new System.Drawing.Size(56, 13);
            this.ChangesLabel.TabIndex = 25;
            this.ChangesLabel.Text = "Changes";
            this.ChangesLabel.Visible = false;
            // 
            // DiscardLink
            // 
            this.DiscardLink.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.DiscardLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DiscardLink.AutoSize = true;
            this.DiscardLink.BackColor = System.Drawing.Color.Red;
            this.DiscardLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.DiscardLink.LinkColor = System.Drawing.Color.White;
            this.DiscardLink.Location = new System.Drawing.Point(476, 416);
            this.DiscardLink.Name = "DiscardLink";
            this.DiscardLink.Size = new System.Drawing.Size(43, 13);
            this.DiscardLink.TabIndex = 24;
            this.DiscardLink.TabStop = true;
            this.DiscardLink.Text = "Discard";
            this.DiscardLink.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.DiscardLink.Visible = false;
            this.DiscardLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.DiscardLink_LinkClicked);
            // 
            // SaveLink
            // 
            this.SaveLink.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.SaveLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveLink.AutoSize = true;
            this.SaveLink.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.SaveLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.SaveLink.LinkColor = System.Drawing.Color.White;
            this.SaveLink.Location = new System.Drawing.Point(438, 416);
            this.SaveLink.Name = "SaveLink";
            this.SaveLink.Size = new System.Drawing.Size(32, 13);
            this.SaveLink.TabIndex = 23;
            this.SaveLink.TabStop = true;
            this.SaveLink.Text = "Save";
            this.SaveLink.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.SaveLink.Visible = false;
            this.SaveLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SaveLink_LinkClicked);
            // 
            // SecondTimer
            // 
            this.SecondTimer.Enabled = true;
            this.SecondTimer.Interval = 1000;
            this.SecondTimer.Tick += new System.EventHandler(this.SecondTimer_Tick);
            // 
            // RescanLabel
            // 
            this.RescanLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.RescanLabel.AutoSize = true;
            this.RescanLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RescanLabel.Location = new System.Drawing.Point(3, 416);
            this.RescanLabel.Name = "RescanLabel";
            this.RescanLabel.Size = new System.Drawing.Size(86, 13);
            this.RescanLabel.TabIndex = 26;
            this.RescanLabel.Text = "Rescanning...";
            this.RescanLabel.Visible = false;
            // 
            // StorageView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.RescanLabel);
            this.Controls.Add(this.ChangesLabel);
            this.Controls.Add(this.DiscardLink);
            this.Controls.Add(this.SaveLink);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "StorageView";
            this.Size = new System.Drawing.Size(523, 432);
            this.Load += new System.EventHandler(this.StorageView_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel divLabel;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private DeOps.Interface.TLVex.TreeListViewEx FolderTreeView;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private DeOps.Interface.TLVex.ContainerListViewEx FileListView;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripComboBox DiffCombo;
        internal System.Windows.Forms.ToolStripButton GhostsButton;
        private System.Windows.Forms.Label ChangesLabel;
        private System.Windows.Forms.LinkLabel DiscardLink;
        private System.Windows.Forms.LinkLabel SaveLink;
        private System.Windows.Forms.Timer SecondTimer;
        private System.Windows.Forms.ToolStripButton FoldersButton;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private InfoPanel SelectedInfo;
        private System.Windows.Forms.Label RescanLabel;
    }
}
