using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using DeOps.Services;
using DeOps.Services.Board;
using DeOps.Services.Mail;
using DeOps.Services.Profile;
using DeOps.Services.Storage;
using DeOps.Services.Transfer;
using DeOps.Services.Trust;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Implementation.Protocol.Net;
using DeOps.Implementation.Transport;

using DeOps.Interface;


namespace DeOps.Simulator
{
    public partial class NetView : CustomIconForm
    {
        SimForm Main;
        InternetSim Sim;
        ulong OpID;

        Bitmap DisplayBuffer;
        bool Redraw;
        bool ReInitBuffer;

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


        ulong SelectedID;
        bool  ShowInbound;

        Dictionary<ulong, Rectangle> NodeBoxes = new Dictionary<ulong, Rectangle>();
        Dictionary<ulong, Point> NodePoints = new Dictionary<ulong, Point>();
        List<Point> TrackPoints = new List<Point>();
        List<Point> TransferPoints = new List<Point>();
        Dictionary<ulong, BitArray> NodeBitfields = new Dictionary<ulong, BitArray>();

        G2Protocol Protocol = new G2Protocol();

        public string TrackString;
        public byte[] TrackHash;
        public ulong TrackHashID;

        ToolTip NodeTip = new ToolTip();

        LegendForm Legend = new LegendForm();


        public NetView(SimForm main, ulong id)
        {
            Main = main;
            Sim  = Main.Sim;
            OpID = id;

            Redraw = true;

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            Sim.UpdateView += new UpdateViewHandler(OnUpdateView);

            InitializeComponent();

            NodeTip.ShowAlways = true;
        }

        private void NetView_Load(object sender, EventArgs e)
        {
            string name = "Unknown";

            if (Sim.OpNames.ContainsKey(OpID))
                name = Sim.OpNames[OpID];
            else if (OpID == 0)
                name = "Lookup";

            Text = name + " Network";
        }
        
        private void NetView_FormClosing(object sender, FormClosingEventArgs e)
        {
            Sim.UpdateView -= new UpdateViewHandler(OnUpdateView);


            Main.NetViews.Remove(OpID);
        }

        public void OnUpdateView()
        {
            if (Thread.CurrentThread.ManagedThreadId != Main.UiThreadId)
            {
                BeginInvoke(Sim.UpdateView);
                return;
            }

            Redraw = true;
            Invalidate();
        }
        
