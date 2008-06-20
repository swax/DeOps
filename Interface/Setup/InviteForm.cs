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
    internal partial class InviteForm : Form
    {
        OpCore Core;

        internal InviteForm(OpCore core)
        {
            InitializeComponent();

            Core = core;
        }

        private void CreateButton_Click(object sender, EventArgs e)
        {
            try
            {
                ValidateInput();

                if (!Utilities.VerifyPassphrase(Core, ThreatLevel.Medium))
                    return;

                LinkBox.Text = Core.CreateInvite(PasswordBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        void ValidateInput()
        {
            if (PasswordBox.Text == "")
                throw new Exception("Password cannot be blank");


            if (PasswordBox.Text != ConfirmBox.Text)
                throw new Exception("Passwords do not match");
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void CopyLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Clipboard.SetText(LinkBox.Text);
        }

        private void InviteForm_Load(object sender, EventArgs e)
        {

        }
    }
}