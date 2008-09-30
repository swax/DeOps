namespace RiseOp.Interface
{
    partial class IMForm
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
            RiseOp.Interface.TLVex.ToggleColumnHeader toggleColumnHeader1 = new RiseOp.Interface.TLVex.ToggleColumnHeader();
            RiseOp.Interface.TLVex.ToggleColumnHeader toggleColumnHeader2 = new RiseOp.Interface.TLVex.ToggleColumnHeader();
            RiseOp.Interface.TLVex.ToggleColumnHeader toggleColumnHeader3 = new RiseOp.Interface.TLVex.ToggleColumnHeader();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IMForm));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.TopStrip = new System.Windows.Forms.ToolStrip();
            this.AddButton = new System.Windows.Forms.ToolStripButton();
            this.MinButton = new System.Windows.Forms.ToolStripButton();
            BuddyList = new RiseOp.Services.Buddy.BuddyView();
            SelectionInfo = new StatusPanel();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.TopStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.BuddyList);
            this.splitContainer1.Panel1.Controls.Add(this.TopStrip);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.SelectionInfo);
            this.splitContainer1.Size = new System.Drawing.Size(190, 426);
            this.splitContainer1.SplitterDistance = 299;
            this.splitContainer1.TabIndex = 0;
            // 
            // BuddyList
            // 
            this.BuddyList.AllowDrop = true;
            this.BuddyList.BackColor = System.Drawing.SystemColors.Window;
            toggleColumnHeader1.Hovered = false;
            toggleColumnHeader1.Image = null;
            toggleColumnHeader1.Index = 0;
            toggleColumnHeader1.Pressed = false;
            toggleColumnHeader1.ScaleStyle = RiseOp.Interface.TLVex.ColumnScaleStyle.Spring;
            toggleColumnHeader1.Selected = false;
            toggleColumnHeader1.Text = "";
            toggleColumnHeader1.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader1.Visible = true;
            toggleColumnHeader1.Width = 62;
            toggleColumnHeader2.Hovered = false;
            toggleColumnHeader2.Image = null;
            toggleColumnHeader2.Index = 0;
            toggleColumnHeader2.Pressed = false;
            toggleColumnHeader2.ScaleStyle = RiseOp.Interface.TLVex.ColumnScaleStyle.Spring;
            toggleColumnHeader2.Selected = false;
            toggleColumnHeader2.Text = "";
            toggleColumnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader2.Visible = true;
            toggleColumnHeader2.Width = 62;
            toggleColumnHeader3.Hovered = false;
            toggleColumnHeader3.Image = null;
            toggleColumnHeader3.Index = 0;
            toggleColumnHeader3.Pressed = false;
            toggleColumnHeader3.ScaleStyle = RiseOp.Interface.TLVex.ColumnScaleStyle.Spring;
            toggleColumnHeader3.Selected = false;
            toggleColumnHeader3.Text = "";
            toggleColumnHeader3.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader3.Visible = true;
            toggleColumnHeader3.Width = 62;
            this.BuddyList.Columns.AddRange(new RiseOp.Interface.TLVex.ToggleColumnHeader[] {
            toggleColumnHeader1,
            toggleColumnHeader2,
            toggleColumnHeader3});
            this.BuddyList.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.BuddyList.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.BuddyList.DisableHorizontalScroll = true;
            this.BuddyList.Dock = System.Windows.Forms.DockStyle.Fill;
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
            this.BuddyList.Size = new System.Drawing.Size(190, 274);
            this.BuddyList.SmallImageList = null;
            this.BuddyList.StateImageList = null;
            this.BuddyList.TabIndex = 1;
            this.BuddyList.Text = "buddyView1";
            this.BuddyList.SelectedIndexChanged += new System.EventHandler(this.BuddyList_SelectedIndexChanged);
            // 
            // TopStrip
            // 
            this.TopStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.TopStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.AddButton,
            this.MinButton});
            this.TopStrip.Location = new System.Drawing.Point(0, 0);
            this.TopStrip.Name = "TopStrip";
            this.TopStrip.Size = new System.Drawing.Size(190, 25);
            this.TopStrip.TabIndex = 0;
            this.TopStrip.Text = "toolStrip1";
            // 
            // AddButton
            // 
            this.AddButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.AddButton.Image = ((System.Drawing.Image)(resources.GetObject("AddButton.Image")));
            this.AddButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(23, 22);
            this.AddButton.Text = "Add Buddy";
            this.AddButton.ToolTipText = "Add Buddy";
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // MinButton
            // 
            this.MinButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.MinButton.Image = ((System.Drawing.Image)(resources.GetObject("MinButton.Image")));
            this.MinButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.MinButton.Name = "MinButton";
            this.MinButton.Size = new System.Drawing.Size(23, 22);
            this.MinButton.Text = "Min to Tray";
            this.MinButton.Click += new System.EventHandler(this.MinButton_Click);
            // 
            // SelectionInfo
            // 
            this.SelectionInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SelectionInfo.Location = new System.Drawing.Point(0, 0);
            this.SelectionInfo.Name = "SelectionInfo";
            this.SelectionInfo.Size = new System.Drawing.Size(190, 123);
            this.SelectionInfo.TabIndex = 0;
            // 
            // IMForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(190, 426);
            this.Controls.Add(this.splitContainer1);
            this.Name = "IMForm";
            this.Text = "RiseOp IM";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.IMForm_FormClosing);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.TopStrip.ResumeLayout(false);
            this.TopStrip.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private RiseOp.Services.Buddy.BuddyView BuddyList;
        private System.Windows.Forms.ToolStrip TopStrip;
        private StatusPanel SelectionInfo;
        private System.Windows.Forms.ToolStripButton AddButton;
        private System.Windows.Forms.ToolStripButton MinButton;
    }
}