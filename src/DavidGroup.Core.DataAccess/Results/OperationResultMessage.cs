using System.Text.Json.Serialization;

namespace DavidGroup.Core.DataAccess.Results;

/// <summary>
/// Represents a single message associated with an <see cref="OperationResult"/>.
/// Messages can be informational, warnings, or errors.
/// </summary>
public record OperationResultMessage
{
    /// <summary>
    /// Initializes a new instance of <see cref="OperationResultMessage"/> with the specified message and severity.
    /// </summary>
    /// <param name="message">The message text. Cannot be null.</param>
    /// <param name="severity">The severity level of the message.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
    public OperationResultMessage(string message, OperationResultSeverity severity)
    {
        Message = message;
        Severity = severity;
    }

    /// <summary>
    /// Gets the text of the message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the severity of the message.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OperationResultSeverity Severity { get; }
}
