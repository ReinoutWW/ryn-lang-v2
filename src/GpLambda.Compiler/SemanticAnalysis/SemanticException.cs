namespace GpLambda.Compiler.SemanticAnalysis;

/// <summary>
/// Exception thrown during semantic analysis
/// </summary>
public class SemanticException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public SemanticException(string message, int line, int column) 
        : base($"Semantic error at line {line}, column {column}: {message}")
    {
        Line = line;
        Column = column;
    }
}