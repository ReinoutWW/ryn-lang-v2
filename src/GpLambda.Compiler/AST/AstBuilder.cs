using Antlr4.Runtime;
using GpLambda.Compiler.AST.Nodes;
using GpLambda.Compiler.Grammar;

namespace GpLambda.Compiler.AST;

/// <summary>
/// Builds an AST from the ANTLR parse tree
/// </summary>
public class AstBuilder : GpLambdaBaseVisitor<Node?>
{
    public override Node? VisitProgram(GpLambdaParser.ProgramContext context)
    {
        var declarations = new List<Node>();
        
        foreach (var decl in context.declaration())
        {
            var node = Visit(decl);
            if (node != null)
                declarations.Add(node);
        }
        
        return new ProgramNode(declarations);
    }
    
    public override Node? VisitFunctionDecl(GpLambdaParser.FunctionDeclContext context)
    {
        var name = context.ID().GetText();
        var parameters = ParseParameterList(context.paramList());
        var returnType = context.returnType() != null ? ParseType(context.returnType().type()) : null;
        var body = (BlockStmtNode)Visit(context.block())!;
        
        return new FunctionDeclNode(name, parameters, returnType, body)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitBlock(GpLambdaParser.BlockContext context)
    {
        var statements = new List<Stmt>();
        
        foreach (var stmt in context.statement())
        {
            var node = Visit(stmt) as Stmt;
            if (node != null)
                statements.Add(node);
        }
        
        return new BlockStmtNode(statements);
    }
    
    public override Node? VisitVarDecl(GpLambdaParser.VarDeclContext context)
    {
        var name = context.ID().GetText();
        var declaredType = context.type() != null ? ParseType(context.type()) : null;
        var initializer = context.expr() != null ? Visit(context.expr()) as Expr : null;
        
        return new VarDeclStmtNode(name, declaredType, initializer)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitAssignStmt(GpLambdaParser.AssignStmtContext context)
    {
        var name = context.ID().GetText();
        var value = (Expr)Visit(context.expr())!;
        
        return new AssignmentStmtNode(name, value)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitIfStmt(GpLambdaParser.IfStmtContext context)
    {
        var condition = (Expr)Visit(context.expr())!;
        var thenBranch = (BlockStmtNode)Visit(context.block(0))!;
        var elseBranch = context.block().Length > 1 ? (BlockStmtNode)Visit(context.block(1)) : null;
        
        return new IfStmtNode(condition, thenBranch, elseBranch)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitReturnStmt(GpLambdaParser.ReturnStmtContext context)
    {
        var value = context.expr() != null ? (Expr)Visit(context.expr())! : null;
        
        return new ReturnStmtNode(value)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitAssertStmt(GpLambdaParser.AssertStmtContext context)
    {
        var condition = (Expr)Visit(context.expr())!;
        var message = context.STRING()?.GetText().Trim('"');
        
        return new AssertStmtNode(condition, message)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitExprStmt(GpLambdaParser.ExprStmtContext context)
    {
        var expr = (Expr)Visit(context.expr())!;
        
        return new ExpressionStmtNode(expr)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    // Expression visitors
    
    public override Node? VisitIntLiteral(GpLambdaParser.IntLiteralContext context)
    {
        var value = int.Parse(context.INT().GetText());
        return new IntLiteralNode(value)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitStringLiteral(GpLambdaParser.StringLiteralContext context)
    {
        var rawText = context.STRING().GetText();
        // Remove the surrounding quotes
        var value = rawText.Substring(1, rawText.Length - 2);
        
        // Handle escape sequences
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '\\' && i + 1 < value.Length)
            {
                switch (value[i + 1])
                {
                    case 'n':
                        result.Append('\n');
                        i++;
                        break;
                    case 'r':
                        result.Append('\r');
                        i++;
                        break;
                    case 't':
                        result.Append('\t');
                        i++;
                        break;
                    case '"':
                        result.Append('"');
                        i++;
                        break;
                    case '\\':
                        result.Append('\\');
                        i++;
                        break;
                    default:
                        // Unknown escape sequence, keep as is
                        result.Append(value[i]);
                        break;
                }
            }
            else
            {
                result.Append(value[i]);
            }
        }
        
        return new StringLiteralNode(result.ToString())
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitBoolLiteral(GpLambdaParser.BoolLiteralContext context)
    {
        var value = context.GetText() == "true";
        return new BoolLiteralNode(value)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitVarExpr(GpLambdaParser.VarExprContext context)
    {
        var name = context.ID().GetText();
        return new VariableNode(name)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitCallExpr(GpLambdaParser.CallExprContext context)
    {
        var funcExpr = (Expr)Visit(context.expr())!;
        var arguments = ParseArgumentList(context.argList());
        
        // For now, we only support simple function calls (not higher-order)
        if (funcExpr is VariableNode varNode)
        {
            return new CallNode(varNode.Name, arguments)
            {
                Line = context.Start.Line,
                Column = context.Start.Column
            };
        }
        
        throw new NotSupportedException("Higher-order function calls not yet supported");
    }
    
    public override Node? VisitMethodCallExpr(GpLambdaParser.MethodCallExprContext context)
    {
        var target = (Expr)Visit(context.expr())!;
        var methodName = context.ID().GetText();
        var arguments = ParseArgumentList(context.argList());
        
        // For now, we'll treat method calls as regular function calls with the target as first argument
        // This is a simplification - in a full implementation, we'd have a separate MethodCallNode
        var allArgs = new List<Expr> { target };
        allArgs.AddRange(arguments);
        
        return new CallNode(methodName, allArgs)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitLambdaExpr(GpLambdaParser.LambdaExprContext context)
    {
        var parameters = ParseParameterList(context.paramList());
        
        Node body;
        if (context.expr() != null)
        {
            body = (Expr)Visit(context.expr())!;
        }
        else
        {
            body = (BlockStmtNode)Visit(context.block())!;
        }
        
        return new LambdaNode(parameters, body)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    
    public override Node? VisitUnaryExpr(GpLambdaParser.UnaryExprContext context)
    {
        var operand = (Expr)Visit(context.expr())!;
        var op = context.op.Text == "-" ? UnaryOperator.Negate : UnaryOperator.Not;
        
        return new UnaryNode(op, operand)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitParenExpr(GpLambdaParser.ParenExprContext context)
    {
        return Visit(context.expr());
    }
    
    public override Node? VisitPrimaryExpr(GpLambdaParser.PrimaryExprContext context)
    {
        return Visit(context.primary());
    }
    
    // Binary expression visitors
    public override Node? VisitMultiplicativeExpr(GpLambdaParser.MultiplicativeExprContext context)
    {
        var left = (Expr)Visit(context.expr(0))!;
        var right = (Expr)Visit(context.expr(1))!;
        var op = ParseBinaryOperator(context.op.Text);
        
        return new BinaryNode(left, op, right)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitAdditiveExpr(GpLambdaParser.AdditiveExprContext context)
    {
        var left = (Expr)Visit(context.expr(0))!;
        var right = (Expr)Visit(context.expr(1))!;
        var op = ParseBinaryOperator(context.op.Text);
        
        return new BinaryNode(left, op, right)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitRelationalExpr(GpLambdaParser.RelationalExprContext context)
    {
        var left = (Expr)Visit(context.expr(0))!;
        var right = (Expr)Visit(context.expr(1))!;
        var op = ParseBinaryOperator(context.op.Text);
        
        return new BinaryNode(left, op, right)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitEqualityExpr(GpLambdaParser.EqualityExprContext context)
    {
        var left = (Expr)Visit(context.expr(0))!;
        var right = (Expr)Visit(context.expr(1))!;
        var op = ParseBinaryOperator(context.op.Text);
        
        return new BinaryNode(left, op, right)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitLogicalAndExpr(GpLambdaParser.LogicalAndExprContext context)
    {
        var left = (Expr)Visit(context.expr(0))!;
        var right = (Expr)Visit(context.expr(1))!;
        
        return new BinaryNode(left, BinaryOperator.And, right)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    public override Node? VisitLogicalOrExpr(GpLambdaParser.LogicalOrExprContext context)
    {
        var left = (Expr)Visit(context.expr(0))!;
        var right = (Expr)Visit(context.expr(1))!;
        
        return new BinaryNode(left, BinaryOperator.Or, right)
        {
            Line = context.Start.Line,
            Column = context.Start.Column
        };
    }
    
    // Helper methods
    
    private List<Parameter> ParseParameterList(GpLambdaParser.ParamListContext? context)
    {
        var parameters = new List<Parameter>();
        
        if (context == null) return parameters;
        
        foreach (var param in context.param())
        {
            var name = param.ID().GetText();
            var type = ParseType(param.type());
            parameters.Add(new Parameter(name, type));
        }
        
        return parameters;
    }
    
    private List<Expr> ParseArgumentList(GpLambdaParser.ArgListContext? context)
    {
        var arguments = new List<Expr>();
        
        if (context == null) return arguments;
        
        foreach (var expr in context.expr())
        {
            arguments.Add((Expr)Visit(expr)!);
        }
        
        return arguments;
    }
    
    private Nodes.Type ParseType(GpLambdaParser.TypeContext context)
    {
        if (context is GpLambdaParser.PrimitiveTypeContext primitive)
        {
            var typeName = primitive.GetText();
            return typeName switch
            {
                "Int" => PrimitiveType.Int,
                "String" => PrimitiveType.String,
                "Bool" => PrimitiveType.Bool,
                "Void" => PrimitiveType.Void,
                _ => throw new InvalidOperationException($"Unknown primitive type: {typeName}")
            };
        }
        else if (context is GpLambdaParser.FunctionTypeContext func)
        {
            var paramTypes = new List<Nodes.Type>();
            foreach (var type in func.typeList().type())
            {
                paramTypes.Add(ParseType(type));
            }
            var returnType = ParseType(func.type());
            return new FunctionType(paramTypes, returnType);
        }
        
        throw new InvalidOperationException("Unknown type context");
    }
    
    private BinaryOperator ParseBinaryOperator(string op)
    {
        return op switch
        {
            "+" => BinaryOperator.Add,
            "-" => BinaryOperator.Subtract,
            "*" => BinaryOperator.Multiply,
            "/" => BinaryOperator.Divide,
            "%" => BinaryOperator.Modulo,
            "==" => BinaryOperator.Equal,
            "!=" => BinaryOperator.NotEqual,
            "<" => BinaryOperator.LessThan,
            ">" => BinaryOperator.GreaterThan,
            "<=" => BinaryOperator.LessOrEqual,
            ">=" => BinaryOperator.GreaterOrEqual,
            "&&" => BinaryOperator.And,
            "||" => BinaryOperator.Or,
            _ => throw new InvalidOperationException($"Unknown binary operator: {op}")
        };
    }
}