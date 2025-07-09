using Xunit;
using FluentAssertions;
using System.IO;
using System.Threading.Tasks;
using GpLambda.Compiler.Grammar;
using GpLambda.Compiler.AST;
using GpLambda.Compiler.CodeGeneration;
using Antlr4.Runtime;

namespace GpLambda.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task CompileAndRun_HelloWorld_ShouldWork()
    {
        // Arrange
        var source = @"
            func main() {
                println(""Hello, World!"");
            }
        ";
        
        // Act
        var csharpCode = CompileToCs(source);
        var output = await CompileAndRunCs(csharpCode);
        
        // Assert
        output.Should().Be("Hello, World!\n");
    }
    
    [Fact]
    public async Task CompileAndRun_Variables_ShouldWork()
    {
        // Arrange
        var source = @"
            func main() {
                let x = 10;
                let y = 20;
                let sum = x + y;
                println(toString(sum));
            }
        ";
        
        // Act
        var csharpCode = CompileToCs(source);
        var output = await CompileAndRunCs(csharpCode);
        
        // Assert
        output.Should().Be("30\n");
    }
    
    [Fact]
    public async Task CompileAndRun_Functions_ShouldWork()
    {
        // Arrange
        var source = @"
            func add(x: Int, y: Int) -> Int {
                return x + y;
            }
            
            func main() {
                let result = add(15, 25);
                println(toString(result));
            }
        ";
        
        // Act
        var csharpCode = CompileToCs(source);
        var output = await CompileAndRunCs(csharpCode);
        
        // Assert
        output.Should().Be("40\n");
    }
    
    [Fact]
    public async Task CompileAndRun_StringConcatenation_ShouldWork()
    {
        // Arrange
        var source = @"
            func main() {
                let name = ""GP-λ"";
                let greeting = ""Hello, "" + name + ""!"";
                println(greeting);
            }
        ";
        
        // Act
        var csharpCode = CompileToCs(source);
        var output = await CompileAndRunCs(csharpCode);
        
        // Assert
        output.Should().Be("Hello, GP-λ!\n");
    }
    
    [Fact]
    public async Task CompileAndRun_Conditionals_ShouldWork()
    {
        // Arrange
        var source = @"
            func main() {
                let x = 10;
                if (x > 5) {
                    println(""x is greater than 5"");
                } else {
                    println(""x is not greater than 5"");
                }
            }
        ";
        
        // Act
        var csharpCode = CompileToCs(source);
        var output = await CompileAndRunCs(csharpCode);
        
        // Assert
        output.Should().Be("x is greater than 5\n");
    }
    
    [Fact]
    public async Task CompileAndRun_Lambdas_ShouldWork()
    {
        // Arrange
        var source = @"
            func main() {
                let add = (x: Int, y: Int) => x + y;
                let result = add(7, 3);
                println(toString(result));
            }
        ";
        
        // Act
        var csharpCode = CompileToCs(source);
        var output = await CompileAndRunCs(csharpCode);
        
        // Assert
        output.Should().Be("10\n");
    }
    
    [Fact]
    public async Task CompileAndRun_BooleanOperations_ShouldWork()
    {
        // Arrange
        var source = @"
            func main() {
                let a = true;
                let b = false;
                let c = a && b;
                let d = a || b;
                let e = !b;
                
                if (c) {
                    println(""c is true"");
                } else {
                    println(""c is false"");
                }
                
                if (d) {
                    println(""d is true"");
                } else {
                    println(""d is false"");
                }
                
                if (e) {
                    println(""e is true"");
                } else {
                    println(""e is false"");
                }
            }
        ";
        
        // Act
        var csharpCode = CompileToCs(source);
        var output = await CompileAndRunCs(csharpCode);
        
        // Assert
        output.Should().Be("c is false\nd is true\ne is true\n");
    }
    
    [Fact]
    public async Task CompileAndRun_MethodCalls_ShouldWork()
    {
        // Arrange
        var source = @"
            func main() {
                let x = 42;
                let str = x.toString();
                println(""The answer is: "" + str);
            }
        ";
        
        // Act
        var csharpCode = CompileToCs(source);
        var output = await CompileAndRunCs(csharpCode);
        
        // Assert
        output.Should().Be("The answer is: 42\n");
    }
    
    private string CompileToCs(string source)
    {
        var inputStream = new AntlrInputStream(source);
        var lexer = new GpLambdaLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new GpLambdaParser(tokenStream);
        
        var parseTree = parser.program();
        parser.NumberOfSyntaxErrors.Should().Be(0);
        
        var astBuilder = new AstBuilder();
        var ast = (GpLambda.Compiler.AST.Nodes.ProgramNode)astBuilder.Visit(parseTree)!;
        
        var codeGenerator = new CSharpCodeGenerator();
        return codeGenerator.GenerateCode(ast);
    }
    
    private async Task<string> CompileAndRunCs(string csharpCode)
    {
        // Create a temporary directory
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Write C# code to file
            var csFile = Path.Combine(tempDir, "Program.cs");
            await File.WriteAllTextAsync(csFile, csharpCode);
            
            // Create a simple project file
            var projFile = Path.Combine(tempDir, "temp.csproj");
            await File.WriteAllTextAsync(projFile, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>");
            
            // Compile and run
            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "dotnet";
            process.StartInfo.Arguments = "run --project " + projFile;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.WorkingDirectory = tempDir;
            
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            if (process.ExitCode != 0)
            {
                throw new Exception($"Compilation failed: {error}");
            }
            
            return output;
        }
        finally
        {
            // Clean up
            Directory.Delete(tempDir, true);
        }
    }
}