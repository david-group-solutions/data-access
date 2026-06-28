using System.Text.Json;

using DavidGroup.Core.DataAccess.Pagination;

namespace DavidGroup.Core.DataAccess.Tests.Pagination;

public static class PageDataTests
{
    public class ConstructorsTests
    {
        // -------------------------------------------------------------------------
        // Parameterless constructor tests
        // -------------------------------------------------------------------------

        [Fact]
        public void DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange, Act
            PageData<string> pageData = new();

            // Assert
            Assert.Empty(pageData.Entities);
            Assert.Equal(0, pageData.TotalCount);
            Assert.Equal(0, pageData.TotalPages);
            Assert.False(pageData.HasPreviousPage);
            Assert.False(pageData.HasNextPage);
        }

        // -------------------------------------------------------------------------
        // Constructor(entities, totalCount, page, size)
        // -------------------------------------------------------------------------

        [Fact]
        public void Constructor1_WithEntitiesAndPagination_ShouldSetEntities()
        {
            // Arrange
            List<string> entities = ["a", "b", "c"];

            // Act
            PageData<string> pageData = new(entities, 10, 1, 3);

            // Assert
            Assert.Equal(entities, pageData.Entities);
        }

        [Fact]
        public void Constructor1_ShouldSetTotalCount()
        {
            // Arrange, Act
            PageData<int> pageData = new([], 42, 1, 10);

            // Assert
            Assert.Equal(42, pageData.TotalCount);
        }

        [Theory]
        [InlineData(10, 10, 1)] // Exactly one page
        [InlineData(11, 10, 2)] // One item spills onto next page
        [InlineData(0, 10, 0)] // No items
        [InlineData(1, 1, 1)] // Single item, single page
        [InlineData(100, 10, 10)] // Even split
        [InlineData(101, 10, 11)] // One extra page
        public void Constructor1_ShouldCalculateTotalPages(int totalCount, int size, int expectedTotalPages)
        {
            // Arrange, Act
            PageData<int> pageData = new([], totalCount, 1, size);

            // Assert
            Assert.Equal(expectedTotalPages, pageData.TotalPages);
        }

        [Theory]
        [InlineData(1, false)] // First page has no previous
        [InlineData(2, true)] // Any page > 1 has a previous
        [InlineData(5, true)]
        public void Constructor1_ShouldSetHasPreviousPage(int page, bool expected)
        {
            // Arrange, Act
            PageData<int> pageData = new([], 100, page, 10);

            // Assert
            Assert.Equal(expected, pageData.HasPreviousPage);
        }

        [Theory]
        [InlineData(1, 10, 100, true)] // Page 1 of 10 has next
        [InlineData(9, 10, 100, true)] // Page 9 of 10 has next
        [InlineData(10, 10, 100, false)] // Last page has no next
        [InlineData(1, 10, 5, false)] // Only one page total, no next
        [InlineData(1, 10, 0, false)] // Zero items, no pages, no next
        public void Constructor1_ShouldSetHasNextPage(int page, int size, int totalCount, bool expected)
        {
            // Arrange, Act
            PageData<int> pageData = new([], totalCount, page, size);

            // Assert
            Assert.Equal(expected, pageData.HasNextPage);
        }

        [Fact]
        public void Constructor1_FirstPageOfMany_ShouldHaveNextButNoPrevious()
        {
            // Arrange, Act
            PageData<int> pageData = new([], 50, 1, 10);

            // Assert
            Assert.False(pageData.HasPreviousPage);
            Assert.True(pageData.HasNextPage);
        }

        [Fact]
        public void Constructor1_MiddlePage_ShouldHaveBothPreviousAndNext()
        {
            // Arrange, Act
            PageData<int> pageData = new([], 50, 3, 10);

            // Assert
            Assert.True(pageData.HasPreviousPage);
            Assert.True(pageData.HasNextPage);
        }

        [Fact]
        public void Constructor1_LastPage_ShouldHavePreviousButNoNext()
        {
            // Arrange, Act
            PageData<int> pageData = new([], 50, 5, 10);

            // Assert
            Assert.True(pageData.HasPreviousPage);
            Assert.False(pageData.HasNextPage);
        }

        [Fact]
        public void Constructor1_SinglePage_ShouldHaveNeitherPreviousNorNext()
        {
            PageData<int> pageData = new([], 5, 1, 10);

            Assert.False(pageData.HasPreviousPage);
            Assert.False(pageData.HasNextPage);
        }

