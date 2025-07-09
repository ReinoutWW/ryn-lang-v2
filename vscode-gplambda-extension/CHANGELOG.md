# Change Log

All notable changes to the GP-λ Language Support extension will be documented in this file.

## [0.1.0] - 2025-01-09

### Added
- Initial release of GP-λ language support
- Syntax highlighting for GP-λ source files (`.gpl`, `.gplambda`)
- Support for all GP-λ language constructs:
  - Keywords: `func`, `let`, `if`, `else`, `return`, `assert`
  - Types: `Int`, `String`, `Bool`, `Void`, `Func`
  - Built-in functions: `println`, `readLine`, `toString`
  - Operators: arithmetic, comparison, logical, lambda, function
  - String literals with escape sequences
  - Integer and floating-point number literals
  - Single-line and block comments
- Automatic bracket matching and closing
- Comment toggling support (Ctrl+/)
- Code folding for functions and blocks
- Language configuration for proper indentation