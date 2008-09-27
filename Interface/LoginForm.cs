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
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Special;

using RiseOp.Interface.Startup;

using RiseOp.Simulator;


namespace RiseOp.Interface
{
    internal partial class LoginForm : CustomIconForm
    {
        RiseOpContext Context;

        internal string Arg = "";
        string LastBrowse;
        bool SuppressProcessLink;

        internal G2Protocol Protocol = new G2Protocol();


        internal LoginForm(RiseOpContext context, string arg)
        {
            Context = context;
            Arg = arg;

            InitializeComponent();

            SplashBox.Image = InterfaceRes.splash;

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
                    OpComboItem item = new OpComboItem(this, file);

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

            OpCombo.Items.Add("Browse...");

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

        private void JoinLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        { 
            JoinOp join = new JoinOp();

            if (join.ShowDialog(this) != DialogResult.OK)
                return;

            CreateUser user = null;

            if (join.GlobalIM)
            {
                user = new CreateUser(Context, "Global IM", AccessType.Secret);
                user.GlobalIM = true;
            }

            // private or secret network
            else if (join.OpName.StartsWith(@"invite/"))
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

                InvitePackage invite = null;

                try
                {
                    invite = OpCore.OpenInvite(Protocol, link, getPassword.ResultBox.Text);
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

        private void BrowseLink_LinkClicked()
        {
            try
            {
                OpenFileDialog open = new OpenFileDialog();

                open.InitialDirectory = LastBrowse;
                open.Filter = "RiseOp Identity (*.rop)|*.rop";

                if (open.ShowDialog() == DialogResult.OK)
                {
                    OpComboItem select = null;

                    foreach(object item in OpCombo.Items)
                        if(item is OpComboItem)
                            if (((OpComboItem)item).Fullpath == open.FileName)
                            {
                                select = item as OpComboItem;
                                break;
                            }

                    if (select == null)
                    {
                        select = new OpComboItem(this, open.FileName);
                        OpCombo.Items.Insert(0, select);
                    }

                    OpCombo.SelectedItem = select;

                    LastBrowse = open.FileName;

                    TextPassword.Text = "";
                    TextPassword.Select();
                }

                else
                    OpCombo.Text = "";
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
                        if (core.User.ProfilePath == item.Fullpath)
                        {
                            if (core.GuiMain != null)
                            {
                                core.GuiMain.WindowState = FormWindowState.Normal;
                                core.GuiMain.Activate();

                                Close(); // user thinks they logged back on, window just brought to front
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

            if (OpCombo.SelectedItem is OpComboItem)
            {
                OpComboItem item = OpCombo.SelectedItem as OpComboItem;

                item.UpdateSplash();

                TextPassword.Enabled = File.Exists(item.Fullpath);

                TextPassword.Text = "";
                TextPassword.Select();

                CheckLoginButton();

                return;
            }
            else
                TextPassword.Enabled = ButtonLoad.Enabled = false;


            if (OpCombo.SelectedItem is string)
            {
                SplashBox.Image = InterfaceRes.splash;

                BrowseLink_LinkClicked();
            }
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

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            if (Context.Sim == null)
                EnterSimLink.Visible = !EnterSimLink.Visible;
        }
    }

    internal class OpComboItem
    {
        LoginForm Login;

        internal string Name;
        internal string Fullpath;

        internal bool TriedSplash;
        internal Bitmap Splash;
        

        internal OpComboItem(LoginForm login, string path)
        {
            Login = login;
            Fullpath = path;

            Name = Path.GetFileNameWithoutExtension(path);
        }

        public override string ToString()
        {
            return Name;
        }

        internal void UpdateSplash()
        {
            if(Splash != null)
            {
                Login.SplashBox.Image = Splash;
                return;
            }

            Login.SplashBox.Image = InterfaceRes.splash; // set default

            if(TriedSplash) 
               return;

            TriedSplash = true;

            // open file
            if (!File.Exists(Fullpath))
                return;

            // read image
            try
            {
                using (TaggedStream file = new TaggedStream(Fullpath, Login.Protocol, new ProcessTagsHandler(ProcessSplash)))
                { }
            }
            catch { }
        }

        void ProcessSplash(PacketStream stream)
        {
            G2Header root = null;
            if (stream.ReadPacket(ref root))
                if (root.Name == IdentityPacket.Splash)
                {
                    LargeDataPacket start = LargeDataPacket.Decode(root);
                    if (start.Size > 0)
                    {
                        byte[] data = LargeDataPacket.Read(start, stream, IdentityPacket.Splash);
                        Splash = (Bitmap)Bitmap.FromStream(new MemoryStream(data));
                        Login.SplashBox.Image = Splash;
                    }
                }
        }
    }
}