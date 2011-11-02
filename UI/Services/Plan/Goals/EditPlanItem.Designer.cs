namespace DeOps.Services.Plan
{
    partial class EditPlanItem
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditPlanItem));
            this.OfLabel = new System.Windows.Forms.Label();
            this.TotalHours = new System.Windows.Forms.TextBox();
            this.CompletedHours = new System.Windows.Forms.TextBox();
            this.ProgressLabel = new System.Windows.Forms.Label();
            this.ExitButton = new System.Windows.Forms.Button();
            this.OkButton = new System.Windows.Forms.Button();
            this.DescriptionInput = new DeOps.Interface.TextInput();
            this.label5 = new System.Windows.Forms.Label();
            this.TitleBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // OfLabel
            // 
            this.OfLabel.AutoSize = true;
            this.OfLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OfLabel.Location = new System.Drawing.Point(121, 48);
            this.OfLabel.Name = "OfLabel";
            this.OfLabel.Size = new System.Drawing.Size(18, 13);
            this.OfLabel.TabIndex = 44;
            this.OfLabel.Text = "of";
            // 
            // TotalHours
            // 
            this.TotalHours.Location = new System.Drawing.Point(145, 45);
            this.TotalHours.Name = "TotalHours";
            this.TotalHours.Size = new System.Drawing.Size(31, 20);
            this.TotalHours.TabIndex = 43;
            this.TotalHours.Text = "10";
            this.TotalHours.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // CompletedHours
            // 
            this.CompletedHours.Location = new System.Drawing.Point(84, 45);
            this.CompletedHours.Name = "CompletedHours";
            this.CompletedHours.Size = new System.Drawing.Size(31, 20);
            this.CompletedHours.TabIndex = 42;
            this.CompletedHours.Text = "0";
            this.CompletedHours.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // ProgressLabel
            // 
            this.ProgressLabel.AutoSize = true;
            this.ProgressLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ProgressLabel.Location = new System.Drawing.Point(12, 48);
            this.ProgressLabel.Name = "ProgressLabel";
            this.ProgressLabel.Size = new System.Drawing.Size(66, 13);
            this.ProgressLabel.TabIndex = 41;
            this.ProgressLabel.Text = "Completed";
            // 
            // ExitButton
            // 
            this.ExitButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ExitButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ExitButton.Location = new System.Drawing.Point(206, 286);
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.Size = new System.Drawing.Size(75, 23);
            this.ExitButton.TabIndex = 40;
            this.ExitButton.Text = "Cancel";
            this.ExitButton.UseVisualStyleBackColor = true;
            this.ExitButton.Click += new System.EventHandler(this.ExitButton_Click);
            // 
            // OkButton
            // 
            this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OkButton.Location = new System.Drawing.Point(125, 286);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(75, 23);
            this.OkButton.TabIndex = 39;
            this.OkButton.Text = "OK";
            this.OkButton.UseVisualStyleBackColor = true;
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // DescriptionInput
            // 
            this.DescriptionInput.AcceptTabs = true;
            this.DescriptionInput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.DescriptionInput.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.DescriptionInput.EnterClears = false;
            this.DescriptionInput.Location = new System.Drawing.Point(14, 102);
            this.DescriptionInput.Name = "DescriptionInput";
            this.DescriptionInput.ReadOnly = false;
            this.DescriptionInput.ShowFontStrip = false;
            this.DescriptionInput.Size = new System.Drawing.Size(267, 178);
            this.DescriptionInput.TabIndex = 38;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(11, 86);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(40, 13);
            this.label5.TabIndex = 37;
            this.label5.Text = "Notes";
            // 
            // TitleBox
            // 
            this.TitleBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.TitleBox.Location = new System.Drawing.Point(52, 12);
            this.TitleBox.Name = "TitleBox";
            this.TitleBox.Size = new System.Drawing.Size(229, 20);
            this.TitleBox.TabIndex = 30;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(11, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 13);
            this.label1.TabIndex = 29;
            this.label1.Text = "Title";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(182, 48);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(99, 13);
            this.label4.TabIndex = 45;
            this.label4.Text = "Estimated Hours";
            // 
            // EditPlanItem
            // 
            this.AcceptButton = this.OkButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.ExitButton;
            this.ClientSize = new System.Drawing.Size(293, 321);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.OfLabel);
            this.Controls.Add(this.TotalHours);
            this.Controls.Add(this.CompletedHours);
            this.Controls.Add(this.ProgressLabel);
            this.Controls.Add(this.ExitButton);
            this.Controls.Add(this.OkButton);
            this.Controls.Add(this.DescriptionInput);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.TitleBox);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EditPlanItem";
            this.ShowInTaskbar = false;
            this.Text = "EditPlanItem";
            this.Load += new System.EventHandler(this.EditPlanItem_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label OfLabel;
        private System.Windows.Forms.TextBox TotalHours;
        private System.Windows.Forms.TextBox CompletedHours;
        private System.Windows.Forms.Label ProgressLabel;
        private System.Windows.Forms.Button ExitButton;
        private System.Windows.Forms.Button OkButton;
        private DeOps.Interface.TextInput DescriptionInput;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox TitleBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
    }
}