using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;
using DeOps.Interface;

namespace DeOps.Services.Board
{
    internal partial class PostMessage : ViewShell
    {
        OpCore Core;
        BoardService Board;

        ulong UserID;
        uint ProjectID;

        bool Reply;

        OpPost EditPost;
        OpPost ParentPost;
        uint ParentID;

        bool MessagePosted;


        internal PostMessage(BoardService board, ulong id, uint project)
        {
            InitializeComponent();

            Core = board.Core;
            Board = board;

            UserID = id;
            ProjectID = project;
        }

        internal void PostReply(OpPost parent)
        {
            Reply = true;

            ParentPost = parent;
            ParentID = parent.Header.PostID ;

            SubjectTextBox.Text = parent.Info.Subject;
            SubjectTextBox.Enabled = false;
            SubjectTextBox.BackColor = Color.WhiteSmoke;

            SetScopeInvisible();

            PostButton.Text = "Reply";
        }

        internal void PostEdit(OpPost post, uint parentID, string rtf, bool plain)
        {
            EditPost = post;

            ParentID = parentID;

            SubjectTextBox.Text = post.Info.Subject;

            MessageBody.InputBox.Rtf = rtf;
            MessageBody.PlainTextMode = plain;

            if (post.Header.Scope == ScopeType.All)
                ScopeAll.Checked = true;
            else if (post.Header.Scope == ScopeType.High)
                ScopeHigh.Checked = true;
            else if (post.Header.Scope == ScopeType.Low)
                ScopeLow.Checked = true;

            if (parentID != 0)
            {
                SubjectTextBox.Text = post.Info.Subject;
                SubjectTextBox.Enabled = false;

                SetScopeInvisible();
            }

            PostButton.Text = "Edit";
        }

        private void SetScopeInvisible()
        {
            ScopeLabel.Visible = false;
            ScopeAll.Visible = false;
            ScopeHigh.Visible = false;
            ScopeLow.Visible = false;
        }

        private void PostMessage_Load(object sender, EventArgs e)
        {
            MessageBody.InputBox.Select();
        }

        internal override Icon GetIcon()
        {
            return BoardRes.Compose;
        }

        internal override bool Fin()
        {
            if (!MessagePosted && MessageBody.InputBox.Text.Length > 0)
                if (MessageBox.Show(this, "Discard Post?", "New Post", MessageBoxButtons.YesNo) == DialogResult.No)
                    return false;

            return true;
        }

        internal override string GetTitle(bool small)
        {
            if (small)
                return "Post";

            string title = "";

            // Edit Post: Hey Guys
            if (EditPost != null)
            {
                title += "Edit Post: " + EditPost.Info.Subject;
            }

                // replying to (parent thread's name)
            else if (Reply)
            {
                title += "Reply to " + ParentPost.Info.Subject;
            }

            // post to x's project board
            else
            {
                title += "Post to ";

                if (UserID == Core.UserID)
                    title += "My ";
                else
                    title += Core.GetName(UserID) + "'s ";

                if (ProjectID != 0)
                    title += Core.Trust.GetProjectName(ProjectID) + " ";

                title += "Board";
            }

  
            return title;
        }

        internal override Size GetDefaultSize()
        {
            return new Size(400, 450);
        }

        private void LinkAdd_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Multiselect = true;
            open.Title = "Add Files to Post";
            open.Filter = "All files (*.*)|*.*";

            if (open.ShowDialog() == DialogResult.OK)
                foreach (string path in open.FileNames)
                {
                    bool added = false;
                    foreach (AttachedFile attached in ListFiles.Items)
                        if (attached.FilePath == path)
                            added = true;

                    if (!added)
                        ListFiles.Items.Add(new AttachedFile(path));
                }

            if (ListFiles.SelectedItem == null && ListFiles.Items.Count > 0)
                ListFiles.SelectedItem = ListFiles.Items[0];
        }

        private void LinkRemove_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (ListFiles.SelectedItem != null)
            {
                int index = ListFiles.Items.IndexOf(ListFiles.SelectedItem);

                ListFiles.Items.Remove(ListFiles.SelectedItem);

                if (ListFiles.Items.Count > 0)
                {
                    if (index < ListFiles.Items.Count)
                        ListFiles.SelectedItem = ListFiles.Items[index];
                    else if (index - 1 < ListFiles.Items.Count)
                        ListFiles.SelectedItem = ListFiles.Items[index - 1];
                }
            }
        }

        private void PostButton_Click(object sender, EventArgs e)
        {
            try
            {
                // scope
                ScopeType scope;

                if (ScopeHigh.Checked)
                    scope = ScopeType.High;
                else if (ScopeLow.Checked)
                    scope = ScopeType.Low;
                else if (ScopeAll.Checked)
                    scope = ScopeType.All;
                else
                    throw new Exception("No scope selected");

                // subject
                if (SubjectTextBox.Text.Length == 0)
                    throw new Exception("Subject is blank");

                // body
                if (MessageBody.InputBox.Text.Length == 0)
                    throw new Exception("Message body is blank");

                // files
                List<AttachedFile> files = new List<AttachedFile>();
                foreach (AttachedFile file in ListFiles.Items)
                    files.Add(file);

                // if reply - set subject to preview of message
                string subject = SubjectTextBox.Text;

                string message = (MessageBody.TextFormat == TextFormat.Plain) ? MessageBody.InputBox.Text : MessageBody.InputBox.Rtf;

                Board.PostMessage(UserID, ProjectID, ParentID, scope, subject, message, MessageBody.TextFormat, files, EditPost);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            MessagePosted = true;

            if (External != null)
                External.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            if (External != null)
                External.Close();
        }


    }
}
