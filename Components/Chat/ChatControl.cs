using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Transport;
using DeOps.Components.Link;
using DeOps.Components.Location;

namespace DeOps.Components.Chat
{
    internal delegate void CreateRoomHandler(ChatRoom room);
    internal delegate void RemoveRoomHandler(ChatRoom room);

    internal class ChatControl : OpComponent
    {
        internal OpCore Core;
        internal LinkControl Links;

        internal ThreadedList<ChatRoom> Rooms = new ThreadedList<ChatRoom>();
        ThreadedDictionary<ulong, List<ushort>> ConnectedClients = new ThreadedDictionary<ulong, List<ushort>>();

        internal CreateRoomHandler  CreateRoomEvent;
        internal RemoveRoomHandler  RemoveRoomEvent;
 

        internal ChatControl(OpCore core)
        {
            Core = core;
            Links = core.Links;

            Core.RudpControl.SessionUpdate += new SessionUpdateHandler(Session_Update);
            Core.RudpControl.SessionData[ComponentID.Chat] = new SessionDataHandler(Session_Data);

            Core.LoadEvent += new LoadHandler(Core_Load);
            Core.ExitEvent += new ExitHandler(Core_Exit);
        }

        void Core_Load()
        {
            Links.LinkUpdate += new LinkUpdateHandler(Link_Update);
            Core.Locations.LocationUpdate += new LocationUpdateHandler(Location_Update);

            Link_Update(Links.LocalLink);
        }

        void Core_Exit()
        {
            if (CreateRoomEvent != null || RemoveRoomEvent != null)
                throw new Exception("Chat Events not fin'd");
        }

        internal override List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong key, uint proj)
        {
            if (key != Core.LocalDhtID)
                return null;

            List<MenuItemInfo> menus = new List<MenuItemInfo>();

            if(menuType == InterfaceMenuType.Internal)
                menus.Add(new MenuItemInfo("Comm/Chat", ChatRes.Icon, new EventHandler(Menu_View)));

            if(menuType == InterfaceMenuType.External)
                menus.Add(new MenuItemInfo("Chat", ChatRes.Icon, new EventHandler(Menu_View)));


            return menus;
        }

        void Menu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            if (node.GetKey() != Core.LocalDhtID)
                return;

            // gui creates viewshell, component just passes view object
            ChatView view = new ChatView(this, node.GetProject());

