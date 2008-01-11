using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;

using RiseOp.Implementation;
using RiseOp.Services.Location;
using RiseOp.Interface.TLVex;

namespace RiseOp.Services.Trust
{
    internal enum CommandTreeMode { Operation, Online };

    class LinkTree : TreeListViewEx 
    {
        internal OpCore Core;
        internal TrustService Links;

        internal LabelNode ProjectNode;
        internal LabelNode UnlinkedNode;
        internal LabelNode OnlineNode;


        Dictionary<ulong, LinkNode> NodeMap = new Dictionary<ulong, LinkNode>();

        internal ulong SelectedLink;
        internal uint SelectedProject;

        internal ulong ForceRootID;
        internal bool HideUnlinked;

        internal CommandTreeMode TreeMode;
        internal uint Project;

        internal bool FirstLineBlank = true;


        internal LinkTree()
        {
            HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
        }

        internal void Init(TrustService links)
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

        private void RefreshOperationTree()
        {
            BeginUpdate();

            // save selected
            LinkNode selected = GetSelected();

            // save visible while unloading
            List<ulong> visible = new List<ulong>();
            foreach (TreeListNode node in Nodes)
                if (node.GetType() == typeof(LinkNode))
                    UnloadNode((LinkNode)node, visible);

            NodeMap.Clear();
            Nodes.Clear();

            // white space
            if(FirstLineBlank)
                Nodes.Add(new LabelNode(""));

            if (!Links.ProjectRoots.SafeContainsKey(Project))
            {
                EndUpdate();
                return;
            }

            string rootname = Core.User.Settings.Operation;
            if (Project != 0)
                rootname = Links.GetProjectName(Project);

            // operation
            ProjectNode = new LabelNode(rootname);
            ProjectNode.Font = new System.Drawing.Font("Tahoma", 8.25F, FontStyle.Bold | FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            Nodes.Add(ProjectNode);

            // white space
            Nodes.Add(new LabelNode(""));

            // unlinked
            UnlinkedNode = new LabelNode("Untrusted");
            UnlinkedNode.Font = new System.Drawing.Font("Tahoma", 8.25F, FontStyle.Bold | FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            
            Nodes.Add(UnlinkedNode);


            // if forced, load specific node as root
            if (ForceRootID != 0)
            {
                OpLink root = Links.GetLink(ForceRootID, Project);

                if (root != null)
                    SetupRoot(root);
            }

            // get roots for specific project
            else
            {
                List<OpLink> roots = null;
                if (Links.ProjectRoots.SafeTryGetValue(Project, out roots))
                    foreach (OpLink root in roots)
                        SetupRoot(root);
            }

            // show unlinked if there's something to show
            if (Nodes.IndexOf(UnlinkedNode) + 1 == Nodes.Count)
                UnlinkedNode.Text = "";
            else
                UnlinkedNode.Text = "Untrusted";

            // restore visible
            foreach (ulong id in visible)
                foreach (TreeListNode node in Nodes)
                    if (node.GetType() == typeof(LinkNode))
                    {
                        List<ulong> uplinks = Links.GetUnconfirmedUplinkIDs(id, Project);
                        uplinks.Add(id);
                        VisiblePath((LinkNode)node, uplinks);
                    }
            
            // restore selected
            if (selected != null)
                if (NodeMap.ContainsKey(selected.Link.DhtID))
                    Select(NodeMap[selected.Link.DhtID]);

            EndUpdate();
        }

        private void SetupRoot(OpLink root)
        {
            LinkNode node = CreateNode(root);
            LoadRoot(node);

            List<ulong> uplinks = Links.GetUnconfirmedUplinkIDs(Core.LocalDhtID, Project);
            uplinks.Add(Core.LocalDhtID);

            ExpandPath(node, uplinks);

            node.Expand(); // expand first level of roots regardless
        }

        private void LoadRoot(LinkNode node)
        {
            // set up root with hidden subs
            LoadNode(node);

            // if self or uplinks contains root, put in project
            if(node.Link.DhtID == Core.LocalDhtID || 
                Links.IsUnconfirmedHigher(node.Link.DhtID, Project))
                InsertRootNode(ProjectNode, node);

            // else put in untrusted
            else if (!HideUnlinked)
                InsertRootNode(UnlinkedNode, node);

        }

        private void LoadNode(LinkNode node)
        {
            // check if already loaded
            if (node.AddSubs)
                return;

            node.AddSubs = true;

            // go through downlinks
            foreach (OpLink link in node.Link.Downlinks)
                if (!node.Link.IsLoopedTo(link))
                {
                    // if doesnt exist search for it
                    if (!link.Trust.Loaded)
                    {
                        Links.Research(link.DhtID, Project, false);
                        continue;
                    }

                    //if(node.Link.IsLoopRoot)
                    //    node.Nodes.Insert(0, CreateNode(link));
                    //else
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
                if (ready)
                    if (start == ProjectNode ||
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
            Links.TrustMap.LockReading(delegate()
            {
                foreach (ulong key in Links.TrustMap.Keys)
                    OnUpdateLink(key);
            });

            EndUpdate();

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
            // update
            OpLink link = Links.GetLink(key, Project);

            if (link == null)
            {
                if (NodeMap.ContainsKey(key))
                    RemoveNode(NodeMap[key]);

                return;
            }

            /* taken care of above
             * if (!link.Projects.Contains(Project) && !link.Downlinks.ContainsKey(Project))
            {
                if (NodeMap.ContainsKey(key))
                    RemoveNode(NodeMap[key]);

                return;
            }*/

            if (ForceRootID != 0)
            {
                // root must be a parent of the updating node
                if (link.DhtID != ForceRootID && !Links.IsUnconfirmedHigher(link.DhtID, ForceRootID, Project))
                    return;
            }

            LinkNode node = null;

            if (TreeMode == CommandTreeMode.Operation)
            {
                if (NodeMap.ContainsKey(key))
                    node = NodeMap[key];

                TreeListNode parent = null;
                OpLink uplink = GetTreeHigher(link);

                if (uplink == null)
                    parent = virtualParent;

                else if (NodeMap.ContainsKey(uplink.DhtID))
                    parent = NodeMap[uplink.DhtID];

                else if (uplink.IsLoopRoot)
                    parent = new TreeListNode(); // ensures that tree is refreshed

                // if nodes status unchanged
                if (node != null && parent != null && node.Parent == parent)
                {
                    node.UpdateName(CommandTreeMode.Operation);
                    Invalidate();
                    return;
                }

                // only if parent is visible
                if(parent != null)
                    RefreshOperationTree();
            }

            else if (TreeMode == CommandTreeMode.Online)
            {
                

                if (NodeMap.ContainsKey(key))
                    node = NodeMap[key];
                else
                    node = new LinkNode(link, this, TreeMode);


                UpdateOnline(node);
            }
        }

        private void UpdateOperation(LinkNode node)
        {
            OpLink link = node.Link;

            TreeListNode parent = null;

            OpLink uplink = GetTreeHigher(link);

            if (uplink == null)
                parent = virtualParent;

            else if (NodeMap.ContainsKey(uplink.DhtID))
                parent = NodeMap[uplink.DhtID];

            else if (uplink.IsLoopRoot)
            {
                parent = CreateNode(uplink);
                LoadRoot((LinkNode)parent);
            }

            // else branch this link is apart of is not visible in current display
            

            // self is changing ensure it's visible 
            if (node.Link.DhtID == Core.LocalDhtID)
            {
                if (parent == null)
                {
                    List<ulong> uplinks = Links.GetUnconfirmedUplinkIDs(Core.LocalDhtID, Project);
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

                    LinkNode unload = (oldParent != null && oldParent.Link.IsLoopRoot) ? oldParent : node;
                    
                    // if old parent is a loop node, the loop is made obsolete by change
                    UnloadNode(unload, visible);
                    unload.Remove();
                }

                if (parent == null)
                    return;

                // if new parent is hidden, dont bother adding till user expands
                LinkNode newParent = parent as LinkNode; // null if virtual parent (root)

                if (newParent != null && newParent.AddSubs == false)
                    return;


                // copy node to start fresh
                LinkNode newNode = CreateNode(node.Link);

                if (newParent != null)
                    Utilities.InsertSubNode(newParent, newNode);
                else
                    LoadRoot(newNode);


                ArrangeRoots();

                // arrange nodes can cause newNode to become invalid, retrieve updated copy
                if(!NodeMap.ContainsKey(link.DhtID))
                    return;

                newNode = NodeMap[link.DhtID];
     
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
                    List<ulong> uplinks = Links.GetUnconfirmedUplinkIDs(id, Project);

                    foreach (LinkNode root in roots)
                        VisiblePath(root, uplinks);
                }

                // show unlinked if there's something to show
                if (Nodes.IndexOf(UnlinkedNode) + 1 == Nodes.Count)
                    UnlinkedNode.Text = "";
                else
                    UnlinkedNode.Text = "Untrusted";
            }

            node.UpdateColor();
            node.UpdateName(CommandTreeMode.Operation);

            if (selected)
                node.Selected = true;

            Invalidate();
        }

        private OpLink GetTreeHigher(OpLink link)
        {
            if (link.LoopRoot != null)
                return link.LoopRoot;

            return link.GetHigher(false);
        }

        private void ArrangeRoots()
        {
            List<ulong> uplinks = Links.GetUnconfirmedUplinkIDs(Core.LocalDhtID, Project);
            uplinks.Add(Core.LocalDhtID);

            OpLink highest = Links.GetLink(uplinks[uplinks.Count - 1], Project);
            if (highest.LoopRoot != null)
                uplinks.Add(highest.LoopRoot.DhtID);

            List<LinkNode> makeUntrusted = new List<LinkNode>();
            List<LinkNode> makeProject = new List<LinkNode>();

            // look for nodes to switch
            foreach (TreeListNode entry in Nodes)
            {
                LinkNode node = entry as LinkNode;

                if (node == null) 
                    continue;


                if(entry == ProjectNode && !uplinks.Contains(node.Link.DhtID))
                    makeUntrusted.Add(node);

                else if(entry == UnlinkedNode && uplinks.Contains(node.Link.DhtID))
                    makeProject.Add(node);
            }

            // remove, recreate, insert, expand root, expand to self
            foreach (LinkNode delNode in makeUntrusted)
            {
                RemoveNode(delNode); 

                if (HideUnlinked)
                    continue;

                LinkNode node = CreateNode(delNode.Link);
                LoadNode(node);
                InsertRootNode(UnlinkedNode, node);
                node.Expand();
            }

            Debug.Assert(makeProject.Count <= 1);

            foreach (LinkNode delNode in makeProject)
            {
                RemoveNode(delNode);
                LinkNode node = CreateNode(delNode.Link);
                LoadNode(node);
                InsertRootNode(ProjectNode, node);

                node.Expand();
                ExpandPath(node, uplinks);
            }

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
            if (!Core.Locations.LocationMap.SafeContainsKey(node.Link.DhtID))
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

                node.UpdateName(CommandTreeMode.Online);
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

            if (NodeMap.ContainsKey(node.Link.DhtID))
                NodeMap.Remove(node.Link.DhtID);
            
            // for each child, call unload node, then clear
            foreach (LinkNode child in node.Nodes)
                UnloadNode(child, visible);

            // unloads children of node, not the node itself
            node.Nodes.Clear();
            node.Collapse();
        }

        internal void SelectLink(ulong id, uint project)
        {
            if (!Links.TrustMap.SafeContainsKey(id))
                id = Core.LocalDhtID;

            // unbold current
            if (NodeMap.ContainsKey(SelectedLink))
                NodeMap[SelectedLink].Font = new System.Drawing.Font("Tahoma", 8.25F);

            // bold new and set
            SelectedLink = id;
            SelectedProject = project;

            if (NodeMap.ContainsKey(id) && Project == SelectedProject)
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
            RefreshOperationTree();

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
                    if( !((LinkNode)node).Link.IsLoopRoot )
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
        internal TrustService Links;
        internal LocationService Locations;

        internal bool AddSubs;
        internal LabelNode Section;

        static Color DarkDarkGray = Color.FromArgb(96, 96, 96);


        internal LinkNode(OpLink link, LinkTree main, CommandTreeMode mode)
        {
            Link = link;
            Links = main.Links;
            Locations = main.Core.Locations;

            UpdateName(mode);

            if (main.SelectedLink == Link.DhtID && main.Project == main.SelectedProject)
                Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        }

        internal void UpdateName(CommandTreeMode mode)
        {
            string txt = "";

            string title = Link.Title;

            if (mode == CommandTreeMode.Operation)
            {
                txt += Links.GetName(Link.DhtID);

                //if (title != "")
                //    txt += " - " + title;

                if (Link.IsLoopRoot)
                    txt = "Trust Loop";

                OpLink parent = Link.GetHigher(false);

                if (parent != null)
                {
                    bool confirmed = false;
                    bool requested = false;

                    if (parent.Confirmed.Contains(Link.DhtID))
                        confirmed = true;

                    foreach (UplinkRequest request in parent.Requests)
                        if (request.KeyID == Link.DhtID)
                            requested = true;

                    if (confirmed)
                    { }
                    else if (requested && parent.DhtID == Links.Core.LocalDhtID)
                        txt += " (Accept Trust?)";
                    else if (requested)
                        txt += " (Trust Requested)";
                    else if(parent.DhtID == Links.Core.LocalDhtID)
                        txt += " (Trust Denied)";
                    else
                        txt += " (Trust Unconfirmed)";
                }

                else if (!Link.Active)
                {
                    txt += " (Left Project)";
                }
            }
            else
            {
                txt += Links.GetName(Link.DhtID);

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

            if (Link.DhtID == Links.Core.LocalDhtID || Locations.LocationMap.SafeContainsKey(Link.DhtID))
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
