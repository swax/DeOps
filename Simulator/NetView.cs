using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

using RiseOp.Services;
using RiseOp.Services.Trust;
using RiseOp.Services.Profile;
using RiseOp.Services.Mail;
using RiseOp.Services.Board;
using RiseOp.Services.Transfer;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
using RiseOp.Implementation.Protocol;
using RiseOp.Implementation.Protocol.Net;
using RiseOp.Implementation.Transport;


namespace RiseOp.Simulator
{
    internal partial class NetView : Form
    {
        SimForm Main;
        InternetSim Sim;
        ulong OpID;

        Bitmap DisplayBuffer;
        bool Redraw;

        Pen BluePen  = new Pen(Color.LightBlue, 1);
        Pen TrafficPen  = new Pen(Color.WhiteSmoke, 1);
        Pen BlackPen = new Pen(Color.Black, 1); 

        SolidBrush GreenBrush = new SolidBrush(Color.Green);
        SolidBrush RedBrush    = new SolidBrush(Color.Red);
        SolidBrush OrangeBrush = new SolidBrush(Color.Orange);
        SolidBrush BlackBrush = new SolidBrush(Color.Black);
        SolidBrush BlueBrush = new SolidBrush(Color.Blue);
        SolidBrush PurpleBrush = new SolidBrush(Color.Purple);

        Font TahomaFont = new Font("Tahoma", 8);

        internal delegate void UpdateViewHandler();
        internal UpdateViewHandler UpdateView;

        ulong SelectedID;
        bool  ShowInbound;

        Dictionary<ulong, Rectangle> NodeBoxes = new Dictionary<ulong, Rectangle>();
        Dictionary<ulong, Point> NodePoints = new Dictionary<ulong, Point>();
        List<Point> TrackPoints = new List<Point>();
        List<Point> TransferPoints = new List<Point>();

        G2Protocol Protocol = new G2Protocol();

        internal byte[] TrackHash;

        ToolTip NodeTip = new ToolTip();

        LegendForm Legend = new LegendForm();


        internal NetView(SimForm main, ulong id)
        {
            Main = main;
            Sim  = Main.Sim;
            OpID = id;

            Redraw = true;

            UpdateView = new UpdateViewHandler(OnUpdateView);

            InitializeComponent();


            NodeTip.ShowAlways = true;
        }

        private void NetView_Load(object sender, EventArgs e)
        {
            string name = "Unknown";

            if (Sim.OpNames.ContainsKey(OpID))
                name = Sim.OpNames[OpID];
            else if (OpID == 0)
                name = "Global";

            Text = name + " Network";
        }
        
        private void NetView_FormClosing(object sender, FormClosingEventArgs e)
        {
            Main.NetViews.Remove(OpID);
        }

        internal void OnUpdateView()
        {
            Redraw = true;
            Invalidate();
        }

