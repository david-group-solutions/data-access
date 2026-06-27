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
}
