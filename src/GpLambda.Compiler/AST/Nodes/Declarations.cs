namespace GpLambda.Compiler.AST.Nodes;

/// <summary>
/// Root node of the AST representing the entire program
/// </summary>
public class ProgramNode : Node
{
    public List<Node> Declarations { get; }
    
    public ProgramNode(List<Node> declarations)
    {
        Declarations = declarations;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitProgram(this);
}

/// <summary>
/// Function declaration node
/// </summary>
public class FunctionDeclNode : Stmt
{
    public string Name { get; }
    public List<Parameter> Parameters { get; }
    public Type? ReturnType { get; }
    public BlockStmtNode Body { get; }
    
    public FunctionDeclNode(string name, List<Parameter> parameters, Type? returnType, BlockStmtNode body)
    {
        Name = name;
        Parameters = parameters;
        ReturnType = returnType;
        Body = body;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitFunctionDecl(this);
}

/// <summary>
/// Function parameter
/// </summary>
public class Parameter
{
    public string Name { get; }
    public Type Type { get; }
    
    public Parameter(string name, Type type)
    {
        Name = name;
        Type = type;
    }
}