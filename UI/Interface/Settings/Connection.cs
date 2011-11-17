using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;

namespace DeOps.Interface.Settings
{
    public partial class Connection : CustomIconForm
    {
        OpCore Core;
        OpCore Lookup;
        OpUser Profile;

        bool SaveCache;


        public Connection(OpCore core)
            : base(core)
        {
            InitializeComponent();

            Core = core;
            Lookup = Core.Context.Lookup;
            Profile = Core.User;

            OperationLabel.Text = Core.User.Settings.Operation;

            if (Profile.Settings.OpAccess == AccessType.Secret && !Core.User.Settings.GlobalIM)
            {
                LookupLabel.Visible = false;
                LookupTcpBox.Visible = false;
                LookupUdpBox.Visible = false;
                LookupLanBox.Visible = false;
                LookupStatusBox.Visible = false;
            }

            OpTcpBox.Text = Core.Network.TcpControl.ListenPort.ToString();
            OpUdpBox.Text = Core.Network.UdpControl.ListenPort.ToString();
            OpLanBox.Text = Core.Network.LanControl.ListenPort.ToString();

            OpStatusBox.Text = Core.Firewall.ToString();
            OpStatusBox.BackColor = GetStatusColor(Core.Firewall);

            OpTcpBox.KeyPress += new KeyPressEventHandler(PortBox_KeyPress);
            OpUdpBox.KeyPress += new KeyPressEventHandler(PortBox_KeyPress);

            if (Lookup != null)
            {
                LookupTcpBox.Text = Lookup.Network.TcpControl.ListenPort.ToString();
                LookupUdpBox.Text = Lookup.Network.UdpControl.ListenPort.ToString();
                LookupLanBox.Text = Lookup.Network.LanControl.ListenPort.ToString();

                LookupStatusBox.Text = Lookup.Firewall.ToString();
                LookupStatusBox.BackColor = GetStatusColor(Lookup.Firewall);

                LookupTcpBox.KeyPress += new KeyPressEventHandler(PortBox_KeyPress);
                LookupUdpBox.KeyPress += new KeyPressEventHandler(PortBox_KeyPress);
            }

            // load web caches
            foreach (WebCache cache in Core.Network.Cache.WebCaches)
                CacheList.Items.Add(new CacheItem(cache));
        }

        private Color GetStatusColor(FirewallType firewall)
        {
            if (firewall == FirewallType.Open)
                return Color.FromArgb(0, 192, 0);

            else if (firewall == FirewallType.NAT)
                return Color.FromArgb(255, 128, 0);
            
            else
                return Color.FromArgb(192, 0, 0);
        }

        private void AddLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CacheSetup setup = new CacheSetup(Core, new WebCache());

            if (setup.ShowDialog() == DialogResult.OK)
            {
                CacheList.Items.Add(new CacheItem(setup.Cache));
                SaveCache = true;
            }
        }

        private void RemoveLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            List<CacheItem> copy = new List<CacheItem>();

            foreach (CacheItem item in CacheList.SelectedItems)
                copy.Add(item);

            foreach (CacheItem item in copy)
                CacheList.Items.Remove(item);

            SaveCache = true;
        }

        private void SetupLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (CacheList.SelectedItem == null)
                return;

            CacheItem item = CacheList.SelectedItem as CacheItem;

            CacheSetup setup = new CacheSetup(Core, item.Cache);

            if (setup.ShowDialog() == DialogResult.OK)
            {
                CacheList.Update();
                SaveCache = true;
            }
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            bool savePorts = false;

            ushort opTcp = 0, opUdp = 0, opLan = 0, 
                   globalTcp = 0, globalUdp = 0, globalLan = 0;

            // get port settings from text boxes
            try
            {
                opTcp = ushort.Parse(OpTcpBox.Text);
                opUdp = ushort.Parse(OpUdpBox.Text);
                opLan = ushort.Parse(OpLanBox.Text);

                if (opTcp <= 0 || opUdp <= 0)
                    throw new Exception();

                if (Lookup != null)
                {
                    globalTcp = ushort.Parse(LookupTcpBox.Text);
                    globalUdp = ushort.Parse(LookupUdpBox.Text);
                    globalLan = ushort.Parse(LookupLanBox.Text);

                    if (globalTcp <= 0 || globalUdp <= 0)
                        throw new Exception();
                }
            }
            catch
            {
                MessageBox.Show(this, "Port must be between 1 and " + ushort.MaxValue, "DeOps");
                return;
            }

            // check that tcp are not equal
            if (Lookup != null && opTcp == globalTcp)
            {
                MessageBox.Show(this, "TCP ports cannot be equal", "DeOps");
                return;
            }

            // check that udp/lan ports are not equal
            Dictionary<ushort, bool> checkMap = new Dictionary<ushort, bool>();
            checkMap.Add(opUdp, true);
            checkMap.Add(opLan, true);
            
            if (Lookup != null)
            {
                checkMap.Add(globalUdp, true);
                checkMap.Add(globalLan, true);
            }

            if ((Lookup == null && checkMap.Count != 2) || (Lookup != null && checkMap.Count != 4) )
            {
                MessageBox.Show(this, "UDP/LAN ports cannot be equal", "DeOps");
                return;
            }

            if (opTcp != Core.Network.TcpControl.ListenPort || opUdp != Core.Network.UdpControl.ListenPort)
                savePorts = true;

            if(Lookup != null)
                if (globalTcp != Lookup.Network.TcpControl.ListenPort || globalUdp != Lookup.Network.UdpControl.ListenPort)
                    savePorts = true;

            
            if (savePorts)
            {
                Core.Network.ChangePorts(opTcp, opUdp);
                Lookup.Network.ChangePorts(globalTcp, globalUdp);
            }

            if (SaveCache)
            {
                List<WebCache> caches = new List<WebCache>();

                foreach (CacheItem item in CacheList.Items)
                    // if original same, save original - persists time outs
                    if (item.Cache.Equals(item.Original))
                        caches.Add(item.Original);
                    // else save edit
                    else
                        caches.Add(item.Cache);

                Core.Network.Cache.AddWebCache(caches);
            }

            Close();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void PortBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            bool valid = char.IsNumber(e.KeyChar) || e.KeyChar == '\b';

            e.Handled = !valid; 
        }

        private void UPnPLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            new UpnpSetup(Core).ShowDialog();
        }
    }

    public class CacheItem
    {
        public WebCache Original;
        public WebCache Cache;

        public CacheItem(WebCache cache)
        {
            Original = cache;

            Cache = new WebCache();

            Cache.Address = cache.Address;
            Cache.AccessKey = cache.AccessKey;
        }

        public override string ToString()
        {
            return Cache.Address;
        }
    }
}
