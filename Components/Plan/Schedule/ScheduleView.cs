using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Components.Link;
using DeOps.Implementation;
using DeOps.Interface;
using DeOps.Interface.TLVex;
using DeOps.Interface.Views;


namespace DeOps.Components.Plan
{
    internal partial class ScheduleView : ViewShell
    {
        internal OpCore Core;
        internal PlanControl Plans;
        LinkControl Links;

        internal ulong DhtID;
        internal uint  ProjectID;

        internal DateTime StartTime;
        internal DateTime EndTime;

        internal Dictionary<ulong, PlanNode> NodeMap = new Dictionary<ulong, PlanNode>();
        internal List<ulong> Uplinks = new List<ulong>();

        internal PlanBlock SelectedBlock;
        internal int SelectedGoalID;

        BlockTip HoverTip = new BlockTip();
        Point    HoverPos = new Point();
        BlockRow HoverBlock;
        int      HoverTicks;
        string   HoverText;

        internal int LoadGoal;
        internal int LoadGoalBranch;

        StringBuilder Details = new StringBuilder(4096);
        const string DefaultPage = @"<html>
                                    <head>
                                    <style>
                                        body { font-family:tahoma; font-size:12px;margin-top:3px;}
                                        td { font-size:10px;vertical-align: middle; }
                                    </style>
                                    </head>

                                    <body bgcolor=#f5f5f5>

                                        

                                    </body>
                                    </html>";

        const string BlockPage = @"<html>
                                    <head>
                                    <style>
                                        body { font-family:tahoma; font-size:12px;margin-top:3px;}
                                        td { font-size:10px;vertical-align: middle; }
                                    </style>

                                    <script>
                                        function SetElement(id, text)
                                        {
                                            document.getElementById(id).innerHTML = text;
                                        }
                                    </script>
                                    
                                    </head>

                                    <body bgcolor=#f5f5f5>

                                        <br>
                                        <b><u><span id='title'><?=title?></span></u></b><br>
                                        <br>
                                        <b>Start</b><br>
                                        <span id='start'><?=start?></span><br>
                                        <br>
                                        <b>Finish</b><br>
                                        <span id='finish'><?=finish?></span><br>
                                        <br>
                                        <b>Notes</b><br>
                                        <span id='notes'><?=notes?></span>

                                    </body>
                                    </html>";

        internal ScheduleView(PlanControl plans, ulong id, uint project)
        {
            InitializeComponent();

            Plans = plans;
            Core = Plans.Core;
            Links = Core.Links;

            DhtID = id;
            ProjectID = project;

            StartTime = Core.TimeNow;
            EndTime   = Core.TimeNow.AddMonths(3);
            
            TopStrip.Renderer = new ToolStripProfessionalRenderer(new OpusColorTable());
            splitContainer1.Panel2Collapsed = true;

            Links.GetFocused += new LinkGetFocusedHandler(LinkandPlans_GetFocused);
            Plans.GetFocused += new PlanGetFocusedHandler(LinkandPlans_GetFocused);

            PlanStructure.NodeExpanding += new EventHandler(PlanStructure_NodeExpanding);
            PlanStructure.NodeCollapsed += new EventHandler(PlanStructure_NodeCollapsed);

            SetDetails(null);
        }

        internal override string GetTitle(bool small)
        {
            if (small)
                return "Schedule";

            string title = "";

            if (DhtID == Core.LocalDhtID)
                title += "My ";
            else
                title += Links.GetName(DhtID) + "'s ";

            if (ProjectID != 0)
                title += Links.ProjectNames[ProjectID] + " ";

            title += "Schedule";

            return title;
        }

        internal override void Init()
        {
            if (DhtID != Core.LocalDhtID)
                NewButton.Visible = false;

            PlanStructure.Columns[1].WidthResized += new EventHandler(PlanStructure_Resized);

            ScheduleSlider.Init(this);

            DateRange.Value = 40;
            UpdateRange();

            GotoTime(Core.TimeNow);

            // guilty of a hack, this sets the last column to the correct length, 
            // firing the event to set the slider to the same size as the column
            PlanStructure.GenerateColumnRects();
            PlanStructure.Invalidate();


            // load links
            RefreshUplinks();
            RefreshStructure();


            // events
            Links.GuiUpdate += new LinkGuiUpdateHandler(Links_Update);
            Plans.PlanUpdate += new PlanUpdateHandler(Plans_Update);
        }

