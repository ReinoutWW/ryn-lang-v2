namespace GpLambda.Compiler.AST.Nodes;

/// <summary>
/// Visitor interface for traversing the AST
/// </summary>
public interface IAstVisitor<T>
{
    // Program structure
    T VisitProgram(ProgramNode node);
    
    // Declarations
    T VisitFunctionDecl(FunctionDeclNode node);
    
    // Statements
    T VisitBlock(BlockStmtNode node);
    T VisitVarDecl(VarDeclStmtNode node);
    T VisitAssignment(AssignmentStmtNode node);
    T VisitIf(IfStmtNode node);
    T VisitReturn(ReturnStmtNode node);
    T VisitAssert(AssertStmtNode node);
    T VisitExpressionStmt(ExpressionStmtNode node);
    
    // Expressions
    T VisitIntLiteral(IntLiteralNode node);
    T VisitStringLiteral(StringLiteralNode node);
    T VisitBoolLiteral(BoolLiteralNode node);
    T VisitVariable(VariableNode node);
    T VisitLambda(LambdaNode node);
    T VisitCall(CallNode node);
    T VisitBinary(BinaryNode node);
    T VisitUnary(UnaryNode node);
}