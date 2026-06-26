using System.Text.Json.Serialization;

namespace DavidGroup.Core.DataAccess.Pagination;

/// <summary>
/// Represents a paginated set of data along with pagination metadata.
/// </summary>
/// <typeparam name="T">The type of the entities in the page.</typeparam>
public record PageData<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PageData{T}"/> class.
    /// Constructor is used for deserialization.
    /// </summary>
    [JsonConstructor]
    public PageData() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PageData{T}"/> class with entities and pagination information.
    /// </summary>
    /// <param name="entities">The collection of entities for the current page.</param>
    /// <param name="totalCount">The total number of entities across all pages.</param>
    /// <param name="page">The current page number (1-based).</param>
    /// <param name="size">The number of items per page.</param>
    public PageData(IEnumerable<T> entities, int totalCount, int page, int size)
    {
        Entities = entities;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(TotalCount / (double)size);
        HasPreviousPage = page > 1;
        HasNextPage = page < TotalPages;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PageData{T}"/> class using <see cref="PageOptions"/>.
    /// </summary>
    /// <param name="entities">The collection of entities for the current page.</param>
    /// <param name="totalCount">The total number of entities across all pages.</param>
    /// <param name="options">The pagination options containing page number and size.</param>
    public PageData(IEnumerable<T> entities, int totalCount, PageOptions options)
        : this(entities, totalCount, options.Page, options.Size) { }


    /// <summary>
    /// Gets the entities in the current page.
    /// </summary>
    [JsonInclude]
    public IEnumerable<T> Entities { get; protected init; } = [];

    /// <summary>
    /// Gets the total number of entities across all pages.
    /// </summary>
    [JsonInclude]
    public int TotalCount { get; protected init; }

    /// <summary>
    /// Gets the total number of pages based on the <see cref="PageOptions.Size"/>.
    /// </summary>
    [JsonInclude]
    public int TotalPages { get; protected init; }

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    [JsonInclude]
    public bool HasPreviousPage { get; protected init; }

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    [JsonInclude]
    public bool HasNextPage { get; protected init; }
}
