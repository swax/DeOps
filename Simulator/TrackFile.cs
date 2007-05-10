using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DeOps.Simulator
{
    internal partial class TrackFile : Form
    {
        NetView View;

        internal TrackFile(NetView view)
        {
            InitializeComponent();

            View = view;
        }

        private void TrackFile_Load(object sender, EventArgs e)
        {
            if (View.TrackHash != null)
                HashBox.Text = Utilities.BytestoHex(View.TrackHash);

            UpdateTrackButton();
        }

        private void TrackButton_Click(object sender, EventArgs e)
        {
            View.TrackHash = Utilities.HextoBytes(HashBox.Text);

            UpdateTrackButton();
            
            View.OnUpdateView();
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            View.TrackHash = null;

            HashBox.Text = "";

            UpdateTrackButton();

            View.OnUpdateView();
        }

        private void HashBox_TextChanged(object sender, EventArgs e)
        {
            UpdateTrackButton();
        }

        void UpdateTrackButton()
        {
            byte[] newHash = Utilities.HextoBytes(HashBox.Text);

            if (Utilities.MemCompare(newHash, View.TrackHash))
                TrackButton.Text = "Tracking";
            else
                TrackButton.Text = "Track";
        }


    }
}