/********************************************************************************

	De-Ops: Decentralized Operations
	Copyright (C) 2006 John Marshall Group, Inc.

	By contributing code you grant John Marshall Group an unlimited, non-exclusive
	license to your contribution.

	For support, questions, commercial use, etc...
	E-Mail: swabby@c0re.net

********************************************************************************/

using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Net;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Transport;
using DeOps.Implementation.Protocol.Net;

using DeOps.Components;
using DeOps.Components.Board;
using DeOps.Components.Chat;
using DeOps.Components.IM;
using DeOps.Components.Link;
using DeOps.Components.Location;
using DeOps.Components.Mail;
using DeOps.Components.Profile;
using DeOps.Components.Transfer;


namespace DeOps.Interface.Tools
{
	internal delegate void ShowDelegate(object pass);

	/// <summary>
	/// Summary description for InternalsForm.
	/// </summary>
	internal class InternalsForm : System.Windows.Forms.Form
	{
		OpCore Core;

		private System.Windows.Forms.Splitter splitter1;
		private System.Windows.Forms.TreeView treeStructure;
		private System.Windows.Forms.ListView listValues;
		private System.Windows.Forms.Button buttonRefresh;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        internal InternalsForm(OpCore core)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			Core    = core;

			Text = "Internal (" + Core.User.Settings.ScreenName + ")";
			
			treeStructure.Nodes.Add( new StructureNode("", new ShowDelegate(ShowNone), null));
			
            // core
                // identity
                // networks (global/operation)
                    // cache
                    // logs
                    // routing
                    // search
                    // store
                    // tcp
                // Components
                    // Link
                    // Location
                    // ...
                // rudp
                    // sessions[]

            // core
            StructureNode coreNode = new StructureNode("Core", new ShowDelegate(ShowCore), null);
            
            // identity
            coreNode.Nodes.Add( new StructureNode(".Identity", new ShowDelegate(ShowIdentity), null));

            // networks
            LoadNetwork(coreNode.Nodes, "Global", Core.GlobalNet);
            LoadNetwork(coreNode.Nodes, "Operation", Core.OperationNet);

            // components
            StructureNode componentsNode = new StructureNode("Components", new ShowDelegate(ShowNone), null);
            LoadComponents(componentsNode);
            coreNode.Nodes.Add(componentsNode);

            // identity
            coreNode.Nodes.Add(new StructureNode("Rudp", new ShowDelegate(ShowRudp), null));


            treeStructure.Nodes.Add(coreNode);
            coreNode.Expand();			
		}

