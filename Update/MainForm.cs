using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace UpdateOp
{
    public partial class MainForm : Form
    {
        string NewExe;

        Thread Worker;
        bool KillThread;
        AutoResetEvent TryUpdate = new AutoResetEvent(false);

        public MainForm(string newExe)
        {
            InitializeComponent();

            if (Application.RenderWithVisualStyles)
                BackColor = System.Drawing.Color.WhiteSmoke;

            NewExe = newExe;

            Worker = new Thread(ApplyUpdate);
            Worker.Start();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
           TryUpdate.Set();
        }

        private void SecondTimer_Tick(object sender, EventArgs e)
        {
            TryUpdate.Set();
        }

        public void ApplyUpdate()
        {
            while (!KillThread)
            {
                TryUpdate.WaitOne();

                if (KillThread)
                    break;

                try
                {
                    string windir = Environment.GetEnvironmentVariable("windir");
                    string ngen = windir + "\\Microsoft.NET\\Framework\\v2.0.50727\\ngen.exe";


                    RunInGui(() => StatusLabel.Text = "Uninstalling...");
                    StartSilent(ngen, "uninstall RiseOp.exe");


                    RunInGui(() => StatusLabel.Text = "Installing...");
                    File.Copy(NewExe, "RiseOp.exe", true);
                    File.Delete(NewExe);


                    RunInGui(() => StatusLabel.Text = "Optimizing...");
                    StartSilent(ngen, "install RiseOp.exe /queue");

                    Process.Start("RiseOp.exe");

                    RunInGui(() => Close());
                }
                catch (Exception ex)
                {
                    RunInGui(() => StatusLabel.Text = "Could not apply update. Ensure RiseOp is closed.\n" + ex.Message);
                }
            }
        }

        private void StartSilent(string file, string args)
        {
            ProcessStartInfo info = new ProcessStartInfo(file, args);
            info.WindowStyle = ProcessWindowStyle.Hidden;

            Process.Start(info).WaitForExit();
        }

        void RunInGui(Action action)
        {
            BeginInvoke(action);
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            KillThread = true;

            if (Worker != null)
            {
                TryUpdate.Set();
                Worker.Join();
            }
        }


    }
}
