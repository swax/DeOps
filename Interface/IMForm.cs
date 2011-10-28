using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;

using DeOps.Interface.Info;
using DeOps.Interface.Views;

using DeOps.Services;
using DeOps.Services.Buddy;
using DeOps.Services.Share;
using DeOps.Services.Chat;
using DeOps.Services.IM;


namespace DeOps.Interface
{
    internal partial class IMForm : HostsExternalViews
    {
        CoreUI UI;
        OpCore Core;
        IMUI IM;

        internal IMForm(CoreUI ui)
            : base(ui.Core)
        {
            InitializeComponent();

            UI = ui;
            Core = ui.Core;

            GuiUtils.SetupToolstrip(TopStrip, new OpusColorTable());
            GuiUtils.FixMonoDropDownOpening(OptionsButton, OptionsButton_DropDownOpening);

            UI.ShowView += ShowExternal;

            Text = "IM - " + Core.GetName(Core.UserID);

            BuddyList.Init(UI, Core.Buddies, SelectionInfo, true);

            IM = UI.GetService(ServiceIDs.IM) as IMUI;

            SelectionInfo.Init(UI);

            SelectionInfo.ShowNetwork();

            if (GuiUtils.IsRunningOnMono())
            {
                AddButton.Text = "Add";
                SharedButton.Text = "Files";
            }

            Rectangle screen = Screen.GetWorkingArea(this);
            Location = new Point(screen.Width - Width, screen.Height / 2 - Height / 2);


            ShowExternal(new Info.InfoView(Core, false, true));
        }

        public void ShowExternal(ViewShell view, bool external=false)
        {
            var extView = new ExternalView(this, ExternalViews, view);

            ExternalViews.Add(extView);

            view.Init();

            extView.Show();
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

            UI.ShowView -= ShowExternal;

            UI.GuiMain = null;

            if (Core.Sim == null)
                Core.Exit();
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            BuddyView.AddBuddyDialog(Core, "");
   
        }

        bool InTray;

        private void IMForm_SizeChanged(object sender, EventArgs e)
        {
            if (Core == null)
                return;

            if (!InTray && WindowState == FormWindowState.Minimized)
            {
                InTray = true;
                ShowInTaskbar = false;

                UI.GuiTray = new TrayLock(UI, false);
            }

            else
            {
                InTray = false;
                ShowInTaskbar = true;

                if(UI.GuiTray != null)
                    UI.GuiTray.CleanupTray();
            }
        }

        private void HelpInfoButton_Click(object sender, EventArgs e)
        {
            if(!ShowExistingView(typeof(InfoView)))
                ShowExternal(new InfoView(Core, true, false));
        }

        private void SharedButton_Click(object sender, EventArgs e)
        {
            if (!ShowExistingView(typeof(SharingView)))
                ShowExternal(new SharingView(Core, Core.UserID));    
        }

        private void ChatButton_Click(object sender, EventArgs e)
        {
            if (!ShowExistingView(typeof(ChatView)))
            {
                ChatService chat = Core.GetService(ServiceIDs.Chat) as ChatService;
                ShowExternal(new ChatView(UI, chat, 0));
            }
        }

        private void OptionsButton_DropDownOpening(object sender, EventArgs e)
        {
            OptionsButton.DropDownItems.Clear();

            MainForm.FillManageMenu(UI, OptionsButton.DropDownItems);
        }
    }
}
