using FluentAssertions;
using GpLambda.Compiler.AST.Nodes;
using Xunit;

namespace GpLambda.Tests;

public class AstBuilderTests
{
    [Fact]
    public void BuildAst_EmptyProgram_ShouldCreateEmptyProgramNode()
    {
        // Arrange
        var source = "";
        
        // Act
        var ast = TestHelpers.BuildAst(source);
        
        // Assert
        ast.Should().NotBeNull();
        ast.Should().BeOfType<ProgramNode>();
        ast.Declarations.Should().BeEmpty();
    }
    
    [Fact]
    public void BuildAst_SimpleFunction_ShouldCreateFunctionNode()
    {
        // Arrange
        var source = @"
            func greet(name: String) -> String {
                return ""Hello, "" + name;
            }
        ";
        
        // Act
        var ast = TestHelpers.BuildAst(source);
        
        // Assert
        ast.Declarations.Should().HaveCount(1);
        
        var funcDecl = ast.Declarations[0].Should().BeOfType<FunctionDeclNode>().Subject;
        funcDecl.Name.Should().Be("greet");
        funcDecl.Parameters.Should().HaveCount(1);
        funcDecl.Parameters[0].Name.Should().Be("name");
        funcDecl.Parameters[0].Type.Should().BeOfType<PrimitiveType>()
            .Which.Kind.Should().Be(PrimitiveKind.String);
        funcDecl.ReturnType.Should().BeOfType<PrimitiveType>()
            .Which.Kind.Should().Be(PrimitiveKind.String);
        
        funcDecl.Body.Statements.Should().HaveCount(1);
        var returnStmt = funcDecl.Body.Statements[0].Should().BeOfType<ReturnStmtNode>().Subject;
        returnStmt.Value.Should().BeOfType<BinaryNode>();
    }
    
    [Fact]
    public void BuildAst_VariableDeclarations_ShouldCreateCorrectNodes()
    {
        // Arrange
        var source = @"
            func test() {
                let x = 42;
                let message: String = ""Hello"";
                let flag: Bool = true;
            }
        ";
        
        // Act
        var ast = TestHelpers.BuildAst(source);
        
        // Assert
        var funcDecl = (FunctionDeclNode)ast.Declarations[0];
        var statements = funcDecl.Body.Statements;
        statements.Should().HaveCount(3);
        
        // First variable: let x = 42
        var varDecl1 = statements[0].Should().BeOfType<VarDeclStmtNode>().Subject;
        varDecl1.Name.Should().Be("x");
        varDecl1.DeclaredType.Should().BeNull();
        varDecl1.Initializer.Should().BeOfType<IntLiteralNode>()
            .Which.Value.Should().Be(42);
        
        // Second variable: let message: String = "Hello"
        var varDecl2 = statements[1].Should().BeOfType<VarDeclStmtNode>().Subject;
        varDecl2.Name.Should().Be("message");
        varDecl2.DeclaredType.Should().BeOfType<PrimitiveType>()
            .Which.Kind.Should().Be(PrimitiveKind.String);
        varDecl2.Initializer.Should().BeOfType<StringLiteralNode>()
            .Which.Value.Should().Be("Hello");
        
        // Third variable: let flag: Bool = true
        var varDecl3 = statements[2].Should().BeOfType<VarDeclStmtNode>().Subject;
        varDecl3.Name.Should().Be("flag");
        varDecl3.DeclaredType.Should().BeOfType<PrimitiveType>()
            .Which.Kind.Should().Be(PrimitiveKind.Bool);
        varDecl3.Initializer.Should().BeOfType<BoolLiteralNode>()
            .Which.Value.Should().BeTrue();
    }
    
    [Fact]
    public void BuildAst_IfStatement_ShouldCreateIfNode()
    {
        // Arrange
        var source = @"
            func checkNumber(n: Int) {
                if (n > 0) {
                    println(""positive"");
                } else {
                    println(""not positive"");
                }
            }
        ";
        
        // Act
        var ast = TestHelpers.BuildAst(source);
        
        // Assert
        var funcDecl = (FunctionDeclNode)ast.Declarations[0];
        var ifStmt = funcDecl.Body.Statements[0].Should().BeOfType<IfStmtNode>().Subject;
        
        // Condition: n > 0
        var condition = ifStmt.Condition.Should().BeOfType<BinaryNode>().Subject;
        condition.Operator.Should().Be(BinaryOperator.GreaterThan);
        condition.Left.Should().BeOfType<VariableNode>().Which.Name.Should().Be("n");
        condition.Right.Should().BeOfType<IntLiteralNode>().Which.Value.Should().Be(0);
        
        // Then branch
        ifStmt.ThenBranch.Statements.Should().HaveCount(1);
        ifStmt.ThenBranch.Statements[0].Should().BeOfType<ExpressionStmtNode>();
        
        // Else branch
        ifStmt.ElseBranch.Should().NotBeNull();
        ifStmt.ElseBranch!.Statements.Should().HaveCount(1);
    }
    
