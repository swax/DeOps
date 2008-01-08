using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface;
using DeOps.Implementation;
using DeOps.Services.Link;
using DeOps.Interface.TLVex;
using DeOps.Interface.Views;

namespace DeOps.Services.Mail
{

    internal partial class MailView : ViewShell
    {
        OpCore Core;
        MailControl Mail;
        LinkControl Links;

        private ListViewColumnSorter lvwColumnSorter = new ListViewColumnSorter();

        Font RegularFont = new Font("Tahoma", 8.25F);
        Font BoldFont = new Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

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
                    <p>
                        <span id='content'></span>
                    </p>
                </body>
            </html>";


        internal MailView(MailControl mail)
        {
            InitializeComponent();

            //MessageList.ListViewItemSorter = lvwColumnSorter;

            Mail  = mail;
            Core  = mail.Core;
            Links = Core.Links;

            MessageList.SmallImageList = new List<Image>();
            MessageList.SmallImageList.Add(MailRes.Mail);
            MessageList.SmallImageList.Add(MailRes.Attach);

            MessageHeader.DocumentText = HeaderPage.ToString();

            toolStrip1.Renderer = new ToolStripProfessionalRenderer(new OpusColorTable());
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

            InboxButton_Click(null, null);
        }

        internal override bool Fin()
        {
            Mail.MailUpdate -= new MailUpdateHandler(OnMailUpdate);

            return true;
        }

        internal void SetHeader(string content)
        {
            MessageHeader.Document.InvokeScript("SetElement", new String[] { "content", content });
        }

        private void InboxButton_Click(object sender, EventArgs e)
        {
            InboxButton.Checked = true;
            OutboxButton.Checked = false;
            
            // setup - From / Subject / Received
            MessageList.Columns.Clear();
            MessageList.Columns.Add("Subject", 250, HorizontalAlignment.Left, ColumnScaleStyle.Spring);
            MessageList.Columns.Add("From", 100, HorizontalAlignment.Left, ColumnScaleStyle.Slide);
            MessageList.Columns.Add("Received", 150, HorizontalAlignment.Left, ColumnScaleStyle.Slide);

            MessageList.Items.Clear();

            MessageList.RecalcLayout();

            if (Mail.Inbox == null)
                Mail.LoadLocalHeaders(MailBoxType.Inbox);
            
            // display messages in list box
            Mail.Inbox.LockReading(delegate()
            {
                foreach (LocalMail message in Mail.Inbox)
                    AddInboxMessage(message);
            });

            // select first item
            if (MessageList.Items.Count > 0)
                MessageList.Select(MessageList.Items[0]);

            ShowSelected();
        }

        private void OutboxButton_Click(object sender, EventArgs e)
        {
            InboxButton.Checked = false;
            OutboxButton.Checked = true;

            // setup - To / Subject / Sent
            MessageList.Columns.Clear();
            MessageList.Columns.Add("Subject", 250, HorizontalAlignment.Left, ColumnScaleStyle.Spring);
            MessageList.Columns.Add("To", 100, HorizontalAlignment.Left, ColumnScaleStyle.Slide);
            MessageList.Columns.Add("Sent", 150, HorizontalAlignment.Left, ColumnScaleStyle.Slide);
            MessageList.Columns.Add("Status", 100, HorizontalAlignment.Left, ColumnScaleStyle.Slide);

            MessageList.Items.Clear();

            MessageList.RecalcLayout();

            if (Mail.Outbox == null)
                Mail.LoadLocalHeaders(MailBoxType.Outbox);

            // display messages
            Mail.Outbox.LockReading(delegate()
            {
                foreach (LocalMail message in Mail.Outbox)
                    AddOutboxMessage(message);
            });

            // select first item
            if (MessageList.Items.Count > 0)
                MessageList.Select(MessageList.Items[0]);

            ShowSelected();
        }

        void OnMailUpdate(bool inbox, LocalMail message)
        {
            // in already in list remove
            MessageListItem item = FindMessage(message);

            if (item != null)
                MessageList.Items.Remove(item);

            if (inbox && InboxButton.Checked)
                AddInboxMessage(message);

            if (!inbox && OutboxButton.Checked)
                AddOutboxMessage(message);
        }

