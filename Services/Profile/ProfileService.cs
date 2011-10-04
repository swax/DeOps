using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;

using DeOps.Services.Trust;
using DeOps.Services.Location;
using DeOps.Services.Transfer;
using DeOps.Services.Assist;


namespace DeOps.Services.Profile
{
    internal delegate void ProfileUpdateHandler(OpProfile profile);
    

    class ProfileService : OpService
    {
        public string Name { get { return "Profile"; } }
        public uint ServiceID { get { return (uint)ServiceIDs.Profile; } }

        const uint DataTypeFile = 0x01;
        const uint DataTypeExtracted = 0x02;

        internal OpCore     Core;
        internal G2Protocol Protocol;
        internal DhtNetwork Network;
        internal DhtStore   Store;
        TrustService Trust;

        internal string ExtractPath;
   
        internal OpProfile LocalProfile;
        internal ThreadedDictionary<ulong, OpProfile> ProfileMap = new ThreadedDictionary<ulong, OpProfile>();
        
        internal ProfileUpdateHandler ProfileUpdate;

        internal VersionedCache Cache;

        internal const string DefaultTemplate = @"
                                                <html>
                                                <body style='color: #000000'>
                                                    <div style='float:right'><font style='font-family:Tahoma; font-size: 9pt; color:red;'><?text:local_help?></font></div>
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
	                                                  <strong>&nbsp; Public Email:</strong> <?text:Email?><br />
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
            Network = core.Network;
            Protocol = Network.Protocol;
            Store = Network.Store;
            Trust = Core.Trust;

            ExtractPath = Core.User.RootPath + Path.DirectorySeparatorChar +
                        "Data" + Path.DirectorySeparatorChar +
                        ServiceID.ToString() + Path.DirectorySeparatorChar +
                        DataTypeExtracted.ToString();

            Cache = new VersionedCache(Network, ServiceID, DataTypeFile, false);

            Cache.FileAquired += new FileAquiredHandler(Cache_FileAquired);
            Cache.FileRemoved += new FileRemovedHandler(Cache_FileRemoved);
            Cache.Load();

            if (!ProfileMap.SafeContainsKey(Core.UserID))
                SaveLocal(DefaultTemplate, null, null);
        }

        public void Dispose()
        {
            Cache.FileAquired -= new FileAquiredHandler(Cache_FileAquired);
            Cache.FileRemoved -= new FileRemovedHandler(Cache_FileRemoved);
            Cache.Dispose();
        }

        internal OpProfile GetProfile(ulong id)
        {
            OpProfile profile = null;

            ProfileMap.SafeTryGetValue(id, out profile);

            return profile;
        }

        public void GetMenuInfo(InterfaceMenuType menuType, List<MenuItemInfo> menus, ulong user, uint project)
        {
            if (menuType == InterfaceMenuType.Internal)
                menus.Add(new MenuItemInfo("Data/Profile", ProfileRes.IconX, new EventHandler(Menu_View)));

            if (menuType == InterfaceMenuType.External)
                menus.Add(new MenuItemInfo("Profile", ProfileRes.IconX, new EventHandler(Menu_View)));
        }

        internal void Menu_View(object sender, EventArgs args)
        {
            IViewParams node = sender as IViewParams;

            if (node == null)
                return;

            ulong key = node.GetUser();

            if (Network.Responsive)
                Research(key);

            // gui creates viewshell, component just passes view object
            ProfileView view = new ProfileView(this, key, node.GetProject());

            Core.InvokeView(node.IsExternal(), view);
        }

