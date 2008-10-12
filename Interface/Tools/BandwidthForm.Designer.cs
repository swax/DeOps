namespace RiseOp.Interface.Tools
{
    partial class BandwidthForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BandwidthForm));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.BandwidthGraph = new ZedGraph.ZedGraphControl();
            this.PauseButton = new System.Windows.Forms.Button();
            this.CoresList = new System.Windows.Forms.ListView();
            this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader4 = new System.Windows.Forms.ColumnHeader();
            this.ServiceList = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.AverageLink = new System.Windows.Forms.LinkLabel();
            this.RecordLink = new System.Windows.Forms.LinkLabel();
            this.RudpRadio = new System.Windows.Forms.RadioButton();
            this.TcpBox = new System.Windows.Forms.CheckBox();
            this.UdpBox = new System.Windows.Forms.CheckBox();
            this.FilterGlobal = new System.Windows.Forms.CheckBox();
            this.TransportRadio = new System.Windows.Forms.RadioButton();
            this.ServiceRadio = new System.Windows.Forms.RadioButton();
            this.CoresLabel = new System.Windows.Forms.Label();
            this.SecondTimer = new System.Windows.Forms.Timer(this.components);
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.BandwidthGraph);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.PauseButton);
            this.splitContainer1.Panel2.Controls.Add(this.CoresList);
            this.splitContainer1.Panel2.Controls.Add(this.ServiceList);
            this.splitContainer1.Panel2.Controls.Add(this.AverageLink);
            this.splitContainer1.Panel2.Controls.Add(this.RecordLink);
            this.splitContainer1.Panel2.Controls.Add(this.RudpRadio);
            this.splitContainer1.Panel2.Controls.Add(this.TcpBox);
            this.splitContainer1.Panel2.Controls.Add(this.UdpBox);
            this.splitContainer1.Panel2.Controls.Add(this.FilterGlobal);
            this.splitContainer1.Panel2.Controls.Add(this.TransportRadio);
            this.splitContainer1.Panel2.Controls.Add(this.ServiceRadio);
            this.splitContainer1.Panel2.Controls.Add(this.CoresLabel);
            this.splitContainer1.Size = new System.Drawing.Size(560, 592);
            this.splitContainer1.SplitterDistance = 334;
            this.splitContainer1.TabIndex = 0;
            // 
            // BandwidthGraph
            // 
            this.BandwidthGraph.Dock = System.Windows.Forms.DockStyle.Fill;
            this.BandwidthGraph.IsAntiAlias = true;
            this.BandwidthGraph.Location = new System.Drawing.Point(0, 0);
            this.BandwidthGraph.Name = "BandwidthGraph";
            this.BandwidthGraph.ScrollGrace = 0;
            this.BandwidthGraph.ScrollMaxX = 0;
            this.BandwidthGraph.ScrollMaxY = 0;
            this.BandwidthGraph.ScrollMaxY2 = 0;
            this.BandwidthGraph.ScrollMinX = 0;
            this.BandwidthGraph.ScrollMinY = 0;
            this.BandwidthGraph.ScrollMinY2 = 0;
            this.BandwidthGraph.Size = new System.Drawing.Size(560, 334);
            this.BandwidthGraph.TabIndex = 0;
            // 
            // PauseButton
            // 
            this.PauseButton.Location = new System.Drawing.Point(257, 174);
            this.PauseButton.Name = "PauseButton";
            this.PauseButton.Size = new System.Drawing.Size(75, 23);
            this.PauseButton.TabIndex = 27;
            this.PauseButton.Text = "Pause";
            this.PauseButton.UseVisualStyleBackColor = true;
            this.PauseButton.Click += new System.EventHandler(this.PauseButton_Click);
            // 
            // CoresList
            // 
            this.CoresList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.CoresList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader3,
            this.columnHeader4});
            this.CoresList.FullRowSelect = true;
            this.CoresList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.CoresList.HideSelection = false;
            this.CoresList.Location = new System.Drawing.Point(12, 37);
            this.CoresList.Name = "CoresList";
            this.CoresList.Size = new System.Drawing.Size(216, 204);
            this.CoresList.TabIndex = 26;
            this.CoresList.UseCompatibleStateImageBehavior = false;
            this.CoresList.View = System.Windows.Forms.View.Details;
            this.CoresList.SelectedIndexChanged += new System.EventHandler(this.CoresList_SelectedIndexChanged);
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Core";
            this.columnHeader3.Width = 119;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Bytes";
            this.columnHeader4.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader4.Width = 65;
            // 
            // ServiceList
            // 
            this.ServiceList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.ServiceList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.ServiceList.FullRowSelect = true;
            this.ServiceList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.ServiceList.HideSelection = false;
            this.ServiceList.Location = new System.Drawing.Point(399, 37);
            this.ServiceList.Name = "ServiceList";
            this.ServiceList.Size = new System.Drawing.Size(149, 204);
            this.ServiceList.TabIndex = 25;
            this.ServiceList.UseCompatibleStateImageBehavior = false;
            this.ServiceList.View = System.Windows.Forms.View.Details;
            this.ServiceList.SelectedIndexChanged += new System.EventHandler(this.ServiceList_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Service";
            this.columnHeader1.Width = 63;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Bytes";
            this.columnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader2.Width = 56;
            // 
            // AverageLink
            // 
            this.AverageLink.AutoSize = true;
            this.AverageLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.AverageLink.Location = new System.Drawing.Point(254, 228);
            this.AverageLink.Name = "AverageLink";
            this.AverageLink.Size = new System.Drawing.Size(88, 13);
            this.AverageLink.TabIndex = 24;
            this.AverageLink.TabStop = true;
            this.AverageLink.Text = "Average over 5 s";
            this.AverageLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.AverageLink_LinkClicked);
            // 
            // RecordLink
            // 
            this.RecordLink.AutoSize = true;
            this.RecordLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.RecordLink.Location = new System.Drawing.Point(254, 210);
            this.RecordLink.Name = "RecordLink";
            this.RecordLink.Size = new System.Drawing.Size(59, 13);
            this.RecordLink.TabIndex = 23;
            this.RecordLink.TabStop = true;
            this.RecordLink.Text = "Record 5 s";
            this.RecordLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.RecordLink_LinkClicked);
            // 
            // RudpRadio
            // 
            this.RudpRadio.AutoSize = true;
            this.RudpRadio.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RudpRadio.Location = new System.Drawing.Point(257, 92);
            this.RudpRadio.Name = "RudpRadio";
            this.RudpRadio.Size = new System.Drawing.Size(60, 17);
            this.RudpRadio.TabIndex = 18;
            this.RudpRadio.Text = "RUDP";
            this.RudpRadio.UseVisualStyleBackColor = true;
            this.RudpRadio.CheckedChanged += new System.EventHandler(this.RudpRadio_CheckedChanged);
            // 
            // TcpBox
            // 
            this.TcpBox.AutoSize = true;
            this.TcpBox.Checked = true;
            this.TcpBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.TcpBox.Location = new System.Drawing.Point(277, 37);
            this.TcpBox.Name = "TcpBox";
            this.TcpBox.Size = new System.Drawing.Size(47, 17);
            this.TcpBox.TabIndex = 16;
            this.TcpBox.Text = "TCP";
            this.TcpBox.UseVisualStyleBackColor = true;
            this.TcpBox.CheckedChanged += new System.EventHandler(this.TcpBox_CheckedChanged);
            // 
            // UdpBox
            // 
            this.UdpBox.AutoSize = true;
            this.UdpBox.Checked = true;
            this.UdpBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.UdpBox.Location = new System.Drawing.Point(277, 60);
            this.UdpBox.Name = "UdpBox";
            this.UdpBox.Size = new System.Drawing.Size(49, 17);
            this.UdpBox.TabIndex = 17;
            this.UdpBox.Text = "UDP";
            this.UdpBox.UseVisualStyleBackColor = true;
            this.UdpBox.CheckedChanged += new System.EventHandler(this.UdpBox_CheckedChanged);
            // 
            // FilterGlobal
            // 
            this.FilterGlobal.AutoSize = true;
            this.FilterGlobal.Location = new System.Drawing.Point(147, 15);
            this.FilterGlobal.Name = "FilterGlobal";
            this.FilterGlobal.Size = new System.Drawing.Size(81, 17);
            this.FilterGlobal.TabIndex = 11;
            this.FilterGlobal.Text = "Filter Global";
            this.FilterGlobal.UseVisualStyleBackColor = true;
            this.FilterGlobal.CheckedChanged += new System.EventHandler(this.FilterGlobal_CheckedChanged);
            // 
            // TransportRadio
            // 
            this.TransportRadio.AutoSize = true;
            this.TransportRadio.Checked = true;
            this.TransportRadio.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TransportRadio.Location = new System.Drawing.Point(257, 14);
            this.TransportRadio.Name = "TransportRadio";
            this.TransportRadio.Size = new System.Drawing.Size(79, 17);
            this.TransportRadio.TabIndex = 10;
            this.TransportRadio.TabStop = true;
            this.TransportRadio.Text = "Transport";
            this.TransportRadio.UseVisualStyleBackColor = true;
            this.TransportRadio.CheckedChanged += new System.EventHandler(this.TransportRadio_CheckedChanged);
            // 
            // ServiceRadio
            // 
            this.ServiceRadio.AutoSize = true;
            this.ServiceRadio.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ServiceRadio.Location = new System.Drawing.Point(399, 14);
            this.ServiceRadio.Name = "ServiceRadio";
            this.ServiceRadio.Size = new System.Drawing.Size(74, 17);
            this.ServiceRadio.TabIndex = 9;
            this.ServiceRadio.Text = "Services";
            this.ServiceRadio.UseVisualStyleBackColor = true;
            this.ServiceRadio.CheckedChanged += new System.EventHandler(this.ServiceRadio_CheckedChanged);
            // 
            // CoresLabel
            // 
            this.CoresLabel.AutoSize = true;
            this.CoresLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CoresLabel.Location = new System.Drawing.Point(12, 16);
            this.CoresLabel.Name = "CoresLabel";
            this.CoresLabel.Size = new System.Drawing.Size(39, 13);
            this.CoresLabel.TabIndex = 1;
            this.CoresLabel.Text = "Cores";
            // 
            // SecondTimer
            // 
            this.SecondTimer.Enabled = true;
            this.SecondTimer.Interval = 1000;
            this.SecondTimer.Tick += new System.EventHandler(this.SecondTimer_Tick);
            // 
            // BandwidthForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(560, 592);
            this.Controls.Add(this.splitContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "BandwidthForm";
            this.Text = "Bandwidth";
            this.Load += new System.EventHandler(this.BandwidthForm_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Label CoresLabel;
        private System.Windows.Forms.RadioButton ServiceRadio;
        private System.Windows.Forms.CheckBox FilterGlobal;
        private System.Windows.Forms.Timer SecondTimer;
        private System.Windows.Forms.RadioButton RudpRadio;
        private System.Windows.Forms.CheckBox TcpBox;
        private System.Windows.Forms.CheckBox UdpBox;
        private ZedGraph.ZedGraphControl BandwidthGraph;
        private System.Windows.Forms.RadioButton TransportRadio;
        private System.Windows.Forms.LinkLabel AverageLink;
        private System.Windows.Forms.LinkLabel RecordLink;
        private System.Windows.Forms.ListView ServiceList;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ListView CoresList;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.Button PauseButton;
    }
}