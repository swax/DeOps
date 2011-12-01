namespace DeOps.Interface.Settings
{
    partial class MyAddress
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
            this.OrgLabel = new System.Windows.Forms.Label();
            this.OrgAddressBox = new System.Windows.Forms.TextBox();
            this.CopyOrgLink = new System.Windows.Forms.LinkLabel();
            this.CopyLookupLink = new System.Windows.Forms.LinkLabel();
            this.LookupAddressBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.CloseButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // OrgLabel
            // 
            this.OrgLabel.AutoSize = true;
            this.OrgLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OrgLabel.Location = new System.Drawing.Point(12, 9);
            this.OrgLabel.Name = "OrgLabel";
            this.OrgLabel.Size = new System.Drawing.Size(78, 13);
            this.OrgLabel.TabIndex = 0;
            this.OrgLabel.Text = "Organization";
            // 
            // OrgAddressBox
            // 
            this.OrgAddressBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OrgAddressBox.Location = new System.Drawing.Point(12, 25);
            this.OrgAddressBox.Name = "OrgAddressBox";
            this.OrgAddressBox.ReadOnly = true;
            this.OrgAddressBox.Size = new System.Drawing.Size(343, 20);
            this.OrgAddressBox.TabIndex = 1;
            // 
            // CopyOrgLink
            // 
            this.CopyOrgLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.CopyOrgLink.AutoSize = true;
            this.CopyOrgLink.Location = new System.Drawing.Point(361, 25);
            this.CopyOrgLink.Name = "CopyOrgLink";
            this.CopyOrgLink.Size = new System.Drawing.Size(90, 13);
            this.CopyOrgLink.TabIndex = 2;
            this.CopyOrgLink.TabStop = true;
            this.CopyOrgLink.Text = "Copy to Clipboard";
            this.CopyOrgLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.CopyOrgLink_LinkClicked);
            // 
            // CopyLookupLink
            // 
            this.CopyLookupLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.CopyLookupLink.AutoSize = true;
            this.CopyLookupLink.Location = new System.Drawing.Point(361, 73);
            this.CopyLookupLink.Name = "CopyLookupLink";
            this.CopyLookupLink.Size = new System.Drawing.Size(90, 13);
            this.CopyLookupLink.TabIndex = 5;
            this.CopyLookupLink.TabStop = true;
            this.CopyLookupLink.Text = "Copy to Clipboard";
            this.CopyLookupLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.CopyLookupLink_LinkClicked);
            // 
            // LookupAddressBox
            // 
            this.LookupAddressBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LookupAddressBox.Location = new System.Drawing.Point(12, 73);
            this.LookupAddressBox.Name = "LookupAddressBox";
            this.LookupAddressBox.ReadOnly = true;
            this.LookupAddressBox.Size = new System.Drawing.Size(343, 20);
            this.LookupAddressBox.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(12, 57);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Lookup Network";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 107);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(296, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Give these links out if you want to help people get connected";
            // 
            // CloseButton
            // 
            this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseButton.Location = new System.Drawing.Point(376, 107);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(75, 23);
            this.CloseButton.TabIndex = 7;
            this.CloseButton.Text = "Close";
            this.CloseButton.UseVisualStyleBackColor = true;
            this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
            // 
            // MyAddress
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(463, 139);
            this.Controls.Add(this.CloseButton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.CopyLookupLink);
            this.Controls.Add(this.LookupAddressBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.CopyOrgLink);
            this.Controls.Add(this.OrgAddressBox);
            this.Controls.Add(this.OrgLabel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MyAddress";
            this.Text = "My Bootstrap Address";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label OrgLabel;
        private System.Windows.Forms.TextBox OrgAddressBox;
        private System.Windows.Forms.LinkLabel CopyOrgLink;
        private System.Windows.Forms.LinkLabel CopyLookupLink;
        private System.Windows.Forms.TextBox LookupAddressBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button CloseButton;
    }
}