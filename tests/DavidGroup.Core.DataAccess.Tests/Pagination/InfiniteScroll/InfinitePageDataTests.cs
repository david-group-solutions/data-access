using System.Text.Json;

using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;

namespace DavidGroup.Core.DataAccess.Tests.Pagination.InfiniteScroll;

public static class InfinitePageDataTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static DynamicCursor SomeCursor() => new([42, "abc"]);

    private static List<int> SomeEntities() => [1, 2, 3];

    // =========================================================================
    // Default / JSON constructor
    // =========================================================================

    public class DefaultCtor
    {
        [Fact]
        public void DefaultsAllFields()
        {
            // Arrange, Act
            InfinitePageData<int> page = new();

            // Assert
            Assert.Empty(page.Entities);
            Assert.Null(page.NextCursor);
            Assert.False(page.HasNextPage);
        }
    }

    // =========================================================================
    // Main constructor — entities
    // =========================================================================

    public class CtorEntities
    {
        [Fact]
        public void NonNullEntities_AreStored()
        {
            // Arrange
            List<int> entities = SomeEntities();

            // Act
            InfinitePageData<int> page = new(entities, null);

            // Assert
            Assert.Equal(entities, page.Entities);
        }

        [Fact]
        public void NullEntities_FallBackToEmptyEnumerable()
        {
            // Arrange, Act
            InfinitePageData<int> page = new(null, null);

            // Assert
            Assert.Empty(page.Entities);
        }

        [Fact]
        public void EmptyEntities_AreStoredAsEmpty()
        {
            // Arrange, Act
            InfinitePageData<int> page = new([], null);

            // Assert
            Assert.Empty(page.Entities);
        }

        [Fact]
        public void EntitiesPreserveOrder()
        {
            // Arrange
            List<int> entities = [3, 1, 2];

            // Act
            InfinitePageData<int> page = new(entities, null);

            // Assert
            Assert.True(page.Entities.SequenceEqual([3, 1, 2]));
        }

        [Fact]
        public void WorksWithStringType()
        {
            // Arrange
            List<string> entities = ["a", "b"];

            // Act
            InfinitePageData<string> page = new(entities, null);

            // Assert
            Assert.True(page.Entities.SequenceEqual(["a", "b"]));
        }

        [Fact]
        public void WorksWithReferenceType()
        {
            // Arrange
            List<object> items = [new(), new()];

            // Act
            InfinitePageData<object> page = new(items, null);

            // Assert
            Assert.Equal(2, page.Entities.Count);
        }
    }

    // =========================================================================
    // Main constructor — NextCursor
    // =========================================================================

    public class CtorNextCursor
    {
        [Fact]
        public void NonNullCursor_IsStoredCorrectly()
        {
            // Arrange, Act
            DynamicCursor cursor = SomeCursor();
            InfinitePageData<int> page = new(null, cursor);

            // Assert
            Assert.Same(cursor, page.NextCursor);
        }

        [Fact]
        public void NullCursor_IsStoredAsNull()
        {
            // Arrange, Act
            InfinitePageData<int> page = new(null, null);

            // Assert
            Assert.Null(page.NextCursor);
        }
    }

    // =========================================================================
    // Combined / realistic scenarios
    // =========================================================================

    public class RealisticScenarios
    {
        [Fact]
        public void FullPage_WithCursor()
        {
            // Arrange
            List<int> entities = SomeEntities();
            DynamicCursor cursor = SomeCursor();

            // Act
            InfinitePageData<int> page = new(entities, cursor);

            // Assert
            Assert.Equal(entities, page.Entities);
            Assert.Same(cursor, page.NextCursor);
            Assert.True(page.HasNextPage);
        }

        [Fact]
        public void LastPage_NoCursor()
        {
            // Arrange
            List<int> entities = SomeEntities();

            // Act
            InfinitePageData<int> page = new(entities, nextCursor: null);

            // Assert
            Assert.Equal(entities, page.Entities);
            Assert.Null(page.NextCursor);
            Assert.False(page.HasNextPage);
        }

        [Fact]
        public void EmptyResult_NoCursor()
        {
            // Arrange, Act
            InfinitePageData<int> page = new([], null);

            // Assert
            Assert.Empty(page.Entities);
            Assert.Null(page.NextCursor);
            Assert.False(page.HasNextPage);
        }
    }

    // =========================================================================
    // Record equality
    // =========================================================================

    public class RecordEquality
    {
        [Fact]
        public void TwoDefaultInstances_AreEqual()
        {
            // Arrange
            InfinitePageData<int> a = new();
            InfinitePageData<int> b = new();

            // Assert
            Assert.Equal(a, b);
        }

        [Fact]
        public void DifferentEntities_SameCursor_AreNotEqual()
        {
            // Arrange
            InfinitePageData<int> a = new([1, 2], null);
            InfinitePageData<int> b = new([3, 4], null);

            // Assert
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void SameEntities_DifferentCursors_AreNotEqual()
        {
            // Arrange
            InfinitePageData<int> a = new([1], new DynamicCursor([1, "a"]));
            InfinitePageData<int> b = new([1], new DynamicCursor([1, "b"]));

            // Assert
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void SameEntities_SameCursors_AreEqual()
        {
            // Arrange
            InfinitePageData<int> a = new([1], new DynamicCursor([1, "a"]));
            InfinitePageData<int> b = new([1], new DynamicCursor([1, "a"]));

            // Assert
            Assert.Equal(a, b);
        }
    }

    // =========================================================================
    // JSON round-trip
    // =========================================================================

    public class JsonRoundTrip
    {
        private readonly JsonSerializerOptions _serializerOptions = new() { Converters = { new DynamicCursorJsonConverter() } };

        [Fact]
        public void SurviveRoundTrip()
        {
            // Arrange
            DynamicCursor cursor = new([(byte)42, "abc"]);
            InfinitePageData<int> page = new([10, 20, 30], cursor);

            // Act
            string json = JsonSerializer.Serialize(page, _serializerOptions);
            InfinitePageData<int>? restored = JsonSerializer.Deserialize<InfinitePageData<int>>(json, _serializerOptions)!;

            // Assert
            Assert.Equal(page, restored);
            Assert.True(restored.Entities.SequenceEqual([10, 20, 30]));
            Assert.Equal(page.NextCursor, restored.NextCursor);
            Assert.True(restored.HasNextPage);
        }

        [Fact]
        public void NullCursorToken_SurvivesRoundTrip()
        {
            // Arrange
            InfinitePageData<int> page = new(null, null);

            // Act
            string json = JsonSerializer.Serialize(page, _serializerOptions);
            InfinitePageData<int>? restored = JsonSerializer.Deserialize<InfinitePageData<int>>(json, _serializerOptions)!;

            // Assert
            Assert.Equal(page, restored);
            Assert.Empty(restored.Entities);
            Assert.Null(restored.NextCursor);
            Assert.False(restored.HasNextPage);
        }

        [Fact]
        public void SerializedJson_ContainsExpectedPropertyNames()
        {
            // Arrange
            InfinitePageData<int> page = new([1], SomeCursor());

            // Act
            string json = JsonSerializer.Serialize(page, _serializerOptions);

            // Assert
            Assert.Contains(nameof(InfinitePageData<>.Entities), json);
            Assert.Contains(nameof(InfinitePageData<>.NextCursor), json);
            Assert.Contains(nameof(InfinitePageData<>.HasNextPage), json);
        }
    }
}
