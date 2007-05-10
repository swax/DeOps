using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace DeOps
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public IntPtr iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    };

    class Win32
    {
        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0;    // 'Large icon
        public const uint SHGFI_SMALLICON = 0x1;    // 'Small icon
        const uint SHGFI_USEFILEATTRIBUTES = 0x10;

        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath,
                                    uint dwFileAttributes,
                                    ref SHFILEINFO psfi,
                                    uint cbSizeFileInfo,
                                    uint uFlags);

        [DllImport("User32.dll")]
        private static extern int
                DestroyIcon(System.IntPtr hIcon);

        public static Bitmap GetIcon(string ext)
        {
            // Icon.ExtractAssociatedIcon only works with embedded like an exe's icon

            Bitmap img = null;

            try
            {
                SHFILEINFO info = new SHFILEINFO();

                SHGetFileInfo(
                    ext,
                    0,
                    ref info,
                    (uint)Marshal.SizeOf(info),
                    SHGFI_ICON | SHGFI_USEFILEATTRIBUTES | SHGFI_SMALLICON);


                img = Icon.FromHandle(info.hIcon).ToBitmap();

                DestroyIcon(info.hIcon);
            }
            catch { }

            return img;
        }
    }
}
