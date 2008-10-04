using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;

namespace RiseOp.Interface
{
    internal partial class InviteForm : CustomIconForm
    {
        OpCore Core;

        int page = 1;

        internal InviteForm(OpCore core)
            : base(core)
        {
            InitializeComponent();

            Core = core;

            HelpLabel.Text = HelpLabel.Text.Replace("<op>", Core.User.Settings.Operation);
        }

        private void CreateButton_Click(object sender, EventArgs e)
        {
            if (page == 1)
            {
                try
                {
                    string name;
                    IdentityBox.Text = Core.GenerateInvite(IdentityBox.Text, out name);


                    HelpLabel.Text = "This invitation can be safely given back to <name> through any medium such as IM or Email.";
                    HelpLabel.Text = HelpLabel.Text.Replace("<name>", name);

                    CopyLink.Visible = true;
                    DirectionLabel.Text = "Invitation Created";

                    NextButton.Text = "OK";
                    page++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message);
                }
            }

            else if (page == 2)
                Close();
        }

        private void CopyLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Clipboard.SetText(IdentityBox.Text);
            MessageBox.Show("Copied");
        }
    }
}