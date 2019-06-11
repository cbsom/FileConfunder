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
        private static int _offset = Properties.Settings.Default.ByteOffset;
        private static string _runWith = Properties.Settings.Default.OpenWithPath;
        private static bool _silent;
        private const string HELP_TEXT = @"USAGE: fnc -path fileOrFolderPath -action actionName
    If ""-action"" is not suplied, the default action is ""run"".
    The ""-action"" argument value can be any one of the following values:
        ""run"" unconfund then run. When app exits, reconfunds the file.
        ""confund"" confund the given file 
        ""unconfund"" unconfund the given file 
        ""rununconfund"" unconfund then run. When app exits, does not reconfund the file.
        ""confundall"" confund all the files in the given folder. If -pattern is supplied, only those files matching the pattern will be processed.
        ""unconfundall"" unconfund all the files in the given folder. If -pattern is supplied, only those files matching the pattern will be processed.
        ""help"" show this help text
    OTHER OPTIONAL ARGUMENTS
        [-runwith *] for use with the action ""run"". If  supplied, after unconfundation, the file will be run with the given path. Otherwise the path set in the config file is used.
        [-pattern *] for use with the action ""confundall"" or ""unconfundall"". If supplied, only those files matching the pattern will be processed.
        [-buflen *] If supplied, the given number of bytes in the file will be ""confunded"". Otherwise the value set in the config file is used. NOTE: files need to be unconfunded with the same buffer length and offset values whith which they were originally confunded.
        [-offset *] If supplied, during ""confunding"" or ""unconfunding"" process, each byte will be offset by the given number. Otherwise the value set in the config file is used. NOTE: files need to be unconfunded with the same buffer length and offset values whith which they were originally confunded.
        [-silent] If supplied, the command window is hidden and automatically. Otherwise, the user needs to press enter to exit.";
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
                }
                else
                {
                    SpitOut("{0} is not a valid value for the -buflen argument", str);
                    return;
                }
            }
            if (args.Contains("-offset"))
            {
                string str = args[Array.IndexOf(args, "-offset") + 1];
                int offset;
                if (int.TryParse(str, out offset))
                {
                    _offset = offset;
                }
                else
                {
                    SpitOut("{0} is not a valid value for the -offset argument", str);
                    return;
                }
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
                SpitOut("{0} cannot be found", _path);
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
                if (realData[i] == Byte.MinValue || realData[i] == Byte.MaxValue)
                {
                    //So not to be too obvious, null and max-value bytes are not changed.
                    muckedData[i] = realData[i];
                }
                else if ((realData[i] + _offset) > Byte.MaxValue)
                {
                    muckedData[i] = (byte)(Byte.MaxValue - realData[i]);
                }
                else
                {
                    muckedData[i] = (byte)(realData[i] + _offset);
                }
            }
            return muckedData;
        }

        private static byte[] UngarbleData(byte[] muckedData)
        {
            byte[] origData = new byte[muckedData.Length];

            for (int i = 0; i < muckedData.Length; i++)
            {
                if (muckedData[i] == Byte.MinValue || muckedData[i] == Byte.MaxValue)
                {
                    //So not to be too obvious, null and max-value bytes are not changed.
                    origData[i] = muckedData[i];
                }
                else if ((muckedData[i] - _offset) < Byte.MinValue)
                {
                    origData[i] = (byte)(Byte.MaxValue - muckedData[i]);
                }
                else
                {
                    origData[i] = (byte)(muckedData[i] - _offset);
                }
            }
            return origData;
        }
    }
}
