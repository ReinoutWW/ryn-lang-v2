namespace GpLambda.Compiler.AST.Nodes;

/// <summary>
/// Block statement containing multiple statements
/// </summary>
public class BlockStmtNode : Stmt
{
    public List<Stmt> Statements { get; }
    
    public BlockStmtNode(List<Stmt> statements)
    {
        Statements = statements;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBlock(this);
}

/// <summary>
/// Variable declaration statement
/// </summary>
public class VarDeclStmtNode : Stmt
{
    public string Name { get; }
    public Type? DeclaredType { get; }
    public Expr? Initializer { get; }
    
    public VarDeclStmtNode(string name, Type? declaredType, Expr? initializer)
    {
        Name = name;
        DeclaredType = declaredType;
        Initializer = initializer;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitVarDecl(this);
}

/// <summary>
/// Assignment statement
/// </summary>
public class AssignmentStmtNode : Stmt
{
    public string VariableName { get; }
    public Expr Value { get; }
    
    public AssignmentStmtNode(string variableName, Expr value)
    {
        VariableName = variableName;
        Value = value;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitAssignment(this);
}

/// <summary>
/// If statement with optional else branch
/// </summary>
public class IfStmtNode : Stmt
{
    public Expr Condition { get; }
    public BlockStmtNode ThenBranch { get; }
    public BlockStmtNode? ElseBranch { get; }
    
    public IfStmtNode(Expr condition, BlockStmtNode thenBranch, BlockStmtNode? elseBranch)
    {
        Condition = condition;
        ThenBranch = thenBranch;
        ElseBranch = elseBranch;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIf(this);
}

/// <summary>
/// Return statement
/// </summary>
public class ReturnStmtNode : Stmt
{
    public Expr? Value { get; }
    
    public ReturnStmtNode(Expr? value)
    {
        Value = value;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitReturn(this);
}

/// <summary>
/// Assert statement for runtime checks
/// </summary>
public class AssertStmtNode : Stmt
{
    public Expr Condition { get; }
    public string? Message { get; }
    
    public AssertStmtNode(Expr condition, string? message)
    {
        Condition = condition;
        Message = message;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitAssert(this);
}

/// <summary>
/// Expression statement (expression followed by semicolon)
/// </summary>
public class ExpressionStmtNode : Stmt
{
    public Expr Expression { get; }
    
    public ExpressionStmtNode(Expr expression)
    {
        Expression = expression;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitExpressionStmt(this);
}