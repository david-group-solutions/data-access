namespace DavidGroup.Core.DataAccess.Results;

/// <summary>
/// Represents a failed operation result.
/// </summary>
/// <remarks>
/// This type always indicates that the operation did not succeed (<see cref="Succeeded"/> returns false).
/// Optional messages can be attached to provide additional information, warning or error details.
/// </remarks>
public sealed record FailedOperationResult : OperationResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="FailedOperationResult"/> with optional error messages.
    /// </summary>
    /// <param name="messages">An optional array of <see cref="OperationResultMessage"/> providing details about the failure.</param>
    public FailedOperationResult(params OperationResultMessage[] messages)
        : base(messages) { }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// Always returns <c>false</c>.
    /// </summary>
    public override bool Succeeded => false;
}
