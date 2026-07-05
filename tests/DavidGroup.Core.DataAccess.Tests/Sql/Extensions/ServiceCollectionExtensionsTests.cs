using System.Data.Common;
using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Sql.Entities;
using DavidGroup.Core.DataAccess.Sql.Extensions;
using DavidGroup.Core.DataAccess.Sql.Interceptors;
using DavidGroup.Core.DataAccess.Sql.Repositories;
using DavidGroup.Core.DataAccess.Sql.Services;
using DavidGroup.Core.DataAccess.Sql.UnitOfWork.ADO.NET;
using DavidGroup.Core.DataAccess.Sql.UnitOfWork.EFCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DavidGroup.Core.DataAccess.Tests.Sql.Extensions;

public static class ServiceCollectionExtensionsTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private class SpyTestDbContext(DbContextOptions<SpyTestDbContext> options) : DbContext(options);

    private class SqlServerSpyDbContext(DbContextOptions<SqlServerSpyDbContext> options) : DbContext(options);

    private class EfUowTestDbContext(DbContextOptions<EfUowTestDbContext> options) : DbContext(options);

    private class DummyRepoEntity : Entity<int>;

    private class DummyRepoDbContext(DbContextOptions<DummyRepoDbContext> options) : DbContext(options)
    {
        public DbSet<DummyRepoEntity> Entities => Set<DummyRepoEntity>();
    }

    private interface IDummyRepository : IBaseRepository<DummyRepoEntity, int>;

    private sealed class DummyRepository(DummyRepoDbContext context) : BaseRepository<DummyRepoEntity, int>(context),
        IDummyRepository;

    private class DummyReadDto
    {
        public int Id { get; set; }
    }

    private interface IDummyReadonlyService : IBaseReadonlyService<DummyRepoEntity, int, DummyReadDto>;

    private sealed class DummyReadonlyService(IDummyRepository repository)
        : BaseReadonlyService<IDummyRepository, DummyRepoEntity, int, DummyReadDto>(repository),
            IDummyReadonlyService
    {
        protected override Expression<Func<DummyRepoEntity, DummyReadDto>> ToReadDto =>
            entity => new DummyReadDto { Id = entity.Id };
    }

    // -------------------------------------------------------------------------
    // ServiceCollectionExtensions.AddDatabase tests
    // -------------------------------------------------------------------------

    public sealed class AddDatabaseTests
    {
        [Fact]
        public void GivenExplicitConnectionString_PassesItToConfigureProvider()
        {
            // Arrange
            ServiceCollection services = new();

            string? capturedConnectionString = null;
            Action<DbContextOptionsBuilder, string, string> spyConfigureProvider = (builder, connStr, asmName) =>
            {
                capturedConnectionString = connStr;

                builder.UseInMemoryDatabase(Guid.NewGuid().ToString());
            };

            // Act
            services.AddDatabase<SpyTestDbContext>(spyConfigureProvider, connectionString: "Server=Explicit;Database=Test;");

            ServiceProvider provider = services.BuildServiceProvider();
            _ = provider.GetRequiredService<SpyTestDbContext>();

            // Assert
            Assert.Equal("Server=Explicit;Database=Test;", capturedConnectionString);
        }

        [Fact]
        public void WithoutConnectionString_ResolvesFromConfiguration()
        {
            // Arrange
            ServiceCollection services = new();

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "Server=FromConfig;Database=Test;" })
                .Build();
            services.AddSingleton(configuration);

            string? capturedConnectionString = null;
            Action<DbContextOptionsBuilder, string, string> spyConfigureProvider = (builder, connStr, asmName) =>
            {
                capturedConnectionString = connStr;

                builder.UseInMemoryDatabase(Guid.NewGuid().ToString());
            };

            // Act
            services.AddDatabase<SpyTestDbContext>(spyConfigureProvider);

            ServiceProvider provider = services.BuildServiceProvider();
            _ = provider.GetRequiredService<SpyTestDbContext>();

            // Assert
            Assert.Equal("Server=FromConfig;Database=Test;", capturedConnectionString);
        }

        [Fact]
        public void WithoutConnectionStringOrConfiguration_ThrowsInvalidOperationException()
        {
            // Arrange
            ServiceCollection services = new();

            IConfiguration emptyConfiguration = new ConfigurationBuilder().Build();
            services.AddSingleton(emptyConfiguration);

            Action<DbContextOptionsBuilder, string, string> spyConfigureProvider = (builder, connStr, asmName) =>
                builder.UseInMemoryDatabase(Guid.NewGuid().ToString());

            // Act
            services.AddDatabase<SpyTestDbContext>(spyConfigureProvider);
            ServiceProvider provider = services.BuildServiceProvider();

            // Act & Assert
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                provider.GetRequiredService<SpyTestDbContext>);

            Assert.Equal("No connection string was provided and 'DefaultConnection' was not found in configuration.",
                ex.Message);
        }

        [Fact]
        public void WithoutAssemblyName_DefaultsToTestsAssembly()
        {
            // Arrange
            ServiceCollection services = new();

            string? capturedAssemblyName = null;
            Action<DbContextOptionsBuilder, string, string> spyConfigureProvider = (builder, connStr, asmName) =>
            {
                capturedAssemblyName = asmName;

                builder.UseInMemoryDatabase(Guid.NewGuid().ToString());
            };

            // Act
            services.AddDatabase<SpyTestDbContext>(spyConfigureProvider, connectionString: "Server=Test;Database=Test;");

            ServiceProvider provider = services.BuildServiceProvider();
            _ = provider.GetRequiredService<SpyTestDbContext>();

            // Assert
            Assert.Equal(typeof(ServiceCollectionExtensionsTests).Assembly.GetName().Name, capturedAssemblyName);
        }

        [Fact]
        public void RegistersTimedEntitiesAndSoftDeleteInterceptors()
        {
            // Arrange
            ServiceCollection services = new();

            Action<DbContextOptionsBuilder, string, string> spyConfigureProvider = (builder, connStr, asmName) =>
                builder.UseInMemoryDatabase(Guid.NewGuid().ToString());

            // Act
            services.AddDatabase<SpyTestDbContext>(spyConfigureProvider, connectionString: "Server=Test;Database=Test;");

            ServiceProvider provider = services.BuildServiceProvider();
            DbContextOptions<SpyTestDbContext> options = provider.GetRequiredService<DbContextOptions<SpyTestDbContext>>();
            CoreOptionsExtension? coreExtension = options.FindExtension<CoreOptionsExtension>();

            // Assert
            Assert.NotNull(coreExtension);
            Assert.Contains(coreExtension.Interceptors!, interceptor => interceptor is TimedEntitiesInterceptor);
            Assert.Contains(coreExtension.Interceptors!, interceptor => interceptor is SoftDeleteInterceptor);
        }
    }

    // -------------------------------------------------------------------------
    // ServiceCollectionExtensions.AddSqlServerDatabase tests
    // -------------------------------------------------------------------------

    public sealed class AddSqlServerDatabaseTests
    {
        [Fact]
        public void GivenConnectionString_RegistersResolvableDbContext()
        {
            // Arrange
            ServiceCollection services = new();

            // Act
            services.AddSqlServerDatabase<SqlServerSpyDbContext>(
                connectionString: "Server=Test;Database=Test;Trusted_Connection=True;TrustServerCertificate=True;");

            ServiceProvider provider = services.BuildServiceProvider();
            SqlServerSpyDbContext context = provider.GetRequiredService<SqlServerSpyDbContext>();

            // Assert
            Assert.NotNull(context);
        }
    }

    // -------------------------------------------------------------------------
    // ServiceCollectionExtensions.AddEfUnitOfWork tests
    // -------------------------------------------------------------------------

    public sealed class AddEfUnitOfWorkTests
    {
        [Fact]
        public void RegistersResolvableUnitOfWork()
        {
            // Arrange
            ServiceCollection services = new();

            services.AddDbContext<EfUowTestDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            // Act
            services.AddEfUnitOfWork<EfUowTestDbContext>();

            ServiceProvider provider = services.BuildServiceProvider();
            IEfUnitOfWork<EfUowTestDbContext> unitOfWork =
                provider.GetRequiredService<IEfUnitOfWork<EfUowTestDbContext>>();

            // Assert
            Assert.IsType<EfUnitOfWork<EfUowTestDbContext>>(unitOfWork);
        }

        [Fact]
        public void CalledTwice_RegistersOnlyOnce()
        {
            // Arrange
            ServiceCollection services = new();

            services.AddDbContext<EfUowTestDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            // Act
            services.AddEfUnitOfWork<EfUowTestDbContext>();
            services.AddEfUnitOfWork<EfUowTestDbContext>();

            // Assert
            int registrationCount = services.Count(descriptor =>
                descriptor.ServiceType == typeof(IEfUnitOfWork<EfUowTestDbContext>));

            Assert.Equal(1, registrationCount);
        }
    }

    // -------------------------------------------------------------------------
    // ServiceCollectionExtensions.AddAdoNetUnitOfWork tests
    // -------------------------------------------------------------------------

    public sealed class AddAdoNetUnitOfWorkTests
    {
        [Fact]
        public void RegistersResolvableUnitOfWork()
        {
            // Arrange
            ServiceCollection services = new();

            Func<DbConnection> connectionFactory = () => null!;

            // Act
            services.AddAdoNetUnitOfWork(connectionFactory);

            ServiceProvider provider = services.BuildServiceProvider();
            IAdoNetUnitOfWork unitOfWork = provider.GetRequiredService<IAdoNetUnitOfWork>();

            // Assert
            Assert.IsType<AdoNetUnitOfWork>(unitOfWork);
        }

        [Fact]
        public void CalledTwice_RegistersOnlyOnce()
        {
            // Arrange
            ServiceCollection services = new();

            Func<DbConnection> connectionFactory = () => null!;

            // Act
            services.AddAdoNetUnitOfWork(connectionFactory);
            services.AddAdoNetUnitOfWork(connectionFactory);

            // Assert
            int registrationCount = services.Count(descriptor =>
                descriptor.ServiceType == typeof(IAdoNetUnitOfWork));

            Assert.Equal(1, registrationCount);
        }
    }

    // -------------------------------------------------------------------------
    // ServiceCollectionExtensions.AddAllRepositoriesAuto tests
    // -------------------------------------------------------------------------

    public sealed class AddAllRepositoriesAutoTests
    {
        [Fact]
        public void ScansCallingAssembly_RegistersDiscoveredRepository()
        {
            // Arrange
            ServiceCollection services = new();

            services.AddDbContext<DummyRepoDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            // Act
            services.AddAllRepositoriesAuto<AddAllRepositoriesAutoTests>();

            ServiceProvider provider = services.BuildServiceProvider();
            IDummyRepository repository = provider.GetRequiredService<IDummyRepository>();

            // Assert
            Assert.IsType<DummyRepository>(repository);
        }
    }

    // -------------------------------------------------------------------------
    // ServiceCollectionExtensions.AddAllServicesAuto tests
    // -------------------------------------------------------------------------

    public sealed class AddAllServicesAutoTests
    {
        [Fact]
        public void ScansCallingAssembly_RegistersDiscoveredService()
        {
            // Arrange
            ServiceCollection services = new();

            services.AddDbContext<DummyRepoDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            services.AddScoped<IDummyRepository, DummyRepository>();

            // Act
            services.AddAllServicesAuto<AddAllServicesAutoTests>();

            ServiceProvider provider = services.BuildServiceProvider();
            IDummyReadonlyService service = provider.GetRequiredService<IDummyReadonlyService>();

            // Assert
            Assert.IsType<DummyReadonlyService>(service);
        }
    }
}
