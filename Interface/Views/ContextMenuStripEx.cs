using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;


namespace RiseOp.Interface.Views
{
    internal class ContextMenuStripEx : ContextMenuStrip
    {
        internal ContextMenuStripEx()
        {
            Renderer = new ToolStripProfessionalRenderer(new OpusColorTable());
        }
    }
}