    [Fact]
    public void BuildAst_AssertStatement_ShouldCreateAssertNode()
    {
        // Arrange
        var source = @"
            func validate(x: Int) {
                assert(x > 0);
                assert(x < 100, ""x must be less than 100"");
            }
        ";
        
        // Act
        var ast = TestHelpers.BuildAst(source);
        
        // Assert
        var funcDecl = (FunctionDeclNode)ast.Declarations[0];
        var statements = funcDecl.Body.Statements;
        
        // First assert without message
        var assert1 = statements[0].Should().BeOfType<AssertStmtNode>().Subject;
        assert1.Condition.Should().BeOfType<BinaryNode>();
        assert1.Message.Should().BeNull();
        
        // Second assert with message
        var assert2 = statements[1].Should().BeOfType<AssertStmtNode>().Subject;
        assert2.Condition.Should().BeOfType<BinaryNode>();
        assert2.Message.Should().Be("x must be less than 100");
    }
    
    [Fact]
    public void BuildAst_LambdaExpression_ShouldCreateLambdaNode()
    {
        // Arrange
        var source = @"
            func test() {
                let double = (x: Int) => x * 2;
                let greet = (name: String) => {
                    return ""Hello, "" + name;
                };
            }
        ";
        
        // Act
        var ast = TestHelpers.BuildAst(source);
        
        // Assert
        var funcDecl = (FunctionDeclNode)ast.Declarations[0];
        
        // First lambda: (x: Int) => x * 2
        var varDecl1 = (VarDeclStmtNode)funcDecl.Body.Statements[0];
        var lambda1 = varDecl1.Initializer.Should().BeOfType<LambdaNode>().Subject;
        lambda1.Parameters.Should().HaveCount(1);
        lambda1.Parameters[0].Name.Should().Be("x");
        lambda1.Parameters[0].Type.Should().BeOfType<PrimitiveType>()
            .Which.Kind.Should().Be(PrimitiveKind.Int);
        lambda1.Body.Should().BeOfType<BinaryNode>();
        
        // Second lambda with block body
        var varDecl2 = (VarDeclStmtNode)funcDecl.Body.Statements[1];
        var lambda2 = varDecl2.Initializer.Should().BeOfType<LambdaNode>().Subject;
        lambda2.Parameters.Should().HaveCount(1);
        lambda2.Body.Should().BeOfType<BlockStmtNode>();
    }
    
    [Fact]
    public void BuildAst_BinaryExpressions_ShouldRespectPrecedence()
    {
        // Arrange
        var source = @"
            func test() {
                let a = 1 + 2 * 3;      // Should be 1 + (2 * 3)
                let b = (1 + 2) * 3;    // Should be (1 + 2) * 3
                let c = 1 < 2 && 3 < 4; // Should be (1 < 2) && (3 < 4)
            }
        ";
        
        // Act
        var ast = TestHelpers.BuildAst(source);
        
        // Assert
        var funcDecl = (FunctionDeclNode)ast.Declarations[0];
        
        // First expression: 1 + 2 * 3
        var varDecl1 = (VarDeclStmtNode)funcDecl.Body.Statements[0];
        var expr1 = varDecl1.Initializer.Should().BeOfType<BinaryNode>().Subject;
        expr1.Operator.Should().Be(BinaryOperator.Add);
        expr1.Left.Should().BeOfType<IntLiteralNode>().Which.Value.Should().Be(1);
        
        var rightMul = expr1.Right.Should().BeOfType<BinaryNode>().Subject;
        rightMul.Operator.Should().Be(BinaryOperator.Multiply);
        rightMul.Left.Should().BeOfType<IntLiteralNode>().Which.Value.Should().Be(2);
        rightMul.Right.Should().BeOfType<IntLiteralNode>().Which.Value.Should().Be(3);
        
        // Second expression: (1 + 2) * 3
        var varDecl2 = (VarDeclStmtNode)funcDecl.Body.Statements[1];
        var expr2 = varDecl2.Initializer.Should().BeOfType<BinaryNode>().Subject;
        expr2.Operator.Should().Be(BinaryOperator.Multiply);
        expr2.Right.Should().BeOfType<IntLiteralNode>().Which.Value.Should().Be(3);
        
        var leftAdd = expr2.Left.Should().BeOfType<BinaryNode>().Subject;
        leftAdd.Operator.Should().Be(BinaryOperator.Add);
        leftAdd.Left.Should().BeOfType<IntLiteralNode>().Which.Value.Should().Be(1);
        leftAdd.Right.Should().BeOfType<IntLiteralNode>().Which.Value.Should().Be(2);
        
        // Third expression: 1 < 2 && 3 < 4
        var varDecl3 = (VarDeclStmtNode)funcDecl.Body.Statements[2];
        var expr3 = varDecl3.Initializer.Should().BeOfType<BinaryNode>().Subject;
        expr3.Operator.Should().Be(BinaryOperator.And);
        
        var leftLt = expr3.Left.Should().BeOfType<BinaryNode>().Subject;
        leftLt.Operator.Should().Be(BinaryOperator.LessThan);
        
        var rightLt = expr3.Right.Should().BeOfType<BinaryNode>().Subject;
        rightLt.Operator.Should().Be(BinaryOperator.LessThan);
    }
    
