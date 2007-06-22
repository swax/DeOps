using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using DeOps.Implementation;
using DeOps.Implementation.Protocol;

namespace DeOps.Components.Storage
{
    enum WorkingChange { Created, Updated, Removed };

    internal class WorkingStorage
    {
        OpCore Core;
        G2Protocol Protocol;
        StorageControl Storages;

        internal uint ProjectID;

        internal bool Modified;
        internal bool PeriodicSave;

        FileSystemWatcher FileWatcher;
        FileSystemWatcher FolderWatcher;

        internal LocalFolder RootFolder;
        internal string RootPath;


        internal WorkingStorage(StorageControl storages, uint project)
        {
            Storages = storages;
            Core = Storages.Core;
            Protocol = Core.Protocol;

            RootPath = Storages.GetRootPath(Core.LocalDhtID, project);
            ProjectID = project;

           
            StorageFolder packet = new StorageFolder();
            packet.Name = Core.Links.ProjectNames[project] + " Storage";

            RootFolder = new LocalFolder(null, packet);

            LoadWorking();
        }

        internal void SaveWorking()
        {
            try
            {
                string tempPath = Core.GetTempPath();
                FileStream file = new FileStream(tempPath, FileMode.Create);
                CryptoStream stream = new CryptoStream(file, Storages.LocalFileKey.CreateEncryptor(), CryptoStreamMode.Write);

                Protocol.WriteToFile(new StorageRoot(ProjectID), stream);

                WriteWorkingFile(stream, RootFolder, false); // record all so working can be browsed while locked

                stream.FlushFinalBlock();
                stream.Close();

                string finalPath = Storages.GetWorkingPath(ProjectID);

                File.Delete(finalPath);
                File.Move(tempPath, finalPath);
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Storage", "Error saving headers " + ex.Message);
            }
        }


