using DavidGroup.Core.DataAccess.Results;

namespace DavidGroup.Core.DataAccessTests.Results;

public class FailedOperationResultTests : OperationResultTestsBase
{
    [Fact]
    public void Succeeded_IsFalse()
    {
        // Arrange, Act
        FailedOperationResult result = new();

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void NoMessages_MessagesIsEmpty()
    {
        // Arrange, Act
        FailedOperationResult result = new();

        // Assert
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void WithMessages_MessagesAreStored()
    {
        // Arrange
        OperationResultMessage info = Info();
        OperationResultMessage warn = Warn();
        OperationResultMessage error = Error();

        // Act
        FailedOperationResult result = new(info, warn, error);

        // Assert
        Assert.True(result.Messages.SequenceEqual([info, warn, error]));
    }

    [Fact]
    public void IsAssignableToOperationResult()
    {
        // Arrange, Act
        OperationResult result = new FailedOperationResult();

        // Assert
        Assert.False(result.Succeeded);
    }
}
