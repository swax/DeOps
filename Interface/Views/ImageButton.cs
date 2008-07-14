using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;


namespace RiseOp.Interface.Views
{
    enum ButtonState { Hot, Normal, Down }


    public class ImageButton : PictureBox
    {
        Image _ButtonHot;
        Image _ButtonNormal;
        Image _ButtonDown;

        [Category("Appearance")]
        public Image ButtonHot
        {
            get { return _ButtonHot; }
            set { _ButtonHot = value; }
        }

        [Category("Appearance")]
        public Image ButtonNormal
        {
            get { return _ButtonNormal; }
            set { _ButtonNormal = value; }
        }

        [Category("Appearance")]
        public Image ButtonDown
        {
            get { return _ButtonDown; }
            set { _ButtonDown = value; }
        }

        ButtonState State = ButtonState.Normal;


        public ImageButton()
        {
            MouseMove  += new MouseEventHandler(ImageButton_MouseMove);
            MouseDown  += new MouseEventHandler(ImageButton_MouseDown);
            MouseLeave += new EventHandler(ImageButton_MouseLeave);
            MouseUp    += new MouseEventHandler(ImageButton_MouseUp);
        }

        private void ImageButton_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.None)
                SetState(ButtonState.Hot);
            else
                SetState(ButtonState.Down);
        }

        private void ImageButton_MouseDown(object sender, MouseEventArgs e)
        {
            SetState(ButtonState.Down);
        }

        private void ImageButton_MouseLeave(object sender, EventArgs e)
        {
            SetState(ButtonState.Normal);
        }

        private void ImageButton_MouseUp(object sender, MouseEventArgs e)
        {
            SetState(ButtonState.Normal);
        }

        void SetState(ButtonState state)
        {
            if (State == state)
                return;

            State = state;

            if (State == ButtonState.Hot)
                Image = _ButtonHot;

            else if (State == ButtonState.Normal)
                Image = _ButtonNormal;

            else if (State == ButtonState.Down)
                Image = _ButtonDown;
        }
    }


}
