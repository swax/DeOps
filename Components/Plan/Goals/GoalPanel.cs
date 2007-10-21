using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Interface;
using DeOps.Interface.Views;
using DeOps.Interface.TLVex;
using DeOps.Components.Link;


namespace DeOps.Components.Plan
{
    internal partial class GoalPanel : UserControl
    {
        internal GoalsView View;
        internal OpCore Core;
        internal PlanControl Plans;
        internal LinkControl Links;

        PlanGoal Head;
        PlanGoal Selected;

        Dictionary<ulong, List<GoalNode>> TreeMap = new Dictionary<ulong, List<GoalNode>>();


        internal GoalPanel(GoalsView view, PlanGoal head)
        {
            InitializeComponent();

            View = view;
            Core = View.Core;
            Plans = Core.Plans;
            Links = Core.Links;
            Head = head;
            
            GoalTree.NodeExpanding += new EventHandler(GoalTree_NodeExpanding);
            GoalTree.NodeCollapsed += new EventHandler(GoalTree_NodeCollapsed);

            GoalTree.ControlPadding = 3;
            //splitContainer2.Panel1Collapsed = true;

            GoalTree.SmallImageList = new List<Image>();
            GoalTree.SmallImageList.Add(new Bitmap(16, 16));
            GoalTree.SmallImageList.Add(PlanRes.star);
            GoalTree.SmallImageList.Add(PlanRes.high_goal);
            GoalTree.SmallImageList.Add(PlanRes.low_goal);
        }

        private void GoalPanel_Load(object sender, EventArgs e)
        {
            ReloadGoals();

            
            // make self visible / select
            if (TreeMap.ContainsKey(View.DhtID))
            {
                List<GoalNode> list = TreeMap[View.DhtID];

                list[0].Selected = true;
                UpdatePlanItems(list[0]);
            } 
        }

        private void ReloadGoals()
        {
            TreeMap.Clear();
            GoalTree.Nodes.Clear();
            
            List<ulong> uplinks = Links.GetUplinkIDs(View.DhtID, View.ProjectID);
            uplinks.Add(View.DhtID);

            if (!MineOnly.Checked)
            {
                GoalNode root = CreateNode(Head);
                LoadNode(root);
                GoalTree.Nodes.Add(root);

                ExpandPath(root, uplinks);
            }
            else
            {
                foreach (ulong id in uplinks)
                {
                    OpPlan plan = Plans.GetPlan(id);

                    if (plan != null && plan.GoalMap.ContainsKey(Head.Ident))
                        foreach (PlanGoal goal in plan.GoalMap[Head.Ident])
                            if (goal.Person == View.DhtID)
                            {
                                GoalNode root = CreateNode(goal);
                                LoadNode(root);
                                InsertSubNode(GoalTree.virtualParent, root);
                                root.Expand();
                            }
                }
            }

            Reselect();
        }

        internal void InsertSubNode(TreeListNode parent, GoalNode node)
        {
            int index = 0;

            foreach (TreeListNode entry in parent.Nodes)
                if (string.Compare(node.Text, entry.Text, true) < 0)
                {
                    parent.Nodes.Insert(index, node);
                    node.RefreshProgress();
                    return;
                }
                else
                    index++;

            parent.Nodes.Insert(index, node);
            node.RefreshProgress();
        }

        private GoalNode CreateNode(PlanGoal goal)
        {
            GoalNode node = new GoalNode(this, goal);

            if (!TreeMap.ContainsKey(goal.Person))
                TreeMap[goal.Person] = new List<GoalNode>();

            TreeMap[goal.Person].Add(node);

            return node;
        }

        private void ExpandPath(GoalNode node, List<ulong> uplinks)
        {
            if (!uplinks.Contains(node.Goal.Person))
                return;

            // expand triggers even loading nodes two levels down, one level shown, the other hidden
            node.Expand();

            foreach (GoalNode sub in node.Nodes)
                ExpandPath(sub, uplinks);
        }