    [Fact]
    public void BuildAst_FunctionCall_ShouldCreateCallNode()
    {
        // Arrange
        var source = @"
            func test() {
                println(""Hello"");
                let sum = add(1, 2);
                process(x, y + z, true);
            }
        ";
        
        // Act
        var ast = TestHelpers.BuildAst(source);
        
        // Assert
        var funcDecl = (FunctionDeclNode)ast.Declarations[0];
        
        // First call: println("Hello")
        var stmt1 = (ExpressionStmtNode)funcDecl.Body.Statements[0];
        var call1 = stmt1.Expression.Should().BeOfType<CallNode>().Subject;
        call1.FunctionName.Should().Be("println");
        call1.Arguments.Should().HaveCount(1);
        call1.Arguments[0].Should().BeOfType<StringLiteralNode>()
            .Which.Value.Should().Be("Hello");
        
        // Second call: add(1, 2)
        var varDecl = (VarDeclStmtNode)funcDecl.Body.Statements[1];
        var call2 = varDecl.Initializer.Should().BeOfType<CallNode>().Subject;
        call2.FunctionName.Should().Be("add");
        call2.Arguments.Should().HaveCount(2);
        call2.Arguments[0].Should().BeOfType<IntLiteralNode>().Which.Value.Should().Be(1);
        call2.Arguments[1].Should().BeOfType<IntLiteralNode>().Which.Value.Should().Be(2);
        
        // Third call: process(x, y + z, true)
        var stmt3 = (ExpressionStmtNode)funcDecl.Body.Statements[2];
        var call3 = stmt3.Expression.Should().BeOfType<CallNode>().Subject;
        call3.FunctionName.Should().Be("process");
        call3.Arguments.Should().HaveCount(3);
        call3.Arguments[0].Should().BeOfType<VariableNode>();
        call3.Arguments[1].Should().BeOfType<BinaryNode>();
        call3.Arguments[2].Should().BeOfType<BoolLiteralNode>();
    }
    
    [Fact]
    public void BuildAst_UnaryExpressions_ShouldCreateUnaryNodes()
    {
        // Arrange
        var source = @"
            func test() {
                let a = -42;
                let b = !true;
                let c = -(x + y);
            }
        ";
        
        // Act
        var ast = TestHelpers.BuildAst(source);
        
        // Assert
        var funcDecl = (FunctionDeclNode)ast.Declarations[0];
        
        // -42
        var varDecl1 = (VarDeclStmtNode)funcDecl.Body.Statements[0];
        var unary1 = varDecl1.Initializer.Should().BeOfType<UnaryNode>().Subject;
        unary1.Operator.Should().Be(UnaryOperator.Negate);
        unary1.Operand.Should().BeOfType<IntLiteralNode>().Which.Value.Should().Be(42);
        
        // !true
        var varDecl2 = (VarDeclStmtNode)funcDecl.Body.Statements[1];
        var unary2 = varDecl2.Initializer.Should().BeOfType<UnaryNode>().Subject;
        unary2.Operator.Should().Be(UnaryOperator.Not);
        unary2.Operand.Should().BeOfType<BoolLiteralNode>().Which.Value.Should().BeTrue();
        
        // -(x + y)
        var varDecl3 = (VarDeclStmtNode)funcDecl.Body.Statements[2];
        var unary3 = varDecl3.Initializer.Should().BeOfType<UnaryNode>().Subject;
        unary3.Operator.Should().Be(UnaryOperator.Negate);
        unary3.Operand.Should().BeOfType<BinaryNode>();
    }
    
