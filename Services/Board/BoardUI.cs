using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DeOps.Interface;


namespace DeOps.Services.Board
{
    public class BoardUI : IServiceUI
    {
        CoreUI UI;
        BoardService Board;


        public BoardUI(CoreUI ui, OpService board)
        {
            UI = ui;
            Board = board as BoardService;
        }

        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong user, uint project)
        {
            if (menuType == InterfaceMenuType.Quick)
                return;

            if (menuType == InterfaceMenuType.Internal)
                menus.Add(new MenuItemInfo("Comm/Board", BoardRes.Icon, Menu_View));

            if (menuType == InterfaceMenuType.External)
                menus.Add(new MenuItemInfo("Board", BoardRes.Icon, Menu_View));
        }

        void Menu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            var view = new BoardView(UI, Board, node.GetUser(), node.GetProject());

            UI.ShowView(view, node.IsExternal());
        }

        public void GetNewsAction(ref System.Drawing.Icon symbol, ref EventHandler onClick)
        {
            symbol = BoardRes.Icon;
            onClick = Menu_View;
        }
    }
}
