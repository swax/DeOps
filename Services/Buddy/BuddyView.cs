using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;

using RiseOp.Interface;
using RiseOp.Interface.Views;
using RiseOp.Interface.TLVex;

using RiseOp.Services.Location;
using RiseOp.Services.Trust;


namespace RiseOp.Services.Buddy
{
    class BuddyView : ContainerListViewEx
    {
        OpCore Core;
        BuddyService Buddies;
        LocationService Locations;

        internal Font OnlineFont = new Font("Tahoma", 8.25F);
        internal Font LabelFont = new Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        internal Font OfflineFont = new Font("Tahoma", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));


        internal BuddyView()
        {
            DisableHScroll = true;
            HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;

            Columns.Add("", 100, System.Windows.Forms.HorizontalAlignment.Left, ColumnScaleStyle.Spring);
        }

        internal void Init(BuddyService buddies)
        {
            Buddies = buddies;
            Core = buddies.Core;
            Locations = Core.Locations;

            Buddies.GuiUpdate += new BuddyGuiUpdateHandler(Buddy_Update);
            Locations.GuiUpdate += new LocationGuiUpdateHandler(Location_Update);

            MouseClick += new MouseEventHandler(BuddyList_MouseClick);

            SmallImageList = new List<Image>(); // itit here, cause main can re-init
            SmallImageList.Add(new Bitmap(16, 16));

            RefreshView();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Buddies.GuiUpdate -= new BuddyGuiUpdateHandler(Buddy_Update);
                Locations.GuiUpdate -= new LocationGuiUpdateHandler(Location_Update);

                MouseClick -= new MouseEventHandler(BuddyList_MouseClick);
            }

            base.Dispose(disposing);
        }


        void Buddy_Update()
        {
            RefreshView();
        }

        void Location_Update(ulong user)
        {
            if (Buddies.BuddyList.SafeContainsKey(user))
                RefreshView();           
        }

        private void RefreshView()
        {
            SortedList<string, BuddyItem> Online = new SortedList<string,BuddyItem>();
            SortedList<string, BuddyItem> Offline = new SortedList<string,BuddyItem>();
            
            Buddies.BuddyList.LockReading(delegate()
            {
                foreach (OpBuddy buddy in Buddies.BuddyList.Values)
                {
                    BuddyItem item = new BuddyItem(buddy.Name, buddy.ID);

                    if (Locations.ActiveClientCount(buddy.ID) > 0)
                    {
                        item.Font = OnlineFont;
                        Online.Add(buddy.Name, item);
                    }
                    else
                    {
                        item.Font = OfflineFont;
                        item.ForeColor = Color.Gray;
                        Offline.Add(buddy.Name, item);
                    }
                }
            });

            Items.Clear();

            Items.Add(new BuddyItem());
            
            // show online buddies
            Items.Add(new BuddyItem("Buddies", LabelFont));

            foreach (BuddyItem item in Online.Values)
                Items.Add(item);

            Items.Add(new BuddyItem());


            // show offline
            Items.Add(new BuddyItem("Offline", LabelFont));

            foreach (BuddyItem item in Offline.Values)
                Items.Add(item);

        }


        private void BuddyList_MouseClick(object sender, MouseEventArgs e)
        {
            // this gets right click to select item
            BuddyItem clicked = GetItemAt(e.Location) as BuddyItem;

            if (clicked == null || clicked.User == 0)
                return;


            // right click menu
            if (e.Button != MouseButtons.Right)
                return;

            uint project = 0;

            // menu
            ContextMenuStripEx treeMenu = new ContextMenuStripEx();


            // views
            List<ToolStripMenuItem> quickMenus = new List<ToolStripMenuItem>();
            List<ToolStripMenuItem> extMenus = new List<ToolStripMenuItem>();

            foreach (OpService service in Core.ServiceMap.Values)
            {
                if (service is TrustService)
                    continue;

                // quick
                List<MenuItemInfo> menuList = service.GetMenuInfo(InterfaceMenuType.Quick, clicked.User, project);

                if (menuList != null && menuList.Count > 0)
                    foreach (MenuItemInfo info in menuList)
                        quickMenus.Add(new OpMenuItem(clicked.User, project, info.Path, info));

                // external
                menuList = service.GetMenuInfo(InterfaceMenuType.External, clicked.User, project);

                if (menuList != null && menuList.Count > 0)
                    foreach (MenuItemInfo info in menuList)
                        extMenus.Add(new OpMenuItem(clicked.User, project, info.Path, info));
            }

            foreach (ToolStripMenuItem menu in quickMenus)
                treeMenu.Items.Add(menu);

            if (extMenus.Count > 0)
            {
                ToolStripMenuItem viewItem = new ToolStripMenuItem("Views", InterfaceRes.views);

                foreach (ToolStripMenuItem menu in extMenus)
                    viewItem.DropDownItems.Add(menu);

                treeMenu.Items.Add(viewItem);
            }

            //crit - add projcet sub-menus

            // show
            if (treeMenu.Items.Count > 0)
                treeMenu.Show(this, e.Location);
        }
    }


    internal class BuddyItem : ContainerListViewItem
    {
        internal ulong User;

        internal BuddyItem()
        {
            Text = "";
            ImageIndex = 0;
        }

        internal BuddyItem(string text, Font font)
        {
            Text = text;
            Font = font;
            ImageIndex = 0;
        }

        internal BuddyItem(string text, ulong id)
        {
            Text = text;
            User = id;
            ImageIndex = 0;
        }

    }
}
