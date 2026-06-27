namespace DavidGroup.Core.DataAccess.Entities;

/// <summary>
/// Marks an entity that supports soft deletion.
/// </summary>
/// <remarks>
/// Soft deletion allows marking an entity as deleted without physically removing it
/// from the data store. This enables scenarios such as data recovery,
/// historical tracking, or audit logging.
/// </remarks>
public interface ISoftDeletable
{
    /// <summary>
    /// Indicates if entity is deleted
    /// </summary>
    bool IsDeleted { get; set; }
}
