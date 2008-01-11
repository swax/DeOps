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

        private void InviteForm_Load(object sender, EventArgs e)
        {
            LinkLocation.Text = Application.StartupPath + Path.DirectorySeparatorChar + Core.User.Settings.Operation + "-invite.dop";
        }

        private void LinkLocation_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                FolderBrowserDialog browse = new FolderBrowserDialog();

                browse.ShowNewFolderButton = true;
                browse.SelectedPath = LinkLocation.Text;

                if (browse.ShowDialog(this) == DialogResult.OK)
                    LinkLocation.Text = browse.SelectedPath + Path.DirectorySeparatorChar + Core.User.Settings.Operation + "-invite.dop";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            try
            {
                Core.User.SaveInvite(LinkLocation.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }

            Close();
        }

       
    }
}