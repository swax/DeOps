using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface;
using DeOps.Implementation;
using DeOps.Services.Trust;
using DeOps.Interface.TLVex;
using DeOps.Interface.Views;


namespace DeOps.Services.Mail
{

    internal partial class MailView : ViewShell
    {
        internal OpCore Core;
        internal MailUI UI;
        internal MailService Mail;
        internal TrustService Trust;

        private ListViewColumnSorter lvwColumnSorter = new ListViewColumnSorter();

        internal Font RegularFont = new Font("Tahoma", 8.25F);
        internal Font BoldFont = new Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

        const string DefaultPage =
                @"<html>
                <body bgcolor=whitesmoke>
                </body>
                </html>";

        const string HeaderPage =
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


        internal MailView(MailUI ui)
        {
            InitializeComponent();

            UI = ui;
            Mail = ui.Mail;
            Core  = ui.Core;
            Trust = Core.Trust;

            MessageView.SmallImageList = new List<Image>();
            MessageView.SmallImageList.Add(MailRes.recvmail);
            MessageView.SmallImageList.Add(MailRes.sentmail);

            MessageHeader.DocumentText = HeaderPage.ToString();

            GuiUtils.SetupToolstrip(toolStrip1, new OpusColorTable());

            MessageBody.Core = Core;
        }

        internal override string GetTitle(bool small)
        {
            if (small)
                return "Mail";

            return "My Mail";
        }

        internal override Size GetDefaultSize()
        {
            return new Size(600, 575);
        }

        internal override Icon GetIcon()
        {
            return MailRes.Icon;
        }

        internal override void Init()
        {
            Mail.MailUpdate += new MailUpdateHandler(OnMailUpdate);

            MessageView.BeginUpdate();

            RefreshView();

            if (MessageView.Nodes.Count > 0)
            {
                MessageNode node = MessageView.Nodes[0] as MessageNode;

                MessageView.Select(node);
                ShowMessage(node.Message);
            }
            MessageView.EndUpdate();
        }

        internal override bool Fin()
        {
            Mail.MailUpdate -= new MailUpdateHandler(OnMailUpdate);

            return true;
        }

        internal void SetHeader(string content)
        {
            MessageHeader.SafeInvokeScript("SetElement", new String[] { "content", content });
        }

        private void InboxButton_Click(object sender, EventArgs e)
        {
            RefreshView();
        }

        private void OutboxButton_Click(object sender, EventArgs e)
        {
            RefreshView();
        }

        private void RefreshView()
        {
            MessageView.Nodes.Clear();

            if (Mail.LocalMailbox == null)
                Mail.LoadLocalHeaders();

            // messages sorted oldest to newest so thread parents should be correct
            Mail.LocalMailbox.LockReading(delegate()
            {
                foreach (LocalMail message in Mail.LocalMailbox.Values)
                    AddMessage(message, false);
            });
        }

        void OnMailUpdate(LocalMail message)
        {
            AddMessage(message, true);
        }

        private void AddMessage(LocalMail message, bool ensureVisible)
        {
            bool local = (message.Header.TargetID == Core.UserID);

            if(local && !ReceivedButton.Checked)
                return;

            if(!local && !SentButton.Checked)
                return;

            // find thread id and add to thread
            MessageNode node = new MessageNode(this, message);

            // interate through parents
            foreach (MessageNode parent in MessageView.Nodes)
            {
                if (Utilities.MemCompare(parent.Message.Header.MailID, node.Message.Header.MailID))
                {
                    parent.UpdateRow();
                    return;
                }

                // iterate through children
                if (parent.Message.Header.ThreadID == message.Header.ThreadID)
                {
                    foreach (MessageNode child in parent.Nodes)
                        if (Utilities.MemCompare(child.Message.Header.MailID, node.Message.Header.MailID))
                        {
                            child.UpdateRow();
                            return;
                        }

                    // not found add to thread
                    parent.Nodes.Add(node);
                    node.UpdateRow();
                    
                    if(!message.Header.Read || ensureVisible)
                        parent.Expand();

                    return;
                }
            }

            // thread not found add as parent, sort new to old
            MessageView.Nodes.Insert(0, node);
            node.UpdateRow(); // update here so node knows whether to put subject or quip
        }

        private void ComposeButton_Click(object sender, EventArgs e)
        {
            UI.OpenComposeWindow(0);
        }