        private void NetView_Paint(object sender, PaintEventArgs e)
        {
            if (ClientSize.Width == 0 || ClientSize.Height == 0)
                return;

            if (DisplayBuffer == null)
                DisplayBuffer = new Bitmap(ClientSize.Width, ClientSize.Height);

            if (!Redraw)
            {
                e.Graphics.DrawImage(DisplayBuffer, 0, 0);
                return;
            }
            Redraw = false;

            // background
            Graphics buffer = Graphics.FromImage(DisplayBuffer);

            buffer.Clear(Color.White);
            buffer.SmoothingMode = SmoothingMode.AntiAlias;

            // calc radii
            Point centerPoint = new Point(ClientSize.Width / 2, ClientSize.Height / 2);

            int maxRadius = (ClientSize.Height > ClientSize.Width) ? ClientSize.Width / 2 : ClientSize.Height / 2;
            maxRadius -= 5;

            // get node points
            NodePoints.Clear();
            TrackPoints.Clear();
            TransferPoints.Clear();
            Dictionary<ulong, DhtNetwork> networks = new Dictionary<ulong, DhtNetwork>();

            foreach (SimInstance instance in Sim.Online)
                if (instance.Core != null &&
                    (instance.Core.OpID == OpID || (OpID == 0 && instance.Core.GlobalNet != null)))
                {
                    int nodeRadius = (instance.Core.Firewall == FirewallType.Open) ? maxRadius - 30 : maxRadius;

                    NodePoints[instance.Core.LocalDhtID] = GetCircumPoint(centerPoint, nodeRadius, IDto32(instance.Core.LocalDhtID));

                    networks[instance.Core.LocalDhtID] = (OpID == 0) ? instance.Core.GlobalNet : instance.Core.OperationNet;

                    if (TrackHash != null)
                    {
                        if (IsTracked(instance.Core))
                            TrackPoints.Add(GetCircumPoint(centerPoint, nodeRadius + 7, IDto32(instance.Core.LocalDhtID)));
                        else if (IsTransferring(instance.Core))
                            TransferPoints.Add(GetCircumPoint(centerPoint, nodeRadius + 7, IDto32(instance.Core.LocalDhtID)));
                    }
                }

            // draw lines for tcp between points
            foreach(DhtNetwork network in networks.Values)
                lock(network.TcpControl.Connections)
                    foreach(TcpConnect connect in network.TcpControl.Connections)
                        if(connect.State == TcpState.Connected && NodePoints.ContainsKey(connect.DhtID))
                            buffer.DrawLine(BluePen, NodePoints[network.Core.LocalDhtID], NodePoints[connect.DhtID]);

            // draw traffic lines
            DrawTraffic(buffer);

            // draw nodes
            NodeBoxes.Clear();

            foreach (ulong id in NodePoints.Keys)
            {
                SolidBrush brush = null;

                FirewallType firewall = networks[id].Core.Firewall;
                if(firewall == FirewallType.Open)
                    brush = GreenBrush;
                if(firewall == FirewallType.NAT)
                    brush = OrangeBrush;
                if(firewall == FirewallType.Blocked)
                    brush = RedBrush;

                NodeBoxes[id] = GetBoundingBox(NodePoints[id], 4);
                buffer.FillEllipse(brush, NodeBoxes[id]); 
            }

            // draw tracked
            foreach (Point point in TrackPoints)
                buffer.FillEllipse(BlueBrush, GetBoundingBox(point, 3));

            foreach (Point point in TransferPoints)
                buffer.FillEllipse(PurpleBrush, GetBoundingBox(point, 3));

            // mark selected
            if (SelectedID != 0 && NodeBoxes.ContainsKey(SelectedID))
            {
                Rectangle selectBox = NodeBoxes[SelectedID];
                selectBox.Inflate(2,2);
                buffer.DrawEllipse(BlackPen, selectBox);

                string name = networks[SelectedID].Core.User.Settings.ScreenName;
                name += " " + Utilities.IDtoBin(networks[SelectedID].Core.LocalDhtID);
                name += ShowInbound ? " Inbound Traffic" : " Outbound Traffic";

                buffer.DrawString(name, TahomaFont, BlackBrush, new PointF(3, 3));
            }

            // Copy buffer to display
            e.Graphics.DrawImage(DisplayBuffer, 0, 0);

        }

        private bool IsTracked(OpCore core)
        {
            bool found = false;

            BoardService boards = core.GetService("Board") as BoardService;
            MailService mail = core.GetService("Mail") as MailService;
            ProfileService profiles = core.GetService("Profile") as ProfileService;

            // link
            if(!found)
                core.Links.TrustMap.LockReading(delegate()
                {
                    foreach (OpTrust trust in core.Links.TrustMap.Values)
                        if (trust.Loaded && Utilities.MemCompare(trust.Header.FileHash, TrackHash))
                            found = true;
                });

            // profile
            if (!found && profiles != null)
                profiles.ProfileMap.LockReading(delegate()
                {
                    foreach (OpProfile profile in profiles.ProfileMap.Values)
                        if (Utilities.MemCompare(profile.Header.FileHash, TrackHash))
                            found = true ;
                });

            // mail
            if (mail != null)
            {
                foreach (CachedPending pending in mail.PendingMap.Values)
                    if (Utilities.MemCompare(pending.Header.FileHash, TrackHash))
                        return true;

                foreach (List<CachedMail> list in mail.MailMap.Values)
                    foreach (CachedMail cached in list)
                        if (Utilities.MemCompare(cached.Header.FileHash, TrackHash))
                            return true;
            }

            // board
            if (!found && boards != null)
                boards.BoardMap.LockReading(delegate()
                {
                    foreach (OpBoard board in boards.BoardMap.Values)
                        board.Posts.LockReading(delegate()
                        {
                            foreach (OpPost post in board.Posts.Values)
                                if (Utilities.MemCompare(post.Header.FileHash, TrackHash))
                                    found = true;
                        });
                });

            return found;
        }

        private bool IsTransferring(OpCore core)
        {
            foreach (FileDownload download in core.Transfers.DownloadMap.Values)
                if (Utilities.MemCompare(download.Details.Hash, TrackHash))
                    return true;

            return false;
        }