        private void ScheduleView_Load(object sender, EventArgs e)
        {
            RefreshGoalCombo();

            foreach(GoalComboItem item in GoalCombo.Items)
                if (item.ID == LoadGoal)
                {
                    GoalCombo.SelectedItem = item;
                    break;
                }
        }

        private void GotoTime(DateTime time)
        {
            long ticks = ScheduleSlider.Width * ScheduleSlider.TicksperPixel;

            StartTime = time.AddTicks(-ticks * 1/4);
            EndTime   = time.AddTicks(ticks * 3/4);

            ScheduleSlider.RefreshSlider();
        }

        internal override bool Fin()
        {
            if (SaveLink.Visible)
            {
                DialogResult result = MessageBox.Show(this, "Save Chages to Schedule?", "De-Ops", MessageBoxButtons.YesNoCancel);

                if (result == DialogResult.OK)
                    Plans.SaveLocal();
                if (result == DialogResult.Cancel)
                    return false;
            }

            Links.GuiUpdate -= new LinkGuiUpdateHandler(Links_Update);
            Plans.PlanUpdate -= new PlanUpdateHandler(Plans_Update);

            Links.GetFocused -= new LinkGetFocusedHandler(LinkandPlans_GetFocused);
            Plans.GetFocused -= new PlanGetFocusedHandler(LinkandPlans_GetFocused);

            HoverTimer.Enabled = false;
            return true;
        }

        internal override Size GetDefaultSize()
        {
            return new Size(475, 325);
        }

        internal override Icon GetIcon()
        {
            return PlanRes.Schedule;
        }

        void PlanStructure_Resized(object sender, EventArgs args)
        {

            LabelPlus.Location = new Point(PlanStructure.Columns[0].Width - 30, LabelPlus.Location.Y);

            DateRange.Width = LabelPlus.Location.X + 1 - DateRange.Location.X;
            
            ScheduleSlider.Location = new Point( splitContainer1.Panel1.Width - PlanStructure.Columns[1].Width, ScheduleSlider.Location.Y);
            ScheduleSlider.Width = PlanStructure.Columns[1].Width;

            ExtendedLabel.Location = new Point(ScheduleSlider.Location.X, ExtendedLabel.Location.Y);
            ExtendedLabel.Width = ScheduleSlider.Width;

            ExtendedLabel.Update();
            LabelPlus.Update();
        }

        private void DateRange_Scroll(object sender, EventArgs e)
        {
            UpdateRange();
        }

        private void UpdateRange()
        {
            /*
                            tick	hours
            quarter day	    1	    6
            day	            2	    24
            week	        3	    168
            month	        4	    672
            quarter year	5	    2016
            year	        6	    8064
            5 years	        7	    40320
         
            exponential fit,  tick = 1.592 * e ^ (1.4485 * hours)
            */

            double x = DateRange.Maximum - DateRange.Value;
            x /= 20;

            double hours = 1.592 * Math.Exp(1.4485 * x);

            //EndTime = StartTime.AddHours(hours);

            DateTime fourthTime = new DateTime(StartTime.Ticks + (EndTime.Ticks-StartTime.Ticks) / 4);

            StartTime = fourthTime.AddHours(-hours * 1 / 4);
            EndTime = fourthTime.AddHours(hours * 3 / 4);
            
            ScheduleSlider.RefreshSlider();
        }

        private void RefreshStructure()
        {
            PlanStructure.BeginUpdate();

            NodeMap.Clear();
            PlanStructure.Nodes.Clear();

     
            // nodes
            if (Links.ProjectRoots.ContainsKey(ProjectID))
                lock (Links.ProjectRoots[ProjectID])        
                    foreach (OpLink root in Links.ProjectRoots[ProjectID])
                        if (Uplinks.Contains(root.DhtID))
                        {
                            PlanNode node = CreateNode(root);

                            Plans.Research(root.DhtID);

                            LoadNode(node);

                            PlanStructure.Nodes.Add(node);

                            ExpandPath(node, Uplinks);
                        }

            PlanStructure.EndUpdate();
        }

