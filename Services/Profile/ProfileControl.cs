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

using DeOps.Services.Link;
using DeOps.Services.Location;
using DeOps.Services.Transfer;


namespace DeOps.Services.Profile
{
    internal delegate void ProfileUpdateHandler(OpProfile profile);
    

    class ProfileControl : OpComponent
    {
        internal OpCore     Core;
        internal G2Protocol Protocol;
        internal DhtNetwork Network;
        internal DhtStore   Store;
        LinkControl Links;

        internal string ProfilePath;
        
        internal OpProfile LocalProfile;
        internal ThreadedDictionary<ulong, OpProfile> ProfileMap = new ThreadedDictionary<ulong, OpProfile>();
        
        internal ProfileUpdateHandler ProfileUpdate;
        
        internal bool RunSaveHeaders;
        RijndaelManaged LocalFileKey;

        internal int PruneSize = 100;

        internal string DefaultTemplate;

        Dictionary<ulong, DateTime> NextResearch = new Dictionary<ulong, DateTime>();
        Dictionary<ulong, uint> DownloadLater = new Dictionary<ulong, uint>();
        

        internal ProfileControl(OpCore core)
        {
            Core = core;
            Protocol = core.Protocol;
            Core.Profiles = this;
            Network = core.OperationNet;
            Store = Network.Store;

            Core.LoadEvent  += new LoadHandler(Core_Load);
            Core.TimerEvent += new TimerHandler(Core_Timer);

            Network.EstablishedEvent += new EstablishedHandler(Network_Established);

            Store.StoreEvent[ComponentID.Profile] = new StoreHandler(Store_Local);
            Store.ReplicateEvent[ComponentID.Profile] = new ReplicateHandler(Store_Replicate);
            Store.PatchEvent[ComponentID.Profile] = new PatchHandler(Store_Patch);

            Network.Searches.SearchEvent[ComponentID.Profile] = new SearchRequestHandler(Search_Local);

            if (Core.Sim != null)
                PruneSize = 25;

            DefaultTemplate = @"
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

        }

        void Core_Load()
        {
            Links = Core.Links;

            Core.Transfers.FileSearch[ComponentID.Profile] = new FileSearchHandler(Transfers_FileSearch);
            Core.Transfers.FileRequest[ComponentID.Profile] = new FileRequestHandler(Transfers_FileRequest);

            ProfilePath = Core.User.RootPath + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + ComponentID.Profile.ToString();
            Directory.CreateDirectory(ProfilePath);

            LocalFileKey = Core.User.Settings.FileKey;

            LoadHeaders();


            if (!ProfileMap.SafeContainsKey(Core.LocalDhtID))
                SaveLocal(DefaultTemplate, null, null);

            //crit - delete
            //Dictionary<string, string> test = new Dictionary<string, string>();
            //test["Photo"] = @"C:\Dev\De-Ops\Graphics\guy.jpg";
            //SaveLocal(DefaultTemplate, null, test);
        }

        void Core_Timer()
        {
            if (RunSaveHeaders)
                SaveHeaders();


            // clean download later map
            if (!Network.Established)
                Utilities.PruneMap(DownloadLater, Core.LocalDhtID, PruneSize);


            // do below once per minute
            if (Core.TimeNow.Second != 0)
                return;


            // prune
            List<ulong> removeIDs = new List<ulong>();

            ProfileMap.LockReading(delegate()
            {
                if (ProfileMap.Count > PruneSize)
                    foreach (OpProfile profile in ProfileMap.Values)
                        if (profile.DhtID != Core.LocalDhtID &&
                            !Core.Links.TrustMap.ContainsKey(profile.DhtID) &&
                            !Utilities.InBounds(profile.DhtID, profile.DhtBounds, Core.LocalDhtID))
                            removeIDs.Add(profile.DhtID);
            });

            if (removeIDs.Count > 0)
                ProfileMap.LockWriting(delegate()
                {
                    while (removeIDs.Count > 0 && ProfileMap.Count > PruneSize / 2)
                    {
                        ulong furthest = Core.LocalDhtID;
                        OpProfile profile = ProfileMap[furthest];

                        foreach (ulong id in removeIDs)
                            if ((id ^ Core.LocalDhtID) > (furthest ^ Core.LocalDhtID))
                                furthest = id;

                        if (profile.Header != null)
                            try { File.Delete(GetFilePath(profile.Header)); }
                            catch { }

                        ProfileMap.Remove(furthest);
                        removeIDs.Remove(furthest);
                        RunSaveHeaders = true;
                    }
                });
            
            // clean research map
            removeIDs.Clear();

            foreach (KeyValuePair<ulong, DateTime> pair in NextResearch)
                if (Core.TimeNow > pair.Value)
                    removeIDs.Add(pair.Key);

            if (removeIDs.Count > 0)
                foreach (ulong id in removeIDs)
                    NextResearch.Remove(id);
        }