        private void LoadComponents(StructureNode componentsNode)
        {
            foreach (ushort id in Core.Components.Keys)
            {
                switch (id)
                {
                    case ComponentID.Link:
                        StructureNode linkNode = new StructureNode("Links", new ShowDelegate(ShowLinks), null);
                        linkNode.Nodes.Add(new StructureNode("Index", new ShowDelegate(ShowLinkMap), null));
                        linkNode.Nodes.Add(new StructureNode("Roots", new ShowDelegate(ShowLinkRoots), null));
                        linkNode.Nodes.Add(new StructureNode("Projects", new ShowDelegate(ShowLinkProjects), null));
                        componentsNode.Nodes.Add(linkNode);
                        break;

                    case ComponentID.Location:  
                        StructureNode locNode = new StructureNode("Locations", new ShowDelegate(ShowLocations), null);
                        locNode.Nodes.Add(new StructureNode("Global", new ShowDelegate(ShowLocGlobal), null)); 
                        locNode.Nodes.Add(new StructureNode("Operation", new ShowDelegate(ShowLocOperation), null));
                        componentsNode.Nodes.Add(locNode);
                        break;

                    case ComponentID.Transfer:
                        StructureNode transNode = new StructureNode("Transfers", new ShowDelegate(ShowTransfers), null);
                        transNode.Nodes.Add(new StructureNode("Uploads", new ShowDelegate(ShowUploads), null));
                        transNode.Nodes.Add(new StructureNode("Downloads", new ShowDelegate(ShowDownloads), null));
                        componentsNode.Nodes.Add(transNode);
                        break;

                    case ComponentID.Profile:                       
                        StructureNode profileNode = new StructureNode("Profiles", new ShowDelegate(ShowProfiles), null);
                        componentsNode.Nodes.Add(profileNode);
                        break;

                    case ComponentID.Mail:
                        StructureNode mailNode = new StructureNode("Mail", new ShowDelegate(ShowMail), null);
                        mailNode.Nodes.Add(new StructureNode("Mail", new ShowDelegate(ShowMailMap), null));
                        mailNode.Nodes.Add(new StructureNode("Acks", new ShowDelegate(ShowAckMap), null));
                        mailNode.Nodes.Add(new StructureNode("Pending", new ShowDelegate(ShowPendingMap), null));
                        mailNode.Nodes.Add(new StructureNode("My Pending Mail", new ShowDelegate(ShowPendingMail), null));
                        mailNode.Nodes.Add(new StructureNode("My Pending Acks", new ShowDelegate(ShowPendingAcks), null));
                        componentsNode.Nodes.Add(mailNode);
                        break;

                    case ComponentID.Board:
                        StructureNode boardNode = new StructureNode("Board", new ShowDelegate(ShowBoard), null);
                        componentsNode.Nodes.Add(boardNode);
                        break;
                }
            }
        }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InternalsForm));
            this.treeStructure = new System.Windows.Forms.TreeView();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.listValues = new System.Windows.Forms.ListView();
            this.buttonRefresh = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // treeStructure
            // 
            this.treeStructure.BackColor = System.Drawing.SystemColors.ControlLight;
            this.treeStructure.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.treeStructure.Dock = System.Windows.Forms.DockStyle.Left;
            this.treeStructure.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.treeStructure.FullRowSelect = true;
            this.treeStructure.HideSelection = false;
            this.treeStructure.Location = new System.Drawing.Point(0, 0);
            this.treeStructure.Name = "treeStructure";
            this.treeStructure.ShowLines = false;
            this.treeStructure.Size = new System.Drawing.Size(160, 414);
            this.treeStructure.Sorted = true;
            this.treeStructure.TabIndex = 0;
            this.treeStructure.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeStructure_AfterSelect);
            // 
            // splitter1
            // 
            this.splitter1.Location = new System.Drawing.Point(160, 0);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(3, 414);
            this.splitter1.TabIndex = 1;
            this.splitter1.TabStop = false;
            // 
            // listValues
            // 
            this.listValues.AllowColumnReorder = true;
            this.listValues.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listValues.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listValues.FullRowSelect = true;
            this.listValues.Location = new System.Drawing.Point(163, 0);
            this.listValues.Name = "listValues";
            this.listValues.Size = new System.Drawing.Size(301, 414);
            this.listValues.TabIndex = 2;
            this.listValues.UseCompatibleStateImageBehavior = false;
            this.listValues.View = System.Windows.Forms.View.Details;
            this.listValues.MouseClick += new System.Windows.Forms.MouseEventHandler(this.listValues_MouseClick);
            // 
            // buttonRefresh
            // 
            this.buttonRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRefresh.Location = new System.Drawing.Point(368, 368);
            this.buttonRefresh.Name = "buttonRefresh";
            this.buttonRefresh.Size = new System.Drawing.Size(75, 23);
            this.buttonRefresh.TabIndex = 3;
            this.buttonRefresh.Text = "Refresh";
            this.buttonRefresh.Click += new System.EventHandler(this.buttonRefresh_Click);
            // 
            // InternalsForm
            // 
            this.AcceptButton = this.buttonRefresh;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(464, 414);
            this.Controls.Add(this.buttonRefresh);
            this.Controls.Add(this.listValues);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.treeStructure);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "InternalsForm";
            this.Text = "Internals";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.InternalsForm_Closing);
            this.ResumeLayout(false);

		}
		#endregion

		private void treeStructure_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			RefreshView();
		}

		internal void ShowNone(object pass)
		{
			listValues.Columns.Clear();
			listValues.Items.Clear();
		}

        internal void ShowCore(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Property", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Value", 300, HorizontalAlignment.Left);

            listValues.Items.Add(new ListViewItem(new string[] { "LocalIP",         xStr(Core.LocalIP) }));
            listValues.Items.Add(new ListViewItem(new string[] { "LocalDhtID",      IDtoStr(Core.LocalDhtID) }));
            listValues.Items.Add(new ListViewItem(new string[] { "OpID",            IDtoStr(Core.OpID) })); 
            listValues.Items.Add(new ListViewItem(new string[] { "Firewall",        xStr(Core.Firewall) }));
            listValues.Items.Add(new ListViewItem(new string[] { "ClientID",        xStr(Core.ClientID) }));
            listValues.Items.Add(new ListViewItem(new string[] { "StartTime",       xStr(Core.StartTime) }));
            listValues.Items.Add(new ListViewItem(new string[] { "NextSaveCache",   xStr(Core.NextSaveCache) }));
        }


        internal void ShowIdentity(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Property", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Value", 300, HorizontalAlignment.Left);

            listValues.Items.Add(new ListViewItem(new string[] { "ProfilePath", xStr(Core.User.ProfilePath) }));
            listValues.Items.Add(new ListViewItem(new string[] { "Operation", xStr(Core.User.Settings.Operation) }));
            listValues.Items.Add(new ListViewItem(new string[] { "ScreenName", xStr(Core.User.Settings.ScreenName) }));
            listValues.Items.Add(new ListViewItem(new string[] { "GlobalPortTcp", xStr(Core.User.Settings.GlobalPortTcp) }));
            listValues.Items.Add(new ListViewItem(new string[] { "GlobalPortUdp", xStr(Core.User.Settings.GlobalPortUdp) }));
            listValues.Items.Add(new ListViewItem(new string[] { "OpPortTcp", xStr(Core.User.Settings.OpPortTcp) }));
            listValues.Items.Add(new ListViewItem(new string[] { "OpPortUdp", xStr(Core.User.Settings.OpPortUdp) }));
            listValues.Items.Add(new ListViewItem(new string[] { "OpAccess", xStr(Core.User.Settings.OpAccess) }));
        }

        internal void LoadNetwork(TreeNodeCollection root, string name, DhtNetwork network)
        {
            StructureNode netItem = new StructureNode(name, new ShowDelegate(ShowNetwork), network);

            // cache
            netItem.Nodes.Add(new StructureNode("Cache", new ShowDelegate(ShowCache), network));

            // logs
            StructureNode logsNode = new StructureNode("Logs", new ShowDelegate(UpdateLogs), network);
            AddLogs(logsNode, network);
            netItem.Nodes.Add(logsNode);

            // routing
            netItem.Nodes.Add(new StructureNode("Routing", new ShowDelegate(ShowRouting), network));
            
            // search
            StructureNode searchNode = new StructureNode("Searches", new ShowDelegate(ShowNone), null);

            StructureNode pendingNode = new StructureNode("Pending", new ShowDelegate(UpdateSearches), network.Searches.Pending);
            StructureNode activeNode = new StructureNode("Active", new ShowDelegate(UpdateSearches), network.Searches.Active);
            
            AddSearchNodes(pendingNode, network.Searches.Pending);
            AddSearchNodes(activeNode, network.Searches.Active);

            searchNode.Nodes.Add(pendingNode);
            searchNode.Nodes.Add(activeNode);

            netItem.Nodes.Add(searchNode);
            
            // store
            StructureNode storeNode = new StructureNode("Store", new ShowDelegate(ShowStore), network);

            netItem.Nodes.Add(storeNode);
            
            // tcp
            StructureNode connectionsNode = new StructureNode("Tcp", new ShowDelegate(ShowTcp), network);
            AddConnectionNodes(connectionsNode, network);
            netItem.Nodes.Add(connectionsNode); 
            
            root.Add(netItem);
        }

        internal void ShowNetwork(object pass)
        {
            DhtNetwork network = pass as DhtNetwork;

            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Property", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Value", 300, HorizontalAlignment.Left);

            listValues.Items.Add(new ListViewItem(new string[] { "IPCache", xStr(network.IPCache.Count) }));
            listValues.Items.Add(new ListViewItem(new string[] { "IPTable", xStr(network.IPTable.Count) }));
            listValues.Items.Add(new ListViewItem(new string[] { "Searches Pending", xStr(network.Searches.Pending.Count) }));
            listValues.Items.Add(new ListViewItem(new string[] { "Searches Active", xStr(network.Searches.Active.Count) }));
            listValues.Items.Add(new ListViewItem(new string[] { "Routing LastUpdated", xStr(network.Routing.LastUpdated) }));
        }


		internal void ShowCache(object pass)
		{
            DhtNetwork network = pass as DhtNetwork;

			listValues.Columns.Clear();
			listValues.Items.Clear();

			listValues.Columns.Add("Address",	100, HorizontalAlignment.Left);
			listValues.Columns.Add("TcpPort",	100, HorizontalAlignment.Left);
			listValues.Columns.Add("UdpPort",	100, HorizontalAlignment.Left);
            listValues.Columns.Add("DhtID", 100, HorizontalAlignment.Left);
			listValues.Columns.Add("NextTry",	150, HorizontalAlignment.Left);
			listValues.Columns.Add("NextTryTcp",150, HorizontalAlignment.Left);


            foreach (IPCacheEntry entry in network.IPCache)
				listValues.Items.Add( new ListViewItem( new string[]
				{
					xStr(entry.Address.IP),		
					xStr(entry.TcpPort),
					xStr(entry.Address.UdpPort),
                    xStr(Utilities.IDtoBin(entry.Address.DhtID)),
					xStr(entry.NextTry),
					xStr(entry.NextTryTcp)
				}));
			
		}

		internal void UpdateSearches(object pass)
		{
            List<DhtSearch> searchList = pass as List<DhtSearch>;

			listValues.Columns.Clear();
			listValues.Items.Clear();

            listValues.Columns.Add("Name",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("KeyType",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("Target",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("SearchID",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("LookupList",	100, HorizontalAlignment.Left);
			listValues.Columns.Add("Finished",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("FinishReason",	100, HorizontalAlignment.Left);
            listValues.Columns.Add("FoundProxy",	100, HorizontalAlignment.Left);
			listValues.Columns.Add("ProxyTcp",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("FoundContact",	100, HorizontalAlignment.Left);
            listValues.Columns.Add("FoundValues",	100, HorizontalAlignment.Left);

            lock(searchList)
                foreach (DhtSearch search in searchList)
				    listValues.Items.Add( new ListViewItem( new string[]
				    {
                        xStr(search.Name),		
					    xStr(search.Component),
					    IDtoStr(search.TargetID),		
					    xStr(search.SearchID),
					    xStr(search.LookupList.Count),
					    xStr(search.Finished),		
					    xStr(search.FinishReason),
					    xStr(search.FoundProxy),
                        xStr(search.ProxyTcp),
					    xStr(search.FoundContact),
                        xStr(search.FoundValues.Count)
				    }));


            AddSearchNodes((StructureNode)treeStructure.SelectedNode, searchList);
		}

        internal void AddSearchNodes(StructureNode parentNode, List<DhtSearch> searchList)
		{
			parentNode.Nodes.Clear();

			foreach(DhtSearch search in searchList)
				parentNode.Nodes.Add( new StructureNode(search.Name, new ShowDelegate(ShowSearch), search));
		}

		internal void ShowSearch(object pass)
		{
			DhtSearch search = (DhtSearch) pass;
			
			listValues.Columns.Clear();
			listValues.Items.Clear();

			listValues.Columns.Add("Satus",		100, HorizontalAlignment.Left);
            listValues.Columns.Add("Age",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("DhtID",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("ClientID",	100, HorizontalAlignment.Left);
			listValues.Columns.Add("Address",	100, HorizontalAlignment.Left);
			listValues.Columns.Add("TcpPort",	100, HorizontalAlignment.Left);
			listValues.Columns.Add("UdpPort",	100, HorizontalAlignment.Left);
			listValues.Columns.Add("LastSeen",	100, HorizontalAlignment.Left);
			listValues.Columns.Add("NextTry",	100, HorizontalAlignment.Left);
			listValues.Columns.Add("Attempts",	100, HorizontalAlignment.Left);
			listValues.Columns.Add("NextTryProxy",	100, HorizontalAlignment.Left);

            foreach (DhtLookup lookup in search.LookupList)
                listValues.Items.Add(new ListViewItem(new string[]
				{
                    xStr(lookup.Status),
                    xStr(lookup.Age),
					IDtoStr(lookup.Contact.DhtID),		
					xStr(lookup.Contact.ClientID),
					xStr(lookup.Contact.Address),
					xStr(lookup.Contact.TcpPort),
					xStr(lookup.Contact.UdpPort),		
					xStr(lookup.Contact.LastSeen),
					xStr(lookup.Contact.NextTry),
					xStr(lookup.Contact.Attempts),
					xStr(lookup.Contact.NextTryProxy)
				}));
		}

        internal void ShowLocations(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Property", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Value", 300, HorizontalAlignment.Left);

            listValues.Items.Add(new ListViewItem(new string[] { "LocationVersion", xStr(Core.Locations.LocationVersion) }));
            listValues.Items.Add(new ListViewItem(new string[] { "NextLocationUpdate", xStr(Core.Locations.NextLocationUpdate) }));
            listValues.Items.Add(new ListViewItem(new string[] { "GlobalIndex", xStr(Core.Locations.GlobalIndex.Count) }));
            listValues.Items.Add(new ListViewItem(new string[] { "LocationMap", xStr(Core.Locations.LocationMap.Count) }));
            
        }

        internal void ShowLocGlobal(object pass)
        {
            SetupLocationList();

            foreach (ulong opid in Core.Locations.GlobalIndex.Keys)
                foreach (CryptLoc loc in Core.Locations.GlobalIndex[opid])
                {
                    SignedData signed = SignedData.Decode(Core.Protocol, loc.Data);

                    if (signed != null)
                    {
                        LocationData data = LocationData.Decode(Core.Protocol, signed.Data);

                        LocInfo info = new LocInfo();
                        info.Location = data;
                        info.TTL = loc.TTL;

                        DisplayLoc(opid, info);
                    }
                }
        }

        internal void ShowLocOperation(object pass)
        {
            SetupLocationList();

            foreach (Dictionary<ushort, LocInfo> dict in Core.Locations.LocationMap.Values)
                foreach (LocInfo info in dict.Values)
                    DisplayLoc(Core.OpID, info);
        }

        private void DisplayLoc(ulong opid, LocInfo info)
        {
            listValues.Items.Add(new ListViewItem(new string[]
				{
                    IDtoStr(opid),
                    xStr(info.TTL),
                    IDtoStr(info.Location.KeyID),
					xStr(info.Location.Source.ClientID),		
					xStr(info.Location.Source.TcpPort),
					xStr(info.Location.Source.UdpPort),
					xStr(info.Location.Source.Firewall),
					xStr(info.Location.IP),		
					xStr(info.Location.Proxies.Count),
					xStr(info.Location.Location),
					xStr(info.Location.TTL),
					xStr(info.Location.Version)
				}));
        }

        internal void SetupLocationList()
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("OpID", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("TTL", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("DhtID", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("ClientID", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("TcpPort", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("UdpPort", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Firewall", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("IP", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Proxies", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Location", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("TTL", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Version", 100, HorizontalAlignment.Left);
        }

		internal void ShowTcp(object pass)
		{
            DhtNetwork network = pass as DhtNetwork;

            AddConnectionNodes((StructureNode)treeStructure.SelectedNode, network);
		
			ShowNone(null);
		}

        internal void AddConnectionNodes(StructureNode parentNode, DhtNetwork network)
		{
			parentNode.Nodes.Clear();

            lock(network.TcpControl.Connections)
                foreach (TcpConnect connect in network.TcpControl.Connections)
			    	parentNode.Nodes.Add( new StructureNode(connect.ToString(), new ShowDelegate(ShowTcpConnect), connect));
		}

		internal void ShowTcpConnect(object pass)
		{
			TcpConnect connect = (TcpConnect) pass;

			listValues.Columns.Clear();
			listValues.Items.Clear();

			listValues.Columns.Add("Property", 100, HorizontalAlignment.Left);
			listValues.Columns.Add("Value", 300, HorizontalAlignment.Left);

			listValues.Items.Add( new ListViewItem( new string[]{"Address",				xStr(connect.RemoteIP)} ) );
			listValues.Items.Add( new ListViewItem( new string[]{"TcpPort",				xStr(connect.TcpPort)} ) );
			listValues.Items.Add( new ListViewItem( new string[]{"UdpPort",				xStr(connect.UdpPort)} ) );
			listValues.Items.Add( new ListViewItem( new string[]{"DhtID",				IDtoStr(connect.DhtID)} ) );
			listValues.Items.Add( new ListViewItem( new string[]{"ClientID",			xStr(connect.ClientID)} ) );
			listValues.Items.Add( new ListViewItem( new string[]{"TcpSocket",			xStr(connect.TcpSocket)} ) );
			listValues.Items.Add( new ListViewItem( new string[]{"Age",					xStr(connect.Age)} ) );
			listValues.Items.Add( new ListViewItem( new string[]{"CheckedFirewall",		xStr(connect.CheckedFirewall)} ) );
			listValues.Items.Add( new ListViewItem( new string[]{"Outbound",			xStr(connect.Outbound)} ) );
			listValues.Items.Add( new ListViewItem( new string[]{"ByeMessage",			xStr(connect.ByeMessage)} ) );
			listValues.Items.Add( new ListViewItem( new string[]{"BytesReceivedinSec",	xStr(connect.BytesReceivedinSec)} ) );
			listValues.Items.Add( new ListViewItem( new string[]{"BytesSentinSec",		xStr(connect.BytesSentinSec)} ) );
			listValues.Items.Add( new ListViewItem( new string[]{"Proxy",				xStr(connect.Proxy)} ) );
		}

        internal void ShowStore(object pass)
        {
            DhtNetwork network = pass as DhtNetwork;
            DhtStore store = network.Store;

            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Key", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Mode", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Kind", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Value", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("TTL", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("LastUpdate", 100, HorizontalAlignment.Left);
        }

		internal void ShowRouting(object pass)
		{
            DhtNetwork network = pass as DhtNetwork;

			listValues.Columns.Clear();
			listValues.Items.Clear();

			listValues.Columns.Add("Depth",	100, HorizontalAlignment.Left);
			listValues.Columns.Add("LastBucket",	100, HorizontalAlignment.Left);
			listValues.Columns.Add("NextRefresh",150, HorizontalAlignment.Left);

            foreach (DhtBucket bucket in network.Routing.BucketList)
				listValues.Items.Add( new ListViewItem( new string[]
				{
					xStr(bucket.Depth),		
					xStr(bucket.Last),
					xStr(bucket.NextRefresh)
				}));

			AddBuckets((StructureNode) treeStructure.SelectedNode, network);
		}

        internal void AddBuckets(StructureNode parentNode, DhtNetwork network)
		{
			parentNode.Nodes.Clear();

            foreach (DhtBucket bucket in network.Routing.BucketList)
				parentNode.Nodes.Add( new StructureNode(bucket.Depth.ToString(), new ShowDelegate(ShowBucket), bucket));
		}

		internal void ShowBucket(object pass)
		{
            DhtBucket bucket = pass as DhtBucket;

			listValues.Columns.Clear();
			listValues.Items.Clear();

			listValues.Columns.Add("DhtID",			100, HorizontalAlignment.Left);
			listValues.Columns.Add("ClientID",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("Address",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("TcpPort",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("UdpPort",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("LastSeen",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("NextTry",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("Attempts",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("NextTryProxy",	100, HorizontalAlignment.Left);

			foreach(DhtContact contact in bucket.ContactList)
				
				
					listValues.Items.Add( new ListViewItem( new string[]
					{
						IDtoStr(contact.DhtID),
						xStr(contact.ClientID),		
						xStr(contact.Address),
						xStr(contact.TcpPort),
						xStr(contact.UdpPort),		
						xStr(contact.LastSeen),
						xStr(contact.NextTry),
						xStr(contact.Attempts),		
						xStr(contact.NextTryProxy)
					}));

		}

        internal void ShowRudp(object pass)
        {
			listValues.Columns.Clear();
			listValues.Items.Clear();

            listValues.Columns.Add("Name",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("DhtID",		100, HorizontalAlignment.Left);
			listValues.Columns.Add("ClientID",	100, HorizontalAlignment.Left);
			listValues.Columns.Add("Status",	100, HorizontalAlignment.Left);
            listValues.Columns.Add("Startup",	100, HorizontalAlignment.Left);

            foreach (List<RudpSession> list in Core.RudpControl.SessionMap.Values)
                foreach (RudpSession session in list)
                    listValues.Items.Add(new ListViewItem(new string[]
					{
						xStr(session.Name),
						IDtoStr(session.DhtID),		
						xStr(session.ClientID),
						xStr(session.Status),
						xStr(session.Startup)	
					}));
		}

        internal void ShowTransfers(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Property", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Value", 300, HorizontalAlignment.Left);

            listValues.Items.Add(new ListViewItem(new string[] { "TransferMap", xStr(Core.Transfers.DownloadMap.Count) }));
            listValues.Items.Add(new ListViewItem(new string[] { "UploadMap",   xStr(Core.Transfers.UploadMap.Count) }));
            listValues.Items.Add(new ListViewItem(new string[] { "Active",      xStr(Core.Transfers.Active.Count) }));
            listValues.Items.Add(new ListViewItem(new string[] { "Pending",     xStr(Core.Transfers.Pending.Count) }));
        
        }

        internal void ShowUploads(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Name",      100, HorizontalAlignment.Left);
            listValues.Columns.Add("DhtID",     100, HorizontalAlignment.Left);
            listValues.Columns.Add("ClientID",  100, HorizontalAlignment.Left);
            listValues.Columns.Add("Done",      100, HorizontalAlignment.Left);
            listValues.Columns.Add("TransferID", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Component",  100, HorizontalAlignment.Left);
            listValues.Columns.Add("FileSize",  100, HorizontalAlignment.Left);
            listValues.Columns.Add("FileHash",  100, HorizontalAlignment.Left);
            listValues.Columns.Add("FilePos", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Path",      100, HorizontalAlignment.Left);

            foreach (List<FileUpload> list in Core.Transfers.UploadMap.Values)
                foreach (FileUpload upload in list)
                    listValues.Items.Add(new ListViewItem(new string[]
					{
						xStr(upload.Session.Name),
						IDtoStr(upload.Session.DhtID),		
						xStr(upload.Session.ClientID),
						xStr(upload.Done),
						xStr(upload.Request.TransferID),
	                    ComponentID.GetName(upload.Details.Component),
						xStr(upload.Details.Size),		
						Utilities.BytestoHex(upload.Details.Hash),
						xStr(upload.FilePos),
                        xStr(upload.Path)
					}));
        }

        internal void ShowDownloads(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Target", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Status", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("TransferID", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Component", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("FileSize", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("FileHash", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("FilePos", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Path", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Searching", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Sources", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Attempted", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Sessions", 100, HorizontalAlignment.Left);

            
                foreach (FileDownload download in Core.Transfers.DownloadMap.Values)
                    listValues.Items.Add(new ListViewItem(new string[]
					{
						IDtoStr(download.Target),		
						xStr(download.Status),
						xStr(download.ID),	
                        ComponentID.GetName(download.Details.Component),
						xStr(download.Details.Size),		
						Utilities.BytestoHex(download.Details.Hash),
						xStr(download.FilePos),	
                        xStr(download.Path),
                        xStr(download.Searching),	
						xStr(download.Sources.Count),		
						xStr(download.Attempted.Count),
						xStr(download.Sessions.Count)
					}));
        }


        internal void ShowLinks(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Property", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Value", 300, HorizontalAlignment.Left);

            listValues.Items.Add(new ListViewItem(new string[] { "LocalLink",       xStr(Core.Links.LocalLink.Name) }));

            listValues.Items.Add(new ListViewItem(new string[] { "ProjectRoots",    xStr(Core.Links.ProjectRoots.Count) }));
            listValues.Items.Add(new ListViewItem(new string[] { "LinkMap",         xStr(Core.Links.LinkMap.Count) }));
            listValues.Items.Add(new ListViewItem(new string[] { "ProjectNames",    xStr(Core.Links.ProjectNames.Count) }));

            listValues.Items.Add(new ListViewItem(new string[] { "LinkPath",        xStr(Core.Links.LinkPath) }));
            listValues.Items.Add(new ListViewItem(new string[] { "StructureKnown",  xStr(Core.Links.StructureKnown) }));
        }

        internal void ShowLinkMap(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Name", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("DhtID", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Loaded", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Error", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Local Link", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Searched", 100, HorizontalAlignment.Left);
            
            listValues.Columns.Add("Projects", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Titles", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Uplinks", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Downlinks", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Confirmed", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Requests", 100, HorizontalAlignment.Left);

            listValues.Columns.Add("Version", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("FileHash", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("FileSize", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Path", 100, HorizontalAlignment.Left);


            foreach (OpLink link in Core.Links.LinkMap.Values)
            {
                string projects  = "";
                string titles    = "";
                string uplinks   = "";
                string downlinks = "";
                string confirmed = "";
                string requests = "";

                foreach (uint id in link.Projects)
                {
                    string projectName = GetProjectName(id);
                    
                    projects += projectName + ", ";

                    if (link.Title.ContainsKey(id) && link.Title[id] != "")
                        titles += projectName + ": " + link.Title[id] + ", ";

                    if (link.Uplink.ContainsKey(id))
                        uplinks += projectName + ": " + GetLinkName(link.Uplink[id].DhtID) + ", ";

                    if (link.Confirmed.ContainsKey(id) && link.Confirmed[id].Count > 0)
                    {
                        confirmed += projectName + ": ";

                        foreach (ulong key in link.Confirmed[id])
                            confirmed += GetLinkName(key) + ", ";
                    }
                }

                foreach (uint id in link.Downlinks.Keys)
                    if(link.Downlinks[id].Count > 0)
                    {
                        downlinks += GetProjectName(id) + ": ";

                        foreach (OpLink downlink in link.Downlinks[id])
                            downlinks += GetLinkName(downlink.DhtID) + ", ";
                    }

                 foreach (uint id in link.Requests.Keys)
                     if (link.Requests[id].Count > 0)
                     {
                         requests += GetProjectName(id) + ": ";

                         foreach (UplinkRequest request in link.Requests[id])
                             requests += GetLinkName(request.KeyID) + ", ";
                     }

                ListViewItem item = new ListViewItem(new string[]
					{
						xStr(link.Name),		
						IDtoStr(link.DhtID),
                        xStr(link.Loaded),
						xStr(link.Error),		
						xStr(link.InLocalLinkTree),
						xStr(link.Searched),	

                        xStr(projects),
                        xStr(titles),
						xStr(uplinks),		
						xStr(downlinks),
						xStr(confirmed),
                        xStr(requests),

                        "",
						"",		
						"",
						""
                    });

                if (link.Header != null)
                {
                    item.SubItems[12] = new ListViewItem.ListViewSubItem(item, xStr(link.Header.Version));
                    item.SubItems[13] = new ListViewItem.ListViewSubItem(item, Utilities.BytestoHex(link.Header.FileHash));
                    item.SubItems[14] = new ListViewItem.ListViewSubItem(item, xStr(link.Header.FileSize));
                    item.SubItems[15] = new ListViewItem.ListViewSubItem(item, xStr(Core.Links.GetFilePath(link.Header)));
                }
            
                listValues.Items.Add(item);
            }
        }


        internal void ShowMail(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Property", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Value", 300, HorizontalAlignment.Left);

            listValues.Items.Add(new ListViewItem(new string[] { "MailMap",    xStr(Core.Mail.MailMap.Count) }));
            listValues.Items.Add(new ListViewItem(new string[] { "AckMap",     xStr(Core.Mail.AckMap.Count) }));
            listValues.Items.Add(new ListViewItem(new string[] { "PendingMap", xStr(Core.Mail.PendingMap.Count) }));

            listValues.Items.Add(new ListViewItem(new string[] { "PendingMail", xStr(Core.Mail.PendingMail.Count) }));
            listValues.Items.Add(new ListViewItem(new string[] { "PendingAcks", xStr(Core.Mail.PendingAcks.Count) }));
        }

        internal void ShowMailMap(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Source", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Target", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("FileHash", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("FileSize", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("TargetVersion", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("SourceVersion", 100, HorizontalAlignment.Left); 
            listValues.Columns.Add("MailID", 100, HorizontalAlignment.Left);


            foreach (List<CachedMail> list in Core.Mail.MailMap.Values)
                foreach (CachedMail mail in list)
                    listValues.Items.Add(new ListViewItem(new string[]
					{
						GetLinkName(mail.Header.SourceID),
                        GetLinkName(mail.Header.TargetID),
                        Utilities.BytestoHex(mail.Header.FileHash),
                        mail.Header.FileSize.ToString(),
                        mail.Header.TargetVersion.ToString(),
                        mail.Header.SourceVersion.ToString(),
                        Utilities.BytestoHex(mail.Header.MailID)
					}));
        }

        internal void ShowAckMap(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Source", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Target", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("TargetVersion", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("SourceVersion", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("MailID", 100, HorizontalAlignment.Left);


            foreach (List<CachedAck> list in Core.Mail.AckMap.Values)
                foreach (CachedAck cached in list)
                    listValues.Items.Add(new ListViewItem(new string[]
					{
						GetLinkName(cached.Ack.SourceID),
                        GetLinkName(cached.Ack.TargetID),
                        cached.Ack.TargetVersion.ToString(),
                        cached.Ack.SourceVersion.ToString(),
                        Utilities.BytestoHex(cached.Ack.MailID)
					}));
        }

        internal void ShowPendingMap(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Key", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Version", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("FileSize", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("FileHash", 100, HorizontalAlignment.Left);
            
            foreach (CachedPending pending in Core.Mail.PendingMap.Values)
                listValues.Items.Add(new ListViewItem(new string[]
					{
						GetLinkName(pending.Header.KeyID),
                        pending.Header.Version.ToString(),
                        pending.Header.FileSize.ToString(),
                        Utilities.BytestoHex(pending.Header.FileHash)
					}));
        }

        internal void ShowPendingMail(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Target", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("HashID", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("MailID", 100, HorizontalAlignment.Left);

            foreach(ulong hashID in Core.Mail.PendingMail.Keys)
                foreach(ulong target in Core.Mail.PendingMail[hashID])
                    listValues.Items.Add(new ListViewItem(new string[]
					{
						GetLinkName(target),
                        Utilities.BytestoHex(BitConverter.GetBytes(hashID)),
                        Utilities.BytestoHex(Core.Mail.GetMailID(hashID, target))
					}));
        }

        internal void ShowPendingAcks(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Target", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("MailID", 100, HorizontalAlignment.Left);

            foreach (ulong target in Core.Mail.PendingAcks.Keys)
                foreach (byte[] mailID in Core.Mail.PendingAcks[target])
                    listValues.Items.Add(new ListViewItem(new string[]
					{
						GetLinkName(target),
                        Utilities.BytestoHex(mailID)
					}));
        }

        internal void ShowBoard(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();


            // Target, Posts

            listValues.Columns.Add("Target", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Posts", 100, HorizontalAlignment.Left);

            foreach (ulong target in Core.Board.BoardMap.Keys)
            {
                listValues.Items.Add(new ListViewItem(new string[]
					{
						GetLinkName(target),
                        Core.Board.BoardMap[target].Posts.Count.ToString()
					}));
            }

            AddBoardNodes((StructureNode)treeStructure.SelectedNode);
        }

        internal void AddBoardNodes(StructureNode parentNode)
        {
            parentNode.Nodes.Clear();

            foreach (OpBoard board in Core.Board.BoardMap.Values)
                parentNode.Nodes.Add(new StructureNode(GetLinkName(board.DhtID), new ShowDelegate(ShowTargetBoard), board));
        }

        internal void ShowTargetBoard(object pass)
        {
            OpBoard board = pass as OpBoard;

            if (board == null)
                return;

            listValues.Columns.Clear();
            listValues.Items.Clear();

            // Target, Source, Project, Post, Parent, Version, Time, Scope, Hash, Size

            listValues.Columns.Add("Target", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Source", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Project", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Post", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Parent", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Version", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Time", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Scope", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Hash", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Size", 100, HorizontalAlignment.Left);
            

            foreach (OpPost post in board.Posts.Values)
            {
                PostHeader header = post.Header;

                listValues.Items.Add(new ListViewItem(new string[]
					{
						GetLinkName(header.TargetID),
                        GetLinkName(header.SourceID),
                        header.ProjectID.ToString(), 
                        header.PostID.ToString(),
                        header.ParentID.ToString(),
                        header.Version.ToString(), 
                        header.Time.ToString(),
                        header.Scope.ToString(),
                        Utilities.BytestoHex(header.FileHash),
                        header.FileSize.ToString()
					}));
            }
        }

        private string GetLinkName(ulong key)
        {
            string name = IDtoStr(key);

            if (Core.Links.LinkMap.ContainsKey(key))
                if (Core.Links.LinkMap[key].Loaded)
                    name = Core.Links.LinkMap[key].Name;
            
            return name;
        }

        private string GetProjectName(uint id)
        {
            string name = "unknown " + id.ToString();

            if (Core.Links.ProjectNames.ContainsKey(id))
                name = Core.Links.ProjectNames[id];

            return name;
        }

        internal void ShowLinkRoots(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("ID", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Name", 300, HorizontalAlignment.Left);

            foreach (uint id in Core.Links.ProjectRoots.Keys)
            {
                string project = "unknown";
                string names = "";

                if (Core.Links.ProjectNames.ContainsKey(id))
                    project = Core.Links.ProjectNames[id];

                foreach (OpLink link in Core.Links.ProjectRoots[id])
                    names += xStr(link.Name) + ", ";

                listValues.Items.Add(new ListViewItem(new string[] { project, names }));
            }
        }

        internal void ShowLinkProjects(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("ID", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Name", 300, HorizontalAlignment.Left);

            foreach (uint id in Core.Links.ProjectNames.Keys)
            {
                listValues.Items.Add(new ListViewItem(new string[] { id.ToString(), xStr(Core.Links.ProjectNames[id]) }));
            }

        }

        internal void ShowProfiles(object pass)
        {
            listValues.Columns.Clear();
            listValues.Items.Clear();

            listValues.Columns.Add("Name", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Version", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("FileHash", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("FileSize", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("EmbedStart", 100, HorizontalAlignment.Left);
            listValues.Columns.Add("Path", 100, HorizontalAlignment.Left);
            
            listValues.Columns.Add("Embedded", 100, HorizontalAlignment.Left);


            foreach (OpProfile profile in Core.Profiles.ProfileMap.Values)
            {
                if (!profile.Loaded)
                    Core.Profiles.LoadProfile(profile.DhtID);

                string embedded = "";
                foreach (ProfileFile file in profile.Files)
                    embedded += file.Name + ": " + file.Size.ToString() + "bytes, ";

                ListViewItem item = new ListViewItem(new string[]
				{
					GetLinkName(profile.DhtID),
					"",
					"",
					"",
                    "",
                    "",

					embedded
				});

                if (profile.Header != null)
                {
                    item.SubItems[2] = new ListViewItem.ListViewSubItem(item, xStr(profile.Header.Version));
                    item.SubItems[3] = new ListViewItem.ListViewSubItem(item, Utilities.BytestoHex(profile.Header.FileHash));
                    item.SubItems[4] = new ListViewItem.ListViewSubItem(item, xStr(profile.Header.FileSize));
                    item.SubItems[5] = new ListViewItem.ListViewSubItem(item, xStr(profile.Header.EmbeddedStart));
                    item.SubItems[6] = new ListViewItem.ListViewSubItem(item, xStr(Core.Profiles.GetFilePath(profile.Header)));
                }

                listValues.Items.Add(item);
            }
        }

		internal void UpdateLogs(object pass)
		{
            DhtNetwork network = pass as DhtNetwork;

            AddLogs((StructureNode)treeStructure.SelectedNode, network);
		
			ShowNone(null);
		}

		internal void AddLogs(StructureNode parentNode, DhtNetwork network)
		{
			parentNode.Nodes.Clear();

            lock(network.LogTable)
                foreach (string name in network.LogTable.Keys)
				    parentNode.Nodes.Add( new StructureNode(name, new ShowDelegate(ShowLog), new LogInfo(name, network)));
		}

		internal void ShowLog(object pass)
		{
            LogInfo info = pass as LogInfo;

			listValues.Columns.Clear();
			listValues.Items.Clear();

			listValues.Columns.Add("Entry", 600, HorizontalAlignment.Left);

            lock(info.Network.LogTable)
                foreach (string message in info.Network.LogTable[info.Name])
				    listValues.Items.Add( new ListViewItem( new string[] {message} ) );

			listValues.EnsureVisible( listValues.Items.Count - 1);
		}

		private void InternalsForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
            Core.GuiInternal = null;
		}

		string xStr(object o)
		{
			if(o == null)
				return "null";
			
			return o.ToString();
		}

		string IDtoStr(UInt64 id)
		{
			return Utilities.IDtoBin(id);
		}

		private void buttonRefresh_Click(object sender, System.EventArgs e)
		{
			RefreshView();
		}

		void RefreshView()
		{
			if(treeStructure.SelectedNode == null)
				return;

			if(treeStructure.SelectedNode.GetType() != typeof(StructureNode))
				return;

			StructureNode node = (StructureNode) treeStructure.SelectedNode;
				
			node.RunShow();
		}

        string copytext = "";

        private void listValues_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            ListViewItem.ListViewSubItem item = GetItemAt(e.Location);

            if (item == null)
                return;

            copytext = item.Text;

            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add( new MenuItem("Copy", new EventHandler(ItemCopy_Click)));

            menu.Show(listValues, e.Location);
        }

        private void ItemCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(copytext);
        }

        private ListViewItem.ListViewSubItem GetItemAt(Point point)
        {
            ListViewItem item = listValues.GetItemAt(point.X, point.Y);

            if (item == null)
                return null;

            ListViewItem.ListViewSubItem sub = item.GetSubItemAt(point.X, point.Y);

            return sub;
        }

		
	}

    internal class LogInfo
    {
        internal string Name;
        internal DhtNetwork Network;

        internal LogInfo(string name, DhtNetwork network)
        {
            Name = name;
            Network = network;
        }
    }


	
	internal class StructureNode : TreeNode
	{
		ShowDelegate Show;
		object Pass;

		internal StructureNode(string text, ShowDelegate show, object pass) : base(text)
		{
			Show = show;
			Pass = pass;
		}

		internal void RunShow()
		{
			if(Show != null)
				Show(Pass);
		}
	}
}
