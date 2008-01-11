using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace RiseOp.Interface
{
    internal partial class ExternalView : Form
    {
        MainForm  Main;
        internal ViewShell Shell;


        internal ExternalView(MainForm main, ViewShell shell)
        {
            InitializeComponent();

            Main = main;

            Shell = shell;
            Shell.Dock = DockStyle.Fill;
            Shell.External = this;

            Controls.Add(Shell);

            Text = shell.GetTitle(false);
            Size = shell.GetDefaultSize();
            Icon = shell.GetIcon();
        }

        private void ExternalWin_Load(object sender, EventArgs e)
        {
            
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