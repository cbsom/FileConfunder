# Confunder

Confunder is a command-line utility that obscures executable files by transforming their leading bytes into a non-runnable form, then restores them when needed.

It supports:
- Confunding a single file
- Confunding all files in a folder (with optional pattern filtering)
- Unconfunding files
- Running a confunded file by temporarily restoring it, then reconfudding it after exit
- Storing and rotating an application key

## How It Works

Confunder writes a marker to the beginning of a file and applies reversible byte-level transformation to file content.

At runtime, it can detect whether a file is already confunded by checking the marker.

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
- -pattern <glob>
- -key <text>
- -silent

Notes:
- For file paths, if -action is omitted:
  - Not yet confunded: it will be confunded
  - Already confunded: it will be unconfunded, run, then reconfudded
- For folder paths, -action is required
- setkey requires -key

## Actions

### confund
Confund a file or all matching files in a folder.

Examples:

  confunder ./myapp.exe -action confund -key "my secret"

  confunder ./tools -action confund -pattern "*.exe" -key "my secret"

### unconfund
Reverse confund for a file or all matching files in a folder.

Examples:

  confunder ./myapp.exe -action unconfund -key "my secret"

  confunder ./tools -action unconfund -pattern "*.exe" -key "my secret"

### run
If the file is confunded, Confunder restores it, starts it, waits for process exit, then reconfuds it.

Example:

  confunder ./myapp.exe -action run -key "my secret"

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
- The derived key is encoded in base64 for internal encryption operations.
- When saved, the key is encrypted using Confunder encryption logic and stored at:
  - LocalApplicationData/Confunder/key.dat

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

- Confunder/Program.cs: CLI logic and file processing flow
- Confunder/FPE.Net/: transformation primitives
- Confunder.sln: solution

## Disclaimer

This tool is for controlled environments and operational obscuring workflows.
It is not a replacement for full cryptographic at-rest protection policies or key-management infrastructure.
