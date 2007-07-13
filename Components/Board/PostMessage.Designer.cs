namespace DeOps.Components.Board
{
    partial class PostMessage
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.LinkAdd = new System.Windows.Forms.LinkLabel();
            this.LinkRemove = new System.Windows.Forms.LinkLabel();
            this.ListFiles = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.ExitButton = new System.Windows.Forms.Button();
            this.PostButton = new System.Windows.Forms.Button();
            this.MessageBody = new DeOps.Interface.TextInput();
            this.SubjectTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.ScopeLabel = new System.Windows.Forms.Label();
            this.ScopeHigh = new System.Windows.Forms.RadioButton();
            this.ScopeLow = new System.Windows.Forms.RadioButton();
            this.ScopeAll = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // LinkAdd
            // 
            this.LinkAdd.ActiveLinkColor = System.Drawing.Color.Blue;
            this.LinkAdd.AutoSize = true;
            this.LinkAdd.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LinkAdd.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.LinkAdd.Location = new System.Drawing.Point(56, 40);
            this.LinkAdd.Name = "LinkAdd";
            this.LinkAdd.Size = new System.Drawing.Size(26, 13);
            this.LinkAdd.TabIndex = 32;
            this.LinkAdd.TabStop = true;
            this.LinkAdd.Text = "Add";
            this.LinkAdd.VisitedLinkColor = System.Drawing.Color.Blue;
            this.LinkAdd.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkAdd_LinkClicked);
            // 
            // LinkRemove
            // 
            this.LinkRemove.ActiveLinkColor = System.Drawing.Color.Blue;
            this.LinkRemove.AutoSize = true;
            this.LinkRemove.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LinkRemove.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.LinkRemove.Location = new System.Drawing.Point(81, 40);
            this.LinkRemove.Name = "LinkRemove";
            this.LinkRemove.Size = new System.Drawing.Size(46, 13);
            this.LinkRemove.TabIndex = 31;
            this.LinkRemove.TabStop = true;
            this.LinkRemove.Text = "Remove";
            this.LinkRemove.VisitedLinkColor = System.Drawing.Color.Blue;
            this.LinkRemove.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkRemove_LinkClicked);
            // 
            // ListFiles
            // 
            this.ListFiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ListFiles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ListFiles.FormattingEnabled = true;
            this.ListFiles.Location = new System.Drawing.Point(133, 37);
            this.ListFiles.Name = "ListFiles";
            this.ListFiles.Size = new System.Drawing.Size(253, 21);
            this.ListFiles.TabIndex = 30;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(3, 40);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(32, 13);
            this.label4.TabIndex = 29;
            this.label4.Text = "Files";
            // 
            // ExitButton
            // 
            this.ExitButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ExitButton.Location = new System.Drawing.Point(315, 313);
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.Size = new System.Drawing.Size(75, 23);
            this.ExitButton.TabIndex = 28;
            this.ExitButton.Text = "Cancel";
            this.ExitButton.UseVisualStyleBackColor = true;
            this.ExitButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // PostButton
            // 
            this.PostButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.PostButton.Location = new System.Drawing.Point(234, 313);
            this.PostButton.Name = "PostButton";
            this.PostButton.Size = new System.Drawing.Size(75, 23);
            this.PostButton.TabIndex = 27;
            this.PostButton.Text = "Post";
            this.PostButton.UseVisualStyleBackColor = true;
            this.PostButton.Click += new System.EventHandler(this.PostButton_Click);
            // 
            // MessageBody
            // 
            this.MessageBody.AcceptTabs = true;
            this.MessageBody.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.MessageBody.EnterClears = false;
            this.MessageBody.Location = new System.Drawing.Point(6, 68);
            this.MessageBody.Name = "MessageBody";
            this.MessageBody.ShowFontStrip = true;
            this.MessageBody.Size = new System.Drawing.Size(384, 239);
            this.MessageBody.TabIndex = 26;
            // 
            // SubjectTextBox
            // 
            this.SubjectTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.SubjectTextBox.Location = new System.Drawing.Point(59, 7);
            this.SubjectTextBox.Name = "SubjectTextBox";
            this.SubjectTextBox.Size = new System.Drawing.Size(327, 20);
            this.SubjectTextBox.TabIndex = 25;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(3, 10);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(50, 13);
            this.label3.TabIndex = 24;
            this.label3.Text = "Subject";
            // 
            // ScopeLabel
            // 
            this.ScopeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ScopeLabel.AutoSize = true;
            this.ScopeLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ScopeLabel.Location = new System.Drawing.Point(3, 318);
            this.ScopeLabel.Name = "ScopeLabel";
            this.ScopeLabel.Size = new System.Drawing.Size(41, 13);
            this.ScopeLabel.TabIndex = 33;
            this.ScopeLabel.Text = "Scope";
            // 
            // ScopeHigh
            // 
            this.ScopeHigh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ScopeHigh.AutoSize = true;
            this.ScopeHigh.Location = new System.Drawing.Point(92, 316);
            this.ScopeHigh.Name = "ScopeHigh";
            this.ScopeHigh.Size = new System.Drawing.Size(47, 17);
            this.ScopeHigh.TabIndex = 34;
            this.ScopeHigh.Text = "High";
            this.ScopeHigh.UseVisualStyleBackColor = true;
            // 
            // ScopeLow
            // 
            this.ScopeLow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ScopeLow.AutoSize = true;
            this.ScopeLow.Location = new System.Drawing.Point(145, 316);
            this.ScopeLow.Name = "ScopeLow";
            this.ScopeLow.Size = new System.Drawing.Size(45, 17);
            this.ScopeLow.TabIndex = 35;
            this.ScopeLow.Text = "Low";
            this.ScopeLow.UseVisualStyleBackColor = true;
            // 
            // ScopeAll
            // 
            this.ScopeAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ScopeAll.AutoSize = true;
            this.ScopeAll.Checked = true;
            this.ScopeAll.Location = new System.Drawing.Point(50, 316);
            this.ScopeAll.Name = "ScopeAll";
            this.ScopeAll.Size = new System.Drawing.Size(36, 17);
            this.ScopeAll.TabIndex = 36;
            this.ScopeAll.TabStop = true;
            this.ScopeAll.Text = "All";
            this.ScopeAll.UseVisualStyleBackColor = true;
            // 
            // PostMessage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ScopeAll);
            this.Controls.Add(this.ScopeLow);
            this.Controls.Add(this.ScopeHigh);
            this.Controls.Add(this.ScopeLabel);
            this.Controls.Add(this.LinkAdd);
            this.Controls.Add(this.LinkRemove);
            this.Controls.Add(this.ListFiles);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.ExitButton);
            this.Controls.Add(this.PostButton);
            this.Controls.Add(this.MessageBody);
            this.Controls.Add(this.SubjectTextBox);
            this.Controls.Add(this.label3);
            this.DoubleBuffered = true;
            this.Name = "PostMessage";
            this.Size = new System.Drawing.Size(393, 339);
            this.Load += new System.EventHandler(this.PostMessage_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.LinkLabel LinkAdd;
        private System.Windows.Forms.LinkLabel LinkRemove;
        private System.Windows.Forms.ComboBox ListFiles;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button ExitButton;
        private System.Windows.Forms.Button PostButton;
        internal DeOps.Interface.TextInput MessageBody;
        internal System.Windows.Forms.TextBox SubjectTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label ScopeLabel;
        private System.Windows.Forms.RadioButton ScopeHigh;
        private System.Windows.Forms.RadioButton ScopeLow;
        private System.Windows.Forms.RadioButton ScopeAll;
    }
}
