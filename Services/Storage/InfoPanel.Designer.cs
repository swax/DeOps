namespace RiseOp.Services.Storage
{
    partial class InfoPanel
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
            this.InfoDisplay = new RiseOp.Interface.Views.WebBrowserEx();
            this.SuspendLayout();
            // 
            // InfoDisplay
            // 
            this.InfoDisplay.AllowWebBrowserDrop = false;
            this.InfoDisplay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InfoDisplay.IsWebBrowserContextMenuEnabled = false;
            this.InfoDisplay.Location = new System.Drawing.Point(0, 0);
            this.InfoDisplay.MinimumSize = new System.Drawing.Size(20, 20);
            this.InfoDisplay.Name = "InfoDisplay";
            this.InfoDisplay.Size = new System.Drawing.Size(150, 150);
            this.InfoDisplay.TabIndex = 0;
            this.InfoDisplay.ScriptErrorsSuppressed = true;
            this.InfoDisplay.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.InfoDisplay_Navigating);
            // 
            // InfoPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.InfoDisplay);
            this.Name = "InfoPanel";
            this.ResumeLayout(false);

        }

        #endregion

        internal RiseOp.Interface.Views.WebBrowserEx InfoDisplay;
    }
}
