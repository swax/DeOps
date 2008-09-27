using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;
using RiseOp.Interface.Views;


namespace RiseOp.Interface
{
    internal partial class IMForm : CustomIconForm
    {
        OpCore Core;

        internal List<ExternalView> ExternalViews = new List<ExternalView>();



        internal IMForm(OpCore core)
            : base(core)
        {
            InitializeComponent();

            Core = core;

            TopStrip.Renderer = new ToolStripProfessionalRenderer(new OpusColorTable());

            Core.ShowExternal += new ShowExternalHandler(OnShowExternal);

            BuddyList.Init(Core.Buddies);
            SelectionInfo.Init(Core);
        }


        void OnShowExternal(ViewShell view)
        {
            ExternalView external = new ExternalView(this, ExternalViews, view);

            ExternalViews.Add(external);

            external.Show();
        }

        private void IMForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            while (ExternalViews.Count > 0)
                // safe close removes entry from external views
                if (!ExternalViews[0].SafeClose())
                {
                    e.Cancel = true;
                    return;
                }

            Core.ShowExternal -= new ShowExternalHandler(OnShowExternal);

            Core.GuiMain = null;

            if (LockForm)
            {
                LockForm = false;
                return;
            }

            if (Core.Sim == null)
                Core.Exit();
        }

        bool LockForm;

        private void MinButton_Click(object sender, EventArgs e)
        {
            LockForm = true;

            Close();

            Core.GuiTray = new TrayLock(Core, false, false);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            GetTextDialog add = new GetTextDialog("Add Buddy", "Enter someone's buddy link below", Core.Buddies.GetLink(Core.UserID));

            if (add.ShowDialog() == DialogResult.OK)
                Core.Buddies.AddBuddy(add.ResultBox.Text);
        }
    }
}
