namespace DeOps.Interface
{
    partial class LoaderForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoaderForm));
            this.label1 = new System.Windows.Forms.Label();
            this.LinkIdentity = new System.Windows.Forms.LinkLabel();
            this.label2 = new System.Windows.Forms.Label();
            this.TextPassword = new System.Windows.Forms.TextBox();
            this.ButtonLoad = new System.Windows.Forms.Button();
            this.ButtonExit = new System.Windows.Forms.Button();
            this.TimerMain = new System.Windows.Forms.Timer(this.components);
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.LinkNew = new System.Windows.Forms.LinkLabel();
            this.LinkSearch = new System.Windows.Forms.LinkLabel();
            this.EnterSimLink = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 200);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Operation File";
            // 
            // LinkIdentity
            // 
            this.LinkIdentity.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.LinkIdentity.AutoEllipsis = true;
            this.LinkIdentity.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.LinkIdentity.Location = new System.Drawing.Point(15, 216);
            this.LinkIdentity.Name = "LinkIdentity";
            this.LinkIdentity.Size = new System.Drawing.Size(253, 18);
            this.LinkIdentity.TabIndex = 2;
            this.LinkIdentity.TabStop = true;
            this.LinkIdentity.Text = "Click to Browse";
            this.LinkIdentity.UseCompatibleTextRendering = true;
            this.LinkIdentity.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkIdentity_LinkClicked);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(12, 242);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(61, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Password";
            // 
            // TextPassword
            // 
            this.TextPassword.Enabled = false;
            this.TextPassword.Location = new System.Drawing.Point(15, 258);
            this.TextPassword.Name = "TextPassword";
            this.TextPassword.PasswordChar = '•';
            this.TextPassword.Size = new System.Drawing.Size(138, 20);
            this.TextPassword.TabIndex = 4;
            // 
            // ButtonLoad
            // 
            this.ButtonLoad.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonLoad.Enabled = false;
            this.ButtonLoad.Location = new System.Drawing.Point(109, 288);
            this.ButtonLoad.Name = "ButtonLoad";
            this.ButtonLoad.Size = new System.Drawing.Size(75, 23);
            this.ButtonLoad.TabIndex = 5;
            this.ButtonLoad.Text = "Load";
            this.ButtonLoad.UseVisualStyleBackColor = true;
            this.ButtonLoad.Click += new System.EventHandler(this.ButtonLoad_Click);
            // 
            // ButtonExit
            // 
            this.ButtonExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonExit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ButtonExit.Location = new System.Drawing.Point(190, 288);
            this.ButtonExit.Name = "ButtonExit";
            this.ButtonExit.Size = new System.Drawing.Size(75, 23);
            this.ButtonExit.TabIndex = 7;
            this.ButtonExit.Text = "Exit";
            this.ButtonExit.UseVisualStyleBackColor = true;
            this.ButtonExit.Click += new System.EventHandler(this.ButtonExit_Click);
            // 
            // TimerMain
            // 
            this.TimerMain.Enabled = true;
            this.TimerMain.Interval = 1000;
            this.TimerMain.Tick += new System.EventHandler(this.TimerMain_Tick);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(253, 174);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // LinkNew
            // 
            this.LinkNew.AutoSize = true;
            this.LinkNew.LinkBehavior = System.Windows.Forms.LinkBehavior.AlwaysUnderline;
            this.LinkNew.LinkColor = System.Drawing.Color.Green;
            this.LinkNew.Location = new System.Drawing.Point(187, 200);
            this.LinkNew.Name = "LinkNew";
            this.LinkNew.Size = new System.Drawing.Size(29, 13);
            this.LinkNew.TabIndex = 8;
            this.LinkNew.TabStop = true;
            this.LinkNew.Text = "New";
            this.LinkNew.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkNew_LinkClicked);
            // 
            // LinkSearch
            // 
            this.LinkSearch.AutoSize = true;
            this.LinkSearch.LinkBehavior = System.Windows.Forms.LinkBehavior.AlwaysUnderline;
            this.LinkSearch.LinkColor = System.Drawing.Color.Green;
            this.LinkSearch.Location = new System.Drawing.Point(222, 200);
            this.LinkSearch.Name = "LinkSearch";
            this.LinkSearch.Size = new System.Drawing.Size(41, 13);
            this.LinkSearch.TabIndex = 9;
            this.LinkSearch.TabStop = true;
            this.LinkSearch.Text = "Search";
            this.LinkSearch.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkSearch_LinkClicked);
            // 
            // EnterSimLink
            // 
            this.EnterSimLink.AutoSize = true;
            this.EnterSimLink.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.EnterSimLink.LinkBehavior = System.Windows.Forms.LinkBehavior.AlwaysUnderline;
            this.EnterSimLink.LinkColor = System.Drawing.Color.Red;
            this.EnterSimLink.Location = new System.Drawing.Point(12, 291);
            this.EnterSimLink.Name = "EnterSimLink";
            this.EnterSimLink.Size = new System.Drawing.Size(87, 16);
            this.EnterSimLink.TabIndex = 10;
            this.EnterSimLink.TabStop = true;
            this.EnterSimLink.Text = "Launch Sim";
            this.EnterSimLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.EnterSimLink_LinkClicked);
            // 
            // LoaderForm
            // 
            this.AcceptButton = this.ButtonLoad;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.CancelButton = this.ButtonExit;
            this.ClientSize = new System.Drawing.Size(277, 323);
            this.Controls.Add(this.EnterSimLink);
            this.Controls.Add(this.LinkSearch);
            this.Controls.Add(this.LinkNew);
            this.Controls.Add(this.ButtonExit);
            this.Controls.Add(this.ButtonLoad);
            this.Controls.Add(this.TextPassword);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.LinkIdentity);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LoaderForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "De-Ops";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel LinkIdentity;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox TextPassword;
        private System.Windows.Forms.Button ButtonLoad;
        private System.Windows.Forms.Button ButtonExit;
        private System.Windows.Forms.Timer TimerMain;
        private System.Windows.Forms.LinkLabel LinkNew;
        private System.Windows.Forms.LinkLabel LinkSearch;
        private System.Windows.Forms.LinkLabel EnterSimLink;

    }
}