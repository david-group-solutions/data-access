using System.Linq.Expressions;
using System.Text;

using DavidGroup.Core.DataAccess.Elasticsearch;

using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DavidGroup.Core.DataAccess.Tests.Elasticsearch;

public class ServiceCollectionExtensionsTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private const string ValidEndpoint = "http://localhost:9200";

    private sealed class TestDocument
    {
        public string FirstName { get; init; } = string.Empty;
    }

    private static IConfiguration BuildConfiguration(
        string[] endpoints,
        string? apiKey = null,
        string? certificateFingerprint = null,
        TimeSpan? requestTimeout = null,
        int? maximumRetries = null)
    {
        Dictionary<string, string?> values = new();

        for (int i = 0; i < endpoints.Length; i++)
            values[$"{ElasticsearchOptions.SectionName}:Endpoints:{i}"] = endpoints[i];

        if (apiKey is not null)
            values[$"{ElasticsearchOptions.SectionName}:ApiKey"] = apiKey;

        if (certificateFingerprint is not null)
            values[$"{ElasticsearchOptions.SectionName}:CertificateFingerprint"] = certificateFingerprint;

        if (requestTimeout is not null)
            values[$"{ElasticsearchOptions.SectionName}:RequestTimeout"] = requestTimeout.Value.ToString("c");

        if (maximumRetries is not null)
            values[$"{ElasticsearchOptions.SectionName}:MaximumRetries"] = maximumRetries.Value.ToString();

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static ServiceProvider BuildProvider(IConfiguration configuration)
    {
        ServiceCollection services = new();

        services.AddElasticsearchClient(configuration);

        return services.BuildServiceProvider();
    }

    public class AddElasticsearchClient
    {
        [Fact]
        public void ReturnsSameServiceCollection()
        {
            // Arrange
            ServiceCollection services = new();

            IConfiguration configuration = BuildConfiguration([ValidEndpoint]);

            // Act
            IServiceCollection result = services.AddElasticsearchClient(configuration);

            // Assert
            Assert.Same(services, result);
        }

        [Fact]
        public void RegistersClientAsSingleton()
        {
            // Arrange
            ServiceCollection services = new();

            IConfiguration configuration = BuildConfiguration([ValidEndpoint]);

            // Act
            services.AddElasticsearchClient(configuration);

            // Assert
            ServiceDescriptor descriptor = Assert.Single(
                services,
                d => d.ServiceType == typeof(ElasticsearchClient)
            );

            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        }

        [Fact]
        public void CalledTwice_RegistersClientOnlyOnce()
        {
            // Arrange
            ServiceCollection services = new();

            IConfiguration configuration = BuildConfiguration([ValidEndpoint]);

            // Act
            services.AddElasticsearchClient(configuration);
            services.AddElasticsearchClient(configuration);

            // Assert
            int count = services.Count(d => d.ServiceType == typeof(ElasticsearchClient));
            Assert.Equal(1, count);
        }

        [Fact]
        public void ClientAlreadyRegistered_DoesNotReplaceExistingRegistration()
        {
            // Arrange
            ElasticsearchClient existingClient = new(new Uri("http://existing:9200"));
            ServiceCollection services = new();
            services.AddSingleton(existingClient);

            IConfiguration configuration = BuildConfiguration([ValidEndpoint]);

            // Act
            services.AddElasticsearchClient(configuration);

            using ServiceProvider provider = services.BuildServiceProvider();
            ElasticsearchClient resolved = provider.GetRequiredService<ElasticsearchClient>();

            // Assert
            Assert.Same(existingClient, resolved);
        }

        [Fact]
        public void ValidConfiguration_BindsOptions()
        {
            // Arrange
            IConfiguration configuration = BuildConfiguration(
                ["http://node1:9200", "http://node2:9200"],
                apiKey: "my-api-key",
                certificateFingerprint: "AB:CD:EF",
                requestTimeout: TimeSpan.FromSeconds(15),
                maximumRetries: 7
            );

            // Act
            using ServiceProvider provider = BuildProvider(configuration);

            ElasticsearchOptions options = provider.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;

            // Assert
            Assert.Equal(2, options.Endpoints.Count);
            Assert.Contains("http://node1:9200", options.Endpoints);
            Assert.Contains("http://node2:9200", options.Endpoints);
            Assert.Equal("my-api-key", options.ApiKey);
            Assert.Equal("AB:CD:EF", options.CertificateFingerprint);
            Assert.Equal(TimeSpan.FromSeconds(15), options.RequestTimeout);
            Assert.Equal(7, options.MaximumRetries);
        }

        [Fact]
        public void NoEndpoints_ThrowsOptionsValidationExceptionWhenResolvingOptions()
        {
            // Arrange
            IConfiguration configuration = BuildConfiguration([]);

            using ServiceProvider provider = BuildProvider(configuration);

            // Act
            IOptions<ElasticsearchOptions> options = provider.GetRequiredService<IOptions<ElasticsearchOptions>>();

            // Assert
            Assert.Throws<OptionsValidationException>(() => options.Value);
        }

        [Fact]
        public void NoEndpoints_ThrowsOptionsValidationExceptionWhenResolvingClient()
        {
            // Arrange
            IConfiguration configuration = BuildConfiguration([]);

            using ServiceProvider provider = BuildProvider(configuration);

            // Act & Assert
            Assert.Throws<OptionsValidationException>(provider.GetRequiredService<ElasticsearchClient>);
        }

        [Fact]
        public void NoEndpoints_FailsOnStartupValidation()
        {
            // Arrange
            IConfiguration configuration = BuildConfiguration([]);

            using ServiceProvider provider = BuildProvider(configuration);

            IStartupValidator startupValidator = provider.GetRequiredService<IStartupValidator>();

            // Act & Assert
            Assert.Throws<OptionsValidationException>(startupValidator.Validate);
        }

        [Fact]
        public void ValidConfiguration_PassesStartupValidation()
        {
            // Arrange
            IConfiguration configuration = BuildConfiguration([ValidEndpoint]);

            using ServiceProvider provider = BuildProvider(configuration);

            IStartupValidator startupValidator = provider.GetRequiredService<IStartupValidator>();

            // Act
            Exception? exception = Record.Exception(startupValidator.Validate);

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void ValidConfiguration_ResolvesSameClientInstance()
        {
            // Arrange
            IConfiguration configuration = BuildConfiguration([ValidEndpoint]);

            using ServiceProvider provider = BuildProvider(configuration);

            // Act
            ElasticsearchClient first = provider.GetRequiredService<ElasticsearchClient>();
            ElasticsearchClient second = provider.GetRequiredService<ElasticsearchClient>();

            // Assert
            Assert.NotNull(first);
            Assert.Same(first, second);
        }

        [Fact]
        public void MultipleEndpoints_ConfiguresAllNodes()
        {
            // Arrange
            string[] endpoints = ["http://node1:9200", "http://node2:9200", "http://node3:9200"];

            IConfiguration configuration = BuildConfiguration(endpoints);

            using ServiceProvider provider = BuildProvider(configuration);

            // Act
            ElasticsearchClient client = provider.GetRequiredService<ElasticsearchClient>();

            Uri[] actualNodes = client.ElasticsearchClientSettings.NodePool.Nodes
                .Select(node => node.Uri)
                .ToArray();

            // Assert
            Uri[] expectedNodes = endpoints.Select(e => new Uri(e)).ToArray();

            Assert.Equal(
                expectedNodes.OrderBy(u => u.AbsoluteUri),
                actualNodes.OrderBy(u => u.AbsoluteUri));
        }

        [Fact]
        public void ValidConfiguration_UsesStaticNodePool()
        {
            // Arrange
            IConfiguration configuration = BuildConfiguration([ValidEndpoint]);

            using ServiceProvider provider = BuildProvider(configuration);

            // Act
            ElasticsearchClient client = provider.GetRequiredService<ElasticsearchClient>();

            // Assert
            Assert.IsType<StaticNodePool>(client.ElasticsearchClientSettings.NodePool);
        }

        [Fact]
        public void RequestTimeoutConfigured_UsesConfiguredRequestTimeout()
        {
            // Arrange
            TimeSpan expectedTimeout = TimeSpan.FromSeconds(42);

            IConfiguration configuration = BuildConfiguration(
                [ValidEndpoint],
                requestTimeout: expectedTimeout
            );

            using ServiceProvider provider = BuildProvider(configuration);

            // Act
            ElasticsearchClient client = provider.GetRequiredService<ElasticsearchClient>();

            // Assert
            Assert.Equal(expectedTimeout, client.ElasticsearchClientSettings.RequestTimeout);
        }

        [Fact]
        public void RequestTimeoutNotConfigured_UsesDefaultRequestTimeoutFromOptions()
        {
            // Arrange
            TimeSpan expectedTimeout = new ElasticsearchOptions().RequestTimeout;

            IConfiguration configuration = BuildConfiguration([ValidEndpoint]);

            using ServiceProvider provider = BuildProvider(configuration);

            // Act
            ElasticsearchClient client = provider.GetRequiredService<ElasticsearchClient>();

            // Assert
            Assert.Equal(expectedTimeout, client.ElasticsearchClientSettings.RequestTimeout);
        }

        [Fact]
        public void MaximumRetriesConfigured_UsesConfiguredMaximumRetries()
        {
            // Arrange
            const int expectedRetries = 7;

            IConfiguration configuration = BuildConfiguration(
                [ValidEndpoint],
                maximumRetries: expectedRetries
            );

            using ServiceProvider provider = BuildProvider(configuration);

            // Act
            ElasticsearchClient client = provider.GetRequiredService<ElasticsearchClient>();

            // Assert
            Assert.Equal(expectedRetries, client.ElasticsearchClientSettings.MaxRetries);
        }

        [Fact]
        public void MaximumRetriesNotConfigured_UsesDefaultMaximumRetriesFromOptions()
        {
            // Arrange
            int expectedRetries = new ElasticsearchOptions().MaximumRetries;

            IConfiguration configuration = BuildConfiguration([ValidEndpoint]);

            using ServiceProvider provider = BuildProvider(configuration);

            // Act
            ElasticsearchClient client = provider.GetRequiredService<ElasticsearchClient>();

            // Assert
            Assert.Equal(expectedRetries, client.ElasticsearchClientSettings.MaxRetries);
        }

        [Fact]
        public void ValidConfiguration_SerializesSourceWithSnakeCase()
        {
            // Arrange
            IConfiguration configuration = BuildConfiguration([ValidEndpoint]);

            using ServiceProvider provider = BuildProvider(configuration);

            ElasticsearchClient client = provider.GetRequiredService<ElasticsearchClient>();

            TestDocument document = new() { FirstName = "David" };

            using MemoryStream stream = new();

            // Act
            client.ElasticsearchClientSettings.SourceSerializer.Serialize(document, stream);

            string json = Encoding.UTF8.GetString(stream.ToArray());

            // Assert
            Assert.Contains("\"first_name\"", json);
            Assert.DoesNotContain("FirstName", json);
        }

        [Fact]
        public void ValidConfiguration_InfersFieldNamesInSnakeCase()
        {
            // Arrange
            IConfiguration configuration = BuildConfiguration([ValidEndpoint]);

            using ServiceProvider provider = BuildProvider(configuration);

            ElasticsearchClient client = provider.GetRequiredService<ElasticsearchClient>();

            Expression<Func<TestDocument, object?>> expression = d => d.FirstName;
            Field field = new(expression);

            // Act
            string fieldName = client.Infer.Field(field);

            // Assert
            Assert.Equal("first_name", fieldName);
        }

        [Fact]
        public void FingerprintProvided_SetsCertificateFingerprint()
        {
            // Arrange
            IConfiguration configuration = BuildConfiguration(
                [ValidEndpoint],
                certificateFingerprint: "AB:CD:EF"
            );

            using ServiceProvider provider = BuildProvider(configuration);

            // Act
            ElasticsearchClient client = provider.GetRequiredService<ElasticsearchClient>();

            // Assert
            Assert.Equal("AB:CD:EF", client.ElasticsearchClientSettings.CertificateFingerprint);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void FingerprintMissingOrWhitespace_DoesNotSetCertificateFingerprint(string? fingerprint)
        {
            // Arrange
            IConfiguration configuration = BuildConfiguration(
                [ValidEndpoint],
                certificateFingerprint: fingerprint
            );

            using ServiceProvider provider = BuildProvider(configuration);

            // Act
            ElasticsearchClient client = provider.GetRequiredService<ElasticsearchClient>();

            // Assert
            Assert.Null(client.ElasticsearchClientSettings.CertificateFingerprint);
        }

        [Fact]
        public void ApiKeyProvided_SetsApiKeyAuthentication()
        {
            // Arrange
            IConfiguration configuration = BuildConfiguration(
                [ValidEndpoint],
                apiKey: "my-api-key"
            );

            using ServiceProvider provider = BuildProvider(configuration);

            // Act
            ElasticsearchClient client = provider.GetRequiredService<ElasticsearchClient>();

            // Assert
            Assert.IsType<ApiKey>(client.ElasticsearchClientSettings.Authentication);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ApiKeyMissingOrWhitespace_DoesNotSetAuthentication(string? apiKey)
        {
            // Arrange
            IConfiguration configuration = BuildConfiguration(
                [ValidEndpoint],
                apiKey: apiKey
            );

            using ServiceProvider provider = BuildProvider(configuration);

            // Act
            ElasticsearchClient client = provider.GetRequiredService<ElasticsearchClient>();

            // Assert
            Assert.Null(client.ElasticsearchClientSettings.Authentication);
        }
    }
}
