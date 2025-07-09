using Xunit;
using FluentAssertions;
using GpLambda.Compiler.SemanticAnalysis;
using GpLambda.Compiler.AST.Nodes;

namespace GpLambda.Tests;

public class SymbolTableTests
{
    [Fact]
    public void SymbolTable_ShouldInitializeWithBuiltIns()
    {
        // Arrange & Act
        var symbolTable = new SymbolTable();
        
        // Assert
        var println = symbolTable.Resolve("println");
        println.Should().NotBeNull();
        println.Should().BeOfType<BuiltInFunctionSymbol>();
        println!.Type.Should().BeOfType<FunctionType>()
            .Which.ReturnType.Should().Be(PrimitiveType.Void);
        
        var readLine = symbolTable.Resolve("readLine");
        readLine.Should().NotBeNull();
        readLine.Should().BeOfType<BuiltInFunctionSymbol>();
        
        var toString = symbolTable.Resolve("toString");
        toString.Should().NotBeNull();
        toString.Should().BeOfType<BuiltInFunctionSymbol>();
    }
    
    [Fact]
    public void Define_ShouldAddSymbolToCurrentScope()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        var symbol = new VariableSymbol("x", PrimitiveType.Int, 1, 1);
        
        // Act
        symbolTable.Define(symbol);
        
        // Assert
        var resolved = symbolTable.Resolve("x");
        resolved.Should().Be(symbol);
    }
    
    [Fact]
    public void Define_ShouldThrowOnDuplicateSymbol()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        var symbol1 = new VariableSymbol("x", PrimitiveType.Int, 1, 1);
        var symbol2 = new VariableSymbol("x", PrimitiveType.String, 2, 1);
        
        // Act
        symbolTable.Define(symbol1);
        var action = () => symbolTable.Define(symbol2);
        
        // Assert
        action.Should().Throw<SemanticException>()
            .WithMessage("*Symbol 'x' is already defined in this scope*");
    }
    
    [Fact]
    public void Resolve_ShouldSearchUpScopeChain()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        var outerSymbol = new VariableSymbol("x", PrimitiveType.Int, 1, 1);
        symbolTable.Define(outerSymbol);
        
        // Act
        symbolTable.EnterScope(ScopeType.Block);
        var resolved = symbolTable.Resolve("x");
        
        // Assert
        resolved.Should().Be(outerSymbol);
    }
    
    [Fact]
    public void InnerScope_ShouldShadowOuterScope()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        var outerSymbol = new VariableSymbol("x", PrimitiveType.Int, 1, 1);
        symbolTable.Define(outerSymbol);
        
        // Act
        symbolTable.EnterScope(ScopeType.Block);
        var innerSymbol = new VariableSymbol("x", PrimitiveType.String, 2, 1);
        symbolTable.Define(innerSymbol);
        
        var resolvedInner = symbolTable.Resolve("x");
        symbolTable.ExitScope();
        var resolvedOuter = symbolTable.Resolve("x");
        
        // Assert
        resolvedInner.Should().Be(innerSymbol);
        resolvedOuter.Should().Be(outerSymbol);
    }
    
    [Fact]
    public void IsDefinedLocally_ShouldOnlyCheckCurrentScope()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        var symbol = new VariableSymbol("x", PrimitiveType.Int, 1, 1);
        symbolTable.Define(symbol);
        
        // Act
        symbolTable.EnterScope(ScopeType.Block);
        var isDefinedInner = symbolTable.IsDefinedLocally("x");
        symbolTable.ExitScope();
        var isDefinedOuter = symbolTable.IsDefinedLocally("x");
        
        // Assert
        isDefinedInner.Should().BeFalse();
        isDefinedOuter.Should().BeTrue();
    }
    
    [Fact]
    public void ExitScope_ShouldThrowOnGlobalScope()
    {
        // Arrange
        var symbolTable = new SymbolTable();
        
        // Act
        var action = () => symbolTable.ExitScope();
        
        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot exit global scope");
    }
}