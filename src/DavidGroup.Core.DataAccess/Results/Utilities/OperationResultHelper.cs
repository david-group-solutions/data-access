using DavidGroup.Core.DataAccess.Results.Generic;

namespace DavidGroup.Core.DataAccess.Results.Utilities;

/// <summary>
/// Provides utility methods for creating failed <see cref="OperationResult"/> instances.
/// </summary>
public static class OperationResultHelper
{
    /// <summary>
    /// Creates a failed operation result with the specified message and severity.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="severity">The severity of the failure.</param>
    /// <returns>A failed <see cref="OperationResult"/> containing the specified message and severity.</returns>
    public static OperationResult Fail(string message, OperationResultSeverity severity) =>
        OperationResult.Failure(new OperationResultMessage(message, severity));

    /// <summary>
    /// Creates a failed operation result with the specified message and severity.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="severity">The severity of the failure.</param>
    /// <returns>A failed <see cref="OperationResult{T}"/> containing the specified message and severity.</returns>
    public static OperationResult<T> Fail<T>(string message, OperationResultSeverity severity) =>
        OperationResult<T>.Failure(new OperationResultMessage(message, severity));


    /// <summary>
    /// Creates a failed operation result with error severity.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <returns>A failed <see cref="OperationResult"/> containing the specified error message.</returns>
    public static OperationResult Error(string error) =>
        Fail(error, OperationResultSeverity.Error);

    /// <summary>
    /// Creates a failed operation result with error severity.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="error">The error message describing the failure.</param>
    /// <returns>A failed <see cref="OperationResult{T}"/> containing the specified error message.</returns>
    public static OperationResult<T> Error<T>(string error) =>
        Fail<T>(error, OperationResultSeverity.Error);

    /// <summary>
    /// Creates a failed operation result with warning severity.
    /// </summary>
    /// <param name="warn">The warning message describing the failure.</param>
    /// <returns>A failed <see cref="OperationResult"/> containing the specified warning message.</returns>
    public static OperationResult Warning(string warn) =>
        Fail(warn, OperationResultSeverity.Warning);

    /// <summary>
    /// Creates a failed operation result with warning severity.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="warn">The warning message describing the failure.</param>
    /// <returns>A failed <see cref="OperationResult{T}"/> containing the specified warning message.</returns>
    public static OperationResult<T> Warning<T>(string warn) =>
        Fail<T>(warn, OperationResultSeverity.Warning);
}
