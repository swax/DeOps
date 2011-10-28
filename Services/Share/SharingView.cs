using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;

using DeOps.Interface;
using DeOps.Interface.TLVex;
using DeOps.Interface.Views;

using DeOps.Services.Transfer;


namespace DeOps.Services.Share
{
    internal partial class SharingView : ViewShell
    {
        internal OpCore Core;
        ShareService Sharing;

        Dictionary<string, int> IconMap = new Dictionary<string, int>();
        List<Image> FileIcons = new List<Image>();

        internal ulong UserID;
        internal bool Local;


        internal SharingView(OpCore core, ulong user)
        {
            InitializeComponent();

            Core = core;
            UserID = user;

            Sharing = core.GetService(ServiceIDs.Share) as ShareService;

            GuiUtils.SetupToolstrip(TopStrip, new OpusColorTable());
            
            SharedFiles.SmallImageList = FileIcons;

            StatusLabel.Text = "";

            Local = (UserID == Core.UserID);

            if (Local)
            {
                SharedFiles.Columns.Add("Public", 50, HorizontalAlignment.Left, ColumnScaleStyle.Slide);
                StatusLabel.Visible = false;
            }
            else
            {
                DownloadButton.Visible = false;
                ShareButton.Visible = false;
            }
        }

        internal override void Init() 
        {
            RefreshView();

            Sharing.GuiFileUpdate += new ShareFileUpdateHandler(Sharing_FileUpdate);
            Sharing.GuiCollectionUpdate += new ShareCollectionUpdateHandler(Sharing_CollectionUpdate);
        }

        internal override bool Fin() 
        {
            Sharing.GuiFileUpdate -= new ShareFileUpdateHandler(Sharing_FileUpdate);
            Sharing.GuiCollectionUpdate -= new ShareCollectionUpdateHandler(Sharing_CollectionUpdate);

            return true; 
        }

        internal override string GetTitle(bool small) 
        {
            if (small)
                return "Share";

            string title = "";

            if (UserID == Core.UserID)
                title += "My Shared Files";
            else
                title += Core.GetName(UserID) + "'s Publicly Shared";

            return title;
        }

        internal override Size GetDefaultSize()
        {
            return new Size(600, 350);
        }

        internal override Icon GetIcon()
        {
            return Res.ShareRes.Icon;
        }

        internal int GetImageIndex(SharedFile share)
        {
            string ext = Path.GetExtension(share.Name);

            if (!IconMap.ContainsKey(ext))
            {
                IconMap[ext] = FileIcons.Count;

                Bitmap img = Win32.GetIcon(ext);


                if (img == null)
                    img = new Bitmap(16, 16);

                FileIcons.Add(img);
            }

            return IconMap[ext];
        }

        private void RefreshView()
        {
            SharedFiles.Items.Clear();

            ShareCollection collection;
            if (Sharing.Collections.SafeTryGetValue(UserID, out collection))
                collection.Files.LockReading(() =>
                {
                    foreach (SharedFile share in collection.Files)
                        SharedFiles.Items.Add(new ShareItem(this, share));
                });

            SharedFiles.Invalidate();
        }

        void Sharing_FileUpdate(SharedFile share)
        {
            // only for local user
            if (UserID != Core.UserID)
                return;

            bool deleted = false;

            Sharing.Local.Files.LockReading(() =>
            {
                deleted = !Sharing.Local.Files.Any(s => s == share);
            });

            // if share exists 
            ShareItem exists = SharedFiles.Items.Cast<ShareItem>().Where(s => s.Share == share).FirstOrDefault();

            if (exists != null)
            {
                if (deleted)
                    SharedFiles.Items.Remove(exists);
                else
                    exists.RefreshStatus();
            }
            else if( !deleted )
                SharedFiles.Items.Add(new ShareItem(this, share));
            
            SharedFiles.Invalidate();
        }

        void Sharing_CollectionUpdate(ulong user)
        {
            if (user != UserID)
                return;

            RefreshView();
        }

        private void SecondTimer_Tick(object sender, EventArgs e)
        {
            // sync items with local status, we can see our transfer in remote's view
            Sharing.Local.Files.LockReading(() =>
            {
                foreach (SharedFile file in SharedFiles.Items.Cast<ShareItem>().Select(i => i.Share))
                {
                    SharedFile localStatus = Sharing.Local.Files.Where(f => f.FileID == file.FileID).FirstOrDefault();

                    if (localStatus == null)
                        continue;

                    file.FileStatus = localStatus.FileStatus;
                    file.TransferStatus = localStatus.TransferStatus;
                    file.TransferActive = localStatus.TransferActive;
                }
            });

            foreach (ShareItem item in SharedFiles.Items)
                item.RefreshStatus();

            ShareCollection collection;
            if (Sharing.Collections.SafeTryGetValue(UserID, out collection))
                StatusLabel.Text = collection.Status;

            SharedFiles.Invalidate(); // update download progress, file colors, etc..
        }

