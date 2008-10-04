using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Implementation.Transport;

using ZedGraph;


namespace RiseOp.Interface.Tools
{
    internal partial class BandwidthForm : RiseOp.Interface.CustomIconForm
    {
        List<OpCore> Cores;

        Dictionary<RiseOpContext, int> ContextIndex = new Dictionary<RiseOpContext, int>();
        int ContextCount = 1;

        int RecordSeconds = 5;
        int AverageSeconds = 5;

        Dictionary<uint, ServiceItem> ServiceItemMap = new Dictionary<uint, ServiceItem>();


        internal static void Show(RiseOpContext context)
        {
            List<OpCore> cores = new List<OpCore>();

            if(context.Lookup != null)
                cores.Add(context.Lookup);
            
            context.Cores.LockReading(delegate() { cores.AddRange(context.Cores); });

            new BandwidthForm(cores).Show();
        }

        internal BandwidthForm(List<OpCore> cores)
        {
            InitializeComponent();

            Cores = cores;

            BandwidthGraph.GraphPane.Title.Text = "Bandwidth";
            BandwidthGraph.GraphPane.XAxis.Title.IsVisible = false;
            BandwidthGraph.GraphPane.YAxis.Title.IsVisible = false;
            BandwidthGraph.GraphPane.XAxis.Title.Text = "Seconds";
            BandwidthGraph.GraphPane.YAxis.Title.Text = "Bytes";
            BandwidthGraph.GraphPane.Legend.IsVisible = false;

            if (Cores.Count > 0)
            {
                RiseOpContext sample = Cores[0].Context;
                RecordSeconds = Cores[0].RecordBandwidthSeconds;

                foreach (uint id in sample.KnownServices.Keys)
                {
                    ServiceItem item = new ServiceItem(id, sample.KnownServices[id]);
                    ServiceList.Items.Add(item);

                    ServiceItemMap[id] = item;
                }
            }

            RecordLink.Text = "Record " + RecordSeconds + " seconds";
            AverageLink.Text = "Average " + AverageSeconds + " seconds";

            RefreshCoreList();
        }

        private void FilterGlobal_CheckedChanged(object sender, EventArgs e)
        {
            RefreshCoreList();
        }

        private void RefreshCoreList()
        {
            ContextCount = 1;
            ContextIndex.Clear();
            CoresList.Items.Clear();

            foreach (OpCore core in Cores)
            {
                if (core.Network.IsLookup && FilterGlobal.Checked)
                    continue;

                if (!ContextIndex.ContainsKey(core.Context))
                    ContextIndex[core.Context] = ContextCount++;

                int index = ContextIndex[core.Context];

                CoresList.Items.Add(new CoreItem(core, index));
            }
        }

        private void BandwidthForm_Load(object sender, EventArgs e)
        {
            RefreshGraph();
        }

        private void SecondTimer_Tick(object sender, EventArgs e)
        {
            RefreshGraph();
        }

        private void RefreshGraph()
        {
            BandwidthGraph.GraphPane.CurveList.Clear();

            if (Cores.Count == 0)
                return;

            // get all core averages
            float netAvg = 0;

            foreach (CoreItem item in CoresList.Items)
            {
                float coreAvg = 0;

                coreAvg += item.Core.Network.UdpControl.Bandwidth.InOutAvg(AverageSeconds);

                foreach (TcpConnect tcp in item.Core.Network.TcpControl.SocketList)
                    coreAvg += tcp.Bandwidth.InOutAvg(AverageSeconds);

                item.SubItems[1].Text = AvgFormat(coreAvg);

                netAvg += coreAvg;
            }

            CoresLabel.Text = "Cores: " + AvgFormat(netAvg);


            // get selected core averages
            double[] inBytes = new double[RecordSeconds];
            double[] outBytes = new double[RecordSeconds];


            float tcpAvg = 0;
            float udpAvg = 0;
            float rudpAvg = 0;

            foreach (ServiceItem service in ServiceList.Items)
                service.BandwidthAverage = 0;

            foreach (CoreItem item in CoresList.SelectedItems)
            {
                if (item.Core.RecordBandwidthSeconds != RecordSeconds)
                {
                    item.Core.ResizeBandwidthRecord(RecordSeconds);
                    continue;
                }

                // averages summed
                udpAvg += item.Core.Network.UdpControl.Bandwidth.InOutAvg(AverageSeconds);

                foreach (TcpConnect tcp in item.Core.Network.TcpControl.SocketList)
                    tcpAvg += tcp.Bandwidth.InOutAvg(AverageSeconds);

                foreach (RudpSession session in item.Core.Network.RudpControl.SessionMap.Values)
                    rudpAvg += session.Comm.Bandwidth.InOutAvg(AverageSeconds);

                foreach (ServiceItem service in ServiceList.Items)
                    if (item.Core.ServiceBandwidth.ContainsKey(service.ID))
                        service.BandwidthAverage += item.Core.ServiceBandwidth[service.ID].InOutAvg(AverageSeconds);

                // graphs
                if (TransportRadio.Checked)
                {
                    if (UdpBox.Checked)
                        AddtoArrays(inBytes, outBytes, item.Core.Network.UdpControl.Bandwidth);

                    if (TcpBox.Checked)
                        foreach (TcpConnect tcp in item.Core.Network.TcpControl.SocketList)
                            AddtoArrays(inBytes, outBytes, tcp.Bandwidth);
                }

                else if (RudpRadio.Checked)
                {
                    foreach (RudpSession session in item.Core.Network.RudpControl.SessionMap.Values)
                        AddtoArrays(inBytes, outBytes, session.Comm.Bandwidth);
                }

                else if (ServiceRadio.Checked)
                {
                    foreach (ServiceItem selected in ServiceList.SelectedItems)
                        if (item.Core.ServiceBandwidth.ContainsKey(selected.ID))
                            AddtoArrays(inBytes, outBytes, item.Core.ServiceBandwidth[selected.ID]);
                }

            }

            float transAvg = tcpAvg + udpAvg;

            TransportRadio.Text = "Transport: " + AvgFormat(transAvg);
            TcpBox.Text = "TCP: " + AvgFormat(tcpAvg);
            UdpBox.Text = "UDP: " + AvgFormat(udpAvg);
            RudpRadio.Text = "RUDP: " + AvgFormat(rudpAvg);

            float serviceAvg = 0;
            foreach (ServiceItem service in ServiceList.Items)
            {
                service.SubItems[1].Text = AvgFormat(service.BandwidthAverage);
                serviceAvg += service.BandwidthAverage;
            }

            ServiceRadio.Text = "Services: " + AvgFormat(serviceAvg);


            // title
            string title = "Bandwidth of " + CoresList.SelectedItems.Count + " Cores: ";

            if (TransportRadio.Checked)
            {
                if (TcpBox.Checked)
                    title += "TCP ";
                if (UdpBox.Checked)
                    title += "UDP ";
            }

            if (RudpRadio.Checked)
                title += "RUDP ";

            if (ServiceRadio.Checked)
                title += "Selected Services";

            BandwidthGraph.GraphPane.Title.Text = title;

            // older old to new
            Array.Reverse(inBytes);
            Array.Reverse(outBytes);

            double[] xAxis = new double[RecordSeconds];
            for (int i = 0; i < RecordSeconds; i++)
                xAxis[i] = -1 * i;
            Array.Reverse(xAxis);

            PointPairList inPoints = new PointPairList(xAxis, inBytes);
            PointPairList outPoints = new PointPairList(xAxis, outBytes);


            BandwidthGraph.GraphPane.AddCurve("In", inPoints, Color.Blue, SymbolType.None);
            BandwidthGraph.GraphPane.AddCurve("Out", outPoints, Color.Red, SymbolType.None);

            BandwidthGraph.GraphPane.XAxis.Scale.Min = 1 - RecordSeconds;
            BandwidthGraph.GraphPane.XAxis.Scale.Max = 0;

            BandwidthGraph.GraphPane.YAxis.Scale.Min = 0;
            BandwidthGraph.GraphPane.YAxis.Scale.Max = Math.Max(GetMaxAxis(inBytes), GetMaxAxis(outBytes));
            

            BandwidthGraph.AxisChange();
            BandwidthGraph.Invalidate();
        }

