using System.Reflection;
using System.Security.Authentication;

using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DavidGroup.Core.DataAccess.EventBus;

/// <summary>
/// Provides extension methods for registering and configuring MassTransit-based event bus transports
/// in an  <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers and configures MassTransit with RabbitMQ transport.
    /// </summary>
    /// <param name="configSectionKey">
    /// The configuration section key for binding <see cref="RabbitMqTransportOptions"/>.
    /// If not specified, the default section name <c>RabbitMqTransportOptions</c> is used.
    /// </param>
    /// <param name="assembly">
    /// The assembly containing message consumers.
    /// If not specified, the executing assembly is used.
    /// </param>
    /// <param name="services">The <see cref="IServiceCollection"/> used to register services.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <remarks>
    /// This method configures MassTransit with RabbitMQ, applies exponential retry policies,
    /// and enables SSL if specified in configuration.
    /// </remarks>
    public static IServiceCollection AddRabbitMq(this IServiceCollection services,
        string? configSectionKey = null,
        Assembly? assembly = null)
    {
        services.AddOptions<RabbitMqTransportOptions>()
            .BindConfiguration(configSectionKey ?? nameof(RabbitMqTransportOptions));

        assembly ??= Assembly.GetCallingAssembly();

        services.AddMassTransit(busConfiguration =>
        {
            busConfiguration.SetKebabCaseEndpointNameFormatter();

            busConfiguration.AddConsumers(assembly);

            busConfiguration.UsingRabbitMq((context, config) =>
            {
                RabbitMqTransportOptions options =
                    context.GetRequiredService<IOptions<RabbitMqTransportOptions>>().Value;

                config.Host(options.Host, options.Port, options.VHost, h =>
                {
                    h.Username(options.User);
                    h.Password(options.Pass);

                    if (options.UseSsl)
                        h.UseSsl(s => { s.Protocol = SslProtocols.Tls12; });
                });

                config.UseMessageRetry(retryConfig =>
                {
                    retryConfig.Exponential(
                        retryLimit: 5,
                        minInterval: TimeSpan.FromMilliseconds(100),
                        maxInterval: TimeSpan.FromSeconds(30),
                        intervalDelta: TimeSpan.FromSeconds(5)
                    );
                });

                config.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>
    /// <para>
    /// Registers MassTransit with RabbitMQ transport and configures
    /// an Entity Framework Core–backed transactional outbox pattern.
    /// </para>
    /// <para>
    /// This setup ensures reliable message delivery by persisting outgoing
    /// messages in the database and publishing them only after the
    /// corresponding database transaction is successfully committed.
    /// </para>
    /// </summary>
    /// <typeparam name="TDbContext">
    /// The <see cref="DbContext"/> used by the Entity Framework outbox.
    /// </typeparam>
    /// <param name="configureOutboxStore">
    /// Configures the database-specific locking behavior for the Entity Framework transactional outbox
    /// (e.g. <c>o => o.UseSqlServer()</c>, <c>o => o.UsePostgres()</c>, <c>o => o.UseMySql()</c>).
    /// Required so the outbox setup isn't tied to a single database provider.
    /// </param>
    /// <param name="configSectionKey">
    /// The configuration section key for binding <see cref="RabbitMqTransportOptions"/>.
    /// If not specified, the default section name <c>RabbitMqTransportOptions</c> is used.
    /// </param>
    /// <param name="assembly">
    /// The assembly containing message consumers.
    /// If not specified, the executing assembly is used.
    /// </param>
    /// <param name="services">The <see cref="IServiceCollection"/> used to register services.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <remarks>
    /// This method configures MassTransit with RabbitMQ, applies exponential retry policies,
    /// and enables SSL if specified in configuration.
    /// </remarks>
    public static IServiceCollection AddRabbitMqWithTransactionalOutbox<TDbContext>(
        this IServiceCollection services,
        Action<IEntityFrameworkOutboxConfigurator> configureOutboxStore,
        string? configSectionKey = null,
        Assembly? assembly = null)
        where TDbContext : DbContext
    {
        services.AddOptions<RabbitMqTransportOptions>()
            .BindConfiguration(configSectionKey ?? nameof(RabbitMqTransportOptions));

        assembly ??= Assembly.GetCallingAssembly();

        services.AddMassTransit(busConfiguration =>
        {
            busConfiguration.SetKebabCaseEndpointNameFormatter();

            busConfiguration.AddConsumers(assembly);

            busConfiguration.AddEntityFrameworkOutbox<TDbContext>(o =>
            {
                configureOutboxStore(o);
                o.UseBusOutbox();
            });

            busConfiguration.AddConfigureEndpointsCallback((context, _, cfg) =>
                cfg.UseEntityFrameworkOutbox<TDbContext>(context));

            busConfiguration.UsingRabbitMq((context, config) =>
            {
                RabbitMqTransportOptions options =
                    context.GetRequiredService<IOptions<RabbitMqTransportOptions>>().Value;

                config.Host(options.Host, options.Port, options.VHost, h =>
                {
                    h.Username(options.User);
                    h.Password(options.Pass);

                    if (options.UseSsl)
                        h.UseSsl(s => { s.Protocol = SslProtocols.Tls12; });
                });

                config.UseMessageRetry(retryConfig =>
                {
                    retryConfig.Exponential(
                        retryLimit: 5,
                        minInterval: TimeSpan.FromMilliseconds(100),
                        maxInterval: TimeSpan.FromSeconds(30),
                        intervalDelta: TimeSpan.FromSeconds(5)
                    );
                });

                config.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>
    /// Registers RabbitMQ messaging with a SQL Server-backed transactional outbox.
    /// </summary>
    /// <typeparam name="TDbContext">
    /// The type of the <see cref="DbContext"/> used to persist outbox messages.
    /// </typeparam>
    /// <param name="services">
    /// The service collection to add the RabbitMQ and transactional outbox services to.
    /// </param>
    /// <param name="configSectionKey">
    /// The configuration section key containing the RabbitMQ settings. If
    /// <see langword="null"/>, the default configuration section is used.
    /// </param>
    /// <param name="assembly">
    /// The assembly to scan for message consumers.
    /// If <see langword="null"/>, the executing assembly is scanned.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddRabbitMqWithSqlServerTransactionalOutbox<TDbContext>(
        this IServiceCollection services,
        string? configSectionKey = null,
        Assembly? assembly = null)
        where TDbContext : DbContext
    {
        return services.AddRabbitMqWithTransactionalOutbox<TDbContext>(
            o => o.UseSqlServer(), configSectionKey, assembly);
    }

    /// <summary>
    /// Registers and configures MassTransit with Azure Service Bus transport.
    /// </summary>
    /// <param name="configSectionKey">
    /// The configuration section key for binding <see cref="AzureServiceBusTransportOptions"/>.
    /// If not specified, the default section name <c>AzureServiceBusTransportOptions</c> is used.
    /// </param>
    /// <param name="assembly">
    /// The assembly containing message consumers.
    /// If not specified, the executing assembly is used.
    /// </param>
    /// <param name="services">The <see cref="IServiceCollection"/> used to register services.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <remarks>
    /// This method configures MassTransit with Azure Service Bus using the provided connection string.
    /// </remarks>
    public static IServiceCollection AddAzureServiceBus(this IServiceCollection services,
        string? configSectionKey = null,
        Assembly? assembly = null)
    {
        services.AddOptions<AzureServiceBusTransportOptions>()
            .BindConfiguration(configSectionKey ?? nameof(AzureServiceBusTransportOptions));

        assembly ??= Assembly.GetCallingAssembly();

        services.AddMassTransit(busConfiguration =>
        {
            busConfiguration.SetKebabCaseEndpointNameFormatter();

            busConfiguration.AddConsumers(assembly);


            busConfiguration.UsingAzureServiceBus((context, config) =>
            {
                AzureServiceBusTransportOptions options =
                    context.GetRequiredService<IOptions<AzureServiceBusTransportOptions>>().Value;

                config.Host(options.ConnectionString);

                config.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>
    /// <para>
    /// Registers MassTransit with Azure Service Bus transport and configures
    /// an Entity Framework Core–backed transactional outbox pattern.
    /// </para>
    /// <para>
    /// This setup ensures reliable message delivery by persisting outgoing
    /// messages in the database and publishing them only after the
    /// corresponding database transaction is successfully committed.
    /// </para>
    /// </summary>
    /// <param name="configureOutboxStore">
    /// Configures the database-specific locking behavior for the Entity Framework transactional outbox
    /// (e.g. <c>o => o.UseSqlServer()</c>, <c>o => o.UsePostgres()</c>, <c>o => o.UseMySql()</c>).
    /// Required so the outbox setup isn't tied to a single database provider.
    /// </param>
    /// <param name="configSectionKey">
    /// The configuration section key for binding <see cref="AzureServiceBusTransportOptions"/>.
    /// If not specified, the default section name <c>AzureServiceBusTransportOptions</c> is used.
    /// </param>
    /// <param name="assembly">
    /// The assembly containing message consumers.
    /// If not specified, the executing assembly is used.
    /// </param>
    /// <param name="services">The <see cref="IServiceCollection"/> used to register services.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <remarks>
    /// This method configures MassTransit with Azure Service Bus using the provided connection string.
    /// </remarks>
    public static IServiceCollection AddAzureServiceBusWithTransactionalOutbox<TDbContext>(
        this IServiceCollection services,
        Action<IEntityFrameworkOutboxConfigurator> configureOutboxStore,
        string? configSectionKey = null,
        Assembly? assembly = null)
        where TDbContext : DbContext
    {
        services.AddOptions<AzureServiceBusTransportOptions>()
            .BindConfiguration(configSectionKey ?? nameof(AzureServiceBusTransportOptions));

        assembly ??= Assembly.GetCallingAssembly();

        services.AddMassTransit(busConfiguration =>
        {
            busConfiguration.SetKebabCaseEndpointNameFormatter();

            busConfiguration.AddConsumers(assembly);

            busConfiguration.AddEntityFrameworkOutbox<TDbContext>(o =>
            {
                configureOutboxStore(o);
                o.UseBusOutbox();
            });

            busConfiguration.AddConfigureEndpointsCallback((context, _, cfg) =>
                cfg.UseEntityFrameworkOutbox<TDbContext>(context));

            busConfiguration.UsingAzureServiceBus((context, config) =>
            {
                AzureServiceBusTransportOptions options =
                    context.GetRequiredService<IOptions<AzureServiceBusTransportOptions>>().Value;

                config.Host(options.ConnectionString);

                config.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    /// <summary>
    /// Registers Azure Service Bus messaging with a SQL Server-backed transactional outbox.
    /// </summary>
    /// <typeparam name="TDbContext">
    /// The type of the <see cref="DbContext"/> used to persist outbox messages.
    /// </typeparam>
    /// <param name="services">
    /// The service collection to add the Azure Service Bus and transactional outbox services to.
    /// </param>
    /// <param name="configSectionKey">
    /// The configuration section key containing the Azure Service Bus settings. If
    /// <see langword="null"/>, the default configuration section is used.
    /// </param>
    /// <param name="assembly">
    /// The assembly to scan for message consumers.
    /// If <see langword="null"/>, the executing assembly is scanned.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddAzureServiceBusWithSqlServerTransactionalOutbox<TDbContext>(
        this IServiceCollection services,
        string? configSectionKey = null,
        Assembly? assembly = null)
        where TDbContext : DbContext
    {
        return services.AddAzureServiceBusWithTransactionalOutbox<TDbContext>(
            o => o.UseSqlServer(), configSectionKey, assembly);
    }

    /// <summary>
    /// Registers and configures MassTransit with an in-memory transport for local testing or development.
    /// </summary>
    /// <param name="assembly">
    /// The assembly containing message consumers.
    /// If not specified, the executing assembly is used.
    /// </param>
    /// <param name="services">The <see cref="IServiceCollection"/> used to register services.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <remarks>
    /// This method configures MassTransit to use an in-memory transport suitable for unit tests or simple local setups.
    /// </remarks>
    public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();

        services.AddMassTransit(busConfiguration =>
        {
            busConfiguration.SetKebabCaseEndpointNameFormatter();

            busConfiguration.AddConsumers(assembly);

            busConfiguration.UsingInMemory((context, config) => config.ConfigureEndpoints(context));
        });

        return services;
    }
}
