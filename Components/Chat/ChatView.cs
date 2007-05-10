using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface;


namespace DeOps.Components.Chat
{
    internal partial class ChatView : ViewShell
    {
        ChatControl Chat;

        internal ChatView(ChatControl chat)
        {
            InitializeComponent();

            Chat = chat;

            Chat.CreateRoomEvent += new CreateRoomHandler(OnCreateRoom);
            Chat.RemoveRoomEvent += new RemoveRoomHandler(OnRemoveRoom);
        }

        internal override void Init()
        {
            foreach (ChatRoom room in Chat.Rooms)
                OnCreateRoom(room);

            if (ViewTabs.TabPages.Count > 1)
                ViewTabs.SelectedTab = ViewTabs.TabPages[1];
        }

        internal override bool Fin()
        {
            Chat.CreateRoomEvent -= new CreateRoomHandler(OnCreateRoom);
            Chat.RemoveRoomEvent -= new RemoveRoomHandler(OnRemoveRoom);

            foreach (TabPage page in ViewTabs.TabPages)
                    foreach (Control obj in page.Controls)
                        if (obj is ChatSplit)
                        {
                            ClearPanel(((ChatSplit)obj).ViewContainer.Panel1);
                            ClearPanel(((ChatSplit)obj).ViewContainer.Panel2);
                        }

            return true;
        }

        internal override string GetTitle()
        {
            return "My Chat";
        }

        internal override Size GetDefaultSize()
        {
            return new Size(600, 350);
        }

        private void OnCreateRoom(ChatRoom room)
        {
            ChatSplit splitView = FindSplit(room);

            // if split not found, create
            if(splitView == null)
            {
                TabPage page = new TabPage(room.Name);
                ViewTabs.TabPages.Add(page);

                splitView = new ChatSplit(room.ID);
                splitView.Dock = DockStyle.Fill;
                page.Controls.Add(splitView);
            }

            // high room
            if (room.Kind != RoomKind.Command_Low)
            {
                ClearPanel(splitView.ViewContainer.Panel1);

                RoomView view = new RoomView(Chat, room);
                view.Dock = DockStyle.Fill;
                splitView.ViewContainer.Panel1.Controls.Add(view);
                
                view.Init();
                splitView.ViewContainer.Panel1Collapsed = false;
            }

            // low room
            else
            {
                ClearPanel(splitView.ViewContainer.Panel2);

                RoomView view = new RoomView(Chat, room);
                view.Dock = DockStyle.Fill;
                splitView.ViewContainer.Panel2.Controls.Add(view);
                
                view.Init();
                splitView.ViewContainer.Panel2Collapsed = false;
            }

            // collapse unused panels
            if (splitView.ViewContainer.Panel1.Controls.Count == 0)
                splitView.ViewContainer.Panel1Collapsed = true;

            if (splitView.ViewContainer.Panel2.Controls.Count == 0)
                splitView.ViewContainer.Panel2Collapsed = true;
        }

        private void OnRemoveRoom(ChatRoom room)
        {
            ChatSplit splitView = FindSplit(room);

            if (splitView == null)
                return;

            // high room
            if (room.Kind != RoomKind.Command_Low)
            {
                ClearPanel(splitView.ViewContainer.Panel1);
                splitView.ViewContainer.Panel1Collapsed = true;
            }

            // low room
            else
            {
                ClearPanel(splitView.ViewContainer.Panel2);
                splitView.ViewContainer.Panel2Collapsed = true;
            }

            // check if tab page needs to be removed
            if (splitView.ViewContainer.Panel1.Controls.Count == 0 &&
                splitView.ViewContainer.Panel2.Controls.Count == 0)
            {
                foreach (TabPage page in ViewTabs.TabPages)
                    foreach (Control obj in page.Controls)
                        if (obj is ChatSplit)
                            if(obj == splitView)
                            {
                                ViewTabs.TabPages.Remove(page);
                                return;
                            }
            }
        }

        private void ClearPanel(SplitterPanel panel)
        {
            foreach (Control item in panel.Controls)
                if (item is RoomView)
                    ((RoomView)item).Fin();

            panel.Controls.Clear();
        }
        
        private ChatSplit FindSplit(ChatRoom room)
        {
            foreach (TabPage page in ViewTabs.TabPages)
                foreach (Control obj in page.Controls)
                    if (obj is ChatSplit && ((ChatSplit)obj).RoomID == room.ID)
                        return obj as ChatSplit;

            return null;
        }


    }
}
