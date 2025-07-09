grammar GpLambda;

// Parser Rules

program : declaration* EOF ;

declaration
    : functionDecl
    | statement
    ;

functionDecl
    : 'func' ID '(' paramList? ')' returnType? block
    ;

paramList
    : param (',' param)*
    ;

param
    : ID ':' type
    ;

returnType
    : '->' type
    ;

type
    : 'Int'                                  # PrimitiveType
    | 'String'                               # PrimitiveType
    | 'Bool'                                 # PrimitiveType
    | 'Void'                                 # PrimitiveType
    | 'Func' '<' typeList ',' type '>'     # FunctionType
    ;

typeList
    : type (',' type)*
    ;

block
    : '{' statement* '}'
    ;

statement
    : varDecl
    | assignStmt
    | ifStmt
    | returnStmt
    | assertStmt
    | exprStmt
    | block
    ;

varDecl
    : 'let' ID ':' type ('=' expr)? ';'    // Type required when no initializer
    | 'let' ID '=' expr ';'                 // Type optional with initializer
    ;

assignStmt
    : ID '=' expr ';'
    ;

ifStmt
    : 'if' '(' expr ')' block ('else' block)?
    ;

returnStmt
    : 'return' expr? ';'
    ;

assertStmt
    : 'assert' '(' expr (',' STRING)? ')' ';'
    ;

exprStmt
    : expr ';'
    ;

expr
    : primary                                         # PrimaryExpr
    | expr '.' ID '(' argList? ')'                  # MethodCallExpr
    | expr '(' argList? ')'                          # CallExpr
    | op=('-'|'!') expr                              # UnaryExpr
    | expr op=('*'|'/'|'%') expr                     # MultiplicativeExpr
    | expr op=('+'|'-') expr                         # AdditiveExpr
    | expr op=('<'|'>'|'<='|'>=') expr               # RelationalExpr
    | expr op=('=='|'!=') expr                       # EqualityExpr
    | expr '&&' expr                                 # LogicalAndExpr
    | expr '||' expr                                 # LogicalOrExpr
    | '(' paramList? ')' '=>' (expr | block)         # LambdaExpr
    ;

primary
    : INT                          # IntLiteral
    | STRING                       # StringLiteral
    | 'true'                       # BoolLiteral
    | 'false'                      # BoolLiteral
    | ID                           # VarExpr
    | '(' expr ')'                 # ParenExpr
    ;

argList
    : expr (',' expr)*
    ;

// Lexer Rules

// Keywords
FUNC    : 'func' ;
LET     : 'let' ;
IF      : 'if' ;
ELSE    : 'else' ;
RETURN  : 'return' ;
ASSERT  : 'assert' ;
TRUE    : 'true' ;
FALSE   : 'false' ;

// Types
INT_TYPE    : 'Int' ;
STRING_TYPE : 'String' ;
BOOL_TYPE   : 'Bool' ;
VOID_TYPE   : 'Void' ;
FUNC_TYPE   : 'Func' ;

// Operators and Delimiters
ARROW       : '->' ;
LAMBDA_ARROW: '=>' ;
EQ          : '==' ;
NEQ         : '!=' ;
LT          : '<' ;
GT          : '>' ;
LE          : '<=' ;
GE          : '>=' ;
AND         : '&&' ;
OR          : '||' ;
PLUS        : '+' ;
MINUS       : '-' ;
MULT        : '*' ;
DIV         : '/' ;
MOD         : '%' ;
NOT         : '!' ;
ASSIGN      : '=' ;
LPAREN      : '(' ;
RPAREN      : ')' ;
LBRACE      : '{' ;
RBRACE      : '}' ;
SEMI        : ';' ;
COMMA       : ',' ;
COLON       : ':' ;

// Identifiers and Literals
ID      : [a-zA-Z_][a-zA-Z0-9_]* ;
INT     : [0-9]+ ;
STRING  : '"' (~["\r\n\\] | '\\' .)* '"' ;

// Built-in functions (treated as identifiers but documented for clarity)
// println, readLine, toString etc. will be handled as regular function calls

// Whitespace and Comments
WS      : [ \t\r\n]+ -> skip ;
COMMENT : '//' ~[\r\n]* -> skip ;
BLOCK_COMMENT : '/*' .*? '*/' -> skip ;