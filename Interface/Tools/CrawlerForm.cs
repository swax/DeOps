using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Services;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;

namespace RiseOp.Interface.Tools
{
	internal enum CrawlStatus {Active, Paused};

	/// <summary>
	/// Summary description for CrawlerForm.
	/// </summary>
	internal class CrawlerForm : System.Windows.Forms.Form
	{
		DhtNetwork Network;
		DhtRouting Routing;

		Hashtable CrawlMap = new Hashtable();

		CrawlStatus Status = CrawlStatus.Paused;

		internal delegate void SearchAckHandler(SearchAck ack, G2ReceivedPacket packet);
		internal SearchAckHandler SearchAck;

		internal delegate void CrawlAckHandler(CrawlAck ack, G2ReceivedPacket packet);
		internal CrawlAckHandler CrawlAck;

		private ListViewColumnSorter lvwColumnSorter = new ListViewColumnSorter();

		private System.Windows.Forms.ListView listViewNodes;
		private System.Windows.Forms.Button buttonRefresh;
        private System.Windows.Forms.ColumnHeader columnHeaderKidBin;
		private System.Windows.Forms.ColumnHeader columnHeaderIP;
		private System.Windows.Forms.ColumnHeader columnHeaderClientID;
		private System.Windows.Forms.ColumnHeader columnHeaderPorts;
		private System.Windows.Forms.ColumnHeader columnHeaderVersion;
		private System.Windows.Forms.ColumnHeader columnHeaderFirewall;
		private System.Windows.Forms.ColumnHeader columnHeaderUptime;
		private System.Windows.Forms.ColumnHeader columnHeaderDepth;
		private System.Windows.Forms.ColumnHeader columnHeaderProxied;
		private System.Windows.Forms.ColumnHeader columnHeaderServers;
		private System.Windows.Forms.Button buttonControl;
        private System.Windows.Forms.Timer timerSecond;
		private System.Windows.Forms.Label labelCount;
		private System.ComponentModel.IContainer components;

        internal CrawlerForm(string name, DhtNetwork network)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

            Network = network;
            Routing = Network.Routing;

			SearchAck = new SearchAckHandler(AsyncSearchAck);
			CrawlAck  = new CrawlAckHandler(AsyncCrawlAck);

            Text = name + " Crawler (" + Network.Core.User.Settings.ScreenName + ")";