        private void LoadNode(PlanNode node)
        {
            // check if already loaded
            if (node.AddSubs)
                return;


            node.AddSubs = true;

            // go through downlinks
            foreach (ulong id in Links.GetDownlinkIDs(node.Link.DhtID, ProjectID, 1))
            {
                OpLink link = Links.GetLink(id);

                if (link == null)
                    continue;

                // if doesnt exist search for it
                if (!link.Loaded)
                {
                    Links.Research(link.DhtID, ProjectID, false);
                    continue;
                }

                Plans.Research(link.DhtID);

                Utilities.InsertSubNode(node, CreateNode(link));
            }
        }

        private PlanNode CreateNode(OpLink link)
        {
            PlanNode node = new PlanNode(this, link, link.DhtID == DhtID);

            NodeMap[link.DhtID] = node;

            return node;
        }

        private void ExpandPath(PlanNode node, List<ulong> path)
        {
            if (!path.Contains(node.Link.DhtID))
                return;

            // expand triggers even loading nodes two levels down, one level shown, the other hidden
            node.Expand();

            foreach (PlanNode sub in node.Nodes)
                ExpandPath(sub, path);
        }

        void Links_Update(ulong key)
        {
            // if removed from link control, remove from gui
            if (!Links.LinkMap.ContainsKey(key))
            {
                if (NodeMap.ContainsKey(key))
                    RemoveNode(NodeMap[key]);

                return;
            }


            // must be loaded and pertaining to our current project to display
            OpLink link = Links.LinkMap[key];

            if (!link.Loaded)
                return;

            if (!link.Projects.Contains(ProjectID) && !link.Downlinks.ContainsKey(ProjectID))
            {
                if (NodeMap.ContainsKey(key))
                    RemoveNode(NodeMap[key]);

                return;
            }


            // update uplinks
            if (key == DhtID || Uplinks.Contains(key))
                RefreshUplinks();


            // create a node item, or get the current one
            PlanNode node = null;

            if (NodeMap.ContainsKey(key))
                node = NodeMap[key];
            else
                node = new PlanNode(this, link, key == DhtID);


            // get the right parent node for this iem
            TreeListNode parent = null;

            if (!link.Uplink.ContainsKey(ProjectID)) // dont combine below, causes next if to fail
                parent = Uplinks.Contains(key) ? PlanStructure.virtualParent : null;

            else if (NodeMap.ContainsKey(link.Uplink[ProjectID].DhtID))
                parent = NodeMap[link.Uplink[ProjectID].DhtID];

            else
                parent = null; // branch this link is apart of is not visible in current display


            // remember settings
            bool selected = node.Selected;


            if (node.Parent != parent)
            {
                List<ulong> visible = new List<ulong>();

                // remove previous instance of node
                if (node.Parent != null)
                {
                    if (node.IsVisible())
                        visible.Add(link.DhtID);

                    UnloadNode(node, visible);
                    NodeMap.Remove(link.DhtID);
                    node.Remove();
                }


                // if node changes to be sub of another root 3 levels down, whole branch must be reloaded
                if (parent == null || parent == PlanStructure.virtualParent)
                {
                    if(Uplinks.Contains(key))
                        RefreshStructure();
                    
                    return;
                }

                // if new parent is hidden, dont bother adding till user expands
                PlanNode newParent = parent as PlanNode; // null if root

                if (newParent != null && newParent.AddSubs == false)
                    return;


                // copy node to start fresh
                PlanNode newNode = CreateNode(node.Link);


                // check if parent should be moved to project header
                if (newParent != null)
                {
                    Utilities.InsertSubNode(newParent, newNode);

                    // if we are a visible child, must load hidden sub nodes
                    if (newParent.IsVisible() && newParent.IsExpanded)
                        LoadNode(newNode);
                }

                // if node itself is the root
                else
                {
                    LoadNode(newNode);

                    // remove previous
                    foreach (PlanNode old in PlanStructure.Nodes)
                        UnloadNode(old, visible);

                    PlanStructure.Nodes.Clear();
                    PlanStructure.Nodes.Add(newNode);
                }

                node = newNode;


                // recurse to each previously visible node
                foreach (ulong id in visible)
                {
                    List<ulong> uplinks = Links.GetUplinkIDs(id, ProjectID);

                    foreach (PlanNode root in PlanStructure.Nodes) // should only be one root
                        VisiblePath(root, uplinks);
                }
            }

            node.UpdateName();


            node.Selected = selected;
        }

