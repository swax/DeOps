using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Interface;
using DeOps.Interface.TLVex;
using DeOps.Interface.Views;
using DeOps.Services.Trust;


namespace DeOps.Services.Board
{
    public partial class BoardView : ViewShell
    {
        CoreUI UI;
        OpCore Core;
        BoardService Boards;
        TrustService Trust;

        ulong UserID;
        uint ProjectID;

        ScopeType CurrentScope = ScopeType.All;
        
        List<ulong> HighIDs = new List<ulong>();
        List<ulong> LowIDs = new List<ulong>();

        Dictionary<int, PostViewNode> ThreadMap = new Dictionary<int, PostViewNode>();
        Dictionary<int, Dictionary<int, PostViewNode>> ActiveThreads = new Dictionary<int,Dictionary<int, PostViewNode>>();

        // the system of active threads is required because posts exist individually and must be pulled
        // from their location on the network with a thread search


        const string HeaderPage = @"<html>
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


                    <script>
                        function SetElement(id, text)
                        {
                            document.getElementById(id).innerHTML = text;
                        }
                    </script>

                </head>
                <body bgcolor=whitesmoke>
                    <p style='margin-left:5'>
                        <span id='content'><?=content?></span>
                    </p>
                </body>
            </html>";

        public BoardView(CoreUI ui, BoardService boards, ulong id, uint project)
        {
            InitializeComponent();

            UI = ui;
            Core = boards.Core;
            Boards = boards;
            Trust = Core.Trust;
            

            UserID = id;
            ProjectID = project;

            if (UserID != Core.UserID)
            {
                PostButton.Visible = false;
                RightSplitter.Visible = false;
                ArchiveButton.Visible = false;
            }

            GuiUtils.SetupToolstrip(toolStrip1, new OpusColorTable());

            PostHeader.DocumentText = HeaderPage.ToString();

            PostView.SmallImageList = new List<Image>();
            PostView.SmallImageList.Add(BoardRes.post);
            PostView.SmallImageList.Add(BoardRes.higher);
            PostView.SmallImageList.Add(BoardRes.lower);

            PostView.OverlayImages.Add(BoardRes.high_scope);
            PostView.OverlayImages.Add(BoardRes.low_scope);

            PostBody.Core = Core;
        }

        public override void Init()
        {
            Boards.PostUpdate += new PostUpdateHandler(Board_PostUpdate);
            Core.Trust.GuiUpdate += new LinkGuiUpdateHandler(Trust_Update);

            PostView.NodeExpanding += new EventHandler(OnNodeExpanding);
            PostView.NodeCollapsed += new EventHandler(OnNodeCollapsed);

            Boards.LoadView(GetHashCode(), UserID);

            RefreshBoard();
        }

        private void BoardView_Load(object sender, EventArgs e)
        {
            if (PostView.Nodes.Count > 0)
            {
                PostViewNode node = PostView.Nodes[0] as PostViewNode;

                PostView.Select(node);
                ShowMessage(node.Post, null);
            }
            else
                ShowTips();
        }

        private void ShowTips()
        {
            SetHeader("<b>Tip</b> - Your posts can be seen by those directly above and below you in the trust tree");
        }

        public override bool Fin()
        {
            Boards.PostUpdate -= new PostUpdateHandler(Board_PostUpdate);
            Trust.GuiUpdate  -= new LinkGuiUpdateHandler(Trust_Update);

            Boards.UnloadView(GetHashCode(), UserID);

            return true;
        }

        public override string GetTitle(bool small)
        {
            if (small)
                return "Board";

            string title = "";

            if (UserID == Core.UserID)
                title += "My ";
            else
                title += Core.GetName(UserID) + "'s ";

            if(ProjectID != 0)
                title += Core.Trust.GetProjectName(ProjectID) + " ";

            title += "Board";

            return title;
        }

        public override Size GetDefaultSize()
        {
            return new Size(500, 425);
        }

        public override Icon GetIcon()
        {
            return BoardRes.Icon;
        }

        private void ButtonHigh_CheckedChanged(object sender, EventArgs e)
        {
            RefreshBoard();
        }

        private void ButtonLow_CheckedChanged(object sender, EventArgs e)
        {
            RefreshBoard();
        }

