using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Drawing;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Interface;
using DeOps.Interface.TLVex;
using DeOps.Interface.Views;

using DeOps.Components.Link;

namespace DeOps.Components.Board
{
    internal partial class BoardView : ViewShell
    {
        OpCore Core;
        BoardControl Board;
        LinkControl Links;

        ulong DhtID;
        uint ProjectID;

        OpLink CurrentLink;

        ScopeType CurrentScope = ScopeType.All;
        
        List<ulong> HighIDs = new List<ulong>();
        List<ulong> LowIDs = new List<ulong>();

        Dictionary<int, PostViewNode> ThreadMap = new Dictionary<int, PostViewNode>();
        Dictionary<int, Dictionary<int, PostViewNode>> ActiveThreads = new Dictionary<int,Dictionary<int, PostViewNode>>();

        internal PostUpdateHandler PostUpdate;

        string PostHeaderDefault = @"<html>
                <body bgcolor=whitesmoke>
                </body>
                </html>";


        internal BoardView(BoardControl board, ulong id, uint project)
        {
            InitializeComponent();

            Core = board.Core;
            Board = board;
            Links = Core.Links;
            

            DhtID = id;
            ProjectID = project;
            CurrentLink = Links.LinkMap[DhtID];

            if (DhtID != Core.LocalDhtID)
            {
                PostButton.Visible = false;
                RightSplitter.Visible = false;
                ArchiveButton.Visible = false;
            }

            toolStrip1.Renderer = new ToolStripProfessionalRenderer(new OpusColorTable());

            PostHeader.DocumentText = PostHeaderDefault;

            PostView.SmallImageList = new List<Image>();
            PostView.SmallImageList.Add(BoardRes.post);

            PostView.OverlayImages.Add(BoardRes.higher);
            PostView.OverlayImages.Add(BoardRes.lower);
            PostView.OverlayImages.Add(BoardRes.high_scope);
            PostView.OverlayImages.Add(BoardRes.low_scope);
        }

        internal override void Init()
        {
            Board.PostUpdate += new PostUpdateHandler(Board_PostUpdate);
            Core.Links.GuiUpdate += new LinkGuiUpdateHandler(Links_Update);

            PostView.NodeExpanding += new EventHandler(OnNodeExpanding);
            PostView.NodeCollapsed += new EventHandler(OnNodeCollapsed);

            Board.LoadView(this, DhtID);

            SelectProject(ProjectID);
        }

        private void BoardView_Load(object sender, EventArgs e)
        {
       
        }

        internal override bool Fin()
        {
            Board.PostUpdate -= new PostUpdateHandler(Board_PostUpdate);

            Board.UnloadView(this, DhtID);

            return true;
        }

        internal override string GetTitle(bool small)
        {
            if (small)
                return "Board";

            string title = "";

            if (DhtID == Core.LocalDhtID)
                title += "My ";
            else
                title += Core.Links.GetName(DhtID) + "'s ";

            if(ProjectID != 0)
                title += Core.Links.ProjectNames[ProjectID] + " ";

            title += "Board";

            return title;
        }

        internal override Size GetDefaultSize()
        {
            return new Size(500, 425);
        }

        internal override Icon GetIcon()
        {
            return BoardRes.Icon;
        }

        private void ProjectButton_DropDownOpening(object sender, EventArgs e)
        {
            ProjectButton.DropDownItems.Clear();

            ProjectButton.DropDownItems.Add(new ProjectItem("Main", 0, new EventHandler(ProjectMenu_Click)));

            foreach (uint id in Links.ProjectNames.Keys)
                if (id != 0 && CurrentLink.Projects.Contains(id) && id != ProjectID)
                    ProjectButton.DropDownItems.Add(new ProjectItem(Links.ProjectNames[id], id, new EventHandler(ProjectMenu_Click)));
        }

        private void ProjectMenu_Click(object sender, EventArgs e)
        {
            ProjectItem item = sender as ProjectItem;

            if (item == null)
                return;

            SelectProject(item.ProjectID);
        }

        private void SelectProject(uint id)
        {
            ProjectID = id;

            string name = "Main";
            if (id != 0)
                name = Links.ProjectNames[id];

            ProjectButton.Text = "Project: " + name;

            RefreshBoard();
        }

