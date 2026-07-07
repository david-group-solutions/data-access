using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Sql.Builders;
using DavidGroup.Core.DataAccess.Sql.Builders.InfiniteScrollPaginationQuery;

namespace DavidGroup.Core.DataAccess.Tests.Sql.Builders.InfiniteScrollPaginationQuery;

public static class InfiniteScrollPaginationSearchAfterFilterBuilderTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private sealed class TestEntity
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsActive { get; init; }
    }

    // -------------------------------------------------------------------------
    // InfiniteScrollPaginationSearchAfterFilterBuilder.Build<TEntity> tests
    // -------------------------------------------------------------------------

    public class BuildTests
    {
        [Fact]
        public void WhenOrderingSpecificationsAreEmpty_ShouldThrowInvalidOperationException()
        {
            // Arrange
            IReadOnlyList<OrderingSpecification<TestEntity>> orderingSpecifications = [];

            DynamicCursor cursor = new([1]);

            // Act
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(()
                => InfiniteScrollPaginationSearchAfterFilterBuilder.Build(orderingSpecifications, cursor));

            // Assert
            Assert.Equal("No ordering specifications were found.", exception.Message);
        }

        [Fact]
        public void WhenSingleAscendingIntegerOrdering_ShouldBuildCorrectPredicate()
        {
            // Arrange
            IReadOnlyList<OrderingSpecification<TestEntity>> orderingSpecifications =
            [
                new(entity => entity.Id, false)
            ];

            DynamicCursor cursor = new([5]);

            // Act
            Expression<Func<TestEntity, bool>> expression =
                InfiniteScrollPaginationSearchAfterFilterBuilder.Build(orderingSpecifications, cursor);

            Func<TestEntity, bool> predicate = expression.Compile();

            // Assert
            Assert.False(predicate(new TestEntity { Id = 4 }));
            Assert.False(predicate(new TestEntity { Id = 5 }));
            Assert.True(predicate(new TestEntity { Id = 6 }));
        }

        [Fact]
        public void WhenSingleDescendingIntegerOrdering_ShouldBuildCorrectPredicate()
        {
            // Arrange
            IReadOnlyList<OrderingSpecification<TestEntity>> orderingSpecifications =
            [
                new(entity => entity.Id, true)
            ];

            DynamicCursor cursor = new([5]);

            // Act
            Expression<Func<TestEntity, bool>> expression =
                InfiniteScrollPaginationSearchAfterFilterBuilder.Build(orderingSpecifications, cursor);

            Func<TestEntity, bool> predicate = expression.Compile();

            // Assert
            Assert.True(predicate(new TestEntity { Id = 4 }));
            Assert.False(predicate(new TestEntity { Id = 5 }));
            Assert.False(predicate(new TestEntity { Id = 6 }));
        }

        [Fact]
        public void WhenAscendingStringOrdering_ShouldBuildCorrectPredicate()
        {
            // Arrange
            IReadOnlyList<OrderingSpecification<TestEntity>> orderingSpecifications =
            [
                new(entity => entity.Name, false)
            ];

            DynamicCursor cursor = new(["AAA"]);

            // Act
            Expression<Func<TestEntity, bool>> expression =
                InfiniteScrollPaginationSearchAfterFilterBuilder.Build(orderingSpecifications, cursor);

            Func<TestEntity, bool> predicate = expression.Compile();

            // Assert
            Assert.False(predicate(new TestEntity { Name = "A" }));
            Assert.False(predicate(new TestEntity { Name = "AA" }));
            Assert.True(predicate(new TestEntity { Name = "B" }));
        }

        [Fact]
        public void WhenAscendingBooleanOrdering_ShouldBuildCorrectPredicate()
        {
            // Arrange
            IReadOnlyList<OrderingSpecification<TestEntity>> orderingSpecifications =
            [
                new(entity => entity.IsActive, false)
            ];

            DynamicCursor cursor = new([false]);

            // Act
            Expression<Func<TestEntity, bool>> expression =
                InfiniteScrollPaginationSearchAfterFilterBuilder.Build(orderingSpecifications, cursor);

            Func<TestEntity, bool> predicate = expression.Compile();

            // Assert
            Assert.True(predicate(new TestEntity { IsActive = true }));
            Assert.False(predicate(new TestEntity { IsActive = false }));
        }

        [Fact]
        public void WhenMultipleOrderingSpecifications_ShouldBuildSearchAfterPredicate()
        {
            // Arrange
            IReadOnlyList<OrderingSpecification<TestEntity>> orderingSpecifications =
            [
                new(entity => entity.Id, false),
                new(entity => entity.Name, false)
            ];

            DynamicCursor cursor = new([5, "Bob"]);

            // Act
            Expression<Func<TestEntity, bool>> expression =
                InfiniteScrollPaginationSearchAfterFilterBuilder.Build(orderingSpecifications, cursor);

            Func<TestEntity, bool> predicate = expression.Compile();

            // Assert
            Assert.True(predicate(new TestEntity
            {
                Id = 6,
                Name = "Aaron"
            }));

            Assert.True(predicate(new TestEntity
            {
                Id = 6,
                Name = "Bob"
            }));

            Assert.False(predicate(new TestEntity
            {
                Id = 5,
                Name = "Bob"
            }));

            Assert.False(predicate(new TestEntity
            {
                Id = 5,
                Name = "Alice"
            }));

            Assert.False(predicate(new TestEntity
            {
                Id = 4,
                Name = "Zulu"
            }));
        }

        [Fact]
        public void WhenMultipleOrderingSpecifications_And_InvalidCursor_ShouldThrowInvalidOperationException()
        {
            // Arrange
            IReadOnlyList<OrderingSpecification<TestEntity>> orderingSpecifications =
            [
                new(entity => entity.Id, false),
                new(entity => entity.Name, false)
            ];

            DynamicCursor cursor = new([2025]); // Invalid cursor; does not match to orderingSpecifications.

            // Act
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                InfiniteScrollPaginationSearchAfterFilterBuilder.Build(orderingSpecifications, cursor));

            Assert.Equal("Invalid cursor provided.", ex.Message);
        }

        [Fact]
        public void WhenMultipleOrderingSpecifications_And_InvalidCursorWithNotMatchingTypes_ShouldThrowInvalidOperationException()
        {
            // Arrange
            IReadOnlyList<OrderingSpecification<TestEntity>> orderingSpecifications =
            [
                new(entity => entity.Id, false),
                new(entity => entity.Name, false)
            ];

            DynamicCursor cursor = new(["1", true]); // Invalid cursor; types don't match to orderingSpecifications.

            // Act
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
                InfiniteScrollPaginationSearchAfterFilterBuilder.Build(orderingSpecifications, cursor));

            Assert.Equal("Invalid cursor provided.", ex.Message);
        }
    }
}
