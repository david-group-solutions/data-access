using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DavidGroup.Core.DataAccess.Results.Generic;

/// <summary>
/// Represents a successful operation result that carries a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the value returned by the operation.</typeparam>
public sealed record SuccessfulOperationResult<T> : OperationResult<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="SuccessfulOperationResult{T}"/> with a value and optional messages.
    /// </summary>
    /// <param name="value">The value of the successful operation.</param>
    /// <param name="messages">Optional messages associated with the operation.</param>
    public SuccessfulOperationResult(T value, params OperationResultMessage[] messages)
        : base(value, messages)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (HasErrors())
            throw new InvalidOperationException(ErrorMessages.SuccessfulOperationResultCannotContainAnyErrors);
    }

    /// <summary>
    /// Constructor for JSON serialization/deserialization.
    /// </summary>
    /// <param name="value">The value of the successful operation.</param>
    /// <param name="messages">Optional messages associated with the operation.</param>
    [JsonConstructor]
    public SuccessfulOperationResult(T value, ImmutableArray<OperationResultMessage> messages)
        : this(value, messages.ToArray()) { }

    /// <summary>
    /// Indicates that this operation was successful.
    /// Always returns <c>true</c>.
    /// </summary>
    public override bool Succeeded => true;
}