        private void RefreshUplinks()
        {
            Uplinks.Clear();
            Uplinks.Add(DhtID);

            if(Links.LinkMap.ContainsKey(DhtID))
                if (Links.LinkMap[DhtID].Uplink.ContainsKey(ProjectID))
                {
                    OpLink next = Links.LinkMap[DhtID].Uplink[ProjectID];

                    while (next != null)
                    {
                        Uplinks.Add(next.DhtID);
                        next = next.Uplink.ContainsKey(ProjectID) ? next.Uplink[ProjectID] : null;
                    }
                }
        }

        private void VisiblePath(PlanNode node, List<ulong> path)
        {
            bool found = false;

            foreach (PlanNode sub in node.Nodes)
                if (path.Contains(sub.Link.DhtID))
                    found = true;

            if (found)
            {
                node.Expand();

                foreach (PlanNode sub in node.Nodes)
                    VisiblePath(sub, path);
            }
        }

        private void RemoveNode(PlanNode node)
        {
            UnloadNode(node, null); // unload subs
            NodeMap.Remove(node.Link.DhtID); // remove from map
            node.Remove(); // remove from tree
        }

        List<ulong> LinkandPlans_GetFocused()
        {
            List<ulong> focused = new List<ulong>();

            foreach (PlanNode node in PlanStructure.Nodes)
                RecurseFocus(node, focused);

            return focused;
        }

        void RecurseFocus(PlanNode node, List<ulong> focused)
        {
            // add parent to focus list
            focused.Add(node.Link.DhtID);

            // iterate through sub items
            foreach (PlanNode sub in node.Nodes)
                RecurseFocus(sub, focused);
        }

        void Plans_Update(OpPlan plan)
        {
            // if node not tracked
            if(!NodeMap.ContainsKey(plan.DhtID))
                return;

            // update this node, and all subs      (visible below)
            TreeListNode node = (TreeListNode) NodeMap[plan.DhtID];
            
            bool done = false;

            while (node != null && !done)
            {
                ((PlanNode)node).UpdateBlock();

                done = PlanStructure.GetNextNode(ref node);
            }

            RefreshGoalCombo();
        }

        internal void RefreshRows()
        {
            TreeListNode node = (TreeListNode) PlanStructure.virtualParent.FirstChild();

            bool done = false;

            while (node != null && !done)
            {
                ((PlanNode)node).UpdateBlock();

                done = PlanStructure.GetNextNode(ref node);
            }

            SetDetails(LastBlock);
        }

        private void PlanStructure_SelectedItemChanged(object sender, EventArgs e)
        {
            RefreshRows(); // updates selection box graphics

            // children searched on expand

            PlanNode node = GetSelected();

            if (node == null)
            {
                SetDetails(null);
                return;
            }
            // link research
            Links.Research(node.Link.DhtID, ProjectID, false);

            // plan research
            Plans.Research(node.Link.DhtID);
        }

        PlanNode GetSelected()
        {
            if (PlanStructure.SelectedNodes.Count == 0)
                return null;

            PlanNode node = (PlanNode) PlanStructure.SelectedNodes[0];

            return node;
        }

        private void PlanStructure_Enter(object sender, EventArgs e)
        {
            RefreshRows(); // updates selection box graphics
        }

        private void PlanStructure_Leave(object sender, EventArgs e)
        {
            RefreshRows(); // updates selection box graphics
        }

        void PlanStructure_NodeExpanding(object sender, EventArgs e)
        {
            PlanNode node = sender as PlanNode;

            if (node == null)
                return;

            Debug.Assert(node.AddSubs);

            // node now expanded, get next level below children
            foreach (PlanNode child in node.Nodes)
                LoadNode(child);
        }