        internal void SaveLocal(string template, Dictionary<string, string> textFields, Dictionary<string, string> fileFields)
        {
            try
            {
                long embeddedStart = 0;

                string tempPath = Core.GetTempPath();
                byte[] key = Utilities.GenerateKey(Core.StrongRndGen, 256);
                using (IVCryptoStream stream = IVCryptoStream.Save(tempPath, key))
                {
                    int written = 0;

                    // write template info
                    byte[] htmlBytes = UTF8Encoding.UTF8.GetBytes(template);
                    written += Protocol.WriteToFile(new ProfileAttachment("template", htmlBytes.Length), stream);


                    // write fields info (convert into fields into packet list)
                    List<byte[]> fieldPackets = new List<byte[]>();

                    int fieldsTotalSize = 0;

                    if (textFields != null)
                        foreach (KeyValuePair<string, string> pair in textFields)
                        {
                            if (pair.Value == null)
                                continue;

                            ProfileField field = new ProfileField();
                            field.Name = pair.Key;
                            field.Value = UTF8Encoding.UTF8.GetBytes(pair.Value);
                            field.FieldType = ProfileFieldType.Text;

                            byte[] packet = field.Encode(Network.Protocol);
                            fieldPackets.Add(packet);
                            fieldsTotalSize += packet.Length;
                        }

                    if (fileFields != null)
                        foreach (KeyValuePair<string, string> pair in fileFields)
                        {
                            if (pair.Value == null)
                                continue;

                            ProfileField field = new ProfileField();
                            field.Name = pair.Key;
                            field.Value = UTF8Encoding.UTF8.GetBytes(Path.GetFileName(pair.Value));
                            field.FieldType = ProfileFieldType.File;

                            byte[] packet = field.Encode(Network.Protocol);
                            fieldPackets.Add(packet);
                            fieldsTotalSize += packet.Length;
                        }

                    if (fieldsTotalSize > 0)
                        written += Protocol.WriteToFile(new ProfileAttachment("fields", fieldsTotalSize), stream);


                    // write files info
                    if (fileFields != null)
                        foreach (string path in fileFields.Values)
                            using (FileStream file = File.OpenRead(path))
                                written += Protocol.WriteToFile(new ProfileAttachment("file=" + Path.GetFileName(path), file.Length), stream);


                    stream.WriteByte(0); // end packets
                    embeddedStart = written + 1;

                    // write template bytes
                    stream.Write(htmlBytes, 0, htmlBytes.Length);

                    // write field bytes
                    foreach (byte[] packet in fieldPackets)
                        stream.Write(packet, 0, packet.Length);


                    // write file bytes
                    const int buffSize = 4096;
                    byte[] buffer = new byte[buffSize];

                    if (fileFields != null)
                        foreach (string path in fileFields.Values)
                            using (FileStream file = File.OpenRead(path))
                            {
                                int read = buffSize;
                                while (read == buffSize)
                                {
                                    read = file.Read(buffer, 0, buffSize);
                                    stream.Write(buffer, 0, read);
                                }
                            }

                    stream.FlushFinalBlock();
                }

                OpVersionedFile vfile = Cache.UpdateLocal(tempPath, key, BitConverter.GetBytes(embeddedStart));

                Store.PublishDirect(Trust.GetLocsAbove(), Core.UserID, ServiceID, DataTypeFile, vfile.SignedHeader);
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Profile", "Error updating local " + ex.Message);
            }
        }


        void Cache_FileRemoved(OpVersionedFile file)
        {
            OpProfile profile = GetProfile(file.UserID);

            if (profile != null)
                ProfileMap.SafeRemove(file.UserID);
        }

        private void Cache_FileAquired(OpVersionedFile file)
        {
            // get profile
            OpProfile prevProfile = GetProfile(file.UserID);

            OpProfile newProfile = new OpProfile(file);

            ProfileMap.SafeAdd(file.UserID, newProfile);


            if (file.UserID == Core.UserID)
                LocalProfile = newProfile;

            if ((newProfile == LocalProfile) || (prevProfile != null && prevProfile.Loaded))
                LoadProfile(newProfile.UserID);


            // update subs
            if (Network.Established)
            {
                List<LocationData> locations = new List<LocationData>();

                Trust.ProjectRoots.LockReading(delegate()
                {
                    foreach (uint project in Trust.ProjectRoots.Keys)
                        if (newProfile.UserID == Core.UserID || Trust.IsHigher(newProfile.UserID, project))
                            Trust.GetLocsBelow(Core.UserID, project, locations);
                });

                Store.PublishDirect(locations, newProfile.UserID, ServiceID, 0, file.SignedHeader);
            }

            if (ProfileUpdate != null)
                Core.RunInGuiThread(ProfileUpdate, newProfile);

            if (Core.NewsWorthy(newProfile.UserID, 0, false))
                Core.MakeNews("Profile updated by " + Core.GetName(newProfile.UserID), newProfile.UserID, 0, true, ProfileRes.IconX, Menu_View);
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

                using(TaggedStream file = new TaggedStream(path, Network.Protocol))
                using (IVCryptoStream crypto = IVCryptoStream.Load(file, profile.File.Header.FileKey))
                {
                    PacketStream stream = new PacketStream(crypto, Network.Protocol, FileAccess.Read);

                    G2Header root = null;

                    while (stream.ReadPacket(ref root))
                        if (root.Name == ProfilePacket.Attachment)
                        {
                            ProfileAttachment packet = ProfileAttachment.Decode(root);

                            if (packet == null)
                                continue;

                            profile.Attached.Add(packet);
                        }
                }

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

        public void SimTest()
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { SimTest(); });
                return;
            }

            // Email, IM, Phone, MOTD

            Dictionary<string, string> textFields = new Dictionary<string, string>();

            textFields["Email"] = Core.User.Settings.UserName.Replace(' ', '_') + "@" + Core.User.Settings.Operation + ".com";
            textFields["IM"] = Core.User.Settings.UserName.Split(' ')[0] + Core.RndGen.Next(100).ToString();

            uint project = 0;
            
            textFields["MOTD-" + project] = Core.TextGen.GenerateParagraphs(1, NLipsum.Core.Paragraph.Short)[0];

            string phone = "";
            for (int i = 0; i < 3; i++)
                phone += Core.RndGen.Next(1, 9).ToString();
            phone += "-";
            for (int i = 0; i < 3; i++)
                phone += Core.RndGen.Next(1, 9).ToString();
            phone += "-";
            for (int i = 0; i < 4; i++)
                phone += Core.RndGen.Next(1, 9).ToString();

            textFields["Phone"] = phone;

            SaveLocal(DefaultTemplate, textFields, null);

        }

        public void SimCleanup()
        {
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

        internal ulong UserID
        {
            get
            {
                return File.UserID;
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
