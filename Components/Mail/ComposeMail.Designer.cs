using DeOps.Interface;

namespace DeOps.Components.Mail
{
    partial class ComposeMail
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.ToTextBox = new System.Windows.Forms.TextBox();
            this.CCTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.SubjectTextBox = new System.Windows.Forms.TextBox();
            this.MessageBody = new DeOps.Interface.TextInput();
            this.SendButton = new System.Windows.Forms.Button();
            this.ExitButton = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.ListFiles = new System.Windows.Forms.ComboBox();
            this.LinkAdd = new System.Windows.Forms.LinkLabel();
            this.LinkRemove = new System.Windows.Forms.LinkLabel();
            this.BrowseTo = new System.Windows.Forms.LinkLabel();
            this.BrowseCC = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(21, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "To";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(3, 35);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(21, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "CC";
            // 
            // ToTextBox
            // 
            this.ToTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ToTextBox.Location = new System.Drawing.Point(52, 6);
            this.ToTextBox.Name = "ToTextBox";
            this.ToTextBox.Size = new System.Drawing.Size(206, 20);
            this.ToTextBox.TabIndex = 2;
            // 
            // CCTextBox
            // 
            this.CCTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.CCTextBox.Location = new System.Drawing.Point(52, 32);
            this.CCTextBox.Name = "CCTextBox";
            this.CCTextBox.Size = new System.Drawing.Size(206, 20);
            this.CCTextBox.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(3, 61);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(50, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Subject";
            // 
            // SubjectTextBox
            // 
            this.SubjectTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.SubjectTextBox.Location = new System.Drawing.Point(52, 58);
            this.SubjectTextBox.Name = "SubjectTextBox";
            this.SubjectTextBox.Size = new System.Drawing.Size(254, 20);
            this.SubjectTextBox.TabIndex = 5;
            // 
            // MessageBody
            // 
            this.MessageBody.AcceptTabs = true;
            this.MessageBody.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.MessageBody.EnterClears = false;
            this.MessageBody.Location = new System.Drawing.Point(6, 111);
            this.MessageBody.Name = "MessageBody";
            this.MessageBody.ShowFontStrip = true;
            this.MessageBody.Size = new System.Drawing.Size(300, 136);
            this.MessageBody.TabIndex = 6;
            // 
            // SendButton
            // 
            this.SendButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SendButton.Location = new System.Drawing.Point(150, 253);
            this.SendButton.Name = "SendButton";
            this.SendButton.Size = new System.Drawing.Size(75, 23);
            this.SendButton.TabIndex = 7;
            this.SendButton.Text = "Send";
            this.SendButton.UseVisualStyleBackColor = true;
            this.SendButton.Click += new System.EventHandler(this.SendButton_Click);
            // 
            // ExitButton
            // 
            this.ExitButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ExitButton.Location = new System.Drawing.Point(231, 253);
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.Size = new System.Drawing.Size(75, 23);
            this.ExitButton.TabIndex = 8;
            this.ExitButton.Text = "Cancel";
            this.ExitButton.UseVisualStyleBackColor = true;
            this.ExitButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(6, 87);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(32, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Files";
            // 
            // ListFiles
            // 
            this.ListFiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ListFiles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ListFiles.FormattingEnabled = true;
            this.ListFiles.Location = new System.Drawing.Point(130, 84);
            this.ListFiles.Name = "ListFiles";
            this.ListFiles.Size = new System.Drawing.Size(176, 21);
            this.ListFiles.TabIndex = 10;
            // 
            // LinkAdd
            // 
            this.LinkAdd.ActiveLinkColor = System.Drawing.Color.Blue;
            this.LinkAdd.AutoSize = true;
            this.LinkAdd.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LinkAdd.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.LinkAdd.Location = new System.Drawing.Point(49, 87);
            this.LinkAdd.Name = "LinkAdd";
            this.LinkAdd.Size = new System.Drawing.Size(26, 13);
            this.LinkAdd.TabIndex = 14;
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
            this.LinkRemove.Location = new System.Drawing.Point(77, 87);
            this.LinkRemove.Name = "LinkRemove";
            this.LinkRemove.Size = new System.Drawing.Size(46, 13);
            this.LinkRemove.TabIndex = 13;
            this.LinkRemove.TabStop = true;
            this.LinkRemove.Text = "Remove";
            this.LinkRemove.VisitedLinkColor = System.Drawing.Color.Blue;
            this.LinkRemove.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkRemove_LinkClicked);
            // 
            // BrowseTo
            // 
            this.BrowseTo.ActiveLinkColor = System.Drawing.Color.Blue;
            this.BrowseTo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BrowseTo.AutoSize = true;
            this.BrowseTo.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BrowseTo.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.BrowseTo.Location = new System.Drawing.Point(264, 9);
            this.BrowseTo.Name = "BrowseTo";
            this.BrowseTo.Size = new System.Drawing.Size(42, 13);
            this.BrowseTo.TabIndex = 15;
            this.BrowseTo.TabStop = true;
            this.BrowseTo.Text = "Browse";
            this.BrowseTo.VisitedLinkColor = System.Drawing.Color.Blue;
            this.BrowseTo.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.BrowseTo_LinkClicked);
            // 
            // BrowseCC
            // 
            this.BrowseCC.ActiveLinkColor = System.Drawing.Color.Blue;
            this.BrowseCC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BrowseCC.AutoSize = true;
            this.BrowseCC.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BrowseCC.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.BrowseCC.Location = new System.Drawing.Point(264, 35);
            this.BrowseCC.Name = "BrowseCC";
            this.BrowseCC.Size = new System.Drawing.Size(42, 13);
            this.BrowseCC.TabIndex = 16;
            this.BrowseCC.TabStop = true;
            this.BrowseCC.Text = "Browse";
            this.BrowseCC.VisitedLinkColor = System.Drawing.Color.Blue;
            this.BrowseCC.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.BrowseCC_LinkClicked);
            // 
            // ComposeMail
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.BrowseCC);
            this.Controls.Add(this.BrowseTo);
            this.Controls.Add(this.LinkAdd);
            this.Controls.Add(this.LinkRemove);
            this.Controls.Add(this.ListFiles);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.ExitButton);
            this.Controls.Add(this.SendButton);
            this.Controls.Add(this.MessageBody);
            this.Controls.Add(this.SubjectTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.CCTextBox);
            this.Controls.Add(this.ToTextBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "ComposeMail";
            this.Size = new System.Drawing.Size(309, 279);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox ToTextBox;
        private System.Windows.Forms.TextBox CCTextBox;
        private System.Windows.Forms.Label label3;
        internal System.Windows.Forms.TextBox SubjectTextBox;
        internal TextInput MessageBody;
        private System.Windows.Forms.Button SendButton;
        private System.Windows.Forms.Button ExitButton;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox ListFiles;
        private System.Windows.Forms.LinkLabel LinkAdd;
        private System.Windows.Forms.LinkLabel LinkRemove;
        private System.Windows.Forms.LinkLabel BrowseTo;
        private System.Windows.Forms.LinkLabel BrowseCC;
    }
}
