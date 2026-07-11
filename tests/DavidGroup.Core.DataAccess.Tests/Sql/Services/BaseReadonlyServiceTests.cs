using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Pagination;
using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Results;
using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.DataAccess.Sql.Entities;
using DavidGroup.Core.DataAccess.Sql.Repositories;
using DavidGroup.Core.DataAccess.Sql.Services;

using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccess.Tests.Sql.Services;

public static class BaseReadonlyServiceTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private class BaseReadonlySvcTestEntity : Entity<int>
    {
        public string Name { get; set; } = string.Empty;
    }

    private class BaseReadonlySvcTestReadDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private class BaseReadonlySvcTestDbContext(DbContextOptions<BaseReadonlySvcTestDbContext> options) : DbContext(options)
    {
        public DbSet<BaseReadonlySvcTestEntity> Entities => Set<BaseReadonlySvcTestEntity>();
    }

    private sealed class BaseReadonlySvcTestRepository(DbContext context)
        : BaseRepository<BaseReadonlySvcTestEntity, int>(context);

    private sealed class BaseReadonlySvcTestReadonlyService(BaseReadonlySvcTestRepository repository)
        : BaseReadonlyService<BaseReadonlySvcTestRepository, BaseReadonlySvcTestEntity, int, BaseReadonlySvcTestReadDto>(repository)
    {
        protected override Expression<Func<BaseReadonlySvcTestEntity, BaseReadonlySvcTestReadDto>> ToReadDto =>
            entity => new BaseReadonlySvcTestReadDto
            {
                Id = entity.Id,
                Name = entity.Name
            };
    }

    private static BaseReadonlySvcTestDbContext CreateContext(params BaseReadonlySvcTestEntity[] entities)
    {
        DbContextOptions<BaseReadonlySvcTestDbContext> options = new DbContextOptionsBuilder<BaseReadonlySvcTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        BaseReadonlySvcTestDbContext context = new(options);

        context.Entities.AddRange(entities);
        context.SaveChanges();

        return context;
    }

    private static BaseReadonlySvcTestEntity[] CreateFourEntities()
    {
        return
        [
            new BaseReadonlySvcTestEntity
            {
                Id = 1,
                Name = "Alpha"
            },

            new BaseReadonlySvcTestEntity
            {
                Id = 2,
                Name = "Beta"
            },

            new BaseReadonlySvcTestEntity
            {
                Id = 3,
                Name = "Gamma"
            },

            new BaseReadonlySvcTestEntity
            {
                Id = 4,
                Name = "Delta"
            }
        ];
    }

    // -------------------------------------------------------------------------
    // BaseReadonlyService.GetAllAsync (no pagination) tests
    // -------------------------------------------------------------------------

    public sealed class GetAllAsyncTests
    {
        [Fact]
        public async Task ReturnsSuccessWithAllEntitiesMappedToReadDto()
        {
            // Arrange
            await using BaseReadonlySvcTestDbContext context = CreateContext(CreateFourEntities());
            BaseReadonlySvcTestReadonlyService service = new(new BaseReadonlySvcTestRepository(context));

            // Act
            OperationResult<List<BaseReadonlySvcTestReadDto>> result = await service.GetAllAsync();

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(4, result.Value.Count);
            Assert.Contains(result.Value, dto => dto.Name == "Alpha");
            Assert.Contains(result.Value, dto => dto.Name == "Beta");
            Assert.Contains(result.Value, dto => dto.Name == "Gamma");
            Assert.Contains(result.Value, dto => dto.Name == "Delta");
        }
    }

    // -------------------------------------------------------------------------
    // BaseReadonlyService.GetAllAsync (offset pagination) tests
    // -------------------------------------------------------------------------

    public sealed class GetAllAsyncOffsetPaginationTests
    {
        [Fact]
        public async Task WithoutOrderBy_ReturnsPagedResults()
        {
            // Arrange
            await using BaseReadonlySvcTestDbContext context = CreateContext(CreateFourEntities());
            BaseReadonlySvcTestReadonlyService service = new(new BaseReadonlySvcTestRepository(context));

            PageOptions options = new()
            {
                Page = 1,
                Size = 2
            };

            // Act
            OperationResult<PageData<BaseReadonlySvcTestReadDto>> result = await service.GetAllAsync(options);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(4, result.Value.TotalCount);
            Assert.Equal(2, result.Value.Entities.Count);
            Assert.Contains(result.Value.Entities, dto => dto.Name == "Alpha");
            Assert.Contains(result.Value.Entities, dto => dto.Name == "Beta");
            Assert.DoesNotContain(result.Value.Entities, dto => dto.Name == "Gamma");
            Assert.DoesNotContain(result.Value.Entities, dto => dto.Name == "Delta");
        }

        [Fact]
        public async Task WithValidOrderBy_ReturnsResultsInSpecifiedOrder()
        {
            // Arrange
            await using BaseReadonlySvcTestDbContext context = CreateContext(CreateFourEntities());
            BaseReadonlySvcTestReadonlyService service = new(new BaseReadonlySvcTestRepository(context));

            PageOptions options = new()
            {
                Page = 1,
                Size = 3
            };

            // Act
            OperationResult<PageData<BaseReadonlySvcTestReadDto>> result =
                await service.GetAllAsync(options, orderBy: "Id desc");

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal([
                "Delta",
                "Gamma",
                "Beta"
            ], result.Value.Entities.Select(dto => dto.Name));
        }

        [Fact]
        public async Task WithDisallowedOrderByField_ReturnsFailureWithoutQuerying()
        {
            // Arrange
            await using BaseReadonlySvcTestDbContext context = CreateContext();
            BaseReadonlySvcTestReadonlyService service = new(new BaseReadonlySvcTestRepository(context));

            PageOptions options = new()
            {
                Page = 1,
                Size = 10
            };
            List<Expression<Func<BaseReadonlySvcTestEntity, object>>> allowedToOrderBy =
            [
                entity => entity.Id
            ];

            // Act
            OperationResult<PageData<BaseReadonlySvcTestReadDto>> result =
                await service.GetAllAsync(options, orderBy: "Name", allowedToOrderBy: allowedToOrderBy);

            // Assert
            Assert.False(result.Succeeded);
        }
    }

    // -------------------------------------------------------------------------
    // BaseReadonlyService.GetAllAsync (cursor pagination) tests
    // -------------------------------------------------------------------------

    public sealed class GetAllAsyncCursorPaginationTests
    {
        [Fact]
        public async Task WithoutOrderBy_ReturnsPagedResults()
        {
            // Arrange
            await using BaseReadonlySvcTestDbContext context = CreateContext(CreateFourEntities());
            BaseReadonlySvcTestReadonlyService service = new(new BaseReadonlySvcTestRepository(context));

            InfinitePageOptions options = new() { Size = 2 };

            // Act
            OperationResult<InfinitePageData<BaseReadonlySvcTestReadDto>> result = await service.GetAllAsync(options);

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Value.NextCursor);
            Assert.Equal(2, result.Value.Entities.Count);
            Assert.Contains(result.Value.Entities, dto => dto.Name == "Gamma");
            Assert.Contains(result.Value.Entities, dto => dto.Name == "Delta");
            Assert.DoesNotContain(result.Value.Entities, dto => dto.Name == "Alpha");
            Assert.DoesNotContain(result.Value.Entities, dto => dto.Name == "Beta");
        }

        [Fact]
        public async Task WithValidOrderBy_ReturnsResultsInSpecifiedOrder()
        {
            // Arrange
            await using BaseReadonlySvcTestDbContext context = CreateContext(CreateFourEntities());
            BaseReadonlySvcTestReadonlyService service = new(new BaseReadonlySvcTestRepository(context));

            InfinitePageOptions options = new() { Size = 3 };

            // Act
            OperationResult<InfinitePageData<BaseReadonlySvcTestReadDto>> result =
                await service.GetAllAsync(options, orderBy: "Id desc");

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal([
                "Delta",
                "Gamma",
                "Beta"
            ], result.Value.Entities.Select(dto => dto.Name));
        }

        [Fact]
        public async Task WithDisallowedOrderByField_ReturnsFailureWithoutQuerying()
        {
            // Arrange
            await using BaseReadonlySvcTestDbContext context = CreateContext();
            BaseReadonlySvcTestReadonlyService service = new(new BaseReadonlySvcTestRepository(context));

            InfinitePageOptions options = new() { Size = 3 };
            List<Expression<Func<BaseReadonlySvcTestEntity, object>>> allowedToOrderBy =
            [
                entity => entity.Id
            ];

            // Act
            OperationResult<InfinitePageData<BaseReadonlySvcTestReadDto>> result =
                await service.GetAllAsync(options, orderBy: "Name", allowedToOrderBy: allowedToOrderBy);

            // Assert
            Assert.False(result.Succeeded);
        }
    }

    // -------------------------------------------------------------------------
    // BaseReadonlyService.GetByIdAsync tests
    // -------------------------------------------------------------------------

    public sealed class GetByIdAsyncTests
    {
        [Fact]
        public async Task ExistingId_ReturnsSuccessWithMappedDto()
        {
            // Arrange
            await using BaseReadonlySvcTestDbContext context = CreateContext(CreateFourEntities());
            BaseReadonlySvcTestReadonlyService service = new(new BaseReadonlySvcTestRepository(context));

            // Act
            OperationResult<BaseReadonlySvcTestReadDto> result = await service.GetByIdAsync(1);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal("Alpha", result.Value.Name);
        }

        [Fact]
        public async Task NonExistingId_ReturnsFailure()
        {
            // Arrange
            await using BaseReadonlySvcTestDbContext context = CreateContext();
            BaseReadonlySvcTestReadonlyService service = new(new BaseReadonlySvcTestRepository(context));

            // Act
            OperationResult<BaseReadonlySvcTestReadDto> result = await service.GetByIdAsync(99);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(ErrorMessages.NotFound, result.Messages[0].Message);
            Assert.Equal(OperationResultSeverity.Error, result.Messages[0].Severity);
        }
    }
}
