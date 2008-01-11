namespace RiseOp.Interface.Tools
{
    partial class SearchForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SearchForm));
            this.RadioUser = new System.Windows.Forms.RadioButton();
            this.RadioOp = new System.Windows.Forms.RadioButton();
            this.TextSearch = new System.Windows.Forms.TextBox();
            this.ButtonSearch = new System.Windows.Forms.Button();
            this.ListResults = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.LabelResults = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // RadioUser
            // 
            this.RadioUser.AutoSize = true;
            this.RadioUser.Location = new System.Drawing.Point(12, 12);
            this.RadioUser.Name = "RadioUser";
            this.RadioUser.Size = new System.Drawing.Size(47, 17);
            this.RadioUser.TabIndex = 0;
            this.RadioUser.TabStop = true;
            this.RadioUser.Text = "User";
            this.RadioUser.UseVisualStyleBackColor = true;
            // 
            // RadioOp
            // 
            this.RadioOp.AutoSize = true;
            this.RadioOp.Location = new System.Drawing.Point(65, 12);
            this.RadioOp.Name = "RadioOp";
            this.RadioOp.Size = new System.Drawing.Size(71, 17);
            this.RadioOp.TabIndex = 1;
            this.RadioOp.TabStop = true;
            this.RadioOp.Text = "Operation";
            this.RadioOp.UseVisualStyleBackColor = true;
            // 
            // TextSearch
            // 
            this.TextSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.TextSearch.Location = new System.Drawing.Point(12, 35);
            this.TextSearch.Name = "TextSearch";
            this.TextSearch.Size = new System.Drawing.Size(226, 20);
            this.TextSearch.TabIndex = 2;
            // 
            // ButtonSearch
            // 
            this.ButtonSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonSearch.Location = new System.Drawing.Point(244, 33);
            this.ButtonSearch.Name = "ButtonSearch";
            this.ButtonSearch.Size = new System.Drawing.Size(75, 23);
            this.ButtonSearch.TabIndex = 3;
            this.ButtonSearch.Text = "Search";
            this.ButtonSearch.UseVisualStyleBackColor = true;
            this.ButtonSearch.Click += new System.EventHandler(this.ButtonSearch_Click);
            // 
            // ListResults
            // 
            this.ListResults.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ListResults.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.ListResults.Location = new System.Drawing.Point(12, 61);
            this.ListResults.Name = "ListResults";
            this.ListResults.Size = new System.Drawing.Size(307, 197);
            this.ListResults.TabIndex = 4;
            this.ListResults.UseCompatibleStateImageBehavior = false;
            this.ListResults.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Dht";
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Client";
            // 
            // LabelResults
            // 
            this.LabelResults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LabelResults.AutoSize = true;
            this.LabelResults.Location = new System.Drawing.Point(12, 261);
            this.LabelResults.Name = "LabelResults";
            this.LabelResults.Size = new System.Drawing.Size(0, 13);
            this.LabelResults.TabIndex = 6;
            // 
            // SearchForm
            // 
            this.AcceptButton = this.ButtonSearch;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(331, 283);
            this.Controls.Add(this.LabelResults);
            this.Controls.Add(this.ListResults);
            this.Controls.Add(this.ButtonSearch);
            this.Controls.Add(this.TextSearch);
            this.Controls.Add(this.RadioOp);
            this.Controls.Add(this.RadioUser);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SearchForm";
            this.Text = "Search";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton RadioUser;
        private System.Windows.Forms.RadioButton RadioOp;
        private System.Windows.Forms.TextBox TextSearch;
        private System.Windows.Forms.Button ButtonSearch;
        private System.Windows.Forms.ListView ListResults;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.Label LabelResults;
    }
}