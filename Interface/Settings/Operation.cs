using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;


namespace RiseOp.Interface.Settings
{
    internal partial class Operation : CustomIconForm
    {
        OpUser Profile;

        Bitmap SelectedIcon;
        Bitmap SelectedSplash;


        internal Operation(OpCore core) : base(core)
        {
            InitializeComponent();

            Profile = core.User;

            OperationBox.Text = Profile.Settings.Operation;

            if (Profile.Settings.OpAccess == AccessType.Public)
                OperationBox.Enabled = false;
        }

        private void Operation_Load(object sender, EventArgs e)
        {
            SetIcon(Profile.OpIcon);

            SetSplash(Profile.OpSplash);
        }

        private void ChangeIconLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();

            if (open.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                SetIcon(Image.FromFile(open.FileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void DefaultIconLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            SetIcon(null);
        }

        private void SetIcon(Image image)
        {
            Bitmap result = new Bitmap(16, 16);

            if (image == null)
            {
                SelectedIcon = null;

                using (Graphics g = Graphics.FromImage((Image)result))
                    g.DrawImage(InterfaceRes.riseop.ToBitmap(), 0, 0, 16, 16);

                IconPicture.Image = result;
            }
            else
            {
                using (Graphics g = Graphics.FromImage((Image)result))
                    g.DrawImage(image, 0, 0, 16, 16);

                SelectedIcon = result;
                IconPicture.Image = result;
            }
        }

        private void ChangeSplashLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();

            if (open.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                SetSplash(Image.FromFile(open.FileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DefaultSplashLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            SetSplash(null);
        }

        private void SetSplash(Image image)
        {
            if (image == null)
            {
                SelectedSplash = null;
                SplashPicture.Image = InterfaceRes.splash;
                return;
            }

            Bitmap result = new Bitmap(240, 180);

            using (Graphics g = Graphics.FromImage((Image)result))
                g.DrawImage(image, 0, 0, 240, 180);

            SelectedSplash = result;
            SplashPicture.Image = result;
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            string error = ValidateFields();

            if (error != null)
            {
                MessageBox.Show(this, error, "User Settings");
                return;
            }

            bool verify = false;

            if (Profile.Settings.Operation != OperationBox.Text)
                verify = true;

            if (Profile.OpIcon != SelectedIcon)
                verify = true;

            if (Profile.OpSplash != SelectedSplash)
                verify = true;

            if (verify && Utilities.VerifyPassphrase(Profile.Core, ThreatLevel.Medium))
            {
                Profile.Settings.Operation = OperationBox.Text;
                Profile.OpIcon = SelectedIcon;
                Profile.OpSplash = SelectedSplash;

                Profile.Core.RunInCoreAsync(delegate()
                {
                    Profile.Save();
                    Profile.Core.Trust.SaveLocal(); // triggers icon update
                });
            }

            Close();
        }

        private string ValidateFields()
        {
            if (OperationBox.Text == "")
                return "Operation Name cannont be blank";

            return null;
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }



    }
}
