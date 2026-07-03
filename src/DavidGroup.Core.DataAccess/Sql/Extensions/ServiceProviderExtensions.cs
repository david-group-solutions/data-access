using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DavidGroup.Core.DataAccess.Sql.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IServiceProvider"/>.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Migrates the database for the specified <typeparamref name="TDbContext"/> type using DbContext.Database.Migrate().
    /// </summary>
    /// <typeparam name="TDbContext">The type of the <see cref="DbContext"/> to migrate.</typeparam>
    /// <param name="services">The <see cref="IServiceProvider"/> to resolve the <see cref="DbContext"/>.</param>
    /// <returns>The same <see cref="IServiceProvider"/> after migration.</returns>
    /// <remarks>
    /// Carefully consider before using this approach in production.
    /// Experience has shown that the simplicity of this deployment
    /// strategy is outweighed by the issues it creates. Consider
    /// generating SQL scripts from migrations instead.
    /// </remarks>
    public static IServiceProvider MigrateDatabase<TDbContext>(this IServiceProvider services)
        where TDbContext : DbContext
    {
        using IServiceScope scope = services.CreateScope();

        TDbContext db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        db.Database.Migrate();

        return services;
    }
}
