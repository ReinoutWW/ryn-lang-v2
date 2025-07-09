# GP-λ Compiler Implementation Logbook

This document provides a comprehensive record of all key decisions, architectural choices, and implementation details made during the development of the GP-λ (General-Purpose Lambda) compiler.

## Table of Contents

1. [Project Overview](#project-overview)
2. [Technology Stack](#technology-stack)
3. [Architecture Decisions](#architecture-decisions)
4. [Implementation Timeline](#implementation-timeline)
5. [Key Design Choices](#key-design-choices)
6. [Problem Solutions](#problem-solutions)
7. [Testing Strategy](#testing-strategy)
8. [Tooling and Developer Experience](#tooling-and-developer-experience)

## Project Overview

The GP-λ compiler is a source-to-source compiler (transpiler) that converts GP-λ source code to C#. GP-λ is a functional programming language designed for teaching functional programming concepts with features including:

- Strong static typing with type inference
- First-class functions and lambda expressions
- Design by Contract (always-on runtime assertions)
- Immutability by default
- Clean, minimal syntax

## Technology Stack

### Core Technologies

1. **Language**: C# (.NET 8.0)
   - **Rationale**: Modern, cross-platform runtime with excellent tooling support
   - **Benefits**: Strong type system, async/await support, rich ecosystem

2. **Parser Generator**: ANTLR 4
   - **Rationale**: Industry-standard parser generator with excellent C# support
   - **Benefits**: Clear grammar syntax, automatic AST generation, good error recovery
   - **Package**: Antlr4.Runtime (4.13.1) and Antlr4BuildTasks

3. **Code Generation**: Roslyn (Microsoft.CodeAnalysis.CSharp)
   - **Rationale**: Official C# compiler APIs for code generation
   - **Benefits**: Guaranteed valid C# output, formatting capabilities
   - **Package**: Microsoft.CodeAnalysis.CSharp (4.8.0)

4. **Testing Framework**: xUnit
   - **Rationale**: Modern, extensible testing framework
   - **Benefits**: Parallel test execution, good VS/VS Code integration
   - **Packages**: xunit (2.6.1), xunit.runner.visualstudio (2.5.3)

5. **Build System**: MSBuild / .NET SDK
   - **Rationale**: Native .NET build system
   - **Benefits**: Cross-platform, integrated with VS/VS Code, NuGet support

## Architecture Decisions

### 1. Multi-Stage Compilation Pipeline

```
Source Code → Lexing → Parsing → AST Building → Semantic Analysis → Type Checking → Code Generation → C# Output
```

**Rationale**: Clear separation of concerns, easier testing, better error reporting

### 2. Project Structure

```
ryn-lang-v2/
├── src/
│   ├── GpLambda.Compiler/      # Core compiler library
│   │   ├── Grammar/            # ANTLR grammar and generated files
│   │   ├── AST/               # Abstract Syntax Tree definitions
│   │   ├── SemanticAnalysis/  # Symbol table and semantic checking
│   │   ├── TypeChecking/      # Type system implementation
│   │   └── CodeGeneration/    # C# code generator
│   ├── GpLambda.CLI/          # Command-line interface
│   └── GpLambda.Tests/        # Unit and integration tests
├── examples/                   # Example GP-λ programs
├── docs/                      # Documentation
└── vscode-gplambda-extension/ # VS Code syntax highlighting
```

**Rationale**: 
- Separation of compiler logic from CLI
- Clear module boundaries
- Testability of individual components

### 3. AST Design

Chose a **strongly-typed AST** with visitor pattern:

```csharp
public abstract class AstNode
{
    public int Line { get; set; }
    public int Column { get; set; }
    public abstract T Accept<T>(IAstVisitor<T> visitor);
}
```

**Rationale**:
- Type safety during compilation
- Visitor pattern enables multiple passes without modifying AST nodes
- Line/column tracking for error reporting

### 4. Symbol Table Architecture

Implemented a **hierarchical scope-based symbol table**:

```csharp
public class Scope
{
    private readonly Dictionary<string, Symbol> _symbols;
    private readonly Scope? _parent;
    public ScopeType Type { get; }
}
```

**Rationale**:
- Natural representation of lexical scoping
- Efficient symbol lookup with parent chain
- Support for different scope types (Global, Function, Block, Lambda)

### 5. Type System Implementation

Designed a **structural type system** with:
- Primitive types: Int, String, Bool, Void
- Function types: (T1, T2, ...) -> TReturn
- Type inference for local variables

**Rationale**:
- Simple enough for educational purposes
- Powerful enough for real programs
- Type inference reduces boilerplate

### 6. Combined Semantic Analysis and Type Checking

After initial issues with separate passes, implemented **CombinedAnalyzer** that performs both semantic analysis and type checking in a single pass.

**Rationale**:
- Solves scope management issues between phases
- More efficient (single traversal)
- Enables type inference during semantic analysis

## Implementation Timeline

### Phase 1: Project Setup and Infrastructure
1. Created .NET solution with three projects (Compiler, CLI, Tests)
2. Set up ANTLR 4 build integration
3. Configured test framework and CI/CD considerations

### Phase 2: Grammar and Parsing
1. Implemented complete ANTLR grammar for GP-λ
2. Added support for all language constructs
3. Fixed grammar ambiguities and precedence issues

### Phase 3: AST Construction
1. Designed strongly-typed AST node hierarchy
2. Implemented AstBuilder visitor to convert parse tree to AST
3. Added source location tracking for error reporting

### Phase 4: Semantic Analysis
1. Implemented Symbol and Scope classes
2. Created SymbolTable with hierarchical scope management
3. Added semantic validation (duplicate definitions, undefined variables)

### Phase 5: Type System
1. Implemented type representation (primitive and function types)
2. Created TypeChecker with full type inference
3. Fixed scope management by combining with semantic analysis

### Phase 6: Code Generation
1. Implemented CSharpCodeGenerator using Roslyn
2. Added support for all GP-λ constructs
3. Implemented built-in functions (println, readLine, toString)

### Phase 7: CLI and Tooling
1. Created command-line compiler interface
2. Added project management (init, build commands)
3. Implemented VS Code extension for syntax highlighting

### Phase 8: Developer Experience
1. Simplified CLI with 'gpl' command
2. Added 'run' command for direct execution
3. Created wrapper scripts to hide .NET details

## Key Design Choices

### 1. Transpilation to C# Instead of Direct Compilation

**Decision**: Generate C# code rather than IL or native code

**Rationale**:
- Leverages C# runtime and libraries
- Easier debugging (readable output)
- Cross-platform support via .NET
- Educational value (students can see generated code)

### 2. Immutability by Default

**Decision**: All variables are immutable unless explicitly marked mutable

**Rationale**:
- Encourages functional programming style
- Prevents common bugs
- Aligns with GP-λ's educational goals

### 3. Always-On Assertions

**Decision**: Assertions cannot be disabled at runtime

**Rationale**:
- Design by Contract philosophy
- Catches bugs early in development
- Educational emphasis on correctness

### 4. Type Inference for Local Variables

**Decision**: Allow `let x = 5` without explicit type annotation

**Rationale**:
- Reduces boilerplate
- Type safety maintained through inference
- Better developer experience

### 5. Function Declaration Order

**Decision**: Functions must be declared before use (no forward declarations)

**Rationale**:
- Simplifies compiler implementation
- Clearer code organization
- Matches many functional languages

## Problem Solutions

### 1. Parse Error: "missing ';' at 'toString'"

**Problem**: Method calls like `x.toString()` were not recognized

**Solution**: Added method call expression to grammar:
```antlr
expr
    : ...
    | expr '.' ID '(' argList? ')'  # MethodCallExpr
    ;
```

### 2. Type Checker Scope Management

**Problem**: TypeChecker couldn't find symbols after SemanticAnalyzer exited scopes

**Solution**: Created CombinedAnalyzer that performs both analyses in one pass:
```csharp
public class CombinedAnalyzer : IAstVisitor<Type?>
{
    // Performs both semantic analysis and type checking
    // Maintains scope throughout entire analysis
}
```

### 3. String Escape Sequences

**Problem**: Incorrect handling of escape sequences in string literals

**Solution**: Proper unescaping in AstBuilder:
```csharp
text = text.Replace("\\n", "\n")
           .Replace("\\t", "\t")
           .Replace("\\r", "\r")
           .Replace("\\\"", "\"")
           .Replace("\\\\", "\\");
```

### 4. VS Code Extension Icon

**Problem**: "SVGs can't be used as icons" error

**Solution**: Created PNG icon instead of SVG for VS Code extension

### 5. .NET Details Exposure

**Problem**: Users had to understand .NET tooling to use the compiler

**Solution**: 
- Created 'gpl' wrapper scripts
- Implemented 'run' command that handles compilation and execution
- Hid all .NET-specific commands behind simple interface

## Testing Strategy

### 1. Unit Tests
- **Parser Tests**: Verify grammar handles all language constructs
- **AST Builder Tests**: Ensure correct AST construction
- **Semantic Analysis Tests**: Check error detection and symbol resolution
- **Type Checker Tests**: Validate type inference and checking
- **Code Generator Tests**: Verify correct C# output

### 2. Integration Tests
- End-to-end compilation of sample programs
- Execution of generated C# code
- Error reporting validation

### 3. Test Organization
- Separate test classes for each compiler phase
- Use of xUnit's Fact and Theory attributes
- Descriptive test names following Given_When_Then pattern

**Total Tests**: 98 tests covering all compiler components

## Tooling and Developer Experience

### 1. VS Code Extension
- **Language ID**: gplambda
- **File Extensions**: .gpl, .gplambda
- **Features**: Syntax highlighting using TextMate grammar
- **Installation**: Via VSIX package or marketplace

### 2. CLI Design
- **Simple Commands**: `gpl init`, `gpl build`, `gpl run`
- **Project Structure**: Standardized with gplambda.json
- **Error Messages**: Clear, actionable error reporting with line numbers

### 3. Build Integration
- **ANTLR Integration**: Automatic grammar compilation on build
- **.NET Global Tool**: Can be installed via `dotnet tool install`
- **Cross-Platform**: Works on Windows, Linux, macOS

### 4. Documentation
- **README**: Comprehensive project overview
- **Academic Context**: Links to research papers
- **Examples**: Working GP-λ programs demonstrating features

## Lessons Learned

1. **Incremental Development**: Building the compiler in phases with tests at each stage prevented major rework
2. **Clear Separation**: Keeping parsing, analysis, and generation separate made debugging easier
3. **User Experience Matters**: Hiding implementation details (like .NET) greatly improves usability
4. **Type Safety**: Using C#'s type system to model the AST caught many bugs at compile time
5. **Tool Integration**: VS Code extension and CLI tools are essential for language adoption

## Future Considerations

1. **Language Server Protocol**: Implement LSP for better IDE support
2. **Optimization**: Add optimization passes before code generation
3. **REPL**: Interactive Read-Eval-Print Loop for quick experimentation
4. **More Backends**: Consider LLVM or WebAssembly targets
5. **Package Manager**: System for sharing GP-λ libraries

---

This logbook represents the complete implementation journey of the GP-λ compiler, from initial setup to final developer experience enhancements.