        private void LoadNode(GoalNode node)
        {
            // check if already loaded
            if (node.AddSubs)
                return;

            node.AddSubs = true;

            // load that person specified by the node
            OpPlan plan = Plans.GetPlan(node.Goal.Person);

            if (plan == null)
            {
                Plans.Research(node.Goal.Person);
                return;
            }

            if (!plan.GoalMap.ContainsKey(Head.Ident))
                return;

            // read the person's goals
            foreach (PlanGoal goal in plan.GoalMap[Head.Ident])
            {
                // if the upbranch matches the node's down branch, add
                if (goal.BranchDown == 0 || goal.BranchUp != node.Goal.BranchDown)
                    continue;

                if (CheckGoal(plan.DhtID, goal))
                    InsertSubNode(node, CreateNode(goal));
            }
        }

        bool CheckGoal(ulong higher, PlanGoal goal)
        {
            // if only branch
            if (MineOnly.Checked && View.DhtID != goal.Person)
                if (!Links.IsHigher(View.DhtID, goal.Person, View.ProjectID) && // show only if person is higher than self
                    !Links.IsLower(View.DhtID, goal.Person, View.ProjectID))   // or is lower than self
                    return false;

            // only subordinates can have goals assigned
            if (!Links.IsLower(higher, goal.Person, View.ProjectID))
                return false;

            return true;

        }

        private void GoalTree_SelectedItemChanged(object sender, EventArgs e)
        {
            if (GoalTree.SelectedNodes.Count == 0)
                return;

            GoalNode node = GoalTree.SelectedNodes[0] as GoalNode;

            if (node == null)
                return;

            Plans.Research(node.Goal.Person);
            Links.Research(node.Goal.Person, View.ProjectID, false);

            UpdatePlanItems(node);
        }

        private void UpdatePlanItems(GoalNode node)
        {
            PlanList.Items.Clear();
            
            
            if (node == null)
            {
                Selected = null;
                DelegateLink.Hide();
                PlanList.Columns[0].Text = "Plan";
                //splitContainer2.Panel1Collapsed = true;
                return;
            }

            Selected = node.Goal;

            /* notes panel
            NotesBox.Rtf = Selected.Description;
            splitContainer2.Panel1Collapsed = (NotesBox.Text == "");

            if (!splitContainer2.Panel1Collapsed)
            {
                Color backColor = Color.WhiteSmoke;

                if (View.DhtID == node.Goal.Person || Links.IsHigher(View.DhtID, node.Goal.Person, View.ProjectID))
                    backColor = Color.FromArgb(255, 245, 245);

                else if (Links.IsLower(View.DhtID, node.Goal.Person, View.ProjectID))
                    backColor = Color.FromArgb(245, 245, 255);

                splitContainer2.Panel1.BackColor = backColor;
                NotesBox.BackColor = backColor;
            }*/

            // set delegate task vis
            if (Selected.Person == Core.LocalDhtID && Links.HasSubs(Selected.Person, View.ProjectID ))
                DelegateLink.Show();
            else
                DelegateLink.Hide();

            if (Selected.Person == Core.LocalDhtID)
            {
                PlanList.Height = splitContainer1.Panel2.Height - 15;
                AddItemLink.Show();
            }
            else
            {
                PlanList.Height = splitContainer1.Panel2.Height;
                AddItemLink.Hide();
            }

            // name's Plan for <goal>

            PlanList.Columns[0].Text = Links.GetName(node.Goal.Person) + "'s Plan for " + node.Goal.Title;

            // set plan items
            OpPlan plan = Plans.GetPlan(Selected.Person);

            if (plan == null) // re-searched at during selection
                return;


            if (plan.ItemMap.ContainsKey(Head.Ident))
                foreach (PlanItem item in plan.ItemMap[Head.Ident])
                    if (item.BranchUp == Selected.BranchDown)
                    {
                        PlanListItem row = new PlanListItem(item);
                        PlanList.Items.Add(row);
                        FormatTime(row);
                    }

            PlanList.Invalidate();
        }


        private void DelegateLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            PlanGoal goal = new PlanGoal();
            goal.Ident = Head.Ident;
            goal.Project = Head.Project;
            goal.End = Head.End;

            goal.BranchUp = Selected.BranchDown;
            goal.BranchDown = Core.RndGen.Next();

            EditGoal form = new EditGoal(EditGoalMode.Delgate, Core, goal);