        private void DownloadButton_Click(object sender, EventArgs e)
        {
            GetTextDialog getlink = new GetTextDialog("Download File", "Enter a File Link below", "");
            getlink.BigResultBox();

            if (getlink.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Sharing.DownloadLink(getlink.ResultBox.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void ShareButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Title = "Select File to Send";
            open.Filter = "All files (*.*)|*.*";

            if (open.ShowDialog() != DialogResult.OK)
                return;

            Sharing.LoadFile(open.FileName);
        }

        private void SharedFiles_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (SharedFiles.SelectedItems.Count == 0)
                return;

            ShareItem item = SharedFiles.SelectedItems[0] as ShareItem;

            if (File.Exists(Sharing.GetFilePath(item.Share)))
                Sharing.OpenFile(UserID, item.Share);
            else
                Sharing.DownloadFile(UserID, item.Share);
        }

        private void SharedFiles_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            if (SharedFiles.SelectedItems.Count == 0)
                return;

            ShareItem item = SharedFiles.SelectedItems[0] as ShareItem;

            if (item == null)
                return;

            ContextMenuStripEx menu = new ContextMenuStripEx();

            if (item.Share.Public)
                menu.Items.Add(new ToolStripMenuItem("Make Private", null, new EventHandler(Menu_MakePrivate)));
            else
                menu.Items.Add(new ToolStripMenuItem("Make Public", null, new EventHandler(Menu_MakePublic)));

            menu.Items.Add(new ToolStripMenuItem("Copy File Link", null, new EventHandler(Menu_CopyFileLink)));
            

            menu.Items.Add("-");


            if (File.Exists(Sharing.GetFilePath(item.Share)))
            {
                menu.Items.Add(new ToolStripMenuItem("Open", null, (s, ea) =>
                    SharedFiles.SelectedItems.ForEach(i => Sharing.OpenFile(UserID, ((ShareItem)i).Share))));
             
                if (item.Share.SystemPath != null)
                    menu.Items.Add(new ToolStripMenuItem("Open Containing Folder", null, (s, ea) =>
                        Utilities.OpenFolder(Path.GetDirectoryName(item.Share.SystemPath))));
            }
            else
                menu.Items.Add(new ToolStripMenuItem("Try Download", null, (s, ea) =>
                    SharedFiles.SelectedItems.ForEach(i => Sharing.DownloadFile(UserID, ((ShareItem)i).Share))));


            if (item.Share.TransferActive)
                menu.Items.Add(new ToolStripMenuItem("Transfer Details", null, (s, ea) =>
                    TransferView.Show(Core.Network)));


            if (Local && item.Share.ClientID == Core.Network.Local.ClientID)
            {
                menu.Items.Add(new ToolStripMenuItem("Rename", null, new EventHandler(Menu_Rename)));

                menu.Items.Add("-");
                menu.Items.Add(new ToolStripMenuItem("Remove", null, new EventHandler(Menu_Remove)));

            }

            if (menu.Items.Count > 0)
                menu.Show(SharedFiles, e.Location);
        }

        void Menu_CopyFileLink(object sender, EventArgs e)
        {
            if (SharedFiles.SelectedItems.Count == 0)
                return;

            ShareItem item = SharedFiles.SelectedItems[0] as ShareItem;

            Clipboard.SetText(Sharing.GetFileLink(UserID, item.Share));
        }

        void Menu_Remove(object sender, EventArgs e)
        {
            foreach (ShareItem item in SharedFiles.SelectedItems)
                Sharing.RemoveFile(item.Share);
        }

        void Menu_Rename(object sender, EventArgs e)
        {
            if (SharedFiles.SelectedItems.Count == 0)
                return;

            ShareItem item = SharedFiles.SelectedItems[0] as ShareItem;

            GetTextDialog rename = new GetTextDialog("Rename File", "Enter new name for " + item.Share.Name, item.Share.Name);

            if (rename.ShowDialog() == DialogResult.OK)
                if (rename.ResultBox.Text.Trim() == "" || rename.ResultBox.Text.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                    MessageBox.Show("Name Contains Invalid Characters");
                else
                    item.Share.Name = rename.ResultBox.Text;

            SharedFiles.Invalidate();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            Sharing.GetPublicList(UserID);
        }

        void Menu_MakePublic(object sender, EventArgs e)
        {
            foreach (ShareItem item in SharedFiles.SelectedItems)
            {
                item.Share.Public = true;
                item.RefreshStatus();
            }

            Sharing.RunSave = true;
        }

        void Menu_MakePrivate(object sender, EventArgs e)
        {
            foreach (ShareItem item in SharedFiles.SelectedItems)
            {
                item.Share.Public = false;
                item.RefreshStatus();
            }

            Sharing.RunSave = true;
        }

        private void SharedFiles_DragOver(object sender, DragEventArgs e)
        {
            if (UserID != Core.UserID)
                return;

            e.Effect = DragDropEffects.None;

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            e.Effect = DragDropEffects.All;
        }

        private void SharedFiles_DragDrop(object sender, DragEventArgs e)
        {
            if (UserID != Core.UserID)
                return;

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach(string path in paths)
                Sharing.LoadFile(path);
        }
    }


    internal class ShareItem : ContainerListViewItem
    {
        SharingView TheView;
        internal SharedFile Share;
        internal bool Local;

        internal ShareItem(SharingView theView, SharedFile share)
        {
            Share = share;
            TheView = theView;
            Local = theView.Local && share.ClientID == theView.Core.Network.Local.ClientID;

            ImageIndex = theView.GetImageIndex(share);

            SubItems.Add("");
            SubItems.Add("");
            SubItems.Add("");

            RefreshStatus();
        }

        internal void RefreshStatus()
        {
            Text = Share.Name;
            SubItems[0].Text = Utilities.ByteSizetoDecString(Share.Size);

            string status = Share.FileStatus;

            if (Share.FileStatus != "" && Share.TransferStatus != "")
                status += ", ";

            status += Share.TransferStatus;

            SubItems[1].Text = status;

            SubItems[2].Text = (Local && !Share.Public) ? "No" : "Yes";

            if (TheView.Local && !Share.Completed)
                ForeColor = Color.Gray;
            else
                ForeColor = Color.Black;
        }
    }
}
