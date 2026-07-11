using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;

/// <summary>
/// Represents a paginated result set for infinite scroll (cursor) pagination.
/// Contains the retrieved entities and information for fetching the next page.
/// </summary>
/// <typeparam name="T">The type of the items in the result set.</typeparam>
public record InfinitePageData<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InfinitePageData{T}"/> record.
    /// This constructor is used for JSON deserialization.
    /// </summary>
    [JsonConstructor]
    public InfinitePageData() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InfinitePageData{T}"/> record with the specified entities and cursor information.
    /// </summary>
    /// <param name="entities">The entities retrieved for the current page.</param>
    /// <param name="nextCursor">The dynamic cursor to be used in the next query.</param>
    public InfinitePageData(IEnumerable<T>? entities, DynamicCursor? nextCursor)
    {
        Entities = entities?.ToImmutableList() ?? [];
        NextCursor = nextCursor;
        HasNextPage = nextCursor is not null;
    }

    /// <summary>
    /// Gets the entities retrieved for the current page.
    /// </summary>
    [JsonInclude]
    public ImmutableList<T> Entities { get; private init; } = [];

    /// <summary>
    /// Gets the dynamic cursor to be used in the next query.
    /// </summary>
    [JsonInclude]
    public DynamicCursor? NextCursor { get; private init; }

    /// <summary>
    /// Gets a value indicating whether there are more pages available after this page.
    /// </summary>
    [JsonInclude]
    public bool HasNextPage { get; private init; }

    /// <summary>
    /// Overrides equality in order to make two records comparable.
    /// </summary>
    /// <param name="other">Other instance which is compared to this.</param>
    /// <returns></returns>
    public virtual bool Equals(InfinitePageData<T>? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null)
            return false;

        return Entities.SequenceEqual(other.Entities) &&
               (NextCursor is null || (NextCursor is not null && NextCursor.Equals(other.NextCursor))) &&
               HasNextPage == other.HasNextPage;
    }

    /// <summary>
    /// Overrides equality in order to make two records comparable.
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        HashCode hash = new();

        foreach (T entity in Entities)
            hash.Add(entity);

        hash.Add(NextCursor);
        hash.Add(HasNextPage);

        return hash.ToHashCode();
    }
}
