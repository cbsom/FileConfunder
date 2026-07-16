# Confunder

Confunder is a command-line utility that obscures files by transforming their leading bytes into a non-runnable and non-recognizable form, and restores the files to their original state when needed.

Its primary purpose is as a good deterrent for an undetermined hacker or a nosy individual from accessing your files.
<br />
It can make life difficult for someone who shouldn't be looking at your files, but it DOES NOT ENCRYPT the entire file. <br />
Therefore, you should never use confunder as a fool-proof security tool for protecting objectively sensitive information.

PLEASE USE WITH CAUTION!!!
<br />
Running this tool will change the files supplied to it without asking for verification.<br />
Your files will have the first 4096 bytes of their contents encrypted.<br />
If you forget or lose the "key" used during obfuscation, you may never be able to recover your full file contents.

It supports:

- Confunding a file
- Restoring or "unconfunding" the file
- Confunding or Unconfunding all files in a folder (with optional pattern filtering)
- Running a confunded file by temporarily restoring it, then reconfunding it after exit
- Changing the key used to confund or unconfund files
- Checking whether a supplied key matches the key stored on the system

## How It Works

Confunder applies AES-ECB encryption to the first 4096 bytes of a file to obscure it without altering the file size, and then appends a binary marker to the end of the file.
<br />
At runtime, it can detect whether a file is confunded by instantly checking for this marker.

## Requirements

- Windows, Linux, or macOS
- .NET SDK that supports net10.0

## Build

From repository root:

```bash
dotnet build Confunder.sln
```

## Run

From repository root:

```bash
dotnet run --project Confunder/Confunder.csproj -- <arguments>
```

If you publish or build release binaries, the executable name is confunder.

## Usage

General form:

confunder path -action <confund|unconfund|run|setkey|changekey|checkkey|help> [options]

Options:

- -pattern <text>
- -key <text>
- -newkey <text>
- -silent

Notes:

- If -action is omitted, the path must be supplied and it must point to a single file, not a folder.
  <br /> The following action will be taken for this file:
  - If the path is a single file which is not yet confunded, it will be confunded
  - If the path is a single file which is confunded, it will be unconfunded and run. On Windows after closing the file, it will be reconfunded.
- If the path is to a folder, an -action argument needs to be supplied.

## Actions

### confund

Confund a file or all matching files in a folder.

Examples:

```bash
confunder ./myfile.txt -action confund
```

```bash
confunder ./allMyFiles -action confund -pattern "\*.txt"
```

### unconfund

Reverse confund for a file or all matching files in a folder.

Examples:

```bash
confunder ./myfile.txt -action unconfund
```

```bash
confunder ./allMyFiles -action unconfund -pattern "\*.exe"
```

### run

If the file is confunded, Confunder restores it, starts it, waits for process to exit, then reconfunds it.

Examples:

```bash
confunder ./myfile.txt -action run
```

### setkey

Store the key to be used for any future confunding operations.

Note: any previous key will be overwritten. This will mean that to unconfund any file that was confunded with the previous key, you will need to supply the -key argument with the previous key.

You can change the confund key for any confunded file by using the "changekey" action.

Examples:

```bash
confunder . -action setkey -key "my new secret"
```

### changekey

Change the key used to confund or unconfund the target file or matching files in a folder.
You can supply the new key in the -newkey argument.
If you do not supply it, you will be prompted for it. (twice!)

If the file is not currently confunded with the key stored in the system,
you will need to also supply the -key argument with the key currently confunding the file.

Examples:

```bash
confunder ./myfile.txt -action changekey -newkey "my new secret"
```

```bash
confunder ./allMyFiles -action changekey -pattern "\*.txt" -newkey "my new secret"
```

### checkkey

Check whether the supplied key matches the key stored in the system.

Examples:

```bash
confunder . -action checkkey -newkey "my new secret"
```

### help

Show built-in help text.

Examples:

```bash
confunder . -action help
```

## Key Handling

- You can provide any text with -key.
- Confunder derives a fixed 256-bit key from that text using SHA-256.
- The derived key is encoded in base64 for internal AES encryption operations.
- When saved using `setkey`, the key is securely encrypted at rest and stored at:
  - LocalApplicationData/Confunder/key.dat
- When using `changekey` or `checkkey`, supply the comparison key with `-newkey`.
- **At-Rest Security:**
  - On Windows, Confunder uses native DPAPI (`ProtectedData`) to tie the saved key securely to your user account.
  - On Linux and macOS, Confunder dynamically derives a unique local AES key based on the machine name and username to protect the saved key file from casual observation, and relies on strict OS file permissions.
- **Cross-Platform Compatibility:** The file manipulation itself relies solely on the derived SHA-256 key. This means a file confunded on Windows can be unconfunded on Linux or macOS as long as the same original text key is provided.

Important:

- Files encrypted with one key cannot be unconfunded with a different key.
- Rotating the stored key does not re-encrypt previously confunded files.

## Silent Mode

Use -silent to suppress console output.

Examples:

```bash
confunder ./myapp.exe -action confund -silent
```

## Disclaimer

This tool is for controlled environments and operational obscuring workflows.
It is not a replacement for full cryptographic at-rest protection policies or key-management infrastructure.
It should never use confunder as a fool-proof security tool for protecting objectively sensitive information.

USE WITH CAUTION!
This tool changes files without asking you for verification!
It obfuscates files by encrypting the begining of their contents.
If you forget or lose the "key" used during obsificutaion, you will never be able to recover your full file contents.
