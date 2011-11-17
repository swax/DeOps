namespace DeOps.Interface.Settings
{
    partial class Operation
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
            this.CloseButton = new System.Windows.Forms.Button();
            this.OKButton = new System.Windows.Forms.Button();
            this.OperationBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.IconPicture = new System.Windows.Forms.PictureBox();
            this.ChangeIconLink = new System.Windows.Forms.LinkLabel();
            this.label3 = new System.Windows.Forms.Label();
            this.SplashPicture = new System.Windows.Forms.PictureBox();
            this.DefaultIconLink = new System.Windows.Forms.LinkLabel();
            this.DefaultSplashLink = new System.Windows.Forms.LinkLabel();
            this.ChangeSplashLink = new System.Windows.Forms.LinkLabel();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.IconPicture)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SplashPicture)).BeginInit();
            this.SuspendLayout();
            // 
            // CloseButton
            // 
            this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CloseButton.Location = new System.Drawing.Point(125, 277);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(75, 23);
            this.CloseButton.TabIndex = 9;
            this.CloseButton.Text = "Cancel";
            this.CloseButton.UseVisualStyleBackColor = true;
            this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
            // 
            // OKButton
            // 
            this.OKButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OKButton.Location = new System.Drawing.Point(44, 277);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 8;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // OperationBox
            // 
            this.OperationBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.OperationBox.Location = new System.Drawing.Point(53, 18);
            this.OperationBox.Name = "OperationBox";
            this.OperationBox.Size = new System.Drawing.Size(147, 20);
            this.OperationBox.TabIndex = 11;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(12, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(66, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "Icon (16x16)";
            // 
            // IconPicture
            // 
            this.IconPicture.Location = new System.Drawing.Point(84, 53);
            this.IconPicture.Name = "IconPicture";
            this.IconPicture.Size = new System.Drawing.Size(16, 16);
            this.IconPicture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.IconPicture.TabIndex = 13;
            this.IconPicture.TabStop = false;
            // 
            // ChangeIconLink
            // 
            this.ChangeIconLink.AutoSize = true;
            this.ChangeIconLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.ChangeIconLink.Location = new System.Drawing.Point(107, 53);
            this.ChangeIconLink.Name = "ChangeIconLink";
            this.ChangeIconLink.Size = new System.Drawing.Size(44, 13);
            this.ChangeIconLink.TabIndex = 14;
            this.ChangeIconLink.TabStop = true;
            this.ChangeIconLink.Text = "Change";
            this.ChangeIconLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ChangeIconLink_LinkClicked);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(12, 90);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(89, 13);
            this.label3.TabIndex = 16;
            this.label3.Text = "Splash (240x180)";
            // 
            // SplashPicture
            // 
            this.SplashPicture.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SplashPicture.Location = new System.Drawing.Point(38, 117);
            this.SplashPicture.Name = "SplashPicture";
            this.SplashPicture.Size = new System.Drawing.Size(160, 120);
            this.SplashPicture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.SplashPicture.TabIndex = 15;
            this.SplashPicture.TabStop = false;
            // 
            // DefaultIconLink
            // 
            this.DefaultIconLink.AutoSize = true;
            this.DefaultIconLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.DefaultIconLink.Location = new System.Drawing.Point(157, 53);
            this.DefaultIconLink.Name = "DefaultIconLink";
            this.DefaultIconLink.Size = new System.Drawing.Size(41, 13);
            this.DefaultIconLink.TabIndex = 17;
            this.DefaultIconLink.TabStop = true;
            this.DefaultIconLink.Text = "Default";
            this.DefaultIconLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.DefaultIconLink_LinkClicked);
            // 
            // DefaultSplashLink
            // 
            this.DefaultSplashLink.AutoSize = true;
            this.DefaultSplashLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.DefaultSplashLink.Location = new System.Drawing.Point(157, 90);
            this.DefaultSplashLink.Name = "DefaultSplashLink";
            this.DefaultSplashLink.Size = new System.Drawing.Size(41, 13);
            this.DefaultSplashLink.TabIndex = 19;
            this.DefaultSplashLink.TabStop = true;
            this.DefaultSplashLink.Text = "Default";
            this.DefaultSplashLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.DefaultSplashLink_LinkClicked);
            // 
            // ChangeSplashLink
            // 
            this.ChangeSplashLink.AutoSize = true;
            this.ChangeSplashLink.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.ChangeSplashLink.Location = new System.Drawing.Point(107, 90);
            this.ChangeSplashLink.Name = "ChangeSplashLink";
            this.ChangeSplashLink.Size = new System.Drawing.Size(44, 13);
            this.ChangeSplashLink.TabIndex = 18;
            this.ChangeSplashLink.TabStop = true;
            this.ChangeSplashLink.Text = "Change";
            this.ChangeSplashLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ChangeSplashLink_LinkClicked);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(9, 250);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(197, 13);
            this.label4.TabIndex = 20;
            this.label4.Text = "** These settings are inherited. **";
            // 
            // Operation
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CloseButton;
            this.ClientSize = new System.Drawing.Size(212, 312);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.DefaultSplashLink);
            this.Controls.Add(this.ChangeSplashLink);
            this.Controls.Add(this.DefaultIconLink);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.SplashPicture);
            this.Controls.Add(this.ChangeIconLink);
            this.Controls.Add(this.IconPicture);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.OperationBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.CloseButton);
            this.Controls.Add(this.OKButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Organization";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Organization";
            this.Load += new System.EventHandler(this.Operation_Load);
            ((System.ComponentModel.ISupportInitialize)(this.IconPicture)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SplashPicture)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button CloseButton;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.TextBox OperationBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox IconPicture;
        private System.Windows.Forms.LinkLabel ChangeIconLink;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.PictureBox SplashPicture;
        private System.Windows.Forms.LinkLabel DefaultSplashLink;
        private System.Windows.Forms.LinkLabel ChangeSplashLink;
        private System.Windows.Forms.LinkLabel DefaultIconLink;
        private System.Windows.Forms.Label label4;
    }
}