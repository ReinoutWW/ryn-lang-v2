namespace GpLambda.Compiler.AST.Nodes;

/// <summary>
/// Represents a type in the GP-λ type system
/// </summary>
public abstract class Type
{
    public abstract bool Equals(Type other);
    public abstract override string ToString();
}

/// <summary>
/// Primitive types (Int, String, Bool, Void)
/// </summary>
public class PrimitiveType : Type
{
    public PrimitiveKind Kind { get; }
    
    public PrimitiveType(PrimitiveKind kind)
    {
        Kind = kind;
    }
    
    public override bool Equals(Type other)
    {
        return other is PrimitiveType pt && pt.Kind == Kind;
    }
    
    public override string ToString() => Kind.ToString();
    
    // Singleton instances for common types
    public static readonly PrimitiveType Int = new(PrimitiveKind.Int);
    public static readonly PrimitiveType String = new(PrimitiveKind.String);
    public static readonly PrimitiveType Bool = new(PrimitiveKind.Bool);
    public static readonly PrimitiveType Void = new(PrimitiveKind.Void);
}

/// <summary>
/// Function type (Func<T1, T2, ..., TResult>)
/// </summary>
public class FunctionType : Type
{
    public List<Type> ParameterTypes { get; }
    public Type ReturnType { get; }
    
    public FunctionType(List<Type> parameterTypes, Type returnType)
    {
        ParameterTypes = parameterTypes;
        ReturnType = returnType;
    }
    
    public override bool Equals(Type other)
    {
        if (other is not FunctionType ft) return false;
        if (ft.ParameterTypes.Count != ParameterTypes.Count) return false;
        if (!ft.ReturnType.Equals(ReturnType)) return false;
        
        for (int i = 0; i < ParameterTypes.Count; i++)
        {
            if (!ParameterTypes[i].Equals(ft.ParameterTypes[i]))
                return false;
        }
        
        return true;
    }
    
    public override string ToString()
    {
        var paramStr = string.Join(", ", ParameterTypes.Select(t => t.ToString()));
        return $"Func<{paramStr}, {ReturnType}>";
    }
}

/// <summary>
/// Primitive type kinds
/// </summary>
public enum PrimitiveKind
{
    Int,
    String,
    Bool,
    Void
}