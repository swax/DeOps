using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DeOps.Interface;
using DeOps.Implementation;


namespace DeOps.Services.Mail
{
    public class MailUI : IServiceUI
    {
        public CoreUI UI;
        public OpCore Core;
        public MailService Mail;


        public MailUI(CoreUI ui, OpService service)
        {
            UI = ui;
            Core = ui.Core;
            Mail = service as MailService;

            Mail.ShowCompose += Mail_ShowCompose;
        }


        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong user, uint project)
        {
            if (menuType == InterfaceMenuType.Quick)
            {
                if (user == Core.UserID)
                    return;

                menus.Add(new MenuItemInfo("Send Mail", MailRes.SendMail, new EventHandler(QuickMenu_View)));
                return;
            }

            if (user != Core.UserID)
                return;

            if (menuType == InterfaceMenuType.Internal)
                menus.Add(new MenuItemInfo("Comm/Mail", MailRes.Icon, new EventHandler(Menu_View)));

            if (menuType == InterfaceMenuType.External)
                menus.Add(new MenuItemInfo("Mail", MailRes.Icon, new EventHandler(Menu_View)));
        }

        internal void QuickMenu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            ulong user = 0;

            if (node != null)
                user = node.GetUser();

            OpenComposeWindow(user);
        }

        void Menu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            if (node.GetUser() != Core.UserID)
                return;

            var view = new MailView(this);

            UI.ShowView(view, node.IsExternal());
        }

        public void OpenComposeWindow(ulong user)
        {
            // if window already exists to node, show it
            var compose = new ComposeMail(UI, Mail, user);

            UI.ShowView(compose, true);
        }

        public void GetNewsAction(ref System.Drawing.Icon symbol, ref EventHandler onClick)
        {
            symbol = MailRes.Icon;
            onClick = Menu_View;
        }

        public void Mail_ShowCompose(ulong userID, LocalMail message, string title, string body)
        {
            ComposeMail compose = new ComposeMail(UI, Mail, userID);
            compose.CustomTitle = title;
            compose.ThreadID = message.Header.ThreadID;

            compose.SubjectTextBox.Text = message.Info.Subject;
            compose.SubjectTextBox.Enabled = false;
            compose.SubjectTextBox.BackColor = System.Drawing.Color.WhiteSmoke;

            //crit attach files

            UI.ShowView(compose, true);
        }
    }
}
