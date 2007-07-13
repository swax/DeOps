namespace DeOps.Interface
{
    partial class TextInput
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TextInput));
            this.FontToolStrip = new System.Windows.Forms.ToolStrip();
            this.SmallerButton = new System.Windows.Forms.ToolStripButton();
            this.NormalButton = new System.Windows.Forms.ToolStripButton();
            this.LargerButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.BoldButton = new System.Windows.Forms.ToolStripButton();
            this.ItalicsButton = new System.Windows.Forms.ToolStripButton();
            this.UnderlineButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.ColorsButton = new System.Windows.Forms.ToolStripButton();
            this.InputBox = new System.Windows.Forms.RichTextBox();
            this.FontToolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // FontToolStrip
            // 
            this.FontToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.FontToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SmallerButton,
            this.NormalButton,
            this.LargerButton,
            this.toolStripSeparator1,
            this.BoldButton,
            this.ItalicsButton,
            this.UnderlineButton,
            this.toolStripSeparator2,
            this.ColorsButton});
            this.FontToolStrip.Location = new System.Drawing.Point(0, 0);
            this.FontToolStrip.Name = "FontToolStrip";
            this.FontToolStrip.Size = new System.Drawing.Size(239, 25);
            this.FontToolStrip.TabIndex = 0;
            this.FontToolStrip.Text = "toolStrip1";
            // 
            // SmallerButton
            // 
            this.SmallerButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.SmallerButton.Image = ((System.Drawing.Image)(resources.GetObject("SmallerButton.Image")));
            this.SmallerButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.SmallerButton.Name = "SmallerButton";
            this.SmallerButton.Size = new System.Drawing.Size(23, 22);
            this.SmallerButton.Text = "Smaller";
            this.SmallerButton.Click += new System.EventHandler(this.SmallerButton_Click);
            // 
            // NormalButton
            // 
            this.NormalButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.NormalButton.Image = ((System.Drawing.Image)(resources.GetObject("NormalButton.Image")));
            this.NormalButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.NormalButton.Name = "NormalButton";
            this.NormalButton.Size = new System.Drawing.Size(23, 22);
            this.NormalButton.Text = "Normal";
            this.NormalButton.Click += new System.EventHandler(this.NormalButton_Click);
            // 
            // LargerButton
            // 
            this.LargerButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.LargerButton.Image = ((System.Drawing.Image)(resources.GetObject("LargerButton.Image")));
            this.LargerButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.LargerButton.Name = "LargerButton";
            this.LargerButton.Size = new System.Drawing.Size(23, 22);
            this.LargerButton.Text = "Larger";
            this.LargerButton.Click += new System.EventHandler(this.LargerButton_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // BoldButton
            // 
            this.BoldButton.CheckOnClick = true;
            this.BoldButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.BoldButton.Image = ((System.Drawing.Image)(resources.GetObject("BoldButton.Image")));
            this.BoldButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.BoldButton.Name = "BoldButton";
            this.BoldButton.Size = new System.Drawing.Size(23, 22);
            this.BoldButton.Text = "Bold";
            this.BoldButton.Click += new System.EventHandler(this.BoldButton_Click);
            // 
            // ItalicsButton
            // 
            this.ItalicsButton.CheckOnClick = true;
            this.ItalicsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ItalicsButton.Image = ((System.Drawing.Image)(resources.GetObject("ItalicsButton.Image")));
            this.ItalicsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ItalicsButton.Name = "ItalicsButton";
            this.ItalicsButton.Size = new System.Drawing.Size(23, 22);
            this.ItalicsButton.Text = "Italics";
            this.ItalicsButton.Click += new System.EventHandler(this.ItalicsButton_Click);
            // 
            // UnderlineButton
            // 
            this.UnderlineButton.CheckOnClick = true;
            this.UnderlineButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.UnderlineButton.Image = ((System.Drawing.Image)(resources.GetObject("UnderlineButton.Image")));
            this.UnderlineButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.UnderlineButton.Name = "UnderlineButton";
            this.UnderlineButton.Size = new System.Drawing.Size(23, 22);
            this.UnderlineButton.Text = "Underline";
            this.UnderlineButton.Click += new System.EventHandler(this.UnderlineButton_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // ColorsButton
            // 
            this.ColorsButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ColorsButton.Image = ((System.Drawing.Image)(resources.GetObject("ColorsButton.Image")));
            this.ColorsButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ColorsButton.Name = "ColorsButton";
            this.ColorsButton.Size = new System.Drawing.Size(23, 22);
            this.ColorsButton.Text = "Colors";
            this.ColorsButton.Click += new System.EventHandler(this.ColorsButton_Click);
            // 
            // InputBox
            // 
            this.InputBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.InputBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InputBox.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InputBox.Location = new System.Drawing.Point(0, 25);
            this.InputBox.Name = "InputBox";
            this.InputBox.Size = new System.Drawing.Size(239, 26);
            this.InputBox.TabIndex = 1;
            this.InputBox.Text = "";
            this.InputBox.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.InputBox_LinkClicked);
            this.InputBox.SelectionChanged += new System.EventHandler(this.InputBox_SelectionChanged);
            this.InputBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.InputBox_KeyDown);
            // 
            // TextInput
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.InputBox);
            this.Controls.Add(this.FontToolStrip);
            this.Name = "TextInput";
            this.Size = new System.Drawing.Size(239, 51);
            this.FontToolStrip.ResumeLayout(false);
            this.FontToolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip FontToolStrip;
        private System.Windows.Forms.ToolStripButton SmallerButton;
        private System.Windows.Forms.ToolStripButton NormalButton;
        private System.Windows.Forms.ToolStripButton LargerButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton BoldButton;
        private System.Windows.Forms.ToolStripButton ItalicsButton;
        private System.Windows.Forms.ToolStripButton UnderlineButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton ColorsButton;
        internal System.Windows.Forms.RichTextBox InputBox;
    }
}
