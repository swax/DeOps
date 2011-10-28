using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;

using Microsoft.Win32;

using DeOps.Implementation;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Special;

using DeOps.Interface.Startup;

using DeOps.Simulator;
using System.Web;


namespace DeOps.Interface
{
    internal partial class LoginForm : CustomIconForm
    {
        AppContext App;
        DeOpsContext Context;
        

        string LastBrowse;
        string LastOpened;
        internal string Arg = "";

        internal G2Protocol Protocol = new G2Protocol();


        internal LoginForm(AppContext app, string arg)
        {
            App = app;
            Context = app.Context;
            Arg = arg;

            InitializeComponent();

            SplashBox.Controls.Add(VersionLabel);
            VersionLabel.Left = SplashBox.Width - VersionLabel.Width;
            VersionLabel.Top = SplashBox.Height - VersionLabel.Height;
            VersionLabel.BackColor = Color.FromArgb(0, VersionLabel.BackColor);
            VersionLabel.Text = "v" + Application.ProductVersion.Substring(0, Application.ProductVersion.Length - 2);

            SplashBox.Image = InterfaceRes.splash;

            if (Context.Sim != null) // prevent sim recursion
                EnterSimLink.Visible = false;

            LastBrowse = Application.StartupPath;

            LastOpened = (arg != "") ? arg : Properties.Settings.Default.LastOpened;

            // each profile (.rop) is in its own directory
            // /root/profiledirs[]/profile.rop
            if (App.Context.Sim == null)
            {
                LoadProfiles(Application.StartupPath);

                // if started with file argument, load profiles around the same location
                if (File.Exists(arg))
                    LoadProfiles(Path.GetDirectoryName(Path.GetDirectoryName(arg)));
            }
            else
                LoadProfiles(App.Context.Sim.Internet.LoadedPath);


            OpComboItem select = null;
            if(LastOpened != null)
                foreach (OpComboItem item in OpCombo.Items)
                    if (item.Fullpath == LastOpened)
                        select = item;


            if (select != null)
                OpCombo.SelectedItem = select;

            else if (OpCombo.Items.Count > 0)
                OpCombo.SelectedIndex = 0;

            else
                CreateGlobalLink.Visible = true;

            OpCombo.Items.Add("Browse...");

            if (OpCombo.SelectedItem != null)
                TextPassword.Select();
        }

        private void LoadProfiles(string root)
        {
            foreach (string directory in Directory.GetDirectories(root))
                foreach (string file in Directory.GetFiles(directory, "*.rop"))
                {
                    foreach (OpComboItem item in OpCombo.Items)
                        if (item.Fullpath == file)
                            continue;

                    OpCombo.Items.Add(new OpComboItem(this, file));
                }
        }

        internal bool ProcessLink()
        {
            //if (SuppressProcessLink)
            //    return;

            try
            {
                string arg = Arg.TrimEnd('/'); // copy so modifications arent permanent

                CreateUser user = null;

                if (arg.Contains("/invite/"))
                    user = ReadInvite(arg);

                // public network
                else if (arg.Replace("deops://", "").IndexOf('/') == -1)
                    user = new CreateUser(App, arg, AccessType.Public);

                // show create user dialog
                if (user != null)
                {
                    user.ShowDialog(this);
                    return true;
                }
            }
            catch { }

            return false;
        }


        private void CreateLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CreateOp form = new CreateOp();

            if (form.ShowDialog(this) != DialogResult.OK)
                return;

            CreateUser create = new CreateUser(App, form.OpName, form.OpAccess);

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
                user = new CreateUser(App, "Global IM", AccessType.Secret);
                user.GlobalIM = true;
            }

            // private or secret network
            else if (join.OpLink.Contains("/invite/"))
                user = ReadInvite(join.OpLink);

            // public network
            else
                user = new CreateUser(App, join.OpLink, join.OpAccess);


