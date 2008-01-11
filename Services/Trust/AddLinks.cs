using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface.TLVex;
using DeOps.Services.Trust;


namespace DeOps.Services.Trust
{
    internal partial class AddLinks : Form
    {
        TrustService Links;
        uint ProjectID;

        internal List<ulong> People = new List<ulong>();


        internal AddLinks(TrustService links, uint project)
        {
            InitializeComponent();

            Links = links;
            ProjectID = project;
        }

        private void LinkChooser_Load(object sender, EventArgs e)
        {
            // add projects to combo
            Links.ProjectRoots.LockReading(delegate()
            {
                foreach (uint id in Links.ProjectRoots.Keys)
                {
                    string name = "";

                    if (id == 0)
                        name = "Main";
                    else
                        name = Links.GetProjectName(id);

                    ProjectCombo.Items.Add(new AddProjectItem(id, name));
                }
            });

            PersonTree.FirstLineBlank = false;
            PersonTree.Init(Links);

            foreach (AddProjectItem item in ProjectCombo.Items)
                if( item.ID == ProjectID)
                    ProjectCombo.SelectedItem = item;

            PersonTree.MultiSelect = true;
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            People = PersonTree.GetSelectedIDs();

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

            PersonTree.ShowProject(item.ID);
        }

    }

    internal class AddProjectItem
    {
        internal string Name;
        internal uint ID;

        internal AddProjectItem(uint id, string name)
        {
            Name = name;
            ID = id;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}