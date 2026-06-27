namespace DavidGroup.Core.DataAccess.Sql.Entities;

/// <summary>
/// Marks an entity that tracks its creation and last modification timestamps in UTC.
/// </summary>
/// <remarks>
/// This interface is typically implemented by entities that require auditing
/// or time-based tracking. The timestamps are stored in Coordinated Universal Time (UTC)
/// to ensure consistency across distributed systems and time zones.
/// </remarks>
public interface ITimedEntity
{
    /// <summary>
    /// Gets or sets the UTC timestamp when the entity was initially created.
    /// </summary>
    DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the entity was last modified.
    /// </summary>
    /// <remarks>
    /// When entity is newly created it has the same value as <see cref="CreatedAtUtc"/>.
    /// </remarks>
    DateTime ModifiedAtUtc { get; set; }
}
