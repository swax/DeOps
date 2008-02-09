using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Transport;
using RiseOp.Services.Trust;
using RiseOp.Services.Location;

namespace RiseOp.Services.Chat
{
    internal delegate void RefreshHandler();
    internal delegate void InvitedHandler(ulong inviter, ChatRoom room);

    internal class ChatService : OpService
    {
        public string Name { get { return "Chat"; } }
        public ushort ServiceID { get { return 6; } }

        internal OpCore Core;
        internal TrustService Links;

        //internal ThreadedList<ChatRoom> Rooms = new ThreadedList<ChatRoom>();
        internal ThreadedDictionary<uint, ChatRoom> RoomMap = new ThreadedDictionary<uint, ChatRoom>();

        internal Dictionary<ulong, bool> StatusUpdate = new Dictionary<ulong, bool>();

        internal RefreshHandler Refresh;
        internal InvitedHandler Invited;


        internal ChatService(OpCore core)
        {
            Core = core;
            Links = core.Links;

            Core.RudpControl.SessionUpdate += new SessionUpdateHandler(Session_Update);
            Core.RudpControl.SessionData[ServiceID, 0] += new SessionDataHandler(Session_Data);
            Core.RudpControl.KeepActive += new KeepActiveHandler(Session_KeepActive);

            Core.SecondTimerEvent += new TimerHandler(Core_SecondTimer);
            Core.GetFocusedCore += new GetFocusedHandler(Core_GetFocusedCore);

            Links.LinkUpdate += new LinkUpdateHandler(Link_Update);
            Core.Locations.LocationUpdate += new LocationUpdateHandler(Location_Update);

            Link_Update(Links.LocalTrust);
        }

        public void Dispose()
        {
            if (Refresh != null)
                throw new Exception("Chat Events not fin'd");

            Core.RudpControl.SessionUpdate -= new SessionUpdateHandler(Session_Update);
            Core.RudpControl.SessionData[ServiceID, 0] -= new SessionDataHandler(Session_Data);
            Core.RudpControl.KeepActive -= new KeepActiveHandler(Session_KeepActive);

            Core.SecondTimerEvent -= new TimerHandler(Core_SecondTimer);
            Core.GetFocusedCore -= new GetFocusedHandler(Core_GetFocusedCore);

            Links.LinkUpdate -= new LinkUpdateHandler(Link_Update);
            Core.Locations.LocationUpdate -= new LocationUpdateHandler(Location_Update);
        }

        public List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong key, uint proj)
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
            ChatView view = new ChatView(this, node.GetProject(), null);