        private void NetView_Paint(object sender, PaintEventArgs e)
        {

            int width = ClientRectangle.Width;
            int height = ClientRectangle.Height;

            if (width == 0 || height == 0)
                return;
            
            if (DisplayBuffer == null || ReInitBuffer)
            {
                DisplayBuffer = new Bitmap(width, height);
                ReInitBuffer = false;
            }

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
            Point centerPoint = new Point(width / 2, height / 2);

            int maxRadius = (height > width) ? width / 2 : height / 2;
            maxRadius -= 15;

            // get node points
            NodePoints.Clear();
            TrackPoints.Clear();
            TransferPoints.Clear();
            Dictionary<ulong, DhtNetwork> networks = new Dictionary<ulong, DhtNetwork>();

            Sim.Instances.SafeForEach(instance =>
            {
                if (OpID == 0)
                {
                    if (instance.Context.Lookup != null)
                        networks[instance.Context.Lookup.UserID] = instance.Context.Lookup.Network;
                }
                else
                    instance.Context.Cores.LockReading(delegate()
                    {
                        foreach (OpCore core in instance.Context.Cores)
                            if (OpID == core.Network.OpID)
                                networks[core.UserID] = core.Network;
                    });
            });


            NodeBitfields.Clear();

            foreach (DhtNetwork network in networks.Values)
            {
                ulong userID = network.Core.UserID;
                int nodeRadius = (network.Core.Firewall == FirewallType.Open) ? maxRadius - 30 : maxRadius;

                NodePoints[userID] = GetCircumPoint(centerPoint, nodeRadius, IDto32(userID));

                if (TrackHash != null)
                {
                    if (IsTracked(network.Core))
                        TrackPoints.Add(GetCircumPoint(centerPoint, nodeRadius + 7, IDto32(userID)));

             
                    foreach (OpTransfer transfer in network.Core.Transfers.Transfers.Values)
                        if (Utilities.MemCompare(transfer.Details.Hash, TrackHash))
                        {
                            TransferPoints.Add(GetCircumPoint(centerPoint, nodeRadius + 7, IDto32(userID)));

                            if(transfer.LocalBitfield != null)  
                                NodeBitfields[userID] = transfer.LocalBitfield;
                        }
                }
            }

            // draw lines for tcp between points
            foreach(DhtNetwork network in networks.Values)
                lock(network.TcpControl.SocketList)
                    foreach(TcpConnect connect in network.TcpControl.SocketList)
                        if(connect.State == TcpState.Connected && NodePoints.ContainsKey(connect.UserID))
                            buffer.DrawLine(BluePen, NodePoints[network.Local.UserID], NodePoints[connect.UserID]);

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

            int barwidth = 150;

            MissingBrush.Color = Color.White;
            int[] popularity = null;
            // popularity is nodes who have the transfer loaded (up/down)
            foreach(ulong user in NodeBitfields.Keys)
            {
                BitArray bitfield = NodeBitfields[user];
                Rectangle box = NodeBoxes[user];

                // determine popularity
                if (popularity == null)
                    popularity = new int[bitfield.Length];

                for (int i = 0; i < bitfield.Length; i++)
                    if (bitfield[i])
                        popularity[i]++;
                 
                // draw bar
                int x = (box.X < width / 2) ? box.X + box.Width + 1 : // right sie
                                              box.X - 1 - barwidth; // right side

                Rectangle bar = new Rectangle(x, box.Y, barwidth, box.Height);

                DrawBitfield(buffer, bar, bitfield);
            }

            // draw total completed bar in bottom right
            if (popularity != null)
            {
                int max = popularity.Max();

                Rectangle totalBox = new Rectangle(width - 300 - 5, height - 16 - 5, 300, 16);
                buffer.FillRectangle(CompletedBrush, totalBox);

                for (int i = 0; i < popularity.Length; i++)
                {
                    int brightness = 255 - popularity[i] * 255 / max; // high brighness -> white -> less people have file
                    MissingBrush.Color = Color.FromArgb(brightness, brightness, 255);
                    DrawPiece(buffer, popularity.Length, totalBox, i, i + 1);
                }

                buffer.DrawRectangle(BorderPen, totalBox);
            }

            // mark selected
            if (SelectedID != 0 && NodeBoxes.ContainsKey(SelectedID))
            {
                Rectangle selectBox = NodeBoxes[SelectedID];
                selectBox.Inflate(2,2);
                buffer.DrawEllipse(BlackPen, selectBox);

                string name = networks[SelectedID].Core.User.Settings.UserName;
                name += " " + Utilities.IDtoBin(networks[SelectedID].Local.UserID);
                name += ShowInbound ? " Inbound Traffic" : " Outbound Traffic";

                buffer.DrawString(name, TahomaFont, BlackBrush, new PointF(3, 37));
            }

            // write hits for global
            if(OpID == 0)
                buffer.DrawString(Sim.WebCacheHits + " WebCache Hits", TahomaFont, BlackBrush, new PointF(3, 25));

            if (TrackString != null)
                buffer.DrawString("Tracking: " + TrackString, TahomaFont, BlackBrush, 3, height - 20);
            
            // Copy buffer to display
            e.Graphics.DrawImage(DisplayBuffer, ClientRectangle.X, ClientRectangle.Y);

        }


        SolidBrush CompletedBrush = new SolidBrush(Color.CornflowerBlue);
        SolidBrush MissingBrush = new SolidBrush(Color.White);
        Pen BorderPen = new Pen(Color.Black);

