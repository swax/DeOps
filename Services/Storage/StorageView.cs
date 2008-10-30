using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections.Specialized;

using RiseOp.Interface;
using RiseOp.Interface.TLVex;
using RiseOp.Interface.Views;

using RiseOp.Implementation;
using RiseOp.Implementation.Protocol;
using RiseOp.Services.Trust;


namespace RiseOp.Services.Storage
{
    internal partial class StorageView : ViewShell
    {
        internal OpCore Core;
        internal StorageService Storages;
        internal TrustService Trust;

        internal ulong UserID;
        internal uint ProjectID;

        internal WorkingStorage Working;
        FolderNode RootFolder;
        FolderNode SelectedFolder;

        Dictionary<string, int> IconMap = new Dictionary<string,int>();
        List<Image> FileIcons = new List<Image>();

        internal Font RegularFont = new Font("Tahoma", 8.25F);
        internal Font BoldFont = new Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

        internal List<ulong> HigherIDs = new List<ulong>();
        internal List<ulong> CurrentDiffs = new List<ulong>();
        internal List<ulong> FailedDiffs = new List<ulong>();

        internal Dictionary<ulong, int> ChangeCount = new Dictionary<ulong, int>();

        Dictionary<ulong, RescanFolder> RescanFolderMap = new Dictionary<ulong, RescanFolder>();
        int NextRescan;

        internal bool WatchTransfers;

        internal bool IsLocal;

        ToolStripMenuItem MenuAdd;
        ToolStripMenuItem MenuLock;
        ToolStripMenuItem MenuUnlock;
        ToolStripMenuItem MenuRestore;
        ToolStripMenuItem MenuDelete;
        ToolStripMenuItem MenuDetails;

        ContainerListViewEx LastSelectedView;


        internal StorageView(StorageService storages, ulong id, uint project)
        {
            InitializeComponent();
            
            Storages = storages;
            Core = Storages.Core;
            Trust = Core.Trust;

            UserID = id;
            ProjectID = project;

            splitContainer1.Height = Height - toolStrip1.Height;

            if (UserID == Core.UserID)
                if (Storages.Working.ContainsKey(ProjectID))
                {
                    Working = Storages.Working[ProjectID];
                    IsLocal = true;
                }

            MenuAdd = new ToolStripMenuItem("Add to Files", StorageRes.Add, FileView_Add);
            MenuLock = new ToolStripMenuItem("Lock", StorageRes.Locked, FileView_Lock);
            MenuUnlock = new ToolStripMenuItem("Unlock", StorageRes.Unlocked, FileView_Unlock);
            MenuRestore = new ToolStripMenuItem("Restore", null, FileView_Restore);
            MenuDelete = new ToolStripMenuItem("Delete", StorageRes.Reject, FileView_Delete);
            MenuDetails = new ToolStripMenuItem("Details", StorageRes.details, FileView_Details);

            toolStrip1.Renderer = new ToolStripProfessionalRenderer(new OpusColorTable());
        }

        internal override string GetTitle(bool small)
        {
            if (small)
                return "Files";

            string title = "";

            if (IsLocal)
                title += "My ";
            else
                title += Core.GetName(UserID) + "'s ";

            if (ProjectID != 0)
                title += Trust.GetProjectName(ProjectID) + " ";

            title += "Files";

            return title;
        }

        internal override Size GetDefaultSize()
        {
            return new Size(550, 425);
        }

        internal override Icon GetIcon()
        {
            return StorageRes.Icon;
        }

        internal override void Init()
        {
            FolderTreeView.SmallImageList = new List<Image>();
            FolderTreeView.SmallImageList.Add(StorageRes.Folder);
            FolderTreeView.SmallImageList.Add(StorageRes.GhostFolder);

            FolderTreeView.OverlayImages.Add(StorageRes.Higher);
            FolderTreeView.OverlayImages.Add(StorageRes.Lower);
            FolderTreeView.OverlayImages.Add(StorageRes.Temp);
            FolderTreeView.OverlayImages.Add(StorageRes.InHigher);
            FolderTreeView.OverlayImages.Add(StorageRes.InLower);


            FileIcons.Add(new Bitmap(16, 16));
            FileIcons.Add(StorageRes.Ghost);
            FileIcons.Add(StorageRes.Folder);
            FileIcons.Add(StorageRes.GhostFolder);

            FileListView.OverlayImages.Add(StorageRes.Higher);
            FileListView.OverlayImages.Add(StorageRes.Lower);
            FileListView.OverlayImages.Add(StorageRes.DownloadSmall);
            FileListView.OverlayImages.Add(StorageRes.Temp);
            FileListView.OverlayImages.Add(StorageRes.InHigher);
            FileListView.OverlayImages.Add(StorageRes.InLower);


            SelectedInfo.Init(this);


            // hook up events
            Storages.StorageUpdate += new StorageUpdateHandler(Storages_StorageUpdate);
            Trust.GuiUpdate += new LinkGuiUpdateHandler(Trust_Update);
            Core.KeepDataGui += new KeepDataHandler(Core_KeepData);

            if (IsLocal)
            {
                Storages.WorkingFileUpdate += new WorkingUpdateHandler(Storages_WorkingFileUpdate);
                Storages.WorkingFolderUpdate += new WorkingUpdateHandler(Storages_WorkingFolderUpdate);
            }


            // research higher / lowers
            List<ulong> ids = new List<ulong>();
            ids.Add(UserID);
            ids.AddRange(Trust.GetUplinkIDs(UserID, ProjectID));
            ids.AddRange(Trust.GetAdjacentIDs(UserID, ProjectID));
            ids.AddRange(Trust.GetDownlinkIDs(UserID, ProjectID, 1));

            foreach (ulong id in ids)
                Storages.Research(id);


            // diff
            DiffCombo.Items.Add("Local");
            DiffCombo.Items.Add("Higher");
            DiffCombo.Items.Add("Lower");
            DiffCombo.Items.Add("Custom...");

            DiffCombo.SelectedIndex = 0;

            // dont show folder panel unless there are folders
            bool showFolders = false;

            if (RootFolder != null)
                foreach (FolderNode folder in RootFolder.Folders.Values)
                    if (!folder.Details.IsFlagged(StorageFlags.Archived))
                    {
                        showFolders = true;
                        break;
                    }

            FoldersButton.Checked = showFolders;
            FoldersButton_CheckedChanged(null, null); // event doesnt fire when setting false = false
        }

        private void StorageView_Load(object sender, EventArgs e)
        {
            // if SelectedInfo.InfoDisplay.DocumentText updated multiple times before load, it will show nothing

            SelectedInfo.DisplayActivated = true;
            SelectedInfo.ShowDiffs();
        }


        internal override bool Fin()
        {
            Storages.StorageUpdate -= new StorageUpdateHandler(Storages_StorageUpdate);
            Trust.GuiUpdate -= new LinkGuiUpdateHandler(Trust_Update);
            Core.KeepDataGui -= new KeepDataHandler(Core_KeepData);

            Storages.WorkingFileUpdate -= new WorkingUpdateHandler(Storages_WorkingFileUpdate);
            Storages.WorkingFolderUpdate -= new WorkingUpdateHandler(Storages_WorkingFolderUpdate);

            return true;
        }

        void Core_KeepData()
        {
            foreach (ulong id in CurrentDiffs)
                Core.KeepData.SafeAdd(id, true);
        }


        private void DiffCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            HigherIDs = Storages.GetHigherRegion(UserID, ProjectID);

            CurrentDiffs.Clear();

            if (DiffCombo.Text == "Local")
            {
                CurrentDiffs.AddRange(HigherIDs);
                CurrentDiffs.AddRange(Trust.GetDownlinkIDs(UserID, ProjectID, 1));
            }

            else if (DiffCombo.Text == "Higher")
            {
                CurrentDiffs.AddRange(HigherIDs);
            }

            else if (DiffCombo.Text == "Lower")
            {
                CurrentDiffs.AddRange(Trust.GetDownlinkIDs(UserID, ProjectID, 1));
            }

            else if (DiffCombo.Text == "Custom...")
            {
                AddUsersDialog form = new AddUsersDialog(Core, ProjectID);

                if (form.ShowDialog(this) == DialogResult.OK)
                    CurrentDiffs.AddRange(form.People);

                // if cancel then no diffs are made, isnt too bad, there are situations you'd watch no diffs
            }

            FailedDiffs.Clear();

            foreach(ulong user in CurrentDiffs)
                Storages.Research(user);

            // clear out current diffs
            SelectedInfo.DiffsView = true; // show diff status panel

            RefreshView();
        }

        private void RefreshView()
        {
            FolderNode prevSelected = SelectedFolder;

            RootFolder = null;

            FolderTreeView.Nodes.Clear();
            FileListView.Items.Clear();

            FolderTreeView.Nodes.Add(new TreeListNode());

            // if local
            if (IsLocal)
                RootFolder = LoadWorking(FolderTreeView.virtualParent, Working.RootFolder);

            // else load from file
            else
            {
                StorageFolder root = new StorageFolder();
                root.Name = Trust.GetProjectName(ProjectID) + " Files";

                RootFolder = new FolderNode(this, root, FolderTreeView.virtualParent, false);
                FolderTreeView.Nodes.Add(RootFolder);

                OpStorage storage = Storages.GetStorage(UserID);

                if (storage != null)
                    LoadHeader(Storages.GetFilePath(storage), storage.File.Header.FileKey);
            }

            // re-diff
            foreach (ulong id in CurrentDiffs)
                ApplyDiff(id);


            if (RootFolder != null)
            {
                RootFolder.Expand();
                
                bool high = false, low = false;
                AnalyzeChanges(RootFolder, true, ref high, ref low);
            }

            bool showDiffs = SelectedInfo.DiffsView; // save diffs view mode here because selectFolder resets it

            // if prev == selected, this means selected wasnt updated in refresh
            if (SelectedFolder == null || prevSelected == SelectedFolder)
                SelectFolder(RootFolder);
            else
            {
                SelectedFolder.EnsureVisible();
                RefreshFileList();
            }

            if (showDiffs)
                SelectedInfo.ShowDiffs();
        }

