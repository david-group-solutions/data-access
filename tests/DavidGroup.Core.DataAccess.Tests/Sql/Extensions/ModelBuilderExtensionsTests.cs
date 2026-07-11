using DavidGroup.Core.DataAccess.Sql.Entities;
using DavidGroup.Core.DataAccess.Sql.Extensions;
using DavidGroup.Core.DataAccess.Sql.ValueGenerators;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace DavidGroup.Core.DataAccess.Tests.Sql.Extensions;

public static class ModelBuilderExtensionsTests
{
    // -------------------------------------------------------------------------
    // ModelBuilderExtensions.ApplySequentialGuids tests
    // -------------------------------------------------------------------------

    public sealed class ApplySequentialGuids
    {
        private class SeqGuidTestEntity : Entity<Guid>, ISequentialGuid
        {
            public string Name { get; set; } = string.Empty;
        }

        private class PlainIdTestEntity : Entity<Guid>
        {
            public string Name { get; set; } = string.Empty;
        }

        private class ModelBuilderExtensionsTestDbContext(DbContextOptions<ModelBuilderExtensionsTestDbContext> options)
            : DbContext(options)
        {
            public DbSet<SeqGuidTestEntity> SeqGuidEntities => Set<SeqGuidTestEntity>();
            public DbSet<PlainIdTestEntity> PlainIdEntities => Set<PlainIdTestEntity>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.ApplySequentialGuids();
            }
        }

        [Fact]
        public void SequentialIdEntity_ConfiguresValueGenerator()
        {
            // Arrange
            DbContextOptions<ModelBuilderExtensionsTestDbContext> options =
                new DbContextOptionsBuilder<ModelBuilderExtensionsTestDbContext>()
                    .UseSqlServer("Server=Test;Database=Test;Trusted_Connection=True;TrustServerCertificate=True;")
                    .Options;

            using ModelBuilderExtensionsTestDbContext context = new(options);

            // Act
            IEntityType entityType = context.Model.FindEntityType(typeof(SeqGuidTestEntity))!;
            IProperty idProperty = context.Model
                .FindEntityType(typeof(SeqGuidTestEntity))!
                .FindProperty(nameof(SeqGuidTestEntity.Id))!;

            Func<IProperty, ITypeBase, ValueGenerator> factory = idProperty.GetValueGeneratorFactory()!;
            ValueGenerator generator = factory(idProperty, entityType);

            // Assert
            Assert.IsType<SequentialGuidValueGenerator>(generator);
        }

        [Fact]
        public void PlainEntity_DoesNotConfigureValueGenerator()
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

            Func<IProperty, ITypeBase, ValueGenerator>? factory = idProperty.GetValueGeneratorFactory();

            // Assert
            Assert.Null(factory);
        }
    }

    // -------------------------------------------------------------------------
    // ModelBuilderExtensions.ApplyStronglyTypedSequentialIds tests
    // -------------------------------------------------------------------------

    public sealed class ApplyStronglyTypedSequentialIds
    {
        private struct SeqId(Guid id)
        {
            public Guid Id { get; set; } = id;
        }

        private class StronglyTypedSeqIdTestEntity : Entity<SeqId>, IStronglyTypedSequentialId<SeqId>
        {
            public string Name { get; set; } = string.Empty;
        }

        private class PlainIdTestEntity : Entity<SeqId>
        {
            public string Name { get; set; } = string.Empty;
        }

        private class ModelBuilderExtensionsTestDbContext(DbContextOptions<ModelBuilderExtensionsTestDbContext> options)
            : DbContext(options)
        {
            public DbSet<StronglyTypedSeqIdTestEntity> StronglyTypedSeqIdEntities => Set<StronglyTypedSeqIdTestEntity>();
            public DbSet<PlainIdTestEntity> PlainIdEntities => Set<PlainIdTestEntity>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.ApplyStronglyTypedSequentialIds();

                modelBuilder.Entity<StronglyTypedSeqIdTestEntity>()
                    .Property(x => x.Id)
                    .HasConversion(v => v.Id, v => new SeqId(v));

                modelBuilder.Entity<PlainIdTestEntity>()
                    .Property(x => x.Id)
                    .HasConversion(v => v.Id, v => new SeqId(v));
            }
        }

        [Fact]
        public void StronglyTypedSequentialIdEntity_ConfiguresValueGenerator()
        {
            // Arrange
            DbContextOptions<ModelBuilderExtensionsTestDbContext> options =
                new DbContextOptionsBuilder<ModelBuilderExtensionsTestDbContext>()
                    .UseSqlServer("Server=Test;Database=Test;Trusted_Connection=True;TrustServerCertificate=True;")
                    .Options;

            using ModelBuilderExtensionsTestDbContext context = new(options);

            // Act
            IEntityType entityType = context.Model.FindEntityType(typeof(StronglyTypedSeqIdTestEntity))!;
            IProperty idProperty = context.Model
                .FindEntityType(typeof(StronglyTypedSeqIdTestEntity))!
                .FindProperty(nameof(StronglyTypedSeqIdTestEntity.Id))!;

            Func<IProperty, ITypeBase, ValueGenerator> factory = idProperty.GetValueGeneratorFactory()!;
            ValueGenerator generator = factory(idProperty, entityType);

            // Assert
            Assert.IsType<SequentialStronglyTypedIdValueGenerator<SeqId>>(generator);
        }

        [Fact]
        public void PlainEntity_DoesNotConfigureValueGenerator()
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

            Func<IProperty, ITypeBase, ValueGenerator>? factory = idProperty.GetValueGeneratorFactory();

            // Assert
            Assert.Null(factory);
        }
    }

    // -------------------------------------------------------------------------
    // ModelBuilderExtensions.ApplySqlServerSequentialIds tests
    // -------------------------------------------------------------------------

    public sealed class ApplySqlServerSequentialIdsTests
    {
        private class SqlServerSeqIdTestEntity : Entity<Guid>, ISqlServerSequentialId<Guid>
        {
            public string Name { get; set; } = string.Empty;
        }

        private class PlainIdTestEntity : Entity<Guid>
        {
            public string Name { get; set; } = string.Empty;
        }

        private class ModelBuilderExtensionsTestDbContext(DbContextOptions<ModelBuilderExtensionsTestDbContext> options)
            : DbContext(options)
        {
            public DbSet<SqlServerSeqIdTestEntity> SqlServerSeqIdEntities => Set<SqlServerSeqIdTestEntity>();
            public DbSet<PlainIdTestEntity> PlainIdEntities => Set<PlainIdTestEntity>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.ApplySqlServerSequentialIds(this);
            }
        }

        [Fact]
        public void SqlServerProviderWithSequentialIdEntity_ConfiguresNewSequentialIdDefault()
        {
            // Arrange
            DbContextOptions<ModelBuilderExtensionsTestDbContext> options =
                new DbContextOptionsBuilder<ModelBuilderExtensionsTestDbContext>()
                    .UseSqlServer("Server=Test;Database=Test;Trusted_Connection=True;TrustServerCertificate=True;")
                    .Options;

            using ModelBuilderExtensionsTestDbContext context = new(options);

            // Act
            IProperty idProperty = context.Model
                .FindEntityType(typeof(SqlServerSeqIdTestEntity))!
                .FindProperty(nameof(SqlServerSeqIdTestEntity.Id))!;

            // Assert
            Assert.Equal("NEWSEQUENTIALID()", idProperty.GetDefaultValueSql());
        }

        [Fact]
        public void SqlServerProviderWithPlainEntity_DoesNotConfigureDefaultValueSql()
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
