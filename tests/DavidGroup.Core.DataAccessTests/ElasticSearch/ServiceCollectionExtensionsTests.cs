using DavidGroup.Core.DataAccess.ElasticSearch;

using Elastic.Clients.Elasticsearch;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DavidGroup.Core.DataAccessTests.ElasticSearch;

public class ServiceCollectionExtensionsTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static ServiceCollection CreateServices() => [];

    private static IConfiguration BuildConfiguration(string? elasticsearchConnectionString)
    {
        Dictionary<string, string?> data = new();

        if (elasticsearchConnectionString is not null)
            data["ConnectionStrings:Elasticsearch"] = elasticsearchConnectionString;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }

    private static ElasticsearchClient ResolveClient(IServiceCollection services)
    {
        return services.BuildServiceProvider().GetRequiredService<ElasticsearchClient>();
    }

    // -------------------------------------------------------------------------
    // Registration — explicit connection string
    // -------------------------------------------------------------------------

    [Fact]
    public void Registers_ElasticsearchClient_When_ConnectionString_Provided_Explicitly()
    {
        // Arrange
        ServiceCollection services = CreateServices();
        services.AddElasticsearchClient("http://localhost:9200");

        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        ElasticsearchClient? client = provider.GetService<ElasticsearchClient>();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Registers_Client_As_Singleton_When_ConnectionString_Provided_Explicitly()
    {
        // Arrange
        ServiceCollection services = CreateServices();
        services.AddElasticsearchClient("http://localhost:9200");

        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        ElasticsearchClient first = provider.GetRequiredService<ElasticsearchClient>();
        ElasticsearchClient second = provider.GetRequiredService<ElasticsearchClient>();

        // Assert
        Assert.Same(first, second);
    }

    // -------------------------------------------------------------------------
    // Registration — connection string from IConfiguration
    // -------------------------------------------------------------------------

    [Fact]
    public void Registers_ElasticsearchClient_When_ConnectionString_Resolved_From_Configuration()
    {
        // Arrange
        ServiceCollection services = CreateServices();
        services.AddSingleton(BuildConfiguration("http://localhost:9200"));

        services.AddElasticsearchClient();

        // Act
        ElasticsearchClient client = ResolveClient(services);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Registers_Client_As_Singleton_When_ConnectionString_Resolved_From_Configuration()
    {
        // Arrange
        ServiceCollection services = CreateServices();
        services.AddSingleton(BuildConfiguration("http://localhost:9200"));
        services.AddElasticsearchClient();

        ServiceProvider provider = services.BuildServiceProvider();

        // Act
        ElasticsearchClient first = provider.GetRequiredService<ElasticsearchClient>();
        ElasticsearchClient second = provider.GetRequiredService<ElasticsearchClient>();

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
        ServiceCollection services = CreateServices();
        services.AddSingleton(BuildConfiguration(elasticsearchConnectionString: null));
        services.AddElasticsearchClient();

        ServiceProvider provider = services.BuildServiceProvider();

        // Act, Assert

        // The factory is lazy — the exception surfaces on first resolution.
        Assert.Throws<InvalidOperationException>(
            provider.GetRequiredService<ElasticsearchClient>);
    }

    [Fact]
    public void Exception_Message_Indicates_Missing_ConnectionString()
    {
        // Arrange
        ServiceCollection services = CreateServices();
        services.AddSingleton(BuildConfiguration(elasticsearchConnectionString: null));
        services.AddElasticsearchClient();

        ServiceProvider provider = services.BuildServiceProvider();

        // Act, Assert
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            provider.GetRequiredService<ElasticsearchClient>);

        Assert.Contains("No Elasticsearch connection string found", ex.Message);
    }

    [Fact]
    public void Throws_When_IConfiguration_Is_Not_Registered_And_No_Explicit_ConnectionString()
    {
        // Arrange
        ServiceCollection services = CreateServices();
        services.AddElasticsearchClient();

        ServiceProvider provider = services.BuildServiceProvider();

        // Act, Assert
        Assert.ThrowsAny<Exception>(
            provider.GetRequiredService<ElasticsearchClient>);
    }

    // -------------------------------------------------------------------------
    // URI validation
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("not-a-uri")]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_When_ConnectionString_Is_Not_A_Valid_Uri(string badConnectionString)
    {
        // Arrange
        ServiceCollection services = CreateServices();
        services.AddElasticsearchClient(badConnectionString);

        ServiceProvider provider = services.BuildServiceProvider();

        // Act, Assert
        Assert.ThrowsAny<Exception>(
            provider.GetRequiredService<ElasticsearchClient>);
    }

    [Theory]
    [InlineData("http://localhost:9200")]
    [InlineData("https://elastic.example.com:9243")]
    [InlineData("http://10.0.0.5:9200")]
    public void Resolves_Client_Successfully_For_Valid_Uris(string connectionString)
    {
        // Arrange
        ServiceCollection services = CreateServices();
        services.AddElasticsearchClient(connectionString);

        // Act
        ElasticsearchClient client = ResolveClient(services);

        // Assert
        Assert.NotNull(client);
    }

    // -------------------------------------------------------------------------
    // Other
    // -------------------------------------------------------------------------

    [Fact]
    public void EnsureNoDuplicateRegistrations()
    {
        // Arrange, Act
        ServiceCollection services = CreateServices();
        services.AddElasticsearchClient("http://localhost:9200");
        services.AddElasticsearchClient("http://localhost:9200");

        List<string?> duplicates = services
            .GroupBy(descriptor => descriptor.ServiceType)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key.FullName)
            .ToList();

        // Assert
        Assert.Empty(duplicates);
    }
}
