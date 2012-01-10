using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;

using DeOps.Services;
using DeOps.Services.Trust;
using DeOps.Services.Buddy;
using DeOps.Services.IM;
using DeOps.Services.Location;
using DeOps.Services.Mail;


namespace DeOps.Interface
{
    public partial class StatusPanel : CustomDisposeControl
    {
        // bottom status panel
        StringBuilder StatusHtml = new StringBuilder(4096);

        const string ContentPage =
                @"<html>
                <head>
	                <style type='text/css'>
		                body { margin: 0; font-size: 8.25pt; font-family: Tahoma; }

		                A:link, A:visited, A:active {text-decoration: none; color: blue;}
		                A:hover {text-decoration: underline; color: blue;}

		                .header{color: white;}
		                A.header:link, A.header:visited, A.header:active {text-decoration: none; color: white;}
		                A.header:hover {text-decoration: underline; color: white;}
                		
                        .untrusted{text-decoration: blink; line-height: 18pt;}
                        A.untrusted:link, A.untrusted:visited, A.untrusted:active {text-decoration: none; color: red;}
                        A.untrusted:hover {text-decoration: underline; color: red;}

		                .content{padding: 3px; line-height: 12pt;}
                		
	                </style>

	                <script>
		                function SetElement(id, text)
		                {
			                document.getElementById(id).innerHTML = text;
		                }
	                </script>
                </head>
                <body bgcolor=WhiteSmoke>

                    <div class='header' id='header'><?=header?></div>
                    <div class='content' id='content'><?=content?></div>

                </body>
                </html>";

        public CoreUI UI;
        public OpCore Core;
        public IMUI IM;
        public MailUI Mail;
        public TrustUI Trust;
        public BuddyUI Buddy;

        enum StatusModeType { None, Network, User, Project, Group };

        StatusModeType CurrentMode = StatusModeType.None;

        ulong UserID;
        uint  ProjectID;
        string BuddyGroup;

        string IMImg, MailImg, BuddyWhoImg, TrustImg, UntrustImg, RegImg;


        public StatusPanel()
        {
            InitializeComponent();

            StatusBrowser.DocumentText = ContentPage;
        }

        public void Init(CoreUI ui)
        {
            UI = ui;
            Core = ui.Core;

            Trust = UI.GetService(ServiceIDs.Trust) as TrustUI;
            IM = UI.GetService(ServiceIDs.IM) as IMUI;
            Mail = UI.GetService(ServiceIDs.Mail) as MailUI;
            Buddy = UI.GetService(ServiceIDs.Buddy) as BuddyUI;

            Core.Locations.GuiUpdate += new LocationGuiUpdateHandler(Location_Update);
            Core.Buddies.GuiUpdate += new BuddyGuiUpdateHandler(Buddy_Update);

            if (Core.Trust != null)
                Core.Trust.GuiUpdate += new LinkGuiUpdateHandler(Trust_Update);

            IMImg       = ExtractImage("IM",        DeOps.Services.IM.IMRes.Icon.ToBitmap());
            MailImg     = ExtractImage("Mail",      DeOps.Services.Mail.MailRes.Mail);
            BuddyWhoImg = ExtractImage("BuddyWho",  DeOps.Services.Buddy.BuddyRes.buddy_who);
            TrustImg    = ExtractImage("Trust",     DeOps.Services.Trust.LinkRes.linkup);
            UntrustImg = ExtractImage("Untrust",    DeOps.Services.Trust.LinkRes.unlink);
            RegImg      = ExtractImage("Reg",       InterfaceRes.reg);

            ShowNetwork();
        }

        private string ExtractImage(string filename, Bitmap image)
        {
            if (!File.Exists(Core.User.TempPath + Path.DirectorySeparatorChar + filename + ".png"))
            {
                using (FileStream stream = new FileStream(Core.User.TempPath + Path.DirectorySeparatorChar + filename + ".png", FileMode.CreateNew, FileAccess.Write))
                    image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            }

            string path = "file:///" + Core.User.TempPath + "/" + filename + ".png";

            path = path.Replace(Path.DirectorySeparatorChar, '/');

            return path;
        }

