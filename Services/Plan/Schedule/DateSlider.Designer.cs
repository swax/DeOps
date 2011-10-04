namespace DeOps.Services.Plan
{
    partial class DateSlider
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
            this.components = new System.ComponentModel.Container();
            this.ButtonTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // ButtonTimer
            // 
            this.ButtonTimer.Interval = 25;
            this.ButtonTimer.Tick += new System.EventHandler(this.ButtonTimer_Tick);
            // 
            // DateSlider
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.DoubleBuffered = true;
            this.Name = "DateSlider";
            this.Size = new System.Drawing.Size(396, 25);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.DateSlider_Paint);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.DateSlider_MouseMove);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.DateSlider_MouseDown);
            this.Resize += new System.EventHandler(this.DateSlider_Resize);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.DateSlider_MouseUp);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer ButtonTimer;
    }
}
