using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface;
using DeOps.Interface.TLVex;
using DeOps.Interface.Views;
using DeOps.Implementation;
using DeOps.Implementation.Transport;
using DeOps.Components.Link;
using DeOps.Components.IM;
using DeOps.Components.Location;


namespace DeOps.Components.Chat
{
    internal partial class RoomView : UserControl
    {
        internal ChatView ParentView;
        internal ChatControl Chat;
        internal ChatRoom Room;

        internal OpCore Core;
        LocationControl Locations;
        LinkControl Links;

        MenuItem TimestampMenu;

        Font BoldFont = new Font("Tahoma", 10, FontStyle.Bold);
        Font RegularFont = new Font("Tahoma", 10, FontStyle.Regular);
        Font TimeFont = new Font("Tahoma", 8, FontStyle.Regular);
        Font SystemFont = new Font("Tahoma", 8, FontStyle.Bold);

        Dictionary<ulong, MemberNode> NodeMap = new Dictionary<ulong, MemberNode>();


        internal RoomView(ChatView parent, ChatControl chat, ChatRoom room)
        {
            InitializeComponent();

            ParentView = parent;
            Chat = chat;
            Room = room;

            Core = chat.Core;
            Locations = Core.Locations;
            Links = Core.Links;

            if (room.Kind == RoomKind.Command_High || room.Kind == RoomKind.Live_High)
                MessageTextBox.BackColor = Color.FromArgb(255, 250, 250);

            else if(room.Kind == RoomKind.Command_Low || room.Kind == RoomKind.Live_Low )
                MessageTextBox.BackColor = Color.FromArgb(250, 250, 255);

            MemberTree.PreventCollapse = true;

            ContextMenu menu = new ContextMenu();
            TimestampMenu = new MenuItem("Timestamps", new EventHandler(Menu_Timestamps));
            menu.MenuItems.Add(TimestampMenu);
            MessageTextBox.ContextMenu = menu;
        }

        internal void Init()
        {
            InputControl.SendMessage += new TextInput.SendMessageHandler(Input_SendMessage);

            Room.MembersUpdate += new MembersUpdateHandler(Chat_MembersUpdate);
            Room.ChatUpdate    += new ChatUpdateHandler(Chat_Update);

            Chat_MembersUpdate();

            DisplayLog();
        }

        internal bool Fin()
        {
            InputControl.SendMessage -= new TextInput.SendMessageHandler(Input_SendMessage);

            Room.MembersUpdate -= new MembersUpdateHandler(Chat_MembersUpdate);
            Room.ChatUpdate    -= new ChatUpdateHandler(Chat_Update);

            return true;
        }

        private void DisplayLog()
        {
            MessageTextBox.Clear();

            Room.Log.LockReading(delegate()
            {
                foreach (ChatMessage message in Room.Log)
                    Chat_Update(message);
            });
        }

        internal void Input_SendMessage(string message)
        {
            Chat.SendMessage(Room, message);
        }

        void Chat_MembersUpdate()
        {
            MemberTree.BeginUpdate();

            MemberTree.Nodes.Clear();
            NodeMap.Clear();

            Room.Members.LockReading(delegate()
            {
                if (Room.Members.SafeCount == 0)
                {
                    MemberTree.EndUpdate();
                    return;
                }

                TreeListNode root = MemberTree.virtualParent;

                if (Room.Host != 0)
                {
                    root = new MemberNode(this, Room.Host);

                    if (Room.IsLoop)
                        ((MemberNode)root).IsLoopRoot = true;
                    else
                        NodeMap[Room.Host] = root as MemberNode;
                    
                    UpdateNode(root as MemberNode);

                    MemberTree.Nodes.Add(root);
                    root.Expand();
                }

                Room.Members.LockReading(delegate()
                {
                    foreach (ulong id in Room.Members.Keys)
                        if (id != Room.Host)
                        {
                            // if they left the room dont show them
                            if (Room.Kind == RoomKind.Public || Room.Kind == RoomKind.Private)
                                if (Room.Members[id].SafeCount == 0)
                                    continue; 

                            MemberNode node = new MemberNode(this, id);
                            Utilities.InsertSubNode(root, node);
                            UpdateNode(node);
                            NodeMap[id] = node;
                        }
                });
            });

            MemberTree.EndUpdate();
        }

        void UpdateNode(MemberNode node)
        {

            ThreadedList<ushort> clients = null;
            if (!Room.Members.TryGetValue(node.DhtID, out clients))
            {
                if (node.IsLoopRoot)
                    node.Text = "Trust Loop";

                return;
            }

            // get if node is connected
            bool connected = (Room.Active && clients.SafeCount > 0);

            // get away status
            bool away = false;
            clients.LockReading(delegate()
            {
                foreach (ushort client in clients)
                {
                    LocInfo info = Locations.GetLocationInfo(node.DhtID, client);

                    if (info != null && info.Location.Away)
                        away = true;
                }
            });

            node.Text = Links.GetName(node.DhtID);

            if (away)
                node.Text += " (away)";

            // color based on connect status
            Color foreColor = connected ? Color.Black : Color.Gray;

            if (node.ForeColor == foreColor)
            {
                MemberTree.Invalidate();
                return; // no change
            }

            if (!node.Unset) // on first run don't show everyone as joined
            {
                string message = "";

                if (connected)
                    message = Links.GetName(node.DhtID) + " has joined the room";
                else
                    message = Links.GetName(node.DhtID) + " has left the room";


                // dont log
                Chat_Update(new ChatMessage(Core, message, true));
            }

            node.Unset = false;

            node.ForeColor = foreColor;
            MemberTree.Invalidate();
        }

