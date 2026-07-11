using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace DavidGroup.Core.DataAccess.Sql.Entities;

/// <summary>
/// Marker interface for entities which must have sequential GUID
/// using <see cref="SequentialGuidValueGenerator"/>.
/// </summary>
public interface ISequentialGuid
{
    /// <summary>
    /// The PK which has generated value using <see cref="SequentialGuidValueGenerator"/>.
    /// </summary>
    Guid Id { get; set; }
}
