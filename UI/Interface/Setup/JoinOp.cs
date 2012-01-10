using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DeOps.Interface.Startup
{
    public partial class JoinOp : CustomIconForm
    {
        public string OpLink = "";
        public AccessType OpAccess = AccessType.Public;

        public JoinOp()
        {
            InitializeComponent();
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            OpLink = LinkBox.Text;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
