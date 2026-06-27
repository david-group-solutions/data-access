namespace DavidGroup.Core.DataAccess.Sql.Entities;

/// <summary>
/// Represents an entity with a strongly-typed primary key.
/// </summary>
/// <typeparam name="TKey">The type of the primary key.</typeparam>
public interface IEntity<TKey>
{
    /// <summary>
    /// Gets or sets the unique identifier for this entity.
    /// </summary>
    TKey Id { get; set; }
}
