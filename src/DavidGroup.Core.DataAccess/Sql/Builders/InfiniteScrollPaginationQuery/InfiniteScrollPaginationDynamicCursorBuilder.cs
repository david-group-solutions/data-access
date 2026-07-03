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
    /// Builds the <see cref="DynamicCursor"/> for the next page of an infinite scroll query.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being queried.</typeparam>
    /// <param name="pageSize">Pagination option that specifies page size.</param>
    /// <param name="orderBy">
    /// A collection of expressions defining the order of the query.
    /// Each expression should correspond to a sortable entity property.
    /// </param>
    /// <param name="ordered">
    /// The <see cref="IQueryable{T}"/> representing the already sorted query.
    /// The cursor will be built from the last item in the current page.
    /// </param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>
    /// A <see cref="DynamicCursor"/> representing the key values of the next page.
    /// </returns>
    public static async Task<DynamicCursor> BuildNextCursorAsync<TEntity>(
        int pageSize,
        IEnumerable<Expression<Func<TEntity, object>>> orderBy,
        IQueryable<TEntity> ordered,
        CancellationToken cancellationToken = default)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "e");

        NewArrayExpression nextCursorExpr = Expression.NewArrayInit(
            typeof(object),
            orderBy.Select(o =>
            {
                Expression body = o.Body is UnaryExpression { NodeType: ExpressionType.Convert } unary
                    ? unary.Operand
                    : o.Body;

                body = new ReplaceParameterVisitor(o.Parameters[0], parameter).Visit(body);

                return Expression.Convert(body, typeof(object));
            })
        );

        Expression<Func<TEntity, object[]>> cursorSelector =
            Expression.Lambda<Func<TEntity, object[]>>(nextCursorExpr, parameter);

        object[] nextValues = await ordered
            .Skip(pageSize)
            .Select(cursorSelector)
            .FirstAsync(cancellationToken); // TODO: Try also with one query just including every column we need.

        return new DynamicCursor(nextValues);
    }
}
