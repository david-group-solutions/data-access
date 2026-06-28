using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DavidGroup.Core.DataAccess.Pagination;
using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;

namespace DavidGroup.Core.DataAccess.Tests.Pagination.InfiniteScroll;

public static class InfinitePageOptionsTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static List<ValidationResult> Validate(InfinitePageOptions options)
    {
        ValidationContext ctx = new(options);
        List<ValidationResult> results = [];

        Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

        return results;
    }

    private static DynamicCursor SomeCursor() => new([1, "abc"]);

    // =========================================================================
    // Constructor(int size, DynamicCursor? searchAfter)
    // =========================================================================

    public class CtorSizeDynamicCursor
    {
        [Fact]
        public void Sets_SizeAndCursor_Correctly()
        {
            // Arrange
            DynamicCursor cursor = SomeCursor();

            // Act
            InfinitePageOptions opts = new(10, cursor);

            // Assert
            Assert.Equal(10, opts.Size);
            Assert.Same(cursor, opts.SearchAfter);
        }

        [Fact]
        public void ValidSize_SearchAfterToken_IsNull()
        {
            // Arrange, Act
            InfinitePageOptions opts = new(10, SomeCursor());

            // Assert
            Assert.Null(opts.SearchAfterToken);
        }

        [Fact]
        public void NullCursor_IsAllowed()
        {
            // Arrange, Act
            InfinitePageOptions opts = new(5, (DynamicCursor?)null);

            // Assert
            Assert.Null(opts.SearchAfter);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void SizeLessThanOrEqualToZero_ThrowsArgumentException(int size)
        {
            // Arrange, Act, Assert
            Assert.Throws<ArgumentException>(() =>
                new InfinitePageOptions(size, SomeCursor()));
        }

        [Fact]
        public void ZeroSize_ExceptionNames_SizeParameter()
        {
            // Arrange, Act, Assert
            ArgumentException ex = Assert.Throws<ArgumentException>(() =>
                new InfinitePageOptions(0, SomeCursor()));

            Assert.Equal("size", ex.ParamName);
        }

        [Fact]
        public void ZeroSize_ExceptionMessage_ContainsExpectedText()
        {
            // Arrange, Act, Assert
            ArgumentException ex = Assert.Throws<ArgumentException>(() =>
                new InfinitePageOptions(0, SomeCursor()));

            Assert.Contains(PaginationErrorMessages.PageSizeShouldBeGreaterThanZero, ex.Message);
        }
    }

    // =========================================================================
    // Constructor(int size, string? searchAfterToken)
    // =========================================================================

    public class CtorSizeToken
    {
        [Fact]
        public void Sets_SizeAndCursor_Correctly()
        {
            // Arrange
            const int size = 10;
            const string token = "some-token";

            // Act
            InfinitePageOptions opts = new(size, token);

            // Assert
            Assert.Equal(size, opts.Size);
            Assert.Same(token, opts.SearchAfterToken);
        }

        [Fact]
        public void ValidSize_SearchAfter_IsNull()
        {
            // Arrange, Act
            InfinitePageOptions opts = new(20, "some-token");

            // Assert
            Assert.Null(opts.SearchAfter);
        }

        [Fact]
        public void NullToken_IsAllowed()
        {
            // Arrange, Act
            InfinitePageOptions opts = new(5, (string?)null);

            // Assert
            Assert.Null(opts.SearchAfterToken);
        }

        [Fact]
        public void EmptyToken_IsAllowed()
        {
            // Arrange, Act
            InfinitePageOptions opts = new(5, "");

            // Assert
            Assert.Equal("", opts.SearchAfterToken);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void SizeLessThanOrEqualToZero_ThrowsArgumentException(int size)
        {
            // Arrange, Act, Assert
            Assert.Throws<ArgumentException>(() =>
                new InfinitePageOptions(size, "token"));
        }

        [Fact]
        public void ZeroSize_ExceptionNames_SizeParameter()
        {
            // Arrange, Act, Assert
            ArgumentException ex = Assert.Throws<ArgumentException>(() =>
                new InfinitePageOptions(0, "token"));

            Assert.Equal("size", ex.ParamName);
        }

        [Fact]
        public void ZeroSize_ExceptionMessage_ContainsExpectedText()
        {
            // Arrange, Act, Assert
            ArgumentException ex = Assert.Throws<ArgumentException>(() =>
                new InfinitePageOptions(0, "token"));

            Assert.Contains(PaginationErrorMessages.PageSizeShouldBeGreaterThanZero, ex.Message);
        }
    }

    // =========================================================================
    // Data annotation validation on Size
    // =========================================================================

    public class DataAnnotationSizeValidation
    {
        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(100)]
        public void SizeInRange_PassesValidation(int size)
        {
            // Arrange
            InfinitePageOptions opts = new() { Size = size };

            // Act
            List<ValidationResult> results = Validate(opts);

            // Assert
            Assert.Empty(results);
            Assert.DoesNotContain(results, r =>
                r.MemberNames.Contains(nameof(InfinitePageOptions.Size)));
            Assert.DoesNotContain(results, r =>
                r.ErrorMessage == PaginationErrorMessages.PageSizeShouldBeGreaterThanZero);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void SizeLessThanOrEqualToZero_FailsValidation(int size)
        {
            // Arrange
            InfinitePageOptions opts = new() { Size = size };

            // Act
            List<ValidationResult> results = Validate(opts);

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r =>
                r.MemberNames.Contains(nameof(InfinitePageOptions.Size)));
            Assert.Contains(results, r =>
                r.ErrorMessage == PaginationErrorMessages.PageSizeShouldBeGreaterThanZero);
        }

        [Theory]
        [InlineData(101)]
        [InlineData(int.MaxValue)]
        public void SizeGraterThanDefaultConfiguredMaximum_FailsValidation(int size)
        {
            // Arrange
            InfinitePageOptions opts = new() { Size = size };

            // Act
            List<ValidationResult> results = Validate(opts);

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Equals($"{PaginationErrorMessages.PageSizeShouldNotExceedMaximum}=100"));
        }
    }

    // =========================================================================
    // JSON constructor (default ctor)
    // =========================================================================

    public class JsonConstructorDefaultCtor
    {
        [Fact]
        public void DefaultCtor_DefaultsAllFields()
        {
            // Arrange, Act
            InfinitePageOptions opts = new();

            // Assert
            Assert.Equal(0, opts.Size);
            Assert.Null(opts.SearchAfter);
            Assert.Null(opts.SearchAfterToken);
        }
    }

    // =========================================================================
    // Record equality
    // =========================================================================

    public class RecordEquality
    {
        [Fact]
        public void SameTokenAndSize_AreEqual()
        {
            // Arrange, Act
            InfinitePageOptions a = new(10, "tok");
            InfinitePageOptions b = new(10, "tok");

            // Assert
            Assert.Equal(a, b);
        }

        [Fact]
        public void DifferentSize_AreNotEqual()
        {
            // Arrange, Act
            InfinitePageOptions a = new(10, "tok");
            InfinitePageOptions b = new(20, "tok");

            // Assert
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void DifferentToken_AreNotEqual()
        {
            // Arrange, Act
            InfinitePageOptions a = new(10, "tok-a");
            InfinitePageOptions b = new(10, "tok-b");

            // Assert
            Assert.NotEqual(a, b);
        }
    }

    // =========================================================================
    // JSON round-trip (exercises the [JsonConstructor] path)
    // =========================================================================

    public class JsonRoundTrip
    {
        [Fact]
        public void StringCursor_SurvivesJsonRoundTrip()
        {
            // Arrange
            InfinitePageOptions original = new(25, "cursor-token");
            string json = JsonSerializer.Serialize(original);

            // Act
            InfinitePageOptions? restored = JsonSerializer.Deserialize<InfinitePageOptions>(json);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal(25, restored.Size);
            Assert.Equal("cursor-token", restored.SearchAfterToken);
            Assert.Null(restored.SearchAfter);
        }

        [Fact]
        public void NullStringCursor_SurvivesJsonRoundTrip()
        {
            // Arrange
            InfinitePageOptions original = new(5, (string?)null);
            string json = JsonSerializer.Serialize(original);

            // Act
            InfinitePageOptions? restored = JsonSerializer.Deserialize<InfinitePageOptions>(json);

            // Assert
            Assert.NotNull(restored);
            Assert.Null(restored.SearchAfter);
            Assert.Null(restored.SearchAfterToken);
        }

        [Fact]
        public void DynamicCursor_SurvivesJsonRoundTrip()
        {
            // Arrange
            DynamicCursor cursor = new([(byte)1, "abc"]);
            InfinitePageOptions original = new(25, cursor);

            JsonSerializerOptions serializerOptions = new();
            serializerOptions.Converters.Add(new DynamicCursorJsonConverter());
            string json = JsonSerializer.Serialize(original, serializerOptions);

            // Act
            InfinitePageOptions? restored = JsonSerializer.Deserialize<InfinitePageOptions>(json, serializerOptions);

            // Assert
            Assert.NotNull(restored);
            Assert.Equal(25, restored.Size);
            Assert.Equal(cursor, restored.SearchAfter);
            Assert.Null(restored.SearchAfterToken);
        }

        [Fact]
        public void DynamicStringCursor_SurvivesJsonRoundTrip()
        {
            // Arrange
            InfinitePageOptions original = new(5, (DynamicCursor?)null);
            string json = JsonSerializer.Serialize(original);

            // Act
            InfinitePageOptions? restored = JsonSerializer.Deserialize<InfinitePageOptions>(json);

            // Assert
            Assert.NotNull(restored);
            Assert.Null(restored.SearchAfter);
            Assert.Null(restored.SearchAfterToken);
        }
    }
}
