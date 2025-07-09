using GpLambda.Compiler.AST.Nodes;

namespace GpLambda.Compiler.SemanticAnalysis;

/// <summary>
/// Represents a symbol in the symbol table
/// </summary>
public abstract class Symbol
{
    public string Name { get; }
    public AST.Nodes.Type Type { get; }
    public int Line { get; }
    public int Column { get; }

    protected Symbol(string name, AST.Nodes.Type type, int line, int column)
    {
        Name = name;
        Type = type;
        Line = line;
        Column = column;
    }
}

/// <summary>
/// Represents a variable symbol
/// </summary>
public class VariableSymbol : Symbol
{
    public bool IsInitialized { get; set; }
    public bool IsUsed { get; set; }

    public VariableSymbol(string name, AST.Nodes.Type type, int line, int column) 
        : base(name, type, line, column)
    {
        IsInitialized = false;
        IsUsed = false;
    }
}

/// <summary>
/// Represents a function symbol
/// </summary>
public class FunctionSymbol : Symbol
{
    public List<Parameter> Parameters { get; }
    public bool IsDefined { get; set; }

    public FunctionSymbol(string name, FunctionType type, List<Parameter> parameters, int line, int column) 
        : base(name, type, line, column)
    {
        Parameters = parameters;
        IsDefined = false;
    }
}

/// <summary>
/// Represents a built-in function symbol
/// </summary>
public class BuiltInFunctionSymbol : FunctionSymbol
{
    public BuiltInFunctionSymbol(string name, FunctionType type, List<Parameter> parameters) 
        : base(name, type, parameters, 0, 0)
    {
        IsDefined = true; // Built-in functions are always defined
    }
}