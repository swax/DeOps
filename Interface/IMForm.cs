using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Interface.Views;
using RiseOp.Services.Buddy;


namespace RiseOp.Interface
{
    internal partial class IMForm : CustomIconForm
    {
        OpCore Core;

        internal List<ExternalView> ExternalViews = new List<ExternalView>();



        internal IMForm(OpCore core)
            : base(core)
        {
            InitializeComponent();

            Core = core;

            TopStrip.Renderer = new ToolStripProfessionalRenderer(new OpusColorTable());

            Core.ShowExternal += new ShowExternalHandler(OnShowExternal);

            Text = "IM - " + Core.GetName(Core.UserID);

            BuddyList.Init(Core.Buddies);
            SelectionInfo.Init(Core);

            SelectionInfo.ShowNetwork();
        }


        void OnShowExternal(ViewShell view)
        {
            ExternalView external = new ExternalView(this, ExternalViews, view);

            ExternalViews.Add(external);

            view.Init();

            external.Show();
        }

        private void BuddyList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (BuddyList.SelectedItems.Count == 0)
            {
                SelectionInfo.ShowNetwork();
                return;
            }

            BuddyItem item = BuddyList.SelectedItems[0] as BuddyItem;

            if (item == null || item.Blank)
                SelectionInfo.ShowNetwork();

            else if (item.User != 0)
                SelectionInfo.ShowUser(item.User, 0);

            else
                SelectionInfo.ShowGroup(item.GroupLabel ? item.Text : null);
        }

        private void IMForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            while (ExternalViews.Count > 0)
                // safe close removes entry from external views
                if (!ExternalViews[0].SafeClose())
                {
                    e.Cancel = true;
                    return;
                }

            Core.ShowExternal -= new ShowExternalHandler(OnShowExternal);

            Core.GuiMain = null;

            if (LockForm)
            {
                LockForm = false;
                return;
            }

            if (Core.Sim == null)
                Core.Exit();
        }

        bool LockForm;

        private void MinButton_Click(object sender, EventArgs e)
        {
            LockForm = true;

            Close();

            Core.GuiTray = new TrayLock(Core, false, false);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            GetTextDialog add = new GetTextDialog("Add Buddy", "Enter a buddy link", Core.Buddies.GetLink(Core.UserID));

            if (add.ShowDialog() == DialogResult.OK)
                Core.Buddies.AddBuddy(add.ResultBox.Text);
        }
    }
}
