using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;


namespace DeOps.Interface.Views
{
    public class ContextMenuStripEx : ContextMenuStrip
    {
        public ContextMenuStripEx()
        {
            GuiUtils.SetupToolstrip(this, new OpusColorTable());
        }
    }
}
