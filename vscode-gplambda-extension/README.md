# GP-λ Language Support for Visual Studio Code

This extension provides syntax highlighting and language support for GP-λ (General-Purpose Lambda) programming language files.

## Features

- Syntax highlighting for `.gpl` and `.gplambda` files
- Automatic bracket matching and closing
- Comment toggling support
- Code folding regions

## Supported File Extensions

- `.gpl`
- `.gplambda`

## Syntax Highlighting

The extension highlights:

- **Keywords**: `func`, `let`, `if`, `else`, `return`, `assert`
- **Types**: `Int`, `String`, `Bool`, `Void`, `Func`
- **Built-in Functions**: `println`, `readLine`, `toString`
- **Operators**: Arithmetic (`+`, `-`, `*`, `/`, `%`), Comparison (`==`, `!=`, `<`, `>`, `<=`, `>=`), Logical (`&&`, `||`, `!`), Lambda (`=>`) and Function (`->`)
- **Constants**: `true`, `false`
- **Numbers**: Integer and floating-point literals
- **Strings**: String literals with escape sequences
- **Comments**: Single-line (`//`) and block (`/* */`) comments
- **Function definitions and calls**
- **Variable declarations**

## Installation

### From VSIX file

1. Download the `.vsix` file
2. In VS Code, go to Extensions view (Ctrl+Shift+X)
3. Click on the "..." menu and select "Install from VSIX..."
4. Select the downloaded `.vsix` file

### For Development

1. Clone the repository
2. Open the `vscode-gplambda-extension` folder in VS Code
3. Press F5 to run a new VS Code instance with the extension loaded

## Example

```gplambda
// GP-λ example with syntax highlighting
func factorial(n: Int) -> Int {
    if (n <= 1) {
        return 1;
    } else {
        return n * factorial(n - 1);
    }
}

func main() {
    let result = factorial(5);
    println("5! = " + result.toString());
    
    // Lambda expression
    let double = (x: Int) => x * 2;
    assert(double(21) == 42, "Math is broken!");
}
```

## Known Issues

- No language server features yet (intellisense, go to definition, etc.)
- No code formatting support

## Release Notes

### 0.1.0

Initial release of GP-λ language support:
- Basic syntax highlighting
- Bracket matching
- Comment support

## Contributing

Contributions are welcome! Please submit issues and pull requests to the [GP-λ repository](https://github.com/ReinoutWW/ryn-lang-v2).

## License

This extension is licensed under the MIT License.