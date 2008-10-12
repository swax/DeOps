namespace RiseOp.Services.Sharing
{
    partial class SharingView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SharingView));
            RiseOp.Interface.TLVex.ToggleColumnHeader toggleColumnHeader1 = new RiseOp.Interface.TLVex.ToggleColumnHeader();
            RiseOp.Interface.TLVex.ToggleColumnHeader toggleColumnHeader2 = new RiseOp.Interface.TLVex.ToggleColumnHeader();
            RiseOp.Interface.TLVex.ToggleColumnHeader toggleColumnHeader3 = new RiseOp.Interface.TLVex.ToggleColumnHeader();
            this.TopStrip = new System.Windows.Forms.ToolStrip();
            this.DownloadButton = new System.Windows.Forms.ToolStripButton();
            this.ShareButton = new System.Windows.Forms.ToolStripButton();
            this.StatusLabel = new System.Windows.Forms.ToolStripLabel();
            this.RefreshButton = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.SharedFiles = new RiseOp.Interface.TLVex.ContainerListViewEx();
            this.shareInfoPanel1 = new RiseOp.Services.Sharing.ShareInfoPanel();
            this.SecondTimer = new System.Windows.Forms.Timer(this.components);
            this.TopStrip.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // TopStrip
            // 
            this.TopStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.TopStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DownloadButton,
            this.ShareButton,
            this.StatusLabel,
            this.RefreshButton});
            this.TopStrip.Location = new System.Drawing.Point(0, 0);
            this.TopStrip.Name = "TopStrip";
            this.TopStrip.Size = new System.Drawing.Size(512, 25);
            this.TopStrip.TabIndex = 0;
            // 
            // DownloadButton
            // 
            this.DownloadButton.Image = ((System.Drawing.Image)(resources.GetObject("DownloadButton.Image")));
            this.DownloadButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.DownloadButton.Name = "DownloadButton";
            this.DownloadButton.Size = new System.Drawing.Size(93, 22);
            this.DownloadButton.Text = "Download File";
            this.DownloadButton.Click += new System.EventHandler(this.DownloadButton_Click);
            // 
            // ShareButton
            // 
            this.ShareButton.Image = ((System.Drawing.Image)(resources.GetObject("ShareButton.Image")));
            this.ShareButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ShareButton.Name = "ShareButton";
            this.ShareButton.Size = new System.Drawing.Size(74, 22);
            this.ShareButton.Text = "Share File";
            this.ShareButton.Click += new System.EventHandler(this.ShareButton_Click);
            // 
            // StatusLabel
            // 
            this.StatusLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(92, 22);
            this.StatusLabel.Text = "Remote Status";
            // 
            // RefreshButton
            // 
            this.RefreshButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.RefreshButton.Image = ((System.Drawing.Image)(resources.GetObject("RefreshButton.Image")));
            this.RefreshButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.RefreshButton.Name = "RefreshButton";
            this.RefreshButton.Size = new System.Drawing.Size(65, 22);
            this.RefreshButton.Text = "Refresh";
            this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.SharedFiles);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.shareInfoPanel1);
            this.splitContainer1.Size = new System.Drawing.Size(512, 272);
            this.splitContainer1.SplitterDistance = 170;
            this.splitContainer1.TabIndex = 1;
            // 
            // SharedFiles
            // 
            this.SharedFiles.AllowDrop = true;
            this.SharedFiles.BackColor = System.Drawing.SystemColors.Window;
            toggleColumnHeader1.Hovered = false;
            toggleColumnHeader1.Image = null;
            toggleColumnHeader1.Index = 0;
            toggleColumnHeader1.Pressed = false;
            toggleColumnHeader1.ScaleStyle = RiseOp.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader1.Selected = false;
            toggleColumnHeader1.Text = "Name";
            toggleColumnHeader1.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader1.Visible = true;
            toggleColumnHeader1.Width = 175;
            toggleColumnHeader2.Hovered = false;
            toggleColumnHeader2.Image = null;
            toggleColumnHeader2.Index = 0;
            toggleColumnHeader2.Pressed = false;
            toggleColumnHeader2.ScaleStyle = RiseOp.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader2.Selected = false;
            toggleColumnHeader2.Text = "Size";
            toggleColumnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            toggleColumnHeader2.Visible = true;
            toggleColumnHeader2.Width = 75;
            toggleColumnHeader3.Hovered = false;
            toggleColumnHeader3.Image = null;
            toggleColumnHeader3.Index = 0;
            toggleColumnHeader3.Pressed = false;
            toggleColumnHeader3.ScaleStyle = RiseOp.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader3.Selected = false;
            toggleColumnHeader3.Text = "Status";
            toggleColumnHeader3.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader3.Visible = true;
            toggleColumnHeader3.Width = 200;
            this.SharedFiles.Columns.AddRange(new RiseOp.Interface.TLVex.ToggleColumnHeader[] {
            toggleColumnHeader1,
            toggleColumnHeader2,
            toggleColumnHeader3});
            this.SharedFiles.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.SharedFiles.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.SharedFiles.DisableHorizontalScroll = true;
            this.SharedFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SharedFiles.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.SharedFiles.HeaderMenu = null;
            this.SharedFiles.ItemMenu = null;
            this.SharedFiles.LabelEdit = false;
            this.SharedFiles.Location = new System.Drawing.Point(0, 0);
            this.SharedFiles.MultiSelect = true;
            this.SharedFiles.Name = "SharedFiles";
            this.SharedFiles.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.SharedFiles.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.SharedFiles.Size = new System.Drawing.Size(512, 170);
            this.SharedFiles.SmallImageList = null;
            this.SharedFiles.StateImageList = null;
            this.SharedFiles.TabIndex = 0;
            this.SharedFiles.MouseClick += new System.Windows.Forms.MouseEventHandler(this.SharedFiles_MouseClick);
            this.SharedFiles.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.SharedFiles_MouseDoubleClick);
            this.SharedFiles.DragDrop += new System.Windows.Forms.DragEventHandler(this.SharedFiles_DragDrop);
            this.SharedFiles.DragOver += new System.Windows.Forms.DragEventHandler(this.SharedFiles_DragOver);
            // 
            // shareInfoPanel1
            // 
            this.shareInfoPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.shareInfoPanel1.Location = new System.Drawing.Point(0, 0);
            this.shareInfoPanel1.Name = "shareInfoPanel1";
            this.shareInfoPanel1.Size = new System.Drawing.Size(512, 98);
            this.shareInfoPanel1.TabIndex = 0;
            // 
            // SecondTimer
            // 
            this.SecondTimer.Enabled = true;
            this.SecondTimer.Interval = 1000;
            this.SecondTimer.Tick += new System.EventHandler(this.SecondTimer_Tick);
            // 
            // SharingView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.TopStrip);
            this.Name = "SharingView";
            this.Size = new System.Drawing.Size(512, 297);
            this.TopStrip.ResumeLayout(false);
            this.TopStrip.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip TopStrip;
        private System.Windows.Forms.ToolStripButton DownloadButton;
        private System.Windows.Forms.ToolStripButton ShareButton;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private RiseOp.Interface.TLVex.ContainerListViewEx SharedFiles;
        private System.Windows.Forms.Timer SecondTimer;
        private ShareInfoPanel shareInfoPanel1;
        private System.Windows.Forms.ToolStripLabel StatusLabel;
        private System.Windows.Forms.ToolStripButton RefreshButton;
    }
}