        private void RefreshBoard()
        {
            ActiveThreads.Clear();

            PostView.Nodes.Clear();
            ThreadMap.Clear();

            if (ButtonHigh.Checked && ButtonLow.Checked)
                CurrentScope = ScopeType.All;
            else if (ButtonHigh.Checked)
                CurrentScope = ScopeType.High;
            else if (ButtonLow.Checked)
                CurrentScope = ScopeType.Low;
            else
                CurrentScope = ScopeType.None;

            HighIDs = Boards.GetBoardRegion(UserID, ProjectID, ScopeType.High);
            LowIDs = Boards.GetBoardRegion(UserID, ProjectID, ScopeType.Low);

            Boards.LoadRegion(UserID, ProjectID);

            List<ulong> localIDs = Boards.GetBoardRegion(UserID, ProjectID, ScopeType.All);
            foreach (ulong target in localIDs)
            {
                OpBoard board = Boards.GetBoard(target);

                if (board == null)
                {
                    Boards.LoadHeader(target); // updateinterface called in processheader
                    continue;
                }

                // call update for all posts
                board.Posts.LockReading(delegate()
                {
                    foreach (OpPost post in board.Posts.Values)
                        if (post.Header.ProjectID == ProjectID)
                            Board_PostUpdate(post);
                });
            }

            //SetHeader("");
            //PostBody.Rtf = "";
        }

        public void SetHeader(string content)
        {
            PostHeader.SafeInvokeScript("SetElement", new String[] { "content", content });
        }

