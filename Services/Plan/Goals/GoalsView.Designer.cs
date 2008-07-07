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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.DetailsBrowser = new System.Windows.Forms.WebBrowser();
            this.DiscardButton = new RiseOp.Interface.Views.ImageButton();
            this.SaveButton = new RiseOp.Interface.Views.ImageButton();
            this.SelectGoalButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.DetailsButton = new System.Windows.Forms.ToolStripButton();
            MainPanel = new GoalPanel();
            this.toolStrip1.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DiscardButton)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SaveButton)).BeginInit();
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
            this.toolStrip1.Size = new System.Drawing.Size(594, 25);
            this.toolStrip1.TabIndex = 20;
            this.toolStrip1.Text = "toolStrip1";
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
            this.splitContainer1.Panel2.Controls.Add(this.groupBox1);
            this.splitContainer1.Size = new System.Drawing.Size(594, 353);
            this.splitContainer1.SplitterDistance = 418;
            this.splitContainer1.TabIndex = 21;
            // 
            // MainPanel
            // 
            this.MainPanel.BackColor = System.Drawing.Color.WhiteSmoke;
            this.MainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainPanel.Location = new System.Drawing.Point(0, 0);
            this.MainPanel.Name = "MainPanel";
            this.MainPanel.Size = new System.Drawing.Size(418, 353);
            this.MainPanel.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.DetailsBrowser);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(166, 347);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Details";
            // 
            // DetailsBrowser
            // 
            this.DetailsBrowser.AllowWebBrowserDrop = false;
            this.DetailsBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DetailsBrowser.IsWebBrowserContextMenuEnabled = false;
            this.DetailsBrowser.Location = new System.Drawing.Point(3, 16);
            this.DetailsBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.DetailsBrowser.Name = "DetailsBrowser";
            this.DetailsBrowser.ScriptErrorsSuppressed = true;
            this.DetailsBrowser.Size = new System.Drawing.Size(160, 328);
            this.DetailsBrowser.TabIndex = 0;
            // 
            // DiscardButton
            // 
            this.DiscardButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DiscardButton.ButtonDown = global::RiseOp.Properties.Resources.discard_down;
            this.DiscardButton.ButtonHot = global::RiseOp.Properties.Resources.discard_hot;
            this.DiscardButton.ButtonNormal = global::RiseOp.Properties.Resources.discard_norm;
            this.DiscardButton.Image = global::RiseOp.Properties.Resources.discard_norm;
            this.DiscardButton.Location = new System.Drawing.Point(511, 384);
            this.DiscardButton.Name = "DiscardButton";
            this.DiscardButton.Size = new System.Drawing.Size(64, 19);
            this.DiscardButton.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.DiscardButton.TabIndex = 23;
            this.DiscardButton.TabStop = false;
            this.DiscardButton.Visible = false;
            this.DiscardButton.Click += new System.EventHandler(this.DiscardButton_Click);
            // 
            // SaveButton
            // 
            this.SaveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveButton.ButtonDown = global::RiseOp.Properties.Resources.save_down;
            this.SaveButton.ButtonHot = global::RiseOp.Properties.Resources.save_hot;
            this.SaveButton.ButtonNormal = global::RiseOp.Properties.Resources.save_norm;
            this.SaveButton.Image = global::RiseOp.Properties.Resources.save_norm;
            this.SaveButton.Location = new System.Drawing.Point(441, 384);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(64, 19);
            this.SaveButton.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.SaveButton.TabIndex = 22;
            this.SaveButton.TabStop = false;
            this.SaveButton.Visible = false;
            this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
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
            this.DetailsButton.Click += new System.EventHandler(this.DetailsButton_Click);
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
            this.Size = new System.Drawing.Size(594, 406);
            this.Load += new System.EventHandler(this.GoalsView_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.DiscardButton)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SaveButton)).EndInit();
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
        private RiseOp.Interface.Views.ImageButton SaveButton;
        private RiseOp.Interface.Views.ImageButton DiscardButton;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}
