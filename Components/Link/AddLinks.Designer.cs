namespace DeOps.Components.Link
{
    partial class AddLinks
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
            PersonTree = new LinkTree();
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
            this.label2.Size = new System.Drawing.Size(47, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Project";
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
            this.PersonTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.PersonTree.BackColor = System.Drawing.SystemColors.Window;
            this.PersonTree.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.PersonTree.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.PersonTree.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.PersonTree.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.PersonTree.HeaderMenu = null;
            this.PersonTree.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.PersonTree.ItemHeight = 20;
            this.PersonTree.ItemMenu = null;
            this.PersonTree.LabelEdit = false;
            this.PersonTree.Location = new System.Drawing.Point(15, 39);
            this.PersonTree.Name = "PersonTree";
            this.PersonTree.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.PersonTree.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.PersonTree.Size = new System.Drawing.Size(224, 232);
            this.PersonTree.SmallImageList = null;
            this.PersonTree.StateImageList = null;
            this.PersonTree.TabIndex = 4;
            this.PersonTree.Text = "linkList1";
            // 
            // AddLinks
            // 
            this.AcceptButton = this.AddButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.TheCancelButton;
            this.ClientSize = new System.Drawing.Size(251, 312);
            this.Controls.Add(this.ProjectCombo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.PersonTree);
            this.Controls.Add(this.AddButton);
            this.Controls.Add(this.TheCancelButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddLinks";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Add People";
            this.Load += new System.EventHandler(this.LinkChooser_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button TheCancelButton;
        private System.Windows.Forms.Button AddButton;
        internal LinkTree PersonTree;
        private System.Windows.Forms.Label label2;
        internal System.Windows.Forms.ComboBox ProjectCombo;
    }
}