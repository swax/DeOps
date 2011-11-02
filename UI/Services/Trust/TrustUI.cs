using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeOps.Interface;
using DeOps.Implementation;

namespace DeOps.Services.Trust
{
    public class TrustUI : IServiceUI
    {
        CoreUI UI;
        OpCore Core;
        TrustService Trust;


        public TrustUI(CoreUI ui, OpService service)
        {
            UI = ui;
            Core = ui.Core;
            Trust = service as TrustService;
        }

        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong user, uint project)
        {
            if (menuType != InterfaceMenuType.Quick)
                return;

            bool unlink = false;

            OpLink remoteLink = Trust.GetLink(user, project);
            OpLink localLink = Trust.LocalTrust.GetLink(project);

            if (remoteLink == null)
                return;

            // linkup
            if (Core.UserID != user &&
                (localLink == null ||
                 localLink.Uplink == null ||
                 localLink.Uplink.UserID != user)) // not already linked to
                menus.Add(new MenuItemInfo("Trust", LinkRes.linkup, new EventHandler(Menu_Linkup)));

            if (localLink == null)
                return;

            // confirm
            if (localLink.Downlinks.Contains(remoteLink))
            {
                unlink = true;

                if (!localLink.Confirmed.Contains(user)) // not already confirmed
                    menus.Add(new MenuItemInfo("Accept Trust", LinkRes.linkup, new EventHandler(Menu_ConfirmLink)));
            }

            // unlink
            if ((unlink && localLink.Confirmed.Contains(user)) ||
                (localLink.Uplink != null && localLink.Uplink.UserID == user))
                menus.Add(new MenuItemInfo("Revoke Trust", LinkRes.unlink, new EventHandler(Menu_Unlink)));
        }

        private void Menu_Linkup(object sender, EventArgs e)
        {
            if (!(sender is IViewParams))
                return;

            ulong user = ((IViewParams)sender).GetUser();
            uint project = ((IViewParams)sender).GetProject();

            Trust.LinkupTo(user, project);
        }

        private void Menu_ConfirmLink(object sender, EventArgs e)
        {
            if (!(sender is IViewParams))
                return;

            ulong user = ((IViewParams)sender).GetUser();
            uint project = ((IViewParams)sender).GetProject();

            Trust.AcceptTrust(user, project);
        }

        private void Menu_Unlink(object sender, EventArgs e)
        {
            if (!(sender is IViewParams))
                return;

            ulong user = ((IViewParams)sender).GetUser();
            uint project = ((IViewParams)sender).GetProject();

            Trust.UnlinkFrom(user, project);
        }

        public void GetNewsAction(ref System.Drawing.Icon symbol, ref EventHandler onClick)
        {
            symbol = LinkRes.link;
        }
    }
}
