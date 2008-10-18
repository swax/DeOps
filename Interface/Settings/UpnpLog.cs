using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Implementation.Transport;


namespace RiseOp.Interface.Settings
{
    internal partial class UpnpLog : CustomIconForm
    {
        UpnpSetup Setup;
        UPnPHandler UPnP;


        internal UpnpLog(UpnpSetup setup)
        {
            InitializeComponent();

            Setup = setup;
            UPnP = Setup.UPnP;

            RefreshView();
        }

        private void RefreshLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            RefreshView();
        }

        private void RefreshView()
        {
            LogBox.Clear();

            UPnP.Log.LockReading(() =>
            {
                foreach (Tuple<UpnpLogType, string> item in UPnP.Log)
                {
                    LogBox.SelectionStart = LogBox.Text.Length;
                    LogBox.SelectionLength = 0;

                    switch (item.First)
                    {
                        case UpnpLogType.In:
                            LogBox.SelectionColor = Color.Blue;
                            break;
                        case UpnpLogType.Out:
                            LogBox.SelectionColor = Color.Red;
                            break;
                        case UpnpLogType.Other:
                            LogBox.SelectionColor = Color.Black;
                            break;
                        case UpnpLogType.Error:
                            LogBox.SelectionColor = Color.Orange;
                            break;
                    }

                    LogBox.AppendText(item.Second + "\n\n");
                }
            });
        }

        private void UpnpLog_FormClosing(object sender, FormClosingEventArgs e)
        {
            Setup.Log = null;
        }
    }
}
