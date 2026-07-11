using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Sql.Builders.InfiniteScrollPaginationQuery;

using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccess.Tests.Sql.Builders.InfiniteScrollPaginationQuery;

public static class InfiniteScrollPaginationDynamicCursorBuilderTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private class InfiniteScrollTestEntity
    {
        public int Id { get; set; }
        public int Age { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class InfiniteScrollTestDbContext(DbContextOptions<InfiniteScrollTestDbContext> options)
        : DbContext(options)
    {
        public DbSet<InfiniteScrollTestEntity> Entities => Set<InfiniteScrollTestEntity>();
    }

    private static InfiniteScrollTestDbContext CreateContext()
    {
        DbContextOptions<InfiniteScrollTestDbContext> options =
            new DbContextOptionsBuilder<InfiniteScrollTestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        return new InfiniteScrollTestDbContext(options);
    }

    // -------------------------------------------------------------------------
    // InfiniteScrollPaginationDynamicCursorBuilder.BuildNextCursorAsync tests
    // -------------------------------------------------------------------------

    public sealed class BuildNextCursorTests
    {
        [Fact]
        public async Task BuildNextCursorAsync_SingleOrderByExpression_ReturnsCursorWithNextItemValue()
        {
            // Arrange
            await using InfiniteScrollTestDbContext context = CreateContext();
            context.Entities.AddRange(
                new InfiniteScrollTestEntity
                {
                    Id = 1,
                    Age = 20,
                    Name = "Alpha"
                },
                new InfiniteScrollTestEntity
                {
                    Id = 2,
                    Age = 25,
                    Name = "Beta"
                },
                new InfiniteScrollTestEntity
                {
                    Id = 3,
                    Age = 30,
                    Name = "Gamma"
                },
                new InfiniteScrollTestEntity
                {
                    Id = 4,
                    Age = 35,
                    Name = "Delta"
                });
            await context.SaveChangesAsync();

            IQueryable<InfiniteScrollTestEntity> ordered = context.Entities.OrderBy(entity => entity.Id);
            List<Expression<Func<InfiniteScrollTestEntity, object>>> orderBy = [entity => entity.Id];

            // Act
            DynamicCursor cursor =
                await InfiniteScrollPaginationDynamicCursorBuilder.BuildNextCursorAsync(ordered, orderBy, 3);

            // Assert
            Assert.Equal([3], cursor.Values);
        }

        [Fact]
        public async Task BuildNextCursorAsync_MultipleOrderByExpressions_ReturnsCursorWithAllValuesInOrder()
        {
            // Arrange
            await using InfiniteScrollTestDbContext context = CreateContext();
            context.Entities.AddRange(
                new InfiniteScrollTestEntity
                {
                    Id = 1,
                    Age = 30,
                    Name = "Alpha"
                },
                new InfiniteScrollTestEntity
                {
                    Id = 2,
                    Age = 30,
                    Name = "Beta"
                },
                new InfiniteScrollTestEntity
                {
                    Id = 3,
                    Age = 20,
                    Name = "Gamma"
                });
            await context.SaveChangesAsync();

            IQueryable<InfiniteScrollTestEntity> ordered = context.Entities
                .OrderBy(entity => entity.Age)
                .ThenBy(entity => entity.Id);

            List<Expression<Func<InfiniteScrollTestEntity, object>>> orderBy =
            [
                entity => entity.Age,
                entity => entity.Id
            ];

            // Act
            DynamicCursor cursor =
                await InfiniteScrollPaginationDynamicCursorBuilder.BuildNextCursorAsync(ordered, orderBy, 2);

            // Assert
            Assert.Equal([30, 1], cursor.Values);
        }

        [Fact]
        public async Task BuildNextCursorAsync_PageSizeExceedsAllItems_ThrowsInvalidOperationException()
        {
            // Arrange
            await using InfiniteScrollTestDbContext context = CreateContext();
            context.Entities.AddRange(
                new InfiniteScrollTestEntity
                {
                    Id = 1,
                    Age = 20,
                    Name = "Alpha"
                },
                new InfiniteScrollTestEntity
                {
                    Id = 2,
                    Age = 25,
                    Name = "Beta"
                });
            await context.SaveChangesAsync();

            IQueryable<InfiniteScrollTestEntity> ordered = context.Entities.OrderBy(entity => entity.Id);
            List<Expression<Func<InfiniteScrollTestEntity, object>>> orderBy = [entity => entity.Id];

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(()
                => InfiniteScrollPaginationDynamicCursorBuilder.BuildNextCursorAsync(ordered, orderBy, 3));
        }

        [Fact]
        public async Task BuildNextCursorAsync_CancelledToken_ThrowsOperationCanceledException()
        {
            // Arrange
            await using InfiniteScrollTestDbContext context = CreateContext();
            context.Entities.Add(new InfiniteScrollTestEntity
            {
                Id = 1,
                Age = 20,
                Name = "Alpha"
            });
            await context.SaveChangesAsync();

            IQueryable<InfiniteScrollTestEntity> ordered = context.Entities.OrderBy(entity => entity.Id);
            List<Expression<Func<InfiniteScrollTestEntity, object>>> orderBy = [entity => entity.Id];

            CancellationTokenSource cancellationTokenSource = new();
            await cancellationTokenSource.CancelAsync();

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(()
                => InfiniteScrollPaginationDynamicCursorBuilder.BuildNextCursorAsync(
                    ordered, orderBy, 0, cancellationTokenSource.Token));
        }
    }
}
