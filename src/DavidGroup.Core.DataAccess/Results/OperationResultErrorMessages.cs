namespace DavidGroup.Core.DataAccess.Results;

/// <summary>
/// Error message for OperationResult
/// </summary>
public static class OperationResultErrorMessages
{
    /// <summary>
    /// Successful operation result cannot contain any errors.
    /// </summary>
    public const string SuccessfulOperationResultCannotContainAnyErrors = nameof(SuccessfulOperationResultCannotContainAnyErrors);

    /// <summary>
    /// Used when no value found.
    /// </summary>
    public const string NoValue = nameof(NoValue);
}
