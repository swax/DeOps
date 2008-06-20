using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RiseOp.Interface.Settings
{
    internal partial class Operation : Form
    {
        Identity Profile;

        internal Operation(MainForm parent)
        {
            InitializeComponent();

            Profile = parent.Core.Profile;

            OperationBox.Text = Profile.Settings.Operation;

            if (Profile.Settings.OpAccess == AccessType.Public)
                OperationBox.Enabled = false;
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

            if (verify && Utilities.VerifyPassphrase(Profile.Core, ThreatLevel.Medium))
            {
                Profile.Settings.Operation = OperationBox.Text;

                Profile.Save();
                Profile.Core.Trust.SaveLocal();
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
