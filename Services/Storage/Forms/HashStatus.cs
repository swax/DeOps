using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;



namespace RiseOp.Services.Storage
{
    internal partial class HashStatus : RiseOp.Interface.CustomIconForm
    {
        StorageService Storages;

        internal HashStatus(StorageService storages)
        {
            InitializeComponent();

            Storages = storages;
        }

        private void QuarterSecondTimer_Tick(object sender, EventArgs e)
        {
            if (Storages.HashFiles.Pending.Count == 0)
                Close();

            // Securing test.mpg, 2 files left, 200 MB total

            HashPack pack = null;
            long totalSize = 0;

            lock (Storages.HashFiles.Pending)
            {
                pack = Storages.HashFiles.Pending.First.Value.Param2 as HashPack;

                foreach (HashPack packy in Storages.HashFiles.Pending.Select(p => p.Param2 as HashPack))
                    totalSize += packy.File.Info.Size;
            }

            if (pack == null)
                return;

            string status = "Securing '" + pack.File.Info.Name + "'\r\n";

            int filesLeft = Storages.HashFiles.Pending.Count - 1;

            if (filesLeft > 0)
                if (filesLeft == 1)
                    status += "1 File Left\r\n";
                else
                    status += filesLeft.ToString() + " Files Left\r\n";

            status += Utilities.ByteSizetoString(totalSize) + " Total";


            StatusLabel.Text = status;           
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            // hashing could hang if we have no access to the file

            // files with no hash value will not be saved to header file

            Close();
        }
    }
}