        public override void CustomDispose()
        {
            Core.Locations.GuiUpdate -= new LocationGuiUpdateHandler(Location_Update);
            Core.Buddies.GuiUpdate -= new BuddyGuiUpdateHandler(Buddy_Update);

            if(Core.Trust != null)
                Core.Trust.GuiUpdate -= new LinkGuiUpdateHandler(Trust_Update);
        }

        void Trust_Update(ulong user)
        {
            if (CurrentMode == StatusModeType.Project)
                ShowProject(ProjectID);

            if (CurrentMode == StatusModeType.User && user == UserID)
                ShowUser(user, ProjectID);
        }

        void Buddy_Update()
        {
            // if buddy list viewed reload
            if (CurrentMode == StatusModeType.User)
                ShowUser(UserID, ProjectID);

        }

        void Location_Update(ulong user)
        {
            if (CurrentMode == StatusModeType.User && user == UserID)
                ShowUser(user, ProjectID);
        }

        private void SecondTimer_Tick(object sender, EventArgs e)
        {
            if (DesignMode)
                return;

            if (CurrentMode == StatusModeType.Network)
                ShowNetwork();
        }

        bool PrevLookup;
        bool PrevOp;
        FirewallType PrevFirewall;

        public void ShowNetwork()
        {
            if (GuiUtils.IsRunningOnMono())
                if(CurrentMode == StatusModeType.Network &&
                    PrevOp ==  Core.Network.Responsive &&
                    PrevFirewall == Core.Firewall &&
                    (Core.Context.Lookup == null || PrevLookup == Core.Context.Lookup.Network.Responsive))
                    return;

            CurrentMode = StatusModeType.Network;
            UserID = 0;

            UpdateHeader("Green", "Network Status");

            string content = "";

            content += "<div style='padding-left: 10; line-height: 14pt;'>";

            if (Core.Context.Lookup != null)
            {
                string lookup = Core.Context.Lookup.Network.Responsive ? "Connected" : "Connecting";
                content += "<b>Lookup: </b>" + lookup + "<br>";

                PrevLookup = Core.Context.Lookup.Network.Responsive;
            }

            string operation = Core.Network.Responsive ? "Connected" : "Connecting";
            content += "<b>Network: </b>" + operation + "<br>";
            PrevOp = Core.Network.Responsive;

            content += "<b>Firewall: </b>" + Core.Firewall.ToString() + "<br>";
            PrevFirewall = Core.Firewall;

            content += "<b><a href='http://settings'>Settings</a></b><br>";

            content += "</div>";

            UpdateContent(content);
        }

        void UpdateHeader(string color, string title)
        {
            string header = "";
            header += "<div style='padding: 3px; background: " + color + "; '>";
            header += "<b>" + title + "</b>";
            header += "</div>";

            StatusBrowser.SafeInvokeScript("SetElement", new String[] { "header", header });
        }

        private void UpdateContent(string content)
        {
            StatusBrowser.SafeInvokeScript("SetElement", new String[] { "content", content });
        }


        public void ShowProject(uint project)
        {
            CurrentMode = StatusModeType.Project;
            ProjectID = project;

            UpdateHeader("FireBrick",  Core.Trust.GetProjectName(project));

            string content = "<div style='padding-left: 10; line-height: 14pt;'>";

            content += AddContentLink("rename", "Rename"); 

            if (project != 0)
                content += AddContentLink("leave", "Leave");

            if (project == 0 && Core.Trust.LocalTrust.Links[0].Uplink == null)
                content += AddContentLink("settings", "Settings");

            content += "</div>";

            UpdateContent(content);
        }

        string AddContentLink(string link, string name)
        {
            return "<b><a href='http://" + link + "'>" + name + "</a></b><br>";
        }

        public void ShowGroup(string name)
        {
            CurrentMode = StatusModeType.Group;

            BuddyGroup = name;

            UpdateHeader("FireBrick", BuddyGroup == null ? "Buddies" : BuddyGroup);

            string content = "<div style='padding-left: 10;'>";

            content += AddContentLink("add_buddy", "Add Buddy");

            if (BuddyGroup != null)
            {
                content += AddContentLink("remove_group/" + name, "Remove Group");
                content += AddContentLink("rename_group/" + name, "Rename Group");
            }
            //else
            //    content += AddContentLink("add_group", "Add Group");

            content += "</div>";

            UpdateContent(content);
        }