        void Network_Established()
        {
            ulong localBounds = Store.RecalcBounds(Core.LocalDhtID);

            // set bounds for objects
            ProfileMap.LockReading(delegate()
            {
                foreach (OpProfile profile in ProfileMap.Values)
                {
                    profile.DhtBounds = Store.RecalcBounds(profile.DhtID);

                    // republish objects that were not seen on the network during startup
                    if (profile.Unique && Utilities.InBounds(Core.LocalDhtID, localBounds, profile.DhtID))
                        Store.PublishNetwork(profile.DhtID, ComponentID.Profile, profile.SignedHeader);
                }
            });

            // only download those objects in our local area
            foreach (KeyValuePair<ulong, uint> pair in DownloadLater)
                if (Utilities.InBounds(Core.LocalDhtID, localBounds, pair.Key))
                    StartSearch(pair.Key, pair.Value);

            DownloadLater.Clear();
        }

        internal OpProfile GetProfile(ulong dhtid)
        {
            OpProfile profile = null;

            ProfileMap.SafeTryGetValue(dhtid, out profile);

            return profile;
        }

        internal override List<MenuItemInfo> GetMenuInfo(InterfaceMenuType menuType, ulong key, uint proj)
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

            if (Network.Routing.Responsive())
                Research(key);

            // gui creates viewshell, component just passes view object
            ProfileView view = new ProfileView(this, key, node.GetProject());

            Core.InvokeView(node.IsExternal(), view);
        }

        private void StartSearch(ulong key, uint version)
        {
            byte[] parameters = BitConverter.GetBytes(version);
            DhtSearch search = Network.Searches.Start(key, "Profile", ComponentID.Profile, parameters, new EndSearchHandler(EndSearch));

            if (search != null)
                search.TargetResults = 2;
        }

        void EndSearch(DhtSearch search)
        {
            foreach (SearchValue found in search.FoundValues)
                Store_Local(new DataReq(found.Sources, search.TargetID, ComponentID.Profile, found.Value));
        }

        List<byte[]> Search_Local(ulong key, byte[] parameters)
        {
            List<Byte[]> results = new List<byte[]>();

            uint minVersion = BitConverter.ToUInt32(parameters, 0);

            OpProfile profile = GetProfile(key);

            if (profile != null)
                if (profile.Header.Version >= minVersion)
                    results.Add(profile.SignedHeader);


            return results;
        }

        bool Transfers_FileSearch(ulong key, FileDetails details)
        {
            OpProfile profile = GetProfile(key);

            if (profile != null)
                if (details.Size == profile.Header.FileSize && Utilities.MemCompare(details.Hash, profile.Header.FileHash))
                    return true;

            return false;
        }

        string Transfers_FileRequest(ulong key, FileDetails details)
        {
            OpProfile profile = GetProfile(key);

            if (profile != null)
                if (details.Size == profile.Header.FileSize && Utilities.MemCompare(details.Hash, profile.Header.FileHash))
                    return GetFilePath(profile.Header);

            return null;
        }

        void Store_Local(DataReq store)
        {
            // getting published to - search results - patch

            SignedData signed = SignedData.Decode(Core.Protocol, store.Data);
            ProfileHeader header = ProfileHeader.Decode(Core.Protocol, signed.Data);

            Process_ProfileHeader(null, signed, header);
        }

        const int PatchEntrySize = 12;

