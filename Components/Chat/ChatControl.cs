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
    internal delegate void RefreshHandler();


    internal class ChatControl : OpComponent
    {
        internal OpCore Core;
        internal LinkControl Links;

        //internal ThreadedList<ChatRoom> Rooms = new ThreadedList<ChatRoom>();
        internal ThreadedDictionary<uint, ChatRoom> RoomMap = new ThreadedDictionary<uint, ChatRoom>();

        internal Dictionary<ulong, bool> SendUpdates = new Dictionary<ulong, bool>();

        internal RefreshHandler Refresh;


        internal ChatControl(OpCore core)
        {
            Core = core;
            Links = core.Links;

            Core.RudpControl.SessionUpdate += new SessionUpdateHandler(Session_Update);
            Core.RudpControl.SessionData[ComponentID.Chat] = new SessionDataHandler(Session_Data);
            Core.RudpControl.KeepActive += new KeepActiveHandler(Session_KeepActive);

            Core.LoadEvent += new LoadHandler(Core_Load);
            Core.TimerEvent += new TimerHandler(Core_Timer);
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
            if (Refresh != null)
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
            ChatView view = new ChatView(this, node.GetProject(), false);

            Core.InvokeView(node.IsExternal(), view);
        }

        void Core_Timer()
        {
            // send status upates once per second so we're not sending multiple updates to the same client more than
            // once per second

            foreach (ulong key in SendUpdates.Keys)
                foreach (RudpSession session in Core.RudpControl.GetActiveSessions(key))
                    SendStatusUpdate(session);

            SendUpdates.Clear();

        }

        internal void Link_Update(OpLink link)
        {
            // update command/live rooms
            Links.ProjectRoots.LockReading(delegate()
            {
                foreach (uint project in Links.ProjectRoots.Keys)
                {
                    OpLink uplink = Links.LocalLink.GetHigher(project, true);
                    List<OpLink> downlinks = Links.LocalLink.GetLowers(project, true);
                    
                    // if local link updating
                    if (link == Links.LocalLink)
                    {
                        // if we are in the project
                        if (Links.LocalLink.Projects.Contains(project))
                        {
                            if (uplink == null && downlinks.Count == 0)
                                JoinRoom(project, RoomKind.Untrusted);

                            JoinRoom(project, RoomKind.Command_High);
                            JoinRoom(project, RoomKind.Command_Low);
                            JoinRoom(project, RoomKind.Live_High);
                            JoinRoom(project, RoomKind.Live_Low);

                        }

                        // else leave any command/live rooms for this project
                        else
                        {
                            LeaveRooms(project);
                        }
                    }

                    // else if remote user updating
                    else
                    {
                        if(uplink != null)
                            if (uplink == link || uplink.GetLowers(project, true).Contains(link))
                            {
                                RefreshRoom(project, RoomKind.Command_High);
                                RefreshRoom(project, RoomKind.Live_High);
                            }

                        if (downlinks.Contains(link))
                        {
                            RefreshRoom(project, RoomKind.Command_Low);
                            RefreshRoom(project, RoomKind.Live_Low);
                        }

                    }

                    Core.RunInGuiThread(Refresh);

                    // if us
                    /*if (link == Links.LocalLink)
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
                        // check if room should be removed now
                        ChatRoom currentRoom = FindRoom(link.DhtID, project);

                        if (currentRoom != null)
                            RefreshRoom(currentRoom.Kind, project);

                        // check if room should be added now
                        if (Links.IsHigherDirect(link.DhtID, project) || Links.LocalLink.IsLoopedTo(link, project))
                            RefreshRoom(RoomKind.Command_High, project);

                        else if (Links.IsLowerDirect(link.DhtID, project))
                            RefreshRoom(RoomKind.Command_Low, project);
                    }*/
                }
            });

            // refresh member list of any commmand/live room this person is apart of
            // link would already be added above, this ensures user is removed
            foreach(ChatRoom room in FindRoom(link.DhtID))
                RefreshRoom(room);
        }

        void JoinRoom(uint project, RoomKind kind)
        {
            uint id = GetRoomID(project, kind);

            // create if doesnt exist
            ChatRoom room = null;
            if (!RoomMap.SafeTryGetValue(id, out room))
                room = new ChatRoom(kind, project, "");
            
            // activate it
            room.Active = true;

            RoomMap.SafeAdd(id, room);

            RefreshRoom(room);
        }

        void RefreshRoom(uint project, RoomKind kind)
        {
            uint id = GetRoomID(project, kind);

            ChatRoom room = null;
            if (RoomMap.SafeTryGetValue(id, out room))
                RefreshRoom(room);
        }

        void RefreshRoom(ChatRoom room)
        {
            if (!room.Active)
                return;

            // remember connection status from before
            // nodes we arent connected to do try connect
            // if socket already active send status request

            OpLink uplink = Links.LocalLink.GetHigher(room.ProjectID, true);
            
            // updates room's member list

            if (room.Kind == RoomKind.Command_High)
            {
                ThreadedDictionary<ulong, ThreadedList<ushort>> members = new ThreadedDictionary<ulong, ThreadedList<ushort>>();
            
                if (uplink != null)
                {
                    if (uplink.LoopRoot.ContainsKey(room.ProjectID))
                    {
                        uplink = uplink.LoopRoot[room.ProjectID];
                        room.Host = uplink.DhtID; // 0 reserved for no root
                        room.IsLoop = true;
                    }
                    else
                    {
                        room.Host = uplink.DhtID;
                        room.IsLoop = false;

                        ThreadedList<ushort> connected = null;
                        if (!room.Members.SafeTryGetValue(room.Host, out connected))
                            connected = new ThreadedList<ushort>();
                
                        members.SafeAdd(room.Host, connected);
                    }

                    foreach (OpLink downlink in uplink.GetLowers(room.ProjectID, true))
                    {
                        ThreadedList<ushort> connected = null;
                        if (!room.Members.SafeTryGetValue(downlink.DhtID, out connected))
                            connected = new ThreadedList<ushort>();
    
                        if(downlink.DhtID == Core.LocalDhtID && !connected.SafeContains(Core.ClientID))
                            connected.SafeAdd(Core.ClientID);
                        
                        members.SafeAdd(downlink.DhtID, connected);
                    }
                }

                room.Members = members;
                room.Active = (members.SafeCount > 1);
            }

            else if (room.Kind == RoomKind.Command_Low)
            {
                ThreadedDictionary<ulong, ThreadedList<ushort>> members = new ThreadedDictionary<ulong, ThreadedList<ushort>>();
            
                ThreadedList<ushort> connected = null;
                if(!room.Members.SafeTryGetValue(Core.LocalDhtID, out connected))
                    connected = new ThreadedList<ushort>();
                
                if (!connected.SafeContains(Core.ClientID))
                    connected.SafeAdd(Core.ClientID);

                members.SafeAdd(room.Host, connected);

                if(uplink != null)
                    foreach (OpLink downlink in uplink.GetLowers(room.ProjectID, true))
                    {
                        connected = new ThreadedList<ushort>();
                        members.SafeTryGetValue(downlink.DhtID, out connected);
                        members.SafeAdd(downlink.DhtID, connected);
                    }

                room.Members = members;
                room.Active = (members.SafeCount > 1);
            }

            else if (room.Kind == RoomKind.Live_High)
            {
                // find highest thats online and make that the host,

                // if host changes, clear members

                // higher should send live lowers which members are conneted to it so everyone can sync up
                // location update should trigger a refresh of the live rooms
            }

            else if (room.Kind == RoomKind.Live_Low)
            {
                // just add self, dont remove members
            }

            else if (room.Kind == RoomKind.Untrusted)
            {
                // don't remove previous connections, let that happen on demand
                // if someone chatting in untrusted and they link up, dont cut their conversation short

                ThreadedList<ushort> connected = null;
                if(!room.Members.SafeTryGetValue(Core.LocalDhtID, out connected))
                    connected = new ThreadedList<ushort>();
                
                if (connected.SafeCount == 0)
                    connected.SafeAdd(Core.ClientID);
    
                room.Members.SafeAdd(Core.LocalDhtID, connected);


                List<OpLink> roots = null;
                if (Links.ProjectRoots.SafeTryGetValue(room.ProjectID, out roots))
                    foreach (OpLink root in roots)
                        if (root.GetLowers(room.ProjectID, true).Count == 0 &&
                            !room.Members.SafeContainsKey(root.DhtID))
                        {
                            room.Members.SafeAdd(root.DhtID, new ThreadedList<ushort>());
                        }

            }

            else if (room.Kind == RoomKind.Public)
            {

            }

            else if (room.Kind == RoomKind.Private)
            {


            }
            // ensure connected or tried connected to members in the room
            room.Members.LockReading(delegate()
            {
                foreach (ulong key in room.Members.Keys)
                {
                    foreach (LocInfo info in Core.Locations.GetClients(key))
                        Core.RudpControl.Connect(info.Location);

                    SendUpdates[key] = true;
                }
            });

            // update dispaly that members has been refreshed
            Core.RunInGuiThread(room.MembersUpdate);
        }

        void LeaveRooms(uint project)
        {
            LeaveRoom(project, RoomKind.Untrusted);
            LeaveRoom(project, RoomKind.Command_High);
            LeaveRoom(project, RoomKind.Command_Low);
            LeaveRoom(project, RoomKind.Live_High);
            LeaveRoom(project, RoomKind.Live_Low);
        }

        void LeaveRoom(uint project, RoomKind kind)
        {
            // deactivates room, let timer remove object is good once we know user no longer wants it

            uint id = GetRoomID(project, kind);

            ChatRoom room = null;
            if (!RoomMap.SafeTryGetValue(id, out room))
                return;

            room.Active = false;

            room.Members.LockReading(delegate()
            {
                foreach (ulong key in room.Members.Keys)
                    SendUpdates[key] = true;
            });

            // if user leaves/rejoins anywhere, previous messages should be saved

            //update interface
            Core.RunInGuiThread(room.MembersUpdate);
        }


        internal uint GetRoomID(uint project, RoomKind kind)
        {
            return project + (uint)kind;
        }

        internal ChatRoom GetRoom(uint project, RoomKind kind)
        {
            ChatRoom room = null;
            if(RoomMap.TryGetValue(GetRoomID(project, kind), out room))
                return room;

            return null;
        }

        /*private void RefreshRoom(RoomKind kind, uint project)
        {
            // ensure room exists
            ChatRoom room = FindRoom(kind, project);
            bool newRoom = false;

            if (room == null)
            {
                string name = Links.GetProjectName(project);
                room = new ChatRoom(kind, project, name);
                Rooms.SafeAdd(room);
                newRoom = true;
            }

            // get root node
            OpLink highNode = null;

            if (kind == RoomKind.Command_High)
            {
                if (Links.LocalLink.LoopRoot.ContainsKey(project))
                {
                    highNode = Links.LocalLink.LoopRoot[project];
                    room.IsLoop = true;
                }
                else
                    highNode = Links.LocalLink.GetHigher(project, true);

                if (highNode == null)
                {
                    RemoveRoom(kind, project);
                    return;
                }
            }

            if (kind == RoomKind.Command_Low)
                highNode = Links.LocalLink;


            // refresh members
            room.Members.SafeClear();

            room.Members.SafeAdd(highNode.DhtID);

            foreach (OpLink downlink in highNode.GetLowers(project, true))
                room.Members.SafeAdd(downlink.DhtID);

            if(room.Members.SafeCount == 1)
            {
                RemoveRoom(kind, project);
                return;
            }

            if(newRoom)
                Core.RunInGuiThread(CreateRoomEvent, room);

            Core.RunInGuiThread(room.MembersUpdate);
        }*/
        
        /*private void RemoveRoom(RoomKind kind, uint id)
        {
            ChatRoom room = FindRoom(kind, id);

            if (room == null)
                return;

            Rooms.SafeRemove(room);

            Core.RunInGuiThread(RemoveRoomEvent, room);
        }*/

        /*private ChatRoom FindRoom(RoomKind kind, uint project)
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
        }*/

        private List<ChatRoom> FindRoom(ulong key)
        {
            List<ChatRoom> results = new List<ChatRoom>();

            RoomMap.LockReading(delegate()
            {
                foreach (ChatRoom room in RoomMap.Values)
                    if(room.Members.SafeContainsKey(key))
                        results.Add(room);
            });

            return results;
        }

        /*private ChatRoom FindRoom(ulong key, uint project)
        {
            ChatRoom result = null;

            Rooms.LockReading(delegate()
            {
                foreach (ChatRoom room in Rooms)
                    if (room.ProjectID == project)
                    {
                        room.Members.LockReading(delegate()
                        {
                            foreach (ulong member in room.Members)
                                if (member == key)
                                {
                                    result = room;
                                    break;
                                }
                        });

                        break;
                    }
            });

            return result;
        }*/

        internal void Location_Update(LocationData location)
        {
            bool connect = false;

            foreach (ChatRoom room in FindRoom(location.KeyID))
                if (room.Active)
                    connect = true;

            if(connect)
                Core.RudpControl.Connect(location); // func checks if already connected
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

            ChatText packet = new ChatText();
            packet.RoomID = room.RoomID;
            packet.Text = text;

            room.Members.LockReading(delegate()
            {
               foreach (ulong member in room.Members.Keys)
                   foreach (RudpSession session in Core.RudpControl.GetActiveSessions(member))
                   {
                       sent = true;
                       session.SendData(ComponentID.Chat, packet, true);
                   }
            });

            if (!sent)
                ProcessMessage(room, new ChatMessage(Core, "Could not send message, not connected to anyone", true));
        }


        internal void Session_Update(RudpSession session)
        {

            // send node rooms that we have in common
            if (session.Status == SessionStatus.Active)
                SendStatusUpdate(session);

            List<RudpSession> active = Core.RudpControl.GetActiveSessions(session.DhtID);

            // if disconnected
            if (session.Status == SessionStatus.Closed)
                foreach (ChatRoom room in FindRoom(session.DhtID))
                {
                    room.RemoveMember(session.DhtID, session.ClientID);
                    Core.RunInGuiThread(room.MembersUpdate, room);
                }
        }

        private void SendStatusUpdate(RudpSession session)
        {
            // send even if empty so they know to remove us

            List<ChatRoom> rooms = FindRoom(session.DhtID);

            ChatStatus status = new ChatStatus();

            foreach (ChatRoom room in rooms)
                if (room.Active)
                    status.ActiveRooms.Add(room.RoomID);

            session.SendData(ComponentID.Chat, status, true);
        }


        void Session_Data(RudpSession session, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (Core.Protocol.ReadPacket(root))
            {
                if (root.Name == ChatPacket.Data)
                {
                    ChatText packet = ChatText.Decode(Core.Protocol, root);

                    ChatRoom room = null;
                    if (!RoomMap.TryGetValue(packet.RoomID, out room))
                        return;

                    ProcessMessage(room, new ChatMessage(Core, session, packet.Text));

                }
                else if (root.Name == ChatPacket.Status)
                {
                    ChatStatus status = ChatStatus.Decode(Core.Protocol, root);

                    List<ChatRoom> rooms = FindRoom(session.DhtID);

                    // update online status for rooms node is in
                    ThreadedList<ushort> connected = null;

                    foreach (ChatRoom room in rooms)
                    {
                        bool update = true;
                        bool inRoom = status.ActiveRooms.Contains(room.RoomID);
                        
                        // member not in room
                        if(!inRoom)
                            room.RemoveMember(session.DhtID, session.ClientID);
                        
                        // member listed in room
                        else if (room.Members.SafeTryGetValue(session.DhtID, out connected))
                        {
                            if (!connected.SafeContains(session.ClientID))
                                connected.SafeAdd(session.ClientID);
                            else
                                update = false;

                            room.Members.SafeAdd(session.DhtID, connected); // update list
                        }

                        // member unlisted in our records for room
                        // if room is untrusted, or custom/public allow user to enter, send response as well
                        else if(room.Kind == RoomKind.Untrusted || room.Kind == RoomKind.Public)
                        {
                            connected = new ThreadedList<ushort>();
                            connected.SafeAdd(session.ClientID);
                            room.Members.SafeAdd(session.DhtID, connected);
                        }

                        if(update)
                            Core.RunInGuiThread(room.MembersUpdate);
                    }
                }
            }
        }

        void Session_KeepActive(Dictionary<ulong, bool> active)
        {
            RoomMap.LockReading(delegate()
            {
                foreach (ChatRoom room in RoomMap.Values)
                    if(room.Active)
                        room.Members.LockReading(delegate()
                        {
                            foreach (ulong member in room.Members.Keys)
                                active[member] = true;
                        });
            });
        }


        private void ProcessMessage(ChatRoom room, ChatMessage message)
        {
            room.Log.SafeAdd(message);

            // ask user here if invite to room

            Core.RunInGuiThread(room.ChatUpdate, message);
        }

    }


    internal enum RoomKind { Command_High, Command_Low, Live_High, Live_Low, Untrusted, Public, Private  }; // do not change order


    internal delegate void MembersUpdateHandler();
    internal delegate void ChatUpdateHandler(ChatMessage message);

    internal class ChatRoom
    {
        internal uint     RoomID;
        internal uint     ProjectID;
        internal string   Name;
        internal RoomKind Kind;
        internal bool     IsLoop;
        internal bool     Active;

        internal ulong Host;
        internal ThreadedDictionary<ulong, ThreadedList<ushort>> Members = new ThreadedDictionary<ulong, ThreadedList<ushort>>();

        internal ThreadedList<ChatMessage> Log = new ThreadedList<ChatMessage>();

        internal MembersUpdateHandler MembersUpdate;
        internal ChatUpdateHandler    ChatUpdate;

        // per channel polling needs to be done because client may be still connected, leaving one channel, joining another


        internal ChatRoom(RoomKind kind, uint project, string name)
        {
            RoomID = project + (uint)kind;
            Kind = kind;
            ProjectID   = project;
            Name = name;
        }

        internal void RemoveMember(ulong dhtid, ushort client)
        {
            ThreadedList<ushort> connected = null;

            if (!Members.SafeTryGetValue(dhtid, out connected))
                return;

            if (connected.SafeContains(client))
                connected.SafeRemove(client);

            Members.SafeAdd(dhtid, connected); // update list

            // remove member himself if no connections, and not a command room 
            if (connected.SafeCount == 0 && (Kind == RoomKind.Untrusted || Kind == RoomKind.Public || Kind == RoomKind.Private))
                Members.SafeRemove(dhtid);
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