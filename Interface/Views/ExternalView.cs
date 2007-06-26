using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace DeOps.Interface
{
    internal partial class ExternalView : Form
    {
        MainForm  Main;
        ViewShell Shell;


        internal ExternalView(MainForm main, ViewShell shell)
        {
            InitializeComponent();

            Main = main;

            Shell = shell;
            Shell.Dock = DockStyle.Fill;
            Shell.External = this;

            Controls.Add(Shell);

            Text = shell.GetTitle();
            Size = shell.GetDefaultSize();
        }

        private void ExternalWin_Load(object sender, EventArgs e)
        {
            Shell.Init();
        }

        internal bool SafeClose()
        {
            if (!Shell.Fin())
                return false;

  
            Close();
            Main.ExternalViews.Remove(this);
            Shell.Dispose();

            return true;
        }

        private void ExternalView_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Shell.Fin())
            {
                Main.ExternalViews.Remove(this);
                Shell.Dispose();
            }
            else
                e.Cancel = true;
        }
    }
}