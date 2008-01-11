using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using DeOps.Implementation;
using DeOps.Interface;


namespace DeOps.Services.Storage
{
    [ComVisible(true)]
    public partial class InfoPanel : UserControl
    {
        internal StorageView ParentView;
        internal StorageService Storages;

        internal bool DisplayActivated;

        internal FolderNode CurrentFolder;
        internal FileItem CurrentFile;
        internal StorageItem CurrentItem;
        internal string CurrentPath = "";
        
        internal bool IsFile;
        bool Local;
        bool WatchTransfers;

        internal bool DiffsView;

        internal Dictionary<ulong, StorageItem> CurrentChanges = new Dictionary<ulong, StorageItem>();
        internal Dictionary<ulong, StorageItem> CurrentIntegrated = new Dictionary<ulong, StorageItem>();

        List<StorageItem> History  = new List<StorageItem>();

        List<StorageFile> StatusList = new List<StorageFile>();

        List<string> Reset = new List<string>();

        const string DefaultPage = @"<html>
                                    <head>
                                    <style>
                                        body { font-family:tahoma; font-size:12px;margin-top:3px;}
                                        td { font-size:10px;vertical-align: middle; }
                                    </style>
                                    </head>

                                    <body bgcolor=#f5f5f5>

                                        <?=note?>

                                    </body>
                                    </html>";

        const string Template = @"<html>

                                <head>
                                <style>
                                    body { font-family:tahoma; font-size:12px;margin-top:3px;}
                                    td { font-size:10px;vertical-align:middle; }
                                    