            // show create user dialog
            if (user != null && user.ShowDialog(this) == DialogResult.OK)
                Close();
        }

        private CreateUser ReadInvite(string link)
        {
            string[] mainParts = link.Replace("deops://", "").Split('/');

            if (mainParts.Length < 4)
                throw new Exception("Invalid Link");

            // Select John Marshall's Global IM Profile
            string[] nameParts = mainParts[2].Split('@');
            string name = HttpUtility.UrlDecode(nameParts[0]);
            string op = HttpUtility.UrlDecode(nameParts[1]);

            byte[] data = Utilities.FromBase64String(mainParts[3]);
            byte[] pubOpID = Utilities.ExtractBytes(data, 0, 8);
            byte[] encrypted = Utilities.ExtractBytes(data, 8, data.Length - 8);
            byte[] decrypted = null;

            // try opening invite with a currently loaded core
            Context.Cores.LockReading(delegate()
            {
                foreach (var core in Context.Cores)
                    try
                    {
                        if (Utilities.MemCompare(pubOpID, core.User.Settings.PublicOpID))
                            decrypted = core.User.Settings.KeyPair.Decrypt(encrypted, false);
                    }
                    catch { }
            });

            // have user select profile associated with the invite
            while (decrypted == null)
            {
                OpenFileDialog open = new OpenFileDialog();

                open.Title = "Open " + name + "'s " + op + " Profile to Verify Invitation";
                open.InitialDirectory = Application.StartupPath;
                open.Filter = "DeOps Identity (*.dop)|*.dop";

                if (open.ShowDialog() != DialogResult.OK)
                    return null; // user doesnt want to try any more

                GetTextDialog pass = new GetTextDialog("Passphrase", "Enter the passphrase for this profile", "");
                pass.ResultBox.UseSystemPasswordChar = true;

                if (pass.ShowDialog() != DialogResult.OK)
                    continue; // let user choose another profile

                try
                {
                    // open profile
                    var user = new OpUser(open.FileName, pass.ResultBox.Text, null);
                    user.Load(LoadModeType.Settings);

                    // ensure the invitation is for this op specifically
                    if (!Utilities.MemCompare(pubOpID, user.Settings.PublicOpID))
                    {
                        MessageBox.Show("This is not a " + op + " profile");
                        continue;
                    }

                    // try to decrypt the invitation
                    try
                    {
                        decrypted = user.Settings.KeyPair.Decrypt(encrypted, false);
                    }
                    catch
                    {
                        MessageBox.Show("Could not open the invitation with this profile");
                        continue;
                    }
                }
                catch
                {
                    MessageBox.Show("Wrong password");
                }
            }

            CreateUser created = null;

            try
            {
                InvitePackage invite = OpCore.OpenInvite(decrypted, Protocol);

                if(invite != null)
                    created = new CreateUser(App, invite);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);        
            }

            return created;
        }

        private void BrowseLink_LinkClicked()
        {
            try
            {
                OpenFileDialog open = new OpenFileDialog();

                open.InitialDirectory = LastBrowse;
                open.Filter = "DeOps Identity (*.dop)|*.dop";

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
            Debug.WriteLine("GUI Thread: " + Thread.CurrentThread.ManagedThreadId);


            OpComboItem item = OpCombo.SelectedItem as OpComboItem;

            if (item == null)
                return;

            bool handled = false;

            // look for item in context cores, show mainGui, or notify user to check tray
            App.CoreUIs.LockReading(delegate()
            {
                foreach (var ui in App.CoreUIs)
                    if (ui.Core.User.ProfilePath == item.Fullpath)
                    {
                        if (ui.GuiMain != null)
                        {
                            ui.GuiMain.WindowState = FormWindowState.Normal;
                            ui.GuiMain.Activate();

                            Close(); // user thinks they logged back on, window just brought to front
                        }
                        else
                            MessageBox.Show(this, "This profile is already loaded, check the system tray", "DeOps");

                        handled = true;
                    }
            });

            if (handled)
                return;

            OpCore newCore = null;

            try
            {
                newCore = new OpCore(Context, item.Fullpath, TextPassword.Text);

                App.ShowCore(newCore);

                Properties.Settings.Default.LastOpened = item.Fullpath;

                Close();
            }
            catch(Exception ex)
            {
                // if interface threw exception, remove the added core
                if (newCore != null)
                    App.RemoveCore(newCore);

                if (ex is System.Security.Cryptography.CryptographicException)
                    MessageBox.Show(this, "Wrong Passphrase");

                else
                {
                    //crit - delete
                    using (StreamWriter log = File.CreateText("debug.txt"))
                    {
                        log.Write(ex.Message);
                        log.WriteLine("");
                        log.Write(ex.StackTrace); 
                    }

                    new ErrorReport(ex).ShowDialog();
                }
            }
        }

        private void EnterSimLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            App.ShowSimulator();
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
#if DEBUG
            if (Context.Sim == null)
                EnterSimLink.Visible = !EnterSimLink.Visible;
#endif
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            if (App.License != null)
            {
                LicenseLabel.Text = "supported - licensed";
                LicenseLabel.BackColor = Color.Blue;
            }
        }

        private void LicenseLabel_Click(object sender, EventArgs e)
        {
            if (App.License == null)
                Process.Start("http://www.c0re.net/deops/download");

            else
                new LicenseForm(App.License).ShowDialog(this);
        }

        private void CreateGlobalLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CreateUser form = new CreateUser(App, "Global IM", AccessType.Secret);
            form.GlobalIM = true;

            if (form.ShowDialog(this) == DialogResult.OK)
                Close();
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