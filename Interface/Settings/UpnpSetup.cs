using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Implementation.Transport;

namespace RiseOp.Interface.Settings
{
    internal partial class UpnpSetup : CustomIconForm
    {
        OpCore Core;
        UPnPHandler UPnP;

        internal UpnpSetup(OpCore core)
        {
            InitializeComponent();

            Core = core;
            UPnP = core.Network.UPnPControl;
        }

        private void RefreshLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Enabled = false;
            RefreshLink.Text = "Working...";

            EntryList.Items.Clear();

            new Thread(RefreshThread).Start();


            // keep log

            // add exceptions to log
            
        }

        void RefreshThread()
        {
            string type = IPradio.Checked ? "WANIP" : "WANPPP";

            UPnP.RefreshDevices();

            foreach(UPnPDevice device in UPnP.Devices.Where(d=>d.Name.Contains(type)))
                for (int i = 0; i < 250; i++)
                {
                    PortEntry entry = UPnP.GetPortEntry(device, i);

                    BeginInvoke((MethodInvoker)delegate() { AddPortEntry(entry); });

                    if (entry == null)
                        break;
                }

            BeginInvoke(new MethodInvoker(RefreshFinished));
        }

        void AddPortEntry(PortEntry entry)
        {
            EntryList.Items.Add(entry);
        }

        void RefreshFinished()
        {
            Enabled = true;
            RefreshLink.Text = "Refresh";
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
