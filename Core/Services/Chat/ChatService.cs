using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Transport;

using DeOps.Services.Assist;
using DeOps.Services.Trust;
using DeOps.Services.Location;
using DeOps.Services.Share;


namespace DeOps.Services.Chat
{
    public delegate void RefreshHandler();


    public class ChatService : OpService
    {
        public string Name { get { return "Chat"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.Chat; } }

        public OpCore Core;
        public DhtNetwork Network;
        public TrustService Trust;

        public ThreadedDictionary<ulong, ChatRoom> RoomMap = new ThreadedDictionary<ulong, ChatRoom>();

        public Dictionary<ulong, bool> StatusUpdate = new Dictionary<ulong, bool>();

        public RefreshHandler Refresh;

        bool ChatNewsUpdate;

        const uint DataTypeLocation = 0x02;
        TempCache TempLocation;

        public delegate void NewInviteHandler(ulong userID, ChatRoom room);
        public NewInviteHandler NewInvite;


        public ChatService(OpCore core)
        {
            Core = core;
            Network = Core.Network;
            Trust = core.Trust;

            Network.RudpControl.SessionUpdate += new SessionUpdateHandler(Session_Update);
            Network.RudpControl.SessionData[ServiceID, 0] += new SessionDataHandler(Session_Data);
            Network.RudpControl.KeepActive += new KeepActiveHandler(Session_KeepActive);

            Core.SecondTimerEvent += Core_SecondTimer;
            Core.KeepDataCore += new KeepDataHandler(Core_KeepData);

            Core.Locations.KnowOnline += new KnowOnlineHandler(Location_KnowOnline);
            Core.Locations.LocationUpdate += new LocationUpdateHandler(Location_Update);

            if (Trust != null)
            {
                Trust.LinkUpdate += new LinkUpdateHandler(Link_Update);
                Link_Update(Trust.LocalTrust);
            }

            TempLocation = new TempCache(Network, ServiceID, DataTypeLocation);
        }

        public void Dispose()
        {
            if (Refresh != null)
                throw new Exception("Chat Events not fin'd");

            Network.RudpControl.SessionUpdate -= new SessionUpdateHandler(Session_Update);
            Network.RudpControl.SessionData[ServiceID, 0] -= new SessionDataHandler(Session_Data);
            Network.RudpControl.KeepActive -= new KeepActiveHandler(Session_KeepActive);

            Core.SecondTimerEvent -= Core_SecondTimer;
            Core.KeepDataCore -= new KeepDataHandler(Core_KeepData);

            Core.Locations.KnowOnline -= new KnowOnlineHandler(Location_KnowOnline);
            Core.Locations.LocationUpdate -= new LocationUpdateHandler(Location_Update);

            if(Trust != null)
                Trust.LinkUpdate -= new LinkUpdateHandler(Link_Update);

            TempLocation.Dispose();
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


            // publish room location for public rooms hourly
            RoomMap.LockReading(delegate()
            {
                foreach (ChatRoom room in RoomMap.Values)
                    if (room.Active && room.PublishRoom && Core.TimeNow > room.NextPublish)
                    {
                        TempLocation.Publish(room.RoomID, Core.Locations.LocalClient.Data.EncodeLight(Network.Protocol));
                        room.NextPublish = Core.TimeNow.AddHours(1);
                    }
            });
           
            
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
                    SendMessage(rooms[Core.RndGen.Next(rooms.Count)], Core.TextGen.GenerateSentences(1)[0], TextFormat.Plain);
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

        public void Link_Update(OpTrust trust)
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

        public ChatRoom CreateRoom(string name, RoomKind kind)
        {
            ulong id = Utilities.RandUInt64(Core.RndGen);

            if (kind == RoomKind.Public)
                id = ChatService.GetPublicRoomID(name);
     
            ChatRoom room = new ChatRoom(kind, id, name);

            room.Active = true;
            room.AddMember(Core.UserID);

            RoomMap.SafeAdd(id, room);
            
            if (kind == RoomKind.Secret)
            {
                room.Host = Core.UserID;
                room.Verified[Core.UserID] = true;
                SendInviteRequest(room, Core.UserID); // send invite to copies of ourself that exist
            }

            Core.RunInGuiThread(Refresh);

            if (room.PublishRoom)
                SetupPublic(room);
           
            return room;
        }

        private void SetupPublic(ChatRoom room)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => SetupPublic(room));
                return;
            }

