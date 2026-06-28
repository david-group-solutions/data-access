using DavidGroup.Core.DataAccess.Results;

namespace DavidGroup.Core.DataAccessTests.Results;

public class OperationResultMessageTests
{
    [Fact]
    public void Message_IsStored()
    {
        // Arrange
        OperationResultMessage msg = new("something went wrong", OperationResultSeverity.Error);

        // Assert
        Assert.Equal("something went wrong", msg.Message);
    }

    [Fact]
    public void Severity_IsStored()
    {
        // Arrange
        OperationResultMessage msg = new("note", OperationResultSeverity.Information);

        // Assert
        Assert.Equal(OperationResultSeverity.Information, msg.Severity);
    }

    [Theory]
    [InlineData(OperationResultSeverity.Information)]
    [InlineData(OperationResultSeverity.Warning)]
    [InlineData(OperationResultSeverity.Error)]
    public void AllSeverities_AreStoredCorrectly(OperationResultSeverity severity)
    {
        // Arrange
        OperationResultMessage msg = new("text", severity);

        // Assert
        Assert.Equal(severity, msg.Severity);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        OperationResultMessage a = new("oops", OperationResultSeverity.Error);
        OperationResultMessage b = new("oops", OperationResultSeverity.Error);

        // Assert
        Assert.Equal(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentMessage_AreNotEqual()
    {
        // Arrange
        OperationResultMessage a = new("a", OperationResultSeverity.Error);
        OperationResultMessage b = new("b", OperationResultSeverity.Error);

        // Assert
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void RecordEquality_DifferentSeverity_AreNotEqual()
    {
        // Arrange
        OperationResultMessage a = new("x", OperationResultSeverity.Warning);
        OperationResultMessage b = new("x", OperationResultSeverity.Error);

        // Assert
        Assert.NotEqual(a, b);
    }
}
