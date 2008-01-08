using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Interface;
using DeOps.Interface.TLVex;
using DeOps.Services.Link;


namespace DeOps.Services.Storage
{
    internal partial class DetailsForm : Form
    {
        StorageView View;
        LinkControl Links;

        FolderNode TargetFolder;
        FileItem TargetFile;


        internal DetailsForm(StorageView view, FolderNode folder, FileItem file)
        {
            InitializeComponent();

            View = view;
            Links = View.Links;

            TargetFolder = folder;
            TargetFile = file;

            EnableControls(View.Working != null);

            NameBox.Text = file.Text;

            SizeLabel.Text = Utilities.CommaIze(((StorageFile)file.Details).InternalSize.ToString()) + " Bytes";

            LoadVis(file.Details.Scope);
        }

        internal DetailsForm(StorageView view, FolderNode folder)
        {
            InitializeComponent();

            View = view;
            Links = View.Links;
            TargetFolder = folder;

            EnableControls(View.Working != null);

            NameBox.Text = folder.Text;

            SizeLabel.Text = "Contains " + Utilities.CommaIze(CalculateFolderSize(folder, 0).ToString()) + " Bytes";

            LoadVis(folder.Details.Scope);
        }

        private void LoadVis(Dictionary<ulong, short> scope)
        {
            foreach (ulong id in scope.Keys)
                VisList.Items.Add(new VisItem(Links.GetName(id), id, scope[id]));
        }

        private void EnableControls(bool enabled)
        {
            NameBox.Enabled = enabled;

            AddLink.Visible = enabled;
            RemoveLink.Visible = enabled;
            EditLink.Visible = enabled;
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

        private void AddLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            AddLinks form = new AddLinks(Links, View.ProjectID);


            if (form.ShowDialog(this) == DialogResult.OK)
            {
                foreach (ulong id in form.People)
                {
                    bool add = true;

                    foreach (VisItem item in VisList.Items)
                        if (item.DhtID == id)
                            add = false;

                    if (add)
                        VisList.Items.Add(new VisItem(Links.GetName(id), id, -1));
                }
            }
        }

        private void VisList_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateLinkVis(VisList.SelectedItems.Count > 0);
        }

        private void UpdateLinkVis(bool visible)
        {
            RemoveLink.Visible = visible;
            EditLink.Visible = visible;
        }


        private void RemoveLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            List<VisItem> removeList = new List<VisItem>();

            foreach(VisItem item in VisList.SelectedItems)
                removeList.Add(item);

            foreach (VisItem item in removeList)
                VisList.Items.Remove(item);

            VisList.Refresh();

            UpdateLinkVis(VisList.Items.Count > 0); // remove item doesn't update selected immediately
        }

        private void EditLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            EditSelected();
        }

        private void VisList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            EditSelected();
        }

        private void EditSelected()
        {
            if (VisList.SelectedItems.Count == 0)
                return;

            VisItem selected = (VisItem)VisList.SelectedItems[0];

            GetTextDialog getText = new GetTextDialog("Sub-Levels", "Visible how many levels down from " + selected.Name + "? 0 for no one, -1 for everyone.", selected.Levels.ToString());

            if (getText.ShowDialog() == DialogResult.OK)
            {
                short levels;
                short.TryParse(getText.ResultBox.Text, out levels);

                if (levels >= -1)
                    selected.Levels = levels;

                selected.Update();
            }
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

                Dictionary<ulong, short> scope = new Dictionary<ulong, short>();
                foreach (VisItem item in VisList.Items)
                    scope[item.DhtID] = item.Levels;

                try
                {
                    if (TargetFile != null)
                        View.Working.SetFileDetails(TargetFile.GetPath(), NameBox.Text, scope);
                    else
                        View.Working.SetFolderDetails(TargetFolder.GetPath(), NameBox.Text, scope);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return; // dont close
                }

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Close();
        }



    }


    internal class VisItem : ContainerListViewItem 
    {
        internal string Name;
        internal ulong DhtID;
        internal short Levels = -1;

        internal VisItem(string name, ulong id, short levels)
        {
            Name = name;
            DhtID = id;
            Levels = levels;

            SubItems.Add("");

            Update();
        }

        internal void Update()
        {
            Text = Name;

            string sublevels = Levels.ToString();

            if (Levels == -1)
                sublevels = "All";
            else if (Levels == 0)
                sublevels = "None";

            SubItems[0].Text = sublevels;
        }
    }
}