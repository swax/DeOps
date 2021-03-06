﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface;


namespace DeOps.Services.Update
{
    public partial class UpdateForm : CustomIconForm
    {
        public UpdateForm(UpdateInfo info)
        {
            InitializeComponent();

            MessageLabel.Text = "DeOps needs to be restarted to finish updating to version " + info.DottedVersion;
            
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
