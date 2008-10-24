using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp.Interface;


namespace RiseOp.Services.Update
{
    internal partial class UpdateForm : CustomIconForm
    {
        internal UpdateForm(UpdateInfo info)
        {
            InitializeComponent();

            MessageLabel.Text = "RiseOp needs to be restarted to finish updating to version " + info.DottedVersion;
            
            NotesBox.Text = info.Notes;
        }

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void LaterButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
