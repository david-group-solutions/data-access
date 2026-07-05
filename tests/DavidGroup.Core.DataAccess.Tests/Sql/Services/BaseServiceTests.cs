using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Results;
using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.DataAccess.Sql.Entities;
using DavidGroup.Core.DataAccess.Sql.Repositories;
using DavidGroup.Core.DataAccess.Sql.Services;
using DavidGroup.Core.DataAccess.Sql.UnitOfWork.EFCore;

using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccess.Tests.Sql.Services;

public static class BaseServiceTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private record BaseSvcTestCreateModel(string Name);

    private record BaseSvcTestUpdateModel(string Name);

    private class BaseSvcTestEntity : Entity<int>,
        ISelfManageable<BaseSvcTestEntity, BaseSvcTestCreateModel, BaseSvcTestUpdateModel>
    {
        private BaseSvcTestEntity() { }

        public string Name { get; private set; } = string.Empty;

        public static BaseSvcTestEntity Create(BaseSvcTestCreateModel model) => new() { Name = model.Name };

        public void Update(BaseSvcTestUpdateModel model) => Name = model.Name;
    }

    private class BaseSvcTestReadDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private class BaseSvcTestDbContext(DbContextOptions<BaseSvcTestDbContext> options) : DbContext(options)
    {
        public DbSet<BaseSvcTestEntity> Entities => Set<BaseSvcTestEntity>();
    }

    private sealed class BaseSvcTestRepository(DbContext context) : BaseRepository<BaseSvcTestEntity, int>(context);

    private sealed class BaseSvcTestService(
        BaseSvcTestRepository repository,
        IEfUnitOfWork<BaseSvcTestDbContext> unitOfWork)
        : BaseService<
            BaseSvcTestDbContext,
            BaseSvcTestRepository,
            BaseSvcTestEntity,
            int,
            BaseSvcTestCreateModel,
            BaseSvcTestUpdateModel,
            BaseSvcTestReadDto>(repository, unitOfWork)
    {
        protected override Expression<Func<BaseSvcTestEntity, BaseSvcTestReadDto>> ToReadDto =>
            entity => new BaseSvcTestReadDto
            {
                Id = entity.Id,
                Name = entity.Name
            };
    }

    private static BaseSvcTestDbContext CreateContext(params BaseSvcTestEntity[] entities)
    {
        DbContextOptions<BaseSvcTestDbContext> options = new DbContextOptionsBuilder<BaseSvcTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        BaseSvcTestDbContext context = new(options);

        context.Entities.AddRange(entities);
        context.SaveChanges();

        return context;
    }

    private static BaseSvcTestEntity[] CreateFourEntities()
    {
        return
        [
            BaseSvcTestEntity.Create(new BaseSvcTestCreateModel("Alpha")),
            BaseSvcTestEntity.Create(new BaseSvcTestCreateModel("Beta")),
            BaseSvcTestEntity.Create(new BaseSvcTestCreateModel("Gamma")),
            BaseSvcTestEntity.Create(new BaseSvcTestCreateModel("Delta"))
        ];
    }

    // -------------------------------------------------------------------------
    // BaseService.CreateAsync tests
    // -------------------------------------------------------------------------

    public sealed class CreateAsyncTests
    {
        [Fact]
        public async Task GivenValidModel_PersistsEntityAndReturnsMappedDto()
        {
            // Arrange
            await using BaseSvcTestDbContext context = CreateContext();
            BaseSvcTestRepository repository = new(context);
            IEfUnitOfWork<BaseSvcTestDbContext> unitOfWork = new EfUnitOfWork<BaseSvcTestDbContext>(context);
            BaseSvcTestService service = new(repository, unitOfWork);

            BaseSvcTestCreateModel model = new("Alpha");

            // Act
            OperationResult<BaseSvcTestReadDto> result = await service.CreateAsync(model);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal("Alpha", result.Value.Name);
            Assert.Equal(1, await context.Entities.CountAsync());
        }
    }

    // -------------------------------------------------------------------------
    // BaseService.UpdateAsync tests
    // -------------------------------------------------------------------------

    public sealed class UpdateAsyncTests
    {
        [Fact]
        public async Task ExistingId_UpdatesEntityAndReturnsMappedDto()
        {
            // Arrange
            await using BaseSvcTestDbContext context = CreateContext(CreateFourEntities());
            BaseSvcTestRepository repository = new(context);
            IEfUnitOfWork<BaseSvcTestDbContext> unitOfWork = new EfUnitOfWork<BaseSvcTestDbContext>(context);
            BaseSvcTestService service = new(repository, unitOfWork);

            BaseSvcTestUpdateModel model = new("Updated Beta");

            // Act
            OperationResult<BaseSvcTestReadDto> result = await service.UpdateAsync(2, model);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal("Updated Beta", result.Value.Name);

            BaseSvcTestEntity? persisted = await context.Entities.FindAsync(2);
            Assert.NotNull(persisted);
            Assert.Equal("Updated Beta", persisted.Name);
        }

        [Fact]
        public async Task NonExistingId_ReturnsFailure()
        {
            // Arrange
            await using BaseSvcTestDbContext context = CreateContext();
            BaseSvcTestRepository repository = new(context);
            IEfUnitOfWork<BaseSvcTestDbContext> unitOfWork = new EfUnitOfWork<BaseSvcTestDbContext>(context);
            BaseSvcTestService service = new(repository, unitOfWork);

            BaseSvcTestUpdateModel model = new("Updated Beta");

            // Act
            OperationResult<BaseSvcTestReadDto> result = await service.UpdateAsync(99, model);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(ErrorMessages.NotFound, result.Messages[0].Message);
            Assert.Equal(OperationResultSeverity.Error, result.Messages[0].Severity);
        }
    }

    // -------------------------------------------------------------------------
    // BaseService.DeleteAsync tests
    // -------------------------------------------------------------------------

    public sealed class DeleteAsyncTests
    {
        [Fact]
        public async Task ExistingId_RemovesEntityAndReturnsSuccess()
        {
            // Arrange
            await using BaseSvcTestDbContext context = CreateContext(CreateFourEntities());
            BaseSvcTestRepository repository = new(context);
            IEfUnitOfWork<BaseSvcTestDbContext> unitOfWork = new EfUnitOfWork<BaseSvcTestDbContext>(context);
            BaseSvcTestService service = new(repository, unitOfWork);

            // Act
            OperationResult result = await service.DeleteAsync(1);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Equal(3, await context.Entities.CountAsync());
        }

        [Fact]
        public async Task NonExistingId_ReturnsFailure()
        {
            // Arrange
            await using BaseSvcTestDbContext context = CreateContext();
            BaseSvcTestRepository repository = new(context);
            IEfUnitOfWork<BaseSvcTestDbContext> unitOfWork = new EfUnitOfWork<BaseSvcTestDbContext>(context);
            BaseSvcTestService service = new(repository, unitOfWork);

            // Act
            OperationResult result = await service.DeleteAsync(99);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal(ErrorMessages.NotFound, result.Messages[0].Message);
            Assert.Equal(OperationResultSeverity.Error, result.Messages[0].Severity);
        }
    }
}
