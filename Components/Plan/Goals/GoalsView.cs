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


            GoalTabs.Height = Height;

            if (DhtID != Core.LocalDhtID)
                CreateButton.Hide();

            // research highers for assignments
            List<ulong> ids = Links.GetUplinkIDs(DhtID, ProjectID);

            foreach (ulong id in ids)
                Plans.Research(id);

            UpdateTabs();

            if (GoalTabs.TabPages.Count > 1)
                GoalTabs.SelectedTab = GoalTabs.TabPages[1];
        }
        private void GoalsView_Load(object sender, EventArgs e)
        {
             foreach (TabPage tab in GoalTabs.TabPages)
                if (tab.GetType() == typeof(GoalPage))
                    if (((GoalPage)tab).Goal.Ident == LoadIdent)
                    {
                        GoalTabs.SelectedTab = tab;
                        //crit go to specific branch ((GoalPage)tab).SelectBranch(LoadBranch);
                        // schedule needs to pass a list of the branches from the root to this node
                        // so appropriate path can be expanded
                        break;
                    }
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

            foreach (TabPage tab in GoalTabs.TabPages)
                if (tab.GetType() == typeof(GoalPage))
                    ((GoalPage)tab).Panel.GetFocused(focus);

            return focus;
        }

        void Links_Update(ulong key)
        {
            UpdateTabs();

            foreach (TabPage tab in GoalTabs.TabPages )
                if (tab.GetType() == typeof(GoalPage))
                    ((GoalPage)tab).Panel.LinkUpdate(key);
        }

        void Plans_Update(OpPlan plan)
        {
            UpdateTabs();


            foreach (TabPage tab in GoalTabs.TabPages)
                if (tab.GetType() == typeof(GoalPage))
                    ((GoalPage)tab).Panel.PlanUpdate(plan);
        }


        void UpdateTabs()
        {
            List<PlanGoal> rootList = new List<PlanGoal>();
            List<PlanGoal> archiveList = new List<PlanGoal>();
            List<int> assigned = new List<int>();

            // foreach self & higher
            List<ulong> ids = Links.GetUplinkIDs(DhtID, ProjectID);
            ids.Add(DhtID);
            
            foreach (ulong id in ids)
            {
                OpPlan plan = Plans.GetPlan(id);

                if (plan == null)
                    continue;

                // apart of goals we have been assigned to

                foreach (List<PlanGoal> list in plan.GoalMap.Values)
                    foreach (PlanGoal goal in list)
                    {
                        if (goal.Project != ProjectID)
                            break;

                        if (goal.Person == DhtID && !assigned.Contains(goal.Ident))
                            assigned.Add(goal.Ident);

                        if (goal.BranchDown == 0)
                        {
                            if(goal.Archived)
                                archiveList.Add(goal);
                            else
                                rootList.Add(goal);
                        }
                    }
            }

            // update archive
            ArchivedList.Items.Clear();

            foreach (PlanGoal goal in archiveList)
                if(assigned.Contains(goal.Ident))
                    ArchivedList.Items.Add(new ArchiveItem(goal));

            ArchivedList.Invalidate();

            // check if in tabs, if not (add tab)
            foreach (PlanGoal goal in rootList)
                if (assigned.Contains(goal.Ident))
                {
                    bool add = true;

                    foreach (TabPage tab in GoalTabs.TabPages)
                        if (tab.GetType() == typeof(GoalPage))
                            if (((GoalPage)tab).Goal.Ident == goal.Ident)
                            {
                                add = false;
                                break;
                            }

                    if (add)
                        AddTab(goal);
                }

            // check if in list, if not remove
            List<TabPage> removeTabs = new List<TabPage>();

            foreach (TabPage tab in GoalTabs.TabPages )
                if (tab.GetType() == typeof(GoalPage))
                {
                    bool remove = true;

                    GoalPage page = (GoalPage)tab;

                    foreach (PlanGoal goal in rootList)
                        if(assigned.Contains(goal.Ident))
                            if (goal.Ident == page.Goal.Ident)
                            {
                                page.Update(goal);
                                remove = false;
                                break;
                            }

                    if (remove && // special for archive tabs
                        SpecialList.Contains(page.Goal.Ident) &&
                        Plans.GetPlan(page.Goal.Person).GoalMap.ContainsKey(page.Goal.Ident))
                    {
                        page.Update(page.Goal);
                        remove = false;
                    }

                    if (remove)
                        removeTabs.Add(tab);
                }
               
            foreach(TabPage page in removeTabs)
                GoalTabs.TabPages.Remove(page);
        }

        void AddTab(PlanGoal goal)
        {
            // sort from highest assigner to lowest, and by deadline


            GoalTabs.TabPages.Add(new GoalPage(goal, this)); 
        }

        private void CreateButton_Click(object sender, EventArgs e)
        {
            PlanGoal goal = new PlanGoal();
            goal.Ident = Core.RndGen.Next();
            goal.Project = ProjectID;
            goal.Person = Core.LocalDhtID;
            goal.End = Core.TimeNow.AddDays(30).ToUniversalTime();

            EditGoal form = new EditGoal(EditGoalMode.New, Core, goal);

            if (form.ShowDialog(this) == DialogResult.OK)
                ChangesMade();

        }

        internal void ChangesMade()
        {
            Plans_Update(Plans.LocalPlan);

            ChangesLabel.Visible = true;
            SaveLink.Visible = true;
            DiscardLink.Visible = true;
            
            GoalTabs.Height = Height - 15;
        }

        private void SaveLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ChangesLabel.Visible = false;
            SaveLink.Visible = false;
            DiscardLink.Visible = false;

            GoalTabs.Height = Height;

            Plans.SaveLocal();
        }

        private void DiscardLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ChangesLabel.Visible = false;
            SaveLink.Visible = false;
            DiscardLink.Visible = false;
            GoalTabs.Height = Height;

            Plans.LoadPlan(Core.LocalDhtID);
            Plans_Update(Plans.LocalPlan);
        }

        private void ArchivedList_SelectedIndexChanged(object sender, EventArgs e)
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

        ArchiveItem GetSelectedArchive()
        {
            if (ArchivedList.SelectedItems.Count == 0)
                return null;

            ArchiveItem item = ArchivedList.SelectedItems[0] as ArchiveItem;

            return item;
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
        }
        


    }

    internal class GoalPage : TabPage
    {
        internal PlanGoal Goal;

        internal GoalPanel Panel;


        internal GoalPage(PlanGoal goal, GoalsView view)
        {
            UseVisualStyleBackColor = true;

            Update(goal);

            Panel = new GoalPanel(view, goal);
            Panel.Dock = DockStyle.Fill;
            Controls.Add(Panel);
        }

        internal void Update(PlanGoal goal)
        {
            Goal = goal;

            if (Goal.Archived)
                Text = "* " + Goal.Title;
            else
                Text = Goal.Title;
        }
    }

    internal class ArchiveItem : ContainerListViewItem
    {
        internal PlanGoal Goal;

        internal ArchiveItem(PlanGoal goal)
        {
            Goal = goal;

            Text = goal.Title;
        }
    }
}
