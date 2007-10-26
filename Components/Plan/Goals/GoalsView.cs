using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface;
using DeOps.Implementation;
using DeOps.Components.Link;
using DeOps.Interface.TLVex;
using DeOps.Interface.Views;


namespace DeOps.Components.Plan
{
    internal partial class GoalsView : ViewShell
    {
        internal OpCore Core;
        internal PlanControl Plans;
        internal LinkControl Links;

        internal ulong DhtID;
        internal uint ProjectID;

        List<int> SpecialList = new List<int>();

        List<PlanGoal> RootList = new List<PlanGoal>();
        List<PlanGoal> ArchiveList = new List<PlanGoal>();

        internal int LoadIdent;
        internal int LoadBranch;


        internal GoalsView(PlanControl plans, ulong id, uint project)
        {
            InitializeComponent();

            Plans = plans;
            Core = Plans.Core;
            Links = Core.Links;

            DhtID = id;
            ProjectID = project;

            toolStrip1.Renderer = new ToolStripProfessionalRenderer(new OpusColorTable());
        }

        internal override string GetTitle(bool small)
        {
            if (small)
                return "Goals";

            string title = "";

            if (DhtID == Core.LocalDhtID)
                title += "My ";
            else
                title += Links.GetName(DhtID) + "'s ";

            if (ProjectID != 0)
                title += Links.ProjectNames[ProjectID] + " ";

            title += "Goals";

            return title;
        }

        internal override Size GetDefaultSize()
        {
            return new Size(475, 325);
        }

        internal override Icon GetIcon()
        {
            return PlanRes.Goals;
        }

        internal override void Init()
        {
            Links.GuiUpdate += new LinkGuiUpdateHandler(Links_Update);
            Plans.PlanUpdate += new PlanUpdateHandler(Plans_Update);

            Links.GetFocused += new LinkGetFocusedHandler(LinkandPlans_GetFocused);
            Plans.GetFocused += new PlanGetFocusedHandler(LinkandPlans_GetFocused);


            splitContainer1.Height = Height - toolStrip1.Height;

            MainPanel.Init(this);
            
         
            // research highers for assignments
            List<ulong> ids = Links.GetUplinkIDs(DhtID, ProjectID);

            foreach (ulong id in ids)
                Plans.Research(id);


            RefreshAssigned();
        }
        private void GoalsView_Load(object sender, EventArgs e)
        {
            if(LoadIdent != 0)
                foreach(PlanGoal goal in RootList)
                    if (goal.Ident == LoadIdent)
                    {
                        //crit go to specific branch ((GoalPage)tab).SelectBranch(LoadBranch);
                        // schedule needs to pass a list of the branches from the root to this node
                        // so appropriate path can be expanded

                        MainPanel.LoadGoal(goal);
                        return;
                    }

            if (RootList.Count > 0)
                MainPanel.LoadGoal(RootList[0]);
        }

        internal override bool Fin()
        {
            if (SaveLink.Visible)
            {
                DialogResult result = MessageBox.Show(this, "Save Chages to Goals?", "De-Ops", MessageBoxButtons.YesNoCancel);

                if (result == DialogResult.OK)
                    Plans.SaveLocal();
                if (result == DialogResult.Cancel)
                    return false;
            }

            Links.GuiUpdate -= new LinkGuiUpdateHandler(Links_Update);
            Plans.PlanUpdate -= new PlanUpdateHandler(Plans_Update);

            Links.GetFocused -= new LinkGetFocusedHandler(LinkandPlans_GetFocused);
            Plans.GetFocused -= new PlanGetFocusedHandler(LinkandPlans_GetFocused);

            return true;
        }

        List<ulong> LinkandPlans_GetFocused()
        {
            List<ulong> focus = new List<ulong>(); // new List<ulong>(ids);

            MainPanel.GetFocused(focus);

            return focus;
        }

        void Links_Update(ulong key)
        {
            RefreshAssigned();

            MainPanel.LinkUpdate(key);
        }

        void Plans_Update(OpPlan plan)
        {
            RefreshAssigned();

            MainPanel.PlanUpdate(plan);
        }


        void RefreshAssigned()
        {
            RootList.Clear();
            ArchiveList.Clear();

            Plans.GetAssignedGoals(DhtID, ProjectID, RootList, ArchiveList);

            string label = RootList.Count.ToString();
            label += (RootList.Count == 1) ? " Goal" : " Goals";
            SelectGoalButton.Text = label;
        }

        internal void ChangesMade()
        {
            Plans_Update(Plans.LocalPlan);

            ChangesLabel.Visible = true;
            SaveLink.Visible = true;
            DiscardLink.Visible = true;

            splitContainer1.Height = Height - toolStrip1.Height - 15;
        }

        private void SaveLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ChangesLabel.Visible = false;
            SaveLink.Visible = false;
            DiscardLink.Visible = false;

            splitContainer1.Height = Height - toolStrip1.Height;

            Plans.SaveLocal();
        }

        private void DiscardLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ChangesLabel.Visible = false;
            SaveLink.Visible = false;
            DiscardLink.Visible = false;
            splitContainer1.Height = Height - toolStrip1.Height;