        /*private void MessageList_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.ColumnToSort)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.OrderOfSort == SortOrder.Ascending)
                    lvwColumnSorter.OrderOfSort = SortOrder.Descending;
                else
                    lvwColumnSorter.OrderOfSort = SortOrder.Ascending;
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.ColumnToSort = e.Column;
                lvwColumnSorter.OrderOfSort = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            //MessageList.Sort();
        }*/

        private void MessageView_SelectedItemChanged(object sender, EventArgs e)
        {
            ShowSelected();
        }

        private void ShowSelected()
        {
            if (MessageView.SelectedNodes.Count == 0)
            {
                ShowMessage(null);
                return;
            }

            MessageNode item = (MessageNode)MessageView.SelectedNodes[0];

            ShowMessage(item.Message);
        }

        private void ShowMessage(LocalMail message)
        {
            if (message == null)
            {
                SetHeader("");
                MessageBody.Clear();
                
                return;
            }

      
            string content = "<b><font size=2>" + message.Info.Subject + "</font></b> from " + 
                              Core.GetName(message.Header.SourceID) + ", sent " +
                              Utilities.FormatTime(message.Info.Date) + @"<br> 
                              <b>To:</b> " + Mail.GetNames(message.To) + "<br>";

            if(message.CC.Count > 0)
                content += "<b>CC:</b> " + Mail.GetNames(message.CC) + "<br>";
                    
            if(message.Attached.Count > 1)
            {
                string attachHtml = "";

                for (int i = 0; i < message.Attached.Count; i++)
                {
                    if (message.Attached[i].Name == "body")
                        continue;

                    attachHtml += "<a href='http://attach/" + i.ToString() + "'>" + message.Attached[i].Name + "</a> (" + Utilities.ByteSizetoString(message.Attached[i].Size) + "), ";
                }

                attachHtml = attachHtml.TrimEnd(new char[] { ' ', ',' });

                content += "<b>Attachments: </b> " + attachHtml;
            }

            content += "<br>";

            string actions = "";

            if (message.Header.TargetID == Core.UserID)
                actions += @"<a href='http://reply" + "'>Reply</a>";

            actions += @", <a href='http://forward'>Forward</a>";
            actions += @", <a href='http://delete'>Delete</a>";

            content += "<b>Actions: </b>" + actions.Trim(',', ' ');

            SetHeader(content);

            // body

            try
            {
                using (TaggedStream stream = new TaggedStream(Mail.GetLocalPath(message.Header), Core.GuiProtocol))
                using (CryptoStream crypto = IVCryptoStream.Load(stream, message.Header.LocalKey))
                {
                    int buffSize = 4096;
                    byte[] buffer = new byte[4096];
                    long bytesLeft = message.Header.FileStart;
                    while (bytesLeft > 0)
                    {
                        int readSize = (bytesLeft > buffSize) ? buffSize : (int)bytesLeft;
                        int read = crypto.Read(buffer, 0, readSize);
                        bytesLeft -= read;
                    }

                    // load file
                    foreach (MailFile file in message.Attached)
                        if (file.Name == "body")
                        {
                            byte[] msgBytes = new byte[file.Size];
                            crypto.Read(msgBytes, 0, (int)file.Size);

                            UTF8Encoding utf = new UTF8Encoding();

                            MessageBody.Clear();
                            MessageBody.SelectionFont = new Font("Tahoma", 9.75f);
                            MessageBody.SelectionColor = Color.Black;

                            if (message.Info.Format == TextFormat.RTF)
                                MessageBody.Rtf = utf.GetString(msgBytes);
                            else
                                MessageBody.Text = utf.GetString(msgBytes);

                            MessageBody.DetectLinksDefault();
                        }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error Opening Mail: " + ex.Message);
            }

            if (message.Header.Read == false)
            {
                message.Header.Read = true;

                Mail.SaveMailbox = true;

                if (MessageView.SelectedNodes.Count > 0)
                    ((MessageNode)MessageView.SelectedNodes[0]).UpdateRow();
            }
        }

        private void MessageHeader_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            string url = e.Url.OriginalString;

            if (GuiUtils.IsRunningOnMono() && url.StartsWith("wyciwyg"))
                return;

            url = url.Replace("http://", "");
            url = url.TrimEnd('/');

            string[] parts = url.Split('/');

            if (parts.Length < 1)
                return;

            if (MessageView.SelectedNodes.Count == 0)
                return;

            if (parts[0] == "about")
                return;

            MessageNode item = MessageView.SelectedNodes[0] as MessageNode;
            
            if(item == null)
                return;

            LocalMail message = item.Message;

            if (parts[0] == "attach" && parts.Length > 1)
            {
                int index = int.Parse(parts[1]);

                if (index < message.Attached.Count)
                {
                    string path = Core.User.RootPath + Path.DirectorySeparatorChar +
                        "Downloads" + Path.DirectorySeparatorChar + message.Attached[index].Name;
                        

                    try
                    {
                        if (!File.Exists(path))
                            Utilities.ExtractAttachedFile(Mail.GetLocalPath(message.Header),
                                                            message.Header.LocalKey,
                                                            message.Header.FileStart,
                                                            message.Attached.Select(a => a.Size).ToArray(),
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

            if (parts[0] == "reply")
                Message_Reply(new MessageMenuItem(message), null);

            else if (parts[0] == "forward")
                Message_Forward(new MessageMenuItem(message), null);

            else if (parts[0] == "delete")
                Message_Delete(new MessageMenuItem(message), null);

            e.Cancel = true;
        }


        void Message_Reply(object sender, EventArgs e)
        {
            MessageMenuItem item = sender as MessageMenuItem;

            if (item == null)
                return;

            Mail.Reply(item.Message, MessageBody.Rtf);
        }

        void Message_Forward(object sender, EventArgs e)
        {
            MessageMenuItem item = sender as MessageMenuItem;

            if (item == null)
                return;

            Mail.Forward(item.Message, MessageBody.Rtf);

        }

        void Message_Delete(object sender, EventArgs e)
        {
            MessageNode[] selected = MessageView.SelectedNodes.OfType<MessageNode>().ToArray();
        
            if(selected.Length == 0)
                return;

            if (MessageBox.Show(this, "Are you sure you want to delete this message?", "Delete", MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            foreach (MessageNode item in selected)
                Mail.DeleteLocal(item.Message);

            // need to figure if parent or child, if parent then first child is the new parent in thread
            // refresh is quick fix for now

            RefreshView();
        }

        private void MessageView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            MessageNode item = MessageView.GetNodeAt(e.Location) as MessageNode;

            if (item == null)
                return;

            ContextMenuStripEx menu = new ContextMenuStripEx();

            if (item.Message.Header.TargetID == Core.UserID)
                menu.Items.Add(new MessageMenuItem(item.Message, "Reply", null, new EventHandler(Message_Reply)));

            menu.Items.Add(new MessageMenuItem(item.Message, "Forward", null, new EventHandler(Message_Forward)));
            menu.Items.Add("-");
            menu.Items.Add(new MessageMenuItem(item.Message, "Delete", MailRes.delete, new EventHandler(Message_Delete)));

            menu.Show(MessageView, e.Location);
        }
    }

    class MessageNode : TreeListNode
    {
        MailView View;
        internal LocalMail Message;

        internal MessageNode(MailView view, LocalMail message)
        {
            View = view;
            Message = message;

            SubItems.Add("");
            SubItems.Add("");
        }

        internal void UpdateRow()
        {
            bool local = (Message.Header.TargetID == View.Core.UserID);

            ImageIndex = local ? 0 : 1;

            string subject = (TreeList.virtualParent == Parent) ? Message.Info.Subject : Message.Info.Quip;

            string who = local ? "From: " + View.Core.GetName(Message.From) :
                                 "To: " + View.Mail.GetNames(Message.To);

            DateTime utc = local ? Message.Header.Received : Message.Info.Date;
            string date = Utilities.FormatTime(utc.ToLocalTime());


            Text = subject;
            SubItems[0].Text = who;
            SubItems[1].Text = date;

            // if unread put in bold
            if (local && !Message.Header.Read)
                Font = View.BoldFont;
            else
                Font = View.RegularFont;
        }
    }

    class MessageMenuItem : ToolStripMenuItem
    {
        internal LocalMail Message;


        internal MessageMenuItem(LocalMail message)
        {
            Message = message;
        }

        internal MessageMenuItem(LocalMail message, string text, Image icon, EventHandler onClick)
            : base(text, icon, onClick)
        {
            Message = message;
        }
    }
}
