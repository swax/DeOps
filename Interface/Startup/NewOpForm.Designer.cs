namespace DeOps.Interface
{
    partial class NewOpForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewOpForm));
            this.label1 = new System.Windows.Forms.Label();
            this.TextOperation = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.TextConfirm = new System.Windows.Forms.TextBox();
            this.TextPassword = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.TextName = new System.Windows.Forms.TextBox();
            this.ButtonCancel = new System.Windows.Forms.Button();
            this.ButtonOK = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.RadioPublic = new System.Windows.Forms.RadioButton();
            this.RadioPrivate = new System.Windows.Forms.RadioButton();
            this.RadioSecret = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.LinkLocation = new System.Windows.Forms.LinkLabel();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(98, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Operation Name";
            // 
            // TextOperation
            // 
            this.TextOperation.Location = new System.Drawing.Point(116, 17);
            this.TextOperation.Name = "TextOperation";
            this.TextOperation.Size = new System.Drawing.Size(164, 20);
            this.TextOperation.TabIndex = 1;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.TextConfirm);
            this.groupBox1.Controls.Add(this.TextPassword);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.TextName);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(15, 106);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(265, 142);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Your Identity";
            // 
            // TextConfirm
            // 
            this.TextConfirm.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TextConfirm.Location = new System.Drawing.Point(19, 114);
            this.TextConfirm.Name = "TextConfirm";
            this.TextConfirm.PasswordChar = '•';
            this.TextConfirm.Size = new System.Drawing.Size(240, 20);
            this.TextConfirm.TabIndex = 5;
            // 
            // TextPassword
            // 
            this.TextPassword.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TextPassword.Location = new System.Drawing.Point(19, 68);
            this.TextPassword.Name = "TextPassword";
            this.TextPassword.PasswordChar = '•';
            this.TextPassword.Size = new System.Drawing.Size(240, 20);
            this.TextPassword.TabIndex = 3;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(16, 98);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(118, 13);
            this.label5.TabIndex = 4;
            this.label5.Text = "Confirm Passphrase";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(16, 52);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(72, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Passphrase";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(16, 26);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(84, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Display Name";
            // 
            // TextName
            // 
            this.TextName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TextName.Location = new System.Drawing.Point(109, 23);
            this.TextName.Name = "TextName";
            this.TextName.Size = new System.Drawing.Size(150, 20);
            this.TextName.TabIndex = 1;
            // 
            // ButtonCancel
            // 
            this.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ButtonCancel.Location = new System.Drawing.Point(205, 370);
            this.ButtonCancel.Name = "ButtonCancel";
            this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
            this.ButtonCancel.TabIndex = 12;
            this.ButtonCancel.Text = "Cancel";
            this.ButtonCancel.UseVisualStyleBackColor = true;
            this.ButtonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
            // 
            // ButtonOK
            // 
            this.ButtonOK.Location = new System.Drawing.Point(124, 370);
            this.ButtonOK.Name = "ButtonOK";
            this.ButtonOK.Size = new System.Drawing.Size(75, 23);
            this.ButtonOK.TabIndex = 11;
            this.ButtonOK.Text = "OK";
            this.ButtonOK.UseVisualStyleBackColor = true;
            this.ButtonOK.Click += new System.EventHandler(this.ButtonOK_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(12, 55);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(48, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Access";
            // 
            // RadioPublic
            // 
            this.RadioPublic.AutoSize = true;
            this.RadioPublic.Location = new System.Drawing.Point(34, 71);
            this.RadioPublic.Name = "RadioPublic";
            this.RadioPublic.Size = new System.Drawing.Size(54, 17);
            this.RadioPublic.TabIndex = 3;
            this.RadioPublic.TabStop = true;
            this.RadioPublic.Text = "Public";
            this.RadioPublic.UseVisualStyleBackColor = true;
            // 
            // RadioPrivate
            // 
            this.RadioPrivate.AutoSize = true;
            this.RadioPrivate.Location = new System.Drawing.Point(94, 71);
            this.RadioPrivate.Name = "RadioPrivate";
            this.RadioPrivate.Size = new System.Drawing.Size(58, 17);
            this.RadioPrivate.TabIndex = 4;
            this.RadioPrivate.TabStop = true;
            this.RadioPrivate.Text = "Private";
            this.RadioPrivate.UseVisualStyleBackColor = true;
            // 
            // RadioSecret
            // 
            this.RadioSecret.AutoSize = true;
            this.RadioSecret.Location = new System.Drawing.Point(158, 71);
            this.RadioSecret.Name = "RadioSecret";
            this.RadioSecret.Size = new System.Drawing.Size(56, 17);
            this.RadioSecret.TabIndex = 5;
            this.RadioSecret.TabStop = true;
            this.RadioSecret.Text = "Secret";
            this.RadioSecret.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(12, 262);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(90, 13);
            this.label6.TabIndex = 7;
            this.label6.Text = "Store Location";
            // 
            // LinkLocation
            // 
            this.LinkLocation.AutoSize = true;
            this.LinkLocation.Location = new System.Drawing.Point(12, 275);
            this.LinkLocation.Name = "LinkLocation";
            this.LinkLocation.Size = new System.Drawing.Size(80, 13);
            this.LinkLocation.TabIndex = 8;
            this.LinkLocation.TabStop = true;
            this.LinkLocation.Text = "Click to Browse";
            this.LinkLocation.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkStore_LinkClicked);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(12, 309);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(34, 13);
            this.label7.TabIndex = 9;
            this.label7.Text = "Note";
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(12, 322);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(262, 45);
            this.label8.TabIndex = 10;
            this.label8.Text = "To create a mobile profile point the location to an external device, such as a US" +
                "B flash drive.";
            // 
            // NewOpForm
            // 
            this.AcceptButton = this.ButtonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.CancelButton = this.ButtonCancel;
            this.ClientSize = new System.Drawing.Size(292, 405);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.LinkLocation);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.RadioSecret);
            this.Controls.Add(this.RadioPrivate);
            this.Controls.Add(this.RadioPublic);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.ButtonOK);
            this.Controls.Add(this.ButtonCancel);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.TextOperation);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NewOpForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "New Operation File";
            this.Load += new System.EventHandler(this.NewOps_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox TextOperation;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox TextConfirm;
        private System.Windows.Forms.TextBox TextPassword;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox TextName;
        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.Button ButtonOK;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RadioButton RadioPublic;
        private System.Windows.Forms.RadioButton RadioPrivate;
        private System.Windows.Forms.RadioButton RadioSecret;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.LinkLabel LinkLocation;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
    }
}