        public void ShowUser(ulong user, uint project)
        {
            CurrentMode = StatusModeType.User;

            UserID = user;
            ProjectID = project;

            string header = "";
            string content = "";


            // get trust info
            OpLink link = null, parent = null;
            if (Core.Trust != null)
            {
                link = Core.Trust.GetLink(user, project);

                if(link != null)
                    parent = link.GetHigher(false);
            }

            // if loop root
            if (link != null && link.IsLoopRoot)
            {
                content = "<b>Order</b><br>";

                content += "<div style='padding-left: 10;'>";

                if (link.Downlinks.Count > 0)
                {
                    foreach (OpLink downlink in link.Downlinks)
                    {
                        string entry = "";

                        if (downlink.UserID == Core.UserID)
                            entry += "<b>" + Core.GetName(downlink.UserID) + "</b> <i>trusts</i>";
                        else
                            entry += Core.GetName(downlink.UserID) + " <i>trusts</i>";

                        if (downlink.GetHigher(true) == null)
                            entry = "<font style='color: red;'>" + entry + "</font>";

                        content += entry + "<br>";
                    }

                    content += Core.GetName(link.Downlinks[0].UserID) + "<br>";
                }

                content += "</div>";

                UpdateHeader("MediumSlateBlue", "Trust Loop");
                StatusBrowser.SafeInvokeScript("SetElement", new String[] { "content", content });
                return;
            }

            // add icons on right
            content += "<div style='float: right;'>";

            Func<string, string, string> getImgLine = (url, path) => "<a href='http://" + url + "'><img style='margin:2px;' src='" + path + "' border=0></a><br>";

            if (UserID != Core.UserID && Core.GetService(ServiceIDs.IM) != null && Core.Locations.ActiveClientCount(UserID) > 0)
                content += getImgLine("im", IMImg);

            if (UserID != Core.UserID && Core.GetService(ServiceIDs.Mail) != null)
                content += getImgLine("mail", MailImg);

            content += getImgLine("buddy_who", BuddyWhoImg);

            if (UserID != Core.UserID && link != null)
            {
                OpLink local = Core.Trust.GetLink(Core.UserID, ProjectID);

                if (local != null && local.Uplink == link)
                    content += getImgLine("untrust", UntrustImg); 
                else
                    content += getImgLine("trust", TrustImg); 
            }

            content += "</div>";


            // name
            string username = Core.GetName(user);
            header = "<a class='header' href='http://rename_user'>" + username + "</a>";

 
            if (link != null)
            {
                // trust unconfirmed?
                if (parent != null && !parent.Confirmed.Contains(link.UserID))
                {
                    bool requested = parent.Requests.Any(r => r.KeyID == link.UserID);

                    string msg = requested ? "Trust Requested" : "Trust Denied";

                    if (parent.UserID == Core.UserID)
                        msg = "<b><a class='untrusted' href='http://trust_accept'>" + msg + "</a></b>";

                    msg = "<span class='untrusted'>" + msg + "</span>";

                    content += msg + "<br>";
                }

                // title
                if(parent != null)
                {
                    string title = parent.Titles.ContainsKey(UserID) ? parent.Titles[UserID] : "None";
                
                    if(parent.UserID == Core.UserID)
                        title = "<a href='http://change_title/" + title + "'>" + title + "</a>";
                   
                    content += "<b>Title: </b>" + title + "<br>"; 
                }
                // projects
                string projects = "";
                foreach (uint id in link.Trust.Links.Keys)
                    if (id != 0)
                        projects += "<a href='http://project/" + id.ToString() + "'>" + Core.Trust.GetProjectName(id) + "</a>, ";
                projects = projects.TrimEnd(new char[] { ' ', ',' });

                if (projects != "")
                    content += "<b>Projects: </b>" + projects + "<br>";
            }


            if (Core.Buddies.IgnoreList.SafeContainsKey(user))
                content += "<span class='untrusted'><b><a class='untrusted' href='http://unignore'>Ignored</a></b></span><br>";


            //Locations:
            //    Home: Online
            //    Office: Away - At Home
            //    Mobile: Online, Local Time 2:30pm
            //    Server: Last Seen 10/2/2007

            string aliases = "";
            string locations = "";

            foreach (ClientInfo info in Core.Locations.GetClients(user))
            {
                string name = Core.Locations.GetLocationName(user, info.ClientID);
                bool local = Core.Network.Local.Equals(info);

                if (info.Data.Name != username)
                    aliases += AddAlias(info.Data.Name);
                 
                if (local)
                    name = "<a href='http://edit_location'>" + name + "</a>";

                locations += "<b>" + name + ": </b>";


                string status = "Online";

                if (local && Core.User.Settings.Invisible)
                    status = "Invisible";

                else if (info.Data.Away)
                    status = "Away - " + info.Data.AwayMessage;


                if (local)
                    locations += "<a href='http://edit_status'>" + status + "</a>";
                else
                    locations += status;


                if (info.Data.GmtOffset != System.TimeZone.CurrentTimeZone.GetUtcOffset(Core.TimeNow).TotalMinutes)
                    locations += ", Local Time " + Core.TimeNow.ToUniversalTime().AddMinutes(info.Data.GmtOffset).ToString("t");

                locations += "<br>";
            }

            if (locations == "")
                content += "<b>Offline</b><br>";
            else
            {
                content += "<b>Locations</b><br>";
                content += "<div style='padding-left: 10; line-height: normal'>";
                content += locations;
                content += "</div>";
            }
            
            // add aliases
            if (Core.Trust != null)
            {
                OpTrust trust = Core.Trust.GetTrust(user);

                if (trust != null && trust.Name != username)
                    aliases += AddAlias(trust.Name);
            }

            OpBuddy buddy;
            if(Core.Buddies.BuddyList.SafeTryGetValue(user, out buddy))
                if(buddy.Name != username) // should be equal unless we synced our buddy list with ourselves somewhere else
                    aliases += AddAlias(buddy.Name);

            if(aliases != "")
                content += "<b>Aliases: </b>" + aliases.Trim(',', ' ') + "<br>";


            UpdateHeader("MediumSlateBlue", header);
            StatusBrowser.SafeInvokeScript("SetElement", new String[] { "content", content });
        }

