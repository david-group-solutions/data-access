using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;

/// <summary>
/// Represents the options for an infinite scroll (cursor) pagination request.
/// Supports either a dynamic cursor or a token for fetching the next page.
/// </summary>
public record InfinitePageOptions
{
    /// <summary>
    /// Gets the number of items per page. Must be between 1 and 100.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = PaginationErrorMessages.PageSizeShouldBeGreaterThanZero)]
    [MaxPageSize]
    public int Size { get; init; }

    /// <summary>
    /// Gets the encoded token representing the cursor for the next page.
    /// </summary>
    public string? SearchAfterToken { get; init; }

    /// <summary>
    /// Constructor for JSON.
    /// </summary>
    [JsonConstructor]
    public InfinitePageOptions() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InfinitePageOptions"/> record using an encoded search-after token.
    /// </summary>
    /// <param name="size">The number of items per page. Must be greater than zero.</param>
    /// <param name="searchAfterToken">The token representing the starting point for the next page.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="size"/> is less than or equal to zero.</exception>
    public InfinitePageOptions(int size, string? searchAfterToken)
    {
        if (size <= 0)
            throw new ArgumentException(PaginationErrorMessages.PageSizeShouldBeGreaterThanZero, nameof(size));

        Size = size;
        SearchAfterToken = searchAfterToken;
    }
}
