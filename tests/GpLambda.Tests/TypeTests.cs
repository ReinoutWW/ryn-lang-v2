using FluentAssertions;
using GpLambda.Compiler.AST.Nodes;
using Xunit;
using Type = GpLambda.Compiler.AST.Nodes.Type;

namespace GpLambda.Tests;

public class TypeTests
{
    [Fact]
    public void PrimitiveTypes_ShouldBeSingletons()
    {
        // Assert
        PrimitiveType.Int.Should().BeSameAs(PrimitiveType.Int);
        PrimitiveType.String.Should().BeSameAs(PrimitiveType.String);
        PrimitiveType.Bool.Should().BeSameAs(PrimitiveType.Bool);
        PrimitiveType.Void.Should().BeSameAs(PrimitiveType.Void);
    }
    
    [Fact]
    public void PrimitiveTypes_Equals_ShouldWorkCorrectly()
    {
        // Arrange
        var int1 = new PrimitiveType(PrimitiveKind.Int);
        var int2 = new PrimitiveType(PrimitiveKind.Int);
        var str = new PrimitiveType(PrimitiveKind.String);
        
        // Assert
        int1.Equals(int2).Should().BeTrue();
        int1.Equals(str).Should().BeFalse();
        PrimitiveType.Int.Equals(int1).Should().BeTrue();
        PrimitiveType.Int.Equals(PrimitiveType.String).Should().BeFalse();
    }
    
    [Theory]
    [InlineData(PrimitiveKind.Int, "Int")]
    [InlineData(PrimitiveKind.String, "String")]
    [InlineData(PrimitiveKind.Bool, "Bool")]
    [InlineData(PrimitiveKind.Void, "Void")]
    public void PrimitiveType_ToString_ShouldReturnCorrectString(PrimitiveKind kind, string expected)
    {
        // Arrange
        var type = new PrimitiveType(kind);
        
        // Act
        var result = type.ToString();
        
        // Assert
        result.Should().Be(expected);
    }
    
    [Fact]
    public void FunctionType_SingleParameter_ShouldFormatCorrectly()
    {
        // Arrange
        var funcType = new FunctionType(
            new List<Type> { PrimitiveType.Int },
            PrimitiveType.String
        );
        
        // Act
        var result = funcType.ToString();
        
        // Assert
        result.Should().Be("Func<Int, String>");
    }
    
    [Fact]
    public void FunctionType_MultipleParameters_ShouldFormatCorrectly()
    {
        // Arrange
        var funcType = new FunctionType(
            new List<Type> { PrimitiveType.Int, PrimitiveType.String, PrimitiveType.Bool },
            PrimitiveType.Void
        );
        
        // Act
        var result = funcType.ToString();
        
        // Assert
        result.Should().Be("Func<Int, String, Bool, Void>");
    }
    
    [Fact]
    public void FunctionType_NestedFunction_ShouldFormatCorrectly()
    {
        // Arrange
        var innerFunc = new FunctionType(
            new List<Type> { PrimitiveType.Int },
            PrimitiveType.String
        );
        var outerFunc = new FunctionType(
            new List<Type> { innerFunc },
            PrimitiveType.Bool
        );
        
        // Act
        var result = outerFunc.ToString();
        
        // Assert
        result.Should().Be("Func<Func<Int, String>, Bool>");
    }
    
    [Fact]
    public void FunctionType_Equals_ShouldCompareStructurally()
    {
        // Arrange
        var func1 = new FunctionType(
            new List<Type> { PrimitiveType.Int, PrimitiveType.String },
            PrimitiveType.Bool
        );
        var func2 = new FunctionType(
            new List<Type> { PrimitiveType.Int, PrimitiveType.String },
            PrimitiveType.Bool
        );
        var func3 = new FunctionType(
            new List<Type> { PrimitiveType.String, PrimitiveType.Int },
            PrimitiveType.Bool
        );
        var func4 = new FunctionType(
            new List<Type> { PrimitiveType.Int, PrimitiveType.String },
            PrimitiveType.Int
        );
        
        // Assert
        func1.Equals(func2).Should().BeTrue();
        func1.Equals(func3).Should().BeFalse(); // Different parameter order
        func1.Equals(func4).Should().BeFalse(); // Different return type
    }
    
    [Fact]
    public void FunctionType_Equals_WithDifferentParameterCount_ShouldReturnFalse()
    {
        // Arrange
        var func1 = new FunctionType(
            new List<Type> { PrimitiveType.Int },
            PrimitiveType.String
        );
        var func2 = new FunctionType(
            new List<Type> { PrimitiveType.Int, PrimitiveType.Int },
            PrimitiveType.String
        );
        
        // Assert
        func1.Equals(func2).Should().BeFalse();
    }
    
    [Fact]
    public void Type_Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        Type primitive = PrimitiveType.Int;
        Type function = new FunctionType(new List<Type>(), PrimitiveType.Void);
        
        // Assert
        primitive.Equals(null!).Should().BeFalse();
        function.Equals(null!).Should().BeFalse();
    }
    
    [Fact]
    public void Type_Equals_WithDifferentTypeKind_ShouldReturnFalse()
    {
        // Arrange
        Type primitive = PrimitiveType.Int;
        Type function = new FunctionType(new List<Type>(), PrimitiveType.Int);
        
        // Assert
        primitive.Equals(function).Should().BeFalse();
        function.Equals(primitive).Should().BeFalse();
    }
    
    [Fact]
    public void ComplexFunctionType_Equals_ShouldWorkCorrectly()
    {
        // Arrange
        // Func<Func<Int, String>, Func<String, Bool>>
        var innerFunc1 = new FunctionType(
            new List<Type> { PrimitiveType.Int },
            PrimitiveType.String
        );
        var innerFunc2 = new FunctionType(
            new List<Type> { PrimitiveType.String },
            PrimitiveType.Bool
        );
        var complexFunc1 = new FunctionType(
            new List<Type> { innerFunc1 },
            innerFunc2
        );
        
        // Create the same type structure again
        var innerFunc1Copy = new FunctionType(
            new List<Type> { PrimitiveType.Int },
            PrimitiveType.String
        );
        var innerFunc2Copy = new FunctionType(
            new List<Type> { PrimitiveType.String },
            PrimitiveType.Bool
        );
        var complexFunc2 = new FunctionType(
            new List<Type> { innerFunc1Copy },
            innerFunc2Copy
        );
        
        // Create a different type structure
        var differentFunc = new FunctionType(
            new List<Type> { innerFunc1 },
            PrimitiveType.Bool // Different return type
        );
        
        // Assert
        complexFunc1.Equals(complexFunc2).Should().BeTrue();
        complexFunc1.Equals(differentFunc).Should().BeFalse();
    }
}