        void Chat_Update(ChatMessage message)
        {
            int oldStart  = MessageTextBox.SelectionStart;
            int oldLength = MessageTextBox.SelectionLength;

            MessageTextBox.SelectionStart = MessageTextBox.Text.Length;
            MessageTextBox.SelectionLength = 0;

            // name, in bold, blue for incoming, red for outgoing
            if (message.System)
                MessageTextBox.SelectionColor = Color.Black;
            else if (message.Source == Chat.Core.LocalDhtID && message.ClientID == Core.ClientID)
                MessageTextBox.SelectionColor = Color.Red;
            else
                MessageTextBox.SelectionColor = Color.Blue;

            MessageTextBox.SelectionFont = BoldFont;

            string prefix = " ";
            if (!message.System)
                prefix += Links.GetName(message.Source);

            if (MessageTextBox.Text.Length != 0)
                prefix = "\n" + prefix;

            
            // add timestamp
            if (TimestampMenu.Checked)
            {
                MessageTextBox.AppendText(prefix);

                MessageTextBox.SelectionFont = TimeFont;
                MessageTextBox.AppendText(" (" + message.TimeStamp.ToString("T") + ")");

                MessageTextBox.SelectionFont = BoldFont;
                prefix = "";
            }

            if (!message.System)
                prefix += Chat.LocationSuffix(message.Source, message.ClientID) + ": ";

            MessageTextBox.AppendText(prefix);

            // message, grey for not acked
            MessageTextBox.SelectionColor = Color.Black;

            if (message.System)
            {
                MessageTextBox.SelectionFont = SystemFont;
                MessageTextBox.AppendText(" *" + message.Text);
            }
            else
            {
                MessageTextBox.SelectionFont = RegularFont;
                MessageTextBox.SelectedRtf = message.Text;
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

            ParentView.MessageFlash();
        }

        private void MemberTree_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            MemberNode node = MemberTree.GetNodeAt(e.Location) as MemberNode;

            if (node == null)
                return;

            ContextMenuStripEx treeMenu = new ContextMenuStripEx();

            // views
            List<ToolStripMenuItem> quickMenus = new List<ToolStripMenuItem>();
            List<ToolStripMenuItem> extMenus = new List<ToolStripMenuItem>();

            foreach (OpComponent component in Core.Components.Values)
            {
                if (component is LinkControl)
                    continue;

                // quick
                List<MenuItemInfo> menuList = component.GetMenuInfo(InterfaceMenuType.Quick, node.DhtID, Room.ProjectID);

                if (menuList != null && menuList.Count > 0)
                    foreach (MenuItemInfo info in menuList)
                        quickMenus.Add(new OpMenuItem(node.DhtID, Room.ProjectID, info.Path, info));

                // external
                menuList = component.GetMenuInfo(InterfaceMenuType.External, node.DhtID, Room.ProjectID);

                if (menuList != null && menuList.Count > 0)
                    foreach (MenuItemInfo info in menuList)
                        extMenus.Add(new OpMenuItem(node.DhtID, Room.ProjectID, info.Path, info));
            }

            if (quickMenus.Count > 0 || extMenus.Count > 0)
                if (treeMenu.Items.Count > 0)
                    treeMenu.Items.Add("-");

            foreach (ToolStripMenuItem menu in quickMenus)
                treeMenu.Items.Add(menu);

            if (extMenus.Count > 0)
            {
                ToolStripMenuItem viewItem = new ToolStripMenuItem("Views", InterfaceRes.views);

                foreach (ToolStripMenuItem menu in extMenus)
                    viewItem.DropDownItems.Add(menu);

                treeMenu.Items.Add(viewItem);
            }

            // show
            if (treeMenu.Items.Count > 0)
                treeMenu.Show(MemberTree, e.Location);
        }

        private void MemberTree_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            MemberNode node = MemberTree.GetNodeAt(e.Location) as MemberNode;

            if (node == null)
                return;

            OpMenuItem info = new OpMenuItem(node.DhtID, 0);

            if (Locations.LocationMap.SafeContainsKey(node.DhtID))
                ((IMControl)Core.Components[ComponentID.IM]).QuickMenu_View(info, null);
            else
                Core.Mail.QuickMenu_View(info, null);
        }

        void Menu_Timestamps(object sender, EventArgs e)
        {
            TimestampMenu.Checked = !TimestampMenu.Checked;

            DisplayLog();
        }
    }

  

    internal class MemberNode : TreeListNode
    {
        OpCore Core;
        internal ulong DhtID;
        internal bool Unset = true;
        internal bool IsLoopRoot;

        internal MemberNode(RoomView view, ulong id)
        {
            Core = view.Core;
            DhtID = id;

            //Update();
        }

        /*internal void Update()
        {
            if (IsLoopRoot)
            {
                Text = "Trust Loop";
                return;
            }
            else
                Text = Core.Links.GetName(DhtID);


            if (DhtID == Core.LocalDhtID)
                Font = new Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

            ForeColor = Color.Gray;
        }*/
    }
}
