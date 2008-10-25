using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

using RiseOp.Implementation;


namespace RiseOp.Interface
{
    internal partial class ExternalView : CustomIconForm
    {
        Form  Main;
        List<ExternalView> MainViews;
        internal ViewShell Shell;


        internal ExternalView(Form main, List<ExternalView> views, ViewShell shell)
        {
            InitializeComponent();

            Main = main;
            MainViews = views;

            Shell = shell;
            Shell.Dock = DockStyle.Fill;
            Shell.External = this;

            Controls.Add(Shell);

            Text = shell.GetTitle(false);
            Size = shell.GetDefaultSize();

            Icon icon = shell.GetIcon();

            if (icon != null)
                Icon = icon;
        }

        private void ExternalWin_Load(object sender, EventArgs e)
        {
            
        }

        internal bool SafeClose()
        {
            if (!Shell.Fin())
                return false;

  
            Close();
            MainViews.Remove(this);
            Shell.Dispose();

            return true;
        }

        private void ExternalView_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Shell.Fin())
            {
                MainViews.Remove(this);
                Shell.Dispose();
            }
            else
                e.Cancel = true;
        }
    }

    class HostsExternalViews : CustomIconForm
    {
        internal List<ExternalView> ExternalViews = new List<ExternalView>();

        internal HostsExternalViews() { }

        internal HostsExternalViews(OpCore core)
            : base(core)
        { }

        internal ExternalView FindViewType(Type x)
        {
            foreach (ExternalView view in ExternalViews)
                if (view.Shell.GetType() == x)
                    return view;

            return null;
        }

        internal bool ShowExistingView(Type x)
        {
            ExternalView view = FindViewType(x);

            if (view == null)
                return false;

            view.WindowState = FormWindowState.Normal;
            view.Activate();

            return true;
        }
    }
}