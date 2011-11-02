using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DeOps.Interface;
using DeOps.Implementation;


namespace DeOps.Services.IM
{
    public class IMUI : IServiceUI
    {       
        CoreUI UI;
        IMService IM;


        public IMUI(CoreUI ui, OpService service)
        {
            UI = ui;
            IM = service as IMService;

            IM.CreateView += IM_CreateView;
        }

        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong user, uint project)
        {
            if (menuType == InterfaceMenuType.Quick)
            {
                if (user == UI.Core.UserID)
                    return;

                if (UI.Core.Locations.ActiveClientCount(user) == 0)
                    return;

                menus.Add(new MenuItemInfo("Send IM", IMRes.Icon, new EventHandler(QuickMenu_View)));
            }
        }

        public void QuickMenu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            ulong user = node.GetUser();

            OpenIMWindow(user);
        }

        public void OpenIMWindow(ulong user)
        {
            // if window already exists to node, show it
            var view = FindView(user);

            if (view != null && view.External != null)
                view.External.BringToFront();

            // else create new window
            else
            {
                IM_CreateView(user);

                IM.Connect(user);
            }
        }

        public IM_View FindView(ulong userID)
        {
            if (IM.MessageUpdate != null)
                foreach (Delegate func in IM.MessageUpdate.GetInvocationList())
                {
                    var view = func.Target as IM_View;
                    if (view != null && view.UserID == userID)
                        return view;
                }

            return null;
        }

        public void IM_CreateView(ulong userID)
        {
            var view = new IM_View(UI, IM, userID);

            UI.ShowView(view, true);
        }

        public void GetNewsAction(ref System.Drawing.Icon symbol, ref EventHandler onClick)
        {

        }
    }
}
