using DavidGroup.Core.DataAccess.Sql.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DavidGroup.Core.DataAccess.Sql.Extensions;

/// <summary>
/// Provides extension methods for configuring <see cref="ModelBuilder"/>.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures all entities implementing <see cref="ISqlServerSequentialId{TId}"/>
    /// to use <c>NEWSEQUENTIALID()</c> as the default value for their <c>Id</c> property.
    /// </summary>
    /// <param name="modelBuilder">
    /// The <see cref="ModelBuilder"/> used to configure the entity model.
    /// </param>
    /// <param name="context">
    /// The <see cref="DbContext"/> used to check the database provider.
    /// </param>
    /// <returns>
    /// The same <see cref="ModelBuilder"/> instance so that additional configuration can be chained.
    /// </returns>
    /// <remarks>
    /// This configuration applies only when using SQL Server, as
    /// <c>NEWSEQUENTIALID()</c> is a SQL Server-specific function.
    /// </remarks>
    public static ModelBuilder ApplySqlServerSequentialIds(this ModelBuilder modelBuilder, DbContext context)
    {
        if (!context.Database.IsSqlServer())
            return modelBuilder;

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!entityType.ClrType.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISqlServerSequentialId<>)))
            {
                continue;
            }

            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(ISqlServerSequentialId<>.Id))
                .HasDefaultValueSql("NEWSEQUENTIALID()");
        }

        return modelBuilder;
    }
}
