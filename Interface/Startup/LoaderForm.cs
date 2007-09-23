using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

using Microsoft.Win32;

using DeOps.Implementation;
using DeOps.Simulator;


namespace DeOps.Interface
{
    internal partial class LoaderForm : Form
    {
        OpCore Core;

        internal LoaderForm(string[] args)
        {
            InitializeComponent();

            RegisterType();

            // if launched from .dop file
            if (args.Length > 0 && args[0].IndexOf(".dop") != -1)
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
                root.SetValue("", "De-Ops Identity");

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
                OpenProfile.Filter = "De-Ops Identity (*.dop)|*.dop";

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
                MessageBox.Show(this, "Please Enter a Password");
                return;
            }

            Login();
        }

        void Login()
        {
            try
            {
                Core = new OpCore(this, LinkIdentity.Text, TextPassword.Text);

                Hide();

                Core.GuiMain = new MainForm(Core);
                Core.GuiMain.Show();
            }
            catch
            {
                //MessageBox.Show(ex.ToString());
                //UpdateLog("Exception", "Login: " + ex.ToString());
                MessageBox.Show(this, "Wrong Password");
            }

            TextPassword.Clear();
        }

        private void TimerMain_Tick(object sender, EventArgs e)
        {
            if (Core != null)
                Core.SecondTimer();
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
                LinkIdentity.Text = form.IdentityPath;
        }

        private void LinkSearch_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void EnterSimLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Hide();
            new SimForm().ShowDialog();
        }
    }
}