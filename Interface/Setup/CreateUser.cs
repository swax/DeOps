using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Implementation.Protocol.Special;


namespace DeOps.Interface.Startup
{
    internal partial class CreateUser : CustomIconForm
    {
        AppContext App;
        DeOpsContext Context;
        string OpName = "";
        AccessType OpAccess = AccessType.Public;

        InvitePackage Invite;
        internal bool GlobalIM;

        internal CreateUser(AppContext app, string opName, AccessType opAccess)
        {
            InitializeComponent();

            App = app;
            Context = app.Context;
            OpName = opName.Replace("deops://", "");
            OpAccess = opAccess;

            OpNameLabel.Text = OpName;

            BrowseLink.Text = (Context.Sim == null) ? Application.StartupPath : Context.Sim.Internet.LoadedPath;
        }

        internal CreateUser(AppContext app, InvitePackage invite)
        {
            InitializeComponent();

            App = app;
            Context = app.Context;
            Invite = invite;
            OpName = invite.Info.OpName;
            OpAccess = invite.Info.OpAccess;

            OpNameLabel.Text = OpName;
            TextName.Text = invite.Info.UserName;

            BrowseLink.Text = (Context.Sim == null) ? Application.StartupPath : Context.Sim.Internet.LoadedPath;
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
                {
                    // A profile for test - swax already exists, overwrite? Yes / No / Cancel
                    DialogResult result = MessageBox.Show("A profile for " + filename + " already exists. Overwrite?", "Overwrite?", MessageBoxButtons.YesNoCancel);

                    if (result == DialogResult.Cancel)
                        return;
                    
                    if (result == DialogResult.No)
                    {
                        Close();
                        return;
                    }

                    // All data for test - swax will be lost OK/Cancel
                    result = MessageBox.Show("Are you sure? All previous data will be lost for " + filename, "Overwrite?", MessageBoxButtons.YesNo);

                    if (result == DialogResult.No)
                        return;

                    Directory.Delete(Path.GetDirectoryName(path), true);
                }
                
                byte[] opKey = null;

                if (Invite != null)
                    opKey = Invite.Info.OpID;

                OpUser.CreateNew(path, OpName, TextName.Text, TextPassword.Text, OpAccess, opKey, GlobalIM);

                OpCore core = new OpCore(Context, path, TextPassword.Text);

                if (Invite != null)
                    core.ProcessInvite(Invite);

                App.ShowCore(core);

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
