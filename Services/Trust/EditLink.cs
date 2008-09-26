using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol.Net;


namespace RiseOp.Services.Trust
{
    internal partial class EditLink : RiseOp.Interface.CustomIconForm
    {
        OpCore Core;
        TrustService Trust;

        uint ProjectID;


        internal EditLink(OpCore core, uint id)
            : base(core)
        {
            InitializeComponent();

            Core = core;
            Trust = core.Trust;

            ProjectID = id;

            AwayCheckBox.Checked = Core.Locations.LocalAway;
            AwayMessage.Enabled = Core.Locations.LocalAway;
            AwayMessage.Text = Core.User.Settings.AwayMessage;
        }

        private void EditLink_Load(object sender, EventArgs e)
        {
            LocationBox.Text = Trust.Core.User.Settings.Location;

            OpLink link = Trust.GetLink(Core.UserID, ProjectID);

            if (link != null)
                TitleBox.Text = link.Title;
        }  
        
        private void ButtonOK_Click(object sender, EventArgs e)
        {
            OpLink link = Trust.GetLink(Core.UserID, ProjectID);

            if (link != null)
                link.Title = TitleBox.Text;

            Trust.SaveLocal();

            Core.Locations.LocalAway = AwayCheckBox.Checked;

            if (LocationBox.Text != Core.User.Settings.Location || AwayMessage.Text != Core.User.Settings.AwayMessage)
            {
                Core.User.Settings.Location = LocationBox.Text;
                Core.User.Settings.AwayMessage = AwayMessage.Text;

                if (Core.User.Settings.AwayMessage.Length > 100)
                    Core.User.Settings.AwayMessage = Core.User.Settings.AwayMessage.Substring(0, 100);

                Core.RunInCoreAsync(delegate()
                {
                    Trust.Core.User.Save();
                });
            }

            if (Core.Network.Responsive)
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