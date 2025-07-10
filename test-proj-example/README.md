# test-proj-example

A GP-λ (General-Purpose Lambda) project.

## Getting Started

### Prerequisites

- GP-λ compiler (`gpl` CLI)
- .NET 8.0 SDK or later (installed automatically with GP-λ)

### Building

```bash
gpl build
```

### Running

```bash
gpl run
```

### Building and Running

The `gpl run` command automatically builds your project before running it.

## Project Structure

- `src/` - Source code directory for .gpl files
- `build/` - Output directory for generated C# files
- `gplambda.json` - Project configuration file

## Commands

- `gpl build` - Compile all .gpl files in the project
- `gpl run` - Build and run the main program
- `gpl run src/example.gpl` - Run a specific file
