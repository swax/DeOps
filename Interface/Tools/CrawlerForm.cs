using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Services;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;

namespace DeOps.Interface.Tools
{
	internal enum CrawlStatus {Active, Paused};

	/// <summary>
	/// Summary description for CrawlerForm.
	/// </summary>
    internal class CrawlerForm : DeOps.Interface.CustomIconForm
	{
        OpCore Core;
		DhtNetwork Network;
		DhtRouting Routing;

        Dictionary<ulong, CrawlNode> CrawlMap = new Dictionary<ulong, CrawlNode>();

		CrawlStatus Status = CrawlStatus.Paused;

		internal delegate void SearchAckHandler(SearchAck ack, G2ReceivedPacket packet);
		internal SearchAckHandler SearchAck;

		internal delegate void CrawlAckHandler(CrawlAck ack, G2ReceivedPacket packet);
		internal CrawlAckHandler CrawlAck;

		private ListViewColumnSorter lvwColumnSorter = new ListViewColumnSorter();

		private System.Windows.Forms.ListView NodeList;
		private System.Windows.Forms.Button RefreshButton;
        private System.Windows.Forms.ColumnHeader columnHeaderRouting;
        private System.Windows.Forms.ColumnHeader columnHeaderIP;
		private System.Windows.Forms.ColumnHeader columnHeaderVersion;
		private System.Windows.Forms.ColumnHeader columnHeaderFirewall;
        private System.Windows.Forms.ColumnHeader columnHeaderUptime;
		private System.Windows.Forms.ColumnHeader columnHeaderProxied;
		private System.Windows.Forms.ColumnHeader columnHeaderServers;
		private System.Windows.Forms.Button StartButton;
        private System.Windows.Forms.Timer SecondTimer;
		private System.Windows.Forms.Label labelCount;
        private ColumnHeader columnHeaderBandwidth;
		private System.ComponentModel.IContainer components;


        internal static void Show(DhtNetwork network)
        {
            if (network.GuiCrawler == null)
                network.GuiCrawler = new CrawlerForm(network);

            network.GuiCrawler.Show();
            network.GuiCrawler.Activate();
        }

        internal CrawlerForm(DhtNetwork network)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

            Core = network.Core;
            Network = network;
            Routing = Network.Routing;

            SearchAck = new SearchAckHandler(Receive_SearchAck);
			CrawlAck  = new CrawlAckHandler(Receive_CrawlAck);

            Text = "Crawler (" + Network.GetLabel() + ")";

