using DeOps.Interface;

namespace DeOps.Services.Chat
{
    partial class RoomView
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.MessageTextBox = new System.Windows.Forms.RichTextBox();
            this.InputControl = new DeOps.Interface.TextInput();
            this.MemberTree = new DeOps.Interface.TLVex.TreeListViewEx();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.BackColor = System.Drawing.Color.White;
            this.splitContainer1.Panel2.Controls.Add(this.MemberTree);
            this.splitContainer1.Size = new System.Drawing.Size(315, 194);
            this.splitContainer1.SplitterDistance = 170;
            this.splitContainer1.SplitterWidth = 2;
            this.splitContainer1.TabIndex = 0;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.MessageTextBox);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.InputControl);
            this.splitContainer2.Size = new System.Drawing.Size(170, 194);
            this.splitContainer2.SplitterDistance = 163;
            this.splitContainer2.SplitterWidth = 2;
            this.splitContainer2.TabIndex = 0;
            // 
            // MessageTextBox
            // 
            this.MessageTextBox.BackColor = System.Drawing.Color.White;
            this.MessageTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.MessageTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MessageTextBox.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MessageTextBox.Location = new System.Drawing.Point(0, 0);
            this.MessageTextBox.Name = "MessageTextBox";
            this.MessageTextBox.ReadOnly = true;
            this.MessageTextBox.Size = new System.Drawing.Size(170, 163);
            this.MessageTextBox.TabIndex = 0;
            this.MessageTextBox.Text = "";
            // 
            // InputControl
            // 
            this.InputControl.AcceptTabs = false;
            this.InputControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InputControl.EnterClears = true;
            this.InputControl.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InputControl.Location = new System.Drawing.Point(0, 0);
            this.InputControl.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.InputControl.Name = "InputControl";
            this.InputControl.ReadOnly = false;
            this.InputControl.ShowFontStrip = false;
            this.InputControl.Size = new System.Drawing.Size(170, 29);
            this.InputControl.TabIndex = 0;
            // 
            // MemberTree
            // 
            this.MemberTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.MemberTree.BackColor = System.Drawing.SystemColors.Window;
            this.MemberTree.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.MemberTree.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.MemberTree.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.MemberTree.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.MemberTree.HeaderMenu = null;
            this.MemberTree.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.MemberTree.ItemHeight = 20;
            this.MemberTree.ItemMenu = null;
            this.MemberTree.LabelEdit = false;
            this.MemberTree.Location = new System.Drawing.Point(0, 6);
            this.MemberTree.Name = "MemberTree";
            this.MemberTree.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.MemberTree.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.MemberTree.ShowPlusMinus = false;
            this.MemberTree.Size = new System.Drawing.Size(143, 188);
            this.MemberTree.SmallImageList = null;
            this.MemberTree.StateImageList = null;
            this.MemberTree.TabIndex = 0;
            this.MemberTree.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.MemberTree_MouseDoubleClick);
            this.MemberTree.MouseClick += new System.Windows.Forms.MouseEventHandler(this.MemberTree_MouseClick);
            // 
            // RoomView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.Controls.Add(this.splitContainer1);
            this.Name = "RoomView";
            this.Size = new System.Drawing.Size(315, 194);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.RichTextBox MessageTextBox;
        private TextInput InputControl;
        private DeOps.Interface.TLVex.TreeListViewEx MemberTree;

    }
}
