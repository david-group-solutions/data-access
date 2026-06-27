using Elastic.Clients.Elasticsearch;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DavidGroup.Core.DataAccess.ElasticSearch;

/// <summary>
/// Represents a health check for an Elasticsearch cluster.
/// </summary>
/// <remarks>
/// This implementation uses an <see cref="ElasticsearchClient"/> to ping the cluster and
/// report its health status. It can be registered with the ASP.NET Core health check system.
/// </remarks>
public class ElasticSearchHealthCheck(ElasticsearchClient client) : IHealthCheck
{
    /// <summary>
    /// Logic of health check.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see cref="HealthCheckResult"/></returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            PingResponse response = await client.PingAsync(cancellationToken: cancellationToken);

            return response.IsValidResponse
                ? HealthCheckResult.Healthy("Elasticsearch is healthy")
                : HealthCheckResult.Unhealthy("Elasticsearch ping failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Elasticsearch ping exception", ex);
        }
    }
}
