namespace RiseOp.Interface.Startup
{
    partial class JoinOp
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
            this.LinkBox = new System.Windows.Forms.TextBox();
            this.ButtonOK = new System.Windows.Forms.Button();
            this.ButtonCancel = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.JoinGlobalLink = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // LinkBox
            // 
            this.LinkBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.LinkBox.Location = new System.Drawing.Point(15, 72);
            this.LinkBox.Multiline = true;
            this.LinkBox.Name = "LinkBox";
            this.LinkBox.Size = new System.Drawing.Size(322, 88);
            this.LinkBox.TabIndex = 0;
            // 
            // ButtonOK
            // 
            this.ButtonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonOK.Location = new System.Drawing.Point(181, 172);
            this.ButtonOK.Name = "ButtonOK";
            this.ButtonOK.Size = new System.Drawing.Size(75, 23);
            this.ButtonOK.TabIndex = 1;
            this.ButtonOK.Text = "OK";
            this.ButtonOK.UseVisualStyleBackColor = true;
            this.ButtonOK.Click += new System.EventHandler(this.ButtonOK_Click);
            // 
            // ButtonCancel
            // 
            this.ButtonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ButtonCancel.Location = new System.Drawing.Point(262, 172);
            this.ButtonCancel.Name = "ButtonCancel";
            this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
            this.ButtonCancel.TabIndex = 2;
            this.ButtonCancel.Text = "Cancel";
            this.ButtonCancel.UseVisualStyleBackColor = true;
            this.ButtonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(12, 46);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(168, 13);
            this.label2.TabIndex = 20;
            this.label2.Text = "or enter a RiseOp link below";
            // 
            // JoinGlobalLink
            // 
            this.JoinGlobalLink.AutoSize = true;
            this.JoinGlobalLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.JoinGlobalLink.Location = new System.Drawing.Point(12, 18);
            this.JoinGlobalLink.Name = "JoinGlobalLink";
            this.JoinGlobalLink.Size = new System.Drawing.Size(226, 13);
            this.JoinGlobalLink.TabIndex = 21;
            this.JoinGlobalLink.TabStop = true;
            this.JoinGlobalLink.Text = "Click here to join the Global Instant Messenger";
            this.JoinGlobalLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.JoinGlobalLink_LinkClicked);
            // 
            // JoinOp
            // 
            this.AcceptButton = this.ButtonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.ButtonCancel;
            this.ClientSize = new System.Drawing.Size(349, 207);
            this.Controls.Add(this.JoinGlobalLink);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.ButtonOK);
            this.Controls.Add(this.ButtonCancel);
            this.Controls.Add(this.LinkBox);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "JoinOp";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Join Operation";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ButtonOK;
        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.Label label2;
        internal System.Windows.Forms.TextBox LinkBox;
        private System.Windows.Forms.LinkLabel JoinGlobalLink;
    }
}