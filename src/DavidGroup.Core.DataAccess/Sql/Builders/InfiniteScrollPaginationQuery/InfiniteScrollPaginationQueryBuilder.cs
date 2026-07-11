using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Sql.Builders.BasicQuery;

using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccess.Sql.Builders.InfiniteScrollPaginationQuery;

/// <summary>
/// Builds an infinite scroll pagination query.
/// </summary>
/// <typeparam name="TEntity">The entity type being queried.</typeparam>
/// <remarks>
/// <para>
/// This query builder extends <see cref="BasicQueryBuilder{TEntity}"/> and adds
/// support for infinite scrolling using "search-after" cursors.
/// </para>
/// </remarks>
public class InfiniteScrollPaginationQueryBuilder<TEntity>(IQueryable<TEntity> query)
    : BasicQueryBuilder<TEntity>(query)
    where TEntity : class
{
    /// <summary>
    /// Use <c>ExecuteWithCursorPagination</c> method instead and pass the ordering specifications there.
    /// </summary>
    /// <exception cref="NotSupportedException">Use <c>ExecuteWithCursorPagination</c> method instead and pass the ordering specifications there.</exception>
    public new BasicQueryBuilder<TEntity> WithOrdering(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy)
    {
        throw new NotSupportedException($"Use {nameof(ExecuteWithCursorPagination)}() method instead and pass the ordering specifications there.");
    }

    /// <summary>
    /// Use <c>ExecuteWithCursorPagination</c> method instead and pass the ordering specifications there.
    /// </summary>
    /// <exception cref="NotSupportedException">Use <c>ExecuteWithCursorPagination</c> method instead and pass the ordering specifications there.</exception>
    public new BasicQueryBuilder<TEntity> WithOrdering(IReadOnlyList<OrderingSpecification<TEntity>>? orderingSpecifications)
    {
        throw new NotSupportedException($"Use {nameof(ExecuteWithCursorPagination)}() method instead and pass the ordering specifications there.");
    }

    /// <summary>
    /// Use <c>ExecuteWithCursorPagination</c> method instead and pass the selector expression there.
    /// </summary>
    /// <exception cref="NotSupportedException">Use <c>ExecuteWithCursorPagination</c> method instead and pass the selector expression there.</exception>
    public new InfiniteScrollPaginationQueryBuilder<TResult> WithProjection<TResult>(
        Expression<Func<TEntity, TResult>> selector)
        where TResult : class
    {
        throw new NotSupportedException($"Use {nameof(ExecuteWithCursorPagination)}() method instead and pass the selector expression there.");
    }

    /// <summary>
    /// Executes the query using cursor-based pagination starting after the specified cursor
    /// and projects the results into the specified type.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type of the projected result.
    /// </typeparam>
    /// <param name="pageOptions">
    /// The pagination options containing the page size and either decoded search-after
    /// cursor or an encoded search-after token that identifies the last item from the
    /// previous page.
    /// </param>
    /// <param name="orderingSpecifications">
    /// The list of ordering specifications that define the sort order.
    /// At least one ordering specification
    /// must be provided.
    /// </param>
    /// <param name="selector">An expression defining the projection. Defaults to <c>e => e</c>.</param>
    /// <param name="nextCursorSelector">
    /// The selector for next cursor. By default, it will determine it automatically but execute the second SQL query,
    /// in order to increase performance you must specify it manually.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// An <see cref="InfinitePageData{TResult}"/> containing up to
    /// <see cref="InfinitePageOptions.Size"/> projected items, a cursor for retrieving
    /// the next page (if one exists), and a value indicating whether additional items
    /// are available.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The query is filtered to return only entities that appear after the specified
    /// cursor according to the provided ordering specifications.
    /// </para>
    /// <para>
    /// To determine whether another page exists, the method retrieves one additional
    /// item beyond the requested page size. The extra item is used only to determine
    /// whether more results are available and is not included in the returned page.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="orderingSpecifications"/> is empty.
    /// </exception>
    public async Task<InfinitePageData<TResult>> ExecuteWithCursorPagination<TResult>(
        InfinitePageOptions pageOptions,
        IReadOnlyList<OrderingSpecification<TEntity>> orderingSpecifications,
        Expression<Func<TEntity, TResult>>? selector = null,
        Func<TResult, object[]>? nextCursorSelector = null,
        CancellationToken cancellationToken = default)
    {
        if (orderingSpecifications.Count == 0)
        {
            throw new InvalidOperationException(
                "No ordering specifications were found. At least one ordering specification must be specified.");
        }

        if (pageOptions.SearchAfterToken is not null)
        {
            DynamicCursor? lastCursor = DynamicCursorTokenizer.Decode(pageOptions.SearchAfterToken);

            if (lastCursor is not null)
            {
                Expression<Func<TEntity, bool>> searchAfterFilter =
                    InfiniteScrollPaginationSearchAfterFilterBuilder.Build(orderingSpecifications, lastCursor);

                Query = Query.Where(searchAfterFilter);
            }
        }

        Query = OrderingSpecification<TEntity>.Apply(Query, orderingSpecifications);

        List<TResult> items = await Query
            .Select(selector ??
                    (Expression<Func<TEntity, TResult>>)(object)
                    (Expression<Func<TEntity, TEntity>>)(e => e))
            .Take(pageOptions.Size + 1)
            .ToListAsync(cancellationToken);

        bool hasMore = items.Count > pageOptions.Size;

        DynamicCursor? nextCursor = null;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);

            if (nextCursorSelector is not null)
            {
                object[] nextCursorValues = nextCursorSelector.Invoke(items.Last());
                nextCursor = new DynamicCursor(nextCursorValues);
            }
            else
            {
                nextCursor = await InfiniteScrollPaginationDynamicCursorBuilder.BuildNextCursorAsync(
                    Query, orderingSpecifications.Select(spec => spec.OrderBy), pageOptions.Size, cancellationToken);
            }
        }

        return new InfinitePageData<TResult>(items, nextCursor);
    }
}
