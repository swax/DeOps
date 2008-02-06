using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;

using RiseOp.Services.Trust;
using RiseOp.Services.Location;
using RiseOp.Services.Transfer;
using RiseOp.Services.Assist;


namespace RiseOp.Services.Profile
{
    internal delegate void ProfileUpdateHandler(OpProfile profile);
    

    class ProfileService : OpService
    {
        public string Name { get { return "Profile"; } }
        public ushort ServiceID { get { return 4; } }

        internal OpCore     Core;
        internal G2Protocol Protocol;
        internal DhtNetwork Network;
        internal DhtStore   Store;
        TrustService Links;

        internal string ExtractPath;
   
        internal OpProfile LocalProfile;
        internal ThreadedDictionary<ulong, OpProfile> ProfileMap = new ThreadedDictionary<ulong, OpProfile>();
        
        internal ProfileUpdateHandler ProfileUpdate;
        
        enum DataType { File = 1, Extracted = 2 };

        internal VersionedCache Cache;

        internal const string DefaultTemplate = @"
                                                <html>
                                                <body style='color: #000000'>
                                                    <table border='0' cellpadding='5' cellspacing='0' id='TABLE1' width='100%'>
                                                        <tr>
                                                            <td style='width: 122px;'>
                                                                <img height='110' src='<?file:Photo?>' style='border-right: black thin solid; border-top: black thin solid; border-left: black thin solid; border-bottom: black thin solid' /></td>
                                                            <td style='height: 124px'>
                                                                <span style='font-family: Tahoma'><strong><span style='font-size: 20pt'>
                                                                <?link:name?><br />
                                                                </span></strong><span style='font-size: 14pt'>
                                                                <?link:title?></span></span></td>
                                                        </tr>
                                                    </table>
                                                    <br />
                                                    <div style='width: 95%; background-color: #ff9900; padding: 5px; border-bottom-width: thin; border-bottom-color: white;'>
                                                    <strong><span style='font-size: 10pt; font-family: Tahoma; color: #ffffff;'>
                                                            Contact </span></strong>
                                                    </div>
                                                    <div style='width: 95%; background-color: #f8f8ff; padding: 5px;'>
                                                        <span style='font-family: Tahoma;font-size: 10pt;'>
                                                            <strong>&nbsp; Phone:</strong> <?text:Phone?><br />
                                                        
                                                        <br />  
	                                                  <strong>&nbsp; Email:</strong> <?text:Email?><br />
                                                        <br />
                                                            <strong>&nbsp; IM:</strong> <?text:IM?></span>
                                                    </div>
                                                    <br />
                                                    <div style='width: 95%; background-color: #ff9900; padding: 5px; border-bottom-width: thin; border-bottom-color: black;'>
                                                        <strong><span style='font-size: 10pt; font-family: Tahoma; color: #ffffff; border-bottom-width: thin; border-bottom-color: black;'>
                                                            Messages of the Day
                                                        </span></strong>
                                                    </div>
                                                    <div style='width: 95%; background-color:#f8f8ff; padding: 5px;'>
                                                    
	                                                <?motd:start?>

	                                                <div style='background-color:#f8f8ff; padding: 5px; margin-left: 15px;'>
		                                                <span style='font-family: Tahoma;font-size: 10pt;'>
		                                                    <b> <span style='font-size:10pt'><?link:name?> - <?link:title?></span></b><br />
		                                                    <?text:MOTD?>
                                                            <br />
                                                            <br />
		                                                </span>

		                                                <?motd:next?>
	                                                <div>

	                                                <?motd:end?>

                                                    </div>
                                                </body>
                                                </html>";


        internal ProfileService(OpCore core)
        {
            Core = core;
            Protocol = core.Protocol;
            Network = core.OperationNet;
            Store = Network.Store;
            Links = Core.Links;

            ExtractPath = Core.User.RootPath + Path.DirectorySeparatorChar +
                        "Data" + Path.DirectorySeparatorChar +
                        ServiceID.ToString() + Path.DirectorySeparatorChar +
                        ((ushort)DataType.Extracted).ToString();


            Cache = new VersionedCache(Network, ServiceID, (ushort)DataType.File, true);

            Cache.FileAquired += new FileAquiredHandler(Cache_FileAquired);
            Cache.FileRemoved += new FileRemovedHandler(Cache_FileRemoved);
            Cache.Load();

            if (!ProfileMap.SafeContainsKey(Core.LocalDhtID))
                SaveLocal(DefaultTemplate, null, null);
        }

