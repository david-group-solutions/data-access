using System.Diagnostics.CodeAnalysis;

namespace DavidGroup.Core.DataAccess.Results.Generic;

/// <summary>
/// Represents the result of an operation that may carry a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the value returned by an operation.</typeparam>
public abstract record OperationResult<T> : OperationResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="OperationResult{T}"/> with a value and optional messages.
    /// </summary>
    /// <param name="value">The value of the operation, may be null for failures.</param>
    /// <param name="messages">Optional messages associated with the operation.</param>
    private protected OperationResult(T? value, params OperationResultMessage[] messages)
        : base(messages)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the value of the operation if available.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Indicates whether the operation succeeded.
    /// </summary>
    [MemberNotNullWhen(returnValue: true, member: nameof(Value))]
    public override bool Succeeded => base.Succeeded;

    /// <summary>
    /// Indicates whether the operation has any warnings.
    /// </summary>
    [MemberNotNullWhen(returnValue: false, member: nameof(Value))]
    public override bool HasWarnings() => base.HasWarnings();

    /// <summary>
    /// Indicates whether the operation has any errors.
    /// </summary>
    [MemberNotNullWhen(returnValue: false, member: nameof(Value))]
    public override bool HasErrors() => base.HasErrors();

    /// <summary>
    /// Creates a successful operation result with a value.
    /// </summary>
    public static OperationResult<T> Success(T value, params OperationResultMessage[] messages) =>
        new SuccessfulOperationResult<T>(value, messages);

    /// <summary>
    /// Creates a failed operation result with a value (usually for partial success scenarios).
    /// </summary>
    public static OperationResult<T> Failure(T value, params OperationResultMessage[] messages) =>
        new FailedOperationResult<T>(value, messages);

    /// <summary>
    /// Creates a failed operation result with no value.
    /// </summary>
    public new static OperationResult<T> Failure(params OperationResultMessage[] messages) =>
        new FailedOperationResult<T>(default, messages);

    /// <summary>
    /// Implicitly converts an <see cref="OperationResult{T}"/> to its underlying value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="Value"/> is null.</exception>
    public static implicit operator T(OperationResult<T> result)
    {
        if (result.Value == null)
            throw new InvalidOperationException();

        return result.Value;
    }

    /// <summary>
    /// Implicitly converts a value to <see cref="OperationResult{T}"/>.
    /// </summary>
    public static implicit operator OperationResult<T>(T? value)
    {
        return value is null
            ? Failure()
            : Success(value);
    }
}