namespace DeOps.Services.Profile
{
    partial class EditProfile
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditProfile));
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.TemplateCombo = new System.Windows.Forms.ComboBox();
            this.FieldsCombo = new System.Windows.Forms.ComboBox();
            this.ValueTextBox = new System.Windows.Forms.TextBox();
            this.ButtonOK = new System.Windows.Forms.Button();
            this.ButtonCancel = new System.Windows.Forms.Button();
            this.LinkEdit = new System.Windows.Forms.LinkLabel();
            this.LinkBrowse = new System.Windows.Forms.LinkLabel();
            this.LinkNew = new System.Windows.Forms.LinkLabel();
            this.LinkPreview = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 18);
            this.label1.Name = "label1";
            this.label1.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.label1.Size = new System.Drawing.Size(61, 18);
            this.label1.TabIndex = 3;
            this.label1.Text = "Template";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(12, 95);
            this.label2.Name = "label2";
            this.label2.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.label2.Size = new System.Drawing.Size(39, 18);
            this.label2.TabIndex = 4;
            this.label2.Text = "Fields";
            // 
            // TemplateCombo
            // 
            this.TemplateCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.TemplateCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TemplateCombo.FormattingEnabled = true;
            this.TemplateCombo.Location = new System.Drawing.Point(15, 39);
            this.TemplateCombo.Name = "TemplateCombo";
            this.TemplateCombo.Size = new System.Drawing.Size(227, 21);
            this.TemplateCombo.TabIndex = 5;
            this.TemplateCombo.SelectedIndexChanged += new System.EventHandler(this.TemplateCombo_SelectedIndexChanged);
            // 
            // FieldsCombo
            // 
            this.FieldsCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.FieldsCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.FieldsCombo.FormattingEnabled = true;
            this.FieldsCombo.Location = new System.Drawing.Point(57, 92);
            this.FieldsCombo.Name = "FieldsCombo";
            this.FieldsCombo.Size = new System.Drawing.Size(124, 21);
            this.FieldsCombo.TabIndex = 6;
            this.FieldsCombo.SelectedIndexChanged += new System.EventHandler(this.FieldsCombo_SelectedIndexChanged);
            // 
            // ValueTextBox
            // 
            this.ValueTextBox.AcceptsReturn = true;
            this.ValueTextBox.AcceptsTab = true;
            this.ValueTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ValueTextBox.BackColor = System.Drawing.Color.White;
            this.ValueTextBox.Location = new System.Drawing.Point(15, 116);
            this.ValueTextBox.Multiline = true;
            this.ValueTextBox.Name = "ValueTextBox";
            this.ValueTextBox.ReadOnly = true;
            this.ValueTextBox.Size = new System.Drawing.Size(227, 149);
            this.ValueTextBox.TabIndex = 7;
            this.ValueTextBox.TextChanged += new System.EventHandler(this.ValueTextBox_TextChanged);
            // 
            // ButtonOK
            // 
            this.ButtonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonOK.Location = new System.Drawing.Point(86, 276);
            this.ButtonOK.Name = "ButtonOK";
            this.ButtonOK.Size = new System.Drawing.Size(75, 23);
            this.ButtonOK.TabIndex = 9;
            this.ButtonOK.Text = "OK";
            this.ButtonOK.UseVisualStyleBackColor = true;
            this.ButtonOK.Click += new System.EventHandler(this.ButtonOK_Click);
            // 
            // ButtonCancel
            // 
            this.ButtonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ButtonCancel.Location = new System.Drawing.Point(167, 276);
            this.ButtonCancel.Name = "ButtonCancel";
            this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
            this.ButtonCancel.TabIndex = 8;
            this.ButtonCancel.Text = "Cancel";
            this.ButtonCancel.UseVisualStyleBackColor = true;
            this.ButtonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
            // 
            // LinkEdit
            // 
            this.LinkEdit.AutoSize = true;
            this.LinkEdit.Location = new System.Drawing.Point(168, 18);
            this.LinkEdit.Name = "LinkEdit";
            this.LinkEdit.Size = new System.Drawing.Size(25, 13);
            this.LinkEdit.TabIndex = 10;
            this.LinkEdit.TabStop = true;
            this.LinkEdit.Text = "Edit";
            this.LinkEdit.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkEdit_LinkClicked);
            // 
            // LinkBrowse
            // 
            this.LinkBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LinkBrowse.AutoSize = true;
            this.LinkBrowse.Enabled = false;
            this.LinkBrowse.Location = new System.Drawing.Point(187, 95);
            this.LinkBrowse.Name = "LinkBrowse";
            this.LinkBrowse.Size = new System.Drawing.Size(51, 13);
            this.LinkBrowse.TabIndex = 11;
            this.LinkBrowse.TabStop = true;
            this.LinkBrowse.Text = "Browse...";
            this.LinkBrowse.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkBrowse_LinkClicked);
            // 
            // LinkNew
            // 
            this.LinkNew.AutoSize = true;
            this.LinkNew.Location = new System.Drawing.Point(133, 18);
            this.LinkNew.Name = "LinkNew";
            this.LinkNew.Size = new System.Drawing.Size(29, 13);
            this.LinkNew.TabIndex = 12;
            this.LinkNew.TabStop = true;
            this.LinkNew.Text = "New";
            this.LinkNew.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkNew_LinkClicked);
            // 
            // LinkPreview
            // 
            this.LinkPreview.AutoSize = true;
            this.LinkPreview.Location = new System.Drawing.Point(199, 18);
            this.LinkPreview.Name = "LinkPreview";
            this.LinkPreview.Size = new System.Drawing.Size(45, 13);
            this.LinkPreview.TabIndex = 13;
            this.LinkPreview.TabStop = true;
            this.LinkPreview.Text = "Preview";
            this.LinkPreview.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkPreview_LinkClicked);
            // 
            // EditProfile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.ButtonCancel;
            this.ClientSize = new System.Drawing.Size(254, 311);
            this.Controls.Add(this.LinkPreview);
            this.Controls.Add(this.LinkNew);
            this.Controls.Add(this.LinkBrowse);
            this.Controls.Add(this.LinkEdit);
            this.Controls.Add(this.ButtonOK);
            this.Controls.Add(this.ButtonCancel);
            this.Controls.Add(this.ValueTextBox);
            this.Controls.Add(this.FieldsCombo);
            this.Controls.Add(this.TemplateCombo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(200, 219);
            this.Name = "EditProfile";
            this.ShowInTaskbar = false;
            this.Text = "My Profile";
            this.Load += new System.EventHandler(this.EditProfile_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        internal System.Windows.Forms.ComboBox TemplateCombo;
        private System.Windows.Forms.ComboBox FieldsCombo;
        private System.Windows.Forms.TextBox ValueTextBox;
        private System.Windows.Forms.Button ButtonOK;
        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.LinkLabel LinkEdit;
        private System.Windows.Forms.LinkLabel LinkBrowse;
        private System.Windows.Forms.LinkLabel LinkNew;
        private System.Windows.Forms.LinkLabel LinkPreview;
    }
}