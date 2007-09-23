using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;

using DeOps.Implementation;
using DeOps.Components.Location;
using DeOps.Interface.TLVex;

namespace DeOps.Components.Link
{
    internal enum CommandTreeMode { Operation, Online };

    class LinkTree : TreeListViewEx 
    {
        internal OpCore Core;
        internal LinkControl Links;

        internal LabelNode ProjectNode;
        internal LabelNode UnlinkedNode;
        internal LabelNode OnlineNode;


        Dictionary<ulong, LinkNode> NodeMap = new Dictionary<ulong, LinkNode>();

        internal ulong SelectedLink;
        internal ulong ForceRootID;
        internal bool HideUnlinked;

        internal CommandTreeMode TreeMode;
        internal uint Project;

        internal bool FirstLineBlank = true;


        internal LinkTree()
        {
            HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
        }

        internal void Init(LinkControl links)
        {
            FullRowSelect = false;

            Links = links;
            Core = links.Core;

            Core.Locations.GuiUpdate += new LocationGuiUpdateHandler(Locations_Update);
            Links.GuiUpdate += new LinkGuiUpdateHandler(Links_Update);
            Links.GetFocused += new LinkGetFocusedHandler(Links_GetFocused);

            SelectedLink = Core.LocalDhtID;

            SelectedItemChanged += new EventHandler(LinkTree_SelectedItemChanged);
            NodeExpanding += new EventHandler(LinkTree_NodeExpanding);
            NodeCollapsed += new EventHandler(LinkTree_NodeCollapsed);
        }

        internal void Fin()
        {
            SelectedItemChanged -= new EventHandler(LinkTree_SelectedItemChanged);
            NodeExpanding -= new EventHandler(LinkTree_NodeExpanding);
            NodeCollapsed -= new EventHandler(LinkTree_NodeCollapsed);

            Core.Locations.GuiUpdate -= new LocationGuiUpdateHandler(Locations_Update);
            Links.GuiUpdate -= new LinkGuiUpdateHandler(Links_Update);
            Links.GetFocused -= new LinkGetFocusedHandler(Links_GetFocused);
        }

