using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Text;
using System.Threading;
using System.Windows.Forms;


using DeOps.Interface;


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

            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);

            try
            {
                var context = new AppContext(args);

                if (context.StartSuccess)
                    Application.Run(context);
            }
            catch(Exception ex)
            {
                CrashApp(ex);
            }
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            CrashApp(e.Exception);
        }

        private static void CrashApp(Exception ex)
        {
            // cascading errors, we want to catch the FIRST error, so if another one comes in, ignore it
            foreach (Form window in Application.OpenForms)
                if (window is ErrorReport)
                    return; // dont want to call app exit, causing first error form to closed

            // need to exit app asap
            // this signals timers (possibly looping crashing timers) to die
            // also signals running core threads to die
            Application.Exit();

            
            new System.Threading.Thread(() => Application.Run(new ErrorReport(ex))).Start();
        }
    }


    public class DeOpsMutex
    {
        private Mutex TheMutex;
        private IChannel DeOpsChannel;
        public bool First;


        public DeOpsMutex(AppContext context, string[] args)
        {
            try
            {
                string host = "DeOps_" + DeOpsContext.LocalSeqVersion.ToString();

                TheMutex = new Mutex(true, host, out First);

                string objectUri = "SingleInstanceProxy";
                string url = "ipc://" + host + "/" + objectUri;

               

                if (First)
                {
                    DeOpsChannel = new IpcServerChannel(host);

                    ChannelServices.RegisterChannel(DeOpsChannel, false);
                    RemotingConfiguration.RegisterWellKnownServiceType(typeof(IpcObject), objectUri, WellKnownObjectMode.Singleton);

                    IpcObject obj = new IpcObject();
                    obj.NewInstance += context.SecondInstanceStarted;

                    RemotingServices.Marshal(obj, objectUri);
                }

                else
                {
                    DeOpsChannel = new IpcClientChannel();
                    ChannelServices.RegisterChannel(DeOpsChannel, false);
                    
                    IpcObject obj = Activator.GetObject(typeof(IpcObject), url) as IpcObject;

                    obj.SignalNewInstance(args);
                }
            }
            catch { }
        }
    }


    public delegate void NewInstanceHandler(string[] args);


    public class IpcObject : MarshalByRefObject
    {
        public event NewInstanceHandler NewInstance;


        public void SignalNewInstance(string[] args)
        {
            NewInstance(args);
        }

        // Make sure the object exists "forever"
        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
