# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is the GP-λ (General-Purpose Lambda) programming language project - a new language designed to run on the .NET runtime. The project is currently in the architecture/design phase with implementation starting soon.

## Key Architecture Documents

- **docs/ryn-architecture.md**: The comprehensive architecture document that defines the entire language design, compiler architecture, and implementation strategy. Always refer to this when implementing features.

## Build Commands

Since this is a planned .NET project, use these commands once implementation begins:

```bash
# Build the entire solution
dotnet build

# Run tests
dotnet test

# Build in release mode
dotnet build -c Release

# Run the compiler CLI (after building)
dotnet run --project src/GpLambda.CLI -- <arguments>
```

## Project Structure

The planned structure (from architecture document):

```
src/
├── GpLambda.Compiler/       # Core compiler library
│   ├── Grammar/             # ANTLR .g4 grammar files
│   ├── Parser/              # Generated lexer/parser code
│   ├── AST/                 # AST nodes and visitors
│   └── CodeGen/             # Code generators (C# and IL)
├── GpLambda.CLI/            # Command-line compiler tool
└── GpLambda.Runtime/        # Runtime support library
```

## Language Design Principles

1. **Lambda calculus influenced**: First-class functions, immutability by default
2. **Design by Contract**: Runtime assertions are always enabled
3. **Token-compact**: Optimize for minimal token usage in AI contexts
4. **Type inference**: Strong static typing with inference
5. **.NET interoperability**: Seamless integration with C# and other .NET languages

## Implementation Strategy

The architecture document specifies two backend approaches:
1. **C# Transpilation**: Generate C# code (initial approach)
2. **Direct IL Emission**: Generate .NET IL directly (future optimization)

Start with C# transpilation for faster initial development.

## Key Dependencies

When setting up the project, add these packages:
- ANTLR4.Runtime for C# parser generation
- Microsoft.CodeAnalysis for C# code generation
- System.CommandLine for CLI parsing
- xUnit or NUnit for testing framework

## Grammar Development

The language grammar should be developed in `src/GpLambda.Compiler/Grammar/GpLambda.g4`. Use ANTLR 4 for parser generation.

## Testing Strategy

- Unit tests for each compiler phase (lexer, parser, semantic analysis, code generation)
- Integration tests with example GP-λ programs
- Test both valid and invalid programs
- Ensure Design by Contract assertions are properly tested

## Example GP-λ Syntax

Refer to docs/ryn-architecture.md for complete syntax examples. Key features:
- Function definitions: `fn name(params) -> type { body }`
- Contracts: `requires`, `ensures`, `invariant`
- Lambda expressions: `x => x * 2`
- Pattern matching capabilities
- Type annotations and inference