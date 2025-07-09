using FluentAssertions;
using Xunit;

namespace GpLambda.Tests;

public class ParserTests
{
    [Fact]
    public void ParseEmptyProgram_ShouldSucceed()
    {
        // Arrange
        var source = "";
        
        // Act
        var parseTree = TestHelpers.ParseProgram(source);
        
        // Assert
        parseTree.Should().NotBeNull();
        parseTree.declaration().Should().BeEmpty();
    }
    
    [Fact]
    public void ParseSimpleFunction_ShouldSucceed()
    {
        // Arrange
        var source = @"
            func main() {
                return;
            }
        ";
        
        // Act
        var parseTree = TestHelpers.ParseProgram(source);
        
        // Assert
        parseTree.Should().NotBeNull();
        parseTree.declaration().Should().HaveCount(1);
        var funcDecl = parseTree.declaration(0).functionDecl();
        funcDecl.Should().NotBeNull();
        funcDecl.ID().GetText().Should().Be("main");
        funcDecl.paramList().Should().BeNull();
        funcDecl.returnType().Should().BeNull();
        funcDecl.block().Should().NotBeNull();
    }
    
    [Fact]
    public void ParseFunctionWithParameters_ShouldSucceed()
    {
        // Arrange
        var source = @"
            func add(x: Int, y: Int) -> Int {
                return x + y;
            }
        ";
        
        // Act
        var parseTree = TestHelpers.ParseProgram(source);
        
        // Assert
        var funcDecl = parseTree.declaration(0).functionDecl();
        funcDecl.ID().GetText().Should().Be("add");
        
        var paramList = funcDecl.paramList();
        paramList.Should().NotBeNull();
        paramList.param().Should().HaveCount(2);
        
        paramList.param(0).ID().GetText().Should().Be("x");
        paramList.param(0).type().GetText().Should().Be("Int");
        
        paramList.param(1).ID().GetText().Should().Be("y");
        paramList.param(1).type().GetText().Should().Be("Int");
        
        funcDecl.returnType().type().GetText().Should().Be("Int");
    }
    
    [Fact]
    public void ParseVariableDeclarations_ShouldSucceed()
    {
        // Arrange
        var source = @"
            func test() {
                let x = 42;
                let y: String = ""hello"";
                let z: Bool = true;
            }
        ";
        
        // Act
        var parseTree = TestHelpers.ParseProgram(source);
        
        // Assert
        var block = parseTree.declaration(0).functionDecl().block();
        block.statement().Should().HaveCount(3);
        
        // Check each variable declaration
        var varDecl1 = block.statement(0).varDecl();
        varDecl1.ID().GetText().Should().Be("x");
        varDecl1.type().Should().BeNull();
        varDecl1.expr().Should().NotBeNull();
        
        var varDecl2 = block.statement(1).varDecl();
        varDecl2.ID().GetText().Should().Be("y");
        varDecl2.type().GetText().Should().Be("String");
        
        var varDecl3 = block.statement(2).varDecl();
        varDecl3.ID().GetText().Should().Be("z");
        varDecl3.type().GetText().Should().Be("Bool");
    }
    
    [Fact]
    public void ParseIfStatement_ShouldSucceed()
    {
        // Arrange
        var source = @"
            func test() {
                if (x > 0) {
                    println(""positive"");
                } else {
                    println(""not positive"");
                }
            }
        ";
        
        // Act
        var parseTree = TestHelpers.ParseProgram(source);
        
        // Assert
        var ifStmt = parseTree.declaration(0).functionDecl().block().statement(0).ifStmt();
        ifStmt.Should().NotBeNull();
        ifStmt.expr().Should().NotBeNull();
        ifStmt.block().Should().HaveCount(2); // then and else blocks
    }
    
    [Fact]
    public void ParseAssertStatement_ShouldSucceed()
    {
        // Arrange
        var source = @"
            func test() {
                assert(x > 0);
                assert(y != 0, ""y must not be zero"");
            }
        ";
        
        // Act
        var parseTree = TestHelpers.ParseProgram(source);
        
        // Assert
        var block = parseTree.declaration(0).functionDecl().block();
        
        var assert1 = block.statement(0).assertStmt();
        assert1.expr().Should().NotBeNull();
        assert1.STRING().Should().BeNull();
        
        var assert2 = block.statement(1).assertStmt();
        assert2.expr().Should().NotBeNull();
        assert2.STRING().Should().NotBeNull();
        assert2.STRING().GetText().Should().Be("\"y must not be zero\"");
    }
    
