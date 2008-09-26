using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;

using RiseOp.Services.Trust;
using RiseOp.Services.Buddy;
using RiseOp.Services.Location;

namespace RiseOp.Interface
{
    internal partial class StatusPanel : CustomDisposeControl
    {
        // bottom status panel
        StringBuilder StatusHtml = new StringBuilder(4096);

        const string NetworkPage =
             @"<html>
                <head>
	                <style type=""text/css"">
	                <!--
	                    body { margin: 0; }
	                    p    { font-size: 8.25pt; font-family: Tahoma }
	                -->
	                </style>

                    <script>
                        function SetElement(id, text)
                        {
                            document.getElementById(id).innerHTML = text;
                        }
                    </script>
                </head>
                <body bgcolor=WhiteSmoke>
	                <table width=100% cellpadding=4>
	                    <tr><td bgcolor=green><p><b><font color=#ffffff>Network Status</font></b></p></td></tr>
	                </table>
                    <table callpadding=3>    
                        <tr><td><p><b>Global:</b></p></td><td><p><span id='global'><?=global?></span></p></td></tr>
	                    <tr><td><p><b>Network:</b></p></td><td><p><span id='operation'><?=operation?></span></p></td></tr>
	                    <tr><td><p><b>Firewall:</b></p></td><td><p><span id='firewall'><?=firewall?></span></p></td></tr>
                    </table>
                </body>
                </html>";

        const string NodePage =
                @"<html>
                <head>
	                <style type=""text/css"">
	                <!--
	                    body { margin: 0 }
	                    p    { font-size: 8.25pt; font-family: Tahoma }
                        A:link {text-decoration: none; color: black}
                        A:visited {text-decoration: none; color: black}
                        A:active {text-decoration: none; color: black}
                        A:hover {text-decoration: underline; color: black}
	                -->
	                </style>

                    <script>
                        function SetElement(id, text)
                        {
                            document.getElementById(id).innerHTML = text;
                        }
                    </script>
                </head>
                <body bgcolor=WhiteSmoke>
	                <table width=100% cellpadding=4>
	                    <tr><td bgcolor=MediumSlateBlue><p><font color=#ffffff><span id='name'><?=name?></span></font></p></td></tr>
	                </table>

                    <span id='content'><?=content?></span>

                    
                </body>
                </html>";

        // add gmt
        // add away status
        // add online status
        // add edit link

        OpCore Core;

        enum StatusModeType { None, Network, User };
        
        StatusModeType CurrentMode = StatusModeType.None;

        ulong UserID;
        uint  ProjectID;

        internal StatusPanel()
        {
            InitializeComponent();

        }

        internal void Init(OpCore core)
        {
            Core = core;

            Core.Locations.GuiUpdate += new LocationGuiUpdateHandler(Location_Update);
            Core.Buddies.GuiUpdate += new BuddyGuiUpdateHandler(Buddy_Update);

            if (Core.Trust != null)
                Core.Trust.GuiUpdate += new LinkGuiUpdateHandler(Trust_Update);
        }

        internal override void CustomDispose()
        {
            Core.Locations.GuiUpdate -= new LocationGuiUpdateHandler(Location_Update);
            Core.Buddies.GuiUpdate -= new BuddyGuiUpdateHandler(Buddy_Update);

            if(Core.Trust != null)
                Core.Trust.GuiUpdate -= new LinkGuiUpdateHandler(Trust_Update);
        }

        void Trust_Update(ulong user)
        {
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
            if (user == UserID)
                ShowUser(user, ProjectID);
        }

        /*private void RightClickMenu_Opening(object sender, CancelEventArgs e)
        {
            LinkNode item = GetSelected();

            if (item == null)
            {
                e.Cancel = true;
                return;
            }

            if (item.Link.UserID != Core.UserID)
            {
                e.Cancel = true;
                return;
            }
        }

        private void EditMenu_Click(object sender, EventArgs e)
        {
            EditLink edit = new EditLink(Core, CommandTree.Project);
            edit.ShowDialog(this);

            UpdateCommandPanel();
        }*/

        internal void ShowNetwork()
        {
            StatusModeType mode = StatusModeType.Network;
            UserID = 0;

            string global = "";
            string operation = "";

            if (Core.Context.Global == null)
                global = "Disconnected";
            else if (Core.Context.Global.Network.Responsive)
                global = "Connected";
            else
                global = "Connecting";

            if (Core.Network.Responsive)
                operation = "Connected";
            else
                operation = "Connecting";


            List<string[]> tuples = new List<string[]>();
            tuples.Add(new string[] { "global", global });
            tuples.Add(new string[] { "operation", operation });
            tuples.Add(new string[] { "firewall", Core.GetFirewallString() });


            if (CurrentMode != mode)
            {
                CurrentMode = mode;

                StatusHtml.Length = 0;
                StatusHtml.Append(NetworkPage);

                foreach (string[] tuple in tuples)
                    StatusHtml.Replace("<?=" + tuple[0] + "?>", tuple[1]);

                SetStatus(StatusHtml.ToString());
            }
            else
            {
                foreach (string[] tuple in tuples)
                    StatusBrowser.Document.InvokeScript("SetElement", new String[] { tuple[0], tuple[1] });
            }
        }

