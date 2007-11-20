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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChatView));
            this.ViewContainer = new System.Windows.Forms.SplitContainer();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.CreateButton = new System.Windows.Forms.ToolStripButton();
            this.ToolSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.LocalButton = new System.Windows.Forms.ToolStripButton();
            this.LiveButton = new System.Windows.Forms.ToolStripButton();
            this.JoinButton = new System.Windows.Forms.ToolStripButton();
            this.UntrustedButton = new System.Windows.Forms.ToolStripButton();
            this.ViewContainer.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ViewContainer
            // 
            this.ViewContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ViewContainer.Location = new System.Drawing.Point(0, 25);
            this.ViewContainer.Name = "ViewContainer";
            this.ViewContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.ViewContainer.Size = new System.Drawing.Size(397, 298);
            this.ViewContainer.SplitterDistance = 142;
            this.ViewContainer.TabIndex = 0;
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CreateButton,
            this.ToolSeparator,
            this.LocalButton,
            this.LiveButton,
            this.JoinButton,
            this.UntrustedButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(397, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // CreateButton
            // 
            this.CreateButton.Image = ((System.Drawing.Image)(resources.GetObject("CreateButton.Image")));
            this.CreateButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.CreateButton.Name = "CreateButton";
            this.CreateButton.Size = new System.Drawing.Size(90, 22);
            this.CreateButton.Text = "Create Room";
            // 
            // ToolSeparator
            // 
            this.ToolSeparator.Name = "ToolSeparator";
            this.ToolSeparator.Size = new System.Drawing.Size(6, 25);
            // 
            // LocalButton
            // 
            this.LocalButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.LocalButton.Image = ((System.Drawing.Image)(resources.GetObject("LocalButton.Image")));
            this.LocalButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.LocalButton.Name = "LocalButton";
            this.LocalButton.Size = new System.Drawing.Size(35, 22);
            this.LocalButton.Text = "Local";
            this.LocalButton.Click += new System.EventHandler(this.LocalButton_Click);
            // 
            // LiveButton
            // 
            this.LiveButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.LiveButton.Image = ((System.Drawing.Image)(resources.GetObject("LiveButton.Image")));
            this.LiveButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.LiveButton.Name = "LiveButton";
            this.LiveButton.Size = new System.Drawing.Size(30, 22);
            this.LiveButton.Text = "Live";
            this.LiveButton.Click += new System.EventHandler(this.LiveButton_Click);
            // 
            // JoinButton
            // 
            this.JoinButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.JoinButton.Image = ((System.Drawing.Image)(resources.GetObject("JoinButton.Image")));
            this.JoinButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.JoinButton.Name = "JoinButton";
            this.JoinButton.Size = new System.Drawing.Size(46, 22);
            this.JoinButton.Text = "Join";
            // 
            // UntrustedButton
            // 
            this.UntrustedButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.UntrustedButton.Image = ((System.Drawing.Image)(resources.GetObject("UntrustedButton.Image")));
            this.UntrustedButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.UntrustedButton.Name = "UntrustedButton";
            this.UntrustedButton.Size = new System.Drawing.Size(59, 22);
            this.UntrustedButton.Text = "Untrusted";
            this.UntrustedButton.Click += new System.EventHandler(this.UntrustedButton_Click);
            // 
            // ChatView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.ViewContainer);
            this.Name = "ChatView";
            this.Size = new System.Drawing.Size(397, 323);
            this.ViewContainer.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer ViewContainer;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton CreateButton;
        private System.Windows.Forms.ToolStripButton LocalButton;
        private System.Windows.Forms.ToolStripButton LiveButton;
        private System.Windows.Forms.ToolStripButton JoinButton;
        private System.Windows.Forms.ToolStripSeparator ToolSeparator;
        private System.Windows.Forms.ToolStripButton UntrustedButton;

    }
}
