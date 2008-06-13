using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

using Microsoft.Win32;

using RiseOp.Implementation;
using RiseOp.Implementation.Protocol.Special;

using RiseOp.Interface.Startup;
using RiseOp.Simulator;


namespace RiseOp.Interface
{
    internal partial class LoginForm : Form
    {
        RiseOpContext Context;

        string LastBrowse;


        internal LoginForm(RiseOpContext context, string[] args)
        {
            Context = context;

            InitializeComponent();

            if (Context.Sim != null) // prevent sim recursion
                EnterSimLink.Visible = false;

            LastBrowse = (context.Sim == null) ? Application.StartupPath : context.Sim.Internet.LoadedPath;

            foreach (string directory in Directory.GetDirectories(LastBrowse))
                foreach (string file in Directory.GetFiles(directory, "*.rop"))
                    OpCombo.Items.Add(new OpComboItem(file));

            if(OpCombo.Items.Count > 0)
                OpCombo.SelectedIndex = 0;


            // if launched from .rop file
            if (args != null && args.Length > 0 && args[0].IndexOf(".rop") != -1)
            {
                /*if (IsInvite(args[0]))
                {
                    CreateOp form = new CreateOp(args[0]);
                    if (form.ShowDialog(this) == DialogResult.OK)
                        LinkIdentity.Text = form.IdentityPath;

                    return;
                }

                BrowseLink.Text = args[0];
                TextPassword.Focus();*/
            }
        }


        private void CreateLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CreateOp form = new CreateOp();

            if (form.ShowDialog(this) != DialogResult.OK)
                return;

            CreateUser create = new CreateUser(Context, form.OpName, form.OpAccess);

            if (create.ShowDialog(this) == DialogResult.OK)
                Close();
        }

        private void ShowCreateOp()
        {
            throw new NotImplementedException();
        }

        private void JoinLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        { 
            JoinOp join = new JoinOp();

            if (join.ShowDialog(this) != DialogResult.OK)
                return;


            CreateUser user = null;

            // private or secret network
            if(join.OpName.StartsWith(@"invite/"))
            {
                string link = join.OpName.Substring(7);
                bool passed = false;

                // loop get password dialog until it works
                while(!passed)
                {
                    GetTextDialog getPassword = new GetTextDialog("Invite Link", "Enter the password for this invite link", "");
                    getPassword.ResultBox.PasswordChar = '•';

                    if(getPassword.ShowDialog() != DialogResult.OK)
                        return;

                    OneWayInvite invite = null;

                    try
                    {
                        invite = OpCore.OpenInvite(link, getPassword.ResultBox.Text);
                    }
                    catch {}

                    if (invite == null)
                        MessageBox.Show("Wrong password");
                    else
                    {
                        user = new CreateUser(Context, invite);
                        passed = true;
                    }
                }
            }

            // public network
            else
                user = new CreateUser(Context, join.OpName, join.OpAccess);


            // show create user dialog
            if (user.ShowDialog(this) == DialogResult.OK)
                Close();
        }

        private void BrowseLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                OpenFileDialog open = new OpenFileDialog();

                open.InitialDirectory = LastBrowse;
                open.Filter = "RiseOp Identity (*.rop)|*.rop";

                if (open.ShowDialog() == DialogResult.OK)
                {
                    OpComboItem item = new OpComboItem(open.FileName);
                    OpCombo.Items.Add(item);
                    OpCombo.SelectedItem = item;

                    LastBrowse = open.FileName;

                    TextPassword.Focus();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void ButtonExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ButtonLoad_Click(object sender, EventArgs e)
        {
            try
            {
                OpComboItem item = OpCombo.SelectedItem as OpComboItem;

                if (item == null)
                    return;

                OpCore core = new OpCore(Context, item.Fullpath, TextPassword.Text);

                Context.ShowCore(core);

                Close();
            }
            catch
            {
                //MessageBox.Show(ex.ToString());
                //UpdateLog("Exception", "Login: " + ex.ToString());
                MessageBox.Show(this, "Wrong Passphrase");
            }
        }

        private void EnterSimLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Context.ShowSimulator();
            Close();
        }

        private void OpCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            OpComboItem item = OpCombo.SelectedItem as OpComboItem;

            if (item == null)
                TextPassword.Enabled = ButtonLoad.Enabled = false;

            TextPassword.Enabled = File.Exists(item.Fullpath);

            TextPassword.Focus();

            CheckLoginButton();
        }

        private void TextPassword_TextChanged(object sender, EventArgs e)
        {
            CheckLoginButton();
        }

        private void CheckLoginButton()
        {
            if (TextPassword.Enabled && TextPassword.Text.Length > 0)
                ButtonLoad.Enabled = true;
            else
                ButtonLoad.Enabled = false;
        }
    }

    internal class OpComboItem
    {
        internal string Name;
        internal string Fullpath;


        internal OpComboItem(string path)
        {
            Fullpath = path;

            Name = Path.GetFileNameWithoutExtension(path);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}