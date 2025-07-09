using Xunit;
using FluentAssertions;
using GpLambda.Compiler.CodeGeneration;
using GpLambda.Compiler.SemanticAnalysis;

namespace GpLambda.Tests;

public class CodeGeneratorTests
{
    [Fact]
    public void GenerateCode_HelloWorld_ShouldGenerateValidCSharp()
    {
        // Arrange
        var source = @"
            func main() {
                println(""Hello, World!"");
            }
        ";
        
        // Act
        var code = GenerateCSharpCode(source);
        
        // Assert
        code.Should().Contain("public static class Program");
        code.Should().Contain("private static void println(string message)");
        code.Should().Contain("private static void main()");
        code.Should().Contain("println(\"Hello, World!\");");
        code.Should().Contain("public static void Main(string[] args)");
        code.Should().Contain("main();");
    }
    
    [Fact]
    public void GenerateCode_Variables_ShouldGenerateDeclarations()
    {
        // Arrange
        var source = @"
            func main() {
                let x: Int = 42;
                let message = ""Hello"";
                let flag: Bool = true;
            }
        ";
        
        // Act
        var code = GenerateCSharpCode(source);
        
        // Assert
        code.Should().Contain("int x = 42;");
        code.Should().Contain("string message = \"Hello\";");
        code.Should().Contain("bool flag = true;");
    }
    
    [Fact]
    public void GenerateCode_FunctionWithParameters_ShouldGenerateCorrectSignature()
    {
        // Arrange
        var source = @"
            func add(x: Int, y: Int) -> Int {
                return x + y;
            }
            
            func main() {
                let result = add(5, 3);
                println(toString(result));
            }
        ";
        
        // Act
        var code = GenerateCSharpCode(source);
        
        // Assert
        code.Should().Contain("private static int add(int x, int y)");
        code.Should().Contain("return (x + y);");
        code.Should().Contain("int result = add(5, 3);");
        code.Should().Contain("println(toString(result));");
    }
    
    [Fact]
    public void GenerateCode_IfStatement_ShouldGenerateConditional()
    {
        // Arrange
        var source = @"
            func main() {
                let x = 5;
                if (x > 0) {
                    println(""Positive"");
                } else {
                    println(""Not positive"");
                }
            }
        ";
        
        // Act
        var code = GenerateCSharpCode(source);
        
        // Assert
        code.Should().Contain("if ((x > 0))");
        code.Should().Contain("println(\"Positive\");");
        code.Should().Contain("else");
        code.Should().Contain("println(\"Not positive\");");
    }
    
    [Fact]
    public void GenerateCode_Operators_ShouldGenerateCorrectOperators()
    {
        // Arrange
        var source = @"
            func main() {
                let a = 10 + 5;
                let b = 10 - 5;
                let c = 10 * 5;
                let d = 10 / 5;
                let e = 10 % 3;
                let f = 10 == 10;
                let g = 10 != 5;
                let h = 10 < 20;
                let i = 10 > 5;
                let j = 10 <= 10;
                let k = 10 >= 10;
                let l = true && false;
                let m = true || false;
                let n = -5;
                let o = !true;
            }
        ";
        
        // Act
        var code = GenerateCSharpCode(source);
        
        // Assert
        code.Should().Contain("int a = (10 + 5);");
        code.Should().Contain("int b = (10 - 5);");
        code.Should().Contain("int c = (10 * 5);");
        code.Should().Contain("int d = (10 / 5);");
        code.Should().Contain("int e = (10 % 3);");
        code.Should().Contain("bool f = (10 == 10);");
        code.Should().Contain("bool g = (10 != 5);");
        code.Should().Contain("bool h = (10 < 20);");
        code.Should().Contain("bool i = (10 > 5);");
        code.Should().Contain("bool j = (10 <= 10);");
        code.Should().Contain("bool k = (10 >= 10);");
        code.Should().Contain("bool l = (true && false);");
        code.Should().Contain("bool m = (true || false);");
        code.Should().Contain("int n = (-5);");
        code.Should().Contain("bool o = (!true);");
    }
    
