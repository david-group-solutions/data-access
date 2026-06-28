using DavidGroup.Core.DataAccess.Results;
using DavidGroup.Core.DataAccess.Results.Generic;

namespace DavidGroup.Core.DataAccessTests.Results.Generic;

public class GenericSuccessfulOperationResultTests : OperationResultTestsBase
{
    [Fact]
    public void Succeeded_IsTrue()
    {
        // Arrange
        SuccessfulOperationResult<int> result = new(42);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Value_IsStored()
    {
        // Arrange
        SuccessfulOperationResult<string> result = new("hello");

        // Assert
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void NoMessages_MessagesIsEmpty()
    {
        // Arrange
        SuccessfulOperationResult<int> result = new(1);

        // Assert
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void WithMessages_MessagesAreStored()
    {
        // Arrange
        OperationResultMessage info = Info();
        OperationResultMessage warn = Warn();

        // Act
        SuccessfulOperationResult<int> result = new(1, info, warn);

        // Assert
        Assert.True(result.Messages.SequenceEqual([info, warn]));
    }

    [Fact]
    public void IsAssignableToGenericBase()
    {
        // Arrange
        OperationResult<int> result = new SuccessfulOperationResult<int>(99);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void IsAssignableToNonGenericBase()
    {
        // Arrange
        OperationResult result = new SuccessfulOperationResult<int>(99);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void WorksWithReferenceType()
    {
        // Arrange
        object obj = new();
        SuccessfulOperationResult<object> result = new(obj);

        // Assert
        Assert.Same(obj, result.Value);
    }

    [Fact]
    public void Throws_ArgumentNullException_When()
    {
        // Arrange, Assert
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new SuccessfulOperationResult<int?>(null));

        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void IsAssignableToGenericOperationResult()
    {
        // Arrange, Act
        OperationResult<int> result = new SuccessfulOperationResult<int>(1);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void IsAssignableToOperationResult()
    {
        // Arrange, Act
        OperationResult result = new SuccessfulOperationResult<int>(1);

        // Assert
        Assert.True(result.Succeeded);
    }
}
