namespace RiseOp.Interface
{
    partial class StatusPanel
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
            this.StatusBrowser = new RiseOp.Interface.Views.WebBrowserEx();
            this.SecondTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // StatusBrowser
            // 
            this.StatusBrowser.AllowWebBrowserDrop = false;
            this.StatusBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StatusBrowser.IsWebBrowserContextMenuEnabled = false;
            this.StatusBrowser.Location = new System.Drawing.Point(0, 0);
            this.StatusBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.StatusBrowser.Name = "StatusBrowser";
            this.StatusBrowser.IsWebBrowserContextMenuEnabled = true;
            this.StatusBrowser.ScrollBarsEnabled = false;
            this.StatusBrowser.Size = new System.Drawing.Size(150, 150);
            this.StatusBrowser.TabIndex = 0;
            this.StatusBrowser.WebBrowserShortcutsEnabled = false;
            this.StatusBrowser.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.StatusBrowser_Navigating);
            // 
            // SecondTimer
            // 
            this.SecondTimer.Enabled = true;
            this.SecondTimer.Interval = 1000;
            this.SecondTimer.Tick += new System.EventHandler(this.SecondTimer_Tick);
            // 
            // StatusPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.StatusBrowser);
            this.Name = "StatusPanel";
            this.ResumeLayout(false);

        }

        #endregion

        private RiseOp.Interface.Views.WebBrowserEx StatusBrowser;
        private System.Windows.Forms.Timer SecondTimer;
    }
}
