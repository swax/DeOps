using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;

using RiseOp.Services.Assist;
using RiseOp.Services.Location;


namespace RiseOp.Services.Buddy
{
    internal delegate void BuddyGuiUpdateHandler();

    class BuddyService : OpService
    {
        public string Name { get { return "Buddy"; } }
        public uint ServiceID { get { return 13; } }

        internal OpCore Core;
        internal DhtNetwork Network;

        internal VersionedCache Cache;

        internal ThreadedDictionary<ulong, OpBuddy> BuddyList = new ThreadedDictionary<ulong, OpBuddy>();

        internal BuddyGuiUpdateHandler GuiUpdate;


        internal BuddyService(OpCore core)
        {
            Core = core;
            Network = Core.Network;
            Core.Buddies = this;

            Network.StatusChange += new StatusChange(Network_StatusChange);
            Core.KeepDataCore += new KeepDataHandler(Core_KeepData);
            Core.Locations.KnowOnline += new KnowOnlineHandler(Location_KnowOnline);

            Cache = new VersionedCache(Network, ServiceID, 0, true);

            Cache.FileAquired += new FileAquiredHandler(Cache_FileAquired);
            Cache.Load();

            if(!BuddyList.SafeContainsKey(Network.Local.UserID))
            {
                OpBuddy self = new OpBuddy() { ID = Network.Local.UserID, Name = Core.User.Settings.UserName, Group = "", Key = Core.User.Settings.KeyPublic };
                BuddyList.SafeAdd(Network.Local.UserID, self);
            }
        }

        public void Dispose()
        {
            Network.StatusChange     -= new StatusChange(Network_StatusChange);
            Core.KeepDataCore      -= new KeepDataHandler(Core_KeepData);
            Core.Locations.KnowOnline -= new KnowOnlineHandler(Location_KnowOnline);

            Cache.FileAquired -= new FileAquiredHandler(Cache_FileAquired);
        }

        public List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong user, uint project)
        {
            if (menuType != InterfaceMenuType.Quick)
                return null;

            List<MenuItemInfo> menus = new List<MenuItemInfo>();

            if (BuddyList.SafeContainsKey(user))
                menus.Add(new MenuItemInfo("Remove Buddy", null, new EventHandler(Menu_Remove)));
            else
                menus.Add(new MenuItemInfo("Add Buddy", null, new EventHandler(Menu_Add)));

            return menus;
        }

        private void Menu_Add(object sender, EventArgs e)
        {
            if (!(sender is IViewParams) || Core.GuiMain == null)
                return;

            ulong user = ((IViewParams)sender).GetKey();
            uint project = ((IViewParams)sender).GetProject();


            string name = Core.GetName(user);

            AddBuddy(name, "", Core.KeyMap[user]);
        }

        private void Menu_Remove(object sender, EventArgs e)
        {
            if (!(sender is IViewParams) || Core.GuiMain == null)
                return;

            ulong user = ((IViewParams)sender).GetKey();
            uint project = ((IViewParams)sender).GetProject();

            RemoveBuddy(user);
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
            if (!Network.Established)
                return;

            // look for all buddies on network
            ForAllUsers(id => Core.Locations.Research(id));
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
                users.Union(BuddyList.Keys));
        }

        void ForAllUsers(Action<ulong> action)
        {
            BuddyList.LockReading(() =>
            {
                foreach (ulong id in BuddyList.Keys)
                    action(id);
            });
        }

        internal string GetLink(ulong user)
        {
            OpBuddy buddy;
            if(!BuddyList.TryGetValue(user, out buddy))
                return null;

            string link = "riseop://" + Core.GetName(user) + "/";

            link += Utilities.ToBase64String(BitConverter.GetBytes(Network.OpID)) + "/";

            link += Utilities.ToBase64String(buddy.Key);

            return link;
        }

        internal void AddBuddy(string link)
        {
            link = link.Replace("riseop://", "");

            string[] parts = link.Split('/');

            if (parts.Length < 3)
                return;

            ulong opID = BitConverter.ToUInt64(Utilities.FromBase64String(parts[1]), 0);

            if (opID != Network.OpID)
                throw new Exception("This buddy link is not for " + Core.User.Settings.Operation);

            byte[] key = Utilities.FromBase64String(parts[2]);

            AddBuddy(parts[0], "", key);
        }

        internal void AddBuddy(string name, string group, byte[] key)
        {
            ulong id = Utilities.KeytoID(key);
           
            OpBuddy buddy = new OpBuddy() { ID = id, Name = name, Group = group, Key = key };

            BuddyList.SafeAdd(id, buddy);

            Core.IndexName(id, name); // always associate this buddy with name

            SaveLocal();
        }

        internal void RemoveBuddy(ulong user)
        {
            if (!BuddyList.SafeContainsKey(user))
                return;

            BuddyList.SafeRemove(user);

            SaveLocal();
        }

        private void SaveLocal()
        {
            // create temp, write buddy list
            string tempPath = Core.GetTempPath();
            byte[] key = Utilities.GenerateKey(Core.StrongRndGen, 256);
            using (IVCryptoStream crypto = IVCryptoStream.Save(tempPath, key))
            {
                PacketStream stream = new PacketStream(crypto, Network.Protocol, FileAccess.Write);

                BuddyList.LockReading(delegate()
                {
                    foreach (OpBuddy buddy in BuddyList.Values)
                        stream.WritePacket(buddy);
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
                        BuddyList.SafeAdd(id, buddy);
                    }
            }

            Core.RunInGuiThread(GuiUpdate);
        }

    }

    internal class BuddyPacket
    {
        internal const byte Buddy = 0x10;
    }

    class OpBuddy : G2Packet
    {
        const byte Packet_Name = 0x10;
        const byte Packet_Group = 0x20;


        internal string Name;
        internal string Group;
        internal byte[] Key;
        internal ulong  ID;

        internal OpBuddy() { }

        internal override byte[] Encode(G2Protocol protocol)
        {
            lock (protocol.WriteSection)
            {
                G2Frame buddy = protocol.WritePacket(null, BuddyPacket.Buddy, Key);

                protocol.WritePacket(buddy, Packet_Name, UTF8Encoding.UTF8.GetBytes(Name));

                if(Group != null)
                    protocol.WritePacket(buddy, Packet_Group, UTF8Encoding.UTF8.GetBytes(Group));

                return protocol.WriteFinish();
            }
        }

        internal static OpBuddy Decode(G2Header root)
        {
            OpBuddy buddy = new OpBuddy();

            if (G2Protocol.ReadPayload(root))
            {
                buddy.Key = Utilities.ExtractBytes(root.Data, root.PayloadPos, root.PayloadSize);
                buddy.ID = Utilities.KeytoID(buddy.Key);
                G2Protocol.ResetPacket(root);
            }

            G2Header child = new G2Header(root.Data);

            while (G2Protocol.ReadNextChild(root, child) == G2ReadResult.PACKET_GOOD)
            {
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
