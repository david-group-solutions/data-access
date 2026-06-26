using System.Text.Json;

using DavidGroup.Core.DataAccess.Results;

namespace DavidGroup.Core.DataAccessTests.Results;

public class OperationResultTests : OperationResultTestsBase
{
    // =========================================================================
    // Static factory: OperationResult.Success
    // =========================================================================

    public class StaticFactorySuccess
    {
        [Fact]
        public void ReturnsSuccessfulOperationResult()
        {
            // Act
            OperationResult result = OperationResult.Success();

            // Assert
            Assert.IsType<SuccessfulOperationResult>(result);
        }

        [Fact]
        public void Succeeded_IsTrue()
        {
            // Act
            OperationResult result = OperationResult.Success();

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public void NoMessages_MessagesIsEmpty()
        {
            // Act
            OperationResult result = OperationResult.Success();

            // Assert
            Assert.Empty(result.Messages);
        }

        [Fact]
        public void WithMessages_MessagesAreAttached()
        {
            // Arrange
            OperationResultMessage warn = Warn();

            // Act
            OperationResult result = OperationResult.Success(warn);

            // Assert
            Assert.True(result.Messages.SequenceEqual([warn]));
        }

        [Fact]
        public void WithErrors_MustThrow_InvalidOperationException()
        {
            // Arrange, Act, Assert
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(()
                => OperationResult.Success(Error()));

            Assert.Equal(ErrorMessages.SuccessfulOperationResultCannotContainAnyErrors, ex.Message);
        }
    }

    // =========================================================================
    // Static factory: OperationResult.Failure
    // =========================================================================

    public class StaticFactoryFailure
    {
        [Fact]
        public void ReturnsFailedOperationResult()
        {
            // Act
            OperationResult result = OperationResult.Failure();

            // Assert
            Assert.IsType<FailedOperationResult>(result);
        }

        [Fact]
        public void Succeeded_IsFalse()
        {
            OperationResult result = OperationResult.Failure();

            // Assert
            Assert.False(result.Succeeded);
        }

        [Fact]
        public void NoMessages_MessagesIsEmpty()
        {
            OperationResult result = OperationResult.Failure();

            // Assert
            Assert.Empty(result.Messages);
        }

        [Fact]
        public void WithMessages_MessagesAreAttached()
        {
            // Arrange
            OperationResultMessage info = Info();
            OperationResultMessage warn = Warn();
            OperationResultMessage error = Error();

            // Act
            OperationResult result = OperationResult.Failure(info, warn, error);

            // Assert
            Assert.True(result.Messages.SequenceEqual([info, warn, error]));
        }
    }

    // =========================================================================
    // HasWarnings
    // =========================================================================

    public class HasWarningsTests
    {
        [Fact]
        public void NoMessages_ReturnsFalse()
        {
            // Arrange, Act, Assert
            Assert.False(OperationResult.Success().HasWarnings());
        }

        [Fact]
        public void OnlyInfoMessages_ReturnsFalse()
        {
            // Arrange
            OperationResult result = OperationResult.Success(Info());

            // Act, Assert
            Assert.False(result.HasWarnings());
        }

        [Fact]
        public void OnlyErrorMessages_ReturnsFalse()
        {
            // Arrange
            OperationResult result = OperationResult.Failure(Error());

            // Act, Assert
            Assert.False(result.HasWarnings());
        }

        [Fact]
        public void SingleWarning_ReturnsTrue()
        {
            // Arrange
            OperationResult result = OperationResult.Success(Warn());

            // Act, Assert
            Assert.True(result.HasWarnings());
        }

        [Fact]
        public void MultipleWarnings_ReturnsTrue()
        {
            // Arrange
            OperationResult result = OperationResult.Success(Warn("w1"), Warn("w2"));

            // Act, Assert
            Assert.True(result.HasWarnings());
        }

        [Fact]
        public void MixedSeverities_WithWarning_ReturnsTrue()
        {
            // Arrange
            OperationResult result = OperationResult.Success(Info(), Warn());

            // Act, Assert
            Assert.True(result.HasWarnings());
        }