        private void DrawBitfield(Graphics buffer, Rectangle bar, BitArray bitfield)
        {
            buffer.FillRectangle(CompletedBrush, bar);

            // cut out missing pieces, so worst case they are visible
            // opposed to other way where they'd be hidden

            int start = -1;

            for (int i = 0; i < bitfield.Length; i++)
                // has
                if (bitfield[i])
                {
                    if (start != -1)
                    {
                        DrawPiece(buffer, bitfield.Length, bar, start, i);
                        start = -1;
                    }
                }
                // missing
                else if (start == -1)
                    start = i;

            // draw last missing piece
            if (start != -1)
                DrawPiece(buffer, bitfield.Length, bar, start, bitfield.Length);

            buffer.DrawRectangle(BorderPen, bar);
        }


        private void DrawPiece(Graphics buffer, int bits, Rectangle box, float start, float end)
        {
            float scale = (float)box.Width / (float)bits;

            int x1 = (int)(start * scale);
            int x2 = (int)(end * scale);

            buffer.FillRectangle(MissingBrush, box.X + x1, box.Y, x2 - x1, box.Height);
        }

        private bool IsTracked(OpCore core)
        {
            bool found = false;

        
            // storage
            StorageService storage = core.GetService(ServiceIDs.Storage) as StorageService;
            
            if (!found && storage != null)
                if (storage.FileMap.SafeContainsKey(TrackHashID))
                    found = true;

            // link
            if(!found)
                core.Trust.TrustMap.LockReading(delegate()
                {
                    foreach (OpTrust trust in core.Trust.TrustMap.Values)
                        if (trust.Loaded && Utilities.MemCompare(trust.File.Header.FileHash, TrackHash))
                        {
                            found = true;
                            break;
                        }
                });

            // profile
            ProfileService profiles = core.GetService(ServiceIDs.Profile) as ProfileService;

            if (!found && profiles != null)
                profiles.ProfileMap.LockReading(delegate()
                {
                    foreach (OpProfile profile in profiles.ProfileMap.Values)
                        if (Utilities.MemCompare(profile.File.Header.FileHash, TrackHash))
                        {
                            found = true;
                            break;
                        }
                });

            // mail
            MailService mail = core.GetService(ServiceIDs.Mail) as MailService;

            if (!found && mail != null)
            {
                foreach (CachedPending pending in mail.PendingMap.Values)
                    if (Utilities.MemCompare(pending.Header.FileHash, TrackHash))
                        return true;

                foreach (List<CachedMail> list in mail.MailMap.Values)
                    foreach (CachedMail cached in list)
                        if (Utilities.MemCompare(cached.Header.FileHash, TrackHash))
                        {
                            found = true;
                            break;
                        }
            }

            // board
            BoardService boards = core.GetService(ServiceIDs.Board) as BoardService;

            if (!found && boards != null)
                boards.BoardMap.LockReading(delegate()
                {
                    foreach (OpBoard board in boards.BoardMap.Values)
                        if (!found)
                            board.Posts.LockReading(delegate()
                            {
                                foreach (OpPost post in board.Posts.Values)
                                    if (Utilities.MemCompare(post.Header.FileHash, TrackHash))
                                    {
                                        found = true;
                                        break;
                                    }
                            });
                });

            return found;
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
            if (Width > 0 && Height > 0)
            {
                ReInitBuffer = true;
                Redraw = true;
                Invalidate();
            }
        }

