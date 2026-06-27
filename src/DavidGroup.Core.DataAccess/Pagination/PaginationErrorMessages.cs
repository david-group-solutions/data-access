namespace DavidGroup.Core.DataAccess.Pagination;

/// <summary>
/// Pagination errors messages
/// </summary>
public static class PaginationErrorMessages
{
    /// <summary>
    /// Used when page number is less or equals to zero
    /// </summary>
    public const string PageNumberShouldBeGreaterThanZero = nameof(PageNumberShouldBeGreaterThanZero);

    /// <summary>
    /// Used when page size is less or equals to zero
    /// </summary>
    public const string PageSizeShouldBeGreaterThanZero = nameof(PageSizeShouldBeGreaterThanZero);

    /// <summary>
    /// Used when page size exceeds the maximum configured value
    /// </summary>
    public const string PageSizeShouldNotExceedMaximum = nameof(PageSizeShouldNotExceedMaximum);
}
