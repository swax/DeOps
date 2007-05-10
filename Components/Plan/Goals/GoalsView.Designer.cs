namespace DeOps.Components.Plan
{
    partial class GoalsView
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
            this.GoalTabs = new System.Windows.Forms.TabControl();
            this.NewPage = new System.Windows.Forms.TabPage();
            this.CreateButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.EditLink = new System.Windows.Forms.LinkLabel();
            this.DeleteLink = new System.Windows.Forms.LinkLabel();
            this.ViewLink = new System.Windows.Forms.LinkLabel();
            this.UnarchiveLink = new System.Windows.Forms.LinkLabel();
            this.ArchivedList = new DeOps.Interface.TLVex.ContainerListViewEx();
            this.DiscardLink = new System.Windows.Forms.LinkLabel();
            this.SaveLink = new System.Windows.Forms.LinkLabel();
            this.ChangesLabel = new System.Windows.Forms.Label();
            this.GoalTabs.SuspendLayout();
            this.NewPage.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // GoalTabs
            // 
            this.GoalTabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.GoalTabs.Controls.Add(this.NewPage);
            this.GoalTabs.Location = new System.Drawing.Point(0, 0);
            this.GoalTabs.Name = "GoalTabs";
            this.GoalTabs.SelectedIndex = 0;
            this.GoalTabs.Size = new System.Drawing.Size(350, 251);
            this.GoalTabs.TabIndex = 0;
            // 
            // NewPage
            // 
            this.NewPage.Controls.Add(this.CreateButton);
            this.NewPage.Controls.Add(this.groupBox1);
            this.NewPage.Location = new System.Drawing.Point(4, 22);
            this.NewPage.Name = "NewPage";
            this.NewPage.Padding = new System.Windows.Forms.Padding(3);
            this.NewPage.Size = new System.Drawing.Size(342, 225);
            this.NewPage.TabIndex = 0;
            this.NewPage.Text = "Manager";
            this.NewPage.UseVisualStyleBackColor = true;
            // 
            // CreateButton
            // 
            this.CreateButton.Location = new System.Drawing.Point(12, 16);
            this.CreateButton.Name = "CreateButton";
            this.CreateButton.Size = new System.Drawing.Size(129, 23);
            this.CreateButton.TabIndex = 1;
            this.CreateButton.Text = "Create New Goal";
            this.CreateButton.UseVisualStyleBackColor = true;
            this.CreateButton.Click += new System.EventHandler(this.CreateButton_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.EditLink);
            this.groupBox1.Controls.Add(this.DeleteLink);
            this.groupBox1.Controls.Add(this.ViewLink);
            this.groupBox1.Controls.Add(this.UnarchiveLink);
            this.groupBox1.Controls.Add(this.ArchivedList);
            this.groupBox1.Location = new System.Drawing.Point(6, 55);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(330, 164);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Archived";
            // 
            // EditLink
            // 
            this.EditLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.EditLink.AutoSize = true;
            this.EditLink.Location = new System.Drawing.Point(193, 146);
            this.EditLink.Name = "EditLink";
            this.EditLink.Size = new System.Drawing.Size(25, 13);
            this.EditLink.TabIndex = 4;
            this.EditLink.TabStop = true;
            this.EditLink.Text = "Edit";
            this.EditLink.Visible = false;
            this.EditLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.EditLink_LinkClicked);
            // 
            // DeleteLink
            // 
            this.DeleteLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DeleteLink.AutoSize = true;
            this.DeleteLink.Location = new System.Drawing.Point(286, 146);
            this.DeleteLink.Name = "DeleteLink";
            this.DeleteLink.Size = new System.Drawing.Size(38, 13);
            this.DeleteLink.TabIndex = 3;
            this.DeleteLink.TabStop = true;
            this.DeleteLink.Text = "Delete";
            this.DeleteLink.Visible = false;
            this.DeleteLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.DeleteLink_LinkClicked);
            // 
            // ViewLink
            // 
            this.ViewLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ViewLink.AutoSize = true;
            this.ViewLink.Location = new System.Drawing.Point(6, 146);
            this.ViewLink.Name = "ViewLink";
            this.ViewLink.Size = new System.Drawing.Size(30, 13);
            this.ViewLink.TabIndex = 2;
            this.ViewLink.TabStop = true;
            this.ViewLink.Text = "View";
            this.ViewLink.Visible = false;
            this.ViewLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ViewLink_LinkClicked);
            // 
            // UnarchiveLink
            // 
            this.UnarchiveLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.UnarchiveLink.AutoSize = true;
            this.UnarchiveLink.Location = new System.Drawing.Point(224, 146);
            this.UnarchiveLink.Name = "UnarchiveLink";
            this.UnarchiveLink.Size = new System.Drawing.Size(56, 13);
            this.UnarchiveLink.TabIndex = 1;
            this.UnarchiveLink.TabStop = true;
            this.UnarchiveLink.Text = "Unarchive";
            this.UnarchiveLink.Visible = false;
            this.UnarchiveLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.UnarchiveLink_LinkClicked);
            // 
            // ArchivedList
            // 
            this.ArchivedList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ArchivedList.BackColor = System.Drawing.SystemColors.Window;
            toggleColumnHeader1.Hovered = false;
            toggleColumnHeader1.Image = null;
            toggleColumnHeader1.Index = 0;
            toggleColumnHeader1.Pressed = false;
            toggleColumnHeader1.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Spring;
            toggleColumnHeader1.Selected = false;
            toggleColumnHeader1.Text = "Title";
            toggleColumnHeader1.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader1.Visible = true;
            toggleColumnHeader1.Width = 134;
            toggleColumnHeader2.Hovered = false;
            toggleColumnHeader2.Image = null;
            toggleColumnHeader2.Index = 0;
            toggleColumnHeader2.Pressed = false;
            toggleColumnHeader2.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader2.Selected = false;
            toggleColumnHeader2.Text = "Head";
            toggleColumnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader2.Visible = true;
            toggleColumnHeader3.Hovered = false;
            toggleColumnHeader3.Image = null;
            toggleColumnHeader3.Index = 0;
            toggleColumnHeader3.Pressed = false;
            toggleColumnHeader3.ScaleStyle = DeOps.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader3.Selected = false;
            toggleColumnHeader3.Text = "Deadline";
            toggleColumnHeader3.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader3.Visible = true;
            this.ArchivedList.Columns.AddRange(new DeOps.Interface.TLVex.ToggleColumnHeader[] {
            toggleColumnHeader1,
            toggleColumnHeader2,
            toggleColumnHeader3});
            this.ArchivedList.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.ArchivedList.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.ArchivedList.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.ArchivedList.HeaderMenu = null;
            this.ArchivedList.ItemMenu = null;
            this.ArchivedList.LabelEdit = false;
            this.ArchivedList.Location = new System.Drawing.Point(6, 19);
            this.ArchivedList.Name = "ArchivedList";
            this.ArchivedList.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.ArchivedList.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.ArchivedList.Size = new System.Drawing.Size(318, 124);
            this.ArchivedList.SmallImageList = null;
            this.ArchivedList.StateImageList = null;
            this.ArchivedList.TabIndex = 0;
            this.ArchivedList.Text = "containerListViewEx1";
            this.ArchivedList.VisualStyles = false;
            this.ArchivedList.SelectedIndexChanged += new System.EventHandler(this.ArchivedList_SelectedIndexChanged);
            // 
            // DiscardLink
            // 
            this.DiscardLink.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.DiscardLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DiscardLink.AutoSize = true;
            this.DiscardLink.BackColor = System.Drawing.Color.Red;
            this.DiscardLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.DiscardLink.LinkColor = System.Drawing.Color.White;
            this.DiscardLink.Location = new System.Drawing.Point(303, 254);
            this.DiscardLink.Name = "DiscardLink";
            this.DiscardLink.Size = new System.Drawing.Size(43, 13);
            this.DiscardLink.TabIndex = 18;
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
            this.SaveLink.Location = new System.Drawing.Point(265, 254);
            this.SaveLink.Name = "SaveLink";
            this.SaveLink.Size = new System.Drawing.Size(32, 13);
            this.SaveLink.TabIndex = 17;
            this.SaveLink.TabStop = true;
            this.SaveLink.Text = "Save";
            this.SaveLink.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.SaveLink.Visible = false;
            this.SaveLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SaveLink_LinkClicked);
            // 
            // ChangesLabel
            // 
            this.ChangesLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ChangesLabel.AutoSize = true;
            this.ChangesLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ChangesLabel.Location = new System.Drawing.Point(203, 254);
            this.ChangesLabel.Name = "ChangesLabel";
            this.ChangesLabel.Size = new System.Drawing.Size(56, 13);
            this.ChangesLabel.TabIndex = 19;
            this.ChangesLabel.Text = "Changes";
            this.ChangesLabel.Visible = false;
            // 
            // GoalsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ChangesLabel);
            this.Controls.Add(this.DiscardLink);
            this.Controls.Add(this.SaveLink);
            this.Controls.Add(this.GoalTabs);
            this.Name = "GoalsView";
            this.Size = new System.Drawing.Size(350, 269);
            this.Load += new System.EventHandler(this.GoalsView_Load);
            this.GoalTabs.ResumeLayout(false);
            this.NewPage.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl GoalTabs;
        private System.Windows.Forms.TabPage NewPage;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button CreateButton;
        private DeOps.Interface.TLVex.ContainerListViewEx ArchivedList;
        private System.Windows.Forms.LinkLabel ViewLink;
        private System.Windows.Forms.LinkLabel DiscardLink;
        private System.Windows.Forms.LinkLabel SaveLink;
        private System.Windows.Forms.LinkLabel UnarchiveLink;
        private System.Windows.Forms.LinkLabel EditLink;
        private System.Windows.Forms.LinkLabel DeleteLink;
        private System.Windows.Forms.Label ChangesLabel;
    }
}