        void PlanStructure_NodeCollapsed(object sender, EventArgs e)
        {
            PlanNode node = sender as PlanNode;

            if (node == null)
                return;

            if (!node.AddSubs) // this node is already collapsed
                return;

            // remove nodes 2 levels down
            foreach (PlanNode child in node.Nodes)
                UnloadNode(child, null);

            Debug.Assert(node.AddSubs); // this is the top level, children hidden underneath
        }

        private void UnloadNode(PlanNode node, List<ulong> visible)
        {
            node.AddSubs = false;

            if (visible != null && node.IsVisible())
                visible.Add(node.Link.DhtID);

            // for each child, call unload node, then clear
            foreach (PlanNode child in node.Nodes)
            {
                if (NodeMap.ContainsKey(child.Link.DhtID))
                    NodeMap.Remove(child.Link.DhtID);

                UnloadNode(child, visible);
            }

            // unloads children of node, not the node itself
            node.Nodes.Clear();
            node.Collapse();
        }

        private void HoverTimer_Tick(object sender, EventArgs e)
        {
            // if mouse in same block at same position for 5 ticks (2,5 seconds) display tool tip, else hide tool tip

            if ( !Cursor.Position.Equals(HoverPos) )
            {
                HoverTicks = 0;
                HoverPos   = new Point(0, 0);
                HoverBlock = null;
                HoverText  = null;
                HoverTip.Hide(this);
                return;
            }

            if (HoverTicks < 1)
            {
                HoverTicks++;
                return;
            }

            if (HoverText == null)
            {
                HoverText = HoverBlock.GetHoverText();

                HoverTip.Show(HoverText, this, PointToClient(Cursor.Position));
            }
        }

        internal void CursorUpdate(BlockRow block)
        {
            // test block because control can scroll without mouse moving
            if (Cursor.Position.Equals(HoverPos) && block == HoverBlock) 
                return;

            HoverTicks = 0;
            HoverPos   = Cursor.Position;
            HoverBlock = block;
            HoverText  = null;
            HoverTip.Hide(this);
        }

        private void SaveLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ChangesLabel.Visible = false;
            SaveLink.Visible = false;
            DiscardLink.Visible = false;

            PlanStructure.Height += 15;

            Plans.SaveLocal();
        }

        private void DiscardLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ChangesLabel.Visible = false;
            SaveLink.Visible = false;
            DiscardLink.Visible = false;

            PlanStructure.Height += 20;