            if (form.ShowDialog(this) == DialogResult.OK)
                View.ChangesMade();
        }

        private void AddItemLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            PlanItem item = new PlanItem();
            item.Ident = Head.Ident;
            item.Project = Head.Project;
            item.BranchUp = Selected.BranchDown;
            item.Start = Core.TimeNow.ToUniversalTime();
            item.End = Selected.End;

            EditPlanItem form = new EditPlanItem(EditItemMode.New, Selected, item);

            if (form.ShowDialog(this) == DialogResult.OK)
            {
                Plans.LocalPlan.AddItem(form.Editing);
                View.ChangesMade();
            }
        }

        private void PlanList_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            PlanListItem clicked = PlanList.GetItemAt(e.Location) as PlanListItem;

            if (clicked == null)
                return;

            ContextMenuStripEx menu = new ContextMenuStripEx();

            if (Selected.Person == Core.LocalDhtID)
            {
                menu.Items.Add(new PlanMenuItem("Edit", clicked.Item, null, Plan_Edit));
                menu.Items.Add("-");
                menu.Items.Add(new PlanMenuItem("Delete", clicked.Item, PlanRes.delete, Plan_Delete));
            }
            else
                menu.Items.Add(new PlanMenuItem("Details", clicked.Item, PlanRes.details, Plan_View));


            menu.Show(PlanList, e.Location);
        }

        void Plan_Edit(object sender, EventArgs e)
        {
            PlanMenuItem menu = sender as PlanMenuItem;

            if (menu == null)
                return;

            if (Selected == null)
                return;

            EditPlanItem form = new EditPlanItem(EditItemMode.Edit, Selected, menu.Item);

            if (form.ShowDialog(this) == DialogResult.OK)
                View.ChangesMade();
        }

        void Plan_Delete(object sender, EventArgs e)
        {
            PlanMenuItem menu = sender as PlanMenuItem;

            if (menu == null)
                return;

            DialogResult result = MessageBox.Show(this, "Are you sure you want to delete:\n" + menu.Item.Title + "?", "De-Ops", MessageBoxButtons.YesNo);

            if (result == DialogResult.No)
                return;

            Plans.LocalPlan.RemoveItem(menu.Item);

            // signal update
            View.ChangesMade();
        }

        void Plan_View(object sender, EventArgs e)
        {
            PlanMenuItem menu = sender as PlanMenuItem;

            if (menu == null)
                return;

            EditPlanItem form = new EditPlanItem(EditItemMode.View, Selected, menu.Item);
            form.ShowDialog(this);
        }

        private void GoalTree_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            GoalNode node = GoalTree.GetNodeAt(e.Location) as GoalNode;

            if (node == null)
                return;


            ContextMenuStripEx menu = new ContextMenuStripEx();


            bool owned = IsOwned(node);

            bool root = false;
            GoalNode parent = node.ParentNode() as GoalNode;

            if (parent == null && node.Goal.Person == Core.LocalDhtID && Head.Person == Core.LocalDhtID)
                root = true;

            if(owned)
            {
                menu.Items.Add(new GoalMenuItem("Edit", node.Goal, null, Goal_Edit));
                menu.Items.Add(new GoalMenuItem("View Schedule", node.Goal, PlanRes.Schedule.ToBitmap(), Goal_Schedule));
                menu.Items.Add("-");
            }

            if(root)
                menu.Items.Add(new GoalMenuItem("Archive", node.Goal, PlanRes.archive, Goal_Archive));

            if(owned)
                menu.Items.Add(new GoalMenuItem("Delete", node.Goal, PlanRes.delete, Goal_Delete));

            if(!owned)
                menu.Items.Add(new GoalMenuItem("Details", node.Goal, PlanRes.details, Goal_View));

  
            menu.Show(GoalTree, e.Location);
        }

        private bool IsOwned(GoalNode node)
        {
            GoalNode parent = node.ParentNode() as GoalNode;

            if (parent != null && parent.Goal.Person == Core.LocalDhtID)
                return true;

            if (parent == null && node.Goal.Person == Core.LocalDhtID && Head.Person == Core.LocalDhtID)
                return true;

            return false;
        }

        void Goal_Edit(object sender, EventArgs e)
        {
            GoalMenuItem item = sender as GoalMenuItem;

            if (item == null)
                return;

            EditGoal form = new EditGoal(EditGoalMode.Edit, Core, item.Goal);

            if (form.ShowDialog(this) == DialogResult.OK)
                View.ChangesMade();
        }

        void Goal_Schedule(object sender, EventArgs e)
        {
            GoalMenuItem item = sender as GoalMenuItem;

            if (item == null)
                return;

            if (View.External != null)
                foreach (ExternalView ext in View.Core.GuiMain.ExternalViews)
                    if (ext.Shell.GetType() == typeof(ScheduleView))
                        if (((ScheduleView)ext.Shell).DhtID == View.DhtID && ((ScheduleView)ext.Shell).ProjectID == View.ProjectID)
                        {
                            ext.BringToFront();
                            return;
                        }

            ScheduleView view = new ScheduleView(Plans, View.DhtID, View.ProjectID);
            view.LoadGoal = item.Goal.Ident;
            view.LoadGoalBranch = item.Goal.BranchUp;

            if (View.External != null)
                Core.InvokeInterface(Core.GuiMain.ShowExternal, view);
            else
                Core.InvokeInterface(Core.GuiMain.ShowInternal, view);
        }


        void Goal_Delete(object sender, EventArgs e)
        {
            GoalMenuItem item = sender as GoalMenuItem;

            if (item == null)
                return;

            DialogResult result = MessageBox.Show(this, "Are you sure you want to delete:\n" + item.Goal.Title + "?", "De-Ops", MessageBoxButtons.YesNo);

            if (result == DialogResult.No)
                return;

            Plans.LocalPlan.RemoveGoal(item.Goal);

            // signal update
            View.ChangesMade();
        }

        void Goal_View(object sender, EventArgs e)
        {
            GoalMenuItem item = sender as GoalMenuItem;

            if (item == null)
                return;

            EditGoal form = new EditGoal(EditGoalMode.View, Core, item.Goal);
            form.ShowDialog(this);
        }

        void Goal_Archive(object sender, EventArgs e)
        {
            GoalMenuItem item = sender as GoalMenuItem;

            if (item == null)
                return;

            DialogResult result = MessageBox.Show(this, "Are you sure you want to archive:\n" + item.Goal.Title + "?", "De-Ops", MessageBoxButtons.YesNo);

            if (result == DialogResult.No)
                return;

            item.Goal.Archived = true;

            // signal update
            View.ChangesMade();
        }

        private void FormatTime(PlanListItem row)
        {
            row.SubItems[0].Text =
                GetTimeText(row.Item.Start.ToLocalTime() - Core.TimeNow, row.Item.Start.ToLocalTime(), true) + " to " +
                GetTimeText(row.Item.End.ToLocalTime() - Core.TimeNow, row.Item.End.ToLocalTime(), false);
        }

        internal string GetTimeText(TimeSpan span, DateTime time, bool start)
        {
            span = span.Duration(); // get absolute value so past can be handled the same

            // if less than 24 hours left just show time
            if (!start && span.TotalHours < 24)
                return time.ToString("t"); // 1:45 PM

            // if less than a week left show day
            if (span.TotalDays < 7)
                return time.ToString("dddd"); // Monday

            if (Core.TimeNow.Year == time.Year)
                return time.ToString("MMM %d"); // Feb 2

            return time.ToString("d");
        }
        
        private void MineOnly_CheckedChanged(object sender, EventArgs e)
        {
            ReloadGoals();

            Reselect();
        }

        private void Reselect()
        {
            if (Selected == null)
            {
                UpdatePlanItems(null);
                return;
            }


            // make self visible / select
            if (TreeMap.ContainsKey(Selected.Person))
                foreach (GoalNode node in TreeMap[Selected.Person])
                    if (node.Goal.BranchDown == Selected.BranchDown)
                    {
                        node.Selected = true;
                        UpdatePlanItems(node);
                        return;
                    }


            UpdatePlanItems(null);
        }

        internal void GetFocused(List<ulong> focus)
        {
            // return all plans in tree, plus 1 down what is not visible
            // return all plans up from our own assigned plans, and 1 down from them

            RecurseFocus(GoalTree.Nodes, focus);
        }

        private void RecurseFocus(TreeListNodeCollection children, List<ulong> focus)
        {
            foreach (GoalNode node in children)
            {
                focus.Add(node.Goal.Person);

                RecurseFocus(node.Nodes, focus);
            }
        }
  
        void GoalTree_NodeExpanding(object sender, EventArgs e)
        {
            GoalNode node = sender as GoalNode;

            if (node == null)
                return;

            Debug.Assert(node.AddSubs);

            // search invisible children of expanded nodes so ready when expanded
            foreach (GoalNode child in node.Nodes)
                LoadNode(child);
            
        }

        void GoalTree_NodeCollapsed(object sender, EventArgs e)
        {
            GoalNode node = sender as GoalNode;

            if (node == null)
                return;

            if (!node.AddSubs) // this node is already collapsed
                return;
        
            // remove nodes 2 levels down
            foreach (GoalNode child in node.Nodes)
                UnloadNode(child, null);

            Debug.Assert(node.AddSubs); // this is the top level, children hidden underneath
        }

        private void UnloadNode(GoalNode node, List<ulong> visible)
        {
              node.AddSubs = false;

            // for each child, call unload node, then clear
              foreach (GoalNode child in node.Nodes)
              {
                  RemoveFromMap(child.Goal);
                 

                  if (visible != null && child.IsVisible())
                      visible.Add(child.Goal.Person);

                  UnloadNode(child, visible);
              }


              node.Nodes.Clear();
              node.Collapse();
        }

        private void RemoveFromMap(PlanGoal goal)
        {

            if (!TreeMap.ContainsKey(goal.Person))
                return;

            List<GoalNode> list = TreeMap[goal.Person];

            foreach (GoalNode item in list)
                if (item.Goal.BranchDown == goal.BranchDown)
                {
                    list.Remove(item);
                    break;
                }

            if (list.Count == 0)
                TreeMap.Remove(goal.Person);
        }


        internal void LinkUpdate(ulong key)
        {
            List<ulong> visible = new List<ulong>();

            // remove key from all parent sub items
            // if link changed children confirmations, this takes care of that during the re-add

            if( MineOnly.Checked )
                return;

            if (key == Head.Person || MineOnly.Checked)
            {
                ReloadGoals();
                return;
            }
            
            else if (TreeMap.ContainsKey(key))
            {
                List<GoalNode> list = new List<GoalNode>(TreeMap[key]);

                foreach (GoalNode node in list)
                    RemoveNode(node, visible);
            }
            
            List<ulong> myUplinks = Links.GetUplinkIDs(View.DhtID, View.ProjectID);
            List<ulong> linkUplinks = Links.GetUplinkIDs(key, View.ProjectID);

            // each uplink from this key can assign goals, we need to see if a re-add is necessary
            foreach (ulong uplink in linkUplinks)
            {
                OpPlan plan = Plans.GetPlan(uplink);

                if (plan != null && TreeMap.ContainsKey(uplink))
                    foreach (GoalNode treeNode in TreeMap[uplink])
                        // look through goals for assignments for this link
                        if(treeNode.AddSubs && plan.GoalMap.ContainsKey(Head.Ident))
                            foreach(PlanGoal goal in plan.GoalMap[Head.Ident])
                                if (goal.Person == key && 
                                    treeNode.Goal.BranchDown == goal.BranchUp &&
                                    CheckGoal(plan.DhtID, goal))
                                {
                                    GoalNode node = CreateNode(goal);
                                    
                                    InsertSubNode(treeNode, node);
                                    RefreshParents(node);

                                    if (treeNode.IsExpanded)
                                        LoadNode(node);
                                }
            }

            // make removed nodes that were visible, visible again
            foreach (ulong id in visible)
            {
                List<ulong> uplinks = Links.GetUplinkIDs(id,View. ProjectID);

                foreach (GoalNode root in GoalTree.Nodes) // should only be one root
                    VisiblePath(root, uplinks);
            }

            Reselect();
        }

        private void VisiblePath(GoalNode node, List<ulong> path)
        {
             bool found = false;

            foreach (GoalNode sub in node.Nodes)
                if (path.Contains(sub.Goal.Person))
                    found = true;

            if (found)
            {
                node.Expand();

                foreach (GoalNode sub in node.Nodes)
                    VisiblePath(sub, path);
            }
        }

        private void RemoveNode(GoalNode node, List<ulong> visible)
        {
            RefreshParents(node);
            UnloadNode(node, visible);
            RemoveFromMap(node.Goal);
            node.Remove();
        }

        internal void PlanUpdate(OpPlan plan)
        {
            if (!plan.Loaded)
                Plans.LoadPlan(plan.DhtID);


            // update progress of high levels for updated plan not in tree map because it is hidden
            if (plan.GoalMap.ContainsKey(Head.Ident) || plan.ItemMap.ContainsKey(Head.Ident))
            {
                List<ulong> uplinks = Links.GetUplinkIDs(plan.DhtID, View.ProjectID);
                uplinks.Add(plan.DhtID);

                if (uplinks.Contains(Head.Person))
                    foreach (ulong id in uplinks)
                        if (TreeMap.ContainsKey(id))
                            foreach (GoalNode node in TreeMap[id])
                                node.RefreshProgress();
            }
           
            if (!TreeMap.ContainsKey(plan.DhtID))
                return;

            if (MineOnly.Checked)
            {
                List<ulong> myUplinks = Links.GetUplinkIDs(View.DhtID, View.ProjectID);
                myUplinks.Add(View.DhtID);

                if (myUplinks.Contains(plan.DhtID))
                {
                    ReloadGoals();
                    return;
                }
            }


            List<PlanGoal> updated = new List<PlanGoal>();
            if(plan.GoalMap.ContainsKey(Head.Ident))
                updated = plan.GoalMap[Head.Ident];


            List<ulong> visible = new List<ulong>();

            foreach (GoalNode oldNode in TreeMap[plan.DhtID])
            {
                // if root
                if (oldNode.Goal.BranchDown == 0)
                {
                    foreach (PlanGoal updatedGoal in updated)
                        if (updatedGoal.BranchDown == 0)
                        {
                            oldNode.Update(updatedGoal);
                            RefreshParents(oldNode);
                            break;
                        }
                }


                // go through displayed goals, if doesn't exist in update, remove
                List<GoalNode> removeList = new List<GoalNode>();

                foreach (GoalNode original in oldNode.Nodes)
                {
                    bool remove = true;

                    foreach (PlanGoal updatedGoal in updated)
                        if (oldNode.Goal.BranchDown == updatedGoal.BranchUp &&
                            original.Goal.BranchDown == updatedGoal.BranchDown)
                        {
                            remove = false;
                            original.Update(updatedGoal);
                            RefreshParents(original);
                        }

                    if (remove)
                        removeList.Add(original);
                }

                foreach (GoalNode node in removeList)
                    RemoveNode(node, visible);

                // go through updated goals, if isn't shown in display, add
                foreach (PlanGoal updatedGoal in updated)
                {
                    bool add = true; // if not in subs, add

                    if (updatedGoal.BranchDown == 0)
                        continue;

                    if (oldNode.Goal.BranchDown != updatedGoal.BranchUp)
                        continue;

                    foreach (GoalNode original in oldNode.Nodes)
                        if (oldNode.Goal.BranchDown == updatedGoal.BranchUp &&
                            original.Goal.BranchDown == updatedGoal.BranchDown)
                            add = false;
                            //crit check this is hit

                    if (add && oldNode.AddSubs && CheckGoal(plan.DhtID, updatedGoal))
                    {
                        GoalNode node = CreateNode(updatedGoal);
                        
                        InsertSubNode(oldNode, node);
                        RefreshParents(node);

                        if(oldNode.IsExpanded)
                            LoadNode(node);                     
                    }
                }
            }


            Reselect();
        }

        private void RefreshParents(GoalNode node)
        {
            GoalNode parent = node.Parent as GoalNode;

            while (parent != null)
            {
                parent.RefreshProgress();

                parent = parent.Parent as GoalNode;
            }
        }

        private void GoalTree_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            GoalNode node = GoalTree.GetNodeAt(e.Location) as GoalNode;

            if (node == null)
                return;

            bool owned  = IsOwned(node);
            EditGoalMode editMode = owned ? EditGoalMode.Edit : EditGoalMode.View;

            EditGoal form = new EditGoal(editMode, Core, node.Goal);

            if (form.ShowDialog(this) == DialogResult.OK)
                if (owned)
                    View.ChangesMade();
        }

        private void PlanList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            PlanListItem item = PlanList.GetItemAt(e.Location) as PlanListItem;

            if (item == null)
                return;

            if (Selected == null)
                return;

            bool local = (Selected.Person == Core.LocalDhtID);

            EditItemMode mode = local ? EditItemMode.Edit : EditItemMode.View;
            EditPlanItem form = new EditPlanItem(mode, Selected, item.Item);

            if (form.ShowDialog(this) == DialogResult.OK)
                if(local)
                    View.ChangesMade();
        }
    }


    internal class GoalNode : TreeListNode
    {
        GoalPanel Panel;
        internal PlanGoal Goal;

        ProgressText Progress = new ProgressText();
        internal bool AddSubs;


        internal GoalNode(GoalPanel panel, PlanGoal goal)
        {
            Panel = panel;

            SubItems.Add(""); // person
            SubItems.Add(Progress); // progress
            SubItems.Add(""); // deadline

            Update(goal);
        }

        internal void Update(PlanGoal goal)
        {
            Goal = goal;

            Text = Goal.Title;

            string name = Panel.Links.GetName(goal.Person);

            if (name == "")
            {
                name = "Unknown";
                Panel.Links.Research(goal.Person, goal.Project, false);
            }

            SubItems[0].Text = name;
            SubItems[2].Text = Panel.GetTimeText(goal.End.ToLocalTime() - Panel.Core.TimeNow, goal.End.ToLocalTime(), false);

            if (goal.Person == Panel.View.DhtID)
                ImageIndex = 1;
            // higher
            else if (Panel.Links.IsHigher(Panel.View.DhtID, goal.Person, Panel.View.ProjectID))
                ImageIndex = 2;
            // lower
            else if (Panel.Links.IsHigher(goal.Person, Panel.View.DhtID, Panel.View.ProjectID))
                ImageIndex = 3;
            else
                ImageIndex = 0;

            RefreshProgress();
        }

        private bool LowNode()
        {
            GoalNode above = Parent as GoalNode;

            while (above != null)
            {
                if (above.Goal.Person == Panel.View.DhtID)
                    return true;

                above = above.Parent as GoalNode;
            }

            return false;
        }

        internal void RefreshProgress()
        {
            int completed = 0, total = 0, level = Level();
            
            Panel.Plans.GetEstimate(Goal, ref completed, ref total);

            // minimize refresh flickering
            if (Progress.Level != level || Progress.Completed != completed || Progress.Total != total)
            {
                Progress.Level = level;
                Progress.Completed = completed;
                Progress.Total = total;

                Progress.Invalidate();
            }
        }

        public override string ToString()
        {

            return Text;
        }
    }

    internal class PlanListItem : ContainerListViewItem
    {
        internal PlanItem Item;

        ProgressText Progress = new ProgressText();


        internal PlanListItem(PlanItem item)
        {
            Item = item;
            Text = item.Title;

            // when
            SubItems.Add("");

            // progress
            Progress.Total = item.HoursTotal;
            Progress.Completed = item.HoursCompleted;

            SubItems.Add(Progress);
        }
    }

    internal class GoalMenuItem : ToolStripMenuItem
    {
        internal PlanGoal Goal;

        internal GoalMenuItem(string caption, PlanGoal goal, Image icon, EventHandler onClick)
            : base(caption, icon, onClick)
        {
            Goal = goal;
        }
    }

    internal class PlanMenuItem : ToolStripMenuItem
    {
        internal PlanItem Item;

        internal PlanMenuItem(string caption, PlanItem item, Image icon, EventHandler onClick)
            : base(caption, icon, onClick)
        {
            Item = item;
        }
    }

}
