namespace DavidGroup.Core.DataAccess.Results;

/// <summary>
/// Represents a successful operation result.
/// </summary>
/// <remarks>
/// This type always indicates that the operation succeeded (<see cref="Succeeded"/> returns true).
/// Optional messages can be attached to provide additional information or warnings.
/// </remarks>
public sealed record SuccessfulOperationResult : OperationResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="SuccessfulOperationResult"/> with optional messages.
    /// </summary>
    /// <param name="messages">An optional array of <see cref="OperationResultMessage"/> providing additional context for the successful result.</param>
    public SuccessfulOperationResult(params OperationResultMessage[] messages)
        : base(messages)
    {
        if (HasErrors())
            throw new InvalidOperationException(ErrorMessages.SuccessfulOperationResultCannotContainAnyErrors);
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// Always returns <c>true</c>.
    /// </summary>
    public override bool Succeeded => true;
}
