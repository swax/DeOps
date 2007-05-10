namespace DeOps.Components.Chat
{
    partial class ChatSplit
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
            this.ViewContainer = new System.Windows.Forms.SplitContainer();
            this.ViewContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // ViewContainer
            // 
            this.ViewContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ViewContainer.Location = new System.Drawing.Point(0, 0);
            this.ViewContainer.Name = "ViewContainer";
            this.ViewContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.ViewContainer.Size = new System.Drawing.Size(410, 316);
            this.ViewContainer.SplitterDistance = 157;
            this.ViewContainer.TabIndex = 0;
            // 
            // ChatSplit
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ViewContainer);
            this.Name = "ChatSplit";
            this.Size = new System.Drawing.Size(410, 316);
            this.ViewContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.SplitContainer ViewContainer;

    }
}
