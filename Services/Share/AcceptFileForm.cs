using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface;
using DeOps.Implementation;
using DeOps.Implementation.Protocol.Net;


namespace DeOps.Services.Share
{
    internal partial class AcceptFileForm : CustomIconForm
    {
        OpCore Core;
        ShareService Sharing;

        SharedFile TheFile;
        DhtClient Source;

        internal AcceptFileForm(OpCore core, DhtClient client, SharedFile share)
        {
            InitializeComponent();

            Core = core;
            Sharing = core.GetService(ServiceIDs.Share) as ShareService;

            TheFile = share;
            Source = client;

            DescriptionLabel.Text = core.GetName(client.UserID) + " wants to send you a file";

            NameLabel.Text = TheFile.Name;

            SizeLabel.Text = Utilities.ByteSizetoDecString(TheFile.Size);
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
           /* Sharing.AcceptRequest(Source, TheFile);

            // try to find an external existing view and use it, the make a new one

            // show the user the transfer starting
            if (Core.GuiMain is MainForm && !((MainForm)Core.GuiMain).SideMode)
                Core.ShowInternal(new SharingView(Core, Core.UserID));

            else if (!Core.GuiMain.ShowExistingView(typeof(SharingView)))
                Core.ShowExternal(new SharingView(Core, Core.UserID));

            Close();*/
        }

        private void DenyButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
