using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

using DeOps.Components.Link;
using DeOps.Implementation;
using DeOps.Implementation.Protocol;
using DeOps.Interface;


namespace DeOps.Components.Profile
{
    internal partial class ProfileView : ViewShell
    {
        OpCore Core;
        ProfileControl Profiles;
        ulong CurrentDhtID;
        internal uint ProjectID;

        internal Dictionary<string, string> TextFields = new Dictionary<string, string>();
        internal Dictionary<string, string> FileFields = new Dictionary<string, string>();



        internal ProfileView(ProfileControl profile, ulong id, uint project)
        {
            InitializeComponent();

            Profiles = profile;
            Core = profile.Core;
            CurrentDhtID = id;
            ProjectID = project;
        }

        internal override void Init()
        {
            Profiles.ProfileUpdate += new ProfileUpdateHandler(OnProfileUpdate);
            
            OpProfile profile = Profiles.GetProfile(CurrentDhtID);

            if (profile == null)
                DisplayLoading();

            else
                OnProfileUpdate(profile);
        }

        internal override bool Fin()
        {
            Profiles.ProfileUpdate -= new ProfileUpdateHandler(OnProfileUpdate);

            return true;
        }

        private void DisplayLoading()
        {
           string html = @"<html>
                            <body>
                                <table width=""100%"" height=""100%"">
                                    <tr valign=""middle"">
                                        <td align=""center"">
                                        <b>Loading...</b>
                                        </td>
                                    </tr>
                                </table>
                            </body>
                        </html>";

           // prevents clicking sound
           if (!Browser.DocumentText.Equals(html))
           {
               Browser.Hide();
               Browser.DocumentText = html;
               Browser.Show();
           }
        }

        internal override string GetTitle(bool small)
        {
            if (small)
                return "Profile";

            if (CurrentDhtID == Profiles.Core.LocalDhtID)
                return "My Profile";

            return Profiles.Core.Links.GetName(CurrentDhtID) + "'s Profile";
        }

        internal override Size GetDefaultSize()
        {
            return new Size(500, 625);
        }

        internal override Icon GetIcon()
        {
            return ProfileRes.Icon;
        }

        void OnProfileUpdate(OpProfile profile)
        {
            // if self or in uplink chain, update profile
            List<ulong> uplinks = new List<ulong>();
            uplinks.Add(CurrentDhtID);
            uplinks.AddRange(Core.Links.GetUplinkIDs(CurrentDhtID, ProjectID));

            if (!uplinks.Contains(profile.DhtID))
                return;


            // get fields from profile

            // if temp/id dir exists use it
            // clear temp/id dir
            // extract files to temp dir

            // get html
            // insert fields into html

            // display

            string tempPath = Profiles.ProfilePath + "\\0";


            // create if needed, clear of pre-existing data
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);

            // not secure
            else 
            {
                string[] files = Directory.GetFiles(tempPath);

                foreach (string path in files)
                    File.Delete(path);
            }

            string template = LoadProfile(Core, profile, tempPath, TextFields, FileFields);

            if (template == null)
            {
                template = @"<html>
                            <body>
                                <table width=""100%"" height=""100%"">
                                    <tr valign=""middle"">
                                        <td align=""center"">
                                        <b>Unable to Load</b>
                                        </td>
                                    </tr>
                                </table>
                            </body>
                        </html>";
            }

            string html = FleshTemplate(Core, profile.DhtID, ProjectID, template, TextFields, FileFields);

            // prevents clicking sound when browser navigates
            if (!Browser.DocumentText.Equals(html))
            {
                Browser.Hide();
                Browser.DocumentText = html;
                Browser.Show();
            }
        }

