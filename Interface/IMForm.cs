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

        internal IMForm(OpCore core)
            : base(core)
        {
            InitializeComponent();

            Core = core;

            TopStrip.Renderer = new ToolStripProfessionalRenderer(new OpusColorTable());

            BuddyList.Init(Core.Buddies);
            SelectionInfo.Init(Core);
        }

        private void IMForm_FormClosing(object sender, FormClosingEventArgs e)
        {
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
            GetTextDialog add = new GetTextDialog("Add Buddy", "Enter someone's buddy link below", "riseop://");

            if (add.ShowDialog() == DialogResult.OK)
                Core.Buddies.AddBuddy(add.ResultBox.Text);
        }
    }
}
