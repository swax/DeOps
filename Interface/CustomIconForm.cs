using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace RiseOp.Interface
{
    internal class CustomIconForm : Form
    {
        internal CustomIconForm()
        {
            Icon = InterfaceRes.riseop;
        }

        /*internal CustomIconForm(OpCore core)
        {
            // when core given as argument hook into the icon changed event
            // also means this needs to be disposed

            // default icon is in core.profile.settings.opIcon
            // or inherited
        }


        protected override void Dispose(bool disposing)
        {
            //crit check this doesnt override forms dispose
            // unhook event
            

            base.Dispose(disposing);
        }*/
    }
}
