# Confunder

Confunder is a command-line utility that obscures files by transforming their leading bytes into a non-runnable and non-recognizable form, and restores the files to their original state when needed.

Its primary purpose is as a good deterrent for an undetermined hacker or curious individual from accessing your files.

It can make life difficult for someone who shouldn't be looking at your files, but it is NOT a fool-proof high-end security tool and should not be used to protect objectively sensitive information.

It supports:

- Confunding a file
- Restoring or "unconfunding" the file
- Confunding or Unconfunding all files in a folder (with optional pattern filtering)
- Running a confunded file by temporarily restoring it, then reconfunding it after exit

## How It Works

Confunder applies standard AES-ECB encryption to the first 4096 bytes of a file to obscure it without altering the file size, and then appends a marker to the end of the file.

At runtime, it can detect whether a file is already confunded by instantly checking for the marker at the end of the file.

## Requirements

- .NET SDK that supports net10.0
- Windows, Linux, or macOS

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

confunder path -action <confund|unconfund|run|setkey|help> [options]

Options:

- -pattern <text>
- -key <text>
- -silent

Notes:

- For file paths, if -action is omitted:
  - Not yet confunded: it will be confunded
  - Already confunded: it will be unconfunded, run, then reconfunded
- For folder paths, -action is required

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

Store or rotate the key used for future operations.

Examples:

```bash
confunder . -action setkey -key "my new secret"
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

## Exit Behavior

When not in silent mode, Confunder waits for Enter before exiting.

## Project Structure

- Confunder/Program.cs: CLI logic, AES encryption, and file processing flow
- Confunder.sln: solution

## Disclaimer

This tool is for controlled environments and operational obscuring workflows.
It is not a replacement for full cryptographic at-rest protection policies or key-management infrastructure.
