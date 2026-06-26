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
public sealed record DynamicCursor(object?[] Values)
{
    /// <summary>
    /// Overrides equality in order to make two record comparable.
    /// </summary>
    /// <param name="other">Other instance which is compared to this.</param>
    /// <returns></returns>
    public bool Equals(DynamicCursor? other)
        => other is not null &&
           Values.AsSpan().SequenceEqual(other.Values);

    /// <summary>
    /// Overrides equality in order to make two record comparable.
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        HashCode hash = new();

        foreach (object? value in Values)
            hash.Add(value);

        return hash.ToHashCode();
    }
}
