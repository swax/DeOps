using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Implementation.Protocol.Net;
using DeOps.Interface;

namespace DeOps.Services.Share
{
    internal partial class SendFileForm : CustomIconForm
    {
        CoreUI UI;
        OpCore Core;
        ulong User;


        ShareService Sharing;

        internal Tuple<FileProcessedHandler, object> FileProcessed;


        internal SendFileForm(CoreUI ui, ulong user)
        {
            InitializeComponent();

            UI = ui;
            Core = ui.Core;
            User = user;

            Sharing = Core.GetService(ServiceIDs.Share) as ShareService;

            if(user == 0)
                Text = "Send File to Room";
            else
                Text = "Send File to " + Core.GetName(user);

            Sharing.Local.Files.LockReading(() =>
            {
                foreach (SharedFile share in Sharing.Local.Files)
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
                    Sharing.SendFile(BrowseLink.Text, FileProcessed);

                else
                {
                    MessageBox.Show("No File Selected");
                    return;
                }
            }

            else if (RecentRadio.Checked)
            {
                SharedFile file = RecentCombo.SelectedItem as SharedFile;

                if (file != null)
                {
                    if (FileProcessed != null)
                        Core.RunInCoreAsync(() => FileProcessed.Param1.Invoke(file, FileProcessed.Param2));
                }
                else
                {
                    MessageBox.Show("No File Selected");
                    return;
                }
            }

            // show if processing otherwise, request immediately sent
            if(BrowseLink.Enabled)
                if (!UI.GuiMain.ShowExistingView(typeof(SharingView)))
                    UI.ShowView(new SharingView(Core, Core.UserID), true);
   

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
