using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
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
    [Range(1, int.MaxValue, ErrorMessage = ErrorMessages.PageSizeShouldBeGreaterThanZero)]
    [MaxPageSize]
    public int Size { get; init; }

    /// <summary>
    /// Gets or sets the dynamic cursor used for fetching the next page.
    /// This property is not null when <see cref="SearchAfterToken"/> is null.
    /// </summary>
    [MemberNotNullWhen(returnValue: false, nameof(SearchAfterToken))]
    public DynamicCursor? SearchAfter { get; init; }

    /// <summary>
    /// Gets the encoded token representing the cursor for the next page.
    /// This property is not null when <see cref="SearchAfter"/> is null.
    /// </summary>
    [MemberNotNullWhen(returnValue: false, nameof(SearchAfter))]
    public string? SearchAfterToken { get; init; }

    /// <summary>
    /// Constructor for JSON.
    /// </summary>
    [JsonConstructor]
    public InfinitePageOptions() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InfinitePageOptions"/> record using a dynamic cursor.
    /// </summary>
    /// <param name="size">The number of items per page. Must be greater than zero.</param>
    /// <param name="searchAfter">The dynamic cursor representing the starting point for the next page.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="size"/> is less than or equal to zero.</exception>
    public InfinitePageOptions(int size, DynamicCursor? searchAfter)
    {
        if (size <= 0)
            throw new ArgumentException(ErrorMessages.PageSizeShouldBeGreaterThanZero, nameof(size));

        Size = size;
        SearchAfter = searchAfter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InfinitePageOptions"/> record using an encoded search-after token.
    /// </summary>
    /// <param name="size">The number of items per page. Must be greater than zero.</param>
    /// <param name="searchAfterToken">The token representing the starting point for the next page.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="size"/> is less than or equal to zero.</exception>
    public InfinitePageOptions(int size, string? searchAfterToken)
    {
        if (size <= 0)
            throw new ArgumentException(ErrorMessages.PageSizeShouldBeGreaterThanZero, nameof(size));

        Size = size;
        SearchAfterToken = searchAfterToken;
    }
}
