namespace DeOps.Services.Plan
{
    partial class ProgressText
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
            this.SuspendLayout();
            // 
            // ProgressText
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "ProgressText";
            this.Size = new System.Drawing.Size(226, 16);
            this.MouseEnter += new System.EventHandler(this.ProgressText_MouseEnter);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.ProgressText_Paint);
            this.MouseLeave += new System.EventHandler(this.ProgressText_MouseLeave);
            this.ResumeLayout(false);

        }

        #endregion

    }
}
