using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Windows.Forms;

using Microsoft.Win32;

using RiseOp.Implementation;
using RiseOp.Implementation.Dht;
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
        internal ThreadedList<OpCore> Cores = new ThreadedList<OpCore>();
        LoginForm Login;
        SimForm Simulator;

        internal SimInstance Sim;

        Timer SecondTimer = new Timer();

        internal IPAddress LocalIP;
        internal FirewallType Firewall = FirewallType.Blocked;




        internal RiseOpContext(string[] args)
        {
            //crit check if already running

            RegisterType();

            SecondTimer.Interval = 1000;
            SecondTimer.Tick += new EventHandler(SecondTimer_Tick);
            SecondTimer.Enabled = true ;

            ShowLogin(args);
        }

        internal RiseOpContext(SimInstance sim)
        {
            // starting up simulated context context->simulator->instances[]->context
            Sim = sim;
        }


        void RegisterType()
        {
            // try to register file type association
            try
            {
                RegistryKey type = Registry.ClassesRoot.CreateSubKey(".rop");
                type.SetValue("", "rop");

                RegistryKey root = Registry.ClassesRoot.CreateSubKey("rop");
                root.SetValue("", "RiseOp Identity");

                RegistryKey icon = root.CreateSubKey("DefaultIcon");
                icon.SetValue("", Application.ExecutablePath + ",0");

                RegistryKey shell = root.CreateSubKey("shell");
                RegistryKey open = shell.CreateSubKey("open");
                RegistryKey command = open.CreateSubKey("command");
                command.SetValue("", "\"" + Application.ExecutablePath + "\" \"%1\"");
            }
            catch
            {
                //UpdateLog("Exception", "LoginForm::RegisterType: " + ex.Message);
            }
        }

        internal void SecondTimer_Tick(object sender, EventArgs e)
        {
            if (Global != null)
                Global.SecondTimer();

            Cores.LockReading(delegate()
            {
                foreach (OpCore core in Cores)
                    core.SecondTimer();
            });
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
        }

        internal void ShowCore(OpCore core)
        {
            AddCore(core);

            core.GuiMain = new MainForm(core);
            core.GuiMain.Show();
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
        }

        void Window_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (sender == Login)
                Login = null;

            if (sender == Simulator)
                Simulator = null;

            CheckExit();
        }

        internal void AddCore(OpCore core)
        {
            Cores.SafeAdd(core);

            CheckGlobal();
        }

        void CheckGlobal()
        {
            // called from gui thread

            bool runGlobal = false;

            Cores.LockReading(delegate()
            {
                foreach (OpCore core in Cores)
                    if (core.User.Settings.OpAccess != AccessType.Secret)
                        runGlobal = true;

                // if public cores exist, sign into global
                if (runGlobal && Global == null)
                {
                    Global = new OpCore(this);
                }

                // else destroy global context
                if (!runGlobal && Global != null)
                {
                    Global.Exit();
                    Global = null;
                }
            });
        }

        internal void RemoveCore(OpCore removed)
        {
            if (removed == Global)
                return;

            Cores.LockWriting(delegate()
            {
                foreach (OpCore core in Cores)
                    if (core == removed)
                    {
                        Cores.Remove(core);
                        break;
                    }
            });

            CheckGlobal();
            CheckExit();
        }

        private void CheckExit()
        {
            // if context running inside a simulator dont exit thread
            if (Sim != null)
                return;

            if (Login == null && Cores.SafeCount == 0)
            {
                if (Global != null)
                    Global.Exit();

                if (Sim == null) // context not running inside a simulation
                {
                    if(Simulator == null) // simulation interface closed
                        ExitThread();
                }
                else
                    Sim.Internet.ExitInstance(Sim);
            }
        }

        internal void SetFirewallType(FirewallType type)
        {
            // check if already set
            if (Firewall == type)
                return;


            // if client previously blocked, cancel any current searches through proxy
            if (Global != null && Firewall == FirewallType.Blocked)
                lock (Global.Network.Searches.Active)
                    foreach (DhtSearch search in Global.Network.Searches.Active)
                        search.ProxyTcp = null;


            if (type == FirewallType.Open)
            {
                Firewall = FirewallType.Open; // do first, otherwise publish will fail

                if (Global != null)
                    Global.Network.FirewallChangedtoOpen();

                Cores.LockReading(delegate()
                {
                    foreach (OpCore core in Cores)
                        core.Network.FirewallChangedtoOpen();
                });

                return;
            }

            if (type == FirewallType.NAT && Firewall != FirewallType.Open)
            {
                Firewall = FirewallType.NAT;

                if (Global != null)
                    Global.Network.FirewallChangedtoNAT();

                Cores.LockReading(delegate()
                {
                    foreach (OpCore core in Cores)
                        core.Network.FirewallChangedtoNAT();
                });

                return;
            }

            if (type == FirewallType.Blocked)
            {
                // why is this being set (forced)
                //Debug.Assert(false);
            }
        }
    }

}
