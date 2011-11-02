namespace DeOps.Services.Location
{
    partial class StatusForm
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
            this.AvailableRadio = new System.Windows.Forms.RadioButton();
            this.AwayRadio = new System.Windows.Forms.RadioButton();
            this.InvisibleRadio = new System.Windows.Forms.RadioButton();
            this.AwayBox = new System.Windows.Forms.TextBox();
            this.ExitButton = new System.Windows.Forms.Button();
            this.OkButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // AvailableRadio
            // 
            this.AvailableRadio.AutoSize = true;
            this.AvailableRadio.Location = new System.Drawing.Point(12, 12);
            this.AvailableRadio.Name = "AvailableRadio";
            this.AvailableRadio.Size = new System.Drawing.Size(68, 17);
            this.AvailableRadio.TabIndex = 0;
            this.AvailableRadio.TabStop = true;
            this.AvailableRadio.Text = "Available";
            this.AvailableRadio.UseVisualStyleBackColor = true;
            // 
            // AwayRadio
            // 
            this.AwayRadio.AutoSize = true;
            this.AwayRadio.Location = new System.Drawing.Point(12, 45);
            this.AwayRadio.Name = "AwayRadio";
            this.AwayRadio.Size = new System.Drawing.Size(51, 17);
            this.AwayRadio.TabIndex = 1;
            this.AwayRadio.TabStop = true;
            this.AwayRadio.Text = "Away";
            this.AwayRadio.UseVisualStyleBackColor = true;
            // 
            // InvisibleRadio
            // 
            this.InvisibleRadio.AutoSize = true;
            this.InvisibleRadio.Location = new System.Drawing.Point(12, 80);
            this.InvisibleRadio.Name = "InvisibleRadio";
            this.InvisibleRadio.Size = new System.Drawing.Size(63, 17);
            this.InvisibleRadio.TabIndex = 2;
            this.InvisibleRadio.TabStop = true;
            this.InvisibleRadio.Text = "Invisible";
            this.InvisibleRadio.UseVisualStyleBackColor = true;
            // 
            // AwayBox
            // 
            this.AwayBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.AwayBox.Location = new System.Drawing.Point(69, 44);
            this.AwayBox.Name = "AwayBox";
            this.AwayBox.Size = new System.Drawing.Size(142, 20);
            this.AwayBox.TabIndex = 3;
            // 
            // CancelButton
            // 
            this.ExitButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ExitButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ExitButton.Location = new System.Drawing.Point(139, 128);
            this.ExitButton.Name = "CancelButton";
            this.ExitButton.Size = new System.Drawing.Size(75, 23);
            this.ExitButton.TabIndex = 4;
            this.ExitButton.Text = "Cancel";
            this.ExitButton.UseVisualStyleBackColor = true;
            this.ExitButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // OkButton
            // 
            this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OkButton.Location = new System.Drawing.Point(58, 128);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(75, 23);
            this.OkButton.TabIndex = 5;
            this.OkButton.Text = "OK";
            this.OkButton.UseVisualStyleBackColor = true;
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // StatusForm
            // 
            this.AcceptButton = this.OkButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(226, 163);
            this.Controls.Add(this.OkButton);
            this.Controls.Add(this.ExitButton);
            this.Controls.Add(this.AwayBox);
            this.Controls.Add(this.InvisibleRadio);
            this.Controls.Add(this.AwayRadio);
            this.Controls.Add(this.AvailableRadio);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StatusForm";
            this.ShowInTaskbar = false;
            this.Text = "Edit Status";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton AvailableRadio;
        private System.Windows.Forms.RadioButton AwayRadio;
        private System.Windows.Forms.RadioButton InvisibleRadio;
        private System.Windows.Forms.TextBox AwayBox;
        private System.Windows.Forms.Button ExitButton;
        private System.Windows.Forms.Button OkButton;
    }
}