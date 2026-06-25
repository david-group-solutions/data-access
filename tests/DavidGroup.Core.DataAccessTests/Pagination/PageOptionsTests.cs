using System.ComponentModel.DataAnnotations;

using DavidGroup.Core.DataAccess.Pagination;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DavidGroup.Core.DataAccessTests.Pagination;

public static class PageOptionsTests
{
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
            PageOptions options = new() { Page = page, Size = size };

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
            PageOptions options = new() { Page = page, Size = size };

            // Act
            IList<ValidationResult> results = Validate(options);

            // Assert
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(PageOptions.Page)));
            Assert.Contains(results, r => r.ErrorMessage!.Equals(ErrorMessages.PageNumberShouldBeGreaterThanZero));
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(1, -1)]
        public void GivenSizeLessOrEqualToZero_ShouldFailValidation(int page, int size)
        {
            // Arrange
            PageOptions options = new() { Page = page, Size = size };

            // Act
            IList<ValidationResult> results = Validate(options);

            // Assert
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(PageOptions.Size)));
            Assert.Contains(results, r => r.ErrorMessage!.Equals(ErrorMessages.PageSizeShouldBeGreaterThanZero));
        }

        [Fact]
        public void GivenSizeExceedsDefault100_AndNoConfiguration_ShouldFailValidation()
        {
            // Arrange
            PageOptions options = new() { Page = 1, Size = 101 };

            // Act
            IList<ValidationResult> results = Validate(options);

            // Assert
            Assert.Contains(results, r => r.ErrorMessage!.Equals($"{ErrorMessages.PageSizeShouldNotExceedMaximum}=100"));
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
            PageOptions options = new() { Page = 1, Size = 50 };

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
            PageOptions options = new() { Page = 1, Size = 51 };

            // Act
            IList<ValidationResult> results = Validate(options, config);

            // Assert
            Assert.Contains(results, r => r.ErrorMessage!.Equals($"{ErrorMessages.PageSizeShouldNotExceedMaximum}=50"));
        }

        [Fact]
        public void GivenNoConfiguration_ShouldFallBackToDefault100()
        {
            // Arrange

            // Empty config — no Pagination:MaxPageSize key
            IConfigurationRoot config = new ConfigurationBuilder().Build();
            PageOptions options = new() { Page = 1, Size = 100 };

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
            PageOptions options = new() { Page = 1, Size = 101 };

            // Act
            IList<ValidationResult> results = Validate(options, config);

            // Assert
            Assert.Contains(results, r => r.ErrorMessage!.Equals($"{ErrorMessages.PageSizeShouldNotExceedMaximum}=100"));
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
            PageOptions options = new() { Page = 1, Size = requestedSize };

            // Act
            IList<ValidationResult> results = Validate(options, config);

            // Assert
            if (expectValid)
                Assert.Empty(results);
            else
            {
                Assert.NotEmpty(results);
                Assert.Contains(results, r => r.ErrorMessage!.Equals($"{ErrorMessages.PageSizeShouldNotExceedMaximum}={configuredMax}"));
            }
        }
    }
}
