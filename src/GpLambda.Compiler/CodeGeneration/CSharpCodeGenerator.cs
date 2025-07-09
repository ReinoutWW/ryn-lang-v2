using System.Text;
using GpLambda.Compiler.AST;
using GpLambda.Compiler.AST.Nodes;
using GpLambda.Compiler.SemanticAnalysis;

namespace GpLambda.Compiler.CodeGeneration;

/// <summary>
/// Generates C# code from the GP-λ AST with integrated semantic analysis
/// </summary>
public class CSharpCodeGenerator
{
    private readonly StringBuilder _code = new();
    private readonly CombinedAnalyzer _analyzer;
    private readonly CodeGeneratorVisitor _visitor;

    public CSharpCodeGenerator()
    {
        _analyzer = new CombinedAnalyzer();
        _visitor = new CodeGeneratorVisitor(_analyzer);
    }

    public string GenerateCode(ProgramNode program)
    {
        // First analyze the program
        _analyzer.Analyze(program);
        
        if (_analyzer.HasErrors)
        {
            throw new InvalidOperationException("Cannot generate code due to semantic/type errors");
        }
        
        // Then generate code
        return _visitor.GenerateCode(program);
    }
    
    private class CodeGeneratorVisitor : IAstVisitor<object?>
    {
        private readonly StringBuilder _code = new();
        private readonly CombinedAnalyzer _analyzer;
        private readonly Stack<Dictionary<string, AST.Nodes.Type>> _localScopes = new();
        private int _indentLevel = 0;
        private const string IndentString = "    ";

