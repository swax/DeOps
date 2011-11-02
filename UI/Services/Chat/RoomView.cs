using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface;
using DeOps.Interface.TLVex;
using DeOps.Interface.Views;
using DeOps.Interface.Views.Res;

using DeOps.Implementation;
using DeOps.Implementation.Transport;

using DeOps.Services.Buddy;
using DeOps.Services.IM;
using DeOps.Services.Location;
using DeOps.Services.Mail;
using DeOps.Services.Trust;
using DeOps.Services.Share;
using DeOps.Services.Voice;


namespace DeOps.Services.Chat
{
    public partial class RoomView : UserControl
    {
        public ChatView ParentView;
        public ChatService Chat;
        public ChatRoom Room;

        public OpCore Core;
        LocationService Locations;

        ToolStripMenuItem TimestampMenu;

        Font BoldFont = new Font("Tahoma", 10, FontStyle.Bold);
        Font RegularFont = new Font("Tahoma", 10, FontStyle.Regular);
        Font TimeFont = new Font("Tahoma", 8, FontStyle.Regular);
        Font SystemFont = new Font("Tahoma", 8, FontStyle.Bold);

        Dictionary<ulong, MemberNode> NodeMap = new Dictionary<ulong, MemberNode>();

        VoiceToolstripButton VoiceButton; 


        public RoomView(ChatView parent, ChatService chat, ChatRoom room)
        {
            InitializeComponent();

            ParentView = parent;
            Chat = chat;
            Room = room;

            Core = chat.Core;
            Locations = Core.Locations;

            GuiUtils.SetupToolstrip(BottomStrip, new OpusColorTable());

            if (room.Kind == RoomKind.Command_High || room.Kind == RoomKind.Live_High)
                MessageTextBox.BackColor = Color.FromArgb(255, 250, 250);

            else if(room.Kind == RoomKind.Command_Low || room.Kind == RoomKind.Live_Low )
                MessageTextBox.BackColor = Color.FromArgb(250, 250, 255);

            MemberTree.PreventCollapse = true;

            MessageTextBox.Core = Core;
            MessageTextBox.ContextMenuStrip.Items.Insert(0, new ToolStripSeparator());

            TimestampMenu = new ToolStripMenuItem("Timestamps", ViewRes.timestamp, new EventHandler(Menu_Timestamps));
            MessageTextBox.ContextMenuStrip.Items.Insert(0, TimestampMenu);

           
            VoiceService voices = Core.GetService(ServiceIDs.Voice) as VoiceService;
            if (voices != null)
            {
                VoiceButton = new VoiceToolstripButton(voices);
                BottomStrip.Items.Add(VoiceButton);
            }
        }

        public void Init()
        {
            InputControl.SendMessage += new TextInput.SendMessageHandler(Input_SendMessage);

            Room.MembersUpdate += new MembersUpdateHandler(Chat_MembersUpdate);
            Room.ChatUpdate    += new ChatUpdateHandler(Chat_Update);

            Chat_MembersUpdate();

            DisplayLog();

            ActiveControl = InputControl.InputBox; 
        }

        public bool Fin()
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

        public void Input_SendMessage(string message, TextFormat format)
        {
            Chat.SendMessage(Room, message, format);
        }

        void Chat_MembersUpdate()
        {
            MemberTree.BeginUpdate();

            MemberTree.Nodes.Clear();
            NodeMap.Clear();

            List<ulong> users = new List<ulong>();

            Room.Members.LockReading(delegate()
            {
                if (Room.Members.SafeCount == 0)
                {
                    MemberTree.EndUpdate();
                    return;
                }

                users = Room.Members.ToList();

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

                foreach (ulong id in Room.Members)
                    if (id != Room.Host)
                    {
                        // if they left the room dont show them
                        if (!ChatService.IsCommandRoom(Room.Kind))
                            if (Room.Members.SafeCount == 0)
                                continue;

                        MemberNode node = new MemberNode(this, id);
                        NodeMap[id] = node;
                        UpdateNode(node);
                        GuiUtils.InsertSubNode(root, node);
                    }

            });

            MemberTree.EndUpdate();

            if (VoiceButton != null)
            {
                AudioDirection direction = AudioDirection.Both;

                if (ParentView.ViewHigh != null && ParentView.ViewLow != null)
                {
                    if (Room.Kind == RoomKind.Command_High || Room.Kind == RoomKind.Live_High)
                        direction = AudioDirection.Left;

                    else if (Room.Kind == RoomKind.Command_Low || Room.Kind == RoomKind.Live_Low)
                        direction = AudioDirection.Right;
                }

                VoiceButton.SetUsers(users, direction);
            }
        }

        void UpdateNode(MemberNode node)
        {
            if (!Room.Members.SafeContains(node.UserID))
            {
                if (node.IsLoopRoot)
                    node.Text = "Trust Loop";

                return;
            }

            // get if node is connected
            bool connected = (Room.Active && 
                            (node.UserID == Core.UserID || Chat.Network.RudpControl.GetActiveSessions(node.UserID).Count > 0));

            // get away status
            bool away = false;


            foreach (ClientInfo info in Core.Locations.GetClients(node.UserID))
                if (info != null && info.Data.Away)
                    away = true;
      

            node.Text = Core.GetName(node.UserID);

            if (away)
                node.Text += " [away]";


            // bold if local
            if (node.UserID == Core.UserID)
                node.Font = new Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));


            // color based on connect status
            Color foreColor = connected ? Color.Black : Color.Gray;

            if (node.ForeColor == foreColor)
            {
                MemberTree.Invalidate();
                return; // no change
            }

