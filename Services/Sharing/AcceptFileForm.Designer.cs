namespace RiseOp.Services.Sharing
{
    partial class AcceptFileForm
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
            this.GetButton = new System.Windows.Forms.Button();
            this.DenyButton = new System.Windows.Forms.Button();
            this.DescriptionLabel = new System.Windows.Forms.Label();
            this.NameLabel = new System.Windows.Forms.Label();
            this.SizeLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // GetButton
            // 
            this.GetButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.GetButton.Location = new System.Drawing.Point(109, 107);
            this.GetButton.Name = "GetButton";
            this.GetButton.Size = new System.Drawing.Size(75, 23);
            this.GetButton.TabIndex = 0;
            this.GetButton.Text = "Accept";
            this.GetButton.UseVisualStyleBackColor = true;
            this.GetButton.Click += new System.EventHandler(this.AcceptButton_Click);
            // 
            // DenyButton
            // 
            this.DenyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DenyButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.DenyButton.Location = new System.Drawing.Point(190, 107);
            this.DenyButton.Name = "DenyButton";
            this.DenyButton.Size = new System.Drawing.Size(75, 23);
            this.DenyButton.TabIndex = 1;
            this.DenyButton.Text = "Deny";
            this.DenyButton.UseVisualStyleBackColor = true;
            this.DenyButton.Click += new System.EventHandler(this.DenyButton_Click);
            // 
            // DescriptionLabel
            // 
            this.DescriptionLabel.AutoSize = true;
            this.DescriptionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DescriptionLabel.Location = new System.Drawing.Point(12, 20);
            this.DescriptionLabel.Name = "DescriptionLabel";
            this.DescriptionLabel.Size = new System.Drawing.Size(173, 13);
            this.DescriptionLabel.TabIndex = 2;
            this.DescriptionLabel.Text = "John wants to send you a file";
            // 
            // NameLabel
            // 
            this.NameLabel.AutoSize = true;
            this.NameLabel.Location = new System.Drawing.Point(82, 44);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(39, 13);
            this.NameLabel.TabIndex = 3;
            this.NameLabel.Text = "File.zip";
            // 
            // SizeLabel
            // 
            this.SizeLabel.AutoSize = true;
            this.SizeLabel.Location = new System.Drawing.Point(82, 66);
            this.SizeLabel.Name = "SizeLabel";
            this.SizeLabel.Size = new System.Drawing.Size(47, 13);
            this.SizeLabel.TabIndex = 4;
            this.SizeLabel.Text = "3.24 MB";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(33, 44);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(43, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Name:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(33, 66);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Size:";
            // 
            // AcceptFileForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.DenyButton;
            this.ClientSize = new System.Drawing.Size(277, 142);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.SizeLabel);
            this.Controls.Add(this.NameLabel);
            this.Controls.Add(this.DescriptionLabel);
            this.Controls.Add(this.DenyButton);
            this.Controls.Add(this.GetButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AcceptFileForm";
            this.ShowInTaskbar = false;
            this.Text = "Send File Request";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button GetButton;
        private System.Windows.Forms.Button DenyButton;
        private System.Windows.Forms.Label DescriptionLabel;
        private System.Windows.Forms.Label NameLabel;
        private System.Windows.Forms.Label SizeLabel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
    }
}