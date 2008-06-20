using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
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

        internal string Arg = "";
        string LastBrowse;
        bool SuppressProcessLink;

        internal LoginForm(RiseOpContext context, string arg)
        {
            Context = context;
            Arg = arg;

            InitializeComponent();

            if (Context.Sim != null) // prevent sim recursion
                EnterSimLink.Visible = false;

            // each profile (.rop) is in its own directory
            // /root/profiledirs[]/profile.rop
            LastBrowse = (context.Sim == null) ? Application.StartupPath : context.Sim.Internet.LoadedPath;

            // if started with file argument, load profiles around the same location
            if (File.Exists(arg))
                LastBrowse = Path.GetDirectoryName(Path.GetDirectoryName(arg));

            // if started wtih url argument, select an already created user by default
            string publicNet = null;
            if (arg.StartsWith(@"riseop://") && !arg.StartsWith(@"riseop://invite/"))
                publicNet = arg.Substring(9).TrimEnd('/');

            // load combo box
            OpComboItem select = null;

            foreach (string directory in Directory.GetDirectories(LastBrowse))
                foreach (string file in Directory.GetFiles(directory, "*.rop"))
                {
                    OpComboItem item = new OpComboItem(file);

                    if (file == arg)
                        select = item;

                    if (publicNet != null && Path.GetFileName(file).Contains(publicNet))
                    {
                        select = item;
                        SuppressProcessLink = true; // found an existing profile, dont need to bother user to create a new one
                    }

                    OpCombo.Items.Add(item);
                }

            if (select != null)
                OpCombo.SelectedItem = select;

            else if(OpCombo.Items.Count > 0)
                OpCombo.SelectedIndex = 0;

            if(OpCombo.SelectedItem != null)
                TextPassword.Select();
        }

        internal void ProcessLink()
        {
            if (SuppressProcessLink)
                return;

            string arg = Arg.TrimEnd('/'); // copy so modifications arent permanent

            CreateUser user = null;

            // private or secret network
            if (arg.StartsWith(@"riseop://invite/"))
                user = ReadInvite(arg.Substring(16));

            // public network
            else
                user = new CreateUser(Context, arg.Substring(9), AccessType.Public);

            // show create user dialog
            user.ShowDialog(this);

            Close(); // if user doesnt choose to create link, just close app
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
                user = ReadInvite(join.OpName.Substring(7));

            // public network
            else
                user = new CreateUser(Context, join.OpName, join.OpAccess);


            // show create user dialog
            if (user != null && user.ShowDialog(this) == DialogResult.OK)
                Close();
        }

        private CreateUser ReadInvite(string link)
        {
            CreateUser user = null;
            bool passed = false;

            // loop get password dialog until it works
            while (!passed)
            {
                GetTextDialog getPassword = new GetTextDialog("Invite Link", "Enter the password for this invite link", "");
                getPassword.ResultBox.PasswordChar = '•';

                if (getPassword.ShowDialog() != DialogResult.OK)
                    return null;

                OneWayInvite invite = null;

                try
                {
                    invite = OpCore.OpenInvite(link, getPassword.ResultBox.Text);
                }
                catch { }

                if (invite == null)
                    MessageBox.Show("Wrong password");
                else
                {
                    user = new CreateUser(Context, invite);
                    passed = true;
                }
            }

            return user;
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

                    TextPassword.Select();
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

                bool unique = true;

                // look for item in context cores, show mainGui, or notify user to check tray
                Context.Cores.LockReading(delegate()
                {
                    foreach (OpCore core in Context.Cores)
                        if (core.Profile.ProfilePath == item.Fullpath)
                        {
                            if (core.GuiMain != null)
                            {
                                core.GuiMain.WindowState = FormWindowState.Normal;
                                core.GuiMain.Activate();
                            }
                            else
                                MessageBox.Show(this, "This profile is already loaded, check the system tray", "RiseOp");

                            unique = false;
                        }
                });

                if (unique)
                {
                    Context.ShowCore(new OpCore(Context, item.Fullpath, TextPassword.Text));

                    Close();
                }
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

            TextPassword.Select();

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