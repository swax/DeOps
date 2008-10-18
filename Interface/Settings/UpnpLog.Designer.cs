namespace RiseOp.Interface.Settings
{
    partial class UpnpLog
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
            this.LogBox = new System.Windows.Forms.RichTextBox();
            this.RefreshLink = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // LogBox
            // 
            this.LogBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.LogBox.DetectUrls = false;
            this.LogBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LogBox.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LogBox.Location = new System.Drawing.Point(0, 0);
            this.LogBox.Name = "LogBox";
            this.LogBox.Size = new System.Drawing.Size(351, 399);
            this.LogBox.TabIndex = 0;
            this.LogBox.Text = "";
            // 
            // RefreshLink
            // 
            this.RefreshLink.ActiveLinkColor = System.Drawing.Color.Lime;
            this.RefreshLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.RefreshLink.AutoSize = true;
            this.RefreshLink.BackColor = System.Drawing.Color.White;
            this.RefreshLink.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RefreshLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.RefreshLink.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.RefreshLink.Location = new System.Drawing.Point(12, 374);
            this.RefreshLink.Name = "RefreshLink";
            this.RefreshLink.Size = new System.Drawing.Size(62, 16);
            this.RefreshLink.TabIndex = 1;
            this.RefreshLink.TabStop = true;
            this.RefreshLink.Text = "Refresh";
            this.RefreshLink.VisitedLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.RefreshLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.RefreshLink_LinkClicked);
            // 
            // UpnpLog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(351, 399);
            this.Controls.Add(this.RefreshLink);
            this.Controls.Add(this.LogBox);
            this.MinimizeBox = false;
            this.Name = "UpnpLog";
            this.ShowInTaskbar = false;
            this.Text = "UPnP Log";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.UpnpLog_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox LogBox;
        private System.Windows.Forms.LinkLabel RefreshLink;
    }
}