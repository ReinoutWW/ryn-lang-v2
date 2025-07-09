using GpLambda.Compiler.AST;
using GpLambda.Compiler.AST.Nodes;
using GpLambda.Compiler.TypeChecking;

namespace GpLambda.Compiler.SemanticAnalysis;

/// <summary>
/// Combines semantic analysis and type checking in a single pass
/// </summary>
public class CombinedAnalyzer : IAstVisitor<AST.Nodes.Type?>
{
    private readonly SymbolTable _symbolTable;
    private readonly List<SemanticError> _semanticErrors = new();
    private readonly List<TypeError> _typeErrors = new();
    private FunctionType? _currentFunction;

    public SymbolTable SymbolTable => _symbolTable;
    public IReadOnlyList<SemanticError> SemanticErrors => _semanticErrors;
    public IReadOnlyList<TypeError> TypeErrors => _typeErrors;
    public bool HasErrors => _semanticErrors.Count > 0 || _typeErrors.Count > 0;

    public CombinedAnalyzer()
    {
        _symbolTable = new SymbolTable();
    }

    public void Analyze(ProgramNode program)
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
        // Semantic analysis: Check if function is already defined
        if (_symbolTable.IsDefinedLocally(node.Name))
        {
            AddSemanticError($"Function '{node.Name}' is already defined", node.Line, node.Column);
            return null;
        }

        // Create function type
        var paramTypes = node.Parameters.Select(p => p.Type).ToList();
        var returnType = node.ReturnType ?? PrimitiveType.Void;
        var functionType = new FunctionType(paramTypes, returnType);

        // Define function symbol
        var functionSymbol = new FunctionSymbol(node.Name, functionType, node.Parameters, node.Line, node.Column)
        {
            IsDefined = true
        };
        _symbolTable.Define(functionSymbol);

        // Enter function scope
        _symbolTable.EnterScope(ScopeType.Function);
        _currentFunction = functionType;

        // Define parameters
        foreach (var param in node.Parameters)
        {
            var paramSymbol = new VariableSymbol(param.Name, param.Type, node.Line, node.Column)
            {
                IsInitialized = true // Parameters are always initialized
            };
            _symbolTable.Define(paramSymbol);
        }

        // Visit function body
        node.Body.Accept(this);

        // Type checking: Check for missing return
        if (functionType.ReturnType != PrimitiveType.Void && !HasReturnStatement(node.Body))
        {
            AddTypeError($"Function '{node.Name}' must return a value of type {functionType.ReturnType}", 
                node.Line, node.Column);
        }

        // Exit function scope
        _symbolTable.ExitScope();
        _currentFunction = null;

