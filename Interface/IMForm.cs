using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;

using RiseOp.Interface.Info;
using RiseOp.Interface.Views;

using RiseOp.Services;
using RiseOp.Services.Buddy;
using RiseOp.Services.Sharing;
using RiseOp.Services.Chat;


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

            BuddyList.Init(Core.Buddies, SelectionInfo, true);
            SelectionInfo.Init(Core);

            SelectionInfo.ShowNetwork();

            MainForm.FillManageMenu(Core, OptionsButton.DropDownItems);

            Rectangle screen = Screen.GetWorkingArea(this);
            Location = new Point(screen.Width - Width, screen.Height / 2 - Height / 2); 

            

            OnShowExternal(new Info.InfoView(core, false, true));
        }

        void OnShowExternal(ViewShell view)
        {
            ExternalView external = new ExternalView(this, ExternalViews, view);

            ExternalViews.Add(external);

            view.Init();

            external.Show();
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

            if (Core.Sim == null)
                Core.Exit();
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            BuddyView.AddBuddyDialog(Core);
   
        }

        bool InTray;

        private void IMForm_SizeChanged(object sender, EventArgs e)
        {
            if (!InTray && WindowState == FormWindowState.Minimized)
            {
                InTray = true;
                ShowInTaskbar = false;

                Core.GuiTray = new TrayLock(Core, false);
            }

            else
            {
                InTray = false;
                ShowInTaskbar = true;

                if(Core.GuiTray != null)
                    Core.GuiTray.CleanupTray();
            }
        }

        private void HelpInfoButton_Click(object sender, EventArgs e)
        {
            foreach (ExternalView ext in ExternalViews)
                if (ext.Shell is InfoView)
                {
                    ext.Activate();
                    return;
                }

            OnShowExternal(new InfoView(Core, true, false));
        }

        private void SharedButton_Click(object sender, EventArgs e)
        {
            foreach(ExternalView ext in ExternalViews)
                if (ext.Shell is SharingView)
                {
                    ext.Activate();
                    return;
                }

            if(Core.GetService(ServiceIDs.Sharing) != null)
                OnShowExternal(new SharingView(Core, Core.UserID));
        }

        private void ChatButton_Click(object sender, EventArgs e)
        {
            foreach (ExternalView ext in ExternalViews)
                if (ext.Shell is ChatView)
                {
                    ext.Activate();
                    return;
                }

            ChatService chat = Core.GetService(ServiceIDs.Chat) as ChatService;

            if (chat != null)
                OnShowExternal(new ChatView(chat, 0));
        }
    }
}
