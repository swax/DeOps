using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Xml;

using DeOps.Implementation.Protocol;

namespace DeOps.Interface
{
    internal partial class NewOpForm : Form
    {
        internal string IdentityPath = "";

        RijndaelManaged OpKey = new RijndaelManaged();
        RijndaelManaged FileKey = new RijndaelManaged();

        internal NewOpForm()
        {
            InitializeComponent();

            OpKey.GenerateKey();
            FileKey.GenerateKey();
        }

        internal NewOpForm(string path)
        {
            InitializeComponent();

            try
            {
                FileStream readStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                XmlTextReader xmlReader = new XmlTextReader(readStream);
                xmlReader.WhitespaceHandling = WhitespaceHandling.None;

                while (xmlReader.Read())
                    if (xmlReader.NodeType == XmlNodeType.Element)
                    {
                        if (xmlReader.Name == "Operation" && xmlReader.Read())
                            TextOperation.Text = xmlReader.Value;

                        if (xmlReader.Name == "OpKey" && xmlReader.Read())
                        {
                            string[] opValue = xmlReader.Value.Split(new char[] { '/' });

                            if (opValue[0] != "aes 256")
                                throw new Exception("Unsupported Op Encryption");

                            OpKey = new RijndaelManaged();
                            OpKey.GenerateKey();
                            OpKey.Key = Utilities.HextoBytes(opValue[1]);
                        }

                        else if (xmlReader.Name == "OpAccess" && xmlReader.Read())
                        {
                            AccessType access = (AccessType)Enum.Parse(typeof(AccessType), xmlReader.Value);

                            switch (access)
                            {
                                case AccessType.Public:
                                    RadioPublic.Checked = true;
                                    break;
                                case AccessType.Private:
                                    RadioPrivate.Checked = true;
                                    break;
                                case AccessType.Secret:
                                    RadioSecret.Checked = true;
                                    break;
                            }
                        }

                    }

                TextOperation.Enabled = false;
                RadioPublic.Enabled = false;
                RadioPrivate.Enabled = false;
                RadioSecret.Enabled = false;

            }
            catch
            {
                MessageBox.Show(this, "Error Loading Invite File");
                Close();
            }
        }

        private void NewOps_Load(object sender, EventArgs e)
        {
            LinkLocation.Text = Application.StartupPath;
        }

        private void LinkStore_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                FolderBrowserDialog browse = new FolderBrowserDialog();

                browse.ShowNewFolderButton = true;
                browse.SelectedPath = LinkLocation.Text;

                if (browse.ShowDialog(this) == DialogResult.OK)
                    LinkLocation.Text = browse.SelectedPath;
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
            // check input data
            if (!ValidateFields())
                return;

            // check if file exists
            string filename = TextOperation.Text + "-" + TextName.Text;
            string path = LinkLocation.Text + Path.DirectorySeparatorChar + filename + Path.DirectorySeparatorChar + filename + ".dop";
            Directory.CreateDirectory(LinkLocation.Text + Path.DirectorySeparatorChar + filename);

            if (File.Exists(path))
            {
                MessageBox.Show(this, "Cannot create because file " + filename + " already exists");
                return;
            }
            
            // create new operation / identity
            Identity user = new Identity(path, TextPassword.Text, new G2Protocol());
            user.Settings.Operation = TextOperation.Text;
            user.Settings.ScreenName = TextName.Text;
            user.Settings.KeyPair = new RSACryptoServiceProvider(1024);
            user.Settings.OpKey = OpKey;
            user.Settings.FileKey = FileKey;

            if (RadioPublic.Checked) user.Settings.OpAccess = AccessType.Public;
            if (RadioPrivate.Checked) user.Settings.OpAccess = AccessType.Private;
            if (RadioSecret.Checked) user.Settings.OpAccess = AccessType.Secret;

            user.Save();

            IdentityPath = path;

            DialogResult = DialogResult.OK;
            Close();
        }

        private bool ValidateFields()
        {
            try
            {
                if (TextOperation.Text.Length == 0)
                    throw new Exception("Operation Name Blank");

                if( !RadioPublic.Checked && !RadioPrivate.Checked && !RadioSecret.Checked)
                    throw new Exception("Access Type not specified");
                
                if ( TextName.Text.Length == 0)
                    throw new Exception("Display Name Blank");

                if (TextPassword.Text.Length == 0)
                    throw new Exception("Password Blank");

                if (TextConfirm.Text.Length == 0)
                    throw new Exception("Confirm Password Blank");

                if(TextPassword.Text != TextConfirm.Text)
                    throw new Exception("Password Confirmation does not match");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                return false;
            }

            return true;
        }

        
    }
}