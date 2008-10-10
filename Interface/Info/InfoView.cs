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
                                <?=status?><br>
                                <br>
                                <b>Getting Started</b><br>
                                Everyone on RiseOp has an identity, yours can be found in the Manage menu above.
                                In order to invite people to <?=op?> you must <?=invitedirections?>.<br>
                                <br>
                                <b>Whats the point?</b><br>
                                RiseOp is a secure shell around your group, their files, and communications.  Security
                                is derived from trust.  So once people join  <?=op?>, determine who trusts who to build the
                                functional structure of your group.<br>
                                <br>
                                <b>What can I do?</b><br>
                                Grow <op> use trust to build your <op> big and keep it organized.  RiseOp has a number
                                of services that make coordination easier.<br>
                                <br>
                                <b>What Services?</b><br>
                                Double-click on some one in <?=op?> to send them a secure Mail, or if they're online, a secure IM.<br>
                                <br>
                                Update your profile info so if you're offline people know how to find you.<br>
                                <br>
                                Keep <?=op?>'s files safe and secure in the <op> file system<br>
                                <br>
                                Start making a timeline of your plans in the scheduler<br>
                                <br>
                                Post on the message board to have an offline discussion with those around you<br>
                                <br>
                                If <?=op?> gets big, create a new project with its own file system, message board, etc...<br>
                                <br>
                                <br>
                                <b>Grow <?=op?> and let RiseOp facilitate your large scale coordination!</b>
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
        }

        private void NetworkButton_CheckedChanged(object sender, EventArgs e)
        {
            Fresh = false;

            splitContainer1.Panel1Collapsed = !NetworkButton.Checked;

        }

        private void NewsButton_CheckedChanged(object sender, EventArgs e)
        {
            Fresh = false;

            if (NewsButton.Checked)
            {
                HelpButton.Checked = false;
                webBrowser1.Navigate("http://www.riseop.com/client/news.html");
            }
        }

        private void HelpButton_CheckedChanged(object sender, EventArgs e)
        {
            Fresh = false;

            if (HelpButton.Checked)
            {
                NewsButton.Checked = false;

                string page = HelpPage;
                page = page.Replace("<?=op?>", Core.User.Settings.Operation);
                page = page.Replace("<?=status?>", "");

                webBrowser1.DocumentText = page;
            }
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
    }
}
