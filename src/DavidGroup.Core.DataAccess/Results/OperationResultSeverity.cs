namespace DavidGroup.Core.DataAccess.Results;

/// <summary>
/// Indicates the severity of the associated result
/// </summary>
public enum OperationResultSeverity
{
    /// <summary>
    /// May include note, status or something which contains helpful information.
    /// </summary>
    Information,

    /// <summary>
    /// Shows that it succeeded partially.
    /// </summary>
    Warning,

    /// <summary>
    /// Something went wrong through the operation process.
    /// </summary>
    Error
}
