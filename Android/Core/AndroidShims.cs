using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

/* These are missing functions and classes from Mono's Android API, nothing missing prevents DeOps basic functionality
 * 
 */

namespace System.Drawing
{
    public class Icon
    {
        public Image ToBitmap()
        {
            return new Image();
        }

        public static Icon FromHandle(IntPtr p)
        {
            return new Icon();
        }
    }

    public class Image
    {
    }

    public class Bitmap
    {
        public static Bitmap FromStream(MemoryStream memoryStream)
        {
            return new Bitmap();
        }

        public IntPtr GetHicon()
        {
            return new IntPtr();
        }

        public void Save(MemoryStream mem, ImageFormat imageFormat)
        {
            
        }
    }
}

namespace System.Drawing.Imaging
{
    public class ImageFormat
    {
        public static ImageFormat Jpeg { get; set; }

        public static ImageFormat Png { get; set; }
    }
}

namespace System.IO
{
    public class FileSystemWatcher
    {
        public FileSystemWatcher(string path, string filter)
        {

        }

        public bool EnableRaisingEvents { get; set; }
        public bool IncludeSubdirectories { get; set; }

        public NotifyFilters NotifyFilter { get; set; }

        public event FileSystemEventHandler Changed;
        public event FileSystemEventHandler Created;
        public event FileSystemEventHandler Deleted;
        public event RenamedEventHandler Renamed;

        internal void Dispose()
        {

        }
    }

    public delegate void FileSystemEventHandler(object sender, FileSystemEventArgs e);
    public delegate void RenamedEventHandler(object sender, RenamedEventArgs e);

    [Flags]
    public enum NotifyFilters
    {
        FileName = 1,
        DirectoryName = 2,
        Attributes = 4,
        Size = 8,
        LastWrite = 16,
        LastAccess = 32,
        CreationTime = 64,
        Security = 256,
    }

    public class FileSystemEventArgs : EventArgs
    {
        public FileSystemEventArgs(WatcherChangeTypes changeType, string directory, string name)
        {

        }

        public WatcherChangeTypes ChangeType { get; set; }
        public string FullPath { get; set; }
        public string Name { get; set; }
    }

    public class RenamedEventArgs : FileSystemEventArgs
    {
        public RenamedEventArgs(WatcherChangeTypes changeType, string directory, string name, string oldName)
            : base(changeType, directory, name)
        {

        }

        public string OldFullPath { get; set; }
        public string OldName { get; set; }
    }

    public enum WatcherChangeTypes
    {
        Created = 1,
        Deleted = 2,
        Changed = 4,
        Renamed = 8,
        All = 15,
    }
}