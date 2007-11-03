using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace DeOps.Components.Storage
{
    internal partial class UnlockForm : Form
    {
        StorageView View;


        internal UnlockForm(StorageView view)
        {
            InitializeComponent();

            View = view;

            // label
            string text = "Unlock ";

            text += View.Links.GetProjectName(View.ProjectID) + "'s ";
            text += "File System to";

            MainLabel.Text = text;

            // link
            text = View.Core.User.RootPath + Path.DirectorySeparatorChar + View.Links.GetProjectName(View.ProjectID) + " Storage";

            PathLink.Text = text;
        }

        private void PathLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();

            dialog.SelectedPath = PathLink.Text;
            dialog.ShowNewFolderButton = true;

            if (dialog.ShowDialog(this) == DialogResult.OK)
                PathLink.Text = dialog.SelectedPath;
        }
        
        private void OkButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
        
        private void ExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }


    }
}