        private string AddAlias(string name)
        {
            return "<a href='http://use_name/" + name + "'>" + name + "</a>, ";
        }

        string GenerateContent(List<Tuple<string, string>> tuples, List<Tuple<string, string>> locations, bool online)
        {
            string content = "<table callpadding=3>  ";

            foreach (Tuple<string, string> tuple in tuples)
                content += "<tr><td><p><b>" + tuple.Param1 + "</b></p></td> <td><p>" + tuple.Param2 + "</p></td></tr>";

            if (locations == null)
                return content + "</table>";

            // locations
            string ifonline = online ? "Locations" : "Offline";

            content += "<tr><td colspan=2><p><b>" + ifonline + "</b><br>";
            foreach (Tuple<string, string> tuple in locations)
                content += "&nbsp&nbsp&nbsp <b>" + tuple.Param1 + ":</b> " + tuple.Param2 + "<br>";
            content += "</p></td></tr>";

            return content + "</table>";
        }

        private void StatusBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            string url = e.Url.OriginalString;

            if (GuiUtils.IsRunningOnMono() && url.StartsWith("wyciwyg"))
                return;

            if (url.StartsWith("about:blank"))
                return;

            url = url.Replace("http://", "");
            url = url.TrimEnd('/');

            string[] command = url.Split('/');


