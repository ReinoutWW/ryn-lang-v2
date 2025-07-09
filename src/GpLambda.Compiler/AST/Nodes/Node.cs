namespace GpLambda.Compiler.AST.Nodes;

/// <summary>
/// Base class for all AST nodes
/// </summary>
public abstract class Node
{
    /// <summary>
    /// Line number in source code (1-based)
    /// </summary>
    public int Line { get; set; }
    
    /// <summary>
    /// Column number in source code (1-based)
    /// </summary>
    public int Column { get; set; }
    
    /// <summary>
    /// Accept a visitor for tree traversal
    /// </summary>
    public abstract T Accept<T>(IAstVisitor<T> visitor);
}

/// <summary>
/// Base class for all statement nodes
/// </summary>
public abstract class Stmt : Node
{
}

/// <summary>
/// Base class for all expression nodes
/// </summary>
public abstract class Expr : Node
{
    /// <summary>
    /// The resolved type of this expression (filled in during semantic analysis)
    /// </summary>
    public Type? ResolvedType { get; set; }
}