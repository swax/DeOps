using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;


namespace DeOps.Interface.Views
{
    
    public class TrayLock
    {
        CoreUI UI;
        OpCore Core;
        NotifyIcon Tray = new NotifyIcon();
        bool PreserveSideMode;


        public TrayLock(CoreUI ui, bool sideMode)
        {
            UI = ui;
            Core = ui.Core;

            PreserveSideMode = sideMode;

            Profile_IconUpdate();
            Tray.Text = Core.User.GetTitle(); 
            Tray.Visible = true;

            Tray.DoubleClick += new EventHandler(Tray_DoubleClick);
            Core.User.GuiIconUpdate += Profile_IconUpdate;

            ContextMenuStripEx menu = new ContextMenuStripEx();

            menu.Items.Add("Restore", null, new EventHandler(Menu_Restore));
            menu.Items.Add("Exit", null, new EventHandler(Menu_Exit));

            Tray.ContextMenuStrip = menu;
        }

        void Profile_IconUpdate()
        {
            //crit bug, wont fire when in tray mode because core.RunInGuiThread depends on guiMain which is null
            // need to create a hidden form which events like this can be processed on, a little tricky, todo l8r
            // on other hand might be a good idea to prevent icon from changing in tray mode

            /*HiddenForm = new Form(); // put this code in core / context to remove fast timer
            HiddenForm.Size = new Size(0, 0);
            HiddenForm.GotFocus += new EventHandler(HiddenForm_GotFocus); // run hide() in here
            HiddenForm.Show();*/ // sill a little flicker which is annyoing

            Tray.Icon = Core.User.GetOpIcon();
        }

        void Tray_DoubleClick(object sender, EventArgs e)
        {
            Menu_Restore(sender, e);
        }
        
        void Menu_Restore(object sender, EventArgs e)
        {
            // if IM window (minimized, not unloaded)
            if (UI.GuiMain != null)
            {
                UI.GuiMain.WindowState = FormWindowState.Normal;
                return;
            }

            if (GuiUtils.VerifyPassphrase(Core, ThreatLevel.High))
            {
                UI.ShowMainView(PreserveSideMode);

                CleanupTray();
            }                
        }

        void Menu_Exit(object sender, EventArgs e)
        {
            CleanupTray();
         
            if (Core.Sim == null)
                Core.Exit(); 
        }

        public void CleanupTray()
        {
            Core.User.GuiIconUpdate -= Profile_IconUpdate;
            Tray.Visible = false;
            UI.GuiTray = null;
        }
    }
}