        [Fact]
        public void Constructor1_WithEmptyEntities_ShouldReturnEmptyCollection()
        {
            // Arrange
            IEnumerable<string> entities = [];

            // Act
            PageData<string> pageData = new(entities, 0, 1, 10);

            // Assert
            Assert.Empty(pageData.Entities);
        }

        [Fact]
        public void Constructor1_WithLargeDataset_ShouldCalculateCorrectly()
        {
            PageData<int> pageData = new([], 1_000_000, 500, 100);

            Assert.Equal(10000, pageData.TotalPages);
            Assert.True(pageData.HasPreviousPage);
            Assert.True(pageData.HasNextPage);
        }

        // -------------------------------------------------------------------------
        // Constructor(entities, totalCount, PageOptions)
        // -------------------------------------------------------------------------

        [Fact]
        public void Constructor2_WithPageOptions_ShouldProduceSameResultAsExplicitPageAndSize()
        {
            // Arrange
            List<string> entities = ["x", "y"];
            PageOptions options = new() { Page = 2, Size = 5 };

            // Act
            PageData<string> fromOptions = new(entities, 20, options);
            PageData<string> fromExplicit = new(entities, 20, 2, 5);

            // Assert
            Assert.Equal(fromExplicit, fromOptions);
            Assert.Equal(fromExplicit.TotalCount, fromOptions.TotalCount);
            Assert.Equal(fromExplicit.TotalPages, fromOptions.TotalPages);
            Assert.Equal(fromExplicit.HasPreviousPage, fromOptions.HasPreviousPage);
            Assert.Equal(fromExplicit.HasNextPage, fromOptions.HasNextPage);
            Assert.True(fromExplicit.Entities.SequenceEqual(fromOptions.Entities));
        }
    }

    // -------------------------------------------------------------------------
    // Checks equality for records
    // -------------------------------------------------------------------------

    public class RecordEqualityTests
    {
        [Fact]
        public void TwoPageDataInstances_WithSameValues_ShouldBeEqual()
        {
            // Arrange
            List<int> entities = [1, 2, 3];

            PageData<int> a = new(entities, 30, 1, 10);
            PageData<int> b = new(entities, 30, 1, 10);

            // Assert
            Assert.Equal(a, b);
        }

        [Fact]
        public void TwoPageDataInstances_WithDifferentPages_ShouldNotBeEqual()
        {
            // Arrange
            List<int> entities = [1, 2, 3];

            PageData<int> a = new(entities, 30, 1, 10);
            PageData<int> b = new(entities, 30, 2, 10);

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
        public void Serialize_ThenDeserialize_ShouldPreserveAllProperties()
        {
            // Arrange
            List<int> entities = [1, 2, 3];
            PageData<int> original = new(entities, 30, 2, 10);

            // Act
            string json = JsonSerializer.Serialize(original);
            PageData<int>? deserialized = JsonSerializer.Deserialize<PageData<int>>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.TotalCount, deserialized.TotalCount);
            Assert.Equal(original.TotalPages, deserialized.TotalPages);
            Assert.Equal(original.HasPreviousPage, deserialized.HasPreviousPage);
            Assert.Equal(original.HasNextPage, deserialized.HasNextPage);
            Assert.True(original.Entities.SequenceEqual(deserialized.Entities));
        }

        [Fact]
        public void Deserialize_WithMissingFields_ShouldUseDefaults()
        {
            // Arrange
            const string json = "{}";

            // Act
            PageData<string>? pageData = JsonSerializer.Deserialize<PageData<string>>(json);

            // Assert
            Assert.NotNull(pageData);
            Assert.Empty(pageData.Entities);
            Assert.Equal(0, pageData.TotalCount);
            Assert.Equal(0, pageData.TotalPages);
            Assert.False(pageData.HasPreviousPage);
            Assert.False(pageData.HasNextPage);
        }
    }

    // -------------------------------------------------------------------------
    // Generic type support tests
    // -------------------------------------------------------------------------

    public class GenericTypeSupportTests
    {
        [Fact]
        public void PageData_WithCustomType_ShouldWork()
        {
            // Arrange
            List<SampleEntity> entities = [new() { Id = 1, Name = "Alice" }, new() { Id = 2, Name = "Bob" }];

            // Act
            PageData<SampleEntity> pageData = new(entities, 2, 1, 10);

            // Assert
            Assert.Equal(entities, pageData.Entities);
            Assert.Equal(2, pageData.TotalCount);
            Assert.Equal(1, pageData.TotalPages);
            Assert.False(pageData.HasPreviousPage);
            Assert.False(pageData.HasNextPage);
        }

        private record SampleEntity
        {
            public int Id { get; init; }
            public string? Name { get; init; }
        }
    }
}
