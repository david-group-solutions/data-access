using System.Linq.Expressions;
using System.Reflection;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace DavidGroup.Core.DataAccess.Sql.ValueGenerators;

/// <summary>
/// Generates sequential GUID-based values for strongly typed identifier types.
/// </summary>
/// <typeparam name="TKey">
/// The strongly typed identifier that exposes a public constructor accepting a single <see cref="Guid"/>.
/// </typeparam>
public sealed class SequentialStronglyTypedIdValueGenerator<TKey> : ValueGenerator<TKey>
{
    private static readonly SequentialGuidValueGenerator GuidGenerator = new();
    private static readonly Func<Guid, TKey> Factory = CreateFactory();

    /// <summary>
    /// Gets a value indicating whether the generated values are temporary.
    /// </summary>
    /// <value>
    /// <see langword="false"/>, because the generated values are permanent and can be persisted to the database.
    /// </value>
    public override bool GeneratesTemporaryValues => false;

    /// <summary>
    /// Generates the next sequential GUID-based value for the specified entity.
    /// </summary>
    /// <param name="entry">The entity entry for which the value is being generated.</param>
    /// <returns>A new instance of <typeparamref name="TKey"/> initialized with a sequential <see cref="Guid"/>.</returns>
    public override TKey Next(EntityEntry entry)
    {
        Guid guid = GuidGenerator.Next(entry);

        return Factory(guid);
    }

    /// <summary>
    /// Creates a compiled factory delegate that constructs instances of <typeparamref name="TKey"/>
    /// from a <see cref="Guid"/>.
    /// </summary>
    /// <returns>
    /// A delegate that creates a <typeparamref name="TKey"/> from a <see cref="Guid"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="TKey"/> does not expose a public constructor
    /// that accepts a single <see cref="Guid"/> parameter.
    /// </exception>
    private static Func<Guid, TKey> CreateFactory()
    {
        ConstructorInfo ctor = typeof(TKey).GetConstructor([typeof(Guid)])
                               ?? throw new InvalidOperationException(
                                   $"'{typeof(TKey)}' must expose a public constructor accepting a single Guid " +
                                   $"to be used with {nameof(SequentialStronglyTypedIdValueGenerator<>)}.");

        ParameterExpression param = Expression.Parameter(typeof(Guid), "value");
        NewExpression body = Expression.New(ctor, param);

        return Expression.Lambda<Func<Guid, TKey>>(body, param).Compile();
    }
}