        [Fact]
        public void FailedResult_WithWarning_ReturnsTrue()
        {
            // Arrange
            OperationResult result = OperationResult.Failure(Warn());

            // Act, Assert
            Assert.True(result.HasWarnings());
        }
    }

    // =========================================================================
    // HasErrors
    // =========================================================================

    public class HasErrorsTests
    {
        [Fact]
        public void NoMessages_ReturnsFalse()
        {
            // Arrange, Act, Assert
            Assert.False(OperationResult.Success().HasErrors());
        }

        [Fact]
        public void OnlyInfoMessages_ReturnsFalse()
        {
            // Arrange
            OperationResult result = OperationResult.Success(Info());

            // Act, Assert
            Assert.False(result.HasErrors());
        }

        [Fact]
        public void OnlyWarningMessages_ReturnsFalse()
        {
            // Arrange
            OperationResult result = OperationResult.Success(Warn());

            // Act, Assert
            Assert.False(result.HasErrors());
        }

        [Fact]
        public void SingleError_ReturnsTrue()
        {
            // Arrange
            OperationResult result = OperationResult.Failure(Error());

            // Act, Assert
            Assert.True(result.HasErrors());
        }

        [Fact]
        public void MultipleErrors_ReturnsTrue()
        {
            // Arrange
            OperationResult result = OperationResult.Failure(Error("e1"), Error("e2"));

            // Act, Assert
            Assert.True(result.HasErrors());
        }

        [Fact]
        public void MixedSeverities_WithError_ReturnsTrue()
        {
            // Arrange
            OperationResult result = OperationResult.Failure(Info(), Warn(), Error());

            // Act, Assert
            Assert.True(result.HasErrors());
        }
    }

    // =========================================================================
    // Status derivation
    // =========================================================================

    public class StatusTests
    {
        // --- Success path ---

        [Fact]
        public void Succeeded_NoMessages_IsSuccess()
        {
            // Arrange, Act, Assert
            Assert.Equal(OperationStatus.Success, OperationResult.Success().Status);
        }

        [Fact]
        public void Succeeded_WithInfoOnly_IsSuccess()
        {
            // Arrange, Act
            OperationResult result = OperationResult.Success(Info());

            // Assert
            Assert.Equal(OperationStatus.Success, result.Status);
        }

        // --- PartialSuccess path ---

        [Fact]
        public void Succeeded_WithWarningMessage_IsPartialSuccess()
        {
            // Arrange, Act
            OperationResult result = OperationResult.Success(Warn());

            // Assert
            Assert.Equal(OperationStatus.PartialSuccess, result.Status);
        }

        // --- Failure path ---

        [Fact]
        public void Failed_NoMessages_IsFailure()
        {
            // Arrange, Act, Assert
            Assert.Equal(OperationStatus.Failure, OperationResult.Failure().Status);
        }

        [Fact]
        public void Failed_WithErrorOnly_IsFailure()
        {
            // Arrange, Act
            OperationResult result = OperationResult.Failure(Error());

            // Assert
            Assert.Equal(OperationStatus.Failure, result.Status);
        }

        [Fact]
        public void Failed_WithWarningOnly_IsFailure()
        {
            // Arrange, Act
            OperationResult result = OperationResult.Failure(Warn());

            // Assert
            Assert.Equal(OperationStatus.Failure, result.Status);
        }

        [Fact]
        public void Failed_WithInfoOnly_IsFailure()
        {
            // Arrange, Act
            OperationResult result = OperationResult.Failure(Info());

            // Assert
            Assert.Equal(OperationStatus.Failure, result.Status);
        }

        [Fact]
        public void Failed_WithMultipleErrors_IsFailure()
        {
            // Arrange, Act
            OperationResult result = OperationResult.Failure(Error("e1"), Error("e2"), Warn("w1"), Info("i1"));

            // Assert
            Assert.Equal(OperationStatus.Failure, result.Status);
        }
    }

