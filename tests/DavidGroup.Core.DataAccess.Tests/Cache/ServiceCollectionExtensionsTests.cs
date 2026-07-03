using DavidGroup.Core.DataAccess.Cache;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

using RedLockNet;

using StackExchange.Redis;

namespace DavidGroup.Core.DataAccess.Tests.Cache;

public static class ServiceCollectionExtensionsTests
{
    // -------------------------------------------------------------------------
    // AddRedis()
    // -------------------------------------------------------------------------

    public class AddResistTests
    {
        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private const string ConnectionString = "redis://localhost:6379,abortConnect=false";

        private static IConfiguration BuildConfiguration(string? connectionString)
        {
            Dictionary<string, string?> data = new();

            if (connectionString is not null)
                data["ConnectionStrings:Redis"] = connectionString;

            return new ConfigurationBuilder()
                .AddInMemoryCollection(data)
                .Build();
        }

        private static IConnectionMultiplexer ResolveMultiplexer(IServiceCollection services)
        {
            return services.BuildServiceProvider().GetRequiredService<IConnectionMultiplexer>();
        }

        // -------------------------------------------------------------------------
        // Registration — explicit connection string
        // -------------------------------------------------------------------------

        [Fact]
        public void Registers_IConnectionMultiplexer_When_ConnectionString_Provided_Explicitly()
        {
            // Arrange
            ServiceCollection services = [];
            services.AddRedis(ConnectionString);

            ServiceProvider provider = services.BuildServiceProvider();

            // Act
            IConnectionMultiplexer? multiplexer = provider.GetService<IConnectionMultiplexer>();

            // Assert
            Assert.NotNull(multiplexer);
        }

        [Fact]
        public void Registers_Multiplexer_As_Singleton_When_ConnectionString_Provided_Explicitly()
        {
            // Arrange
            ServiceCollection services = [];
            services.AddRedis(ConnectionString);

            ServiceProvider provider = services.BuildServiceProvider();

            // Act
            IConnectionMultiplexer first = provider.GetRequiredService<IConnectionMultiplexer>();
            IConnectionMultiplexer second = provider.GetRequiredService<IConnectionMultiplexer>();

            // Assert
            Assert.Same(first, second);
        }

        // -------------------------------------------------------------------------
        // Registration — connection string from IConfiguration
        // -------------------------------------------------------------------------

        [Fact]
        public void Registers_IConnectionMultiplexer_When_ConnectionString_Resolved_From_Configuration()
        {
            // Arrange
            ServiceCollection services = [];
            services.AddSingleton(BuildConfiguration(ConnectionString));

            services.AddRedis();

            // Act
            IConnectionMultiplexer multiplexer = ResolveMultiplexer(services);

            // Assert
            Assert.NotNull(multiplexer);
        }

        [Fact]
        public void Registers_Multiplexer_As_Singleton_When_ConnectionString_Resolved_From_Configuration()
        {
            // Arrange
            ServiceCollection services = [];
            services.AddSingleton(BuildConfiguration(ConnectionString));
            services.AddRedis();

            ServiceProvider provider = services.BuildServiceProvider();

            // Act
            IConnectionMultiplexer first = provider.GetRequiredService<IConnectionMultiplexer>();
            IConnectionMultiplexer second = provider.GetRequiredService<IConnectionMultiplexer>();

            // Assert
            Assert.Same(first, second);
        }

        // -------------------------------------------------------------------------
        // Error — missing connection string
        // -------------------------------------------------------------------------

        [Fact]
        public void Throws_InvalidOperationException_When_No_ConnectionString_In_Configuration()
        {
            // Arrange
            ServiceCollection services = [];
            services.AddSingleton(BuildConfiguration(connectionString: null));
            services.AddRedis();

            ServiceProvider provider = services.BuildServiceProvider();

            // Act, Assert

            // The factory is lazy — the exception surfaces on first resolution.
            Assert.Throws<InvalidOperationException>(
                provider.GetRequiredService<IConnectionMultiplexer>);
        }

        [Fact]
        public void Exception_Message_Indicates_Missing_ConnectionString()
        {
            // Arrange
            ServiceCollection services = [];
            services.AddSingleton(BuildConfiguration(connectionString: null));
            services.AddRedis();

            ServiceProvider provider = services.BuildServiceProvider();

            // Act, Assert
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                provider.GetRequiredService<IConnectionMultiplexer>);

            Assert.Equal("No redis connection string found.", ex.Message);
        }

