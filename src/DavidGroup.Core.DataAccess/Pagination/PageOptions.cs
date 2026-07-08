using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DavidGroup.Core.DataAccess.Pagination;

/// <summary>
/// Represents pagination options for a query, including the page number and page size.
/// </summary>
public record PageOptions
{
    /// <summary>
    /// Gets the current page number (1-based).
    /// Must be greater than 0.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = PaginationErrorMessages.PageNumberShouldBeGreaterThanZero)]
    public int Page { get; init; }

    /// <summary>
    /// Gets the number of items per page.
    /// Must be between 1 and <see cref="MaxPageSizeAttribute"/>.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = PaginationErrorMessages.PageSizeShouldBeGreaterThanZero)]
    [MaxPageSize]
    public int Size { get; init; }

    /// <summary>
    /// Constructor for JSON.
    /// </summary>
    [JsonConstructor]
    public PageOptions() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PageOptions"/> class with the specified page number and size.
    /// </summary>
    /// <param name="page">The current page number (must be greater than 0).</param>
    /// <param name="size">The number of items per page (must be greater than 0).</param>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="page"/> is less than or equal to 0 or
    /// if <paramref name="size"/> is less than or equal to 0.
    /// </exception>
    public PageOptions(int page, int size)
    {
        if (page <= 0)
            throw new ArgumentException(PaginationErrorMessages.PageNumberShouldBeGreaterThanZero, nameof(page));

        if (size <= 0)
            throw new ArgumentException(PaginationErrorMessages.PageSizeShouldBeGreaterThanZero, nameof(size));

        Page = page;
        Size = size;
    }
}
