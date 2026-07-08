using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DavidGroup.Core.DataAccess.Pagination;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DavidGroup.Core.DataAccess.Tests.Pagination;

public static class PageOptionsTests
{
    // =========================================================================
    // Constructor(int page, int size)
    // =========================================================================

    public class CtorPageSize
    {
        [Fact]
        public void Sets_PageAndSize_Correctly()
        {
            // Arrange & Act
            PageOptions opts = new(page: 1, size: 10);

            // Assert
            Assert.Equal(1, opts.Page);
            Assert.Equal(10, opts.Size);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void PageLessThanOrEqualToZero_ThrowsArgumentException(int page)
        {
            // Arrange, Act, Assert
            ArgumentException ex = Assert.Throws<ArgumentException>(() =>
                new PageOptions(page, 10));

            Assert.Equal("page", ex.ParamName);
            Assert.Contains(PaginationErrorMessages.PageNumberShouldBeGreaterThanZero, ex.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void SizeLessThanOrEqualToZero_ThrowsArgumentException(int size)
        {
            // Arrange, Act, Assert
            ArgumentException ex = Assert.Throws<ArgumentException>(() =>
                new PageOptions(1, size));

            Assert.Equal("size", ex.ParamName);
            Assert.Contains(PaginationErrorMessages.PageSizeShouldBeGreaterThanZero, ex.Message);
        }
    }

    // -------------------------------------------------------------------------
    // Data annotation validation tests (without IConfiguration — uses default 100)
    // -------------------------------------------------------------------------

    public class DataAnnotationValidation
    {
        private static List<ValidationResult> Validate(PageOptions options)
        {
            ValidationContext ctx = new(options, null, items: null);
            List<ValidationResult> results = [];

            Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

            return results;
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(5, 50)]
        [InlineData(1, 100)]
        public void GivenValidPageAndSize_ShouldPassValidation(int page, int size)
        {
            // Arrange
            PageOptions options = new()
            {
                Page = page,
                Size = size
            };

            // Act
            IList<ValidationResult> results = Validate(options);

            // Assert
            Assert.Empty(results);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(-1, 10)]
        public void GivenInvalidPage_ShouldFailValidation(int page, int size)
        {
            // Arrange
            PageOptions options = new()
            {
                Page = page,
                Size = size
            };

            // Act
            IList<ValidationResult> results = Validate(options);

            // Assert
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(PageOptions.Page)));
            Assert.Contains(results, r => r.ErrorMessage!.Equals(PaginationErrorMessages.PageNumberShouldBeGreaterThanZero));
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(1, -1)]
        public void GivenSizeLessOrEqualToZero_ShouldFailValidation(int page, int size)
        {
            // Arrange
            PageOptions options = new()
            {
                Page = page,
                Size = size
            };

            // Act
            IList<ValidationResult> results = Validate(options);

            // Assert
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(PageOptions.Size)));
            Assert.Contains(results, r => r.ErrorMessage!.Equals(PaginationErrorMessages.PageSizeShouldBeGreaterThanZero));
        }

