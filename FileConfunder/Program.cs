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

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine(@"USAGE: fc fileOrFolderPath 
    The default action is -run.
    OPTIONS
        [-run] unconfund then run the file with the application set in the config file. When app exits, reconfunds the file.
        [-confund] confund the given file 
        [-unconfund] unconfund the given file 
        [-confundall] confund all the files in the given folder. If -pattern is supplied, only those files matching the pattern will be processed. 
        [-unconfundall] unconfund all the files in the given folder. If -pattern is supplied, only those files matching the pattern will be processed.
        [-pattern *] for use with -confundall or -unconfundall. If -pattern is supplied, only those files matching the pattern will be processed.");
                return;
            }
            string path = args[0],
                   action = args.Length > 1
                       ? args[1].TrimStart(new char[] { '-' }).ToLower()
                       : "run",
                   pattern = "*";

            if (args.Contains("-pattern"))
            {
                pattern = args[Array.IndexOf(args, "-pattern") + 1];
            };

            if ((action.EndsWith("all") && !Directory.Exists(path)) ||
                (!action.EndsWith("all") && !File.Exists(path)))
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
                            confundFile(f) ? "been successfully" : "FAILED to be");
                    }
                    Console.WriteLine("{0} has been processed", path);
                    break;
                case "unconfundall":
                    foreach (var f in Directory.GetFiles(path, pattern))
                    {
                        Console.WriteLine("{0} has {1} unconfunded", f,
                            confundFile(f, true) ? "been successfully" : "FAILED to be");
                    }
                    Console.WriteLine("{0} has been processed", path);
                    break;
                case "confund":
                    Console.WriteLine("{0} has {1} confunded", path,
                        confundFile(path) ? "been successfully" : "FAILED to be");
                    break;
                case "unconfund":
                    Console.WriteLine("{0} has {1} unconfunded", path,
                        confundFile(path, true) ? "been successfully" : "FAILED to be");
                    break;
                case "run":
                    string openWithPath = Properties.Settings.Default.OpenWithPath;

                    if (confundFile(path, true))
                    {
                        Console.WriteLine("{0} has been successfully unconfunded", path);
                        Console.WriteLine("Running {0} {1}", openWithPath, path);
                        using (var pr = Process.Start(openWithPath, path))
                        {
                            pr.WaitForExit();
                            Console.WriteLine("{0} has exited", Path.GetFileName(openWithPath));
                            Console.WriteLine("Starting to reconfund {0}", path);
                        }
                        Console.WriteLine("{0} has {1} re-confunded", path,
                            confundFile(path) ? "been successfully" : "FAILED to be");
                    }
                    else
                    {
                        Console.WriteLine("{0} cannot be unconfunded", path);
                    }
                    break;
            }

            Console.WriteLine("...........................\n\nPress <ENTER> to exit...");
            Console.ReadLine();
        }

        private static bool confundFile(string path, bool unconfund = false)
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
                        ? ChangeDataBack(buf)
                        : ChangeData(buf);
                    file.Position = 0;
                    file.Write(buf, 0, buf.Length);
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

        private static byte[] ChangeData(byte[] origData)
        {
            byte[] changedData = new byte[origData.Length];

            for (int i = 0; i < origData.Length; i++)
            {
                if ((origData[i] + OFFSET) > Byte.MaxValue)
                {
                    changedData[i] = (byte)(Byte.MinValue + Byte.MaxValue - changedData[i]);
                }
                else
                {
                    changedData[i] = (byte)(origData[i] + OFFSET);
                }
            }
            return changedData;
        }

        private static byte[] ChangeDataBack(byte[] changedData)
        {
            byte[] origData = new byte[changedData.Length];

            for (int i = 0; i < changedData.Length; i++)
            {
                if ((changedData[i] - OFFSET) < Byte.MinValue)
                {
                    origData[i] = (byte)(Byte.MaxValue - Byte.MinValue + changedData[i]);
                }
                else
                {
                    origData[i] = (byte)(changedData[i] - OFFSET);
                }
            }
            return origData;
        }
    }
}
