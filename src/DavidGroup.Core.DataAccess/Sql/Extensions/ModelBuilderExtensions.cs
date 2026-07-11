using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Sql.Entities;
using DavidGroup.Core.DataAccess.Sql.ValueGenerators;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace DavidGroup.Core.DataAccess.Sql.Extensions;

/// <summary>
/// Provides extension methods for configuring <see cref="ModelBuilder"/>.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures all entities implementing <see cref="ISequentialGuid"/>
    /// to use <see cref="SequentialGuidValueGenerator"/> for their <c>Id</c> property.
    /// </summary>
    /// <param name="modelBuilder">
    /// The <see cref="ModelBuilder"/> used to configure the entity model.
    /// </param>
    /// <returns>
    /// The same <see cref="ModelBuilder"/> instance so that additional configuration can be chained.
    /// </returns>
    public static ModelBuilder ApplySequentialGuids(this ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISequentialGuid).IsAssignableFrom(entityType.ClrType))
                continue;

            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(ISequentialGuid.Id))
                .HasValueGenerator<SequentialGuidValueGenerator>();
        }

        return modelBuilder;
    }

    /// <summary>
    /// Configures all entities implementing <see cref="IStronglyTypedSequentialId{TKey}"/>
    /// to use <see cref="SequentialStronglyTypedIdValueGenerator{Tkey}"/> for their <c>Id</c> property.
    /// </summary>
    /// <param name="modelBuilder">
    /// The <see cref="ModelBuilder"/> used to configure the entity model.
    /// </param>
    /// <returns>
    /// The same <see cref="ModelBuilder"/> instance so that additional configuration can be chained.
    /// </returns>
    public static ModelBuilder ApplyStronglyTypedSequentialIds(this ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            Type? sequentialIdInterface = entityType.ClrType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStronglyTypedSequentialId<>));

            if (sequentialIdInterface is null)
                continue;

            Type idType = sequentialIdInterface.GetGenericArguments()[0];
            Type valueGeneratorType = typeof(SequentialStronglyTypedIdValueGenerator<>).MakeGenericType(idType);

            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(IStronglyTypedSequentialId<>.Id))
                .HasValueGenerator(valueGeneratorType);
        }

        return modelBuilder;
    }

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
    /// <exception cref="NotSupportedException">
    /// Thrown when called from db context which is not using SQL Server provider.
    /// </exception>
    /// <remarks>
    /// This configuration applies only when using SQL Server, as
    /// <c>NEWSEQUENTIALID()</c> is a SQL Server-specific function.
    /// </remarks>
    public static ModelBuilder ApplySqlServerSequentialIds(this ModelBuilder modelBuilder, DbContext context)
    {
        if (!context.Database.IsSqlServer())
            throw new NotSupportedException("Only SQL Server is supported.");

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

    /// <summary>
    /// Applies a global query filter to all entity types that implement <see cref="ISoftDeletable"/>,
    /// automatically excluding entities marked as deleted from query results.
    /// </summary>
    /// <param name="modelBuilder">
    /// The <see cref="ModelBuilder"/> used to configure the entity model.
    /// </param>
    /// <returns>
    /// The same <see cref="ModelBuilder"/> instance, allowing additional configuration to be chained.
    /// </returns>
    /// <remarks>
    /// This method configures a global query filter equivalent to
    /// <c>entity => !entity.IsDeleted</c> for every entity type implementing
    /// <see cref="ISoftDeletable"/>. The filter is applied automatically to all LINQ
    /// queries unless explicitly disabled using <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.IgnoreQueryFilters{TEntity}(IQueryable{TEntity})"/>.
    /// </remarks>
    public static ModelBuilder ApplyQueryFiltersForSoftDeletedEntities(this ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                continue;

            ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "e");

            MemberExpression isDeleted = Expression.Property(
                Expression.Convert(parameter, typeof(ISoftDeletable)),
                nameof(ISoftDeletable.IsDeleted));

            UnaryExpression body = Expression.Not(isDeleted);

            LambdaExpression filter = Expression.Lambda(body, parameter);

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(filter);
        }

        return modelBuilder;
    }
}
