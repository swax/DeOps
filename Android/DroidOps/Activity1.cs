using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.IO;
using DeOps;
using DeOps.Implementation;


namespace DroidOps
{
    [Activity(Label = "DroidOps", MainLauncher = true)]
    public class Activity1 : Activity
    {
        DeOpsContext Context;
        OpCore Core;

        string OpName;
        string Username;
        string Password;
        string UserFile;
        string UserPath;

        TextView StatusLabel;
        TextView LogView;


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // UI init
            SetContentView(Resource.Layout.Main);

            StatusLabel = FindViewById<TextView>(Resource.Id.StatusTextView);
            LogView = FindViewById<TextView>(Resource.Id.LogTextView);

            var createUserButton = FindViewById<Button>(Resource.Id.CreateUserButton);
            var loginButton = FindViewById<Button>(Resource.Id.LoginButton);
            var logoffButton = FindViewById<Button>(Resource.Id.LogoffButton);

            createUserButton.Click += new EventHandler(CreateUserButton_Click);
            loginButton.Click += new EventHandler(LoginButton_Click);
            logoffButton.Click += new EventHandler(LogoffButton_Click);

            // context init
            Context = new DeOpsContext(CacheDir.Path, null);

            OpName = "Global IM";
            Username = "JennyKelly";
            Password = "xroxxxroxx";

            UserFile = OpName + " - " + Username;

            UserPath = CacheDir.Path + Path.DirectorySeparatorChar +
                       UserFile + Path.DirectorySeparatorChar +
                       UserFile + ".dop";
        }

        void CreateUserButton_Click(object sender, EventArgs e)
        {
            try
            {
                Directory.CreateDirectory(CacheDir.Path + Path.DirectorySeparatorChar + UserFile);

                if (File.Exists(UserPath))
                {
                    StatusLabel.Text = "User already created";
                    return;
                }

                StatusLabel.Text = "Creating user...";

                OpUser.CreateNew(UserPath, OpName, Username, Password, AccessType.Secret, null, true);

                StatusLabel.Text = "User created";
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "Create error: " + ex.Message;
            }
        }

        void LoginButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (Core != null)
                    return;

                StatusLabel.Text = "Loading core...";

                Core = Context.LoadCore(UserPath, Password);
                Core.RunInGui += Core_RunInGui;
                Core.UpdateConsole += Core_UpdateConsole;
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "Login error: " + ex.Message;
            }
        }

        void LogoffButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (Core == null)
                    return;

                Core.UpdateConsole -= Core_UpdateConsole;
                Core.Exit();
                Core = null;

                StatusLabel.Text = "Logged off";
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "Logoff error: " + ex.Message;
            }
        }

        public void Core_RunInGui(Delegate method, params object[] args)
        {
            if (method == null)
                return;

            RunOnUiThread(new Action(() => method.DynamicInvoke(args)));
        }

        void Core_UpdateConsole(string message)
        {
            LogView.Text = message + "\n" + LogView.Text;
        }
    }
}


