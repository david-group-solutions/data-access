namespace DavidGroup.Core.DataAccess.Pagination;

/// <summary>
/// Provides extension methods for pagination.
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Returns <see cref="PageData{T}"/> from a collection of items based on the specified pagination options.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="source">The source collection to paginate.</param>
    /// <param name="pageOptions">The pagination options containing page number and page size.</param>
    /// <returns>A <see cref="PageData{T}"/> object containing the paginated items and metadata.</returns>
    public static PageData<T> ToPageData<T>(this IEnumerable<T> source, PageOptions pageOptions)
    {
        List<T> sourceList = source.ToList();

        List<T> entities = sourceList
            .Skip((pageOptions.Page - 1) * pageOptions.Size)
            .Take(pageOptions.Size)
            .ToList();

        int totalCount = sourceList.Count;

        return new PageData<T>(entities, totalCount, pageOptions);
    }

    /// <summary>
    /// Returns <see cref="PageData{T}"/> from <see cref="IQueryable{T}"/> based on the specified pagination options.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="source">The source collection to paginate.</param>
    /// <param name="pageOptions">The pagination options containing page number and page size.</param>
    /// <returns>A <see cref="PageData{T}"/> object containing the paginated items and metadata.</returns>
    public static PageData<T> ToPageData<T>(this IQueryable<T> source, PageOptions pageOptions)
    {
        List<T> entities = source
            .Skip((pageOptions.Page - 1) * pageOptions.Size)
            .Take(pageOptions.Size)
            .ToList();

        int totalCount = source.Count();

        return new PageData<T>(entities, totalCount, pageOptions);
    }
}