        private void ApplyDiff(ulong id)
        {
            
            // change log - who / not


            // read file uid
                
                // if exists locally
                    // if integrated mark as so
                    // else mark as changed, run history diff, and use last note

                // if doesnt exist
                    // add file as a temp
                    // user does - add to storage, to add it

                // if remote doesnt have any record of local file, ignore

            ChangeCount[id] = 0;

            OpStorage storage = Storages.GetStorage(id);

            if (storage == null)
            {
                FailedDiffs.Add(id);
                return;
            }

            string path = Storages.GetFilePath(storage);

            if (!File.Exists(path))
            {
                FailedDiffs.Add(id); 
                return;
            }

            try
            {
                using (TaggedStream filex = new TaggedStream(path, Core.GuiProtocol))
                using (IVCryptoStream crypto = IVCryptoStream.Load(filex, storage.File.Header.FileKey))
                {
                    PacketStream stream = new PacketStream(crypto, Storages.Protocol, FileAccess.Read);

                    ulong remoteUID = 0;
                    FolderNode currentFolder = RootFolder;
                    bool readingProject = false;

                    G2Header header = null;

                    while (stream.ReadPacket(ref header))
                    {
                        if (header.Name == StoragePacket.Root)
                        {
                            StorageRoot root = StorageRoot.Decode(header);

                            readingProject = (root.ProjectID == ProjectID);
                        }

                        if (readingProject)
                        {
                            if (header.Name == StoragePacket.Folder)
                            {
                                StorageFolder folder = StorageFolder.Decode(header);

                                // if new UID 
                                if (remoteUID == folder.UID)
                                    continue;

                                remoteUID = folder.UID;

                                // check scope
                                bool ignore = false;
                                if (folder.Scope.Count > 0 && !Trust.IsInScope(folder.Scope, UserID, ProjectID))
                                    ignore = true;

                                bool added = false;

                                while (!added)
                                {
                                    if (currentFolder.Details.UID == folder.ParentUID)
                                    {
                                        // if folder exists with UID
                                        if (currentFolder.Folders.ContainsKey(remoteUID))
                                            currentFolder = currentFolder.Folders[remoteUID];

                                        // else add folder as temp, mark as changed
                                        else
                                        {
                                            if (ignore) // temp so traverse works, but not saved in structure
                                            {
                                                currentFolder = new FolderNode(this, folder, currentFolder, false);
                                                break;
                                            }
                                            else
                                                currentFolder = currentFolder.AddFolderInfo(folder, true);
                                        }

                                        bool found = false;


                                        currentFolder.Archived.LockReading(delegate()
                                        {
                                            foreach (StorageFolder archive in currentFolder.Archived)
                                                if (Storages.ItemDiff(archive, folder) == StorageActions.None)
                                                    if (archive.Date == folder.Date)
                                                    {
                                                        found = true;
                                                        break;
                                                    }
                                        });

                                        if (!found)
                                        {
                                            currentFolder.Changes[id] = folder;
                                            currentFolder.UpdateOverlay();
                                        }

                                        // diff file properties, if different, add as change
                                        /*if (currentFolder.Temp || Storages.ItemDiff(currentFolder.Details, folder) != StorageActions.None)
                                        {
                                            currentFolder.Changes[id] = folder;
                                            currentFolder.UpdateOverlay();
                                        }*/

                                        added = true;
                                    }
                                    else if (currentFolder.Parent.GetType() == typeof(FolderNode))
                                        currentFolder = (FolderNode)currentFolder.Parent;
                                    else
                                        break;
                                }
                            }

                            if (header.Name == StoragePacket.File)
                            {
                                StorageFile file = StorageFile.Decode(header);

                                // if new UID 
                                if (remoteUID == file.UID)
                                    continue;

                                remoteUID = file.UID;

                                // check scope
                                if (file.Scope.Count > 0 && !Trust.IsInScope(file.Scope, UserID, ProjectID))
                                    continue;

                                FileItem currentFile = null;

                                // if file exists with UID
                                if (currentFolder.Files.ContainsKey(remoteUID))
                                    currentFile = currentFolder.Files[remoteUID];

                                // else add file as temp, mark as changed
                                else
                                    currentFile = currentFolder.AddFileInfo(file, true);

                                // if file is integrated, still add, so that reject functions

                                // true if file doesnt exist in local file history
                                // if it does exist and file is newer than latest, true

                                bool found = false;

                                currentFile.Archived.LockReading(delegate()
                                {
                                    foreach (StorageFile archive in currentFile.Archived)
                                        if (Storages.ItemDiff(archive, file) == StorageActions.None)
                                            if (archive.Date == file.Date)
                                            {
                                                found = true;
                                                break;
                                            }
                                });

                                if (!found)
                                    currentFile.Changes[id] = file;
                            }
                        }
                    }
                }
            }
            catch
            {
                FailedDiffs.Add(id); 
            }
        }

        private void RemoveDiff(ulong id, FolderNode folder)
        {
            if (FailedDiffs.Contains(id))
                FailedDiffs.Remove(id);

            // remove changes from files
            List<ulong> removeUIDs = new List<ulong>();

            foreach (FileItem file in folder.Files.Values)
            {
                if (file.Changes.ContainsKey(id))
                    file.Changes.Remove(id);

                // remove entirely if temp etc..
                if (file.Temp && file.Changes.Count == 0)
                    removeUIDs.Add(file.Details.UID);
            }

            foreach (ulong uid in removeUIDs)
                folder.Files.Remove(uid);

            removeUIDs.Clear();

            // remove changes from folders
            foreach (FolderNode sub in folder.Folders.Values)
            {
                if (sub.Changes.ContainsKey(id))
                {
                    sub.Changes.Remove(id);
                    sub.UpdateOverlay();
                }

                // remove entirely if temp etc..
                if (sub.Temp && sub.Changes.Count == 0)
                {
                    removeUIDs.Add(sub.Details.UID);

                    if (folder.Nodes.Contains(sub))
                        folder.Nodes.Remove(sub);

                    if (SelectedFolder == sub)
                        SelectFolder(folder);
                }
            }

            foreach (ulong uid in removeUIDs)
                folder.Folders.Remove(uid);

            // recurse
            foreach (FolderNode sub in folder.Folders.Values)
                RemoveDiff(id, sub);
        }

        private void AnalyzeChanges(FolderNode folder, bool expand, ref bool showHigher, ref bool showLower)
        {
            // look at files for changes
            foreach (FileItem file in folder.Files.Values)
                foreach (ulong id in file.GetRealChanges().Keys)
                {
                    ChangeCount[id]++;

                    if (HigherIDs.Contains(id))
                        showHigher = true;
                    else
                        showLower = true;
                }
            // look at folders for changes
            foreach (FolderNode sub in folder.Folders.Values)
            {
                foreach (ulong id in sub.GetRealChanges().Keys)
                {
                    ChangeCount[id]++;

                    if (HigherIDs.Contains(id))
                        showHigher = true;
                    else
                        showLower = true;
                }

                // recurse
                bool subChangesHigh = false, subChangesLow = false;

                AnalyzeChanges(sub, expand, ref subChangesHigh, ref subChangesLow);

                if(subChangesHigh)
                    showHigher = true;
                if(subChangesLow)
                    showLower = true;
            }

            folder.ContainsHigherChanges = showHigher;
            folder.ContainsLowerChanges = showLower;

            if (showHigher || showLower)
            {
                folder.UpdateOverlay();

                if (expand)
                    folder.EnsureVisible();
            }
        }

        private void Trust_Update(ulong key)
        {
            OpLink link = Trust.GetLink(key, ProjectID);

            if (link == null)
                return;

            // check if command structure has changed
            List<ulong> check = new List<ulong>();
            List<ulong> highers = Storages.GetHigherRegion(UserID, ProjectID);

            if (DiffCombo.Text == "Local")
            {
                check.AddRange(HigherIDs);
                check.AddRange(Trust.GetDownlinkIDs(UserID, ProjectID, 1));
            }

            else if (DiffCombo.Text == "Higher")
                check.AddRange(HigherIDs);

            else if (DiffCombo.Text == "Lower")
                check.AddRange(Trust.GetDownlinkIDs(UserID, ProjectID, 1));

            else // custom
                return;

            if (ListsMatch(highers, HigherIDs) && ListsMatch(check, CurrentDiffs))
                return;


            HigherIDs = highers;
            CurrentDiffs = check;
            FailedDiffs.Clear();

            RefreshView();
        }

        private bool ListsMatch(List<ulong> first, List<ulong> second)
        {
            if (first.Count != second.Count)
                return false;

            foreach (ulong id in first)
                if (!second.Contains(id))
                    return false;

            return true;
        }

        private void Storages_StorageUpdate(OpStorage storage)
        {
            if (storage.UserID == UserID)
                RefreshView();

            // re-apply diff
            if (CurrentDiffs.Contains(storage.UserID))
            {
                RemoveDiff(storage.UserID, RootFolder);
                ApplyDiff(storage.UserID);

                bool high = false, low = false;
                AnalyzeChanges(RootFolder, false, ref high, ref low);

                SelectedInfo.UpdateDiffView(storage.UserID);

                RefreshFileList();
            }
        }

        private void GhostsButton_CheckedChanged(object sender, EventArgs e)
        {
            RefreshView();
        }


