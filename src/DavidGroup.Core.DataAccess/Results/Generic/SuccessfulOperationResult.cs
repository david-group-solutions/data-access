namespace DavidGroup.Core.DataAccess.Results.Generic;

/// <summary>
/// Represents a successful operation result that carries a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the value returned by the operation.</typeparam>
public record SuccessfulOperationResult<T> : OperationResult<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="SuccessfulOperationResult{T}"/> with a value and optional messages.
    /// </summary>
    /// <param name="value">The value of the successful operation.</param>
    /// <param name="messages">Optional messages associated with the operation.</param>
    public SuccessfulOperationResult(T value, params OperationResultMessage[] messages)
        : base(value, messages) { }

    /// <summary>
    /// Indicates that this operation was successful.
    /// Always returns <c>true</c>.
    /// </summary>
    public override bool Succeeded => true;
}