        private void LoadWorking()
        {
            // load working
            try
            {
                string path = Storages.GetWorkingPath(ProjectID);
                RijndaelManaged key = Storages.LocalFileKey;

                if (!File.Exists(path))
                {
                    path = Storages.GetFilePath(Storages.LocalStorage.Header);
                    key = Storages.LocalStorage.Header.FileKey;
                }
                else
                    Modified = true; // working file present on startup, meaning there are changes lingering to be committed

                if (!File.Exists(path))
                    return;

                FileStream filex = new FileStream(path, FileMode.Open);
                CryptoStream crypto = new CryptoStream(filex, key.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Protocol, FileAccess.Read);

                G2Header header = null;

                LocalFolder CurrentFolder = RootFolder;
                string CurrentPath = RootPath;

                while (stream.ReadPacket(ref header))
                {
                    if (header.Name == StoragePacket.Folder)
                    {
                        StorageFolder folder = StorageFolder.Decode(Core.Protocol, header);

                        bool added = false;

                        while (!added)
                        {
                            if (CurrentFolder.Info.UID == folder.ParentUID)
                            {
                                // tracked, so need to add multiple folders (archives) with same UIDs
                                CurrentFolder = CurrentFolder.AddFolderInfo(folder);

                                added = true;
                            }
                            else if (CurrentFolder.Parent.GetType() == typeof(LocalFolder))
                                CurrentFolder = CurrentFolder.Parent;
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
                        StorageFile file = StorageFile.Decode(Core.Protocol, header);

                        CurrentFolder.AddFileInfo(file);
                    }
                }

                stream.Close();
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Storage", "Error loading headers " + ex.Message);
            }
        }

        internal void StartWatchers()
        {
            try
            {
                if (FileWatcher == null)
                {
                    FileWatcher = new FileSystemWatcher(RootPath, "*");

                    FileWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;

                    FileWatcher.IncludeSubdirectories = true;

                    FileWatcher.Changed += new FileSystemEventHandler(OnFileChanged);
                    FileWatcher.Created += new FileSystemEventHandler(OnFileChanged);
                    FileWatcher.Deleted += new FileSystemEventHandler(OnFileChanged);
                    FileWatcher.Renamed += new RenamedEventHandler(OnFileRenamed);
                    
                    FileWatcher.EnableRaisingEvents = true;

                }

                if (FolderWatcher == null)
                {
                    FolderWatcher = new FileSystemWatcher(RootPath, "*");

                    FolderWatcher.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.LastWrite;

                    FolderWatcher.IncludeSubdirectories = true;

                    FolderWatcher.Created += new FileSystemEventHandler(OnFolderChanged);
                    FolderWatcher.Deleted += new FileSystemEventHandler(OnFolderChanged);
                    FolderWatcher.Renamed += new RenamedEventHandler(OnFolderRenamed);
                    
                    FolderWatcher.EnableRaisingEvents = true;
                }
            }
            catch { }
        }

        internal void StopWatchers()
        {
            FileWatcher.EnableRaisingEvents = false;
            FileWatcher.Dispose();
            FileWatcher = null;

            FolderWatcher.EnableRaisingEvents = false;
            FolderWatcher.Dispose();
            FolderWatcher = null;

        }

        internal void TrackFile(string path)
        {
            string name = Path.GetFileName(path);
            string dir = Utilities.StripOneLevel(path);

            LocalFolder folder = GetLocalFolder(dir);
            LocalFile file = folder.GetFile(name);

            if (file != null)
                return;

            file = CreateNewFile(name);
            file.Info.SetFlag(StorageFlags.Modified);
            file.Info.SetFlag(StorageFlags.Unlocked);

            folder.AddFile(file);

            Storages.MarkforHash(file, RootPath + dir + "\\" + name, ProjectID, dir);
            Modified = true;
            PeriodicSave = true;

            Storages.CallFileUpdate(ProjectID, folder.GetPath(), file.Info.UID, WorkingChange.Created);
        }

        internal void TrackFile(string path, StorageFile track)
        {
            LocalFolder folder = GetLocalFolder(path);

            if (folder == null)
                return;

            // increase references
            if(Storages.FileMap.ContainsKey(track.HashID))
                Storages.FileMap[track.HashID].References++;

            LocalFile file = new LocalFile(track);
            file.Info.SetFlag(StorageFlags.Modified);
            file.Archived.AddFirst(track);

            folder.AddFile(file);

            Modified = true;
            PeriodicSave = true;

            Storages.CallFileUpdate(ProjectID, folder.GetPath(), file.Info.UID, WorkingChange.Created);
        }

        internal void ReplaceFile(string path, StorageFile replacement)
        {
            string name = Path.GetFileName(path);
            string dir = Utilities.StripOneLevel(path);

            LocalFolder folder = GetLocalFolder(dir);
            LocalFile file = folder.GetFile(name);

            //crit if unlocked need to replace file on drive as well
                
            // only create new entry for un-modified file
            ReadyChange(file, replacement);

            Storages.CallFileUpdate(ProjectID, folder.GetPath(), file.Info.UID, WorkingChange.Updated);
        }

        internal void TrackFolder(string path)
        {
            string name = Path.GetFileName(path);

            string parentPath = Utilities.StripOneLevel(path);

            LocalFolder folder = GetLocalFolder(path);

            if (folder != null)
                return;

            LocalFolder parent = GetLocalFolder(parentPath);

            if(parent == null)
                return;

            folder = CreateNewFolder(parent, name);
            folder.Info.SetFlag(StorageFlags.Modified);

            parent.AddFolder(folder);

            if (Directory.Exists(RootPath + parentPath + "\\" + name))
                folder.Info.SetFlag(StorageFlags.Unlocked);

            Modified = true;
            PeriodicSave = true;

            Storages.CallFolderUpdate(ProjectID, parentPath, folder.Info.UID, WorkingChange.Created);   
        }

        internal void TrackFolder(string path, StorageFolder track)
        {
            string parentPath = Utilities.StripOneLevel(path);

            LocalFolder folder = GetLocalFolder(path);

            if (folder != null)
                return;

            LocalFolder parent = GetLocalFolder(parentPath);

            if (parent == null)
                return;

            folder = new LocalFolder(parent, track);
            folder.Archived.AddFirst(track);
            folder.Info.SetFlag(StorageFlags.Modified);

            parent.AddFolder(folder);

            if (Directory.Exists(RootPath + parentPath + "\\" + folder.Info.Name))
                folder.Info.SetFlag(StorageFlags.Unlocked);

            Modified = true;
            PeriodicSave = true;

            Storages.CallFolderUpdate(ProjectID, parentPath, folder.Info.UID, WorkingChange.Created);
        }

        // Define the event handlers.
        private void OnFileChanged(object source, FileSystemEventArgs e)
        {
            string directory = Path.GetDirectoryName(e.FullPath);
            string filename = Path.GetFileName(e.FullPath); // e.Name is path from watch root

            directory = directory.Replace(RootPath, "");

            try
            {
                if (Directory.Exists(e.FullPath)) // filter out pure folder changes, will be handled by folder functions
                    return;

                LocalFolder folder = GetLocalFolder(directory);

                if (folder == null)
                    return;

                LocalFile file = folder.GetFile(filename);


                if (e.ChangeType == WatcherChangeTypes.Created)
                {
                }

                if (e.ChangeType == WatcherChangeTypes.Changed)
                {
                    if (file != null)
                        Storages.MarkforHash(file, e.FullPath, ProjectID, directory);
                }

                if (e.ChangeType == WatcherChangeTypes.Deleted)
                {
                    if (file != null)
                        file.Info.RemoveFlag(StorageFlags.Unlocked);
                }

                Storages.CallFileUpdate(ProjectID, directory, file != null ? file.Info.UID : 0, WorkingChange.Updated);
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Storage", "OnFileChanged: " + ex.Message);
            }
        }

        private void OnFileRenamed(object source, RenamedEventArgs e)
        {
            try
            {
                string oldName = Path.GetFileName(e.OldFullPath);
                string newName = Path.GetFileName(e.FullPath);

                LocalFolder folder = GetLocalFolder(Path.GetDirectoryName(e.OldFullPath));

                if (folder == null)
                    return;

                
                LocalFile file = folder.GetFile(oldName);

                if (file != null)
                {
                    ReadyChange(file);
                    file.Info.Name = newName;
                    file.Info.Note = "Previously named " + oldName;
                }

                Storages.CallFileUpdate(ProjectID, folder.GetPath(), file != null ? file.Info.UID : 0, WorkingChange.Updated);
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Storage", "OnFileRenamed: " + ex.Message);
            }
        }

        private void OnFolderChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                string directory = e.FullPath.Replace(RootPath, "");
                string dirname = Path.GetFileName(e.FullPath);

                string parent = Utilities.StripOneLevel(directory);

                // no matter what happens here its an update, create / delete only happens when working directly changed

                LocalFolder folder = null;

                if (e.ChangeType == WatcherChangeTypes.Created)
                {
                }

                if (e.ChangeType == WatcherChangeTypes.Deleted)
                {
                    folder = GetLocalFolder(e.FullPath);

                    if (folder != null)
                        LockFolder(folder.GetPath(), folder, true);
                }

                Storages.CallFolderUpdate(ProjectID, parent, folder != null ? folder.Info.UID : 0, WorkingChange.Updated);
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Storage", "OnFolderChanged: " + ex.Message);
            }
        }

