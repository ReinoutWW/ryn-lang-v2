using Antlr4.Runtime;
using GpLambda.Compiler.AST;
using GpLambda.Compiler.AST.Nodes;
using GpLambda.Compiler.Grammar;

namespace GpLambda.Tests;

public static class TestHelpers
{
    /// <summary>
    /// Parses GP-λ source code and returns the parse tree
    /// </summary>
    public static GpLambdaParser.ProgramContext ParseProgram(string source)
    {
        var inputStream = new AntlrInputStream(source);
        var lexer = new GpLambdaLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new GpLambdaParser(tokenStream);
        
        // Add error listener to collect errors
        var errorListener = new TestErrorListener();
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errorListener);
        
        var program = parser.program();
        
        if (errorListener.Errors.Count > 0)
        {
            throw new ParseException($"Parse errors: {string.Join("; ", errorListener.Errors)}");
        }
        
        return program;
    }
    
    /// <summary>
    /// Parses GP-λ source code and builds the AST
    /// </summary>
    public static ProgramNode BuildAst(string source)
    {
        var parseTree = ParseProgram(source);
        var astBuilder = new AstBuilder();
        return (ProgramNode)astBuilder.Visit(parseTree)!;
    }
    
    /// <summary>
    /// Custom error listener for tests
    /// </summary>
    private class TestErrorListener : BaseErrorListener
    {
        public List<string> Errors { get; } = new();
        
        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, 
            int line, int charPositionInLine, string msg, RecognitionException e)
        {
            Errors.Add($"Line {line}:{charPositionInLine} - {msg}");
        }
    }
}

public class ParseException : Exception
{
    public ParseException(string message) : base(message) { }
}