namespace DeOps.Interface
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginForm));
            this.ButtonLoad = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.CreateLink = new System.Windows.Forms.LinkLabel();
            this.JoinLink = new System.Windows.Forms.LinkLabel();
            this.EnterSimLink = new System.Windows.Forms.LinkLabel();
            this.OpCombo = new System.Windows.Forms.ComboBox();
            this.TextPassword = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // ButtonLoad
            // 
            this.ButtonLoad.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonLoad.Enabled = false;
            this.ButtonLoad.Location = new System.Drawing.Point(477, 72);
            this.ButtonLoad.Name = "ButtonLoad";
            this.ButtonLoad.Size = new System.Drawing.Size(74, 23);
            this.ButtonLoad.TabIndex = 2;
            this.ButtonLoad.Text = "Login";
            this.ButtonLoad.UseVisualStyleBackColor = true;
            this.ButtonLoad.Click += new System.EventHandler(this.ButtonLoad_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.linkLabel1);
            this.groupBox1.Controls.Add(this.CreateLink);
            this.groupBox1.Controls.Add(this.JoinLink);
            this.groupBox1.Controls.Add(this.EnterSimLink);
            this.groupBox1.Location = new System.Drawing.Point(287, 117);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(264, 135);
            this.groupBox1.TabIndex = 16;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Options";
            // 
            // linkLabel1
            // 
            this.linkLabel1.ActiveLinkColor = System.Drawing.Color.Red;
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.linkLabel1.LinkColor = System.Drawing.Color.Blue;
            this.linkLabel1.Location = new System.Drawing.Point(6, 26);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(103, 13);
            this.linkLabel1.TabIndex = 15;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "Create Global Profile";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.CreateGlobalLink_LinkClicked);
            // 
            // CreateLink
            // 
            this.CreateLink.ActiveLinkColor = System.Drawing.Color.Red;
            this.CreateLink.AutoSize = true;
            this.CreateLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.CreateLink.LinkColor = System.Drawing.Color.Blue;
            this.CreateLink.Location = new System.Drawing.Point(6, 53);
            this.CreateLink.Name = "CreateLink";
            this.CreateLink.Size = new System.Drawing.Size(134, 13);
            this.CreateLink.TabIndex = 8;
            this.CreateLink.TabStop = true;
            this.CreateLink.Text = "Create a New Organization";
            this.CreateLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.CreateLink_LinkClicked);
            // 
            // JoinLink
            // 
            this.JoinLink.ActiveLinkColor = System.Drawing.Color.Red;
            this.JoinLink.AutoSize = true;
            this.JoinLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.JoinLink.LinkColor = System.Drawing.Color.Blue;
            this.JoinLink.Location = new System.Drawing.Point(6, 80);
            this.JoinLink.Name = "JoinLink";
            this.JoinLink.Size = new System.Drawing.Size(211, 13);
            this.JoinLink.TabIndex = 9;
            this.JoinLink.TabStop = true;
            this.JoinLink.Text = "Join an Organization (requires an invite link)";
            this.JoinLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.JoinLink_LinkClicked);
            // 
            // EnterSimLink
            // 
            this.EnterSimLink.AutoSize = true;
            this.EnterSimLink.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.EnterSimLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.EnterSimLink.Location = new System.Drawing.Point(6, 108);
            this.EnterSimLink.Name = "EnterSimLink";
            this.EnterSimLink.Size = new System.Drawing.Size(89, 13);
            this.EnterSimLink.TabIndex = 4;
            this.EnterSimLink.TabStop = true;
            this.EnterSimLink.Text = "Launch Simulator";
            this.EnterSimLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.EnterSimLink_LinkClicked);
            // 
            // OpCombo
            // 
            this.OpCombo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.OpCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.OpCombo.FormattingEnabled = true;
            this.OpCombo.Location = new System.Drawing.Point(371, 19);
            this.OpCombo.Name = "OpCombo";
            this.OpCombo.Size = new System.Drawing.Size(180, 21);
            this.OpCombo.TabIndex = 0;
            this.OpCombo.SelectedIndexChanged += new System.EventHandler(this.OpCombo_SelectedIndexChanged);
            // 
            // TextPassword
            // 
            this.TextPassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.TextPassword.Enabled = false;
            this.TextPassword.Location = new System.Drawing.Point(371, 46);
            this.TextPassword.Name = "TextPassword";
            this.TextPassword.Size = new System.Drawing.Size(180, 20);
            this.TextPassword.TabIndex = 1;
            this.TextPassword.UseSystemPasswordChar = true;
            this.TextPassword.TextChanged += new System.EventHandler(this.TextPassword_TextChanged);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(287, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Passphrase";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(287, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(78, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Organization";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.Black;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(258, 240);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 16;
            this.pictureBox1.TabStop = false;
            // 
            // LoginForm
            // 
            this.AcceptButton = this.ButtonLoad;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(563, 264);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.OpCombo);
            this.Controls.Add(this.ButtonLoad);
            this.Controls.Add(this.TextPassword);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DeOps Alpha";
            this.Load += new System.EventHandler(this.LoginForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox TextPassword;
        private System.Windows.Forms.Button ButtonLoad;
        private System.Windows.Forms.LinkLabel CreateLink;
        private System.Windows.Forms.LinkLabel JoinLink;
        private System.Windows.Forms.LinkLabel EnterSimLink;
        private System.Windows.Forms.ComboBox OpCombo;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.PictureBox pictureBox1;

    }
}