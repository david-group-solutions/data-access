namespace DavidGroup.Core.DataAccess.Results;

/// <summary>
/// Indicates the overall status of the operation
/// </summary>
public enum OperationStatus
{
    /// <summary>
    /// Successful outcome
    /// </summary>
    Success,

    /// <summary>
    /// Failed result
    /// </summary>
    Failure,

    /// <summary>
    /// Succeeded with some warnings or errors
    /// </summary>
    PartialSuccess
}
