using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

using DeOps.Implementation;
using DeOps.Implementation.Dht;

using DeOps.Components;
using DeOps.Components.Link;
using DeOps.Components.IM;
using DeOps.Components.Location;

using DeOps.Interface.TLVex;


namespace DeOps.Interface
{
    internal enum CommandTreeMode { Operation, Online };

    internal delegate void ShowExternalHandler(ViewShell view);
    internal delegate void ShowInternalHandler(ViewShell view);


    internal partial class MainForm : Form
    {
        internal OpCore Core;
        DhtStore Store;
        internal LinkControl Links;

        internal ShowExternalHandler ShowExternal;
        internal ShowInternalHandler ShowInternal;

        LabelNode ProjectNode;
        LabelNode UnlinkedNode;
        LabelNode OnlineNode;

        Dictionary<ulong, LinkNode> NodeMap = new Dictionary<ulong, LinkNode>();

        internal ulong SelectedLink;

        CommandTreeMode TreeMode;

        uint ProjectID;
        ToolStripButton ProjectButton;
        uint ProjectButtonID;

        internal List<ExternalView> ExternalViews = new List<ExternalView>();


        internal MainForm(OpCore core)
        {
            InitializeComponent();

            Core = core;
            Store = Core.OperationNet.Store;
            Links = Core.Links;

            ShowExternal += new ShowExternalHandler(OnShowExternal);
            ShowInternal += new ShowInternalHandler(OnShowInternal);

            Core.Locations.GuiUpdate += new LocationGuiUpdateHandler(Locations_Update);
            Links.GuiUpdate  += new LinkGuiUpdateHandler(Links_Update);
            Links.GetFocused += new LinkGetFocusedHandler(Links_GetFocused);

            SelectedLink = Core.LocalDhtID;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Text = Core.User.Settings.Operation + " - " + Core.User.Settings.ScreenName;

            CurrentViewLabel.Text = "";

            CommandTree.NodeExpanding += new EventHandler(CommandTree_NodeExpanding);
            CommandTree.NodeCollapsed += new EventHandler(CommandTree_NodeCollapsed);

            SetupOperationTree();
            
            OnSelectChange(Links.LocalLink);
            UpdateCommandPanel();
        }

        private void InviteMenuItem_Click(object sender, EventArgs e)
        {
            InviteForm form = new InviteForm(Core);
            form.ShowDialog(this);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Trace.WriteLine("Main Closing " + Thread.CurrentThread.ManagedThreadId.ToString());

            while (ExternalViews.Count > 0)
                if( !ExternalViews[0].SafeClose())
                {
                    e.Cancel = true;
                    return;
                }

            if (!CleanInternal())
            {
                e.Cancel = true;
                return;
            }

            CommandTree.NodeExpanding -= new EventHandler(CommandTree_NodeExpanding);
            CommandTree.NodeCollapsed -= new EventHandler(CommandTree_NodeCollapsed);

            ShowExternal -= new ShowExternalHandler(OnShowExternal);
            ShowInternal -= new ShowInternalHandler(OnShowInternal);

            Core.Locations.GuiUpdate -= new LocationGuiUpdateHandler(Locations_Update);
            Links.GuiUpdate -= new LinkGuiUpdateHandler(Links_Update);
            Links.GetFocused -= new LinkGetFocusedHandler(Links_GetFocused);

            foreach (OpComponent component in Core.Components.Values)
                component.GuiClosing();

            if (Core.Sim == null)
                Application.Exit();
            else
                Core.GuiMain = null;
        }

        private bool CleanInternal()
        {
            foreach (Control item in InternalView.Controls)
                if (item is ViewShell)
                {
                    if (!((ViewShell)item).Fin())
                        return false;

                    item.Dispose();
                }

            InternalView.Controls.Clear();
            return true;
        }

        void OnShowExternal(ViewShell view)
        {
            ExternalView external = new ExternalView(this, view);

            ExternalViews.Add(external);

            external.Show();
        }

        void OnShowInternal(ViewShell view)
        {
            if (!CleanInternal())
                return;

            view.Dock = DockStyle.Fill;

            InternalView.Controls.Add(view);

            CurrentViewLabel.Text = " " + view.GetTitle();

            view.Init();
        }

