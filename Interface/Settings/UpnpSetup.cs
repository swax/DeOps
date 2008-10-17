using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;


namespace RiseOp.Interface.Settings
{
    internal partial class UpnpSetup : CustomIconForm
    {
        OpCore Core;


        internal UpnpSetup(OpCore core)
        {
            InitializeComponent();

            Core = core;
        }

        private void RefreshLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void RemoveLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void AddRiseOpLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void LogLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
