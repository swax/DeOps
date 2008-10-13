namespace RiseOp.Interface
{
    partial class AddUsersDialog
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
            this.TheCancelButton = new System.Windows.Forms.Button();
            this.AddButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.ProjectCombo = new System.Windows.Forms.ComboBox();
            this.TrustTree = new RiseOp.Services.Trust.LinkTree();
            this.BuddyList = new RiseOp.Services.Buddy.BuddyView();
            this.SuspendLayout();
            // 
            // TheCancelButton
            // 
            this.TheCancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.TheCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.TheCancelButton.Location = new System.Drawing.Point(164, 277);
            this.TheCancelButton.Name = "TheCancelButton";
            this.TheCancelButton.Size = new System.Drawing.Size(75, 23);
            this.TheCancelButton.TabIndex = 2;
            this.TheCancelButton.Text = "Cancel";
            this.TheCancelButton.UseVisualStyleBackColor = true;
            this.TheCancelButton.Click += new System.EventHandler(this.TheCancelButton_Click);
            // 
            // AddButton
            // 
            this.AddButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.AddButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.AddButton.Location = new System.Drawing.Point(83, 277);
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(75, 23);
            this.AddButton.TabIndex = 3;
            this.AddButton.Text = "Add";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(12, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Group";
            // 
            // ProjectCombo
            // 
            this.ProjectCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ProjectCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ProjectCombo.FormattingEnabled = true;
            this.ProjectCombo.Location = new System.Drawing.Point(65, 12);
            this.ProjectCombo.Name = "ProjectCombo";
            this.ProjectCombo.Size = new System.Drawing.Size(174, 21);
            this.ProjectCombo.TabIndex = 6;
            this.ProjectCombo.SelectedIndexChanged += new System.EventHandler(this.ProjectCombo_SelectedIndexChanged);
            // 
            // PersonTree
            // 
            this.TrustTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.TrustTree.BackColor = System.Drawing.SystemColors.Window;
            this.TrustTree.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TrustTree.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.TrustTree.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.TrustTree.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.TrustTree.HeaderMenu = null;
            this.TrustTree.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.TrustTree.ItemHeight = 20;
            this.TrustTree.ItemMenu = null;
            this.TrustTree.LabelEdit = false;
            this.TrustTree.Location = new System.Drawing.Point(15, 39);
            this.TrustTree.Name = "PersonTree";
            this.TrustTree.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.TrustTree.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.TrustTree.Size = new System.Drawing.Size(224, 232);
            this.TrustTree.SmallImageList = null;
            this.TrustTree.StateImageList = null;
            this.TrustTree.TabIndex = 4;
            // 
            // BuddyList
            // 
            this.BuddyList.AllowDrop = true;
            this.BuddyList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.BuddyList.BackColor = System.Drawing.SystemColors.Window;
            this.BuddyList.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.BuddyList.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.BuddyList.DisableHorizontalScroll = true;
            this.BuddyList.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.BuddyList.HeaderMenu = null;
            this.BuddyList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.BuddyList.ItemMenu = null;
            this.BuddyList.LabelEdit = false;
            this.BuddyList.Location = new System.Drawing.Point(15, 39);
            this.BuddyList.MultiSelect = true;
            this.BuddyList.Name = "BuddyList";
            this.BuddyList.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.BuddyList.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.BuddyList.Size = new System.Drawing.Size(224, 232);
            this.BuddyList.SmallImageList = null;
            this.BuddyList.StateImageList = null;
            this.BuddyList.TabIndex = 7;
            // 
            // AddUsersDialog
            // 
            this.AcceptButton = this.AddButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.TheCancelButton;
            this.ClientSize = new System.Drawing.Size(251, 312);
            this.Controls.Add(this.BuddyList);
            this.Controls.Add(this.ProjectCombo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.TrustTree);
            this.Controls.Add(this.AddButton);
            this.Controls.Add(this.TheCancelButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddUsersDialog";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Add People";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button TheCancelButton;
        internal RiseOp.Services.Trust.LinkTree TrustTree;
        private System.Windows.Forms.Label label2;
        internal System.Windows.Forms.ComboBox ProjectCombo;
        internal System.Windows.Forms.Button AddButton;
        private RiseOp.Services.Buddy.BuddyView BuddyList;
    }
}