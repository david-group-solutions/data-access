using DavidGroup.Core.DataAccess.Results;
using DavidGroup.Core.DataAccess.Results.Generic;

namespace DavidGroup.Core.DataAccess.Tests.Results.Generic;

public class GenericFailedOperationResultTests : OperationResultTestsBase
{
    [Fact]
    public void Succeeded_IsFalse()
    {
        // Arrange, Act
        FailedOperationResult<int> result = new(0, Error());

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Value_IsStored()
    {
        // Arrange, Act
        FailedOperationResult<string> result = new("partial", Error());

        // Assert
        Assert.Equal("partial", result.Value);
    }

    [Fact]
    public void WithMessages_MessagesAreStored()
    {
        // Arrange
        OperationResultMessage info = Info();
        OperationResultMessage warn = Warn();
        OperationResultMessage error = Error();

        // Act
        FailedOperationResult<int> result = new(0, info, warn, error);

        // Assert
        Assert.True(result.Messages.SequenceEqual([info, warn, error]));
    }

    [Fact]
    public void IsAssignableToGenericOperationResult()
    {
        // Arrange, Act
        OperationResult<int> result = new FailedOperationResult<int>(1);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void IsAssignableToOperationResult()
    {
        // Arrange, Act
        OperationResult result = new FailedOperationResult<int>(1);

        // Assert
        Assert.False(result.Succeeded);
    }

    // =========================================================================
    // Without value
    // =========================================================================

    public class NoValueTests
    {
        [Fact]
        public void Succeeded_IsFalse()
        {
            // Arrange, Act
            FailedOperationResult<int> result = new(Error());

            // Assert
            Assert.False(result.Succeeded);
        }

        [Fact]
        public void Value_IsDefault()
        {
            // Arrange, Act
            FailedOperationResult<int> result = new(Error());

            // Assert
            Assert.Equal(0, result.Value);
        }

        [Fact]
        public void Value_IsNull_ForReferenceType()
        {
            // Arrange, Act
            FailedOperationResult<string> result = new(Error());

            // Assert
            Assert.Null(result.Value);
        }

        [Fact]
        public void NoMessages_MessagesAreEmpty()
        {
            // Arrange, Act
            FailedOperationResult<int> result = new();

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
            FailedOperationResult<string> result = new(info, warn, error);

            // Assert
            Assert.True(result.Messages.SequenceEqual([info, warn, error]));
        }

        [Fact]
        public void IsAssignableToGenericOperationResult()
        {
            // Arrange, Act
            OperationResult<int> result = new FailedOperationResult<int>();

            // Assert
            Assert.False(result.Succeeded);
        }

        [Fact]
        public void IsAssignableToOperationResult()
        {
            // Arrange, Act
            OperationResult result = new FailedOperationResult<int>();

            // Assert
            Assert.False(result.Succeeded);
        }
    }
}
