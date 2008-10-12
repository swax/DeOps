using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp.Interface;
using RiseOp.Implementation;
using RiseOp.Implementation.Protocol.Net;


namespace RiseOp.Services.Sharing
{
    internal partial class AcceptFileForm : CustomIconForm
    {
        OpCore Core;
        SharingService Sharing;

        SharedFile TheFile;
        DhtClient Source;

        internal AcceptFileForm(OpCore core, DhtClient client, SharedFile share)
        {
            InitializeComponent();

            Core = core;
            Sharing = core.GetService(ServiceIDs.Sharing) as SharingService;

            TheFile = share;
            Source = client;

            DescriptionLabel.Text = core.GetName(client.UserID) + " wants to send you a file";

            NameLabel.Text = TheFile.Name;

            SizeLabel.Text = Utilities.ByteSizetoDecString(TheFile.Size);
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            Sharing.AcceptRequest(Source, TheFile);

            // show the user the transfer starting
            SharingView view = new SharingView(Core, Core.UserID);

            if (Core.GuiMain is MainForm)
            {
                if (((MainForm)Core.GuiMain).SideMode)
                    Core.ShowExternal(view);
                else
                    Core.ShowInternal(view);
            }
            else
                Core.ShowExternal(view);

            Close();
        }

        private void DenyButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
