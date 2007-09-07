namespace DeOps.Simulator
{
    partial class SimForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SimForm));
            this.buttonStart = new System.Windows.Forms.Button();
            this.buttonPause = new System.Windows.Forms.Button();
            this.labelTime = new System.Windows.Forms.Label();
            this.listInstances = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader7 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader4 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader5 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader6 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader8 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader9 = new System.Windows.Forms.ColumnHeader();
            this.SecondTimer = new System.Windows.Forms.Timer(this.components);
            this.LabelInstances = new System.Windows.Forms.Label();
            this.LinkUpdate = new System.Windows.Forms.LinkLabel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.FileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.LoadMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SaveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ViewMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.ButtonStep = new System.Windows.Forms.Button();
            this.TimeLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.LoadProgress = new System.Windows.Forms.ProgressBar();
            this.menuStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonStart
            // 
            this.buttonStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStart.Location = new System.Drawing.Point(15, 32);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(75, 23);
            this.buttonStart.TabIndex = 0;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // buttonPause
            // 
            this.buttonPause.Location = new System.Drawing.Point(177, 32);
            this.buttonPause.Name = "buttonPause";
            this.buttonPause.Size = new System.Drawing.Size(75, 23);
            this.buttonPause.TabIndex = 1;
            this.buttonPause.Text = "Pause";
            this.buttonPause.UseVisualStyleBackColor = true;
            this.buttonPause.Click += new System.EventHandler(this.buttonPause_Click);
            // 
            // labelTime
            // 
            this.labelTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelTime.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.labelTime.Location = new System.Drawing.Point(389, 19);
            this.labelTime.Name = "labelTime";
            this.labelTime.Size = new System.Drawing.Size(130, 18);
            this.labelTime.TabIndex = 2;
            this.labelTime.Text = "00:00:00";
            this.labelTime.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // listInstances
            // 
            this.listInstances.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listInstances.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader3,
            this.columnHeader2,
            this.columnHeader7,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6,
            this.columnHeader8,
            this.columnHeader9});
            this.listInstances.FullRowSelect = true;
            this.listInstances.Location = new System.Drawing.Point(12, 171);
            this.listInstances.Name = "listInstances";
            this.listInstances.Size = new System.Drawing.Size(525, 255);
            this.listInstances.TabIndex = 3;
            this.listInstances.UseCompatibleStateImageBehavior = false;
            this.listInstances.View = System.Windows.Forms.View.Details;
            this.listInstances.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listInstances_MouseDoubleClick);
            this.listInstances.MouseClick += new System.Windows.Forms.MouseEventHandler(this.listInstances_MouseClick);
            this.listInstances.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listInstances_ColumnClick);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "User";
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Op";
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Dht";
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "Client";
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Firewall";
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Alerts";
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Proxied";
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "Notes";
            // 
            // columnHeader9
            // 
            this.columnHeader9.Text = "Bandwidth";
            // 
            // SecondTimer
            // 
            this.SecondTimer.Enabled = true;
            this.SecondTimer.Interval = 1000;
            this.SecondTimer.Tick += new System.EventHandler(this.SecondTimer_Tick);
            // 
            // LabelInstances
            // 
            this.LabelInstances.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LabelInstances.AutoSize = true;
            this.LabelInstances.Location = new System.Drawing.Point(12, 429);
            this.LabelInstances.Name = "LabelInstances";
            this.LabelInstances.Size = new System.Drawing.Size(62, 13);
            this.LabelInstances.TabIndex = 5;
            this.LabelInstances.Text = "0 Instances";
            // 
            // LinkUpdate
            // 
            this.LinkUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.LinkUpdate.AutoSize = true;
            this.LinkUpdate.Location = new System.Drawing.Point(495, 429);
            this.LinkUpdate.Name = "LinkUpdate";
            this.LinkUpdate.Size = new System.Drawing.Size(42, 13);
            this.LinkUpdate.TabIndex = 6;
            this.LinkUpdate.TabStop = true;
            this.LinkUpdate.Text = "Update";
            this.LinkUpdate.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.LinkUpdate.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkUpdate_LinkClicked);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileMenu,
            this.ViewMenu});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.menuStrip1.Size = new System.Drawing.Size(549, 24);
            this.menuStrip1.TabIndex = 8;
            this.menuStrip1.Text = "MainMenu";
            // 
            // FileMenu
            // 
            this.FileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.LoadMenuItem,
            this.SaveMenuItem,
            this.toolStripMenuItem1,
            this.ExitMenuItem});
            this.FileMenu.Name = "FileMenu";
            this.FileMenu.Size = new System.Drawing.Size(35, 20);
            this.FileMenu.Text = "File";
            // 
            // LoadMenuItem
            // 
            this.LoadMenuItem.Name = "LoadMenuItem";
            this.LoadMenuItem.Size = new System.Drawing.Size(109, 22);
            this.LoadMenuItem.Text = "Load";
            this.LoadMenuItem.Click += new System.EventHandler(this.LoadMenuItem_Click);
            // 
            // SaveMenuItem
            // 
            this.SaveMenuItem.Name = "SaveMenuItem";
            this.SaveMenuItem.Size = new System.Drawing.Size(109, 22);
            this.SaveMenuItem.Text = "Save";
            this.SaveMenuItem.Click += new System.EventHandler(this.SaveMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(106, 6);
            // 
            // ExitMenuItem
            // 
            this.ExitMenuItem.Name = "ExitMenuItem";
            this.ExitMenuItem.Size = new System.Drawing.Size(109, 22);
            this.ExitMenuItem.Text = "Exit";
            this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
            // 
            // ViewMenu
            // 
            this.ViewMenu.Name = "ViewMenu";
            this.ViewMenu.Size = new System.Drawing.Size(41, 20);
            this.ViewMenu.Text = "View";
            this.ViewMenu.DropDownOpening += new System.EventHandler(this.ViewMenu_DropDownOpening);
            // 
            // ButtonStep
            // 
            this.ButtonStep.Location = new System.Drawing.Point(96, 32);
            this.ButtonStep.Name = "ButtonStep";
            this.ButtonStep.Size = new System.Drawing.Size(75, 23);
            this.ButtonStep.TabIndex = 9;
            this.ButtonStep.Text = "Step";
            this.ButtonStep.UseVisualStyleBackColor = true;
            this.ButtonStep.Click += new System.EventHandler(this.ButtonStep_Click);
            // 
            // TimeLabel
            // 
            this.TimeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.TimeLabel.Location = new System.Drawing.Point(392, 37);
            this.TimeLabel.Name = "TimeLabel";
            this.TimeLabel.Size = new System.Drawing.Size(127, 13);
            this.TimeLabel.TabIndex = 10;
            this.TimeLabel.Text = "Date / Time";
            this.TimeLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(325, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Sim Speed:";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(333, 37);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "Sim Date:";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 398);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(0, 13);
            this.label3.TabIndex = 13;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(9, 35);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(528, 13);
            this.label5.TabIndex = 15;
            this.label5.Text = "This is a limited alpha not to be distributed without permission of John Marshall" +
                " Group";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(12, 153);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(398, 13);
            this.label6.TabIndex = 16;
            this.label6.Text = "Click start then right-click on an instance and select Main to switch into its in" +
                "terface";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.buttonPause);
            this.groupBox1.Controls.Add(this.buttonStart);
            this.groupBox1.Controls.Add(this.ButtonStep);
            this.groupBox1.Controls.Add(this.labelTime);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.TimeLabel);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 62);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(525, 70);
            this.groupBox1.TabIndex = 17;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Controls";
            // 
            // LoadProgress
            // 
            this.LoadProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.LoadProgress.Location = new System.Drawing.Point(100, 429);
            this.LoadProgress.Name = "LoadProgress";
            this.LoadProgress.Size = new System.Drawing.Size(363, 13);
            this.LoadProgress.TabIndex = 18;
            this.LoadProgress.Visible = false;
            // 
            // SimForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(549, 451);
            this.Controls.Add(this.LoadProgress);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.LinkUpdate);
            this.Controls.Add(this.LabelInstances);
            this.Controls.Add(this.listInstances);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "SimForm";
            this.Text = "De-Ops Internet Simulator";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ControlForm_FormClosing);
            this.Load += new System.EventHandler(this.ControlForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonPause;
        private System.Windows.Forms.Label labelTime;
        private System.Windows.Forms.ListView listInstances;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.Timer SecondTimer;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.Label LabelInstances;
        private System.Windows.Forms.LinkLabel LinkUpdate;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.ColumnHeader columnHeader9;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem FileMenu;
        private System.Windows.Forms.ToolStripMenuItem ViewMenu;
        private System.Windows.Forms.ToolStripMenuItem LoadMenuItem;
        private System.Windows.Forms.ToolStripMenuItem SaveMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
        private System.Windows.Forms.Button ButtonStep;
        private System.Windows.Forms.Label TimeLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ProgressBar LoadProgress;
    }
}