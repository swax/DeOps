using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace DeOps.Services.Plan
{
    public partial class ProgressText : UserControl
    {
        Font TahomaBold = new Font("Tahoma", 7);
        SolidBrush BlackBrush = new SolidBrush(Color.Black);

        SolidBrush Overlay = new SolidBrush(Color.FromArgb(128, Color.White));

        public int Level;
        public int Completed;
        public int Total;

        bool ShowCompletion;

        public ProgressText()
        {
            InitializeComponent();

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        private void ProgressText_Paint(object sender, PaintEventArgs e)
        {
            int indent = Level * 8;
            int width = Width - indent;

            if (ProgressBarRenderer.IsSupported)
            {   
                int div = (Total == 0) ? 1 : Total;

                Rectangle bar = new Rectangle(indent, 0, width, Height);
                Rectangle chunks = new Rectangle(indent, 0, width * Completed / div, Height);

                ProgressBarRenderer.DrawHorizontalBar(e.Graphics, bar);
                ProgressBarRenderer.DrawHorizontalChunks(e.Graphics, chunks);

                e.Graphics.FillRectangle(Overlay, ClientRectangle);
            }

            if (ShowCompletion)
            {
                string text = Completed.ToString() + " / " + Total.ToString() + " hours";
                SizeF size = e.Graphics.MeasureString(text, TahomaBold);

                e.Graphics.DrawString(text, TahomaBold, BlackBrush,
                                      indent + (width - size.Width) / 2,
                                      (Height - size.Height) / 2);
            }
        }

        private void ProgressText_MouseEnter(object sender, EventArgs e)
        {
            ShowCompletion = true;
            Invalidate();
        }

        private void ProgressText_MouseLeave(object sender, EventArgs e)
        {
            ShowCompletion = false;
            Invalidate();
        }
    }
}