        private static string LoadProfile(OpCore core, OpProfile profile, string tempPath, Dictionary<string, string> textFields, Dictionary<string, string> fileFields)
        {
            string template = null;

            textFields.Clear();

            if(fileFields != null)
                fileFields.Clear();
           
            if (!profile.Loaded)
                core.Profiles.LoadProfile(profile.DhtID);
            
            try
            { 
                FileStream stream = new FileStream(core.Profiles.GetFilePath(profile.Header), FileMode.Open, FileAccess.Read, FileShare.Read);
                CryptoStream crypto = new CryptoStream(stream, profile.Header.FileKey.CreateDecryptor(), CryptoStreamMode.Read);

                int buffSize = 4096;
                byte[] buffer = new byte[4096];
                long bytesLeft = profile.Header.EmbeddedStart;
                while (bytesLeft > 0)
                {
                    int readSize = (bytesLeft > (long)buffSize) ? buffSize : (int)bytesLeft;
                    int read = crypto.Read(buffer, 0, readSize);
                    bytesLeft -= (long)read;
                }

                // load file
                foreach (ProfileFile file in profile.Files)
                {
                    if (file.Name.StartsWith("template"))
                    {
                        byte[] html = new byte[file.Size];
                        crypto.Read(html, 0, (int)file.Size);

                        UTF8Encoding utf = new UTF8Encoding();
                        template = utf.GetString(html);
                    }

                    else if (file.Name.StartsWith("fields"))
                    {
                        byte[] data = new byte[file.Size];
                        crypto.Read(data, 0, (int)file.Size);

                        int start = 0, length = data.Length;
                        G2ReadResult streamStatus = G2ReadResult.PACKET_GOOD;

                        while (streamStatus == G2ReadResult.PACKET_GOOD)
                        {
                            G2ReceivedPacket packet = new G2ReceivedPacket();
                            packet.Root = new G2Header(data);

                            streamStatus = core.Protocol.ReadNextPacket(packet.Root, ref start, ref length);

                            if (streamStatus != G2ReadResult.PACKET_GOOD)
                                break;

                            if (packet.Root.Name == ProfilePacket.Field)
                            {
                                ProfileField field = ProfileField.Decode(core.Protocol, packet.Root);

                                if (field.Value == null)
                                    continue;

                                if (field.FieldType == ProfileFieldType.Text)
                                    textFields[field.Name] = core.Protocol.UTF.GetString(field.Value);
                                else if (field.FieldType == ProfileFieldType.File && fileFields != null)
                                    fileFields[field.Name] = core.Protocol.UTF.GetString(field.Value);
                            }
                        }
                    }

                    else if (file.Name.StartsWith("file=") && fileFields != null)
                    {
                        string name = file.Name.Substring(5);

                        try
                        {
                            string fileKey = null;
                            foreach (string key in fileFields.Keys)
                                if (name == fileFields[key])
                                {
                                    fileKey = key;
                                    break;
                                }

                            fileFields[fileKey] = tempPath + "\\" + name;
                            FileStream extract = new FileStream(fileFields[fileKey], FileMode.CreateNew, FileAccess.Write);

                            long remaining = file.Size;
                            byte[] buff = new byte[2096];

                            while (remaining > 0)
                            {
                                int read = (remaining > 2096) ? 2096 : (int)remaining;
                                remaining -= read;

                                crypto.Read(buff, 0, read);
                                extract.Write(buff, 0, read);
                            }

                            extract.Close();
                        }
                        catch
                        { }
                    }
                }

                Utilities.ReadtoEnd(crypto);
                crypto.Close();
            }
            catch (Exception)
            {
            }

            return template;
        }