        private void SetupOperationTree()
        {
            BeginUpdate();

            NodeMap.Clear();
            Nodes.Clear();

            // white space
            if(FirstLineBlank)
                Nodes.Add(new LabelNode(""));

            if (!Links.ProjectRoots.ContainsKey(Project))
            {
                EndUpdate();
                return;
            }

            string rootname = Core.User.Settings.Operation;
            if (Project != 0)
                rootname = Links.ProjectNames[Project];

            // operation
            ProjectNode = new LabelNode(rootname);
            ProjectNode.Font = new System.Drawing.Font("Tahoma", 8.25F, FontStyle.Bold | FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            Nodes.Add(ProjectNode);

            // white space
            Nodes.Add(new LabelNode(""));

            // unlinked
            UnlinkedNode = new LabelNode("Unlinked");
            UnlinkedNode.Font = new System.Drawing.Font("Tahoma", 8.25F, FontStyle.Bold | FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            
            if (!HideUnlinked)
                Nodes.Add(UnlinkedNode);

            // nodes
            List<OpLink> roots = null;

            if (ForceRootID != 0)
            {
                if (Links.LinkMap.ContainsKey(ForceRootID))
                    SetupRoot(Links.LinkMap[ForceRootID]);
            }
            else if (Links.ProjectRoots.ContainsKey(Project))
                lock (Links.ProjectRoots[Project])
                    foreach (OpLink root in Links.ProjectRoots[Project])
                        SetupRoot(root);

            // show unlinked if there's something to show
            if (Nodes.IndexOf(UnlinkedNode) + 1 == Nodes.Count)
                UnlinkedNode.Text = "";
            else
                UnlinkedNode.Text = "Unlinked";

            EndUpdate();
        }

        private void SetupRoot(OpLink root)
        {
            LinkNode node = CreateNode(root);
            LoadRoot(node);

            List<ulong> uplinks = Links.GetUplinkIDs(Core.LocalDhtID, Project);
            uplinks.Add(Core.LocalDhtID);

            ExpandPath(node, uplinks);

            node.Expand(); // expand first level of roots regardless
        }

        private void LoadRoot(LinkNode node)
        {
            // set up root with hidden subs
            LoadNode(node);

            if (node.Nodes.Count == 0)
                InsertRootNode(UnlinkedNode, node);
            else
                InsertRootNode(ProjectNode, node);
        }

        private void LoadNode(LinkNode node)
        {
            // check if already loaded
            if (node.AddSubs)
                return;


            node.AddSubs = true;

            // go through downlinks
            if (node.Link.Downlinks.ContainsKey(Project))
                foreach (OpLink link in node.Link.Downlinks[Project])
                {
                    // if doesnt exist search for it
                    if (!link.Loaded)
                    {
                        Links.Research(link.DhtID, Project, false);
                        continue;
                    }

                    Utilities.InsertSubNode(node, CreateNode(link));
                }
        }

        internal void InsertRootNode(LabelNode start, LinkNode node)
        {
            // inserts item directly under start, not as a child node

            int index = 0;


            TreeListNode root = start.Parent;
            node.Section = start;
            bool ready = false;

            foreach (TreeListNode entry in root.Nodes)
            {
                LinkNode compare = entry as LinkNode;

                if (ready)
                    if ((start == ProjectNode && compare != null && node.Link.Downlinks[Project].Count > compare.Link.Downlinks[Project].Count) ||
                        (start == UnlinkedNode && string.Compare(node.Text, entry.Text, true) < 0) ||
                        entry.GetType() == typeof(LabelNode)) // lower bounds
                    {
                        root.Nodes.Insert(index, node);
                        return;
                    }

                if (entry == start)
                    ready = true;

                index++;
            }

            root.Nodes.Insert(index, node);
        }

        private void ExpandPath(LinkNode node, List<ulong> uplinks)
        {
            if (!uplinks.Contains(node.Link.DhtID))
                return;

            // expand triggers even loading nodes two levels down, one level shown, the other hidden
            node.Expand();

            foreach (LinkNode sub in node.Nodes)
                ExpandPath(sub, uplinks);
        }

        private void VisiblePath(LinkNode node, List<ulong> uplinks)
        {
            bool found = false;

            foreach (LinkNode sub in node.Nodes)
                if (uplinks.Contains(sub.Link.DhtID))
                    found = true;

            if (found)
            {
                node.Expand();

                foreach (LinkNode sub in node.Nodes)
                    VisiblePath(sub, uplinks);
            }
        }

        private LinkNode CreateNode(OpLink link)
        {
            LinkNode node = new LinkNode(link, this, CommandTreeMode.Operation);

            NodeMap[link.DhtID] = node;

            return node;
        }

        private void SetupOnlineTree()
        {
            BeginUpdate();

            NodeMap.Clear();
            Nodes.Clear();

            // white space
            if(FirstLineBlank)
                Nodes.Add(new LabelNode(""));

            // operation
            OnlineNode = new LabelNode("People");
            OnlineNode.Font = new System.Drawing.Font("Tahoma", 8.25F, FontStyle.Bold | FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            Nodes.Add(OnlineNode);

            // add nodes
            lock (Links.LinkMap)
                foreach (ulong key in Links.LinkMap.Keys)
                    OnUpdateLink(key);

            EndUpdate();

        }

        void RecurseUpdate(OpLink link)
        {
            OnUpdateLink(link.DhtID);

            if (link.Downlinks.ContainsKey(Project))
                foreach (OpLink downlink in link.Downlinks[Project])
                    RecurseUpdate(downlink);
        }


        List<ulong> Links_GetFocused()
        {
            List<ulong> focused = new List<ulong>();

            foreach (TreeListNode item in Nodes)
                RecurseFocus(item, focused);

            return focused;
        }

        void RecurseFocus(TreeListNode parent, List<ulong> focused)
        {
            // add parent to focus list
            if (parent.GetType() == typeof(LinkNode))
                focused.Add(((LinkNode)parent).Link.DhtID);

            // iterate through sub items
            foreach (TreeListNode subitem in parent.Nodes)
                if (parent.GetType() == typeof(LinkNode))
                    RecurseFocus(subitem, focused);
        }

        void Links_Update(ulong key)
        {
            OnUpdateLink(key);
        }

        void Locations_Update(ulong key)
        {
            OnUpdateLink(key);
        }

        void OnUpdateLink(ulong key)
        {
            // check if removed
            if (!Links.LinkMap.ContainsKey(key))
            {
                if (NodeMap.ContainsKey(key))
                    RemoveNode(NodeMap[key]);

                return;
            }

            // update
            OpLink link = Links.LinkMap[key];

            if (!link.Loaded)
                return;

            if (!link.Projects.Contains(Project) && !link.Downlinks.ContainsKey(Project))
            {
                if (NodeMap.ContainsKey(key))
                    RemoveNode(NodeMap[key]);

                return;
            }

            if (ForceRootID != 0)
            {
                // root must be a parent of the updating node
                if (link.DhtID != ForceRootID || !Links.IsHigher(link.DhtID, ForceRootID, Project))
                    return;
            }

            LinkNode node = null;

            if (NodeMap.ContainsKey(key))
                node = NodeMap[key];
            else
                node = new LinkNode(link, this, TreeMode);


            if (TreeMode == CommandTreeMode.Operation)
                UpdateOperation(node);

            else if (TreeMode == CommandTreeMode.Online)
                UpdateOnline(node);
        }

        private void UpdateOperation(LinkNode node)
        {
            OpLink link = node.Link;

            TreeListNode parent = null;

            OpLink uplink = link.GetHigher(Project);

            if (uplink == null)
                parent = virtualParent;

            else if (NodeMap.ContainsKey(uplink.DhtID))
                parent = NodeMap[uplink.DhtID];

            // else branch this link is apart of is not visible in current display

            // self is changing ensure it's visible 
            if (node.Link.DhtID == Core.LocalDhtID)
            {
                if (parent == null)
                {
                    List<ulong> uplinks = Links.GetUplinkIDs(Core.LocalDhtID, Project);
                    uplinks.Add(Core.LocalDhtID);

                    ExpandPath(node, uplinks);

                    // check nodeMap again now that highers added
                    if (NodeMap.ContainsKey(uplink.DhtID))
                        parent = NodeMap[uplink.DhtID];
                }

                if (parent != null)
                    parent.Expand();
            }


            // remember settings
            bool selected = node.Selected;
            bool expanded = node.IsExpanded;
            bool loadsubs = node.AddSubs;


            // update parent node
            if (node.Parent != parent)
            {
                List<ulong> visible = new List<ulong>();

                // remove previous instance of node
                if (node.Parent != null)
                {
                    if (node.IsVisible())
                        visible.Add(link.DhtID);

                    LinkNode oldParent = node.Parent as LinkNode;

                    UnloadNode(node, visible);
                    NodeMap.Remove(link.DhtID);
                    node.Remove();

                    // check if should be moved to unlinked
                    if (oldParent != null && oldParent.Section == ProjectNode && oldParent.Nodes.Count == 0)
                    {
                        RemoveNode(oldParent);
                        LoadRoot(CreateNode(oldParent.Link)); // unlink not removing from parents downlinks
                    }
                }

                if (parent == null)
                    return;

                // if new parent is hidden, dont bother adding till user expands
                LinkNode newParent = parent as LinkNode; // null if root

                if (newParent != null && newParent.AddSubs == false)
                    return;


                // copy node to start fresh
                LinkNode newNode = CreateNode(node.Link);


                // check if parent should be moved to project header
                if (newParent != null)
                    if (newParent.Section == UnlinkedNode)
                    {
                        RemoveNode(newParent);
                        newParent = CreateNode(newParent.Link);
                        LoadRoot(newParent);
                    }
                    else
                        Utilities.InsertSubNode(newParent, newNode);


                // if node itself is the root
                if (newParent == null)
                    LoadRoot(newNode);


                if (loadsubs) // if previous node set to add kids
                {
                    LoadNode(newNode);

                    if (expanded) // if previous node set expanded
                        newNode.Expand();
                }

                node = newNode;


                // recurse to each previously visible node
                List<LinkNode> roots = new List<LinkNode>();
                foreach (TreeListNode treeNode in Nodes)
                    if (treeNode.GetType() == typeof(LinkNode))
                        if (((LinkNode)treeNode).Section == ProjectNode)
                            roots.Add(treeNode as LinkNode);

                foreach (ulong id in visible)
                {
                    List<ulong> uplinks = Links.GetUplinkIDs(id, Project);

                    foreach (LinkNode root in roots)
                        VisiblePath(root, uplinks);
                }

                // show unlinked if there's something to show
                if (Nodes.IndexOf(UnlinkedNode) + 1 == Nodes.Count)
                    UnlinkedNode.Text = "";
                else
                    UnlinkedNode.Text = "Unlinked";
            }

            node.UpdateColor();
            node.UpdateName(CommandTreeMode.Operation, Project);

            if (selected)
            {
                node.Selected = true;
                
                //CRIT!!!!! *****
                //UpdateCommandPanel();
            }

            Invalidate();
        }

        private void RemoveNode(LinkNode node)
        {
            UnloadNode(node, null); // unload subs
            NodeMap.Remove(node.Link.DhtID); // remove from map
            node.Remove(); // remove from tree
        }

        private void UpdateOnline(LinkNode node)
        {
            // if node offline, remove
            if (!Core.Locations.LocationMap.ContainsKey(node.Link.DhtID))
            {
                if (NodeMap.ContainsKey(node.Link.DhtID))
                {
                    node.Remove();
                    NodeMap.Remove(node.Link.DhtID);
                }
            }

            // if node online, add if not already there
            else
            {
                if (node.Parent == null)
                {
                    NodeMap[node.Link.DhtID] = node;

                    Utilities.InsertSubNode(OnlineNode, node);
                    OnlineNode.Expand();
                }

                node.UpdateName(CommandTreeMode.Online, Project);
            }
        }

        void ParentCheck(TreeListNode node)
        {
            if (node == null)
                return;

            if (node.GetType() != typeof(LinkNode))
                return;

            LinkNode item = (LinkNode)node;

            TreeListNode newParent = null;
            bool expand = false;

            if (item.Parent == ProjectNode && !item.Link.Downlinks.ContainsKey(Project))
                newParent = UnlinkedNode;

            if (item.Parent == UnlinkedNode && item.Link.Downlinks.ContainsKey(Project))
            {
                newParent = ProjectNode;
                expand = true;
            }

            if (newParent != null)
            {
                item.Remove();

                Utilities.InsertSubNode(newParent, item);

                if (expand)
                    newParent.Expand();
            }
        }

        void LinkTree_NodeExpanding(object sender, EventArgs e)
        {
            LinkNode node = sender as LinkNode;

            if (node == null)
                return;

            Debug.Assert(node.AddSubs);

            // node now expanded, get next level below children
            foreach (LinkNode child in node.Nodes)
                LoadNode(child);
        }

        void LinkTree_NodeCollapsed(object sender, EventArgs e)
        {
            LinkNode node = sender as LinkNode;

            if (node == null)
                return;

            if (!node.AddSubs) // this node is already collapsed
                return;

            // remove nodes 2 levels down
            foreach (LinkNode child in node.Nodes)
                UnloadNode(child, null);

            Debug.Assert(node.AddSubs); // this is the top level, children hidden underneath
        }

        private void UnloadNode(LinkNode node, List<ulong> visible)
        {
            node.AddSubs = false;

            if (visible != null && node.IsVisible())
                visible.Add(node.Link.DhtID);

            // for each child, call unload node, then clear
            foreach (LinkNode child in node.Nodes)
            {
                if (NodeMap.ContainsKey(child.Link.DhtID))
                    NodeMap.Remove(child.Link.DhtID);

                UnloadNode(child, visible);
            }

            // unloads children of node, not the node itself
            node.Nodes.Clear();
            node.Collapse();
        }

        internal void SelectLink(ulong id)
        {
            if (!Links.LinkMap.ContainsKey(id))
                id = Core.LocalDhtID;

            // unbold current
            if (NodeMap.ContainsKey(SelectedLink))
                NodeMap[SelectedLink].Font = new System.Drawing.Font("Tahoma", 8.25F);

            // bold new and set
            SelectedLink = id;

            if (NodeMap.ContainsKey(id))
            {
                NodeMap[id].Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                NodeMap[id].EnsureVisible();
            }

            Invalidate();
        }

        internal void ShowProject(uint project)
        {
            LinkNode item = GetSelected();
               
            TreeMode = CommandTreeMode.Operation;
            Project = project;
            SetupOperationTree();

            if (item != null && NodeMap.ContainsKey(item.Link.DhtID))
                NodeMap[item.Link.DhtID].Selected = true;
        }

        internal void ShowOnline()
        {
            LinkNode item = GetSelected();
            
            TreeMode = CommandTreeMode.Online;
            SetupOnlineTree();

            if (item != null && NodeMap.ContainsKey(item.Link.DhtID))
                NodeMap[item.Link.DhtID].Selected = true;
        }


        LinkNode GetSelected()
        {
            if (SelectedNodes.Count == 0)
                return null;

            TreeListNode node = SelectedNodes[0];

            if (node.GetType() != typeof(LinkNode))
                return null;

            return (LinkNode)node;
        }

        void LinkTree_SelectedItemChanged(object sender, EventArgs e)
        {
            foreach (TreeListNode node in SelectedNodes)
                if (node.GetType() != typeof(LinkNode))
                    node.Selected = false;
                else
                {
                    LinkNode item = node as LinkNode;
                    Links.Research(item.Link.DhtID, Project, true);
                    Core.Locations.Research(item.Link.DhtID);
                }
        }

        internal List<ulong> GetSelectedIDs()
        {
            List<ulong> selected = new List<ulong>();

            foreach (TreeListNode node in SelectedNodes)
                if (node.GetType() == typeof(LinkNode))
                    selected.Add(((LinkNode)node).Link.DhtID);

            return selected;
        }
    }


    internal class LabelNode : TreeListNode
    {
        internal LabelNode(string text)
        {
            Text = text;
        }
    }

    internal class LinkNode : TreeListNode
    {
        internal OpLink Link;
        internal LinkControl Links;
        internal LocationControl Locations;

        internal bool AddSubs;
        internal LabelNode Section;

        static Color DarkDarkGray = Color.FromArgb(96, 96, 96);


        internal LinkNode(OpLink link, LinkTree main, CommandTreeMode mode)
        {
            Link = link;
            Links = main.Links;
            Locations = main.Core.Locations;

            UpdateName(mode, 0);

            if (main.SelectedLink == Link.DhtID)
                Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        }

        internal void UpdateName(CommandTreeMode mode, uint proj)
        {
            string txt = "";

            string title = "";
            if (Link.Title.ContainsKey(proj) && Link.Title[proj] != "")
                title = Link.Title[proj];

            if (mode == CommandTreeMode.Operation)
            {
                txt += Link.Name;

                //if (title != "")
                //    txt += " - " + title;

                if (Link.Error != null && Link.Error != "")
                    txt += " (Error " + Link.Error + ")";

                else if (Link.Uplink.ContainsKey(proj))
                {
                    bool confirmed = false;
                    bool requested = false;

                    if (Link.Uplink[proj].Confirmed.ContainsKey(proj))
                        if (Link.Uplink[proj].Confirmed[proj].Contains(Link.DhtID))
                            confirmed = true;

                    if (Link.Uplink[proj].Requests.ContainsKey(proj))
                        foreach (UplinkRequest request in Link.Uplink[proj].Requests[proj])
                            if (request.KeyID == Link.DhtID)
                                requested = true;

                    if (confirmed)
                    { }
                    else if (requested)
                        txt += " (Link Pending)";
                    else
                        txt += " (Link Unconfirmed)";
                }

                else if (!Link.Projects.Contains(proj))
                {
                    txt += " (Left Project)";
                }
            }
            else
            {
                txt += Link.Name;

                // if (title != "")
                //     txt += " - " + title;
            }

            txt += "     ";

            if (Text != txt)
                Text = txt;
        }

        internal void UpdateColor()
        {
            Color newColor = Color.Black;

            if (Link == Links.LocalLink || Locations.LocationMap.ContainsKey(Link.DhtID))
                newColor = Color.Black;
            else
                newColor = Color.DarkGray;

            if (newColor != ForeColor)
                ForeColor = newColor;
        }

        public override string ToString()
        {
            return Text;
        }
    }

}
