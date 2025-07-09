using GpLambda.Compiler.AST.Nodes;

namespace GpLambda.Compiler.SemanticAnalysis;

/// <summary>
/// Manages symbols and scopes throughout the compilation process
/// </summary>
public class SymbolTable
{
    private Scope _currentScope;
    private readonly Scope _globalScope;

    public Scope CurrentScope => _currentScope;
    public Scope GlobalScope => _globalScope;

    public SymbolTable()
    {
        _globalScope = new Scope(null, ScopeType.Global);
        _currentScope = _globalScope;
        
        // Initialize built-in functions
        InitializeBuiltIns();
    }

    private void InitializeBuiltIns()
    {
        // println: (String) -> Void
        DefineBuiltIn("println", 
            new FunctionType(new List<AST.Nodes.Type> { PrimitiveType.String }, PrimitiveType.Void),
            new[] { new Parameter("message", PrimitiveType.String) });

        // readLine: () -> String
        DefineBuiltIn("readLine", 
            new FunctionType(new List<AST.Nodes.Type>(), PrimitiveType.String),
            new Parameter[0]);

        // toString: (Int) -> String
        DefineBuiltIn("toString", 
            new FunctionType(new List<AST.Nodes.Type> { PrimitiveType.Int }, PrimitiveType.String),
            new[] { new Parameter("value", PrimitiveType.Int) });
    }

    private void DefineBuiltIn(string name, FunctionType type, Parameter[] parameters)
    {
        var symbol = new BuiltInFunctionSymbol(name, type, parameters.ToList());
        _globalScope.Define(symbol);
    }

    /// <summary>
    /// Enters a new scope
    /// </summary>
    public void EnterScope(ScopeType type)
    {
        _currentScope = new Scope(_currentScope, type);
    }

    /// <summary>
    /// Exits the current scope and returns to the parent
    /// </summary>
    public void ExitScope()
    {
        if (_currentScope.Parent == null)
        {
            throw new InvalidOperationException("Cannot exit global scope");
        }
        _currentScope = _currentScope.Parent;
    }

    /// <summary>
    /// Defines a symbol in the current scope
    /// </summary>
    public void Define(Symbol symbol)
    {
        _currentScope.Define(symbol);
    }

    /// <summary>
    /// Resolves a symbol by name
    /// </summary>
    public Symbol? Resolve(string name)
    {
        return _currentScope.Resolve(name);
    }

    /// <summary>
    /// Checks if a symbol is defined in the current scope
    /// </summary>
    public bool IsDefinedLocally(string name)
    {
        return _currentScope.IsDefinedLocally(name);
    }
}