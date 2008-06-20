namespace RiseOp.Services.Transfer
{
    partial class TransferView
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.UploadTab = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.DownloadList = new RiseOp.Interface.TLVex.TreeListViewEx();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.UploadList = new RiseOp.Interface.TLVex.TreeListViewEx();
            this.FastTimer = new System.Windows.Forms.Timer(this.components);
            this.UploadTab.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // UploadTab
            // 
            this.UploadTab.Controls.Add(this.tabPage1);
            this.UploadTab.Controls.Add(this.tabPage2);
            this.UploadTab.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UploadTab.Location = new System.Drawing.Point(0, 0);
            this.UploadTab.Name = "UploadTab";
            this.UploadTab.SelectedIndex = 0;
            this.UploadTab.Size = new System.Drawing.Size(442, 293);
            this.UploadTab.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.DownloadList);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(434, 267);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Downloads";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // DownloadList
            // 
            this.DownloadList.BackColor = System.Drawing.SystemColors.Window;
            this.DownloadList.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.DownloadList.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.DownloadList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DownloadList.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.DownloadList.HeaderMenu = null;
            this.DownloadList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.DownloadList.ItemHeight = 20;
            this.DownloadList.ItemMenu = null;
            this.DownloadList.LabelEdit = false;
            this.DownloadList.Location = new System.Drawing.Point(3, 3);
            this.DownloadList.Name = "DownloadList";
            this.DownloadList.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.DownloadList.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.DownloadList.Size = new System.Drawing.Size(428, 261);
            this.DownloadList.SmallImageList = null;
            this.DownloadList.StateImageList = null;
            this.DownloadList.TabIndex = 0;
            this.DownloadList.Text = "treeListViewEx1";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.UploadList);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(434, 267);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Uploads";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // UploadList
            // 
            this.UploadList.BackColor = System.Drawing.SystemColors.Window;
            this.UploadList.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.UploadList.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.UploadList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UploadList.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.UploadList.HeaderMenu = null;
            this.UploadList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.UploadList.ItemHeight = 20;
            this.UploadList.ItemMenu = null;
            this.UploadList.LabelEdit = false;
            this.UploadList.Location = new System.Drawing.Point(3, 3);
            this.UploadList.Name = "UploadList";
            this.UploadList.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.UploadList.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.UploadList.Size = new System.Drawing.Size(428, 261);
            this.UploadList.SmallImageList = null;
            this.UploadList.StateImageList = null;
            this.UploadList.TabIndex = 0;
            this.UploadList.Text = "treeListViewEx2";
            // 
            // FastTimer
            // 
            this.FastTimer.Enabled = true;
            this.FastTimer.Interval = 250;
            this.FastTimer.Tick += new System.EventHandler(this.FastTimer_Tick);
            // 
            // TransferView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(442, 293);
            this.Controls.Add(this.UploadTab);
            this.Name = "TransferView";
            this.Text = "Transfers";
            this.Load += new System.EventHandler(this.TransferView_Load);
            this.UploadTab.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl UploadTab;
        private System.Windows.Forms.TabPage tabPage1;
        private RiseOp.Interface.TLVex.TreeListViewEx DownloadList;
        private System.Windows.Forms.TabPage tabPage2;
        private RiseOp.Interface.TLVex.TreeListViewEx UploadList;
        private System.Windows.Forms.Timer FastTimer;
    }
}