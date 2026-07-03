using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;

using StackExchange.Redis;

namespace DavidGroup.Core.DataAccess.Cache;

/// <summary>
/// Provides extension methods for registering caching related components in an  <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a Redis connection (<see cref="IConnectionMultiplexer"/>) in the DI container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="connectionString">Optional Redis connection string. If null, will use "Redis" from ConnectionStings section.</param>
    /// <returns>The configured <see cref="IServiceCollection"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the Redis connection string cannot be resolved.</exception>
    public static IServiceCollection AddRedis(this IServiceCollection services, string? connectionString = null)
    {
        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            connectionString ??= sp.GetRequiredService<IConfiguration>().GetConnectionString("Redis")
                                 ?? throw new InvalidOperationException("No redis connection string found.");

            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(connectionString);

            return redis;
        });

        return services;
    }

    /// <summary>
    /// Registers a distributed cache implementation based on the current host environment.
    /// In development, an in-memory cache is used, while in non-development environments
    /// a Redis-backed distributed cache is configured.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="environment">The current host environment used to determine the cache implementation.</param>
    /// <param name="configuration">The application configuration used to resolve the Redis connection string.</param>
    /// <param name="connectionString">Optional Redis connection string. If null, will use "Redis" from ConnectionStings section.</param>
    /// <returns>The configured <see cref="IServiceCollection"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the Redis connection string cannot be resolved.</exception>
    public static IServiceCollection AddDistributedCache(this IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration configuration,
        string? connectionString = null)
    {
        if (environment.IsDevelopment())
            services.AddDistributedMemoryCache();
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                connectionString ??= configuration.GetConnectionString("Redis")
                                     ?? throw new InvalidOperationException("No redis connection string found.");

                options.Configuration = connectionString;
            });
        }

        return services;
    }

    /// <summary>
    /// Registers a RedLock distributed lock factory (<see cref="IDistributedLockFactory"/>) in the DI container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration used to obtain Redis endpoints for RedLock.</param>
    /// <returns>The configured <see cref="IServiceCollection"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no RedLock endpoints are configured.</exception>
    /// <example>
    /// The following example demonstrates how to register RedLock with multiple Redis endpoints in an ASP.NET Core application:
    /// <code>
    /// // appsettings.json:
    /// {
    ///   "RedLock": {
    ///     "Endpoints": [
    ///       "redis1.example.com:6379",
    ///       "redis2.example.com:6379",
    ///       "redis3.example.com:6379"
    ///     ]
    ///   }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddRedLock(this IServiceCollection services, IConfiguration configuration)
    {
        List<string> connectionStrings = configuration.GetSection("RedLock:Endpoints").Get<List<string>>()
                                         ?? throw new InvalidOperationException("RedLock endpoints are not configured.");

        List<RedLockMultiplexer> multiplexers = connectionStrings
            .Select(connectionString => (RedLockMultiplexer)ConnectionMultiplexer.Connect(connectionString))
            .ToList();

        RedLockFactory? redLockFactory = RedLockFactory.Create(multiplexers);

        services.TryAddSingleton<IDistributedLockFactory>(redLockFactory);

        return services;
    }
}
