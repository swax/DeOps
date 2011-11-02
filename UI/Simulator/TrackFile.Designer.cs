namespace DeOps.Simulator
{
    partial class TrackFile
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
            this.HashBox = new System.Windows.Forms.TextBox();
            this.TrackButton = new System.Windows.Forms.Button();
            this.ExitButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Hash:";
            // 
            // HashBox
            // 
            this.HashBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.HashBox.Location = new System.Drawing.Point(53, 12);
            this.HashBox.Name = "HashBox";
            this.HashBox.Size = new System.Drawing.Size(235, 20);
            this.HashBox.TabIndex = 1;
            this.HashBox.TextChanged += new System.EventHandler(this.HashBox_TextChanged);
            // 
            // TrackButton
            // 
            this.TrackButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.TrackButton.Location = new System.Drawing.Point(132, 38);
            this.TrackButton.Name = "TrackButton";
            this.TrackButton.Size = new System.Drawing.Size(75, 23);
            this.TrackButton.TabIndex = 2;
            this.TrackButton.Text = "Track";
            this.TrackButton.UseVisualStyleBackColor = true;
            this.TrackButton.Click += new System.EventHandler(this.TrackButton_Click);
            // 
            // ExitButton
            // 
            this.ExitButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ExitButton.Location = new System.Drawing.Point(213, 38);
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.Size = new System.Drawing.Size(75, 23);
            this.ExitButton.TabIndex = 3;
            this.ExitButton.Text = "Clear";
            this.ExitButton.UseVisualStyleBackColor = true;
            this.ExitButton.Click += new System.EventHandler(this.ClearButton_Click);
            // 
            // TrackFile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;;
            this.ClientSize = new System.Drawing.Size(300, 69);
            this.Controls.Add(this.ExitButton);
            this.Controls.Add(this.TrackButton);
            this.Controls.Add(this.HashBox);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TrackFile";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Track File";
            this.Load += new System.EventHandler(this.TrackFile_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox HashBox;
        private System.Windows.Forms.Button TrackButton;
        private System.Windows.Forms.Button ExitButton;
    }
}