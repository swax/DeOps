using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using DeOps;
using DeOps.Implementation;
using System.IO;


namespace DeOpsConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            string help = 
@"list  - shows a numbered list of loadable profiles
load   - eg: load <profile#> <pass>
ident  - de-ops user id for loaded profiles
status - status of loaded profiles
exit   - exit deops
help   - show command list";

            string startupPath = AppDomain.CurrentDomain.BaseDirectory;

            using (var context = new DeOpsContext(startupPath, null))
            {
                Console.WriteLine("DeOps Alpha v" + context.LocalSeqVersion.ToString());

                while (true)
                {
                    try
                    {
                        Console.Write("> ");
                        string input = Console.ReadLine();

                        if (input.CompareNoCase("exit"))
                            break;

                        else if (input.CompareNoCase("list"))
                        {
                            var profiles = GetProfiles(startupPath);

                            for (int i = 0; i < profiles.Length; i++)
                                Console.WriteLine("{0}. {1}", i + 1, Path.GetFileName(profiles[i]));

                            if (profiles.Length == 0)
                                Console.WriteLine("No profiles in startup directory found");
                        }

                        else if (input.StartsWith("load", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = input.Split(' ');

                            var profiles = GetProfiles(startupPath);

                            var userPath = profiles[int.Parse(parts[1]) - 1];
                            var pass = parts[2];

                            var core = context.LoadCore(userPath, pass);

                            Console.Clear();
                            Console.WriteLine("Loaded op: {0}, user: {1}", core.User.Settings.Operation, core.User.Settings.UserName);
                        }

                        else if (input.CompareNoCase("ident"))
                        {
                            context.Cores.SafeForEach(c =>
                                Console.WriteLine(c.GetIdentity(c.UserID))
                            );
                        }

                        else if (input.CompareNoCase("status"))
                        {
                            Action<OpCore> showStatus = c => 
                                {
                                    Console.WriteLine("{0} - {1}:{2}:{3} - {4}", 
                                        (c.User != null) ? c.User.Settings.Operation : "Lookup", 
                                        c.LocalIP, 
                                        c.Network.TcpControl.ListenPort, 
                                        c.Network.UdpControl.ListenPort, 
                                        c.Network.Established ? "Connected" : "Connecting...");
                                };

                            if (context.Lookup != null)
                                showStatus(context.Lookup);

                            context.Cores.SafeForEach(c => showStatus(c));
                        }

                        else if (input.CompareNoCase("help"))
                            Console.WriteLine(help);

                        else
                            Console.WriteLine("unknown command: " + input);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception: " + ex.Message);
                    }
                }
            }
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
