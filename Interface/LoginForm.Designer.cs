namespace RiseOp.Interface
{
    partial class LoginForm
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.TextPassword = new System.Windows.Forms.TextBox();
            this.ButtonLoad = new System.Windows.Forms.Button();
            this.ButtonExit = new System.Windows.Forms.Button();
            this.CreateLink = new System.Windows.Forms.LinkLabel();
            this.JoinLink = new System.Windows.Forms.LinkLabel();
            this.EnterSimLink = new System.Windows.Forms.LinkLabel();
            this.OpCombo = new System.Windows.Forms.ComboBox();
            this.SplashBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.SplashBox)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 211);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(62, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Operation";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(12, 253);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Passphrase";
            // 
            // TextPassword
            // 
            this.TextPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.TextPassword.Enabled = false;
            this.TextPassword.Location = new System.Drawing.Point(15, 269);
            this.TextPassword.Name = "TextPassword";
            this.TextPassword.PasswordChar = '•';
            this.TextPassword.Size = new System.Drawing.Size(241, 20);
            this.TextPassword.TabIndex = 1;
            this.TextPassword.TextChanged += new System.EventHandler(this.TextPassword_TextChanged);
            // 
            // ButtonLoad
            // 
            this.ButtonLoad.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonLoad.Enabled = false;
            this.ButtonLoad.Location = new System.Drawing.Point(100, 295);
            this.ButtonLoad.Name = "ButtonLoad";
            this.ButtonLoad.Size = new System.Drawing.Size(75, 23);
            this.ButtonLoad.TabIndex = 2;
            this.ButtonLoad.Text = "Login";
            this.ButtonLoad.UseVisualStyleBackColor = true;
            this.ButtonLoad.Click += new System.EventHandler(this.ButtonLoad_Click);
            // 
            // ButtonExit
            // 
            this.ButtonExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonExit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ButtonExit.Location = new System.Drawing.Point(181, 295);
            this.ButtonExit.Name = "ButtonExit";
            this.ButtonExit.Size = new System.Drawing.Size(75, 23);
            this.ButtonExit.TabIndex = 3;
            this.ButtonExit.Text = "Exit";
            this.ButtonExit.UseVisualStyleBackColor = true;
            this.ButtonExit.Click += new System.EventHandler(this.ButtonExit_Click);
            // 
            // CreateLink
            // 
            this.CreateLink.ActiveLinkColor = System.Drawing.Color.Green;
            this.CreateLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CreateLink.AutoSize = true;
            this.CreateLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.CreateLink.LinkColor = System.Drawing.Color.Green;
            this.CreateLink.Location = new System.Drawing.Point(221, 211);
            this.CreateLink.Name = "CreateLink";
            this.CreateLink.Size = new System.Drawing.Size(38, 13);
            this.CreateLink.TabIndex = 8;
            this.CreateLink.TabStop = true;
            this.CreateLink.Text = "Create";
            this.CreateLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.CreateLink_LinkClicked);
            // 
            // JoinLink
            // 
            this.JoinLink.ActiveLinkColor = System.Drawing.Color.Green;
            this.JoinLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.JoinLink.AutoSize = true;
            this.JoinLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.JoinLink.LinkColor = System.Drawing.Color.Green;
            this.JoinLink.Location = new System.Drawing.Point(189, 211);
            this.JoinLink.Name = "JoinLink";
            this.JoinLink.Size = new System.Drawing.Size(26, 13);
            this.JoinLink.TabIndex = 9;
            this.JoinLink.TabStop = true;
            this.JoinLink.Text = "Join";
            this.JoinLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.JoinLink_LinkClicked);
            // 
            // EnterSimLink
            // 
            this.EnterSimLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.EnterSimLink.AutoSize = true;
            this.EnterSimLink.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.EnterSimLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.EnterSimLink.LinkColor = System.Drawing.Color.Red;
            this.EnterSimLink.Location = new System.Drawing.Point(21, 300);
            this.EnterSimLink.Name = "EnterSimLink";
            this.EnterSimLink.Size = new System.Drawing.Size(63, 13);
            this.EnterSimLink.TabIndex = 4;
            this.EnterSimLink.TabStop = true;
            this.EnterSimLink.Text = "Launch Sim";
            this.EnterSimLink.Visible = false;
            this.EnterSimLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.EnterSimLink_LinkClicked);
            // 
            // OpCombo
            // 
            this.OpCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.OpCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.OpCombo.FormattingEnabled = true;
            this.OpCombo.Location = new System.Drawing.Point(15, 227);
            this.OpCombo.Name = "OpCombo";
            this.OpCombo.Size = new System.Drawing.Size(241, 21);
            this.OpCombo.TabIndex = 0;
            this.OpCombo.SelectedIndexChanged += new System.EventHandler(this.OpCombo_SelectedIndexChanged);
            // 
            // SplashBox
            // 
            this.SplashBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SplashBox.Location = new System.Drawing.Point(15, 12);
            this.SplashBox.Name = "SplashBox";
            this.SplashBox.Size = new System.Drawing.Size(240, 180);
            this.SplashBox.TabIndex = 10;
            this.SplashBox.TabStop = false;
            this.SplashBox.DoubleClick += new System.EventHandler(this.pictureBox1_DoubleClick);
            // 
            // LoginForm
            // 
            this.AcceptButton = this.ButtonLoad;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.ButtonExit;
            this.ClientSize = new System.Drawing.Size(268, 330);
            this.Controls.Add(this.SplashBox);
            this.Controls.Add(this.OpCombo);
            this.Controls.Add(this.EnterSimLink);
            this.Controls.Add(this.JoinLink);
            this.Controls.Add(this.CreateLink);
            this.Controls.Add(this.ButtonExit);
            this.Controls.Add(this.ButtonLoad);
            this.Controls.Add(this.TextPassword);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "RiseOp";
            ((System.ComponentModel.ISupportInitialize)(this.SplashBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox TextPassword;
        private System.Windows.Forms.Button ButtonLoad;
        private System.Windows.Forms.Button ButtonExit;
        private System.Windows.Forms.LinkLabel CreateLink;
        private System.Windows.Forms.LinkLabel JoinLink;
        private System.Windows.Forms.LinkLabel EnterSimLink;
        private System.Windows.Forms.ComboBox OpCombo;
        internal System.Windows.Forms.PictureBox SplashBox;

    }
}