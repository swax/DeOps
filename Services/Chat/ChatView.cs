using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using RiseOp.Interface;
using RiseOp.Interface.Views;
using RiseOp.Implementation;
using RiseOp.Services.Trust;

namespace RiseOp.Services.Chat
{
    internal partial class ChatView : ViewShell
    {
        ChatService Chat;
        uint ProjectID;
        
        RoomView ViewHigh;
        RoomView ViewLow;
        ChatRoom Custom;

        bool WindowActivated;
        bool FlashMe;


        internal ChatView(ChatService chat, uint project)
        {
            InitializeComponent();

            Chat = chat;
            ProjectID = project;

            Chat.Refresh += new RefreshHandler(Chat_Refresh);
            Chat.Invited += new InvitedHandler(Chat_Invited);

            toolStrip1.Renderer = new ToolStripProfessionalRenderer(new OpusColorTable());

            RoomsButton.Visible  = true;
            RoomSeparator.Visible = true;
            LocalButton.Visible   = true;
            LiveButton.Visible    = true;
            CustomButton.Visible  = false;
            
            JoinButton.Visible   = false;
            LeaveButton.Visible  = false;
            InviteButton.Visible = false; 
        }

        internal override void Init()
        {
            Chat_Refresh();

            //crit command room default, next public rooms, else message showing info
            if (RoomsActive(RoomKind.Command_High, RoomKind.Command_Low))
                LocalButton_Click(null, null);

            if (External != null)
            {
                External.Activated += new EventHandler(External_Activated);
                External.Deactivate += new EventHandler(External_Deactivate);
            }
        }

        internal override bool Fin()
        {
            Chat.Refresh -= new RefreshHandler(Chat_Refresh);

            SetTopView(null, false);
            SetBottomView(null);

            if (External != null)
            {
                External.Activated -= new EventHandler(External_Activated);
                External.Deactivate -= new EventHandler(External_Deactivate);
            }

            return true;
        }

        internal override string GetTitle(bool small)
        {
            if (small)
                return "Chat";

            string title = "";

            if (!CustomButton.Checked)
            {
                title += Chat.Core.Trust.GetProjectName(ProjectID) + " ";

                if (LocalButton.Checked)
                    title += "Local ";
                else if (LiveButton.Checked)
                    title += "Live ";

                title += "Chat";
            }
            else if (Custom != null)
            {
                title = Custom.Title;
            }
            else
                title = "Chat";

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

            // set which buttons are visible/checked


            if (LocalButton.Checked)
            {
                SetTopView( Chat.GetRoom(ProjectID, RoomKind.Command_High), false);
                SetBottomView( Chat.GetRoom(ProjectID, RoomKind.Command_Low));

                JoinButton.Visible = false;
                LeaveButton.Visible = false;
                InviteButton.Visible = false;
            }

            else if (LiveButton.Checked)
            {
                SetTopView(Chat.GetRoom(ProjectID, RoomKind.Live_High), false);
                SetBottomView(Chat.GetRoom(ProjectID, RoomKind.Live_Low));
               
                JoinButton.Visible = false;
                LeaveButton.Visible = false;
                InviteButton.Visible = false;
            }

            else if (CustomButton.Checked)
            {
                SetTopView(Custom, true);
                SetBottomView(null);

                JoinButton.Visible = !Custom.Active;
                LeaveButton.Visible = Custom.Active;

                InviteButton.Visible = (Custom.Active && 
                                        (Custom.Kind == RoomKind.Public || Custom.Host == Chat.Core.UserID));
            }

            LocalButton.ForeColor = RoomsActive(RoomKind.Command_High, RoomKind.Command_Low) ? Color.Black : Color.DimGray;
            LiveButton.ForeColor = RoomsActive(RoomKind.Live_High, RoomKind.Live_Low) ? Color.Black : Color.DimGray;
            
            if(Custom != null)
                CustomButton.ForeColor = Custom.Active ? Color.Black : Color.DimGray;

            // collapse unused panels
            if (ViewContainer.Panel1.Controls.Count == 0)
                ViewContainer.Panel1Collapsed = true;

            if (ViewContainer.Panel2.Controls.Count == 0)
                ViewContainer.Panel2Collapsed = true;

            if (External != null)
                External.Text = GetTitle(false);
        }

        private bool RoomsActive(params RoomKind[] kinds)
        {
            foreach (RoomKind kind in kinds)
            {
                ChatRoom room = Chat.GetRoom(ProjectID, kind);

                if (room != null)
                {
                    if (ChatService.IsCommandRoom(room.Kind ))
                    {
                        // if more people in command room, even if not online then it is active
                        if (room.Members.SafeCount > 1)
                            return true;
                    }
                    else if (room.Active)
                        return true;
                }
            }

            return false;
        }

        void SetTopView(ChatRoom room, bool force)
        {
            if (ViewHigh != null)
            {
                if (ViewHigh.Room == room && (force || room.Members.SafeCount > 1))
                    return;

                ViewHigh.Fin();
                ViewContainer.Panel1.Controls.Clear();
                ViewHigh = null;
            }

            if (room == null || (!force && room.Members.SafeCount <= 1))
                return;

            ViewHigh = new RoomView(this, Chat, room);
            ViewHigh.Dock = DockStyle.Fill;
            ViewContainer.Panel1.Controls.Add(ViewHigh);

            ViewHigh.Init();
            ViewContainer.Panel1Collapsed = false;
        }

