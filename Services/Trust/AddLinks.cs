using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Interface;
using RiseOp.Interface.TLVex;
using RiseOp.Services.Trust;

namespace RiseOp.Services.Trust
{
    internal partial class AddLinks : CustomIconForm
    {
        OpCore Core;
        TrustService Trust;

        internal ulong Person;
        internal List<ulong> People = new List<ulong>();
        internal uint ProjectID;

        internal bool MultiSelect;


        internal AddLinks(TrustService trust, uint project)
            : base(trust.Core)
        {
            InitializeComponent();

            Core = trust.Core;
            Trust = trust;
            ProjectID = project;
        }

        private void LinkChooser_Load(object sender, EventArgs e)
        {
            // add projects to combo
            Trust.ProjectRoots.LockReading(delegate()
            {
                foreach (uint id in Trust.ProjectRoots.Keys)
                {
                    string name = "";

                    if (id == 0)
                        name = "Main";
                    else
                        name = Trust.GetProjectName(id);

                    ProjectCombo.Items.Add(new AddProjectItem(id, name));
                }
            });

            PersonTree.FirstLineBlank = false;
            PersonTree.Init(Trust);

            foreach (AddProjectItem item in ProjectCombo.Items)
                if( item.ID == ProjectID)
                    ProjectCombo.SelectedItem = item;

            PersonTree.MultiSelect = MultiSelect;
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            People = PersonTree.GetSelectedIDs();

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

            PersonTree.ShowProject(item.ID);
            ProjectID = item.ID;
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