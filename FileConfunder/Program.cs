using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FileConfunder
{
    class Program
    {
        private static string _path = null;
        private static string _action = "run";
        private static string _pattern = "*";
        private static int _bufferLength = Properties.Settings.Default.BufferSize;
        private static string _key = Properties.Settings.Default.Key;
        private static string _runWith = Properties.Settings.Default.OpenWithPath;
        private static bool _silent;
        private const string HELP_TEXT = @"Encrypts a set number of bytes in the beginning of a file.
Use to prevent easy determination of the file type from the byte format and prevent running the file normally.
USAGE: fnc -path fileOrFolderPath -action actionName
The ""-action"" argument value can be any one of the following values:    
    ""confund"" confund the given file 
    ""unconfund"" unconfund the given file 
    ""run"" unconfund the given file then run the file with the app specified in the -runwith argument or in the config file. When the app exits, the file is reconfunded.
    ""rununconfund"" unconfund  the given file then run the file with the app specified in the -runwith argument or in the config file. When app exits, the file is not reconfunded.
    ""confundall"" confund all the files in the given folder. If -pattern is supplied, only those files matching the pattern will be processed.
    ""unconfundall"" unconfund all the files in the given folder. If -pattern is supplied, only those files matching the pattern will be processed.
    ""help"" show this help text
OTHER OPTIONAL ARGUMENTS
    [-runwith *] For use with the actions ""run"" or ""rununconfund"". If this argument is supplied, after the file has been unconfunded, the file will be run with the app specified in the value given for this -runwith argument. If this argument is not supplied the app at the path set in the config file is used.
    [-pattern *] For use with the action ""confundall"" or ""unconfundall"". If supplied, only those files matching the pattern will be processed.
    [-buflen *] The number of bytes in the file to be ""confunded"" by encryption. If this argument is not supplied the value set in the config file is used. NOTE: files need to be unconfunded with the same buffer length and key values with which they were originally confunded.
    [-key *] The AES 256 key used to encrypt the data during ""confunding"". If this argument is not set, the value set in the config file is used. NOTE: files need to be unconfunded with the same buffer length and key values with which they were originally confunded.
    [-silent] The command window is hidden and automatically. If this argument is not supplied, the user needs to press enter to exit.";
        static void Main(string[] args)
        {
            if (args.Length < 2 || !args.Contains("-path"))
            {
                Console.WriteLine(HELP_TEXT);
                return;
            }

            _path = args[Array.IndexOf(args, "-path") + 1];

            if (args.Contains("-action"))
            {
                _action = args[Array.IndexOf(args, "-action") + 1];
            }
            if (args.Contains("-runwith"))
            {
                _runWith = args[Array.IndexOf(args, "-runwith") + 1];
            }
            if (args.Contains("-pattern"))
            {
                _pattern = args[Array.IndexOf(args, "-pattern") + 1];
            }
            if (args.Contains("-buflen"))
            {
                string str = args[Array.IndexOf(args, "-buflen") + 1];
                int buflen;
                if (int.TryParse(str, out buflen))
                {
                    _bufferLength = buflen;
                    if(_bufferLength > FPE.Net.Constants.MAXLEN)
                    {
                        Console.Error.WriteLine("The max value for the buffer length is {0}\nThe recommended buffer length is 4096", 
                            FPE.Net.Constants.MAXLEN);
                        return;
                    }
                    else if (_bufferLength > 4096)
                    {
                        ConsoleColor prevCColor = ConsoleColor.White;
                        try
                        {
                            prevCColor = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Red;
                        }
                        catch { }
                        SpitOut("WARNING! Your buffer length has been set to {0}.\nEncryption time for buffer lengths more than 4096 bytes may be prohibitively long.",
                            _bufferLength);
                        try
                        {
                            Console.ForegroundColor = prevCColor;
                        }
                        catch { }
                    }
                }
                else
                {
                    Console.Error.WriteLine("{0} is not a valid value for the -buflen argument", str);
                    return;
                }
            }
            if (args.Contains("-key"))
            {
                _key = args[Array.IndexOf(args, "-offset") + 1];
            }

            if (args.Contains("-silent"))
            {
                _silent = true;
                try
                {
                    Console.SetBufferSize(1, 1);
                    Console.SetWindowSize(1, 1);
                    Console.SetWindowPosition(-1, -1);
                }
                catch { }
            }
            if (_action != "help" && ((_action.EndsWith("all") && !Directory.Exists(_path)) ||
                (!_action.EndsWith("all") && !File.Exists(_path))))
            {
                Console.Error.WriteLine("{0} cannot be found", _path);
                return;
            }

            switch (_action)
            {
                case "confundall":
                    foreach (var f in Directory.GetFiles(_path, _pattern))
                    {
                        SpitOut("{0} has {1} confunded", f,
                            ConfundFile(f)
                                ? "been successfully"
                                : "FAILED to be");
                    }
                    SpitOut("{0} has been processed", _path);
                    break;
                case "unconfundall":
                    foreach (var f in Directory.GetFiles(_path, _pattern))
                    {
                        SpitOut("{0} has {1} unconfunded", f,
                            ConfundFile(f, true)
                                ? "been successfully"
                                : "FAILED to be");
                    }
                    SpitOut("{0} has been processed", _path);
                    break;
                case "confund":
                    SpitOut("{0} has {1} confunded", _path,
                        ConfundFile(_path)
                            ? "been successfully"
                            : "FAILED to be");
                    break;
                case "unconfund":
                    SpitOut("{0} has {1} unconfunded", _path,
                        ConfundFile(_path, true)
                            ? "been successfully"
                            : "FAILED to be");
                    break;
                case "run":
                case "rununconfund":
                    if (ConfundFile(_path, true))
                    {
                        SpitOut("{0} has been successfully unconfunded", _path);
                        SpitOut("Running {0} {1}", _runWith, _path);
                        using (var pr = Process.Start(_runWith, _path))
                        {
                            if (_action == "run")
                            {
                                pr.WaitForExit();
                                SpitOut("{0} has exited", Path.GetFileName(_runWith));
                                SpitOut("Starting to reconfund {0}", _path);
                                SpitOut("{0} has {1} re-confunded", _path,
                                    ConfundFile(_path) ? "been successfully" : "FAILED to be");
                            }
                        }
                    }
                    else
                    {
                        SpitOut("{0} cannot be unconfunded", _path);
                    }
                    break;
                default:
                    SpitOut(HELP_TEXT);
                    break;
            }

            if (_silent)
            {
                return;
            }
            SpitOut("...........................\n\nPress <ENTER> to exit...");
            Console.ReadLine();
        }

        private static void SpitOut(string txt, params object[] par)
        {
            if (!_silent)
            {
                Console.WriteLine(txt, par);
            }
        }

        private static bool ConfundFile(string path, bool unconfund = false)
        {
            bool success = false;
            FileInfo fi = new FileInfo(path);
            int bufferLength = (int)Math.Min(_bufferLength, fi.Length);
            try
            {
                using (var file = fi.Open(FileMode.Open, FileAccess.ReadWrite))
                {
                    byte[] buf = new byte[bufferLength];
                    file.Read(buf, 0, bufferLength);
                    buf = EncryptData(buf, unconfund);
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

        private static byte[] EncryptData(byte[] data, bool ungarble = false)
        {
            var intArray = data.Select(b => (int)b).ToArray();
            byte[] aes = Convert.FromBase64String(_key);
            var ff1 = new FPE.Net.FF1(Byte.MaxValue + 1, _bufferLength);
            int[] enced = ungarble
                ? ff1.decrypt(aes, new byte[] { }, intArray)
                : ff1.encrypt(aes, new byte[] { }, intArray);
            return enced.Select(i => (byte)i).ToArray();
        }        
    }
}
