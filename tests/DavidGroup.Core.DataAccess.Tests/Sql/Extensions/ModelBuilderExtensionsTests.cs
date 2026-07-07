using DavidGroup.Core.DataAccess.Sql.Entities;
using DavidGroup.Core.DataAccess.Sql.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DavidGroup.Core.DataAccess.Tests.Sql.Extensions;

file class SeqIdTestEntity : Entity<Guid>, ISqlServerSequentialId<Guid>
{
    public string Name { get; set; } = string.Empty;
}

file class PlainIdTestEntity : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
}

file class ModelBuilderExtensionsTestDbContext(DbContextOptions<ModelBuilderExtensionsTestDbContext> options)
    : DbContext(options)
{
    public DbSet<SeqIdTestEntity> SeqIdEntities => Set<SeqIdTestEntity>();
    public DbSet<PlainIdTestEntity> PlainIdEntities => Set<PlainIdTestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplySqlServerSequentialIds(this);
    }
}

public static class ModelBuilderExtensionsTests
{
    // -------------------------------------------------------------------------
    // ModelBuilderExtensions.ApplySqlServerSequentialIds tests
    // -------------------------------------------------------------------------

    public sealed class ApplySqlServerSequentialIdsTests
    {
        [Fact]
        public void ApplySqlServerSequentialIds_SqlServerProviderWithSequentialIdEntity_ConfiguresNewSequentialIdDefault()
        {
            // Arrange
            DbContextOptions<ModelBuilderExtensionsTestDbContext> options =
                new DbContextOptionsBuilder<ModelBuilderExtensionsTestDbContext>()
                    .UseSqlServer("Server=Test;Database=Test;Trusted_Connection=True;TrustServerCertificate=True;")
                    .Options;

            using ModelBuilderExtensionsTestDbContext context = new(options);

            // Act
            IProperty idProperty = context.Model
                .FindEntityType(typeof(SeqIdTestEntity))!
                .FindProperty(nameof(SeqIdTestEntity.Id))!;

            // Assert
            Assert.Equal("NEWSEQUENTIALID()", idProperty.GetDefaultValueSql());
        }

        [Fact]
        public void ApplySqlServerSequentialIds_SqlServerProviderWithPlainEntity_DoesNotConfigureDefaultValueSql()
        {
            // Arrange
            DbContextOptions<ModelBuilderExtensionsTestDbContext> options =
                new DbContextOptionsBuilder<ModelBuilderExtensionsTestDbContext>()
                    .UseSqlServer("Server=Test;Database=Test;Trusted_Connection=True;TrustServerCertificate=True;")
                    .Options;

            using ModelBuilderExtensionsTestDbContext context = new(options);

            // Act
            IProperty idProperty = context.Model
                .FindEntityType(typeof(PlainIdTestEntity))!
                .FindProperty(nameof(PlainIdTestEntity.Id))!;

            // Assert
            Assert.Null(idProperty.GetDefaultValueSql());
        }
    }
}