            room.NextPublish = Core.TimeNow ;
            TempLocation.Search(room.RoomID, room, Search_FoundRoom);
        }

        void Search_FoundRoom(byte[] data, object arg)
        {
            ChatRoom room = arg as ChatRoom;

            if (!room.Active)
                return;

            // add locations to running transfer
            LocationData loc = LocationData.Decode(data);
            DhtClient client = new DhtClient(loc.UserID, loc.Source.ClientID);

            Core.Network.LightComm.Update(loc);

            if (!room.Members.SafeContains(client.UserID))
            {
                room.AddMember(client.UserID);
                Core.Locations.Research(client.UserID);
            }

            // connect to new members
            ConnectRoom(room); 
        }

        public void JoinRoom(ChatRoom room)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(() => JoinRoom(room));
                return;
            }

            if ( IsCommandRoom(room.Kind))
            {
                JoinCommand(room.ProjectID, room.Kind);
                return;
            }

            room.Active = true;
            room.AddMember(Core.UserID);

            // for private rooms, send proof of invite first
            if (room.Kind == RoomKind.Secret)
                SendInviteProof(room);

            SendStatus(room);

            SendWhoRequest(room);

            ConnectRoom(room);

            if (room.PublishRoom)
                SetupPublic(room);

            Core.RunInGuiThread(Refresh);
            Core.RunInGuiThread(room.MembersUpdate);
        }

        public void JoinCommand(uint project, RoomKind kind)
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

        public void RefreshCommand(ChatRoom room) // sends status updates to all members of room
        {
            if (!IsCommandRoom(room.Kind))
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

        public void LeaveRoom(uint project, RoomKind kind)
        {
            // deactivates room, let timer remove object is good once we know user no longer wants it

            uint id = GetRoomID(project, kind);

            ChatRoom room = null;
            if (!RoomMap.SafeTryGetValue(id, out room))
                return;

            LeaveRoom(room);
        }

        public void LeaveRoom(ChatRoom room)
        {
            room.Active = false;

            room.RemoveMember(Core.UserID);

            SendStatus(room);

            //update interface
            Core.RunInGuiThread(Refresh);
            Core.RunInGuiThread(room.MembersUpdate);
        }

        public static bool IsCommandRoom(RoomKind kind)
        {
            return (kind == RoomKind.Command_High || kind == RoomKind.Command_Low ||
                    kind == RoomKind.Live_High || kind == RoomKind.Live_Low);
        }

        public uint GetRoomID(uint project, RoomKind kind)
        {
            return project + (uint)kind;
        }

        public ChatRoom GetRoom(uint project, RoomKind kind)
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

        public void Location_Update(LocationData location)
        {
            bool connect = false;

            foreach (ChatRoom room in FindRoom(location.UserID))
                if (room.Active)
                    connect = true;

            if(connect)
                Network.RudpControl.Connect(location); // func checks if already connected
        }

        public void SendMessage(ChatRoom room, string text, TextFormat format)
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
                    // also sends to other instances of self
                    foreach (ulong member in room.Members)
                        foreach (RudpSession session in Network.RudpControl.GetActiveSessions(member))
                        {
                            sent = true;
                            session.SendData(ServiceID, 0, message);
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

            ulong id = IsCommandRoom(message.Kind) ? GetRoomID(message.ProjectID, message.Kind) : message.RoomID;

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
                Core.MakeNews(ServiceIDs.Chat, Core.GetName(session.UserID) + " is chatting", session.UserID, 0, false);
            }

            ProcessMessage(room, new ChatMessage(Core, session, message));
        }

        public void Session_Update(RudpSession session)
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
                            if (room.Kind == RoomKind.Private || room.Host == Core.UserID)
                            {
                                session.SendData(ServiceID, 0, room.Invites[session.UserID].Param1);
                                room.Invites[session.UserID].Param2.Add(session.ClientID);
                                ProcessMessage(room, "Invite sent to " + GetNameAndLocation(session));
                                SendWhoResponse(room, session);
                            }
                            // else private room and we are not the host, send proof we belong here
                            else
                            {
                                SendInviteProof(room, session);
                            }

                        // ask member who else is in room
                        if (!IsCommandRoom(room.Kind) &&
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
                        if (room.Kind == RoomKind.Secret && !room.Verified.ContainsKey(session.UserID))
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
                        if (room.Kind == RoomKind.Secret && !room.Verified.ContainsKey(session.UserID))
                            continue;

                        status.ActiveRooms.Add(room.RoomID);
                    }
            });

            session.SendData(ServiceID, 0, status);
        }

        public void SendInviteRequest(ChatRoom room, ulong id)
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
            if (room.Kind == RoomKind.Secret)
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
                session.SendData(ServiceID, 0, invite);
                room.Invites[id].Param2.Add(session.ClientID);
                ProcessMessage(room, "Invite sent to " + GetNameAndLocation(session));
                SendStatus(room); // so we get added as active to new room invitee creates
                SendWhoResponse(room, session);
            }
        }

        public string GetNameAndLocation(DhtClient client)
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

            if (tried.Param2.Contains(session.ClientID))
                return;

            tried.Param2.Add(session.ClientID);

            ChatInvite invite = new ChatInvite();
            invite.RoomID = room.RoomID;
            invite.Title = room.Title;
            invite.SignedInvite = room.Invites[Core.UserID].Param1.SignedInvite;

            session.SendData(ServiceID, 0, invite);
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
                 RoomKind kind = invite.SignedInvite != null ? RoomKind.Secret : RoomKind.Private;
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
            if (room.Kind == RoomKind.Secret)
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
                Core.RunInGuiThread(NewInvite, session.UserID, room);
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
            session.SendData(ServiceID, 0, whoReq);
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
                session.SendData(ServiceID, 0, packet);
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
            if (IsCommandRoom(room.Kind) || (room.Kind == RoomKind.Secret && !room.Verified.ContainsKey(session.UserID)))
                return;

            if (!room.Active)
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

        public void Share_FileProcessed(SharedFile file, object arg)
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

        static ulong GetPublicRoomID(string name)
        {
            return BitConverter.ToUInt64(new SHA1Managed().ComputeHash(UTF8Encoding.UTF8.GetBytes(name.ToLowerInvariant())), 0);
        }
    }


    public enum RoomKind { Command_High, Command_Low, Live_High, Live_Low, Public, Private, Secret  }; // do not change order


    public delegate void MembersUpdateHandler();
    public delegate void ChatUpdateHandler(ChatMessage message);

    public class ChatRoom
    {
        public ulong    RoomID;
        public uint     ProjectID;
        public string   Title;
        public RoomKind Kind;
        public bool     IsLoop;
        public bool     Active;

        public bool     PublishRoom;
        public DateTime NextPublish;

        public ulong Host;
        // members in room by key, if online there will be elements in list for each location
        public ThreadedList<ulong> Members = new ThreadedList<ulong>();
        
        // for host this is a map of clients who have been sent invitations
        // for invitee this is a map of clients who have been sent proof that we are part of the room
        public Dictionary<ulong, Tuple<ChatInvite, List<ushort>>> Invites;
        public Dictionary<ulong, bool> Verified;

        public ThreadedList<ChatMessage> Log = new ThreadedList<ChatMessage>();

        public MembersUpdateHandler MembersUpdate;
        public ChatUpdateHandler    ChatUpdate;

        // per channel polling needs to be done because client may be still connected, leaving one channel, joining another


        public ChatRoom(RoomKind kind, uint project)
        {
            Debug.Assert(ChatService.IsCommandRoom(kind));

            Kind = kind;
            RoomID = project + (uint)kind;
            ProjectID = project;
        }

        public ChatRoom( RoomKind kind, ulong id, string title)
        {
            Debug.Assert( !ChatService.IsCommandRoom(kind) );

            Kind = kind;
            RoomID = id;
            Title = title;

            Invites = new Dictionary<ulong, Tuple<ChatInvite, List<ushort>>>();
            
            // public rooms are private rooms with static room ids
            if (Kind == RoomKind.Public)
            {
                PublishRoom = true;
                Kind = RoomKind.Private;
            }

            if (Kind == RoomKind.Secret)
                Verified = new Dictionary<ulong, bool>();
        }

        public int GetActiveMembers(ChatService chat)
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

        public bool NeedSendInvite(ulong id, ushort client)
        {
            return  Invites != null &&
                    Invites.ContainsKey(id) &&
                    !Invites[id].Param2.Contains(client);
        }

        public void AddMember(ulong user)
        {
            if(!Members.SafeContains(user))
                Members.SafeAdd(user);
        }

        public void RemoveMember(ulong user)
        {
            Members.SafeRemove(user);
        }
    }

    public class ChatMessage : DhtClient
    {
        public bool       System;
        public DateTime   TimeStamp;
        public string     Text;
        public TextFormat Format;
        public bool       Sent;


        public ChatMessage(OpCore core, string text, TextFormat format)
        {
            UserID = core.UserID;
            ClientID = core.Network.Local.ClientID;
            TimeStamp = core.TimeNow;
            Text = text;
            Format = format;
        }

        public ChatMessage(OpCore core, RudpSession session, ChatText text)
        {
            UserID = session.UserID;
            ClientID = session.ClientID;
            TimeStamp = core.TimeNow;
            Text = text.Text;
            Format = text.Format;
        }
    }
}