        void Board_PostUpdate(OpPost post)
        {
            if (post == null)
            {
                RefreshBoard();
               
                return;
            }

            if (post.Header.ProjectID != ProjectID)
                return;

            if (post.Header.Archived != ArchiveButton.Checked)
                return;

            ScopeType level = ScopeType.All;
            bool pass = false;

            // check if belongs in list
            if (post.Header.TargetID == UserID)
            {
                if ((post.Header.Scope == ScopeType.High && CurrentScope == ScopeType.Low) ||
                    (post.Header.Scope == ScopeType.Low && CurrentScope == ScopeType.High))
                    return; // pass fail

                pass = true;
            }

            else if (CurrentScope != ScopeType.None) // use else because local id is in highIDs
            {
                if (HighIDs.Contains(post.Header.TargetID) && // high user
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
            }

            if (!pass)
                return;
            

            // parent thread
            if (post.Header.ParentID == 0)
            {
                PostViewNode node = null;

                if (!ThreadMap.ContainsKey(post.Ident))
                {
                    node = new PostViewNode(Boards, post, level, post.Header.Scope);
                    ThreadMap[post.Ident] = node;

                    AddPostNode(PostView.Nodes, node, false);
                }
                else
                {
                    node = ThreadMap[post.Ident];
                    node.Update(Boards, post);
                }

                if (node.Selected)
                    ShowMessage(post, null);
            }

            // reply - must be on active threads list to show
            else 
            {
                OpBoard board = Boards.GetBoard(post.Header.TargetID);

                if (board == null)
                    return;

                PostUID parentUid = new PostUID(post.Header.TargetID, post.Header.ProjectID, post.Header.ParentID);

                OpPost parentPost = Boards.GetPost(post.Header.TargetID, parentUid);

                if (parentPost == null)
                    return;

                int parentIdent = parentPost.Ident;

                if (!ThreadMap.ContainsKey(parentIdent))
                    return;

                PostViewNode parent = ThreadMap[parentIdent];
                parent.Update(Boards, parentPost);


                // if post has replies, add an empty item below so it has an expand option
                if(!ActiveThreads.ContainsKey(parentIdent))
                {
                    if (parent.Nodes.Count == 0)
                        parent.Nodes.Add(new TreeListNode());

                    PostView.Invalidate();
                    return;
                }

                // else post is active
                PostViewNode replyNode = null;

                if (!ActiveThreads[parentIdent].ContainsKey(post.Ident))
                {
                    replyNode = new PostViewNode(Boards, post, ScopeType.All, ScopeType.All);

                    ActiveThreads[parentIdent][post.Ident] = replyNode;

                    AddPostNode(parent.Nodes, replyNode, true);
                }
                else
                {
                    replyNode = ActiveThreads[parentIdent][post.Ident];
                    replyNode.Update(Boards, post);               
                }

                if (replyNode.Selected)
                    ShowMessage(replyNode.Post, parent.Post);

                PostView.Invalidate();
            } 
        }

        private void AddPostNode(TreeListNodeCollection list, PostViewNode add, bool lowToHigh)
        {
            int i = 0;

            foreach (PostViewNode node in list)
            {
                if (lowToHigh && add.Post.Header.Time < node.Post.Header.Time)
                {
                    list.Insert(i, add);
                    return;
                }

                if (!lowToHigh && add.Post.Header.Time > node.Post.Header.Time)
                {
                    list.Insert(i, add);
                    return;
                }

                i++;
            }

            list.Insert(i, add);
        }

        void OnNodeExpanding(object sender, EventArgs e)
        {
            PostViewNode node = sender as PostViewNode;

            if (node == null)
                return;

            node.Nodes.Clear(); // remove place holder

            ActiveThreads[node.Post.Ident] = new Dictionary<int,PostViewNode>();

            Boards.LoadThread(node.Post);
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

        void Trust_Update(ulong key)
        {
            // reset high and low scopes, if change detected to refresh

            if(CurrentScope != ScopeType.Low &&
                DetectChange(HighIDs, Boards.GetBoardRegion(UserID, ProjectID, ScopeType.High)))
                RefreshBoard();

            else if (CurrentScope != ScopeType.High &&
                DetectChange(LowIDs, Boards.GetBoardRegion(UserID, ProjectID, ScopeType.Low)))
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
            {
                ShowTips();
                return;
            }

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
            string content = responseTo + "<b><font size=2>" + parent.Info.Subject + @"</font></b> posted by " +
                              Core.GetName(post.Header.SourceID) + @" at " +
                              Utilities.FormatTime(post.Header.Time) + @"<br>";

            // edit time
            if (post.Header.EditTime > post.Header.Time)
                content += "Edited at " + Utilities.FormatTime(post.Header.EditTime) + "<br>";

            // attached files
            if (post.Attached.Count > 1)
            {
                string attachHtml = "";

                for (int i = 0; i < post.Attached.Count; i++)
                {
                    if (post.Attached[i].Name == "body")
                        continue;

                    attachHtml += "<a href='http://attach/" + i.ToString() + "'>" + post.Attached[i].Name + "</a> (" + Utilities.ByteSizetoString(post.Attached[i].Size) + "), ";
                }

                attachHtml = attachHtml.TrimEnd(new char[] { ' ', ',' });

                content += "<b>Attachments: </b> " + attachHtml;
            }

            content += "<br>";

            // actions
            string actions = "";

            if (!post.Header.Archived)
                actions += @" <a href='http://reply'>Reply</a>";

            if (post.Header.SourceID == Core.UserID)
            {
                if (!post.Header.Archived)
                    actions += @", <a href='http://edit'>Edit</a>";

                if (post == parent)
                {
                    if (post.Header.Archived)
                        actions += @", <a href='http://restore'>Restore</a>";
                    else
                        actions += @", <a href='http://archive'>Remove</a>";
                }
            }

            content += "<b>Actions: </b>" + actions.Trim(',', ' ');

            SetHeader(content);


            // body

            try
            {
                using (TaggedStream stream = new TaggedStream(Boards.GetPostPath(post.Header), Core.GuiProtocol))
                using (IVCryptoStream crypto = IVCryptoStream.Load(stream, post.Header.FileKey))
                {
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
                            byte[] msgBytes = new byte[file.Size];
                            crypto.Read(msgBytes, 0, (int)file.Size);

                            UTF8Encoding utf = new UTF8Encoding();

                            PostBody.Clear();
                            PostBody.SelectionFont = new Font("Tahoma", 9.75f);
                            PostBody.SelectionColor = Color.Black;

                            if (post.Info.Format == TextFormat.RTF)
                                PostBody.Rtf = utf.GetString(msgBytes);
                            else
                                PostBody.Text = utf.GetString(msgBytes);

                            PostBody.DetectLinksDefault();
                        }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error Opening Post: " + ex.Message);
            }

        }

        private void PostHeader_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            string url = e.Url.OriginalString;

            if (GuiUtils.IsRunningOnMono() && url.StartsWith("wyciwyg"))
                return;

            url = url.Replace("http://", "");
            url = url.TrimEnd('/');

            string[] parts = url.Split('/');

            if (parts.Length < 1)
                return;

            if (parts[0] == "about")
                return;

            if (PostView.SelectedNodes.Count == 0)
                return;

            PostViewNode node = PostView.SelectedNodes[0] as PostViewNode;

            if (node == null || node.Post == null)
                return;

            OpPost post = node.Post;

            if (parts[0] == "reply")
            {
                // replies are directed at parent
                PostViewNode parent = node.ParentNode() as PostViewNode;

                if (parent != null)
                    post = parent.Post;

                ReplyPost(post);
            }
            if (parts[0] == "edit")
                EditPost(post);

            if (parts[0] == "archive")
                Boards.Archive(post, true);

            if (parts[0] == "restore")
                Boards.Archive(post, false);

            if (parts[0] == "attach" && parts.Length > 1)
            {
                int index = int.Parse(parts[1]);

                if (index < post.Attached.Count)
                {
                    string path = Core.User.RootPath + Path.DirectorySeparatorChar +
                        "Downloads" + Path.DirectorySeparatorChar + post.Attached[index].Name;

                    try
                    {
                        if (!File.Exists(path))
                            Utilities.ExtractAttachedFile(Boards.GetPostPath(post.Header),
                                                            post.Header.FileKey,
                                                            post.Header.FileStart,
                                                            post.Attached.Select(a => a.Size).ToArray(),
                                                            index,
                                                            path);

                        Process.Start(path);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Error Opening Attachment: " + ex.Message);
                    }
                }
            }

            e.Cancel = true;
        }

        private void PostButton_Click(object sender, EventArgs e)
        {
            PostMessage post = new PostMessage(Boards, UserID, ProjectID);

            UI.ShowView(post, true);
        }


        private void RefreshButton_Click(object sender, EventArgs e)
        {
            List<ulong> targets = Boards.GetBoardRegion(UserID, ProjectID, CurrentScope);

            foreach (ulong target in targets)
                Boards.SearchBoard(target, ProjectID);


            foreach(int parentHash in ActiveThreads.Keys)
                if (ThreadMap.ContainsKey(parentHash))
                {
                    OpPost post = ThreadMap[parentHash].Post;

                    Boards.ThreadSearch(post.Header.TargetID, post.Header.ProjectID, post.Header.PostID);
                }
        }

        private void PostView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            PostViewNode node = PostView.GetNodeAt(e.Location) as PostViewNode;

            if (node == null)
                return;

            PostViewNode parent = node.ParentNode() as PostViewNode;

            OpPost replyTo = node.Post;
            if (parent != null)
                replyTo = parent.Post;

            ContextMenuStripEx menu = new ContextMenuStripEx();

            if (!replyTo.Header.Archived)
                menu.Items.Add(new PostMenuItem("Reply", replyTo, (s, a) => ReplyPost(replyTo)));

            if (node.Post.Header.SourceID == Core.UserID)
            {
                if (!replyTo.Header.Archived)
                {
                    menu.Items.Add(new PostMenuItem("Edit", node.Post, (s, a) => EditPost(node.Post)));
                    menu.Items.Add("-");
                }

                if (parent == null)
                {
                    if (node.Post.Header.Archived)
                        menu.Items.Add(new PostMenuItem("Restore", node.Post, (s, a) => Boards.Archive(node.Post, false)));
                    else
                        menu.Items.Add(new PostMenuItem("Archive", node.Post, (s, a) => Boards.Archive(node.Post, true)));
                }

            }

            menu.Show(PostView, e.Location);
        }