        private void ViewButton_DropDownOpening(object sender, EventArgs e)
        {
            ViewButton.DropDownItems.Clear();

            if (CurrentScope != ScopeType.High)
                ViewButton.DropDownItems.Add(new ViewItem("High", ScopeType.High, new EventHandler(ViewMenu_Click)));

            if (CurrentScope != ScopeType.Low)
                ViewButton.DropDownItems.Add(new ViewItem("Low", ScopeType.Low, new EventHandler(ViewMenu_Click)));

            if (CurrentScope != ScopeType.All)
                ViewButton.DropDownItems.Add(new ViewItem("All", ScopeType.All, new EventHandler(ViewMenu_Click)));
        }

        private void ViewMenu_Click(object sender, EventArgs e)
        {
            ViewItem item = sender as ViewItem;

            if (item == null)
                return;

            CurrentScope = item.Scope;
            ViewButton.Text = "View: " + CurrentScope.ToString();

            RefreshBoard();
        }


        private void RefreshBoard()
        {
            ActiveThreads.Clear();

            PostView.Nodes.Clear();
            ThreadMap.Clear();     

            HighIDs = Board.GetBoardRegion(DhtID, ProjectID, ScopeType.High);
            LowIDs = Board.GetBoardRegion(DhtID, ProjectID, ScopeType.Low);

            Board.LoadRegion(DhtID, ProjectID);

            PostHeader.DocumentText = PostHeaderDefault;
            PostBody.Rtf = "";
        }
        
        void Board_PostUpdate(OpPost post)
        {
            if (post.Header.ProjectID != ProjectID)
                return;

            ScopeType level = ScopeType.All;
            bool pass = false;

            // check if belongs in list
            if (post.Header.TargetID == DhtID)
            {
                if ((post.Header.Scope == ScopeType.High && CurrentScope == ScopeType.Low) ||
                    (post.Header.Scope == ScopeType.Low && CurrentScope == ScopeType.High))
                    return; // pass fail

                pass = true;
            }

            else if (HighIDs.Contains(post.Header.TargetID) && // high user
                     CurrentScope != ScopeType.Low && // view filter
                     post.Header.Scope != ScopeType.High) // post meant for high user's highers, not us
            {
                pass = true;
                level = ScopeType.High;
            }

            else if (LowIDs.Contains(post.Header.TargetID) && // low user
                     CurrentScope != ScopeType.High && // view filter
                     post.Header.Scope != ScopeType.Low) // post meant for low user's lowers, not us
            {
                pass = true;
                level = ScopeType.Low;
            }

            if (!pass)
                return;
            

            // parent thread
            if (post.Header.ParentID == 0)
            {
                PostViewNode node = null;

                if (!ThreadMap.ContainsKey(post.Ident))
                {
                    node = new PostViewNode(Core, post, level, post.Header.Scope);
                    ThreadMap[post.Ident] = node;

                    PostView.Nodes.Add(node);
                }
                else
                {
                    node = ThreadMap[post.Ident];
                    node.Update(Core, post);
                }

                if (node.Selected)
                    ShowMessage(post, null);

                // if post has replies, add an empty item below so it has an expand option
                if (node.Nodes.Count == 0 && post.Replies > 0)
                    node.Nodes.Add(new TreeListNode());
            }

            // reply - must be on active threads list to show
            else 
            {
                if (!Board.BoardMap.ContainsKey(post.Header.TargetID))
                    return;

                PostUID parentUid = new PostUID(post.Header.TargetID, post.Header.ProjectID, post.Header.ParentID);

                if (!Board.BoardMap[post.Header.TargetID].Posts.ContainsKey(parentUid))
                    return;

                int parentIdent = Board.BoardMap[post.Header.TargetID].Posts[parentUid].Ident;

                if (!ActiveThreads.ContainsKey(parentIdent) || 
                    !ThreadMap.ContainsKey(parentIdent))
                    return;

                PostViewNode parentNode = ThreadMap[parentIdent];

                PostViewNode replyNode = null;

                if (!ActiveThreads[parentIdent].ContainsKey(post.Ident))
                {
                    replyNode = new PostViewNode(Core, post, ScopeType.All, ScopeType.All);

                    ActiveThreads[parentIdent][post.Ident] = replyNode;
                    parentNode.Nodes.Add(replyNode);
                }
                else
                {
                    replyNode = ActiveThreads[parentIdent][post.Ident];
                    replyNode.Update(Core, post);
                }

                if (replyNode.Selected)
                    ShowMessage(replyNode.Post, parentNode.Post);
            } 
        }

