using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Sql.Builders;
using DavidGroup.Core.DataAccess.Sql.Builders.InfiniteScrollPaginationQuery;

using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccess.Tests.Sql.Builders.InfiniteScrollPaginationQuery;

public static class InfiniteScrollPaginationQueryBuilderTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDto
    {
        public int Id { get; set; }
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestEntity> Entities => Set<TestEntity>();
    }

    private static TestDbContext CreateContext(params TestEntity[] entities)
    {
        DbContextOptions<TestDbContext> options =
            new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        TestDbContext context = new(options);

        context.Entities.AddRange(entities);
        context.SaveChanges();

        return context;
    }

    private static TestEntity[] CreateFourEntities()
    {
        return
        [
            new TestEntity
            {
                Id = 1,
                Name = "Alpha"
            },

            new TestEntity
            {
                Id = 2,
                Name = "Beta"
            },

            new TestEntity
            {
                Id = 3,
                Name = "Gamma"
            },

            new TestEntity
            {
                Id = 4,
                Name = "Delta"
            }
        ];
    }

    // -------------------------------------------------------------------------
    // Not supported methods
    // -------------------------------------------------------------------------

    public class NotSupportedMethodTests
    {
        [Fact]
        public void WithOrdering_WhenOrderingFuncProvided_ShouldThrowNotSupportedException()
        {
            // Arrange
            IReadOnlyList<TestEntity> entities = [];
            IQueryable<TestEntity> sourceQuery = entities.AsQueryable();

            InfiniteScrollPaginationQueryBuilder<TestEntity> builder = new(sourceQuery);

            // Act
            NotSupportedException exception = Assert.Throws<NotSupportedException>(()
                => builder.WithOrdering(q => q.OrderBy(x => x.Id)));

            // Assert
            Assert.Equal(
                "Use ExecuteWithCursorPagination() method instead and pass the ordering specifications there.",
                exception.Message);
        }

        [Fact]
        public void WithOrdering_WhenOrderingSpecificationsProvided_ShouldThrowNotSupportedException()
        {
            // Arrange
            IReadOnlyList<TestEntity> entities = [];
            IQueryable<TestEntity> sourceQuery = entities.AsQueryable();

            InfiniteScrollPaginationQueryBuilder<TestEntity> builder = new(sourceQuery);

            IReadOnlyList<OrderingSpecification<TestEntity>> ordering = [new(entity => entity.Id, false)];

            // Act
            NotSupportedException exception = Assert.Throws<NotSupportedException>(()
                => builder.WithOrdering(ordering));

            // Assert
            Assert.Equal(
                "Use ExecuteWithCursorPagination() method instead and pass the ordering specifications there.",
                exception.Message);
        }

        [Fact]
        public void WithProjection_WhenCalled_ShouldThrowNotSupportedException()
        {
            // Arrange
            IReadOnlyList<TestEntity> entities = [];
            IQueryable<TestEntity> sourceQuery = entities.AsQueryable();

            InfiniteScrollPaginationQueryBuilder<TestEntity> builder = new(sourceQuery);

            // Act
            NotSupportedException exception = Assert.Throws<NotSupportedException>(()
                => builder.WithProjection(x => new TestDto { Id = x.Id }));

            // Assert
            Assert.Equal(
                "Use ExecuteWithCursorPagination() method instead and pass the selector expression there.",
                exception.Message);
        }
    }

    // -------------------------------------------------------------------------
    // ExecuteWithCursorPagination<TResult> tests
    // -------------------------------------------------------------------------

    public class ExecuteWithCursorPaginationTests
    {
        [Fact]
        public async Task WhenOrderingIsEmpty_ShouldThrowInvalidOperationException()
        {
            // Arrange
            TestEntity[] entities = CreateFourEntities();
            IQueryable<TestEntity> sourceQuery = entities.AsQueryable();

            InfiniteScrollPaginationQueryBuilder<TestEntity> builder = new(sourceQuery);

            InfinitePageOptions options = new() { Size = 2 };

            // Act
            InvalidOperationException exception =
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    builder.ExecuteWithCursorPagination(options, [], x => new TestDto { Id = x.Id }));

            // Assert
            Assert.Equal(
                "No ordering specifications were found. At least one ordering specification must be specified.",
                exception.Message);
        }

        [Fact]
        public async Task WhenFirstPage_ShouldReturnFirstItemsAndNextCursor()
        {
            // Arrange
            TestDbContext context = CreateContext(CreateFourEntities());
            InfiniteScrollPaginationQueryBuilder<TestEntity> builder = new(context.Entities);

            InfinitePageOptions options = new() { Size = 2 };
            IReadOnlyList<OrderingSpecification<TestEntity>> ordering = [new(x => x.Id, false)];

            // Act
            InfinitePageData<TestDto> result =
                await builder.ExecuteWithCursorPagination(options, ordering, x => new TestDto { Id = x.Id });

            // Assert
            Assert.Equal(2, result.Entities.Count);
            Assert.Equal(1, result.Entities[0].Id);
            Assert.Equal(2, result.Entities[1].Id);

            Assert.NotNull(result.NextCursor);
            Assert.NotNull(result.NextCursorToken);

            Assert.Equal([3], result.NextCursor.Values);
        }

        [Fact]
        public async Task WhenLastPage_ShouldReturnAllItemsAndNullCursor()
        {
            // Arrange
            TestDbContext context = CreateContext(CreateFourEntities());
            InfiniteScrollPaginationQueryBuilder<TestEntity> builder = new(context.Entities);

            InfinitePageOptions options = new() { Size = 5 };
            IReadOnlyList<OrderingSpecification<TestEntity>> ordering = [new(x => x.Id, false)];

            // Act
            InfinitePageData<TestDto> result =
                await builder.ExecuteWithCursorPagination(options, ordering, x => new TestDto { Id = x.Id });

            // Assert
            Assert.Equal(4, result.Entities.Count);

            Assert.Null(result.NextCursor);
            Assert.Null(result.NextCursorToken);
        }

        [Fact]
        public async Task WhenSearchAfterProvided_ShouldReturnItemsAfterCursor()
        {
            // Arrange
            TestDbContext context = CreateContext(CreateFourEntities());
            InfiniteScrollPaginationQueryBuilder<TestEntity> builder = new(context.Entities);

            InfinitePageOptions options = new()
            {
                Size = 2,
                SearchAfter = new DynamicCursor([2])
            };

            IReadOnlyList<OrderingSpecification<TestEntity>> ordering = [new(x => x.Id, false)];

            // Act
            InfinitePageData<TestDto> result =
                await builder.ExecuteWithCursorPagination(options, ordering, x => new TestDto { Id = x.Id });

            // Assert
            Assert.Equal(2, result.Entities.Count);
            Assert.Equal(3, result.Entities[0].Id);
            Assert.Equal(4, result.Entities[1].Id);

            Assert.Null(result.NextCursor);
            Assert.Null(result.NextCursorToken);
        }

        [Fact]
        public async Task WhenNextCursorSelectorProvided_ShouldReturnCorrectCursor()
        {
            // Arrange
            TestDbContext context = CreateContext(CreateFourEntities());
            InfiniteScrollPaginationQueryBuilder<TestEntity> builder = new(context.Entities);

            InfinitePageOptions options = new() { Size = 2 };
            IReadOnlyList<OrderingSpecification<TestEntity>> ordering = [new(x => x.Id, false)];

            // Act
            InfinitePageData<TestDto> result = await builder.ExecuteWithCursorPagination(
                options,
                orderingSpecifications: ordering,
                selector: x => new TestDto { Id = x.Id },
                nextCursorSelector: r => [r.Id]
            );

            // Assert
            Assert.NotNull(result.NextCursor);
            Assert.NotNull(result.NextCursorToken);

            Assert.Equal([3], result.NextCursor.Values);
        }
    }
}