    [Fact]
    public void GenerateCode_Assert_ShouldGenerateDebugAssert()
    {
        // Arrange
        var source = @"
            func main() {
                let x = 5;
                assert(x > 0);
                assert(x < 10, ""x must be less than 10"");
            }
        ";
        
        // Act
        var code = GenerateCSharpCode(source);
        
        // Assert
        code.Should().Contain("System.Diagnostics.Debug.Assert((x > 0));");
        code.Should().Contain("System.Diagnostics.Debug.Assert((x < 10), \"x must be less than 10\");");
    }
    
    [Fact]
    public void GenerateCode_Lambda_ShouldGenerateLambdaExpression()
    {
        // Arrange
        var source = @"
            func main() {
                let add = (x: Int, y: Int) => x + y;
                let result = add(5, 3);
            }
        ";
        
        // Act
        var code = GenerateCSharpCode(source);
        
        // Assert
        code.Should().Contain("Func<int, int, int> add = (int x, int y) => (x + y);");
        code.Should().Contain("int result = add(5, 3);");
    }
    
    [Fact]
    public void GenerateCode_StringConcatenation_ShouldWork()
    {
        // Arrange
        var source = @"
            func main() {
                let greeting = ""Hello, "" + ""World!"";
                let message = ""The answer is "" + toString(42);
            }
        ";
        
        // Act
        var code = GenerateCSharpCode(source);
        
        // Assert
        code.Should().Contain("string greeting = (\"Hello, \" + \"World!\");");
        code.Should().Contain("string message = (\"The answer is \" + toString(42));");
    }
    
    [Fact]
    public void GenerateCode_VoidFunction_ShouldGenerateVoidMethod()
    {
        // Arrange
        var source = @"
            func printMessage(msg: String) {
                println(msg);
            }
            
            func main() {
                printMessage(""Hello"");
            }
        ";
        
        // Act
        var code = GenerateCSharpCode(source);
        
        // Assert
        code.Should().Contain("private static void printMessage(string msg)");
        code.Should().Contain("println(msg);");
        code.Should().Contain("printMessage(\"Hello\");");
    }
    
    [Fact]
    public void GenerateCode_Assignment_ShouldGenerateAssignment()
    {
        // Arrange
        var source = @"
            func main() {
                let x = 5;
                x = 10;
                x = x + 1;
            }
        ";
        
        // Act
        var code = GenerateCSharpCode(source);
        
        // Assert
        code.Should().Contain("int x = 5;");
        code.Should().Contain("x = 10;");
        code.Should().Contain("x = (x + 1);");
    }
    
    [Fact]
    public void GenerateCode_BuiltInFunctions_ShouldBeGenerated()
    {
        // Arrange
        var source = @"
            func main() {
                println(""Enter your name:"");
                let name = readLine();
                println(""Hello, "" + name);
            }
        ";
        
        // Act
        var code = GenerateCSharpCode(source);
        
        // Assert
        code.Should().Contain("private static void println(string message)");
        code.Should().Contain("Console.WriteLine(message);");
        code.Should().Contain("private static string readLine()");
        code.Should().Contain("return Console.ReadLine() ?? string.Empty;");
        code.Should().Contain("private static string toString(int value)");
        code.Should().Contain("return value.ToString();");
    }
    
    [Fact]
    public void GenerateCode_EscapeSequences_ShouldBePreserved()
    {
        // Arrange
        var source = @"
            func main() {
                println(""Hello\nWorld"");
                println(""Tab:\tIndented"");
                println(""Quote: \""text\"""");
            }
        ";
        
        // Act
        var code = GenerateCSharpCode(source);
        
        // Assert
        code.Should().Contain("println(\"Hello\\nWorld\");");
        code.Should().Contain("println(\"Tab:\\tIndented\");");
        code.Should().Contain("println(\"Quote: \\\"text\\\"\");");
    }
    
    private string GenerateCSharpCode(string source)
    {
        var ast = TestHelpers.BuildAst(source);
        var generator = new CSharpCodeGenerator();
        return generator.GenerateCode(ast);
    }
}