namespace DeOps.Services.Share
{
    partial class SendFileForm
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
            this.BrowseLink = new System.Windows.Forms.LinkLabel();
            this.RecentCombo = new System.Windows.Forms.ComboBox();
            this.SendButton = new System.Windows.Forms.Button();
            this.ExitButton = new System.Windows.Forms.Button();
            this.BrowseRadio = new System.Windows.Forms.RadioButton();
            this.RecentRadio = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // BrowseLink
            // 
            this.BrowseLink.AutoSize = true;
            this.BrowseLink.Location = new System.Drawing.Point(35, 23);
            this.BrowseLink.Name = "BrowseLink";
            this.BrowseLink.Size = new System.Drawing.Size(85, 13);
            this.BrowseLink.TabIndex = 0;
            this.BrowseLink.TabStop = true;
            this.BrowseLink.Text = "Browse for a File";
            this.BrowseLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.BrowseLink_LinkClicked);
            // 
            // RecentCombo
            // 
            this.RecentCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.RecentCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.RecentCombo.Enabled = false;
            this.RecentCombo.FormattingEnabled = true;
            this.RecentCombo.Location = new System.Drawing.Point(32, 71);
            this.RecentCombo.Name = "RecentCombo";
            this.RecentCombo.Size = new System.Drawing.Size(240, 21);
            this.RecentCombo.TabIndex = 1;
            // 
            // SendButton
            // 
            this.SendButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SendButton.Location = new System.Drawing.Point(116, 119);
            this.SendButton.Name = "SendButton";
            this.SendButton.Size = new System.Drawing.Size(75, 23);
            this.SendButton.TabIndex = 3;
            this.SendButton.Text = "Send";
            this.SendButton.UseVisualStyleBackColor = true;
            this.SendButton.Click += new System.EventHandler(this.SendButton_Click);
            // 
            // ExitButton
            // 
            this.ExitButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ExitButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ExitButton.Location = new System.Drawing.Point(197, 119);
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.Size = new System.Drawing.Size(75, 23);
            this.ExitButton.TabIndex = 4;
            this.ExitButton.Text = "Cancel";
            this.ExitButton.UseVisualStyleBackColor = true;
            this.ExitButton.Click += new System.EventHandler(this.ExitButton_Click);
            // 
            // BrowseRadio
            // 
            this.BrowseRadio.AutoSize = true;
            this.BrowseRadio.Checked = true;
            this.BrowseRadio.Location = new System.Drawing.Point(15, 23);
            this.BrowseRadio.Name = "BrowseRadio";
            this.BrowseRadio.Size = new System.Drawing.Size(14, 13);
            this.BrowseRadio.TabIndex = 5;
            this.BrowseRadio.TabStop = true;
            this.BrowseRadio.UseVisualStyleBackColor = true;
            this.BrowseRadio.CheckedChanged += new System.EventHandler(this.BrowseRadio_CheckedChanged);
            // 
            // RecentRadio
            // 
            this.RecentRadio.AutoSize = true;
            this.RecentRadio.Location = new System.Drawing.Point(15, 49);
            this.RecentRadio.Name = "RecentRadio";
            this.RecentRadio.Size = new System.Drawing.Size(133, 17);
            this.RecentRadio.TabIndex = 6;
            this.RecentRadio.Text = "or Select a Recent File";
            this.RecentRadio.UseVisualStyleBackColor = true;
            this.RecentRadio.CheckedChanged += new System.EventHandler(this.RecentRadio_CheckedChanged);
            // 
            // SendFileForm
            // 
            this.AcceptButton = this.SendButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.ExitButton;
            this.ClientSize = new System.Drawing.Size(284, 154);
            this.Controls.Add(this.RecentRadio);
            this.Controls.Add(this.BrowseRadio);
            this.Controls.Add(this.ExitButton);
            this.Controls.Add(this.SendButton);
            this.Controls.Add(this.RecentCombo);
            this.Controls.Add(this.BrowseLink);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SendFileForm";
            this.ShowInTaskbar = false;
            this.Text = "Send File";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.LinkLabel BrowseLink;
        private System.Windows.Forms.ComboBox RecentCombo;
        private System.Windows.Forms.Button SendButton;
        private System.Windows.Forms.Button ExitButton;
        private System.Windows.Forms.RadioButton BrowseRadio;
        private System.Windows.Forms.RadioButton RecentRadio;
    }
}