    [Fact]
    public void ParseLambdaExpressions_ShouldSucceed()
    {
        // Arrange
        var source = @"
            func test() {
                let inc = (x: Int) => x + 1;
                let add = (x: Int, y: Int) => x + y;
                let complex = (x: Int) => {
                    let temp = x * 2;
                    return temp + 1;
                };
            }
        ";
        
        // Act
        var parseTree = TestHelpers.ParseProgram(source);
        
        // Assert
        var block = parseTree.declaration(0).functionDecl().block();
        block.statement().Should().HaveCount(3);
        
        // All three should be variable declarations with lambda expressions
        block.statement(0).varDecl().expr().Should().NotBeNull();
        block.statement(1).varDecl().expr().Should().NotBeNull();
        block.statement(2).varDecl().expr().Should().NotBeNull();
    }
    
    [Fact]
    public void ParseBinaryExpressions_ShouldSucceed()
    {
        // Arrange
        var source = @"
            func test() {
                let a = 1 + 2 * 3;
                let b = (1 + 2) * 3;
                let c = x < y && y <= z;
                let d = a == b || c != d;
            }
        ";
        
        // Act
        var parseTree = TestHelpers.ParseProgram(source);
        
        // Assert
        parseTree.Should().NotBeNull();
        var block = parseTree.declaration(0).functionDecl().block();
        block.statement().Should().HaveCount(4);
    }
    
    [Fact]
    public void ParseFunctionCalls_ShouldSucceed()
    {
        // Arrange
        var source = @"
            func test() {
                println(""Hello"");
                let result = add(1, 2);
                doSomething();
            }
        ";
        
        // Act
        var parseTree = TestHelpers.ParseProgram(source);
        
        // Assert
        var block = parseTree.declaration(0).functionDecl().block();
        block.statement().Should().HaveCount(3);
        
        // First is expression statement with call
        block.statement(0).exprStmt().Should().NotBeNull();
        
        // Second is variable declaration with call
        block.statement(1).varDecl().expr().Should().NotBeNull();
        
        // Third is expression statement with call
        block.statement(2).exprStmt().Should().NotBeNull();
    }
    
    [Fact]
    public void ParseFunctionType_ShouldSucceed()
    {
        // Arrange
        var source = @"
            func test(f: Func<Int, Int>) -> Func<Int, String> {
                return (x: Int) => x.toString();
            }
        ";
        
        // Act
        var parseTree = TestHelpers.ParseProgram(source);
        
        // Assert
        var funcDecl = parseTree.declaration(0).functionDecl();
        funcDecl.paramList().param(0).type().GetText().Should().Be("Func<Int,Int>");
        funcDecl.returnType().type().GetText().Should().Be("Func<Int,String>");
    }
    
    [Theory]
    [InlineData("func () {}", "Line 1:5 - missing ID at '('")]
    [InlineData("func test {}", "Line 1:10 - mismatched input '{' expecting '('")]
    [InlineData("let x = ;", "Line 1:8 - mismatched input ';' expecting {'true', 'false', '-', '!', '(', ID, INT, STRING}")]
    [InlineData("if x > 0 {}", "Line 1:3 - missing '(' at 'x'")]
    public void ParseInvalidSyntax_ShouldThrowParseException(string source, string expectedError)
    {
        // Act & Assert
        var action = () => TestHelpers.ParseProgram(source);
        action.Should().Throw<ParseException>()
            .WithMessage($"*{expectedError}*");
    }
    
    [Fact]
    public void ParseComplexProgram_ShouldSucceed()
    {
        // Arrange
        var source = @"
// Hello World example in GP-λ

func main() {
    println(""Hello, World!"");
    
    // Using variables
    let message: String = ""Welcome to GP-λ!"";
    println(message);
    
    // Using expressions
    let x = 5;
    let y = 10;
    let sum = x + y;
    println(""5 + 10 = "" + sum.toString());
    
    // Using assertions
    assert(sum == 15, ""Math is broken!"");
}";
        
        // Act
        var parseTree = TestHelpers.ParseProgram(source);
        
        // Assert
        parseTree.Should().NotBeNull();
        parseTree.declaration().Should().HaveCount(1);
        
        var mainFunc = parseTree.declaration(0).functionDecl();
        mainFunc.ID().GetText().Should().Be("main");
        mainFunc.block().statement().Should().NotBeEmpty();
    }
}