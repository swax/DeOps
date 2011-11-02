using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Services;


namespace DeOps.Simulator
{
    public partial class SelectServices : Form
    {
        SimForm Sim;
        RunServiceMethod Method;


        public SelectServices(SimForm sim, string title, RunServiceMethod method)
        {
            InitializeComponent();

            Sim = sim;
            Text = title;
            Method = method;

            foreach (ListInstanceItem item in Sim.ListInstances.Items)
                if (item.Core != null)
                {
                    DeOpsContext sample = item.Core.Context;

                    foreach (uint id in sample.KnownServices.Keys)
                        ServiceList.Items.Add(new ServiceItem(id, sample.KnownServices[id]));

                    break;
                }
        }

        private void TestButton_Click(object sender, EventArgs e)
        {
            List<uint> selected = new List<uint>();

            foreach (ServiceItem item in ServiceList.SelectedItems)
                selected.Add(item.ID);


            if (selected.Count > 0)
                foreach (ListInstanceItem item in Sim.ListInstances.SelectedItems)
                    if (item.Core != null)
                        foreach (OpService service in item.Core.ServiceMap.Values)
                            if (selected.Contains(service.ServiceID))
                                Method.Invoke(service);
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }

    public class ServiceItem
    {
        public uint ID;
        public string Name;

        public ServiceItem(uint id, string name)
        {
            ID = id;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
