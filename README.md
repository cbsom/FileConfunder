# Confunder

Confunder is a command-line utility that obscures executable files by transforming their leading bytes into a non-runnable form, then restores them when needed.

It supports:
- Confunding a file
- Restoring or "unconfunding" a file
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

  dotnet build Confunder.sln

## Run

From repository root:

  dotnet run --project Confunder/Confunder.csproj -- <arguments>

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
- setkey requires -key

## Actions

### confund
Confund a file or all matching files in a folder.

Examples:

  confunder ./myfile.txt -action confund -key "my secret"

  confunder ./allMyFiles -action confund -pattern "*.txt" -key "my secret"

### unconfund
Reverse confund for a file or all matching files in a folder.

Examples:

  confunder ./myfile.txt -action unconfund -key "my secret"

  confunder ./allMyFiles -action unconfund -pattern "*.exe" -key "my secret"

### run
If the file is confunded, Confunder restores it, starts it, waits for process to exit, then reconfunds it.

Example:

  confunder ./myfile.txt -action run -key "my secret"

### setkey
Store or rotate the key used for future operations.

Example:

  confunder . -action setkey -key "my new secret"

### help
Show built-in help text.

Example:

  confunder . -action help

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

Example:

  confunder ./myapp.exe -action confund -silent -key "my secret"

## Exit Behavior

When not in silent mode, Confunder waits for Enter before exiting.

## Project Structure

- Confunder/Program.cs: CLI logic, AES encryption, and file processing flow
- Confunder.sln: solution

## Disclaimer

This tool is for controlled environments and operational obscuring workflows.
It is not a replacement for full cryptographic at-rest protection policies or key-management infrastructure.