    // =========================================================================
    // HasWarnings and HasErrors are independent
    // =========================================================================

    public class HasWarningsAndHasErrorsAreIndependent
    {
        [Fact]
        public void BothCanBeTrue_Simultaneously()
        {
            // Arrange
            OperationResult result = OperationResult.Failure(Warn(), Error());

            // Act, Assert
            Assert.True(result.HasWarnings());
            Assert.True(result.HasErrors());
        }

        [Fact]
        public void BothCanBeFalse_Simultaneously()
        {
            // Arrange
            OperationResult result = OperationResult.Success(Info());

            // Act, Assert
            Assert.False(result.HasWarnings());
            Assert.False(result.HasErrors());
        }

        [Fact]
        public void HasWarnings_True_HasErrors_False()
        {
            // Arrange
            OperationResult result = OperationResult.Failure(Warn());

            // Act, Assert
            Assert.True(result.HasWarnings());
            Assert.False(result.HasErrors());
        }

        [Fact]
        public void HasWarnings_False_HasErrors_True()
        {
            // Arrange
            OperationResult result = OperationResult.Failure(Error());

            // Act, Assert
            Assert.False(result.HasWarnings());
            Assert.True(result.HasErrors());
        }
    }

    // =========================================================================
    // Record equality on OperationResult subtypes
    // =========================================================================

    public class RecordEqualityTests
    {
        [Fact]
        public void TwoSuccessResults_NoMessages_AreEqual()
        {
            // Arrange
            OperationResult a = OperationResult.Success();
            OperationResult b = OperationResult.Success();

            // Assert
            Assert.Equal(a, b);
        }

        [Fact]
        public void TwoFailureResults_NoMessages_AreEqual()
        {
            // Arrange
            OperationResult a = OperationResult.Failure();
            OperationResult b = OperationResult.Failure();

            // Assert
            Assert.Equal(a, b);
        }

        [Fact]
        public void SuccessAndFailure_AreNotEqual()
        {
            // Arrange
            OperationResult a = OperationResult.Success();
            OperationResult b = OperationResult.Failure();

            // Assert
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void SameType_SameMessages_AreEqual()
        {
            // Arrange
            OperationResult a = OperationResult.Success(Info("note"));
            OperationResult b = OperationResult.Success(Info("note"));

            // Assert
            Assert.Equal(a, b);
        }

        [Fact]
        public void SameType_DifferentMessages_AreNotEqual()
        {
            // Arrange
            OperationResult a = OperationResult.Success(Info("note-a"));
            OperationResult b = OperationResult.Success(Info("note-b"));

            // Assert
            Assert.NotEqual(a, b);
        }
    }

    // -------------------------------------------------------------------------
    // JSON Serialization / Deserialization
    // -------------------------------------------------------------------------

    public class JsonSerializationDeserializationTests
    {
        [Fact]
        public void SuccessfulResult_Serialize_ThenDeserialize_ShouldPreserveAllProperties()
        {
            // Arrange
            OperationResult original = OperationResult.Success(Info(), Warn());

            // Act
            string json = JsonSerializer.Serialize(original);
            OperationResult? deserialized = JsonSerializer.Deserialize<SuccessfulOperationResult>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original, deserialized);
            Assert.Equal(original.Succeeded, deserialized.Succeeded);
            Assert.True(original.Messages.SequenceEqual(deserialized.Messages));
        }

        [Fact]
        public void FailedResult_Serialize_ThenDeserialize_ShouldPreserveAllProperties()
        {
            // Arrange
            OperationResult original = OperationResult.Failure(Info(), Warn(), Error());

            // Act
            string json = JsonSerializer.Serialize(original);
            OperationResult? deserialized = JsonSerializer.Deserialize<FailedOperationResult>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original, deserialized);
            Assert.Equal(original.Succeeded, deserialized.Succeeded);
            Assert.True(original.Messages.SequenceEqual(deserialized.Messages));
        }
    }
}
