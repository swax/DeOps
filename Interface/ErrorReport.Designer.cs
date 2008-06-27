namespace RiseOp.Interface
{
    partial class ErrorReport
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
            this.SendButton = new System.Windows.Forms.Button();
            this.Label3 = new System.Windows.Forms.Label();
            this.NotesBox = new System.Windows.Forms.TextBox();
            this.Label2 = new System.Windows.Forms.Label();
            this.DetailsBox = new System.Windows.Forms.TextBox();
            this.Label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // ExitButton
            // 
            this.ExitButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ExitButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ExitButton.Location = new System.Drawing.Point(234, 303);
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.Size = new System.Drawing.Size(75, 23);
            this.ExitButton.TabIndex = 9;
            this.ExitButton.Text = "Close";
            this.ExitButton.UseVisualStyleBackColor = true;
            this.ExitButton.Click += new System.EventHandler(this.ExitButton_Click);
            // 
            // SendButton
            // 
            this.SendButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SendButton.Location = new System.Drawing.Point(140, 303);
            this.SendButton.Name = "SendButton";
            this.SendButton.Size = new System.Drawing.Size(88, 23);
            this.SendButton.TabIndex = 8;
            this.SendButton.Text = "Send Report";
            this.SendButton.UseVisualStyleBackColor = true;
            this.SendButton.Click += new System.EventHandler(this.SendButton_Click);
            // 
            // Label3
            // 
            this.Label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.Label3.AutoSize = true;
            this.Label3.Location = new System.Drawing.Point(12, 225);
            this.Label3.Name = "Label3";
            this.Label3.Size = new System.Drawing.Size(84, 13);
            this.Label3.TabIndex = 10;
            this.Label3.Text = "Additional Notes";
            // 
            // NotesBox
            // 
            this.NotesBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.NotesBox.Location = new System.Drawing.Point(12, 241);
            this.NotesBox.Multiline = true;
            this.NotesBox.Name = "NotesBox";
            this.NotesBox.Size = new System.Drawing.Size(297, 56);
            this.NotesBox.TabIndex = 7;
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Location = new System.Drawing.Point(9, 60);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(39, 13);
            this.Label2.TabIndex = 12;
            this.Label2.Text = "Details";
            // 
            // DetailsBox
            // 
            this.DetailsBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.DetailsBox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.DetailsBox.Location = new System.Drawing.Point(12, 78);
            this.DetailsBox.Multiline = true;
            this.DetailsBox.Name = "DetailsBox";
            this.DetailsBox.ReadOnly = true;
            this.DetailsBox.Size = new System.Drawing.Size(297, 144);
            this.DetailsBox.TabIndex = 11;
            // 
            // Label1
            // 
            this.Label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.Label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label1.Location = new System.Drawing.Point(12, 9);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(297, 42);
            this.Label1.TabIndex = 13;
            this.Label1.Text = "RiseOp has closed due to an unexpected error.  Assist us in resolving this issue " +
                "by sending in an error report.";
            // 
            // ErrorReport
            // 
            this.AcceptButton = this.SendButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.ExitButton;
            this.ClientSize = new System.Drawing.Size(321, 338);
            this.Controls.Add(this.ExitButton);
            this.Controls.Add(this.SendButton);
            this.Controls.Add(this.Label3);
            this.Controls.Add(this.NotesBox);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.DetailsBox);
            this.Controls.Add(this.Label1);
            this.Name = "ErrorReport";
            this.Text = "Unexpected Error";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal System.Windows.Forms.Button ExitButton;
        internal System.Windows.Forms.Button SendButton;
        internal System.Windows.Forms.Label Label3;
        internal System.Windows.Forms.TextBox NotesBox;
        internal System.Windows.Forms.Label Label2;
        internal System.Windows.Forms.TextBox DetailsBox;
        internal System.Windows.Forms.Label Label1;
    }
}