using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UpdateOp
{
    public partial class MainForm : Form
    {
        string NewExe;

        public MainForm(string newExe)
        {
            InitializeComponent();

            if (Application.RenderWithVisualStyles)
                BackColor = System.Drawing.Color.WhiteSmoke;

            NewExe = newExe;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            ApplyUpdate();
        }

        void ApplyUpdate()
        {
            try
            {
                File.Copy(NewExe, "RiseOp.exe", true);
                File.Delete(NewExe);

                Process.Start("RiseOp.exe");

                Close();
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "Could not apply update. Ensure RiseOp is closed.\n" + ex.Message;
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void SecondTimer_Tick(object sender, EventArgs e)
        {
            ApplyUpdate();
        }


    }
}