        private void LoadHeader(string path, byte[] key)
        {
            try
            {
                using (TaggedStream filex = new TaggedStream(path, Core.GuiProtocol))
                using (IVCryptoStream crypto = IVCryptoStream.Load(filex, key))
                {
                    PacketStream stream = new PacketStream(crypto, Storages.Protocol, FileAccess.Read);

                    FolderNode currentFolder = RootFolder;
                    bool readingProject = false;

                    G2Header header = null;

                    while (stream.ReadPacket(ref header))
                    {
                        if (header.Name == StoragePacket.Root)
                        {
                            StorageRoot root = StorageRoot.Decode(header);

                            readingProject = (root.ProjectID == ProjectID);
                        }

                        // show archived if selected, only add top uid, not copies

                        if (readingProject)
                        {
                            if (header.Name == StoragePacket.Folder)
                            {
                                StorageFolder folder = StorageFolder.Decode(header);

                                bool added = false;

                                while (!added)
                                {
                                    if (currentFolder.Details.UID == folder.ParentUID)
                                    {
                                        currentFolder = currentFolder.AddFolderInfo(folder, false);

                                        // re-select on re-load
                                        if (SelectedFolder != null && currentFolder.Details.UID == SelectedFolder.Details.UID)
                                        {
                                            currentFolder.Selected = true;
                                            SelectedFolder = currentFolder;
                                        }

                                        added = true;
                                    }
                                    else if (currentFolder.Parent.GetType() == typeof(FolderNode))
                                        currentFolder = (FolderNode)currentFolder.Parent;
                                    else
                                        break;
                                }

                                if (!added)
                                {
                                    Debug.Assert(false);
                                    throw new Exception("Error loading CFS");
                                }
                            }

                            if (header.Name == StoragePacket.File)
                            {
                                StorageFile file = StorageFile.Decode(header);

                                currentFolder.AddFileInfo(file, false);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Network.UpdateLog("Storage", "Error loading files " + ex.Message);
            }
        }

        private FolderNode LoadWorking(TreeListNode parent, LocalFolder folder)
        {
            FolderNode node = new FolderNode(this, folder.Info, parent, false);
            node.Archived = folder.Archived;
            node.Integrated = folder.Integrated;

            if (SelectedFolder != null && node.Details.UID == SelectedFolder.Details.UID)
            {
                node.Selected = true;
                SelectedFolder = node;
            }

            if (!folder.Info.IsFlagged(StorageFlags.Archived) || GhostsButton.Checked)
                Utilities.InsertSubNode(parent, node);

            if (parent.GetType() == typeof(FolderNode))
            {
                FolderNode parentNode = (FolderNode)parent;
                parentNode.Folders[folder.Info.UID] = node;
            }

            if (node.Details.IsFlagged(StorageFlags.Modified))
                node.EnsureVisible();

            folder.Folders.LockReading(delegate()
            {
                foreach (LocalFolder sub in folder.Folders.Values)
                    LoadWorking(node, sub);
            });

            folder.Files.LockReading(delegate()
            {
                foreach (LocalFile file in folder.Files.Values)
                {
                    FileItem item = new FileItem(this, node, file.Info, false);
                    item.Archived = file.Archived;
                    item.Integrated = file.Integrated;

                    node.Files[file.Info.UID] = item;
                }
            });

            return node;
        }

        private void FolderTreeView_SelectedItemChanged(object sender, EventArgs e)
        {
            if (FolderTreeView.SelectedNodes.Count == 0)
            {
                SelectedInfo.ShowDiffs();
                return;
            }

            FolderNode node = FolderTreeView.SelectedNodes[0] as FolderNode;

            if (node == null)
                return;

            SelectedInfo.ShowItem(node, null);
            SelectFolder(node);
        }

        private void SelectFolder(FolderNode folder)
        {
            if (folder == null)
                return;

            /*
             *             
             * if (!SelectedInfo.IsFile && SelectedInfo.CurrentFolder != null)
            {
                if (SelectedInfo.CurrentFolder.Details.UID == SelectedFolder.Details.UID)
                    SelectedInfo.ShowItem(SelectedFolder, null);
                else
                    SelectedInfo.ShowDefault();
            }
             * */

            bool infoSet = false;
            string infoPath = SelectedInfo.CurrentPath;
            

            SelectedFolder = folder;
            folder.Selected = true;

            string dirpath = null;
            if (Working != null)
                dirpath = Working.RootPath + folder.GetPath();


            ulong selectedUID = 0;
            if (SelectedInfo.CurrentItem != null)
                selectedUID = SelectedInfo.CurrentItem.UID;
            

            FileListView.Items.Clear();
            FileListView.SmallImageList = FileIcons;

            // add sub folders
            if (!FoldersButton.Checked && folder.Parent.GetType() == typeof(FolderNode))
            {
                FileItem dots = new FileItem(this, new FolderNode(this, new StorageFolder(), folder.Parent, false));
                dots.Folder.Details.Name = "..";
                dots.Text = "..";

                if (string.Compare("..", infoPath, true) == 0)
                {
                    infoSet = true;
                    dots.Selected = true;
                    SelectedInfo.ShowDots();
                }

                FileListView.Items.Add(dots);
            }

            foreach (FolderNode sub in folder.Nodes)
            {
                FileItem subItem = new FileItem(this, sub);

                if (string.Compare(sub.GetPath(), infoPath, true) == 0)
                {
                    infoSet = true;
                    subItem.Selected = true;
                    SelectedInfo.ShowItem(sub, null);
                }

                FileListView.Items.Add(subItem);
            }

            // if folder unlocked, add untracked folders, mark temp
            if(folder.Details.IsFlagged(StorageFlags.Unlocked) && Directory.Exists(dirpath))
                try
                {
                    foreach (string dir in Directory.GetDirectories(dirpath))
                    {
                        string name = Path.GetFileName(dir);

                        if (name.CompareTo(".history") == 0)
                            continue;

                        if (folder.GetFolder(name) != null)
                            continue;

                        StorageFolder details = new StorageFolder();
                        details.Name = name;

                        FileItem tempFolder = new FileItem(this, new FolderNode(this, details, folder, true));

                        if (string.Compare(tempFolder.GetPath(), infoPath, true) == 0)
                        {
                            infoSet = true;
                            tempFolder.Selected = true;
                            SelectedInfo.ShowItem(tempFolder.Folder, null);
                        }

                        FileListView.Items.Add(tempFolder);
                    }
                }
                catch { }

            // add tracked files
            foreach (FileItem file in folder.Files.Values)
                if (!file.Details.IsFlagged(StorageFlags.Archived) || GhostsButton.Checked)
                {
                    if (string.Compare(file.GetPath(), infoPath, true) == 0)
                    {
                        infoSet = true;
                        file.Selected = true;
                        SelectedInfo.ShowItem(folder, file);
                    }
                    else
                        file.Selected = false;

                    FileListView.Items.Add(file);
                }

            // if folder unlocked, add untracked files, mark temp
            if (folder.Details.IsFlagged(StorageFlags.Unlocked) && Directory.Exists(dirpath))
                try
                {
                    foreach (string filepath in Directory.GetFiles(dirpath))
                    {
                        string name = Path.GetFileName(filepath);

                        if (folder.GetFile(name) != null)
                            continue;


                        StorageFile details = new StorageFile();
                        details.Name = name;
                        details.InternalSize = new FileInfo(filepath).Length;

                        FileItem tempFile = new FileItem(this, folder, details, true);

                        if (string.Compare(tempFile.GetPath(), infoPath, true) == 0)
                        {
                            infoSet = true;
                            tempFile.Selected = true;
                            SelectedInfo.ShowItem(folder, tempFile);
                        }

                        FileListView.Items.Add(tempFile);
                    }
                }
                catch { }

            UpdateListItems();

            if (!infoSet && SelectedFolder.GetPath() == infoPath)
            {
                infoSet = true;
                SelectedInfo.ShowItem(SelectedFolder, null);
            }
            if (!infoSet)
                SelectedInfo.ShowDiffs();
        }



        internal int GetImageIndex(FileItem file)
        {
            if (file.IsFolder)
                return file.Folder.Details.IsFlagged(StorageFlags.Archived) ? 3 : 2;

            string ext = Path.GetExtension(file.Text);

            if (!IconMap.ContainsKey(ext))
            {
                IconMap[ext] = FileIcons.Count;

                Bitmap img = Win32.GetIcon(ext);


                if (img == null)
                    img = new Bitmap(16, 16);

                FileIcons.Add(img);
            }

            if (file.Details != null && file.Details.IsFlagged(StorageFlags.Archived))
                return 1;

            return IconMap[ext];
        }

        void Storages_WorkingFolderUpdate(uint project, string parent, ulong uid, WorkingChange action)
        {
            if (project != ProjectID)
                return;

            try
            {
                LocalFolder workingParent = Working.GetLocalFolder(parent);
                FolderNode treeParent = GetFolderNode(parent);

                LocalFolder workingFolder;
                workingParent.Folders.SafeTryGetValue(uid, out workingFolder);

                FolderNode treeFolder;
                treeParent.Folders.TryGetValue(uid, out treeFolder);


                // working folder can only be null on update or remove action


                if (action == WorkingChange.Created)
                {
                    // create
                    if (treeFolder == null)
                    {
                        LoadWorking(treeParent, workingFolder);
                    }

                    // convert
                    else if(treeFolder.Temp)
                    {
                        treeFolder.Temp = false;
                        treeFolder.Details = workingFolder.Info;
                        treeFolder.Archived = workingFolder.Archived;
                        // changes remains the same
                        treeFolder.Update();
                    }
                }

                else if (action == WorkingChange.Updated)
                {
                    if (workingFolder != null)
                    {
                        if (treeFolder != null)
                        {
                            // check if should be removed
                            if (workingFolder.Info.IsFlagged(StorageFlags.Archived) && !GhostsButton.Checked)
                            {
                                if (treeParent.Nodes.Contains(treeFolder))
                                {
                                    treeParent.Nodes.Remove(treeFolder);

                                    if (SelectedFolder == treeFolder)
                                        SelectFolder(treeParent);
                                }
                            }

                            // update
                            else
                            {
                                treeFolder.Details = workingFolder.Info;
                                treeFolder.Archived = workingFolder.Archived;

                                // loop through working, if folder doesnt exist in tree, create it
                                workingFolder.Folders.LockReading(delegate()
                                {
                                    foreach (ulong id in workingFolder.Folders.Keys)
                                        if (!treeFolder.Folders.ContainsKey(id))
                                            LoadWorking(treeFolder, workingFolder);
                                });

                                // loop through tree, if folder doesnt exist in working, delete it
                                List<FolderNode> unload = new List<FolderNode>();

                                foreach (ulong id in treeFolder.Folders.Keys)
                                    if (!workingFolder.Folders.SafeContainsKey(id))
                                        unload.Add(treeFolder.Folders[id]);

                                foreach (FolderNode remove in unload)
                                    UnloadWorking(treeFolder, remove);

                                treeFolder.Update();

                                if (!SelectedInfo.IsFile && treeFolder == SelectedInfo.CurrentFolder)
                                    SelectedInfo.ShowItem(treeFolder, null);
                            }
                        }

                        // check if should be added
                        else if (!workingFolder.Info.IsFlagged(StorageFlags.Archived) || GhostsButton.Checked)
                        {
                            LoadWorking(treeParent, workingFolder);

                            MarkRescanFolder(workingFolder.Info.UID, treeParent.GetPath(), null, true);
                        }
                    }

                    // local temp folder
                    else
                    {
                        // check if folder a child of selected, refresh view if so
                        if ((treeParent != null && treeParent == SelectedFolder) ||
                            (treeFolder != null && treeFolder == SelectedFolder)) // re-naming un-tracked
                        {
                            RefreshFileList();
                            return;
                        }
                    }

                    
                }

                else if (action == WorkingChange.Removed)
                {
                    // rescan only needed here because changes aren't kept for similar files, so if folder is removed
                    // a rescan needs to be done to re-build changes for sub directories / files

                    string reselectPath = null;

                    if (SelectedFolder.ParentPathContains(treeFolder.Details.UID))
                        reselectPath = SelectedFolder.GetPath();

                   
                    if (treeParent != null && treeParent.Folders.ContainsKey(treeFolder.Details.UID))
                    {
                        treeParent.Folders.Remove(treeFolder.Details.UID);
                        treeParent.Nodes.Remove(treeFolder);
                    }

                    // if selected folders parent's contains deleted uid, reselect
                    if (reselectPath != null)
                    {
                        FolderNode node = null;

                        while (node == null)
                        {
                            node = GetFolderNode(reselectPath);

                            if (node != null)
                            {
                                SelectFolder(node);
                                break;
                            }
                            else if (reselectPath == "")
                            {
                                SelectFolder(RootFolder);
                                break;
                            }

                            reselectPath = Utilities.StripOneLevel(reselectPath);
                        }
                    }
                    
                    if(treeParent != null)
                        MarkRescanFolder(treeFolder.Details.UID, treeParent.GetPath(), null, true);

                }

                if (treeParent != null && treeParent == SelectedFolder)
                    RefreshFileList();

                FolderTreeView.Invalidate();

                CheckWorkingStatus();
            }
            catch (Exception ex)
            {
                Core.Network.UpdateLog("Storage", "Interface:WorkingFolderUpdate: " + ex.Message);
            }
        }

        private void UnloadWorking(FolderNode parent, FolderNode folder)
        {
            // need to leave ghosts of removed working files because we dont know what diffs applied
            // had the same hash has the file being unloaded (exists but never added to change log)
            // to be safe, we remove most everything and set everything as temp

            folder.Details = folder.Details.Clone();
            folder.Details.RemoveFlag(StorageFlags.Modified);
            folder.Archived.SafeClear();
            folder.Temp = true;

            if (parent.Nodes.Contains(folder))
                if (folder.Details.IsFlagged(StorageFlags.Archived) && !GhostsButton.Checked)
                    parent.Nodes.Remove(folder);
                else
                    folder.Update();


            foreach (FolderNode sub in folder.Folders.Values)
                UnloadWorking(folder, sub);


            foreach (FileItem file in folder.Files.Values)
            {
                file.Details = ((StorageFile)file.Details).Clone();
                file.Details.RemoveFlag(StorageFlags.Modified);
                file.Archived.SafeClear();
                file.Temp = true;
            }           
        }

        private void MarkRescanFolder(ulong uid, string path, FolderNode node, bool recurse)
        {
            RescanFolderMap[uid] = new RescanFolder(path, node, recurse);

            NextRescan = 2;

            RescanLabel.Visible = true;

        }

        void Storages_WorkingFileUpdate(uint project, string dir, ulong uid, WorkingChange action)
        {
            if (project != ProjectID)
                return;

             try
             {
                 LocalFolder workingFolder = Working.GetLocalFolder(dir);
                 FolderNode treeFolder = GetFolderNode(dir);

                 if (treeFolder == null || workingFolder == null)
                     return;


                 if (action == WorkingChange.Created)
                 {
                     LocalFile workingFile;

                     if (workingFolder.Files.SafeTryGetValue(uid, out workingFile))
                     {
                         FileItem treeFile = new FileItem(this, treeFolder, workingFile.Info, false);
                         treeFile.Archived = workingFile.Archived;

                         treeFolder.Files[treeFile.Details.UID] = treeFile;
                     }
                 }

                 else if (action == WorkingChange.Updated)
                 {
                     FileItem treeFile;
                     treeFolder.Files.TryGetValue(uid, out treeFile);

                     LocalFile workingFile;

                     if (workingFolder.Files.SafeTryGetValue(uid, out workingFile))
                         if (treeFile != null)
                         {
                             treeFile.Details = workingFile.Info;
                             treeFile.Archived = workingFile.Archived;

                             treeFile.UpdateInterface();
                             UpdateListItems();

                             if (treeFile == SelectedInfo.CurrentFile)
                                 SelectedInfo.ShowItem(SelectedFolder, treeFile);
                         }

                         // not there, add
                         else
                         {
                             treeFile = new FileItem(this, treeFolder, workingFile.Info, false);
                             treeFile.Archived = workingFile.Archived;

                             treeFolder.Files[treeFile.Details.UID] = treeFile;
                         }
                 }

                 else if (action == WorkingChange.Removed)
                 {
                     FileItem treeFile;
                     treeFolder.Files.TryGetValue(uid, out treeFile);

                     if (treeFile != null && treeFolder.Files.ContainsKey(treeFile.Details.UID))
                         treeFolder.Files.Remove(treeFile.Details.UID);
                 }

                 if (treeFolder == SelectedFolder)
                     SelectFolder(treeFolder);  // doesnt 

                 CheckWorkingStatus();
             }
             catch (Exception ex)
             {
                 Core.Network.UpdateLog("Storage", "Interface:WorkingFileUpdate: " + ex.Message);
             }
        }

        internal FolderNode GetFolderNode(string path)
        {
            // path: \a\b\c

            string[] folders = path.Split(Path.DirectorySeparatorChar);

            FolderNode current = RootFolder;

            foreach (string name in folders)
                if (name != "")
                {
                    bool found = false;

                    foreach (FolderNode sub in current.Folders.Values)
                        if (string.Compare(name, sub.Text, true) == 0)
                        {
                            found = true;
                            current = sub;
                            break;
                        }

                    if (!found)
                        return null;
                }

            return current;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            Storages.SaveLocal(ProjectID);

            RefreshView();

            CheckWorkingStatus();
        }

        private void DiscardButton_Click(object sender, EventArgs e)
        {
            Working = Storages.Discard(ProjectID);

            RefreshView();

            CheckWorkingStatus();
        }

        private void FolderTreeView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            FolderNode node = FolderTreeView.GetNodeAt(e.Location) as FolderNode;

            if (node == null)
                return;

            ContextMenuStripEx menu = new ContextMenuStripEx();

            if (!node.Temp)
            {
                if (node.Details.IsFlagged(StorageFlags.Unlocked))
                    menu.Items.Add(MenuLock);
                else
                    menu.Items.Add(MenuUnlock);
            }
            else
                menu.Items.Add(MenuAdd); // remove in web interface\


            if (node != RootFolder && !node.Temp)
                menu.Items.Add(MenuDetails);

            if (IsLocal && node != RootFolder && !node.Temp)
            {
                menu.Items.Add("-");

                if (node.Details.IsFlagged(StorageFlags.Archived))
                    menu.Items.Add(MenuRestore);

                menu.Items.Add(MenuDelete);
            }


            if (menu.Items.Count > 0)
            {
                LastSelectedView = FolderTreeView;
                menu.Show(FolderTreeView, e.Location);
            }
        }

        private void FileListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            FileItem clicked = FileListView.GetItemAt(e.Location) as FileItem;

            ContextMenuStripEx menu = new ContextMenuStripEx();

            if (clicked == null)
            {
                SelectedInfo.ShowDiffs();

                if (Working != null)
                {
                    menu.Items.Add(new ToolStripMenuItem("Create Folder", StorageRes.Folder, new EventHandler(Folder_Create)));
                    menu.Show(FileListView, e.Location);
                }

                return;
            }


            // add to storage, lock, unlock, restore, delete
            bool firstLoop = true;
            List<ToolStripMenuItem> potentialMenus = new List<ToolStripMenuItem>();

            foreach (FileItem item in FileListView.SelectedItems)
            {
                if (item.Text.CompareTo("..") == 0)
                    continue;

                // create list of items to add, intersect with current list, first run is free
                potentialMenus.Clear();

                // add
                if (IsLocal && item.Temp)
                    potentialMenus.Add(MenuAdd);

                // lock / unlock
                if (!item.Temp)
                    if (item.IsFolder)
                    {
                        if (item.Folder.Details.IsFlagged(StorageFlags.Unlocked))
                            potentialMenus.Add(MenuLock);
                        else
                            potentialMenus.Add(MenuUnlock);
                    }
                    else if (item.Details != null)
                    {
                        if (item.IsUnlocked())
                            potentialMenus.Add(MenuLock);
                        else
                            potentialMenus.Add(MenuUnlock);
                    }

                // details
                if (!item.Temp && FileListView.SelectedItems.Count == 1)
                    potentialMenus.Add(MenuDetails);

                // delete / restore
                if (IsLocal && !item.Temp)
                    if (item.IsFolder)
                    {
                        if (item.Folder.Details.IsFlagged(StorageFlags.Archived))
                            potentialMenus.Add(MenuRestore);

                        potentialMenus.Add(MenuDelete);
                    }

                    else if (item.Details != null)
                    {
                        if (item.Details.IsFlagged(StorageFlags.Archived))
                            potentialMenus.Add(MenuRestore);

                        potentialMenus.Add(MenuDelete);
                    }

                // initial list
                if (firstLoop)
                {
                    foreach (ToolStripMenuItem potential in potentialMenus)
                        menu.Items.Add(potential);

                    firstLoop = false;
                    continue;
                }

                // intersect both ways
                foreach (ToolStripMenuItem potential in potentialMenus)
                    if (!menu.Items.Contains(potential))
                        menu.Items.Remove(potential);

                List<ToolStripMenuItem> selfRemove = new List<ToolStripMenuItem>();
                foreach (ToolStripMenuItem current in menu.Items)
                    if (!potentialMenus.Contains(current))
                        selfRemove.Add(current);

                 foreach (ToolStripMenuItem current in selfRemove)
                     menu.Items.Remove(current);
            }


            // place '-' before restore or delete
            int i = 0;
            bool separator = false;
            foreach (ToolStripMenuItem item in menu.Items)
            {
                if (item.Text == "Restore" || item.Text == "Delete")
                {
                    separator = true;
                    break;
                }

                i++;
            }

            if (separator && i > 0)
                menu.Items.Insert(i, new ToolStripSeparator());


            if (menu.Items.Count > 0)
            {
                LastSelectedView = FileListView;
                menu.Show(FileListView, e.Location);
            }
        }

        void FileView_Add(object sender, EventArgs e)
        { 
            
            // folder view
            if (LastSelectedView == FolderTreeView)
            {

                if(FolderTreeView.SelectedNodes.Count > 0)
                    AddNewFolder((FolderNode)FolderTreeView.SelectedNodes[0]);
                
            }

            // file view
            else
            {
                foreach (FileItem item in FileListView.SelectedItems)
                {
                    if (item.IsFolder)
                    {
                        AddNewFolder(item.Folder);
                        continue;
                    }

                    // if no changes then temp local, add path
                    if (item.Changes.Count == 0)
                        Working.TrackFile(item.GetPath()); // add through working path

                    // if 1 change, then file is remote, add item
                    else if (item.Changes.Count == 1)
                        Working.TrackFile(item.Folder.GetPath(), (StorageFile)GetFirstItem(item.Changes));

                    // if more than 1 change, them multiple remotes
                    else if (item.Changes.Count > 1)
                        MessageBox.Show("Select specific file from changes below to add", item.Details.Name);
                }
            }
           
        }

        private StorageItem GetFirstItem(Dictionary<ulong, StorageItem> dictionary)
        {
            Dictionary<ulong, StorageItem>.Enumerator enumer = dictionary.GetEnumerator();

            if (enumer.MoveNext())
                return enumer.Current.Value;

            return null;
        }

        void AddNewFolder(FolderNode folder)
        {
            // if no changes then temp local, add path
            if (folder.Changes.Count == 0)
                Working.TrackFolder(folder.GetPath()); // add through working path

            // if 1 change, then file is remote, add item
            else if (folder.Changes.Count == 1)
                Working.TrackFolder(folder.GetPath(), (StorageFolder)GetFirstItem(folder.Changes));

            // if more than 1 change, them multiple remotes
            else if (folder.Changes.Count > 1)
                MessageBox.Show("Select specific folder from changes below to add");
        }

        void FileView_Restore(object sender, EventArgs e)
        {
            // folder view
            if (LastSelectedView == FolderTreeView)
            {

                if (FolderTreeView.SelectedNodes.Count > 0)
                {
                    FolderNode folder = (FolderNode)FolderTreeView.SelectedNodes[0];
                    Working.RestoreFolder(folder.GetPath());
                }

            }

            // file view
            else
            {
                foreach (FileItem item in FileListView.SelectedItems)
                {
                    if (item.IsFolder)
                    {
                        Working.RestoreFolder(item.Folder.GetPath());
                        continue;
                    }

                    Working.RestoreFile(item.Folder.GetPath(), item.Details.Name);
                }
            }

        }

        void FileView_Delete(object sender, EventArgs e)
        {
            List<string> tempFolders = new List<string>();
            List<string> tempFiles = new List<string>();

            List<FolderNode> folders = new List<FolderNode>();
            List<FileItem> files = new List<FileItem>();


            // folder view
            if (LastSelectedView == FolderTreeView)
            {

                if (FolderTreeView.SelectedNodes.Count > 0)
                {
                    FolderNode folder = (FolderNode)FolderTreeView.SelectedNodes[0];

                    if (folder.Temp)
                        tempFolders.Add(Working.RootPath + folder.GetPath());
                    else
                        folders.Add(folder);
                }

            }

            // file view
            else
            {
                foreach (FileItem item in FileListView.SelectedItems)
                {
                    if (item.IsFolder)
                    {
                        if (item.Folder.Temp)
                            tempFolders.Add(Working.RootPath + item.Folder.GetPath());
                        else
                            folders.Add(item.Folder);

                        continue;
                    }

                    if (item.Temp)
                        tempFiles.Add(Working.RootPath + item.GetPath());
                    else
                        files.Add(item);
                }
            }

            try
            {
                // handle temps
                if (tempFiles.Count > 0 || tempFolders.Count > 0)
                {
                    string message = "";

                    foreach (string name in tempFiles)
                        message += name + "\n";
                    foreach (string name in tempFolders)
                        message += name + "\n";

                    DialogResult result = MessageBox.Show(this, "Are you sure you want to delete?\n '" + message, "Delete", MessageBoxButtons.YesNoCancel);

                    if (result == DialogResult.Cancel)
                        return;

                    if (result == DialogResult.Yes)
                    {
                        foreach (string path in tempFiles)
                            File.Delete(path);
                        foreach (string path in tempFolders)
                            Directory.Delete(path, true);
                    }
                }

                // handle rest
                if (files.Count > 0 || folders.Count > 0)
                {
                    string message = "";

                    foreach (FileItem file in files)
                        message += file.Details.Name + "\n";
                    foreach (FolderNode node in folders)
                        message += node.Details.Name + "\n";

                    DialogResult result = MessageBox.Show(this, "Would you like to keep record of these files being deleted?\n" + message, "Delete", MessageBoxButtons.YesNoCancel);

                    if (result == DialogResult.Cancel)
                        return;

                    // archive
                    if (result == DialogResult.Yes)
                    {
                        foreach (FileItem file in files)
                            Working.ArchiveFile(file.Folder.GetPath(), file.Details.Name);
                        foreach (FolderNode folder in folders)
                            Working.ArchiveFolder(folder.GetPath());
                    }

                    // delete
                    if (result == DialogResult.No)
                    {
                        foreach (FileItem file in files)
                            Working.DeleteFile(file.Folder.GetPath(), file.Details.Name);
                        foreach (FolderNode folder in folders)
                            Working.DeleteFolder(folder.GetPath());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        void FileView_Lock(object sender, EventArgs e)
        {
            List<LockError> errors = new List<LockError>();
            List<FolderNode> folders = new List<FolderNode>();

            // folder view
            if (LastSelectedView == FolderTreeView)
            {

                if (FolderTreeView.SelectedNodes.Count > 0)
                    folders.Add( (FolderNode)FolderTreeView.SelectedNodes[0]);

            }

            // file view
            else
            {
                foreach (FileItem item in FileListView.SelectedItems)
                {
                    if (item.IsFolder)
                    {
                        folders.Add(item.Folder);
                        continue;
                    }

                    Storages.LockFileCompletely(UserID, ProjectID, item.Folder.GetPath(), item.Archived, errors);
                }
            }

            // lock folders
            bool lockSubs = false;

            foreach(FolderNode folder in folders)
                if (folder.Nodes.Count > 0)
                {
                    if (MessageBox.Show("Lock sub-folders as well?", "Lock", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        lockSubs = true;

                    break;
                }

            foreach (FolderNode folder in folders)
                LockFolder(folder, lockSubs, errors);

            RefreshFileList();
            
            LockMessage.Alert(this, errors);      
        }


        void FileView_Unlock(object sender, EventArgs e)
        {
            List<LockError> errors = new List<LockError>();
            List<FolderNode> folders = new List<FolderNode>();

            Cursor = Cursors.WaitCursor;

            // folder view
            if (LastSelectedView == FolderTreeView)
            {

                if (FolderTreeView.SelectedNodes.Count > 0)
                    folders.Add((FolderNode)FolderTreeView.SelectedNodes[0]);

            }

            // file view
            else
            {
                foreach (FileItem item in FileListView.SelectedItems)
                {
                    if (item.IsFolder)
                    {
                        folders.Add(item.Folder);
                        continue;
                    }

                    Storages.UnlockFile(UserID, ProjectID, item.Folder.GetPath(), (StorageFile)item.Details, false, errors);
                }
            }

            // unlock folders
            bool unlockSubs = false;

            foreach (FolderNode folder in folders)
                if (folder.Nodes.Count > 0)
                {
                    if (MessageBox.Show("Unlock sub-folders as well?", "Unlock", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        unlockSubs = true;

                    break;
                }

            foreach (FolderNode folder in folders)
                UnlockFolder(folder, unlockSubs, errors);

            Cursor = Cursors.Default;

            RefreshFileList();
            
            // set watch unlocked directories for changes
            if (Working != null)
                Working.StartWatchers();
            
            LockMessage.Alert(this, errors);
        }

        internal string UnlockFile(FileItem file)
        {
            List<LockError> errors = new List<LockError>();

            string path = Storages.UnlockFile(UserID, ProjectID, file.Folder.GetPath(), (StorageFile)file.Details, false, errors);

            LockMessage.Alert(this, errors);

            file.UpdateInterface();
            SelectedInfo.RefreshItem();

            return path;
        }

        internal void LockFile(FileItem file)
        {
            List<LockError> errors = new List<LockError>();

            Storages.LockFileCompletely(UserID, ProjectID, file.Folder.GetPath(), file.Archived, errors);

            LockMessage.Alert(this, errors);

            file.UpdateInterface();
            SelectedInfo.RefreshItem();
        }

        void FileView_Details(object sender, EventArgs e)
        {
            DetailsForm details = null;

            // folder view
            if (LastSelectedView == FolderTreeView)
            {

                if (FolderTreeView.SelectedNodes.Count > 0)
                {
                    details = new DetailsForm(this, (FolderNode)FolderTreeView.SelectedNodes[0]);
                    details.ShowDialog(this);
                    return;
                }

            }

            // file view
            else
            {
                foreach (FileItem item in FileListView.SelectedItems)
                {
                    if (item.IsFolder)
                    {
                        details = new DetailsForm(this, item.Folder);
                        details.ShowDialog(this);
                        return;
                    }

                    details = new DetailsForm(this, SelectedFolder, item);
                    details.ShowDialog(this);
                    return;
                }
            }
        }

        void Folder_Create(object sender, EventArgs args)
        {
            GetTextDialog dialog = new GetTextDialog(StorageRes.Icon, "Create Folder", "Enter a name for the new folder", "New Folder");

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    if (dialog.ResultBox.Text.Length == 0)
                        throw new Exception("Folder needs to have a name");

                    if (SelectedFolder.GetFolder(dialog.ResultBox.Text) != null)
                        throw new Exception("Folder with same name already exists");

                    if (dialog.ResultBox.Text.IndexOf(Path.DirectorySeparatorChar) != -1)
                        throw new Exception("Folder name contains invalid characters");

                    Working.TrackFolder(SelectedFolder.GetPath() + Path.DirectorySeparatorChar + dialog.ResultBox.Text);

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        internal void LockFolder(FolderNode folder, bool subs, List<LockError> errors)
        {
            string path = folder.GetPath();

            string wholepath = Storages.GetRootPath(UserID, ProjectID) + path;

            List<string> stillLocked = new List<string>();

            foreach (FileItem file in folder.Files.Values)
                if (!file.Temp)
                {
                    Storages.LockFileCompletely(UserID, ProjectID, path, file.Archived, errors);

                    string filepath = wholepath + Path.DirectorySeparatorChar + file.Details.Name;
                    if (File.Exists(filepath))
                        stillLocked.Add(filepath);
                }

            folder.Details.RemoveFlag(StorageFlags.Unlocked);
            folder.Update();

            if (subs)
                foreach (FolderNode subfolder in folder.Folders.Values)
                    if (!subfolder.Temp)
                        LockFolder(subfolder, subs, errors);

            Storages.DeleteFolder(wholepath, errors, stillLocked);
        }

        internal void UnlockFolder(FolderNode folder, bool subs, List<LockError> errors)
        {
            string path = folder.GetPath();
            string root = Storages.GetRootPath(UserID, ProjectID);

            if(!Storages.CreateFolder(root + path, errors, subs))
                return;

            if (Directory.Exists(root + path))
            {
                // set flag
                folder.Details.SetFlag(StorageFlags.Unlocked);
                folder.Update();

                // unlock files
                foreach (FileItem file in folder.Files.Values)
                    if (!file.Temp && !file.Details.IsFlagged(StorageFlags.Archived))
                        Storages.UnlockFile(UserID, ProjectID, path, (StorageFile)file.Details, false, errors);

                // unlock subfolders
                if (subs)
                    foreach (FolderNode subfolder in folder.Folders.Values)
                        if (!subfolder.Details.IsFlagged(StorageFlags.Archived))
                            UnlockFolder(subfolder, subs, errors);
            }
        }

        private void SecondTimer_Tick(object sender, EventArgs e)
        {
            // status of hashing
            CheckWorkingStatus();

            // keeps track of lil download icon on file
            if (WatchTransfers)
                UpdateListItems();

            SelectedInfo.SecondTimer();

            // rescan remote files against specific locals
            // happens when local file / folders are changed
            if (NextRescan > 0 && !Storages.HashingActive())
            {
                NextRescan--;

                if (NextRescan == 0)
                {
                    foreach (RescanFolder folder in RescanFolderMap.Values)
                        if (folder.Node != null)
                            folder.Node.Changes.Clear();

                    foreach (ulong id in CurrentDiffs)
                        RescanDiff(id);


                    // if selected folder / selected items / info panel in uid map - refresh
                    if (RescanFolderMap.ContainsKey(SelectedFolder.Details.UID))
                        RefreshFileList();

                    foreach (FileItem item in FileListView.Items)
                        if (item.IsFolder && RescanFolderMap.ContainsKey(item.Details.UID))
                            item.UpdateInterface();

                    if (SelectedInfo.CurrentFolder != null && RescanFolderMap.ContainsKey(SelectedInfo.CurrentFolder.Details.UID))
                        SelectedInfo.RefreshItem();


                    // clear maps
                    RescanFolderMap.Clear();

                    RescanLabel.Visible = false;
                }
            }
        }

        private void RescanDiff(ulong id)
        {
            /*
             * see if uid exists in rescan map
            if file 
                add to changes
            if folder
                add temp if uid doesnt exist
                set inTarget[uid], add further uids if recurse set
             */

            OpStorage storage = Storages.GetStorage(id);

            if (storage == null)
                return;

            string path = Storages.GetFilePath(storage);

            if (!File.Exists(path))
                return;

            try
            {
                using (TaggedStream filex = new TaggedStream(path, Core.GuiProtocol))
                using (IVCryptoStream crypto = IVCryptoStream.Load(filex, storage.File.Header.FileKey))
                {
                    PacketStream stream = new PacketStream(crypto, Storages.Protocol, FileAccess.Read);

                    ulong remoteUID = 0;
                    FolderNode currentFolder = RootFolder;
                    bool readingProject = false;

                    G2Header header = null;

                    List<ulong> Recursing = new List<ulong>();


                    while (stream.ReadPacket(ref header))
                    {
                        if (header.Name == StoragePacket.Root)
                        {
                            StorageRoot root = StorageRoot.Decode(header);

                            readingProject = (root.ProjectID == ProjectID);
                        }

                        if (readingProject)
                        {
                            if (header.Name == StoragePacket.Folder)
                            {
                                StorageFolder folder = StorageFolder.Decode(header);

                                // if new UID 
                                if (remoteUID == folder.UID)
                                    continue;

                                remoteUID = folder.UID;

                                if (RescanFolderMap.ContainsKey(remoteUID))
                                    if (RescanFolderMap[remoteUID].Recurse)
                                        Recursing.Add(remoteUID);

                                if (Recursing.Count > 0 || RescanFolderMap.ContainsKey(remoteUID))
                                {
                                    bool added = false;

                                    while (!added)
                                    {
                                        if (currentFolder.Details.UID == folder.ParentUID)
                                        {
                                            // if folder exists with UID
                                            if (currentFolder.Folders.ContainsKey(remoteUID))
                                                currentFolder = currentFolder.Folders[remoteUID];

                                            // else add folder as temp, mark as changed
                                            else
                                                currentFolder = currentFolder.AddFolderInfo(folder, true);

                                            // diff file properties, if different, add as change
                                            if (currentFolder.Temp || Storages.ItemDiff(currentFolder.Details, folder) != StorageActions.None)
                                            {
                                                currentFolder.Changes[id] = folder;
                                                currentFolder.UpdateOverlay();
                                            }

                                            added = true;
                                        }
                                        else if (currentFolder.Parent.GetType() == typeof(FolderNode))
                                        {
                                            // if current folder id equals recurse marker stop recursing
                                            if (Recursing.Contains(currentFolder.Details.UID))
                                                Recursing.Remove(currentFolder.Details.UID);

                                            currentFolder = (FolderNode)currentFolder.Parent;
                                        }
                                        else
                                            break;
                                    }
                                }
                            }

                            if (header.Name == StoragePacket.File)
                            {
                                StorageFile file = StorageFile.Decode(header);

                                // if new UID 
                                if (remoteUID == file.UID)
                                    continue;

                                remoteUID = file.UID;

                                if (Recursing.Count > 0 /*|| RescanFileMap.ContainsKey(remoteUID)*/)
                                {
                                    FileItem currentFile = null;

                                    // if file exists with UID
                                    if (currentFolder.Files.ContainsKey(remoteUID))
                                        currentFile = currentFolder.Files[remoteUID];

                                    // else add file as temp, mark as changed
                                    else
                                        currentFile = currentFolder.AddFileInfo(file, true);

                                    //crit check if file is integrated

                                    if (currentFile.Temp || Storages.ItemDiff(currentFile.Details, file) != StorageActions.None)
                                        currentFile.Changes[id] = file;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void CheckWorkingStatus()
        {
            if (!IsLocal)
                return;

            if (Storages.HashingActive())
            {
                if (!ChangesLabel.Visible || SaveButton.Visible)
                {
                    ChangesLabel.Visible = true;
                    SaveButton.Visible = false;
                    DiscardButton.Visible = false;

                    splitContainer1.Height = Height - 24 - toolStrip1.Height;
                }

                ChangesLabel.Text = "Processing " + (Storages.HashQueue.Count).ToString() + " Changes...";
            }

            else if (Working.Modified)
            {
                if (!SaveButton.Visible)
                {
                    ChangesLabel.Visible = false;
                    SaveButton.Visible = true;
                    DiscardButton.Visible = true;

                    splitContainer1.Height = Height - 24 - toolStrip1.Height;
                }
            }
            else
            {
                if (SaveButton.Visible)
                {
                    ChangesLabel.Visible = false;
                    SaveButton.Visible = false;
                    DiscardButton.Visible = false;
                    splitContainer1.Height = Height - toolStrip1.Height;
                }
            }
        }

        private void FoldersButton_CheckedChanged(object sender, EventArgs e)
        {
            splitContainer2.Panel1Collapsed = !FoldersButton.Checked;

            RefreshFileList(); // puts .. dir if needed
        }

        private void FileListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (FileListView.SelectedItems.Count == 0)
            {
                SelectedInfo.ShowDiffs();
                return;
            }

            FileItem file = (FileItem)FileListView.SelectedItems[0];

            if (file.IsFolder)
                SelectedInfo.ShowItem(file.Folder, null);
            else
                SelectedInfo.ShowItem(file.Folder, file);
        }

        private void FileListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (FileListView.SelectedItems.Count == 0)
                return;

            FileItem file = (FileItem)FileListView.SelectedItems[0];


            // if folder
            if (file.IsFolder)
            {
                if (file.Temp)
                {
                    Utilities.OpenFolder(Storages.GetRootPath(UserID, ProjectID) + file.Folder.GetPath());
                    return;
                }

                FolderNode node = file.Text == ".." ? (FolderNode)file.Folder.Parent : file.Folder;

                SelectedInfo.ShowItem(node, null);
                SelectFolder(node);

                return;
            }

            // if file exists in storage, or if the file is a local temp file
            if (Storages.FileExists((StorageFile)file.Details) || (file.Temp && file.Changes.Count == 0))
                OpenFile(file);

            else
            {
                Storages.DownloadFile(UserID, (StorageFile)file.Details);

               
                UpdateListItems();
                SelectedInfo.ShowItem(SelectedFolder, file);
            }
               
        }

        internal void UpdateListItems()
        {
            // if any item in list is transferring, set watch transfers

            // set file icons and overlays, call invalidate

            foreach (FileItem item in FileListView.Items)
            {
                item.UpdateInterface();

                // should only be called when images are displayed            
                item.ImageIndex = GetImageIndex(item);

                item.UpdateOverlay();

                if (!item.IsFolder && Storages.DownloadStatus((StorageFile)item.Details) != null)
                {
                    item.Overlays.Add(2);
                    WatchTransfers = true;
                }
            }

            // during large  operatiosn with lots of messages from worker, paint gets flooded out
            // user needs to see visual progress of move operation for instance
            FileListView.Refresh(); 
        }

        internal void OpenFile(FileItem file)
        {
            string path = null;


            // if temp and changes exist (then remote)
            if (file.Temp)
            {
                // remote file that doesnt exist local yet
                if (file.Changes.Count > 0)
                {
                    foreach (ulong id in file.Changes.Keys)
                        if (file.Changes[id] == file.Details)
                            path = UnlockFile(file);
                }

                // local temp file
                else
                    path = Storages.GetRootPath(UserID, ProjectID) + file.GetPath();
            }

            // non temp file
            else
                path = UnlockFile(file);

            if (path != null && File.Exists(path))
                Process.Start(path);

            file.UpdateInterface();
            FileListView.Invalidate();
        }

        private void FolderTreeView_DragOver(object sender, DragEventArgs e)
        {
            if (UserID != Core.UserID)
                return;

            e.Effect = DragDropEffects.None;

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            // cant drag into someone else's folder
            if (Working == null)
                return;

            // not dragging means, dragging in file from outside de-ops, allow
            if (!Dragging)
            {
                e.Effect = DragDropEffects.All;
                return;
            }

            // else dragging from inside, only allow file to be dropped on folder
            FolderNode node = FolderTreeView.GetNodeAt(FolderTreeView.PointToClient(new Point(e.X, e.Y))) as FolderNode;

            if (node == null)
                return;
          
            if (node != SelectedFolder)
                e.Effect = DragDropEffects.All;
        }

        private void FileListView_DragOver(object sender, DragEventArgs e)
        {
            if (UserID != Core.UserID)
                return;

            e.Effect = DragDropEffects.None;

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            // cant drag into someone else's folder
            if (Working == null)
                return;

            // not dragging means, dragging in file from outside de-ops, allow
            if (!Dragging)
            {
                e.Effect = DragDropEffects.All;
                return;
            }

     
            // else inside de-ops only allow file to be dropped on folder
            FileItem item = FileListView.GetItemAt(FileListView.PointToClient(new Point(e.X, e.Y))) as FileItem;

            if (item == null)
                return;

            if (item.IsFolder && !item.Temp)
                e.Effect = DragDropEffects.All;
        }

        private void FolderTreeView_DragDrop(object sender, DragEventArgs e)
        {
            FinishDrop(true, e);
        }

        private void FileListView_DragDrop(object sender, DragEventArgs e)
        {
            FinishDrop(false, e);
        }

        private void FinishDrop(bool folderView, DragEventArgs e)
        {
            Dragging = false;

            if (SelectedFolder == null)
                return;

            if (Working == null) // can only drop files into local repo
                return;

            // Handle only FileDrop data.
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            // get destination of drop
            string destPath = null;

            if (folderView) // dropped in folder view
            {
                FolderNode node = FolderTreeView.GetNodeAt(FolderTreeView.PointToClient(new Point(e.X, e.Y))) as FolderNode;

                if (node == null)
                    return;
                    
                destPath = node.GetPath();
            }

            else // dropped in file view
            {
                destPath = SelectedFolder.GetPath();

                FileItem item = FileListView.GetItemAt(FileListView.PointToClient(new Point(e.X, e.Y))) as FileItem;
                if (item != null && item.IsFolder)
                    if (item.Text != "..")
                        destPath += Path.DirectorySeparatorChar + item.Folder.Details.Name;
                    else
                        destPath = Utilities.StripOneLevel(destPath);
            }


            // Assign the file names to a string array, in 
            // case the user has selected multiple files.
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

            List<string> errors = new List<string>();


            foreach (string sourcePath in paths)
            {
                try
                {
                    // move secure folder or file
                    if (sourcePath.StartsWith(Working.RootPath))
                    {
                        string securePath = sourcePath.Replace(Working.RootPath, "");

                        LocalFolder folder = Working.GetLocalFolder(securePath);

                        // secure folder
                        if (folder != null)
                        {
                            if (folder.Info.IsFlagged(StorageFlags.Archived))
                                continue;

                            Working.MoveFolder(folder, destPath, errors);
                        }

                        // secure file
                        LocalFile file = Working.GetLocalFile(securePath);

                        if (file != null)
                        {
                            if (file.Info.IsFlagged(StorageFlags.Archived))
                                continue;

                            Working.MoveFile(securePath, destPath, errors);
                        }
                    }

                    // move local folder or file
                    string finalPath = destPath + Path.DirectorySeparatorChar + Path.GetFileName(sourcePath);

                    if (Directory.Exists(sourcePath))
                    {
                        //crit allow overwrite if files exist in spot
                        // if on file sys erased
                        // new entries made in file item's history

                        string fullDestination = Working.RootPath + destPath;

                        if (!fullDestination.Contains(sourcePath)) // dont let folder move into itself
                            CopyDiskDirectory(sourcePath, finalPath, errors);
                    }
                    else if (File.Exists(sourcePath))
                    {
                        Directory.CreateDirectory(Working.RootPath + destPath);

                        CopyDiskFile(sourcePath, finalPath, errors);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add("Exception " + ex.Message + " " + sourcePath);
                }
            }


            if (errors.Count > 0)
            {
                string message = "";
                foreach (string error in errors)
                    message += "\n" + error;

                MessageBox.Show(this, "Errors:\n" + message);
            }
        }

        public void CopyDiskDirectory(string sourcePath, string destPath, List<string> errors)
        {
            // create folder to copy to
            if (!Directory.Exists(Working.RootPath + destPath))
                Directory.CreateDirectory(Working.RootPath + destPath);

            Working.TrackFolder(destPath); // if already there simply returns

            // add folders and files
            String[] sourceFiles = Directory.GetFileSystemEntries(sourcePath);

            foreach (string diskFile in sourceFiles)
            {
                string finalPath = destPath + Path.DirectorySeparatorChar + Path.GetFileName(diskFile);

                if (Directory.Exists(diskFile))
                    CopyDiskDirectory(diskFile, finalPath, errors);

                else if ( File.Exists(diskFile) )
                    CopyDiskFile(diskFile, finalPath, errors);
            }
        }

        public void CopyDiskFile(string sourcePath, string destPath, List<string> errors)
        {
            if (Working.FileExists(destPath))
            {
                errors.Add("File " + destPath + " already exists in files");
                return;
            }

            File.Copy(sourcePath, Working.RootPath + destPath, false);
            Working.TrackFile(destPath);
        }


        bool Dragging;
        Point DragStart = Point.Empty;

        private void FileListView_MouseDown(object sender, MouseEventArgs e)
        {
            Dragging = false;
            DragStart = Point.Empty;

            if (DragStart == Point.Empty && FileListView.GetItemAt(e.Location) != null)
            {
                DragStart = e.Location;
            }
        }
        
        private void FileListView_MouseMove(object sender, MouseEventArgs e)
        {
            if (DragStart != Point.Empty && !Dragging && Utilities.GetDistance(DragStart, e.Location) > 4)
            {
                Dragging = true;

                DataObject data = new DataObject(DataFormats.FileDrop, GetSelectedPaths(false));
                FileListView.DoDragDrop(data, DragDropEffects.Copy);
            }
        }

        string[] GetSelectedPaths(bool folderView)
        {
            string[] paths = null;

            if (folderView)
            {
                paths = new string[FolderTreeView.SelectedNodes.Count];

                int i = 0;

                foreach (FolderNode node in FolderTreeView.SelectedNodes.OfType <FolderNode>())
                {
                    paths[i] = Storages.GetRootPath(UserID, ProjectID) + node.GetPath();
                    i++;
                }
            }
            else
            {
                paths = new string[FileListView.SelectedItems.Count];

                int i = 0;
                foreach (FileItem item in FileListView.SelectedItems)
                    if (item.Text != "..")
                    {
                        paths[i] = Storages.GetRootPath(UserID, ProjectID) + item.GetPath();
                        i++;
                    }
            }

            return paths;
        }

        private void FolderTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            Dragging = false;
            DragStart = Point.Empty;

            if (DragStart == Point.Empty && FolderTreeView.GetNodeAt(e.Location) != null)
            {
                DragStart = e.Location;
            }
        }

        private void FolderTreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (DragStart != Point.Empty && !Dragging && Utilities.GetDistance(DragStart, e.Location) > 4)
            {
               
                Dragging = true;

                DataObject data = new DataObject(DataFormats.FileDrop, GetSelectedPaths(true));
                FolderTreeView.DoDragDrop(data, DragDropEffects.Copy);
            }
        }

        private void FileListView_MouseUp(object sender, MouseEventArgs e)
        {
            Dragging = false;
            DragStart = Point.Empty; 
        }

        internal void RefreshFileList()
        {
            SelectFolder(SelectedFolder);
        }

        Keys LastControlKey;

        private void FileListView_KeyDown(object sender, KeyEventArgs e)
        {  
            if (e.KeyCode == Keys.Delete)
            {
                LastSelectedView = FileListView;
                FileView_Delete(null, null);
            }

            if (e.Control  )
            {
                if (e.KeyCode == LastControlKey)
                    return;

                LastControlKey = e.KeyCode;

                if (e.KeyCode == Keys.C)
                {
                    //StringCollection paths = new StringCollection();
                    //paths.AddRange(GetSelectedPaths(false));
                    //Clipboard.SetFileDropList(paths);
                }
            }
        }

        private void FolderTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                LastSelectedView = FolderTreeView;
                FileView_Delete(null, null);
            }

            if (e.Control)
            {
                if (e.KeyCode == LastControlKey)
                    return;

                LastControlKey = e.KeyCode;

                if (e.KeyCode == Keys.C)
                {
                    //StringCollection paths = new StringCollection();
                    //paths.AddRange(GetSelectedPaths(true));
                    //Clipboard.SetFileDropList(paths);
                }

                if (e.KeyCode == Keys.V)
                {

                    
                }
            }
            
        }

        private void FileListView_KeyUp(object sender, KeyEventArgs e)
        {
            LastControlKey = Keys.None;
        }

        private void FolderTreeView_KeyUp(object sender, KeyEventArgs e)
        {
            LastControlKey = Keys.None;
        }
    }

    internal class FolderNode : TreeListNode 
    {
        internal bool Temp;
        internal StorageFolder Details;
        StorageView View;

        internal ThreadedLinkedList<StorageItem> Archived = new ThreadedLinkedList<StorageItem>();
        internal ThreadedDictionary<ulong, StorageItem> Integrated = new ThreadedDictionary<ulong, StorageItem>();
        internal Dictionary<ulong, StorageItem> Changes = new Dictionary<ulong, StorageItem>();

        internal Dictionary<ulong, FolderNode> Folders = new Dictionary<ulong, FolderNode>();
        internal Dictionary<ulong, FileItem> Files = new Dictionary<ulong, FileItem>();

        internal bool ContainsHigherChanges;
        internal bool ContainsLowerChanges;

        //Nodes - contains sub-folders

        internal FolderNode(StorageView view, StorageFolder folder, TreeListNode parent, bool temp)
        {
            Parent = parent; // needs to be set because some nodes (archived) aren't proper children of parents (not in Nodes list), still needed though so getPath works

            if (parent == null)
                throw new Exception("Parent set to null");

            View = view;

            Details = folder;
            Temp = temp;

            Update();
        }

        internal FolderNode GetFolder(string name)
        {
            foreach (FolderNode folder in Folders.Values)
                if (string.Compare(name, folder.Text, true) == 0)
                    return folder;

            return null;
        }

        internal FileItem GetFile(string name)
        {
            foreach (FileItem file in Files.Values)
                if (string.Compare(name, file.Text, true) == 0)
                    return file;

            return null;
        }

        internal void Update()
        {
            Text = Details.Name;

            if (Details.IsFlagged(StorageFlags.Modified))
                ForeColor = Color.DarkRed;
            else if (Temp || Details.IsFlagged(StorageFlags.Archived))
                ForeColor = Color.Gray;
            else
                ForeColor = Color.Black;

            if (Details.IsFlagged(StorageFlags.Unlocked))
                this.Font = View.BoldFont;
            else
                this.Font = View.RegularFont;

            ImageIndex = Details.IsFlagged(StorageFlags.Archived) ? 1 : 0;


            UpdateOverlay();

            if(TreeList != null)
                TreeList.Invalidate();
        }

        internal string GetPath()
        {
            
            string path = "";

            FolderNode up = this;

            while (up.Parent != null && up.Parent.GetType() == typeof(FolderNode))
            {
                path = Path.DirectorySeparatorChar + up.Details.Name + path;
                up = up.Parent as FolderNode;
            }

            return path;
        }



        internal FolderNode AddFolderInfo(StorageFolder info, bool remote)
        {
            if (!Folders.ContainsKey(info.UID))
            {
                Folders[info.UID] = new FolderNode(View, info, this, remote);

                if (!info.IsFlagged(StorageFlags.Archived) || View.GhostsButton.Checked)
                    Utilities.InsertSubNode(this, Folders[info.UID]);
            }

            FolderNode folder = Folders[info.UID];

            if (!remote)
            {
                if (info.IntegratedID != 0)
                    folder.Integrated.SafeAdd(info.IntegratedID, info);
                else
                    folder.Archived.SafeAddLast(info);
            }

            return folder;
        }

        internal FileItem AddFileInfo(StorageFile info, bool remote)
        {
            if (!Files.ContainsKey(info.UID))
                Files[info.UID] = new FileItem(View, this, info, remote);

            FileItem file = Files[info.UID];

            if (!remote)
            {
                if (info.IntegratedID != 0)
                    file.Integrated.SafeAdd(info.IntegratedID, info);
                else
                    file.Archived.SafeAddLast(info);
            }

            return file;
        }

        internal void UpdateOverlay()
        {
            if (Overlays == null)
                Overlays = new List<int>();

            Overlays.Clear();

            if (Temp)
                Overlays.Add(2);

            bool showHigher = false, showLower = false;

            foreach (ulong id in GetRealChanges().Keys)
                if (View.HigherIDs.Contains(id))
                    showHigher = true;
                else
                    showLower = true;

            if (showHigher)
                this.Overlays.Add(0);

            if (showLower)
                this.Overlays.Add(1);

            if(ContainsHigherChanges)
                this.Overlays.Add(3);

            if (ContainsLowerChanges)
                this.Overlays.Add(4);
        }

        internal Dictionary<ulong, StorageItem> GetRealChanges()
        {
            Dictionary<ulong, StorageItem> realChanges = new Dictionary<ulong, StorageItem>();

            foreach (ulong id in Changes.Keys)
            {
                bool add = true;
                StorageItem change = Changes[id];

                // dont display if current file is equal to this change
                if (!Temp && View.Storages.ItemDiff(change, Details) == StorageActions.None)
                    add = false;

                // dont display if change is integrated
                StorageItem item = null;
                if (Integrated.SafeTryGetValue(id, out item))
                    if (View.Storages.ItemDiff(change, item) == StorageActions.None)
                        add = false;

                if (add)
                    realChanges.Add(id, change);
            }

            return realChanges;
        }

        public override string ToString()
        {
            return Details.Name;
        }

        internal bool ParentPathContains(ulong uid)
        {
            if (Details.UID == uid)
                return true;

            if (Parent.GetType() == typeof(FolderNode))
                return ((FolderNode)Parent).ParentPathContains(uid);

            return false;
        }
    }

    internal class FileItem : ContainerListViewItem
    {
        internal StorageItem Details;
        StorageView View;

        internal ThreadedLinkedList<StorageItem> Archived = new ThreadedLinkedList<StorageItem>();
        internal ThreadedDictionary<ulong, StorageItem> Integrated = new ThreadedDictionary<ulong, StorageItem>();
        internal Dictionary<ulong, StorageItem> Changes = new Dictionary<ulong, StorageItem>();

        internal bool Temp;
        internal bool IsFolder;
        internal FolderNode Folder;

        internal FileItem(StorageView view, FolderNode parent, StorageFile file, bool temp)
        {
            View = view;
            Folder = parent;

            SubItems.Add("");
            SubItems.Add("");

            Details = file;
            Temp = temp;

            UpdateInterface();
        }

        internal FileItem(StorageView view, FolderNode folder)
        {
            View = view;
            Folder = folder;

            SubItems.Add("");
            SubItems.Add("");

            IsFolder = true;
            ImageIndex = folder.Details.IsFlagged(StorageFlags.Archived) ? 3 : 2;

            Details = folder.Details;
            Changes = folder.Changes;
            Temp = folder.Temp;

            UpdateInterface();
        }

        internal void UpdateInterface()
        {
            Text = Details.Name;

            if (Details.IsFlagged(StorageFlags.Modified))
                ForeColor = Color.DarkRed;
            else if (Temp || Details.IsFlagged(StorageFlags.Archived))
                ForeColor = Color.Gray;
            else
                ForeColor = Color.Black;

            if (IsUnlocked())
                this.Font = View.BoldFont;
            else
                this.Font = View.RegularFont;

            if (Details.GetType() == typeof(StorageFile))
            {
                StorageFile file = (StorageFile)Details;

                if (file.Hash == null)
                    ForeColor = Color.Red; // hash processing, timer will update

                SubItems[0].Text = Utilities.ByteSizetoString(file.InternalSize);
            }

            if (!Temp)
                SubItems[1].Text = Details.Date.ToLocalTime().ToString();
        }

        internal bool IsUnlocked()
        {
            return Details.IsFlagged(StorageFlags.Unlocked) ||
                View.Storages.IsHistoryUnlocked(View.UserID, View.ProjectID, Folder.GetPath(), Archived);
        }

        internal string GetPath()
        {
            if (IsFolder)
                return Folder.GetPath();

            return Folder.GetPath() + Path.DirectorySeparatorChar + Details.Name;
        }

        internal void UpdateOverlay()
        {
            if (Overlays == null)
                Overlays = new List<int>();

            Overlays.Clear();

            if(Temp)
                Overlays.Add(3);

            bool showHigher = false, showLower = false;

            foreach (ulong id in GetRealChanges().Keys)
                if (View.HigherIDs.Contains(id))
                    showHigher = true;
                else
                    showLower = true;

            if (showHigher)
                Overlays.Add(0);

            if (showLower)
                Overlays.Add(1);

            if (IsFolder)
            {
                if (Folder.ContainsHigherChanges)
                    this.Overlays.Add(4);

                if (Folder.ContainsLowerChanges)
                    this.Overlays.Add(5);
            }
        }

        internal Dictionary<ulong, StorageItem> GetRealChanges()
        {
            Dictionary<ulong, StorageItem> realChanges = new Dictionary<ulong, StorageItem>();

            foreach (ulong id in Changes.Keys)
            {
                bool add = true;
                StorageItem change = Changes[id];

                // dont display if current file is equal to this change
                if (!Temp && View.Storages.ItemDiff(change, Details) == StorageActions.None)
                    add = false;

                // dont display if change is integrated
                StorageItem item = null;
                if (Integrated.SafeTryGetValue(id, out item))
                    if (View.Storages.ItemDiff(change, item) == StorageActions.None)
                        add = false;

                if (add)
                    realChanges.Add(id, change);
            }

            return realChanges;
        }

        internal Dictionary<ulong, StorageItem> GetRealIntegrated()
        {
            Dictionary<ulong, StorageItem> realIntegrated = new Dictionary<ulong, StorageItem>();

            Integrated.LockReading(delegate()
            {
                foreach (ulong id in Integrated.Keys)
                {
                    bool add = true;
                    StorageItem integrate = Integrated[id];

                    // dont display if current file is equal to this change
                    if (View.Storages.ItemDiff(integrate, Details) == StorageActions.None)
                        add = false;

                    // dont display if this file isnt the lastest from the person id
                    if (Changes.ContainsKey(id))
                        if (View.Storages.ItemDiff(integrate, Changes[id]) != StorageActions.None)
                            add = false;

                    if (add)
                        realIntegrated.Add(id, integrate);
                }
            });

            return realIntegrated;
        }

        public override string ToString()
        {
            return Details.Name.ToString();
        }
    }

    internal class RescanFile
    {
        internal string Path;
        internal FileItem Item;

        internal RescanFile(string path, FileItem item)
        {
            Path = path;
            Item = item;
        }
    }

    internal class RescanFolder
    {
        internal string Path;
        internal FolderNode Node;
        internal bool Recurse;

        internal RescanFolder(string path, FolderNode node, bool recurse)
        {
            Path = path;
            Node = node;
            Recurse = recurse;
        }
    }
}
