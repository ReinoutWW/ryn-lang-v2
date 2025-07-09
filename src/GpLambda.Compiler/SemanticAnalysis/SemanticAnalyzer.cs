using GpLambda.Compiler.AST;
using GpLambda.Compiler.AST.Nodes;

namespace GpLambda.Compiler.SemanticAnalysis;

/// <summary>
/// Performs semantic analysis on the AST, building the symbol table
/// and checking for semantic errors
/// </summary>
public class SemanticAnalyzer : IAstVisitor<object?>
{
    private readonly SymbolTable _symbolTable;
    private readonly List<SemanticError> _errors = new();

    public SymbolTable SymbolTable => _symbolTable;
    public IReadOnlyList<SemanticError> Errors => _errors;
    public bool HasErrors => _errors.Count > 0;

    public SemanticAnalyzer()
    {
        _symbolTable = new SymbolTable();
    }

    public void Analyze(ProgramNode program)
    {
        program.Accept(this);
    }

    // Program and declarations

    public object? VisitProgram(ProgramNode node)
    {
        foreach (var declaration in node.Declarations)
        {
            declaration.Accept(this);
        }
        return null;
    }

    public object? VisitFunctionDecl(FunctionDeclNode node)
    {
        // Check if function is already defined
        if (_symbolTable.IsDefinedLocally(node.Name))
        {
            AddError($"Function '{node.Name}' is already defined", node.Line, node.Column);
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

        // Exit function scope
        _symbolTable.ExitScope();

        return null;
    }

    // Statements

    public object? VisitBlock(BlockStmtNode node)
    {
        _symbolTable.EnterScope(ScopeType.Block);
        
        foreach (var stmt in node.Statements)
        {
            stmt.Accept(this);
        }
        
        _symbolTable.ExitScope();
        return null;
    }

    public object? VisitVarDecl(VarDeclStmtNode node)
    {
        // Check if variable is already defined in current scope
        if (_symbolTable.IsDefinedLocally(node.Name))
        {
            AddError($"Variable '{node.Name}' is already defined in this scope", node.Line, node.Column);
            return null;
        }

        // Visit initializer if present
        if (node.Initializer != null)
        {
            node.Initializer.Accept(this);
        }

        // Determine type
        AST.Nodes.Type type;
        if (node.DeclaredType != null)
        {
            type = node.DeclaredType;
        }
        else if (node.Initializer != null)
        {
            // Simple type inference for lambda expressions
            if (node.Initializer is LambdaNode lambda)
            {
                // Create function type from lambda
                var paramTypes = lambda.Parameters.Select(p => p.Type).ToList();
                // TODO: Infer return type from lambda body
                var returnType = PrimitiveType.Int; // Simplified for now
                type = new FunctionType(paramTypes, returnType);
            }
            else
            {
                // For other expressions, default to Int (simplified)
                type = PrimitiveType.Int; // TODO: Implement full type inference
            }
        }
        else
        {
            AddError($"Variable '{node.Name}' must have a type annotation or initializer", node.Line, node.Column);
            return null;
        }
        
        var symbol = new VariableSymbol(node.Name, type, node.Line, node.Column)
        {
            IsInitialized = node.Initializer != null
        };
        _symbolTable.Define(symbol);

        return null;
    }

    public object? VisitAssignment(AssignmentStmtNode node)
    {
        // Check if variable exists
        var symbol = _symbolTable.Resolve(node.VariableName);
        if (symbol == null)
        {
            AddError($"Undefined variable '{node.VariableName}'", node.Line, node.Column);
            return null;
        }

        if (symbol is not VariableSymbol varSymbol)
        {
            AddError($"'{node.VariableName}' is not a variable", node.Line, node.Column);
            return null;
        }

        // Visit the value expression
        node.Value.Accept(this);

        // Mark variable as initialized
        varSymbol.IsInitialized = true;

        return null;
    }

    public object? VisitIf(IfStmtNode node)
    {
        node.Condition.Accept(this);
        node.ThenBranch.Accept(this);
        node.ElseBranch?.Accept(this);
        return null;
    }

    public object? VisitReturn(ReturnStmtNode node)
    {
        node.Value?.Accept(this);
        // TODO: Check return type matches function return type
        return null;
    }

    public object? VisitAssert(AssertStmtNode node)
    {
        node.Condition.Accept(this);
        return null;
    }

    public object? VisitExpressionStmt(ExpressionStmtNode node)
    {
        node.Expression.Accept(this);
        return null;
    }

    // Expressions

    public object? VisitIntLiteral(IntLiteralNode node)
    {
        return null;
    }

    public object? VisitStringLiteral(StringLiteralNode node)
    {
        return null;
    }

    public object? VisitBoolLiteral(BoolLiteralNode node)
    {
        return null;
    }

    public object? VisitVariable(VariableNode node)
    {
        var symbol = _symbolTable.Resolve(node.Name);
        if (symbol == null)
        {
            AddError($"Undefined variable '{node.Name}'", node.Line, node.Column);
            return null;
        }

        if (symbol is VariableSymbol varSymbol)
        {
            if (!varSymbol.IsInitialized)
            {
                AddError($"Variable '{node.Name}' may not be initialized", node.Line, node.Column);
            }
            varSymbol.IsUsed = true;
        }

        return null;
    }

    public object? VisitBinary(BinaryNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        return null;
    }

    public object? VisitUnary(UnaryNode node)
    {
        node.Operand.Accept(this);
        return null;
    }

    public object? VisitCall(CallNode node)
    {
        var symbol = _symbolTable.Resolve(node.FunctionName);
        if (symbol == null)
        {
            AddError($"Undefined function '{node.FunctionName}'", node.Line, node.Column);
            return null;
        }

        // Handle both function symbols and variables with function types
        if (symbol is FunctionSymbol funcSymbol)
        {
            // Check argument count
            if (node.Arguments.Count != funcSymbol.Parameters.Count)
            {
                AddError($"Function '{node.FunctionName}' expects {funcSymbol.Parameters.Count} arguments, but {node.Arguments.Count} were provided", 
                    node.Line, node.Column);
            }
        }
        else if (symbol is VariableSymbol varSymbol && varSymbol.Type is FunctionType funcType)
        {
            // Variable holds a function value
            // Check argument count against function type
            if (node.Arguments.Count != funcType.ParameterTypes.Count)
            {
                AddError($"Function '{node.FunctionName}' expects {funcType.ParameterTypes.Count} arguments, but {node.Arguments.Count} were provided", 
                    node.Line, node.Column);
            }
        }
        else
        {
            AddError($"'{node.FunctionName}' is not a function", node.Line, node.Column);
            return null;
        }

        // Visit arguments
        foreach (var arg in node.Arguments)
        {
            arg.Accept(this);
        }

        return null;
    }

    public object? VisitLambda(LambdaNode node)
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

        // Visit body
        node.Body.Accept(this);

        _symbolTable.ExitScope();
        return null;
    }

    private void AddError(string message, int line, int column)
    {
        _errors.Add(new SemanticError(message, line, column));
    }
}

public record SemanticError(string Message, int Line, int Column);