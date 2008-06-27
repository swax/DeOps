namespace RiseOp.Services.Plan
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
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.SelectGoalButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.DetailsButton = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.DetailsBrowser = new System.Windows.Forms.WebBrowser();
            this.SaveButton = new System.Windows.Forms.PictureBox();
            this.DiscardButton = new System.Windows.Forms.PictureBox();
            MainPanel = new GoalPanel();
            this.toolStrip1.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SaveButton)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DiscardButton)).BeginInit();
            this.SuspendLayout();
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
            this.splitContainer1.Size = new System.Drawing.Size(489, 379);
            this.splitContainer1.SplitterDistance = 313;
            this.splitContainer1.TabIndex = 21;
            // 
            // MainPanel
            // 
            this.MainPanel.BackColor = System.Drawing.Color.WhiteSmoke;
            this.MainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainPanel.Location = new System.Drawing.Point(0, 0);
            this.MainPanel.Name = "MainPanel";
            this.MainPanel.Size = new System.Drawing.Size(313, 379);
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
            this.DetailsBrowser.ScriptErrorsSuppressed = true;
            this.DetailsBrowser.Size = new System.Drawing.Size(172, 379);
            this.DetailsBrowser.TabIndex = 0;
            // 
            // SaveButton
            // 
            this.SaveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveButton.Image = global::RiseOp.Properties.Resources.save;
            this.SaveButton.Location = new System.Drawing.Point(336, 410);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(64, 19);
            this.SaveButton.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.SaveButton.TabIndex = 22;
            this.SaveButton.TabStop = false;
            this.SaveButton.Visible = false;
            this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // DiscardButton
            // 
            this.DiscardButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DiscardButton.Image = global::RiseOp.Properties.Resources.discard;
            this.DiscardButton.Location = new System.Drawing.Point(406, 410);
            this.DiscardButton.Name = "DiscardButton";
            this.DiscardButton.Size = new System.Drawing.Size(64, 19);
            this.DiscardButton.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.DiscardButton.TabIndex = 23;
            this.DiscardButton.TabStop = false;
            this.DiscardButton.Visible = false;
            this.DiscardButton.Click += new System.EventHandler(this.DiscardButton_Click);
            // 
            // GoalsView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.Controls.Add(this.DiscardButton);
            this.Controls.Add(this.SaveButton);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "GoalsView";
            this.Size = new System.Drawing.Size(489, 432);
            this.Load += new System.EventHandler(this.GoalsView_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SaveButton)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DiscardButton)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStripDropDownButton SelectGoalButton;
        private System.Windows.Forms.ToolStripButton DetailsButton;
        private GoalPanel MainPanel;
        private System.Windows.Forms.WebBrowser DetailsBrowser;
        private System.Windows.Forms.PictureBox SaveButton;
        private System.Windows.Forms.PictureBox DiscardButton;
    }
}
