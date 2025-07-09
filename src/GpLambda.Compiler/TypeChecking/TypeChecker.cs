using GpLambda.Compiler.AST;
using GpLambda.Compiler.AST.Nodes;
using GpLambda.Compiler.SemanticAnalysis;

namespace GpLambda.Compiler.TypeChecking;

/// <summary>
/// Performs type checking and type inference on the AST
/// </summary>
public class TypeChecker : IAstVisitor<AST.Nodes.Type?>
{
    private readonly SymbolTable _symbolTable;
    private readonly List<TypeError> _errors = new();
    private FunctionType? _currentFunction;

    public IReadOnlyList<TypeError> Errors => _errors;
    public bool HasErrors => _errors.Count > 0;

    public TypeChecker(SymbolTable symbolTable)
    {
        _symbolTable = symbolTable;
    }

    public void Check(ProgramNode program)
    {
        program.Accept(this);
    }

    // Program and declarations

    public AST.Nodes.Type? VisitProgram(ProgramNode node)
    {
        foreach (var declaration in node.Declarations)
        {
            declaration.Accept(this);
        }
        return null;
    }

    public AST.Nodes.Type? VisitFunctionDecl(FunctionDeclNode node)
    {
        var symbol = _symbolTable.Resolve(node.Name) as FunctionSymbol;
        if (symbol == null) return null;

        _currentFunction = symbol.Type as FunctionType;
        
        // Enter function scope to find local variables
        _symbolTable.EnterScope(ScopeType.Function);

        // Type check function body
        node.Body.Accept(this);

        // Check for missing return if function has non-void return type
        if (_currentFunction?.ReturnType != PrimitiveType.Void && !HasReturnStatement(node.Body))
        {
            AddError($"Function '{node.Name}' must return a value of type {_currentFunction.ReturnType}", 
                node.Line, node.Column);
        }

        _symbolTable.ExitScope();
        _currentFunction = null;
        return null;
    }

    // Statements

    public AST.Nodes.Type? VisitBlock(BlockStmtNode node)
    {
        // Don't enter a new scope for function body blocks 
        // (already in function scope)
        bool isNested = _symbolTable.CurrentScope.Type != ScopeType.Function;
        
        if (isNested)
        {
            _symbolTable.EnterScope(ScopeType.Block);
        }
        
        foreach (var stmt in node.Statements)
        {
            stmt.Accept(this);
        }
        
        if (isNested)
        {
            _symbolTable.ExitScope();
        }
        
        return null;
    }

    public AST.Nodes.Type? VisitVarDecl(VarDeclStmtNode node)
    {
        var symbol = _symbolTable.Resolve(node.Name) as VariableSymbol;
        if (symbol == null) return null;

        if (node.Initializer != null)
        {
            var initType = node.Initializer.Accept(this);
            if (initType != null && symbol.Type != null)
            {
                if (!TypesAreCompatible(symbol.Type, initType))
                {
                    AddError($"Cannot initialize variable '{node.Name}' of type {symbol.Type} with value of type {initType}", 
                        node.Line, node.Column);
                }
            }
        }

        return null;
    }

    public AST.Nodes.Type? VisitAssignment(AssignmentStmtNode node)
    {
        var symbol = _symbolTable.Resolve(node.VariableName) as VariableSymbol;
        if (symbol == null) return null;

        var valueType = node.Value.Accept(this);
        if (valueType != null && symbol.Type != null)
        {
            if (!TypesAreCompatible(symbol.Type, valueType))
            {
                AddError($"Cannot assign value of type {valueType} to variable '{node.VariableName}' of type {symbol.Type}", 
                    node.Line, node.Column);
            }
        }

        return null;
    }

    public AST.Nodes.Type? VisitIf(IfStmtNode node)
    {
        var condType = node.Condition.Accept(this);
        if (condType != null && !condType.Equals(PrimitiveType.Bool))
        {
            AddError($"If condition must be of type Bool, but got {condType}", node.Line, node.Column);
        }

        node.ThenBranch.Accept(this);
        node.ElseBranch?.Accept(this);
        return null;
    }

    public AST.Nodes.Type? VisitReturn(ReturnStmtNode node)
    {
        if (_currentFunction == null)
        {
            AddError("Return statement outside of function", node.Line, node.Column);
            return null;
        }

        var returnType = node.Value?.Accept(this) ?? PrimitiveType.Void;
        
        if (!TypesAreCompatible(_currentFunction.ReturnType, returnType))
        {
            AddError($"Function should return {_currentFunction.ReturnType}, but returns {returnType}", 
                node.Line, node.Column);
        }

        return null;
    }

