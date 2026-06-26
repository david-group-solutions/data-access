using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DavidGroup.Core.DataAccess.Results;

/// <summary>
/// Represents the result of an operation, including its success status,
/// associated messages, and derived status.
/// </summary>
public abstract record OperationResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="OperationResult"/> with optional messages.
    /// </summary>
    /// <param name="messages">The messages associated with the operation result, such as warnings or errors.</param>
    private protected OperationResult(params OperationResultMessage[] messages)
    {
        Messages = [..messages];
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public virtual bool Succeeded { get; } = false;

    /// <summary>
    /// Determines whether the operation result contains any warnings.
    /// </summary>
    /// <returns><c>true</c> if there are warnings; otherwise, <c>false</c>.</returns>
    public virtual bool HasWarnings() => FindWarnings().Any();

    /// <summary>
    /// Determines whether the operation result contains any errors.
    /// </summary>
    /// <returns><c>true</c> if there are errors; otherwise, <c>false</c>.</returns>
    public virtual bool HasErrors() => FindErrors().Any();

    /// <summary>
    /// Gets the overall status of the operation, derived from <see cref="Succeeded"/> and the <see cref="Messages"/>.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OperationStatus Status
    {
        get
        {
            if (Succeeded && HasWarnings())
                return OperationStatus.PartialSuccess;

            return Succeeded
                ? OperationStatus.Success
                : OperationStatus.Failure;
        }
    }

    /// <summary>
    /// Gets the collection of messages associated with the operation result.
    /// Messages can include errors, warnings, or informational messages.
    /// </summary>
    public ImmutableArray<OperationResultMessage> Messages { get; }

    /// <summary>
    /// Creates a successful operation result with optional messages.
    /// </summary>
    /// <param name="messages">Messages to attach to the success result.</param>
    /// <returns>An <see cref="OperationResult"/> representing success.</returns>
    public static OperationResult Success(params OperationResultMessage[] messages) =>
        new SuccessfulOperationResult(messages);

    /// <summary>
    /// Creates a failed operation result with optional error messages.
    /// </summary>
    /// <param name="errors">Error messages to attach to the failure result.</param>
    /// <returns>An <see cref="OperationResult"/> representing failure.</returns>
    public static OperationResult Failure(params OperationResultMessage[] errors) =>
        new FailedOperationResult(errors);

    /// <summary>
    /// Returns all messages with severity <see cref="OperationResultSeverity.Warning"/>.
    /// </summary>
    public IEnumerable<OperationResultMessage> FindWarnings() =>
        Messages.Where(x => x.Severity == OperationResultSeverity.Warning);

    /// <summary>
    /// Returns all messages with severity <see cref="OperationResultSeverity.Error"/>.
    /// </summary>
    public IEnumerable<OperationResultMessage> FindErrors() =>
        Messages.Where(x => x.Severity == OperationResultSeverity.Error);

    /// <summary>
    /// Overrides equality in order to make two records comparable.
    /// </summary>
    /// <param name="other">Other instance which is compared to this.</param>
    /// <returns></returns>
    public virtual bool Equals(OperationResult? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null)
            return false;

        return Succeeded == other.Succeeded &&
               Messages.SequenceEqual(other.Messages);
    }

    /// <summary>
    /// Overrides equality in order to make two records comparable.
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Succeeded);

        foreach (OperationResultMessage message in Messages)
            hash.Add(message);

        return hash.ToHashCode();
    }
}
