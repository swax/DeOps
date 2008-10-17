using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Transport;

using RiseOp.Services.Trust;
using RiseOp.Services.Location;
using RiseOp.Services.Share;

using NLipsum.Core;


namespace RiseOp.Services.Chat
{
    internal delegate void RefreshHandler();


    internal class ChatService : OpService
    {
        public string Name { get { return "Chat"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.Chat; } }

        internal OpCore Core;
        internal DhtNetwork Network;
        internal TrustService Trust;

        internal ThreadedDictionary<uint, ChatRoom> RoomMap = new ThreadedDictionary<uint, ChatRoom>();

        internal Dictionary<ulong, bool> StatusUpdate = new Dictionary<ulong, bool>();

        internal RefreshHandler Refresh;

        bool ChatNewsUpdate;


        internal ChatService(OpCore core)
        {
            Core = core;
            Network = Core.Network;
            Trust = core.Trust;

            Network.RudpControl.SessionUpdate += new SessionUpdateHandler(Session_Update);
            Network.RudpControl.SessionData[ServiceID, 0] += new SessionDataHandler(Session_Data);
            Network.RudpControl.KeepActive += new KeepActiveHandler(Session_KeepActive);

            Core.SecondTimerEvent += new TimerHandler(Core_SecondTimer);
            Core.KeepDataCore += new KeepDataHandler(Core_KeepData);

            Core.Locations.KnowOnline += new KnowOnlineHandler(Location_KnowOnline);
            Core.Locations.LocationUpdate += new LocationUpdateHandler(Location_Update);

            if (Trust != null)
            {
                Trust.LinkUpdate += new LinkUpdateHandler(Link_Update);
                Link_Update(Trust.LocalTrust);
            }
        }

        public void Dispose()
        {
            if (Refresh != null)
                throw new Exception("Chat Events not fin'd");

            Network.RudpControl.SessionUpdate -= new SessionUpdateHandler(Session_Update);
            Network.RudpControl.SessionData[ServiceID, 0] -= new SessionDataHandler(Session_Data);
            Network.RudpControl.KeepActive -= new KeepActiveHandler(Session_KeepActive);

            Core.SecondTimerEvent -= new TimerHandler(Core_SecondTimer);
            Core.KeepDataCore -= new KeepDataHandler(Core_KeepData);

            Core.Locations.KnowOnline -= new KnowOnlineHandler(Location_KnowOnline);
            Core.Locations.LocationUpdate -= new LocationUpdateHandler(Location_Update);

            if(Trust != null)
                Trust.LinkUpdate -= new LinkUpdateHandler(Link_Update);
        }

        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong key, uint proj)
        {
            if (key != Core.UserID)
                return;

            if(menuType == InterfaceMenuType.Internal)
                menus.Add(new MenuItemInfo("Comm/Chat", ChatRes.Icon, new EventHandler(Menu_View)));

            if(menuType == InterfaceMenuType.External)
                menus.Add(new MenuItemInfo("Chat", ChatRes.Icon, new EventHandler(Menu_View)));
        }

        void Menu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            if (node.GetUser() != Core.UserID)
                return;

            // gui creates viewshell, component just passes view object
            ChatView view = new ChatView(this, node.GetProject());

