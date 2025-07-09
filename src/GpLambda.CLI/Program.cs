using System.IO;
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
        Console.WriteLine("GP-λ compiler - transpiles GP-λ source code to C#");
        Console.WriteLine();
        Console.WriteLine("Usage: gplambda --input <file> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -i, --input <file>    The GP-λ source file to compile (required)");
        Console.WriteLine("  -o, --output <file>   The output C# file (defaults to input filename with .cs extension)");
        Console.WriteLine("  -v, --verbose         Enable verbose output");
        Console.WriteLine("  -h, --help            Show this help message");
    }

    static async Task<int> CompileAsync(FileInfo inputFile, FileInfo? outputFile, bool verbose)
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