        internal static string FleshTemplate(OpCore core, ulong id, uint project, string template, Dictionary<string, string> textFields, Dictionary<string, string> fileFields)
        {
            string final = template;

            // get link
            OpLink link = null;
            if (core.Links.LinkMap.ContainsKey(id))
                link = core.Links.LinkMap[id];

            // replace fields
            while (final.Contains("<?"))
            {
                // get full tag name
                int start = final.IndexOf("<?");
                int end = final.IndexOf("?>");

                if (end == -1)
                    break;

                string fulltag = final.Substring(start, end + 2 - start);
                string tag = fulltag.Substring(2, fulltag.Length - 4);

                string[] parts = tag.Split(new char[] { ':' });

                bool tagfilled = false;

                if (parts.Length == 2)
                {

                    if (parts[0] == "text" && textFields.ContainsKey(parts[1]))
                    {
                        final = final.Replace(fulltag, textFields[parts[1]]);
                        tagfilled = true;
                    }

                    else if (parts[0] == "file" && fileFields != null && fileFields.ContainsKey(parts[1]) )
                    {
                        string path = fileFields[parts[1]];

                        if (File.Exists(path))
                        {
                            path = "file:///" + path;
                            path = path.Replace('\\', '/');
                            final = final.Replace(fulltag, path);
                            tagfilled = true;
                        }
                    }

                    else if (parts[0] == "link" && link != null)
                    {
                        tagfilled = true;

                        if (parts[1] == "name")
                            final = final.Replace(fulltag, link.Name);

                        else if (parts[1] == "title" && link.Title.ContainsKey(0))
                            final = final.Replace(fulltag, link.Title[0]);

                        else
                            tagfilled = false;
                    }

                    else if (parts[0] == "motd" )
                    {
                        if(parts[1] == "start") 
                        {
                            string motd = FleshMotd(core, template, link.DhtID, project);

                            int startMotd = final.IndexOf("<?motd:start?>");
                            int endMotd = final.IndexOf("<?motd:end?>");

                            if (endMotd > startMotd)
                            {
                                endMotd += "<?motd:end?>".Length;

                                final = final.Remove(startMotd, endMotd - startMotd);

                                final = final.Insert(startMotd, motd);
                            }
                        }

                        if (parts[1] == "next")
                            return final;
                    }
                }

                if (!tagfilled)
                    final = final.Replace(fulltag, "");
            }

            return final;
        }

        private static string FleshMotd(OpCore core, string template, ulong id, uint project)
        {
            // extract motd template
            string startTag = "<?motd:start?>";
            string nextTag = "<?motd:next?>";

            int start = template.IndexOf(startTag) + startTag.Length;
            int end = template.IndexOf("<?motd:end?>");

            if (end < start)
                return "";
             
            string motdTemplate = template.Substring(start, end - start);

            // get links in chain up
            List<ulong> uplinks = new List<ulong>();
            uplinks.Add(id);
            uplinks.AddRange( core.Links.GetUplinkIDs(id, project));     
            uplinks.Reverse();

            // build cascading motds
            string finalMotd = "";

            foreach (ulong uplink in uplinks)
                if (core.Profiles.ProfileMap.ContainsKey(uplink))
                {
                    Dictionary<string, string> textFields = new Dictionary<string, string>();

                    LoadProfile(core, core.Profiles.ProfileMap[uplink], null, textFields, null);

                    string motdTag = "MOTD-" + project.ToString();
                    if(!textFields.ContainsKey(motdTag))
                        textFields[motdTag] = "No announcements";

                    textFields["MOTD"] = textFields[motdTag];

                    string currentMotd = motdTemplate;
                    currentMotd = FleshTemplate(core, uplink, project, currentMotd, textFields, null);

                    if (finalMotd == "")
                        finalMotd = currentMotd;
                    
                    else if(finalMotd.IndexOf(nextTag) != -1)
                        finalMotd = finalMotd.Replace(nextTag, currentMotd);
                }

            finalMotd = finalMotd.Replace(nextTag, "");

            return finalMotd;
        }

        private void RightClickMenu_Opening(object sender, CancelEventArgs e)
        {
            if (CurrentDhtID != Profiles.Core.LocalDhtID)
            {
                e.Cancel = true;
                return;
            }           
        }

        private void EditMenu_Click(object sender, EventArgs e)
        {
            EditProfile edit = new EditProfile(Profiles, this);
            edit.ShowDialog(this);
        }

    }
}
