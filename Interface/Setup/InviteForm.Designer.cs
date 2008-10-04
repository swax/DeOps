namespace RiseOp.Interface
{
    partial class InviteForm
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
            this.NextButton = new System.Windows.Forms.Button();
            this.CopyLink = new System.Windows.Forms.LinkLabel();
            this.HelpLabel = new System.Windows.Forms.Label();
            this.IdentityBox = new System.Windows.Forms.TextBox();
            this.DirectionLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // NextButton
            // 
            this.NextButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.NextButton.Location = new System.Drawing.Point(245, 204);
            this.NextButton.Name = "NextButton";
            this.NextButton.Size = new System.Drawing.Size(107, 23);
            this.NextButton.TabIndex = 3;
            this.NextButton.Text = "Create Invite >>";
            this.NextButton.UseVisualStyleBackColor = true;
            this.NextButton.Click += new System.EventHandler(this.CreateButton_Click);
            // 
            // CopyLink
            // 
            this.CopyLink.ActiveLinkColor = System.Drawing.Color.Blue;
            this.CopyLink.AutoSize = true;
            this.CopyLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.CopyLink.Location = new System.Drawing.Point(265, 80);
            this.CopyLink.Name = "CopyLink";
            this.CopyLink.Size = new System.Drawing.Size(90, 13);
            this.CopyLink.TabIndex = 4;
            this.CopyLink.TabStop = true;
            this.CopyLink.Text = "Copy to Clipboard";
            this.CopyLink.Visible = false;
            this.CopyLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.CopyLink_LinkClicked);
            // 
            // HelpLabel
            // 
            this.HelpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.HelpLabel.Location = new System.Drawing.Point(12, 9);
            this.HelpLabel.Name = "HelpLabel";
            this.HelpLabel.Size = new System.Drawing.Size(343, 49);
            this.HelpLabel.TabIndex = 17;
            this.HelpLabel.Text = "To invite someone to <op>, you need to have their user identity from any op they " +
                "are already apart of. For example their global IM identity. ";
            this.HelpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // IdentityBox
            // 
            this.IdentityBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.IdentityBox.Location = new System.Drawing.Point(15, 98);
            this.IdentityBox.Multiline = true;
            this.IdentityBox.Name = "IdentityBox";
            this.IdentityBox.Size = new System.Drawing.Size(340, 100);
            this.IdentityBox.TabIndex = 18;
            // 
            // DirectionLabel
            // 
            this.DirectionLabel.AutoSize = true;
            this.DirectionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DirectionLabel.Location = new System.Drawing.Point(12, 80);
            this.DirectionLabel.Name = "DirectionLabel";
            this.DirectionLabel.Size = new System.Drawing.Size(151, 13);
            this.DirectionLabel.TabIndex = 21;
            this.DirectionLabel.Text = "Place Identity Link Below";
            // 
            // InviteForm
            // 
            this.AcceptButton = this.NextButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(367, 239);
            this.Controls.Add(this.DirectionLabel);
            this.Controls.Add(this.IdentityBox);
            this.Controls.Add(this.HelpLabel);
            this.Controls.Add(this.CopyLink);
            this.Controls.Add(this.NextButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InviteForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Invite";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button NextButton;
        private System.Windows.Forms.LinkLabel CopyLink;
        private System.Windows.Forms.Label HelpLabel;
        private System.Windows.Forms.TextBox IdentityBox;
        private System.Windows.Forms.Label DirectionLabel;
    }
}