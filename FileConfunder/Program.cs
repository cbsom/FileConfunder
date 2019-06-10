using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FileConfunder
{
    class Program
    {
        private const int BUFFER_LENGTH = 2048;
        private const int OFFSET = 8;
        private const string HELP_TEXT = @"USAGE: fc fileOrFolderPath [-action action]
    If ""-action"" is not suplied, the default action is ""run"".
    The optional ""action"" argument value can be one of the following:    
        ""run"" unconfund then run. When app exits, reconfunds the file.        
        ""confund"" confund the given file 
        ""unconfund"" unconfund the given file 
        ""rununconfund"" unconfund then run. When app exits, does not reconfund the file.
        ""confundall"" confund all the files in the given folder. If -pattern is supplied, only those files matching the pattern will be processed. 
        ""unconfundall"" unconfund all the files in the given folder. If -pattern is supplied, only those files matching the pattern will be processed.
        ""help"" show this help text
    OTHER OPTIONAL ARGUMENTS
        [-runwith *] for use with the action ""run"". If  supplied, after unconfundation, the file will be run with the given path. Otherwise the path set in the config file is used.   
        [-pattern *] for use with the action ""confundall"" or ""unconfundall"". If supplied, only those files matching the pattern will be processed.";

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine(HELP_TEXT);
                return;
            }
            string path = args[0],
                   action = "run",
                   pattern = "*",
                   runWith = Properties.Settings.Default.OpenWithPath;

            if (args.Contains("-action"))
            {
                action = args[Array.IndexOf(args, "-action") + 1];
            }
            if (args.Contains("-runwith"))
            {
                runWith = args[Array.IndexOf(args, "-runwith") + 1];
            }
            if (args.Contains("-pattern"))
            {
                pattern = args[Array.IndexOf(args, "-pattern") + 1];
            }

            if (action != "help" && ((action.EndsWith("all") && !Directory.Exists(path)) ||
                (!action.EndsWith("all") && !File.Exists(path))))
            {
                Console.WriteLine("{0} cannot be found", path);
                return;
            }

            switch (action)
            {
                case "confundall":
                    foreach (var f in Directory.GetFiles(path, pattern))
                    {
                        Console.WriteLine("{0} has {1} confunded", f,
                            ConfundFile(f)
                                ? "been successfully"
                                : "FAILED to be");
                    }
                    Console.WriteLine("{0} has been processed", path);
                    break;
                case "unconfundall":
                    foreach (var f in Directory.GetFiles(path, pattern))
                    {
                        Console.WriteLine("{0} has {1} unconfunded", f,
                            ConfundFile(f, true)
                                ? "been successfully"
                                : "FAILED to be");
                    }
                    Console.WriteLine("{0} has been processed", path);
                    break;
                case "confund":
                    Console.WriteLine("{0} has {1} confunded", path,
                        ConfundFile(path)
                            ? "been successfully"
                            : "FAILED to be");
                    break;
                case "unconfund":
                    Console.WriteLine("{0} has {1} unconfunded", path,
                        ConfundFile(path, true)
                            ? "been successfully"
                            : "FAILED to be");
                    break;
                case "run":
                case "rununconfund":
                    if (ConfundFile(path, true))
                    {
                        Console.WriteLine("{0} has been successfully unconfunded", path);
                        Console.WriteLine("Running {0} {1}", runWith, path);
                        using (var pr = Process.Start(runWith, path))
                        {
                            if (action == "run")
                            {
                                pr.WaitForExit();
                                Console.WriteLine("{0} has exited", Path.GetFileName(runWith));
                                Console.WriteLine("Starting to reconfund {0}", path);
                                Console.WriteLine("{0} has {1} re-confunded", path,
                                    ConfundFile(path) ? "been successfully" : "FAILED to be");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("{0} cannot be unconfunded", path);
                    }
                    break;
                default:
                    Console.WriteLine(HELP_TEXT);
                    break;
            }

            Console.WriteLine("...........................\n\nPress <ENTER> to exit...");
            Console.ReadLine();
        }

        private static bool ConfundFile(string path, bool unconfund = false)
        {
            bool success = false;
            FileInfo fi = new FileInfo(path);
            int bufferLength = (int)Math.Min(BUFFER_LENGTH, fi.Length);
            try
            {
                using (var file = fi.Open(FileMode.Open, FileAccess.ReadWrite))
                {
                    byte[] buf = new byte[bufferLength];
                    file.Read(buf, 0, bufferLength);
                    buf = unconfund
                        ? UngarbleData(buf)
                        : GarbleData(buf);
                    file.Position = 0;
                    file.Write(buf, 0, bufferLength);
                    file.Close();
                    success = true;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }

            return success;
        }

        private static byte[] GarbleData(byte[] realData)
        {
            byte[] muckedData = new byte[realData.Length];

            for (int i = 0; i < realData.Length; i++)
            {
                if ((realData[i] + OFFSET) > Byte.MaxValue)
                {
                    muckedData[i] = (byte)(Byte.MinValue + Byte.MaxValue - muckedData[i]);
                }
                else
                {
                    muckedData[i] = (byte)(realData[i] + OFFSET);
                }
            }
            return muckedData;
        }

        private static byte[] UngarbleData(byte[] muckedData)
        {
            byte[] origData = new byte[muckedData.Length];

            for (int i = 0; i < muckedData.Length; i++)
            {
                if ((muckedData[i] - OFFSET) < Byte.MinValue)
                {
                    origData[i] = (byte)(Byte.MaxValue - Byte.MinValue + muckedData[i]);
                }
                else
                {
                    origData[i] = (byte)(muckedData[i] - OFFSET);
                }
            }
            return origData;
        }
    }
}
