namespace DeOps.Services.Plan
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
            this.MainSplit = new System.Windows.Forms.SplitContainer();
            this.DiscardButton = new DeOps.Interface.Views.ImageButton();
            this.SaveButton = new DeOps.Interface.Views.ImageButton();
            this.LabelMinus = new System.Windows.Forms.Label();
            this.LabelPlus = new System.Windows.Forms.Label();
            this.ScheduleSlider = new DeOps.Services.Plan.DateSlider();
            this.ExtendedLabel = new System.Windows.Forms.Label();
            this.RangeLabel = new System.Windows.Forms.Label();
            this.DateRange = new System.Windows.Forms.TrackBar();
            this.PlanStructure = new DeOps.Interface.TLVex.TreeListViewEx();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.DetailsBrowser = new DeOps.Interface.Views.WebBrowserEx();
            this.TopStrip.SuspendLayout();
            this.MainSplit.Panel1.SuspendLayout();
            this.MainSplit.Panel2.SuspendLayout();
            this.MainSplit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DiscardButton)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SaveButton)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DateRange)).BeginInit();
            this.groupBox1.SuspendLayout();
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
            this.MainSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainSplit.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.MainSplit.Location = new System.Drawing.Point(0, 25);
            this.MainSplit.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.MainSplit.Panel1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.MainSplit.Panel1.Controls.Add(this.DiscardButton);
            this.MainSplit.Panel1.Controls.Add(this.SaveButton);
            this.MainSplit.Panel1.Controls.Add(this.LabelMinus);
            this.MainSplit.Panel1.Controls.Add(this.LabelPlus);
            this.MainSplit.Panel1.Controls.Add(this.ScheduleSlider);
            this.MainSplit.Panel1.Controls.Add(this.ExtendedLabel);
            this.MainSplit.Panel1.Controls.Add(this.RangeLabel);
            this.MainSplit.Panel1.Controls.Add(this.DateRange);
            this.MainSplit.Panel1.Controls.Add(this.PlanStructure);
            // 
            // splitContainer1.Panel2
            // 
            this.MainSplit.Panel2.Controls.Add(this.groupBox1);
            this.MainSplit.Size = new System.Drawing.Size(505, 221);
            this.MainSplit.SplitterDistance = 353;
            this.MainSplit.TabIndex = 24;
            // 
            // DiscardButton
            // 
            this.DiscardButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DiscardButton.ButtonDown = global::DeOps.Properties.Resources.discard_down;
            this.DiscardButton.ButtonHot = global::DeOps.Properties.Resources.discard_hot;
            this.DiscardButton.ButtonNormal = global::DeOps.Properties.Resources.discard_norm;
            this.DiscardButton.Image = global::DeOps.Properties.Resources.discard_norm;
            this.DiscardButton.Location = new System.Drawing.Point(269, 199);
            this.DiscardButton.Name = "DiscardButton";
            this.DiscardButton.Size = new System.Drawing.Size(64, 19);
            this.DiscardButton.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.DiscardButton.TabIndex = 31;
            this.DiscardButton.TabStop = false;
            this.DiscardButton.Visible = false;
            this.DiscardButton.Click += new System.EventHandler(this.DiscardButton_Click);
            // 
            // SaveButton
            // 
            this.SaveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveButton.ButtonDown = global::DeOps.Properties.Resources.save_down;
            this.SaveButton.ButtonHot = global::DeOps.Properties.Resources.save_hot;
            this.SaveButton.ButtonNormal = global::DeOps.Properties.Resources.save_norm;
            this.SaveButton.Image = global::DeOps.Properties.Resources.save_norm;
            this.SaveButton.Location = new System.Drawing.Point(199, 199);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(64, 19);
            this.SaveButton.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.SaveButton.TabIndex = 30;
            this.SaveButton.TabStop = false;
            this.SaveButton.Visible = false;
            this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
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
            this.ScheduleSlider.BackColor = System.Drawing.Color.WhiteSmoke;
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
            this.PlanStructure.SelectedItemChanged += new System.EventHandler(this.PlanStructure_SelectedItemChanged);
            this.PlanStructure.Leave += new System.EventHandler(this.PlanStructure_Leave);
            this.PlanStructure.Enter += new System.EventHandler(this.PlanStructure_Enter);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.DetailsBrowser);
            this.groupBox1.Location = new System.Drawing.Point(3, 50);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(142, 168);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Details";
            // 
            // DetailsBrowser
            // 
            this.DetailsBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DetailsBrowser.Location = new System.Drawing.Point(3, 16);
            this.DetailsBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.DetailsBrowser.Name = "DetailsBrowser";
            this.DetailsBrowser.ScriptErrorsSuppressed = true;
            this.DetailsBrowser.Size = new System.Drawing.Size(136, 149);
            this.DetailsBrowser.TabIndex = 0;
            // 
            // ScheduleView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.Controls.Add(this.MainSplit);
            this.Controls.Add(this.TopStrip);
            this.DoubleBuffered = true;
            this.Name = "ScheduleView";
            this.Size = new System.Drawing.Size(505, 246);
            this.Load += new System.EventHandler(this.ScheduleView_Load);
            this.TopStrip.ResumeLayout(false);
            this.TopStrip.PerformLayout();
            this.MainSplit.Panel1.ResumeLayout(false);
            this.MainSplit.Panel1.PerformLayout();
            this.MainSplit.Panel2.ResumeLayout(false);
            this.MainSplit.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.DiscardButton)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SaveButton)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DateRange)).EndInit();
            this.groupBox1.ResumeLayout(false);
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
        private System.Windows.Forms.SplitContainer MainSplit;
        private System.Windows.Forms.Label LabelMinus;
        private System.Windows.Forms.Label LabelPlus;
        internal DateSlider ScheduleSlider;
        internal System.Windows.Forms.Label ExtendedLabel;
        private System.Windows.Forms.Label RangeLabel;
        private System.Windows.Forms.TrackBar DateRange;
        internal DeOps.Interface.TLVex.TreeListViewEx PlanStructure;
        private DeOps.Interface.Views.WebBrowserEx DetailsBrowser;
        private DeOps.Interface.Views.ImageButton SaveButton;
        private DeOps.Interface.Views.ImageButton DiscardButton;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}