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
    internal partial class User : CustomIconForm
    {
        Identity Profile;

        internal User(MainForm parent)
        {
            InitializeComponent();

            Profile = parent.Core.Profile;

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

                Profile.Save();
                Profile.Core.Trust.SaveLocal();
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
