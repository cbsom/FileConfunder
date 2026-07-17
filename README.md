# Confunder

Confunder is a blazingly fast command-line utility that obscures and secures files by encrypting their leading bytes into a non-runnable and non-recognizable form, and restores the files to their original state when needed.

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

- Obscuficate or "confund" or  a file
- Restore or "unconfund" the file
- Confunding or unconfunding all files in a folder (with optional  filtering)
- Running a confunded file by temporarily restoring it, then reconfunding it after exit
- Changing the key used to confund or unconfund files
- Checking whether a supplied key matches the key stored on the system

## How It Works

Confunder applies AES-ECB encryption to the first 4096 bytes of a file to obscure it without altering the file size, and then appends a binary marker to the end of the file.
<br />
At runtime, it detects whether a file is confunded by instantly checking for this marker.

## Requirements

- Windows, Linux, or macOS

## Build

From repository root:

```bash
dotnet build Confunder.sln
```

## Usage

General form:

confunder [action] [arguments] [options]

The following ***action***s are supported: [see below for details and examples]
- ***confund***  - Obscuficate or "confund" a file or all matching files in a folder.
- ***unconfund***  - Restore or "unconfund" a file or all matching files in a folder.
- ***run***  - Run a confunded file like normal using the system default.
- ***setkey*** - Store the "key" or password to be used for any future confunding operations on the current machine.
- ***changekey*** - Change the "key" used to confund or unconfund file/s
- ***checkkey***  - Check whether the supplied key matches the key stored in the system.
- ***help*** - Show built-in help text.

Options:

- ***-pattern*** - For actions on a folder, specify a  to filter which files to take this action upon.
- ***-silent*** - suppress console output. On Windows, hides the console window.

Notes:

- The first argument must be either a path to a single file or one of: ***confund***, ***unconfund***, ***run***, ***setkey***, ***changekey***, ***checkkey***, ***help***.
- If the first argument is a path to a single file, 
a previously *confunded* file will be *unconfunded* and a previously *unconfunded* file will be *confunded*.
- The actions ***confund***, ***unconfund***, ***run*** and ***changekey*** can optionally have a [key] supplied for the action.
<br />
If the [key] is not supplied, the [key] stored on the current system will be used for the operation.
- The -***-*** option is valid only for ***confund***, ***unconfund***, and ***changekey*** when the path is a folder.

## Actions

### ***confund***

Obscuficate or "confund" a file or all matching files in a folder.

Examples:

For *confunding* a single file - using the *key* stored in the system
```bash
confunder confund ./myfile.txt
```
For *confunding* all the files in a folder - using the *key* stored in the system
```bash
confunder confund ./allMyFiles
```
For *confunding* some of the files in a folder - using the *key* stored in the system
```bash
confunder confund ./allMyFiles -pattern "*.txt"
```
For *confunding* a file using a supplied *key*
```bash
confunder confund ./myFile.txt "Use This Key"
```

### ***unconfund***

Restore files to their original *unconfunded* state.

Examples:

For *unconfunding* a single *confunded* file - using the *key* stored in the system
```bash
confunder unconfund ./myfile.txt
```
For *unconfunding* all the *confunded* files in a folder - using the *key* stored in the system
```bash
confunder unconfund ./allMyFiles
```
For *unconfunding* some of the *confunded* files in a folder - using the *key* stored in the system
```bash
confunder unconfund ./allMyFiles -pattern "*.txt"
```
For *unconfunding* a *confunded* file using a supplied *key*
```bash
confunder unconfund ./myFile.txt "Use This Key"
```

### ***run***
Run a *confunded* file normally using the system.
<br />
**Confunder** restores/*unconfund*s the file it and runs it.

NOTE:
- On Windows, **confunder** waits for process to exit, then reconfunds the file.
- On macOS and Linux, the file is left *unconfunded*. You will need to run *confund* to re-confund the file.

Examples:

For running a single *confunded* file - using the *key* stored in the system
```bash
confunder run ./myfile.txt
```
For running a *confunded* file using a supplied *key*
```bash
confunder run ./myFile.txt "Use This Key"
```

### ***setkey***

Store the key to be used for any future confunding operations on this machine.
<br />
Any previous key will be overwritten. 

NOTE:<br />
To *unconfund* or *run* any file that was *confunded* with the original key, you will need to supply the original key.
<br />
You can always *change* the key for a confunded file by using the ***changekey*** action.

Example:

```bash
confunder setkey "my new secret"
```

### ***changekey***

Change the key used to *confund* or *unconfund* the supplied file or files.
<br />
If the [key] currently *confunding* the file is the same as the stored key for this machine, you only need to supply the new key.
<br />
Otherwise, you will need to supply both the current key and the new key.
<br />
If you do not supply any keys, you will be prompted for the new key. (twice!)

Examples:

To change the [key] for the supplied single confunded file using the stored [key] to the new supplied [key]:

```bash
confunder changekey ./myfile.txt "my new secret"
```
To change the [key] for the all the confunded files in the supplied folder using the stored [key] to the new supplied [key]:`

```bash
confunder changekey ./allMyFiles "my new secret"
```
To change the [key] for the all the confunded files in the supplied folder that match the supplied filter using the stored [key] to the new supplied [key]:`

```bash
confunder changekey ./allMyFiles -pattern "*.txt" "my new secret"
```
To change the [key] for the supplied file from the supplied current [key] to the supplied new [key].
```bash
confunder changekey ./myFile.txt "the old key" "my new secret"
```
To change the [key] for all the files in the supplied folder from the supplied current [key] to the supplied new [key].
```bash
confunder changekey ./myFiles "the old key" "my new secret"
```
To change the [key] for some of the files in the supplied folder from the supplied current [key] to the supplied new [key].
```bash
confunder changekey ./myFiles -pattern "*.txt" "the old key" "my new secret"
```
### checkkey

Check whether the supplied key matches the key stored in the system.

Example:

```bash
confunder checkkey "my new secret"
```

### help

Show built-in help text.

Example:

```bash
confunder help
```

## Key Handling

- You can provide any text for the key that is used to encrypt the beginig of the file.
- Confunder derives a fixed 256-bit key from that text using SHA-256.
- The derived key is encoded in base64 for internal AES encryption operations.
- When saved using `setkey`, the key is securely encrypted at rest and stored at:
  - LocalApplicationData/Confunder/key.dat
- When using `checkkey`, supply the key as `confunder checkkey "your key"`.
- When using `changekey`, supply the new key as `confunder changekey <path> "new key"`, or both keys as `confunder changekey <path> "old key" "new key"`.
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
confunder confund ./myapp.exe -silent
```

## Disclaimer

This tool is for controlled environments and operational obscuring workflows.
It is not a replacement for full cryptographic at-rest protection policies or key-management infrastructure.
It should never use confunder as a fool-proof security tool for protecting objectively sensitive information.

USE WITH CAUTION!
This tool changes files without asking you for verification!
It obfuscates files by encrypting the begining of their contents.
If you forget or lose the "key" used during obsificutaion, you will never be able to recover your full file contents.
