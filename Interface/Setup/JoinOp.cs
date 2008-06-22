using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RiseOp.Interface.Startup
{
    internal partial class JoinOp : CustomIconForm
    {
        internal string OpName = "";
        internal AccessType OpAccess = AccessType.Public;


        internal JoinOp()
        {
            InitializeComponent();
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            OpName = LinkBox.Text;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void LinkBox_TextChanged(object sender, EventArgs e)
        {
            if (LinkBox.Text.StartsWith("riseop://"))
                LinkBox.Text = LinkBox.Text.Substring(9);
        }
    }
}