        public void Dispose()
        {
            Cache.FileAquired -= new FileAquiredHandler(Cache_FileAquired);
            Cache.FileRemoved -= new FileRemovedHandler(Cache_FileRemoved);
            Cache.Dispose();
        }

        internal OpProfile GetProfile(ulong dhtid)
        {
            OpProfile profile = null;

            ProfileMap.SafeTryGetValue(dhtid, out profile);

            return profile;
        }

        public List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong user, uint project)
        {
            List<MenuItemInfo> menus = new List<MenuItemInfo>();

            if (menuType == InterfaceMenuType.Internal)
                menus.Add(new MenuItemInfo("Data/Profile", ProfileRes.IconX, new EventHandler(Menu_View)));

            if (menuType == InterfaceMenuType.External)
                menus.Add(new MenuItemInfo("Profile", ProfileRes.IconX, new EventHandler(Menu_View)));


            return menus;
        }

        internal void Menu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            ulong key = node.GetKey();

            if (Network.Routing.Responsive)
                Research(key);

            // gui creates viewshell, component just passes view object
            ProfileView view = new ProfileView(this, key, node.GetProject());

            Core.InvokeView(node.IsExternal(), view);
        }

        internal void SaveLocal(string template, Dictionary<string, string> textFields, Dictionary<string, string> fileFields)
        {
            try
            {
                RijndaelManaged key = new RijndaelManaged();
                key.GenerateKey();
                key.IV = new byte[key.IV.Length]; 

                string tempPath = Core.GetTempPath();
                FileStream tempFile = new FileStream(tempPath, FileMode.CreateNew);
                CryptoStream stream = new CryptoStream(tempFile, key.CreateEncryptor(), CryptoStreamMode.Write);
                int written = 0;

                // write template info
                byte[] htmlBytes = Core.Protocol.UTF.GetBytes(template);
                written += Protocol.WriteToFile(new ProfileAttachment("template", htmlBytes.Length), stream);
                

                // write fields info (convert into fields into packet list)
                List<byte[]> fieldPackets = new List<byte[]>();

                int fieldsTotalSize = 0;

                if(textFields != null)
                    foreach (KeyValuePair<string, string> pair in textFields)
                    {
                        if (pair.Value == null)
                            continue;

                        ProfileField field = new ProfileField();
                        field.Name = pair.Key;
                        field.Value = Core.Protocol.UTF.GetBytes(pair.Value);
                        field.FieldType = ProfileFieldType.Text;

                        byte[] packet = field.Encode(Core.Protocol);
                        fieldPackets.Add(packet);
                        fieldsTotalSize += packet.Length;
                    }

                if(fileFields != null)
                    foreach (KeyValuePair<string, string> pair in fileFields)
                    {
                        if (pair.Value == null)
                            continue;

                        ProfileField field = new ProfileField();
                        field.Name = pair.Key;
                        field.Value = Core.Protocol.UTF.GetBytes(Path.GetFileName(pair.Value));
                        field.FieldType = ProfileFieldType.File;

                        byte[] packet = field.Encode(Core.Protocol);
                        fieldPackets.Add(packet);
                        fieldsTotalSize += packet.Length;
                    }

                if(fieldsTotalSize > 0)
                    written += Protocol.WriteToFile(new ProfileAttachment("fields", fieldsTotalSize), stream); 


                // write files info
                if(fileFields != null)
                    foreach (string path in fileFields.Values)
                    {
                        FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                        written += Protocol.WriteToFile(new ProfileAttachment("file=" + Path.GetFileName(path), file.Length), stream); 

                        file.Close();
                    }

                stream.WriteByte(0); // end packets
                long embeddedStart = written + 1;

                // write template bytes
                stream.Write(htmlBytes, 0, htmlBytes.Length);

                // write field bytes
                foreach(byte[] packet in fieldPackets)
                    stream.Write(packet, 0, packet.Length);


                // write file bytes
                const int buffSize = 4096;
                byte[] buffer = new byte[buffSize];

                if (fileFields != null)
                    foreach (string path in fileFields.Values)
                    {
                        FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                        int read = buffSize;
                        while (read == buffSize)
                        {
                            read = file.Read(buffer, 0, buffSize);
                            stream.Write(buffer, 0, read);
                        }

                        file.Close();
                    }

                stream.FlushFinalBlock();
                stream.Close();


                OpVersionedFile vfile = Cache.UpdateLocal(tempPath, key, BitConverter.GetBytes(embeddedStart));

                Store.PublishDirect(Links.GetLocsAbove(), Core.LocalDhtID, ServiceID, (ushort)DataType.File, vfile.SignedHeader);
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Profile", "Error updating local " + ex.Message);
            }
        }


        void Cache_FileRemoved(OpVersionedFile file)
        {
            OpProfile profile = GetProfile(file.DhtID);

            if (profile != null)
                ProfileMap.SafeRemove(file.DhtID);
        }

        private void Cache_FileAquired(OpVersionedFile file)
        {
            // get profile
            OpProfile prevProfile = GetProfile(file.DhtID);

            OpProfile newProfile = new OpProfile(file);

            ProfileMap.SafeAdd(file.DhtID, newProfile);


            if (file.DhtID == Core.LocalDhtID)
                LocalProfile = newProfile;

            if ((newProfile == LocalProfile) || (prevProfile != null && prevProfile.Loaded))
                LoadProfile(newProfile.DhtID);


            // update subs
            if (Network.Established)
            {
                List<LocationData> locations = new List<LocationData>();

                Links.ProjectRoots.LockReading(delegate()
                {
                    foreach (uint project in Links.ProjectRoots.Keys)
                        if (newProfile.DhtID == Core.LocalDhtID || Links.IsHigher(newProfile.DhtID, project))
                            Links.GetLocsBelow(Core.LocalDhtID, project, locations);
                });

                Store.PublishDirect(locations, newProfile.DhtID, ServiceID, 0, file.SignedHeader);
            }

            if (ProfileUpdate != null)
                Core.RunInGuiThread(ProfileUpdate, newProfile);

            if (Core.NewsWorthy(newProfile.DhtID, 0, false))
                Core.MakeNews("Profile updated by " + Links.GetName(newProfile.DhtID), newProfile.DhtID, 0, true, ProfileRes.IconX, Menu_View);
        }

        internal void LoadProfile(ulong id)
        {
            OpProfile profile = GetProfile(id);

            if (profile == null)
                return;

            try
            {
                string path = GetFilePath(profile);

                if (!File.Exists(path))
                    return;

                profile.Attached = new List<ProfileAttachment>();

                FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                CryptoStream crypto = new CryptoStream(file, profile.File.Header.FileKey.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Core.Protocol, FileAccess.Read);

                G2Header root = null;

                while (stream.ReadPacket(ref root))
                    if (root.Name == ProfilePacket.Attachment)
                    {
                        ProfileAttachment packet = ProfileAttachment.Decode(Core.Protocol, root);

                        if (packet == null)
                            continue;

                        profile.Attached.Add(packet);
                    }

                stream.Close();

                profile.Loaded = true;
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Profile", "Error loading file " + ex.Message);
            }

        }

        internal void Research(ulong key)
        {
            Cache.Research(key);
        }

        internal string GetFilePath(OpProfile profile)
        {
            return Cache.GetFilePath(profile.File.Header);
        }
    }


    internal class OpProfile
    {
        internal OpVersionedFile File;

        internal bool Loaded;

        internal List<ProfileAttachment> Attached = new List<ProfileAttachment>();


        internal OpProfile(OpVersionedFile file)
        {
            File = file;
        }

        internal ulong DhtID
        {
            get
            {
                return File.DhtID;
            }
        }

        internal long EmbeddedStart
        {
            get
            {
                return BitConverter.ToInt64(File.Header.Extra, 0);
            }
        }
    }
}