			listViewNodes.ListViewItemSorter = lvwColumnSorter;
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CrawlerForm));
            this.listViewNodes = new System.Windows.Forms.ListView();
            this.columnHeaderKidBin = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderIP = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderClientID = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderPorts = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderVersion = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderFirewall = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderUptime = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderDepth = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderProxied = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderServers = new System.Windows.Forms.ColumnHeader();
            this.buttonControl = new System.Windows.Forms.Button();
            this.buttonRefresh = new System.Windows.Forms.Button();
            this.timerSecond = new System.Windows.Forms.Timer(this.components);
            this.labelCount = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // listViewNodes
            // 
            this.listViewNodes.AllowColumnReorder = true;
            this.listViewNodes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewNodes.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderKidBin,
            this.columnHeaderIP,
            this.columnHeaderClientID,
            this.columnHeaderPorts,
            this.columnHeaderVersion,
            this.columnHeaderFirewall,
            this.columnHeaderUptime,
            this.columnHeaderDepth,
            this.columnHeaderProxied,
            this.columnHeaderServers});
            this.listViewNodes.FullRowSelect = true;
            this.listViewNodes.Location = new System.Drawing.Point(0, 40);
            this.listViewNodes.Name = "listViewNodes";
            this.listViewNodes.Size = new System.Drawing.Size(696, 312);
            this.listViewNodes.TabIndex = 0;
            this.listViewNodes.UseCompatibleStateImageBehavior = false;
            this.listViewNodes.View = System.Windows.Forms.View.Details;
            this.listViewNodes.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listViewNodes_ColumnClick);
            // 
            // columnHeaderKidBin
            // 
            this.columnHeaderKidBin.Text = "KID Bin";
            // 
            // columnHeaderIP
            // 
            this.columnHeaderIP.Text = "IP";
            // 
            // columnHeaderClientID
            // 
            this.columnHeaderClientID.Text = "ClientID";
            // 
            // columnHeaderPorts
            // 
            this.columnHeaderPorts.Text = "Ports T/U";
            // 
            // columnHeaderVersion
            // 
            this.columnHeaderVersion.Text = "Version";
            // 
            // columnHeaderFirewall
            // 
            this.columnHeaderFirewall.Text = "Firewall";
            // 
            // columnHeaderUptime
            // 
            this.columnHeaderUptime.Text = "Uptime";
            // 
            // columnHeaderDepth
            // 
            this.columnHeaderDepth.Text = "Depth";
            // 
            // columnHeaderProxied
            // 
            this.columnHeaderProxied.Text = "Proxied";
            // 
            // columnHeaderServers
            // 
            this.columnHeaderServers.Text = "Servers";
            // 
            // buttonControl
            // 
            this.buttonControl.Location = new System.Drawing.Point(8, 8);
            this.buttonControl.Name = "buttonControl";
            this.buttonControl.Size = new System.Drawing.Size(75, 23);
            this.buttonControl.TabIndex = 1;
            this.buttonControl.Text = "Start";
            this.buttonControl.Click += new System.EventHandler(this.buttonControl_Click);
            // 
            // buttonRefresh
            // 
            this.buttonRefresh.Location = new System.Drawing.Point(96, 8);
            this.buttonRefresh.Name = "buttonRefresh";
            this.buttonRefresh.Size = new System.Drawing.Size(75, 23);
            this.buttonRefresh.TabIndex = 2;
            this.buttonRefresh.Text = "Refresh";
            this.buttonRefresh.Click += new System.EventHandler(this.buttonRefresh_Click);
            // 
            // timerSecond
            // 
            this.timerSecond.Enabled = true;
            this.timerSecond.Interval = 1000;
            this.timerSecond.Tick += new System.EventHandler(this.timerSecond_Tick);
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
            this.Controls.Add(this.buttonRefresh);
            this.Controls.Add(this.buttonControl);
            this.Controls.Add(this.listViewNodes);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CrawlerForm";
            this.Text = "Crawler";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.CrawlerForm_Closing);
            this.ResumeLayout(false);

		}
		#endregion

		private void buttonControl_Click(object sender, System.EventArgs e)
		{
			if(buttonControl.Text == "Start")
			{
				// copy all nodes from routing to crawl map
				lock(Routing.BucketList)
					foreach(DhtBucket bucket in Routing.BucketList)
						foreach(DhtContact contact in bucket.ContactList)
                            if (!CrawlMap.Contains(contact.DhtID))
                                CrawlMap.Add(contact.DhtID, new CrawlNode(contact));
			
				Status = CrawlStatus.Active;
				buttonControl.Text = "Pause";
			}

			else if(buttonControl.Text == "Pause")
			{
				Status = CrawlStatus.Paused;
				buttonControl.Text = "Resume";
			}

			else if(buttonControl.Text == "Resume")
			{
				Status = CrawlStatus.Active;
				buttonControl.Text = "Pause";
			}
		}

		private void CrawlerForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
            Network.GuiCrawler = null;
		}

		private void timerSecond_Tick(object sender, System.EventArgs e)
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

			foreach(CrawlNode node in CrawlMap.Values)
			{
				// send crawl request
				if( node.Ack == null && node.Attempts < 3 && DateTime.Now > node.NextTry)
				{		
					CrawlNode targetNode;
					
					if(node.LookupContacts)
						targetNode = node; // send direct

					else
						targetNode = node.Proxy; // else send through proxy

                    Network.Send_CrawlRequest(targetNode.Contact.ToDhtAddress());
					
					
					sendPackets--;

					node.Attempts++;
					node.NextTry = DateTime.Now.AddSeconds(15);
				}

				if(node.LookupContacts && !node.Searched)
				{
                    Network.Searches.SendUdpRequest(node.Contact.ToDhtAddress(), node.Contact.DhtID + 1, 0, Network.Core.DhtServiceID, 0, null);

					node.Searched = true;
					sendPackets--;
				}

				if(sendPackets <= 0)
					break;
			}
		}

		internal void AsyncSearchAck(SearchAck ack, G2ReceivedPacket packet)
		{
            DhtContact source = new DhtContact(ack.Source, packet.Source.IP, Network.Core.TimeNow);
			
			if( !CrawlMap.Contains(source.DhtID) )
				CrawlMap.Add(source.DhtID, source);

			foreach(DhtContact contact in ack.ContactList)
				if( !CrawlMap.Contains(contact.DhtID) )
					CrawlMap.Add(contact.DhtID, new CrawlNode(contact));
		}

		internal void AsyncCrawlAck(CrawlAck ack, G2ReceivedPacket packet)
		{
			if( !CrawlMap.Contains(ack.Source.DhtID) )
				return;

			CrawlNode node = (CrawlNode) CrawlMap[ack.Source.DhtID];

			node.Ack = ack;


			foreach(DhtContact contact in ack.ProxyList)
				if( !CrawlMap.Contains(contact.DhtID) )
				{
					CrawlNode newNode = new CrawlNode(contact);
					newNode.LookupContacts = false;
					newNode.Proxy = node;

					CrawlMap.Add(contact.DhtID, newNode);
				}
		}

		private void buttonRefresh_Click(object sender, System.EventArgs e)
		{
			listViewNodes.BeginUpdate();

			listViewNodes.Items.Clear();

			foreach(CrawlNode node in CrawlMap.Values)
				if(node.Ack != null)
				{
					listViewNodes.Items.Add( new ListViewItem( new string[]
					{
								Utilities.IDtoBin(node.Contact.DhtID),
								node.Contact.Address.ToString(),
								node.Contact.ClientID.ToString(),
								node.Contact.TcpPort.ToString() + " / " + node.Contact.UdpPort.ToString(),
								node.Ack.Version,
								(node.Ack.Source.Firewall).ToString(),
								new TimeSpan(0, 0, 0, node.Ack.Uptime, 0).ToString(),
								node.Ack.Depth.ToString(),
								GetProxyIDs(true, node),
								GetProxyIDs(false, node)
					}));
				}

			listViewNodes.EndUpdate();
		}

		string GetProxyIDs(bool server, CrawlNode node)
		{
			if(server && node.Ack.Source.Firewall != FirewallType.Open)
				return "";

            if (!server && node.Ack.Source.Firewall == FirewallType.Open)
				return "";

			string idList = "";
			foreach(DhtContact contact  in node.Ack.ProxyList)
				idList += contact.DhtID + ", ";

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
			listViewNodes.Sort();
		}
	}

	internal class CrawlNode
	{
		internal DhtContact Contact;
		

		// crawl
		internal CrawlAck  Ack;
		internal int		 Attempts;
		internal DateTime  NextTry = new DateTime(0);
		internal CrawlNode Proxy;

		// routing
		internal bool LookupContacts;
		internal bool Searched;

		internal CrawlNode(DhtContact contact)
		{
			Contact  = contact;
			LookupContacts = true;
		}
	}

}
