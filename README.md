# GP-λ Compiler

A compiler implementation for GP-λ (General-Purpose Lambda), a model-centric programming language designed for generative AI systems. This project implements a complete compiler toolchain that transpiles GP-λ source code to C# and runs on the .NET runtime.

## Overview

GP-λ is a programming language that combines functional programming concepts with strong type safety and Design by Contract principles. The language is specifically designed to support building AI-powered applications while maintaining correctness through runtime assertions and type checking.

### Key Features

- **Functional Programming**: First-class functions, lambda expressions, and immutable-by-default semantics
- **Strong Type System**: Static type checking with type inference
- **Design by Contract**: Built-in support for assertions and runtime verification
- **C# Transpilation**: Compiles to readable, efficient C# code
- **.NET Integration**: Full access to the .NET ecosystem and libraries
- **Type Safety**: Comprehensive type checking with helpful error messages

## Academic Context

This implementation is inspired by research papers in the `/docs/papers` directory:

1. **"gp-λ: A Model-Centric Programming Language for Generative AI Systems"** - This paper introduces GP-λ as a language designed to bridge the gap between traditional programming and AI model integration, providing abstractions for working with generative models while maintaining type safety and correctness guarantees.

2. **"SynthAugment: Diffusion-based Data Augmentation for Low-Data Image Classification"** - While focused on data augmentation, this paper demonstrates the types of AI systems that GP-λ is designed to support, showcasing the need for languages that can elegantly express AI workflows.

## Language Examples

### Hello World
```gplambda
func main() {
    println("Hello, World!");
}
```

### Functions and Type Inference
```gplambda
func add(x: Int, y: Int) -> Int {
    return x + y;
}

func main() {
    let result = add(5, 3);  // Type inference: result is Int
    println("5 + 3 = " + result.toString());
}
```

### Lambda Expressions
```gplambda
func main() {
    let double = (x: Int) => x * 2;
    let numbers = [1, 2, 3, 4, 5];
    let doubled = numbers.map(double);
    println("Doubled: " + doubled.toString());
}
```

### Design by Contract
```gplambda
func divide(a: Int, b: Int) -> Int {
    assert(b != 0, "Division by zero!");
    return a / b;
}

func main() {
    let result = divide(10, 2);
    println("10 / 2 = " + result.toString());
}
```

## Project Structure

```
ryn-lang-v2/
├── src/
│   ├── GpLambda.Compiler/     # Core compiler library
│   │   ├── Grammar/           # ANTLR grammar definition
│   │   ├── AST/              # Abstract Syntax Tree nodes
│   │   ├── SemanticAnalysis/ # Symbol table and semantic checking
│   │   ├── TypeChecking/     # Type system implementation
│   │   └── CodeGeneration/   # C# code generator
│   └── GpLambda.CLI/         # Command-line compiler tool
├── tests/
│   └── GpLambda.Tests/       # Comprehensive test suite
├── examples/                 # Example GP-λ programs
└── docs/                    # Documentation and papers
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Git

### Building the Compiler

```bash
# Clone the repository
git clone https://github.com/ReinoutWW/ryn-lang-v2.git
cd ryn-lang-v2

# Build the solution
dotnet build

# Run tests
dotnet test
```

### Using the Compiler

```bash
# Compile a GP-λ program to C#
dotnet run --project src/GpLambda.CLI -- --input examples/hello_world.gpl --output hello_world.cs

# For help
dotnet run --project src/GpLambda.CLI -- --help
```

### Command-Line Options

- `-i, --input <file>`: The GP-λ source file to compile (required)
- `-o, --output <file>`: The output C# file (defaults to input filename with .cs extension)
- `-v, --verbose`: Enable verbose output
- `-h, --help`: Show help message

## Language Features

### Type System

GP-λ supports the following primitive types:
- `Int`: 32-bit integers
- `String`: Text strings
- `Bool`: Boolean values
- `Void`: No return value

And complex types:
- Function types: `Func<T1, T2, ..., TReturn>`
- Future: Arrays, tuples, records, and more

### Built-in Functions

- `println(message: String)`: Print to console with newline
- `readLine() -> String`: Read line from console input
- `toString(value: Int) -> String`: Convert integer to string

### Control Flow

- `if/else` statements
- Function calls
- Return statements
- Future: loops, pattern matching

### Assertions

GP-λ includes Design by Contract support through assertions:
```gplambda
assert(condition, "optional message");
```

These assertions are always checked at runtime and cannot be disabled, ensuring program correctness.

## Implementation Details

### Compiler Pipeline

1. **Lexing & Parsing**: ANTLR 4 generates lexer and parser from grammar
2. **AST Construction**: Parse tree is converted to strongly-typed AST
3. **Semantic Analysis**: Symbol table construction and name resolution
4. **Type Checking**: Type inference and validation
5. **Code Generation**: AST is transpiled to C# code
6. **Output**: Executable C# code ready for .NET compilation

### Technology Stack

- **Language**: C# (.NET 8)
- **Parser Generator**: ANTLR 4
- **Testing**: xUnit with FluentAssertions
- **Code Analysis**: Roslyn (for future IL generation)

## Testing

The project includes comprehensive test coverage:
- **Parser Tests**: Grammar and syntax validation
- **AST Tests**: Tree construction verification
- **Type System Tests**: Type checking and inference
- **Semantic Analysis Tests**: Symbol resolution and scoping
- **Code Generation Tests**: C# output validation
- **Integration Tests**: End-to-end compilation and execution

Run all tests:
```bash
dotnet test
```

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests. Areas of interest include:

- Additional language features (loops, arrays, pattern matching)
- Optimization passes
- Direct IL generation
- Language server protocol (LSP) implementation
- Standard library development

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- The GP-λ language design is inspired by functional programming languages like ML, Haskell, and F#
- Built with ANTLR 4 for robust parsing
- Leverages the .NET ecosystem for cross-platform execution

## Future Roadmap

- [ ] Loop constructs (while, for)
- [ ] Array and list types
- [ ] Pattern matching
- [ ] Module system
- [ ] Generic types
- [ ] Direct IL generation
- [ ] REPL implementation
- [ ] VS Code extension
- [ ] Standard library
- [ ] Package manager