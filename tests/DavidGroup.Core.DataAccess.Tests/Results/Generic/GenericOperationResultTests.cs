using System.Text.Json;

using DavidGroup.Core.DataAccess.Results;
using DavidGroup.Core.DataAccess.Results.Generic;

namespace DavidGroup.Core.DataAccess.Tests.Results.Generic;

public class GenericOperationResultTests : OperationResultTestsBase
{
    // =========================================================================
    // Static factory: OperationResult<T>.Success
    // =========================================================================

    public class StaticFactorySuccess
    {
        [Fact]
        public void ReturnsSuccessfulOperationResult()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Success(1);

            // Assert
            Assert.IsType<SuccessfulOperationResult<int>>(result);
        }

        [Fact]
        public void Succeeded_IsTrue()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Success(1);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Value_IsStored()
        {
            // Arrange, Act
            OperationResult<string> result = OperationResult<string>.Success("ok");

            // Assert
            Assert.Equal("ok", result.Value);
        }

        [Fact]
        public void NoMessages_MessagesIsEmpty()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Success(1);

            // Assert
            Assert.Empty(result.Messages);
        }

        [Fact]
        public void WithMessages_MessagesAreAttached()
        {
            // Arrange
            OperationResultMessage info = Info();
            OperationResultMessage warn = Warn();

            // Act
            OperationResult<int> result = OperationResult<int>.Success(1, info, warn);

            // Assert
            Assert.True(result.Messages.SequenceEqual([info, warn]));
        }

        [Fact]
        public void WithErrors_MustThrow_InvalidOperationException()
        {
            // Arrange, Act, Assert
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(()
                => OperationResult<int>.Success(1, Error()));

            Assert.Equal(OperationResultErrorMessages.SuccessfulOperationResultCannotContainAnyErrors, ex.Message);
        }
    }

    // =========================================================================
    // Static factory: OperationResult<T>.Failure (with value)
    // =========================================================================

    public class StaticFactoryFailureWithValue
    {
        [Fact]
        public void ReturnsFailedOperationResult()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Failure(0, Error());

            // Assert
            Assert.IsType<FailedOperationResult<int>>(result);
        }

        [Fact]
        public void Succeeded_IsFalse()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Failure(0, Error());

            // Assert
            Assert.False(result.Succeeded);
        }

        [Fact]
        public void Value_IsStored()
        {
            // Arrange, Act
            OperationResult<string> result = OperationResult<string>.Failure("partial", Error());

            // Assert
            Assert.Equal("partial", result.Value);
        }

        [Fact]
        public void WithMessages_MessagesAreAttached()
        {
            // Arrange
            OperationResultMessage info = Info();
            OperationResultMessage warn = Warn();
            OperationResultMessage error = Error();

            // Act
            OperationResult<int> result = OperationResult<int>.Failure(0, info, warn, error);

            // Assert
            Assert.True(result.Messages.SequenceEqual([info, warn, error]));
        }
    }

    // =========================================================================
    // Static factory: OperationResult<T>.Failure (no value)
    // =========================================================================

    public class StaticFactoryFailureNoValue
    {
        [Fact]
        public void ReturnsFailedOperationResult()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Failure();

            // Assert
            Assert.IsType<FailedOperationResult<int>>(result);
        }

        [Fact]
        public void Succeeded_IsFalse()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Failure();

            // Assert
            Assert.False(result.Succeeded);
        }

        [Fact]
        public void Value_IsDefault_ForValueType()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Failure(Error());

            // Assert
            Assert.Equal(0, result.Value);
        }

        [Fact]
        public void Value_IsNull_ForReferenceType()
        {
            // Arrange, Act
            OperationResult<string> result = OperationResult<string>.Failure(Error());

            // Assert
            Assert.Null(result.Value);
        }

        [Fact]
        public void NoMessages_MessagesIsEmpty()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Failure();

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
            OperationResult<int> result = OperationResult<int>.Failure(info, warn, error);

            // Assert
            Assert.True(result.Messages.SequenceEqual([info, warn, error]));
        }
    }

    // =========================================================================
    // Implicit operator: OperationResult<T> → T
    // =========================================================================

    public class ImplicitOperatorResultToValue
    {
        [Fact]
        public void Success_WithValue_ExtractsValue()
        {
            // Arrange
            OperationResult<int> result = OperationResult<int>.Success(42);

            // Act
            int value = result;

            // Assert
            Assert.Equal(42, value);
        }

        [Fact]
        public void Success_WithStringValue_ExtractsValue()
        {
            // Arrange
            OperationResult<string> result = OperationResult<string>.Success("hello");

            // Act
            string value = result;

            // Assert
            Assert.Equal("hello", value);
        }

        [Fact]
        public void Failure_WithValue_ExtractsValue()
        {
            // Arrange
            OperationResult<int> result = OperationResult<int>.Failure(7, Error());

            // Act
            int value = result;

            // Assert
            Assert.Equal(7, value);
        }

        [Fact]
        public void Failure_NoValue_ThrowsInvalidOperationException()
        {
            // Arrange
            OperationResult<int?> result = OperationResult<int?>.Failure(Error());

            // Act, Assert
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            {
                int? _ = result;
            });

            Assert.Equal(OperationResultErrorMessages.NoValue, ex.Message);
        }

        [Fact]
        public void Failure_NullReferenceValue_ThrowsInvalidOperationException()
        {
            // Arrange
            OperationResult<string> result = OperationResult<string>.Failure(Error());

            // Act, Assert
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            {
                string _ = result;
            });

            Assert.Equal(OperationResultErrorMessages.NoValue, ex.Message);
        }
    }

    // =========================================================================
    // Implicit operator: T → OperationResult<T>
    // =========================================================================

    public class ImplicitOperatorValueToResult
    {
        [Fact]
        public void NonNullValue_ProducesSuccess()
        {
            // Arrange, Act
            OperationResult<int> result = 42;

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public void NonNullValue_ValueIsStored()
        {
            // Arrange, Act
            OperationResult<int> result = 42;

            // Assert
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void NonNullReferenceValue_NoMessages()
        {
            // Arrange, Act
            OperationResult<string> result = "ok";

            // Assert
            Assert.Empty(result.Messages);
        }

        [Fact]
        public void NonNullValue_IsSuccessfulOperationResult()
        {
            // Arrange, Act
            OperationResult<string> result = "hello";

            // Assert
            Assert.IsType<SuccessfulOperationResult<string>>(result);
        }

        [Fact]
        public void NullReferenceValue_ProducesFailure()
        {
            // Arrange, Act
            OperationResult<string> result = (string?)null;

            // Assert
            Assert.False(result.Succeeded);
        }

        [Fact]
        public void NullReferenceValue_IsFailedOperationResult()
        {
            // Arrange, Act
            OperationResult<string> result = (string?)null;

            // Assert
            Assert.IsType<FailedOperationResult<string>>(result);
        }

        [Fact]
        public void NullReferenceValue_AttachesErrorMessage_And_MatchesNoValueConstant()
        {
            // Arrange, Act
            OperationResult<string> result = (string?)null;

            // Assert
            Assert.Single(result.Messages);
            Assert.Equal(OperationResultErrorMessages.NoValue, result.Messages[0].Message);
            Assert.Equal(OperationResultSeverity.Error, result.Messages[0].Severity);
        }
    }

    // =========================================================================
    // Status derivation (inherits base logic, verify through generic surface)
    // =========================================================================

    public class StatusTests
    {
        // --- Success path ---

        [Fact]
        public void Success_NoMessages_IsSuccess()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Success(1);

            // Assert
            Assert.Equal(OperationStatus.Success, result.Status);
        }

        [Fact]
        public void Succeeded_WithInfoOnly_IsSuccess()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Success(1, Info());

            // Assert
            Assert.Equal(OperationStatus.Success, result.Status);
        }

        // --- PartialSuccess path ---

        [Fact]
        public void Succeeded_WithWarningMessage_IsPartialSuccess()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Success(1, Warn());

            // Assert
            Assert.Equal(OperationStatus.PartialSuccess, result.Status);
        }

        // --- Failure path ---

        [Fact]
        public void Failure_NoMessages_IsFailure()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Failure();

            // Assert
            Assert.Equal(OperationStatus.Failure, result.Status);
        }

        [Fact]
        public void Failure_WithErrorOnly_IsFailure()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Failure(Error());

            // Assert
            Assert.Equal(OperationStatus.Failure, result.Status);
        }

        [Fact]
        public void Failed_WithWarningOnly_IsFailure()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Failure(Warn());

            // Assert
            Assert.Equal(OperationStatus.Failure, result.Status);
        }

        [Fact]
        public void Failed_WithInfoOnly_IsFailure()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Failure(Info());

            // Assert
            Assert.Equal(OperationStatus.Failure, result.Status);
        }

        [Fact]
        public void Failed_WithMultipleErrors_IsFailure()
        {
            // Arrange, Act
            OperationResult<int> result = OperationResult<int>.Failure(Error("e1"), Error("e2"), Warn("w1"), Info("i1"));

            // Assert
            Assert.Equal(OperationStatus.Failure, result.Status);
        }
    }

    // =========================================================================
    // MemberNotNullWhen — runtime behavior of Succeeded
    // =========================================================================

    public class MemberNotNullWhenRuntimeBehaviour
    {
        // MemberNotNullWhen is a static-analysis hint only; these tests verify
        // the underlying runtime values are consistent with what the attribute promises.

        [Fact]
        public void Succeeded_True_ImpliesValue_IsNotNull()
        {
            // Arrange
            OperationResult<string> result = OperationResult<string>.Success("data");

            // Assert
            if (result.Succeeded)
                Assert.NotNull(result.Value);
        }
    }

    // =========================================================================
    // Record equality on generic operation result subtypes
    // =========================================================================

    public class RecordEqualityTests
    {
        [Fact]
        public void TwoSuccessResults_SameValue_AreEqual()
        {
            // Arrange
            OperationResult<int> a = OperationResult<int>.Success(1);
            OperationResult<int> b = OperationResult<int>.Success(1);

            // Assert
            Assert.Equal(a, b);
        }

        [Fact]
        public void TwoSuccessResults_DifferentValues_AreNotEqual()
        {
            // Arrange
            OperationResult<int> a = OperationResult<int>.Success(1);
            OperationResult<int> b = OperationResult<int>.Success(2);

            // Assert
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void TwoSuccessResults_SameValues_SameMessages_AreEqual()
        {
            // Arrange
            OperationResult<int> a = OperationResult<int>.Success(1, Info());
            OperationResult<int> b = OperationResult<int>.Success(1, Info());

            // Assert
            Assert.Equal(a, b);
        }

        [Fact]
        public void TwoSuccessResults_SameValues_DifferentMessages_AreNotEqual()
        {
            // Arrange
            OperationResult<int> a = OperationResult<int>.Success(1);
            OperationResult<int> b = OperationResult<int>.Success(1, Info());

            // Assert
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void TwoSuccessResults_DifferentValues_SameMessages_AreNotEqual()
        {
            // Arrange
            OperationResult<int> a = OperationResult<int>.Success(1, Info());
            OperationResult<int> b = OperationResult<int>.Success(2, Info());

            // Assert
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void SuccessAndFailure_SameValue_AreNotEqual()
        {
            // Arrange
            OperationResult<int> a = OperationResult<int>.Success(1);
            OperationResult<int> b = OperationResult<int>.Failure(1);

            // Assert
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void TwoFailures_NoValue_NoMessages_AreEqual()
        {
            // Arrange
            OperationResult<int> a = OperationResult<int>.Failure();
            OperationResult<int> b = OperationResult<int>.Failure();

            // Assert
            Assert.Equal(a, b);
        }
    }

    // -------------------------------------------------------------------------
    // JSON Serialization / Deserialization
    // -------------------------------------------------------------------------

    public class JsonSerializationDeserializationTests
    {
        [Fact]
        public void SuccessfulResult_WithValueType_Serialize_ThenDeserialize_ShouldPreserveAllProperties()
        {
            // Arrange
            OperationResult<int> original = OperationResult<int>.Success(1, Info(), Warn());

            // Act
            string json = JsonSerializer.Serialize(original);
            OperationResult<int>? deserialized = JsonSerializer.Deserialize<SuccessfulOperationResult<int>>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original, deserialized);
            Assert.Equal(original.Succeeded, deserialized.Succeeded);
            Assert.Equal(original.Value, deserialized.Value);
            Assert.True(original.Messages.SequenceEqual(deserialized.Messages));
        }

        [Fact]
        public void SuccessfulResult_WithReferenceType_Serialize_ThenDeserialize_ShouldPreserveAllProperties()
        {
            // Arrange
            TestValue value = new(1);
            OperationResult<TestValue> original = OperationResult<TestValue>.Success(value, Info(), Warn());

            // Act
            string json = JsonSerializer.Serialize(original);
            OperationResult<TestValue>? deserialized = JsonSerializer.Deserialize<SuccessfulOperationResult<TestValue>>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original, deserialized);
            Assert.Equal(original.Succeeded, deserialized.Succeeded);
            Assert.Equal(original.Value, deserialized.Value);
            Assert.True(original.Messages.SequenceEqual(deserialized.Messages));
        }

        [Fact]
        public void FailedResult_WithNoValue_Serialize_ThenDeserialize_ShouldPreserveAllProperties()
        {
            // Arrange
            OperationResult<int> original = OperationResult<int>.Failure(Info(), Warn(), Error());

            // Act
            string json = JsonSerializer.Serialize(original);
            OperationResult<int>? deserialized = JsonSerializer.Deserialize<FailedOperationResult<int>>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original, deserialized);
            Assert.Equal(original.Succeeded, deserialized.Succeeded);
            Assert.Equal(original.Value, deserialized.Value);
            Assert.True(original.Messages.SequenceEqual(deserialized.Messages));
        }

        [Fact]
        public void FailedResult_WithValueType_Serialize_ThenDeserialize_ShouldPreserveAllProperties()
        {
            // Arrange
            OperationResult<int> original = OperationResult<int>.Failure(1, Info(), Warn(), Error());

            // Act
            string json = JsonSerializer.Serialize(original);
            OperationResult<int>? deserialized = JsonSerializer.Deserialize<FailedOperationResult<int>>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original, deserialized);
            Assert.Equal(original.Succeeded, deserialized.Succeeded);
            Assert.Equal(original.Value, deserialized.Value);
            Assert.True(original.Messages.SequenceEqual(deserialized.Messages));
        }

        [Fact]
        public void FailedResult_WithReferenceType_Serialize_ThenDeserialize_ShouldPreserveAllProperties()
        {
            // Arrange
            TestValue value = new(1);
            OperationResult<TestValue> original = OperationResult<TestValue>.Failure(value, Info(), Warn(), Error());

            // Act
            string json = JsonSerializer.Serialize(original);
            OperationResult<TestValue>? deserialized = JsonSerializer.Deserialize<FailedOperationResult<TestValue>>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original, deserialized);
            Assert.Equal(original.Succeeded, deserialized.Succeeded);
            Assert.Equal(original.Value, deserialized.Value);
            Assert.True(original.Messages.SequenceEqual(deserialized.Messages));
        }

        public record TestValue(int Id);
    }
}
