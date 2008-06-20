using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;


namespace RiseOp.Interface
{
    internal partial class NewProjectForm : Form
    {
        OpCore Core;

        internal uint ProjectID;


        internal NewProjectForm(OpCore core)
        {
            InitializeComponent();

            Core = core;
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            ProjectID = Core.Trust.CreateProject(NameBox.Text);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}