using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DeOps.Interface
{
    internal partial class GetTextDialog : Form
    {
        internal GetTextDialog(string title, string direction, string defaultText)
        {
            InitializeComponent();

            Text = title;
            DirectionLabel.Text  = direction;
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