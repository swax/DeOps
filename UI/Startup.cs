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
        private IChannel IpcChannel;
        public bool First;


        public DeOpsMutex(AppContext context, string[] args)
        {
            try
            {
                string name = "DeOps" + Application.ProductVersion;

                TheMutex = new Mutex(true, name, out First);

                string objectName = "SingleInstanceProxy";
                string objectUri = "ipc://" + name + "/" + objectName;

                if (First)
                {
                    IpcChannel = new IpcServerChannel(name);
                    ChannelServices.RegisterChannel(IpcChannel, false);
                    RemotingConfiguration.RegisterWellKnownServiceType(typeof(IpcObject), objectName, WellKnownObjectMode.Singleton);

                    IpcObject obj = new IpcObject(new NewInstanceHandler(context.SecondInstanceStarted));

                    RemotingServices.Marshal(obj, objectName);
                }

                else
                {
                    IpcChannel = new IpcClientChannel();
                    ChannelServices.RegisterChannel(IpcChannel, false);

                    IpcObject obj = Activator.GetObject(typeof(IpcObject), objectUri) as IpcObject;

                    obj.SignalNewInstance(args);
                }
            }
            catch { }
        }
    }


    public delegate void NewInstanceHandler(string[] args);


    public class IpcObject : MarshalByRefObject
    {
        event NewInstanceHandler NewInstance;

        public IpcObject(NewInstanceHandler handler)
        {
            NewInstance += handler;
        }

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