            Plans.LoadPlan(Core.LocalDhtID);
            Plans_Update(Plans.LocalPlan);
        }

        internal void ChangesMade()
        {
            ChangesLabel.Visible = true;
            SaveLink.Visible = true;
            DiscardLink.Visible = true;

            PlanStructure.Height -= 20;
        }


        private void RefreshGoalCombo()
        {
            GoalComboItem prevItem = GoalCombo.SelectedItem as GoalComboItem;

            int prevSelectedID = 0;
            if (prevItem != null)
                prevSelectedID = prevItem.ID;

            GoalCombo.Items.Clear();

            GoalCombo.Items.Add(new GoalComboItem("None", 0));

            // go up the chain looking for goals which have been assigned to this person
            // at root goal is the title of the goal


            List<PlanGoal> rootList = new List<PlanGoal>();
            List<int> assigned = new List<int>();

            // foreach self & higher
            List<ulong> ids = Links.GetUplinkIDs(DhtID, ProjectID);
            ids.Add(DhtID);

            foreach (ulong id in ids)
            {
                OpPlan plan = Plans.GetPlan(id);

                if (plan == null)
                    continue;

                // goals we have been assigned to
                foreach (List<PlanGoal> list in plan.GoalMap.Values)
                    foreach (PlanGoal goal in list)
                    {
                        if (goal.Project != ProjectID)
                            break;

                        if (goal.Person == DhtID && !assigned.Contains(goal.Ident))
                            assigned.Add(goal.Ident);

                        if (goal.BranchDown == 0)
                            if (!goal.Archived)
                                rootList.Add(goal);
                    }
            }

            // update combo
            GoalComboItem prevSelected = null;

            foreach (PlanGoal goal in rootList)
                if (assigned.Contains(goal.Ident))
                {
                    GoalComboItem item = new GoalComboItem(goal.Title, goal.Ident);

                    if (goal.Ident == prevSelectedID)
                        prevSelected = item;

                    GoalCombo.Items.Add(item);
                }

            if (prevSelected != null)
                GoalCombo.SelectedItem = prevSelected;
            else
                GoalCombo.SelectedIndex = 0;
        }

        private void NewButton_Click(object sender, EventArgs e)
        {
            EditBlock form = new EditBlock(BlockViewMode.New, this, null);

            if (form.ShowDialog(this) == DialogResult.OK)
            {
                ChangesMade();
                Plans_Update(Plans.LocalPlan);
            }
        }

        private void NowButton_Click(object sender, EventArgs e)
        {
            GotoTime(Core.TimeNow);
        }

        private void GoalCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            GoalComboItem item = GoalCombo.SelectedItem as GoalComboItem;

            if (item == null)
                return;

            SelectedGoalID = item.ID;

            RefreshRows();
        }

        private void DetailsButton_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel2Collapsed = !DetailsButton.Checked;

        }


        PlanBlock LastBlock;

        internal void SetDetails(PlanBlock block)
        {
            List<string[]> tuples = new List<string[]>();

            string notes = null;
 
            // get inof that needs to be set
            if (block != null)
            {
                tuples.Add(new string[] { "title",  block.Title });
                tuples.Add(new string[] { "start",  block.StartTime.ToLocalTime().ToString("D") });
                tuples.Add(new string[] { "finish", block.EndTime.ToLocalTime().ToString("D") });
                tuples.Add(new string[] { "notes", block.Description.Replace("\r\n", "<br>") });

                notes = block.Description;
            }

            // set details button
            DetailsButton.ForeColor = Color.Black;

            if (splitContainer1.Panel2Collapsed && notes != null && notes != "")
                DetailsButton.ForeColor = Color.Red;


            if (LastBlock != block)
            {
                Details.Length = 0;

                if (block != null)
                    Details.Append(BlockPage);
                else
                    Details.Append(DefaultPage);

                foreach (string[] tuple in tuples)
                    Details.Replace("<?=" + tuple[0] + "?>", tuple[1]);

                SetDisplay(Details.ToString());
            }
            else
            {
                foreach (string[] tuple in tuples)
                    DetailsBrowser.Document.InvokeScript("SetElement", new String[] { tuple[0], tuple[1] });
            }

            LastBlock = block;
        }

        private void SetDisplay(string html)
        {
            Debug.Assert(!html.Contains("<?"));

            //if (!DisplayActivated)
            //    return;

            // watch transfers runs per second, dont update unless we need to 
            if (html.CompareTo(DetailsBrowser.DocumentText) == 0)
                return;

            // prevents clicking sound when browser navigates
            DetailsBrowser.Hide();
            DetailsBrowser.DocumentText = html;
            DetailsBrowser.Show();
        }

    }


    internal class GoalComboItem
    {
        internal string Name;
        internal int ID;

        internal GoalComboItem(string name, int id)
        {
            Name = name;
            ID = id;
        }

        public override string ToString()
        {
            return Name;
        }
    }


    internal class PlanNode : TreeListNode
    {
        internal ScheduleView View;
        internal OpLink Link;
        internal bool AddSubs;

        internal PlanNode( ScheduleView view, OpLink link, bool local)
        {
            View = view;
            Link = link;

            if (local)
                Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);

            SubItems.Add(new BlockRow(this));

            UpdateName();
            UpdateBlock();
        }

        internal void UpdateName()
        {
            if (Link.Title.ContainsKey(View.ProjectID) && Link.Title[View.ProjectID] != "")
                Text = Link.Name + "\n" + Link.Title[View.ProjectID];
            else
                Text = Link.Name;
        }

        internal void UpdateBlock()
        {
            ((BlockRow)SubItems[0].ItemControl).UpdateRow(true);
        }

        public override string ToString()
        {
            return Text;
        }
    }

    internal class BlockTip : ToolTip
    {
        internal PlanBlock Block;
    }
}