        internal void ShowUser(ulong user, uint project)
        {
            OpLink link = null;
            if(Core.Trust != null)
                link = Core.Trust.GetLink(user, project);

            StatusModeType mode = StatusModeType.User;

            UserID = user;
            ProjectID = project;

            List<Tuple<string, string>> tuples = new List<Tuple<string, string>>();

            string name = "";
            string content = "";

            // if loop root
            if (link != null && link.IsLoopRoot)
            {
                name = "<b>Trust Loop</b>";

                content = @"<table callpadding=3>
                            <tr><td>
                            <p><b>Order:</b><br>";

                string order = "";


                string confirmed = "";

                if (link.Downlinks.Count > 0)
                {
                    foreach (OpLink downlink in link.Downlinks)
                    {
                        confirmed = downlink.GetHigher(true) == null ? "(unconfirmed)" : "";

                        if (downlink.UserID == Core.UserID)
                            order += " &nbsp&nbsp&nbsp <b>" + Core.GetName(downlink.UserID) + "</b> <i>trusts ";
                        else
                            order += " &nbsp&nbsp&nbsp " + Core.GetName(downlink.UserID) + " <i>trusts ";

                        order += confirmed + "</i><br>";
                    }

                    order += " &nbsp&nbsp&nbsp " + Core.GetName(link.Downlinks[0].UserID) + "<br>";
                }

                content += order +
                            @"</p>
                            </tr></td>
                            </table>";
            }
            else
            {
                // name
                name = "<b>" + Core.GetName(user) + "</b>";

                if (user == Core.UserID)
                    name += "  &nbsp&nbsp  (<a href='edit:local'><font color=white>edit</font></a>)";

                if (link != null)
                {
                    // title
                    string title = link.Title;

                    if (title != "")
                        tuples.Add(new Tuple<string, string>("Title: ", title));

                    // projects
                    string projects = "";
                    foreach (uint id in link.Trust.Links.Keys)
                        if (id != 0)
                            projects += "<a href='project:" + id.ToString() + "'>" + Core.Trust.GetProjectName(id) + "</a>, ";
                    projects = projects.TrimEnd(new char[] { ' ', ',' });

                    if (projects != "")
                        tuples.Add(new Tuple<string, string>("Projects: ", projects));
                }

                //Locations:
                List<Tuple<string, string>> locations = new List<Tuple<string, string>>();

                //    Home: Online
                //    Office: Away - At Home
                //    Mobile: Online, Local Time 2:30pm
                //    Server: Last Seen 10/2/2007

                bool online = false;

                List<ClientInfo> clients = Core.Locations.GetClients(user);

                foreach (ClientInfo info in clients)
                {
                    string status = "";

                    if (info.Data.Away)
                        status += "Away " + info.Data.AwayMessage;
                    else
                        status += "Online";

                    if (info.Data.GmtOffset != System.TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Minutes)
                        status += ", Local Time " + Core.TimeNow.ToUniversalTime().AddMinutes(info.Data.GmtOffset).ToString("t");

                    // last seen stuff here

                    online = true;
                    locations.Add(new Tuple<string, string>(Core.Locations.GetLocationName(user, info.ClientID), status));
                }


                //crit - get last update from localSync
                /*if (locations.Count == 0)
                {
                    ClientInfo latest = clients[0];

                    foreach (ClientInfo info in clients)
                        if (info.Data.Date > latest.Data.Date)
                            latest = info;

                    locations.Add(new Tuple<string, string>("Last Seen", latest.Data.Date.ToLocalTime().ToString()));
                }*/

                content = GenerateContent(tuples, locations, online);
            }

            // display
            if (CurrentMode != mode)
            {
                CurrentMode = mode;

                StatusHtml.Length = 0;
                StatusHtml.Append(NodePage);

                StatusHtml.Replace("<?=name?>", name);
                StatusHtml.Replace("<?=content?>", content);

                SetStatus(StatusHtml.ToString());
            }
            else
            {
                StatusBrowser.Document.InvokeScript("SetElement", new String[] { "name", name });
                StatusBrowser.Document.InvokeScript("SetElement", new String[] { "content", content });
            }
        }

        string GenerateContent(List<Tuple<string, string>> tuples, List<Tuple<string, string>> locations, bool online)
        {
            string content = "<table callpadding=3>  ";

            foreach (Tuple<string, string> tuple in tuples)
                content += "<tr><td><p><b>" + tuple.First + "</b></p></td> <td><p>" + tuple.Second + "</p></td></tr>";

            if (locations == null)
                return content + "</table>";

            // locations
            string ifonline = online ? "Locations" : "Offline";

            content += "<tr><td colspan=2><p><b>" + ifonline + "</b><br>";
            foreach (Tuple<string, string> tuple in locations)
                content += "&nbsp&nbsp&nbsp <b>" + tuple.First + ":</b> " + tuple.Second + "<br>";
            content += "</p></td></tr>";

            return content + "</table>";
        }

        private void SetStatus(string html)
        {
            Debug.Assert(!html.Contains("<?"));

            // prevents clicking sound when browser navigates
            StatusBrowser.Hide();
            StatusBrowser.DocumentText = html;
            StatusBrowser.Show();
        }

        private void StatusBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            string url = e.Url.OriginalString;

            string[] parts = url.Split(new char[] { ':' });

            if (parts.Length < 2)
                return;

            if (parts[0] == "edit")
            {
                //EditMenu_Click(null, null);
                e.Cancel = true;
                return;
            }

            if (parts[0] == "project")
            {
                if (Core.GuiMain != null && Core.GuiMain.GetType() == typeof(MainForm))
                    ((MainForm)Core.GuiMain).ShowProject(uint.Parse(parts[1]));
                
                e.Cancel = true;
            }
        }
    }

    // control's dispose code, activates our custom dispose code which we can run in our class
    internal class CustomDisposeControl : UserControl
    {
        virtual internal void CustomDispose() { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                CustomDispose();

            base.Dispose(disposing);
        }
    }

}