                                    .menulink {color:#000;text-decoration:none}
                                    a.menulink:hover { text-decoration: underline }

                                    .menurow {padding:2 2 2 2px;vertical-align:middle;}


                                    .menustyle {position:absolute;
                                                background:#fff;
                                                border:1px solid #000;
                                                margin:.5ex 0ex;
                                                padding:0 0 .5ex .8ex;
                                                width:20ex;
                                                z-index:1000;
                                                font-size:12px;}
                                </style>

                                <script>

                                    function stopB(e)
                                    {
	                                    if(!e)
		                                    e=window.event;

	                                    e.cancelBubble=true;
                                    }

                                    function ToggleMenu(img, e, id)
                                    {
	                                    stopB(e);
                                    	
	                                    var span = document.getElementById(id);

	                                    if(span.style.display == '')
	                                    {
		                                    span.style.display = 'none';
		                                    img.src='<?=imgExtend1?>';
	                                    }
	                                    else
	                                    {
		                                    Reset();
		                                    span.style.display = '';
		                                    img.src='<?=imgExtend2?>';
	                                    }
                                    	
	                                    return false;
                                    }

                                    function Reset()
                                    {	
                                        var extend1 = '<?=imgExtend1?>';

	                                    <?=reset?>
                                    }

                                    function UpdateStatus(id, text)
                                    {
                                        document.getElementById(id).innerHTML = text;
                                    }

                                    // keep at bottom of script, otherwise errors
                                    document.onclick = function(event)
                                    {
	                                    Reset();
                                    }
                                </script>
                                </head>

                                <body bgcolor=#f5f5f5>

                                    <table>
                                    <tr>

                                    <td><?=image?></td>

                                    <td>
	                                    <span style='font-size:14px;'><?=name?></span>
	                                    <br>
	                                    <?=note?>
                                    </td>

                                    </tr>
                                    </table>
                                    <br>

                                    <?=changes?>

                                    <?=integrated?>

                                    <?=history?>

                                </body>
                                </html>";


        const string DifferencePage = @"<html>
                                    <head>
                                    <style>
                                        body { font-family:tahoma; font-size:12px;margin-top:3px;}
                                        td { font-size:10px;vertical-align: middle; }
                                    </style>
                                    <script>
                                         function UpdateStatus(id, text)
                                        {
                                            document.getElementById(id).innerHTML = text;
                                        }

                                        function togglehelp()
                                        {
                                            obj = document.getElementById('helpDiv');
                                    
                                            if (obj.style.display == 'none')
                                                obj.style.display = '';
                                            else 
                                                obj.style.display = 'none';
                                        }
                                                                                                                        
                                    </script>
                                    </head>

                                    <body bgcolor=#f5f5f5>

                                        <?=table?>
                                        <br>
                                        <a href='http://cancel' onclick='togglehelp()'>Help</a>
                                        <div id='helpDiv' style='display:none;'>
                                            
                                            Drag files in to add them to your secure storage. Once changes are saved they are inherited by those below you.
                                            Changes are manually reviewed and integrated before moving to those above you.
                                        </div>
                                        
                                    </body>
                                    </html>";

        const string DifferenceTable = @"<br>
                                        <table cellspacing=0 cellpadding=2>
                                            <tr>
	                                            <td colspan=2><span style='font-size:12px;font-weight:bold;'>Showing Last Revisions by</span></td>
	                                        </tr>

                                            <?=next_difference_row?>

                                        </table>";

        const string DifferenceRowTemplate = @"<tr>
	                                            <td <?=bgcolor?> ><font color='#<?=namecolor?>' ><?=name?></font></td>
                                                <td <?=bgcolor?> id='<?=statusID?>'> <?=status?> </td>
                                            </tr>
                                                    
                                            <?=next_difference_row?>";

        const string ChangesTemplate = @"<table cellspacing=0 cellpadding=2>
                                        <tr>
	                                        <td colspan=5 bgcolor=#ffcc66><span style='font-size:12px;font-weight:bold;'>Changes</span></td>
	                                    </tr>

                                        <?=next_changes_row?>

                                        </table>
                                        <br>";

        const string ChangesRowTemplate = @"<tr>
	                                            <td <?=bgcolor?>><?=menu?></td>
                                                <td <?=bgcolor?> ><font color='#<?=namecolor?>' ><?=who?></font></td>
	                                            <td <?=bgcolor?> ><?=action?></td>
	                                            <td <?=bgcolor?> ><?=date?></td>
	                                            <td <?=bgcolor?> ><?=note?></td>
                                            </tr>
                                                    
                                            <?=next_changes_row?>";

        const string ChangesDownloadRow = @"<tr>
	                                            <td <?=bgcolor?>></td>
                                                <td <?=bgcolor?> colspan=4><i id='<?=statusID?>'> <?=status?> </i></td>
                                            </tr>
                                                    
                                            <?=next_changes_row?>";

        const string IntegratedTemplate = @"<table cellspacing=0 cellpadding=2>
                                        <tr>
	                                        <td colspan=5 bgcolor=#99cc99><span style='font-size:12px;font-weight:bold;'>Integrated</span></td>
	                                    </tr>

                                        <?=next_integrated_row?>

                                        </table>
                                        <br>";

        const string IntegratedRowTemplate = @"<tr>
	                                            <td <?=bgcolor?>><?=menu?></td>
                                                <td <?=bgcolor?> ><font color='#<?=namecolor?>' ><?=who?></font></td>
	                                            <td <?=bgcolor?> ><?=action?></td>
	                                            <td <?=bgcolor?> ><?=date?></td>
	                                            <td <?=bgcolor?> ><?=note?></td>
                                            </tr>
                                                    
                                            <?=next_integrated_row?>";

        const string IntegratedDownloadRow = @"<tr>
	                                            <td <?=bgcolor?>></td>
                                                <td <?=bgcolor?> colspan=4><i id='<?=statusID?>'> <?=status?> </i></td>
                                            </tr>
                                                    
                                            <?=next_integrated_row?>";


        const string HistoryTemplate = @"<table cellspacing=0 cellpadding=2>
                                        <tr>
	                                        <td colspan=3 bgcolor=lightblue><span style='font-size:12px;font-weight:bold;'>History</span></td>
	                                        <td colspan=2 bgcolor=lightblue style='text-align:right;'><?=revs?> revisions kept</td>
                                        </tr>

                                        <?=next_history_row?>

                                        </table>
                                        <br>";

        const string HistoryRowTemplate = @"<tr>
	                                            <td <?=bgcolor?>><?=menu?></td>
	                                            <td <?=bgcolor?> ><span style='<?=textcolor?>'> <?=action?> </span></td>
	                                            <td <?=bgcolor?> ><span style='<?=textcolor?>'> <?=date?> </span></td>
	                                            <td <?=bgcolor?> ><span style='<?=textcolor?>'> <?=note?> </span></td>
	                                            <td <?=bgcolor?> span style='text-align:right;'> <?=edit?> </td>
                                            </tr>
                                            
                                    <?=next_history_row?>";

        const string HistoryDownloadRow = @"<tr>
	                                            <td <?=bgcolor?>></td>
                                                <td <?=bgcolor?> colspan=4><i id='<?=statusID?>'> <?=status?> </i></td>
                                            </tr>
                                            
                                    <?=next_history_row?>";


        const string MenuTemplate = @"<a href=''><img id=<?=menu_img_id?> border=0 src='<?=imgExtend1?>' onclick=""this.blur();return ToggleMenu(this, event,'<?=menu_id?>')""></a>
                                    <span id=<?=menu_id?> class=menustyle style='display:none;' onclick='stopB(event)'>
                                        <a href=# onclick='Reset()'><img border=0 src='<?=imgClose?>' width=12 height=12 alt='Close' align=right hspace=4 vspace=4></a>
                                        <table>
                                            <?=next_menu_row?>
                                        </table>
                                    </span>	";

        const string MenuRowTemplate = @"<a class=menulink href='<?=menu_link?>'><img border=0 src='<?=menu_img?>' class=menurow><?=menu_text?></a><br>
                                        <?=next_menu_row?>";


        string ResPath;

        string ImgUnlocked;
        string ImgLocked;
        string ImgEdit;
        string ImgOpen;
        string ImgDiff;
        string ImgDownload;
        string ImgDownloadCancel;

        string ImgAccept;
        string ImgClose;
        string ImgExtend1;
        string ImgExtend2;
        string ImgReject;
        string ImgReplace;
        string ImgAdd;

        StringBuilder Html = new StringBuilder(4096 * 4);
        StringBuilder ResetLine = new StringBuilder(20 * 2 * 60); // 10 history , 10 changes


        internal InfoPanel()
        {
            InitializeComponent();

            InfoDisplay.ObjectForScripting = this;

       
        }

        internal void Init(StorageView parent)
        {
            ParentView = parent;
            Storages = parent.Storages;

            ResPath = Storages.StoragePath + Path.DirectorySeparatorChar + "2";
            Directory.CreateDirectory(ResPath);

            ImgLocked   = ExtractImage("Locked");
            ImgUnlocked = ExtractImage("Unlocked");
            ImgEdit     = ExtractImage("Edit");
            ImgOpen     = ExtractImage("OpenFile");
            ImgDiff     = ExtractImage("Diff");
            ImgDownload = ExtractImage("Download");
            ImgDownloadCancel = ExtractImage("DownloadCancel");

            ImgAccept   = ExtractImage("Accept");
            ImgClose    = ExtractImage("Close");
            ImgExtend1  = ExtractImage("Extend1");
            ImgExtend2  = ExtractImage("Extend2");
            ImgReject   = ExtractImage("Reject");
            ImgReplace  = ExtractImage("Replace");
            ImgAdd      = ExtractImage("Add");
        }

        private string ExtractImage(string filename)
        {
            if (!File.Exists(ResPath + Path.DirectorySeparatorChar + filename + ".gif"))
            {
                Bitmap image = (Bitmap)StorageRes.ResourceManager.GetObject(filename);
                FileStream stream = new FileStream(ResPath + Path.DirectorySeparatorChar + filename + ".gif", FileMode.CreateNew, FileAccess.Write);
                image.Save(stream, System.Drawing.Imaging.ImageFormat.Gif);
                stream.Close();
            }

            string path = "file:///" + ResPath + "/" + filename + ".gif";

            return path.Replace(Path.DirectorySeparatorChar, '/');
        }
        
        void SetDisplay(string html)
        {
            Debug.Assert(!html.Contains("<?"));

            if (!DisplayActivated)
                return;

            // watch transfers runs per second, dont update unless we need to 
            if (html.CompareTo(InfoDisplay.DocumentText) == 0)
                return;
            
            // prevents clicking sound when browser navigates
            InfoDisplay.Hide();
            InfoDisplay.DocumentText = html;
            InfoDisplay.Show();
        }

        internal void ShowDots()
        {
            CurrentFolder = null;
            CurrentFile = null;
            CurrentItem = null;
            CurrentPath = "..";

            WatchTransfers = false;
            DiffsView = false;

            Html.Length = 0;
            Html.Append(DefaultPage);

            Html.Replace("<?=note?>", ".. (Parent Folder)");

            SetDisplay(Html.ToString());
        }

        internal void ShowDiffs()
        {
            CurrentFolder = null;
            CurrentFile = null;
            CurrentItem = null;
            CurrentPath = "";

            WatchTransfers = false;
            DiffsView = true;

            Html.Length = 0;
            Html.Append(DifferencePage);

            if (ParentView.CurrentDiffs.Count > 0)
            {
                Html.Replace("<?=table?>", DifferenceTable);
                GetDifferenceRows(Html);
            }
            else
                Html.Replace("<?=table?>", "");

            SetDisplay(Html.ToString());

            // explain concept of common storage system
            //      auto integration from above
            //      manual integration from below
            // explain locking file / folder differences
            //      temp files, deletion
            //      archived files
            // explain text colors - grey, black, bold, red

        }

        internal void ShowItem(FolderNode folder, FileItem file)
        {
            if (folder.Details.Name.CompareTo("..") == 0)
            {
                ShowDots();
                return;
            }

            CurrentFolder = folder;
            CurrentFile = file;
            
            Local = (ParentView.Working != null);

            if (CurrentFile != null)
            {
                IsFile = true;
                CurrentItem = file.Details;
                CurrentPath = file.GetPath();
                CurrentChanges = file.GetRealChanges();
                CurrentIntegrated = file.Integrated;
            }
            else
            {
                IsFile = false;
                CurrentItem = folder.Details;
                CurrentPath = folder.GetPath();
                CurrentChanges = folder.GetRealChanges();
                CurrentIntegrated = folder.Integrated;
            }

            Reset.Clear();
            StatusList.Clear();
            WatchTransfers = false;
            DiffsView = false;

            bool unlocked = CurrentItem.IsFlagged(StorageFlags.Unlocked) ||
                            (IsFile && Storages.IsHistoryUnlocked(ParentView.DhtID, ParentView.ProjectID, CurrentFolder.GetPath(), CurrentFile.Archived));
            bool archived = CurrentItem.IsFlagged(StorageFlags.Archived);
            bool temp = IsFile ? file.Temp : folder.Temp;

            Html.Length = 0;
            Html.Append(Template);

            /*
            <?=reset?>
            <?=image?>
            <?=name?> 
            <?=note?>
            <?=revs?>
            <?=changes?>
            <?=intergrated?>
            <?=history?>
            */

            
            if (!temp || CurrentChanges.Count > 0)
            {
                if(unlocked)
                    Html.Replace("<?=image?>", "<a href='http://main.lock_complete'><img border= 0 src='" + ImgUnlocked + "'></a>");
                else
                    Html.Replace("<?=image?>", "<a href='http://main.unlock'><img border= 0 src='" + ImgLocked + "'></a>");
            }
            else
                Html.Replace("<?=image?>", "");


            string displayName = CurrentItem.Name;
            if (IsFile)
                displayName = "<a class=menulink href='http://main.open'>" + displayName + "</a>";
            else
                displayName += " Folder";

            if (unlocked)
                Html.Replace("<?=name?>", "<b>" + displayName + "</b>");
            else
                Html.Replace("<?=name?>", displayName);


            if (temp && CurrentChanges.Count > 0)
                Html.Replace("<?=note?>", "Created - File exists in this spot for others, see changes");
            else if(temp)
                Html.Replace("<?=note?>", "Temporary - Will be deleted when containing folder is locked");
            else if (archived)
                Html.Replace("<?=note?>", "Ghosted - A file that used to exist here, either moved or deleted");
            else if (CurrentItem.IsFlagged(StorageFlags.Unlocked)) // if local unlocked
                Html.Replace("<?=note?>", "Unlocked to <a href='http://main.openfolder'>here</a>");
            else
                Html.Replace("<?=note?>", "");


            // changes
            if (CurrentChanges.Count > 0)
            {
                Html.Replace("<?=changes?>", ChangesTemplate);
                GetChangesRows(Html);
            }
            else
                Html.Replace("<?=changes?>", "");



            if (temp || CurrentItem.UID == 0) // root folder
            {
                Html.Replace("<?=integrated?>", "");
                Html.Replace("<?=history?>", "");

                FinishWriting(Html);
                
                SetDisplay(Html.ToString());
                return;
            }

            
            // integrated
            if (CurrentIntegrated.Count > 0)
            {
                Html.Replace("<?=integrated?>", IntegratedTemplate);
                GetIntegratedRows(Html);
            }
            else
                Html.Replace("<?=integrated?>", "");


            // history
            Html.Replace("<?=history?>", HistoryTemplate);


            if (CurrentItem.Revs == 0)
                Html.Replace("<?=revs?>", Local ? "<a href='http://main.revs'>All</a> " : "All ");
            else
            {
                if (Local)
                    Html.Replace("<?=revs?>", "<a href='http://main.revs'>Last " + CurrentItem.Revs.ToString() + "</a> ");
                else
                    Html.Replace("<?=revs?>", "Last " + CurrentItem.Revs.ToString() + " ");
            }

            GetHistoryRows(Html);

            FinishWriting(Html);

            SetDisplay(Html.ToString());

            if (WatchTransfers)
                UpdateItemView();
        }

   
        private void FinishWriting(StringBuilder html)
        {
            // reset
            ResetLine.Length = 0;

            foreach (string line in Reset)
                ResetLine.Append(line);

            html.Replace("<?=reset?>", ResetLine.ToString());

            // images
            html.Replace("<?=imgExtend1?>", ImgExtend1);
            html.Replace("<?=imgExtend2?>", ImgExtend2);
            html.Replace("<?=imgClose?>", ImgClose);
        }

        private void GetChangesRows(StringBuilder html)
        {
            /*
            <?=bgcolor?>
            <?=icons?>
            <?=who?>
            <?=action?>
            <?=date?> 
            <?=note?> 
             <?=next_changes_row?>*/

            /*
              menu
              menu_img_id
              menu_id
              next_menu_row
             
              menu_link
              menu_img
              menu_text
            */

            StorageItem original = null;
            if (IsFile && !CurrentFile.Temp)
                original = CurrentFile.Details;
            if (!IsFile && !CurrentFolder.Temp)
                original = CurrentFolder.Details;

            int i = 1;


            foreach (ChangeRow row in SortChanges(CurrentChanges))
            {
                html.Replace("<?=next_changes_row?>", ChangesRowTemplate);

                string color = (i % 2 == 0) ? "bgcolor=#ebebeb" : "";
                html.Replace("<?=bgcolor?>", color);

                color = row.Higher ? "ff0000" : "0000ff";
                html.Replace("<?=namecolor?>", color);

                string id = "c" + i.ToString();
                AddReset(id);

                // if file exists
                if (IsFile)
                {
                    StorageFile file = (StorageFile)row.Item;

                    string unlocked = "";
                    if (Storages.IsFileUnlocked(row.ID, ParentView.ProjectID, CurrentFolder.GetPath(), file, false))
                        unlocked = "<a href='http://change.lock." + row.ID.ToString() + "'><img border= 0 src='" + ImgUnlocked + "'></a>";


                    html.Replace("<?=menu?>", MenuTemplate + unlocked);
                    html.Replace("<?=menu_id?>", id);
                    html.Replace("<?=menu_img_id?>", id + "img");

                    if (Storages.FileExists(file))
                    {
                        html.Replace("<?=next_menu_row?>", GetMenuRow("change.open." + row.ID.ToString(), "Open", ImgOpen));
                        html.Replace("<?=next_menu_row?>", GetMenuRow("change.diff." + row.ID.ToString(), "Diff", ImgDiff));

                        if (!CurrentFile.Temp && ParentView.IsLocal)
                        {
                            // if hash the same then this is a rename/restore operation, accepting change replaces entry
                            if (Utilities.MemCompare(file.InternalHash, ((StorageFile)CurrentFile.Details).InternalHash))
                                html.Replace("<?=next_menu_row?>", GetMenuRow("change.replace." + row.ID.ToString(), "Accept", ImgAccept));

                            // otherwise we need to determine if this file's data is now integrated into something else
                            else
                            {
                                html.Replace("<?=next_menu_row?>", GetMenuRow("change.replace." + row.ID.ToString(), "Replace", ImgReplace));
                                html.Replace("<?=next_menu_row?>", GetMenuRow("change.accept." + row.ID.ToString(), "Integrated", ImgAccept));
                            }
                        }
                        else
                            html.Replace("<?=next_menu_row?>", GetMenuRow("change.add." + row.ID.ToString(), "Add", ImgAdd));
                    }
                    else
                    {
                        string status = Storages.FileStatus(file);

                        if (status == null)
                            html.Replace("<?=next_menu_row?>", GetMenuRow("change.download." + row.ID.ToString(), "Download", ImgDownload));
                        else
                        {
                            html.Replace("<?=next_menu_row?>", GetMenuRow("change.dlcancel." + row.ID.ToString(), "Cancel", ImgDownloadCancel));

                            html.Replace("<?=next_changes_row?>", ChangesDownloadRow);
                            html.Replace("<?=bgcolor?>", color);
                            html.Replace("<?=statusID?>", "status" + StatusList.Count.ToString());
                            html.Replace("<?=status?>", status);
                            StatusList.Add(file);

                            WatchTransfers = true;
                        }
                    }

                    html.Replace("<?=next_menu_row?>", "");
                }

                // folder
                else
                {
                    html.Replace("<?=menu?>", MenuTemplate);
                    html.Replace("<?=menu_id?>", id);
                    html.Replace("<?=menu_img_id?>", id + "img");

                    //html.Replace("<?=menu?>", "");

                    if (!CurrentFolder.Temp && ParentView.IsLocal)
                        html.Replace("<?=next_menu_row?>", GetMenuRow("change.replace." + row.ID.ToString(), "Accept", ImgAccept));
                    else
                        html.Replace("<?=next_menu_row?>", GetMenuRow("change.add." + row.ID.ToString(), "Add", ImgAdd));

                    html.Replace("<?=next_menu_row?>", "");
                }

                html.Replace("<?=who?>", row.Name);
                html.Replace("<?=action?>", Storages.ItemDiff(row.Item, original).ToString());
                html.Replace("<?=date?>", row.Item.Date.ToLocalTime().ToString());
                html.Replace("<?=note?>", row.Item.Note != null ? row.Item.Note : "");

                i++;
            }

            html.Replace("<?=next_changes_row?>", "");
        }

        internal List<ChangeRow> SortChanges(Dictionary<ulong, StorageItem> changes)
        {
            List<ChangeRow> highers = new List<ChangeRow>();
            List<ChangeRow> lowers = new List<ChangeRow>();

            foreach (KeyValuePair<ulong, StorageItem> item in changes)
            {
                string name = ParentView.Core.Links.GetName(item.Key);

                if (ParentView.HigherIDs.Contains(item.Key))
                    highers.Add(new ChangeRow(item.Key, name, item.Value, true));
                else
                    lowers.Add(new ChangeRow(item.Key, name, item.Value, false));
            }

            highers.Sort();
            lowers.Sort();

            highers.AddRange(lowers);

            return highers;
        }

        private void GetDifferenceRows(StringBuilder html)
        {
            int i = 1;

            // current diffs already sorted higher/lower
            foreach (ulong id in ParentView.CurrentDiffs)
            {
                html.Replace("<?=next_difference_row?>", DifferenceRowTemplate);

                string color = (i % 2 == 0) ? "bgcolor=#ebebeb" : "";
                html.Replace("<?=bgcolor?>", color);

                color = ParentView.HigherIDs.Contains(id) ? "ff0000" : "0000ff";
                html.Replace("<?=namecolor?>", color);

                html.Replace("<?=name?>", ParentView.Core.Links.GetName(id));
                html.Replace("<?=statusID?>", "status" + id.ToString());
                html.Replace("<?=status?>", GetDiffStatus(id));

                i++;
            }

            html.Replace("<?=next_difference_row?>", "");
        }


        private void GetIntegratedRows(StringBuilder html)
        {
            StorageItem original = null;
            if (IsFile && !CurrentFile.Temp)
                original = CurrentFile.Details;
            if (!IsFile && !CurrentFolder.Temp)
                original = CurrentFolder.Details;

            int i = 1;


            foreach (ChangeRow row in SortChanges(CurrentIntegrated))
            {
                html.Replace("<?=next_integrated_row?>", IntegratedRowTemplate);

                string color = (i % 2 == 0) ? "bgcolor=#ebebeb" : "";
                html.Replace("<?=bgcolor?>", color);

                color = row.Higher ? "ff0000" : "0000ff";
                html.Replace("<?=namecolor?>", color);

                // if file exists
                if (IsFile)
                {
                    StorageFile file = (StorageFile)row.Item;

                    string unlocked = "";
                    if (Storages.IsFileUnlocked(row.ID, ParentView.ProjectID, CurrentFolder.GetPath(), file, true)) // integrated always a history file, avoid conflict with change file from same host
                        unlocked = "<a href='http://integrate.lock." + row.ID.ToString() + "'><img border= 0 src='" + ImgUnlocked + "'></a>";

                    string id = "i" + i.ToString();
                    html.Replace("<?=menu?>", MenuTemplate + unlocked);
                    html.Replace("<?=menu_id?>", id);
                    html.Replace("<?=menu_img_id?>", id + "img");

                    AddReset(id);

                    if (Storages.FileExists(file))
                    {
                        html.Replace("<?=next_menu_row?>", GetMenuRow("integrate.open." + row.ID.ToString(), "Open", ImgOpen));
                        html.Replace("<?=next_menu_row?>", GetMenuRow("integrate.diff." + row.ID.ToString(), "Diff", ImgDiff));

                        html.Replace("<?=next_menu_row?>", GetMenuRow("integrate.replace." + row.ID.ToString(), "Replace", ImgReplace));
                        html.Replace("<?=next_menu_row?>", GetMenuRow("integrate.reject." + row.ID.ToString(), "Not Integrated", ImgReject));
                    }
                    else
                    {
                        string status = Storages.FileStatus(file);

                        if (status == null)
                            html.Replace("<?=next_menu_row?>", GetMenuRow("integrate.download." + row.ID.ToString(), "Download", ImgDownload));
                        else
                        {
                            html.Replace("<?=next_menu_row?>", GetMenuRow("integrate.dlcancel." + row.ID.ToString(), "Cancel", ImgDownloadCancel));

                            html.Replace("<?=next_integrated_row?>", IntegratedDownloadRow);
                            html.Replace("<?=bgcolor?>", color);
                            html.Replace("<?=statusID?>", "status" + StatusList.Count.ToString());
                            html.Replace("<?=status?>", status);

                            StatusList.Add(file);

                            WatchTransfers = true;
                        }
                    }

                    html.Replace("<?=next_menu_row?>", "");
                }
                else
                    html.Replace("<?=menu?>", "");


                html.Replace("<?=who?>", row.Name);
                html.Replace("<?=action?>", Storages.ItemDiff(row.Item, original).ToString());
                html.Replace("<?=date?>", row.Item.Date.ToLocalTime().ToString());
                html.Replace("<?=note?>", row.Item.Note != null ? row.Item.Note : "");

                i++;
            }

            html.Replace("<?=next_integrated_row?>", "");
        }



        private void GetHistoryRows(StringBuilder html)
        {
            /*
            <?=bgcolor?>
            <?=icons?>
            <?=textcolor?>
            <?=action?>
            <?=date?> 
            <?=note?> 
            <?=edit?> 
             <?=next_history_row?>*/

            /*
              menu
              menu_img_id
              menu_id
              next_menu_row
             
              menu_link
              menu_img
              menu_text
            */

            History.Clear();

            ThreadedLinkedList<StorageItem> archived = IsFile ? CurrentFile.Archived : CurrentFolder.Archived;

            archived.LockReading(delegate()
            {
                IEnumerator<StorageItem> next = archived.GetEnumerator();
                next.MoveNext();

                int i = 1;

                foreach (StorageItem item in archived)
                {
                    StorageItem prev = next.MoveNext() ? next.Current : null;


                    History.Add(item);

                    html.Replace("<?=next_history_row?>", HistoryRowTemplate);

                    string color = (i % 2 == 0) ? "bgcolor=#ebebeb" : "";
                    html.Replace("<?=bgcolor?>", color);

                    // if file exists
                    if (IsFile)
                    {
                        StorageFile file = (StorageFile)item;

                        string unlocked = "";
                        if ((i == 1 && CurrentItem.IsFlagged(StorageFlags.Unlocked)) ||
                            Storages.IsFileUnlocked(ParentView.DhtID, ParentView.ProjectID, CurrentFolder.GetPath(), file, true))
                            unlocked = "<a href='http://history.lock." + i.ToString() + "'><img border= 0 src='" + ImgUnlocked + "'></a>";


                        string id = "h" + i.ToString();
                        html.Replace("<?=menu?>", MenuTemplate + unlocked);
                        html.Replace("<?=menu_id?>", id);
                        html.Replace("<?=menu_img_id?>", id + "img");

                        AddReset(id);

                        if (Storages.FileExists(file))
                        {
                            html.Replace("<?=next_menu_row?>", GetMenuRow("history.open." + i.ToString(), "Open", ImgOpen));
                            html.Replace("<?=next_menu_row?>", GetMenuRow("history.diff." + i.ToString(), "Diff", ImgDiff));

                            if (ParentView.IsLocal)
                                html.Replace("<?=next_menu_row?>", GetMenuRow("history.replace." + i.ToString(), "Replace", ImgReplace));
                        }
                        else
                        {
                            string status = Storages.FileStatus(file);

                            if (status == null)
                                html.Replace("<?=next_menu_row?>", GetMenuRow("history.download." + i.ToString(), "Download", ImgDownload));
                            else
                            {
                                html.Replace("<?=next_menu_row?>", GetMenuRow("history.dlcancel." + i.ToString(), "Cancel", ImgDownloadCancel));

                                html.Replace("<?=next_history_row?>", HistoryDownloadRow);
                                html.Replace("<?=bgcolor?>", color);
                                html.Replace("<?=statusID?>", "status" + StatusList.Count.ToString());
                                html.Replace("<?=status?>", status);
                                StatusList.Add(file);

                                WatchTransfers = true;
                            }
                        }

                        html.Replace("<?=next_menu_row?>", "");
                    }
                    else
                        html.Replace("<?=menu?>", "");

                    // if modified
                    if (item.IsFlagged(StorageFlags.Modified))
                        html.Replace("<?=textcolor?>", "color:darkred;");
                    else
                        html.Replace("<?=textcolor?>", "");

                    html.Replace("<?=action?>", Storages.ItemDiff(item, prev).ToString());
                    html.Replace("<?=date?>", item.Date.ToLocalTime().ToString());
                    html.Replace("<?=note?>", item.Note != null ? item.Note : "");

                    // if local
                    if (Local)
                        html.Replace("<?=edit?>", "<a href='http://history.edit." + i.ToString() + "'><img border= 0 src='" + ImgEdit + "'></a>");
                    else
                        html.Replace("<?=edit?>", "");

                    i++;
                }
            });

            html.Replace("<?=next_history_row?>", "");
        }

        private string GetMenuRow(string link, string text, string img)
        {
            return "<tr><td style='font-size:12px;'><a class=menulink href='http://" + link + "' onclick='Reset()'><img border=0 src='" + img + "' class=menurow> " + text + "</a></td></tr><?=next_menu_row?>";
        }

        private void AddReset(string id)
        {
            Reset.Add("document.getElementById('" + id + "').style.display = 'none';\n");

            Reset.Add("document.getElementById('" + id + "img').src = extend1;\n");
        }

        private void InfoDisplay_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            bool transferChange = false;

            string url = e.Url.OriginalString;

            if (url.StartsWith("about:blank"))
                return;

            url = url.Replace("http://", "");
            url = url.TrimEnd('/');

            string[] parts = url.Split(new char[] { '.' });


            StorageFile file = null;
            StorageFolder folder = null;
            bool history = false;
            ulong UserID = ParentView.DhtID;

            if (parts[0] == "cancel")
            {
                e.Cancel = true;
                return;
            }

            if (parts[0] == "main")
            {
                if (IsFile)
                    file = (StorageFile)CurrentFile.Details;

                if (parts[1] == "lock_complete")
                {
                    if (IsFile)
                    {
                        ParentView.LockFile(CurrentFile);
                    }
                    else
                    {
                        bool subs = false;

                        if (CurrentFolder.Nodes.Count > 0)
                            if (MessageBox.Show("Lock sub-folders as well?", "Lock", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                subs = true;

                        List<LockError> errors = new List<LockError>();

                        ParentView.LockFolder(CurrentFolder, subs, errors);

                        LockMessage.Alert(ParentView, errors);

                        ParentView.RefreshFileList();
                        RefreshItem();
                    }
                }

                if (parts[1] == "unlock")
                {
                    Cursor = Cursors.WaitCursor;
                    if (IsFile)
                    {
                        ParentView.UnlockFile(CurrentFile);

                        CurrentFile.UpdateInterface();
                        RefreshItem();
                    }
                    else
                    {
                        bool subs = false;

                        if (CurrentFolder.Nodes.Count > 0)
                            if (MessageBox.Show("Unlock sub-folders as well?", "Unlock", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                subs = true;

                        List<LockError> errors = new List<LockError>();

                        ParentView.UnlockFolder(CurrentFolder, subs, errors);

                        LockMessage.Alert(ParentView, errors);

                        ParentView.RefreshFileList();
                        RefreshItem();
                    }
                    Cursor = Cursors.Default;
                }

                if (parts[1] == "openfolder")
                {
                    Utilities.OpenFolder(Storages.GetRootPath(ParentView.DhtID, ParentView.ProjectID) + CurrentFolder.GetPath());
                }

                if (parts[1] == "revs")
                {
                    string defaultRevs = (CurrentItem.Revs == 0) ? "all" : CurrentItem.Revs.ToString();

                    GetTextDialog dialog = new GetTextDialog("Edit Revisions", "Enter number of revisions to save. To save everything type 'all'", defaultRevs);

                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        int newRevs = 0;

                        if (string.Compare(dialog.ResultBox.Text, "all", true) == 0)
                            newRevs = 0;
                        else
                        {
                            int tryRev;
                            int.TryParse(dialog.ResultBox.Text, out tryRev);

                            if (tryRev != 0)
                                newRevs = tryRev;
                        }

                        if (newRevs != CurrentItem.Revs)
                            try
                            {
                                ParentView.Working.SetRevs(CurrentPath, IsFile, (byte)newRevs);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                    }
                }
            }

            if (parts[0] == "change")
            {
                UserID = ulong.Parse(parts[2]);

                if (IsFile)
                    file = (StorageFile)CurrentChanges[UserID];
                else
                    folder = (StorageFolder)CurrentChanges[UserID];

                if (parts[1] == "add")
                {
                    if (IsFile)
                        ParentView.Working.TrackFile(CurrentFolder.GetPath(), file);
                    else
                        ParentView.Working.TrackFolder(CurrentFolder.GetPath(), folder);
                }

                if (parts[1] == "diff")
                {

                    DiffForm form = new DiffForm(this, UserID, "Changes", file, false);

                    form.ShowDialog(this);

                }

                if (parts[1] == "accept")
                {

                    if (IsFile)
                        ParentView.Working.IntegrateFile(CurrentFolder.GetPath(), UserID, file);

                    // integrate triggers file update, triggering icon / page change


                    // display changes
                    // if change does not equal local
                    // if change not present in integrated
                    // display

                    // display integrated
                    // if integrated does not equal local
                    // if integrated file is equal to latest file from id
                    // display

                    // reject
                    // remove from integrated list and refresh


                    // when looking for changes go through each integration value and see if diff != none
                }
            }

            if (parts[0] == "integrate")
            {
                UserID = ulong.Parse(parts[2]);

                file = (StorageFile)CurrentIntegrated[UserID];

                // integrated should open in remotes history folder, because if changes/integrated both visible they need 
                // to open in different places and changes is always top of the stack no matter what
                history = true;

                if (parts[1] == "diff")
                {
                    DiffForm form = new DiffForm(this, UserID, "Integrated Changes", file, false);

                    form.ShowDialog(this);
                }

                if (parts[1] == "reject")
                {
                    ParentView.Working.UnintegrateFile(CurrentFolder.GetPath(), UserID, file);
                }

            }

            if (parts[0] == "history")
            {
                int index = int.Parse(parts[2]) - 1;

                if (IsFile)
                    file = (StorageFile)History[index];

                if (index != 0)
                    history = true;

                if (parts[1] == "diff")
                {
                    string what = "Current";
                    if (history)
                        what = "History from " + file.Date.ToLocalTime().ToString();

                    DiffForm form = new DiffForm(this, ParentView.DhtID, what, file, history);

                    form.ShowDialog(this);
                }

                if (parts[1] == "edit")
                {
                    StorageItem item = History[index];

                    string defaultNote = item.Note == null ? "" : item.Note;

                    EditNotes form = new EditNotes();
                    defaultNote = defaultNote.Replace("<br>", "\r\n");
                    form.NotesBox.Text = defaultNote;

                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        string newNote = form.NotesBox.Text.Replace("\r\n", "<br>");

                        if (string.Compare(newNote, defaultNote) != 0)
                            try
                            {
                                ParentView.Working.SetNote(CurrentPath, item, IsFile, newNote);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                    }
                }
            }

            // generic - history / change

            if (parts[1] == "open")
            {
                List<LockError> errors = new List<LockError>();

                Cursor = Cursors.WaitCursor;
                string finalpath = Storages.UnlockFile(UserID, ParentView.ProjectID, CurrentFolder.GetPath(), file, history, errors);
                Cursor = Cursors.Default;

                if (finalpath != null && File.Exists(finalpath))
                    System.Diagnostics.Process.Start(finalpath);

                LockMessage.Alert(ParentView, errors);

                CurrentFile.UpdateInterface();
                RefreshItem();
            }

            if (parts[1] == "lock")
            {
                Storages.LockFile(UserID, ParentView.ProjectID, CurrentFolder.GetPath(), file, history);

                CurrentFile.UpdateInterface();
                RefreshItem();
            }

            if (parts[1] == "download")
            {
                Storages.DownloadFile(UserID, file);
                transferChange = true;
            }

            if (parts[1] == "dlcancel")
            {
                ParentView.Core.Transfers.CancelDownload(Storages.ServiceID, file.Hash, file.Size);
                transferChange = true;
            }

            if (parts[1] == "replace")
            {
                if (IsFile)
                {
                    StorageFile replacement = file.Clone();
                    replacement.Note = file.Note;

                    List<LockError> errors = new List<LockError>();
                    ParentView.Working.ReplaceFile(CurrentFile.GetPath(), replacement, errors);
                    LockMessage.Alert(ParentView, errors);
                }
                else
                {
                    StorageFolder replacement = folder.Clone();
                    replacement.Note = folder.Note;

                    ParentView.Working.ReplaceFolder(CurrentFolder.GetPath(), replacement, true);
                }
            }


            e.Cancel = true;

            if (transferChange)
            {
                WatchTransfers = true;
                RefreshItem();

                ParentView.WatchTransfers = true;
                ParentView.UpdateListItems();
            }
        }

        internal void SecondTimer()
        {
            UpdateItemView();
        }


        private void UpdateItemView()
        {
            if (!WatchTransfers)
                return;

            WatchTransfers = false;

            for (int i = 0; i < StatusList.Count; i++)
            {
                string status = Storages.FileStatus(StatusList[i]);

                if (status == null)
                {
                    RefreshItem();
                    return;
                }
                else
                {
                    WatchTransfers = true;

                    string tag = "status" + i.ToString();

                    if (InfoDisplay.DocumentText.Contains(tag))
                        InfoDisplay.Document.InvokeScript("UpdateStatus", new String[] { tag, status });
                }
            }
        }

        internal void UpdateDiffView(ulong id)
        {
            if (!DisplayActivated || !DiffsView)
                return;

            string tag = "status" + id.ToString();

            if (InfoDisplay.DocumentText.Contains(tag))
                InfoDisplay.Document.InvokeScript("UpdateStatus", new String[] { tag, GetDiffStatus(id) });
        }

        private string GetDiffStatus(ulong id)
        {
            string status = "Not Found";

            OpStorage storage = Storages.GetStorage(id);

            if (storage != null)
            {
                if (ParentView.FailedDiffs.Contains(id))
                    status = "Failed to Compare";

                else
                {
                    status = " at " + storage.Header.Date.ToLocalTime().ToString();

                    if (ParentView.ChangeCount.ContainsKey(id) && ParentView.ChangeCount[id] > 0)
                    {
                        int changes = ParentView.ChangeCount[id];

                        status += ", " + changes.ToString() + " Change";

                        if (changes > 1)
                            status += "s";
                    }
                }
            }

            return status;
        }

        internal void RefreshItem()
        {
            ShowItem(CurrentFolder, CurrentFile);
        }
    }

    internal class ChangeRow : IComparable<ChangeRow>
    {
        internal ulong ID;
        internal string Name;
        internal StorageItem Item;
        internal bool Higher;

        internal ChangeRow(ulong id, string name, StorageItem item, bool higher)
        {
            ID = id;
            Name = name;
            Item = item;
            Higher = higher;
        }

        public int CompareTo(ChangeRow other)
        {
            return Name.CompareTo(other.Name);
        }
    }
}
