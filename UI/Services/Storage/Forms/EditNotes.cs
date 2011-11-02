using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DeOps.Services.Storage
{
    public partial class EditNotes : DeOps.Interface.CustomIconForm
    {
        public EditNotes()
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