        ReplicateData Store_Replicate(DhtContact contact, bool add)
        {
            if (!Network.Established)
                return null;


            ReplicateData data = new ReplicateData(ComponentID.Profile, PatchEntrySize);

            byte[] patch = new byte[PatchEntrySize];

            ProfileMap.LockReading(delegate()
            {
                foreach (OpProfile profile in ProfileMap.Values)
                    if (Utilities.InBounds(profile.DhtID, profile.DhtBounds, contact.DhtID))
                    {
                        DhtContact target = contact;
                        profile.DhtBounds = Store.RecalcBounds(profile.DhtID, add, ref target);

                        if (target != null)
                        {
                            BitConverter.GetBytes(profile.DhtID).CopyTo(patch, 0);
                            BitConverter.GetBytes(profile.Header.Version).CopyTo(patch, 8);

                            data.Add(target, patch);
                        }
                    }
            });

            return data;
        }

        void Store_Patch(DhtAddress source, ulong distance, byte[] data)
        {
            if (data.Length % PatchEntrySize != 0)
                return;

            int offset = 0;

            for (int i = 0; i < data.Length; i += PatchEntrySize)
            {
                ulong dhtid = BitConverter.ToUInt64(data, i);
                uint version = BitConverter.ToUInt32(data, i + 8);

                offset += PatchEntrySize;

                if (!Utilities.InBounds(Core.LocalDhtID, distance, dhtid))
                    continue;

                OpProfile profile = GetProfile(dhtid);

                if(profile != null)
                {
                    if (profile.Header != null)
                    {
                        if (profile.Header.Version > version)
                        {
                            Store.Send_StoreReq(source, 0, new DataReq(null, profile.DhtID, ComponentID.Profile, profile.SignedHeader));
                            continue;
                        }

                        profile.Unique = false; // network has current or newer version

                        if (profile.Header.Version == version)
                            continue;

                        // else our version is old, download below
                    }
                }

                if (Network.Established)
                    Network.Searches.SendDirectRequest(source, dhtid, ComponentID.Profile, BitConverter.GetBytes(version));
                else
                    DownloadLater[dhtid] = version;
            }
        }