        private string AvgFormat(float avg)
        {
            return Utilities.CommaIze(avg.ToString("0"));
        }

        private double GetMaxAxis(double[] array)
        {
            double largest = 0;

            foreach (double value in array)
                if (value > largest)
                    largest = value;

            int max = 1024;

            while (largest > max && max >= 1024) // prevent looping
                max = max * 2;

            return max;
        }

        private void AddtoArrays(double[] inBytes, double[] outBytes, BandwidthLog bandwidth)
        {
            for (int i = 0; i < bandwidth.In.Length; i++)
                inBytes[i] += bandwidth.In[i];

            for (int i = 0; i < bandwidth.Out.Length; i++)
                outBytes[i] += bandwidth.Out[i];
        }

        private void TcpBox_CheckedChanged(object sender, EventArgs e)
        {
            RefreshGraph();
        }

        private void UdpBox_CheckedChanged(object sender, EventArgs e)
        {
            RefreshGraph();
        }

        private void TransportRadio_CheckedChanged(object sender, EventArgs e)
        {
            RefreshGraph();
        }

        private void RudpRadio_CheckedChanged(object sender, EventArgs e)
        {
            RefreshGraph();
        }

        private void ServiceRadio_CheckedChanged(object sender, EventArgs e)
        {
            RefreshGraph();
        }

        private void ServiceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshGraph();
        }

        private void RecordLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            GetTextDialog getText = new GetTextDialog("Record Seconds", "Enter the number of seconds to record for", RecordSeconds.ToString());

            if (getText.ShowDialog() != DialogResult.OK)
                return;

            int seconds = 0;
            int.TryParse(getText.ResultBox.Text, out seconds);

            if (seconds < 5)
                return;

            RecordSeconds = seconds;

            foreach (OpCore core in Cores)
                core.ResizeBandwidthRecord(seconds);

            RecordLink.Text = "Record " + seconds + " seconds";
        }

        private void AverageLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            GetTextDialog getText = new GetTextDialog("Average Seconds", "Enter the number of seconds to average over", AverageSeconds.ToString());

            if (getText.ShowDialog() != DialogResult.OK)
                return;

            int seconds = 0;
            int.TryParse(getText.ResultBox.Text, out seconds);

            if (seconds < 5)
                return;

            AverageSeconds = seconds;

            AverageLink.Text = "Average " + seconds + " seconds";
        }

        private void CoresList_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshGraph();
        }
    }

    internal class CoreItem : ListViewItem
    {
        internal OpCore Core;


        internal CoreItem(OpCore core, int index)
        {
            Core = core;

            SubItems.Add("0");

            string text = index + ". ";

            if (Core.Network.IsLookup)
                text += "Lookup";
            else
                text += Core.User.Settings.Operation + " - " + Core.User.Settings.UserName;

            Text = text;
        }
    }

    internal class ServiceItem : ListViewItem
    {
        internal uint ID;
        internal float BandwidthAverage;

        internal ServiceItem(uint id, string name) : base(name)
        {
            ID = id;
            Name = name;

            SubItems.Add("0");
        }
    }
}