        Point GetCircumPoint(Point center, int rad, uint position)
        {
            double fraction = (double)position / (double)uint.MaxValue;

            int xPos = (int)((double)rad * Math.Cos(fraction * 2 * Math.PI)) + center.X;
            int yPos = (int)((double)rad * Math.Sin(fraction * 2 * Math.PI)) + center.Y;

            return new Point(xPos, yPos);
        }

        private void NetView_Resize(object sender, EventArgs e)
        {
            if (ClientSize.Width > 0 && ClientSize.Height > 0)
            {
                DisplayBuffer = new Bitmap(ClientSize.Width, ClientSize.Height);
                Redraw = true;
                Invalidate();
            }
        }

        // needs to be optimized, also used in graph function
        uint IDto32(UInt64 id)
        {
            uint retVal = 0;

            for (int i = 0; i < 32; i++)
                if (Utilities.GetBit(id, i) == 1)
                    retVal |= ((uint)1) << (31 - i);

            return retVal;
        }

        Rectangle GetBoundingBox(Point center, int rad)
        {
            return new Rectangle(center.X - rad, center.Y - rad, rad * 2, rad * 2);
        }

        private void NetView_MouseClick(object sender, MouseEventArgs e)
        {
            bool set = false;

            foreach(ulong id in NodeBoxes.Keys)
                if (NodeBoxes[id].Contains(e.Location))
                {
                    if (id == SelectedID)
                        ShowInbound = !ShowInbound;
                    else
                        SelectedID = id;

                    set = true;
                    break;
                }

            if (!set)
                SelectedID = 0;

            Redraw = true;
            Invalidate();
        }

        
        void DrawTraffic(Graphics buffer)
        {

            Dictionary<ulong, Dictionary<ulong, PacketGroup>> UdpTraffic = new Dictionary<ulong,Dictionary<ulong,PacketGroup>>();
            Dictionary<ulong, Dictionary<ulong, PacketGroup>> TcpTraffic = new Dictionary<ulong, Dictionary<ulong, PacketGroup>>();

            lock (Sim.OutPackets)
            {
                foreach (SimPacket packet in Sim.OutPackets)
                    if (SelectedID == 0 || (!ShowInbound && SelectedID == packet.SenderID) || (ShowInbound && SelectedID == packet.Dest.Core.LocalDhtID))
                        if ((packet.Dest.IsGlobal && OpID == 0) || (packet.Dest.Core.OpID == OpID))
                        {
                            Dictionary<ulong, Dictionary<ulong, PacketGroup>> TrafficGroup = packet.Tcp != null ? TcpTraffic : UdpTraffic;

                            if (!TrafficGroup.ContainsKey(packet.SenderID))
                                TrafficGroup[packet.SenderID] = new Dictionary<ulong, PacketGroup>();

                            if (!TrafficGroup[packet.SenderID].ContainsKey(packet.Dest.Core.LocalDhtID))
                                TrafficGroup[packet.SenderID][packet.Dest.Core.LocalDhtID] = new PacketGroup(packet.SenderID, packet.Dest.Core.LocalDhtID);

                            TrafficGroup[packet.SenderID][packet.Dest.Core.LocalDhtID].Add(packet);
                        }

                DrawGroup(buffer, UdpTraffic, false);
                DrawGroup(buffer, TcpTraffic, true);
            }
        }

