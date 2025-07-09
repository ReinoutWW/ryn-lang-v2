using Xunit;
using FluentAssertions;
using GpLambda.Compiler.SemanticAnalysis;

namespace GpLambda.Tests;

public class SemanticAnalyzerTests
{
    [Fact]
    public void Analyze_ValidProgram_ShouldNotHaveErrors()
    {
        // Arrange
        var source = @"
            func add(x: Int, y: Int) -> Int {
                return x + y;
            }
            
            func main() {
                let a = 5;
                let b = 10;
                let sum = add(a, b);
                println(toString(sum));
            }
        ";
        
        // Act
        var analyzer = AnalyzeProgram(source);
        
        // Assert
        analyzer.HasErrors.Should().BeFalse();
    }
    
    [Fact]
    public void Analyze_DuplicateFunction_ShouldReportError()
    {
        // Arrange
        var source = @"
            func test() {
            }
            
            func test() {
            }
        ";
        
        // Act
        var analyzer = AnalyzeProgram(source);
        
        // Assert
        analyzer.HasErrors.Should().BeTrue();
        analyzer.Errors.Should().HaveCount(1);
        analyzer.Errors[0].Message.Should().Contain("Function 'test' is already defined");
    }
    
    [Fact]
    public void Analyze_DuplicateVariable_ShouldReportError()
    {
        // Arrange
        var source = @"
            func main() {
                let x = 5;
                let x = 10;
            }
        ";
        
        // Act
        var analyzer = AnalyzeProgram(source);
        
        // Assert
        analyzer.HasErrors.Should().BeTrue();
        analyzer.Errors.Should().HaveCount(1);
        analyzer.Errors[0].Message.Should().Contain("Variable 'x' is already defined in this scope");
    }
    
    [Fact]
    public void Analyze_UndefinedVariable_ShouldReportError()
    {
        // Arrange
        var source = @"
            func main() {
                let x = y;
            }
        ";
        
        // Act
        var analyzer = AnalyzeProgram(source);
        
        // Assert
        analyzer.HasErrors.Should().BeTrue();
        analyzer.Errors.Should().HaveCount(1);
        analyzer.Errors[0].Message.Should().Contain("Undefined variable 'y'");
    }
    
    [Fact]
    public void Analyze_UndefinedFunction_ShouldReportError()
    {
        // Arrange
        var source = @"
            func main() {
                foo();
            }
        ";
        
        // Act
        var analyzer = AnalyzeProgram(source);
        
        // Assert
        analyzer.HasErrors.Should().BeTrue();
        analyzer.Errors.Should().HaveCount(1);
        analyzer.Errors[0].Message.Should().Contain("Undefined function 'foo'");
    }
    
    [Fact]
    public void Analyze_WrongArgumentCount_ShouldReportError()
    {
        // Arrange
        var source = @"
            func add(x: Int, y: Int) -> Int {
                return x + y;
            }
            
            func main() {
                add(1);
            }
        ";
        
        // Act
        var analyzer = AnalyzeProgram(source);
        
        // Assert
        analyzer.HasErrors.Should().BeTrue();
        analyzer.Errors.Should().HaveCount(1);
        analyzer.Errors[0].Message.Should().Contain("Function 'add' expects 2 arguments, but 1 were provided");
    }
    
    [Fact]
    public void Analyze_UninitializedVariable_ShouldReportError()
    {
        // Arrange
        var source = @"
            func main() {
                let x: Int;
                let y = x;
            }
        ";
        
        // Act
        var analyzer = AnalyzeProgram(source);
        
        // Assert
        analyzer.HasErrors.Should().BeTrue();
        analyzer.Errors.Should().HaveCount(1);
        analyzer.Errors[0].Message.Should().Contain("Variable 'x' may not be initialized");
    }
    
    [Fact]
    public void Analyze_VariableShadowing_ShouldBeAllowed()
    {
        // Arrange
        var source = @"
            func main() {
                let x = 5;
                {
                    let x = ""hello"";
                    println(x);
                }
                let y = x + 1;
            }
        ";
        
        // Act
        var analyzer = AnalyzeProgram(source);
        
        // Assert
        analyzer.HasErrors.Should().BeFalse();
    }
    
    [Fact]
    public void Analyze_LambdaParameters_ShouldCreateScope()
    {
        // Arrange
        var source = @"
            func main() {
                let add = (x: Int, y: Int) => x + y;
                let result = add(1, 2);
            }
        ";
        
        // Act
        var analyzer = AnalyzeProgram(source);
        
        // Assert
        if (analyzer.HasErrors)
        {
            var errors = string.Join("\n", analyzer.Errors.Select(e => $"{e.Line}:{e.Column} - {e.Message}"));
            analyzer.HasErrors.Should().BeFalse($"Unexpected errors:\n{errors}");
        }
        else
        {
            analyzer.HasErrors.Should().BeFalse();
        }
    }
    
    [Fact]
    public void Analyze_AssignmentToFunction_ShouldReportError()
    {
        // Arrange
        var source = @"
            func foo() {
            }
            
            func main() {
                foo = 5;
            }
        ";
        
        // Act
        var analyzer = AnalyzeProgram(source);
        
        // Assert
        analyzer.HasErrors.Should().BeTrue();
        analyzer.Errors.Should().HaveCount(1);
        analyzer.Errors[0].Message.Should().Contain("'foo' is not a variable");
    }
    
    [Fact]
    public void Analyze_BuiltInFunctions_ShouldBeAvailable()
    {
        // Arrange
        var source = @"
            func main() {
                println(""Hello"");
                let input = readLine();
                let num = 42;
                println(toString(num));
            }
        ";
        
        // Act
        var analyzer = AnalyzeProgram(source);
        
        // Assert
        analyzer.HasErrors.Should().BeFalse();
    }
    
    private SemanticAnalyzer AnalyzeProgram(string source)
    {
        var ast = TestHelpers.BuildAst(source);
        var analyzer = new SemanticAnalyzer();
        analyzer.Analyze(ast);
        return analyzer;
    }
}