using DavidGroup.Core.DataAccess.Results;

namespace DavidGroup.Core.DataAccessTests.Results;

public class SuccessfulOperationResultTests : OperationResultTestsBase
{
    // =========================================================================
    // SuccessfulOperationResult
    // =========================================================================

    [Fact]
    public void Succeeded_IsTrue()
    {
        // Arrange, Act
        SuccessfulOperationResult result = new();

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void NoMessages_MessagesIsEmpty()
    {
        // Arrange, Act
        SuccessfulOperationResult result = new();

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
        SuccessfulOperationResult result = new(info, warn);

        // Assert
        Assert.True(result.Messages.SequenceEqual([info, warn]));
    }

    [Fact]
    public void MessagesPreserveOrder()
    {
        // Arrange
        OperationResultMessage a = Info("first");
        OperationResultMessage b = Warn("second");
        OperationResultMessage c = Info("third");

        // Act
        SuccessfulOperationResult result = new(a, b, c);

        // Assert
        Assert.True(result.Messages.SequenceEqual([a, b, c]));
    }

    [Fact]
    public void IsAssignableToOperationResult()
    {
        // Arrange, Act
        OperationResult result = new SuccessfulOperationResult();

        // Assert
        Assert.True(result.Succeeded);
    }
}
