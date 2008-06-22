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

using RiseOp.Implementation.Protocol;

namespace RiseOp.Interface
{
    internal partial class CreateOp : CustomIconForm
    {
        internal string OpName = "";
        internal AccessType OpAccess = AccessType.Public;


        internal CreateOp()
        {
            InitializeComponent();
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            // check input data
            try
            {
                ValidateFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                return;
            }

            OpName = OpNameBox.Text;

            if (RadioPublic.Checked)  OpAccess = AccessType.Public;
            if (RadioPrivate.Checked) OpAccess = AccessType.Private;
            if (RadioSecret.Checked)  OpAccess = AccessType.Secret;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void ValidateFields()
        {
            if (OpNameBox.Text.Length == 0)
                throw new Exception("Operation Name Blank");

            if (!RadioPublic.Checked && !RadioPrivate.Checked && !RadioSecret.Checked)
                throw new Exception("Access Type not specified");
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}