namespace DeOps.Components.Plan
{
    partial class ScheduleView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ScheduleView));
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader1 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader2 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            this.HoverTimer = new System.Windows.Forms.Timer(this.components);
            this.TopStrip = new System.Windows.Forms.ToolStrip();
            this.NewButton = new System.Windows.Forms.ToolStripButton();
            this.NowButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.GoalCombo = new System.Windows.Forms.ToolStripComboBox();
            this.DetailsButton = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.ChangesLabel = new System.Windows.Forms.Label();
            this.DiscardLink = new System.Windows.Forms.LinkLabel();
            this.SaveLink = new System.Windows.Forms.LinkLabel();
            this.LabelMinus = new System.Windows.Forms.Label();
            this.LabelPlus = new System.Windows.Forms.Label();
            this.ScheduleSlider = new DeOps.Components.Plan.DateSlider();
            this.ExtendedLabel = new System.Windows.Forms.Label();
            this.RangeLabel = new System.Windows.Forms.Label();
            this.DateRange = new System.Windows.Forms.TrackBar();
            this.PlanStructure = new DeOps.Interface.TLVex.TreeListViewEx();
            this.DetailsBrowser = new System.Windows.Forms.WebBrowser();
            this.TopStrip.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DateRange)).BeginInit();
            this.SuspendLayout();
            // 
            // HoverTimer
            // 
            this.HoverTimer.Enabled = true;
            this.HoverTimer.Interval = 500;
            this.HoverTimer.Tick += new System.EventHandler(this.HoverTimer_Tick);
            // 
            // TopStrip
            // 
            this.TopStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.TopStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewButton,
            this.NowButton,
            this.toolStripLabel2,
            this.toolStripLabel1,
            this.GoalCombo,
            this.DetailsButton});
            this.TopStrip.Location = new System.Drawing.Point(0, 0);
            this.TopStrip.Name = "TopStrip";
            this.TopStrip.Size = new System.Drawing.Size(505, 25);
            this.TopStrip.TabIndex = 23;
            // 
            // NewButton
            // 
            this.NewButton.Image = ((System.Drawing.Image)(resources.GetObject("NewButton.Image")));
            this.NewButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.NewButton.Name = "NewButton";
            this.NewButton.Size = new System.Drawing.Size(48, 22);
            this.NewButton.Text = "New";
            this.NewButton.Click += new System.EventHandler(this.NewButton_Click);
            // 
            // NowButton
            // 
            this.NowButton.Image = ((System.Drawing.Image)(resources.GetObject("NowButton.Image")));
            this.NowButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.NowButton.Name = "NowButton";
            this.NowButton.Size = new System.Drawing.Size(48, 22);
            this.NowButton.Text = "Now";
            this.NowButton.Click += new System.EventHandler(this.NowButton_Click);
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(19, 22);
            this.toolStripLabel2.Text = "    ";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(65, 22);
            this.toolStripLabel1.Text = "Show Goal";
            // 
            // GoalCombo
            // 
            this.GoalCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.GoalCombo.Name = "GoalCombo";
            this.GoalCombo.Size = new System.Drawing.Size(121, 25);
            this.GoalCombo.SelectedIndexChanged += new System.EventHandler(this.GoalCombo_SelectedIndexChanged);
            // 
            // DetailsButton
            // 
            this.DetailsButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.DetailsButton.CheckOnClick = true;
            this.DetailsButton.Image = ((System.Drawing.Image)(resources.GetObject("DetailsButton.Image")));
            this.DetailsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.DetailsButton.Name = "DetailsButton";
            this.DetailsButton.Size = new System.Drawing.Size(59, 22);
            this.DetailsButton.Text = "Details";
            this.DetailsButton.Click += new System.EventHandler(this.DetailsButton_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.splitContainer1.Panel1.Controls.Add(this.ChangesLabel);
            this.splitContainer1.Panel1.Controls.Add(this.DiscardLink);
            this.splitContainer1.Panel1.Controls.Add(this.SaveLink);
            this.splitContainer1.Panel1.Controls.Add(this.LabelMinus);
            this.splitContainer1.Panel1.Controls.Add(this.LabelPlus);
            this.splitContainer1.Panel1.Controls.Add(this.ScheduleSlider);
            this.splitContainer1.Panel1.Controls.Add(this.ExtendedLabel);
            this.splitContainer1.Panel1.Controls.Add(this.RangeLabel);
            this.splitContainer1.Panel1.Controls.Add(this.DateRange);
            this.splitContainer1.Panel1.Controls.Add(this.PlanStructure);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.DetailsBrowser);
            this.splitContainer1.Size = new System.Drawing.Size(505, 221);
            this.splitContainer1.SplitterDistance = 353;
            this.splitContainer1.TabIndex = 24;
            // 
            // ChangesLabel
            // 
            this.ChangesLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ChangesLabel.AutoSize = true;
            this.ChangesLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ChangesLabel.Location = new System.Drawing.Point(203, 203);
            this.ChangesLabel.Name = "ChangesLabel";
            this.ChangesLabel.Size = new System.Drawing.Size(56, 13);
            this.ChangesLabel.TabIndex = 30;
            this.ChangesLabel.Text = "Changes";
            this.ChangesLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
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
            this.DiscardLink.Location = new System.Drawing.Point(299, 203);
            this.DiscardLink.Name = "DiscardLink";
            this.DiscardLink.Size = new System.Drawing.Size(43, 13);
            this.DiscardLink.TabIndex = 29;
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
            this.SaveLink.Location = new System.Drawing.Point(261, 203);
            this.SaveLink.Name = "SaveLink";
            this.SaveLink.Size = new System.Drawing.Size(32, 13);
            this.SaveLink.TabIndex = 28;
            this.SaveLink.TabStop = true;
            this.SaveLink.Text = "Save";
            this.SaveLink.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.SaveLink.Visible = false;
            this.SaveLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SaveLink_LinkClicked);
            // 
            // LabelMinus
            // 
            this.LabelMinus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelMinus.Location = new System.Drawing.Point(47, 28);
            this.LabelMinus.Name = "LabelMinus";
            this.LabelMinus.Size = new System.Drawing.Size(13, 13);
            this.LabelMinus.TabIndex = 27;
            this.LabelMinus.Text = "-";
            // 
            // LabelPlus
            // 
            this.LabelPlus.AutoSize = true;
            this.LabelPlus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelPlus.Location = new System.Drawing.Point(145, 28);
            this.LabelPlus.Name = "LabelPlus";
            this.LabelPlus.Size = new System.Drawing.Size(14, 13);
            this.LabelPlus.TabIndex = 26;
            this.LabelPlus.Text = "+";
            // 
            // ScheduleSlider
            // 
            this.ScheduleSlider.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ScheduleSlider.BackColor = System.Drawing.SystemColors.ControlLight;
            this.ScheduleSlider.Location = new System.Drawing.Point(180, 23);
            this.ScheduleSlider.Name = "ScheduleSlider";
            this.ScheduleSlider.Size = new System.Drawing.Size(172, 27);
            this.ScheduleSlider.TabIndex = 25;
            // 
            // ExtendedLabel
            // 
            this.ExtendedLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ExtendedLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ExtendedLabel.Location = new System.Drawing.Point(218, 7);
            this.ExtendedLabel.Name = "ExtendedLabel";
            this.ExtendedLabel.Size = new System.Drawing.Size(106, 13);
            this.ExtendedLabel.TabIndex = 24;
            this.ExtendedLabel.Text = "Extended Label";
            this.ExtendedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // RangeLabel
            // 
            this.RangeLabel.AutoSize = true;
            this.RangeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RangeLabel.Location = new System.Drawing.Point(3, 28);
            this.RangeLabel.Name = "RangeLabel";
            this.RangeLabel.Size = new System.Drawing.Size(38, 13);
            this.RangeLabel.TabIndex = 23;
            this.RangeLabel.Text = "Zoom";
            // 
            // DateRange
            // 
            this.DateRange.AutoSize = false;
            this.DateRange.Location = new System.Drawing.Point(55, 29);
            this.DateRange.Maximum = 140;
            this.DateRange.Minimum = 14;
            this.DateRange.Name = "DateRange";
            this.DateRange.Size = new System.Drawing.Size(91, 16);
            this.DateRange.TabIndex = 22;
            this.DateRange.TickStyle = System.Windows.Forms.TickStyle.None;
            this.DateRange.Value = 80;
            this.DateRange.Scroll += new System.EventHandler(this.DateRange_Scroll);
            // 
            // PlanStructure
            // 
            this.PlanStructure.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.PlanStructure.BackColor = System.Drawing.SystemColors.Window;
            this.PlanStructure.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            toggleColumnHeader1.Hovered = false;
            toggleColumnHeader1.Image = null;
            toggleColumnHeader1.Index = 0;
            toggleColumnHeader1.Pressed = false;
            toggleColumnHeader1.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader1.Selected = false;
            toggleColumnHeader1.Text = "Structure";
            toggleColumnHeader1.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader1.Visible = true;
            toggleColumnHeader1.Width = 180;
            toggleColumnHeader2.Hovered = false;
            toggleColumnHeader2.Image = null;
            toggleColumnHeader2.Index = 0;
            toggleColumnHeader2.Pressed = false;
            toggleColumnHeader2.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Spring;
            toggleColumnHeader2.Selected = false;
            toggleColumnHeader2.Text = "Items";
            toggleColumnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader2.Visible = true;
            toggleColumnHeader2.Width = 171;
            this.PlanStructure.Columns.AddRange(new DeOps.Interface.TLVex.ToggleColumnHeader[] {
            toggleColumnHeader1,
            toggleColumnHeader2});
            this.PlanStructure.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.PlanStructure.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.PlanStructure.DisableHorizontalScroll = true;
            this.PlanStructure.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.PlanStructure.HeaderMenu = null;
            this.PlanStructure.ItemHeight = 40;
            this.PlanStructure.ItemMenu = null;
            this.PlanStructure.LabelEdit = false;
            this.PlanStructure.Location = new System.Drawing.Point(0, 50);
            this.PlanStructure.Name = "PlanStructure";
            this.PlanStructure.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.PlanStructure.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.PlanStructure.ShowLines = true;
            this.PlanStructure.Size = new System.Drawing.Size(353, 171);
            this.PlanStructure.SmallImageList = null;
            this.PlanStructure.StateImageList = null;
            this.PlanStructure.TabIndex = 21;
            this.PlanStructure.Enter += new System.EventHandler(this.PlanStructure_Enter);
            this.PlanStructure.SelectedItemChanged += new System.EventHandler(this.PlanStructure_SelectedItemChanged);
            this.PlanStructure.Leave += new System.EventHandler(this.PlanStructure_Leave);
            // 
            // DetailsBrowser
            // 
            this.DetailsBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DetailsBrowser.Location = new System.Drawing.Point(0, 0);
            this.DetailsBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.DetailsBrowser.Name = "DetailsBrowser";
            this.DetailsBrowser.ScriptErrorsSuppressed = true;
            this.DetailsBrowser.Size = new System.Drawing.Size(148, 221);
            this.DetailsBrowser.TabIndex = 0;
            // 
            // ScheduleView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.TopStrip);
            this.DoubleBuffered = true;
            this.Name = "ScheduleView";
            this.Size = new System.Drawing.Size(505, 246);
            this.Load += new System.EventHandler(this.ScheduleView_Load);
            this.TopStrip.ResumeLayout(false);
            this.TopStrip.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.DateRange)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Timer HoverTimer;
        private System.Windows.Forms.ToolStrip TopStrip;
        private System.Windows.Forms.ToolStripButton NewButton;
        private System.Windows.Forms.ToolStripButton NowButton;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripComboBox GoalCombo;
        private System.Windows.Forms.ToolStripButton DetailsButton;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Label ChangesLabel;
        private System.Windows.Forms.LinkLabel DiscardLink;
        private System.Windows.Forms.LinkLabel SaveLink;
        private System.Windows.Forms.Label LabelMinus;
        private System.Windows.Forms.Label LabelPlus;
        internal DateSlider ScheduleSlider;
        internal System.Windows.Forms.Label ExtendedLabel;
        private System.Windows.Forms.Label RangeLabel;
        private System.Windows.Forms.TrackBar DateRange;
        internal DeOps.Interface.TLVex.TreeListViewEx PlanStructure;
        private System.Windows.Forms.WebBrowser DetailsBrowser;
    }
}