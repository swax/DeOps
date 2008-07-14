using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Services;


namespace RiseOp.Simulator
{
    internal partial class SelectServices : Form
    {
        

        SimForm Sim;
        RunServiceMethod Method;


        internal SelectServices(SimForm sim, string title, RunServiceMethod method)
        {
            InitializeComponent();

            Sim = sim;
            Text = title;
            Method = method;

            OpCore sample = null;

            foreach (ListInstanceItem item in Sim.ListInstances.Items)
                if (item.Core != null)
                {
                    sample = item.Core;
                    break;
                }

            if (sample != null)
                foreach (OpService service in sample.ServiceMap.Values)
                    ServiceList.Items.Add(new ServiceItem(service));
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


    internal class ServiceItem
    {
        internal uint ID;
        internal string Name;


        internal ServiceItem(OpService service)
        {
            ID = service.ServiceID;
            Name = service.Name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
