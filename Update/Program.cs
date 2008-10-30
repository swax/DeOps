using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace UpdateOp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
#if DEBUG
            args = new string[1];
            args[0] = "RiseOp_1.0.1.exe";
#endif

            if (args.Length != 1)
                return;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(args[0]));  
        }
    }
}