        private MessageListItem FindMessage(LocalMail message)
        {
            foreach (MessageListItem item in MessageList.Items)
                if (Utilities.MemCompare(item.Message.Header.MailID, message.Header.MailID))
                    return item;

            return null;
        }

        private void AddInboxMessage(LocalMail message)
        {
            MessageListItem item = new MessageListItem(message, 
                    message.Info.Subject,
                    Links.GetName(message.From),
                    Utilities.FormatTime(message.Header.Received.ToLocalTime()));
            
            //crit - no sub item fonts
            /*if(!message.Header.Read)
                foreach (ContainerSubListViewItem subItem in item.SubItems)
                    subItem.Font = BoldFont;*/
            
            MessageList.Items.Add(item);

            MessageList.Invalidate();
        }

        private void AddOutboxMessage(LocalMail message)
        {
            string to = Mail.GetNames(message.To);

            int total = message.To.Count + message.CC.Count;

            string status = "";
            ulong hashid = BitConverter.ToUInt64(message.Header.FileHash, 0);
            if (Mail.PendingMail.ContainsKey(hashid))
            {
                int unrecved = Mail.PendingMail[hashid].Count;
                if (total == unrecved)
                    status = "Sent";
                else
                    status = "Received by " + (total - unrecved).ToString() + " of " + total.ToString();
            }
            else
                status = "Received";


            MessageList.Items.Add(new MessageListItem(message, 
                    message.Info.Subject,
                    to,
                    Utilities.FormatTime(message.Info.Date.ToLocalTime()),
                    status));

            MessageList.Invalidate();
        }

        private void ComposeButton_Click(object sender, EventArgs e)
        {
            Mail.QuickMenu_View(null, null);
        }

        private void MessageList_ColumnClick(object sender, ColumnClickEventArgs e)
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
        }

