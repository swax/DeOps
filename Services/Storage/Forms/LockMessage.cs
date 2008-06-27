using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using RiseOp.Interface.TLVex;


namespace RiseOp.Services.Storage
{
    internal partial class LockMessage : RiseOp.Interface.CustomIconForm
    {
        LockErrorType Type;
        List<LockError> Errors;

        internal List<LockError> NewErrors = new List<LockError>();

        StorageView ParentView;
        internal StorageService Storages;

        Dictionary<string, int> IconMap = new Dictionary<string, int>();

        internal string RootPath;


        internal LockMessage(StorageView view, LockErrorType type, List<LockError> errors)
        {
            InitializeComponent();

            ParentView = view;
            Storages = view.Storages;
            Type = type;
            Errors = errors;

            RootPath = view.Storages.GetRootPath(view.UserID, view.ProjectID);
        }

        internal static void Alert(StorageView view, List<LockError> errors)
        {
            if (errors.Count == 0)
                return;

            List<LockError> temps = new List<LockError>();
            List<LockError> blocked = new List<LockError>();
            
            List<LockError> unexpected = new List<LockError>();
            List<LockError> existing = new List<LockError>();
            List<LockError> missing = new List<LockError>();
            

            foreach (LockError error in errors)
                switch (error.Type)
                {
                    case LockErrorType.Temp:
                        temps.Add(error);
                        break;

                    case LockErrorType.Blocked:
                        blocked.Add(error);
                        break;

                    case LockErrorType.Unexpected:
                        unexpected.Add(error);
                        break;

                    case LockErrorType.Existing:
                        existing.Add(error);
                        break;

                    case LockErrorType.Missing:
                        missing.Add(error);
                        break;
                }


            LockMessage message = null;

            while (temps.Count > 0)
            {
                message = new LockMessage(view, LockErrorType.Temp, temps);
                message.ShowDialog(view);
                temps = message.NewErrors;
            }

            while (blocked.Count > 0)
            {
                message = new LockMessage(view, LockErrorType.Blocked, blocked);
                message.ShowDialog(view);
                blocked = message.NewErrors;
            }

            while (unexpected.Count > 0)
            {
                message = new LockMessage(view, LockErrorType.Unexpected, unexpected);
                message.ShowDialog(view);
                unexpected = message.NewErrors;
            }

            while (existing.Count > 0)
            {
                message = new LockMessage(view, LockErrorType.Existing, existing);
                message.ShowDialog(view);
                existing = message.NewErrors;
            }

            while (missing.Count > 0)
            {
                message = new LockMessage(view, LockErrorType.Missing, missing);
                message.ShowDialog(view);
                missing = message.NewErrors;
            }

        }

        private void LockMessage_Load(object sender, EventArgs e)
        {
            switch (Type)
            {
                case LockErrorType.Temp:
                    Text = "Lock: Temp Files";
                    Note.Text = "Delete these temp files?";
                    button1.Visible = false;
                    button2.Text = "Delete";
                    button3.Text = "Ignore";
                    break;

                case LockErrorType.Blocked:
                    Text = "Lock: Error";
                    Note.Text = "These files were unable to lock because of the following errors";
                    button1.Visible = false;
                    button2.Text = "Retry";
                    button3.Text = "Ignore";
                    break;

                case LockErrorType.Unexpected:
                    Text = "Unlock: Unexpected";
                    Note.Text = "There were problems unlocking these files";
                    button1.Visible = false;
                    button2.Text = "Retry";
                    button3.Text = "Ignore";
                    break;

                case LockErrorType.Existing:
                    Text = "Unlock: Conflict";
                    Note.Text = "Files with the same name already exist in the spot where the files below would be unlocked to";
                    button1.Text = "Overwrite";
                    button2.Text = "Use";
                    button3.Text = "Cancel";
                    break;

                case LockErrorType.Missing:
                    Text = "Unlock: Missing";
                    Note.Text = "Could not unlock the following files because they are not available on this machine";
                    button1.Visible = false;
                    button2.Text = "Download";
                    button3.Text = "Ignore";
                    break;
            }

            ErrorList.SmallImageList = new List<Image>();
            ErrorList.SmallImageList.Add(new Bitmap(16, 16));
            ErrorList.SmallImageList.Add(StorageRes.Folder);

            foreach (LockError error in Errors)
                ErrorList.Items.Add(new ErrorItem(this, error));
        }

        internal int GetImageIndex(LockError error)
        {
            if (!error.IsFile)
                return 1;

            string ext = Path.GetExtension(error.Path);

            if (!IconMap.ContainsKey(ext))
            {
                IconMap[ext] = ErrorList.SmallImageList.Count;

                Bitmap img = Win32.GetIcon(ext);


                if (img == null)
                    img = new Bitmap(16, 16);

                ErrorList.SmallImageList.Add(img);
            }

            return IconMap[ext];
        }

        private void ErrorList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (ErrorList.SelectedItems.Count == 0)
                return;