        internal void SaveLocal(string template, Dictionary<string, string> textFields, Dictionary<string, string> fileFields)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreBlocked(delegate() {SaveLocal(template, textFields, fileFields); });
                return;
            }

            // timer must be active, so saveheaders is called, so update is made permanent

            try
            {
                ProfileHeader header = null;

                OpProfile oldProfile = GetProfile(Core.LocalDhtID);
                if(oldProfile != null)
                    header = oldProfile.Header;

                string oldFile = null;

                if(header != null)
                    oldFile = GetFilePath(header);
                else
                    header = new ProfileHeader();
                    

                header.Key   = Core.User.Settings.KeyPublic;
                header.KeyID = Core.LocalDhtID; // set so keycheck works
                header.Version++;
                header.FileKey.GenerateKey();
                header.FileKey.IV = new byte[header.FileKey.IV.Length];

                string tempPath = Core.GetTempPath();
                FileStream tempFile = new FileStream(tempPath, FileMode.CreateNew);
                CryptoStream stream = new CryptoStream(tempFile, header.FileKey.CreateEncryptor(), CryptoStreamMode.Write);
                int written = 0;

                // write template info
                byte[] htmlBytes = Core.Protocol.UTF.GetBytes(template);
                written += Protocol.WriteToFile(new ProfileFile("template", htmlBytes.Length), stream);
                

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
                    written += Protocol.WriteToFile(new ProfileFile("fields", fieldsTotalSize), stream); 


                // write files info
                if(fileFields != null)
                    foreach (string path in fileFields.Values)
                    {
                        FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                        written += Protocol.WriteToFile(new ProfileFile("file=" + Path.GetFileName(path), file.Length), stream); 

                        file.Close();
                    }

                stream.WriteByte(0); // end packets
                header.EmbeddedStart = written + 1;

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


                // finish building header
                Utilities.ShaHashFile(tempPath, ref header.FileHash, ref header.FileSize);

                // move file, overwrite if need be
                string finalPath = GetFilePath(header);
                File.Move(tempPath, finalPath);

                CacheProfile(new SignedData(Core.Protocol, Core.User.Settings.KeyPair, header), header);

                SaveHeaders();

                if (oldFile != null && File.Exists(oldFile)) // delete after move to ensure a copy always exists (names different)
                    try { File.Delete(oldFile); }
                    catch { }

            
                // publish header
                OpProfile profile = GetProfile(Core.LocalDhtID);
                if (profile == null)
                    return;

                Store.PublishNetwork(Core.LocalDhtID, ComponentID.Profile, profile.SignedHeader);

                Store.PublishDirect(Links.GetLocsAbove(), Core.LocalDhtID, ComponentID.Profile, profile.SignedHeader);
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Profile", "Error updating local " + ex.Message);
            }
        }

        void SaveHeaders()
        {
            RunSaveHeaders = false;

            try
            {
                string tempPath = Core.GetTempPath();
                FileStream file = new FileStream(tempPath, FileMode.Create);
                CryptoStream stream = new CryptoStream(file, LocalFileKey.CreateEncryptor(), CryptoStreamMode.Write);

                ProfileMap.LockReading(delegate()
                {
                    foreach (OpProfile profile in ProfileMap.Values)
                        if (profile.SignedHeader != null)
                            stream.Write(profile.SignedHeader, 0, profile.SignedHeader.Length);
                });

                stream.FlushFinalBlock();
                stream.Close();


                string finalPath = ProfilePath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "headers");
                File.Delete(finalPath);
                File.Move(tempPath, finalPath);
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Profile", "Error saving header " + ex.Message);
            }
        }

        private void LoadHeaders()
        {
            try
            {
                string path = ProfilePath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, "headers");

                if (!File.Exists(path) )
                    return;

                FileStream   file   = new FileStream(path, FileMode.Open);
                CryptoStream crypto = new CryptoStream(file, LocalFileKey.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Core.Protocol, FileAccess.Read);

                G2Header root = null;

                while (stream.ReadPacket(ref root))
                    if (root.Name == DataPacket.SignedData)
                    {
                        SignedData signed = SignedData.Decode(Core.Protocol, root);
                        G2Header embedded = new G2Header(signed.Data);

                        // figure out data contained
                        if (Core.Protocol.ReadPacket(embedded))
                        {
                            if (embedded.Name == ProfilePacket.Header)
                                Process_ProfileHeader(null, signed, ProfileHeader.Decode(Core.Protocol, embedded));
                        }
                    }

                stream.Close();
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Profile", "Error loading header " + ex.Message);
            }
        }

        private void Process_ProfileHeader(DataReq data, SignedData signed, ProfileHeader header)
        {
            Core.IndexKey(header.KeyID, ref header.Key);

            OpProfile current = GetProfile(header.KeyID);

            // if link loaded
            if (current != null)
            {
                // lower version
                if (header.Version < current.Header.Version)
                {
                    if (data != null && data.Sources != null)
                        foreach (DhtAddress source in data.Sources)
                            Store.Send_StoreReq(source, data.LocalProxy, new DataReq(null, current.DhtID, ComponentID.Profile, current.SignedHeader));

                    return;
                }

                // higher version
                else if (header.Version > current.Header.Version)
                {
                    CacheProfile(signed, header);
                }
            }

            // else load file, set new header after file loaded
            else
                CacheProfile(signed, header);       
        }

        private void DownloadProfile(SignedData signed, ProfileHeader header)
        {
            if (!Utilities.CheckSignedData(header.Key, signed.Data, signed.Signature))
                return;

            FileDetails details = new FileDetails(ComponentID.Profile, header.FileHash, header.FileSize, null);

            Core.Transfers.StartDownload(header.KeyID, details, new object[] { signed, header }, new EndDownloadHandler(EndDownload));
        }

        private void EndDownload(string path, object[] args)
        {
            SignedData signedHeader = (SignedData)args[0];
            ProfileHeader header = (ProfileHeader)args[1];

            try
            {
                File.Move(path, GetFilePath(header));
            }
            catch { return; }

            CacheProfile(signedHeader, header);
        }

        private void CacheProfile(SignedData signedHeader, ProfileHeader header)
        {
            if (Core.InvokeRequired)
                Debug.Assert(false);

            try
            {
                // check if file exists           
                string path = GetFilePath(header);
                if (!File.Exists(path))
                {
                    DownloadProfile(signedHeader, header);
                    return;
                }

                // get profile
                OpProfile prevProfile = GetProfile(header.KeyID);

                OpProfile newProfile = new OpProfile(header.Key);


                // delete old file
                if (prevProfile != null)
                {
                    if (header.Version < prevProfile.Header.Version)
                        return; // dont update with older version

                    string oldPath = GetFilePath(prevProfile.Header);
                    if (path != oldPath && File.Exists(oldPath))
                        try { File.Delete(oldPath); }
                        catch { }
                }

                // set new header
                newProfile.Header = header;
                newProfile.SignedHeader = signedHeader.Encode(Core.Protocol);
                newProfile.Unique = Core.Loading;

                ProfileMap.SafeAdd(header.KeyID, newProfile);


                if (header.KeyID == Core.LocalDhtID)
                    LocalProfile = newProfile;

                if ((newProfile == LocalProfile) || (prevProfile != null && prevProfile.Loaded))
                    LoadProfile(newProfile.DhtID);


                RunSaveHeaders = true;


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

                    Store.PublishDirect(locations, newProfile.DhtID, ComponentID.Profile, newProfile.SignedHeader);
                }

                if (ProfileUpdate != null)
                    Core.RunInGuiThread(ProfileUpdate, newProfile);

                if (Core.NewsWorthy(newProfile.DhtID, 0, false))
                    Core.MakeNews("Profile updated by " + Links.GetName(newProfile.DhtID), newProfile.DhtID, 0, true, ProfileRes.IconX, Menu_View);
         
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Profile", "Error caching profile " + ex.Message);
            }
        }

        internal void LoadProfile(ulong id)
        {
            OpProfile profile = GetProfile(id);

            if (profile == null)
                return;

            try
            {
                string path = GetFilePath(profile.Header);

                if (!File.Exists(path))
                    return;

                profile.Files = new List<ProfileFile>();

                FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                CryptoStream crypto = new CryptoStream(file, profile.Header.FileKey.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Core.Protocol, FileAccess.Read);

                G2Header root = null;

                while (stream.ReadPacket(ref root))
                    if (root.Name == ProfilePacket.File)
                    {
                        ProfileFile packet = ProfileFile.Decode(Core.Protocol, root);

                        if (packet == null)
                            continue;

                        profile.Files.Add(packet);
                    }

                stream.Close();

                profile.Loaded = true;
            }
            catch (Exception ex)
            {
                Network.UpdateLog("Profile", "Error loading file " + ex.Message);
            }

        }

        internal string GetFilePath(ProfileHeader header)
        {
            return ProfilePath + Path.DirectorySeparatorChar + Utilities.CryptFilename(LocalFileKey, header.KeyID, header.FileHash);
        }

        internal void CheckVersion(ulong key, uint version)
        {
            OpProfile profile = GetProfile(key);

            if (profile == null || profile.Header == null)
                return;

            if (profile.Header.Version < version)
                StartSearch(key, version);
        }

        internal void Research(ulong key)
        {
            if (Core.InvokeRequired)
            {
                Core.RunInCoreAsync(delegate() { Research(key); });
                return;
            }

            if (!Network.Routing.Responsive())
                return;

            // limit re-search to once per 30 secs
            DateTime timeout = default(DateTime);

            if (NextResearch.TryGetValue(key, out timeout))
                if (Core.TimeNow < timeout)
                    return;

            uint version = 0;
            OpProfile profile = GetProfile(key);

            if (profile != null)
                version = profile.Header.Version + 1;

            StartSearch(key, version);

            NextResearch[key] = Core.TimeNow.AddSeconds(30);
        }
    }


    internal class OpProfile
    {
        internal ulong  DhtID;
        internal ulong  DhtBounds = ulong.MaxValue;
        internal byte[] Key;    // make sure reference is the same as main key list
        internal bool   Unique;
        internal bool   Loaded;

        internal List<ProfileFile> Files = new List<ProfileFile>();

        internal ProfileHeader Header;
        internal byte[] SignedHeader;


        internal OpProfile(byte[] key)
        {
            Key = key;
            DhtID = Utilities.KeytoID(key);
        }
    }
}
