using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface;
using DeOps.Implementation;

namespace DeOps.Services.Trust
{
    public partial class RemoveLinks : CustomIconForm
    {
        List<ulong> PersonIDs;

        public List<ulong> RemoveIDs = new List<ulong>();

        public RemoveLinks(OpCore core, List<ulong> ids)
            : base(core)
        {
            InitializeComponent();

            PersonIDs = ids;

            foreach (ulong id in PersonIDs)
                PeopleList.Items.Add(new RemoveItem(id, core.GetName(id)));
        }

        private void TheCancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            foreach (RemoveItem item in PeopleList.SelectedItems)
                RemoveIDs.Add(item.ID);

            DialogResult = DialogResult.OK;

            Close();
        }
    }

    public class RemoveItem
    {
        public ulong ID;
        public string Name;

        public RemoveItem(ulong id, string name)
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