using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DeOps.Interface;
using DeOps.Implementation;


namespace DeOps.Services.Plan
{
    class PlanUI : IServiceUI
    {
        CoreUI UI;
        OpCore Core;
        PlanService Plan;


        public PlanUI(CoreUI ui, OpService service)
        {
            UI = ui;
            Core = ui.Core;
            Plan = service as PlanService;
        }

        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong user, uint project)
        {
            if (menuType == InterfaceMenuType.Internal)
            {
                menus.Add(new MenuItemInfo("Plans/Schedule", PlanRes.Schedule, new EventHandler(Menu_ScheduleView)));
                menus.Add(new MenuItemInfo("Plans/Goals", PlanRes.Goals, new EventHandler(Menu_GoalsView)));
            }

            if (menuType == InterfaceMenuType.External)
            {
                menus.Add(new MenuItemInfo("Schedule", PlanRes.Schedule, new EventHandler(Menu_ScheduleView)));
                menus.Add(new MenuItemInfo("Goals", PlanRes.Goals, new EventHandler(Menu_GoalsView)));
            }
        }

        void Menu_ScheduleView(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            var view = new ScheduleView(UI, Plan, node.GetUser(), node.GetProject());

            UI.ShowView(view, node.IsExternal());
        }

        void Menu_GoalsView(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            var view = new GoalsView(UI, Plan, node.GetUser(), node.GetProject());

            UI.ShowView(view, node.IsExternal());
        }

        public void GetNewsAction(ref System.Drawing.Icon symbol, ref EventHandler onClick)
        {
            symbol = PlanRes.Schedule;
            onClick = Menu_ScheduleView;
        }
    }
}