        private void MessageList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowSelected();
        }

        private void ShowSelected()
        {
            if (MessageList.SelectedItems.Count == 0)
            {
                ShowMessage(null);
                return;
            }

            MessageListItem item = (MessageListItem)MessageList.SelectedItems[0];

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
                              Links.GetName(message.Header.SourceID) + ", sent " +
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

                    attachHtml += "<a href='attach:" + i.ToString() + "'>" + message.Attached[i].Name + "</a> (" + Utilities.ByteSizetoString(message.Attached[i].Size) + "), ";
                }

                attachHtml = attachHtml.TrimEnd(new char[] { ' ', ',' });

                content += "<b>Attachments: </b> " + attachHtml;
            }

            content += "<br>";

            string actions = "";

            if (InboxButton.Checked)
                actions += @"<a href='reply:x" + "'>Reply</a>";

            actions += @", <a href='forward:x'>Forward</a>";
            actions += @", <a href='delete:x'>Delete</a>";

            content += "<b>Actions: </b>" + actions.Trim(',', ' ');

            SetHeader(content);

            // body

            try
            {
                FileStream stream = new FileStream(Mail.GetLocalPath(message.Header), FileMode.Open, FileAccess.Read, FileShare.Read);
                CryptoStream crypto = new CryptoStream(stream, message.Header.LocalKey.CreateDecryptor(), CryptoStreamMode.Read);

                int buffSize = 4096;
                byte[] buffer = new byte[4096];
                ulong bytesLeft = message.Header.FileStart;
                while (bytesLeft > 0)
                {
                    int readSize = (bytesLeft > (ulong)buffSize) ? buffSize : (int)bytesLeft;
                    int read = crypto.Read(buffer, 0, readSize);
                    bytesLeft -= (ulong)read;
                }

                // load file
                foreach (MailFile file in message.Attached)
                    if (file.Name == "body")
                    {
                        byte[] htmlBytes = new byte[file.Size];
                        crypto.Read(htmlBytes, 0, (int)file.Size);

                        UTF8Encoding utf = new UTF8Encoding();
                        MessageBody.Rtf = utf.GetString(htmlBytes);
                    }

                Utilities.ReadtoEnd(crypto);
                crypto.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error Opening Mail: " + ex.Message);
            }

            if (message.Header.Read == false)
            {
                message.Header.Read = true;

                Mail.SaveInbox = true;

                MessageListItem item = FindMessage(message);

                //crit - no sub item fonts
                /*if (item != null)
                    foreach (ContainerSubListViewItem subItem in item.SubItems)
                        subItem.Font = RegularFont;*/
            }
        }

        private void MessageHeader_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            string url = e.Url.OriginalString;
            string[] parts = url.Split(new char[] { ':' });

            if (parts.Length < 2)
                return;

            if( MessageList.SelectedItems.Count == 0)
                return;

            if (parts[0] == "about")
                return;

            MessageListItem item = MessageList.SelectedItems[0] as MessageListItem;
            
            if(item == null)
                return;

            LocalMail message = item.Message;

            if (parts[0] == "attach")
            {
                int index = int.Parse(parts[1]);

                

                for(int i = 0; i < message.Attached.Count; i++)
                    if (i == index)
                    {
                        SaveFileDialog save = new SaveFileDialog();
                        save.FileName = message.Attached[i].Name;
                        save.Title = "Save " + message.Attached[i].Name;

                        if (save.ShowDialog() == DialogResult.OK)
                            SaveFile(save.FileName, message, message.Attached[i]);
    
                        e.Cancel = true;
                        break;
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

        private void SaveFile(string path, LocalMail message, MailFile file)
        {
            try
            {
                FileStream stream = new FileStream(Mail.GetLocalPath(message.Header), FileMode.Open, FileAccess.Read, FileShare.Read);
                CryptoStream crypto = new CryptoStream(stream, message.Header.LocalKey.CreateDecryptor(), CryptoStreamMode.Read);

                // get past packet section of file
                const int buffSize = 4096;
                byte[] buffer = new byte[4096];
               
                ulong bytesLeft = message.Header.FileStart;
                while (bytesLeft > 0)
                {
                    int readSize = (bytesLeft > (ulong)buffSize) ? buffSize : (int)bytesLeft;
                    int read = crypto.Read(buffer, 0, readSize);
                    bytesLeft -= (ulong)read;
                }

                // setup write file
                FileStream outstream = new FileStream(path, FileMode.Create, FileAccess.Write);

                // read files, write the right one :P
                foreach (MailFile attached in message.Attached)
                {
                    bytesLeft = (ulong)attached.Size;
                    
                    while (bytesLeft > 0)
                    {
                        int readSize = (bytesLeft > (ulong)buffSize) ? buffSize : (int)bytesLeft;
                        int read = crypto.Read(buffer, 0, readSize);
                        bytesLeft -= (ulong)read;

                        if (attached == file)
                            outstream.Write(buffer, 0, read);
                    }
                }

                outstream.Close();

                Utilities.ReadtoEnd(crypto);
                crypto.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error Opening Mail: " + ex.Message);
            }

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
            MessageMenuItem item = sender as MessageMenuItem;

            if (item == null)
                return;

            if (MessageBox.Show(this, "Are you sure you want to delete this message?", "Delete", MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            Mail.DeleteLocal(item.Message, InboxButton.Checked);

            // remove from list box
            MessageListItem deleted = FindMessage(item.Message);

            if (deleted != null)
                MessageList.Items.Remove(deleted);
        }

        private void MessageList_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            MessageListItem item = MessageList.GetItemAt(e.Location) as MessageListItem;

            if (item == null)
                return;

            ContextMenuStripEx menu = new ContextMenuStripEx();

            if (InboxButton.Checked)
                menu.Items.Add(new MessageMenuItem(item.Message, "Reply", null, new EventHandler(Message_Reply)));

            menu.Items.Add(new MessageMenuItem(item.Message, "Forward", null, new EventHandler(Message_Forward)));
            menu.Items.Add("-");
            menu.Items.Add(new MessageMenuItem(item.Message, "Delete", MailRes.delete, new EventHandler(Message_Delete)));

            menu.Show(MessageList, e.Location);
        }
    }

    class MessageListItem : ContainerListViewItem
    {
        internal LocalMail Message;

        internal MessageListItem(LocalMail message, params string[] columns)
        {
            Message = message;

            if(message.Attached.Count > 1)
                ImageIndex = 1;
            else
                ImageIndex = 0;

            Text = columns[0];
           
            for(int i = 1; i < columns.Length; i++)
                SubItems.Add(columns[i]);
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
