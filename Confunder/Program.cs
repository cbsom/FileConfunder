/*******************************************************************
 * Confunder - File obfuscator and deobfuscator.
 * 
 * Licensed under the GNU General Public License v3.0.
 * 
 * Usage:
 *   confunder <path> <action> [options]
 * 
 * Actions:
 *   confund   - Obfuscate the file or files.
 *   unconfund - Deobfuscate the file or files.
 *   run       - Deobfuscate, run, then obfuscate again.
 *   setkey    - Set or generate the encryption key used for the obfuscation on this machine.
 *   changekey  - Change the encryption key used for the obfuscation on the supplied file/s.
 *   checkkey  - Check if the supplied key matches the stored key.
 *   help      - Show the help message.
 * 
 * Options:
 *   -key <key>     - Use the specified key (default: stored key).
 *   -silent        - Suppress all console output.
 *   -pattern <pattern> - Use a pattern to find files when the target is a folder.
 *   -newkey <newkey> - When changing or checking the key, the new text key to use. * 
 * Examples:
 *   confunder ./myfile.txt -action run
 *   confunder ./myfolder -action run -pattern "*.cs"
 * 
 *********************************************************************/

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Confunder
{
    internal static class Program
    {        
        private static string _key = null;
        private static string _newKey = null;
        private static string _path = null;
        private static bool _isFolder = false;
        private static string _action = null;
        private static string _pattern = "*";
        private static bool _silent = false;
        private const string HELP_TEXT = @"-----------------------------------------------------------------------------------
Confunder - ""Confunding"" is the process of obfuscating a file in order to
make it difficult to determine the file type from the byte format and
partially encrypt the file to prevent the file from being run or viewed normally. 
-----------------------------------------------------------------------------------
USAGE: 
    confunder path [-action] [-pattern] [-silent]

    path: The first argument is always the path to a file or folder. 
          If it is a folder, all files in the folder will be processed.
    ""-action"" which action to run. The action can be any one of the following values:
        * ""confund"" - confund the given file or all the files in a given folder. 
            If path is a folder and -pattern is supplied, only those files matching the pattern will be processed.
        * ""unconfund"" - unconfund the given file or all the files in a given folder. 
            If path is a folder and -pattern is supplied, only those files matching the pattern will be processed.
        * ""run"" - run a single file. If it is confunded, unconfunds it first. 
            On Windows: when the app exits, if the file was previously confunded the file is reconfunded.
        * ""setkey"" - update the key stored securely on this machine for future runs.
        * ""changekey"" - change the key used to confund/unconfund the given file or all the files in a given folder. 
            The new key can be supplied with the -newkey argument.
            If path is a folder and -pattern is supplied, only those files matching the pattern will be processed.
        * ""checkkey"" - check if the supplied -newkey matches the stored key.
        * ""help"" - show this help text
    If -action is not supplied and the path is a single file.
        * If the file was not previously confunded, then the file will be confunded.
        * If the file was previously confunded, 
          then the file will be unconfunded and run.
          If running on Windows, the file will be reconfunded afterwards.
    [-pattern *] For use if the path is a folder. 
        If supplied, only those files within this folder matching the pattern will be processed.
    [-key] A text key to use for confunding and unconfunding the file/s.
    [-newkey] When changing or checking the key, the new key.
    [-silent] No console output and on Windows the command window is hidden.";

        private static void Main(string[] args)
        {
            Confunder.SpitOut = SpitOut;

            if (!ProcessArgs(args)) { return; }

            switch (_action)
            {
                case "setkey":
                    if (!SetKey()) { return; }
                    break;
                case "changekey":
                    if (!EnsureKeyLoaded()) { return; }
                    if(string.IsNullOrEmpty(_newKey))
                    {
                        if (!_silent)
                        {
                            _newKey = PromptForKey("Enter the new text key to use for confunding files", confirm: true);
                        }
                        else
                        {
                            Console.Error.WriteLine("No new key was supplied with the -newkey argument and the app cannot prompt for one in silent mode.");
                            return;
                        }
                    }
                    if (_isFolder)
                    {
                        Confunder.ChangeKeyAll(_path, _key, _newKey, _pattern);
                    }
                    else
                    {
                        Confunder.ChangeKeyFile(_path, _key, _newKey);
                    }
                    break;
                case "confund":
                    if (!EnsureKeyLoaded()) { return; }
                    if (_isFolder)
                    {
                        Confunder.ConfundAll(_path, _key, _pattern);
                    }
                    else
                    {
                        Confunder.ConfundFile(_path, _key);
                    }
                    break;
                case "unconfund":
                    if (!EnsureKeyLoaded()) { return; }
                    if (_isFolder)
                    {
                        Confunder.UnConfundAll(_path, _key, _pattern);
                    }
                    else
                    {
                        Confunder.UnConfundFile(_path, _key);
                    }
                    break;
                case "run":
                    if (_isFolder)
                    {
                        SpitOut("{0} is a folder. The run action can only be performed on a single file.", _path);
                    }
                    else
                    {
                        RunFile(_path);
                    }
                    break;
                case "checkkey":
                    if (!EnsureKeyLoaded()) { return; }
                    if(string.IsNullOrEmpty(_newKey))
                    {
                        SpitOut("To check the key, supply the key to check with the -newkey argument.");
                    }
                    else
                    {
                        if (_key == _newKey)
                        {
                            SpitOut("The supplied key matches the stored key.");
                        }
                        else
                        {
                            SpitOut("The supplied key does NOT match the stored key.");
                        }
                    }
                    break;
                default:
                    SpitOut(HELP_TEXT);
                    break;
            }
        }

        private static bool ProcessArgs(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine(HELP_TEXT);
                return false;
            }

            if (args.Contains("-action"))
            {
                var actionIndex = Array.IndexOf(args, "-action");
                if (actionIndex < 0 || actionIndex + 1 >= args.Length)
                {
                    Console.Error.WriteLine("Invalid action specified");
                    return false;
                }

                var legalActions = new string[] { "confund", "unconfund", "run", "setkey", "changekey", "checkkey", "help" };
                var action = args[actionIndex + 1];
                if (legalActions.Contains(action.ToLower()))
                {
                    _action = action.ToLower();
                }
                else
                {
                    Console.Error.WriteLine("Invalid action specified");
                    return false;
                }
            }

            var actionNeedsPath = _action != "setkey" && _action != "help";

            if (actionNeedsPath)
            {
                if (args.Contains("-path"))
                {
                    _path = args[Array.IndexOf(args, "-path") + 1];
                }
                else
                {
                    _path = args[0];
                }

                if (string.IsNullOrEmpty(_path))
                {
                    Console.Error.WriteLine("No file or folder path supplied");
                    return false;
                }
                else
                {
                    if (Directory.Exists(_path))
                    {
                        _isFolder = true;
                    }
                    else if (File.Exists(_path))
                    {
                        _isFolder = false;
                    }
                    else
                    {
                        Console.Error.WriteLine("file or folder path supplied could not be found");
                        return false;
                    }
                }

                if (string.IsNullOrWhiteSpace(_action))
                {
                    if (!_isFolder)
                    {
                        if (Confunder.IsFileConfunded(_path))
                        {
                            _action = "run";
                        }
                        else
                        {
                            _action = "confund";
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine($"{_path} is a directory and the -action argument has not been supplied");
                        _action = "help";
                        return false;
                    }
                }
            }

            if (args.Contains("-pattern"))
            {
                if (actionNeedsPath && _isFolder)
                {
                    _pattern = args[Array.IndexOf(args, "-pattern") + 1];
                }
                else if (actionNeedsPath)
                {
                    Console.Error.WriteLine($"{_path} is not a directory and the pattern has been supplied");
                    _action = "help";
                    return false;
                }
            }
            if (args.Contains("-key"))
            {
                if (args.Length < 2 || Array.IndexOf(args, "-key") + 1 >= args.Length)
                {
                    Console.Error.WriteLine("No key was supplied with the -key argument");
                    return false;
                }
                var rawKey = args[Array.IndexOf(args, "-key") + 1];
                if (!TryDeriveBase64Key(rawKey, out var derivedKey, out var error))
                {
                    Console.Error.WriteLine(error);
                    return false;
                }

                _key = derivedKey;
            }
            if (args.Contains("-newkey") && (_action == "changekey" || _action == "checkkey"))
            {
                if (args.Length < 2 || Array.IndexOf(args, "-newkey") + 1 >= args.Length)
                {
                    Console.Error.WriteLine("No new key was supplied with the -newkey argument");
                    return false;
                }
                var rawNewKey = args[Array.IndexOf(args, "-newkey") + 1];
                if (!TryDeriveBase64Key(rawNewKey, out var derivedNewKey, out var error))
                {
                    Console.Error.WriteLine(error);
                    return false;
                }

                _newKey = derivedNewKey;
            }

            if (args.Contains("-silent"))
            {
                _silent = true;
                try
                {
                    if (OperatingSystem.IsWindows())
                    {
                        Console.SetBufferSize(1, 1);
                        Console.SetWindowSize(1, 1);
                        Console.SetWindowPosition(-1, -1);
                    }
                }
                catch
                {
                }
            }

            return true;
        }

        private static void SpitOut(string txt, params object[] par)
        {
            if (!_silent)
            {
                Console.WriteLine(txt, par);
            }
        }

        private static bool RunFile(string path)
        {
            bool wasConfunded = Confunder.IsFileConfunded(path);
            if (wasConfunded)
            {
                if (!EnsureKeyLoaded()) { return false; }
                Confunder.UnConfundFile(path, _key);
                SpitOut("{0} has been successfully unconfunded", path);
            }
            SpitOut("Running {0}", path);

            try
            {
                string fullPath = new FileInfo(path).FullName;
                ProcessStartInfo startInfo = new()
                {
                    FileName = fullPath,
                    UseShellExecute = true
                };
                using var pr = Process.Start(startInfo);
                if (pr != null)
                {
                    pr.WaitForExit();
                    SpitOut("Process for {0} has exited", path);
                }
                else
                {
                    SpitOut("Process for {0} was reused or could not be started synchronously.", path);
                }
            }
            finally
            {
                if (wasConfunded && OperatingSystem.IsWindows())
                {
                    SpitOut("Starting to reconfund {0}", path);
                    Confunder.ConfundFile(path, _key);
                }
            }

            return true;
        }

        #region Key Management
        private static bool EnsureKeyLoaded()
        {
            if (!string.IsNullOrWhiteSpace(_key))
            {
                return true;
            }

            if (TryLoadStoredKey(out var storedKey))
            {
                _key = storedKey;
                return true;
            }

            if (_silent || Console.IsInputRedirected)
            {
                Console.Error.WriteLine("No stored key found and the app cannot prompt for one in silent mode.");
                return false;
            }

            var key = PromptForKey("Enter the text key used to confound files", confirm: true);
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            SaveStoredKey(key);
            _key = key;
            return true;
        }

        private static bool SetKey()
        {
            //If -key was supplied, it was saved in the _key variable by the ProcessArgs method.
            if (string.IsNullOrEmpty(_key))
            {
                if (!_silent)
                {
                    var key = PromptForKey("Enter the text key used to confound files", confirm: true);
                    _key = key;
                }
                else
                {
                    Console.Error.WriteLine("No key was supplied with the -key argument and the app cannot prompt for one in silent mode.");
                    return false;
                }
            }
            if (string.IsNullOrEmpty(_key)) { return false; }
            SaveStoredKey(_key);
            SpitOut("Key updated securely for this user.");
            SpitOut("Files encrypted with the previous key will still require that key to be unconfunded.");
            return true;
        }

        private static string PromptForKey(string prompt, bool confirm)
        {
            while (true)
            {
                Console.WriteLine(prompt + ":");
                var firstEntryRaw = ReadSecretLine();

                if (string.IsNullOrWhiteSpace(firstEntryRaw))
                {
                    Console.Error.WriteLine("The key cannot be empty.");
                    continue;
                }

                if (!TryDeriveBase64Key(firstEntryRaw, out var firstEntry, out var validationError))
                {
                    Console.Error.WriteLine(validationError);
                    continue;
                }

                if (confirm)
                {
                    Console.WriteLine("Confirm the key:");
                    var secondEntryRaw = ReadSecretLine();
                    if (!string.Equals(firstEntryRaw, secondEntryRaw, StringComparison.Ordinal))
                    {
                        Console.Error.WriteLine("The keys do not match.");
                        continue;
                    }
                }

                return firstEntry;
            }
        }

        private static string ReadSecretLine()
        {
            if (Console.IsInputRedirected)
            {
                return Console.ReadLine() ?? string.Empty;
            }

            var builder = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (builder.Length > 0)
                    {
                        builder.Length--;
                    }

                    continue;
                }

                if (!char.IsControl(key.KeyChar))
                {
                    builder.Append(key.KeyChar);
                }
            }

            return builder.ToString();
        }

        private static bool IsValidKey(string key, out string error)
        {
            error = null;

            try
            {
                var keyBytes = Convert.FromBase64String(key);
                if (keyBytes.Length != 16 && keyBytes.Length != 24 && keyBytes.Length != 32)
                {
                    error = "The key must decode to a 128, 192, or 256-bit AES key.";
                    return false;
                }

                return true;
            }
            catch
            {
                error = "The key must be valid base64.";
                return false;
            }
        }

        private static bool TryDeriveBase64Key(string rawKeyText, out string key, out string error)
        {
            key = null;
            error = null;

            if (string.IsNullOrWhiteSpace(rawKeyText))
            {
                error = "The key cannot be empty.";
                return false;
            }

            // The cipher expects a 128/192/256-bit AES key. Use SHA-256 to derive a fixed 256-bit key from any text input.
            var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKeyText));
            
            key = Convert.ToBase64String(keyBytes);
            return true;
        }

        private static byte[] GetNonWindowsAtRestKey()
        {
            var machineId = Environment.MachineName + Environment.UserName;
            return SHA256.HashData(Encoding.UTF8.GetBytes(machineId));
        }

        private static bool TryLoadStoredKey(out string key)
        {
            key = null;

            try
            {
                var keyPath = GetKeyStorePath();
                if (!File.Exists(keyPath))
                {
                    return false;
                }

                var protectedBytes = File.ReadAllBytes(keyPath);
                byte[] unprotectedBytes;

                if (OperatingSystem.IsWindows())
                {
                    unprotectedBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                }
                else
                {
                    using var aes = Aes.Create();
                    aes.Key = GetNonWindowsAtRestKey();
                    aes.IV = new byte[16];
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using var decryptor = aes.CreateDecryptor();
                    unprotectedBytes = decryptor.TransformFinalBlock(protectedBytes, 0, protectedBytes.Length);
                }

                var storedKey = Encoding.UTF8.GetString(unprotectedBytes);
                if (!IsValidKey(storedKey, out _))
                {
                    return false;
                }

                key = storedKey;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void SaveStoredKey(string key)
        {
            var keyPath = GetKeyStorePath();
            var directory = Path.GetDirectoryName(keyPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var unprotectedBytes = Encoding.UTF8.GetBytes(key);
            byte[] protectedBytes;

            if (OperatingSystem.IsWindows())
            {
                protectedBytes = ProtectedData.Protect(unprotectedBytes, null, DataProtectionScope.CurrentUser);
            }
            else
            {
                using var aes = Aes.Create();
                aes.Key = GetNonWindowsAtRestKey();
                aes.IV = new byte[16];
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using var encryptor = aes.CreateEncryptor();
                protectedBytes = encryptor.TransformFinalBlock(unprotectedBytes, 0, unprotectedBytes.Length);
            }

            File.WriteAllBytes(keyPath, protectedBytes);

            if (!OperatingSystem.IsWindows())
            {
                try
                {
                    File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                }
                catch { } // Best effort
            }
        }

        private static string GetKeyStorePath()
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(basePath))
            {
                basePath = AppContext.BaseDirectory;
            }

            return Path.Combine(basePath, "Confunder", "key.dat");
        }
        #endregion
    }
}
