using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;


namespace DeOps.Interface.Settings
{
    public partial class MyAddress : CustomIconForm
    {
        OpCore Core;

        public MyAddress(OpCore core)
            : base(core)
        {
            InitializeComponent();

            Core = core;

            OrgLabel.Text = Core.User.Settings.Operation;

            OrgAddressBox.Text = Core.GetMyAddress();

            if (Core.Context.Lookup != null)
                LookupAddressBox.Text = Core.Context.Lookup.GetMyAddress();
            else
                LookupAddressBox.Text = "Not used";
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void CopyOrgLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Clipboard.SetText(OrgAddressBox.Text);
        }

        private void CopyLookupLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Clipboard.SetText(LookupAddressBox.Text);
        }
    }
}
