using System.Text.Json;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Serialization;
using Elastic.Transport;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace DavidGroup.Core.DataAccess.Elasticsearch;

/// <summary>
/// Extensions of IServiceCollection associated with ElasticSearch
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers an Elasticsearch client (<see cref="ElasticsearchClient"/>) in the DI container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration containing <see cref="ElasticsearchOptions"/>.</param>
    /// <exception cref="ArgumentException">Thrown if the Elasticsearch connection string is missing in configuration.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the Elasticsearch connection string cannot be resolved.</exception>
    public static IServiceCollection AddElasticsearchClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ElasticsearchOptions>()
            .Bind(configuration.GetSection(ElasticsearchOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => options.Endpoints.Count > 0,
                "At least one Elasticsearch endpoint must be configured."
            )
            .ValidateOnStart();

        services.TryAddSingleton<ElasticsearchClient>(sp =>
        {
            ElasticsearchOptions options = sp.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;

            Uri[] nodes = options.Endpoints
                .Select(endpoint => new Uri(endpoint))
                .ToArray();

            StaticNodePool nodePool = new(nodes);

            ElasticsearchClientSettings settings = new ElasticsearchClientSettings(
                    nodePool,
                    sourceSerializer: (_, settings) => new DefaultSourceSerializer(settings, jsonOptions =>
                    {
                        jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                        jsonOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
                    })
                )
                .DefaultFieldNameInferrer(p => JsonNamingPolicy.SnakeCaseLower.ConvertName(p))
                .RequestTimeout(options.RequestTimeout)
                .MaximumRetries(options.MaximumRetries);

            if (!string.IsNullOrWhiteSpace(options.CertificateFingerprint))
            {
                settings = settings.CertificateFingerprint(options.CertificateFingerprint);
            }

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                settings = settings.Authentication(new ApiKey(options.ApiKey));
            }

            return new ElasticsearchClient(settings);
        });

        return services;
    }
}
