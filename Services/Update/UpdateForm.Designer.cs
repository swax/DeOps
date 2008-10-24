namespace RiseOp.Services.Update
{
    partial class UpdateForm
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
            this.LaterButton = new System.Windows.Forms.Button();
            this.UpdateButton = new System.Windows.Forms.Button();
            this.MessageLabel = new System.Windows.Forms.Label();
            this.NotesBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // LaterButton
            // 
            this.LaterButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.LaterButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.LaterButton.Location = new System.Drawing.Point(159, 144);
            this.LaterButton.Name = "LaterButton";
            this.LaterButton.Size = new System.Drawing.Size(75, 23);
            this.LaterButton.TabIndex = 0;
            this.LaterButton.Text = "Later";
            this.LaterButton.UseVisualStyleBackColor = true;
            this.LaterButton.Click += new System.EventHandler(this.LaterButton_Click);
            // 
            // UpdateButton
            // 
            this.UpdateButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.UpdateButton.Location = new System.Drawing.Point(78, 144);
            this.UpdateButton.Name = "UpdateButton";
            this.UpdateButton.Size = new System.Drawing.Size(75, 23);
            this.UpdateButton.TabIndex = 1;
            this.UpdateButton.Text = "Update";
            this.UpdateButton.UseVisualStyleBackColor = true;
            this.UpdateButton.Click += new System.EventHandler(this.UpdateButton_Click);
            // 
            // MessageLabel
            // 
            this.MessageLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.MessageLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MessageLabel.Location = new System.Drawing.Point(12, 21);
            this.MessageLabel.Name = "MessageLabel";
            this.MessageLabel.Size = new System.Drawing.Size(222, 37);
            this.MessageLabel.TabIndex = 2;
            this.MessageLabel.Text = "RiseOp needs to be restarted to finish updating to version 1.0.0";
            // 
            // NotesBox
            // 
            this.NotesBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.NotesBox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.NotesBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.NotesBox.Location = new System.Drawing.Point(15, 61);
            this.NotesBox.Multiline = true;
            this.NotesBox.Name = "NotesBox";
            this.NotesBox.ReadOnly = true;
            this.NotesBox.Size = new System.Drawing.Size(219, 77);
            this.NotesBox.TabIndex = 3;
            this.NotesBox.Text = "Notes";
            // 
            // UpdateForm
            // 
            this.AcceptButton = this.UpdateButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.LaterButton;
            this.ClientSize = new System.Drawing.Size(246, 179);
            this.Controls.Add(this.NotesBox);
            this.Controls.Add(this.MessageLabel);
            this.Controls.Add(this.UpdateButton);
            this.Controls.Add(this.LaterButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpdateForm";
            this.Text = "Update Ready";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button LaterButton;
        private System.Windows.Forms.Button UpdateButton;
        internal System.Windows.Forms.Label MessageLabel;
        internal System.Windows.Forms.TextBox NotesBox;
    }
}