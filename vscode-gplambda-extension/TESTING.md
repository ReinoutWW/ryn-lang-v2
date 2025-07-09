# Testing the GP-λ VS Code Extension

## Quick Start

1. Open VS Code
2. Open the `vscode-gplambda-extension` folder in VS Code
3. Press `F5` to launch a new VS Code window with the extension loaded
4. In the new window, open the `sample.gpl` file or create a new file with `.gpl` or `.gplambda` extension
5. You should see syntax highlighting applied to GP-λ code

## What to Test

### Syntax Highlighting
Verify that the following elements are highlighted correctly:

- **Keywords** (should be blue/purple): `func`, `let`, `if`, `else`, `return`, `assert`
- **Types** (should be teal/green): `Int`, `String`, `Bool`, `Void`, `Func`
- **Built-in functions** (should be yellow): `println`, `readLine`, `toString`
- **String literals** (should be orange/red): `"Hello, World!"`
- **Numbers** (should be light green): `42`, `3.14`
- **Boolean constants** (should be blue): `true`, `false`
- **Comments** (should be gray/green): `// comment` and `/* block comment */`
- **Operators**: `+`, `-`, `*`, `/`, `==`, `!=`, `&&`, `||`, `=>`, `->`

### Language Features
Test these language configuration features:

1. **Auto-closing brackets**: Type `{`, `[`, `(`, or `"` and verify they auto-close
2. **Comment toggling**: Select lines and press `Ctrl+/` (or `Cmd+/` on Mac)
3. **Bracket matching**: Click on a bracket to see its matching pair highlighted
4. **Code folding**: Look for fold indicators next to function definitions

## Building the Extension

To create a packaged extension (.vsix file):

1. Install vsce (Visual Studio Code Extension manager):
   ```bash
   npm install -g vsce
   ```

2. From the extension directory, run:
   ```bash
   vsce package
   ```

3. This will create a `.vsix` file that can be installed in VS Code

## Installing the Extension

To install the packaged extension:

1. In VS Code, open the Extensions view (Ctrl+Shift+X)
2. Click on the "..." menu at the top of the Extensions view
3. Select "Install from VSIX..."
4. Browse to and select the `.vsix` file

## Known Limitations

- No IntelliSense support (auto-completion, hover information)
- No error checking or diagnostics
- No code formatting
- No go-to-definition or find-references

These features would require implementing a Language Server Protocol (LSP) server.