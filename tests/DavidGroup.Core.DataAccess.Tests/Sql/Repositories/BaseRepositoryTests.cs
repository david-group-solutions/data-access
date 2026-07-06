using DavidGroup.Core.DataAccess.Pagination;
using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Sql.Builders;
using DavidGroup.Core.DataAccess.Sql.Entities;
using DavidGroup.Core.DataAccess.Sql.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DavidGroup.Core.DataAccess.Tests.Sql.Repositories;

public static class BaseRepositoryTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private class RepoTestEntity : Entity<int>
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public RepoTestEntityAddress RepoTestEntityAddress { get; set; } = new();
    }

    private class RepoTestEntityAddress : Entity<int>
    {
        public string City { get; set; } = string.Empty;
    }

    private class RepoTestDbContext(DbContextOptions<RepoTestDbContext> options) : DbContext(options)
    {
        public DbSet<RepoTestEntity> Entities => Set<RepoTestEntity>();
    }

    private sealed class RepoTestRepository(DbContext context) : BaseRepository<RepoTestEntity, int>(context);

    private static RepoTestDbContext CreateContext(params RepoTestEntity[] entities)
    {
        DbContextOptions<RepoTestDbContext> options = new DbContextOptionsBuilder<RepoTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        RepoTestDbContext context = new(options);

        context.Entities.AddRange(entities);
        context.SaveChanges();

        return context;
    }

    private static RepoTestEntity[] CreateFourEntities()
    {
        return
        [
            new RepoTestEntity
            {
                Id = 1,
                Name = "Alpha",
                Age = 30,
                RepoTestEntityAddress = new RepoTestEntityAddress { City = "New York" }
            },

            new RepoTestEntity
            {
                Id = 2,
                Name = "Beta",
                Age = 20,
                RepoTestEntityAddress = new RepoTestEntityAddress { City = "London" }
            },

            new RepoTestEntity
            {
                Id = 3,
                Name = "Gamma",
                Age = 25,
                RepoTestEntityAddress = new RepoTestEntityAddress { City = "Berlin" }
            },

            new RepoTestEntity
            {
                Id = 4,
                Name = "Delta",
                Age = 18,
                RepoTestEntityAddress = new RepoTestEntityAddress { City = "Bern" }
            }
        ];
    }

    // -------------------------------------------------------------------------
    // BaseRepository<TEntity, TKey>.GetAllAsync (no pagination) tests
    // -------------------------------------------------------------------------

    public sealed class GetAllListTests
    {
        [Fact]
        public async Task WithPredicateOrderingInclude_ReturnsCorrectResults()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext(CreateFourEntities());
            RepoTestRepository repository = new(context);

            // Act
            var result = await repository.GetAllAsync(
                predicate: entity => entity.Age >= 25,
                orderBy: query => query.OrderBy(entity => entity.Age),
                include: query => query.Include(entity => entity.RepoTestEntityAddress),
                selector: entity => new
                {
                    entity.Name,
                    entity.RepoTestEntityAddress.City
                }
            );

            // Assert
            Assert.Equal([
                "Gamma",
                "Alpha"
            ], result.Select(entity => entity.Name));

            Assert.Equal([
                "Berlin",
                "New York"
            ], result.Select(entity => entity.City));
        }

        [Fact]
        public async Task WithoutPredicateOrOrdering_ReturnsAllProjectedResults()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext(CreateFourEntities());
            RepoTestRepository repository = new(context);

            // Act
            List<string> names = await repository.GetAllAsync(selector: entity => entity.Name);

            // Assert
            Assert.Equal(4, names.Count);
            Assert.Contains("Alpha", names);
            Assert.Contains("Beta", names);
            Assert.Contains("Gamma", names);
            Assert.Contains("Delta", names);
        }
    }

    // -------------------------------------------------------------------------
    // BaseRepository<TEntity, TKey>.GetAllAsync (offset pagination, Func ordering) tests
    // -------------------------------------------------------------------------

    public sealed class GetAllOffsetPaginationTests
    {
        [Fact]
        public async Task WithPageOptionsPredicateOrderingInclude_ReturnsCorrectResults()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext(CreateFourEntities());
            RepoTestRepository repository = new(context);

            PageOptions options = new()
            {
                Page = 2,
                Size = 1
            };

            // Act
            var result = await repository.GetAllAsync(
                options,
                predicate: entity => entity.Age >= 25,
                orderBy: query => query.OrderBy(entity => entity.Age),
                include: query => query.Include(entity => entity.RepoTestEntityAddress),
                selector: entity => new
                {
                    entity.Name,
                    entity.RepoTestEntityAddress.City
                }
            );

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Single(result.Entities);
            Assert.Equal("Alpha", result.Entities[0].Name);
            Assert.Equal("New York", result.Entities[0].City);
        }
    }

    // -------------------------------------------------------------------------
    // BaseRepository<TEntity, TKey>.GetAllAsync (offset pagination, OrderingSpecifications) tests
    // -------------------------------------------------------------------------

    public sealed class GetAllOffsetPaginationWithOrderingSpecificationsTests
    {
        [Fact]
        public async Task WithPageOptionsPredicateOrderingInclude_ReturnsCorrectResults()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext(CreateFourEntities());
            RepoTestRepository repository = new(context);

            PageOptions options = new()
            {
                Page = 1,
                Size = 2
            };
            List<OrderingSpecification<RepoTestEntity>> orderingSpecifications =
            [
                new(entity => entity.Age, IsDescending: true)
            ];

            // Act
            var result = await repository.GetAllAsync(
                options,
                predicate: entity => entity.Age >= 25,
                orderingSpecifications: orderingSpecifications,
                include: query => query.Include(entity => entity.RepoTestEntityAddress),
                selector: entity => new
                {
                    entity.Name,
                    entity.RepoTestEntityAddress.City
                }
            );

            // Assert
            Assert.Equal(2, result.TotalCount);

            Assert.Equal([
                "Alpha",
                "Gamma"
            ], result.Entities.Select(entity => entity.Name));

            Assert.Equal([
                "New York",
                "Berlin"
            ], result.Entities.Select(entity => entity.City));
        }
    }

    // -------------------------------------------------------------------------
    // BaseRepository<TEntity, TKey>.GetAllAsync (cursor pagination) tests
    // -------------------------------------------------------------------------

    public sealed class GetAllCursorPaginationTests
    {
        [Fact]
        public async Task WithInfinitePageOptionsPredicateOrderingInclude_ReturnsCorrectResults()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext(CreateFourEntities());
            RepoTestRepository repository = new(context);

            InfinitePageOptions options = new()
            {
                Size = 2,
                SearchAfter = new DynamicCursor([3])
            };
            List<OrderingSpecification<RepoTestEntity>> orderingSpecifications =
            [
                new(entity => entity.Id, IsDescending: true)
            ];

            // Act
            var result = await repository.GetAllAsync(
                options,
                predicate: entity => entity.Age >= 20 && entity.Age <= 25,
                orderingSpecifications: orderingSpecifications,
                include: query => query.Include(entity => entity.RepoTestEntityAddress),
                selector: entity => new
                {
                    entity.Name,
                    entity.RepoTestEntityAddress.City
                }
            );

            // Assert
            Assert.Null(result.NextCursor);
            Assert.Null(result.NextCursorToken);

            Assert.Equal([
                "Beta"
            ], result.Entities.Select(entity => entity.Name));

            Assert.Equal([
                "London"
            ], result.Entities.Select(entity => entity.City));
        }

        [Fact]
        public async Task EmptyOrderingSpecifications_ThrowsArgumentException()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext();
            RepoTestRepository repository = new(context);

            IReadOnlyList<OrderingSpecification<RepoTestEntity>> orderingSpecifications = [];

            // Act & Assert
            ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                repository.GetAllAsync<RepoTestEntity>(null!, orderingSpecifications));

            Assert.Equal("orderingSpecifications", ex.ParamName);
            Assert.Contains("At least one ordering selector must be provided.", ex.Message);
        }
    }

    // -------------------------------------------------------------------------
    // BaseRepository<TEntity, TKey>.FirstOrDefaultAsync tests
    // -------------------------------------------------------------------------

    public sealed class FirstOrDefaultAsyncTests
    {
        [Fact]
        public async Task With_MatchingPredicate_And_Ordering_ReturnsIncludeProjectedEntity()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext(CreateFourEntities());
            RepoTestRepository repository = new(context);

            // Act
            var result = await repository.FirstOrDefaultAsync(
                predicate: entity => entity.Age == 20 || entity.Age == 30,
                orderBy: query => query.OrderBy(entity => entity.Age),
                include: query => query.Include(entity => entity.RepoTestEntityAddress),
                selector: entity => new
                {
                    entity.Name,
                    entity.RepoTestEntityAddress.City
                }
            );

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Beta", result.Name);
            Assert.Equal("London", result.City);
        }

        [Fact]
        public async Task WithNoMatch_ReturnsNull()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext(CreateFourEntities());
            RepoTestRepository repository = new(context);

            // Act
            RepoTestEntity? entity = await repository.FirstOrDefaultAsync(
                selector: entity => entity,
                predicate: entity => entity.Age == 99
            );

            // Assert
            Assert.Null(entity);
        }
    }

    // -------------------------------------------------------------------------
    // BaseRepository<TEntity, TKey>.GetByIdAsync tests
    // -------------------------------------------------------------------------

    public sealed class GetByIdAsyncTests
    {
        [Fact]
        public async Task ExistingId_ReturnsEntity()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext(CreateFourEntities());
            RepoTestRepository repository = new(context);

            // Act
            RepoTestEntity? entity = await repository.GetByIdAsync([1]);

            // Assert
            Assert.NotNull(entity);
            Assert.Equal("Alpha", entity.Name);
        }

        [Fact]
        public async Task NonExistingId_ReturnsNull()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext(CreateFourEntities());
            RepoTestRepository repository = new(context);

            // Act
            RepoTestEntity? entity = await repository.GetByIdAsync([99]);

            // Assert
            Assert.Null(entity);
        }
    }

    // -------------------------------------------------------------------------
    // BaseRepository<TEntity, TKey>.AnyAsync tests
    // -------------------------------------------------------------------------

    public sealed class AnyAsyncTests
    {
        [Fact]
        public async Task MatchingPredicate_ReturnsTrue()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext(CreateFourEntities());
            RepoTestRepository repository = new(context);

            // Act
            bool result = await repository.AnyAsync(entity => entity.Age == 20);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task NoMatchingPredicate_ReturnsFalse()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext(CreateFourEntities());
            RepoTestRepository repository = new(context);

            // Act
            bool result = await repository.AnyAsync(entity => entity.Age == 99);

            // Assert
            Assert.False(result);
        }
    }

    // -------------------------------------------------------------------------
    // BaseRepository<TEntity, TKey>.CreateAsync tests
    // -------------------------------------------------------------------------

    public sealed class CreateAsyncTests
    {
        [Fact]
        public async Task GivenEntity_AddsEntityAndPersistsOnSave()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext();
            RepoTestRepository repository = new(context);

            RepoTestEntity entity = new()
            {
                Id = 1,
                Name = "Alpha",
                Age = 20
            };

            // Act
            EntityEntry<RepoTestEntity> entry = await repository.CreateAsync(entity);
            await context.SaveChangesAsync();

            // Assert
            Assert.Same(entity, entry.Entity);
            Assert.Equal(1, await context.Entities.CountAsync());
        }
    }

    // -------------------------------------------------------------------------
    // BaseRepository<TEntity, TKey>.Update tests
    // -------------------------------------------------------------------------

    public sealed class UpdateTests
    {
        [Fact]
        public void GivenDetachedEntity_AttachesAndMarksAsModified()
        {
            // Arrange
            using RepoTestDbContext context = CreateContext();
            RepoTestRepository repository = new(context);

            RepoTestEntity entity = new()
            {
                Id = 1,
                Name = "Alpha",
                Age = 20
            };

            // Act
            repository.Update(entity);

            // Assert
            Assert.Equal(EntityState.Modified, context.Entry(entity).State);
        }
    }

    // -------------------------------------------------------------------------
    // BaseRepository<TEntity, TKey>.Delete tests
    // -------------------------------------------------------------------------

    public sealed class DeleteTests
    {
        [Fact]
        public void GivenDetachedEntity_AttachesAndMarksAsDeleted()
        {
            // Arrange
            using RepoTestDbContext context = CreateContext();
            RepoTestRepository repository = new(context);

            RepoTestEntity entity = new()
            {
                Id = 1,
                Name = "Alpha",
                Age = 20
            };

            // Act
            repository.Delete(entity);

            // Assert
            Assert.Equal(EntityState.Deleted, context.Entry(entity).State);
        }

        [Fact]
        public async Task GivenTrackedEntity_MarksAsDeletedWithoutReattaching()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext();
            RepoTestRepository repository = new(context);

            RepoTestEntity entity = new()
            {
                Id = 1,
                Name = "Alpha",
                Age = 20
            };
            context.Entities.Add(entity);
            await context.SaveChangesAsync();

            // Act
            repository.Delete(entity);

            // Assert
            Assert.Equal(EntityState.Deleted, context.Entry(entity).State);
        }
    }

    // -------------------------------------------------------------------------
    // BaseRepository<TEntity, TKey>.DeleteAsync tests
    // -------------------------------------------------------------------------

    public sealed class DeleteAsyncTests
    {
        [Fact]
        public async Task ExistingId_DeletesEntityAndReturnsTrue()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext(CreateFourEntities());
            RepoTestRepository repository = new(context);

            // Act
            bool result = await repository.DeleteAsync(1);
            await context.SaveChangesAsync();

            // Assert
            Assert.True(result);
            Assert.Equal(3, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task NonExistingId_ReturnsFalse()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext();
            RepoTestRepository repository = new(context);

            // Act
            bool result = await repository.DeleteAsync(99);

            // Assert
            Assert.False(result);
        }
    }

    // -------------------------------------------------------------------------
    // BaseRepository<TEntity, TKey>.CountAsync tests
    // -------------------------------------------------------------------------

    public sealed class CountAsyncTests
    {
        [Fact]
        public async Task WithPredicate_ReturnsMatchingCount()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext(CreateFourEntities());
            RepoTestRepository repository = new(context);

            // Act
            int count = await repository.CountAsync(entity => entity.Age >= 25);

            // Assert
            Assert.Equal(2, count);
        }
    }

    // -------------------------------------------------------------------------
    // BaseRepository<TEntity, TKey>.LongCountAsync tests
    // -------------------------------------------------------------------------

    public sealed class LongCountAsyncTests
    {
        [Fact]
        public async Task WithPredicate_ReturnsMatchingCount()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext(CreateFourEntities());
            RepoTestRepository repository = new(context);

            // Act
            long count = await repository.LongCountAsync(entity => entity.Age >= 25);

            // Assert
            Assert.Equal(2L, count);
        }
    }

    // -------------------------------------------------------------------------
    // BaseRepository<TEntity, TKey>.AverageAsync tests
    // -------------------------------------------------------------------------

    public sealed class AverageAsyncTests
    {
        [Fact]
        public async Task WithPredicate_ReturnsAverageOfMatchingEntities()
        {
            // Arrange
            await using RepoTestDbContext context = CreateContext();
            RepoTestRepository repository = new(context);

            context.Entities.AddRange(
                new RepoTestEntity
                {
                    Id = 1,
                    Name = "Alpha",
                    Age = 20
                },
                new RepoTestEntity
                {
                    Id = 2,
                    Name = "Beta",
                    Age = 30
                },
                new RepoTestEntity
                {
                    Id = 3,
                    Name = "Gamma",
                    Age = 40
                });
            await context.SaveChangesAsync();

            // Act
            double average = await repository.AverageAsync(
                selector: entity => entity.Age,
                predicate: entity => entity.Age <= 30
            );

            // Assert
            Assert.Equal(25d, average);
        }
    }
}
