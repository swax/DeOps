namespace RiseOp.Services.Chat
{
    partial class CreateRoom
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateRoom));
            this.label1 = new System.Windows.Forms.Label();
            this.TitleBox = new System.Windows.Forms.TextBox();
            this.PrivateRadio = new System.Windows.Forms.RadioButton();
            this.OkButton = new System.Windows.Forms.Button();
            this.ExitButton = new System.Windows.Forms.Button();
            this.SecretRadio = new System.Windows.Forms.RadioButton();
            this.PublicRadio = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(27, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Title";
            // 
            // TitleBox
            // 
            this.TitleBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.TitleBox.Location = new System.Drawing.Point(45, 15);
            this.TitleBox.Name = "TitleBox";
            this.TitleBox.Size = new System.Drawing.Size(195, 20);
            this.TitleBox.TabIndex = 1;
            // 
            // PrivateRadio
            // 
            this.PrivateRadio.AutoSize = true;
            this.PrivateRadio.Checked = true;
            this.PrivateRadio.Location = new System.Drawing.Point(15, 74);
            this.PrivateRadio.Name = "PrivateRadio";
            this.PrivateRadio.Size = new System.Drawing.Size(195, 17);
            this.PrivateRadio.TabIndex = 2;
            this.PrivateRadio.Text = "Private - Unlisted, anyone can invite";
            this.PrivateRadio.UseVisualStyleBackColor = true;
            // 
            // OkButton
            // 
            this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OkButton.Location = new System.Drawing.Point(84, 132);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(75, 23);
            this.OkButton.TabIndex = 3;
            this.OkButton.Text = "OK";
            this.OkButton.UseVisualStyleBackColor = true;
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // ExitButton
            // 
            this.ExitButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ExitButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ExitButton.Location = new System.Drawing.Point(165, 132);
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.Size = new System.Drawing.Size(75, 23);
            this.ExitButton.TabIndex = 4;
            this.ExitButton.Text = "Cancel";
            this.ExitButton.UseVisualStyleBackColor = true;
            this.ExitButton.Click += new System.EventHandler(this.ExitButton_Click);
            // 
            // SecretRadio
            // 
            this.SecretRadio.AutoSize = true;
            this.SecretRadio.Location = new System.Drawing.Point(15, 97);
            this.SecretRadio.Name = "SecretRadio";
            this.SecretRadio.Size = new System.Drawing.Size(216, 17);
            this.SecretRadio.TabIndex = 5;
            this.SecretRadio.Text = "Secret - Only you can invite more people";
            this.SecretRadio.UseVisualStyleBackColor = true;
            // 
            // PublicRadio
            // 
            this.PublicRadio.AutoSize = true;
            this.PublicRadio.Location = new System.Drawing.Point(15, 51);
            this.PublicRadio.Name = "PublicRadio";
            this.PublicRadio.Size = new System.Drawing.Size(212, 17);
            this.PublicRadio.TabIndex = 6;
            this.PublicRadio.Text = "Public - Anyone can join using room title";
            this.PublicRadio.UseVisualStyleBackColor = true;
            // 
            // CreateRoom
            // 
            this.AcceptButton = this.OkButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.ExitButton;
            this.ClientSize = new System.Drawing.Size(252, 167);
            this.Controls.Add(this.PublicRadio);
            this.Controls.Add(this.SecretRadio);
            this.Controls.Add(this.ExitButton);
            this.Controls.Add(this.OkButton);
            this.Controls.Add(this.PrivateRadio);
            this.Controls.Add(this.TitleBox);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CreateRoom";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Create Room";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button OkButton;
        private System.Windows.Forms.Button ExitButton;
        internal System.Windows.Forms.TextBox TitleBox;
        internal System.Windows.Forms.RadioButton PrivateRadio;
        internal System.Windows.Forms.RadioButton SecretRadio;
        internal System.Windows.Forms.RadioButton PublicRadio;
    }
}