    [Fact]
    public void BuildAst_ComplexFunctionType_ShouldCreateFunctionTypeNode()
    {
        // Arrange
        var source = @"
            func compose(f: Func<Int, String>, g: Func<String, Bool>) -> Func<Int, Bool> {
                return (x: Int) => g(f(x));
            }
        ";
        
        // Act
        var ast = TestHelpers.BuildAst(source);
        
        // Assert
        var funcDecl = (FunctionDeclNode)ast.Declarations[0];
        
        // Check first parameter type: Func<Int, String>
        var param1Type = funcDecl.Parameters[0].Type.Should().BeOfType<FunctionType>().Subject;
        param1Type.ParameterTypes.Should().HaveCount(1);
        param1Type.ParameterTypes[0].Should().BeOfType<PrimitiveType>()
            .Which.Kind.Should().Be(PrimitiveKind.Int);
        param1Type.ReturnType.Should().BeOfType<PrimitiveType>()
            .Which.Kind.Should().Be(PrimitiveKind.String);
        
        // Check return type: Func<Int, Bool>
        var returnType = funcDecl.ReturnType.Should().BeOfType<FunctionType>().Subject;
        returnType.ParameterTypes.Should().HaveCount(1);
        returnType.ParameterTypes[0].Should().BeOfType<PrimitiveType>()
            .Which.Kind.Should().Be(PrimitiveKind.Int);
        returnType.ReturnType.Should().BeOfType<PrimitiveType>()
            .Which.Kind.Should().Be(PrimitiveKind.Bool);
    }
    
    [Fact]
    public void BuildAst_StringEscapeSequences_ShouldBeProcessed()
    {
        // Arrange
        // Using regular string literals to be clearer about escape sequences
        var source = "func test() {\n" +
                     "    let a = \"Hello\\nWorld\";\n" +
                     "    let b = \"Tab\\there\";\n" +
                     "    let c = \"Quote: \\\"test\\\"\";\n" +
                     "    let d = \"Backslash: \\\\\";\n" +
                     "}";
        
        // Act
        var ast = TestHelpers.BuildAst(source);
        
        // Assert
        var funcDecl = (FunctionDeclNode)ast.Declarations[0];
        
        var varDecl1 = (VarDeclStmtNode)funcDecl.Body.Statements[0];
        varDecl1.Initializer.Should().BeOfType<StringLiteralNode>()
            .Which.Value.Should().Be("Hello\nWorld");
        
        var varDecl2 = (VarDeclStmtNode)funcDecl.Body.Statements[1];
        varDecl2.Initializer.Should().BeOfType<StringLiteralNode>()
            .Which.Value.Should().Be("Tab\there");
        
        var varDecl3 = (VarDeclStmtNode)funcDecl.Body.Statements[2];
        varDecl3.Initializer.Should().BeOfType<StringLiteralNode>()
            .Which.Value.Should().Be("Quote: \"test\"");
        
        var varDecl4 = (VarDeclStmtNode)funcDecl.Body.Statements[3];
        varDecl4.Initializer.Should().BeOfType<StringLiteralNode>()
            .Which.Value.Should().Be("Backslash: \\");
    }
    
    [Fact]
    public void BuildAst_LineAndColumnNumbers_ShouldBeSet()
    {
        // Arrange
        var source = @"func test() {
    let x = 42;
    if (x > 0) {
        println(""positive"");
    }
}";
        
        // Act
        var ast = TestHelpers.BuildAst(source);
        
        // Assert
        var funcDecl = (FunctionDeclNode)ast.Declarations[0];
        funcDecl.Line.Should().Be(1);
        funcDecl.Column.Should().Be(0);
        
        var varDecl = (VarDeclStmtNode)funcDecl.Body.Statements[0];
        varDecl.Line.Should().Be(2);
        varDecl.Column.Should().Be(4);
        
        var ifStmt = (IfStmtNode)funcDecl.Body.Statements[1];
        ifStmt.Line.Should().Be(3);
        ifStmt.Column.Should().Be(4);
    }
}