            Core.InvokeView(node.IsExternal(), view);
        }

        void Core_KeepData()
        {
            ForAllUsers(id => Core.KeepData.SafeAdd(id, true));
        }

        void Location_KnowOnline(List<ulong> users)
        {
            ForAllUsers(delegate(ulong id)
            { 
                if (!users.Contains(id)) 
                    users.Add(id); 
            });
        }

        void ForAllUsers(Action<ulong> action)
        {
            RoomMap.LockReading(delegate()
            {
                foreach (ChatRoom room in RoomMap.Values)
                    if (room.Active && !IsCommandRoom(room.Kind))
                    {
                        room.Members.LockReading(delegate()
                        {
                            foreach (ulong id in room.Members)
                                action(id);
                        });

                        if (room.Invites != null)
                            foreach (ulong id in room.Invites.Keys)
                                action(id);

                        if (room.Verified != null)
                            foreach (ulong id in room.Verified.Keys)
                                action(id);
                    }
            });
        }

        void Core_SecondTimer()
        {
            // send status upates once per second so we're not sending multiple updates to the same client more than
            // once per second

            foreach (ulong key in StatusUpdate.Keys)
                foreach (RudpSession session in Network.RudpControl.GetActiveSessions(key))
                    SendStatus(session);

            StatusUpdate.Clear();

            
            // for sim test write random msg ever 10 secs
            if (Core.Sim != null && SimTextActive && Core.RndGen.Next(10) == 0)
            {
                List<ChatRoom> rooms = new List<ChatRoom>();
                RoomMap.LockReading(delegate()
                {
                    foreach (ChatRoom room in RoomMap.Values)
                        if (room.Active)
                            rooms.Add(room);
                });

                if (rooms.Count > 0)
                    SendMessage(rooms[Core.RndGen.Next(rooms.Count)], Core.TextGen.GenerateSentences(1, Sentence.Short)[0], TextFormat.Plain);
            }
        }

        bool SimTextActive = false;

        public void SimTest()
        {
            SimTextActive = !SimTextActive;
        }

        public void SimCleanup()
        {
        }

        internal void Link_Update(OpTrust trust)
        {

            // update command/live rooms
            Trust.ProjectRoots.LockReading(delegate()
            {
                foreach (uint project in Trust.ProjectRoots.Keys)
                {
                    OpLink localLink = Trust.LocalTrust.GetLink(project);
                    OpLink remoteLink = trust.GetLink(project);

                    if (localLink == null || remoteLink == null)
                        continue;

                    OpLink uplink = localLink.GetHigher(true);
                    List<OpLink> downlinks = localLink.GetLowers(true);
                    
                    // if local link updating
                    if (trust == Trust.LocalTrust)
                    {
                        // if we are in the project
                        if (localLink.Active)
                        {
                            JoinCommand(project, RoomKind.Command_High);
                            JoinCommand(project, RoomKind.Command_Low);
                            JoinCommand(project, RoomKind.Live_High);
                            JoinCommand(project, RoomKind.Live_Low);
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
                    }

                    Core.RunInGuiThread(Refresh);
                }
            });

            // refresh member list of any commmand/live room this person is apart of
            // link would already be added above, this ensures user is removed
            foreach(ChatRoom room in FindRoom(trust.UserID))
                if(IsCommandRoom(room.Kind))
                    RefreshCommand(room);
                else if(room.Members.SafeContains(trust.UserID))
                    Core.RunInGuiThread(room.MembersUpdate);
        }

        internal ChatRoom CreateRoom(string name, RoomKind kind)
        {
            // create room
            uint id = (uint)Core.RndGen.Next();
            
            ChatRoom room = new ChatRoom(kind, id, name);

            room.Active = true;
            room.AddMember(Core.UserID);

            RoomMap.SafeAdd(id, room);
            
            if (kind == RoomKind.Private)
            {
                room.Host = Core.UserID;
                room.Verified[Core.UserID] = true;
                SendInviteRequest(room, Core.UserID); // send invite to copies of ourself that exist
            }

            Core.RunInGuiThread(Refresh);

            return room;    
        }

        internal void JoinRoom(ChatRoom room)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => JoinRoom(room));
                return;
            }

            if (room.Kind != RoomKind.Public && room.Kind != RoomKind.Private)
            {
                JoinCommand(room.ProjectID, room.Kind);
                return;
            }

            room.Active = true;
            room.AddMember(Core.UserID);

            // for private rooms, send proof of invite first
            if (room.Kind == RoomKind.Private)
                SendInviteProof(room);

            SendStatus(room);

            SendWhoRequest(room);

            ConnectRoom(room);

            Core.RunInGuiThread(Refresh);
            Core.RunInGuiThread(room.MembersUpdate);
        }

        internal void JoinCommand(uint project, RoomKind kind)
        {

            uint id = GetRoomID(project, kind);

            // create if doesnt exist
            ChatRoom room = null;

            if (!RoomMap.SafeTryGetValue(id, out room))
                room = new ChatRoom(kind, project);

            room.Active = true;
            room.AddMember(Core.UserID);

            RoomMap.SafeAdd(id, room);

            RefreshCommand(room);
            SendStatus(room);
            ConnectRoom(room);
        }

        private void ConnectRoom(ChatRoom room)
        {
            room.Members.LockReading(delegate()
            {
                foreach (ulong key in room.Members)
                {
                    List<ClientInfo> clients = Core.Locations.GetClients(key);

                    if(clients.Count == 0)
                        Core.Locations.Research(key);
                    else
                        foreach (ClientInfo info in clients)
                            Network.RudpControl.Connect(info.Data);

                    if (Trust != null && Trust.GetTrust(key) == null)
                        Trust.Research(key, 0, false);    
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

            OpLink localLink = Trust.LocalTrust.GetLink(room.ProjectID);

            if (localLink == null)
                return;

            OpLink uplink = localLink.GetHigher(true);
            
            // updates room's member list

            if (room.Kind == RoomKind.Command_High)
            {
                room.Members = new ThreadedList<ulong>();

                if (uplink != null)
                {
                    if (localLink.LoopRoot != null)
                    {
                        uplink = localLink.LoopRoot;
                        room.Host = uplink.UserID; // use loop id cause 0 is reserved for no root
                        room.IsLoop = true;
                    }
                    else
                    {
                        room.Host = uplink.UserID;
                        room.IsLoop = false;
                        room.AddMember(room.Host);
                    }

                    foreach (OpLink downlink in uplink.GetLowers(true))
                        room.AddMember(downlink.UserID);
                }
            }

            else if (room.Kind == RoomKind.Command_Low)
            {
                room.Members = new ThreadedList<ulong>();

                room.Host = Core.UserID;
                room.AddMember(room.Host);

                foreach (OpLink downlink in localLink.GetLowers(true))
                    room.AddMember(downlink.UserID);
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


            // update dispaly that members has been refreshed
            Core.RunInGuiThread(room.MembersUpdate);
        }

        void LeaveRooms(uint project)
        {
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

            room.RemoveMember(Core.UserID);

            SendStatus(room);

            //update interface
            Core.RunInGuiThread(Refresh);
            Core.RunInGuiThread(room.MembersUpdate);
        }

        internal static bool IsCommandRoom(RoomKind kind)
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
                    if(room.Members.SafeContains(key))
                        results.Add(room);
            });

            return results;
        }

        internal void Location_Update(LocationData location)
        {
            bool connect = false;

            foreach (ChatRoom room in FindRoom(location.UserID))
                if (room.Active)
                    connect = true;

            if(connect)
                Network.RudpControl.Connect(location); // func checks if already connected
        }

        internal void SendMessage(ChatRoom room, string text, TextFormat format)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { SendMessage(room, text, format); });
                return;
            }


            bool sent = false;

            if (room.Active)
            {
                ChatText message = new ChatText();
                message.ProjectID = room.ProjectID;
                message.Kind = room.Kind;
                message.RoomID = room.RoomID;
                message.Text = text;
                message.Format = format;

                room.Members.LockReading(delegate()
                {
                    foreach (ulong member in room.Members)
                        foreach (RudpSession session in Network.RudpControl.GetActiveSessions(member))
                        {
                            sent = true;
                            session.SendData(ServiceID, 0, message, true);
                        }
                });
            }

            ProcessMessage(room, new ChatMessage(Core, text, format) { Sent = sent });


            //if (!sent)
            //    ProcessMessage(room, "Could not send message, not connected to anyone");
        }


        private void ReceiveMessage(ChatText message, RudpSession session)
        {
            if (Core.Buddies.IgnoreList.SafeContainsKey(session.UserID))
                return;

            // remote's command low, is my command high
            // do here otherwise have to send custom roomID packets to selfs/lowers/highers

            if (Trust != null && session.UserID != Core.UserID)
            {
                // if check fails then it is loop node sending data, keep it unchanged
                if (message.Kind == RoomKind.Command_High && Trust.IsLowerDirect(session.UserID, message.ProjectID))
                    message.Kind = RoomKind.Command_Low;

                else if (message.Kind == RoomKind.Command_Low && Trust.IsHigher(session.UserID, message.ProjectID))
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
            if(!room.Members.SafeContains(session.UserID))
                return;

            if (!ChatNewsUpdate)
            {
                ChatNewsUpdate = true;
                Core.MakeNews(Core.GetName(session.UserID) + " is chatting", session.UserID, 0, false, ChatRes.Icon, Menu_View);
            }

            ProcessMessage(room, new ChatMessage(Core, session, message));
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
                        if (room.NeedSendInvite(session.UserID, session.ClientID))
                            // invite not sent
                            if (room.Kind == RoomKind.Public || room.Host == Core.UserID)
                            {
                                session.SendData(ServiceID, 0, room.Invites[session.UserID].First, true);
                                room.Invites[session.UserID].Second.Add(session.ClientID);
                                ProcessMessage(room, "Invite sent to " + GetNameAndLocation(session));
                                SendWhoResponse(room, session);
                            }
                            // else private room and we are not the host, send proof we belong here
                            else
                            {
                                SendInviteProof(room, session);
                            }

                        // ask member who else is in room
                        if ((room.Kind == RoomKind.Public || room.Kind == RoomKind.Private) &&
                            room.Members.SafeContains(session.UserID))
                            SendWhoRequest(room, session);
                    }
                });


                SendStatus(session);
            }

            // if disconnected
            if (session.Status == SessionStatus.Closed)
                foreach (ChatRoom room in FindRoom(session.UserID))
                    if (room.Active) 
                        // don't remove from members unless explicitly told in status
                        Core.RunInGuiThread(room.MembersUpdate);
        }

        void Session_Data(RudpSession session, byte[] data)
        {
            G2Header root = new G2Header(data);

            if (G2Protocol.ReadPacket(root))
            {
                switch (root.Name)
                {
                    case ChatPacket.Data:
                        ReceiveMessage(ChatText.Decode(root), session);
                        break;

                    case ChatPacket.Status:
                        ReceiveStatus(ChatStatus.Decode(root), session);
                        break;

                    case ChatPacket.Invite:
                        ReceiveInvite(ChatInvite.Decode(root), session);
                        break;

                    case ChatPacket.Who:
                        ReceiveWho(ChatWho.Decode(root), session);
                        break;
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
                            foreach (ulong member in room.Members)
                                active[member] = true;
                        });
            });
        }

        // system message
        private void ProcessMessage(ChatRoom room, string text)
        {
            ProcessMessage(room, new ChatMessage(Core, text, TextFormat.Plain) { System = true });
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
                    if (!status.ActiveRooms.Contains(room.RoomID) && room.Members.SafeContains(session.UserID))
                    {
                        if (!IsCommandRoom(room.Kind))
                        {
                            if (room.Members.SafeContains(session.UserID))
                                ProcessMessage(room, GetNameAndLocation(session) + " left the room");
                            
                            room.RemoveMember(session.UserID);
                        }

                        update = true;
                    }

                    // add member to room
                    if (IsCommandRoom(room.Kind) && room.Members.SafeContains(session.UserID))
                        update = true;

                    else if (status.ActiveRooms.Contains(room.RoomID))
                    {
                        // if room private check that sender is verified
                        if (room.Kind == RoomKind.Private && !room.Verified.ContainsKey(session.UserID))
                            continue;

                        if (!room.Members.SafeContains(session.UserID))
                            ProcessMessage(room, GetNameAndLocation(session) + " joined the room");

                        room.AddMember(session.UserID);
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
                foreach (ulong id in room.Members)
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
                        if (room.Kind == RoomKind.Private && !room.Verified.ContainsKey(session.UserID))
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

            room.AddMember(id);


            ChatInvite invite = null;

            // if user explicitly chooses to invite users, invalidate previous recorded attempts
            invite = new ChatInvite();
            invite.RoomID = room.RoomID;
            invite.Title = room.Title;
            

            // if private room sign remote users id with our private key
            if (room.Kind == RoomKind.Private)
            {
                invite.Host = Core.KeyMap[Core.UserID];

                if (!Core.KeyMap.ContainsKey(id))
                    return;

                invite.SignedInvite = Core.User.Settings.KeyPair.SignData(Core.KeyMap[id], new SHA1CryptoServiceProvider());

                room.Verified[id] = true;
            }

            room.Invites[id] = new Tuple<ChatInvite, List<ushort>>(invite, new List<ushort>());

            // try to conncet to all of id's locations
            foreach (ClientInfo loc in Core.Locations.GetClients(id))
                Network.RudpControl.Connect(loc.Data);

            // send invite to already connected locations
            foreach (RudpSession session in Network.RudpControl.GetActiveSessions(id))
            {
                session.SendData(ServiceID, 0, invite, true);
                room.Invites[id].Second.Add(session.ClientID);
                ProcessMessage(room, "Invite sent to " + GetNameAndLocation(session));
                SendStatus(room); // so we get added as active to new room invitee creates
                SendWhoResponse(room, session);
            }
        }

        internal string GetNameAndLocation(DhtClient client)
        {
            string text = Core.GetName(client.UserID);

            // only show user's location if more than one are active
            if (Core.Locations.ActiveClientCount(client.UserID) > 1)
                text += " @" + Core.Locations.GetLocationName(client.UserID, client.ClientID);

            return text;
        }

        void SendInviteProof(ChatRoom room)
        {
            room.Members.LockReading(delegate()
            {
                foreach (ulong id in room.Members)
                    foreach (RudpSession session in Network.RudpControl.GetActiveSessions(id))
                        if(room.NeedSendInvite(id, session.ClientID))
                            SendInviteProof(room, session);
            });
        }

        void SendInviteProof(ChatRoom room, RudpSession session)
        {
            if (!room.Invites.ContainsKey(Core.UserID))
                return;

            // if already sent proof to client, return
            Tuple<ChatInvite, List<ushort>> tried;
            if (!room.Invites.TryGetValue(session.UserID, out tried))
            {
                tried = new Tuple<ChatInvite, List<ushort>>(null, new List<ushort>());
                room.Invites[session.UserID] = tried;
            }

            if (tried.Second.Contains(session.ClientID))
                return;

            tried.Second.Add(session.ClientID);

            ChatInvite invite = new ChatInvite();
            invite.RoomID = room.RoomID;
            invite.Title = room.Title;
            invite.SignedInvite = room.Invites[Core.UserID].First.SignedInvite;

            session.SendData(ServiceID, 0, invite, true);
        }

        void ReceiveInvite(ChatInvite invite, RudpSession session)
        {
            // if in global im, only allow if on buddies list
            if (Core.User.Settings.GlobalIM)
                if (!Core.Buddies.BuddyList.SafeContainsKey(session.UserID))
                    return;

            if (Core.Buddies.IgnoreList.SafeContainsKey(session.UserID))
                return;

             bool showInvite = false;

             ChatRoom room;

             if (!RoomMap.TryGetValue(invite.RoomID, out room))
             {
                 RoomKind kind = invite.SignedInvite != null ? RoomKind.Private : RoomKind.Public;
                 room = new ChatRoom(kind, invite.RoomID, invite.Title);
                 room.RoomID = invite.RoomID;
                 room.Kind = kind;
                 room.AddMember(session.UserID);

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
                if (session.UserID == room.Host)
                {
                    // check that host signed our public key with his private
                    if (!Utilities.CheckSignedData(hostKey, Core.KeyMap[Core.UserID], invite.SignedInvite))
                        return;

                    if(!room.Invites.ContainsKey(Core.UserID)) // would fail if a node's dupe on network sends invite back to itself
                        room.Invites.Add(Core.UserID, new Tuple<ChatInvite, List<ushort>>(invite, new List<ushort>()));
                }

                // else this is node in room sending us proof of being invited
                else
                {
                    if (!Core.KeyMap.ContainsKey(session.UserID))
                        return; // key should def be in map, it was added when session was made to sender

                    // check that host signed remote's key with host's private
                    if (!Utilities.CheckSignedData(hostKey, Core.KeyMap[session.UserID], invite.SignedInvite))
                        return;
                }

                // if not verified yet, add them and send back our own verification
                if (!room.Verified.ContainsKey(session.UserID))
                {
                    room.Verified[session.UserID] = true;

                    if (room.Active)
                    {
                        SendInviteProof(room, session); // someone sends us their proof, we send it back in return
                        SendStatus(session); // send status here because now it will include private rooms
                    }
                }
            }

            if (Trust != null && !Trust.TrustMap.SafeContainsKey(session.UserID))
                Trust.Research(session.UserID, 0, false);

            if (showInvite)
            {
                Core.RunInGuiThread((System.Windows.Forms.MethodInvoker)delegate
                {
                    new InviteForm(this, session.UserID, room).ShowDialog();
                });
            }
        }

        void SendWhoRequest(ChatRoom room)
        {
            Debug.Assert(!IsCommandRoom(room.Kind));

            room.Members.LockReading(delegate()
           {
               foreach (ulong id in room.Members)
                   foreach (RudpSession session in Network.RudpControl.GetActiveSessions(id))
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
                foreach (ulong id in room.Members)
                    if (Network.RudpControl.GetActiveSessions(id).Count > 0) // only send members who are connected
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

            // if room not public, and not from verified private room member or host, igonre
            if (IsCommandRoom(room.Kind) || (room.Kind == RoomKind.Private && !room.Verified.ContainsKey(session.UserID)))
                return;

                
            // if requset
            if(who.Request)
                SendWhoResponse(room, session);
            
            // if reply
            else
            {
                // add members to our own list
                foreach(ulong id in who.Members)
                    if (!room.Members.SafeContains(id))
                    {
                        room.AddMember(id);

                        if (Trust != null && Trust.GetTrust(id) == null)
                            Trust.Research(id, 0, false);

                        Core.Locations.Research(id);
                    }

                // connect to new members
                ConnectRoom(room); 
            }
        }

        internal void Share_FileProcessed(SharedFile file, object arg)
        {
            ChatRoom room = arg as ChatRoom;

            if (room == null || !room.Active)
                return;

            ShareService share = Core.GetService(ServiceIDs.Share) as ShareService;

            string message = "File: " + file.Name +
                ", Size: " + Utilities.ByteSizetoDecString(file.Size) +
                ", Download: " + share.GetFileLink(Core.UserID, file);

            SendMessage(room, message, TextFormat.Plain);
        }
    }


    internal enum RoomKind { Command_High, Command_Low, Live_High, Live_Low, Public, Private  }; // do not change order


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
        // members in room by key, if online there will be elements in list for each location
        internal ThreadedList<ulong> Members = new ThreadedList<ulong>();
        
        // for host this is a map of clients who have been sent invitations
        // for invitee this is a map of clients who have been sent proof that we are part of the room
        internal Dictionary<ulong, Tuple<ChatInvite, List<ushort>>> Invites;
        internal Dictionary<ulong, bool> Verified;

        internal ThreadedList<ChatMessage> Log = new ThreadedList<ChatMessage>();

        internal MembersUpdateHandler MembersUpdate;
        internal ChatUpdateHandler    ChatUpdate;

        // per channel polling needs to be done because client may be still connected, leaving one channel, joining another


        internal ChatRoom(RoomKind kind, uint project)
        {
            Debug.Assert(ChatService.IsCommandRoom(kind));

            Kind = kind;
            RoomID = project + (uint)kind;
            ProjectID = project;
        }

        internal ChatRoom( RoomKind kind, uint id, string title)
        {
            Debug.Assert( !ChatService.IsCommandRoom(kind) );

            Kind = kind;
            RoomID = id;
            Title = title;

            if (Kind == RoomKind.Private || kind == RoomKind.Public)
                Invites = new Dictionary<ulong, Tuple<ChatInvite, List<ushort>>>();

            if (Kind == RoomKind.Private)
                Verified = new Dictionary<ulong, bool>();
        }

        internal int GetActiveMembers(ChatService chat)
        {
            int count = 0;

            Members.LockReading(delegate()
            {
                foreach (ulong user in Members)
                    if (chat.Network.RudpControl.GetActiveSessions(user).Count > 0)
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

        internal void AddMember(ulong user)
        {
            if(!Members.SafeContains(user))
                Members.SafeAdd(user);
        }

        internal void RemoveMember(ulong user)
        {
            Members.SafeRemove(user);
        }
    }

    internal class ChatMessage : DhtClient
    {
        internal bool       System;
        internal DateTime   TimeStamp;
        internal string     Text;
        internal TextFormat Format;
        internal bool       Sent;


        internal ChatMessage(OpCore core, string text, TextFormat format)
        {
            UserID = core.UserID;
            ClientID = core.Network.Local.ClientID;
            TimeStamp = core.TimeNow;
            Text = text;
            Format = format;
        }

        internal ChatMessage(OpCore core, RudpSession session, ChatText text)
        {
            UserID = session.UserID;
            ClientID = session.ClientID;
            TimeStamp = core.TimeNow;
            Text = text.Text;
            Format = text.Format;
        }
    }
}