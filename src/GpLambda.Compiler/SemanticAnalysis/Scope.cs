using System.Collections.Generic;

namespace GpLambda.Compiler.SemanticAnalysis;

/// <summary>
/// Represents a lexical scope in the program
/// </summary>
public class Scope
{
    private readonly Dictionary<string, Symbol> _symbols = new();
    public Scope? Parent { get; }
    public List<Scope> Children { get; } = new();
    public ScopeType Type { get; }

    public Scope(Scope? parent, ScopeType type)
    {
        Parent = parent;
        Type = type;
        parent?.Children.Add(this);
    }

    /// <summary>
    /// Defines a symbol in this scope
    /// </summary>
    public void Define(Symbol symbol)
    {
        if (_symbols.ContainsKey(symbol.Name))
        {
            throw new SemanticException(
                $"Symbol '{symbol.Name}' is already defined in this scope",
                symbol.Line,
                symbol.Column);
        }
        _symbols[symbol.Name] = symbol;
    }

    /// <summary>
    /// Resolves a symbol by name, searching up the scope chain
    /// </summary>
    public Symbol? Resolve(string name)
    {
        if (_symbols.TryGetValue(name, out var symbol))
        {
            return symbol;
        }
        return Parent?.Resolve(name);
    }

    /// <summary>
    /// Checks if a symbol is defined in this scope (not parent scopes)
    /// </summary>
    public bool IsDefinedLocally(string name)
    {
        return _symbols.ContainsKey(name);
    }

    /// <summary>
    /// Gets all symbols defined in this scope
    /// </summary>
    public IEnumerable<Symbol> GetSymbols()
    {
        return _symbols.Values;
    }
}

public enum ScopeType
{
    Global,
    Function,
    Block,
    Lambda
}