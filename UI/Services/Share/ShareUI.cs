using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DeOps.Interface;
using DeOps.Implementation;


namespace DeOps.Services.Share
{
    class ShareUI : IServiceUI
    {
        CoreUI UI;
        OpCore Core;
        ShareService Share;


        public ShareUI(CoreUI ui, OpService service)
        {
            UI = ui;
            Core = ui.Core;
            Share = service as ShareService;
        }

        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong user, uint project)
        {
            if (Core.Locations.ActiveClientCount(user) == 0)
                return;

            if (menuType == InterfaceMenuType.Internal)
                menus.Add(new MenuItemInfo("Data/Share", Res.ShareRes.Icon, new EventHandler(Menu_View)));

            if (menuType == InterfaceMenuType.External)
                menus.Add(new MenuItemInfo("Share", Res.ShareRes.Icon, new EventHandler(Menu_View)));
        }

        public void Menu_View(object sender, EventArgs args)
        {
            var node = sender as IViewParams;
            if (node == null)
                return;

            var view = new SharingView(Core, node.GetUser());

            UI.ShowView(view, node.IsExternal());

            Share.GetPublicList(node.GetUser());
        }

        public void GetNewsAction(ref System.Drawing.Icon symbol, ref EventHandler onClick)
        {
             symbol = Res.ShareRes.Icon;
             onClick = Menu_View;
        }
    }
}