        void SetBottomView(ChatRoom room)
        {
            if (ViewLow != null)
            {
                if (ViewLow.Room == room && room.Members.SafeCount > 1)
                    return;

                ViewLow.Fin();
                ViewContainer.Panel2.Controls.Clear();
                ViewLow = null;
            }

            if (room == null || room.Members.SafeCount <= 1)
                return;

            ViewLow = new RoomView(this, Chat, room);
            ViewLow.Dock = DockStyle.Fill;
            ViewContainer.Panel2.Controls.Add(ViewLow);

            ViewLow.Init();
            ViewContainer.Panel2Collapsed = false;
        }

        private void LocalButton_Click(object sender, EventArgs e)
        {
            LocalButton.Checked = true;
            LiveButton.Checked = false;
            CustomButton.Checked = false;

            Chat_Refresh();
        }

        private void LiveButton_Click(object sender, EventArgs e)
        {
            LocalButton.Checked = false;
            LiveButton.Checked = true;
            CustomButton.Checked = false;

            Chat_Refresh();
        }

        private void CustomButton_Click(object sender, EventArgs e)
        {
            LocalButton.Checked = false;
            LiveButton.Checked = false;
            CustomButton.Checked = true;

            Chat_Refresh();
        }

        private void JoinButton_Click(object sender, EventArgs e)
        {
            Chat.Core.RunInCoreBlocked(delegate()
            {
                if (Custom != null)
                    Chat.JoinRoom(Custom);
            });

            Chat.Refresh.Invoke(); // all interfaces need to reflect this
        }

        private void LeaveButton_Click(object sender, EventArgs e)
        {
            if (Custom != null)
                Chat.LeaveRoom(Custom);

            Chat.Refresh.Invoke(); // all interfaces need to reflect this
        }


        private void RoomsButton_DropDownOpening(object sender, EventArgs e)
        {
            RoomsButton.DropDownItems.Clear();

            Chat.RoomMap.LockReading(delegate()
            {
                foreach (ChatRoom room in Chat.RoomMap.Values)
                    if (room.Kind == RoomKind.Public || room.Kind == RoomKind.Private)
                    {
                        RoomsButton.DropDownItems.Add(new RoomItem(Chat, room, RoomMenu_Click));
                    }
            });

            if(RoomsButton.DropDownItems.Count > 0)
                RoomsButton.DropDownItems.Add(new ToolStripSeparator());
            
            RoomsButton.DropDownItems.Add(new ToolStripMenuItem("Create", null, RoomMenu_Create));
        }

        private void RoomMenu_Click(object sender, EventArgs e)
        {
            RoomItem item = sender as RoomItem;

            if (item == null)
                return;

            SetCustomRoom(item.Room);
        }

        private void RoomMenu_Create(object sender, EventArgs e)
        {
            CreateRoom form = new CreateRoom();

            if (form.ShowDialog() == DialogResult.OK)
            {
                string name = form.TitleBox.Text.Trim(' ');

                if (name == "")
                    return;

                RoomKind kind = form.PublicButton.Checked ? RoomKind.Public : RoomKind.Private;

                ChatRoom room = Chat.CreateRoom(name, kind);

                SetCustomRoom(room);
            }
        }

        private void SetCustomRoom(ChatRoom room)
        {
            CustomButton.Visible = true;
            CustomButton.Text = room.Title;
            Custom = room;

            CustomButton_Click(null, null);
        }

        private void InviteButton_Click(object sender, EventArgs e)
        {
            if(Custom == null)
                return;

            AddLinks add = new AddLinks(Chat.Core.Trust, ProjectID);
            add.Text = "Invite People";
            add.AddButton.Text = "Invite";

            if (add.ShowDialog(this) == DialogResult.OK)
                foreach (ulong id in add.People)
                    Chat.SendInviteRequest(Custom, id);
        }


        void Chat_Invited(ulong inviter, ChatRoom room)
        {
            if (MessageBox.Show("You have been invited by " + Chat.Core.GetName(inviter) + " to the room\r\n" + room.Title + "\r\nJoin now?", "Invite", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            Chat.Core.RunInCoreBlocked(delegate()
            {
                Chat.JoinRoom(room);
            });

            SetCustomRoom(room);
        }

        internal void MessageFlash()
        {
            if (External != null && !WindowActivated)
                FlashMe = true;
        }

        void External_Deactivate(object sender, EventArgs e)
        {
            WindowActivated = false;
        }

        void External_Activated(object sender, EventArgs e)
        {
            WindowActivated = true;
            FlashMe = false;
        }

        private void FlashTimer_Tick(object sender, EventArgs e)
        {
            if (External != null && !WindowActivated && FlashMe)
                Win32.FlashWindow(External.Handle, true);
        }
    }


    class RoomItem : ToolStripMenuItem
    {
        internal ChatRoom Room;


        internal RoomItem(ChatService chat, ChatRoom room, EventHandler onClick)
            : base(room.Title + " (" + room.GetActiveMembers(chat).ToString() + ")", null, onClick)
        {
            Room = room;

            if (!room.Active)
                ForeColor = Color.DimGray;
        }
    }
}
