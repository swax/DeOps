namespace DeOps.Components.Plan
{
    partial class GoalPanel
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
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader1 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader2 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader3 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader4 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader5 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader6 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            DeOps.Interface.TLVex.ToggleColumnHeader toggleColumnHeader7 = new DeOps.Interface.TLVex.ToggleColumnHeader();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.MineOnly = new System.Windows.Forms.CheckBox();
            this.DelegateLink = new System.Windows.Forms.LinkLabel();
            this.GoalTree = new DeOps.Interface.TLVex.TreeListViewEx();
            this.AddItemLink = new System.Windows.Forms.LinkLabel();
            this.PlanList = new DeOps.Interface.TLVex.ContainerListViewEx();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
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
            this.splitContainer1.Panel1.BackColor = System.Drawing.Color.White;
            this.splitContainer1.Panel1.Controls.Add(this.MineOnly);
            this.splitContainer1.Panel1.Controls.Add(this.DelegateLink);
            this.splitContainer1.Panel1.Controls.Add(this.GoalTree);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.BackColor = System.Drawing.Color.White;
            this.splitContainer1.Panel2.Controls.Add(this.AddItemLink);
            this.splitContainer1.Panel2.Controls.Add(this.PlanList);
            this.splitContainer1.Size = new System.Drawing.Size(469, 343);
            this.splitContainer1.SplitterDistance = 212;
            this.splitContainer1.TabIndex = 0;
            // 
            // MineOnly
            // 
            this.MineOnly.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.MineOnly.AutoSize = true;
            this.MineOnly.Location = new System.Drawing.Point(359, 197);
            this.MineOnly.Name = "MineOnly";
            this.MineOnly.Size = new System.Drawing.Size(112, 17);
            this.MineOnly.TabIndex = 2;
            this.MineOnly.Text = "My Branches Only";
            this.MineOnly.UseVisualStyleBackColor = true;
            this.MineOnly.CheckedChanged += new System.EventHandler(this.MineOnly_CheckedChanged);
            // 
            // DelegateLink
            // 
            this.DelegateLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.DelegateLink.AutoSize = true;
            this.DelegateLink.Location = new System.Drawing.Point(3, 198);
            this.DelegateLink.Name = "DelegateLink";
            this.DelegateLink.Size = new System.Drawing.Size(117, 13);
            this.DelegateLink.TabIndex = 1;
            this.DelegateLink.TabStop = true;
            this.DelegateLink.Text = "Delegate Responsibility";
            this.DelegateLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.DelegateLink_LinkClicked);
            // 
            // GoalTree
            // 
            this.GoalTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.GoalTree.BackColor = System.Drawing.SystemColors.Window;
            this.GoalTree.BorderStyle = System.Windows.Forms.BorderStyle.None;
            toggleColumnHeader1.Hovered = false;
            toggleColumnHeader1.Image = null;
            toggleColumnHeader1.Index = 0;
            toggleColumnHeader1.Pressed = false;
            toggleColumnHeader1.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Spring;
            toggleColumnHeader1.Selected = false;
            toggleColumnHeader1.Text = "Goal";
            toggleColumnHeader1.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader1.Visible = true;
            toggleColumnHeader1.Width = 131;
            toggleColumnHeader2.Hovered = false;
            toggleColumnHeader2.Image = null;
            toggleColumnHeader2.Index = 0;
            toggleColumnHeader2.Pressed = false;
            toggleColumnHeader2.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader2.Selected = false;
            toggleColumnHeader2.Text = "Person";
            toggleColumnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader2.Visible = true;
            toggleColumnHeader2.Width = 110;
            toggleColumnHeader3.Hovered = false;
            toggleColumnHeader3.Image = null;
            toggleColumnHeader3.Index = 0;
            toggleColumnHeader3.Pressed = false;
            toggleColumnHeader3.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader3.Selected = false;
            toggleColumnHeader3.Text = "All Progress";
            toggleColumnHeader3.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader3.Visible = true;
            toggleColumnHeader3.Width = 140;
            toggleColumnHeader4.Hovered = false;
            toggleColumnHeader4.Image = null;
            toggleColumnHeader4.Index = 0;
            toggleColumnHeader4.Pressed = false;
            toggleColumnHeader4.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader4.Selected = false;
            toggleColumnHeader4.Text = "Deadline";
            toggleColumnHeader4.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader4.Visible = true;
            this.GoalTree.Columns.AddRange(new DeOps.Interface.TLVex.ToggleColumnHeader[] {
            toggleColumnHeader1,
            toggleColumnHeader2,
            toggleColumnHeader3,
            toggleColumnHeader4});
            this.GoalTree.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.GoalTree.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.GoalTree.DisableHorizontalScroll = true;
            this.GoalTree.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.GoalTree.HeaderMenu = null;
            this.GoalTree.ItemHeight = 20;
            this.GoalTree.ItemMenu = null;
            this.GoalTree.LabelEdit = false;
            this.GoalTree.Location = new System.Drawing.Point(0, 0);
            this.GoalTree.Name = "GoalTree";
            this.GoalTree.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.GoalTree.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.GoalTree.ShowLines = true;
            this.GoalTree.Size = new System.Drawing.Size(471, 197);
            this.GoalTree.SmallImageList = null;
            this.GoalTree.StateImageList = null;
            this.GoalTree.TabIndex = 0;
            this.GoalTree.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.GoalTree_MouseDoubleClick);
            this.GoalTree.MouseClick += new System.Windows.Forms.MouseEventHandler(this.GoalTree_MouseClick);
            this.GoalTree.SelectedItemChanged += new System.EventHandler(this.GoalTree_SelectedItemChanged);
            // 
            // AddItemLink
            // 
            this.AddItemLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.AddItemLink.AutoSize = true;
            this.AddItemLink.Location = new System.Drawing.Point(3, 114);
            this.AddItemLink.Name = "AddItemLink";
            this.AddItemLink.Size = new System.Drawing.Size(102, 13);
            this.AddItemLink.TabIndex = 5;
            this.AddItemLink.TabStop = true;
            this.AddItemLink.Text = "Add Item to My Plan";
            this.AddItemLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.AddItemLink_LinkClicked);
            // 
            // PlanList
            // 
            this.PlanList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.PlanList.BackColor = System.Drawing.SystemColors.Window;
            this.PlanList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            toggleColumnHeader5.Hovered = false;
            toggleColumnHeader5.Image = null;
            toggleColumnHeader5.Index = 0;
            toggleColumnHeader5.Pressed = false;
            toggleColumnHeader5.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Spring;
            toggleColumnHeader5.Selected = false;
            toggleColumnHeader5.Text = "Plan";
            toggleColumnHeader5.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader5.Visible = true;
            toggleColumnHeader5.Width = 179;
            toggleColumnHeader6.Hovered = false;
            toggleColumnHeader6.Image = null;
            toggleColumnHeader6.Index = 0;
            toggleColumnHeader6.Pressed = false;
            toggleColumnHeader6.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader6.Selected = false;
            toggleColumnHeader6.Text = "When";
            toggleColumnHeader6.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader6.Visible = true;
            toggleColumnHeader6.Width = 140;
            toggleColumnHeader7.Hovered = false;
            toggleColumnHeader7.Image = null;
            toggleColumnHeader7.Index = 0;
            toggleColumnHeader7.Pressed = false;
            toggleColumnHeader7.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader7.Selected = false;
            toggleColumnHeader7.Text = "Progress";
            toggleColumnHeader7.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader7.Visible = true;
            toggleColumnHeader7.Width = 150;
            this.PlanList.Columns.AddRange(new DeOps.Interface.TLVex.ToggleColumnHeader[] {
            toggleColumnHeader5,
            toggleColumnHeader6,
            toggleColumnHeader7});
            this.PlanList.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.PlanList.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.PlanList.DisableHorizontalScroll = true;
            this.PlanList.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.PlanList.HeaderMenu = null;
            this.PlanList.ItemMenu = null;
            this.PlanList.LabelEdit = false;
            this.PlanList.Location = new System.Drawing.Point(0, 0);
            this.PlanList.Name = "PlanList";
            this.PlanList.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.PlanList.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.PlanList.Size = new System.Drawing.Size(469, 113);
            this.PlanList.SmallImageList = null;
            this.PlanList.StateImageList = null;
            this.PlanList.TabIndex = 4;
            this.PlanList.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PlanList_MouseClick);
            this.PlanList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.PlanList_MouseDoubleClick);
            // 
            // GoalPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "GoalPanel";
            this.Size = new System.Drawing.Size(469, 343);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.LinkLabel DelegateLink;
        private DeOps.Interface.TLVex.TreeListViewEx GoalTree;
        private System.Windows.Forms.CheckBox MineOnly;
        private System.Windows.Forms.LinkLabel AddItemLink;
        private DeOps.Interface.TLVex.ContainerListViewEx PlanList;
    }
}
