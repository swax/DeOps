using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Interface;


namespace DeOps.Services.Chat
{
    internal partial class InviteForm : CustomIconForm
    {
        OpCore Core;
        ChatService Chat;
        ChatRoom Room;

        internal InviteForm(ChatService chat, ulong user, ChatRoom room)
        {
            InitializeComponent();

            Core = chat.Core;
            Chat = chat;
            Room = room;

            IntroLabel.Text = IntroLabel.Text.Replace("<name>", Core.GetName(user));

            NameLabel.Text = room.Title;

            TypeLabel.Text = room.Kind.ToString();
        }

        private void JoinButton_Click(object sender, EventArgs e)
        {
            Chat.JoinRoom(Room);

            // show the user the transfer starting
            if (Core.GuiMain is MainForm && !((MainForm)Core.GuiMain).SideMode)
                Core.ShowInternal(new ChatView(Chat, 0) { Custom = Room });

            else
            {
                ExternalView view = Core.GuiMain.FindViewType(typeof(ChatView));

                if (view == null)
                    Core.ShowExternal(new ChatView(Chat, 0) { Custom = Room });
                else
                {
                    ((ChatView)view.Shell).SetCustomRoom(Room);

                    view.WindowState = FormWindowState.Normal;
                    view.Activate();
                }
            }

            Close();
        }

        private void IgnoreButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
