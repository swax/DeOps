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

        string HelpPage = @"Help!!";

        bool Fresh;


        internal InfoView(OpCore core, bool help)
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

            Fresh = true;
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
                webBrowser1.DocumentText = HelpPage;
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
