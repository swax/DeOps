using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Services;
using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol.Net;
using DeOps.Simulator;

namespace DeOps.Interface.Tools
{
    internal partial class SearchForm : Form
    {
        SimForm Control;
        InternetSim Sim;
        DhtNetwork  Network;

        delegate void UpdateListHandler(DhtSearch search);
        UpdateListHandler OnUpdateList;

        internal SearchForm(string name, SimForm control, DhtNetwork network)
        {
            InitializeComponent();

            Control = control;
            Sim = control.Sim;
            Network = network;

            Text = name + " Search (" + Network.Core.User.Settings.ScreenName + ")";

            OnUpdateList = new UpdateListHandler(UpdateList);
        }

        private void ButtonSearch_Click(object sender, EventArgs e)
        {
            ulong TargetID = 0;

            // find Dht id or user or operation
            if (RadioUser.Checked)
                foreach (SimInstance instance in Sim.Instances)
                    if (instance.Core.User.Settings.ScreenName == TextSearch.Text)
                    {
                        TargetID = instance.Core.LocalDhtID;
                        break;
                    }
            
            if(RadioOp.Checked)
                foreach (SimInstance instance in Sim.Instances)
                    if (instance.Core.User.Settings.Operation == TextSearch.Text)
                    {
                        TargetID = instance.Core.OpID;
                        break;
                    }

            if (TargetID == 0)
            {
                MessageBox.Show(this, "Not Found");
                return;
            }

            ListResults.Items.Clear();
            LabelResults.Text = "";

            Network.Searches.Start(TargetID, "MANUAL", ComponentID.Node, null, new DeOps.Implementation.Dht.EndSearchHandler(EndManualSearch));
        }

        void EndManualSearch(DhtSearch search)
        {
            BeginInvoke(OnUpdateList, search);          
        }

        void UpdateList(DhtSearch search)
        {
            if (search.FoundValues.Count > 0)
            {
                LabelResults.Text = search.FoundValues.Count.ToString() + " Values Found, ";
            }

            if (search.FoundContact != null)
            {
                LabelResults.Text = "Found Contact(" + search.FoundContact.ClientID.ToString() + ") ";

                if (search.FoundProxy)
                    LabelResults.Text += "is a proxy";
            }



            // Dht
            // client 

            foreach (DhtLookup lookup in search.LookupList)
                ListResults.Items.Add(new ListViewItem(new string[]
					{
						Utilities.IDtoBin(lookup.Contact.DhtID),
						lookup.Contact.ClientID.ToString()		
					}));

        }
    }
}