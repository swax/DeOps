namespace DeOps.Interface.Settings
{
    partial class UpnpSetup
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
            this.label1 = new System.Windows.Forms.Label();
            this.EntryList = new System.Windows.Forms.ListBox();
            this.RefreshLink = new System.Windows.Forms.LinkLabel();
            this.LogLink = new System.Windows.Forms.LinkLabel();
            this.CloseButton = new System.Windows.Forms.Button();
            this.RemoveLink = new System.Windows.Forms.LinkLabel();
            this.AddDeOpsLink = new System.Windows.Forms.LinkLabel();
            this.IPradio = new System.Windows.Forms.RadioButton();
            this.PPPradio = new System.Windows.Forms.RadioButton();
            this.ActionLabel = new System.Windows.Forms.Label();
            this.ActionTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(394, 37);
            this.label1.TabIndex = 0;
            this.label1.Text = "Universal Plug n Play automatically configures your router so that the right port" +
                "s DeOps needs are open. Use the refresh button to check which ports are open. ";
            // 
            // EntryList
            // 
            this.EntryList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.EntryList.FormattingEnabled = true;
            this.EntryList.Location = new System.Drawing.Point(12, 75);
            this.EntryList.Name = "EntryList";
            this.EntryList.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.EntryList.Size = new System.Drawing.Size(394, 173);
            this.EntryList.TabIndex = 1;
            // 
            // RefreshLink
            // 
            this.RefreshLink.AutoSize = true;
            this.RefreshLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.RefreshLink.Location = new System.Drawing.Point(12, 56);
            this.RefreshLink.Name = "RefreshLink";
            this.RefreshLink.Size = new System.Drawing.Size(44, 13);
            this.RefreshLink.TabIndex = 32;
            this.RefreshLink.TabStop = true;
            this.RefreshLink.Text = "Refresh";
            this.RefreshLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.RefreshLink_LinkClicked);
            // 
            // LogLink
            // 
            this.LogLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LogLink.AutoSize = true;
            this.LogLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.LogLink.Location = new System.Drawing.Point(186, 259);
            this.LogLink.Name = "LogLink";
            this.LogLink.Size = new System.Drawing.Size(54, 13);
            this.LogLink.TabIndex = 33;
            this.LogLink.TabStop = true;
            this.LogLink.Text = "Open Log";
            this.LogLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LogLink_LinkClicked);
            // 
            // CloseButton
            // 
            this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CloseButton.Location = new System.Drawing.Point(331, 259);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(75, 23);
            this.CloseButton.TabIndex = 34;
            this.CloseButton.Text = "Close";
            this.CloseButton.UseVisualStyleBackColor = true;
            this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
            // 
            // RemoveLink
            // 
            this.RemoveLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RemoveLink.AutoSize = true;
            this.RemoveLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.RemoveLink.Location = new System.Drawing.Point(314, 56);
            this.RemoveLink.Name = "RemoveLink";
            this.RemoveLink.Size = new System.Drawing.Size(92, 13);
            this.RemoveLink.TabIndex = 35;
            this.RemoveLink.TabStop = true;
            this.RemoveLink.Text = "Remove Selected";
            this.RemoveLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.RemoveLink_LinkClicked);
            // 
            // AddDeOpsLink
            // 
            this.AddDeOpsLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.AddDeOpsLink.AutoSize = true;
            this.AddDeOpsLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.AddDeOpsLink.Location = new System.Drawing.Point(12, 259);
            this.AddDeOpsLink.Name = "AddDeOpsLink";
            this.AddDeOpsLink.Size = new System.Drawing.Size(100, 13);
            this.AddDeOpsLink.TabIndex = 36;
            this.AddDeOpsLink.TabStop = true;
            this.AddDeOpsLink.Text = "Reset DeOps Ports";
            this.AddDeOpsLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.AddDeOpsLink_LinkClicked);
            // 
            // IPradio
            // 
            this.IPradio.AutoSize = true;
            this.IPradio.Checked = true;
            this.IPradio.Location = new System.Drawing.Point(85, 54);
            this.IPradio.Name = "IPradio";
            this.IPradio.Size = new System.Drawing.Size(35, 17);
            this.IPradio.TabIndex = 37;
            this.IPradio.TabStop = true;
            this.IPradio.Text = "IP";
            this.IPradio.UseVisualStyleBackColor = true;
            // 
            // PPPradio
            // 
            this.PPPradio.AutoSize = true;
            this.PPPradio.Location = new System.Drawing.Point(126, 54);
            this.PPPradio.Name = "PPPradio";
            this.PPPradio.Size = new System.Drawing.Size(46, 17);
            this.PPPradio.TabIndex = 38;
            this.PPPradio.Text = "PPP";
            this.PPPradio.UseVisualStyleBackColor = true;
            // 
            // ActionLabel
            // 
            this.ActionLabel.AutoSize = true;
            this.ActionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ActionLabel.ForeColor = System.Drawing.Color.Red;
            this.ActionLabel.Location = new System.Drawing.Point(204, 56);
            this.ActionLabel.Name = "ActionLabel";
            this.ActionLabel.Size = new System.Drawing.Size(66, 13);
            this.ActionLabel.TabIndex = 39;
            this.ActionLabel.Text = "Working...";
            this.ActionLabel.Visible = false;
            // 
            // ActionTimer
            // 
            this.ActionTimer.Enabled = true;
            this.ActionTimer.Interval = 500;
            this.ActionTimer.Tick += new System.EventHandler(this.ActionTimer_Tick);
            // 
            // UpnpSetup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CloseButton;
            this.ClientSize = new System.Drawing.Size(418, 294);
            this.Controls.Add(this.ActionLabel);
            this.Controls.Add(this.PPPradio);
            this.Controls.Add(this.IPradio);
            this.Controls.Add(this.AddDeOpsLink);
            this.Controls.Add(this.RemoveLink);
            this.Controls.Add(this.CloseButton);
            this.Controls.Add(this.LogLink);
            this.Controls.Add(this.RefreshLink);
            this.Controls.Add(this.EntryList);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpnpSetup";
            this.ShowInTaskbar = false;
            this.Text = "UPnP Diagnostic";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox EntryList;
        private System.Windows.Forms.LinkLabel RefreshLink;
        private System.Windows.Forms.LinkLabel LogLink;
        private System.Windows.Forms.Button CloseButton;
        private System.Windows.Forms.LinkLabel RemoveLink;
        private System.Windows.Forms.LinkLabel AddDeOpsLink;
        private System.Windows.Forms.RadioButton IPradio;
        private System.Windows.Forms.RadioButton PPPradio;
        private System.Windows.Forms.Label ActionLabel;
        private System.Windows.Forms.Timer ActionTimer;
    }
}