namespace RiseOp.Simulator
{
    partial class GenerateUsers
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
            this.ExitButton = new System.Windows.Forms.Button();
            this.GenerateButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.NamesFolderLink = new System.Windows.Forms.LinkLabel();
            this.OutputFolderLink = new System.Windows.Forms.LinkLabel();
            this.label2 = new System.Windows.Forms.Label();
            this.OrgNumeric = new System.Windows.Forms.NumericUpDown();
            this.UsersNumeric = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.GenProgress = new System.Windows.Forms.ProgressBar();
            ((System.ComponentModel.ISupportInitialize)(this.OrgNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.UsersNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // ExitButton
            // 
            this.ExitButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ExitButton.Location = new System.Drawing.Point(123, 204);
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.Size = new System.Drawing.Size(75, 23);
            this.ExitButton.TabIndex = 0;
            this.ExitButton.Text = "Cancel";
            this.ExitButton.UseVisualStyleBackColor = true;
            this.ExitButton.Click += new System.EventHandler(this.ExitButton_Click);
            // 
            // GenerateButton
            // 
            this.GenerateButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.GenerateButton.Location = new System.Drawing.Point(42, 204);
            this.GenerateButton.Name = "GenerateButton";
            this.GenerateButton.Size = new System.Drawing.Size(75, 23);
            this.GenerateButton.TabIndex = 1;
            this.GenerateButton.Text = "Generate";
            this.GenerateButton.UseVisualStyleBackColor = true;
            this.GenerateButton.Click += new System.EventHandler(this.GenerateButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(64, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Names data";
            // 
            // NamesFolderLink
            // 
            this.NamesFolderLink.AutoSize = true;
            this.NamesFolderLink.Location = new System.Drawing.Point(12, 32);
            this.NamesFolderLink.Name = "NamesFolderLink";
            this.NamesFolderLink.Size = new System.Drawing.Size(55, 13);
            this.NamesFolderLink.TabIndex = 3;
            this.NamesFolderLink.TabStop = true;
            this.NamesFolderLink.Text = "linkLabel1";
            this.NamesFolderLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.NamesFolderLink_LinkClicked);
            // 
            // OutputFolderLink
            // 
            this.OutputFolderLink.AutoSize = true;
            this.OutputFolderLink.Location = new System.Drawing.Point(12, 77);
            this.OutputFolderLink.Name = "OutputFolderLink";
            this.OutputFolderLink.Size = new System.Drawing.Size(55, 13);
            this.OutputFolderLink.TabIndex = 5;
            this.OutputFolderLink.TabStop = true;
            this.OutputFolderLink.Text = "linkLabel2";
            this.OutputFolderLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OutputFolderLink_LinkClicked);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(51, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Output to";
            // 
            // OrgNumeric
            // 
            this.OrgNumeric.Location = new System.Drawing.Point(15, 141);
            this.OrgNumeric.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.OrgNumeric.Name = "OrgNumeric";
            this.OrgNumeric.Size = new System.Drawing.Size(52, 20);
            this.OrgNumeric.TabIndex = 6;
            this.OrgNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // UsersNumeric
            // 
            this.UsersNumeric.Location = new System.Drawing.Point(15, 109);
            this.UsersNumeric.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.UsersNumeric.Name = "UsersNumeric";
            this.UsersNumeric.Size = new System.Drawing.Size(52, 20);
            this.UsersNumeric.TabIndex = 7;
            this.UsersNumeric.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(73, 143);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(71, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Organizations";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(73, 111);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Users over";
            // 
            // GenProgress
            // 
            this.GenProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.GenProgress.Location = new System.Drawing.Point(12, 175);
            this.GenProgress.Name = "GenProgress";
            this.GenProgress.Size = new System.Drawing.Size(186, 18);
            this.GenProgress.Step = 1;
            this.GenProgress.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.GenProgress.TabIndex = 10;
            this.GenProgress.Visible = false;
            // 
            // GenerateUsers
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(210, 239);
            this.Controls.Add(this.GenProgress);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.UsersNumeric);
            this.Controls.Add(this.OrgNumeric);
            this.Controls.Add(this.OutputFolderLink);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.NamesFolderLink);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.GenerateButton);
            this.Controls.Add(this.ExitButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GenerateUsers";
            this.Text = "Generate Users";
            ((System.ComponentModel.ISupportInitialize)(this.OrgNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.UsersNumeric)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ExitButton;
        private System.Windows.Forms.Button GenerateButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel NamesFolderLink;
        private System.Windows.Forms.LinkLabel OutputFolderLink;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown OrgNumeric;
        private System.Windows.Forms.NumericUpDown UsersNumeric;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ProgressBar GenProgress;
    }
}