using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Interface.Views;


namespace RiseOp.Interface.Info
{
    internal partial class InfoView : ViewShell
    {
        OpCore Core;

        string HelpPage = @"<html>
                            <head>
	                            <style type='text/css'>
		                            body 
		                            { 	
			                            margin: 10;
			                            font-family: Arial;
			                            font-size:10pt;
		                            }
                            		
	                            </style>

	                            <script>
		                            function SetElement(id, text)
		                            {
			                            document.getElementById(id).innerHTML = text;
		                            }
	                            </script>
                            </head>
                            <body bgcolor=White>
<b>Welcome to <?=op?></b><br>
Status: <?=status?><br>
<br>
<b>Getting Started</b><br>
Everyone on RiseOp has an identity, yours can be found in the Manage menu above. Also in the 
Manage Menu you'll find the Invite option to bring more people into <?=op?>.<br>
<br>
<b>Whats the point?</b><br>
RiseOp is a secure shell around your group, including their files, and communications.  Security
is derived from trust.  So once people join  <?=op?>, determine who trusts who to build the
functional structure of your group.<br>
<br>
<b>What can I do?</b><br>
Grow <?=op?>, use trust to build it up and keep it organized.  RiseOp has a number
of services that make coordination easier.<br>
<br>
<b>What Services?</b><br>
    
Double-click on someone in <?=op?> to send them a secure Mail, or if they're online, a secure IM.<br>
<br>
Keep <?=op?>'s files secure and synchronized in the common <op> File System<br>
<br>
Post on the Message Board to have a offline discussions with those around you<br>
<br>
Start making a timeline of your plans in the Scheduler<br>
<br>
Use the 'side mode' button in the bottom-left corner to switch RiseOp into an IM interface.<br>
<br>
Update your profile info so if you're offline people know how to find you.<br>
<br>
As <?=op?> grows, use Projects to create specialized groups with their own Storage and Communication.
<br>
<br>
<b>Grow <?=op?> and let RiseOp facilitate your large scale coordination.</b>
                            </body>
                            </html>";

        string GlobalPage = @"<html>
                            <head>
	                            <style type='text/css'>
		                            body 
		                            { 	
			                            margin: 10;
			                            font-family: Arial;
			                            font-size:10pt;
		                            }
                            		
	                            </style>

	                            <script>
		                            function SetElement(id, text)
		                            {
			                            document.getElementById(id).innerHTML = text;
		                            }
	                            </script>
                            </head>
                            <body bgcolor=White>
                                <b>Welcome to <?=op?></b><br>
                                Status: <?=status?><br>
                                <br>
When you create a new Op with RiseOp, use the Global IM to send people invitations.  The Global IM
network is similar to AOL or Yahoo, but is different because it is fully decentralized and highly secure.<br>
<br>
<b>How do I start?</b><br>
Your Global IM identity can be found in the Options menu.  Give your identity to those with Ops you wish to join.
They will use your identity to create an invitation specifically for you.<br>
<br>
<b>What else can I do?</b><br>
Global IM includes a couple of the services found in a full RiseOp network.  These services are chat rooms, file
transfer, and file sharing.  File transfers are automatically swarmed when multiple people are involved.<br>
                                </body>
                            </html>";


        bool Fresh;


        internal InfoView(OpCore core, bool help, bool fresh)
        {
            Core = core;
            
            InitializeComponent();

            toolStrip1.Renderer = new ToolStripProfessionalRenderer(new OpusColorTable());

            if (help)
            {
                HelpButton.Checked = true;
                NetworkButton.Checked = false;
            }
            else
                NewsButton.Checked = true;

            if (!Core.Network.Responsive)
                NetworkButton.Checked = true;

            Fresh = fresh;
        }

        internal override string GetTitle(bool small)
        {
            return "Info";
        }

        internal override Size GetDefaultSize()
        {
            return new Size(500, 300);
        }

        internal override void Init()
        {
            networkPanel1.Init(Core);

            Core.Network.StatusChange += new RiseOp.Implementation.Dht.StatusChange(Network_StatusChange);
        }

        internal override bool Fin()
        {
            Core.Network.StatusChange -= new RiseOp.Implementation.Dht.StatusChange(Network_StatusChange);
            
            return true;
        }

        private void NetworkButton_CheckedChanged(object sender, EventArgs e)
        {
            Fresh = false;

            splitContainer1.Panel1Collapsed = !NetworkButton.Checked;

        }

        void Network_StatusChange()
        {
            if (HelpButton.Checked)
                RefreshHelp();
        }

        private void NewsButton_CheckedChanged(object sender, EventArgs e)
        {
            Fresh = false;

            if (NewsButton.Checked)
            {
                HelpButton.Checked = false;
                webBrowser1.Navigate("http://www.riseop.com/client/news.html?version=" + Application.ProductVersion);
            }
        }

        private void HelpButton_CheckedChanged(object sender, EventArgs e)
        {
            Fresh = false;

            if (HelpButton.Checked)
            {
                NewsButton.Checked = false;

                RefreshHelp();
            }
        }

        private void RefreshHelp()
        {
            string page = Core.User.Settings.GlobalIM ? GlobalPage : HelpPage;

            page = page.Replace("<?=op?>", Core.User.Settings.Operation);

            string status = "<b><font color='orange'>Connecting...</font></b>";

            if (Core.Network.Responsive)
                status = "<b><font color='green'>Connected</font></b>";

            page = page.Replace("<?=status?>", status);

            webBrowser1.DocumentText = page;
        }

        private void SecondTimer_Tick(object sender, EventArgs e)
        {
            if (Fresh && Core.Network.Responsive)
            {
                HelpButton.Checked = true;
                NetworkButton.Checked = false;

                if (External != null)
                    External.SafeClose();
            }
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (webBrowser1.DocumentText.Contains("<!-- Error title -->"))
                HelpButton.Checked = true;

        }

    }
}
