using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Interface;


namespace DeOps.Services.Location
{
    internal partial class StatusForm : CustomIconForm
    {
        OpCore Core;


        internal StatusForm(OpCore core)
        {
            InitializeComponent();

            Core = core;

            if (core.User.Settings.Invisible)
                InvisibleRadio.Checked = true;

            else if (core.Locations.LocalAway)
            {
                AwayRadio.Checked = true;
                AwayBox.Text = core.User.Settings.AwayMessage;
            }

            else
                AvailableRadio.Checked = true;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            Core.Locations.SetInvisble(InvisibleRadio.Checked);

            Core.Locations.SetAway(AwayRadio.Checked, AwayBox.Text);

            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
