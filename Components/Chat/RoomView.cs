using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface;
using DeOps.Interface.TLVex;
using DeOps.Implementation;
using DeOps.Components.Link;


namespace DeOps.Components.Chat
{
    internal partial class RoomView : UserControl
    {
        internal ChatControl Chat;
        ChatRoom Room;

        //bool ShowTimestamps;

        Font BoldFont = new Font("Tahoma", 10, FontStyle.Bold);
        Font RegularFont = new Font("Tahoma", 10, FontStyle.Regular);
        Font TimeFont = new Font("Tahoma", 8, FontStyle.Bold);

        internal Dictionary<ulong, string> NameMap = new Dictionary<ulong, string>();


        internal RoomView(ChatControl chat, ChatRoom room)
        {
            InitializeComponent();

            Chat = chat;
            Room = room;


            if (room.Kind == RoomKind.Command_High)
                MessageTextBox.BackColor = Color.FromArgb(255, 250, 250);

            else if(room.Kind == RoomKind.Command_Low)
                MessageTextBox.BackColor = Color.FromArgb(250, 250, 255);
        }

        internal void Init()
        {
            OnMembersUpdate(true);

            InputControl.SendMessage += new TextInput.SendMessageHandler(OnSendMessage);

            Room.MembersUpdate += new MembersUpdateHandler(OnMembersUpdate);
            Room.ChatUpdate    += new ChatUpdateHandler(OnChatUpdate);

            DisplayLog();
        }

        internal bool Fin()
        {
            InputControl.SendMessage -= new TextInput.SendMessageHandler(OnSendMessage);

            Room.MembersUpdate -= new MembersUpdateHandler(OnMembersUpdate);
            Room.ChatUpdate    -= new ChatUpdateHandler(OnChatUpdate);

            return true;
        }

        private void DisplayLog()
        {
            foreach (ChatMessage message in Room.Log)
                OnChatUpdate(message);
        }

        internal void OnSendMessage(string message)
        {
            Chat.SendMessage(Room, message);
        }

        void OnMembersUpdate(bool refresh)
        {
            MemberTree.BeginUpdate();

            if (refresh)
            {
                NameMap.Clear();
                MemberTree.Nodes.Clear();

                if (Room.Members.Count > 0 && Room.Members[0].Name != null)
                {
                    MemberNode root = new MemberNode(this, Room.Members[0]);

                    foreach (OpLink member in Room.Members)
                        if (member != root.Node)
                            Utilities.InsertSubNode(root, new MemberNode(this, member));

                    MemberTree.Nodes.Add(root);
                    root.Expand();
                }
            }

            else
            {
                foreach (MemberNode root in MemberTree.Nodes)
                {
                    root.Update();

                    foreach (MemberNode node in root.Nodes)
                        node.Update();
                }
            }

            MemberTree.EndUpdate();
        }

        void OnChatUpdate(ChatMessage message)
        {
            int oldStart  = MessageTextBox.SelectionStart;
            int oldLength = MessageTextBox.SelectionLength;

            MessageTextBox.SelectionStart = MessageTextBox.Text.Length;
            MessageTextBox.SelectionLength = 0;

            // name, in bold, blue for incoming, red for outgoing
            if (message.System)
                MessageTextBox.SelectionColor = Color.Black;
            else if (message.Source == Chat.Core.LocalDhtID)
                MessageTextBox.SelectionColor = Color.Red;
            else
                MessageTextBox.SelectionColor = Color.Blue;

            MessageTextBox.SelectionFont = BoldFont;

            string prefix = " ";
            if (!message.System)
            {
                if (message.Source == Chat.Core.LocalDhtID)
                    prefix += Chat.Core.User.Settings.ScreenName;
                else if (NameMap.ContainsKey(message.Source))
                    prefix += NameMap[message.Source];
                else
                    prefix += "unknown";
            }

            if (MessageTextBox.Text.Length != 0)
                prefix = "\n" + prefix;

            
            // add timestamp
            /*if (ShowTimestamps)
            {
                MessageTextBox.SelectionFont = TimeFont;
                seperator = " (" + message.TimeStamp.ToString("T") + ")" + seperator;
            }*/


            if (!message.System)
                prefix += ": ";
            else
                prefix += "> ";

            MessageTextBox.AppendText(prefix);

            // message, grey for not acked
            MessageTextBox.SelectionColor = Color.Black;
            MessageTextBox.SelectionFont = RegularFont;

            if (!message.System)
                MessageTextBox.SelectedRtf = message.Text;
            else
            {
                MessageTextBox.SelectionFont = BoldFont;
                MessageTextBox.AppendText(message.Text);
            }

            MessageTextBox.SelectionStart = oldStart;
            MessageTextBox.SelectionLength = oldLength;


            if (InputControl.Focused)
            {
                MessageTextBox.Focus();
                MessageTextBox.SelectionStart = MessageTextBox.Text.Length;
                MessageTextBox.ScrollToCaret();

                InputControl.Focus();
                InputControl.InputBox.Focus();
            }
        }
    }

  

    internal class MemberNode : TreeListNode
    {
        RoomView View;
        OpCore Core;
        internal OpLink Node;

        internal MemberNode(RoomView view, OpLink node)
        {
            View = view;
            Core = view.Chat.Core;
            Node = node;

            Update();
        }

        internal void Update()
        {
            if (Node.Name != null)
                Text = Node.Name;
            else
                Text = "Unknown";

            View.NameMap[Node.DhtID] = Text;

            if (Node == Core.Links.LocalLink)
                Font = new Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        
            else if(Core.RudpControl.IsConnected(Node.DhtID))
                ForeColor = Color.Black;

            else
                ForeColor = Color.DarkGray;
        
        }
    }
}