        void OnNodeExpanding(object sender, EventArgs e)
        {
            PostViewNode node = sender as PostViewNode;

            if (node == null)
                return;

            node.Nodes.Clear(); // remove place holder

            ActiveThreads[node.Post.Ident] = new Dictionary<int,PostViewNode>();

            Board.LoadThread(node.Post);
        }

        void OnNodeCollapsed(object sender, EventArgs e)
        {
            PostViewNode node = sender as PostViewNode;

            if (node == null)
                return;

            // remove nodes except for place holder
            node.Nodes.Clear();
            node.Nodes.Add(new TreeListNode());

            ActiveThreads.Remove(node.Post.Ident);
        }

        void Links_Update(ulong key)
        {
            // reset high and low scopes, if change detected to refresh

            if(CurrentScope != ScopeType.Low && 
                DetectChange( HighIDs, Board.GetBoardRegion(DhtID, ProjectID, ScopeType.High)))
                RefreshBoard();

            else if (CurrentScope != ScopeType.High && 
                DetectChange( LowIDs, Board.GetBoardRegion(DhtID, ProjectID, ScopeType.Low)))
                RefreshBoard();
        }

        private bool DetectChange(List<ulong> list, List<ulong> check)
        {
            // removed
            foreach (ulong id in list)
                if (!check.Contains(id))
                    return true;

            // added
            foreach (ulong id in check)
                if (!list.Contains(id))
                    return true;

            return false;
        }

        private void PostView_SelectedItemChanged(object sender, EventArgs e)
        {
            if (PostView.SelectedNodes.Count == 0)
                return;

            PostViewNode node = PostView.SelectedNodes[0] as PostViewNode;

            if (node == null)
                return;

            PostViewNode parent = node.ParentNode() as PostViewNode;

            OpPost parentPost = null;
            if(parent != null) 
                parentPost = parent.Post;

            ShowMessage(node.Post, parentPost);
        }

        private void ShowMessage(OpPost post, OpPost parent)
        {
            if (parent == null)
                parent = post;

            string responseTo = "";
            if (parent != post)
                responseTo = "Response to ";

            // header
            string htmlHeader =
                @"<html>
                <head>
                    <style type='text/css'>
                    <!--
                        p    { font-size: 8.25pt; font-family: Tahoma }
                        body { margin: 4; }
                        A:link {text-decoration: none; color: blue}
                        A:visited {text-decoration: none; color: blue}
                        A:active {text-decoration: none; color: blue}
                        A:hover {text-decoration: underline; color: blue}
                    -->
                    </style>
                </head>
                <body bgcolor=whitesmoke>
                    <p>
                    " + responseTo + "<b><font size=2>" + parent.Info.Subject + @"</font></b> posted by " +
                                      Links.GetName(post.Header.SourceID) + @" at " +
                                      Utilities.FormatTime(post.Header.Time) + @"<br>";

            // edit time
            if (post.Header.EditTime > post.Header.Time)
                htmlHeader += "Edited at " + Utilities.FormatTime(post.Header.EditTime);

            htmlHeader += "<br>";

            // actions
            htmlHeader += @" <b>Actions: </b><a href='reply:" + parent.Ident.ToString() + "'>Reply</a>";

            if(post.Header.SourceID == Core.LocalDhtID)
                    htmlHeader += @", <a href='edit:" + post.Ident.ToString() + "'>Edit</a>";

            htmlHeader +=
                    @"</body>
                </html>";

            PostHeader.DocumentText = htmlHeader;

            // body

            try
            {
                FileStream stream = new FileStream(Board.GetPostPath(post.Header), FileMode.Open, FileAccess.Read, FileShare.Read);
                CryptoStream crypto = new CryptoStream(stream, post.Header.FileKey.CreateDecryptor(), CryptoStreamMode.Read);

                int buffSize = 4096;
                byte[] buffer = new byte[4096];
                long bytesLeft = post.Header.FileStart;
                while (bytesLeft > 0)
                {
                    int readSize = (bytesLeft > (long)buffSize) ? buffSize : (int)bytesLeft;
                    int read = crypto.Read(buffer, 0, readSize);
                    bytesLeft -= (long)read;
                }

                // load file
                foreach (PostFile file in post.Attached)
                    if (file.Name == "body")
                    {
                        byte[] htmlBytes = new byte[file.Size];
                        crypto.Read(htmlBytes, 0, (int)file.Size);

                        UTF8Encoding utf = new UTF8Encoding();
                        PostBody.Rtf = utf.GetString(htmlBytes);
                    }

                Utilities.ReadtoEnd(crypto);
                crypto.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error Opening Post: " + ex.Message);
            }

        }

