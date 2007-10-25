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
        uint ProjectID;

        RoomView ViewHigh;
        RoomView ViewLow;

        internal ChatView(ChatControl chat, uint project)
        {
            InitializeComponent();

            Chat = chat;
            ProjectID = project;

            Chat.CreateRoomEvent += new CreateRoomHandler(OnCreateRoom);
            Chat.RemoveRoomEvent += new RemoveRoomHandler(OnRemoveRoom);
        }

        internal override void Init()
        {
            foreach (ChatRoom room in Chat.Rooms)
                if(room.ProjectID == ProjectID)
                    OnCreateRoom(room);
        }

        internal override bool Fin()
        {
            Chat.CreateRoomEvent -= new CreateRoomHandler(OnCreateRoom);
            Chat.RemoveRoomEvent -= new RemoveRoomHandler(OnRemoveRoom);

            ClearRoom(ViewHigh);
            ClearRoom(ViewLow);

            return true;
        }

        private void ClearRoom(RoomView room)
        {
            if (room == null)
                return;

            if (room == ViewHigh)
            {
                ViewHigh.Fin();
                ViewContainer.Panel1.Controls.Clear();
            }

            if (room == ViewLow)
            {
                ViewLow.Fin();
                ViewContainer.Panel2.Controls.Clear();
            }

        }

        internal override string GetTitle(bool small)
        {
            if (small)
                return "Chat";

            string title = "My ";

            if (ProjectID != 0)
                title += Chat.Core.Links.ProjectNames[ProjectID] + " ";

            title += " Chat";

            return title;
        }

        internal override Size GetDefaultSize()
        {
            return new Size(600, 350);
        }

        internal override Icon GetIcon()
        {
            return ChatRes.Icon;
        }

        private void OnCreateRoom(ChatRoom room)
        {
            // high room
            if (room.Kind != RoomKind.Command_Low)
            {
                ClearRoom(ViewHigh);

                ViewHigh = new RoomView(Chat, room);
                ViewHigh.Dock = DockStyle.Fill;
                ViewContainer.Panel1.Controls.Add(ViewHigh);

                ViewHigh.Init();
                ViewContainer.Panel1Collapsed = false;
            }

            // low room
            else
            {
                ClearRoom(ViewLow);

                ViewLow = new RoomView(Chat, room);
                ViewLow.Dock = DockStyle.Fill;
                ViewContainer.Panel2.Controls.Add(ViewLow);

                ViewLow.Init();
                ViewContainer.Panel2Collapsed = false;
            }

            // collapse unused panels
            if (ViewContainer.Panel1.Controls.Count == 0)
                ViewContainer.Panel1Collapsed = true;

            if (ViewContainer.Panel2.Controls.Count == 0)
                ViewContainer.Panel2Collapsed = true;
        }

        private void OnRemoveRoom(ChatRoom room)
        {

            // high room
            if (room.Kind != RoomKind.Command_Low)
            {
                ClearRoom(ViewHigh);
                ViewContainer.Panel1Collapsed = true;
            }

            // low room
            else
            {
                ClearRoom(ViewLow);
                ViewContainer.Panel2Collapsed = true;
            }
        }
    }
}
