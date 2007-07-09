namespace DeOps.Components.Mail
{
    partial class MailView
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MailView));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.InboxButton = new System.Windows.Forms.ToolStripButton();
            this.OutboxButton = new System.Windows.Forms.ToolStripButton();
            this.ComposeButton = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.MessageList = new DeOps.Interface.TLVex.ContainerListViewEx();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.MessageHeader = new System.Windows.Forms.WebBrowser();
            this.MessageBody = new System.Windows.Forms.RichTextBox();
            this.ListImages = new System.Windows.Forms.ImageList(this.components);
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.toolStrip1.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.InboxButton,
            this.OutboxButton,
            this.ComposeButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.toolStrip1.Size = new System.Drawing.Size(378, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // InboxButton
            // 
            this.InboxButton.Checked = true;
            this.InboxButton.CheckOnClick = true;
            this.InboxButton.CheckState = System.Windows.Forms.CheckState.Checked;
            this.InboxButton.Image = ((System.Drawing.Image)(resources.GetObject("InboxButton.Image")));
            this.InboxButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.InboxButton.Name = "InboxButton";
            this.InboxButton.Size = new System.Drawing.Size(55, 22);
            this.InboxButton.Text = "Inbox";
            this.InboxButton.Click += new System.EventHandler(this.InboxButton_Click);
            // 
            // OutboxButton
            // 
            this.OutboxButton.CheckOnClick = true;
            this.OutboxButton.Image = ((System.Drawing.Image)(resources.GetObject("OutboxButton.Image")));
            this.OutboxButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.OutboxButton.Name = "OutboxButton";
            this.OutboxButton.Size = new System.Drawing.Size(63, 22);
            this.OutboxButton.Text = "Outbox";
            this.OutboxButton.Click += new System.EventHandler(this.OutboxButton_Click);
            // 
            // ComposeButton
            // 
            this.ComposeButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.ComposeButton.Image = ((System.Drawing.Image)(resources.GetObject("ComposeButton.Image")));
            this.ComposeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ComposeButton.Name = "ComposeButton";
            this.ComposeButton.Size = new System.Drawing.Size(83, 22);
            this.ComposeButton.Text = "Compose...";
            this.ComposeButton.Click += new System.EventHandler(this.ComposeButton_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.MessageList);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(378, 307);
            this.splitContainer1.SplitterDistance = 117;
            this.splitContainer1.TabIndex = 1;
            // 
            // MessageList
            // 
            this.MessageList.BackColor = System.Drawing.SystemColors.Window;
            this.MessageList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.MessageList.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.MessageList.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.MessageList.DisableHorizontalScroll = true;
            this.MessageList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MessageList.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.MessageList.HeaderMenu = null;
            this.MessageList.ItemMenu = null;
            this.MessageList.LabelEdit = false;
            this.MessageList.Location = new System.Drawing.Point(0, 0);
            this.MessageList.Name = "MessageList";
            this.MessageList.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.MessageList.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.MessageList.Size = new System.Drawing.Size(378, 117);
            this.MessageList.SmallImageList = null;
            this.MessageList.StateImageList = null;
            this.MessageList.TabIndex = 0;
            this.MessageList.Text = "containerListViewEx1";
            this.MessageList.MouseClick += new System.Windows.Forms.MouseEventHandler(this.MessageList_MouseClick);
            this.MessageList.SelectedIndexChanged += new System.EventHandler(MessageList_SelectedIndexChanged);
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.MessageHeader);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.BackColor = System.Drawing.Color.White;
            this.splitContainer2.Panel2.Controls.Add(this.MessageBody);
            this.splitContainer2.Size = new System.Drawing.Size(378, 186);
            this.splitContainer2.SplitterDistance = 56;
            this.splitContainer2.SplitterWidth = 1;
            this.splitContainer2.TabIndex = 0;
            // 
            // MessageHeader
            // 
            this.MessageHeader.AllowWebBrowserDrop = false;
            this.MessageHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MessageHeader.IsWebBrowserContextMenuEnabled = false;
            this.MessageHeader.Location = new System.Drawing.Point(0, 0);
            this.MessageHeader.MinimumSize = new System.Drawing.Size(20, 20);
            this.MessageHeader.Name = "MessageHeader";
            this.MessageHeader.ScrollBarsEnabled = false;
            this.MessageHeader.Size = new System.Drawing.Size(378, 56);
            this.MessageHeader.TabIndex = 0;
            this.MessageHeader.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.MessageHeader_Navigating);
            // 
            // MessageBody
            // 
            this.MessageBody.BackColor = System.Drawing.Color.White;
            this.MessageBody.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.MessageBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MessageBody.Location = new System.Drawing.Point(0, 0);
            this.MessageBody.Name = "MessageBody";
            this.MessageBody.ReadOnly = true;
            this.MessageBody.Size = new System.Drawing.Size(378, 129);
            this.MessageBody.TabIndex = 0;
            this.MessageBody.Text = "";
            // 
            // ListImages
            // 
            this.ListImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ListImages.ImageStream")));
            this.ListImages.TransparentColor = System.Drawing.Color.Transparent;
            this.ListImages.Images.SetKeyName(0, "mail.png");
            this.ListImages.Images.SetKeyName(1, "mail_attach.png");
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(10, 22);
            this.toolStripLabel1.Text = " ";
            // 
            // MailView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "MailView";
            this.Size = new System.Drawing.Size(378, 332);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton InboxButton;
        private System.Windows.Forms.ToolStripButton OutboxButton;
        private System.Windows.Forms.ToolStripButton ComposeButton;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.RichTextBox MessageBody;
        private System.Windows.Forms.WebBrowser MessageHeader;
        private System.Windows.Forms.ImageList ListImages;
        private DeOps.Interface.TLVex.ContainerListViewEx MessageList;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
    }
}
