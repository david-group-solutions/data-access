namespace DavidGroup.Core.DataAccess.Sql.Entities;

/// <summary>
/// Marker interface for entities which must have SQL Server generated value using <c>NEWSEQUENTIALID()</c>.
/// </summary>
/// <typeparam name="T">The type of the primary key.</typeparam>
public interface ISqlServerSequentialId<T>
{
    /// <summary>
    /// The PK which has SQl Server generated value using <c>NEWSEQUENTIALID()</c>.
    /// </summary>
    T Id { get; set; }
}
