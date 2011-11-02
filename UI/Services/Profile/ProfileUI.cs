using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DeOps.Interface;
using DeOps.Implementation;


namespace DeOps.Services.Profile
{
    class ProfileUI : IServiceUI
    {
        CoreUI UI;
        OpCore Core;
        ProfileService Profile;


        public ProfileUI(CoreUI ui, OpService service)
        {
            UI = ui;
            Core = ui.Core;
            Profile = service as ProfileService;
        }

        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong user, uint project)
        {
            if (menuType == InterfaceMenuType.Internal)
                menus.Add(new MenuItemInfo("Data/Profile", ProfileRes.IconX, new EventHandler(Menu_View)));

            if (menuType == InterfaceMenuType.External)
                menus.Add(new MenuItemInfo("Profile", ProfileRes.IconX, new EventHandler(Menu_View)));
        }

        public void Menu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            ulong key = node.GetUser();

            if (Profile.Network.Responsive)
                Profile.Research(key);

            // gui creates viewshell, component just passes view object
            var view = new ProfileView(Profile, key, node.GetProject());

            UI.ShowView(view, node.IsExternal());
        }

        public void GetNewsAction(ref System.Drawing.Icon symbol, ref EventHandler onClick)
        {
            symbol = ProfileRes.IconX;
            onClick = Menu_View;
        }
    }
}
