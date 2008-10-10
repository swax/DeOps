using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;

using RiseOp.Interface;
using RiseOp.Interface.TLVex;
using RiseOp.Interface.Views;

using RiseOp.Services.Transfer;


namespace RiseOp.Services.Sharing
{
    internal partial class SharingView : ViewShell
    {
        OpCore Core;
        SharingService Sharing;

        Dictionary<string, int> IconMap = new Dictionary<string, int>();
        List<Image> FileIcons = new List<Image>();


        internal SharingView(OpCore core)
        {
            InitializeComponent();

            Core = core;
            Sharing = core.GetService(ServiceIDs.Sharing) as SharingService;

            TopStrip.Renderer = new ToolStripProfessionalRenderer(new OpusColorTable());

            SharedFiles.SmallImageList = FileIcons;
        }

        internal override void Init() 
        {
            Sharing.ShareList.LockReading(() =>
            {
                foreach (OpShare share in Sharing.ShareList)
                    SharedFiles.Items.Add(new ShareItem(this, share));
            });

            Sharing.GuiUpdate += new ShareUpdateHandler(Sharing_GuiUpdate);


        }

        internal override bool Fin() 
        {
            Sharing.GuiUpdate -= new ShareUpdateHandler(Sharing_GuiUpdate);

            return true; 
        }

        internal override string GetTitle(bool small) 
        {
            return small ? "Share" : "My Sharing";
        }

        internal override Size GetDefaultSize()
        {
            return new Size(500, 300);
        }

        internal override Icon GetIcon()
        {
            return Res.ShareRes.Icon;
        }

        internal int GetImageIndex(OpShare share)
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

        void Sharing_GuiUpdate(OpShare share)
        {
            bool deleted = false;

            Sharing.ShareList.LockReading(() =>
            {
                deleted = !Sharing.ShareList.Any(s => s == share);
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
            SharedFiles.Update();
        }
        
        private void SecondTimer_Tick(object sender, EventArgs e)
        {
            foreach (ShareItem item in SharedFiles.Items)
                item.RefreshStatus();
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

            Sharing.ShareFile(open.FileName);
        }

        private void SharedFiles_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (SharedFiles.SelectedItems.Count == 0)
                return;

            ShareItem item = SharedFiles.SelectedItems[0] as ShareItem;

            Sharing.OpenFile(item.Share);
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

            menu.Items.Add(new ToolStripMenuItem("Copy File Link", null, new EventHandler(Menu_CopyFileLink)));

            if(!item.Share.Completed)
                menu.Items.Add(new ToolStripMenuItem("Re-Search", null, new EventHandler(Menu_ReSearch)));
            
            if(item.Share.SystemPath != null)
                menu.Items.Add(new ToolStripMenuItem("Open Containing Folder", null, (EventHandler)delegate(object s, EventArgs ea)
                { Utilities.OpenFolder(item.Share.SystemPath); }));

            if(item.Share.TransferActive)
                menu.Items.Add(new ToolStripMenuItem("Transfer Details", null, (EventHandler) delegate(object s, EventArgs ea) 
                    { TransferView.Show(Core.Network); }));

            menu.Items.Add(new ToolStripMenuItem("Rename", null, new EventHandler(Menu_Rename)));

            menu.Items.Add(new ToolStripMenuItem("Remove", null, new EventHandler(Menu_Remove)));

            if (menu.Items.Count > 0)
                menu.Show(SharedFiles, e.Location);
        }

        void Menu_CopyFileLink(object sender, EventArgs e)
        {
            if (SharedFiles.SelectedItems.Count == 0)
                return;

            ShareItem item = SharedFiles.SelectedItems[0] as ShareItem;


            Clipboard.SetText(Sharing.GetFileLink(item.Share));
        }

        void Menu_Remove(object sender, EventArgs e)
        {
            foreach (ShareItem item in SharedFiles.SelectedItems)
                Sharing.RemoveShare(item.Share);
        }

        void Menu_ReSearch(object sender, EventArgs e)
        {
            foreach (ShareItem item in SharedFiles.SelectedItems)
                Sharing.ReSearchShare(item.Share);
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
            SharedFiles.Update();
        }
    }


    internal class ShareItem : ContainerListViewItem
    {
        internal OpShare Share;

        internal ShareItem(SharingView theView, OpShare share)
        {
            Share = share;

            ImageIndex = theView.GetImageIndex(share);

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
        }
    }
}
