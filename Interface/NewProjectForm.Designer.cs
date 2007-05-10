namespace DeOps.Interface
{
    partial class NewProjectForm
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
            this.ButtonOK = new System.Windows.Forms.Button();
            this.ButtonCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.NameBox = new System.Windows.Forms.TextBox();
            this.RadioSecret = new System.Windows.Forms.RadioButton();
            this.RadioPrivate = new System.Windows.Forms.RadioButton();
            this.RadioPublic = new System.Windows.Forms.RadioButton();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // ButtonOK
            // 
            this.ButtonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonOK.Location = new System.Drawing.Point(84, 113);
            this.ButtonOK.Name = "ButtonOK";
            this.ButtonOK.Size = new System.Drawing.Size(75, 23);
            this.ButtonOK.TabIndex = 0;
            this.ButtonOK.Text = "Ok";
            this.ButtonOK.UseVisualStyleBackColor = true;
            this.ButtonOK.Click += new System.EventHandler(this.ButtonOK_Click);
            // 
            // ButtonCancel
            // 
            this.ButtonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ButtonCancel.Location = new System.Drawing.Point(165, 113);
            this.ButtonCancel.Name = "ButtonCancel";
            this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
            this.ButtonCancel.TabIndex = 1;
            this.ButtonCancel.Text = "Cancel";
            this.ButtonCancel.UseVisualStyleBackColor = true;
            this.ButtonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Project Name";
            // 
            // NameBox
            // 
            this.NameBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.NameBox.Location = new System.Drawing.Point(101, 18);
            this.NameBox.Name = "NameBox";
            this.NameBox.Size = new System.Drawing.Size(139, 20);
            this.NameBox.TabIndex = 3;
            // 
            // RadioSecret
            // 
            this.RadioSecret.AutoSize = true;
            this.RadioSecret.Enabled = false;
            this.RadioSecret.Location = new System.Drawing.Point(158, 68);
            this.RadioSecret.Name = "RadioSecret";
            this.RadioSecret.Size = new System.Drawing.Size(56, 17);
            this.RadioSecret.TabIndex = 9;
            this.RadioSecret.TabStop = true;
            this.RadioSecret.Text = "Secret";
            this.RadioSecret.UseVisualStyleBackColor = true;
            // 
            // RadioPrivate
            // 
            this.RadioPrivate.AutoSize = true;
            this.RadioPrivate.Enabled = false;
            this.RadioPrivate.Location = new System.Drawing.Point(94, 68);
            this.RadioPrivate.Name = "RadioPrivate";
            this.RadioPrivate.Size = new System.Drawing.Size(58, 17);
            this.RadioPrivate.TabIndex = 8;
            this.RadioPrivate.TabStop = true;
            this.RadioPrivate.Text = "Private";
            this.RadioPrivate.UseVisualStyleBackColor = true;
            // 
            // RadioPublic
            // 
            this.RadioPublic.AutoSize = true;
            this.RadioPublic.Enabled = false;
            this.RadioPublic.Location = new System.Drawing.Point(34, 68);
            this.RadioPublic.Name = "RadioPublic";
            this.RadioPublic.Size = new System.Drawing.Size(54, 17);
            this.RadioPublic.TabIndex = 7;
            this.RadioPublic.TabStop = true;
            this.RadioPublic.Text = "Public";
            this.RadioPublic.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Enabled = false;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(12, 52);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(48, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Access";
            // 
            // NewProjectForm
            // 
            this.AcceptButton = this.ButtonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.ButtonCancel;
            this.ClientSize = new System.Drawing.Size(252, 148);
            this.Controls.Add(this.RadioSecret);
            this.Controls.Add(this.RadioPrivate);
            this.Controls.Add(this.RadioPublic);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.NameBox);
            this.Controls.Add(this.ButtonCancel);
            this.Controls.Add(this.ButtonOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NewProjectForm";
            this.Text = "New Project";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ButtonOK;
        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox NameBox;
        private System.Windows.Forms.RadioButton RadioSecret;
        private System.Windows.Forms.RadioButton RadioPrivate;
        private System.Windows.Forms.RadioButton RadioPublic;
        private System.Windows.Forms.Label label3;
    }
}