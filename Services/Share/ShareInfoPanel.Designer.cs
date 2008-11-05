namespace RiseOp.Services.Share
{
    partial class ShareInfoPanel
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
            this.InfoBrowser = new RiseOp.Interface.Views.WebBrowserEx();
            this.SuspendLayout();
            // 
            // InfoBrowser
            // 
            this.InfoBrowser.AllowWebBrowserDrop = false;
            this.InfoBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InfoBrowser.IsWebBrowserContextMenuEnabled = false;
            this.InfoBrowser.Location = new System.Drawing.Point(0, 0);
            this.InfoBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.InfoBrowser.Name = "InfoBrowser";
            this.InfoBrowser.ScriptErrorsSuppressed = true;
            this.InfoBrowser.Size = new System.Drawing.Size(243, 106);
            this.InfoBrowser.TabIndex = 0;
            this.InfoBrowser.WebBrowserShortcutsEnabled = false;
            // 
            // ShareInfoPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.InfoBrowser);
            this.Name = "ShareInfoPanel";
            this.Size = new System.Drawing.Size(243, 106);
            this.ResumeLayout(false);

        }

        #endregion

        private RiseOp.Interface.Views.WebBrowserEx InfoBrowser;
    }
}
