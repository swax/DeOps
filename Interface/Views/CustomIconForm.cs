using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DeOps.Implementation;


namespace DeOps.Interface
{
    public class CustomIconForm : Form
    {
        OpUser Profile;


        internal CustomIconForm()
        {
            Icon = InterfaceRes.deops;

            if (Application.RenderWithVisualStyles && !GuiUtils.IsRunningOnMono())
                BackColor = System.Drawing.Color.WhiteSmoke;
        }

        internal CustomIconForm(OpCore core)
        {
            Profile = core.User;


            // window icon
            Profile_IconUpdate();


            // dialog background color
            if (Application.RenderWithVisualStyles && !GuiUtils.IsRunningOnMono())
                BackColor = System.Drawing.Color.WhiteSmoke;


            // signup for icon updates
            core.User.GuiIconUpdate += Profile_IconUpdate;
        }

        void Profile_IconUpdate()
        {
            Icon = Profile.GetOpIcon();
        }

        protected override void Dispose(bool disposing)
        {
            if(Profile != null)
                Profile.GuiIconUpdate -= Profile_IconUpdate;

            base.Dispose(disposing);
        }
    }
}
