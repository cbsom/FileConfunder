/*******************************************************************
 * Confunder - File obfuscator and deobfuscator.
 * 
 * Licensed under the GNU General Public License v3.0.
 * 
 * Usage:
 *   confunder <action> [arguments] [options]
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
 *   -silent              - Suppress all console output.
 *   -pattern <pattern>   - Use a pattern to find files when the target is a folder.
 * Examples:
 *   confunder confund ./myfolder
 *   confunder unconfund ./myfolder -pattern "*.cs"
 *   confunder run ./myfile.txt
 *   confunder setkey "My New Key"
 *   confunder changekey ./myfolder "The Old Key" "The New Key"
 *   confunder checkkey "The Key To Check"
 * 
 *********************************************************************/

using System;
using System.Collections.Generic;
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
    confunder <action> [arguments] [options]

    The first argument must be an action. Supported actions:
        * ""confund"" - confund the given file or all the files in a given folder.
            Usage: confunder confund <path> [key]
            If path is a folder and -pattern is supplied, only those files matching the pattern will be processed.
        * ""unconfund"" - unconfund the given file or all the files in a given folder.
            Usage: confunder unconfund <path> [key]
            If path is a folder and -pattern is supplied, only those files matching the pattern will be processed.
        * ""run"" - run a single file. If it is confunded, unconfunds it first.
            Usage: confunder run <path> [key]
            On Windows: when the app exits, if the file was previously confunded the file is reconfunded.
        * ""setkey"" - update the key stored securely on this machine for future runs.
            Usage: confunder setkey [key]
        * ""changekey"" - change the key used to confund/unconfund the given file or all the files in a given folder.
            Usage: confunder changekey <path> [newKey]
                   confunder changekey <path> <oldKey> <newKey>
            If path is a folder and -pattern is supplied, only those files matching the pattern will be processed.
        * ""checkkey"" - check if the supplied key matches the stored key.
            Usage: confunder checkkey <keyToCheck>
        * ""help"" - show this help text

    [key] values are text keys and will be converted into an AES key.
    [-pattern *] For use with ""confund"", ""unconfund"", and ""changekey"" when the path is a folder.
        If supplied, only those files within this folder matching the pattern will be processed.
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
                    if (string.IsNullOrEmpty(_newKey))
                    {
                        if (!_silent)
                        {
                            _newKey = PromptForKey("Enter the new text key to use for confunding files", confirm: true);
                        }
                        else
                        {
                            Console.Error.WriteLine("No new key was supplied and the app cannot prompt for one in silent mode.");
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
                    if (string.IsNullOrEmpty(_newKey))
                    {
                        SpitOut("To check the key, supply the key to check as the second argument.");
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

            string pattern = null;
            var positionalArgs = new List<string>();
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (string.Equals(arg, "-silent", StringComparison.OrdinalIgnoreCase))
                {
                    _silent = true;
                    continue;
                }

                if (string.Equals(arg, "-pattern", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("No pattern was supplied with the -pattern argument");
                        return false;
                    }

                    pattern = args[++i];
                    continue;
                }

                positionalArgs.Add(arg);
            }

            if (positionalArgs.Count < 1)
            {
                Console.Error.WriteLine("No action specified");
                return false;
            }

            var firstArg = positionalArgs[0];
            var legalActions = new[] { "confund", "unconfund", "run", "setkey", "changekey", "checkkey", "help" };
            if (!legalActions.Contains(firstArg.ToLowerInvariant()))
            {
                //If the first argument is a file path, then we will infer the action based on whether the file is confunded or not.
                if (File.Exists(firstArg))
                {
                    _action = Confunder.IsFileConfunded(firstArg) ? "unconfund" : "confund";
                    _path = firstArg;
                    if (positionalArgs.Count == 2) //a key was supplied
                    {
                        if (!TryDeriveBase64Key(positionalArgs[1], out var suppliedKey, out var suppliedKeyError))
                        {
                            SpitOut("Invalid <key> supplied for " + _action + ".");
                            Console.Error.WriteLine(suppliedKeyError);
                            return false;
                        }

                        _key = suppliedKey;
                    }
                    return true;
                }
                else
                {
                    SpitOut($"Invalid action specified: {firstArg}. Valid actions: confund, unconfund, run, setkey, changekey, checkkey, help");
                    return false;
                }
            }
            else
            {
                _action = firstArg.ToLowerInvariant();
            }

            var values = positionalArgs.Skip(1).ToArray();

            if (pattern != null)
            {
                if (_action != "confund" && _action != "unconfund" && _action != "changekey")
                {
                    Console.Error.WriteLine("The -pattern argument is only valid for confund, unconfund, and changekey.");
                    return false;
                }

                _pattern = pattern;
            }

            switch (_action)
            {
                case "setkey":
                    if (values.Length > 1)
                    {
                        SpitOut("Too many arguments supplied for setkey.");
                        return false;
                    }

                    if (values.Length == 1)
                    {
                        if (!TryDeriveBase64Key(values[0], out var setKey, out var setKeyError))
                        {
                            SpitOut("Invalid key supplied for setkey.");
                            Console.Error.WriteLine(setKeyError);
                            return false;
                        }

                        _key = setKey;
                    }
                    break;

                case "checkkey":
                    if (values.Length != 1)
                    {
                        SpitOut("The checkkey action requires exactly one key argument.");
                        return false;
                    }

                    if (!TryDeriveBase64Key(values[0], out var checkKey, out var checkKeyError))
                    {
                        SpitOut("Invalid key supplied for checkkey.");
                        Console.Error.WriteLine(checkKeyError);
                        return false;
                    }

                    _newKey = checkKey;
                    break;

                case "confund":
                case "unconfund":
                case "run":
                    if (values.Length < 1 || values.Length > 2)
                    {
                        SpitOut($"The {_action} action format is: confunder {_action} <path> and optionally [<key>].");
                        return false;
                    }

                    _path = values[0];
                    if (!TryResolvePath(_path, out _isFolder))
                    {
                        return false;
                    }

                    if (values.Length == 2)
                    {
                        if (!TryDeriveBase64Key(values[1], out var suppliedKey, out var suppliedKeyError))
                        {
                            SpitOut("Invalid <key> supplied for " + _action + ".");
                            Console.Error.WriteLine(suppliedKeyError);
                            return false;
                        }

                        _key = suppliedKey;
                    }

                    if (pattern != null && _action == "run")
                    {
                        SpitOut("The run action does not support -pattern.");
                        return false;
                    }

                    if (pattern != null && !_isFolder)
                    {
                        SpitOut($"{_path} is not a directory and the pattern has been supplied");
                        return false;
                    }
                    break;

                case "changekey":
                    if (values.Length < 1 || values.Length > 3)
                    {
                        SpitOut("The changekey action requires <path> and supports [newKey] or <oldKey> <newKey>.");
                        return false;
                    }

                    _path = values[0];
                    if (!TryResolvePath(_path, out _isFolder))
                    {
                        return false;
                    }

                    if (pattern != null && !_isFolder)
                    {
                        SpitOut($"{_path} is not a directory and the pattern has been supplied");
                        return false;
                    }

                    if (values.Length == 2)
                    {
                        if (!TryDeriveBase64Key(values[1], out var newKeyOnly, out var newKeyOnlyError))
                        {
                            SpitOut("Invalid <key> supplied for " + _action + ".");
                            Console.Error.WriteLine(newKeyOnlyError);
                            return false;
                        }

                        _newKey = newKeyOnly;
                    }
                    else if (values.Length == 3)
                    {
                        if (!TryDeriveBase64Key(values[1], out var oldKey, out var oldKeyError))
                        {
                            SpitOut("Invalid <key> supplied for the existing key for " + _action + ".");
                            Console.Error.WriteLine(oldKeyError);
                            return false;
                        }

                        if (!TryDeriveBase64Key(values[2], out var newKey, out var newKeyError))
                        {
                            SpitOut("Invalid <key> supplied as the new key for " + _action + ".");
                            Console.Error.WriteLine(newKeyError);
                            return false;
                        }

                        _key = oldKey;
                        _newKey = newKey;
                    }
                    break;
            }

            if (_silent)
            {
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

        private static bool TryResolvePath(string path, out bool isFolder)
        {
            isFolder = false;

            if (string.IsNullOrEmpty(path))
            {
                SpitOut("No file or folder path supplied");
                return false;
            }

            if (Directory.Exists(path))
            {
                isFolder = true;
                return true;
            }

            if (File.Exists(path))
            {
                isFolder = false;
                return true;
            }

            SpitOut("file or folder path supplied could not be found");
            return false;
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
            // If a key argument was supplied, it was saved in _key by ProcessArgs.
            if (string.IsNullOrEmpty(_key))
            {
                if (!_silent)
                {
                    var key = PromptForKey("Enter the text key used to confound files", confirm: true);
                    _key = key;
                }
                else
                {
                    Console.Error.WriteLine("No key was supplied and the app cannot prompt for one in silent mode.");
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
