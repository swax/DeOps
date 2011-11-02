using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DeOps.Interface;
using DeOps.Implementation;
using DeOps.Interface.Setup;


namespace DeOps.Services.Buddy
{
    public class BuddyUI : IServiceUI
    {
        CoreUI UI;
        OpCore Core;
        BuddyService Buddies;


        public BuddyUI(CoreUI ui, OpService service)
        {
            UI = ui;
            Core = ui.Core;
            Buddies = service as BuddyService;
        }

        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong user, uint project)
        {
            if (menuType != InterfaceMenuType.Quick)
                return;

            if (user != Core.UserID && !Buddies.BuddyList.SafeContainsKey(user))
                menus.Add(new MenuItemInfo("Add Buddy", BuddyRes.buddy_add, new EventHandler(Menu_Add)));

            menus.Add(new MenuItemInfo("Identity", BuddyRes.buddy_who, new EventHandler(Menu_Identity)));
        }

        private void Menu_Add(object sender, EventArgs e)
        {
            if (!(sender is IViewParams))
                return;

            ulong user = ((IViewParams)sender).GetUser();

            Buddies.AddBuddy(user);
        }

        private void Menu_Identity(object sender, EventArgs e)
        {
            if (!(sender is IViewParams))
                return;

            ulong user = ((IViewParams)sender).GetUser();

            ShowIdentity(user);
        }

        public void ShowIdentity(ulong user)
        {
            new IdentityForm(Core, user).ShowDialog();
        }

        public void GetNewsAction(ref System.Drawing.Icon symbol, ref EventHandler onClick)
        {

        }
    }
}
