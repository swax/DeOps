using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;


namespace DeOps.Interface
{
    internal partial class GetTextDialog : CustomIconForm
    {
        // default icon
        internal GetTextDialog(string title, string direction, string defaultText)
        {
            InitializeComponent();

            SetupBox(title, direction, defaultText);
        }

        // main operation icon
        internal GetTextDialog(OpCore core, string title, string direction, string defaultText)
            : base(core)
        {
            InitializeComponent();

            SetupBox(title, direction, defaultText);
        }

        // service icon
        internal GetTextDialog(Icon image, string title, string direction, string defaultText)
        {
            InitializeComponent();

            SetupBox(title, direction, defaultText);

            Icon = image;
        }

        internal void BigResultBox()
        {
            ResultBox.Multiline = true;
            ResultBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Width = 350;
            Height = 200;
        }

        void SetupBox(string title, string direction, string defaultText)
        {
            Text = title;
            DirectionLabel.Text = direction;
            ResultBox.Text = defaultText;
        }
        
        private void GetTextDialog_Load(object sender, EventArgs e)
        {
            if (DirectionLabel.Width > ResultBox.Width)
                Width = DirectionLabel.Width + 35;
        }
        
        private void OkButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }

    }
}