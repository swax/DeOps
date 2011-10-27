using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DeOps.Interface;
using DeOps.Implementation;


namespace DeOps.Services.Chat
{
    class ChatUI : IServiceUI
    {
        CoreUI UI;
        OpCore Core;
        ChatService Chat;


        public ChatUI(CoreUI ui, OpService service)
        {
            UI = ui;
            Core = ui.Core;
            Chat = service as ChatService;

            Chat.NewInvite += Chat_NewInvite;
        }

        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong key, uint proj)
        {
            if (key != Core.UserID)
                return;

            if (menuType == InterfaceMenuType.Internal)
                menus.Add(new MenuItemInfo("Comm/Chat", ChatRes.Icon, new EventHandler(Menu_View)));

            if (menuType == InterfaceMenuType.External)
                menus.Add(new MenuItemInfo("Chat", ChatRes.Icon, new EventHandler(Menu_View)));
        }

        void Menu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            if (node.GetUser() != Core.UserID)
                return;

            // gui creates viewshell, component just passes view object
            var view = new ChatView(UI, Chat, node.GetProject());

            UI.ShowView(view, node.IsExternal());
        }

        public void GetNewsAction(ref System.Drawing.Icon symbol, ref EventHandler onClick)
        {
            symbol = ChatRes.Icon;
            onClick = Menu_View;
        }

        public void Chat_NewInvite(ulong userID, ChatRoom room)
        {
            new InviteForm(UI, Chat, userID, room).ShowDialog();
        }
    }
}
