namespace RiseOp.Services.Chat
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChatView));
            this.ViewContainer = new System.Windows.Forms.SplitContainer();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.RoomsButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.RoomSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.LocalButton = new System.Windows.Forms.ToolStripButton();
            this.LiveButton = new System.Windows.Forms.ToolStripButton();
            this.CustomButton = new System.Windows.Forms.ToolStripButton();
            this.InviteButton = new System.Windows.Forms.ToolStripButton();
            this.LeaveButton = new System.Windows.Forms.ToolStripButton();
            this.JoinButton = new System.Windows.Forms.ToolStripButton();
            this.FlashTimer = new System.Windows.Forms.Timer(this.components);
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
            this.ViewContainer.Size = new System.Drawing.Size(420, 280);
            this.ViewContainer.SplitterDistance = 133;
            this.ViewContainer.TabIndex = 0;
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RoomsButton,
            this.RoomSeparator,
            this.LocalButton,
            this.LiveButton,
            this.CustomButton,
            this.InviteButton,
            this.LeaveButton,
            this.JoinButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(420, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // RoomsButton
            // 
            this.RoomsButton.Image = ((System.Drawing.Image)(resources.GetObject("RoomsButton.Image")));
            this.RoomsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.RoomsButton.Name = "RoomsButton";
            this.RoomsButton.Size = new System.Drawing.Size(68, 22);
            this.RoomsButton.Text = "Rooms";
            this.RoomsButton.DropDownOpening += new System.EventHandler(this.RoomsButton_DropDownOpening);
            // 
            // RoomSeparator
            // 
            this.RoomSeparator.Name = "RoomSeparator";
            this.RoomSeparator.Size = new System.Drawing.Size(6, 25);
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
            // CustomButton
            // 
            this.CustomButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.CustomButton.Image = ((System.Drawing.Image)(resources.GetObject("CustomButton.Image")));
            this.CustomButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.CustomButton.Name = "CustomButton";
            this.CustomButton.Size = new System.Drawing.Size(47, 22);
            this.CustomButton.Text = "Custom";
            this.CustomButton.Click += new System.EventHandler(this.CustomButton_Click);
            // 
            // InviteButton
            // 
            this.InviteButton.Image = ((System.Drawing.Image)(resources.GetObject("InviteButton.Image")));
            this.InviteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.InviteButton.Name = "InviteButton";
            this.InviteButton.Size = new System.Drawing.Size(55, 22);
            this.InviteButton.Text = "Invite";
            this.InviteButton.Click += new System.EventHandler(this.InviteButton_Click);
            // 
            // LeaveButton
            // 
            this.LeaveButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.LeaveButton.Image = ((System.Drawing.Image)(resources.GetObject("LeaveButton.Image")));
            this.LeaveButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.LeaveButton.Name = "LeaveButton";
            this.LeaveButton.Size = new System.Drawing.Size(56, 22);
            this.LeaveButton.Text = "Leave";
            this.LeaveButton.Click += new System.EventHandler(this.LeaveButton_Click);
            // 
            // JoinButton
            // 
            this.JoinButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.JoinButton.Image = ((System.Drawing.Image)(resources.GetObject("JoinButton.Image")));
            this.JoinButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.JoinButton.Name = "JoinButton";
            this.JoinButton.Size = new System.Drawing.Size(46, 22);
            this.JoinButton.Text = "Join";
            this.JoinButton.Click += new System.EventHandler(this.JoinButton_Click);
            // 
            // FlashTimer
            // 
            this.FlashTimer.Enabled = true;
            this.FlashTimer.Interval = 500;
            this.FlashTimer.Tick += new System.EventHandler(this.FlashTimer_Tick);
            // 
            // ChatView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.ViewContainer);
            this.Name = "ChatView";
            this.Size = new System.Drawing.Size(420, 305);
            this.ViewContainer.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer ViewContainer;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton LocalButton;
        private System.Windows.Forms.ToolStripButton LiveButton;
        private System.Windows.Forms.ToolStripButton InviteButton;
        private System.Windows.Forms.ToolStripButton LeaveButton;
        private System.Windows.Forms.ToolStripButton JoinButton;
        private System.Windows.Forms.ToolStripSeparator RoomSeparator;
        private System.Windows.Forms.ToolStripDropDownButton RoomsButton;
        private System.Windows.Forms.Timer FlashTimer;
        private System.Windows.Forms.ToolStripButton CustomButton;

    }
}
