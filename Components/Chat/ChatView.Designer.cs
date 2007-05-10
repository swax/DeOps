namespace DeOps.Components.Chat
{
    partial class ChatView
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
            this.ViewTabs = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.ViewTabs.SuspendLayout();
            this.SuspendLayout();
            // 
            // ViewTabs
            // 
            this.ViewTabs.Controls.Add(this.tabPage1);
            this.ViewTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ViewTabs.Location = new System.Drawing.Point(0, 0);
            this.ViewTabs.Name = "ViewTabs";
            this.ViewTabs.SelectedIndex = 0;
            this.ViewTabs.Size = new System.Drawing.Size(252, 191);
            this.ViewTabs.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(244, 165);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Log";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // ChatView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ViewTabs);
            this.Name = "ChatView";
            this.Size = new System.Drawing.Size(252, 191);
            this.ViewTabs.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl ViewTabs;
        private System.Windows.Forms.TabPage tabPage1;
    }
}
