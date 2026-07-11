using DavidGroup.Core.DataAccess.Sql.ValueGenerators;

namespace DavidGroup.Core.DataAccess.Sql.Entities;

/// <summary>
/// Marker interface for entities which must have sequential StronglyTypedIds
/// using <see cref="SequentialStronglyTypedIdValueGenerator{TKey}"/>.
/// </summary>
/// <typeparam name="T">The type of the primary key.</typeparam>
public interface IStronglyTypedSequentialId<T>
{
    /// <summary>
    /// The PK which has generated value using <see cref="SequentialStronglyTypedIdValueGenerator{Tkey}"/>.
    /// </summary>
    T Id { get; set; }
}
