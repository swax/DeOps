using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Microsoft.Win32;

using DeOps.Interface;
using DeOps.Simulator;

namespace DeOps
{
    class Startup
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();

            try
            {
                if (args.Length > 0 && args[0] == "/sim")
                    Application.Run(new SimForm());
                else
                    Application.Run(new LoaderForm(args));
            }
            catch
            {
                // pop up report error interface
            }
        }

    }


}