            Plans.LoadPlan(Core.LocalDhtID);
            Plans_Update(Plans.LocalPlan);
        }


        private void SelectGoal_DropDownOpening(object sender, EventArgs e)
        {
            SelectGoalButton.DropDownItems.Clear();

            // add plans
            foreach(PlanGoal goal in RootList)
                SelectGoalButton.DropDownItems.Add(new SelectMenuItem(goal, new EventHandler(SelectGoalMenu_Click)));

            // add archived if exists
            if (ArchiveList.Count > 0)
            {
                ToolStripMenuItem archived = new ToolStripMenuItem("Archived");

                foreach (PlanGoal goal in ArchiveList)
                    archived.DropDownItems.Add(new SelectMenuItem(goal, new EventHandler(SelectGoalMenu_Click)));

                SelectGoalButton.DropDownItems.Add(archived);
            }

            // if local, add create option
            if (DhtID == Core.LocalDhtID)
            {
                SelectGoalButton.DropDownItems.Add(new ToolStripSeparator());

                SelectGoalButton.DropDownItems.Add(new ToolStripMenuItem("Create Goal", null, SelectGoalMenu_Create));
            }
        }

        private void SelectGoalMenu_Click(object sender, EventArgs e)
        {
            SelectMenuItem item = sender as SelectMenuItem;

            if (item == null)
                return;

            MainPanel.LoadGoal(item.Goal);
        }


        private void SelectGoalMenu_Create(object sender, EventArgs e)
        {
            PlanGoal goal = new PlanGoal();
            goal.Ident = Core.RndGen.Next();
            goal.Project = ProjectID;
            goal.Person = Core.LocalDhtID;
            goal.End = Core.TimeNow.AddDays(30).ToUniversalTime();

            EditGoal form = new EditGoal(EditGoalMode.New, Core, goal);

            if (form.ShowDialog(this) == DialogResult.OK)
            {
                ChangesMade();

                MainPanel.LoadGoal(goal);
            }
        }

        private void DetailsButton_CheckedChanged(object sender, EventArgs e)
        {
            splitContainer1.Panel2Collapsed = !DetailsButton.Checked;

        }


        /*private void ArchivedList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ArchiveItem item = GetSelectedArchive();

            if (item == null)
            {
                HideLinks();
                return;
            }

            bool local = false ;

            if (item.Goal.Person == Core.LocalDhtID)
                local = true;

            ViewLink.Visible      = true;
            UnarchiveLink.Visible = local;
            EditLink.Visible      = local;
            DeleteLink.Visible    = local;

            // view or hide
            bool viewing = false;

             foreach (TabPage tab in GoalTabs.TabPages)
                if (tab.GetType() == typeof(GoalPage))
                    if (((GoalPage)tab).Goal.Ident == item.Goal.Ident)
                    {
                        viewing = true;
                        break;
                    }

            ViewLink.Text = viewing ? "Hide" : "View";
        }

        void HideLinks()
        {
            ViewLink.Visible        = false;
            UnarchiveLink.Visible   = false;
            EditLink.Visible        = false;
            DeleteLink.Visible      = false;
        }

        private void ViewLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ArchiveItem item = GetSelectedArchive();

             bool found = false;

             foreach (TabPage tab in GoalTabs.TabPages)
                if (tab.GetType() == typeof(GoalPage))
                    if (((GoalPage)tab).Goal.Ident == item.Goal.Ident)
                    {
                        found = true;
                        SpecialList.Remove(item.Goal.Ident);
                        GoalTabs.TabPages.Remove(tab);
                        break;
                    }

            if (!found)
            {
                SpecialList.Add(item.Goal.Ident);
                AddTab(item.Goal);
            }

            ViewLink.Text = found ? "View" : "Hide";
        }

        private void UnarchiveLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ArchiveItem item = GetSelectedArchive();

            if (item == null)
                return;

            DialogResult result = MessageBox.Show(this, "Are you sure you want to unarchive:\n" + item.Goal.Title + "?", "De-Ops", MessageBoxButtons.YesNo);

            if (result == DialogResult.No)
                return;

            item.Goal.Archived = false;

            if (SpecialList.Contains(item.Goal.Ident))
                SpecialList.Remove(item.Goal.Ident);

            ChangesMade();

            ArchivedList_SelectedIndexChanged(null, null);
        }

        private void EditLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ArchiveItem item = GetSelectedArchive();

            if (item == null)
                return;

            EditGoal form = new EditGoal(EditGoalMode.Edit, Core, item.Goal);

            if (form.ShowDialog(this) == DialogResult.OK)
                ChangesMade();
        }

        private void DeleteLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ArchiveItem item = GetSelectedArchive();

            if (item == null)
                return;

            DialogResult result = MessageBox.Show(this, "Are you sure you want to delete:\n" + item.Goal.Title + "?", "De-Ops", MessageBoxButtons.YesNo);

            if (result == DialogResult.No)
                return;

            Plans.LocalPlan.RemoveGoal(item.Goal);

            if (SpecialList.Contains(item.Goal.Ident))
                SpecialList.Remove(item.Goal.Ident);

            ChangesMade();

            ArchivedList_SelectedIndexChanged(null, null);
        }*/
    }

    class SelectMenuItem : ToolStripMenuItem
    {
        internal PlanGoal Goal;

        internal SelectMenuItem(PlanGoal goal, EventHandler onClick)
            : base(goal.Title, null, onClick)
        {
            Goal = goal;
        }
    }
}
