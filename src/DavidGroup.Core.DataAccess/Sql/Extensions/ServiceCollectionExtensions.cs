using System.Data.Common;
using System.Reflection;

using DavidGroup.Core.DataAccess.Sql.Interceptors;
using DavidGroup.Core.DataAccess.Sql.Repositories;
using DavidGroup.Core.DataAccess.Sql.Services;
using DavidGroup.Core.DataAccess.Sql.UnitOfWork.ADO.NET;
using DavidGroup.Core.DataAccess.Sql.UnitOfWork.EFCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Scrutor;

namespace DavidGroup.Core.DataAccess.Sql.Extensions;

/// <summary>
/// Provides extension methods for registering Entity Framework Core, ADO.NET,
/// repositories, services, and related data access components in an
/// <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds and configures an Entity Framework Core <typeparamref name="TDbContext"/> to the service collection.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the EF Core <see cref="DbContext"/>.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureProvider">
    /// Delegate that configures the EF Core provider (e.g. <c>UseSqlServer</c>, <c>UseNpgsql</c>, <c>UseSqlite</c>).
    /// Receives the options builder, the resolved connection string, and the migrations assembly name.
    /// </param>
    /// <param name="connectionString">
    /// Optional database connection string. If null, will use "DefaultConnection" from configuration.
    /// </param>
    /// <param name="assemblyName">
    /// Optional assembly name for EF Core migrations. Defaults to the executing assembly name.
    /// </param>
    /// <returns>The configured <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddDatabase<TDbContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder, string, string> configureProvider,
        string? connectionString = null,
        string? assemblyName = null)
        where TDbContext : DbContext
    {
        assemblyName ??= Assembly.GetExecutingAssembly().GetName().Name!;

        services.AddDbContext<TDbContext>((sp, options) =>
        {
            string resolvedConnectionString =
                connectionString
                ?? sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "No connection string was provided and 'DefaultConnection' was not found in configuration.");

            configureProvider(options, resolvedConnectionString, assemblyName);

            options.AddInterceptors(
                new TimedEntitiesInterceptor(),
                new SoftDeleteInterceptor()
            );
        });

        return services;
    }

    /// <summary>
    /// Registers a SQL Server database context and configures it to use the SQL Server provider.
    /// </summary>
    /// <typeparam name="TDbContext">
    /// The type of the <see cref="DbContext"/> to register.
    /// </typeparam>
    /// <param name="services">
    /// The service collection to add the database context to.
    /// </param>
    /// <param name="connectionString">
    /// The SQL Server connection string. If <see langword="null"/>, the connection string is resolved
    /// using the default behavior of <c>AddDatabase</c>.
    /// </param>
    /// <param name="assemblyName">
    /// The name of the assembly containing Entity Framework Core migrations. If
    /// <see langword="null"/>, the default behavior of <c>AddDatabase</c> is used.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddSqlServerDatabase<TDbContext>(
        this IServiceCollection services,
        string? connectionString = null,
        string? assemblyName = null)
        where TDbContext : DbContext
    {
        return services.AddDatabase<TDbContext>(
            (options, connStr, asmName) => options.UseSqlServer(connStr, x => x.MigrationsAssembly(asmName)),
            connectionString,
            assemblyName
        );
    }

    /// <summary>
    /// Registers an Entity Framework Core–based Unit of Work (<see cref="IEfUnitOfWork{TContext}"/>)
    /// in the DI container.
    /// </summary>
    /// <typeparam name="TDbContext">
    /// The type of the EF Core <see cref="DbContext"/> used by the unit of work.
    /// </typeparam>
    /// <param name="services">
    /// The service collection to add the repository registrations to.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddEfUnitOfWork<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.TryAddScoped<IEfUnitOfWork<TDbContext>>(sp
            => new EfUnitOfWork<TDbContext>(sp.GetRequiredService<TDbContext>()));

        return services;
    }

    /// <summary>
    /// Registers an ADO.NET–based Unit of Work (<see cref="IAdoNetUnitOfWork"/>) in the DI container.
    /// </summary>
    /// <param name="services">
    /// The service collection to add the repository registrations to.
    /// </param>
    /// <param name="connectionFactory">
    /// Function which returns <see cref="DbConnection"/> in order to use different databases with their own connectors.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if the connection string cannot be resolved.</exception>
    public static IServiceCollection AddAdoUnitOfWork(this IServiceCollection services, Func<DbConnection> connectionFactory)
    {
        services.TryAddScoped<IAdoNetUnitOfWork>(_
            => new AdoNetUnitOfWork(connectionFactory));

        return services;
    }

    /// <summary>
    /// Automatically registers all repository implementations found in the specified assembly
    /// derived from <see cref="IBaseRepository{TEntity,TKey}"/> or <see cref="IBaseAggregationRepository{TEntity}"/>.
    /// </summary>
    /// <param name="services">
    /// The service collection to add the repository registrations to.
    /// </param>
    /// <param name="assembly">
    /// The assembly to scan for repository implementations. If <see langword="null"/>,
    /// the executing assembly is scanned.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddAllRepositoriesAuto(this IServiceCollection services,
        Assembly? assembly = null)
    {
        return services.AddImplementations(
            assembly ?? Assembly.GetExecutingAssembly(),
            typeof(IBaseRepository<,>),
            typeof(IBaseAggregationRepository<>)
        );
    }

    /// <summary>
    /// Automatically registers all repository implementations found in the assembly
    /// containing <typeparamref name="TAssembly"/> and derived from <see cref="IBaseRepository{TEntity,TKey}"/>
    /// or <see cref="IBaseAggregationRepository{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TAssembly">
    /// A type whose assembly will be scanned for repository implementations.
    /// </typeparam>
    /// <param name="services">
    /// The service collection to add the repository registrations to.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddAllRepositoriesAuto<TAssembly>(this IServiceCollection services)
    {
        return services.AddAllRepositoriesAuto(typeof(TAssembly).Assembly);
    }

    /// <summary>
    /// Automatically registers all service implementations found in the specified assembly
    /// derived from <see cref="IBaseService{TEntity,TKey,TCreateModel,TUpdateModel,TReadDto}"/> or
    /// <see cref="IBaseReadonlyService{TEntity,TKey,TReadDto}"/>.
    /// </summary>
    /// <param name="services">
    /// The service collection to add the service registrations to.
    /// </param>
    /// <param name="assembly">
    /// The assembly to scan for service implementations. If <see langword="null"/>,
    /// the executing assembly is scanned.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddAllServicesAuto(this IServiceCollection services,
        Assembly? assembly = null)
    {
        return services.AddImplementations(
            assembly ?? Assembly.GetExecutingAssembly(),
            typeof(IBaseReadonlyService<,,>),
            typeof(IBaseService<,,,,>)
        );
    }

    /// <summary>
    /// Automatically registers all service implementations found in the assembly derived from
    /// <see cref="IBaseService{TEntity,TKey,TCreateModel,TUpdateModel,TReadDto}"/> or
    /// <see cref="IBaseReadonlyService{TEntity,TKey,TReadDto}"/>.
    /// containing <typeparamref name="TAssembly"/>. and
    /// </summary>
    /// <typeparam name="TAssembly">
    /// A type whose assembly will be scanned for service implementations.
    /// </typeparam>
    /// <param name="services">
    /// The service collection to add the service registrations to.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddAllServicesAuto<TAssembly>(this IServiceCollection services)
    {
        return services.AddAllServicesAuto(typeof(TAssembly).Assembly);
    }

    /// <summary>
    /// Registers all implementations of the specified interface types found in the given assembly.
    /// Implementations are registered as their implemented interfaces with a scoped lifetime.
    /// </summary>
    /// <param name="services">
    /// The service collection to add the registrations to.
    /// </param>
    /// <param name="assembly">
    /// The assembly to scan for implementations.
    /// </param>
    /// <param name="interfaceTypes">
    /// The open generic interface types used to identify implementations.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.
    /// </returns>
    private static IServiceCollection AddImplementations(
        this IServiceCollection services,
        Assembly assembly,
        params Type[] interfaceTypes)
    {
        services.Scan(scan =>
        {
            IImplementationTypeSelector scanner = scan.FromAssemblies(assembly);

            foreach (Type interfaceType in interfaceTypes)
            {
                scanner = scanner
                    .AddClasses(classes => classes.AssignableTo(interfaceType))
                    .AsImplementedInterfaces()
                    .WithScopedLifetime();
            }
        });

        return services;
    }
}
