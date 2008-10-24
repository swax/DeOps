namespace RiseOp.Interface
{
    partial class LicenseForm
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
            this.LicenseBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // LicenseBox
            // 
            this.LicenseBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.LicenseBox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.LicenseBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.LicenseBox.Location = new System.Drawing.Point(12, 12);
            this.LicenseBox.Multiline = true;
            this.LicenseBox.Name = "LicenseBox";
            this.LicenseBox.ReadOnly = true;
            this.LicenseBox.Size = new System.Drawing.Size(209, 159);
            this.LicenseBox.TabIndex = 0;
            // 
            // LicenseForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(233, 183);
            this.Controls.Add(this.LicenseBox);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LicenseForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "License";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox LicenseBox;

    }
}