    public AST.Nodes.Type? VisitAssert(AssertStmtNode node)
    {
        var condType = node.Condition.Accept(this);
        if (condType != null && !condType.Equals(PrimitiveType.Bool))
        {
            AddError($"Assert condition must be of type Bool, but got {condType}", node.Line, node.Column);
        }
        return null;
    }

    public AST.Nodes.Type? VisitExpressionStmt(ExpressionStmtNode node)
    {
        node.Expression.Accept(this);
        return null;
    }

    // Expressions

    public AST.Nodes.Type? VisitIntLiteral(IntLiteralNode node)
    {
        return PrimitiveType.Int;
    }

    public AST.Nodes.Type? VisitStringLiteral(StringLiteralNode node)
    {
        return PrimitiveType.String;
    }

    public AST.Nodes.Type? VisitBoolLiteral(BoolLiteralNode node)
    {
        return PrimitiveType.Bool;
    }

    public AST.Nodes.Type? VisitVariable(VariableNode node)
    {
        var symbol = _symbolTable.Resolve(node.Name);
        if (symbol == null)
        {
            AddError($"Undefined variable '{node.Name}'", node.Line, node.Column);
            return null;
        }
        return symbol.Type;
    }

    public AST.Nodes.Type? VisitBinary(BinaryNode node)
    {
        var leftType = node.Left.Accept(this);
        var rightType = node.Right.Accept(this);

        if (leftType == null || rightType == null) return null;

        return node.Operator switch
        {
            // Arithmetic operators
            BinaryOperator.Add or BinaryOperator.Subtract or BinaryOperator.Multiply or 
            BinaryOperator.Divide or BinaryOperator.Modulo => 
                CheckArithmeticOperator(node.Operator, leftType, rightType, node.Line, node.Column),
            
            // Comparison operators
            BinaryOperator.LessThan or BinaryOperator.GreaterThan or 
            BinaryOperator.LessOrEqual or BinaryOperator.GreaterOrEqual =>
                CheckComparisonOperator(node.Operator, leftType, rightType, node.Line, node.Column),
            
            // Equality operators
            BinaryOperator.Equal or BinaryOperator.NotEqual =>
                CheckEqualityOperator(node.Operator, leftType, rightType, node.Line, node.Column),
            
            // Logical operators
            BinaryOperator.And or BinaryOperator.Or =>
                CheckLogicalOperator(node.Operator, leftType, rightType, node.Line, node.Column),
            
            _ => null
        };
    }

    public AST.Nodes.Type? VisitUnary(UnaryNode node)
    {
        var operandType = node.Operand.Accept(this);
        if (operandType == null) return null;

        return node.Operator switch
        {
            UnaryOperator.Negate => CheckNegateOperator(operandType, node.Line, node.Column),
            UnaryOperator.Not => CheckNotOperator(operandType, node.Line, node.Column),
            _ => null
        };
    }

    public AST.Nodes.Type? VisitCall(CallNode node)
    {
        var symbol = _symbolTable.Resolve(node.FunctionName);
        if (symbol == null)
        {
            AddError($"Undefined function '{node.FunctionName}'", node.Line, node.Column);
            return null;
        }

        FunctionType? funcType = null;
        
        if (symbol is FunctionSymbol funcSymbol)
        {
            funcType = funcSymbol.Type as FunctionType;
        }
        else if (symbol is VariableSymbol varSymbol && varSymbol.Type is FunctionType ft)
        {
            funcType = ft;
        }

        if (funcType == null)
        {
            AddError($"'{node.FunctionName}' is not a function", node.Line, node.Column);
            return null;
        }

        // Check argument types
        if (node.Arguments.Count != funcType.ParameterTypes.Count)
        {
            AddError($"Function '{node.FunctionName}' expects {funcType.ParameterTypes.Count} arguments, but {node.Arguments.Count} were provided", 
                node.Line, node.Column);
        }
        else
        {
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                var argType = node.Arguments[i].Accept(this);
                if (argType != null && !TypesAreCompatible(funcType.ParameterTypes[i], argType))
                {
                    AddError($"Argument {i + 1} of function '{node.FunctionName}' expects type {funcType.ParameterTypes[i]}, but got {argType}", 
                        node.Arguments[i].Line, node.Arguments[i].Column);
                }
            }
        }