        private void DrawGroup(Graphics buffer, Dictionary<ulong, Dictionary<ulong, PacketGroup>> TrafficGroup, bool tcp)
        {
            foreach (Dictionary<ulong, PacketGroup> destination in TrafficGroup.Values)
                foreach (PacketGroup group in destination.Values)
                {
                    if (!NodePoints.ContainsKey(group.SourceID) || !NodePoints.ContainsKey(group.DestID))
                        continue;

                    group.SetPoints(NodePoints[group.SourceID], NodePoints[group.DestID]);

                    TrafficPen.Width = 1;
                    group.LineSize = 200 + 20;

                    if (group.TotalSize > 200)
                    {
                        TrafficPen.Width = 2;
                        group.LineSize = 1000 + 100;
                    }

                    if (group.TotalSize > 1000)
                    {
                        TrafficPen.Width = 3;
                        group.LineSize = group.TotalSize + 500;
                    }

                    // calc break size
                    double breakSize = (group.LineSize - group.TotalSize) / (group.Packets.Count + 1);
                    double pos = breakSize;

                    Color bgColor = Color.WhiteSmoke;

                    //if (SelectedID != 0)
                    //    bgColor = group.SourceID == SelectedID ? Color.LightCoral : Color.LightBlue;
                    //else
                    //    bgColor = tcp ? Color.LightBlue : Color.WhiteSmoke;

                    TrafficPen.Color = bgColor;
                    buffer.DrawLine(TrafficPen, group.GetPoint(0), group.GetPoint(pos));

                    foreach (byte[] packet in group.Packets)
                    {

                        if (Sim.TestEncryption || Sim.TestTcpFullBuffer)
                        {
                            TrafficPen.Color = Legend.PicUnk.BackColor;
                            buffer.DrawLine(TrafficPen, group.GetPoint(pos), group.GetPoint(pos + packet.Length));
                        }

                        else
                        {
                            G2Header root = new G2Header(packet);
                            Protocol.ReadPacket(root);

                            double controlLen = (root.InternalPos > 0) ? root.InternalPos - root.PacketPos : packet.Length;

                            // net packet
                            if (root.Name == RootPacket.Network)
                            {
                                TrafficPen.Color = Legend.PicNet.BackColor;
                                buffer.DrawLine(TrafficPen, group.GetPoint(pos), group.GetPoint(pos + controlLen));

                                NetworkPacket netPacket = NetworkPacket.Decode(Protocol, root);
                                G2Header internalRoot = new G2Header(netPacket.InternalData);
                                Protocol.ReadPacket(internalRoot);

                                G2ReceivedPacket recvedPacket = new G2ReceivedPacket();
                                recvedPacket.Root = internalRoot;

                                // draw internal
                                TrafficPen.Color = Legend.PicUnk.BackColor;

                                if (internalRoot.Name == NetworkPacket.SearchRequest)
                                {
                                    SearchReq req = SearchReq.Decode(Protocol, recvedPacket);

                                    int paramLen = req.Parameters == null ? 10 : req.Parameters.Length;

                                    TrafficPen.Color = Legend.PicSrchReq.BackColor;
                                    buffer.DrawLine(TrafficPen, group.GetPoint(pos + controlLen), group.GetPoint(pos + controlLen + internalRoot.PacketSize - paramLen));

                                    TrafficPen.Color = GetComponentColor(req.Service);
                                    buffer.DrawLine(TrafficPen, group.GetPoint(pos + controlLen + internalRoot.PacketSize - paramLen), group.GetPoint(pos + controlLen + internalRoot.PacketSize));
                                }

                                else if (internalRoot.Name == NetworkPacket.SearchAck)
                                {
                                    SearchAck ack = SearchAck.Decode(Protocol, recvedPacket);

                                    int valLen = 10;

                                    if (ack.ValueList.Count > 0)
                                    {
                                        valLen = 0;
                                        foreach (byte[] val in ack.ValueList)
                                            valLen += val.Length;
                                    }

                                    TrafficPen.Color = Legend.PicSrchAck.BackColor;
                                    buffer.DrawLine(TrafficPen, group.GetPoint(pos + controlLen), group.GetPoint(pos + controlLen + internalRoot.PacketSize - valLen));

                                    TrafficPen.Color = GetComponentColor(ack.Service);
                                    buffer.DrawLine(TrafficPen, group.GetPoint(pos + controlLen + internalRoot.PacketSize - valLen), group.GetPoint(pos + controlLen + internalRoot.PacketSize));
                                }

                                else if (internalRoot.Name == NetworkPacket.StoreRequest)
                                {
                                    StoreReq req = StoreReq.Decode(Protocol, recvedPacket);

                                    int dataLen = req.Data == null ? 10 : req.Data.Length;

                                    TrafficPen.Color = Legend.PicStore.BackColor;
                                    buffer.DrawLine(TrafficPen, group.GetPoint(pos + controlLen), group.GetPoint(pos + controlLen + internalRoot.PacketSize - dataLen));

                                    TrafficPen.Color = GetComponentColor(req.Service);
                                    buffer.DrawLine(TrafficPen, group.GetPoint(pos + controlLen + internalRoot.PacketSize - dataLen), group.GetPoint(pos + controlLen + internalRoot.PacketSize));
                                }

                                else
                                {
                                    if (internalRoot.Name == NetworkPacket.Ping)
                                        TrafficPen.Color = Legend.PicPing.BackColor;

                                    else if (internalRoot.Name == NetworkPacket.Pong)
                                        TrafficPen.Color = Legend.PicPong.BackColor;

                                    else if (internalRoot.Name == NetworkPacket.ProxyRequest)
                                        TrafficPen.Color = Legend.PicPxyReq.BackColor;

                                    else if (internalRoot.Name == NetworkPacket.ProxyAck)
                                        TrafficPen.Color = Legend.PicPxyAck.BackColor;

                                    buffer.DrawLine(TrafficPen, group.GetPoint(pos + controlLen), group.GetPoint(pos + packet.Length));
                                }
                            }

                            // comm packet
                            if (root.Name == RootPacket.Comm)
                            {
                                TrafficPen.Color = Legend.PicComm.BackColor;
                                buffer.DrawLine(TrafficPen, group.GetPoint(pos), group.GetPoint(pos + controlLen));

                                TrafficPen.Color = Legend.PicUnk.BackColor;
                                buffer.DrawLine(TrafficPen, group.GetPoint(pos + controlLen), group.GetPoint(pos + packet.Length));
                            }
                        }

                        if (SelectedID != 0)
                            buffer.DrawString(group.TotalSize.ToString(), TahomaFont, BlackBrush, group.GetPoint(group.LineSize / 4));

                        pos += packet.Length;

                        TrafficPen.Color = bgColor; 
                        buffer.DrawLine(TrafficPen, group.GetPoint(pos), group.GetPoint(pos + breakSize));
                        pos += breakSize;
                    }
                }
        }

