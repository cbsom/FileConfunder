using System;
using System.Diagnostics;
using System.IO;

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
                Console.WriteLine("fc fileOrFolderPath [-confund] [-confundall]");
                return;
            }
            string path = args[0];
            //Confund an entire directory
            if (args.Length > 1 && args[1].ToLower() == "-confundall")
            {
                if (!Directory.Exists(path))
                {
                    Console.WriteLine("{0} cannot be found", path);
                    return;
                }
                foreach (var f in Directory.GetFiles(path))
                {
                    if (confundFile(f))
                    {
                        Console.WriteLine("{0} has been successfully confunded", f);
                    }
                    else
                    {
                        Console.WriteLine("{0} has been FAILED to be confunded", f);
                    }   
                }
                Console.WriteLine("{0} has been processed", path);
            }
            else
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine("{0} cannot be found", path);
                    return;
                }

                //Confund a single file
                if (args.Length > 1 && args[1].ToLower() == "-confund")
                {
                    if (confundFile(path))
                    {
                        Console.WriteLine("{0} has been successfully confunded", path);
                    }
                    else
                    {
                        Console.WriteLine("{0} FAILED to be confunded", path);
                    }
                }
                //Unconfund a file and run it with the process at open-with-path
                else if (confundFile(path, true))
                {
                    Console.WriteLine("{0} has been successfully unconfunded", path);
                    string openWithPath = Properties.Settings.Default.OpenWithPath;
                    Console.WriteLine("Running {0} {1}", openWithPath, path);
                    using (var pr = Process.Start(openWithPath, path))
                    {
                        pr.WaitForExit();
                        Console.WriteLine("{0} has exited", Path.GetFileName(openWithPath));
                        Console.WriteLine("Starting to reconfund {0}", path);
                    }
                    if (confundFile(path))
                    {
                        Console.WriteLine("{0} has been successfully re-confunded", path);
                    }
                    else
                    {
                        Console.WriteLine("{0} has FAILED to be re-confunded", path);
                    }
                }
                else
                {
                    Console.WriteLine("{0} cannot be unconfunded", path);
                }
            }

            Console.WriteLine("...........................\n\nPress <ENTER> to exit...");
            Console.ReadLine();
        }

        private static bool confundFile(string path, bool unconfund = false)
        {
            bool success = false;
            try
            {
                using (var file = File.Open(path, FileMode.Open, FileAccess.ReadWrite))
                {
                    byte[] buf = new byte[BUFFER_LENGTH];
                    file.Read(buf, 0, BUFFER_LENGTH);
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
