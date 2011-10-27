using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Interface;
using DeOps.Interface.TLVex;
using DeOps.Services.Trust;


namespace DeOps.Interface
{
    internal partial class AddUsersDialog : CustomIconForm
    {
        CoreUI UI;
        OpCore Core;

        internal ulong Person;
        internal List<ulong> People = new List<ulong>();
        internal uint ProjectID;

        internal bool MultiSelect
        {
            set
            {
                BuddyList.MultiSelect = value;
                TrustTree.MultiSelect = value;
            }
        }


        internal AddUsersDialog(CoreUI ui, uint project)
            : base(ui.Core)
        {
            InitializeComponent();

            UI = ui;
            Core = ui.Core;
            ProjectID = project;
            
            // load up trust
            if(Core.Trust != null)
                LoadTrustTree();

            // load up buddies - last option
            BuddyList.FirstLineBlank = false;
            BuddyList.Init(ui, Core.Buddies, null, false);
            ProjectCombo.Items.Add(new AddProjectItem("Buddies"));


            if (Core.Trust != null)
            {
                foreach (AddProjectItem item in ProjectCombo.Items)
                    if (item.ID == ProjectID)
                    {
                        ProjectCombo.SelectedItem = item;
                        break; // break before buddies is selected
                    }
            }
            else
                ProjectCombo.SelectedIndex = 0; // buddy list
        }

        private void LoadTrustTree()
        {
            TrustService trust = Core.Trust;

            // add projects to combo
            trust.ProjectRoots.LockReading(delegate()
            {
                foreach (uint id in trust.ProjectRoots.Keys)
                {
                    string name = "";

                    if (id == 0)
                        name = "Main";
                    else
                        name = trust.GetProjectName(id);

                    ProjectCombo.Items.Add(new AddProjectItem(id, name));
                }
            });

            TrustTree.FirstLineBlank = false;
            TrustTree.Init(trust);

            TrustTree.MultiSelect = true;
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            AddProjectItem item = ProjectCombo.SelectedItem as AddProjectItem;

            if (item == null)
                return;

            People = item.TrustType ? TrustTree.GetSelectedIDs() : BuddyList.GetSelectedIDs();
            
            if (People.Count > 0)
                Person = People[0];

            DialogResult = DialogResult.OK;

            Close();
        }

        private void TheCancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ProjectCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            AddProjectItem item = ProjectCombo.SelectedItem as AddProjectItem;

            if (item == null)
                return;

            if (item.TrustType)
            {
                TrustTree.ShowProject(item.ID);
                ProjectID = item.ID;
            }

            TrustTree.Visible = item.TrustType;
            BuddyList.Visible = !item.TrustType;
        }

    }

    internal class AddProjectItem
    {
        internal string Name;
        internal uint ID;
        internal bool TrustType;


        // buddy list item
        internal AddProjectItem(string name)
        {
            Name = name;
            TrustType = false;
        }

        // trust main or project items
        internal AddProjectItem(uint id, string name)
        {
            Name = name;
            ID = id;
            TrustType = true;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}