        private void PopoutButton_Click(object sender, EventArgs e)
        {
            ViewShell view = (ViewShell) InternalView.Controls[0];

            InternalView.Controls.Clear();

            OnShowExternal(view);

            if (Links.LinkMap.ContainsKey(SelectedLink))
                OnSelectChange(Links.LinkMap[SelectedLink]);
            else
                OnSelectChange(Links.LocalLink);
        }

        private void SetupOperationTree()
        {
            CommandTree.BeginUpdate();

            NodeMap.Clear();
            CommandTree.Nodes.Clear();

            // white space
            CommandTree.Nodes.Add( new LabelNode(""));

            if (!Links.ProjectRoots.ContainsKey(ProjectID))
            {
                OperationButton.Checked = true;
                SideToolStrip.Items.Remove(ProjectButton);
                ProjectButton = null;

                CommandTree.EndUpdate();
                return;
            }

            string rootname = Core.User.Settings.Operation;
            if (ProjectID != 0)
                rootname = Links.ProjectNames[ProjectID];

            // operation
            ProjectNode = new LabelNode(rootname);
            ProjectNode.Font = new System.Drawing.Font("Tahoma", 8.25F, FontStyle.Bold | FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            CommandTree.Nodes.Add(ProjectNode);

            // white space
            CommandTree.Nodes.Add(new LabelNode(""));

            // unlinked
            UnlinkedNode = new LabelNode("Unlinked");
            UnlinkedNode.Font = new System.Drawing.Font("Tahoma", 8.25F, FontStyle.Bold | FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            CommandTree.Nodes.Add(UnlinkedNode);

            // nodes
            if (Links.ProjectRoots.ContainsKey(ProjectID))
                lock (Links.ProjectRoots[ProjectID])
                    foreach (OpLink root in Links.ProjectRoots[ProjectID])
                    {
                        LinkNode node = CreateNode(root);
                        LoadRoot(node);
                        
                        List<ulong> uplinks = Links.GetUplinkIDs(Core.LocalDhtID, ProjectID);
                        uplinks.Add(Core.LocalDhtID);

                        ExpandPath(node, uplinks);

                        node.Expand(); // expand first level of roots regardless
                    }

            CommandTree.EndUpdate();
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
            if (node.Link.Downlinks.ContainsKey(ProjectID))
                foreach (OpLink link in node.Link.Downlinks[ProjectID])
                {
                    // if doesnt exist search for it
                    if (!link.Loaded)
                    {
                        Links.Research(link.DhtID, ProjectID, false);
                        continue;
                    }

                    Utilities.InsertSubNode(node, CreateNode(link));
                }
        }

        internal void InsertRootNode(LabelNode start, LinkNode node)
        {
            int index = 0;

            TreeListNode root = start.Parent;
            node.Section = start;
            bool ready = false;


            foreach (TreeListNode entry in root.Nodes)
            {
                LinkNode compare = entry as LinkNode;

                if (ready)
                    if ((start == ProjectNode && compare != null && node.Link.Downlinks[ProjectID].Count > compare.Link.Downlinks[ProjectID].Count) ||
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
            CommandTree.BeginUpdate();

            NodeMap.Clear();
            CommandTree.Nodes.Clear();

            // white space
            CommandTree.Nodes.Add(new LabelNode(""));

            // operation
            OnlineNode = new LabelNode("People");
            OnlineNode.Font = new System.Drawing.Font("Tahoma", 8.25F, FontStyle.Bold | FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            CommandTree.Nodes.Add(OnlineNode);

            // add nodes
            lock (Links.LinkMap)
                foreach (ulong key in Links.LinkMap.Keys)
                    OnUpdateLink(key);

            CommandTree.EndUpdate();

        }

        void RecurseUpdate(OpLink link)
        {
            OnUpdateLink(link.DhtID);

            if (link.Downlinks.ContainsKey(ProjectID))
                foreach (OpLink downlink in link.Downlinks[ProjectID])
                    RecurseUpdate(downlink);
        }

        List<ulong> Links_GetFocused()
        {
            List<ulong> focused = new List<ulong>();

            foreach (TreeListNode item in CommandTree.Nodes)
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

            if (!Links.ProjectRoots.ContainsKey(ProjectID))
            {
                if (ProjectButton.Checked)
                    OperationButton.Checked = true;

                SideToolStrip.Items.Remove(ProjectButton);
                ProjectButton = null;
            }

            if (!link.Projects.Contains(ProjectID) && !link.Downlinks.ContainsKey(ProjectID))
            {
                if (NodeMap.ContainsKey(key))
                    RemoveNode(NodeMap[key]);

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

            OpLink uplink = link.GetHigher(ProjectID);

            if (uplink == null)
                parent = CommandTree.virtualParent;

            else if (NodeMap.ContainsKey(uplink.DhtID))
                parent = NodeMap[uplink.DhtID];

            // else branch this link is apart of is not visible in current display
            
            // self is changing ensure it's visible 
            if (node.Link.DhtID == Core.LocalDhtID)
            {
                if (parent == null)
                {
                    List<ulong> uplinks = Links.GetUplinkIDs(Core.LocalDhtID, ProjectID);
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
                foreach (TreeListNode treeNode in CommandTree.Nodes)
                    if (treeNode.GetType() == typeof(LinkNode))
                        if (((LinkNode)treeNode).Section == ProjectNode)
                            roots.Add(treeNode as LinkNode);

                foreach (ulong id in visible)
                {
                    List<ulong> uplinks = Links.GetUplinkIDs(id, ProjectID);

                    foreach(LinkNode root in roots)
                        VisiblePath(root, uplinks);
                }
            }

            node.UpdateColor();
            node.UpdateName(CommandTreeMode.Operation, ProjectID);

            if (selected)
            {
                node.Selected = true;
                UpdateCommandPanel();
            }

            CommandTree.Invalidate();
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

                node.UpdateName(CommandTreeMode.Online, ProjectID);
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

            if (item.Parent == ProjectNode && !item.Link.Downlinks.ContainsKey(ProjectID))
                newParent = UnlinkedNode;

            if (item.Parent == UnlinkedNode && item.Link.Downlinks.ContainsKey(ProjectID))
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

        LinkNode GetSelected()
        {
            if (CommandTree.SelectedNodes.Count == 0)
                return null;

            TreeListNode node = CommandTree.SelectedNodes[0];

            if (node.GetType() != typeof(LinkNode))
                return null;

            return (LinkNode)node;
        }


        void UpdateCommandPanel()
        {
            if (GetSelected() == null)
                ShowNetworkStatus();

            else
                ShowNodeStatus();
        }

        private void ShowNetworkStatus()
        {
            string GlobalStatus = "";
            string OpStatus = "";

            if (Core.GlobalNet == null)
                GlobalStatus = "Disconnected";
            else if (Core.GlobalNet.Routing.Responsive())
                GlobalStatus = "Connected";
            else
                GlobalStatus = "Connecting";


            if (Core.OperationNet.Routing.Responsive())
                OpStatus = "Connected";
            else
                OpStatus = "Connecting";

            
            string html = 
             
                @"<html>
                <head>
	                <style type=""text/css"">
	                <!--
	                    body { margin: 0; }
	                    p    { font-size: 8.25pt; font-family: Tahoma }
	                -->
	                </style>
                </head>
                <body bgcolor=WhiteSmoke>
	                <table width=100% cellpadding=4>
	                    <tr><td bgcolor=green><p><b><font color=#ffffff>Network Status</font></b></p></td></tr>
	                </table>
                    <table callpadding=3>    
                        <tr><td><p><b>Global:</b></p></td><td><p>" + GlobalStatus + @"</p></td></tr>
	                    <tr><td><p><b>Network:</b></p></td><td><p>" + OpStatus + @"</p></td></tr>
	                    <tr><td><p><b>Firewall:</b></p></td><td><p>" + Core.Firewall.ToString() + @"</p></td></tr>
                    </table>
                </body>
                </html>
                ";

            // prevents clicking sound when browser navigates
            if (!StatusBrowser.DocumentText.Equals(html))
            {
                StatusBrowser.Hide();
                StatusBrowser.DocumentText = html;
                StatusBrowser.Show();
            }
        }

        private void ShowNodeStatus()
        {
            LinkNode node = GetSelected();

            if (node == null)
            {
                ShowNetworkStatus();
                return;
            }

            OpLink link = node.Link;

            string name = link.Name;
            
            string title = "None";
            if (link.Title.ContainsKey(ProjectID))
                if (link.Title[ProjectID] != "") 
                    title = link.Title[ProjectID];

            string projects = "";
            foreach (uint id in link.Projects)
                if(id != 0)
                    projects += "<a href='project:" + id.ToString() + "'>" + Links.ProjectNames[id] +"</a>, ";
            projects = projects.TrimEnd(new char[] { ' ', ',' });


            string locations = "";
            if (Core.OperationNet.Routing.Responsive())
            {
                if (Core.Locations.LocationMap.ContainsKey(link.DhtID))
                    foreach (LocInfo info in Core.Locations.LocationMap[link.DhtID].Values)
                        if (info.Location.Location == "")
                            locations += "Unknown, ";
                        else
                            locations += info.Location.Location + ", ";
                locations = locations.TrimEnd(new char[] { ' ', ',' });
            }

            string html =
                @"<html>
                <head>
	                <style type=""text/css"">
	                <!--
	                    body { margin: 0 }
	                    p    { font-size: 8.25pt; font-family: Tahoma }
                        A:link {text-decoration: none; color: black}
                        A:visited {text-decoration: none; color: black}
                        A:active {text-decoration: none; color: black}
                        A:hover {text-decoration: underline; color: black}
	                -->
	                </style>
                </head>
                <body bgcolor=WhiteSmoke>
	                <table width=100% cellpadding=4>
	                    <tr><td bgcolor=MediumSlateBlue><p><b><font color=#ffffff>" + link.Name + @"</font></b></p></td></tr>
	                </table>
                    <table callpadding=3>  
                        <tr><td><p><b>Title:</b></p></td><td><p>" + title + @"</p></td></tr>
	                    <tr><td><p><b>Projects:</b></p></td><td><p>" + projects + @"</p></td></tr>";

            if (locations != "")
                html += @"<tr><td><p><b>Locations:</b></p></td><td><p>" + locations + @"</p></td></tr>";
                            
            html += 
                        @"<tr><td><p><b>Last Seen:</b></p></td><td><p></p></td></tr>
                    </table>
                </body>
                </html>";

            //crit show locations local time

            // prevents clicking sound when browser navigates
            if (!StatusBrowser.DocumentText.Equals(html))
            {
                StatusBrowser.Hide();
                StatusBrowser.DocumentText = html;
                StatusBrowser.Show();
            }
        }


        private void CommandTree_MouseClick(object sender, MouseEventArgs e)
        {
            // this gets right click to select item
            TreeListNode clicked = CommandTree.GetNodeAt(e.Location) as TreeListNode;

            if (clicked == null)
                return;

            // project menu
            if (clicked == ProjectNode && e.Button == MouseButtons.Right)
            {
                ContextMenu treeMenu = new ContextMenu();

                treeMenu.MenuItems.Add(new MenuItem("Properties", new EventHandler(OnProjectProperties)));

                if (ProjectID != 0)
                {
                    if (Links.LocalLink.Projects.Contains(ProjectID))
                        treeMenu.MenuItems.Add(new MenuItem("Leave", new EventHandler(OnProjectLeave)));
                    else
                        treeMenu.MenuItems.Add(new MenuItem("Join", new EventHandler(OnProjectJoin)));
                }

                treeMenu.Show(CommandTree, e.Location);

                return;
            }


            if (clicked.GetType() != typeof(LinkNode))
                return;

            LinkNode item = clicked as LinkNode;



            // right click menu
            if (e.Button == MouseButtons.Right)
            {
                // menu
                ContextMenuStrip treeMenu = new ContextMenuStrip();

                // select
                treeMenu.Items.Add("Select", InterfaceRes.star, TreeMenu_Select);

                // views
                List<ToolStripMenuItem> quickMenus = new List<ToolStripMenuItem>();
                List<ToolStripMenuItem> extMenus = new List<ToolStripMenuItem>();

                foreach (OpComponent component in Core.Components.Values)
                {
                    if (component is LinkControl)
                        continue;

                    // quick
                    List<MenuItemInfo> menuList = component.GetMenuInfo(InterfaceMenuType.Quick, item.Link.DhtID, ProjectID);

                    if (menuList != null && menuList.Count > 0)
                        foreach (MenuItemInfo info in menuList)
                            quickMenus.Add(new OpMenuItem(item.Link.DhtID, ProjectID, info.Path, info));

                    // external
                    menuList = component.GetMenuInfo(InterfaceMenuType.External, item.Link.DhtID, ProjectID);

                    if (menuList != null && menuList.Count > 0)
                        foreach (MenuItemInfo info in menuList)
                            extMenus.Add(new OpMenuItem(item.Link.DhtID, ProjectID, info.Path, info));
                }

                if (quickMenus.Count > 0 || extMenus.Count > 0)
                {
                    treeMenu.Items.Add("-");

                    foreach (ToolStripMenuItem menu in quickMenus)
                        treeMenu.Items.Add(menu);
                }

                if (extMenus.Count > 0)
                {
                    ToolStripMenuItem viewItem = new ToolStripMenuItem("Views", InterfaceRes.views);

                    foreach (ToolStripMenuItem menu in extMenus)
                        viewItem.DropDownItems.Add(menu);

                    treeMenu.Items.Add(viewItem);
                }

                // link
                if (TreeMode == CommandTreeMode.Operation)
                {
                    List<ToolStripMenuItem> linkMenus = new List<ToolStripMenuItem>();

                    List<MenuItemInfo> menuList = Links.GetMenuInfo(InterfaceMenuType.Quick, item.Link.DhtID, ProjectID);

                    if (menuList != null && menuList.Count > 0)
                        foreach (MenuItemInfo info in menuList)
                            linkMenus.Add(new OpMenuItem(item.Link.DhtID, ProjectID, info.Path, info));

                    if (linkMenus.Count > 0)
                    {
                        treeMenu.Items.Add("-");

                        foreach (ToolStripMenuItem menu in linkMenus)
                            treeMenu.Items.Add(menu);
                    }
                }

                // show
                if (treeMenu.Items.Count > 0)
                    treeMenu.Show(CommandTree, e.Location);
            }
        }

        void TreeMenu_Select(object sender, EventArgs e)
        {
            SelectCurrentItem();
        }

        private void CommandTree_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            SelectCurrentItem();
        }

        void SelectCurrentItem()
        {
            LinkNode item = GetSelected();

            if (item == null)
                return;

            if (SideMode)
                ((IMControl)Core.Components[ComponentID.IM]).QuickMenu_View(new OpMenuItem(item.Link.DhtID, 0, "", null), null);
            else
                OnSelectChange(item.Link);
        }

        void CommandTree_NodeExpanding(object sender, EventArgs e)
        {
            LinkNode node = sender as LinkNode;

            if (node == null)
                return;

            Debug.Assert(node.AddSubs);

            // node now expanded, get next level below children
            foreach (LinkNode child in node.Nodes)
                LoadNode(child);
        }

        void CommandTree_NodeCollapsed(object sender, EventArgs e)
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

        void OnSelectChange(OpLink link)
        {
            OpMenuItem item = new OpMenuItem(link.DhtID, 0, "", new MenuItemInfo("", null, null));            

            // unbold current
            if (NodeMap.ContainsKey(SelectedLink))
                NodeMap[SelectedLink].Font = new System.Drawing.Font("Tahoma", 8.25F);

            // bold new and set
            SelectedLink = link.DhtID;

            if (NodeMap.ContainsKey(link.DhtID))
            {
                NodeMap[link.DhtID].Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                NodeMap[link.DhtID].EnsureVisible();
            }

            CommandTree.Invalidate();

            Core.Profiles.InternalMenu_View(item, null);


            // setup toolbar with menu items for user
            HomeButton.Visible = link.DhtID != Core.LocalDhtID;

            PlanButton.DropDownItems.Clear();
            CommButton.DropDownItems.Clear();
            DataButton.DropDownItems.Clear();

            foreach (OpComponent component in Core.Components.Values)
            {
                List<MenuItemInfo> menuList = component.GetMenuInfo(InterfaceMenuType.Internal, link.DhtID, ProjectID);

                if (menuList == null || menuList.Count == 0)
                    continue;

                foreach (MenuItemInfo info in menuList)
                {
                    string[] parts = info.Path.Split(new char[] { '/' });

                    if (parts.Length < 2)
                        continue;
                    
                    if (parts[0] == PlanButton.Text)
                        PlanButton.DropDownItems.Add(new OpStripItem(link.DhtID, ProjectID, parts[1], info));

                    else if (parts[0] == CommButton.Text)
                        CommButton.DropDownItems.Add(new OpStripItem(link.DhtID, ProjectID, parts[1], info));

                    else if (parts[0] == DataButton.Text)
                        DataButton.DropDownItems.Add(new OpStripItem(link.DhtID, ProjectID, parts[1], info));
                }
            }
        }

        private void OperationButton_CheckedChanged(object sender, EventArgs e)
        {
            // if checked, uncheck other and display
            if (OperationButton.Checked)
            {
                OnlineButton.Checked = false;

                if (ProjectButton != null)
                    ProjectButton.Checked = false;

                MainSplit.Panel1Collapsed = false;

                TreeMode = CommandTreeMode.Operation;
                ProjectID = 0;
                SetupOperationTree();
            }

            // if not check, check if online checked, if not hide
            else
            {
                if (!OnlineButton.Checked)
                    if (ProjectButton == null || !ProjectButton.Checked)
                    {
                        if (SideMode)
                            OperationButton.Checked = true;
                        else
                            MainSplit.Panel1Collapsed = true;
                    }
            }
        }

        private void OnlineButton_CheckedChanged(object sender, EventArgs e)
        {
            // if checked, uncheck other and display
            if (OnlineButton.Checked)
            {
                OperationButton.Checked = false;

                if (ProjectButton != null)
                    ProjectButton.Checked = false;

                MainSplit.Panel1Collapsed = false;

                TreeMode = CommandTreeMode.Online;
                SetupOnlineTree();
            }

            // if not check, check if online checked, if not hide
            else
            {
                if (!OperationButton.Checked)
                    if (ProjectButton == null || !ProjectButton.Checked)
                    {
                        if (SideMode)
                            OnlineButton.Checked = true;
                        else
                            MainSplit.Panel1Collapsed = true;
                    }
            }
        }

        private void HomeButton_Click(object sender, EventArgs e)
        {
            OnSelectChange(Links.LocalLink);
        }

        private void ProjectsButton_DropDownOpening(object sender, EventArgs e)
        {
            ProjectsButton.DropDownItems.Clear();

            ProjectsButton.DropDownItems.Add(new ToolStripMenuItem("New...", null, new EventHandler(ProjectMenu_New)));

            foreach (uint id in Links.ProjectNames.Keys)
                if (id != 0)
                    ProjectsButton.DropDownItems.Add(new ProjectItem(Links.ProjectNames[id], id, new EventHandler(ProjectMenu_Click)));
        }

        private void ProjectMenu_New(object sender, EventArgs e)
        {
            NewProjectForm form = new NewProjectForm(Core);

            if (form.ShowDialog(this) == DialogResult.OK)
            {
                ProjectItem item = new ProjectItem("", form.ProjectID, null);
                ProjectMenu_Click(item, e);
            }
        }

        private void ProjectMenu_Click(object sender, EventArgs e)
        {
            ProjectItem item = sender as ProjectItem;

            if (item == null)
                return;

            UpdateProjectButton(item.ProjectID);
        }

        private void UpdateProjectButton(uint id)
        {
            ProjectButtonID = id;

            // destroy any current project button
            if (ProjectButton != null)
                SideToolStrip.Items.Remove(ProjectButton);

            // create button for project
            ProjectButton = new ToolStripButton(Links.ProjectNames[ProjectButtonID], null, new EventHandler(ShowProject));
            ProjectButton.TextDirection = ToolStripTextDirection.Vertical90;
            ProjectButton.CheckOnClick = true;
            ProjectButton.Checked = true;
            SideToolStrip.Items.Add(ProjectButton);

            // click button
            ShowProject(ProjectButton, null);
        }


        private void ShowProject(object sender, EventArgs e)
        {
            ToolStripButton button = sender as ToolStripButton;

            if (sender == null)
                return;

            // if checked, uncheck other and display
            if (button.Checked)
            {
                OperationButton.Checked = false;
                OnlineButton.Checked = false;
                MainSplit.Panel1Collapsed = false;

                TreeMode = CommandTreeMode.Operation;
                ProjectID = ProjectButtonID;
                SetupOperationTree();
            }

            // if not check, check if online checked, if not hide
            else
            {
                if (!OperationButton.Checked && !OnlineButton.Checked)
                {
                    if (SideMode)
                        ProjectButton.Checked = true;
                    else
                        MainSplit.Panel1Collapsed = true;
                }
            }
        }


        bool SideMode;
        int Panel2Width;

        private void SideButton_CheckedChanged(object sender, EventArgs e)
        {
            if (SideButton.Checked)
            {
                Panel2Width = MainSplit.Panel2.Width;

                MainSplit.Panel1Collapsed = false;
                MainSplit.Panel2Collapsed = true;

                Width -= Panel2Width;
                Left += Panel2Width;

                SideMode = true;

                OnSelectChange(Links.LocalLink);
            }

            else
            {
                Left -= Panel2Width;

                Width += Panel2Width;

                MainSplit.Panel2Collapsed = false;

                SideMode = false;
            }
        }

        private void OnProjectProperties(object sender, EventArgs e)
        {

        }

        private void OnProjectLeave(object sender, EventArgs e)
        {
            if (ProjectID != 0)
                Links.LeaveProject(ProjectID);

            // if no roots, remove button change projectid to 0
            if (!Links.ProjectRoots.ContainsKey(ProjectID))
            {
                SideToolStrip.Items.Remove(ProjectButton);
                ProjectButton = null;
                OperationButton.Checked = true;
            }
        }

        private void OnProjectJoin(object sender, EventArgs e)
        {
            if (ProjectID != 0)
                Links.JoinProject(ProjectID);
        }

        private void StatusBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            string url = e.Url.OriginalString;

            string[] parts = url.Split(new char[] {':'});

            if(parts.Length < 2)
                return;

            if (parts[0] == "project")
            {
                LinkNode item = GetSelected();
               
                UpdateProjectButton(uint.Parse(parts[1]));

                if (item != null && NodeMap.ContainsKey(item.Link.DhtID))
                    NodeMap[item.Link.DhtID].Selected = true;

                e.Cancel = true;
            }
        }

        private void RightClickMenu_Opening(object sender, CancelEventArgs e)
        {
            LinkNode item = GetSelected();

            if (item == null)
            {
                e.Cancel = true;
                return;
            }

            if (item.Link.DhtID != Core.LocalDhtID)
            {
                e.Cancel = true;
                return;
            }
        }

        private void EditMenu_Click(object sender, EventArgs e)
        {
            EditLink edit = new EditLink(Core, ProjectID);
            edit.ShowDialog(this);
        }

        private void CommandTree_SelectedItemChanged(object sender, EventArgs e)
        {
            UpdateCommandPanel();

            LinkNode item = GetSelected();

            if (item == null)
                return;

            Links.Research(item.Link.DhtID, ProjectID, true);

            Core.Locations.Research(item.Link.DhtID);
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


        internal LinkNode(OpLink link, MainForm main, CommandTreeMode mode)
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

    class OpStripItem : ToolStripMenuItem, IContainsNode
    {
        internal ulong DhtID;
        internal uint ProjectID;
        internal MenuItemInfo Info;

        internal OpStripItem(ulong key, uint id, string text, MenuItemInfo info)
            : base(text, null, info.ClickEvent )
        {
            DhtID = key;
            ProjectID = id;
            Info = info;

            Image = Info.Symbol.ToBitmap();
        }

        public ulong GetKey()
        {
            return DhtID;
        }

        public uint GetProject()
        {
            return ProjectID;
        }
    }

    class ProjectItem : ToolStripMenuItem
    {
        internal uint ProjectID;

        internal ProjectItem(string text, uint id, EventHandler onClick)
            : base(text, null, onClick)
        {
            ProjectID = id;
        }
    }

    class OpMenuItem : ToolStripMenuItem, IContainsNode
    {
        internal ulong DhtID;
        internal uint ProjectID;
        internal MenuItemInfo Info;

        internal OpMenuItem(ulong key, uint id, string text, MenuItemInfo info)
            : base(text, null, info.ClickEvent)
        {
            DhtID = key;
            ProjectID = id;
            Info = info;

            if(info.Symbol != null)
                Image = info.Symbol.ToBitmap();
        }

        public ulong GetKey()
        {
            return DhtID;
        }

        public uint GetProject()
        {
            return ProjectID;
        }
    }
}
