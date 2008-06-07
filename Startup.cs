using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Microsoft.Win32;

using RiseOp.Implementation;
using RiseOp.Interface;
using RiseOp.Simulator;

namespace RiseOp
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
                Application.Run(new RiseOpContext(args));
            }
            catch(Exception ex)
            {
                //crit pop up report error interface
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

    }

    internal class RiseOpContext : ApplicationContext
    {
        internal OpCore Global;
        internal List<OpCore> Cores = new List<OpCore>();
        LoginForm Login;
        SimForm Simulator;

        int References;
        internal SimInstance Sim;

        Timer SecondTimer = new Timer();


        internal RiseOpContext(SimInstance sim)
        {
            Sim = sim; // used by simulator
        }

        internal RiseOpContext(string[] args)
        {
            SecondTimer.Interval = 1000;
            SecondTimer.Tick += new EventHandler(SecondTimer_Tick);
            SecondTimer.Enabled = true ;

            ShowLogin(args);
        }

        internal void SecondTimer_Tick(object sender, EventArgs e)
        {
            foreach (OpCore core in Cores)
                core.SecondTimer();
        }

        internal void ShowLogin(string[] args)
        {
            if (Login != null)
            {
                Login.BringToFront();
                return;
            }

            Login = new LoginForm(this, args);
            Login.FormClosed += new FormClosedEventHandler(Window_FormClosed);
            Login.Show();

            References++;
        }

        internal void ShowCore(OpCore core)
        {
            Cores.Add(core);

            core.GuiMain = new MainForm(core);
            core.GuiMain.Show();

            References++;
        }

        internal void ShowSimulator()
        {
            if (Simulator != null)
            {
                Simulator.BringToFront();
                return;
            }

            Simulator = new SimForm();
            Simulator.FormClosed += new FormClosedEventHandler(Window_FormClosed);
            Simulator.Show();

            References++;
        }

        void Window_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (sender == Login)
                Login = null;

            if (sender == Simulator)
                Simulator = null;

            DeReference();
        }

        internal void CoreExited(OpCore terminated)
        {
            foreach (OpCore core in Cores)
                if (core == terminated)
                {
                    Cores.Remove(core);
                    DeReference();
                    break;
                }
        }

        private void DeReference()
        {
            References--;

            if (References == 0 && Sim != null)
                ExitThread();
        }
    }

}
