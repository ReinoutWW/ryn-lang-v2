namespace GpLambda.Compiler.AST.Nodes;

/// <summary>
/// Integer literal expression
/// </summary>
public class IntLiteralNode : Expr
{
    public int Value { get; }
    
    public IntLiteralNode(int value)
    {
        Value = value;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIntLiteral(this);
}

/// <summary>
/// String literal expression
/// </summary>
public class StringLiteralNode : Expr
{
    public string Value { get; }
    
    public StringLiteralNode(string value)
    {
        Value = value;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitStringLiteral(this);
}

/// <summary>
/// Boolean literal expression
/// </summary>
public class BoolLiteralNode : Expr
{
    public bool Value { get; }
    
    public BoolLiteralNode(bool value)
    {
        Value = value;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBoolLiteral(this);
}

/// <summary>
/// Variable reference expression
/// </summary>
public class VariableNode : Expr
{
    public string Name { get; }
    
    public VariableNode(string name)
    {
        Name = name;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitVariable(this);
}

/// <summary>
/// Lambda expression
/// </summary>
public class LambdaNode : Expr
{
    public List<Parameter> Parameters { get; }
    public Node Body { get; } // Can be either Expr or BlockStmtNode
    
    public LambdaNode(List<Parameter> parameters, Node body)
    {
        Parameters = parameters;
        Body = body;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLambda(this);
}

/// <summary>
/// Function call expression
/// </summary>
public class CallNode : Expr
{
    public string FunctionName { get; }
    public List<Expr> Arguments { get; }
    
    public CallNode(string functionName, List<Expr> arguments)
    {
        FunctionName = functionName;
        Arguments = arguments;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitCall(this);
}

/// <summary>
/// Binary operator expression
/// </summary>
public class BinaryNode : Expr
{
    public Expr Left { get; }
    public BinaryOperator Operator { get; }
    public Expr Right { get; }
    
    public BinaryNode(Expr left, BinaryOperator op, Expr right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBinary(this);
}

/// <summary>
/// Unary operator expression
/// </summary>
public class UnaryNode : Expr
{
    public UnaryOperator Operator { get; }
    public Expr Operand { get; }
    
    public UnaryNode(UnaryOperator op, Expr operand)
    {
        Operator = op;
        Operand = operand;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUnary(this);
}

/// <summary>
/// Binary operators
/// </summary>
public enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    Equal,
    NotEqual,
    LessThan,
    GreaterThan,
    LessOrEqual,
    GreaterOrEqual,
    And,
    Or
}

/// <summary>
/// Unary operators
/// </summary>
public enum UnaryOperator
{
    Negate,
    Not
}