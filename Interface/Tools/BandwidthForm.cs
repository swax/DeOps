using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;
using ZedGraph;


namespace RiseOp.Interface.Tools
{
    internal partial class BandwidthForm : Form
    {
        List<OpCore> Cores;

        internal BandwidthForm(List<OpCore> cores)
        {
            InitializeComponent();

            Cores = cores;
        }

        internal static void Show(RiseOpContext context)
        {
            List<OpCore> cores = new List<OpCore>();

            cores.Add(context.Global);
            context.Cores.LockReading(delegate() { cores.AddRange(context.Cores); });

            new BandwidthForm(cores);
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
            BandwidthGraph.MasterPane.GraphObjList.Clear();

            double[] sums = new double[5];

            foreach (OpCore core in Cores)
                if (!core.Network.IsGlobal)
                {
                    foreach(int i in core.Network.UdpControl.BandwidthIn)
                        sum
                    

                    foreach(

                    PointPairList list = new PointPairList();

                    list[

                    for (int i = 0; i < 36; i++)
                    {
                        x = (double)i + 5;
                        y1 = 1.5 + Math.Sin((double)i * 0.2);
                        y2 = 3.0 * (1.5 + Math.Sin((double)i * 0.2));
                        list1.Add(x, y1);
                        list2.Add(x, y2);
                    }

                    BandwidthGraph.GraphPane.AddCurve("Core", list, Color.Red, SymbolType.Diamond);
                }

            BandwidthGraph.AxisChange();
        }

    }
}
