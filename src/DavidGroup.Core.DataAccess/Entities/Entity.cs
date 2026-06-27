using System.ComponentModel.DataAnnotations;

namespace DavidGroup.Core.DataAccess.Entities;

/// <summary>
/// Serves as the base class for all entities that use a strongly-typed primary key.
/// </summary>
/// <typeparam name="TKey">The type of the primary key (must be a value type).</typeparam>
public abstract class Entity<TKey> : IEntity<TKey>
    where TKey : struct
{
    /// <summary>
    /// Gets or sets the unique identifier for this entity.
    /// </summary>
    [Key]
    public TKey Id { get; set; }
}
