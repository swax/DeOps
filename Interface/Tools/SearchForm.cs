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
    internal partial class SearchForm : CustomIconForm
    {
        InternetSim Sim;
        DhtNetwork  Network;

        delegate void UpdateListHandler(DhtSearch search);
        UpdateListHandler OnUpdateList;


        internal static void Show(DhtNetwork network)
        {
            SearchForm form = new SearchForm(network);
            form.Show();
        }

        internal SearchForm(DhtNetwork network)
        {
            InitializeComponent();

            if(network.Core.Sim != null)
                Sim = network.Core.Sim.Internet;

            Network = network;

            Text = "Search (" + Network.GetLabel() + ")";

            OnUpdateList = new UpdateListHandler(UpdateList);
        }

        private void ButtonSearch_Click(object sender, EventArgs e)
        {
            ulong TargetID = 0;

            // find Dht id or user or operation
            if (Sim != null)
            {
                Sim.Instances.SafeForEach(instance =>
                {
                    instance.Context.Cores.SafeForEach(core =>
                    {
                        if (RadioOp.Checked && core.User.Settings.Operation == TextSearch.Text)
                        {
                            TargetID = core.Network.OpID;
                            return;
                        }

                        if (RadioUser.Checked && core.User.Settings.UserName == TextSearch.Text)
                        {
                            TargetID = Network.IsLookup ? core.Context.Lookup.UserID : core.UserID;
                            return;
                        }
                    });
                });
            }

            if (TargetID == 0)
            {
                MessageBox.Show(this, "Not Found");
                return;
            }

            ListResults.Items.Clear();
            LabelResults.Text = "";

            DhtSearch search = Network.Searches.Start(TargetID, "MANUAL", Network.Core.DhtServiceID, 0, null, null);
            search.DoneEvent = (s) => BeginInvoke(OnUpdateList, s);
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
						Utilities.IDtoBin(lookup.Contact.UserID),
						lookup.Contact.ClientID.ToString()		
					}));

        }
    }
}