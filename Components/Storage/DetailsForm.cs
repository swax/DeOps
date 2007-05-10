using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface;
using DeOps.Interface.TLVex;


namespace DeOps.Components.Storage
{
    internal partial class DetailsForm : Form
    {
        StorageView View;

        FolderNode TargetFolder;
        FileItem TargetFile;


        internal DetailsForm(StorageView view, FolderNode folder, FileItem file)
        {
            InitializeComponent();

            View = view;
            TargetFolder = folder;
            TargetFile = file;

            EnableControls(View.Working != null);

            NameBox.Text = file.Text;

            SizeLabel.Text = ((StorageFile)file.Details).InternalSize.ToString() + " bytes";
        }

        internal DetailsForm(StorageView view, FolderNode folder)
        {
            InitializeComponent();

            View = view;
            TargetFolder = folder;

            EnableControls(View.Working != null);

            NameBox.Text = folder.Text;

            SizeLabel.Text = CalculateFolderSize(folder, 0).ToString() + " bytes";
        }

        private void EnableControls(bool enabled)
        {
            NameBox.Enabled = enabled;

            AddLink.Visible = enabled;
            RemoveLink.Visible = enabled;
        }

        private long CalculateFolderSize(FolderNode folder, long total)
        {
            foreach (FileItem item in folder.Files.Values)
                if (!item.Details.IsFlagged(StorageFlags.Archived))
                    total += ((StorageFile)item.Details).InternalSize;

            foreach (FolderNode node in folder.Folders.Values)
                if (!node.Details.IsFlagged(StorageFlags.Archived))
                    total += CalculateFolderSize(node, total);

            return total;
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (View.Working == null)
            {
                Close();
                return;
            }
            
            try
            {
                if (NameBox.Text.Trim().CompareTo("") == 0)
                    throw new Exception("Enter in a name for the file");


                if (TargetFile != null)
                    View.Working.SetFileDetails(TargetFile.GetPath(), NameBox.Text); 
                else
                    View.Working.SetFolderDetails(TargetFolder.GetPath(), NameBox.Text); 


                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


    }
}