        return funcType.ReturnType;
    }

    public AST.Nodes.Type? VisitLambda(LambdaNode node)
    {
        _symbolTable.EnterScope(ScopeType.Lambda);

        // Infer return type from body
        AST.Nodes.Type? returnType;
        if (node.Body is Expr expr)
        {
            returnType = expr.Accept(this);
        }
        else if (node.Body is BlockStmtNode block)
        {
            returnType = InferReturnType(block);
        }
        else
        {
            returnType = PrimitiveType.Void;
        }

        _symbolTable.ExitScope();

        var paramTypes = node.Parameters.Select(p => p.Type).ToList();
        return new FunctionType(paramTypes, returnType ?? PrimitiveType.Void);
    }

    // Helper methods

    private AST.Nodes.Type? CheckArithmeticOperator(BinaryOperator op, AST.Nodes.Type left, AST.Nodes.Type right, int line, int column)
    {
        if (left.Equals(PrimitiveType.Int) && right.Equals(PrimitiveType.Int))
        {
            return PrimitiveType.Int;
        }

        // String concatenation with +
        if (op == BinaryOperator.Add && 
            (left.Equals(PrimitiveType.String) || right.Equals(PrimitiveType.String)))
        {
            return PrimitiveType.String;
        }

        AddError($"Operator '{op}' cannot be applied to types {left} and {right}", line, column);
        return null;
    }

    private AST.Nodes.Type? CheckComparisonOperator(BinaryOperator op, AST.Nodes.Type left, AST.Nodes.Type right, int line, int column)
    {
        if (left.Equals(PrimitiveType.Int) && right.Equals(PrimitiveType.Int))
        {
            return PrimitiveType.Bool;
        }

        AddError($"Operator '{op}' cannot be applied to types {left} and {right}", line, column);
        return null;
    }

    private AST.Nodes.Type? CheckEqualityOperator(BinaryOperator op, AST.Nodes.Type left, AST.Nodes.Type right, int line, int column)
    {
        if (TypesAreCompatible(left, right))
        {
            return PrimitiveType.Bool;
        }

        AddError($"Cannot compare types {left} and {right} for equality", line, column);
        return null;
    }

    private AST.Nodes.Type? CheckLogicalOperator(BinaryOperator op, AST.Nodes.Type left, AST.Nodes.Type right, int line, int column)
    {
        if (left.Equals(PrimitiveType.Bool) && right.Equals(PrimitiveType.Bool))
        {
            return PrimitiveType.Bool;
        }

        AddError($"Operator '{op}' requires Bool operands, but got {left} and {right}", line, column);
        return null;
    }

    private AST.Nodes.Type? CheckNegateOperator(AST.Nodes.Type operand, int line, int column)
    {
        if (operand.Equals(PrimitiveType.Int))
        {
            return PrimitiveType.Int;
        }

        AddError($"Operator '-' cannot be applied to type {operand}", line, column);
        return null;
    }

    private AST.Nodes.Type? CheckNotOperator(AST.Nodes.Type operand, int line, int column)
    {
        if (operand.Equals(PrimitiveType.Bool))
        {
            return PrimitiveType.Bool;
        }

        AddError($"Operator '!' cannot be applied to type {operand}", line, column);
        return null;
    }

    private bool TypesAreCompatible(AST.Nodes.Type expected, AST.Nodes.Type actual)
    {
        return expected.Equals(actual);
    }

    private bool HasReturnStatement(BlockStmtNode block)
    {
        foreach (var stmt in block.Statements)
        {
            if (stmt is ReturnStmtNode) return true;
            if (stmt is IfStmtNode ifStmt)
            {
                if (ifStmt.ElseBranch != null &&
                    HasReturnStatement(ifStmt.ThenBranch) &&
                    HasReturnStatement(ifStmt.ElseBranch))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private AST.Nodes.Type? InferReturnType(BlockStmtNode block)
    {
        // Simple return type inference - find first return statement
        foreach (var stmt in block.Statements)
        {
            if (stmt is ReturnStmtNode ret)
            {
                return ret.Value?.Accept(this) ?? PrimitiveType.Void;
            }
        }
        return PrimitiveType.Void;
    }

    private void AddError(string message, int line, int column)
    {
        _errors.Add(new TypeError(message, line, column));
    }
}

public record TypeError(string Message, int Line, int Column);