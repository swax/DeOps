namespace RiseOp.Simulator
{
    partial class NetView
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.LegendMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.TrackMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.LegendMenu,
            this.TrackMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.menuStrip1.Size = new System.Drawing.Size(426, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // LegendMenu
            // 
            this.LegendMenu.Name = "LegendMenu";
            this.LegendMenu.Size = new System.Drawing.Size(54, 20);
            this.LegendMenu.Text = "Legend";
            this.LegendMenu.Click += new System.EventHandler(this.LegendMenu_Click);
            // 
            // TrackMenuItem
            // 
            this.TrackMenuItem.Name = "TrackMenuItem";
            this.TrackMenuItem.Size = new System.Drawing.Size(45, 20);
            this.TrackMenuItem.Text = "Track";
            this.TrackMenuItem.Click += new System.EventHandler(this.TrackMenuItem_Click);
            // 
            // NetView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(426, 408);
            this.Controls.Add(this.menuStrip1);
            this.DoubleBuffered = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "NetView";
            this.Text = "NetView";
            this.Load += new System.EventHandler(this.NetView_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.NetView_Paint);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.NetView_MouseClick);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.NetView_FormClosing);
            this.Resize += new System.EventHandler(this.NetView_Resize);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.NetView_MouseMove);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem LegendMenu;
        private System.Windows.Forms.ToolStripMenuItem TrackMenuItem;
    }
}