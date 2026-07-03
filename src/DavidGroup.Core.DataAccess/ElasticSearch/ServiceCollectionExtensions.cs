using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DavidGroup.Core.DataAccess.ElasticSearch;

/// <summary>
/// Extensions of IServiceCollection associated with ElasticSearch
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers an Elasticsearch client (<see cref="ElasticsearchClient"/>) in the DI container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="connectionString">Optional Elasticsearch connection string. If null, will use "Elasticsearch" from ConnectionStings section.</param>
    /// <exception cref="ArgumentException">Thrown if the Elasticsearch connection string is missing in configuration.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the Elasticsearch connection string cannot be resolved.</exception>
    public static void AddElasticsearchClient(this IServiceCollection services, string? connectionString = null)
    {
        services.TryAddSingleton<ElasticsearchClient>(sp =>
        {
            connectionString ??= sp.GetRequiredService<IConfiguration>().GetConnectionString("Elasticsearch")
                                 ?? throw new InvalidOperationException("No Elasticsearch connection string found.");

            SingleNodePool connectionPool = new(new Uri(connectionString));

            ElasticsearchClientSettings connectionSettings = new ElasticsearchClientSettings(connectionPool)
                .RequestTimeout(TimeSpan.FromSeconds(10))
                .MaximumRetries(5);

            return new ElasticsearchClient(connectionSettings);
        });
    }
}
