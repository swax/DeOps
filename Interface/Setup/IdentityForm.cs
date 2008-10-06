using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;


namespace RiseOp.Interface.Setup
{
    internal partial class IdentityForm : CustomIconForm
    {
        internal IdentityForm(OpCore core, ulong user)
        {
            InitializeComponent();

            string name = core.GetName(user);

            HeaderLabel.Text = HeaderLabel.Text.Replace("<name>", name);
           
            HelpLabel.Text = HelpLabel.Text.Replace("<name>", name);

            LinkBox.Text = core.GetIdentity(user);
        }

        private void CopyLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Clipboard.SetText(LinkBox.Text);
            CopyLink.Text = "Copied";
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            Close();
        }



    }
}
