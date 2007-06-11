namespace DeOps.Components.Storage
{
    partial class DiffForm
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
            this.ChangesCombo = new System.Windows.Forms.ComboBox();
            this.ChangesRadio = new System.Windows.Forms.RadioButton();
            this.IntegratedRadio = new System.Windows.Forms.RadioButton();
            this.IntegratedCombo = new System.Windows.Forms.ComboBox();
            this.HistoryRadio = new System.Windows.Forms.RadioButton();
            this.HistoryCombo = new System.Windows.Forms.ComboBox();
            this.CompareLabel = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.UsingCombo = new System.Windows.Forms.ComboBox();
            this.ButtonCancel = new System.Windows.Forms.Button();
            this.OkButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.LocalNote = new System.Windows.Forms.Label();
            this.CurrentRadio = new System.Windows.Forms.RadioButton();
            this.WhatLabel = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ChangesCombo
            // 
            this.ChangesCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ChangesCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ChangesCombo.Enabled = false;
            this.ChangesCombo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ChangesCombo.FormattingEnabled = true;
            this.ChangesCombo.Location = new System.Drawing.Point(102, 52);
            this.ChangesCombo.Name = "ChangesCombo";
            this.ChangesCombo.Size = new System.Drawing.Size(139, 21);
            this.ChangesCombo.TabIndex = 1;
            // 
            // ChangesRadio
            // 
            this.ChangesRadio.AutoSize = true;
            this.ChangesRadio.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ChangesRadio.Location = new System.Drawing.Point(13, 53);
            this.ChangesRadio.Name = "ChangesRadio";
            this.ChangesRadio.Size = new System.Drawing.Size(67, 17);
            this.ChangesRadio.TabIndex = 2;
            this.ChangesRadio.TabStop = true;
            this.ChangesRadio.Text = "Changes";
            this.ChangesRadio.UseVisualStyleBackColor = true;
            this.ChangesRadio.CheckedChanged += new System.EventHandler(this.ChangesRadio_CheckedChanged);
            // 
            // IntegratedRadio
            // 
            this.IntegratedRadio.AutoSize = true;
            this.IntegratedRadio.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.IntegratedRadio.Location = new System.Drawing.Point(13, 80);
            this.IntegratedRadio.Name = "IntegratedRadio";
            this.IntegratedRadio.Size = new System.Drawing.Size(73, 17);
            this.IntegratedRadio.TabIndex = 5;
            this.IntegratedRadio.TabStop = true;
            this.IntegratedRadio.Text = "Integrated";
            this.IntegratedRadio.UseVisualStyleBackColor = true;
            this.IntegratedRadio.CheckedChanged += new System.EventHandler(this.IntegratedRadio_CheckedChanged);
            // 
            // IntegratedCombo
            // 
            this.IntegratedCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.IntegratedCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.IntegratedCombo.Enabled = false;
            this.IntegratedCombo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.IntegratedCombo.FormattingEnabled = true;
            this.IntegratedCombo.Location = new System.Drawing.Point(102, 79);
            this.IntegratedCombo.Name = "IntegratedCombo";
            this.IntegratedCombo.Size = new System.Drawing.Size(139, 21);
            this.IntegratedCombo.TabIndex = 4;
            // 
            // HistoryRadio
            // 
            this.HistoryRadio.AutoSize = true;
            this.HistoryRadio.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.HistoryRadio.Location = new System.Drawing.Point(13, 107);
            this.HistoryRadio.Name = "HistoryRadio";
            this.HistoryRadio.Size = new System.Drawing.Size(57, 17);
            this.HistoryRadio.TabIndex = 7;
            this.HistoryRadio.TabStop = true;
            this.HistoryRadio.Text = "History";
            this.HistoryRadio.UseVisualStyleBackColor = true;
            this.HistoryRadio.CheckedChanged += new System.EventHandler(this.HistoryRadio_CheckedChanged);
            // 
            // HistoryCombo
            // 
            this.HistoryCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.HistoryCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.HistoryCombo.Enabled = false;
            this.HistoryCombo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.HistoryCombo.FormattingEnabled = true;
            this.HistoryCombo.Location = new System.Drawing.Point(102, 106);
            this.HistoryCombo.Name = "HistoryCombo";
            this.HistoryCombo.Size = new System.Drawing.Size(139, 21);
            this.HistoryCombo.TabIndex = 6;
            // 
            // CompareLabel
            // 
            this.CompareLabel.AutoSize = true;
            this.CompareLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CompareLabel.Location = new System.Drawing.Point(12, 9);
            this.CompareLabel.Name = "CompareLabel";
            this.CompareLabel.Size = new System.Drawing.Size(56, 13);
            this.CompareLabel.TabIndex = 8;
            this.CompareLabel.Text = "Compare";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(15, 222);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(39, 13);
            this.label5.TabIndex = 11;
            this.label5.Text = "Using";
            // 
            // UsingCombo
            // 
            this.UsingCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.UsingCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.UsingCombo.FormattingEnabled = true;
            this.UsingCombo.Location = new System.Drawing.Point(60, 219);
            this.UsingCombo.Name = "UsingCombo";
            this.UsingCombo.Size = new System.Drawing.Size(202, 21);
            this.UsingCombo.TabIndex = 12;
            // 
            // ButtonCancel
            // 
            this.ButtonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ButtonCancel.Location = new System.Drawing.Point(184, 251);
            this.ButtonCancel.Name = "ButtonCancel";
            this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
            this.ButtonCancel.TabIndex = 13;
            this.ButtonCancel.Text = "Cancel";
            this.ButtonCancel.UseVisualStyleBackColor = true;
            this.ButtonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
            // 
            // OkButton
            // 
            this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OkButton.Location = new System.Drawing.Point(99, 251);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(75, 23);
            this.OkButton.TabIndex = 14;
            this.OkButton.Text = "OK";
            this.OkButton.UseVisualStyleBackColor = true;
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.LocalNote);
            this.groupBox1.Controls.Add(this.CurrentRadio);
            this.groupBox1.Controls.Add(this.ChangesRadio);
            this.groupBox1.Controls.Add(this.ChangesCombo);
            this.groupBox1.Controls.Add(this.IntegratedCombo);
            this.groupBox1.Controls.Add(this.IntegratedRadio);
            this.groupBox1.Controls.Add(this.HistoryCombo);
            this.groupBox1.Controls.Add(this.HistoryRadio);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(15, 63);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(247, 137);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "To";
            this.groupBox1.Enter += new System.EventHandler(this.groupBox1_Enter);
            // 
            // LocalNote
            // 
            this.LocalNote.AutoSize = true;
            this.LocalNote.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LocalNote.Location = new System.Drawing.Point(99, 28);
            this.LocalNote.Name = "LocalNote";
            this.LocalNote.Size = new System.Drawing.Size(0, 13);
            this.LocalNote.TabIndex = 10;
            // 
            // CurrentRadio
            // 
            this.CurrentRadio.AutoSize = true;
            this.CurrentRadio.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CurrentRadio.Location = new System.Drawing.Point(13, 26);
            this.CurrentRadio.Name = "CurrentRadio";
            this.CurrentRadio.Size = new System.Drawing.Size(59, 17);
            this.CurrentRadio.TabIndex = 9;
            this.CurrentRadio.TabStop = true;
            this.CurrentRadio.Text = "Current";
            this.CurrentRadio.UseVisualStyleBackColor = true;
            this.CurrentRadio.CheckedChanged += new System.EventHandler(this.LocalRadio_CheckedChanged);
            // 
            // WhatLabel
            // 
            this.WhatLabel.AutoSize = true;
            this.WhatLabel.Location = new System.Drawing.Point(25, 32);
            this.WhatLabel.Name = "WhatLabel";
            this.WhatLabel.Size = new System.Drawing.Size(33, 13);
            this.WhatLabel.TabIndex = 16;
            this.WhatLabel.Text = "What";
            // 
            // DiffForm
            // 
            this.AcceptButton = this.OkButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.ButtonCancel;
            this.ClientSize = new System.Drawing.Size(271, 286);
            this.Controls.Add(this.WhatLabel);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.OkButton);
            this.Controls.Add(this.ButtonCancel);
            this.Controls.Add(this.UsingCombo);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.CompareLabel);
            this.Name = "DiffForm";
            this.Text = "Differences";
            this.Load += new System.EventHandler(this.DiffForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox ChangesCombo;
        private System.Windows.Forms.RadioButton ChangesRadio;
        private System.Windows.Forms.RadioButton IntegratedRadio;
        private System.Windows.Forms.ComboBox IntegratedCombo;
        private System.Windows.Forms.RadioButton HistoryRadio;
        private System.Windows.Forms.ComboBox HistoryCombo;
        private System.Windows.Forms.Label CompareLabel;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox UsingCombo;
        private System.Windows.Forms.Button ButtonCancel;
        private System.Windows.Forms.Button OkButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton CurrentRadio;
        private System.Windows.Forms.Label LocalNote;
        private System.Windows.Forms.Label WhatLabel;
    }
}