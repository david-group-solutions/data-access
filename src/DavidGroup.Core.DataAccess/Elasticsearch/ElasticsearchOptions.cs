using System.ComponentModel.DataAnnotations;

namespace DavidGroup.Core.DataAccess.Elasticsearch;

/// <summary>
/// Provides configuration options for connecting to an Elasticsearch cluster.
/// </summary>
public sealed class ElasticsearchOptions
{
    /// <summary>
    /// Gets the configuration section name used to bind <see cref="ElasticsearchOptions"/>.
    /// </summary>
    public const string SectionName = "Elasticsearch";

    /// <summary>
    /// Gets the Elasticsearch endpoint URIs.
    /// </summary>
    [Required]
    public List<string> Endpoints { get; init; } = [];

    /// <summary>
    /// Gets the SHA-256 certificate fingerprint used to validate the Elasticsearch server certificate.
    /// </summary>
    public string? CertificateFingerprint { get; init; }

    /// <summary>
    /// Gets the API key used to authenticate with Elasticsearch.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// Gets the maximum amount of time allowed for an Elasticsearch request.
    /// </summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets the maximum number of retries for a failed Elasticsearch request.
    /// </summary>
    public int MaximumRetries { get; init; } = 5;
}