        public CodeGeneratorVisitor(CombinedAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        public string GenerateCode(ProgramNode program)
        {
            _code.Clear();
            _indentLevel = 0;
            
            // Generate file header
            AppendLine("// Generated from GP-λ source");
            AppendLine("using System;");
            AppendLine();
            
            // Generate program class
            AppendLine("public static class Program");
            AppendLine("{");
            _indentLevel++;
            
            // Generate built-in function implementations
            GenerateBuiltInFunctions();
            
            // Visit all declarations
            program.Accept(this);
            
            // Generate Main method if needed
            GenerateMainMethod();
            
            _indentLevel--;
            AppendLine("}");
            
            return _code.ToString();
        }

        private void GenerateBuiltInFunctions()
        {
            // println implementation
            AppendLine("private static void println(string message)");
            AppendLine("{");
            _indentLevel++;
            AppendLine("Console.WriteLine(message);");
            _indentLevel--;
            AppendLine("}");
            AppendLine();
            
            // readLine implementation
            AppendLine("private static string readLine()");
            AppendLine("{");
            _indentLevel++;
            AppendLine("return Console.ReadLine() ?? string.Empty;");
            _indentLevel--;
            AppendLine("}");
            AppendLine();
            
            // toString implementation
            AppendLine("private static string toString(int value)");
            AppendLine("{");
            _indentLevel++;
            AppendLine("return value.ToString();");
            _indentLevel--;
            AppendLine("}");
            AppendLine();
        }

        private void GenerateMainMethod()
        {
            // Check if we have a main function in GP-λ
            var mainSymbol = _analyzer.SymbolTable.GlobalScope.Resolve("main");
            if (mainSymbol is FunctionSymbol)
            {
                AppendLine("public static void Main(string[] args)");
                AppendLine("{");
                _indentLevel++;
                AppendLine("main();");
                _indentLevel--;
                AppendLine("}");
            }
        }

        // Visit methods

        public object? VisitProgram(ProgramNode node)
        {
            foreach (var declaration in node.Declarations)
            {
                declaration.Accept(this);
                AppendLine();
            }
            return null;
        }

        public object? VisitFunctionDecl(FunctionDeclNode node)
        {
            // Enter new scope for parameters
            _localScopes.Push(new Dictionary<string, AST.Nodes.Type>());
            
            // Add parameters to scope
            foreach (var param in node.Parameters)
            {
                _localScopes.Peek()[param.Name] = param.Type;
            }
            
            // Generate method signature
            var returnType = GetCSharpType(node.ReturnType ?? PrimitiveType.Void);
            var signature = $"private static {returnType} {node.Name}(";
            
            // Parameters
            var paramList = new List<string>();
            foreach (var param in node.Parameters)
            {
                paramList.Add($"{GetCSharpType(param.Type)} {param.Name}");
            }
            signature += string.Join(", ", paramList);
            signature += ")";
            
            AppendLine(signature);
            
            // Body
            node.Body.Accept(this);
            
            // Exit scope
            _localScopes.Pop();
            
            return null;
        }

        public object? VisitBlock(BlockStmtNode node)
        {
            AppendLine("{");
            _indentLevel++;
            
            foreach (var stmt in node.Statements)
            {
                stmt.Accept(this);
            }
            
            _indentLevel--;
            AppendLine("}");
            return null;
        }

        public object? VisitVarDecl(VarDeclStmtNode node)
        {
            // Determine type - first check declared type, then infer from initializer
            AST.Nodes.Type type;
            
            if (node.DeclaredType != null)
            {
                type = node.DeclaredType;
            }
            else if (node.Initializer != null)
            {
                type = InferExpressionType(node.Initializer);
            }
            else
            {
                type = PrimitiveType.Int; // Default
            }
            
            // Add to current scope
            if (_localScopes.Count > 0)
            {
                _localScopes.Peek()[node.Name] = type;
            }
            
            // Generate declaration
            var line = $"{GetCSharpType(type)} {node.Name}";
            
            if (node.Initializer != null)
            {
                line += " = ";
                var initCode = GenerateExpression(node.Initializer);
                line += initCode;
            }
            else if (type.Equals(PrimitiveType.String))
            {
                line += " = string.Empty";
            }
            else if (type.Equals(PrimitiveType.Int))
            {
                line += " = 0";
            }
            else if (type.Equals(PrimitiveType.Bool))
            {
                line += " = false";
            }
            
            line += ";";
            AppendLine(line);
            return null;
        }

        public object? VisitAssignment(AssignmentStmtNode node)
        {
            var line = $"{node.VariableName} = {GenerateExpression(node.Value)};";
            AppendLine(line);
            return null;
        }

        public object? VisitIf(IfStmtNode node)
        {
            AppendLine($"if ({GenerateExpression(node.Condition)})");
            node.ThenBranch.Accept(this);
            
            if (node.ElseBranch != null)
            {
                AppendLine("else");
                node.ElseBranch.Accept(this);
            }
            
            return null;
        }

        public object? VisitReturn(ReturnStmtNode node)
        {
            if (node.Value != null)
            {
                AppendLine($"return {GenerateExpression(node.Value)};");
            }
            else
            {
                AppendLine("return;");
            }
            return null;
        }

        public object? VisitAssert(AssertStmtNode node)
        {
            var line = $"System.Diagnostics.Debug.Assert({GenerateExpression(node.Condition)}";
            
            if (!string.IsNullOrEmpty(node.Message))
            {
                line += $", {EscapeString(node.Message)}";
            }
            
            line += ");";
            AppendLine(line);
            return null;
        }

        public object? VisitExpressionStmt(ExpressionStmtNode node)
        {
            AppendLine($"{GenerateExpression(node.Expression)};");
            return null;
        }

        // Expression generation
        private string GenerateExpression(Expr expr)
        {
            return expr switch
            {
                IntLiteralNode n => n.Value.ToString(),
                StringLiteralNode n => EscapeString(n.Value),
                BoolLiteralNode n => n.Value ? "true" : "false",
                VariableNode n => n.Name,
                BinaryNode n => $"({GenerateExpression(n.Left)} {GetBinaryOperator(n.Operator)} {GenerateExpression(n.Right)})",
                UnaryNode n => $"({GetUnaryOperator(n.Operator)}{GenerateExpression(n.Operand)})",
                CallNode n => GenerateCall(n),
                LambdaNode n => GenerateLambda(n),
                _ => throw new NotSupportedException($"Expression type {expr.GetType()} not supported")
            };
        }

        private string GenerateCall(CallNode node)
        {
            var args = node.Arguments.Select(GenerateExpression);
            return $"{node.FunctionName}({string.Join(", ", args)})";
        }

        private string GenerateLambda(LambdaNode node)
        {
            var paramList = new List<string>();
            foreach (var param in node.Parameters)
            {
                paramList.Add($"{GetCSharpType(param.Type)} {param.Name}");
            }
            
            var lambda = $"({string.Join(", ", paramList)}) => ";
            
            if (node.Body is Expr expr)
            {
                lambda += GenerateExpression(expr);
            }
            else
            {
                // For block body, we need to generate a statement lambda
                throw new NotSupportedException("Block lambdas not yet supported");
            }
            
            return lambda;
        }

        private string GetBinaryOperator(BinaryOperator op)
        {
            return op switch
            {
                BinaryOperator.Add => "+",
                BinaryOperator.Subtract => "-",
                BinaryOperator.Multiply => "*",
                BinaryOperator.Divide => "/",
                BinaryOperator.Modulo => "%",
                BinaryOperator.Equal => "==",
                BinaryOperator.NotEqual => "!=",
                BinaryOperator.LessThan => "<",
                BinaryOperator.GreaterThan => ">",
                BinaryOperator.LessOrEqual => "<=",
                BinaryOperator.GreaterOrEqual => ">=",
                BinaryOperator.And => "&&",
                BinaryOperator.Or => "||",
                _ => throw new NotSupportedException($"Operator {op} not supported")
            };
        }

        private string GetUnaryOperator(UnaryOperator op)
        {
            return op switch
            {
                UnaryOperator.Negate => "-",
                UnaryOperator.Not => "!",
                _ => throw new NotSupportedException($"Operator {op} not supported")
            };
        }

        // Type inference
        private AST.Nodes.Type InferExpressionType(Expr expr)
        {
            return expr switch
            {
                IntLiteralNode => PrimitiveType.Int,
                StringLiteralNode => PrimitiveType.String,
                BoolLiteralNode => PrimitiveType.Bool,
                VariableNode n => LookupVariableType(n.Name),
                BinaryNode n => InferBinaryType(n),
                UnaryNode n => InferUnaryType(n),
                CallNode n => InferCallType(n),
                LambdaNode n => InferLambdaType(n),
                _ => PrimitiveType.Int // Default
            };
        }

        private AST.Nodes.Type LookupVariableType(string name)
        {
            // Check local scopes
            foreach (var scope in _localScopes)
            {
                if (scope.ContainsKey(name))
                    return scope[name];
            }
            
            // Check global scope
            var symbol = _analyzer.SymbolTable.GlobalScope.Resolve(name);
            if (symbol != null)
                return symbol.Type;
            
            return PrimitiveType.Int; // Default
        }

        private AST.Nodes.Type InferBinaryType(BinaryNode node)
        {
            var leftType = InferExpressionType(node.Left);
            var rightType = InferExpressionType(node.Right);
            
            return node.Operator switch
            {
                BinaryOperator.Add when leftType.Equals(PrimitiveType.String) || rightType.Equals(PrimitiveType.String) => PrimitiveType.String,
                BinaryOperator.Add or BinaryOperator.Subtract or BinaryOperator.Multiply or BinaryOperator.Divide or BinaryOperator.Modulo => PrimitiveType.Int,
                BinaryOperator.Equal or BinaryOperator.NotEqual or BinaryOperator.LessThan or BinaryOperator.GreaterThan or BinaryOperator.LessOrEqual or BinaryOperator.GreaterOrEqual => PrimitiveType.Bool,
                BinaryOperator.And or BinaryOperator.Or => PrimitiveType.Bool,
                _ => PrimitiveType.Int
            };
        }

        private AST.Nodes.Type InferUnaryType(UnaryNode node)
        {
            return node.Operator switch
            {
                UnaryOperator.Negate => PrimitiveType.Int,
                UnaryOperator.Not => PrimitiveType.Bool,
                _ => PrimitiveType.Int
            };
        }

        private AST.Nodes.Type InferCallType(CallNode node)
        {
            // First check local scopes for lambda variables
            foreach (var scope in _localScopes)
            {
                if (scope.ContainsKey(node.FunctionName))
                {
                    var type = scope[node.FunctionName];
                    if (type is FunctionType funcType)
                    {
                        return funcType.ReturnType;
                    }
                }
            }
            
            // Then check global scope
            var symbol = _analyzer.SymbolTable.GlobalScope.Resolve(node.FunctionName);
            if (symbol is FunctionSymbol funcSymbol && funcSymbol.Type is FunctionType globalFuncType)
            {
                return globalFuncType.ReturnType;
            }
            else if (symbol is VariableSymbol varSymbol && varSymbol.Type is FunctionType varFuncType)
            {
                return varFuncType.ReturnType;
            }
            
            return PrimitiveType.Void;
        }

        private AST.Nodes.Type InferLambdaType(LambdaNode node)
        {
            var paramTypes = node.Parameters.Select(p => p.Type).ToList();
            var returnType = node.Body is Expr expr ? InferExpressionType(expr) : PrimitiveType.Void;
            return new FunctionType(paramTypes, returnType);
        }

        // Helper methods
        private void AppendLine(string text = "")
        {
            if (!string.IsNullOrEmpty(text))
            {
                _code.Append(new string(' ', _indentLevel * 4));
                _code.AppendLine(text);
            }
            else
            {
                _code.AppendLine();
            }
        }

        private string GetCSharpType(AST.Nodes.Type type)
        {
            if (type.Equals(PrimitiveType.Int))
                return "int";
            if (type.Equals(PrimitiveType.String))
                return "string";
            if (type.Equals(PrimitiveType.Bool))
                return "bool";
            if (type.Equals(PrimitiveType.Void))
                return "void";
            
            if (type is FunctionType funcType)
            {
                // Generate delegate type
                var paramTypes = string.Join(", ", funcType.ParameterTypes.Select(GetCSharpType));
                var returnType = GetCSharpType(funcType.ReturnType);
                
                if (funcType.ReturnType.Equals(PrimitiveType.Void))
                {
                    return funcType.ParameterTypes.Count switch
                    {
                        0 => "Action",
                        1 => $"Action<{paramTypes}>",
                        _ => $"Action<{paramTypes}>"
                    };
                }
                else
                {
                    return funcType.ParameterTypes.Count switch
                    {
                        0 => $"Func<{returnType}>",
                        _ => $"Func<{paramTypes}, {returnType}>"
                    };
                }
            }
            
            throw new NotSupportedException($"Type {type} not supported");
        }

        private string EscapeString(string value)
        {
            // Escape the string for C#
            var escaped = value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
            
            return $"\"{escaped}\"";
        }

        // Expression visitor stubs (not used, we use GenerateExpression instead)
        public object? VisitIntLiteral(IntLiteralNode node) => null;
        public object? VisitStringLiteral(StringLiteralNode node) => null;
        public object? VisitBoolLiteral(BoolLiteralNode node) => null;
        public object? VisitVariable(VariableNode node) => null;
        public object? VisitBinary(BinaryNode node) => null;
        public object? VisitUnary(UnaryNode node) => null;
        public object? VisitCall(CallNode node) => null;
        public object? VisitLambda(LambdaNode node) => null;
    }
}