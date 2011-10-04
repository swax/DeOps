namespace DeOps.Interface.Info
{
    partial class InfoView
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InfoView));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.networkPanel1 = new DeOps.Interface.Tools.NetworkPanel();
            this.webBrowser1 = new DeOps.Interface.Views.WebBrowserEx();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.HelpButton = new System.Windows.Forms.ToolStripButton();
            this.NewsButton = new System.Windows.Forms.ToolStripButton();
            this.NetworkButton = new System.Windows.Forms.ToolStripButton();
            this.SecondTimer = new System.Windows.Forms.Timer(this.components);
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.BackColor = System.Drawing.Color.White;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.networkPanel1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.webBrowser1);
            this.splitContainer1.Size = new System.Drawing.Size(531, 268);
            this.splitContainer1.SplitterDistance = 264;
            this.splitContainer1.TabIndex = 0;
            // 
            // networkPanel1
            // 
            this.networkPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.networkPanel1.Location = new System.Drawing.Point(0, 0);
            this.networkPanel1.Name = "networkPanel1";
            this.networkPanel1.Size = new System.Drawing.Size(265, 268);
            this.networkPanel1.TabIndex = 0;
            // 
            // webBrowser1
            // 
            this.webBrowser1.AllowWebBrowserDrop = false;
            this.webBrowser1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.webBrowser1.IsWebBrowserContextMenuEnabled = false;
            this.webBrowser1.Location = new System.Drawing.Point(3, 0);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.ScriptErrorsSuppressed = true;
            this.webBrowser1.Size = new System.Drawing.Size(260, 268);
            this.webBrowser1.TabIndex = 0;
            this.webBrowser1.WebBrowserShortcutsEnabled = false;
            this.webBrowser1.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.webBrowser1_DocumentCompleted);
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.HelpButton,
            this.NewsButton,
            this.NetworkButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(531, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // HelpButton
            // 
            this.HelpButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.HelpButton.CheckOnClick = true;
            this.HelpButton.Image = ((System.Drawing.Image)(resources.GetObject("HelpButton.Image")));
            this.HelpButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.HelpButton.Name = "HelpButton";
            this.HelpButton.Size = new System.Drawing.Size(48, 22);
            this.HelpButton.Text = "Help";
            this.HelpButton.CheckedChanged += new System.EventHandler(this.HelpButton_CheckedChanged);
            // 
            // NewsButton
            // 
            this.NewsButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.NewsButton.CheckOnClick = true;
            this.NewsButton.Image = ((System.Drawing.Image)(resources.GetObject("NewsButton.Image")));
            this.NewsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.NewsButton.Name = "NewsButton";
            this.NewsButton.Size = new System.Drawing.Size(53, 22);
            this.NewsButton.Text = "News";
            this.NewsButton.CheckedChanged += new System.EventHandler(this.NewsButton_CheckedChanged);
            // 
            // NetworkButton
            // 
            this.NetworkButton.Checked = true;
            this.NetworkButton.CheckOnClick = true;
            this.NetworkButton.CheckState = System.Windows.Forms.CheckState.Checked;
            this.NetworkButton.Image = ((System.Drawing.Image)(resources.GetObject("NetworkButton.Image")));
            this.NetworkButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.NetworkButton.Name = "NetworkButton";
            this.NetworkButton.Size = new System.Drawing.Size(67, 22);
            this.NetworkButton.Text = "Network";
            this.NetworkButton.CheckedChanged += new System.EventHandler(this.NetworkButton_CheckedChanged);
            // 
            // SecondTimer
            // 
            this.SecondTimer.Enabled = true;
            this.SecondTimer.Interval = 1000;
            this.SecondTimer.Tick += new System.EventHandler(this.SecondTimer_Tick);
            // 
            // InfoView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.splitContainer1);
            this.Name = "InfoView";
            this.Size = new System.Drawing.Size(531, 293);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private DeOps.Interface.Views.WebBrowserEx webBrowser1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton NewsButton;
        private System.Windows.Forms.ToolStripButton HelpButton;
        private System.Windows.Forms.ToolStripButton NetworkButton;
        private DeOps.Interface.Tools.NetworkPanel networkPanel1;
        private System.Windows.Forms.Timer SecondTimer;
    }
}
