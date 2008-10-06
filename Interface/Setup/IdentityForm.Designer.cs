namespace RiseOp.Interface.Setup
{
    partial class IdentityForm
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
            this.CopyLink = new System.Windows.Forms.LinkLabel();
            this.HeaderLabel = new System.Windows.Forms.Label();
            this.LinkBox = new System.Windows.Forms.TextBox();
            this.OKButton = new System.Windows.Forms.Button();
            this.HelpLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // CopyLink
            // 
            this.CopyLink.ActiveLinkColor = System.Drawing.Color.Blue;
            this.CopyLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.CopyLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.CopyLink.Location = new System.Drawing.Point(248, 19);
            this.CopyLink.Name = "CopyLink";
            this.CopyLink.Size = new System.Drawing.Size(90, 13);
            this.CopyLink.TabIndex = 17;
            this.CopyLink.TabStop = true;
            this.CopyLink.Text = "Copy to Clipboard";
            this.CopyLink.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.CopyLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.CopyLink_LinkClicked);
            // 
            // HeaderLabel
            // 
            this.HeaderLabel.AutoSize = true;
            this.HeaderLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.HeaderLabel.Location = new System.Drawing.Point(15, 19);
            this.HeaderLabel.Name = "HeaderLabel";
            this.HeaderLabel.Size = new System.Drawing.Size(145, 13);
            this.HeaderLabel.TabIndex = 19;
            this.HeaderLabel.Text = "<name>\'s Public Identity";
            // 
            // LinkBox
            // 
            this.LinkBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.LinkBox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.LinkBox.Location = new System.Drawing.Point(15, 38);
            this.LinkBox.Multiline = true;
            this.LinkBox.Name = "LinkBox";
            this.LinkBox.ReadOnly = true;
            this.LinkBox.Size = new System.Drawing.Size(323, 81);
            this.LinkBox.TabIndex = 18;
            // 
            // OKButton
            // 
            this.OKButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OKButton.Location = new System.Drawing.Point(263, 191);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 20;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // HelpLabel
            // 
            this.HelpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.HelpLabel.Location = new System.Drawing.Point(15, 122);
            this.HelpLabel.Name = "HelpLabel";
            this.HelpLabel.Size = new System.Drawing.Size(323, 66);
            this.HelpLabel.TabIndex = 21;
            this.HelpLabel.Text = "This can be used to send op invites to <name> or to add <name> to your buddy list" +
                " or let other people know of <name> by publishing this link on the internet.";
            this.HelpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // IdentityForm
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(350, 226);
            this.Controls.Add(this.HelpLabel);
            this.Controls.Add(this.OKButton);
            this.Controls.Add(this.CopyLink);
            this.Controls.Add(this.HeaderLabel);
            this.Controls.Add(this.LinkBox);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "IdentityForm";
            this.ShowInTaskbar = false;
            this.Text = "Identity";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.LinkLabel CopyLink;
        private System.Windows.Forms.Label HeaderLabel;
        private System.Windows.Forms.TextBox LinkBox;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.Label HelpLabel;
    }
}