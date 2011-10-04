using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Implementation.Transport;

namespace DeOps.Interface.Settings
{
    internal partial class UpnpSetup : CustomIconForm
    {
        OpCore Core;
        internal UPnPHandler UPnP;

        internal UpnpLog Log;


        internal UpnpSetup(OpCore core)
        {
            InitializeComponent();

            Core = core;
            UPnP = core.Network.UPnPControl;

            UPnP.Logging = true;
            UPnP.Log.SafeClear();

            RefreshInterface();
        }

        private void RefreshLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            RefreshLink.Text = "Refreshing...";

            EntryList.Items.Clear();

            // add ports in seperate thread
            UPnP.ActionQueue.Enqueue(() =>
            {
                string type = IPradio.Checked ? "WANIP" : "WANPPP";

                UPnP.RefreshDevices();

                foreach (UPnPDevice device in UPnP.Devices.Where(d => d.Name.Contains(type)))
                    for (int i = 0; i < 250; i++)
                    {
                        PortEntry entry = UPnP.GetPortEntry(device, i);

                        if (entry == null)
                            break;
                        else
                            // add to list box
                            BeginInvoke(new Action(() => EntryList.Items.Add(entry)));
                    }

                // finish
                BeginInvoke(new Action(() => RefreshLink.Text = "Refresh"));
            });

            RefreshInterface();
        }

        private void RemoveLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            RemoveLink.Text = "Removing...";

            List<PortEntry> remove = new List<PortEntry>();

            foreach (PortEntry entry in EntryList.SelectedItems)
                remove.Add(entry);


            UPnP.ActionQueue.Enqueue(() =>
            {
                foreach (PortEntry entry in remove)
                {
                    UPnP.ClosePort(entry.Device, entry.Protocol, entry.Port);

                    BeginInvoke(new Action(() => EntryList.Items.Remove(entry)));
                }

                // finish
                BeginInvoke(new Action(() => RemoveLink.Text = "Remove Selected"));
            });

            RefreshInterface();
        }

        private void AddDeOpsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            AddDeOpsLink.Text = "Resetting...";

            UPnP.Initialize();

            if (Core.Context.Lookup != null)
                Core.Context.Lookup.Network.UPnPControl.Initialize();

            UPnP.ActionQueue.Enqueue(() => BeginInvoke(new Action(() => AddDeOpsLink.Text = "Reset DeOps Ports")));

            RefreshInterface();
        }

        private void LogLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (Log == null)
            {
                Log = new UpnpLog(this);
                Log.Show();
            }
            else
            {
                Log.WindowState = FormWindowState.Normal;
                Log.Activate();
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            UPnP.Logging = false;

            Close();
        }

        private void ActionTimer_Tick(object sender, EventArgs e)
        {
            RefreshInterface();
        }

        private void RefreshInterface()
        {
            bool active = (UPnP.WorkingThread != null);

            ActionLabel.Visible = active;

            RefreshLink.Enabled = !active;
            RemoveLink.Enabled = !active;
            AddDeOpsLink.Enabled = !active;
            IPradio.Enabled = !active;
            PPPradio.Enabled = !active;
        }
    }
}
