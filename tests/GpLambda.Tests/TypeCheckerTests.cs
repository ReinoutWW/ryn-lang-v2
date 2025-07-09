using Xunit;
using FluentAssertions;
using GpLambda.Compiler.SemanticAnalysis;
using GpLambda.Compiler.TypeChecking;

namespace GpLambda.Tests;

public interface ITypeChecker
{
    IReadOnlyList<TypeError> Errors { get; }
    bool HasErrors { get; }
}

public class TypeCheckerTests
{
    [Fact]
    public void TypeCheck_ValidProgram_ShouldNotHaveErrors()
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
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeFalse();
    }
    
    [Fact]
    public void TypeCheck_TypeMismatchInAssignment_ShouldReportError()
    {
        // Arrange
        var source = @"
            func main() {
                let x: Int = 5;
                x = ""hello"";
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeTrue();
        typeChecker.Errors.Should().HaveCount(1);
        typeChecker.Errors[0].Message.Should().Contain("Cannot assign value of type String to variable 'x' of type Int");
    }
    
    [Fact]
    public void TypeCheck_TypeMismatchInInitialization_ShouldReportError()
    {
        // Arrange
        var source = @"
            func main() {
                let x: Int = ""hello"";
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        if (!typeChecker.HasErrors)
        {
            typeChecker.HasErrors.Should().BeTrue("Expected type errors but none were found");
        }
        typeChecker.Errors.Should().HaveCount(1);
        typeChecker.Errors[0].Message.Should().Contain("Cannot initialize variable 'x' of type Int with value of type String");
    }
    
    [Fact]
    public void TypeCheck_WrongArgumentType_ShouldReportError()
    {
        // Arrange
        var source = @"
            func add(x: Int, y: Int) -> Int {
                return x + y;
            }
            
            func main() {
                let result = add(5, ""hello"");
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeTrue();
        typeChecker.Errors.Should().HaveCount(1);
        typeChecker.Errors[0].Message.Should().Contain("Argument 2 of function 'add' expects type Int, but got String");
    }
    
    [Fact]
    public void TypeCheck_WrongReturnType_ShouldReportError()
    {
        // Arrange
        var source = @"
            func getNumber() -> Int {
                return ""hello"";
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeTrue();
        typeChecker.Errors.Should().HaveCount(1);
        typeChecker.Errors[0].Message.Should().Contain("Function should return Int, but returns String");
    }
    
    [Fact]
    public void TypeCheck_MissingReturn_ShouldReportError()
    {
        // Arrange
        var source = @"
            func getNumber() -> Int {
                let x = 5;
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeTrue();
        typeChecker.Errors.Should().HaveCount(1);
        typeChecker.Errors[0].Message.Should().Contain("Function 'getNumber' must return a value of type Int");
    }
    
    [Fact]
    public void TypeCheck_ArithmeticOperators_ShouldWorkWithInts()
    {
        // Arrange
        var source = @"
            func main() {
                let a = 5 + 3;
                let b = 10 - 2;
                let c = 4 * 2;
                let d = 8 / 2;
                let e = 7 % 3;
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeFalse();
    }
    
    [Fact]
    public void TypeCheck_StringConcatenation_ShouldWork()
    {
        // Arrange
        var source = @"
            func main() {
                let s1 = ""Hello"" + "" World"";
                let s2 = ""Number: "" + toString(42);
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeFalse();
    }
    
    [Fact]
    public void TypeCheck_ComparisonOperators_ShouldReturnBool()
    {
        // Arrange
        var source = @"
            func main() {
                let a: Bool = 5 < 10;
                let b: Bool = 10 > 5;
                let c: Bool = 5 <= 5;
                let d: Bool = 10 >= 10;
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeFalse();
    }
    
    [Fact]
    public void TypeCheck_EqualityOperators_ShouldWork()
    {
        // Arrange
        var source = @"
            func main() {
                let a: Bool = 5 == 5;
                let b: Bool = ""hello"" == ""hello"";
                let c: Bool = true != false;
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeFalse();
    }
    
    [Fact]
    public void TypeCheck_LogicalOperators_ShouldRequireBools()
    {
        // Arrange
        var source = @"
            func main() {
                let a: Bool = true && false;
                let b: Bool = true || false;
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeFalse();
    }
    
    [Fact]
    public void TypeCheck_LogicalOperatorsWithNonBool_ShouldReportError()
    {
        // Arrange
        var source = @"
            func main() {
                let a = 5 && 10;
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeTrue();
        typeChecker.Errors.Should().HaveCount(1);
        typeChecker.Errors[0].Message.Should().Contain("Operator '&&' requires Bool operands");
    }
    
    [Fact]
    public void TypeCheck_UnaryOperators_ShouldWork()
    {
        // Arrange
        var source = @"
            func main() {
                let a: Int = -5;
                let b: Bool = !true;
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeFalse();
    }
    
    [Fact]
    public void TypeCheck_IfCondition_ShouldRequireBool()
    {
        // Arrange
        var source = @"
            func main() {
                if (5) {
                    println(""hello"");
                }
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeTrue();
        typeChecker.Errors.Should().HaveCount(1);
        typeChecker.Errors[0].Message.Should().Contain("If condition must be of type Bool, but got Int");
    }
    
    [Fact]
    public void TypeCheck_AssertCondition_ShouldRequireBool()
    {
        // Arrange
        var source = @"
            func main() {
                assert(5);
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeTrue();
        typeChecker.Errors.Should().HaveCount(1);
        typeChecker.Errors[0].Message.Should().Contain("Assert condition must be of type Bool, but got Int");
    }
    
    [Fact]
    public void TypeCheck_LambdaReturnType_ShouldBeInferred()
    {
        // Arrange
        var source = @"
            func main() {
                let add = (x: Int, y: Int) => x + y;
                let result: Int = add(5, 3);
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeFalse();
    }
    
    [Fact]
    public void TypeCheck_VoidFunction_ShouldNotRequireReturn()
    {
        // Arrange
        var source = @"
            func printMessage() {
                println(""Hello"");
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeFalse();
    }
    
    [Fact]
    public void TypeCheck_ConditionalReturn_ShouldRequireBothBranches()
    {
        // Arrange
        var source = @"
            func getValue(condition: Bool) -> Int {
                if (condition) {
                    return 5;
                } else {
                    return 10;
                }
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeFalse();
    }
    
    [Fact]
    public void TypeCheck_IncompleteConditionalReturn_ShouldReportError()
    {
        // Arrange
        var source = @"
            func getValue(condition: Bool) -> Int {
                if (condition) {
                    return 5;
                }
            }
        ";
        
        // Act
        var typeChecker = TypeCheckProgram(source);
        
        // Assert
        typeChecker.HasErrors.Should().BeTrue();
        typeChecker.Errors.Should().HaveCount(1);
        typeChecker.Errors[0].Message.Should().Contain("Function 'getValue' must return a value of type Int");
    }
    
    private ITypeChecker TypeCheckProgram(string source)
    {
        var ast = TestHelpers.BuildAst(source);
        var analyzer = new CombinedAnalyzer();
        analyzer.Analyze(ast);
        
        // Create a wrapper that returns the analyzer's errors
        return new TypeCheckerResult(analyzer);
    }
    
    private class TypeCheckerResult : ITypeChecker
    {
        private readonly CombinedAnalyzer _analyzer;
        
        public TypeCheckerResult(CombinedAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }
        
        public IReadOnlyList<TypeError> Errors => _analyzer.TypeErrors;
        public bool HasErrors => _analyzer.TypeErrors.Count > 0;
    }
}