        [Fact]
        public void GivenSizeExceedsDefault100_AndNoConfiguration_ShouldFailValidation()
        {
            // Arrange
            PageOptions options = new()
            {
                Page = 1,
                Size = 101
            };

            // Act
            IList<ValidationResult> results = Validate(options);

            // Assert
            Assert.Contains(results, r => r.ErrorMessage!.Equals($"{PaginationErrorMessages.PageSizeShouldNotExceedMaximum}=100"));
        }
    }

    // -------------------------------------------------------------------------
    // MaxPageSizeAttribute tests — with IConfiguration injected
    // -------------------------------------------------------------------------

    public class MaxPageSizeAttributeTests
    {
        private static IConfiguration BuildConfig(int maxPageSize) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Pagination:MaxPageSize"] = maxPageSize.ToString() })
                .Build();

        private static IList<ValidationResult> Validate(PageOptions options, IConfiguration config)
        {
            ServiceProvider services = new ServiceCollection()
                .AddSingleton(config)
                .BuildServiceProvider();

            ValidationContext ctx = new(options, services, items: null);
            List<ValidationResult> results = [];

            Validator.TryValidateObject(options, ctx, results, validateAllProperties: true);

            return results;
        }

        [Fact]
        public void GivenSizeWithinConfiguredLimit_ShouldPassValidation()
        {
            // Arrange
            IConfiguration config = BuildConfig(maxPageSize: 50);
            PageOptions options = new()
            {
                Page = 1,
                Size = 50
            };

            // Act
            IList<ValidationResult> results = Validate(options, config);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void GivenSizeExceedsConfiguredLimit_ShouldFailValidation()
        {
            // Arrange
            IConfiguration config = BuildConfig(maxPageSize: 50);
            PageOptions options = new()
            {
                Page = 1,
                Size = 51
            };

            // Act
            IList<ValidationResult> results = Validate(options, config);

            // Assert
            Assert.Contains(results, r => r.ErrorMessage!.Equals($"{PaginationErrorMessages.PageSizeShouldNotExceedMaximum}=50"));
        }

        [Fact]
        public void GivenNoConfiguration_ShouldFallBackToDefault100()
        {
            // Arrange

            // Empty config — no Pagination:MaxPageSize key
            IConfigurationRoot config = new ConfigurationBuilder().Build();
            PageOptions options = new()
            {
                Page = 1,
                Size = 100
            };

            // Act
            IList<ValidationResult> results = Validate(options, config);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void GivenNoConfiguration_AndSizeExceeds100_ShouldFailValidation()
        {
            // Arrange
            IConfigurationRoot config = new ConfigurationBuilder().Build();
            PageOptions options = new()
            {
                Page = 1,
                Size = 101
            };

            // Act
            IList<ValidationResult> results = Validate(options, config);

            // Assert
            Assert.Contains(results, r => r.ErrorMessage!.Equals($"{PaginationErrorMessages.PageSizeShouldNotExceedMaximum}=100"));
        }

        [Theory]
        [InlineData(10, 10, true)]
        [InlineData(10, 11, false)]
        [InlineData(200, 200, true)]
        [InlineData(200, 201, false)]
        public void GivenVariousLimitsAndSizes_ShouldValidateCorrectly(
            int configuredMax, int requestedSize, bool expectValid)
        {
            // Arrange
            IConfiguration config = BuildConfig(configuredMax);
            PageOptions options = new()
            {
                Page = 1,
                Size = requestedSize
            };

            // Act
            IList<ValidationResult> results = Validate(options, config);

            // Assert
            if (expectValid)
                Assert.Empty(results);
            else
            {
                Assert.NotEmpty(results);
                Assert.Contains(results, r => r.ErrorMessage!.Equals($"{PaginationErrorMessages.PageSizeShouldNotExceedMaximum}={configuredMax}"));
            }
        }
    }

    // -------------------------------------------------------------------------
    // JSON Serialization / Deserialization
    // -------------------------------------------------------------------------

    public class JsonSerializationDeserializationTests
    {
        [Fact]
        public void Serialize_ThenDeserialize_ShouldPreserveAllProperties()
        {
            // Arrange
            PageOptions original = new()
            {
                Page = 1,
                Size = 100
            };

            // Act
            string json = JsonSerializer.Serialize(original);
            PageOptions? deserialized = JsonSerializer.Deserialize<PageOptions>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Page, deserialized.Page);
            Assert.Equal(original.Size, deserialized.Size);
        }

        [Fact]
        public void Deserialize_WithMissingFields_ShouldUseDefaults()
        {
            // Arrange
            const string json = "{}";

            // Act
            PageOptions? options = JsonSerializer.Deserialize<PageOptions>(json);

            // Assert
            Assert.NotNull(options);
            Assert.Equal(0, options.Page);
            Assert.Equal(0, options.Size);
        }
    }
}