        void ReplyPost(OpPost parent)
        {
            PostMessage form = new PostMessage(Boards, parent.Header.TargetID, parent.Header.ProjectID);
            form.PostReply(parent);

            UI.ShowView(form, true);
        }

        void EditPost(OpPost post)
        {
            PostMessage form = new PostMessage(Boards, post.Header.TargetID, post.Header.ProjectID);
            form.PostEdit(post, post.Header.ParentID, PostBody.Rtf, post.Info.Format == TextFormat.Plain);

            UI.ShowView(form, true);
        }

        private void ArchiveButton_Click(object sender, EventArgs e)
        {
            RefreshBoard();
        }
    }

    public class PostMenuItem : ToolStripMenuItem
    {
        public OpPost Post;

        public PostMenuItem(OpPost post)
        {
            Post = post;
        }

        public PostMenuItem(string text, OpPost post, EventHandler onClick)
            : base(text, null, onClick)
        {
            Post = post;
        }
    }

    class PostViewNode : TreeListNode
    {
        public OpPost Post;
        ScopeType Position;
        ScopeType Scope;

        public PostViewNode(BoardService boards, OpPost post, ScopeType position, ScopeType scope)
        {
            Post = post;
            Position = position;
            Scope = scope;

            SubItems.Add(new ContainerSubListViewItem());
            SubItems.Add(new ContainerSubListViewItem());

            Update(boards, post);
        }


        public void Update(BoardService boards, OpPost post)
        {
            Post = post; // editing a post will build a new header, create a new object

            Text = boards.GetPostTitle(post);

            if (post.Header.ParentID == 0 && post.Replies > 0)
                Text += " (" + post.Replies.ToString() + ")";

            SubItems[0].Text = boards.Core.GetName(post.Header.SourceID);
            SubItems[1].Text = Utilities.FormatTime(post.Header.Time);


            /*
            0 - PostView.OverlayImages.Add(PostImages.higher);
            1 - PostView.OverlayImages.Add(PostImages.lower);
            2 - PostView.OverlayImages.Add(PostImages.high_scope);
            3 - PostView.OverlayImages.Add(PostImages.low_scope);*/


            ImageIndex = 0;

            if (Position == ScopeType.High)
                ImageIndex = 1;

            if (Position == ScopeType.Low)
                ImageIndex = 2;


            if (Overlays == null)
                Overlays = new List<int>();

            if (Scope == ScopeType.High)
                Overlays.Add(0);

            if (Scope == ScopeType.Low)
                Overlays.Add(1);
 
        }
    }
}
