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
                bool readingProject = false;

                LocalFolder CurrentFolder = RootFolder;
                string CurrentPath = RootPath;

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

                            bool added = false;

                            while (!added)
                            {
                                if (CurrentFolder.Info.UID == folder.ParentUID)
                                {
                                    // tracked, so need to add multiple folders (archives) with same UIDs
                                    CurrentFolder = CurrentFolder.AddFolderInfo(folder);

                                    added = true;
                                }
                                else if (CurrentFolder.Parent == null) // error
                                    break;
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

        internal bool FileExists(string path)
        {
            string name = Path.GetFileName(path);
            string dir = Utilities.StripOneLevel(path);

            LocalFolder folder = GetLocalFolder(dir);

            if (folder == null)
                return false;

            LocalFile file = folder.GetFile(name);

            if (file == null)
                return false;

            return true;
        }

        internal bool TrackFile(string path)
        {
            string name = Path.GetFileName(path);
            string dir = Utilities.StripOneLevel(path);

            LocalFolder folder = GetLocalFolder(dir);
            LocalFile file = folder.GetFile(name);

            if (file != null)
                return false;

            file = CreateNewFile(name);
            file.Info.SetFlag(StorageFlags.Modified);
            file.Info.SetFlag(StorageFlags.Unlocked);

            folder.AddFile(file);

            Storages.MarkforHash(file, RootPath + dir + Path.DirectorySeparatorChar + name, ProjectID, dir);
            Modified = true;
            PeriodicSave = true;

            Storages.CallFileUpdate(ProjectID, folder.GetPath(), file.Info.UID, WorkingChange.Created);

            return true;
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

        internal void ReplaceFolder(string path, StorageFolder replacement)
        {
            LocalFolder folder = GetLocalFolder(path);

            //crit if unlocked need to replace file on drive as well

            // only create new entry for un-modified file
            ReadyChange(folder, replacement);

            Storages.CallFolderUpdate(ProjectID, folder.Parent.GetPath(), folder.Info.UID, WorkingChange.Updated);
        }

        internal void TrackFolder(string path)
        {
            Debug.Assert(path[path.Length - 1] != Path.DirectorySeparatorChar);

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

            if (Directory.Exists(RootPath + parentPath + Path.DirectorySeparatorChar + name))
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

            if (Directory.Exists(RootPath + parentPath + Path.DirectorySeparatorChar + folder.Info.Name))
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
                    //Debug.WriteLine("File Created " + e.Name);
                }

                if (e.ChangeType == WatcherChangeTypes.Changed)
                {
                    //Debug.WriteLine("File Changed " + e.Name);

                    if (file != null)
                        Storages.MarkforHash(file, e.FullPath, ProjectID, directory);
                }

                if (e.ChangeType == WatcherChangeTypes.Deleted)
                {
                    //Debug.WriteLine("File Deleted " + e.Name);

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
            //Debug.WriteLine("File Renamed " + e.OldName + " to " + e.Name);


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
                    file.Info.Note = "from " + oldName + " to " + newName;
                }

                // when visual studio saves a file, it creates a temp, writes to it, deletes the original file and renames the temp to the original
                // as opposed to files being created with the same name while their couterparts remain locked; renaming triggers the file to be
                // unlocked and rehashed
                else
                {
                    file = folder.GetFile(newName);

                    if (file != null)
                    {
                        file.Info.SetFlag(StorageFlags.Unlocked);
                        Storages.MarkforHash(file, e.FullPath, ProjectID, folder.GetPath());
                    }
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
                    {
                        List<LockError> errors = new List<LockError>();
                        LockFolder(folder.GetPath(), folder, true, errors);
                    }
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
                    folder.Info.Note = "from " + oldName + " to " + newName;
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

            string[] folders = path.Split(Path.DirectorySeparatorChar);

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


        internal LocalFile GetLocalFile(string path)
        {
            string name = Path.GetFileName(path);

            path = Utilities.StripOneLevel(path);

            LocalFolder folder = GetLocalFolder(path);

            if (folder == null)
                return null;

            return folder.GetFile(name);
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

            folder.Modify(Core.TimeNow, folder.Info.Clone());
        }

        internal void ReadyChange(LocalFolder folder, StorageFolder newInfo)
        {
            Modified = true;
            PeriodicSave = true;

            folder.Modify(Core.TimeNow, newInfo);
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
                    if (file.Info.HashID == 0 || file.Info.InternalHash == null)
                        continue; // happens if file is still being hashed and auto-save is called

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
                foreach (ulong who in file.Integrated.Keys)
                {
                    StorageFile integrated = (StorageFile) file.Integrated[who];

                    if (commit)
                        integrated.RemoveFlag(StorageFlags.Modified);

                    integrated.IntegratedID = who;
                    Protocol.WriteToFile(integrated, stream);
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

        internal void LockAll(List<LockError> errors )
        {
            // make sure files/folder we care about are deleted

            LockFolder("", RootFolder, true, errors);
        }

        internal void LockFolder(string dirpath, LocalFolder folder, bool subs, List<LockError> errors )
        {
            foreach (LocalFile file in folder.Files.Values)
                Storages.LockFileCompletely(Core.LocalDhtID, ProjectID, dirpath, file.Archived, errors);

            folder.Info.RemoveFlag(StorageFlags.Unlocked);

            if (subs)
                foreach (LocalFolder subfolder in folder.Folders.Values)
                    LockFolder(dirpath + Path.DirectorySeparatorChar + subfolder.Info.Name, subfolder, subs, errors);

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
            if(newName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                return;

            string oldName = Path.GetFileName(path);
            path = Utilities.StripOneLevel(path);

            // get local file / folder
            LocalFolder folder = GetLocalFolder(path);
            LocalFile file = folder.GetFile(oldName);

            bool renamed = file.Info.Name.CompareTo(newName) != 0;

            // if unlocked try to rename
            if (renamed && file.Info.IsFlagged(StorageFlags.Unlocked))
                File.Move(RootPath + Path.DirectorySeparatorChar + path + Path.DirectorySeparatorChar + oldName, RootPath + Path.DirectorySeparatorChar + path + Path.DirectorySeparatorChar + newName);

            // apply changes
            if (renamed)
            {
                ReadyChange(file);
                file.Info.Note = "from " + file.Info.Name + " to " + newName;
                file.Info.Name = newName;

                Storages.CallFileUpdate(ProjectID, path, file.Info.UID, WorkingChange.Updated);
            }
        }

        internal void SetFolderDetails(string path, string newName)
        {
            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                return;

            string oldName = Path.GetFileName(path);
            string parentPath = Utilities.StripOneLevel(path);
       
            // get local file / folder
            LocalFolder folder = GetLocalFolder(path);

            bool renamed = folder.Info.Name.CompareTo(newName) != 0;

            // if unlocked try to rename
            if (renamed && folder.Info.IsFlagged(StorageFlags.Unlocked))
                Directory.Move(RootPath + Path.DirectorySeparatorChar + parentPath + Path.DirectorySeparatorChar + oldName, RootPath + Path.DirectorySeparatorChar + parentPath + Path.DirectorySeparatorChar + newName);

            // apply changes
            if (renamed)
            {
                ReadyChange(folder);
                folder.Info.Note = "from " + folder.Info.Name + " to " + newName;
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
            // dont set file.info modified, because hash hasn't changed

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

            Modified = true;
            PeriodicSave = true;

            Storages.CallFileUpdate(ProjectID, path, file.Info.UID, WorkingChange.Updated);
        }


        internal void DeleteFile(string path, string name)
        {
            LocalFolder folder = GetLocalFolder(path);
            LocalFile file = folder.GetFile(name);

            folder.Files.Remove(file.Info.UID);

            Modified = true;
            PeriodicSave = true;

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

            Modified = true;
            PeriodicSave = true;

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



        internal bool RefreshHigherChanges(ulong id)
        {
            // first remove changes from this id
            RemoveHigherChanges(RootFolder, id);


            bool save = false;

            if (!Storages.StorageMap.ContainsKey(id))
                return save;

            // this is the first step in auto-integration
            // go through this id's storage file, and add any changes or updates to our own system
            StorageHeader headerx = Storages.StorageMap[id].Header;

            string path = Storages.GetFilePath(headerx);

            if (!File.Exists(path))
                return save;

            try
            {
                FileStream filex = new FileStream(path, FileMode.Open);
                CryptoStream crypto = new CryptoStream(filex, headerx.FileKey.CreateDecryptor(), CryptoStreamMode.Read);
                PacketStream stream = new PacketStream(crypto, Core.Protocol, FileAccess.Read);

                ulong currentUID = 0;
                LocalFolder currentFolder = RootFolder;
                LocalFile currentFile = null;
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
                            StorageFolder readFolder = StorageFolder.Decode(Core.Protocol, header);


                            // if new uid
                            if (currentUID != readFolder.UID)
                            {
                                // if only 1 entry in changes for previous file, remove it, its probably a dupe of local
                                // and integration needs more than one file to happen
                                if (currentFolder.HigherChanges.ContainsKey(id) && currentFolder.HigherChanges[id].Count == 1)
                                    currentFolder.HigherChanges.Remove(id);

                                // set new id
                                currentUID = readFolder.UID;

                                bool added = false;

                                while (!added)
                                {
                                    if (currentFolder.Info.UID == readFolder.ParentUID)
                                    {
                                        // if matches with local uid, set current file to file
                                        if (currentFolder.Folders.ContainsKey(currentUID))
                                            currentFolder = currentFolder.Folders[currentUID];

                                        // if doesnt match
                                        else
                                        {
                                            // check for conflicting name
                                            foreach (LocalFolder subfolder in currentFolder.Folders.Values)
                                                if (!subfolder.Info.IsFlagged(StorageFlags.Archived) && subfolder.Info.Name == readFolder.Name)
                                                    subfolder.Info.Name = subfolder.Info.Name + ".fix";

                                            // if not found, create folder
                                            currentFolder = currentFolder.AddFolderInfo(readFolder);
                                            save = true;
                                        }

                                        added = true;
                                    }

                                    else if (currentFolder.Parent == null)
                                        break; // error, couldn't find parent of folder that was read

                                    else if (currentFolder.Parent.GetType() == typeof(LocalFolder))
                                        currentFolder = currentFolder.Parent;

                                    else
                                        break;
                                }
                            }

                            // if file does not equal null
                            if (currentFolder != null)
                            {
                                // log change if file newer than ours
                                // if if not in higher's history 
                                // or if file integrated by higher by a node we would have inherited from

                                // we look for our own file in higher changes, if there then we can auto integrate

                                if (readFolder.Date >= currentFolder.Info.Date)
                                    if (readFolder.IntegratedID == 0 ||
                                        readFolder.IntegratedID == Core.LocalDhtID ||
                                        Core.Links.IsHigher(Core.LocalDhtID, ProjectID))
                                        currentFolder.AddHigherChange(id, readFolder);
                            }

                        }

                        if (header.Name == StoragePacket.File)
                        {
                            StorageFile readFile = StorageFile.Decode(Core.Protocol, header);

                            // if new uid
                            if (currentUID != readFile.UID)
                            {
                                // if only 1 entry in changes for previous file, remove it, its probably a dupe of local
                                // and integration needs more than one file to happen
                                if (currentFile != null && currentFile.HigherChanges.ContainsKey(id) && currentFile.HigherChanges[id].Count == 1)
                                    currentFile.HigherChanges.Remove(id);

                                // set new id
                                currentUID = readFile.UID;

                                currentFile = null;

                                // if file exists with UID
                                if (currentFolder.Files.ContainsKey(currentUID))
                                    currentFile = currentFolder.Files[currentUID];

                                // else add file as temp, mark as changed
                                else
                                {
                                    // check for conflicting name
                                    foreach (LocalFile checkFile in currentFolder.Files.Values)
                                        if (!checkFile.Info.IsFlagged(StorageFlags.Archived) && checkFile.Info.Name == readFile.Name)
                                            checkFile.Info.Name = checkFile.Info.Name + ".fix";

                                    currentFile = currentFolder.AddFileInfo(readFile);
                                    save = true;

                                    if(!Storages.FileExists(currentFile.Info))
                                        Storages.DownloadFile(id, currentFile.Info );
                                }
                            }

                            // if file does not equal null
                            if (currentFile != null)
                            {
                                if (readFile.Date >= currentFile.Info.Date)
                                    if (readFile.IntegratedID == 0 ||
                                        readFile.IntegratedID == Core.LocalDhtID ||
                                        Core.Links.IsHigher(Core.LocalDhtID, ProjectID))
                                        currentFile.AddHigherChange(id, readFile);
                            }
                          
                        }
                    }
                }

                stream.Close();
            }
            catch
            {

            }

            return save;
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

        internal void AutoIntegrate(bool doSave)
        {
            // only 'save' if file system in a saved state
			// still merge with unsaved file system, just won't be made permanent till save is clicked

            if (AutoIntegrate(RootFolder) || doSave)
                if (!Modified)
                    Storages.SaveLocal(ProjectID); // triggers permanent save of system for publish on the network
                else
                    PeriodicSave = true; // triggers save of working file system
        }

        private bool AutoIntegrate(LocalFolder folder)
        {
            bool save = false;

            if (!folder.Info.IsFlagged(StorageFlags.Modified))
            {
                StorageFolder latestFolder = folder.Info;

                List<ulong> uplinkIDs = new List<ulong>();
                uplinkIDs.Add(Core.LocalDhtID);
                uplinkIDs.AddRange(Core.Links.GetUplinkIDs(Core.LocalDhtID, ProjectID));

                // this will process will find higher has integrated my file
                // and highest has integrated his file, and return latest

                // from self to highest
                foreach (ulong id in uplinkIDs)
                    if (folder.HigherChanges.ContainsKey(id))
                        // higherChanges consists of files that are newer than local
                        foreach (StorageFolder changeFolder in folder.HigherChanges[id])
                            if (changeFolder.Date >= latestFolder.Date && Storages.ItemDiff(latestFolder, changeFolder) == StorageActions.None)
                            {
                                latestFolder = (StorageFolder)folder.HigherChanges[id][0]; // first element is newest file
                                break;
                            }

                // if current file/folder is not our own (itemdiff)
                if (Storages.ItemDiff(latestFolder, folder.Info) != StorageActions.None)
                {
                    //crit
                    /*if unlocked, overwrites
                      replace should also use this function
                    */
                    folder.Info = latestFolder;
                    folder.Archived.AddFirst(latestFolder);

                    save = true;
                }
            }

            foreach (LocalFile file in folder.Files.Values)
                if(AutoIntegrate(file))
                    save = true;

            foreach (LocalFolder subfolder in folder.Folders.Values)
                if( AutoIntegrate(subfolder))
                    save = true;

            return save;
        }

        private bool AutoIntegrate(LocalFile file)
        {
            // If file/folder not flagged as modified
            if(file.Info.IsFlagged(StorageFlags.Modified))
                return false;

            StorageFile latestFile = file.Info;
            List<StorageFile> inheritIntegrated = new List<StorageFile>();

            List<ulong> uplinkIDs = new List<ulong>();
            uplinkIDs.Add(Core.LocalDhtID);
            uplinkIDs.AddRange(Core.Links.GetUplinkIDs(Core.LocalDhtID, ProjectID));

            ulong directHigher = (uplinkIDs.Count >= 2) ? uplinkIDs[1] : 0;


            // this process will find higher has integrated my file
            // and highest has integrated his file, and return latest
            
            // from self to highest
            foreach (ulong id in uplinkIDs)
                if (file.HigherChanges.ContainsKey(id))
                    // higherChanges consists of files that are newer than local
                    foreach (StorageFile changeFile in file.HigherChanges[id])
                    {
                        if (changeFile.Date >= latestFile.Date && Storages.ItemDiff(latestFile, changeFile) == StorageActions.None)
                        {
                            latestFile = (StorageFile)file.HigherChanges[id][0]; // first element is newest file

                            if (id != directHigher)
                                break;
                        }

                        if (id == directHigher &&
                            changeFile.IntegratedID != 0 &&
                            Core.Links.IsAdjacent(changeFile.IntegratedID, ProjectID))
                            inheritIntegrated.Add(changeFile);
                    }

            // if current file/folder is not our own (itemdiff)
            bool save = false;

            if (Storages.ItemDiff(latestFile, file.Info) != StorageActions.None)
            {
                //crit
                //if unlocked, overwrites
				//replace should also use this function
						
                file.Info = latestFile;
                file.Archived.AddFirst(latestFile);

                save = true;
            }

            // merges integration list for nodes adjacent to ourselves
            // works even if higher integrates more files, but doesn't necessarily change the file's hash
            foreach (StorageFile inherited in inheritIntegrated)
                if (!file.Integrated.ContainsKey(inherited.IntegratedID) ||
                    inherited.Date > file.Integrated[inherited.IntegratedID].Date)
                {
                    file.Integrated[inherited.IntegratedID] = inherited;
                    save = true;
                }

            return save;
        }


        internal void MoveFile(string sourcePath, string destPath, List<string> errors)
        {
            // get source folder
            string name = Path.GetFileName(sourcePath);

            sourcePath = Utilities.StripOneLevel(sourcePath);

            LocalFolder sourceFolder = GetLocalFolder(sourcePath);
            LocalFolder destFolder = GetLocalFolder(destPath);

            if (sourceFolder == null || destFolder == null || sourceFolder == destFolder)
                return;

            // get source file
            LocalFile sourceFile = sourceFolder.GetFile(name);

            if (sourceFile == null)
                return;

            // if name exists with diff uid, return error
            foreach (LocalFile check in destFolder.Files.Values)
                if (check.Info.UID != sourceFile.Info.UID && 
                    String.Compare(check.Info.Name, sourceFile.Info.Name, true) == 0)
                {
                    errors.Add("File with same name exists at " + destPath);
                    return;
                }

            // if uid exists in destination, merge histories with diff hashes
            WorkingChange destChange = WorkingChange.Created;
            WorkingChange sourceChange = WorkingChange.Updated;

            if (destFolder.Files.ContainsKey(sourceFile.Info.UID))
            {
                sourceFile.MergeFile( destFolder.Files[sourceFile.Info.UID] );
                destFolder.Files[sourceFile.Info.UID] = sourceFile;
                destChange = WorkingChange.Updated;
            }
            else
                destFolder.AddFile(sourceFile);

            // make note file was moved at source in destination
            LocalFile ghost = new LocalFile(sourceFile.Info.Clone()); // do before modified/new date set
            ReadyChange(sourceFile);
            sourceFile.Info.Note = "Moved from " + (sourcePath == "" ? Path.DirectorySeparatorChar.ToString() : sourcePath);

            sourceFolder.Files.Remove(sourceFile.Info.UID);
            
            // only leave a ghost if this file has a committed history
            if (sourceFile.Archived.Count > 1 || !sourceFile.Info.IsFlagged(StorageFlags.Modified))
            {
                ghost.Archived.AddFirst(ghost.Info);
                sourceFolder.AddFile(ghost);
                ReadyChange(ghost);
                ghost.Info.Note = "Moved to " + (destPath == "" ? Path.DirectorySeparatorChar.ToString() : destPath);
                ghost.Info.SetFlag(StorageFlags.Archived);
            }
            else
                sourceChange = WorkingChange.Removed;

            // move actual file if unlocked on disk, create new folder if need be
            if (File.Exists(RootPath + sourcePath + Path.DirectorySeparatorChar + name))
            {
                Directory.CreateDirectory(RootPath + destPath);

                // exceptions handled by caller
                File.Move(  RootPath + sourcePath + Path.DirectorySeparatorChar + name,
                            RootPath + destPath + Path.DirectorySeparatorChar + name);
            }

            // file created at destination, updated at source
            Storages.CallFileUpdate(ProjectID, destPath, sourceFile.Info.UID,  destChange);
            Storages.CallFileUpdate(ProjectID, sourcePath, sourceFile.Info.UID, sourceChange);
        }


        internal void MoveFolder(LocalFolder sourceFolder, string destPath, List<string> errors)
        {
            // cant move root folder ;)
            if (sourceFolder.Parent == null)
                return;

            LocalFolder parentFolder = sourceFolder.Parent;

            LocalFolder destFolder = GetLocalFolder(destPath);

            if (destFolder == null || destFolder == parentFolder)
                return;

            // prevent folder from being moved inside of itself
            LocalFolder oneUp = destFolder;
            while (oneUp != null)
            {
                if (oneUp == sourceFolder)
                    return;

                oneUp = oneUp.Parent;
            }


            // if name exists with diff uid, return error
            foreach (LocalFolder check in destFolder.Folders.Values)
                if (check.Info.UID != sourceFolder.Info.UID &&
                    String.Compare(check.Info.Name, sourceFolder.Info.Name, true) == 0)
                {
                    errors.Add("Folder with same name exists at " + destPath);
                    return;
                }


            // if uid exists in destination, merge histories with diff hashes
            WorkingChange destChange = WorkingChange.Created;
            WorkingChange sourceChange = WorkingChange.Updated;

            if (destFolder.Folders.ContainsKey(sourceFolder.Info.UID))
            {
                destFolder.Folders[sourceFolder.Info.UID] = sourceFolder;
                destChange = WorkingChange.Updated;
            }
            else
                destFolder.AddFolder(sourceFolder);


            // make note file was moved at source in destination
            LocalFolder ghost = new LocalFolder(parentFolder, sourceFolder.Info.Clone());

            ReadyChange(sourceFolder);
            sourceFolder.Parent = destFolder;
            string parentPath = parentFolder.GetPath();
            sourceFolder.Info.Note = "Moved from " + (parentPath == "" ? Path.DirectorySeparatorChar.ToString() : parentPath);
            sourceFolder.Info.ParentUID = destFolder.Info.UID;

            parentFolder.Folders.Remove(sourceFolder.Info.UID);

            // only leave a ghost if this file has a committed history
            if (sourceFolder.Archived.Count > 1 || !sourceFolder.Info.IsFlagged(StorageFlags.Modified))
            {
                ghost.Archived.AddFirst(ghost.Info);
                parentFolder.AddFolder(ghost);
                ReadyChange(ghost); // created ghost needs to have 2 entries because applydiff() looks for date of previous file to apply change
                ghost.Info.Note = "Moved to " + (destPath == "" ? Path.DirectorySeparatorChar.ToString() : destPath);
                ghost.Info.SetFlag(StorageFlags.Archived);
            }
            else
                sourceChange = WorkingChange.Removed;

            // move actual file if unlocked on disk, create new folder if need be
            string name = sourceFolder.Info.Name;
            if (File.Exists(RootPath + parentFolder.GetPath() + Path.DirectorySeparatorChar + name))
            {
                Directory.CreateDirectory(RootPath + destPath);

                // exceptions handled by caller
                File.Move(RootPath + parentFolder.GetPath() + Path.DirectorySeparatorChar + name,
                            RootPath + destPath + Path.DirectorySeparatorChar + name);
            }

            // file created at destination, updated at source
            Storages.CallFolderUpdate(ProjectID, destPath, sourceFolder.Info.UID, destChange);
            Storages.CallFolderUpdate(ProjectID, parentFolder.GetPath(), sourceFolder.Info.UID, sourceChange);
        }


        internal void CreateDirectory(string path)
        {
            string[] destDirs = path.Split(Path.DirectorySeparatorChar);

            LocalFolder destFolder = RootFolder;

            foreach (string dir in destDirs)
            {
                bool notFound = true;

                foreach (LocalFolder folder in destFolder.Folders.Values)
                    if (folder.Info.Name == dir)
                    {
                        destFolder = folder;
                        notFound = false;
                        break;
                    }

                if (notFound)
                {
                    LocalFolder newFolder = CreateNewFolder(destFolder, dir);
                    destFolder.AddFolder(newFolder);
                    destFolder = newFolder;
                }
            }
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

        internal void Modify(DateTime time, StorageFolder newInfo)
        {
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

        internal string GetPath()
        {
            string path = "";

            LocalFolder up = this;

            while (up.Parent != null)
            {
                path = Path.DirectorySeparatorChar + up.Info.Name + path;
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

            // if this is integration info, add to integration map
            if (info.IntegratedID != 0)
                folder.Integrated[info.IntegratedID] = info;

            // if history info, add to files history
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

        internal LocalFile AddFileInfo(StorageFile info)
        {
            if (!Files.ContainsKey(info.UID))
                Files[info.UID] = new LocalFile(info);

            LocalFile file =  Files[info.UID];

            if(info.IntegratedID != 0)
                file.Integrated[info.IntegratedID] = info; 
            else
                file.Archived.AddLast(info);

            return file;
        }

        public override string ToString()
        {
            return Info.Name;
        }

        internal void AddHigherChange(ulong id, StorageFolder change)
        {
            if (!HigherChanges.ContainsKey(id))
                HigherChanges[id] = new List<StorageItem>();

            HigherChanges[id].Add(change);
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

        internal void AddHigherChange(ulong id, StorageFile change)
        {
            if (!HigherChanges.ContainsKey(id))
                HigherChanges[id] = new List<StorageItem>();

            HigherChanges[id].Add(change);
        }


        internal void MergeFile(LocalFile file)
        {
            // if there are any unique files in the merge that don't exist locally, add them


            foreach (StorageFile merge in file.Archived)
            {
                // check if exists
                bool fileExists = false;

                foreach (StorageFile item in Archived)
                    if (Utilities.MemCompare(item.InternalHash, merge.InternalHash))
                    {
                        fileExists = true;
                        break;
                    }

                // add file if doesnt exist
                if (!fileExists)
                {
                    bool added = false;

                    for (LinkedListNode<StorageItem> item = Archived.First; item != null; item = item.Next)
                        if (item.Value.Date > merge.Date) // loop until item is no longer the lowest (oldest)
                        {
                            if(item.Value == Info)
                                Archived.AddAfter(item, merge); // even if newest, the file being moved is the one at top
                            else
                                Archived.AddBefore(item, merge); // put file in right spot

                            added = true;

                            break;
                        }

                    if (!added)
                        Archived.AddLast(merge); // oldest, at end

                }
            }
        }
    }


}
