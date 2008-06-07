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
using RiseOp.Simulator;


namespace RiseOp.Interface
{
    internal partial class LoginForm : Form
    {
        RiseOpContext Context;


        internal LoginForm(RiseOpContext context, string[] args)
        {
            Context = context;

            InitializeComponent();

            RegisterType();

            if (Context.Sim != null) // prevent sim recursion
                EnterSimLink.Visible = false;    

            // if launched from .dop file
            if (args != null && args.Length > 0 && args[0].IndexOf(".dop") != -1)
            {
                if (IsInvite(args[0]))
                {
                    NewOpForm form = new NewOpForm(args[0]);
                    if (form.ShowDialog(this) == DialogResult.OK)
                        LinkIdentity.Text = form.IdentityPath;

                    return;
                }

                LinkIdentity.Text = args[0];
                TextPassword.Focus();
            }
        }

        void RegisterType()
        {
            // try to register file type association
            try
            {
                RegistryKey type = Registry.ClassesRoot.CreateSubKey(".dop");
                type.SetValue("", "dop");

                RegistryKey root = Registry.ClassesRoot.CreateSubKey("dop");
                root.SetValue("", "RiseOp Identity");

                RegistryKey icon = root.CreateSubKey("DefaultIcon");
                icon.SetValue("", Application.ExecutablePath + ",0");

                RegistryKey shell   = root.CreateSubKey("shell");
                RegistryKey open    = shell.CreateSubKey("open");
                RegistryKey command = open.CreateSubKey("command");
                command.SetValue("", "\"" + Application.ExecutablePath + "\" \"%1\"");
            }
            catch
            {
                //UpdateLog("Exception", "LoginForm::RegisterType: " + ex.Message);
            }
        }

        private void LinkIdentity_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                OpenFileDialog OpenProfile = new OpenFileDialog();

                OpenProfile.InitialDirectory = LinkIdentity.Text;
                OpenProfile.Filter = "RiseOp Identity (*.dop)|*.dop";

                if (OpenProfile.ShowDialog() == DialogResult.OK)
                {
                    if (IsInvite(OpenProfile.FileName))
                    {
                        NewOpForm form = new NewOpForm(OpenProfile.FileName);
                        if (form.ShowDialog(this) == DialogResult.OK)
                            LinkIdentity.Text = form.IdentityPath;

                        return;
                    }

                    LinkIdentity.Text = OpenProfile.FileName;

                    TextPassword.Enabled = true;
                    ButtonLoad.Enabled = true;

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
            if( !File.Exists(LinkIdentity.Text) )
            {
                MessageBox.Show(this, "Identity File Not Found");
                return;
            }
            
            if (TextPassword.Text.Length == 0)
            {
                MessageBox.Show(this, "Please Enter a Passphrase");
                return;
            }

            Login();
        }

        void Login()
        {
            try
            {
                OpCore core = new OpCore(Context, LinkIdentity.Text, TextPassword.Text);

                Context.ShowCore(core);

                Close();
            }
            catch
            {
                //MessageBox.Show(ex.ToString());
                //UpdateLog("Exception", "Login: " + ex.ToString());
                MessageBox.Show(this, "Wrong Passphrase");
            }

            TextPassword.Clear();
        }

        private bool IsInvite(string path)
        {
            try
            {
                FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                StreamReader readStream = new StreamReader(file);

                string line = readStream.ReadLine();

                if (line.StartsWith("<invite>"))
                    return true;
            }
            catch
            {
            }

            return false;
        }

        private void LinkNew_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            NewOpForm form = new NewOpForm();

            if (form.ShowDialog(this) == DialogResult.OK)
            {
                LinkIdentity.Text = form.IdentityPath;

                TextPassword.Enabled = true;
                ButtonLoad.Enabled = true;

                TextPassword.Focus();
            }
        }

        private void LinkSearch_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void EnterSimLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Context.ShowSimulator();
            Close();
        }
    }
}