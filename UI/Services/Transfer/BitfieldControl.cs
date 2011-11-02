using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DeOps.Services.Transfer
{
    public partial class BitfieldControl : UserControl
    {
        BitArray Field;
        int Up = -1;
        int Down = -1;
        bool Redraw = false;

        Bitmap DisplayBuffer;


        public BitfieldControl()
        {
            InitializeComponent();

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        public void UpdateField(BitArray field)
        {
            UpdateField(field, -1, -1);
        }

        public void UpdateField(BitArray updated, int upPiece, int downPiece)
        {
            if (updated == null && Field != null)
                Redraw = true;

            else if (updated != null && Field == null)
                Redraw = true;

            else if (updated != null && (Up != upPiece || Down != downPiece || !updated.Compare(Field)))
                Redraw = true;

            Field = (updated != null) ? new BitArray(updated) : null;
            Up = upPiece;
            Down = downPiece;

            Invalidate();
        }

        private void BitfieldControl_Resize(object sender, EventArgs e)
        {
            if (Width > 0 && Height > 0)
            {
                DisplayBuffer = new Bitmap(Width, Height);
                Redraw = true;
                Invalidate();
            }
        }

        Pen BorderPen = new Pen(Color.Black);
        SolidBrush CompletedBrush = new SolidBrush(Color.CornflowerBlue);
        SolidBrush MissingBrush = new SolidBrush(Color.White);
        SolidBrush UpBrush = new SolidBrush(Color.Red);
        SolidBrush DownBrush = new SolidBrush(Color.Lime);

        private void BitfieldControl_Paint(object sender, PaintEventArgs e)
        {
            if (DisplayBuffer == null)
                DisplayBuffer = new Bitmap(Width, Height);

            if (!Redraw)
            {
                e.Graphics.DrawImage(DisplayBuffer, 0, 0);
                return;
            }

            Redraw = false;

            // background
            Graphics buffer = Graphics.FromImage(DisplayBuffer);

            buffer.Clear(Color.White);
            
            if (Field == null)
                return;

            buffer.FillRectangle(CompletedBrush, 0, 3, Width, Height - 6);

            // cut out missing pieces, so worst case they are visible
            // opposed to other way where they'd be hidden

            int start = -1;

            for (int i = 0; i < Field.Length; i++)
                // has
                if (Field[i])
                {
                    if (start != -1)
                    {
                        DrawPiece(buffer, MissingBrush, start, i);
                        start = -1;
                    }
                }
                // missing
                else if(start == -1)
                    start = i;

            // draw last missing piece
            if (start != -1)
                DrawPiece(buffer, MissingBrush, start, Field.Length);

            if(Up != -1)
                DrawPiece(buffer, UpBrush, Up, Up + 1);

            if (Down != -1)
                DrawPiece(buffer, DownBrush, Down, Down + 1);

            buffer.DrawRectangle(BorderPen, 0, 3, Width, Height - 6);

            // Copy buffer to display
            e.Graphics.DrawImage(DisplayBuffer, 0, 0);
        }



        private void DrawPiece(Graphics buffer, SolidBrush brush, float start, float end)
        {
            float scale = (float)Width / (float)Field.Length;

            int x1 = (int)(start * scale);
            int x2 = (int)(end * scale);

            buffer.FillRectangle(brush, x1, 3, x2 - x1, Height - 6);
        }

        
    }
}
