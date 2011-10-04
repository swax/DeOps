using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Services.Buddy;

namespace DeOps.Interface.Settings
{
    internal partial class IgnoreForm : CustomIconForm
    {
        OpCore Core;

        List<ulong> InList = new List<ulong>();


        internal IgnoreForm(OpCore core)
        {
            InitializeComponent();

            Core = core;

            Core.Buddies.IgnoreList.LockReading(() =>
            {
                foreach (OpBuddy ignore in Core.Buddies.IgnoreList.Values)
                {
                    IgnoreList.Items.Add(new IgnoreItem(Core, ignore.ID));
                    InList.Add(ignore.ID);
                }
            });
        }

        private void AddLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            AddUsersDialog add = new AddUsersDialog(Core, 0);

            if(add.ShowDialog() == DialogResult.OK)
                foreach(ulong person in add.People)
                    if(person != Core.UserID && !InList.Contains(person))
                    {
                        IgnoreList.Items.Add(new IgnoreItem(Core, person));
                        InList.Add(person);
                    }
        }

        private void RemoveLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            foreach (IgnoreItem item in IgnoreList.SelectedItems.OfType<IgnoreItem>().ToArray())
            {
                IgnoreList.Items.Remove(item);
                InList.Remove(item.ID);
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            Core.Buddies.IgnoreList.SafeClear();

            foreach (IgnoreItem item in IgnoreList.Items)
                Core.Buddies.Ignore(item.ID, true);

            Close();
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }

    internal class IgnoreItem
    {
        internal string Name;
        internal ulong ID;

        internal IgnoreItem(OpCore core, ulong id)
        {
            Name = core.GetName(id);
            ID = id;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
