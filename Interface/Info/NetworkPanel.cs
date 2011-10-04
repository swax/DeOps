using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Implementation.Dht;
using DeOps.Implementation.Protocol;
using DeOps.Simulator;


namespace DeOps.Interface.Tools
{
    public partial class NetworkPanel : UserControl
    {
        OpCore Core;

        Bitmap DisplayBuffer;
        bool Redraw;
        bool ReInitBuffer;

        Brush BackgroundBrush = new SolidBrush(Color.FromArgb(16, Color.Black));

        Brush BlackBrush = new SolidBrush(Color.Black);
        Brush WhiteBrush = new SolidBrush(Color.White);
        Brush TopHeader = new LinearGradientBrush(new Point(0, 20), new Point(0, 0), Color.Transparent, Color.White);

        StringBuilder LineBuilder = new StringBuilder(1000);

        const int maxLineSize = 40;
        string defaultLine = new string('0', maxLineSize);

        Pen DisconnectedPen = new Pen(Color.Red, 2);
        Pen ConnectedPen = new Pen(Color.LimeGreen, 2);

        Brush NodeBrush = new SolidBrush(Color.Green);
        Pen NodePen = new Pen(Color.LimeGreen, 1);

        Pen TrafficPen = new Pen(Color.FromArgb(16, Color.Black)); // background color
        Pen PacketPen = new Pen(Color.Thistle, 1);

        Font PacketLogFont = new Font(FontFamily.GenericMonospace, 6);

        Brush InBrush = new SolidBrush(Color.LightBlue);
        Brush OutBrush = new SolidBrush(Color.LightCoral);

        Pen BlockedPen = new Pen(Color.Red, 1);
        Pen NATPen = new Pen(Color.Yellow, 1);
        Pen OpenPen = new Pen(Color.Green, 1);


        string StatusLabel = "";
        Font StatusFont = new Font(FontFamily.GenericSansSerif, 16, FontStyle.Bold);
        Brush ConnectingBrush = new SolidBrush(Color.Yellow);
        Brush ConnectedBrush = new SolidBrush(Color.LimeGreen);

        Font GeneralLogFont = new Font(FontFamily.GenericMonospace, 8);
        Brush GenLookupBrush = new SolidBrush(Color.Maroon);
        Brush GenOpBrush = new SolidBrush(Color.Black);