        uint IDto32(UInt64 id)
        {
            return (uint)(id >> 32);
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

            Dictionary<ulong, Dictionary<ulong, PacketGroup>> UdpTraffic = new Dictionary<ulong, Dictionary<ulong, PacketGroup>>();
            Dictionary<ulong, Dictionary<ulong, PacketGroup>> TcpTraffic = new Dictionary<ulong, Dictionary<ulong, PacketGroup>>();

            for (int i = 0; i < Sim.OutPackets.Length; i++)
                lock (Sim.OutPackets[i])
                    foreach (SimPacket packet in Sim.OutPackets[i])
                        if (SelectedID == 0 || (!ShowInbound && SelectedID == packet.SenderID) || (ShowInbound && SelectedID == packet.Dest.Local.UserID))
                            if ((packet.Dest.IsLookup && OpID == 0) || (!packet.Dest.IsLookup && packet.Dest.OpID == OpID))
                            {
                                Dictionary<ulong, Dictionary<ulong, PacketGroup>> TrafficGroup = packet.Tcp != null ? TcpTraffic : UdpTraffic;

                                if (!TrafficGroup.ContainsKey(packet.SenderID))
                                    TrafficGroup[packet.SenderID] = new Dictionary<ulong, PacketGroup>();

                                if (!TrafficGroup[packet.SenderID].ContainsKey(packet.Dest.Local.UserID))
                                    TrafficGroup[packet.SenderID][packet.Dest.Local.UserID] = new PacketGroup(packet.SenderID, packet.Dest.Local.UserID);

                                TrafficGroup[packet.SenderID][packet.Dest.Local.UserID].Add(packet);
                            }

            DrawGroup(buffer, UdpTraffic, false);
            DrawGroup(buffer, TcpTraffic, true);
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
                            G2Protocol.ReadPacket(root);

                            double controlLen = (root.InternalPos > 0) ? root.InternalPos - root.PacketPos : packet.Length;

                            // net packet
                            if (root.Name == RootPacket.Network)
                            {
                                TrafficPen.Color = Legend.PicNet.BackColor;
                                buffer.DrawLine(TrafficPen, group.GetPoint(pos), group.GetPoint(pos + controlLen));

                                NetworkPacket netPacket = NetworkPacket.Decode(root);
                                G2Header internalRoot = new G2Header(netPacket.InternalData);
                                G2Protocol.ReadPacket(internalRoot);

                                G2ReceivedPacket recvedPacket = new G2ReceivedPacket();
                                recvedPacket.Root = internalRoot;

                                // draw internal
                                TrafficPen.Color = Legend.PicUnk.BackColor;

                                if (internalRoot.Name == NetworkPacket.SearchRequest)
                                {
                                    SearchReq req = SearchReq.Decode(recvedPacket);

                                    int paramLen = req.Parameters == null ? 10 : req.Parameters.Length;

                                    TrafficPen.Color = Legend.PicSrchReq.BackColor;
                                    buffer.DrawLine(TrafficPen, group.GetPoint(pos + controlLen), group.GetPoint(pos + controlLen + internalRoot.PacketSize - paramLen));

                                    TrafficPen.Color = GetComponentColor(req.Service);
                                    buffer.DrawLine(TrafficPen, group.GetPoint(pos + controlLen + internalRoot.PacketSize - paramLen), group.GetPoint(pos + controlLen + internalRoot.PacketSize));
                                }

                                else if (internalRoot.Name == NetworkPacket.SearchAck)
                                {
                                    SearchAck ack = SearchAck.Decode(recvedPacket);

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
                                    StoreReq req = StoreReq.Decode(recvedPacket);

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

        private Color GetComponentColor(uint id)
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

                        Sim.Instances.SafeForEach(instance =>
                        {
                            if (instance.Context.Lookup != null && instance.Context.Lookup.UserID == id)
                                name = instance.Context.Lookup.LocalIP.ToString();

                            else
                                instance.Context.Cores.LockReading(delegate()
                                {
                                    foreach (OpCore core in instance.Context.Cores)
                                        if (core.UserID == id)
                                        {
                                            name = core.User.Settings.UserName;
                                            break;
                                        }
                                });
                        });

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

            form.Show();
        }

        private void TrackMenuItem_Click(object sender, EventArgs e)
        {
            TrackFile form = new TrackFile(this);

            form.Show();

        }
    }

    public class PacketGroup
    {
        public ulong SourceID;
        public ulong DestID;

        public Point SourcePoint;
        public Point DestPoint;

        public int TotalSize;
        public double LineSize;
        public List<byte[]> Packets = new List<byte[]>();

        // trig
        double Opp;
        double Adj;
        double Hyp;
        double Ang;


        public PacketGroup(ulong source, ulong dest)
        {
            SourceID = source;
            DestID = dest;
        }

        public void Add(SimPacket wrap)
        {
            if (wrap.Packet == null)
                return;

            TotalSize += wrap.Packet.Length;
            Packets.Add(wrap.Packet);
        }

        public void SetPoints(Point source, Point dest)
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

        public Point GetPoint(double pos)
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