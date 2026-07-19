using System.ComponentModel.DataAnnotations;

namespace DavidGroup.Core.DataAccess.Sql.Entities;

/// <summary>
/// Serves as the base class for all entities that use a strongly-typed primary key.
/// </summary>
/// <typeparam name="TKey">The type of the primary key (must be a value type).</typeparam>
public abstract class Entity<TKey> : IEntity<TKey>
    where TKey : struct
{
    /// <inheritdoc />
    [Key]
    public TKey Id { get; set; }
}
