using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;


namespace RiseOp.Interface.Views
{
    
    internal class TrayLock
    {
        OpCore Core;
        NotifyIcon Tray = new NotifyIcon();
        bool PreserveSideMode;

        internal TrayLock(OpCore core, bool sideMode)
        {
            Core = core;

            PreserveSideMode = sideMode;

            Tray.Icon = InterfaceRes.riseop;
            Tray.Text = Core.Profile.GetTitle(); 
            Tray.Visible = true;

            Tray.DoubleClick += new EventHandler(Tray_DoubleClick);

            ContextMenuStripEx menu = new ContextMenuStripEx();

            menu.Items.Add("Restore", null, new EventHandler(Menu_Restore));
            menu.Items.Add("Exit", null, new EventHandler(Menu_Exit));

            Tray.ContextMenuStrip = menu;
        }

        void Tray_DoubleClick(object sender, EventArgs e)
        {
            Menu_Restore(sender, e);
        }
        
        void Menu_Restore(object sender, EventArgs e)
        {
            if (Utilities.VerifyPassphrase(Core, ThreatLevel.High))
            {
                Core.GuiMain = new MainForm(Core);
                Core.GuiMain.SideMode = PreserveSideMode;
                Core.GuiMain.Show();
               
                Tray.Visible = false;
                Core.GuiTray = null;
            }                
        }

        void Menu_Exit(object sender, EventArgs e)
        {     
            Tray.Visible = false;
            Core.GuiTray = null;
         
            if (Core.Sim == null)
                Core.Exit(); 
        }
    }
}
