using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;


namespace DeOps.Interface.Settings
{
    internal partial class User : CustomIconForm
    {
        OpUser Profile;

        internal User(OpCore core)
            : base(core)
        {
            InitializeComponent();

            Profile = core.User;

            NameBox.Text = Profile.Settings.UserName;
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            // check for errors
            string error = ValidateFields();

            if (error != null)
            {
                MessageBox.Show(this, error, "User Settings");
                return;
            }

            bool verify = false;

            // set user name
            if (Profile.Settings.UserName != NameBox.Text)
                verify = true;

            // set password
            if (NewPassBox.Text != "")
                verify = true;

            // save
            if (verify && Utilities.VerifyPassphrase(Profile.Core, ThreatLevel.High))
            {
                Profile.Settings.UserName = NameBox.Text;

                if (NewPassBox.Text != "")
                    Profile.SetNewPassword(NewPassBox.Text);

                Profile.Core.RunInCoreAsync(delegate()
                {
                    Profile.Save();
                    Profile.Core.Trust.SaveLocal();
                });
            }


            Close();
        }

        private string ValidateFields()
        {
            if (NameBox.Text == "")
                return "Name cannont be blank";

            if (NewPassBox.Text != "")
                if (NewPassBox.Text != ConfirmPassBox.Text)
                    return "New passphrase does not match confirmation";

            return null;
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
