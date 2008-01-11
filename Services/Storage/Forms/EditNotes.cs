using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RiseOp.Services.Storage
{
    internal partial class EditNotes : Form
    {
        internal EditNotes()
        {
            InitializeComponent();
        }

        private void EditNotes_Load(object sender, EventArgs e)
        {

        }

        private void OKButton_Click(object sender, EventArgs e)
        {

            DialogResult = DialogResult.OK;
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}