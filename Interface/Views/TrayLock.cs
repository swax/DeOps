using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;


namespace DeOps.Interface.Views
{
    
    internal class TrayLock
    {
        OpCore Core;
        NotifyIcon Tray = new NotifyIcon();

        internal TrayLock(OpCore core)
        {
            Core = core;

            Tray.Icon = InterfaceRes.rank;
            Tray.Text = Core.User.Settings.Operation + " - " + Core.User.Settings.ScreenName;
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
            GetTextDialog form = new GetTextDialog(Tray.Text, "Enter Password", "");
            
            form.ResultBox.UseSystemPasswordChar = true;

            if(form.ShowDialog() != DialogResult.OK)
                return;


            RijndaelManaged password = Utilities.PasswordtoRijndael(form.ResultBox.Text);


            if (Utilities.MemCompare(password.Key, Core.User.Password.Key) &&
                Utilities.MemCompare(password.IV, Core.User.Password.IV))
            {
                Core.GuiMain = new MainForm(Core);
                Core.GuiMain.Show();
               
                Tray.Visible = false;
                Core.GuiTray = null;
            }
            else
                MessageBox.Show("Wrong Password", "De-Ops");
        }

        void Menu_Exit(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