            if (CurrentMode == StatusModeType.Project)
            {
                if (url == "rename")
                {
                    GetTextDialog rename = new GetTextDialog("Rename Project", "Enter new name for project " + Core.Trust.GetProjectName(ProjectID), Core.Trust.GetProjectName(ProjectID));

                    if (rename.ShowDialog() == DialogResult.OK)
                        Core.Trust.RenameProject(ProjectID, rename.ResultBox.Text);
                }

                else if (url == "leave")
                {
                    if (MessageBox.Show("Are you sure you want to leave " + Core.Trust.GetProjectName(ProjectID) + "?", "Leave Project", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        Core.Trust.LeaveProject(ProjectID);
                }

                else if (url == "settings")
                {
                    new DeOps.Interface.Settings.Operation(Core).ShowDialog(this);
                }
            }

            else if (CurrentMode == StatusModeType.Group)
            {
                if (url == "add_buddy")
                {
                    BuddyView.AddBuddyDialog(Core, "");
                }

                else if (url == "add_group")
                {
                    // not enabled yet
                }

                else if (command[0] == "rename_group")
                {
                    string name = command[1];

                    GetTextDialog rename = new GetTextDialog("Rename Group", "Enter a new name for group " + name, name);

                    if (rename.ShowDialog() == DialogResult.OK)
                        Core.Buddies.RenameGroup(name, rename.ResultBox.Text);
                }

                else if (command[0] == "remove_group")
                {
                    string name = command[1];

                    if(MessageBox.Show("Are you sure you want to remove " + name + "?", "Remove Group", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        Core.Buddies.RemoveGroup(name);
                }
            }

            else if (CurrentMode == StatusModeType.User)
            {
                if (url == "rename_user")
                {
                    GetTextDialog rename = new GetTextDialog("Rename User", "New name for " + Core.GetName(UserID), Core.GetName(UserID));

                    if (rename.ShowDialog() == DialogResult.OK)
                        Core.RenameUser(UserID, rename.ResultBox.Text);
                }

                else if (url == "trust_accept")
                {
                    Core.Trust.AcceptTrust(UserID, ProjectID);
                }

                else if (command[0] == "change_title")
                {
                    string def = command[1];

                    GetTextDialog title = new GetTextDialog("Change Title", "Enter title for " + Core.GetName(UserID), def);

                    if (title.ShowDialog() == DialogResult.OK)
                        Core.Trust.SetTitle(UserID, ProjectID, title.ResultBox.Text);

                }

                else if (url == "edit_location")
                {
                    GetTextDialog place = new GetTextDialog("Change Location", "Where is this instance located? (home, work, mobile?)", "");

                    if (place.ShowDialog() == DialogResult.OK)
                    {
                        Core.User.Settings.Location = place.ResultBox.Text;
                        Core.Locations.UpdateLocation();

                        Core.RunInCoreAsync(() => Core.User.Save());
                    }
                }

                else if (url == "edit_status")
                {
                    // show edit status dialog available / away / invisible
                    new StatusForm(Core).ShowDialog();
                }

                else if (url == "unignore")
                {
                    if (MessageBox.Show("Stop ignoring " + Core.GetName(UserID) + "?", "Ignore", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        Core.Buddies.Ignore(UserID, false);
                }

                else if (command[0] == "project")
                {
                    uint id = uint.Parse(command[1]);

                    if (UI.GuiMain != null && UI.GuiMain.GetType() == typeof(MainForm))
                        ((MainForm)UI.GuiMain).ShowProject(id);
                }

                else if (command[0] == "use_name")
                {
                    string name = System.Web.HttpUtility.UrlDecode(command[1]);

                    if (MessageBox.Show("Change " + Core.GetName(UserID) + "'s name to " + name + "?", "Change Name", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        Core.RenameUser(UserID, name);
                }

                else if (url == "im")
                {
                    if (IM != null)
                        IM.OpenIMWindow(UserID);
                }

                else if (url == "mail")
                {
                    if (Mail != null)
                        Mail.OpenComposeWindow(UserID);
                }

                else if (url == "buddy_who")
                {
                    if(Buddy != null)
                        Buddy.ShowIdentity(UserID);
                }

                else if (url == "trust")
                {
                    Core.Trust.LinkupTo(UserID, ProjectID);
                }

                else if (url == "untrust")
                {
                    Core.Trust.UnlinkFrom(UserID, ProjectID);
                }
            }

            else if (CurrentMode == StatusModeType.Network)
            {
                if (url == "settings")
                {
                    new DeOps.Interface.Settings.Connection(Core).ShowDialog(this);
                }
            }

            e.Cancel = true;

        }
    }

    // control's dispose code, activates our custom dispose code which we can run in our class
    public class CustomDisposeControl : UserControl
    {
        virtual public void CustomDispose() { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                CustomDispose();

            base.Dispose(disposing);
        }
    }

}
