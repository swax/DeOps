using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation;


namespace RiseOp.Interface
{
    internal class CustomIconForm : Form
    {
        Identity Profile;


        internal CustomIconForm()
        {
            Icon = InterfaceRes.riseop;

            if (Application.RenderWithVisualStyles)
                BackColor = System.Drawing.Color.WhiteSmoke;
        }

        internal CustomIconForm(OpCore core)
        {
            Profile = core.Profile;


            // window icon
            Profile_IconUpdate();


            // dialog background color
            if (Application.RenderWithVisualStyles)
                BackColor = System.Drawing.Color.WhiteSmoke;


            // signup for icon updates
            core.Profile.GuiIconUpdate += new IconUpdateHandler(Profile_IconUpdate);
        }

        void Profile_IconUpdate()
        {
            Icon = Profile.GetOpIcon();
        }

        protected override void Dispose(bool disposing)
        {
            if(Profile != null)
                Profile.GuiIconUpdate -= new IconUpdateHandler(Profile_IconUpdate);

            base.Dispose(disposing);
        }
    }
}
