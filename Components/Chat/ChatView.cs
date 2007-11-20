using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface;
using DeOps.Interface.Views;


namespace DeOps.Components.Chat
{
    internal partial class ChatView : ViewShell
    {
        ChatControl Chat;
        uint ProjectID;
        bool Custom;

        RoomView ViewHigh;
        RoomView ViewLow;

        internal ChatView(ChatControl chat, uint project, bool custom)
        {
            InitializeComponent();

            Chat = chat;
            ProjectID = project;
            Custom = custom;

            Chat.Refresh += new RefreshHandler(Chat_Refresh);

            toolStrip1.Renderer = new ToolStripProfessionalRenderer(new OpusColorTable());

            ToolSeparator.Visible = false;
            LocalButton.Visible = false;
            LiveButton.Visible = false;
            UntrustedButton.Visible = false;
            JoinButton.Visible = false;

            LocalButton.Checked = true;
        }

        internal override void Init()
        {
            /*Chat.Rooms.LockReading(delegate()
            {
                foreach (ChatRoom room in Chat.Rooms)
                    if (room.ProjectID == ProjectID)
                        OnCreateRoom(room);
            });*/

            Chat_Refresh();
        }

        internal override bool Fin()
        {
            Chat.Refresh -= new RefreshHandler(Chat_Refresh);

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
                ViewHigh = null;
            }

            if (room == ViewLow)
            {
                ViewLow.Fin();
                ViewContainer.Panel2.Controls.Clear();
                ViewLow = null;
            }

        }

        internal override string GetTitle(bool small)
        {
            if (small)
                return "Chat";

            string title = "My ";

            if (ProjectID != 0)
                title += Chat.Core.Links.GetProjectName(ProjectID) + " ";

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

        void Chat_Refresh()
        {
            if (Custom)
                return;

            // startup in chat mode

            // set default checked in init

            // determine if buttons should be removed

            if (LocalButton.Checked)
            {
                AddView(ViewHigh, RoomKind.Command_High);
                AddView(ViewLow, RoomKind.Command_Low);
            }

            if (LiveButton.Checked)
            {
                AddView(ViewHigh, RoomKind.Live_High);
                AddView(ViewLow, RoomKind.Live_Low);
            }

            // if local doesnt exist, remove local/live, and check untrusted
            if (ViewHigh == null && ViewLow == null)
            {
                LocalButton.Visible = false;
                LiveButton.Visible = false;
                UntrustedButton.Checked = true;
            }

            if (UntrustedButton.Checked)
            {
                ChatRoom room = Chat.GetRoom(ProjectID, RoomKind.Untrusted);

                if (room != null)
                {
                    if (ViewHigh == null || ViewHigh.Room.RoomID != room.RoomID)
                        AddRoom(ViewHigh, room);
                }
                else
                    ClearRoom(ViewHigh);

                ClearRoom(ViewLow);

                JoinButton.Visible = true;
                JoinButton.Text = room.Active ? "Leave Room" : "Join Room";
            }

            // collapse unused panels
            if (ViewContainer.Panel1.Controls.Count == 0)
                ViewContainer.Panel1Collapsed = true;

            if (ViewContainer.Panel2.Controls.Count == 0)
                ViewContainer.Panel2Collapsed = true;
        }

        private void AddView(RoomView view, RoomKind kind)
        {
            ChatRoom room = Chat.GetRoom(ProjectID, kind);

            bool show = false;
            if (room != null && room.Active)
                show = true;

            if (!show)
                ClearRoom(view);

            else if (view == null || view.Room.RoomID != room.RoomID)
                AddRoom(view, room);
        }

        private void AddRoom(RoomView view, ChatRoom room)
        {
            ClearRoom(view);

            if (view == ViewHigh)
            {
                ViewHigh = new RoomView(Chat, room);
                ViewHigh.Dock = DockStyle.Fill;
                ViewContainer.Panel1.Controls.Add(ViewHigh);

                ViewHigh.Init();
                ViewContainer.Panel1Collapsed = false;
            }

            // low room
            if(view == ViewLow)
            {
                ViewLow = new RoomView(Chat, room);
                ViewLow.Dock = DockStyle.Fill;
                ViewContainer.Panel2.Controls.Add(ViewLow);

                ViewLow.Init();
                ViewContainer.Panel2Collapsed = false;
            }
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

        private void LocalButton_Click(object sender, EventArgs e)
        {
            Chat_Refresh();
        }

        private void LiveButton_Click(object sender, EventArgs e)
        {
            Chat_Refresh();
        }

        private void UntrustedButton_Click(object sender, EventArgs e)
        {
            Chat_Refresh();
        }
    }
}