        return null;
    }

    // Statements

    public AST.Nodes.Type? VisitBlock(BlockStmtNode node)
    {
        _symbolTable.EnterScope(ScopeType.Block);
        
        foreach (var stmt in node.Statements)
        {
            stmt.Accept(this);
        }
        
        _symbolTable.ExitScope();
        return null;
    }

    public AST.Nodes.Type? VisitVarDecl(VarDeclStmtNode node)
    {
        // Semantic analysis: Check if variable is already defined in current scope
        if (_symbolTable.IsDefinedLocally(node.Name))
        {
            AddSemanticError($"Variable '{node.Name}' is already defined in this scope", node.Line, node.Column);
            return null;
        }

        // Type checking: Check initializer type if present
        AST.Nodes.Type? initType = null;
        if (node.Initializer != null)
        {
            initType = node.Initializer.Accept(this);
        }

        // Determine type
        AST.Nodes.Type type;
        if (node.DeclaredType != null)
        {
            type = node.DeclaredType;
        }
        else if (node.Initializer != null && initType != null)
        {
            // Type inference from initializer
            type = initType;
        }
        else
        {
            AddSemanticError($"Variable '{node.Name}' must have a type annotation or initializer", node.Line, node.Column);
            return null;
        }
        
        var symbol = new VariableSymbol(node.Name, type, node.Line, node.Column)
        {
            IsInitialized = node.Initializer != null
        };
        _symbolTable.Define(symbol);

        // Type checking: Check type compatibility if both declared type and initializer are present
        if (node.DeclaredType != null && initType != null)
        {
            if (!TypesAreCompatible(node.DeclaredType, initType))
            {
                AddTypeError($"Cannot initialize variable '{node.Name}' of type {node.DeclaredType} with value of type {initType}", 
                    node.Line, node.Column);
            }
        }

        return null;
    }

    public AST.Nodes.Type? VisitAssignment(AssignmentStmtNode node)
    {
        // Semantic analysis: Check if variable exists
        var symbol = _symbolTable.Resolve(node.VariableName);
        if (symbol == null)
        {
            AddSemanticError($"Undefined variable '{node.VariableName}'", node.Line, node.Column);
            return null;
        }

        if (symbol is not VariableSymbol varSymbol)
        {
            AddSemanticError($"'{node.VariableName}' is not a variable", node.Line, node.Column);
            return null;
        }

        // Type checking: Check assignment type
        var valueType = node.Value.Accept(this);
        if (valueType != null && varSymbol.Type != null)
        {
            if (!TypesAreCompatible(varSymbol.Type, valueType))
            {
                AddTypeError($"Cannot assign value of type {valueType} to variable '{node.VariableName}' of type {varSymbol.Type}", 
                    node.Line, node.Column);
            }
        }

        // Mark variable as initialized
        varSymbol.IsInitialized = true;

        return null;
    }

    public AST.Nodes.Type? VisitIf(IfStmtNode node)
    {
        var condType = node.Condition.Accept(this);
        if (condType != null && !condType.Equals(PrimitiveType.Bool))
        {
            AddTypeError($"If condition must be of type Bool, but got {condType}", node.Line, node.Column);
        }

        node.ThenBranch.Accept(this);
        node.ElseBranch?.Accept(this);
        return null;
    }

    public AST.Nodes.Type? VisitReturn(ReturnStmtNode node)
    {
        if (_currentFunction == null)
        {
            AddTypeError("Return statement outside of function", node.Line, node.Column);
            return null;
        }

        var returnType = node.Value?.Accept(this) ?? PrimitiveType.Void;
        
        if (!TypesAreCompatible(_currentFunction.ReturnType, returnType))
        {
            AddTypeError($"Function should return {_currentFunction.ReturnType}, but returns {returnType}", 
                node.Line, node.Column);
        }

        return null;
    }

    public AST.Nodes.Type? VisitAssert(AssertStmtNode node)
    {
        var condType = node.Condition.Accept(this);
        if (condType != null && !condType.Equals(PrimitiveType.Bool))
        {
            AddTypeError($"Assert condition must be of type Bool, but got {condType}", node.Line, node.Column);
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
            AddSemanticError($"Undefined variable '{node.Name}'", node.Line, node.Column);
            return null;
        }

        if (symbol is VariableSymbol varSymbol)
        {
            if (!varSymbol.IsInitialized)
            {
                AddSemanticError($"Variable '{node.Name}' may not be initialized", node.Line, node.Column);
            }
            varSymbol.IsUsed = true;
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
            AddSemanticError($"Undefined function '{node.FunctionName}'", node.Line, node.Column);
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
            AddSemanticError($"'{node.FunctionName}' is not a function", node.Line, node.Column);
            return null;
        }

        // Check argument types
        if (node.Arguments.Count != funcType.ParameterTypes.Count)
        {
            AddTypeError($"Function '{node.FunctionName}' expects {funcType.ParameterTypes.Count} arguments, but {node.Arguments.Count} were provided", 
                node.Line, node.Column);
        }
        else
        {
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                var argType = node.Arguments[i].Accept(this);
                if (argType != null && !TypesAreCompatible(funcType.ParameterTypes[i], argType))
                {
                    AddTypeError($"Argument {i + 1} of function '{node.FunctionName}' expects type {funcType.ParameterTypes[i]}, but got {argType}", 
                        node.Arguments[i].Line, node.Arguments[i].Column);
                }
            }
        }

        return funcType.ReturnType;
    }

    public AST.Nodes.Type? VisitLambda(LambdaNode node)
    {
        _symbolTable.EnterScope(ScopeType.Lambda);

        // Define parameters
        foreach (var param in node.Parameters)
        {
            var paramSymbol = new VariableSymbol(param.Name, param.Type, node.Line, node.Column)
            {
                IsInitialized = true
            };
            _symbolTable.Define(paramSymbol);
        }

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

        AddTypeError($"Operator '{op}' cannot be applied to types {left} and {right}", line, column);
        return null;
    }

    private AST.Nodes.Type? CheckComparisonOperator(BinaryOperator op, AST.Nodes.Type left, AST.Nodes.Type right, int line, int column)
    {
        if (left.Equals(PrimitiveType.Int) && right.Equals(PrimitiveType.Int))
        {
            return PrimitiveType.Bool;
        }

        AddTypeError($"Operator '{op}' cannot be applied to types {left} and {right}", line, column);
        return null;
    }

    private AST.Nodes.Type? CheckEqualityOperator(BinaryOperator op, AST.Nodes.Type left, AST.Nodes.Type right, int line, int column)
    {
        if (TypesAreCompatible(left, right))
        {
            return PrimitiveType.Bool;
        }

        AddTypeError($"Cannot compare types {left} and {right} for equality", line, column);
        return null;
    }

    private AST.Nodes.Type? CheckLogicalOperator(BinaryOperator op, AST.Nodes.Type left, AST.Nodes.Type right, int line, int column)
    {
        if (left.Equals(PrimitiveType.Bool) && right.Equals(PrimitiveType.Bool))
        {
            return PrimitiveType.Bool;
        }

        var opSymbol = op switch
        {
            BinaryOperator.And => "&&",
            BinaryOperator.Or => "||",
            _ => op.ToString()
        };
        AddTypeError($"Operator '{opSymbol}' requires Bool operands, but got {left} and {right}", line, column);
        return null;
    }

    private AST.Nodes.Type? CheckNegateOperator(AST.Nodes.Type operand, int line, int column)
    {
        if (operand.Equals(PrimitiveType.Int))
        {
            return PrimitiveType.Int;
        }

        AddTypeError($"Operator '-' cannot be applied to type {operand}", line, column);
        return null;
    }

    private AST.Nodes.Type? CheckNotOperator(AST.Nodes.Type operand, int line, int column)
    {
        if (operand.Equals(PrimitiveType.Bool))
        {
            return PrimitiveType.Bool;
        }

        AddTypeError($"Operator '!' cannot be applied to type {operand}", line, column);
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

    private void AddSemanticError(string message, int line, int column)
    {
        _semanticErrors.Add(new SemanticError(message, line, column));
    }
    
    private void AddTypeError(string message, int line, int column)
    {
        _typeErrors.Add(new TypeError(message, line, column));
    }
}