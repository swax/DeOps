namespace RiseOp.Services.Plan
{
    partial class BlockRow
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
            // BlockRow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.DoubleBuffered = true;
            this.Name = "BlockRow";
            this.Size = new System.Drawing.Size(309, 22);
            this.VisibleChanged += new System.EventHandler(this.BlockRow_VisibleChanged);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.BlockRow_MouseMove);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.BlockRow_MouseClick);
            this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.BlockRow_MouseDoubleClick);
            this.Resize += new System.EventHandler(this.BlockRow_Resize);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.BlockRow_Paint);
            this.ResumeLayout(false);

        }

        #endregion


    }
}
