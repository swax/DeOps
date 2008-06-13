namespace RiseOp.Interface
{
    partial class InviteForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InviteForm));
            this.ButtonCancel = new System.Windows.Forms.Button();
            this.PasswordBox = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.ConfirmBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.LinkBox = new System.Windows.Forms.TextBox();
            this.CreateButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.CopyLink = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // ButtonCancel
            // 
            this.ButtonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ButtonCancel.Location = new System.Drawing.Point(280, 290);
            this.ButtonCancel.Name = "ButtonCancel";
            this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
            this.ButtonCancel.TabIndex = 7;
            this.ButtonCancel.Text = "Close";
            this.ButtonCancel.UseVisualStyleBackColor = true;
            this.ButtonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
            // 
            // PasswordBox
            // 
            this.PasswordBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.PasswordBox.Location = new System.Drawing.Point(129, 46);
            this.PasswordBox.Name = "PasswordBox";
            this.PasswordBox.PasswordChar = '•';
            this.PasswordBox.Size = new System.Drawing.Size(225, 20);
            this.PasswordBox.TabIndex = 1;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 49);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(99, 13);
            this.label6.TabIndex = 10;
            this.label6.Text = "Unique Passphrase";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(11, 75);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(100, 13);
            this.label7.TabIndex = 12;
            this.label7.Text = "Confirm Passphrase";
            // 
            // ConfirmBox
            // 
            this.ConfirmBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ConfirmBox.Location = new System.Drawing.Point(129, 72);
            this.ConfirmBox.Name = "ConfirmBox";
            this.ConfirmBox.PasswordChar = '•';
            this.ConfirmBox.Size = new System.Drawing.Size(225, 20);
            this.ConfirmBox.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(12, 127);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(88, 13);
            this.label3.TabIndex = 16;
            this.label3.Text = "Invitation Link";
            // 
            // LinkBox
            // 
            this.LinkBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.LinkBox.BackColor = System.Drawing.SystemColors.ControlLight;
            this.LinkBox.Location = new System.Drawing.Point(15, 146);
            this.LinkBox.Multiline = true;
            this.LinkBox.Name = "LinkBox";
            this.LinkBox.ReadOnly = true;
            this.LinkBox.Size = new System.Drawing.Size(340, 138);
            this.LinkBox.TabIndex = 5;
            // 
            // CreateButton
            // 
            this.CreateButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.CreateButton.Location = new System.Drawing.Point(270, 98);
            this.CreateButton.Name = "CreateButton";
            this.CreateButton.Size = new System.Drawing.Size(85, 23);
            this.CreateButton.TabIndex = 3;
            this.CreateButton.Text = "Create Invite";
            this.CreateButton.UseVisualStyleBackColor = true;
            this.CreateButton.Click += new System.EventHandler(this.CreateButton_Click);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Location = new System.Drawing.Point(12, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(343, 27);
            this.label2.TabIndex = 21;
            this.label2.Text = "Send the invitation link and passphrase to the person you are inviting.\r\nFor exam" +
                "ple send the link by IM and the passphrase by phone.\r\n";
            // 
            // CopyLink
            // 
            this.CopyLink.ActiveLinkColor = System.Drawing.Color.Blue;
            this.CopyLink.AutoSize = true;
            this.CopyLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.CopyLink.Location = new System.Drawing.Point(106, 127);
            this.CopyLink.Name = "CopyLink";
            this.CopyLink.Size = new System.Drawing.Size(94, 13);
            this.CopyLink.TabIndex = 4;
            this.CopyLink.TabStop = true;
            this.CopyLink.Text = "(copy to clipboard)";
            this.CopyLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.CopyLink_LinkClicked);
            // 
            // InviteForm
            // 
            this.AcceptButton = this.CreateButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.CancelButton = this.ButtonCancel;
            this.ClientSize = new System.Drawing.Size(367, 325);
            this.Controls.Add(this.CopyLink);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.CreateButton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.LinkBox);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.ConfirmBox);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.PasswordBox);
            this.Controls.Add(this.ButtonCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InviteForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Invite";
            this.Load += new System.EventHandler(this.InviteForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.TextBox PasswordBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox ConfirmBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox LinkBox;
        private System.Windows.Forms.Button CreateButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.LinkLabel CopyLink;
    }
}