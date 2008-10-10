using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Interface;

namespace RiseOp.Services.Sharing
{
    internal partial class SendFileForm : CustomIconForm
    {
        OpCore Core;
        ulong User;
        ushort Client;

        SharingService Sharing;


        internal SendFileForm(OpCore core, ulong user, ushort client)
        {
            InitializeComponent();

            Core = core;
            User = user;
            Client = client;

            Sharing = Core.GetService(ServiceIDs.Sharing) as SharingService;

            Sharing.ShareList.LockReading(() =>
            {
                foreach (OpShare share in Sharing.ShareList)
                    if(share.Hash != null) // processed
                        RecentCombo.Items.Add(share);
            });

            if (RecentCombo.Items.Count > 0)
                RecentCombo.SelectedIndex = 0;
        }

        private void BrowseLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Title = "Select File to Send";
            open.Filter = "All files (*.*)|*.*";

            if (open.ShowDialog() != DialogResult.OK)
                return;

            BrowseLink.Text = open.FileName;
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            if (BrowseLink.Enabled)
            {
                if (File.Exists(BrowseLink.Text))
                    Sharing.SendFile(BrowseLink.Text, User, Client);

                else
                {
                    MessageBox.Show("No File Selected");
                    return;
                }
            }

            else if (RecentRadio.Checked)
            {
                OpShare share = RecentCombo.SelectedItem as OpShare;

                if (share != null)
                {
                    Core.RunInCoreAsync(() =>
                    {
                        Sharing.AddTargets(share, User, Client);

                        foreach (DhtClient target in share.ToRequest.Where(t => t.UserID == User))
                            Sharing.TrySendRequest(share, target);
                    });
                }
                else
                {
                    MessageBox.Show("No File Selected");
                    return;
                }
            }

            // show the user the transfer starting
            SharingView view = new SharingView(Core);

            if (Core.GuiMain is MainForm)
            {
                if (((MainForm)Core.GuiMain).SideMode)
                    Core.ShowExternal(view);
                else
                    Core.ShowInternal(view);
            }
            else
                Core.ShowExternal(view);


            DialogResult = DialogResult.OK;
            Close();
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void RecentRadio_CheckedChanged(object sender, EventArgs e)
        {
            BrowseLink.Enabled = !RecentRadio.Checked;
            RecentCombo.Enabled = RecentRadio.Checked;
        }

        private void BrowseRadio_CheckedChanged(object sender, EventArgs e)
        {
            BrowseLink.Enabled = BrowseRadio.Checked;
            RecentCombo.Enabled = !BrowseRadio.Checked;
        }
    }
}
