using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;


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

            BuddyList.Init(Core.Buddies);
            SelectionInfo.Init(Core);
        }
    }
}
