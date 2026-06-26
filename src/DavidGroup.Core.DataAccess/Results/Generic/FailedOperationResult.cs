namespace DavidGroup.Core.DataAccess.Results.Generic;

/// <summary>
/// Represents a failed operation result that optionally carries a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the value returned by the operation.</typeparam>
public record FailedOperationResult<T> : OperationResult<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="FailedOperationResult{T}"/> with an optional value and error messages.
    /// </summary>
    /// <param name="value">The value associated with the failed operation, if any.</param>
    /// <param name="messages">Error messages describing why the operation failed.</param>
    public FailedOperationResult(T? value, params OperationResultMessage[] messages)
        : base(value, messages) { }

    /// <summary>
    /// Indicates that this operation was not successful.
    /// Always returns <c>false</c>.
    /// </summary>
    public override bool Succeeded => false;
}