            ErrorItem item = (ErrorItem) ErrorList.SelectedItems[0];

            if (item.Error.IsFile)
                Process.Start(item.Error.Path);
            else
                Utilities.OpenFolder(item.Error.Path);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            switch (Type)
            {
                case LockErrorType.Temp:
                    break;

                case LockErrorType.Blocked:
                    break;

                case LockErrorType.Unexpected:
                    break;

                case LockErrorType.Existing:
                    //button1.Text = "Overwrite";

                    Cursor = Cursors.WaitCursor;
                    foreach (LockError error in Errors)
                    {
                        // delete existing file
                        try
                        {
                            File.Delete(error.Path);
                        }
                        catch
                        {
                            NewErrors.Add(error);
                            continue;
                        }

                        // unlock new file / retry
                        string path = Path.GetDirectoryName(error.Path);
                        Storages.UnlockFile(ParentView.UserID, ParentView.ProjectID, path.Replace(RootPath, ""), error.File, error.History, NewErrors);
                    }
                    Cursor = Cursors.Default;

                    break;

                case LockErrorType.Missing:
                    break;
            }


            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            switch (Type)
            {
                case LockErrorType.Temp:
                    //button2.Text = "Delete";

                    foreach (LockError error in Errors)
                        if (error.IsFile)
                            Storages.DeleteFile(error.Path, NewErrors, true);
                        else
                            Storages.DeleteFolder(error.Path, NewErrors, null);

                    break;

                case LockErrorType.Blocked:
                    //button2.Text = "Retry";

                    foreach (LockError error in Errors)
                        if (error.IsFile)
                            Storages.DeleteFile(error.Path, NewErrors, true);
                        else
                            Storages.DeleteFolder(error.Path, NewErrors, null);

                    break;

                case LockErrorType.Unexpected:
                    //button2.Text = "Retry";

                    // try to unlock again
                    Cursor = Cursors.WaitCursor;

                    foreach (LockError error in Errors)
                        if (error.IsFile)
                        {
                            string path = Path.GetDirectoryName(error.Path);
                            Storages.UnlockFile(ParentView.UserID, ParentView.ProjectID, path.Replace(RootPath, ""), error.File, error.History, NewErrors);
                        }
                        // dont know to unlock subs or not
                        else
                            ParentView.UnlockFolder(ParentView.GetFolderNode(error.Path.Replace(RootPath, "")), error.Subs, NewErrors);

                    Cursor = Cursors.Default;

                    break;

                case LockErrorType.Existing:
                    //button2.Text = "Use";

                    if (ParentView.Working != null)
                        foreach (LockError error in Errors)
                        {
                            string path = error.Path.Replace(RootPath, "");

                            LocalFolder folder = ParentView.Working.GetLocalFolder(Utilities.StripOneLevel(path));

                            if (folder == null)
                                return;

                            LocalFile file = folder.GetFile(error.File.Name);

                            file.Info.SetFlag(StorageFlags.Unlocked);
                            Storages.MarkforHash(file, error.Path, ParentView.ProjectID, folder.GetPath());
                        }

                    break;

                case LockErrorType.Missing:
                    //button2.Text = "Download";

                    foreach (LockError error in Errors)
                        Storages.DownloadFile(ParentView.UserID, error.File);

                    ParentView.WatchTransfers = true;

                    break;
            }

            Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            switch (Type)
            {
                case LockErrorType.Temp:
                    //button3.Text = "Ignore";
                    break;

                case LockErrorType.Blocked:
                    //button3.Text = "Ignore";
                    break;

                case LockErrorType.Unexpected:
                    //button3.Text = "Ignore";
                    break;

                case LockErrorType.Existing:
                    //button3.Text = "Cancel";
                    break;

                case LockErrorType.Missing:
                    //button3.Text = "Ignore";
                    break;
            }

            Close();
        }
    }

           


    internal class ErrorItem : ContainerListViewItem
    {
        internal LockError Error;

        internal ErrorItem(LockMessage parent, LockError error)
        {
            Error = error;

            ImageIndex = parent.GetImageIndex(error);
            Text = error.Path.Replace(parent.RootPath, "");

            if (error.Message.Length > 0)
                Text += " (" + error.Message + ")";
        }
    }

    enum LockErrorType { Temp, Blocked,  // lock errors
                         Unexpected, Existing, Missing  }; // unlock errors

    internal class LockError
    {
        internal string Path;
        internal string Message;
        internal bool IsFile;
        internal LockErrorType Type;

        // special case
        internal StorageFile File;
        internal bool History;
        internal bool Subs;


        internal LockError(string path, string message, bool isFile, LockErrorType type)
        {       
            Path = path;
            Message = message;
            IsFile = isFile;
            Type = type;
        }

        // used for unexpected (file) and existing errors
        internal LockError(string path, string message, bool isFile, LockErrorType type, StorageFile file, bool history)
        {
            Path = path;
            Message = message;
            IsFile = isFile;
            Type = type;
            File = file;
            History = history;
        }
    }
}