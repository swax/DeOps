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
            RiseOp.Interface.TLVex.ToggleColumnHeader toggleColumnHeader1 = new RiseOp.Interface.TLVex.ToggleColumnHeader();
            RiseOp.Interface.TLVex.ToggleColumnHeader toggleColumnHeader2 = new RiseOp.Interface.TLVex.ToggleColumnHeader();
            this.FastTimer = new System.Windows.Forms.Timer(this.components);
            this.TransferList = new RiseOp.Interface.TLVex.TreeListViewEx();
            this.SuspendLayout();
            // 
            // FastTimer
            // 
            this.FastTimer.Enabled = true;
            this.FastTimer.Interval = 250;
            this.FastTimer.Tick += new System.EventHandler(this.FastTimer_Tick);
            // 
            // TransferList
            // 
            this.TransferList.BackColor = System.Drawing.SystemColors.Window;
            toggleColumnHeader1.Hovered = false;
            toggleColumnHeader1.Image = null;
            toggleColumnHeader1.Index = 0;
            toggleColumnHeader1.Pressed = false;
            toggleColumnHeader1.ScaleStyle = RiseOp.Interface.TLVex.ColumnScaleStyle.Spring;
            toggleColumnHeader1.Selected = false;
            toggleColumnHeader1.Text = "Details";
            toggleColumnHeader1.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader1.Visible = true;
            toggleColumnHeader1.Width = 438;
            toggleColumnHeader2.Hovered = false;
            toggleColumnHeader2.Image = null;
            toggleColumnHeader2.Index = 0;
            toggleColumnHeader2.Pressed = false;
            toggleColumnHeader2.ScaleStyle = RiseOp.Interface.TLVex.ColumnScaleStyle.Slide;
            toggleColumnHeader2.Selected = false;
            toggleColumnHeader2.Text = "Bitfield";
            toggleColumnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            toggleColumnHeader2.Visible = true;
            toggleColumnHeader2.Width = 200;
            this.TransferList.Columns.AddRange(new RiseOp.Interface.TLVex.ToggleColumnHeader[] {
            toggleColumnHeader1,
            toggleColumnHeader2});
            this.TransferList.ColumnSortColor = System.Drawing.Color.Gainsboro;
            this.TransferList.ColumnTrackColor = System.Drawing.Color.WhiteSmoke;
            this.TransferList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TransferList.GridLineColor = System.Drawing.Color.WhiteSmoke;
            this.TransferList.HeaderMenu = null;
            this.TransferList.ItemHeight = 20;
            this.TransferList.ItemMenu = null;
            this.TransferList.LabelEdit = false;
            this.TransferList.Location = new System.Drawing.Point(0, 0);
            this.TransferList.Name = "TransferList";
            this.TransferList.RowSelectColor = System.Drawing.SystemColors.Highlight;
            this.TransferList.RowTrackColor = System.Drawing.Color.WhiteSmoke;
            this.TransferList.Size = new System.Drawing.Size(642, 280);
            this.TransferList.SmallImageList = null;
            this.TransferList.StateImageList = null;
            this.TransferList.TabIndex = 1;
            this.TransferList.Text = "treeListViewEx2";
            this.TransferList.MouseClick += new System.Windows.Forms.MouseEventHandler(this.TransferList_MouseClick);
            // 
            // TransferView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(642, 280);
            this.Controls.Add(this.TransferList);
            this.Name = "TransferView";
            this.Text = "Transfers";
            this.Load += new System.EventHandler(this.TransferView_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer FastTimer;
        private RiseOp.Interface.TLVex.TreeListViewEx TransferList;
    }
}