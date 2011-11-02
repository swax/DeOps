using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;

using DeOps.Services.Assist;
using DeOps.Services.Location;


namespace DeOps.Services.Buddy
{
    public delegate void BuddyGuiUpdateHandler();

    public class BuddyService : OpService
    {
        public string Name { get { return "Buddy"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.Buddy; } }

        public OpCore Core;
        public DhtNetwork Network;

        public VersionedCache Cache;

        public OpBuddy LocalBuddy;
        public ThreadedDictionary<ulong, OpBuddy> BuddyList = new ThreadedDictionary<ulong, OpBuddy>();
        public ThreadedDictionary<ulong, OpBuddy> IgnoreList = new ThreadedDictionary<ulong, OpBuddy>();

        public BuddyGuiUpdateHandler GuiUpdate;

        public bool SaveList;


        public BuddyService(OpCore core)
        {
            Core = core;
            Network = Core.Network;
            Core.Buddies = this;

            Network.CoreStatusChange += new StatusChange(Network_StatusChange);
            Core.KeepDataCore += new KeepDataHandler(Core_KeepData);
            Core.Locations.KnowOnline += new KnowOnlineHandler(Location_KnowOnline);
            Core.MinuteTimerEvent += Core_MinuteTimer;
            Cache = new VersionedCache(Network, ServiceID, 0, false);

            Cache.FileAquired += new FileAquiredHandler(Cache_FileAquired);
            Cache.Load();

            if(!BuddyList.SafeContainsKey(Core.UserID))
                AddBuddy(Core.User.Settings.UserName, Core.User.Settings.KeyPublic);

            BuddyList.SafeTryGetValue(Core.UserID, out LocalBuddy);
        }

        public void Dispose()
        {
            Network.CoreStatusChange     -= new StatusChange(Network_StatusChange);
            Core.KeepDataCore      -= new KeepDataHandler(Core_KeepData);
            Core.Locations.KnowOnline -= new KnowOnlineHandler(Location_KnowOnline);
            Core.MinuteTimerEvent -= Core_MinuteTimer;
            Cache.FileAquired -= new FileAquiredHandler(Cache_FileAquired);
        }

        public void SimTest()
        {
            throw new NotImplementedException();
        }

        public void SimCleanup()
        {
            throw new NotImplementedException();
        }

        void Network_StatusChange()
        {
            if (!Network.Responsive)
                return;

            // look for all buddies on network
            ForAllUsers(id => Core.Locations.Research(id));
        }

        void Core_MinuteTimer()
        {
            if (SaveList)
            {
                SaveLocal();
                SaveList = false;
            }
        }

        void Core_KeepData()
        {
            // keep data of our buddies
            ForAllUsers(id => Core.KeepData.SafeAdd(id, true));
        }

        void Location_KnowOnline(List<ulong> users)
        {
            // keep aware if our buddies are online or not
            BuddyList.LockReading( () =>
                users.AddRange(BuddyList.Keys));
        }

        void ForAllUsers(Action<ulong> action)
        {
            BuddyList.LockReading(() =>
            {
                foreach (ulong id in BuddyList.Keys)
                    action(id);
            });
        }

        public OpBuddy AddBuddy(ulong user)
        {
            return AddBuddy(Core.GetName(user), Core.KeyMap[user]);
        }

        public OpBuddy AddBuddy(string link)
        {
            IdentityLink ident = IdentityLink.Decode(link);

            if (!Utilities.MemCompare(ident.PublicOpID, Core.User.Settings.PublicOpID))
                throw new Exception("This buddy link is not for " + Core.User.Settings.Operation);

            return AddBuddy(ident.Name, ident.PublicKey);
        }

        public OpBuddy AddBuddy(string name, byte[] key)
        {
            ulong id = Utilities.KeytoID(key);

            OpBuddy buddy;
            if (BuddyList.TryGetValue(id, out buddy))
                return buddy;

            buddy = new OpBuddy() { ID = id, Name = name, Key = key };

            BuddyList.SafeAdd(id, buddy);

            Core.IndexName(id, name); // always associate this buddy with name

            SaveList = true;
            Core.RunInGuiThread(GuiUpdate);

            Core.Locations.Research(id);

            return buddy;
        }

        public void RemoveBuddy(ulong user)
        {
            if (!BuddyList.SafeContainsKey(user))
                return;

            BuddyList.SafeRemove(user);

            SaveList = true;
            Core.RunInGuiThread(GuiUpdate);
        }