			NodeList.ListViewItemSorter = lvwColumnSorter;
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
            this.components = new System.ComponentModel.Container();
            this.NodeList = new System.Windows.Forms.ListView();
            this.columnHeaderRouting = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderFirewall = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderIP = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderBandwidth = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderVersion = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderUptime = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderProxied = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderServers = new System.Windows.Forms.ColumnHeader();
            this.StartButton = new System.Windows.Forms.Button();
            this.RefreshButton = new System.Windows.Forms.Button();
            this.SecondTimer = new System.Windows.Forms.Timer(this.components);
            this.labelCount = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // NodeList
            // 
            this.NodeList.AllowColumnReorder = true;
            this.NodeList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.NodeList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderRouting,
            this.columnHeaderFirewall,
            this.columnHeaderIP,
            this.columnHeaderBandwidth,
            this.columnHeaderVersion,
            this.columnHeaderUptime,
            this.columnHeaderProxied,
            this.columnHeaderServers});
            this.NodeList.FullRowSelect = true;
            this.NodeList.Location = new System.Drawing.Point(0, 40);
            this.NodeList.Name = "NodeList";
            this.NodeList.Size = new System.Drawing.Size(696, 312);
            this.NodeList.TabIndex = 0;
            this.NodeList.UseCompatibleStateImageBehavior = false;
            this.NodeList.View = System.Windows.Forms.View.Details;
            this.NodeList.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listViewNodes_ColumnClick);
            // 
            // columnHeaderRouting
            // 
            this.columnHeaderRouting.Text = "Routing";
            // 
            // columnHeaderFirewall
            // 
            this.columnHeaderFirewall.DisplayIndex = 3;
            this.columnHeaderFirewall.Text = "Firewall";
            // 
            // columnHeaderIP
            // 
            this.columnHeaderIP.DisplayIndex = 1;
            this.columnHeaderIP.Text = "IP";
            // 
            // columnHeaderBandwidth
            // 
            this.columnHeaderBandwidth.DisplayIndex = 7;
            this.columnHeaderBandwidth.Text = "Bandwidth";
            // 
            // columnHeaderVersion
            // 
            this.columnHeaderVersion.DisplayIndex = 2;
            this.columnHeaderVersion.Text = "Version";
            // 
            // columnHeaderUptime
            // 
            this.columnHeaderUptime.DisplayIndex = 4;
            this.columnHeaderUptime.Text = "Uptime";
            // 
            // columnHeaderProxied
            // 
            this.columnHeaderProxied.DisplayIndex = 5;
            this.columnHeaderProxied.Text = "Proxied";
            // 
            // columnHeaderServers
            // 
            this.columnHeaderServers.DisplayIndex = 6;
            this.columnHeaderServers.Text = "Servers";
            // 
            // StartButton
            // 
            this.StartButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.StartButton.Location = new System.Drawing.Point(8, 8);
            this.StartButton.Name = "StartButton";
            this.StartButton.Size = new System.Drawing.Size(75, 23);
            this.StartButton.TabIndex = 1;
            this.StartButton.Text = "Start";
            this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // RefreshButton
            // 
            this.RefreshButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.RefreshButton.Location = new System.Drawing.Point(96, 8);
            this.RefreshButton.Name = "RefreshButton";
            this.RefreshButton.Size = new System.Drawing.Size(75, 23);
            this.RefreshButton.TabIndex = 2;
            this.RefreshButton.Text = "Refresh";
            this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
            // 
            // SecondTimer
            // 
            this.SecondTimer.Enabled = true;
            this.SecondTimer.Interval = 1000;
            this.SecondTimer.Tick += new System.EventHandler(this.SecondTimer_Tick);
            // 
            // labelCount
            // 
            this.labelCount.Location = new System.Drawing.Point(192, 16);
            this.labelCount.Name = "labelCount";
            this.labelCount.Size = new System.Drawing.Size(248, 16);
            this.labelCount.TabIndex = 3;
            // 
            // CrawlerForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(696, 350);
            this.Controls.Add(this.labelCount);
            this.Controls.Add(this.RefreshButton);
            this.Controls.Add(this.StartButton);
            this.Controls.Add(this.NodeList);
            this.Name = "CrawlerForm";
            this.Text = "Crawler";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.CrawlerForm_Closing);
            this.ResumeLayout(false);

		}
		#endregion

		private void StartButton_Click(object sender, System.EventArgs e)
		{
			if(StartButton.Text == "Start")
			{
				// copy all nodes from routing to crawl map
				lock(Routing.BucketList)
					foreach(DhtContact contact in Routing.ContactMap.Values)
                        if (!CrawlMap.ContainsKey(contact.RoutingID))
                            CrawlMap[contact.RoutingID] = new CrawlNode(contact);
			
				Status = CrawlStatus.Active;
				StartButton.Text = "Pause";
			}

			else if(StartButton.Text == "Pause")
			{
				Status = CrawlStatus.Paused;
				StartButton.Text = "Resume";
			}

			else if(StartButton.Text == "Resume")
			{
				Status = CrawlStatus.Active;
				StartButton.Text = "Pause";
			}
		}

		private void CrawlerForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
            Network.GuiCrawler = null;
		}

		private void SecondTimer_Tick(object sender, System.EventArgs e)
		{
			if(Status != CrawlStatus.Active)
				return;

			
			// Total Crawled
			int crawled = 0;
			foreach(CrawlNode node in CrawlMap.Values)
				if(node.Ack != null)
					crawled++;

			labelCount.Text = crawled.ToString() + " Crawled";


			// Send Packets
			int sendPackets = 20;

			foreach(CrawlNode nodeX in CrawlMap.Values)
			{
                // if async funcs called with nodeX, then they get reset with each loop, give node its own object
                CrawlNode node = nodeX; 

				// send crawl request
				if( node.Ack == null && node.Attempts < 3 && Core.TimeNow > node.NextTry)
				{		
					CrawlNode targetNode = node.OpenConnection ? node : node.Proxy;

                    Core.RunInCoreAsync(delegate()
                    {
                        Network.Send_CrawlRequest(targetNode.Contact, node.Contact);
                    });
					
					sendPackets--;

					node.Attempts++;
                    node.NextTry = Core.TimeNow.AddSeconds(15);
				}

                if (node.OpenConnection && !node.Searched)
				{
                    Core.RunInCoreAsync(delegate()
                    {
                        Network.Searches.SendRequest(node.Contact, node.Contact.UserID + 1, 0, Core.DhtServiceID, 0, null);
                    });

					node.Searched = true;
					sendPackets--;
				}

				if(sendPackets <= 0)
					break;
			}
		}

        internal void Receive_SearchAck(SearchAck ack, G2ReceivedPacket packet)
		{
            DhtContact source = new DhtContact(ack.Source, packet.Source.IP);
			
			if( !CrawlMap.ContainsKey(source.RoutingID) )
                CrawlMap[source.RoutingID] = new CrawlNode(source);

			foreach(DhtContact contact in ack.ContactList)
                if (!CrawlMap.ContainsKey(contact.RoutingID))
                    CrawlMap[contact.RoutingID] = new CrawlNode(contact);
		}

		internal void  Receive_CrawlAck(CrawlAck ack, G2ReceivedPacket packet)
		{
            DhtContact source = new DhtContact(ack.Source, packet.Source.IP);

            if (!CrawlMap.ContainsKey(source.RoutingID))
				return;

			CrawlNode node = CrawlMap[source.RoutingID];

			node.Ack = ack;

            foreach(DhtContact contact in ack.ProxyServers)
                if (!CrawlMap.ContainsKey(contact.RoutingID))
                    CrawlMap[contact.RoutingID] = new CrawlNode(contact);

			foreach(DhtContact contact in ack.ProxyClients)
                if (!CrawlMap.ContainsKey(contact.RoutingID))
				{
					CrawlNode newNode = new CrawlNode(contact);
					newNode.Proxy = node;

                    CrawlMap[contact.RoutingID] = newNode;
				}


		}

		private void RefreshButton_Click(object sender, System.EventArgs e)
		{
			NodeList.BeginUpdate();

			NodeList.Items.Clear();

			foreach(CrawlNode node in CrawlMap.Values)
				if(node.Ack != null)
				{
					NodeList.Items.Add( new ListViewItem( new string[]
					{
								Utilities.IDtoBin(node.Contact.RoutingID),
								node.Contact.IP.ToString(),
								node.Ack.Version,
								(node.Ack.Source.Firewall).ToString(),
								new TimeSpan(0, 0, 0, node.Ack.Uptime, 0).ToString(),
								GetProxyIDs(true, node),
								GetProxyIDs(false, node), 
                                ""
					}));
				}

			NodeList.EndUpdate();
		}

		string GetProxyIDs(bool server, CrawlNode node)
		{

            List<DhtContact> list = server ? node.Ack.ProxyClients : node.Ack.ProxyServers;

			string idList = "";
            foreach (DhtContact contact in list)
				idList += contact.UserID + ", ";

			if(idList.Length > 0)
				idList = idList.Substring(0, idList.Length - 2);

			return idList;
		}

		private void listViewNodes_ColumnClick(object sender, System.Windows.Forms.ColumnClickEventArgs e)
		{
			// Determine if clicked column is already the column that is being sorted.
			if ( e.Column == lvwColumnSorter.ColumnToSort )
			{
				// Reverse the current sort direction for this column.
				if (lvwColumnSorter.OrderOfSort == SortOrder.Ascending)
					lvwColumnSorter.OrderOfSort = SortOrder.Descending;
				else
					lvwColumnSorter.OrderOfSort = SortOrder.Ascending;
			}
			else
			{
				// Set the column number that is to be sorted; default to ascending.
				lvwColumnSorter.ColumnToSort = e.Column;
				lvwColumnSorter.OrderOfSort = SortOrder.Ascending;
			}

			// Perform the sort with these new sort options.
			NodeList.Sort();
		}
    }

	internal class CrawlNode
	{
		internal DhtContact Contact;
		

		// crawl
		internal CrawlAck  Ack;
		internal int	   Attempts;
		internal DateTime  NextTry = new DateTime(0);

		internal CrawlNode Proxy;
        internal bool OpenConnection { get { return Proxy == null; } }

		// routing
		internal bool Searched;


		internal CrawlNode(DhtContact contact)
		{
			Contact  = contact;
		}
	}

}