            Core.InvokeView(node.IsExternal(), view);
        }

        internal void Link_Update(OpLink link)
        {
            Links.ProjectRoots.LockReading(delegate()
            {
                foreach (uint project in Links.ProjectRoots.Keys)
                {
                    OpLink uplink = link.GetHigher(project, true);
                    List<OpLink> downlinks = link.GetLowers(project, true);

                    // if us
                    if (link == Links.LocalLink)
                    {
                        // if uplink exists, refresh high room, else remove it
                        if (uplink != null)
                            RefreshRoom(RoomKind.Command_High, project);
                        else
                            RemoveRoom(RoomKind.Command_High, project);

                        // if downlinks exist
                        if (downlinks.Count > 0)
                            RefreshRoom(RoomKind.Command_Low, project);
                        else
                            RemoveRoom(RoomKind.Command_Low, project);
                    }

                    // if not us
                    else
                    {
                        // remove link from whatever room
                        ChatRoom currentRoom = FindRoom(link.DhtID, project);

                        if (currentRoom != null)
                            RefreshRoom(currentRoom.Kind, project);

                        // find where node belongs
                        if (uplink != null && (link == uplink || Links.IsLower(uplink.DhtID, link.DhtID, project)))
                            RefreshRoom(RoomKind.Command_High, project);

                        else if (Links.IsLowerDirect(link.DhtID, project))
                            RefreshRoom(RoomKind.Command_Low, project);
                    }
                }
            });
        }

        private void RefreshRoom(RoomKind kind, uint project)
        {
            OpLink highNode = null;

            if (kind == RoomKind.Command_High)
            {
                highNode = Links.LocalLink.GetHigher(project, true) ;

                if(highNode == null)
                {
                    RemoveRoom(kind, project);
                    return;
                }
            }

            if (kind == RoomKind.Command_Low)
                highNode = Links.LocalLink;

            // ensure top room exists
            ChatRoom room = FindRoom(kind, project);
            bool newRoom = false;

            if (room == null)
            {
                string name = Links.GetProjectName(project);

                room = new ChatRoom(kind, project, name);
                Rooms.SafeAdd(room);

                newRoom = true;
            }

            // remove members
            room.Members.SafeClear();

            // add members
            room.Members.SafeAdd(highNode);

            foreach (OpLink downlink in highNode.GetLowers(project, true))
                room.Members.SafeAdd(downlink);

            if(room.Members.SafeCount == 1)
            {
                RemoveRoom(kind, project);
                return;
            }

            if(newRoom)
                Core.RunInGuiThread(CreateRoomEvent, room);

            Core.RunInGuiThread(room.MembersUpdate, true);
        }
        
        private void RemoveRoom(RoomKind kind, uint id)
        {
            ChatRoom room = FindRoom(kind, id);

            if (room == null)
                return;

            Rooms.SafeRemove(room);

            Core.RunInGuiThread(RemoveRoomEvent, room);
        }

        private ChatRoom FindRoom(RoomKind kind, uint project)
        {
            ChatRoom result = null;

            Rooms.LockReading(delegate()
            {
                foreach (ChatRoom room in Rooms)
                    if (kind == room.Kind && project == room.ProjectID)
                    {
                        result = room;
                        break;
                    }
            });

            return result;
        }

        private List<ChatRoom> FindRoom(ulong key)
        {
            List<ChatRoom> results = new List<ChatRoom>();

            Rooms.LockReading(delegate()
            {
                foreach (ChatRoom room in Rooms)
                    room.Members.LockReading(delegate()
                    {
                        foreach (OpLink member in room.Members)
                            if (member.DhtID == key)
                            {
                                results.Add(room);
                                break;
                            }
                    });
            });

            return results;
        }

        private ChatRoom FindRoom(ulong key, uint project)
        {
            ChatRoom result = null;

            Rooms.LockReading(delegate()
            {
                foreach (ChatRoom room in Rooms)
                    if (room.ProjectID == project)
                    {
                        room.Members.LockReading(delegate()
                        {
                            foreach (OpLink member in room.Members)
                                if (member.DhtID == key)
                                {
                                    result = room;
                                    break;
                                }
                        });

                        break;
                    }
            });

            return result;
        }

        internal void Location_Update(LocationData location)
        {
            // return if node not part of any rooms
            List<ChatRoom> rooms = FindRoom(location.KeyID);

            if (rooms.Count > 0)
                Core.RudpControl.Connect(location); // func checks if already connected
        }

        internal void Session_Update(RudpSession session)
        {
            ulong key = session.DhtID;

            // if node a member of a room
            List<ChatRoom> rooms = FindRoom(key);

            if (rooms.Count == 0)
                return;

            // getstatus message
            string name = Links.GetName(key);

            string location = "";
            if (Core.Locations.ClientCount(session.DhtID ) > 1)
                location = " @" + Core.Locations.GetLocationName(session.DhtID, session.ClientID);


            string message = null;

            List<ushort> connected = null;
            ConnectedClients.TryGetValue(session.DhtID, out connected);

            if (session.Status == SessionStatus.Active &&
                (connected == null || connected.Contains(session.ClientID)))
            {
                message = "Connected to " + name + location;

                if (connected == null)
                {
                    connected = new List<ushort>();
                    ConnectedClients.SafeAdd(session.DhtID, connected);
                }

                connected.Add(session.ClientID);
            }

            if (session.Status == SessionStatus.Closed &&
                connected != null && connected.Contains(session.ClientID))
            {
                message = "Disconnected from " + name + location;

                connected.Remove(session.ClientID);

                if (connected.Count == 0)
                    ConnectedClients.SafeRemove(session.DhtID);
            }

            // update interface
            if(message != null)
                foreach (ChatRoom room in rooms)
                {
                    ProcessMessage(room, new ChatMessage(Core, message, true));

                    Core.RunInGuiThread(room.MembersUpdate, false );
                }
        }

        internal void SendMessage(ChatRoom room, string text)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { SendMessage(room, text); });
                return;
            }

            ProcessMessage(room, new ChatMessage(Core, text, false));

            bool sent = false;

            ChatData packet = new ChatData(ChatPacketType.Message);
            packet.ChatID = room.ProjectID;
            packet.Text = text;
            packet.Custom = (room.Kind == RoomKind.Custom);

            room.Members.LockReading(delegate()
            {
               foreach (OpLink link in room.Members)
                   if (Core.RudpControl.SessionMap.ContainsKey(link.DhtID))
                       foreach (RudpSession session in Core.RudpControl.SessionMap[link.DhtID])
                           if (session.Status == SessionStatus.Active)
                           {
                               sent = true;
                               session.SendData(ComponentID.Chat, packet, true);
                           }
            });

            if (!sent)
                ProcessMessage(room, new ChatMessage(Core, "Message not sent (no one connected to room)", true));
        }

        void Session_Data(RudpSession session, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (Core.Protocol.ReadPacket(root))
            {
                switch (root.Name)
                {
                    case ChatPacket.Data:

                        ChatData packet = ChatData.Decode(Core.Protocol, root);

                        if (packet.Type != ChatPacketType.Message || packet.Custom)
                            return;

                        ChatRoom room = FindRoom(session.DhtID, packet.ChatID);

                        if (room == null)
                            return;
                        
                        ProcessMessage(room, new ChatMessage(Core, session, packet.Text));
        
                        break;
                }
            }
        }

        private void ProcessMessage(ChatRoom room, ChatMessage message)
        {
            room.Log.SafeAdd(message);

            // ask user here if invite to room

            Core.RunInGuiThread(room.ChatUpdate, message);
        }

        internal override void GetActiveSessions( ActiveSessions active)
        {
            Rooms.LockReading(delegate()
            {
                foreach (ChatRoom room in Rooms)
                    foreach (OpLink member in room.Members)
                        active.Add(member.DhtID);
            });
        }
    }


    internal enum RoomKind { None, Command_High, Command_Low, Custom };

    internal delegate void MembersUpdateHandler(bool refresh);
    internal delegate void ChatUpdateHandler(ChatMessage message);

    internal class ChatRoom
    {
        internal uint     ProjectID;
        internal string   Name;
        internal RoomKind Kind;

        internal ThreadedList<ChatMessage> Log = new ThreadedList<ChatMessage>();

        internal ThreadedList<OpLink> Members = new ThreadedList<OpLink>();

        internal MembersUpdateHandler MembersUpdate;
        internal ChatUpdateHandler    ChatUpdate;


        internal ChatRoom(RoomKind kind, uint project, string name)
        {
            Kind = kind;
            ProjectID   = project;
            Name = name;
        }
    }

    internal class ChatMessage
    {
        internal bool       System;
        internal ulong      Source;
        internal ushort     ClientID;
        internal DateTime   TimeStamp;
        internal string     Text;


        internal ChatMessage(OpCore core, string text, bool system)
        {
            Source = core.LocalDhtID;
            ClientID = core.ClientID;
            TimeStamp = core.TimeNow;
            Text = text;
            System = system;
        }

        internal ChatMessage(OpCore core, RudpSession session, string text)
        {
            Source = session.DhtID;
            ClientID = session.ClientID;
            TimeStamp = core.TimeNow;
            Text = text;
        }
    }
}