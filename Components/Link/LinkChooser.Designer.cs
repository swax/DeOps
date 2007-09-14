namespace DeOps.Components.Link
{
    partial class LinkChooser
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
            this.TheCancelButton = new System.Windows.Forms.Button();
            this.AddButton = new System.Windows.Forms.Button();
            this.PersonTree = new DeOps.Components.Link.LinkTree();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(101, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Make Selections";
            // 
            // TheCancelButton
            // 
            this.TheCancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.TheCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.TheCancelButton.Location = new System.Drawing.Point(168, 283);
            this.TheCancelButton.Name = "TheCancelButton";
            this.TheCancelButton.Size = new System.Drawing.Size(75, 23);
            this.TheCancelButton.TabIndex = 2;
            this.TheCancelButton.Text = "Cancel";
            this.TheCancelButton.UseVisualStyleBackColor = true;
            this.TheCancelButton.Click += new System.EventHandler(this.TheCancelButton_Click);
            // 
            // AddButton
            // 
            this.AddButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.AddButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.AddButton.Location = new System.Drawing.Point(87, 283);
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(75, 23);
            this.AddButton.TabIndex = 3;
            this.AddButton.Text = "Add";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // PersonTree
            // 
            this.PersonTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.PersonTree.BackColor = System.Drawing.SystemColors.Window;
            this.PersonTree.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.PersonTree.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.PersonTree.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.PersonTree.HeaderMenu = null;
            this.PersonTree.ItemHeight = 20;
            this.PersonTree.ItemMenu = null;
            this.PersonTree.LabelEdit = false;
            this.PersonTree.Location = new System.Drawing.Point(12, 25);
            this.PersonTree.Name = "PersonTree";
            this.PersonTree.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.PersonTree.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.PersonTree.Size = new System.Drawing.Size(231, 252);
            this.PersonTree.SmallImageList = null;
            this.PersonTree.StateImageList = null;
            this.PersonTree.TabIndex = 4;
            this.PersonTree.Text = "linkList1";
            // 
            // LinkChooser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(255, 318);
            this.Controls.Add(this.PersonTree);
            this.Controls.Add(this.AddButton);
            this.Controls.Add(this.TheCancelButton);
            this.Controls.Add(this.label1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LinkChooser";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Add People";
            this.Load += new System.EventHandler(this.LinkChooser_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button TheCancelButton;
        private System.Windows.Forms.Button AddButton;
        private LinkTree PersonTree;
    }
}