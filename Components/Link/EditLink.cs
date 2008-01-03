using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol.Net;


namespace DeOps.Components.Link
{
    internal partial class EditLink : Form
    {
        OpCore Core;
        LinkControl Links;

        uint ProjectID;


        internal EditLink(OpCore core, uint id)
        {
            InitializeComponent();

            Core = core;
            Links = core.Links;

            ProjectID = id;

            AwayCheckBox.Checked = Core.Away;
            AwayMessage.Enabled = Core.Away;
            AwayMessage.Text = Core.User.Settings.AwayMessage;
        }

        private void EditLink_Load(object sender, EventArgs e)
        {
            NameBox.Text = Links.GetName(Core.LocalDhtID);
            LocationBox.Text = Links.Core.User.Settings.Location;

            OpLink link = Links.GetLink(Core.LocalDhtID, ProjectID);

            if (link != null)
                TitleBox.Text = link.Title;
        }  
        
        private void ButtonOK_Click(object sender, EventArgs e)
        {
            string name = NameBox.Text.Trim();

            if (name == "")
                return;

            Links.LocalTrust.Name = NameBox.Text;

            OpLink link = Links.GetLink(Core.LocalDhtID, ProjectID);

            if (link != null)
                link.Title = TitleBox.Text;

            Links.SaveLocal();

            Core.Away = AwayCheckBox.Checked;

            if (LocationBox.Text != Core.User.Settings.Location || AwayMessage.Text != Core.User.Settings.AwayMessage)
            {
                Core.User.Settings.Location = LocationBox.Text;
                Core.User.Settings.AwayMessage = AwayMessage.Text;

                if (Core.User.Settings.AwayMessage.Length > 100)
                    Core.User.Settings.AwayMessage = Core.User.Settings.AwayMessage.Substring(0, 100);

                Links.Core.User.Save();
            }

            if (Core.OperationNet.Routing.Responsive())
                Core.Locations.UpdateLocation();

            Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void AwayCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            AwayMessage.Enabled = AwayCheckBox.Checked;
        }


    }
}