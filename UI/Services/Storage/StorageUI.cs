using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeOps.Interface;
using DeOps.Implementation;

namespace DeOps.Services.Storage
{
    class StorageUI : IServiceUI
    {
        CoreUI UI;
        OpCore Core;
        StorageService Storage;


        public StorageUI(CoreUI ui, OpService service)
        {
            UI = ui;
            Core = ui.Core;
            Storage = service as StorageService;

            Storage.Disposing += Storage_Disposing;
        }

        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong user, uint project)
        {
            if (menuType == InterfaceMenuType.Internal)
                menus.Add(new MenuItemInfo("Data/File System", StorageRes.Icon, new EventHandler(Menu_View)));

            if (menuType == InterfaceMenuType.External)
                menus.Add(new MenuItemInfo("File System", StorageRes.Icon, new EventHandler(Menu_View)));
        }

        public void Menu_View(object sender, EventArgs args)
        {
            var node = sender as IViewParams;
            if (node == null)
                return;

            var view = new StorageView(UI, Storage, node.GetUser(), node.GetProject());

            UI.ShowView(view, node.IsExternal());
        }

        public void GetNewsAction(ref System.Drawing.Icon symbol, ref EventHandler onClick)
        {
            symbol = StorageRes.Icon;
            onClick = Menu_View;
        }

        public void Storage_Disposing()
        {
            if (Storage.HashFiles.Pending.Count > 0)
            {
                var status = new HashStatus(Storage);
                status.ShowDialog();
            }
        }
    }
}