            Core.InvokeView(node.IsExternal(), view);
        }

        void Core_GetFocusedCore()
        {
            RoomMap.LockReading(delegate()
            {
                foreach (ChatRoom room in RoomMap.Values)
                    if (room.Active && !IsCommandRoom(room.Kind))
                    {
                        room.Members.LockReading(delegate()
                        {
                            foreach (ulong id in room.Members.Keys)
                                Core.Focused.SafeAdd(id, true);
                        });

                        if(room.Invites != null)
                            foreach(ulong id in room.Invites.Keys)
                                Core.Focused.SafeAdd(id, true);

                        if (room.Verified != null)
                            foreach(ulong id in room.Verified.Keys)
                                Core.Focused.SafeAdd(id, true);
                    }
            });
        }

        void Core_SecondTimer()
        {
            // send status upates once per second so we're not sending multiple updates to the same client more than
            // once per second

            foreach (ulong key in StatusUpdate.Keys)
                foreach (RudpSession session in Core.RudpControl.GetActiveSessions(key))
                    SendStatus(session);

            StatusUpdate.Clear();

        }

        internal void Link_Update(OpTrust trust)
        {

            // update command/live rooms
            Links.ProjectRoots.LockReading(delegate()
            {
                foreach (uint project in Links.ProjectRoots.Keys)
                {
                    OpLink localLink = Links.LocalTrust.GetLink(project);
                    OpLink remoteLink = trust.GetLink(project);

                    if (localLink == null || remoteLink == null)
                        continue;

                    OpLink uplink = localLink.GetHigher(true);
                    List<OpLink> downlinks = localLink.GetLowers(true);
                    
                    // if local link updating
                    if (trust == Links.LocalTrust)
                    {
                        // if we are in the project
                        if (localLink.Active)
                        {
                            JoinCommand(project, RoomKind.Command_High);
                            JoinCommand(project, RoomKind.Command_Low);
                            JoinCommand(project, RoomKind.Live_High);
                            JoinCommand(project, RoomKind.Live_Low);

                            //RoomMap.SafeAdd(GetRoomID(project, RoomKind.Untrusted), new ChatRoom(RoomKind.Untrusted, project, ""));
                            //if(uplink == null && downlinks.Count == 0)
                            //    JoinCommand(project, RoomKind.Untrusted);
     
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
                            if (uplink.Trust == trust || uplink.GetLowers(true).Contains(remoteLink))
                            {
                                RefreshCommand(project, RoomKind.Command_High);
                                RefreshCommand(project, RoomKind.Live_High);
                            }

                        if (downlinks.Contains(remoteLink))
                        {
                            RefreshCommand(project, RoomKind.Command_Low);
                            RefreshCommand(project, RoomKind.Live_Low);
                        }

                        if (remoteLink.GetHigher(true) == null)
                            RefreshCommand(project, RoomKind.Untrusted);
                    }

                    Core.RunInGuiThread(Refresh);
                }
            });

            // refresh member list of any commmand/live room this person is apart of
            // link would already be added above, this ensures user is removed
            foreach(ChatRoom room in FindRoom(trust.DhtID))
                if(IsCommandRoom(room.Kind))
                    RefreshCommand(room);
                else if(room.Members.SafeContainsKey(trust.DhtID))
                    Core.RunInGuiThread(room.MembersUpdate);
        }

        internal ChatRoom CreateRoom(string name, RoomKind kind)
        {
            // create room
            ChatRoom room = new ChatRoom(kind, 0, name);

            uint id = (uint) Core.RndGen.Next();

            room.Active = true;
            room.AddMember(Core.LocalDhtID, Core.ClientID);

            RoomMap.SafeAdd(id, room);
            
            if (kind == RoomKind.Private)
            {
                room.Host = Core.LocalDhtID;
                room.Verified[Core.LocalDhtID] = true;
                SendInviteRequest(room, Core.LocalDhtID); // send invite to copies of ourself that exist
            }

            Core.RunInGuiThread(Refresh);

            return room;    
        }

        internal void ShowRoom(ChatRoom room)
        {
            if (room.MembersUpdate != null)
                foreach (Delegate func in room.MembersUpdate.GetInvocationList())
                    if (func.Target is RoomView)
                        if (((RoomView)func.Target).ParentView.External != null)
                        {
                            ((RoomView)func.Target).ParentView.External.BringToFront();
                            return;
                        }

            Core.InvokeView(true, new ChatView(this, room.ProjectID, room));
        }

        internal void JoinRoom(ChatRoom room)
        {
            if (room.Kind != RoomKind.Public && room.Kind != RoomKind.Private)
            {
                JoinCommand(room.ProjectID, room.Kind);
                return;
            }

            room.Active = true;
            room.AddMember(Core.LocalDhtID, Core.ClientID);

            // for private rooms, send proof of invite first
            if (room.Kind == RoomKind.Private)
                SendInviteProof(room);

            SendStatus(room);

            SendWhoRequest(room);

            ConnectRoom(room);
        }

        internal void JoinCommand(uint project, RoomKind kind)
        {
            uint id = GetRoomID(project, kind);

            // create if doesnt exist
            ChatRoom room = null;

            if (!RoomMap.SafeTryGetValue(id, out room))
                room = new ChatRoom(kind, project, "");

            room.Active = true;
            room.AddMember(Core.LocalDhtID, Core.ClientID);

            RoomMap.SafeAdd(id, room);

            RefreshCommand(room);

            SendStatus(room);

            if (kind == RoomKind.Untrusted)
                SendWhoRequest(room);

            ConnectRoom(room);
        }

        private void ConnectRoom(ChatRoom room)
        {
            room.Members.LockReading(delegate()
            {
                foreach (ulong key in room.Members.Keys)
                {
                    foreach (ClientInfo info in Core.Locations.GetClients(key))
                        Core.RudpControl.Connect(info.Data);

                    if (Links.GetTrust(key) == null)
                        Links.Research(key, 0, false);

                    Core.Locations.Research(key);
                }
            });
        }

        void RefreshCommand(uint project, RoomKind kind)
        {
            uint id = GetRoomID(project, kind);

            ChatRoom room = null;
            if (RoomMap.SafeTryGetValue(id, out room))
                RefreshCommand(room);
        }

        internal void RefreshCommand(ChatRoom room) // sends status updates to all members of room
        {
            if (room.Kind == RoomKind.Private || room.Kind == RoomKind.Public)
            {
                Debug.Assert(false);
                return;
            }

            // remember connection status from before
            // nodes we arent connected to do try connect
            // if socket already active send status request

            OpLink localLink = Links.LocalTrust.GetLink(room.ProjectID);

            if (localLink == null)
                return;

            OpLink uplink = localLink.GetHigher(true);
            
            // updates room's member list

            if (room.Kind == RoomKind.Command_High)
            {
                ThreadedDictionary<ulong, ThreadedList<ushort>> prevMembers =  room.Members;
                room.Members = new ThreadedDictionary<ulong,ThreadedList<ushort>>();

                if (uplink != null)
                {
                    if (localLink.LoopRoot != null)
                    {
                        uplink = localLink.LoopRoot;
                        room.Host = uplink.DhtID; // use loop id cause 0 is reserved for no root
                        room.IsLoop = true;
                    }
                    else
                    {
                        room.Host = uplink.DhtID;
                        room.IsLoop = false;
                        room.AddMember(room.Host, prevMembers);
                    }

                    foreach (OpLink downlink in uplink.GetLowers(true))
                        room.AddMember(downlink.DhtID, prevMembers);
                }
            }

            else if (room.Kind == RoomKind.Command_Low)
            {
                ThreadedDictionary<ulong, ThreadedList<ushort>> prevMembers = room.Members;
                room.Members = new ThreadedDictionary<ulong, ThreadedList<ushort>>();

                room.Host = Core.LocalDhtID;
                room.AddMember(room.Host, prevMembers);

                foreach (OpLink downlink in localLink.GetLowers(true))
                    room.AddMember(downlink.DhtID, prevMembers);
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
                // don't remove connections that are no longer in untrusted group

                ThreadedList<OpLink> roots = null;
                if (Links.ProjectRoots.SafeTryGetValue(room.ProjectID, out roots))
                    roots.LockReading(delegate()
                    {
                        foreach (OpLink root in roots)
                            if (root.GetLowers(true).Count == 0 &&
                                !room.Members.SafeContainsKey(root.DhtID))
                            {
                                room.Members.SafeAdd(root.DhtID, new ThreadedList<ushort>());
                            }
                    });
            }

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

        internal void LeaveRoom(uint project, RoomKind kind)
        {
            // deactivates room, let timer remove object is good once we know user no longer wants it

            uint id = GetRoomID(project, kind);

            ChatRoom room = null;
            if (!RoomMap.SafeTryGetValue(id, out room))
                return;

            LeaveRoom(room);
        }

        internal void LeaveRoom(ChatRoom room)
        {
            room.Active = false;

            room.RemoveMember(Core.LocalDhtID, Core.ClientID, !IsCommandRoom(room.Kind));

            SendStatus(room);

            //update interface
            Core.RunInGuiThread(room.MembersUpdate);
        }

        internal bool IsCommandRoom(RoomKind kind)
        {
            return (kind == RoomKind.Command_High || kind == RoomKind.Command_Low ||
                    kind == RoomKind.Live_High || kind == RoomKind.Live_Low);
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

            ChatText message = new ChatText();
            message.ProjectID = room.ProjectID;
            message.Kind = room.Kind;
            message.RoomID = room.RoomID;
            message.Text = text;

            room.Members.LockReading(delegate()
            {
               foreach (ulong member in room.Members.Keys)
                   foreach (RudpSession session in Core.RudpControl.GetActiveSessions(member))
                   {
                       sent = true;
                       session.SendData(ServiceID, 0, message, true);
                   }
            });

            if (!sent)
                ProcessMessage(room, new ChatMessage(Core, "Could not send message, not connected to anyone", true));
        }


        private void ReceiveMessage(ChatText message, RudpSession session)
        {
            // remote's command low, is my command high
            // do here otherwise have to send custom roomID packets to selfs/lowers/highers

            if (session.DhtID != Core.LocalDhtID)
            {
                // if check fails then it is loop node sending data, keep it unchanged
                if (message.Kind == RoomKind.Command_High && Links.IsLowerDirect(session.DhtID, message.ProjectID))
                    message.Kind = RoomKind.Command_Low;

                else if (message.Kind == RoomKind.Command_Low && Links.IsHigher(session.DhtID, message.ProjectID))
                    message.Kind = RoomKind.Command_High;

                else if (message.Kind == RoomKind.Live_High)
                    message.Kind = RoomKind.Live_Low;

                else if (message.Kind == RoomKind.Live_Low)
                    message.Kind = RoomKind.Live_High;
            }

            uint id = IsCommandRoom(message.Kind) ? GetRoomID(message.ProjectID, message.Kind) : message.RoomID;

            ChatRoom room = null;

            // if not in room let remote user know
            if (!RoomMap.TryGetValue(id, out room) ||
                !room.Active )
            {
                SendStatus(session);
                return;
            }

            // if sender not in room
            if(!room.Members.SafeContainsKey(session.DhtID))
                return;

            ProcessMessage(room, new ChatMessage(Core, session, message.Text));
        }

        internal void Session_Update(RudpSession session)
        {

            // send node rooms that we have in common
            if (session.Status == SessionStatus.Active)
            {

                // send invites
                RoomMap.LockReading(delegate()
                {
                    // if we are host of room and connect hasn't been sent invite
                    foreach (ChatRoom room in RoomMap.Values)
                    {
                        if (room.NeedSendInvite(session.DhtID, session.ClientID))
                            // invite not sent
                            if (room.Host == Core.LocalDhtID)
                            {
                                session.SendData(ServiceID, 0, room.Invites[session.DhtID].First, true);
                                room.Invites[session.DhtID].Second.Add(session.ClientID);
                                AlertInviteSent(room, session);
                                SendWhoResponse(room, session);
                            }
                            // else proof not sent
                            else
                            {
                                SendInviteProof(room, session);
                            }

                        if ((room.Kind == RoomKind.Public || room.Kind == RoomKind.Private || room.Kind == RoomKind.Untrusted) &&
                            room.Members.SafeContainsKey(session.DhtID))
                            SendWhoRequest(room, session);
                    }
                });


                SendStatus(session);
            }

            // if disconnected
            if (session.Status == SessionStatus.Closed)
                foreach (ChatRoom room in FindRoom(session.DhtID))
                {
                    room.RemoveMember(session.DhtID, session.ClientID, !IsCommandRoom(room.Kind));

                    Core.RunInGuiThread(room.MembersUpdate);
                }
        }

        void Session_Data(RudpSession session, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (Core.Protocol.ReadPacket(root))
            {
                if (root.Name == ChatPacket.Data)
                {
                    ChatText text = ChatText.Decode(Core.Protocol, root);

                    ReceiveMessage(text, session);
                }

                else if (root.Name == ChatPacket.Status)
                {
                    ChatStatus status = ChatStatus.Decode(Core.Protocol, root);

                    ReceiveStatus(status, session);
                }

                else if (root.Name == ChatPacket.Invite)
                {
                    ChatInvite invite = ChatInvite.Decode(Core.Protocol, root);

                    ReceiveInvite(invite, session);
                }


                else if (root.Name == ChatPacket.Who)
                {
                    ChatWho who = ChatWho.Decode(Core.Protocol, root);

                    ReceiveWho(who, session);
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

        void ReceiveStatus(ChatStatus status, RudpSession session)
        {
            // status is what nodes send to each other to tell what rooms they are active in

            RoomMap.LockReading(delegate()
            {
                foreach (ChatRoom room in RoomMap.Values)
                {
                    bool update = false;

                    // remove from room
                    if (!status.ActiveRooms.Contains(room.RoomID) && room.Members.SafeContainsKey(session.DhtID))
                    {
                        room.RemoveMember(session.DhtID, session.ClientID, !IsCommandRoom(room.Kind));
                        update = true;
                    }

                    // add member to room
                    if (IsCommandRoom(room.Kind) && room.Members.SafeContainsKey(session.DhtID))
                    {
                        room.AddMember(session.DhtID, session.ClientID);
                        update = true;
                    }

                    else if (status.ActiveRooms.Contains(room.RoomID))
                    {
                        // if room private check that sender is verified
                        if (room.Kind == RoomKind.Private && !room.Verified.ContainsKey(session.DhtID))
                            continue;

                         room.AddMember(session.DhtID, session.ClientID);
                         update = true;
                    }

                    if (update)
                        Core.RunInGuiThread(room.MembersUpdate);
                }
            });
        }

        void SendStatus(ChatRoom room)
        {
            room.Members.LockReading(delegate()
            {
                foreach (ulong id in room.Members.Keys)
                    StatusUpdate[id] = true;
            });
        }

        private void SendStatus(RudpSession session)
        {
            // send even if empty so they know to remove us

            ChatStatus status = new ChatStatus();

            RoomMap.LockReading(delegate()
            {
                foreach (ChatRoom room in RoomMap.Values)
                    if (room.Active && !IsCommandRoom(room.Kind))
                    {
                        if (room.Kind == RoomKind.Private && !room.Verified.ContainsKey(session.DhtID))
                            continue;

                        status.ActiveRooms.Add(room.RoomID);
                    }
            });

            session.SendData(ServiceID, 0, status, true);
        }

        internal void SendInviteRequest(ChatRoom room, ulong id)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { SendInviteRequest(room, id); });
                return;
            }

            ChatInvite invite = null;

            // if user explicitly chooses to invite users, invalidate previous recorded attempts
            invite = new ChatInvite();
            invite.RoomID = room.RoomID;
            invite.Title = room.Title;
            

            // if private room sign remote users id with our private key
            if (room.Kind == RoomKind.Private)
            {
                invite.Host = Core.KeyMap[Core.LocalDhtID];

                if (!Core.KeyMap.ContainsKey(id))
                    return;

                invite.SignedInvite = Core.User.Settings.KeyPair.SignData(Core.KeyMap[id], new SHA1CryptoServiceProvider());

                room.Verified[id] = true;
            }

            room.Invites[id] = new Tuple<ChatInvite, List<ushort>>(invite, new List<ushort>());

            // try to conncet to all of id's locations
            foreach (ClientInfo loc in Core.Locations.GetClients(id))
                Core.RudpControl.Connect(loc.Data);

            // send invite to already connected locations
            foreach (RudpSession session in Core.RudpControl.GetActiveSessions(id))
            {
                session.SendData(ServiceID, 0, invite, true);
                room.Invites[id].Second.Add(session.ClientID);
                AlertInviteSent(room, session);
                SendStatus(room); // so we get added as active to new room invitee creates
                SendWhoResponse(room, session);
            }
        }

        private void AlertInviteSent(ChatRoom room, RudpSession session)
        {
            // Invite sent to Bob @Home

            ProcessMessage(room, new ChatMessage(Core, "Invite sent to " + Links.GetName(session.DhtID) + LocationSuffix(session.DhtID, session.ClientID), true));
        }

        void SendInviteProof(ChatRoom room)
        {
            room.Members.LockReading(delegate()
            {
                foreach (ulong id in room.Members.Keys)
                    foreach (RudpSession session in Core.RudpControl.GetActiveSessions(id))
                        if(room.NeedSendInvite(id, session.ClientID))
                            SendInviteProof(room, session);
            });
        }

        void SendInviteProof(ChatRoom room, RudpSession session)
        {
            if (!room.Invites.ContainsKey(Core.LocalDhtID))
                return;

            // if already sent proof to client, return
            Tuple<ChatInvite, List<ushort>> tried;
            if (!room.Invites.TryGetValue(session.DhtID, out tried))
            {
                tried = new Tuple<ChatInvite, List<ushort>>(null, new List<ushort>());
                room.Invites[session.DhtID] = tried;
            }

            if (tried.Second.Contains(session.ClientID))
                return;

            tried.Second.Add(session.ClientID);

            ChatInvite invite = new ChatInvite();
            invite.RoomID = room.RoomID;
            invite.Title = room.Title;
            invite.SignedInvite = room.Invites[Core.LocalDhtID].First.SignedInvite;

            session.SendData(ServiceID, 0, invite, true);
        }

        void ReceiveInvite(ChatInvite invite, RudpSession session)
        {
             
             bool showInvite = false;

             ChatRoom room;

             if (!RoomMap.TryGetValue(invite.RoomID, out room))
             {
                 RoomKind kind = invite.SignedInvite != null ? RoomKind.Private : RoomKind.Public;
                 room = new ChatRoom(kind, 0, invite.Title);
                 room.RoomID = invite.RoomID;
                 room.Kind = kind;
                 room.Members.SafeAdd(session.DhtID, new ThreadedList<ushort>());

                 if (invite.Host != null)
                 {
                     room.Host = Utilities.KeytoID(invite.Host);
                     Core.IndexKey(room.Host, ref invite.Host);
                 }

                 RoomMap.SafeAdd(room.RoomID, room);

                 showInvite = true;
             }

            // private room
            if (room.Kind == RoomKind.Private)
            {
                if(!Core.KeyMap.ContainsKey(room.Host))
                    return;

                byte[] hostKey = Core.KeyMap[room.Host];


                // if this is host sending us our verification
                if (session.DhtID == room.Host)
                {
                    // check that host signed our public key with his private
                    if (!Utilities.CheckSignedData(hostKey, Core.KeyMap[Core.LocalDhtID], invite.SignedInvite))
                        return;

                    if(!room.Invites.ContainsKey(Core.LocalDhtID)) // would fail if a node's dupe on network sends invite back to itself
                        room.Invites.Add(Core.LocalDhtID, new Tuple<ChatInvite, List<ushort>>(invite, new List<ushort>()));
                }

                // else this is node in room sending us proof of being invited
                else
                {
                    if (!Core.KeyMap.ContainsKey(session.DhtID))
                        return; // key should def be in map, it was added when session was made to sender

                    // check that host signed remote's key with host's private
                    if (!Utilities.CheckSignedData(hostKey, Core.KeyMap[session.DhtID], invite.SignedInvite))
                        return;
                }

                // if not verified yet, add them and send back our own verification
                if (!room.Verified.ContainsKey(session.DhtID))
                {
                    room.Verified[session.DhtID] = true;

                    if (room.Active)
                    {
                        SendInviteProof(room, session); // someone sends us their proof, we send it back in return
                        SendStatus(session); // send status here because now it will include private rooms
                    }
                }
            }

            if (!Core.Links.TrustMap.SafeContainsKey(session.DhtID))
                Links.Research(session.DhtID, 0, false);

            if(showInvite)
                Core.RunInGuiThread(Invited, session.DhtID, room);
        }

        void SendWhoRequest(ChatRoom room)
        {
            Debug.Assert(!IsCommandRoom(room.Kind));

            room.Members.LockReading(delegate()
           {
               foreach (ulong id in room.Members.Keys)
                   foreach (RudpSession session in Core.RudpControl.GetActiveSessions(id))
                        SendWhoRequest(room, session);
           });
        }

        void SendWhoRequest(ChatRoom room, RudpSession session)
        {

            ChatWho whoReq = new ChatWho();
            whoReq.Request = true;
            whoReq.RoomID = room.RoomID;
            session.SendData(ServiceID, 0, whoReq, true);
        }

        void SendWhoResponse(ChatRoom room, RudpSession session)
        {
            Debug.Assert(!IsCommandRoom(room.Kind));


            List<ChatWho> whoPackets = new List<ChatWho>();

            ChatWho who = new ChatWho();
            who.RoomID = room.RoomID;
            whoPackets.Add(who);

            room.Members.LockReading(delegate()
            {
                foreach (ulong id in room.Members.Keys)
                {
                    who.Members.Add(id);

                    if (who.Members.Count > 40) // 40 * 8 = 320 bytes
                    {
                        who = new ChatWho();
                        who.RoomID = room.RoomID;
                        whoPackets.Add(who);
                    }
                }
            });

            // send who to already connected locations
            foreach(ChatWho packet in whoPackets)
                session.SendData(ServiceID, 0, packet, true);
        }


        void ReceiveWho(ChatWho who, RudpSession session)
        {
            // if in room 
            ChatRoom room;

            // if not in room, send status
            if (!RoomMap.TryGetValue(who.RoomID, out room))
            {
                SendStatus(session);
                return;
            }

            // if room not public, not untrusted, and not from verified private room member or host, igonre
            if (IsCommandRoom(room.Kind) || (room.Kind == RoomKind.Private && !room.Verified.ContainsKey(session.DhtID)))
                return;

                
            // if requset
            if(who.Request)
                SendWhoResponse(room, session);
            
            // if reply
            else
            {
                // add members to our own list
                foreach(ulong id in who.Members)
                    if (!room.Members.SafeContainsKey(id))
                    {
                        room.Members.SafeAdd(id, new ThreadedList<ushort>());

                        if (Links.GetTrust(id) == null)
                            Links.Research(id, 0, false);

                        Core.Locations.Research(id);
                    }

                // connect to new members
                ConnectRoom(room); 
            }
        }

        internal string LocationSuffix(ulong dhtID, ushort clientID)
        {
            // only show user's location if more than one are active

            if (Core.Locations.ActiveClientCount(dhtID) > 1)
                return " @" + Core.Locations.GetLocationName(dhtID, clientID);

            return "";
        }
    }


    internal enum RoomKind { Command_High, Command_Low, Live_High, Live_Low, Untrusted, Public, Private  }; // do not change order


    internal delegate void MembersUpdateHandler();
    internal delegate void ChatUpdateHandler(ChatMessage message);

    internal class ChatRoom
    {
        internal uint     RoomID;
        internal uint     ProjectID;
        internal string   Title;
        internal RoomKind Kind;
        internal bool     IsLoop;
        internal bool     Active;

        internal ulong Host;
        internal ThreadedDictionary<ulong, ThreadedList<ushort>> Members = new ThreadedDictionary<ulong, ThreadedList<ushort>>();
        
        // for host this is a map of clients who have been sent invitations
        // for invitee this is a map of clients who have been sent proof that we are part of the room
        internal Dictionary<ulong, Tuple<ChatInvite, List<ushort>>> Invites;
        internal Dictionary<ulong, bool> Verified;

        internal ThreadedList<ChatMessage> Log = new ThreadedList<ChatMessage>();

        internal MembersUpdateHandler MembersUpdate;
        internal ChatUpdateHandler    ChatUpdate;

        // per channel polling needs to be done because client may be still connected, leaving one channel, joining another


        internal ChatRoom(RoomKind kind, uint project, string title)
        {
            RoomID = project + (uint)kind;
            Kind = kind;
            ProjectID   = project;
            Title = title;

            if (Kind == RoomKind.Private || kind == RoomKind.Public)
                Invites = new Dictionary<ulong, Tuple<ChatInvite, List<ushort>>>();

            if (Kind == RoomKind.Private)
                Verified = new Dictionary<ulong, bool>();
        }

        internal void AddMember(ulong dhtid, ThreadedDictionary<ulong, ThreadedList<ushort>> prev)
        {
            ThreadedList<ushort> connected = null;
            if (!prev.SafeTryGetValue(dhtid, out connected))
                connected = new ThreadedList<ushort>();

            Members.SafeAdd(dhtid, connected);
        }

        internal void AddMember(ulong dhtid, ushort client)
        {
            ThreadedList<ushort> connected = null;
            if (!Members.SafeTryGetValue(dhtid, out connected))
                connected = new ThreadedList<ushort>();

            if (client != 0 && !connected.SafeContains(client))
                connected.SafeAdd(client);

            Members.SafeAdd(dhtid, connected);
        }

        internal void RemoveMember(ulong dhtid, ushort client, bool delete)
        {
            ThreadedList<ushort> connected = null;

            if (!Members.SafeTryGetValue(dhtid, out connected))
                return;

            if (connected.SafeContains(client))
                connected.SafeRemove(client);

            // remove member himself if no connections, and not a command room 
            if (delete && connected.SafeCount == 0 && (Kind == RoomKind.Untrusted || Kind == RoomKind.Public || Kind == RoomKind.Private))
                Members.SafeRemove(dhtid);
        }

        internal int GetActiveMembers()
        {
            int count = 0;

            Members.LockReading(delegate()
            {
                foreach (ThreadedList<ushort> clients in Members.Values)
                    if (clients.SafeCount > 0)
                        count++;
            });

            return count;
        }

        internal bool NeedSendInvite(ulong id, ushort client)
        {
            return  Invites != null &&
                    Invites.ContainsKey(id) &&
                    !Invites[id].Second.Contains(client);
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