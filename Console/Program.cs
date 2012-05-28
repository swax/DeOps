using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using DeOps;
using DeOps.Implementation;
using System.IO;
using System.Threading;
using System.Security.AccessControl;


namespace DeOpsConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using (var app = new ConsoleApp())
                    app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("App Error: " + ex.Message);
            }
        }
    }

    class ConsoleApp : IDisposable
    {
        TextWriter LogFile;

        string HelpText =
@"list    - shows a numbered list of loadable profiles
load     - eg: load <profile#> <pass>
ident    - de-ops user id for loaded profiles
adddress - address url of ops to be used for bootstrapping
status   - status of loaded profiles
sleep    - suspends console for x seconds
exit     - exit deops
help     - show command list";

        DeOpsContext Context;

        string StartupPath;


        public void Dispose()
        {
            Context.Dispose();
            LogFile.Dispose();
        }

        public void Run()
        {
            StartupPath = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                LogFile = new StreamWriter(new FileStream("log.txt", FileMode.Create, FileAccess.Write, FileShare.Read));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating output file: " + ex.Message);
                return;
            }

            Context = new DeOpsContext(StartupPath, null);

            WriteOut("DeOps Alpha v" + DeOpsContext.CoreVersion.ToString());

            if (File.Exists("init.txt"))
            {
                using (var init = new StreamReader(File.OpenRead("init.txt")))
                {
                    string line = null;

                    do
                    {
                        line = init.ReadLine();
                        if (line == null)
                            break;

                        if (line.StartsWith("#"))
                            continue;

                        Console.WriteLine("> " + line);

                        if (!ProcessInput(line))
                            return;

                    } while (line != null);
                }
            }

            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine();

                // input is null if daemon so just loopity loop
                if (input == null)
                {
                    Thread.Sleep(5000);
                    continue;
                }

                if (!ProcessInput(input))
                    return;
            }
            
        }

        bool ProcessInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            try
            {
                LogFile.WriteLine("> " + input);
                LogFile.Flush();

                if (input.CompareNoCase("exit"))
                    return false;

                else if (input.CompareNoCase("list"))
                {
                    var profiles = GetProfiles(StartupPath);

                    for (int i = 0; i < profiles.Length; i++)
                        WriteOut("{0}. {1}", i + 1, Path.GetFileName(profiles[i]));

                    if (profiles.Length == 0)
                        WriteOut("No profiles in startup directory found");
                }

                else if (input.StartsWith("load", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = input.Split(' ');

                    var profiles = GetProfiles(StartupPath);

                    var userPath = profiles[int.Parse(parts[1]) - 1];
                    var pass = parts[2];

                    var core = Context.LoadCore(userPath, pass);

                    WriteOut("Loaded op: {0}, user: {1}", core.User.Settings.Operation, core.User.Settings.UserName);
                }

                else if (input.CompareNoCase("ident"))
                {
                    Context.Cores.SafeForEach(c =>
                        WriteOut(c.GetIdentity(c.UserID))
                    );
                }

                else if (input.CompareNoCase("address"))
                {
                    Context.Cores.SafeForEach(c =>
                        WriteOut(c.GetMyAddress())
                    );

                    if (Context.Lookup != null)
                        WriteOut(Context.Lookup.GetMyAddress());
                }

                else if (input.CompareNoCase("status"))
                {
                    Action<OpCore> showStatus = c =>
                    {
                        WriteOut("{0} - {1}:{2}:{3} - {4}",
                            (c.User != null) ? c.User.Settings.Operation : "Lookup",
                            c.LocalIP,
                            c.Network.TcpControl.ListenPort,
                            c.Network.UdpControl.ListenPort,
                            c.Network.Established ? "Connected" : "Connecting...");
                    };

                    if (Context.Lookup != null)
                        showStatus(Context.Lookup);

                    Context.Cores.SafeForEach(c => showStatus(c));
                }

                else if (input.StartsWith("sleep"))
                {
                    var parts = input.Split(' ');

                    Thread.Sleep(int.Parse(parts[1]) * 1000);
                }

                else if (input.CompareNoCase("help"))
                    WriteOut(HelpText);

                else
                    WriteOut("unknown command: " + input);
            }
            catch (Exception ex)
            {
                WriteOut("Exception: " + ex.Message);
            }

            return true;
        }

        void WriteOut(string format, params object[] args)
        {
            Console.WriteLine(format, args);

            LogFile.WriteLine(format, args);
            LogFile.Flush();
        }

        static string[] GetProfiles(string rootPath)
        {
            return Directory.GetDirectories(rootPath)
                            .SelectMany(d => Directory.GetFiles(d).Where(f => f.EndsWith(".dop")))
                            .ToArray();
        }
    }

    static class Extensions
    {
        public static bool CompareNoCase(this string a, string b)
        {
            return (string.Compare(a, b) == 0);
        }
    }
}