        // -------------------------------------------------------------------------
        // Other
        // -------------------------------------------------------------------------

        [Fact]
        public void EnsureNoDuplicateRegistrations()
        {
            // Arrange, Act
            ServiceCollection services = [];
            services.AddRedis(ConnectionString);
            services.AddRedis(ConnectionString);

            List<string?> duplicates = services
                .GroupBy(descriptor => descriptor.ServiceType)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key.FullName)
                .ToList();

            // Assert
            Assert.Empty(duplicates);
        }
    }

    // -------------------------------------------------------------------------
    // AddDistributedCache()
    // -------------------------------------------------------------------------

    public class AddDistributedCacheTests
    {
        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private const string ConnectionString = "redis://localhost:6379,abortConnect=false";

        private class ManualHostEnvironment : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Development;
            public string ApplicationName { get; set; } = "TestsApp";
            public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }

        private static IHostEnvironment BuildHostEnvironment(string environmentName)
        {
            return new ManualHostEnvironment
            {
                EnvironmentName = environmentName
            };
        }

        private static IConfiguration BuildConfiguration(string? connectionString)
        {
            Dictionary<string, string?> data = new();

            if (connectionString is not null)
                data["ConnectionStrings:Redis"] = connectionString;

            return new ConfigurationBuilder()
                .AddInMemoryCollection(data)
                .Build();
        }

        private static IDistributedCache ResolveDistributedCache(IServiceCollection services)
        {
            return services.BuildServiceProvider().GetRequiredService<IDistributedCache>();
        }

        // -------------------------------------------------------------------------
        // Registration — explicit connection string
        // -------------------------------------------------------------------------

        [Fact]
        public void Registers_IDistributedCache_When_ConnectionString_Provided_Explicitly_InDevelopment()
        {
            // Arrange
            ServiceCollection services = [];
            IHostEnvironment hostEnvironment = BuildHostEnvironment(Environments.Development);
            IConfiguration configuration = BuildConfiguration(connectionString: null);

            services.AddDistributedCache(hostEnvironment, configuration, ConnectionString);

            ServiceProvider provider = services.BuildServiceProvider();

            // Act
            IDistributedCache? cache = provider.GetService<IDistributedCache>();

            // Assert
            Assert.NotNull(cache);
        }

        [Fact]
        public void Registers_IDistributedCache_When_ConnectionString_Provided_Explicitly_InProduction()
        {
            // Arrange
            ServiceCollection services = [];
            IHostEnvironment hostEnvironment = BuildHostEnvironment(Environments.Production);
            IConfiguration configuration = BuildConfiguration(connectionString: null);

            services.AddDistributedCache(hostEnvironment, configuration, ConnectionString);

            ServiceProvider provider = services.BuildServiceProvider();

            // Act
            IDistributedCache? cache = provider.GetService<IDistributedCache>();

            // Assert
            Assert.NotNull(cache);
        }

        // -------------------------------------------------------------------------
        // Registration — connection string from IConfiguration
        // -------------------------------------------------------------------------

        [Fact]
        public void Registers_IDistributedCache_When_ConnectionString_Resolved_From_Configuration_InDevelopment()
        {
            // Arrange
            ServiceCollection services = [];
            IHostEnvironment hostEnvironment = BuildHostEnvironment(Environments.Development);
            IConfiguration configuration = BuildConfiguration(connectionString: null);

            services.AddDistributedCache(hostEnvironment, configuration, ConnectionString);

            // Act
            IDistributedCache cache = ResolveDistributedCache(services);

            // Assert
            Assert.NotNull(cache);
        }

        [Fact]
        public void Registers_IDistributedCache_When_ConnectionString_Resolved_From_Configuration_InProduction()
        {
            // Arrange
            ServiceCollection services = [];
            IHostEnvironment hostEnvironment = BuildHostEnvironment(Environments.Production);
            IConfiguration configuration = BuildConfiguration(connectionString: null);

            services.AddDistributedCache(hostEnvironment, configuration, ConnectionString);

            // Act
            IDistributedCache cache = ResolveDistributedCache(services);

            // Assert
            Assert.NotNull(cache);
        }

        // -------------------------------------------------------------------------
        // Error — missing connection string
        // -------------------------------------------------------------------------

        [Fact]
        public void Throws_InvalidOperationException_When_No_ConnectionString_In_Configuration()
        {
            // Arrange
            ServiceCollection services = [];
            IHostEnvironment hostEnvironment = BuildHostEnvironment(Environments.Production);
            IConfiguration configuration = BuildConfiguration(connectionString: null);

            services.AddDistributedCache(hostEnvironment, configuration);

            ServiceProvider provider = services.BuildServiceProvider();

            // Act, Assert

            // The factory is lazy — the exception surfaces on first resolution.
            Assert.Throws<InvalidOperationException>(
                provider.GetRequiredService<IDistributedCache>);
        }

        [Fact]
        public void Exception_Message_Indicates_Missing_ConnectionString()
        {
            // Arrange
            ServiceCollection services = [];
            IHostEnvironment hostEnvironment = BuildHostEnvironment(Environments.Production);
            IConfiguration configuration = BuildConfiguration(connectionString: null);

            services.AddDistributedCache(hostEnvironment, configuration);

            ServiceProvider provider = services.BuildServiceProvider();

            // Act, Assert
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                provider.GetRequiredService<IDistributedCache>);

            Assert.Equal("No redis connection string found.", ex.Message);
        }
    }

    // -------------------------------------------------------------------------
    // AddRedLock()
    // -------------------------------------------------------------------------

    public class AddRedLockTests
    {
        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private string[] _endpoints =
        [
            "redis1.example.com:6379,abortConnect=false",
            "redis2.example.com:6379,abortConnect=false",
            "redis3.example.com:6379,abortConnect=false"
        ];

        private static IConfiguration BuildConfiguration(string[] endpoints)
        {
            Dictionary<string, string?> data = new();

            for (int i = 0; i < endpoints.Length; i++)
                data[$"RedLock:Endpoints:{i}"] = endpoints[i];

            return new ConfigurationBuilder()
                .AddInMemoryCollection(data)
                .Build();
        }

        private static IDistributedLockFactory ResolveLockFactory(IServiceCollection services)
        {
            return services.BuildServiceProvider().GetRequiredService<IDistributedLockFactory>();
        }

        // -------------------------------------------------------------------------
        // Registration — connection string from IConfiguration
        // -------------------------------------------------------------------------

        [Fact]
        public void Registers_IDistributedLockFactory_When_ConnectionString_Resolved_From_Configuration()
        {
            // Arrange
            ServiceCollection services = [];
            IConfiguration configuration = BuildConfiguration(_endpoints);

            services.AddRedLock(configuration);

            // Act
            IDistributedLockFactory lockFactory = ResolveLockFactory(services);

            // Assert
            Assert.NotNull(lockFactory);
        }

        [Fact]
        public void Registers_IDistributedLockFactory_As_Singleton_When_ConnectionString_Resolved_From_Configuration()
        {
            // Arrange
            ServiceCollection services = [];
            IConfiguration configuration = BuildConfiguration(_endpoints);

            services.AddRedLock(configuration);

            ServiceProvider provider = services.BuildServiceProvider();

            // Act
            IDistributedLockFactory first = provider.GetRequiredService<IDistributedLockFactory>();
            IDistributedLockFactory second = provider.GetRequiredService<IDistributedLockFactory>();

            // Assert
            Assert.Same(first, second);
        }

        // -------------------------------------------------------------------------
        // Error — missing connection string
        // -------------------------------------------------------------------------

        [Fact]
        public void Throws_InvalidOperationException_When_No_ConnectionString_In_Configuration()
        {
            // Arrange
            ServiceCollection services = [];
            IConfiguration configuration = BuildConfiguration(endpoints: []);

            // Act, Assert

            // The factory is lazy — the exception surfaces on first resolution.
            Assert.Throws<InvalidOperationException>(
                () => services.AddRedLock(configuration));
        }

        [Fact]
        public void Exception_Message_Indicates_Missing_ConnectionString()
        {
            // Arrange
            ServiceCollection services = [];
            IConfiguration configuration = BuildConfiguration(endpoints: []);

            // Act, Assert
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => services.AddRedLock(configuration));

            Assert.Equal("RedLock endpoints are not configured.", ex.Message);
        }

        // -------------------------------------------------------------------------
        // Other
        // -------------------------------------------------------------------------

        [Fact]
        public void EnsureNoDuplicateRegistrations()
        {
            // Arrange, Act
            ServiceCollection services = [];
            IConfiguration configuration = BuildConfiguration(_endpoints);

            services.AddRedLock(configuration);
            services.AddRedLock(configuration);

            List<string?> duplicates = services
                .GroupBy(descriptor => descriptor.ServiceType)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key.FullName)
                .ToList();

            // Assert
            Assert.Empty(duplicates);
        }
    }
}