        public NetworkPanel()
        {
            InitializeComponent();

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        internal void Init(OpCore core)
        {
            Core = core;
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            Redraw = true;
            Invalidate();

        }

        private void NetworkPanel_Paint(object sender, PaintEventArgs e)
        {
            if (Width == 0 || Height == 0 || Core == null)
                return;

            if (DisplayBuffer == null || ReInitBuffer)
            {
                DisplayBuffer = new Bitmap(Width, Height);
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

            bool drawLookup = (Core.Context.Lookup != null && 
                               (Core.User.Settings.OpAccess != AccessType.Secret || Core.User.Settings.GlobalIM));

            Point center = new Point(Width / 2, Height / 2);
            int radius = (Height > Width) ? Width / 2 : Height / 2;

            // draw packet log
            List<InfoPacket> packets = new List<InfoPacket>();

            if (drawLookup)
                GetPackets(Core.Context.Lookup.Network, packets);

            GetPackets(Core.Network, packets);

            DrawPackets(buffer, packets);

            // draw net shadow
            int outterRad = radius * 3 / 4;
            buffer.FillEllipse(BackgroundBrush, GetBoundingBox(center, outterRad));

            // draw network rings and packets
            int innerRad = 0;

            if (drawLookup)
            {
                int lookupRad = radius / 3;
                DrawNetwork(buffer, Core.Context.Lookup.Network, center, lookupRad, innerRad);
                innerRad += lookupRad;
            }

            DrawNetwork(buffer, Core.Network, center, outterRad, innerRad);

            // draw general log
            List<GeneralLogItem> log = new List<GeneralLogItem>();

            if (drawLookup)
                lock (Core.Context.Lookup.Network.GeneralLog)
                    log.AddRange(Core.Context.Lookup.Network.GeneralLog.Select(i =>
                        new GeneralLogItem() { Entry = i.Param2, Time = i.Param1, Lookup = true }));

            lock (Core.Network.GeneralLog)
                log.AddRange(Core.Network.GeneralLog.Select(i =>
                        new GeneralLogItem() { Entry = i.Param2, Time = i.Param1, Lookup = false }));

            DrawGeneralLog(buffer, log);

            // draw connection status / labels
            Brush statusBrush = ConnectingBrush;

            if (Core.Network.Responsive)
            {
                StatusLabel = "Connected";
                statusBrush = ConnectedBrush;
            }
            else if (!drawLookup)
            {
                StatusLabel = "Connecting";
            }
            else if (Core.Context.Lookup.Network.Responsive)
            {
                StatusLabel = "Trying to Locate\r\n" + Core.User.Settings.Operation;
            }
            else
            {
                StatusLabel = "Connecting";
            }

            SizeF size = buffer.MeasureString(StatusLabel, StatusFont);
            int x = 25;// (int)(Width / 2 - size.Width / 2);
            int y = 25;
            buffer.FillRectangle(BlackBrush, x - 5, y - 5, size.Width + 10, size.Height + 10);
            buffer.DrawString(StatusLabel, StatusFont, statusBrush, x, y);

            // Copy buffer to display
            e.Graphics.DrawImage(DisplayBuffer, 0, 0);
        }

        private void DrawGeneralLog(Graphics buffer, List<GeneralLogItem> log)
        {
            // brown lookup, black op
            SizeF size = buffer.MeasureString("Test", GeneralLogFont);

            int line = 1;
            int areaHeight = Height / 2 - 16;
            int lineHeight = (int)(size.Height + 2);
            int lines = log.Count * lineHeight > areaHeight ? areaHeight / lineHeight : log.Count;

            log = log.OrderByDescending(l => l.Time).Take(lines).ToList(); // new to old
            
            foreach (GeneralLogItem item in log)
            {
                int y = Height - 8 - line * lineHeight; // start bottom of window

                Brush logBrush = item.Lookup ? GenLookupBrush : GenOpBrush;

                buffer.DrawString(item.Entry, GeneralLogFont, logBrush, 8, y);

                line++;
            }
        }

        private void GetPackets(DhtNetwork network, List<InfoPacket> log)
        {
            lock (network.LoggedPackets)
                foreach (PacketLogEntry packet in network.LoggedPackets)
                    log.Add(new InfoPacket() { Time = packet.Time, Direction = packet.Direction, Data = packet.Data, Lookup = network.IsLookup});
        }

        private void DrawPackets(Graphics buffer, List<InfoPacket> log)
        {
            SizeF size = buffer.MeasureString(defaultLine, PacketLogFont);

            int areaHeight = Height / 2 - 8;
            int lineHeight = (int) (size.Height + 2);

            int lines = log.Count * lineHeight > areaHeight ? areaHeight / lineHeight : log.Count;

            // sort new to old, take number of lines
            log = log.OrderByDescending(p => p.Time).Take(lines).ToList();

            int line = 0;
             
            foreach (InfoPacket entry in log)
            {
                int y = areaHeight - line * lineHeight; // start midway through window

                LineBuilder.Length = 0;

                int chars = entry.Data.Length % (maxLineSize - 10);

                for (int i = 0; i < chars/2; i++)
                    LineBuilder.Append(String.Format("{0:x2}", entry.Data[i]));

                LineBuilder.Append(":" + entry.Data.Length);
                LineBuilder.Append(entry.Direction == DirectionType.In ? "->" : "<-");
                LineBuilder.Append(entry.Lookup ? "LU" : "OP");

                int extra = maxLineSize - LineBuilder.Length;

                string final = new string(' ', extra) + LineBuilder.ToString();
                int x = (int)(Width - 8 - size.Width);

                Brush packetBrush = entry.Direction == DirectionType.In ? InBrush : OutBrush;
                buffer.DrawString(final, PacketLogFont, packetBrush, x, y);

                line++;
            }
        }

        private void DrawNetwork(Graphics buffer, DhtNetwork network, Point center, int radius, int innerRad)
        {
            // draw dhts
            float sweepAngle = 360;
            Pen orangePen = new Pen(Color.Orange, 1);
            int arcs = 0;

            int maxLevels = 5;
            uint localID = (uint)(network.Local.UserID >> 32);

            int drawBuckets = network.Routing.BucketList.Count - 1;  // -1 last is drawn by previous

            if (network.Routing.BucketList.Count > maxLevels)
                drawBuckets = maxLevels;

            for (int i = 0; i < drawBuckets; i++, sweepAngle /= 2)
            {
                if (sweepAngle < 0.1)
                    break;

                int outterRad = innerRad + ((radius - innerRad) * i / drawBuckets);

                uint lowpos = localID >> (32 - i);
                lowpos = lowpos << (32 - i);
                uint highpos = lowpos | ((uint)1 << 31 - i);

                float startAngle = 360 * ((float)lowpos / (float)uint.MaxValue);

                if (i != 0)
                {
                    arcs++;
                    buffer.DrawArc(orangePen, GetBoundingBox(center, outterRad), startAngle, sweepAngle);

                    buffer.DrawLine(orangePen, GetCircumPoint(center, outterRad, lowpos), GetCircumPoint(center, radius, lowpos));
                    buffer.DrawLine(orangePen, GetCircumPoint(center, outterRad, highpos), GetCircumPoint(center, radius, highpos));
                }
                else
                {
                    buffer.DrawLine(orangePen, GetCircumPoint(center, innerRad, 0), GetCircumPoint(center, radius, 0));
                    buffer.DrawLine(orangePen, GetCircumPoint(center, innerRad, uint.MaxValue / 2), GetCircumPoint(center, radius, uint.MaxValue / 2));
                }
            }


            // load packets to draw
            Dictionary<ulong, PacketGroup> targets = new Dictionary<ulong, PacketGroup>();


            lock (network.LoggedPackets)
                foreach (PacketLogEntry packet in network.LoggedPackets)
                {
                    if (packet.Address == null)
                        continue;

                    if (packet.Time < Core.TimeNow.AddSeconds(-1))
                        continue;

                    PacketGroup group;
                    if (!targets.TryGetValue(packet.Address.UserID, out group))
                    {
                        group = new PacketGroup(network.Local.UserID, packet.Address.UserID);
                        targets[packet.Address.UserID] = group;
                    }

                    group.TotalSize += packet.Data.Length;
                    group.Packets.Add(packet.Data);
                }

            
         
            // draw packets, each group is a different target
            foreach (PacketGroup group in targets.Values)
            {
                group.SetPoints(GetCircumPoint(center, radius, group.SourceID ), GetCircumPoint(center, radius, group.DestID ));

                TrafficPen.Width = PacketPen.Width = 1;
                group.LineSize = 200 + 20;

                if (group.TotalSize > 200)
                {
                    TrafficPen.Width = PacketPen.Width = 2;
                    group.LineSize = 1000 + 100;
                }

                if (group.TotalSize > 1000)
                {
                    TrafficPen.Width = PacketPen.Width = 3;
                    group.LineSize = group.TotalSize + 500;
                }

                // calc break size
                double breakSize = (group.LineSize - group.TotalSize) / (group.Packets.Count + 1);
                double pos = breakSize;

       
                buffer.DrawLine(TrafficPen, group.GetPoint(0), group.GetPoint(pos));

                foreach (byte[] packet in group.Packets)
                {
                    buffer.DrawLine(PacketPen, group.GetPoint(pos), group.GetPoint(pos + packet.Length));
                    pos += packet.Length;

                    buffer.DrawLine(TrafficPen, group.GetPoint(pos), group.GetPoint(pos + breakSize));
                    pos += breakSize;
                }
            }

            // draw network ring
            Rectangle box = GetBoundingBox(center, radius);

            Pen ringPen = network.Responsive ? ConnectedPen : DisconnectedPen;
            buffer.DrawEllipse(ringPen, box);

            // get nodes
            List<ulong> peers = new List<ulong>();
            peers.Add(network.Local.UserID);

            lock (network.Routing.BucketList)
                foreach (DhtBucket bucket in network.Routing.BucketList)
                    foreach (DhtContact contact in bucket.ContactList)
                        peers.Add(contact.UserID);

            // draw nodes
            foreach (ulong id in peers)
            {
                box = GetBoundingBox(GetCircumPoint(center, radius, id), 3);
                buffer.FillEllipse(NodeBrush, box);
            }

            // draw ring around self
            box = GetBoundingBox(GetCircumPoint(center, radius, network.Local.UserID), 5);
            buffer.DrawEllipse(GetSelfPen(network.Core.Firewall), box);
        }

        private Pen GetSelfPen(FirewallType firewall)
        {
            if (firewall == FirewallType.Open)
                return OpenPen;

            else if (firewall == FirewallType.NAT)
                return NATPen;

            return BlockedPen;
        }

        private void NetworkPanel_Resize(object sender, EventArgs e)
        {
            if (Width > 0 && Height > 0)
            {
                ReInitBuffer = true;
                Redraw = true;
                Invalidate();
            }
        }

        Rectangle GetBoundingBox(Point center, int rad)
        {
            return new Rectangle(center.X - rad, center.Y - rad, rad * 2, rad * 2);
        }

        uint IDto32(ulong id)
        {
            return  (uint)(id >> 32);
        }

        Point GetCircumPoint(Point center, int rad, ulong id)
        {
            return GetCircumPoint(center, rad, IDto32(id));
        }

        Point GetCircumPoint(Point center, int rad, uint id)
        {
            double fraction = (double)id / (double)uint.MaxValue;

            int xPos = (int)((double)rad * Math.Cos(fraction * 2 * Math.PI)) + center.X;
            int yPos = (int)((double)rad * Math.Sin(fraction * 2 * Math.PI)) + center.Y;

            return new Point(xPos, yPos);
        }
    }

    internal class GeneralLogItem
    {
        internal string Entry;
        internal DateTime Time;
        internal bool Lookup;
    }

    internal class InfoPacket
    {
        internal DateTime Time;
        internal DirectionType Direction;
        internal byte[] Data;
        internal bool Lookup;
    }
}
