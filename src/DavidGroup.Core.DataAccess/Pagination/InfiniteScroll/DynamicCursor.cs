namespace DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;

/// <summary>
/// Represents a cursor for infinite scroll (cursor) pagination.
/// Contains the values of the last item in a page,
/// which can be used to fetch the next page of results.
/// </summary>
/// <param name="Values">
/// An array of objects representing the key values used for ordering.
/// These values are typically used with the "search after" functionality
/// in queries to continue pagination from the last retrieved item.
/// </param>
public sealed record DynamicCursor(object?[] Values);