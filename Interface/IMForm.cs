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


namespace DeOps.Interface
{
    internal partial class IMForm : HostsExternalViews
    {
        OpCore Core;


        internal IMForm(OpCore core)
            : base(core)
        {
            InitializeComponent();

            Core = core;

            Utilities.SetupToolstrip(TopStrip, new OpusColorTable());
            Utilities.FixMonoDropDownOpening(OptionsButton, OptionsButton_DropDownOpening);

            Core.ShowExternal += new ShowExternalHandler(OnShowExternal);

            Text = "IM - " + Core.GetName(Core.UserID);

            BuddyList.Init(Core.Buddies, SelectionInfo, true);
            SelectionInfo.Init(Core);

            SelectionInfo.ShowNetwork();

            if (Utilities.IsRunningOnMono())
            {
                AddButton.Text = "Add";
                SharedButton.Text = "Files";
            }

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
            if(!ShowExistingView(typeof(InfoView)))
                OnShowExternal(new InfoView(Core, true, false));
        }

        private void SharedButton_Click(object sender, EventArgs e)
        {
            if (!ShowExistingView(typeof(SharingView)))
                OnShowExternal(new SharingView(Core, Core.UserID));    
        }

        private void ChatButton_Click(object sender, EventArgs e)
        {
            if (!ShowExistingView(typeof(ChatView)))
            {
                ChatService chat = Core.GetService(ServiceIDs.Chat) as ChatService;
                OnShowExternal(new ChatView(chat, 0));
            }
        }

        private void OptionsButton_DropDownOpening(object sender, EventArgs e)
        {
            OptionsButton.DropDownItems.Clear();

            MainForm.FillManageMenu(Core, OptionsButton.DropDownItems);
        }
    }
}