        public void SaveLocal()
        {
            // create temp, write buddy list
            string tempPath = Core.GetTempPath();
            byte[] key = Utilities.GenerateKey(Core.StrongRndGen, 256);
            using (IVCryptoStream crypto = IVCryptoStream.Save(tempPath, key))
            {
                PacketStream stream = new PacketStream(crypto, Network.Protocol, FileAccess.Write);

                BuddyList.LockReading(() =>
                {
                    foreach (OpBuddy buddy in BuddyList.Values)
                        stream.WritePacket(buddy);
                });

                IgnoreList.LockReading(() => // we need to ensure we have name/key for each ignore
                {
                    foreach (OpBuddy ignore in IgnoreList.Values)
                        stream.WritePacket(ignore);
                });
            }

            byte[] publicEncryptedKey = Core.User.Settings.KeyPair.Encrypt(key, false); //private key required to decrypt buddy list

            Cache.UpdateLocal(tempPath, publicEncryptedKey, null);
        }

        private void Cache_FileAquired(OpVersionedFile file)
        {
            if (file.UserID != Network.Local.UserID)
                return;

            // only we can open the buddly list stored on the network
            byte[] key = Core.User.Settings.KeyPair.Decrypt(file.Header.FileKey, false);

            using (TaggedStream tagged = new TaggedStream(Cache.GetFilePath(file.Header), Network.Protocol))
            using (IVCryptoStream crypto = IVCryptoStream.Load(tagged, key))
            {
                BuddyList.SafeClear();

                PacketStream stream = new PacketStream(crypto, Network.Protocol, FileAccess.Read);

                G2Header root = null;

                while (stream.ReadPacket(ref root))
                    if (root.Name == BuddyPacket.Buddy)
                    {
                        OpBuddy buddy = OpBuddy.Decode(root);
                        ulong id = Utilities.KeytoID(buddy.Key);

                        Core.IndexKey(id, ref buddy.Key);
                        Core.IndexName(id, buddy.Name);

                        if(buddy.Ignored)
                            IgnoreList.SafeAdd(id, buddy);
                        else
                            BuddyList.SafeAdd(id, buddy);
                    }
            }

            Core.RunInGuiThread(GuiUpdate);
        }


        public void AddtoGroup(ulong user, string group)
        {
            OpBuddy buddy;
            if (!BuddyList.SafeTryGetValue(user, out buddy))
                return;

            buddy.Group = group;

            SaveList = true;

            Core.RunInGuiThread(GuiUpdate);
        }

        public void RemoveGroup(string group)
        {
            BuddyList.LockReading(delegate()
            {
                foreach (OpBuddy buddy in BuddyList.Values.Where(b => b.Group == group))
                    buddy.Group = null;
            });

            Core.RunInGuiThread(GuiUpdate);
        }

        public void RenameGroup(string oldName, string newName)
        {
            BuddyList.LockReading(delegate()
            {
                foreach (OpBuddy buddy in BuddyList.Values.Where(b => b.Group == oldName))
                    buddy.Group = newName;
            });

            Core.RunInGuiThread(GuiUpdate);
        }

        public void Ignore(ulong user, bool ignore)
        {
            if (ignore)
            {
                OpBuddy buddy = new OpBuddy()
                {
                    ID = user,
                    Name = Core.GetName(user),
                    Key = Core.KeyMap[user], //crit - not thread safe, change key map?
                    Ignored = true
                };

                IgnoreList.SafeAdd(user, buddy);
            }
            else
                IgnoreList.SafeRemove(user);


            SaveList = true;

            Core.RunInGuiThread(GuiUpdate);
        }
    }

    public class BuddyPacket
    {
        public const byte Buddy = 0x10;
    }

    public class OpBuddy : G2Packet
    {
        const byte Packet_Name = 0x10;
        const byte Packet_Group = 0x20;
        const byte Packet_Ignored = 0x30;


        public string Name;
        public string Group;
        public byte[] Key;
        public ulong  ID;
        public bool  Ignored;

        public OpBuddy() { }

        public override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame buddy = protocol.WritePacket(null, BuddyPacket.Buddy, Key);

                protocol.WritePacket(buddy, Packet_Name, UTF8Encoding.UTF8.GetBytes(Name));

                if(Group != null)
                    protocol.WritePacket(buddy, Packet_Group, UTF8Encoding.UTF8.GetBytes(Group));

                if(Ignored)
                    protocol.WritePacket(buddy, Packet_Ignored, null);

                return protocol.WriteFinish();
            }
        }

        public static OpBuddy Decode(G2Header root)
        {
            OpBuddy buddy = new OpBuddy();

            if (G2Protocol.ReadPayload(root))
            {
                buddy.Key = Utilities.ExtractBytes(root.Data, root.PayloadPos, root.PayloadSize);
                buddy.ID = Utilities.KeytoID(buddy.Key);
            }

            G2Protocol.ResetPacket(root);

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
                if (child.Name == Packet_Ignored)
                {
                    buddy.Ignored = true;
                    continue;
                }

                if (!G2Protocol.ReadPayload(child))
                    continue;

                switch (child.Name)
                {
                    case Packet_Name:
                        buddy.Name = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;

                    case Packet_Group:
                        buddy.Group = UTF8Encoding.UTF8.GetString(child.Data, child.PayloadPos, child.PayloadSize);
                        break;
                }
            }

            return buddy;
        }
    }
}
