using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.Utilities.Expressions;

using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccess.Sql.Builders.InfiniteScrollPaginationQuery;

/// <summary>
/// Builds dynamic cursor objects for infinite scroll pagination based on
/// the last element of a sorted query.
/// </summary>
public static class InfiniteScrollPaginationDynamicCursorBuilder
{
    /// <summary>
    /// Builds a <see cref="DynamicCursor"/> that points to the first item of the next page
    /// in an infinite scroll query.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being queried.</typeparam>
    /// <param name="orderedQuery">
    /// The query with ordering already applied.
    /// The cursor is built from the first item after the current page.
    /// </param>
    /// <param name="orderSelectors">
    /// The expressions used to order the query.
    /// They determine which values are included in the cursor. The ordering direction
    /// is irrelevant because the query has already been ordered.
    /// </param>
    /// <param name="pageSize">The number of items in the current page.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>
    /// A <see cref="DynamicCursor"/> containing the ordering key values for the next page.
    /// </returns>
    public static async Task<DynamicCursor> BuildNextCursorAsync<TEntity>(
        IQueryable<TEntity> orderedQuery,
        IEnumerable<Expression<Func<TEntity, object>>> orderSelectors,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "e");

        NewArrayExpression cursorValuesExpression = Expression.NewArrayInit(
            typeof(object),
            orderSelectors.Select(selector =>
            {
                Expression selectorBody
                    = selector.Body is UnaryExpression { NodeType: ExpressionType.Convert } conversion
                        ? conversion.Operand
                        : selector.Body;

                selectorBody = new ReplaceParameterVisitor(selector.Parameters[0], parameter)
                    .Visit(selectorBody);

                return Expression.Convert(selectorBody, typeof(object));
            })
        );

        Expression<Func<TEntity, object[]>> cursorProjection =
            Expression.Lambda<Func<TEntity, object[]>>(cursorValuesExpression, parameter);

        object[] cursorValues = await orderedQuery
            .Skip(pageSize - 1)
            .Select(cursorProjection)
            .FirstAsync(cancellationToken);

        return new DynamicCursor(cursorValues);
    }
}
