namespace DeOps.Interface.Settings
{
    partial class Connecting
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
            this.CloseButton = new System.Windows.Forms.Button();
            this.OKButton = new System.Windows.Forms.Button();
            this.OpTcpBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.OperationLabel = new System.Windows.Forms.Label();
            this.LookupLabel = new System.Windows.Forms.Label();
            this.OpUdpBox = new System.Windows.Forms.TextBox();
            this.LookupTcpBox = new System.Windows.Forms.TextBox();
            this.LookupUdpBox = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.OpStatusBox = new System.Windows.Forms.TextBox();
            this.LookupStatusBox = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.CacheList = new System.Windows.Forms.ListBox();
            this.AddLink = new System.Windows.Forms.LinkLabel();
            this.RemoveLink = new System.Windows.Forms.LinkLabel();
            this.SetupLink = new System.Windows.Forms.LinkLabel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.UPnPLink = new System.Windows.Forms.LinkLabel();
            this.OpLanBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.LookupLanBox = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // CloseButton
            // 
            this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CloseButton.Location = new System.Drawing.Point(235, 289);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(75, 23);
            this.CloseButton.TabIndex = 9;
            this.CloseButton.Text = "Cancel";
            this.CloseButton.UseVisualStyleBackColor = true;
            this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
            // 
            // OKButton
            // 
            this.OKButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OKButton.Location = new System.Drawing.Point(154, 289);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 8;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // OpTcpBox
            // 
            this.OpTcpBox.Location = new System.Drawing.Point(76, 34);
            this.OpTcpBox.Name = "OpTcpBox";
            this.OpTcpBox.Size = new System.Drawing.Size(47, 20);
            this.OpTcpBox.TabIndex = 10;
            this.OpTcpBox.Text = "65536";
            this.OpTcpBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(84, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(28, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "TCP";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(140, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(30, 13);
            this.label3.TabIndex = 13;
            this.label3.Text = "UDP";
            // 
            // OperationLabel
            // 
            this.OperationLabel.AutoSize = true;
            this.OperationLabel.Location = new System.Drawing.Point(7, 37);
            this.OperationLabel.Name = "OperationLabel";
            this.OperationLabel.Size = new System.Drawing.Size(53, 13);
            this.OperationLabel.TabIndex = 14;
            this.OperationLabel.Text = "Operation";
            // 
            // GlobalLabel
            // 
            this.LookupLabel.AutoSize = true;
            this.LookupLabel.Location = new System.Drawing.Point(7, 63);
            this.LookupLabel.Name = "GlobalLabel";
            this.LookupLabel.Size = new System.Drawing.Size(43, 13);
            this.LookupLabel.TabIndex = 15;
            this.LookupLabel.Text = "Lookup";
            // 
            // OpUdpBox
            // 
            this.OpUdpBox.Location = new System.Drawing.Point(129, 34);
            this.OpUdpBox.Name = "OpUdpBox";
            this.OpUdpBox.Size = new System.Drawing.Size(47, 20);
            this.OpUdpBox.TabIndex = 16;
            this.OpUdpBox.Text = "65536";
            this.OpUdpBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // GlobalTcpBox
            // 
            this.LookupTcpBox.Location = new System.Drawing.Point(76, 60);
            this.LookupTcpBox.Name = "GlobalTcpBox";
            this.LookupTcpBox.Size = new System.Drawing.Size(47, 20);
            this.LookupTcpBox.TabIndex = 17;
            this.LookupTcpBox.Text = "65536";
            this.LookupTcpBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // GlobalUdpBox
            // 
            this.LookupUdpBox.Location = new System.Drawing.Point(129, 60);
            this.LookupUdpBox.Name = "GlobalUdpBox";
            this.LookupUdpBox.Size = new System.Drawing.Size(47, 20);
            this.LookupUdpBox.TabIndex = 18;
            this.LookupUdpBox.Text = "65536";
            this.LookupUdpBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(238, 16);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(37, 13);
            this.label7.TabIndex = 20;
            this.label7.Text = "Status";
            // 
            // OpStatusBox
            // 
            this.OpStatusBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.OpStatusBox.ForeColor = System.Drawing.Color.White;
            this.OpStatusBox.Location = new System.Drawing.Point(235, 34);
            this.OpStatusBox.Name = "OpStatusBox";
            this.OpStatusBox.ReadOnly = true;
            this.OpStatusBox.Size = new System.Drawing.Size(47, 20);
            this.OpStatusBox.TabIndex = 21;
            this.OpStatusBox.Text = "Open";
            this.OpStatusBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // GlobalStatusBox
            // 
            this.LookupStatusBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.LookupStatusBox.ForeColor = System.Drawing.Color.White;
            this.LookupStatusBox.Location = new System.Drawing.Point(235, 60);
            this.LookupStatusBox.Name = "GlobalStatusBox";
            this.LookupStatusBox.ReadOnly = true;
            this.LookupStatusBox.Size = new System.Drawing.Size(47, 20);
            this.LookupStatusBox.TabIndex = 22;
            this.LookupStatusBox.Text = "Blocked";
            this.LookupStatusBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(12, 190);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(79, 13);
            this.label8.TabIndex = 23;
            this.label8.Text = "Web Caches";
            // 
            // CacheList
            // 
            this.CacheList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.CacheList.FormattingEnabled = true;
            this.CacheList.Location = new System.Drawing.Point(15, 208);
            this.CacheList.Name = "CacheList";
            this.CacheList.Size = new System.Drawing.Size(294, 69);
            this.CacheList.TabIndex = 24;
            // 
            // AddLink
            // 
            this.AddLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AddLink.AutoSize = true;
            this.AddLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.AddLink.Location = new System.Drawing.Point(192, 190);
            this.AddLink.Name = "AddLink";
            this.AddLink.Size = new System.Drawing.Size(26, 13);
            this.AddLink.TabIndex = 25;
            this.AddLink.TabStop = true;
            this.AddLink.Text = "Add";
            this.AddLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.AddLink_LinkClicked);
            // 
            // RemoveLink
            // 
            this.RemoveLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RemoveLink.AutoSize = true;
            this.RemoveLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.RemoveLink.Location = new System.Drawing.Point(222, 190);
            this.RemoveLink.Name = "RemoveLink";
            this.RemoveLink.Size = new System.Drawing.Size(47, 13);
            this.RemoveLink.TabIndex = 26;
            this.RemoveLink.TabStop = true;
            this.RemoveLink.Text = "Remove";
            this.RemoveLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.RemoveLink_LinkClicked);
            // 
            // SetupLink
            // 
            this.SetupLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SetupLink.AutoSize = true;
            this.SetupLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.SetupLink.Location = new System.Drawing.Point(275, 190);
            this.SetupLink.Name = "SetupLink";
            this.SetupLink.Size = new System.Drawing.Size(35, 13);
            this.SetupLink.TabIndex = 27;
            this.SetupLink.TabStop = true;
            this.SetupLink.Text = "Setup";
            this.SetupLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SetupLink_LinkClicked);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.UPnPLink);
            this.groupBox1.Controls.Add(this.OpLanBox);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.LookupLanBox);
            this.groupBox1.Controls.Add(this.OpTcpBox);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.OperationLabel);
            this.groupBox1.Controls.Add(this.LookupLabel);
            this.groupBox1.Controls.Add(this.LookupStatusBox);
            this.groupBox1.Controls.Add(this.OpUdpBox);
            this.groupBox1.Controls.Add(this.OpStatusBox);
            this.groupBox1.Controls.Add(this.LookupTcpBox);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.LookupUdpBox);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(298, 147);
            this.groupBox1.TabIndex = 28;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Ports";
            // 
            // UPnPLink
            // 
            this.UPnPLink.AutoSize = true;
            this.UPnPLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.UPnPLink.Location = new System.Drawing.Point(199, 127);
            this.UPnPLink.Name = "UPnPLink";
            this.UPnPLink.Size = new System.Drawing.Size(88, 13);
            this.UPnPLink.TabIndex = 29;
            this.UPnPLink.TabStop = true;
            this.UPnPLink.Text = "UPnP Diagnostic";
            this.UPnPLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.UPnPLink_LinkClicked);
            // 
            // OpLanBox
            // 
            this.OpLanBox.Location = new System.Drawing.Point(182, 34);
            this.OpLanBox.Name = "OpLanBox";
            this.OpLanBox.ReadOnly = true;
            this.OpLanBox.Size = new System.Drawing.Size(47, 20);
            this.OpLanBox.TabIndex = 23;
            this.OpLanBox.Text = "65536";
            this.OpLanBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(192, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(28, 13);
            this.label1.TabIndex = 24;
            this.label1.Text = "LAN";
            // 
            // GlobalLanBox
            // 
            this.LookupLanBox.Location = new System.Drawing.Point(182, 60);
            this.LookupLanBox.Name = "GlobalLanBox";
            this.LookupLanBox.ReadOnly = true;
            this.LookupLanBox.Size = new System.Drawing.Size(47, 20);
            this.LookupLanBox.TabIndex = 25;
            this.LookupLanBox.Text = "65536";
            this.LookupLanBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(23, 94);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(259, 38);
            this.label6.TabIndex = 19;
            this.label6.Text = "For optimal performance ensure the these ports are open on your router.";
            // 
            // Connecting
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CloseButton;
            this.ClientSize = new System.Drawing.Size(322, 324);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.SetupLink);
            this.Controls.Add(this.RemoveLink);
            this.Controls.Add(this.AddLink);
            this.Controls.Add(this.CacheList);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.CloseButton);
            this.Controls.Add(this.OKButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Connecting";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Connecting";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button CloseButton;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.TextBox OpTcpBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label OperationLabel;
        private System.Windows.Forms.Label LookupLabel;
        private System.Windows.Forms.TextBox LookupTcpBox;
        private System.Windows.Forms.TextBox LookupUdpBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox OpStatusBox;
        private System.Windows.Forms.TextBox LookupStatusBox;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ListBox CacheList;
        private System.Windows.Forms.LinkLabel AddLink;
        private System.Windows.Forms.LinkLabel RemoveLink;
        private System.Windows.Forms.LinkLabel SetupLink;
        private System.Windows.Forms.TextBox OpUdpBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox OpLanBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox LookupLanBox;
        private System.Windows.Forms.LinkLabel UPnPLink;
        private System.Windows.Forms.Label label6;
    }
}