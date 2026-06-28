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
            Assert.Null(page.NextCursorToken);
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
            InfinitePageData<int> page = new(entities, null, false);

            // Assert
            Assert.Equal(entities, page.Entities);
        }

        [Fact]
        public void NullEntities_FallBackToEmptyEnumerable()
        {
            // Arrange, Act
            InfinitePageData<int> page = new(null, null, false);

            // Assert
            Assert.Empty(page.Entities);
        }

        [Fact]
        public void EmptyEntities_AreStoredAsEmpty()
        {
            // Arrange, Act
            InfinitePageData<int> page = new([], null, false);

            // Assert
            Assert.Empty(page.Entities);
        }

        [Fact]
        public void EntitiesPreserveOrder()
        {
            // Arrange
            List<int> entities = [3, 1, 2];

            // Act
            InfinitePageData<int> page = new(entities, null, false);

            // Assert
            Assert.True(page.Entities.SequenceEqual([3, 1, 2]));
        }

        [Fact]
        public void WorksWithStringType()
        {
            // Arrange
            List<string> entities = ["a", "b"];

            // Act
            InfinitePageData<string> page = new(entities, null, false);

            // Assert
            Assert.True(page.Entities.SequenceEqual(["a", "b"]));
        }

        [Fact]
        public void WorksWithReferenceType()
        {
            // Arrange
            List<object> items = [new(), new()];

            // Act
            InfinitePageData<object> page = new(items, null, false);

            // Assert
            Assert.Equal(2, page.Entities.Count());
        }
    }

    // =========================================================================
    // Main constructor — NextCursor & NextCursorToken
    // =========================================================================

    public class CtorNextCursor
    {
        [Fact]
        public void NonNullCursor_IsStoredCorrectly()
        {
            // Arrange, Act
            DynamicCursor cursor = SomeCursor();
            InfinitePageData<int> page = new(null, cursor, false);

            // Assert
            Assert.Same(cursor, page.NextCursor);
            Assert.NotNull(page.NextCursorToken);
            Assert.Equal(cursor.Encode(), page.NextCursorToken);
        }

        [Fact]
        public void NullCursor_IsStoredAsNull()
        {
            // Arrange, Act
            InfinitePageData<int> page = new(null, null, false);

            // Assert
            Assert.Null(page.NextCursor);
            Assert.Null(page.NextCursorToken);
        }

        [Fact]
        public void DifferentCursors_ProduceDifferentTokens()
        {
            // Arrange
            DynamicCursor cursorA = new([1]);
            DynamicCursor cursorB = new([2]);

            // Act
            InfinitePageData<int> pageA = new(null, cursorA, false);
            InfinitePageData<int> pageB = new(null, cursorB, false);

            // Assert
            Assert.NotEqual(pageA.NextCursor, pageB.NextCursor);
            Assert.NotEqual(pageA.NextCursorToken, pageB.NextCursorToken);
        }

        [Fact]
        public void NextCursorToken_CanBeDecodedBackToNextCursor()
        {
            // Arrange
            DynamicCursor cursor = new([99, "page-key"]);
            InfinitePageData<int> page = new(null, cursor, false);

            // Act
            DynamicCursor? decoded = DynamicCursorTokenizer.Decode(page.NextCursorToken);

            // Assert
            Assert.NotNull(decoded);
            Assert.Equal((byte)99, decoded.Values[0]);
            Assert.Equal("page-key", decoded.Values[1]);
        }
    }

    // =========================================================================
    // Main constructor — HasNextPage
    // =========================================================================

    public class CtorHasNextPage
    {
        [Fact]
        public void TrueIsStored()
        {
            // Arrange, Act
            InfinitePageData<int> page = new(null, null, true);

            // Assert
            Assert.True(page.HasNextPage);
        }

        [Fact]
        public void FalseIsStored()
        {
            // Arrange, Act
            InfinitePageData<int> page = new(null, null, false);

            // Assert
            Assert.False(page.HasNextPage);
        }
    }

    // =========================================================================
    // Combined / realistic scenarios
    // =========================================================================

    public class RealisticScenarios
    {
        [Fact]
        public void FullPage_WithCursor_HasNextPage_True()
        {
            // Arrange
            List<int> entities = SomeEntities();
            DynamicCursor cursor = SomeCursor();

            // Act
            InfinitePageData<int> page = new(entities, cursor, hasNextPage: true);

            // Assert
            Assert.Equal(entities, page.Entities);
            Assert.Same(cursor, page.NextCursor);
            Assert.NotNull(page.NextCursorToken);
            Assert.Equal(cursor.Encode(), page.NextCursorToken);
            Assert.True(page.HasNextPage);
        }

        [Fact]
        public void LastPage_NoCursor_HasNextPage_False()
        {
            // Arrange
            List<int> entities = SomeEntities();

            // Act
            InfinitePageData<int> page = new(entities, nextCursor: null, hasNextPage: false);

            // Assert
            Assert.Equal(entities, page.Entities);
            Assert.Null(page.NextCursor);
            Assert.Null(page.NextCursorToken);
            Assert.False(page.HasNextPage);
        }

        [Fact]
        public void EmptyResult_NoCursor_HasNextPage_False()
        {
            // Arrange, Act
            InfinitePageData<int> page = new([], null, hasNextPage: false);

            // Assert
            Assert.Empty(page.Entities);
            Assert.Null(page.NextCursor);
            Assert.Null(page.NextCursorToken);
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
        public void SameHasNextPage_DifferentEntities_AreNotEqual()
        {
            // Arrange
            InfinitePageData<int> a = new([1, 2], null, false);
            InfinitePageData<int> b = new([3, 4], null, false);

            // Assert
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void SameEntities_DifferentHasNextPage_AreNotEqual()
        {
            // Arrange
            InfinitePageData<int> a = new([1], null, true);
            InfinitePageData<int> b = new([1], null, false);

            // Assert
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void SameEntities_SameHasNextPag_DifferentCursors_AreNotEqual()
        {
            // Arrange
            InfinitePageData<int> a = new([1], new DynamicCursor([1, "a"]), true);
            InfinitePageData<int> b = new([1], new DynamicCursor([1, "b"]), true);

            // Assert
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void SameEntities_SameCursors_SameHasNextPag_AreEqual()
        {
            // Arrange
            InfinitePageData<int> a = new([1], new DynamicCursor([1, "a"]), true);
            InfinitePageData<int> b = new([1], new DynamicCursor([1, "a"]), true);

            // Assert
            Assert.Equal(a, b);
        }
    }

    // =========================================================================
    // JSON round-trip
    // =========================================================================

    public class JsonRoundTrip
    {
        [Fact]
        public void SurviveRoundTrip()
        {
            // Arrange
            DynamicCursor cursor = new([(byte)42, "abc"]);
            InfinitePageData<int> page = new([10, 20, 30], cursor, true);

            JsonSerializerOptions serializerOptions = new();
            serializerOptions.Converters.Add(new DynamicCursorJsonConverter());

            // Act
            string json = JsonSerializer.Serialize(page, serializerOptions);
            InfinitePageData<int>? restored = JsonSerializer.Deserialize<InfinitePageData<int>>(json, serializerOptions)!;

            // Assert
            Assert.Equal(page, restored);
            Assert.True(restored.Entities.SequenceEqual([10, 20, 30]));
            Assert.Equal(page.NextCursor, restored.NextCursor);
            Assert.Equal(page.NextCursorToken, restored.NextCursorToken);
            Assert.True(restored.HasNextPage);
        }

        [Fact]
        public void NullCursorToken_SurvivesRoundTrip()
        {
            // Arrange
            InfinitePageData<int> page = new(null, null, false);

            JsonSerializerOptions serializerOptions = new();
            serializerOptions.Converters.Add(new DynamicCursorJsonConverter());

            // Act
            string json = JsonSerializer.Serialize(page, serializerOptions);
            InfinitePageData<int>? restored = JsonSerializer.Deserialize<InfinitePageData<int>>(json, serializerOptions)!;

            // Assert
            Assert.Equal(page, restored);
            Assert.Empty(restored.Entities);
            Assert.Null(restored.NextCursor);
            Assert.Null(restored.NextCursorToken);
            Assert.False(restored.HasNextPage);
        }

        [Fact]
        public void SerializedJson_ContainsExpectedPropertyNames()
        {
            // Arrange
            InfinitePageData<int> page = new([1], SomeCursor(), true);

            // Act
            string json = JsonSerializer.Serialize(page);

            // Assert
            Assert.Contains(nameof(InfinitePageData<>.Entities), json);
            Assert.Contains(nameof(InfinitePageData<>.NextCursor), json);
            Assert.Contains(nameof(InfinitePageData<>.NextCursorToken), json);
            Assert.Contains(nameof(InfinitePageData<>.HasNextPage), json);
        }
    }
}