            /*if (!node.Unset) // on first run don't show everyone as joined
            {
                string message = "";

                if (connected)
                    message = Core.GetName(node.UserID) + " has joined the room";
                else
                    message = Core.GetName(node.UserID) + " has left the room";


                // dont log
                Chat_Update(new ChatMessage(Core, message, true));
            }*/

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
            else if (Core.Network.Local.Equals(message))
                MessageTextBox.SelectionColor = message.Sent ? Color.Red : Color.LightCoral;
            else
                MessageTextBox.SelectionColor = Color.Blue;

            MessageTextBox.SelectionFont = BoldFont;

            string prefix = " ";
            if (!message.System)
                prefix += Chat.GetNameAndLocation(message);

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
                prefix += ": ";

            MessageTextBox.AppendText(prefix);

            // message, grey for not acked
            MessageTextBox.SelectionColor = Color.Black;
            if (Core.Network.Local.Equals(message) && !message.Sent)
                MessageTextBox.SelectionColor = Color.LightGray;

            if (message.System)
            {
                MessageTextBox.SelectionFont = SystemFont;
                MessageTextBox.AppendText(" *" + message.Text);
            }
            else
            {
                MessageTextBox.SelectionFont = RegularFont;

                if(message.Format == TextFormat.RTF)
                    MessageTextBox.SelectedRtf = GuiUtils.RtftoColor(message.Text, MessageTextBox.SelectionColor);
                else
                    MessageTextBox.AppendText(message.Text);
            }


            MessageTextBox.SelectionStart = oldStart;
            MessageTextBox.SelectionLength = oldLength;

            MessageTextBox.DetectLinksDefault();

            if (!MessageTextBox.Focused)
            {
                MessageTextBox.SelectionStart = MessageTextBox.Text.Length;
                MessageTextBox.ScrollToCaret();
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

            if(Core.Trust != null)
                Core.Trust.Research(node.UserID, 0, false);
            
            Core.Locations.Research(node.UserID);

            ContextMenuStripEx treeMenu = new ContextMenuStripEx();

            // views
            List<MenuItemInfo> quickMenus = new List<MenuItemInfo>();
            List<MenuItemInfo> extMenus = new List<MenuItemInfo>();

            foreach (var service in ParentView.UI.Services.Values)
            {
                if (service is TrustService || service is BuddyService)
                    continue;

                service.GetMenuInfo(InterfaceMenuType.Quick, quickMenus, node.UserID, Room.ProjectID);

                service.GetMenuInfo(InterfaceMenuType.External, extMenus, node.UserID, Room.ProjectID);
            }

            if (quickMenus.Count > 0 || extMenus.Count > 0)
                if (treeMenu.Items.Count > 0)
                    treeMenu.Items.Add("-");

            foreach (MenuItemInfo info in quickMenus)
                treeMenu.Items.Add(new OpMenuItem(node.UserID, Room.ProjectID, info.Path, info));

            if (extMenus.Count > 0)
            {
                ToolStripMenuItem viewItem = new ToolStripMenuItem("Views", InterfaceRes.views);

                foreach (MenuItemInfo info in extMenus)
                    viewItem.DropDownItems.SortedAdd(new OpMenuItem(node.UserID, Room.ProjectID, info.Path, info));

                treeMenu.Items.Add(viewItem);
            }

            // add trust/buddy options at bottom
            quickMenus.Clear();

            ParentView.UI.Services[ServiceIDs.Buddy].GetMenuInfo(InterfaceMenuType.Quick, quickMenus, node.UserID, Room.ProjectID);
            ParentView.UI.Services[ServiceIDs.Trust].GetMenuInfo(InterfaceMenuType.Quick, quickMenus, node.UserID, Room.ProjectID);

            if (quickMenus.Count > 0)
            {
                treeMenu.Items.Add("-");
                foreach (MenuItemInfo info in quickMenus)
                    treeMenu.Items.Add(new OpMenuItem(node.UserID, Room.ProjectID, info.Path, info));
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

            OpMenuItem info = new OpMenuItem(node.UserID, 0);

            if (Locations.ActiveClientCount(node.UserID) > 0)
            {
                var im = ParentView.UI.GetService(ServiceIDs.IM) as IMUI;
                if (im != null)
                    im.OpenIMWindow(info.UserID);
            }
            else
            {
                var mail = ParentView.UI.GetService(ServiceIDs.Mail) as MailUI;
                if (mail != null)
                    mail.OpenComposeWindow(info.UserID);
            }
        }

        void Menu_Timestamps(object sender, EventArgs e)
        {
            TimestampMenu.Checked = !TimestampMenu.Checked;

            DisplayLog();
        }

        private void SendFileButton_Click(object sender, EventArgs e)
        {
            SendFileForm form = new SendFileForm(ParentView.UI, 0);
            form.FileProcessed = new Tuple<FileProcessedHandler, object>(new FileProcessedHandler(Chat.Share_FileProcessed), Room);
            form.ShowDialog();
        }
    }

  

    public class MemberNode : TreeListNode
    {
        OpCore Core;
        public ulong UserID;
        public bool Unset = true;
        public bool IsLoopRoot;

        public MemberNode(RoomView view, ulong id)
        {
            Core = view.Core;
            UserID = id;

            //Update();
        }

        /*public void Update()
        {
            if (IsLoopRoot)
            {
                Text = "Trust Loop";
                return;
            }
            else
                Text = Core.Core.GetName(DhtID);


            if (DhtID == Core.LocalDhtID)
                Font = new Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

            ForeColor = Color.Gray;
        }*/
    }
}
