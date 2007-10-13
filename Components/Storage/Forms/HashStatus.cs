using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;



namespace DeOps.Components.Storage
{
    internal partial class HashStatus : Form
    {
        StorageControl Storages;

        internal HashStatus(StorageControl storages)
        {
            InitializeComponent();

            Storages = storages;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!Storages.HashingActive())
                Close();
        }
    }
}