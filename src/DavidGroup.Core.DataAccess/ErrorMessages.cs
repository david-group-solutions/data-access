namespace DavidGroup.Core.DataAccess;

/// <summary>
/// Provides a collection of predefined error message identifiers.
/// </summary>
/// <remarks>
/// These constants are intended to standardize error messages and maintain consistency
/// across the application when reporting or handling common data-related errors.
/// </remarks>
public static class ErrorMessages
{
    /// <summary>
    /// Represents an error indicating that a requested resource was not found.
    /// </summary>
    public const string NotFound = nameof(NotFound);

    /// <summary>
    /// Represents an error indicating that an attempt was made to create or add
    /// a resource that already exists.
    /// </summary>
    public const string AlreadyExists = nameof(AlreadyExists);

    /// <summary>
    /// Represents an error indicating that the current user or operation
    /// is not authorized to perform the requested action.
    /// </summary>
    public const string Forbidden = nameof(Forbidden);

    /// <summary>
    /// Represents an error indicating that an unexpected or generic failure occurred.
    /// </summary>
    public const string SomethingWentWrong = nameof(SomethingWentWrong);

    /// <summary>
    /// Represents an error indicating that one or more input values were invalid or malformed.
    /// </summary>
    public const string InvalidInput = nameof(InvalidInput);
}