        private void PostHeader_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            string url = e.Url.OriginalString;

            string[] parts = url.Split(new char[] { ':' });

            if (parts.Length < 2)
                return;

            if(parts[0] == "reply" || parts[0] == "edit")
            {
                int hash = int.Parse(parts[1]);

                OpPost post = null;

                if (ThreadMap.ContainsKey(hash))
                    post = ThreadMap[hash].Post;
                else
                {
                    foreach (Dictionary<int, PostViewNode> thread in ActiveThreads.Values)
                    {
                        foreach (PostViewNode reply in thread.Values)
                            if (reply.Post.Ident == hash)
                            {
                                post = reply.Post;
                                break;
                            }

                        if (post != null)
                            break;
                    }
                }

                if (post != null)
                {
                    if (parts[0] == "reply")
                        Reply(post);

                    if (parts[0] == "edit")
                        Edit(post);
                }

                e.Cancel = true; 
            }     
        }

        private void PostButton_Click(object sender, EventArgs e)
        {
            PostMessage post = new PostMessage(Board, DhtID, ProjectID);

            Core.InvokeInterface(Core.GuiMain.ShowExternal, post);

        }

        private void Reply(OpPost parent)
        {
            PostMessage form = new PostMessage(Board, parent.Header.TargetID, parent.Header.ProjectID);
            form.PostReply(parent);

            Core.InvokeInterface(Core.GuiMain.ShowExternal, form);
        }

        private void Edit(OpPost post)
        {
            PostMessage form = new PostMessage(Board, post.Header.TargetID, post.Header.ProjectID);
            form.PostEdit(post, post.Header.ParentID, PostBody.Rtf);

            Core.InvokeInterface(Core.GuiMain.ShowExternal, form);
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            List<ulong> targets = Board.GetBoardRegion(DhtID, ProjectID, CurrentScope);

            foreach (ulong target in targets)
                Board.SearchBoard(target, ProjectID);


            foreach(int parentHash in ActiveThreads.Keys)
                if (ThreadMap.ContainsKey(parentHash))
                {
                    OpPost post = ThreadMap[parentHash].Post;

                    Board.ThreadSearch(post.Header.TargetID, post.Header.ProjectID, post.Header.PostID);
                }
        }
    }

    class ViewItem : ToolStripMenuItem
    {
        internal ScopeType Scope;

        internal ViewItem(string text, ScopeType scope, EventHandler onClick)
            : base(text, null, onClick)
        {
            Scope = scope;
        }
    }

    class PostViewNode : TreeListNode
    {
        internal OpPost Post;
        ScopeType Level;
        ScopeType Scope;

        internal PostViewNode(OpCore core, OpPost post, ScopeType level, ScopeType scope)
        {
            Post = post;
            Level = level;
            Scope = scope;

            SubItems.Add(new ContainerSubListViewItem());
            SubItems.Add(new ContainerSubListViewItem());
            SubItems.Add(new ContainerSubListViewItem());

            Update(core, post);
        }


        internal void Update(OpCore Core, OpPost post)
        {
            Post = post; // editing a post will build a new header, create a new object

            Text = Core.Board.GetPostInfo(post);
            SubItems[0].Text = Core.Links.GetName(post.Header.SourceID);
            SubItems[1].Text = Utilities.FormatTime(post.Header.Time);

            if (post.Header.ParentID == 0 && post.Replies > 0)
                SubItems[2].Text = post.Replies.ToString();
            else
                SubItems[2].Text = "";

            /*
            0 - PostView.OverlayImages.Add(PostImages.higher);
            1 - PostView.OverlayImages.Add(PostImages.lower);
            2 - PostView.OverlayImages.Add(PostImages.high_scope);
            3 - PostView.OverlayImages.Add(PostImages.low_scope);*/


            ImageIndex = 0;

            if (Overlays == null)
                Overlays = new List<int>();

            if (Level == ScopeType.High)
                Overlays.Add(0);

            if (Level == ScopeType.Low)
                Overlays.Add(1);

            if (Scope == ScopeType.High)
                Overlays.Add(2);

            if (Scope == ScopeType.Low)
                Overlays.Add(3);
 
        }
    }
}
