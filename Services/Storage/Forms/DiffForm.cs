using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;

namespace RiseOp.Services.Storage
{
    internal partial class DiffForm : Form
    {
        internal OpCore Core;
        internal InfoPanel Info;

        internal StorageFile Target;
        internal ulong TargetID;
        internal bool TargetHistory;

        internal DiffForm(InfoPanel info, ulong whoID, string what, StorageFile file, bool history)
        {
            InitializeComponent();

            Core = info.ParentView.Core;
            Info = info;
            Target = file;
            TargetID = whoID;
            TargetHistory = history;

            Text = Target.Name + " Differences";
           
            // set what txt
                // my/ben's Changes
                // my/ben's Integrated Changes
                // my/ben's History from <date>
            
            string who = (whoID == Core.LocalDhtID) ? "My" : (Core.Links.GetName(whoID) + "'s");

            WhatLabel.Text = who + " " + what;

          
            // local
            if (!info.CurrentFile.Temp)
            {
                if (Utilities.MemCompare(Target.InternalHash, ((StorageFile)Info.CurrentFile.Details).InternalHash))
                    LocalNote.Text = "Identical";
            }
            else
                CurrentRadio.Enabled = false;

            // changes
            foreach (ChangeRow change in Info.SortChanges(Info.CurrentChanges))
                ChangesCombo.Items.Add(new ComboFileItem(this, change.ID, (StorageFile) change.Item));

            if (ChangesCombo.Items.Count > 0)
                ChangesCombo.SelectedIndex = 0;
            else
                ChangesRadio.Enabled = false;

            // integrated
            foreach (ChangeRow integrated in Info.SortChanges(Info.CurrentIntegrated))
                IntegratedCombo.Items.Add(new ComboFileItem(this, integrated.ID, (StorageFile)integrated.Item));

            if (IntegratedCombo.Items.Count > 0)
                IntegratedCombo.SelectedIndex = 0;
            else
                IntegratedRadio.Enabled = false;

            // history
            info.CurrentFile.Archived.LockReading(delegate()
            {
                foreach (StorageFile item in info.CurrentFile.Archived)
                    HistoryCombo.Items.Add(new ComboFileItem(this, 0, item));
            });

            if (HistoryCombo.Items.Count > 0)
                HistoryCombo.SelectedIndex = 0;
            else
                HistoryRadio.Enabled = false;

            // using
            UsingCombo.Items.Add("WinMerge");
            UsingCombo.Items.Add("Open Seperately");
            UsingCombo.Items.Add("Another Tool...");
            UsingCombo.SelectedIndex = 0;
        }

        private void SetCheck(RadioButton button)
        {
            ChangesCombo.Enabled = (ChangesRadio == button);
            IntegratedCombo.Enabled = (IntegratedRadio == button);
            HistoryCombo.Enabled = (HistoryRadio == button);
        }

        private void DiffForm_Load(object sender, EventArgs e)
        {

        }

        private void LocalRadio_CheckedChanged(object sender, EventArgs e)
        {
            SetCheck(CurrentRadio);
        }

        private void ChangesRadio_CheckedChanged(object sender, EventArgs e)
        {
            SetCheck(ChangesRadio);
        }

        private void IntegratedRadio_CheckedChanged(object sender, EventArgs e)
        {
            SetCheck(IntegratedRadio);
        }

        private void HistoryRadio_CheckedChanged(object sender, EventArgs e)
        {
            SetCheck(HistoryRadio);
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {

            // get selected file / id
            StorageFile selected = null;
            ulong selectedID = 0;
            bool selectedHistory = false;
            string selectedText = "";

            if (CurrentRadio.Checked)
            {
                selected = (StorageFile)Info.CurrentFile.Details;
                selectedID = Info.ParentView.DhtID;
                selectedText = "Current";
            }

            if (ChangesRadio.Checked)
            {
                ComboFileItem item = (ComboFileItem)ChangesCombo.SelectedItem;

                selected = item.File;
                selectedID = item.ID;
                selectedText = "Changes";
            }

            if (IntegratedRadio.Checked)
            {
                ComboFileItem item = (ComboFileItem)IntegratedCombo.SelectedItem;

                selected = item.File;
                selectedID = item.ID;
                selectedText = "Integrated Changes";
            }

            if (HistoryRadio.Checked)
            {
                ComboFileItem item = (ComboFileItem)HistoryCombo.SelectedItem;

                selected = item.File;
                selectedID = Info.ParentView.DhtID;
                selectedHistory = HistoryCombo.SelectedIndex != 0;
                selectedText = "History from " + item.File.Date.ToLocalTime().ToString();
            }

            string who = (selectedID == Core.LocalDhtID) ? "My " : (Core.Links.GetName(selectedID) + "'s ");
            selectedText = who + selectedText;

            if (selected == null)
            {
                MessageBox.Show("No file Selected to Compare to");
                return;
            }

            // unlock files
            List<LockError> errors = new List<LockError>();

            Cursor = Cursors.WaitCursor;
            string fileA = Info.Storages.UnlockFile(TargetID, Info.ParentView.ProjectID, Info.CurrentFolder.GetPath(), Target, TargetHistory, errors);

            string fileB = Info.Storages.UnlockFile(selectedID, Info.ParentView.ProjectID, Info.CurrentFolder.GetPath(), selected, selectedHistory, errors);
            Cursor = Cursors.Default;

            if (errors.Count > 0)
            {
                LockMessage.Alert(Info.ParentView, errors);
                return;
            }

            if (UsingCombo.Text == "WinMerge")
            {
                // /e close with esc
                // /ub dont add to MRU
                // /wl left side read only
                // /wr right side read only (only NOT if local dht/not history etc..)
                // /dl left side desc, target info
                // /dr right side desc, current file info

                if (!File.Exists("C:\\Program files\\WinMerge\\WinMerge.exe"))
                {
                    MessageBox.Show("Can't find WinMerge");
                    return;
                }

                string arguments = "/e /ub ";
                arguments += "/dl \"" + WhatLabel.Text + "\" ";
                arguments += "/dr \"" + selectedText + "\" ";

                if (TargetID != Core.LocalDhtID || TargetHistory)
                    arguments += "/wl ";
                if (selectedID != Core.LocalDhtID || selectedHistory)
                    arguments += "/wr ";

                arguments += "\"" + fileA + "\" ";
                arguments += "\"" + fileB + "\" ";

                Process.Start("C:\\Program files\\WinMerge\\WinMerge.exe", arguments);
            }

            if (UsingCombo.Text == "Open Seperately")
            {

                // open
                if (fileA != null && File.Exists(fileA))
                    System.Diagnostics.Process.Start(fileA);

                if (fileB != null && File.Exists(fileB))
                    System.Diagnostics.Process.Start(fileB);


                Info.CurrentFile.UpdateInterface();
                Info.RefreshItem();
            }

            if (UsingCombo.Text == "Another Tool...")
            {


            }


            // if success
            Close();
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
    }

    class ComboFileItem
    {
        string Text = "";

        internal StorageFile File;
        internal ulong ID;

        internal ComboFileItem(DiffForm diff, ulong id, StorageFile file)
        {
            File = file;
            ID = id;

            if (id == 0)
                Text = file.Date.ToLocalTime().ToString();
            else
                Text = diff.Core.Links.GetName(id);

            if (Utilities.MemCompare(file.InternalHash, diff.Target.InternalHash))
                Text += " (Identical)";
            else if(!diff.Info.Storages.FileExists(file) )
                Text += " (Unavailable)";
                    
        }

        public override string ToString()
        {
            return Text;
        }
    }
}