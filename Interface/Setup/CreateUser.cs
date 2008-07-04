using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Implementation.Protocol.Special;


namespace RiseOp.Interface.Startup
{
    internal partial class CreateUser : CustomIconForm
    {
        RiseOpContext Context;
        string OpName = "";
        AccessType OpAccess = AccessType.Public;

        InvitePackage Invite;


        internal CreateUser(RiseOpContext context, string opName, AccessType opAccess)
        {
            InitializeComponent();

            Context = context;
            OpName = opName;
            OpAccess = opAccess;

            Text = OpName + ": Create User";

            BrowseLink.Text = (context.Sim == null) ? Application.StartupPath : context.Sim.Internet.LoadedPath;
        }

        internal CreateUser(RiseOpContext context, InvitePackage invite)
        {
            InitializeComponent();

            Context = context;
            Invite = invite;
            OpName = invite.Info.OpName;
            OpAccess = invite.Info.OpAccess;

            Text = OpName + ": Create User";

            BrowseLink.Text = (context.Sim == null) ? Application.StartupPath : context.Sim.Internet.LoadedPath;
        }

        private void BrowseLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();

                dialog.SelectedPath = BrowseLink.Text;
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                    BrowseLink.Text = dialog.SelectedPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            // check input data
            try
            {
                ValidateFields();


                // check if file exists
                string filename = OpName + " - " + TextName.Text;

                string path = BrowseLink.Text + Path.DirectorySeparatorChar + 
                    filename + Path.DirectorySeparatorChar + 
                    filename + ".rop";

                Directory.CreateDirectory(BrowseLink.Text + Path.DirectorySeparatorChar + filename);

                if (File.Exists(path))
                    throw new Exception("Cannot create because " + filename + " already exists");

                byte[] opKey = Invite != null ? Invite.Info.OpID : null;
                Identity.CreateNew(path, OpName, TextName.Text, TextPassword.Text, OpAccess, opKey);

                OpCore core = new OpCore(Context, path, TextPassword.Text);

                if (Invite != null)
                    core.ProcessInvite(Invite);

                Context.ShowCore(core);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void ValidateFields()
        {
            if (TextName.Text.Length == 0)
                throw new Exception("Name Blank");

            if (TextPassword.Text.Length == 0)
                throw new Exception("Passphrase Blank");

            if (TextConfirm.Text.Length == 0)
                throw new Exception("Confirm Passphrase Blank");

            if (TextPassword.Text != TextConfirm.Text)
                throw new Exception("Passphrase Confirmation does not match");
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
