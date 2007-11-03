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
        }

        private void EditLink_Load(object sender, EventArgs e)
        {
            NameBox.Text = Links.GetName(Core.LocalDhtID);
            LocationBox.Text = Links.Core.User.Settings.Location;

            if (Links.LocalLink.Title.ContainsKey(ProjectID))
                TitleBox.Text = Links.LocalLink.Title[ProjectID];
        }  
        
        private void ButtonOK_Click(object sender, EventArgs e)
        {
            string name = NameBox.Text.Trim();

            if (name == "")
                return;

            Links.LocalLink.Name = NameBox.Text;
            Links.LocalLink.Title[ProjectID] = TitleBox.Text;
            Links.SaveLocal();

            if (LocationBox.Text != Core.User.Settings.Location)
            {
                Core.User.Settings.Location = LocationBox.Text;
                Links.Core.User.Save();

                if (Core.OperationNet.Routing.Responsive())
                    Core.Locations.UpdateLocation();
            }

            Close();
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }


    }
}