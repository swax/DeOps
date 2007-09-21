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
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader1 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader2 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            this.PlanStructure = new DeOps.Interface.TLVex.TreeListViewEx();
            this.NewItem = new System.Windows.Forms.LinkLabel();
            this.NowLink = new System.Windows.Forms.LinkLabel();
            this.RangeLabel = new System.Windows.Forms.Label();
            this.DateRange = new System.Windows.Forms.TrackBar();
            this.ExtendedLabel = new System.Windows.Forms.Label();
            this.ScheduleSlider = new DeOps.Components.Plan.DateSlider();
            this.HoverTimer = new System.Windows.Forms.Timer(this.components);
            this.LabelPlus = new System.Windows.Forms.Label();
            this.LabelMinus = new System.Windows.Forms.Label();
            this.SaveLink = new System.Windows.Forms.LinkLabel();
            this.DiscardLink = new System.Windows.Forms.LinkLabel();
            this.ChangesLabel = new System.Windows.Forms.Label();
            this.GoalsCombo = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.DateRange)).BeginInit();
            this.SuspendLayout();
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
            toggleColumnHeader1.Width = 200;
            toggleColumnHeader2.Hovered = false;
            toggleColumnHeader2.Image = null;
            toggleColumnHeader2.Index = 0;
            toggleColumnHeader2.Pressed = false;
            toggleColumnHeader2.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Spring;
            toggleColumnHeader2.Selected = false;
            toggleColumnHeader2.Text = "Items";
            toggleColumnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader2.Visible = true;
            toggleColumnHeader2.Width = 274;
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
            this.PlanStructure.Location = new System.Drawing.Point(0, 46);
            this.PlanStructure.Name = "PlanStructure";
            this.PlanStructure.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.PlanStructure.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.PlanStructure.ShowLines = true;
            this.PlanStructure.Size = new System.Drawing.Size(476, 238);
            this.PlanStructure.SmallImageList = null;
            this.PlanStructure.StateImageList = null;
            this.PlanStructure.TabIndex = 1;
            this.PlanStructure.SelectedItemChanged += new System.EventHandler(this.PlanStructure_SelectedItemChanged);
            this.PlanStructure.LostFocus += new System.EventHandler(this.PlanStructure_Leave);
            this.PlanStructure.GotFocus += new System.EventHandler(this.PlanStructure_Enter);
            // 
            // NewItem
            // 
            this.NewItem.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.NewItem.AutoSize = true;
            this.NewItem.Location = new System.Drawing.Point(3, 293);
            this.NewItem.Name = "NewItem";
            this.NewItem.Size = new System.Drawing.Size(59, 13);
            this.NewItem.TabIndex = 4;
            this.NewItem.TabStop = true;
            this.NewItem.Text = "New Block";
            this.NewItem.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.NewItem_LinkClicked);
            // 
            // NowLink
            // 
            this.NowLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.NowLink.AutoSize = true;
            this.NowLink.Location = new System.Drawing.Point(68, 293);
            this.NowLink.Name = "NowLink";
            this.NowLink.Size = new System.Drawing.Size(58, 13);
            this.NowLink.TabIndex = 5;
            this.NowLink.TabStop = true;
            this.NowLink.Text = "Go to Now";
            this.NowLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.NowLink_LinkClicked);
            // 
            // RangeLabel
            // 
            this.RangeLabel.AutoSize = true;
            this.RangeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RangeLabel.Location = new System.Drawing.Point(3, 28);
            this.RangeLabel.Name = "RangeLabel";
            this.RangeLabel.Size = new System.Drawing.Size(38, 13);
            this.RangeLabel.TabIndex = 10;
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
            this.DateRange.TabIndex = 9;
            this.DateRange.TickStyle = System.Windows.Forms.TickStyle.None;
            this.DateRange.Value = 80;
            this.DateRange.Scroll += new System.EventHandler(this.DateRange_Scroll);
            // 
            // ExtendedLabel
            // 
            this.ExtendedLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ExtendedLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ExtendedLabel.Location = new System.Drawing.Point(218, 3);
            this.ExtendedLabel.Name = "ExtendedLabel";
            this.ExtendedLabel.Size = new System.Drawing.Size(258, 13);
            this.ExtendedLabel.TabIndex = 11;
            this.ExtendedLabel.Text = "Extended Label";
            this.ExtendedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ScheduleSlider
            // 
            this.ScheduleSlider.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ScheduleSlider.Location = new System.Drawing.Point(218, 19);
            this.ScheduleSlider.Name = "ScheduleSlider";
            this.ScheduleSlider.Size = new System.Drawing.Size(258, 25);
            this.ScheduleSlider.TabIndex = 12;
            // 
            // HoverTimer
            // 
            this.HoverTimer.Enabled = true;
            this.HoverTimer.Interval = 500;
            this.HoverTimer.Tick += new System.EventHandler(this.HoverTimer_Tick);
            // 
            // LabelPlus
            // 
            this.LabelPlus.AutoSize = true;
            this.LabelPlus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelPlus.Location = new System.Drawing.Point(145, 28);
            this.LabelPlus.Name = "LabelPlus";
            this.LabelPlus.Size = new System.Drawing.Size(14, 13);
            this.LabelPlus.TabIndex = 13;
            this.LabelPlus.Text = "+";
            // 
            // LabelMinus
            // 
            this.LabelMinus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelMinus.Location = new System.Drawing.Point(47, 28);
            this.LabelMinus.Name = "LabelMinus";
            this.LabelMinus.Size = new System.Drawing.Size(13, 13);
            this.LabelMinus.TabIndex = 14;
            this.LabelMinus.Text = "-";
            // 
            // SaveLink
            // 
            this.SaveLink.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.SaveLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveLink.AutoSize = true;
            this.SaveLink.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.SaveLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.SaveLink.LinkColor = System.Drawing.Color.White;
            this.SaveLink.Location = new System.Drawing.Point(392, 293);
            this.SaveLink.Name = "SaveLink";
            this.SaveLink.Size = new System.Drawing.Size(32, 13);
            this.SaveLink.TabIndex = 15;
            this.SaveLink.TabStop = true;
            this.SaveLink.Text = "Save";
            this.SaveLink.Visible = false;
            this.SaveLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SaveLink_LinkClicked);
            // 
            // DiscardLink
            // 
            this.DiscardLink.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.DiscardLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DiscardLink.AutoSize = true;
            this.DiscardLink.BackColor = System.Drawing.Color.Red;
            this.DiscardLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.DiscardLink.LinkColor = System.Drawing.Color.White;
            this.DiscardLink.Location = new System.Drawing.Point(430, 293);
            this.DiscardLink.Name = "DiscardLink";
            this.DiscardLink.Size = new System.Drawing.Size(43, 13);
            this.DiscardLink.TabIndex = 16;
            this.DiscardLink.TabStop = true;
            this.DiscardLink.Text = "Discard";
            this.DiscardLink.Visible = false;
            this.DiscardLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.DiscardLink_LinkClicked);
            // 
            // ChangesLabel
            // 
            this.ChangesLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ChangesLabel.AutoSize = true;
            this.ChangesLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ChangesLabel.Location = new System.Drawing.Point(330, 293);
            this.ChangesLabel.Name = "ChangesLabel";
            this.ChangesLabel.Size = new System.Drawing.Size(56, 13);
            this.ChangesLabel.TabIndex = 20;
            this.ChangesLabel.Text = "Changes";
            this.ChangesLabel.Visible = false;
            // 
            // GoalsCombo
            // 
            this.GoalsCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.GoalsCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.GoalsCombo.FormattingEnabled = true;
            this.GoalsCombo.Location = new System.Drawing.Point(206, 290);
            this.GoalsCombo.Name = "GoalsCombo";
            this.GoalsCombo.Size = new System.Drawing.Size(118, 21);
            this.GoalsCombo.TabIndex = 21;
            this.GoalsCombo.SelectedIndexChanged += new System.EventHandler(this.GoalsCombo_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(132, 293);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 13);
            this.label1.TabIndex = 22;
            this.label1.Text = "Show Goal";
            // 
            // ScheduleView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.GoalsCombo);
            this.Controls.Add(this.ChangesLabel);
            this.Controls.Add(this.DiscardLink);
            this.Controls.Add(this.SaveLink);
            this.Controls.Add(this.LabelMinus);
            this.Controls.Add(this.LabelPlus);
            this.Controls.Add(this.ScheduleSlider);
            this.Controls.Add(this.ExtendedLabel);
            this.Controls.Add(this.RangeLabel);
            this.Controls.Add(this.DateRange);
            this.Controls.Add(this.NowLink);
            this.Controls.Add(this.NewItem);
            this.Controls.Add(this.PlanStructure);
            this.DoubleBuffered = true;
            this.Name = "ScheduleView";
            this.Size = new System.Drawing.Size(476, 315);
            this.Load += new System.EventHandler(this.ScheduleView_Load);
            ((System.ComponentModel.ISupportInitialize)(this.DateRange)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal DeOps.Interface.TLVex.TreeListViewEx PlanStructure;
        private System.Windows.Forms.LinkLabel NewItem;
        private System.Windows.Forms.LinkLabel NowLink;
        private System.Windows.Forms.Label RangeLabel;
        private System.Windows.Forms.TrackBar DateRange;
        internal System.Windows.Forms.Label ExtendedLabel;
        internal DateSlider ScheduleSlider;
        private System.Windows.Forms.Timer HoverTimer;
        private System.Windows.Forms.Label LabelPlus;
        private System.Windows.Forms.Label LabelMinus;
        private System.Windows.Forms.LinkLabel SaveLink;
        private System.Windows.Forms.LinkLabel DiscardLink;
        private System.Windows.Forms.Label ChangesLabel;
        private System.Windows.Forms.ComboBox GoalsCombo;
        private System.Windows.Forms.Label label1;
    }
}