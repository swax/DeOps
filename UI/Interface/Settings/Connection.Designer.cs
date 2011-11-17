namespace DeOps.Interface.Settings
{
    partial class Connection
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
            this.label4 = new System.Windows.Forms.Label();
            this.UPnPLink = new System.Windows.Forms.LinkLabel();
            this.OpLanBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.LookupLanBox = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.linkLabel2 = new System.Windows.Forms.LinkLabel();
            this.linkLabel3 = new System.Windows.Forms.LinkLabel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // CloseButton
            // 
            this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CloseButton.Location = new System.Drawing.Point(209, 211);
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
            this.OKButton.Location = new System.Drawing.Point(128, 211);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 8;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // OpTcpBox
            // 
            this.OpTcpBox.Location = new System.Drawing.Point(81, 148);
            this.OpTcpBox.Name = "OpTcpBox";
            this.OpTcpBox.Size = new System.Drawing.Size(47, 20);
            this.OpTcpBox.TabIndex = 10;
            this.OpTcpBox.Text = "65536";
            this.OpTcpBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(89, 130);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(31, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "TCP";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(145, 130);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(33, 13);
            this.label3.TabIndex = 13;
            this.label3.Text = "UDP";
            // 
            // OperationLabel
            // 
            this.OperationLabel.AutoSize = true;
            this.OperationLabel.Location = new System.Drawing.Point(15, 151);
            this.OperationLabel.Name = "OperationLabel";
            this.OperationLabel.Size = new System.Drawing.Size(53, 13);
            this.OperationLabel.TabIndex = 14;
            this.OperationLabel.Text = "Organization";
            // 
            // LookupLabel
            // 
            this.LookupLabel.AutoSize = true;
            this.LookupLabel.Location = new System.Drawing.Point(15, 177);
            this.LookupLabel.Name = "LookupLabel";
            this.LookupLabel.Size = new System.Drawing.Size(43, 13);
            this.LookupLabel.TabIndex = 15;
            this.LookupLabel.Text = "Lookup";
            // 
            // OpUdpBox
            // 
            this.OpUdpBox.Location = new System.Drawing.Point(134, 148);
            this.OpUdpBox.Name = "OpUdpBox";
            this.OpUdpBox.Size = new System.Drawing.Size(47, 20);
            this.OpUdpBox.TabIndex = 16;
            this.OpUdpBox.Text = "65536";
            this.OpUdpBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // LookupTcpBox
            // 
            this.LookupTcpBox.Location = new System.Drawing.Point(81, 174);
            this.LookupTcpBox.Name = "LookupTcpBox";
            this.LookupTcpBox.Size = new System.Drawing.Size(47, 20);
            this.LookupTcpBox.TabIndex = 17;
            this.LookupTcpBox.Text = "65536";
            this.LookupTcpBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // LookupUdpBox
            // 
            this.LookupUdpBox.Location = new System.Drawing.Point(134, 174);
            this.LookupUdpBox.Name = "LookupUdpBox";
            this.LookupUdpBox.Size = new System.Drawing.Size(47, 20);
            this.LookupUdpBox.TabIndex = 18;
            this.LookupUdpBox.Text = "65536";
            this.LookupUdpBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(243, 130);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(43, 13);
            this.label7.TabIndex = 20;
            this.label7.Text = "Status";
            // 
            // OpStatusBox
            // 
            this.OpStatusBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.OpStatusBox.ForeColor = System.Drawing.Color.White;
            this.OpStatusBox.Location = new System.Drawing.Point(240, 148);
            this.OpStatusBox.Name = "OpStatusBox";
            this.OpStatusBox.ReadOnly = true;
            this.OpStatusBox.Size = new System.Drawing.Size(47, 20);
            this.OpStatusBox.TabIndex = 21;
            this.OpStatusBox.Text = "Open";
            this.OpStatusBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // LookupStatusBox
            // 
            this.LookupStatusBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.LookupStatusBox.ForeColor = System.Drawing.Color.White;
            this.LookupStatusBox.Location = new System.Drawing.Point(240, 174);
            this.LookupStatusBox.Name = "LookupStatusBox";
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
            this.label8.Location = new System.Drawing.Point(3, 0);
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
            this.CacheList.IntegralHeight = false;
            this.CacheList.Location = new System.Drawing.Point(6, 18);
            this.CacheList.Name = "CacheList";
            this.CacheList.Size = new System.Drawing.Size(58, 28);
            this.CacheList.TabIndex = 24;
            // 
            // AddLink
            // 
            this.AddLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AddLink.AutoSize = true;
            this.AddLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.AddLink.Location = new System.Drawing.Point(-54, 0);
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
            this.RemoveLink.Location = new System.Drawing.Point(-24, 0);
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
            this.SetupLink.Location = new System.Drawing.Point(29, 0);
            this.SetupLink.Name = "SetupLink";
            this.SetupLink.Size = new System.Drawing.Size(35, 13);
            this.SetupLink.TabIndex = 27;
            this.SetupLink.TabStop = true;
            this.SetupLink.Text = "Setup";
            this.SetupLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SetupLink_LinkClicked);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(12, 130);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(36, 13);
            this.label4.TabIndex = 30;
            this.label4.Text = "Ports";
            // 
            // UPnPLink
            // 
            this.UPnPLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.UPnPLink.AutoSize = true;
            this.UPnPLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.UPnPLink.Location = new System.Drawing.Point(15, 216);
            this.UPnPLink.Name = "UPnPLink";
            this.UPnPLink.Size = new System.Drawing.Size(88, 13);
            this.UPnPLink.TabIndex = 29;
            this.UPnPLink.TabStop = true;
            this.UPnPLink.Text = "UPnP Diagnostic";
            this.UPnPLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.UPnPLink_LinkClicked);
            // 
            // OpLanBox
            // 
            this.OpLanBox.Location = new System.Drawing.Point(187, 148);
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
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(197, 130);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 13);
            this.label1.TabIndex = 24;
            this.label1.Text = "LAN";
            // 
            // LookupLanBox
            // 
            this.LookupLanBox.Location = new System.Drawing.Point(187, 174);
            this.LookupLanBox.Name = "LookupLanBox";
            this.LookupLanBox.ReadOnly = true;
            this.LookupLanBox.Size = new System.Drawing.Size(47, 20);
            this.LookupLanBox.TabIndex = 25;
            this.LookupLanBox.Text = "65536";
            this.LookupLanBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label8);
            this.panel1.Controls.Add(this.CacheList);
            this.panel1.Controls.Add(this.SetupLink);
            this.panel1.Controls.Add(this.AddLink);
            this.panel1.Controls.Add(this.RemoveLink);
            this.panel1.Location = new System.Drawing.Point(290, 231);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(67, 49);
            this.panel1.TabIndex = 29;
            this.panel1.Visible = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(12, 9);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(61, 13);
            this.label5.TabIndex = 31;
            this.label5.Text = "Bootstrap";
            // 
            // listBox1
            // 
            this.listBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.IntegralHeight = false;
            this.listBox1.Location = new System.Drawing.Point(18, 27);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(269, 78);
            this.listBox1.TabIndex = 32;
            // 
            // linkLabel1
            // 
            this.linkLabel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.linkLabel1.Location = new System.Drawing.Point(225, 11);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(62, 13);
            this.linkLabel1.TabIndex = 35;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "My Address";
            // 
            // linkLabel2
            // 
            this.linkLabel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.linkLabel2.AutoSize = true;
            this.linkLabel2.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.linkLabel2.Location = new System.Drawing.Point(140, 11);
            this.linkLabel2.Name = "linkLabel2";
            this.linkLabel2.Size = new System.Drawing.Size(26, 13);
            this.linkLabel2.TabIndex = 33;
            this.linkLabel2.TabStop = true;
            this.linkLabel2.Text = "Add";
            // 
            // linkLabel3
            // 
            this.linkLabel3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.linkLabel3.AutoSize = true;
            this.linkLabel3.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.linkLabel3.Location = new System.Drawing.Point(172, 11);
            this.linkLabel3.Name = "linkLabel3";
            this.linkLabel3.Size = new System.Drawing.Size(47, 13);
            this.linkLabel3.TabIndex = 34;
            this.linkLabel3.TabStop = true;
            this.linkLabel3.Text = "Remove";
            // 
            // Connecting
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CloseButton;
            this.ClientSize = new System.Drawing.Size(296, 246);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.linkLabel2);
            this.Controls.Add(this.linkLabel3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.UPnPLink);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.OpLanBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.CloseButton);
            this.Controls.Add(this.LookupLanBox);
            this.Controls.Add(this.OKButton);
            this.Controls.Add(this.OpTcpBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.LookupUdpBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.OperationLabel);
            this.Controls.Add(this.LookupTcpBox);
            this.Controls.Add(this.LookupLabel);
            this.Controls.Add(this.OpStatusBox);
            this.Controls.Add(this.LookupStatusBox);
            this.Controls.Add(this.OpUdpBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Connection";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Connection";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
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
        private System.Windows.Forms.TextBox OpLanBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox LookupLanBox;
        private System.Windows.Forms.LinkLabel UPnPLink;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.LinkLabel linkLabel2;
        private System.Windows.Forms.LinkLabel linkLabel3;
    }
}