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
        CoreUI UI;
        OpCore Core;
        ChatService Chat;
        ChatRoom Room;

        internal InviteForm(CoreUI ui, ChatService chat, ulong user, ChatRoom room)
        {
            InitializeComponent();

            UI = ui;
            Core = ui.Core;
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
            if (UI.GuiMain is MainForm && !((MainForm)UI.GuiMain).SideMode)
                UI.ShowView(new ChatView(UI, Chat, 0) { Custom = Room }, false);

            else
            {
                ExternalView view = UI.GuiMain.FindViewType(typeof(ChatView));

                if (view == null)
                    UI.ShowView(new ChatView(UI, Chat, 0) { Custom = Room }, true);

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
