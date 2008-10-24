using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RiseOp.Implementation.Protocol.Packets;


namespace RiseOp.Interface
{
    internal partial class LicenseForm : CustomIconForm
    {
        internal LicenseForm(FullLicense license)
        {
            InitializeComponent();

            /*
            John Marshall
            21 cadogan way
            nashua, nh 03062

            john@marshall.com
             
            #234234-2
            9/6/2008
             */


            string text = "This version of RiseOp is licensed for\r\n\r\n";

            text += license.Name + "\r\n";
            text += license.Address + "\r\n\r\n";

            text += license.Email + "\r\n\r\n";

            text += "ID #" + license.LicenseID + "-" + license.Index + "\r\n";
            text += license.Date.ToShortDateString();

            

            LicenseBox.Text = text;
            LicenseBox.Select(0, 0);
        }

        internal LicenseForm(LightLicense license)
        {
            InitializeComponent();

            /*
            John Marshall
            #234234-2
            9/6/2008
             */


            string text = "Licensed User \r\n\r\n";

            text += license.Name + "\r\n";
            text += "ID #" + license.LicenseID + "-" + license.Index + "\r\n";
            text += license.Date.ToShortDateString();

            LicenseBox.Text = text;
            LicenseBox.Select(0, 0);

            Height = 150;
        }
    }
}
