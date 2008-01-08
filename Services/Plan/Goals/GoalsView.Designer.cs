namespace DeOps.Services.Plan
{
    partial class GoalsView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GoalsView));
            this.DiscardLink = new System.Windows.Forms.LinkLabel();
            this.SaveLink = new System.Windows.Forms.LinkLabel();
            this.ChangesLabel = new System.Windows.Forms.Label();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.SelectGoalButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.DetailsButton = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.DetailsBrowser = new System.Windows.Forms.WebBrowser();
            MainPanel = new GoalPanel();
            this.toolStrip1.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // DiscardLink
            // 
            this.DiscardLink.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
            this.DiscardLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DiscardLink.AutoSize = true;
            this.DiscardLink.BackColor = System.Drawing.Color.Red;
            this.DiscardLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.DiscardLink.LinkColor = System.Drawing.Color.White;
            this.DiscardLink.Location = new System.Drawing.Point(442, 417);
            this.DiscardLink.Name = "DiscardLink";
            this.DiscardLink.Size = new System.Drawing.Size(43, 13);
            this.DiscardLink.TabIndex = 18;
            this.DiscardLink.TabStop = true;
            this.DiscardLink.Text = "Discard";
            this.DiscardLink.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.DiscardLink.Visible = false;
            this.DiscardLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.DiscardLink_LinkClicked);
            // 
            // SaveLink
            // 
            this.SaveLink.ActiveLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.SaveLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveLink.AutoSize = true;
            this.SaveLink.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.SaveLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.SaveLink.LinkColor = System.Drawing.Color.White;
            this.SaveLink.Location = new System.Drawing.Point(404, 417);
            this.SaveLink.Name = "SaveLink";
            this.SaveLink.Size = new System.Drawing.Size(32, 13);
            this.SaveLink.TabIndex = 17;
            this.SaveLink.TabStop = true;
            this.SaveLink.Text = "Save";
            this.SaveLink.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.SaveLink.Visible = false;
            this.SaveLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SaveLink_LinkClicked);
            // 
            // ChangesLabel
            // 
            this.ChangesLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ChangesLabel.AutoSize = true;
            this.ChangesLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ChangesLabel.Location = new System.Drawing.Point(346, 417);
            this.ChangesLabel.Name = "ChangesLabel";
            this.ChangesLabel.Size = new System.Drawing.Size(56, 13);
            this.ChangesLabel.TabIndex = 19;
            this.ChangesLabel.Text = "Changes";
            this.ChangesLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.ChangesLabel.Visible = false;
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SelectGoalButton,
            this.DetailsButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(489, 25);
            this.toolStrip1.TabIndex = 20;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // SelectGoalButton
            // 
            this.SelectGoalButton.Image = ((System.Drawing.Image)(resources.GetObject("SelectGoalButton.Image")));
            this.SelectGoalButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.SelectGoalButton.Name = "SelectGoalButton";
            this.SelectGoalButton.Size = new System.Drawing.Size(89, 22);
            this.SelectGoalButton.Text = "Select Goal";
            this.SelectGoalButton.DropDownOpening += new System.EventHandler(this.SelectGoal_DropDownOpening);
            // 
            // DetailsButton
            // 
            this.DetailsButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.DetailsButton.CheckOnClick = true;
            this.DetailsButton.Image = ((System.Drawing.Image)(resources.GetObject("DetailsButton.Image")));
            this.DetailsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.DetailsButton.Name = "DetailsButton";
            this.DetailsButton.Size = new System.Drawing.Size(59, 22);
            this.DetailsButton.Text = "Details";
            this.DetailsButton.CheckedChanged += new System.EventHandler(this.DetailsButton_CheckedChanged);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.MainPanel);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.DetailsBrowser);
            this.splitContainer1.Size = new System.Drawing.Size(489, 389);
            this.splitContainer1.SplitterDistance = 313;
            this.splitContainer1.TabIndex = 21;
            // 
            // MainPanel
            // 
            this.MainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainPanel.Location = new System.Drawing.Point(0, 0);
            this.MainPanel.Name = "MainPanel";
            this.MainPanel.Size = new System.Drawing.Size(313, 389);
            this.MainPanel.TabIndex = 0;
            // 
            // DetailsBrowser
            // 
            this.DetailsBrowser.AllowWebBrowserDrop = false;
            this.DetailsBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DetailsBrowser.IsWebBrowserContextMenuEnabled = false;
            this.DetailsBrowser.Location = new System.Drawing.Point(0, 0);
            this.DetailsBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.DetailsBrowser.Name = "DetailsBrowser";
            this.DetailsBrowser.Size = new System.Drawing.Size(172, 389);
            this.DetailsBrowser.TabIndex = 0;
            this.DetailsBrowser.ScriptErrorsSuppressed = true;
            // 
            // GoalsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.ChangesLabel);
            this.Controls.Add(this.DiscardLink);
            this.Controls.Add(this.SaveLink);
            this.Name = "GoalsView";
            this.Size = new System.Drawing.Size(489, 432);
            this.Load += new System.EventHandler(this.GoalsView_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.LinkLabel DiscardLink;
        private System.Windows.Forms.LinkLabel SaveLink;
        private System.Windows.Forms.Label ChangesLabel;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStripDropDownButton SelectGoalButton;
        private System.Windows.Forms.ToolStripButton DetailsButton;
        private GoalPanel MainPanel;
        private System.Windows.Forms.WebBrowser DetailsBrowser;
    }
}
