namespace LicenseOp
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.LicenseList = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
            this.RefreshLink = new System.Windows.Forms.LinkLabel();
            this.CreateLink = new System.Windows.Forms.LinkLabel();
            this.DeleteLink = new System.Windows.Forms.LinkLabel();
            this.SaveButton = new System.Windows.Forms.Button();
            this.ExportButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // LicenseList
            // 
            this.LicenseList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.LicenseList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
            this.LicenseList.FullRowSelect = true;
            this.LicenseList.HideSelection = false;
            this.LicenseList.Location = new System.Drawing.Point(12, 34);
            this.LicenseList.Name = "LicenseList";
            this.LicenseList.Size = new System.Drawing.Size(378, 157);
            this.LicenseList.TabIndex = 0;
            this.LicenseList.UseCompatibleStateImageBehavior = false;
            this.LicenseList.View = System.Windows.Forms.View.Details;
            this.LicenseList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.LicenseList_MouseDoubleClick);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            this.columnHeader1.Width = 123;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Email";
            this.columnHeader2.Width = 134;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "#";
            this.columnHeader3.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader3.Width = 71;
            // 
            // RefreshLink
            // 
            this.RefreshLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RefreshLink.AutoSize = true;
            this.RefreshLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.RefreshLink.Location = new System.Drawing.Point(346, 18);
            this.RefreshLink.Name = "RefreshLink";
            this.RefreshLink.Size = new System.Drawing.Size(44, 13);
            this.RefreshLink.TabIndex = 1;
            this.RefreshLink.TabStop = true;
            this.RefreshLink.Text = "Refresh";
            this.RefreshLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.RefreshLink_LinkClicked);
            // 
            // CreateLink
            // 
            this.CreateLink.AutoSize = true;
            this.CreateLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.CreateLink.Location = new System.Drawing.Point(12, 18);
            this.CreateLink.Name = "CreateLink";
            this.CreateLink.Size = new System.Drawing.Size(38, 13);
            this.CreateLink.TabIndex = 2;
            this.CreateLink.TabStop = true;
            this.CreateLink.Text = "Create";
            this.CreateLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.CreateLink_LinkClicked);
            // 
            // DeleteLink
            // 
            this.DeleteLink.AutoSize = true;
            this.DeleteLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.DeleteLink.Location = new System.Drawing.Point(72, 18);
            this.DeleteLink.Name = "DeleteLink";
            this.DeleteLink.Size = new System.Drawing.Size(38, 13);
            this.DeleteLink.TabIndex = 5;
            this.DeleteLink.TabStop = true;
            this.DeleteLink.Text = "Delete";
            this.DeleteLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.DeleteLink_LinkClicked);
            // 
            // SaveButton
            // 
            this.SaveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveButton.Location = new System.Drawing.Point(315, 199);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(75, 23);
            this.SaveButton.TabIndex = 6;
            this.SaveButton.Text = "Save";
            this.SaveButton.UseVisualStyleBackColor = true;
            this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // ExportButton
            // 
            this.ExportButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ExportButton.Location = new System.Drawing.Point(12, 199);
            this.ExportButton.Name = "ExportButton";
            this.ExportButton.Size = new System.Drawing.Size(117, 23);
            this.ExportButton.TabIndex = 7;
            this.ExportButton.Text = "Export Selected";
            this.ExportButton.UseVisualStyleBackColor = true;
            this.ExportButton.Click += new System.EventHandler(this.SignButton_Click_1);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(402, 234);
            this.Controls.Add(this.ExportButton);
            this.Controls.Add(this.SaveButton);
            this.Controls.Add(this.DeleteLink);
            this.Controls.Add(this.CreateLink);
            this.Controls.Add(this.RefreshLink);
            this.Controls.Add(this.LicenseList);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "RiseOp License Manager";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView LicenseList;
        private System.Windows.Forms.LinkLabel RefreshLink;
        private System.Windows.Forms.LinkLabel CreateLink;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.LinkLabel DeleteLink;
        private System.Windows.Forms.Button SaveButton;
        private System.Windows.Forms.Button ExportButton;
    }
}

