using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Text;
using GpLambda.Compiler;
using GpLambda.Compiler.CodeGeneration;
using GpLambda.Compiler.Grammar;
using GpLambda.Compiler.AST;
using GpLambda.Compiler.AST.Nodes;
using Antlr4.Runtime;

namespace GpLambda.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return 0;
        }

        // Check for init command
        if (args[0] == "init")
        {
            return await InitializeProject(args.Skip(1).ToArray());
        }

        // Check for build command
        if (args[0] == "build")
        {
            return await BuildProject(args.Skip(1).ToArray());
        }

        // Check for run command
        if (args[0] == "run")
        {
            return await RunProject(args.Skip(1).ToArray());
        }

        // Default to compile mode for backward compatibility
        string? inputFile = null;
        string? outputFile = null;
        bool verbose = false;

        // Parse arguments
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--input":
                case "-i":
                    if (i + 1 < args.Length)
                        inputFile = args[++i];
                    break;
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                        outputFile = args[++i];
                    break;
                case "--verbose":
                case "-v":
                    verbose = true;
                    break;
            }
        }

        if (string.IsNullOrEmpty(inputFile))
        {
            Console.Error.WriteLine("Error: Input file is required.");
            PrintUsage();
            return 1;
        }

        var inputFileInfo = new FileInfo(inputFile);
        var outputFileInfo = outputFile != null ? new FileInfo(outputFile) : null;

        return await CompileAsync(inputFileInfo, outputFileInfo, verbose);
    }

    static void PrintUsage()
    {
        Console.WriteLine("GP-λ compiler and runtime");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  gpl [command] [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  init <name>           Initialize a new GP-λ project");
        Console.WriteLine("  build                 Build all .gpl files in the current project");
        Console.WriteLine("  run [file]            Build and run a GP-λ file or project");
        Console.WriteLine("  <file>                Compile a single file (default)");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -i, --input <file>    The GP-λ source file to compile");
        Console.WriteLine("  -o, --output <file>   The output C# file (defaults to input filename with .cs extension)");
        Console.WriteLine("  -v, --verbose         Enable verbose output");
        Console.WriteLine("  -h, --help            Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  gpl init myproject");
        Console.WriteLine("  gpl build");
        Console.WriteLine("  gpl run");
        Console.WriteLine("  gpl run examples/hello.gpl");
        Console.WriteLine("  gpl hello.gpl");
    }

    static async Task<int> InitializeProject(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Error: Project name is required.");
            Console.WriteLine("Usage: gpl init <project-name>");
            return 1;
        }

        var projectName = args[0];
        var projectDir = Path.Combine(Directory.GetCurrentDirectory(), projectName);

        try
        {
            // Create project directory
            Directory.CreateDirectory(projectDir);
            Console.WriteLine($"Creating GP-λ project '{projectName}'...");

            // Create project structure
            Directory.CreateDirectory(Path.Combine(projectDir, "src"));
            Directory.CreateDirectory(Path.Combine(projectDir, "build"));
            Directory.CreateDirectory(Path.Combine(projectDir, ".vscode"));

            // Create gplambda.json project file
            var projectConfig = new
            {
                name = projectName,
                version = "0.1.0",
                description = $"A GP-λ project",
                main = "src/main.gpl",
                output = "build",
                author = "",
                dependencies = new { }
            };

            var projectJson = JsonSerializer.Serialize(projectConfig, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(Path.Combine(projectDir, "gplambda.json"), projectJson);

            // Create main.gpl
            var mainContent = @"// Entry point for the GP-λ application
func main() {
    println(""Hello from GP-λ!"");
    println(""Welcome to " + projectName + @" project"");
}
";
            await File.WriteAllTextAsync(Path.Combine(projectDir, "src", "main.gpl"), mainContent);

            // Create .gitignore
            var gitignoreContent = @"# Build output
build/
*.cs

# VS Code
.vscode/*
!.vscode/settings.json
!.vscode/extensions.json

# OS files
.DS_Store
Thumbs.db
";
            await File.WriteAllTextAsync(Path.Combine(projectDir, ".gitignore"), gitignoreContent);

            // Create README.md
            var readmeContent = $@"# {projectName}

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
";
            await File.WriteAllTextAsync(Path.Combine(projectDir, "README.md"), readmeContent);

            // Create VS Code settings
            var vscodeSettings = @"{
    ""files.associations"": {
        ""*.gpl"": ""gplambda"",
        ""*.gplambda"": ""gplambda""
    },
    ""editor.formatOnSave"": false
}";
            await File.WriteAllTextAsync(Path.Combine(projectDir, ".vscode", "settings.json"), vscodeSettings);

            // Create VS Code extensions recommendations
            var vscodeExtensions = @"{
    ""recommendations"": [
        ""gplambda-team.gplambda""
    ]
}";
            await File.WriteAllTextAsync(Path.Combine(projectDir, ".vscode", "extensions.json"), vscodeExtensions);

            Console.WriteLine($"✓ Created project directory: {projectName}/");
            Console.WriteLine($"✓ Created source directory: {projectName}/src/");
            Console.WriteLine($"✓ Created build directory: {projectName}/build/");
            Console.WriteLine($"✓ Created main source file: {projectName}/src/main.gpl");
            Console.WriteLine($"✓ Created project file: {projectName}/gplambda.json");
            Console.WriteLine($"✓ Created README.md");
            Console.WriteLine($"✓ Created .gitignore");
            Console.WriteLine($"✓ Created VS Code configuration");
            Console.WriteLine();
            Console.WriteLine($"Project '{projectName}' has been initialized successfully!");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine($"  cd {projectName}");
            Console.WriteLine("  gpl run");
            Console.WriteLine();

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error initializing project: {ex.Message}");
            return 1;
        }
    }

    static async Task<int> BuildProject(string[] args)
    {
        bool verbose = args.Contains("-v") || args.Contains("--verbose");

        try
        {
            // Check if gplambda.json exists
            var projectFile = Path.Combine(Directory.GetCurrentDirectory(), "gplambda.json");
            if (!File.Exists(projectFile))
            {
                Console.Error.WriteLine("Error: No gplambda.json found in current directory.");
                Console.Error.WriteLine("Run 'gpl init' to create a new project.");
                return 1;
            }

            // Read project configuration
            var projectJson = await File.ReadAllTextAsync(projectFile);
            var projectConfig = JsonSerializer.Deserialize<JsonElement>(projectJson);
            
            var outputDir = projectConfig.GetProperty("output").GetString() ?? "build";
            var srcDir = "src";

            // Ensure output directory exists
            Directory.CreateDirectory(outputDir);

            // Find all .gpl files in src directory
            var gplFiles = Directory.GetFiles(srcDir, "*.gpl", SearchOption.AllDirectories);

            if (gplFiles.Length == 0)
            {
                Console.WriteLine("No .gpl files found in src directory.");
                return 0;
            }

            if (!args.Contains("--silent"))
            {
                Console.WriteLine($"Building {gplFiles.Length} file(s)...");
            }

            var success = true;
            foreach (var gplFile in gplFiles)
            {
                var relativePath = Path.GetRelativePath(srcDir, gplFile);
                var outputPath = Path.Combine(outputDir, Path.ChangeExtension(relativePath, ".cs"));
                
                // Ensure output subdirectory exists
                var outputSubDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputSubDir))
                {
                    Directory.CreateDirectory(outputSubDir);
                }

                if (!args.Contains("--silent"))
                {
                    Console.Write($"  Compiling {relativePath}... ");
                }

                var inputFileInfo = new FileInfo(gplFile);
                var outputFileInfo = new FileInfo(outputPath);

                // Compile the file
                var result = await CompileFileAsync(inputFileInfo, outputFileInfo, verbose);
                
                if (result == 0)
                {
                    if (!args.Contains("--silent"))
                    {
                        Console.WriteLine("✓");
                    }
                }
                else
                {
                    if (!args.Contains("--silent"))
                    {
                        Console.WriteLine("✗");
                    }
                    success = false;
                }
            }

            if (success)
            {
                if (!args.Contains("--silent"))
                {
                    Console.WriteLine();
                    Console.WriteLine("Build succeeded!");
                }
                return 0;
            }
            else
            {
                if (!args.Contains("--silent"))
                {
                    Console.WriteLine();
                    Console.WriteLine("Build failed.");
                }
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during build: {ex.Message}");
            return 1;
        }
    }

    static async Task<int> RunProject(string[] args)
    {
        bool verbose = false;
        string? targetFile = null;
        
        // Parse arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-v" || args[i] == "--verbose")
            {
                verbose = true;
            }
            else if (!args[i].StartsWith("-") && targetFile == null)
            {
                targetFile = args[i];
            }
        }

        try
        {
            // If a specific file is provided, run just that file
            if (!string.IsNullOrEmpty(targetFile))
            {
                return await RunSingleFile(targetFile, verbose);
            }

            // Otherwise, check if we're in a project directory
            var projectFile = Path.Combine(Directory.GetCurrentDirectory(), "gplambda.json");
            if (File.Exists(projectFile))
            {
                return await RunProjectMain(verbose);
            }
            
            // If no project and no file specified, show error
            Console.Error.WriteLine("Error: No file specified and no gplambda.json found in current directory.");
            Console.Error.WriteLine("Usage: gpl run [file] or gpl run (in a project directory)");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return 1;
        }
    }

    static async Task<int> RunSingleFile(string gplFile, bool verbose)
    {
        if (!File.Exists(gplFile))
        {
            Console.Error.WriteLine($"Error: File '{gplFile}' not found.");
            return 1;
        }

        var tempDir = Path.Combine(Path.GetTempPath(), $"gpl_{Path.GetRandomFileName()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Compile the GP-λ file
            var inputFile = new FileInfo(gplFile);
            var csFile = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(gplFile) + ".cs");
            var outputFile = new FileInfo(csFile);

            if (verbose)
            {
                Console.WriteLine($"Compiling {gplFile}...");
            }

            var compileResult = await CompileFileAsync(inputFile, outputFile, verbose);
            if (compileResult != 0)
            {
                return compileResult;
            }

            // Create a temporary C# project and run it
            return await RunCSharpFile(csFile, tempDir, verbose);
        }
        finally
        {
            // Clean up temp directory
            try 
            { 
                Directory.Delete(tempDir, true); 
            }
            catch { }
        }
    }

    static async Task<int> RunProjectMain(bool verbose)
    {
        // First build the project
        if (verbose)
        {
            Console.WriteLine("Building project...");
        }

        var buildArgs = new List<string> { "--silent" };
        if (verbose)
        {
            buildArgs.Add("--verbose");
        }
        var buildResult = await BuildProject(buildArgs.ToArray());
        if (buildResult != 0)
        {
            return buildResult;
        }

        // Read project config to find main file
        var projectJson = await File.ReadAllTextAsync("gplambda.json");
        var projectConfig = JsonSerializer.Deserialize<JsonElement>(projectJson);
        
        var mainFile = projectConfig.GetProperty("main").GetString() ?? "src/main.gpl";
        var outputDir = projectConfig.GetProperty("output").GetString() ?? "build";
        
        // Convert main .gpl path to .cs path
        var mainCs = Path.Combine(outputDir, Path.GetRelativePath("src", mainFile));
        mainCs = Path.ChangeExtension(mainCs, ".cs");

        if (!File.Exists(mainCs))
        {
            Console.Error.WriteLine($"Error: Main file not found: {mainCs}");
            return 1;
        }

        // Create a temporary directory for the .NET project
        var tempDir = Path.Combine(Path.GetTempPath(), $"gpl_{Path.GetRandomFileName()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Copy all generated C# files
            foreach (var csFile in Directory.GetFiles(outputDir, "*.cs", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(outputDir, csFile);
                var destPath = Path.Combine(tempDir, relativePath);
                var destDir = Path.GetDirectoryName(destPath);
                if (destDir != null)
                {
                    Directory.CreateDirectory(destDir);
                }
                File.Copy(csFile, destPath);
            }

            // Run the main file
            var mainCsInTemp = Path.Combine(tempDir, Path.GetRelativePath(outputDir, mainCs));
            return await RunCSharpFile(mainCsInTemp, tempDir, verbose);
        }
        finally
        {
            // Clean up temp directory
            try 
            { 
                Directory.Delete(tempDir, true); 
            }
            catch { }
        }
    }

    static async Task<int> RunCSharpFile(string csFile, string workingDir, bool verbose)
    {
        // Create a temporary .NET project
        var projectFile = Path.Combine(workingDir, "temp.csproj");
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";
        await File.WriteAllTextAsync(projectFile, projectContent);

        // Run the project
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --no-build-dependencies",
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            RedirectStandardInput = false
        };

        if (!verbose)
        {
            startInfo.EnvironmentVariables["DOTNET_NOLOGO"] = "1";
        }

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            Console.Error.WriteLine("Error: Failed to start dotnet process.");
            return 1;
        }

        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    static async Task<int> CompileAsync(FileInfo inputFile, FileInfo? outputFile, bool verbose)
    {
        return await CompileFileAsync(inputFile, outputFile, verbose);
    }

    static async Task<int> CompileFileAsync(FileInfo inputFile, FileInfo? outputFile, bool verbose)
    {
        try
        {
            // Validate input file
            if (!inputFile.Exists)
            {
                Console.Error.WriteLine($"Error: Input file '{inputFile.FullName}' does not exist.");
                return 1;
            }

            // Determine output file
            if (outputFile == null)
            {
                var outputPath = Path.ChangeExtension(inputFile.FullName, ".cs");
                outputFile = new FileInfo(outputPath);
            }

            if (verbose)
            {
                Console.WriteLine($"Compiling '{inputFile.Name}' to '{outputFile.Name}'...");
            }

            // Read source code
            var sourceCode = await File.ReadAllTextAsync(inputFile.FullName);

            // Compile
            var (success, csharpCode, errors) = Compile(sourceCode, verbose);

            if (!success)
            {
                if (!verbose) Console.WriteLine(); // New line before errors
                Console.Error.WriteLine("Compilation failed with errors:");
                foreach (var error in errors)
                {
                    Console.Error.WriteLine($"  {error}");
                }
                return 1;
            }

            // Write output
            await File.WriteAllTextAsync(outputFile.FullName, csharpCode);

            if (verbose)
            {
                Console.WriteLine($"Successfully compiled to '{outputFile.FullName}'");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return 1;
        }
    }

    static (bool success, string csharpCode, List<string> errors) Compile(string sourceCode, bool verbose)
    {
        var errors = new List<string>();

        try
        {
            // Step 1: Lex and parse
            if (verbose) Console.WriteLine("Parsing...");
            
            var inputStream = new AntlrInputStream(sourceCode);
            var lexer = new GpLambdaLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new GpLambdaParser(tokenStream);

            // Add error listener
            parser.RemoveErrorListeners();
            var errorListener = new ErrorListener(errors);
            parser.AddErrorListener(errorListener);

            var parseTree = parser.program();

            if (parser.NumberOfSyntaxErrors > 0)
            {
                return (false, "", errors);
            }

            // Step 2: Build AST
            if (verbose) Console.WriteLine("Building AST...");
            
            var astBuilder = new AstBuilder();
            var ast = (ProgramNode?)astBuilder.Visit(parseTree);

            if (ast == null)
            {
                errors.Add("Failed to build AST");
                return (false, "", errors);
            }

            // Step 3: Generate C# code
            if (verbose) Console.WriteLine("Generating C# code...");
            
            var codeGenerator = new CSharpCodeGenerator();
            
            try
            {
                var csharpCode = codeGenerator.GenerateCode(ast);
                return (true, csharpCode, errors);
            }
            catch (InvalidOperationException ex)
            {
                // Semantic or type errors
                errors.Add(ex.Message);
                
                // Get detailed errors from analyzer
                var analyzer = new GpLambda.Compiler.SemanticAnalysis.CombinedAnalyzer();
                analyzer.Analyze(ast);
                
                foreach (var error in analyzer.SemanticErrors)
                {
                    errors.Add($"[{error.Line}:{error.Column}] Semantic error: {error.Message}");
                }
                
                foreach (var error in analyzer.TypeErrors)
                {
                    errors.Add($"[{error.Line}:{error.Column}] Type error: {error.Message}");
                }
                
                return (false, "", errors);
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Internal compiler error: {ex.Message}");
            return (false, "", errors);
        }
    }

    private class ErrorListener : Antlr4.Runtime.BaseErrorListener
    {
        private readonly List<string> _errors;

        public ErrorListener(List<string> errors)
        {
            _errors = errors;
        }

        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, 
            int line, int charPositionInLine, string msg, RecognitionException e)
        {
            _errors.Add($"[{line}:{charPositionInLine}] Syntax error: {msg}");
        }
    }
}