        private void OnFolderRenamed(object source, RenamedEventArgs e)
        {
            try
            {
                string newName = Path.GetFileName(e.FullPath);
                string oldName = Path.GetFileName(e.OldFullPath);

                string parent = e.OldFullPath.Replace(RootPath, "");
                parent = Utilities.StripOneLevel(parent);

                LocalFolder folder = GetLocalFolder(e.OldFullPath);

                if (folder != null)
                {
                    ReadyChange(folder);
                    folder.Info.Name = newName;
                    folder.Info.Note = "Previously named " + oldName;
                }

                Storages.CallFolderUpdate(ProjectID, parent, folder.Info.UID, WorkingChange.Updated);
            }
            catch (Exception ex)
            {
                Core.OperationNet.UpdateLog("Storage", "OnFolderRenamed: " + ex.Message);
            }
        }

        internal LocalFolder GetLocalFolder(string path)
        {
            path = path.Replace(RootPath, "");


            // path: \a\b\c

            string[] folders = path.Split('\\');

            LocalFolder current = RootFolder;

            foreach (string name in folders)
                if (name != "")
                {
                    bool found = false;

                    foreach (LocalFolder sub in current.Folders.Values)
                        if (string.Compare(name, sub.Info.Name, true) == 0)
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

        internal void ReadyChange(LocalFile file)
        {
            Modified = true;
            PeriodicSave = true;

            file.Modify(Core.TimeNow, file.Info.Clone());
        }

        internal void ReadyChange(LocalFile file, StorageFile newInfo)
        {
            Modified = true;
            PeriodicSave = true;

            file.Modify(Core.TimeNow, newInfo);
        }

        internal void ReadyChange(LocalFolder folder)
        {
            Modified = true;
            PeriodicSave = true;

            folder.Modify(Core.TimeNow);
        }

        private LocalFolder CreateNewFolder(LocalFolder parent, string dirname )
        {
            StorageFolder info = new StorageFolder();
            LocalFolder folder = new LocalFolder(parent, info);

            byte[] uid = new byte[8];
            Storages.Core.StrongRndGen.GetBytes(uid);
            info.UID = BitConverter.ToUInt64(uid, 0);

            info.ParentUID = parent.Info.UID;
            info.Name = dirname;
            info.Date = Core.TimeNow.ToUniversalTime();
            info.Revs = 5;

            folder.Archived.AddFirst(info);

            return folder;
        }

        private LocalFile CreateNewFile(string name)
        {
            StorageFile info = new StorageFile();
            LocalFile file = new LocalFile(info);

            byte[] uid = new byte[8];
            Storages.Core.StrongRndGen.GetBytes(uid);
            info.UID = BitConverter.ToUInt64(uid, 0);

            info.Name = name;
            info.Date = Core.TimeNow.ToUniversalTime();
            info.Revs = 5;

            file.Archived.AddFirst(info);

            return file;
        }

        internal void WriteWorkingFile(CryptoStream stream, LocalFolder folder, bool commit)
        {
            // write files and all archives if tracked
            foreach (LocalFile file in folder.Files.Values)
            {
                // history
                int count = 0;
                foreach (StorageFile archive in file.Archived)
                {
                    if (commit)
                    {
                        archive.RemoveFlag(StorageFlags.Modified);

                        if (count == file.Info.Revs)
                            break;
                    }

                    Protocol.WriteToFile(archive, stream);
                    count++;
                }

                // integrated
                foreach (StorageFile change in file.Integrated.Values)
                {
                    if (commit)
                        change.RemoveFlag(StorageFlags.Modified);

                    Protocol.WriteToFile(change, stream);
                }
            }

            // foreach tracked folder, recurse
            foreach (LocalFolder sub in folder.Folders.Values)
            {
                // history
                int count = 0;
                foreach (StorageFolder archive in sub.Archived)
                {
                    if (commit)
                    {
                        archive.RemoveFlag(StorageFlags.Modified);

                        if (count == sub.Info.Revs)
                            break;
                    }

                    Protocol.WriteToFile(archive, stream);
                    count++;
                }

                // integrated
                foreach (StorageFile change in sub.Integrated.Values)
                {
                    if (commit)
                        change.RemoveFlag(StorageFlags.Modified);

                    Protocol.WriteToFile(change, stream);
                }

                WriteWorkingFile(stream, sub, commit);
            }
        }

        internal void LockAll()
        {
            LockFolder("", RootFolder, true);
        }

        internal void LockFolder(string dirpath, LocalFolder folder, bool subs)
        {
            List<LockError> errors = new List<LockError>();

            foreach (LocalFile file in folder.Files.Values)
                Storages.LockFileCompletely(Core.LocalDhtID, ProjectID, dirpath, file.Archived, errors);

            folder.Info.RemoveFlag(StorageFlags.Unlocked);

            if (subs)
                foreach (LocalFolder subfolder in folder.Folders.Values)
                    LockFolder(dirpath + "\\" + subfolder.Info.Name, subfolder, subs);

            try
            {
                string path = RootPath + dirpath;

                if (Directory.Exists(path) &&
                    Directory.GetDirectories(path).Length == 0 &&
                    Directory.GetFiles(path).Length == 0)
                    Directory.Delete(path, true);
            }
            catch { }

            Storages.CallFolderUpdate(ProjectID, Utilities.StripOneLevel(dirpath), folder.Info.UID, WorkingChange.Updated);
        }


        internal void SetFileDetails(string path, string newName)
        {
            string oldName = Path.GetFileName(path);
            path = Utilities.StripOneLevel(path);

            // get local file / folder
            LocalFolder folder = GetLocalFolder(path);
            LocalFile file = folder.GetFile(oldName);

            bool renamed = file.Info.Name.CompareTo(newName) != 0;

            // if unlocked try to rename
            if (renamed && file.Info.IsFlagged(StorageFlags.Unlocked))
                File.Move(RootPath + "\\" + path + "\\" + oldName, RootPath + "\\" + path + "\\" + newName);

            // apply changes
            if (renamed)
            {
                ReadyChange(file);
                file.Info.Name = newName;

                Storages.CallFileUpdate(ProjectID, path, file.Info.UID, WorkingChange.Updated);
            }
        }

        internal void SetFolderDetails(string path, string newName)
        {
            string oldName = Path.GetFileName(path);
            string parentPath = Utilities.StripOneLevel(path);
       
            // get local file / folder
            LocalFolder folder = GetLocalFolder(path);

            bool renamed = folder.Info.Name.CompareTo(newName) != 0;

            // if unlocked try to rename
            if (renamed && folder.Info.IsFlagged(StorageFlags.Unlocked))
                Directory.Move(RootPath + "\\" + parentPath + "\\" + oldName, RootPath + "\\" + parentPath + "\\" + newName);

            // apply changes
            if (renamed)
            {
                ReadyChange(folder);
                folder.Info.Name = newName;

                Storages.CallFolderUpdate(ProjectID, parentPath, folder.Info.UID, WorkingChange.Updated);
            }
        }

        internal void ArchiveFile(string path, string name)
        {
            LocalFolder folder = GetLocalFolder(path);
            LocalFile file = folder.GetFile(name);

            // remove unlocked flag
            file.Info.RemoveFlag(StorageFlags.Unlocked);

            ReadyChange(file);
            file.Info.SetFlag(StorageFlags.Archived);

            Storages.CallFileUpdate(ProjectID, path, file.Info.UID, WorkingChange.Updated);
        }

        internal void IntegrateFile(string path, ulong who, StorageFile change)
        {
            // put file in local's integration map for this user id


            LocalFolder folder = GetLocalFolder(path);
            LocalFile file = folder.Files[change.UID];

            file.Integrated[who] = change.Clone();
            change.SetFlag(StorageFlags.Modified);

            Modified = true;
            PeriodicSave = true;

            Storages.CallFileUpdate(ProjectID, path, file.Info.UID, WorkingChange.Updated);
        }

        internal void UnintegrateFile(string path, ulong who, StorageFile change)
        {
            // put file in local's integration map for this user id


            LocalFolder folder = GetLocalFolder(path);
            LocalFile file = folder.Files[change.UID];

            file.Integrated.Remove(who);
            file.Info.SetFlag(StorageFlags.Modified);

            Modified = true;
            PeriodicSave = true;

            Storages.CallFileUpdate(ProjectID, path, file.Info.UID, WorkingChange.Updated);
        }


        internal void DeleteFile(string path, string name)
        {
            LocalFolder folder = GetLocalFolder(path);
            LocalFile file = folder.GetFile(name);

            folder.Files.Remove(file.Info.UID);

            Storages.CallFileUpdate(ProjectID, path, file.Info.UID, WorkingChange.Removed);
        }

        internal void ArchiveFolder(string path)
        {
            LocalFolder folder = GetLocalFolder(path);

            // remove unlocked flag
            folder.Info.RemoveFlag(StorageFlags.Unlocked);

            ReadyChange(folder);
            folder.Info.SetFlag(StorageFlags.Archived);

            Storages.CallFolderUpdate(ProjectID, Utilities.StripOneLevel(path), folder.Info.UID, WorkingChange.Updated);
        }

        internal void DeleteFolder(string path)
        {
            LocalFolder folder = GetLocalFolder(path);

            folder.Parent.Folders.Remove(folder.Info.UID);

            Storages.CallFolderUpdate(ProjectID, Utilities.StripOneLevel(path), folder.Info.UID, WorkingChange.Removed);
        }

        internal void RestoreFolder(string path)
        {
            LocalFolder folder = GetLocalFolder(path);

            // remove unlocked flag
            folder.Info.RemoveFlag(StorageFlags.Unlocked);

            ReadyChange(folder);
            folder.Info.RemoveFlag(StorageFlags.Archived);

            Storages.CallFolderUpdate(ProjectID, Utilities.StripOneLevel(path), folder.Info.UID, WorkingChange.Updated);
        }

        internal void RestoreFile(string path, string name)
        {
            LocalFolder folder = GetLocalFolder(path);
            LocalFile file = folder.GetFile(name);

            // remove unlocked flag
            file.Info.RemoveFlag(StorageFlags.Unlocked);

            ReadyChange(file);
            file.Info.RemoveFlag(StorageFlags.Archived);

            Storages.CallFileUpdate(ProjectID, path, file.Info.UID, WorkingChange.Updated);
        }

        internal void SetRevs(string path, bool isFile, byte revs)
        {
            // file
            if (isFile)
            {
                string name = Path.GetFileName(path);
                path = Utilities.StripOneLevel(path);
                LocalFolder folder = GetLocalFolder(path);
                LocalFile file = folder.GetFile(name);

                ReadyChange(file);
                file.Info.Revs = revs;

                Storages.CallFileUpdate(ProjectID, path, file.Info.UID, WorkingChange.Updated);  
            }

            // folder
            else
            {
                LocalFolder folder = GetLocalFolder(path);
                ReadyChange(folder);
                folder.Info.Revs = revs;

                Storages.CallFolderUpdate(ProjectID, Utilities.StripOneLevel(path), folder.Info.UID, WorkingChange.Updated);
            }
        }

        internal void SetNote(string path, StorageItem targetItem, bool isFile, string note)
        {
            note = (note.Length == 0) ? null : note;

            LinkedList<StorageItem> archived = null;

            string name = "";

            // file
            if (isFile)
            {
                name = Path.GetFileName(path);
                path = Utilities.StripOneLevel(path);
                LocalFolder folder = GetLocalFolder(path);
                LocalFile file = folder.GetFile(name);

                archived = file.Archived;
            }

            // folder
            else
            {
                LocalFolder folder = GetLocalFolder(path);

                archived = folder.Archived;
            }

            // update
            foreach (StorageItem item in archived)
                if (item == targetItem)
                {
                    item.Note = note;
                    item.SetFlag(StorageFlags.Modified);

                    Modified = true;
                    PeriodicSave = true;
                }

            if(isFile)
                Storages.CallFileUpdate(ProjectID, path, targetItem.UID, WorkingChange.Updated);
            else
                Storages.CallFolderUpdate(ProjectID, Utilities.StripOneLevel(path), targetItem.UID, WorkingChange.Updated);
        }



        internal void RefreshHigherChanges(ulong id)
        {

            RemoveHigherChanges(RootFolder, id);
            
            /*
		        for each uid
				    cache archive archive files newer than current file
				    cache latest integrated file from node on path to ourselves
				    if uid does not exist
					    check all uids to see if dupe exists with diff uid (itemdiff)
					    if exists, replace our uid, with higher's uid
					    else if name conflict create locally, remame local .old.
					    else create locally
             */


            if (!Storages.StorageMap.ContainsKey(id))
                return;

            StorageHeader headerx = Storages.StorageMap[id].Header;

            string path = Storages.GetFilePath(headerx);

            if (!File.Exists(path))
                return;

            try
            {
                FileStream filex = new FileStream(path, FileMode.Open);
                CryptoStream crypto = new CryptoStream(filex, headerx.FileKey.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Core.Protocol, FileAccess.Read);

                ulong remoteUID = 0;
                LocalFolder currentFolder = RootFolder;
                bool readingProject = false;

                G2Header header = null;

                while (stream.ReadPacket(ref header))
                {
                    if (header.Name == StoragePacket.Root)
                    {
                        StorageRoot root = StorageRoot.Decode(Core.Protocol, header);

                        readingProject = (root.ProjectID == ProjectID);
                    }

                    if (readingProject)
                    {
                        if (header.Name == StoragePacket.Folder)
                        {
                            StorageFolder folder = StorageFolder.Decode(Core.Protocol, header);

                            /* if new UID 
                            if (remoteUID == folder.UID)
                                continue;

                            remoteUID = folder.UID;

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
                                    currentFolder = (FolderNode)currentFolder.Parent;
                                else
                                    break;
                            }*/
                        }

                        if (header.Name == StoragePacket.File)
                        {
                            StorageFile file = StorageFile.Decode(Core.Protocol, header);

                            /* if new UID 
                            if (remoteUID == file.UID)
                                continue;

                            remoteUID = file.UID;

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


                            foreach (StorageFile archive in currentFile.Archived)
                                if (Storages.ItemDiff(archive, file) == StorageActions.None)
                                    if (archive.Date == file.Date)
                                    {
                                        found = true;
                                        break;
                                    }

                            if (!found)
                                currentFile.Changes[id] = file;*/
                        }
                    }
                }

                stream.Close();
            }
            catch
            {

            }
        }

        internal void RemoveAllHigherChanges()
        {
            RemoveHigherChanges(RootFolder, 0);
        }

        private void RemoveHigherChanges(LocalFolder folder, ulong id)
        {
            foreach (LocalFile file in folder.Files.Values)
            {
                if (id == 0)
                    file.HigherChanges.Clear();

                else if (file.HigherChanges.ContainsKey(id))
                    file.HigherChanges.Remove(id);
            }

            foreach(LocalFolder subfolder in folder.Folders.Values)
            {
                if (id == 0)
                    subfolder.HigherChanges.Clear();

                else if (subfolder.HigherChanges.ContainsKey(id))
                    subfolder.HigherChanges.Remove(id);

                RemoveHigherChanges(subfolder, id);
            }
        }

        internal void AutoIntegrate()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        
    }

    internal class LocalFolder
    {
        // keep track of untracked because when locked, this is the only way those files would be seen
        internal StorageFolder Info;
        internal LinkedList<StorageItem> Archived = new LinkedList<StorageItem>();
        internal Dictionary<ulong, StorageItem> Integrated = new Dictionary<ulong, StorageItem>();
        internal Dictionary<ulong, List<StorageItem>> HigherChanges = new Dictionary<ulong, List<StorageItem>>();

        internal LocalFolder Parent;
        
        internal Dictionary<ulong, LocalFolder> Folders = new Dictionary<ulong, LocalFolder>();
        internal Dictionary<ulong, LocalFile> Files = new Dictionary<ulong, LocalFile>();

       
        internal LocalFolder(LocalFolder parent, StorageFolder info)
        {
            Parent = parent;

            Info = info;
        }

        internal void Modify(DateTime time)
        {
            // 1 change tracked per commit
            if (Info.IsFlagged(StorageFlags.Modified))
                return;


            StorageFolder changed = new StorageFolder();
            changed.ParentUID = Info.ParentUID;
            changed.UID = Info.UID;
            changed.Name = Info.Name;
            changed.Date = time.ToUniversalTime();
            changed.Flags = Info.Flags;
            changed.SetFlag(StorageFlags.Modified);
            changed.Revs = Info.Revs;

            Archived.AddFirst(changed);

            Info = changed;
        }

        internal string GetPath()
        {
            string path = "";

            LocalFolder up = this;

            while (up.Parent != null)
            {
                path = "\\" + up.Info.Name + path;
                up = up.Parent;
            }

            return path;
        }

        internal LocalFile GetFile(string name)
        {
            foreach (LocalFile file in Files.Values)
                if (string.Compare(name, file.Info.Name, true) == 0)
                    return file;

            return null;
        }

        internal void AddFolder(LocalFolder folder)
        {
            ulong uid = folder.Info.UID;

            Debug.Assert(!Folders.ContainsKey(uid));

            Folders[uid] = folder;
        }

        internal LocalFolder AddFolderInfo(StorageFolder info)
        {
            if (!Folders.ContainsKey(info.UID))
                Folders[info.UID] = new LocalFolder(this, info);

            LocalFolder folder = Folders[info.UID];

            if (info.Integrated != 0)
                folder.Integrated[info.Integrated] = info;
            else
                folder.Archived.AddLast(info);   

            return folder;
        }

        internal void AddFile(LocalFile file)
        {
            ulong uid = file.Info.UID;

            Debug.Assert(!Files.ContainsKey(uid));

            Files[uid] = file;
        }

        internal void AddFileInfo(StorageFile info)
        {
            if (!Files.ContainsKey(info.UID))
                Files[info.UID] = new LocalFile(info);

            if(info.Integrated != 0)
                Files[info.UID].Integrated[info.Integrated] = info; 
            else
                Files[info.UID].Archived.AddLast(info);   
        }

        public override string ToString()
        {
            return Info.Name;
        }
    }

    internal class LocalFile
    {
        internal StorageFile Info;
        internal LinkedList<StorageItem> Archived = new LinkedList<StorageItem>();
        internal Dictionary<ulong, StorageItem> Integrated = new Dictionary<ulong, StorageItem>();
        internal Dictionary<ulong, List<StorageItem>> HigherChanges = new Dictionary<ulong, List<StorageItem>>();

        internal LocalFile(StorageFile info)
        {
            Info = info;
        }

        internal void Modify(DateTime time, StorageFile newInfo)
        {
            // Details is mirror of file in working folder, it can change multiple times before the user commits, archive the
            //   original info when the user starts messing with stuff, only once, dont want to archive stuff not accessible on the network


            if (Info.IsFlagged(StorageFlags.Modified))
            {
                Info = newInfo;
                Archived.RemoveFirst();
                Archived.AddFirst(newInfo);
            }
            else
            {
                Archived.AddFirst(newInfo);
                Info = newInfo;
            }

            Info.SetFlag(StorageFlags.Modified);
            Info.Date = time.ToUniversalTime();
        }

        public override string ToString()
        {
            return Info.Name;
        }
    }


}
