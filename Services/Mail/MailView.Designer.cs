namespace RiseOp.Services.Mail
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
            RiseOp.Interface.TLVex.ToggleColumnHeader toggleColumnHeader1 = new RiseOp.Interface.TLVex.ToggleColumnHeader();
            RiseOp.Interface.TLVex.ToggleColumnHeader toggleColumnHeader2 = new RiseOp.Interface.TLVex.ToggleColumnHeader();
            RiseOp.Interface.TLVex.ToggleColumnHeader toggleColumnHeader3 = new RiseOp.Interface.TLVex.ToggleColumnHeader();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.ReceivedButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.SentButton = new System.Windows.Forms.ToolStripButton();
            this.ComposeButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripLabel3 = new System.Windows.Forms.ToolStripLabel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.MessageView = new RiseOp.Interface.TLVex.TreeListViewEx();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.MessageHeader = new RiseOp.Interface.Views.WebBrowserEx();
            this.MessageBody = new RiseOp.Interface.Views.RichTextBoxEx();
            this.ListImages = new System.Windows.Forms.ImageList(this.components);
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
            this.ReceivedButton,
            this.toolStripLabel2,
            this.SentButton,
            this.ComposeButton,
            this.toolStripLabel3});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(453, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(10, 22);
            this.toolStripLabel1.Text = " ";
            // 
            // ReceivedButton
            // 
            this.ReceivedButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.ReceivedButton.Checked = true;
            this.ReceivedButton.CheckOnClick = true;
            this.ReceivedButton.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ReceivedButton.Image = ((System.Drawing.Image)(resources.GetObject("ReceivedButton.Image")));
            this.ReceivedButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ReceivedButton.Name = "ReceivedButton";
            this.ReceivedButton.Size = new System.Drawing.Size(71, 22);
            this.ReceivedButton.Text = "Received";
            this.ReceivedButton.Click += new System.EventHandler(this.InboxButton_Click);
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel2.AutoSize = false;
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(5, 22);
            // 
            // SentButton
            // 
            this.SentButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.SentButton.Checked = true;
            this.SentButton.CheckOnClick = true;
            this.SentButton.CheckState = System.Windows.Forms.CheckState.Checked;
            this.SentButton.Image = ((System.Drawing.Image)(resources.GetObject("SentButton.Image")));
            this.SentButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.SentButton.Name = "SentButton";
            this.SentButton.Size = new System.Drawing.Size(49, 22);
            this.SentButton.Text = "Sent";
            this.SentButton.Click += new System.EventHandler(this.OutboxButton_Click);
            // 
            // ComposeButton
            // 
            this.ComposeButton.Image = ((System.Drawing.Image)(resources.GetObject("ComposeButton.Image")));
            this.ComposeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ComposeButton.Name = "ComposeButton";
            this.ComposeButton.Size = new System.Drawing.Size(83, 22);
            this.ComposeButton.Text = "Compose...";
            this.ComposeButton.Click += new System.EventHandler(this.ComposeButton_Click);
            // 
            // toolStripLabel3
            // 
            this.toolStripLabel3.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel3.Name = "toolStripLabel3";
            this.toolStripLabel3.Size = new System.Drawing.Size(37, 22);
            this.toolStripLabel3.Text = "Show:";
            // 
            // splitContainer1
            // 
            this.splitContainer1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.MessageView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(453, 282);
            this.splitContainer1.SplitterDistance = 107;
            this.splitContainer1.TabIndex = 1;
            // 
            // MessageView
            // 
            this.MessageView.BackColor = System.Drawing.SystemColors.Window;
            toggleColumnHeader1.Hovered = false;
            toggleColumnHeader1.Image = null;
            toggleColumnHeader1.Index = 0;
            toggleColumnHeader1.Pressed = false;
            toggleColumnHeader1.ScaleStyle = RiseOp.Interface.TLVex.ColumnScaleStyle.Spring;
            toggleColumnHeader1.Selected = false;
            toggleColumnHeader1.Text = "Message";
            toggleColumnHeader1.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader1.Visible = true;
            toggleColumnHeader1.Width = 179;
            toggleColumnHeader2.Hovered = false;
            toggleColumnHeader2.Image = null;
            toggleColumnHeader2.Index = 0;
            toggleColumnHeader2.Pressed = false;
            toggleColumnHeader2.ScaleStyle = RiseOp.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader2.Selected = false;
            toggleColumnHeader2.Text = "Who";
            toggleColumnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader2.Visible = true;
            toggleColumnHeader2.Width = 120;
            toggleColumnHeader3.Hovered = false;
            toggleColumnHeader3.Image = null;
            toggleColumnHeader3.Index = 0;
            toggleColumnHeader3.Pressed = false;
            toggleColumnHeader3.ScaleStyle = RiseOp.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader3.Selected = false;
            toggleColumnHeader3.Text = "Date";
            toggleColumnHeader3.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader3.Visible = true;
            toggleColumnHeader3.Width = 150;
            this.MessageView.Columns.AddRange(new RiseOp.Interface.TLVex.ToggleColumnHeader[] {
            toggleColumnHeader1,
            toggleColumnHeader2,
            toggleColumnHeader3});
            this.MessageView.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.MessageView.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.MessageView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MessageView.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.MessageView.HeaderMenu = null;
            this.MessageView.ItemHeight = 20;
            this.MessageView.ItemMenu = null;
            this.MessageView.LabelEdit = false;
            this.MessageView.Location = new System.Drawing.Point(0, 0);
            this.MessageView.MultiSelect = true;
            this.MessageView.Name = "MessageView";
            this.MessageView.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.MessageView.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.MessageView.Size = new System.Drawing.Size(453, 107);
            this.MessageView.SmallImageList = null;
            this.MessageView.StateImageList = null;
            this.MessageView.TabIndex = 0;
            this.MessageView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.MessageView_MouseClick);
            this.MessageView.SelectedItemChanged += new System.EventHandler(this.MessageView_SelectedItemChanged);
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
            this.splitContainer2.Size = new System.Drawing.Size(453, 171);
            this.splitContainer2.SplitterDistance = 68;
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
            this.MessageHeader.ScriptErrorsSuppressed = true;
            this.MessageHeader.ScrollBarsEnabled = false;
            this.MessageHeader.Size = new System.Drawing.Size(453, 68);
            this.MessageHeader.TabIndex = 0;
            this.MessageHeader.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.MessageHeader_Navigating);
            // 
            // MessageBody
            // 
            this.MessageBody.BackColor = System.Drawing.Color.White;
            this.MessageBody.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.MessageBody.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MessageBody.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MessageBody.Location = new System.Drawing.Point(0, 0);
            this.MessageBody.Name = "MessageBody";
            this.MessageBody.ReadOnly = true;
            this.MessageBody.Size = new System.Drawing.Size(453, 102);
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
            // MailView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "MailView";
            this.Size = new System.Drawing.Size(453, 307);
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
        private System.Windows.Forms.ToolStripButton ReceivedButton;
        private System.Windows.Forms.ToolStripButton SentButton;
        private System.Windows.Forms.ToolStripButton ComposeButton;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private RiseOp.Interface.Views.RichTextBoxEx MessageBody;
        private RiseOp.Interface.Views.WebBrowserEx MessageHeader;
        private System.Windows.Forms.ImageList ListImages;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripLabel toolStripLabel3;
        private RiseOp.Interface.TLVex.TreeListViewEx MessageView;
    }
}