        private Color GetComponentColor(ushort id)
        {
            switch (id)
            {
                case 0://ServiceID.DHT:
                    return Legend.PicNode.BackColor;
                case 1://ServiceID.Trust:
                    return Legend.PicLink.BackColor;
                case 2://ServiceID.Location:
                    return Legend.PicLoc.BackColor;
                case 3://ServiceID.Transfer:
                    return Legend.PicTransfer.BackColor;
                case 4://ServiceID.Profile:
                    return Legend.PicProfile.BackColor;
                case 8://ServiceID.Board:
                    return Legend.PicBoard.BackColor;
                case 7: //ServiceID.Mail:
                    return Legend.PicMail.BackColor;
            }

            return Legend.PicUnk.BackColor;
        }

        private void FileMenu_Click(object sender, EventArgs e)
        {
            TrackFile form = new TrackFile(this);

            form.Show(this);
        }

        ulong CurrentTip;

        private void NetView_MouseMove(object sender, MouseEventArgs e)
        {
            Point client = PointToClient(Cursor.Position);
            
            foreach (ulong id in NodeBoxes.Keys)
                if (NodeBoxes[id].Contains(client))
                {
                    if(CurrentTip != id)
                    {
                        string name = "Unknown";

                        foreach (SimInstance instance in Sim.Online)
                            if (instance.Core != null && instance.Core.LocalDhtID == id)
                            {
                                name = instance.Core.User.Settings.ScreenName;
                                break;
                            }

                        NodeTip.Show(name, this, client.X, client.Y);

                        CurrentTip = id;
                    }

                    return;
                }

            CurrentTip = 0;
            NodeTip.Hide(this);
        }

        private void LegendMenu_Click(object sender, EventArgs e)
        {
            LegendForm form = new LegendForm();

            form.Show(this);
        }
    }

    internal class PacketGroup
    {
        internal ulong SourceID;
        internal ulong DestID;

        internal Point SourcePoint;
        internal Point DestPoint;

        internal int TotalSize;
        internal double LineSize;
        internal List<byte[]> Packets = new List<byte[]>();

        // trig
        double Opp;
        double Adj;
        double Hyp;
        double Ang;


        internal PacketGroup(ulong source, ulong dest)
        {
            SourceID = source;
            DestID = dest;
        }

        internal void Add(SimPacket wrap)
        {
            if (wrap.Packet == null)
                return;

            TotalSize += wrap.Packet.Length;
            Packets.Add(wrap.Packet);
        }

        internal void SetPoints(Point source, Point dest)
        {
            SourcePoint = source;
            DestPoint = dest;     
        
            // get width and height between two lines
            Opp = dest.Y - source.Y;
            Adj = dest.X - source.X;

            // figure length of hypotenuse
            Hyp = Math.Sqrt(Math.Pow(Adj, 2) + Math.Pow(Opp, 2));
            Ang = Math.Abs(Math.Asin(Opp / Hyp));
        }

        internal Point GetPoint(double pos)
        {
            double frac = pos / LineSize;

            double newHyp = Hyp * frac;
            double newOpp = newHyp * Math.Sin(Ang);
            double newAdj = newHyp * Math.Cos(Ang);

            if (Opp < 0)
                newOpp *= -1;
            if (Adj < 0)
                newAdj *= -1;

            return new Point(SourcePoint.X + (int)newAdj, SourcePoint.Y + (int)newOpp);
        }
    }
}