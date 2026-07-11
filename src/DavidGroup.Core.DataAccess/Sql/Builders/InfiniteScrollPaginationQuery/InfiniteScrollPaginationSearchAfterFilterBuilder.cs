using System.Linq.Expressions;
using System.Reflection;

using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.Utilities.Expressions;

namespace DavidGroup.Core.DataAccess.Sql.Builders.InfiniteScrollPaginationQuery;

/// <summary>
/// Builds a dynamic "search-after" predicate for infinite scroll pagination
/// based on multi-field ordering and the last fetched values.
/// </summary>
public static class InfiniteScrollPaginationSearchAfterFilterBuilder
{
    /// <summary>
    /// Builds the search-after predicate expression for a given entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being queried.</typeparam>
    /// <param name="orderingSpecifications">The collection of ordering specifications.</param>
    /// <param name="lastCursor">
    /// Values representing the last entity fetched in the previous page.
    /// Used to compute the "after" filter for the next page.
    /// </param>
    /// <returns>
    /// An expression that represents a predicate for filtering <typeparamref name="TEntity"/> instances.
    /// </returns>
    public static Expression<Func<TEntity, bool>> Build<TEntity>(
        IReadOnlyList<OrderingSpecification<TEntity>> orderingSpecifications,
        DynamicCursor lastCursor) where TEntity : class
    {
        if (orderingSpecifications.Count == 0)
            throw new InvalidOperationException("No ordering specifications were found.");

        if (orderingSpecifications.Count != lastCursor.Values.Length)
            throw new InvalidOperationException("Invalid cursor provided.");

        ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "e");
        Expression? filter = null;

        for (int i = 0; i < orderingSpecifications.Count; i++)
        {
            Expression<Func<TEntity, object>> orderSelector = orderingSpecifications[i].OrderBy;
            Expression orderMember = UnWrap(orderSelector.Body);

            if (!IsValidCursorValue(orderMember.Type, lastCursor.Values[i]))
                throw new InvalidOperationException("Invalid cursor provided.");

            orderMember = new ReplaceParameterVisitor(orderSelector.Parameters[0], parameter).Visit(orderMember);
            ConstantExpression orderValue = Expression.Constant(lastCursor.Values[i]);

            BinaryExpression comparison = BuildComparison(orderMember, orderValue, !orderingSpecifications[i].IsDescending);

            if (filter is null)
                filter = comparison;
            else
            {
                Expression prevEquals = BuildPreviousEquals(orderingSpecifications, i, lastCursor, parameter);
                filter = Expression.OrElse(filter, Expression.AndAlso(prevEquals, comparison));
            }
        }

        return Expression.Lambda<Func<TEntity, bool>>(filter!, parameter);
    }

    /// <summary>
    /// Removes any conversion wrappers from the expression body, e.g. <c>Convert(x)</c> to get the underlying member.
    /// </summary>
    /// <param name="body">The expression body to unwrap.</param>
    /// <returns>The unwrapped expression.</returns>
    private static Expression UnWrap(Expression body)
        => body is UnaryExpression { NodeType: ExpressionType.Convert } unary
            ? unary.Operand
            : body;

    /// <summary>
    /// Validates cursor value against expected type.
    /// </summary>
    /// <param name="expectedType">The type which we expect the <paramref name="cursorValue"/> to be.</param>
    /// <param name="cursorValue">The current value of cursor which is going to be validated.</param>
    /// <returns><see langword="true"/> if the <paramref name="cursorValue"/> is valid otherwise <see langword="false"/>.</returns>
    private static bool IsValidCursorValue(Type expectedType, object? cursorValue)
    {
        if (cursorValue is null)
        {
            if (expectedType.IsValueType && Nullable.GetUnderlyingType(expectedType) is null)
                return false;
        }

        if (expectedType.IsInstanceOfType(cursorValue))
            return true;

        return false;
    }

    /// <summary>
    /// Builds a comparison expression between an entity member and a constant value.
    /// </summary>
    /// <param name="orderMember">The expression representing the entity property.</param>
    /// <param name="orderValue">The constant value to compare against.</param>
    /// <param name="asc">Whether the comparison is ascending (<see langword="true"/> for ascending, <see langword="false"/> for descending).</param>
    /// <returns>A <see cref="BinaryExpression"/> representing the comparison.</returns>
    private static BinaryExpression BuildComparison(Expression orderMember, Expression orderValue, bool asc)
    {
        if (orderMember.Type == typeof(string))
        {
            MethodInfo compareMethod = typeof(string).GetMethod(nameof(string.Compare), [typeof(string), typeof(string)])!;
            MethodCallExpression compareCall = Expression.Call(compareMethod, orderMember, orderValue);

            return asc
                ? Expression.GreaterThan(compareCall, Expression.Constant(0))
                : Expression.LessThan(compareCall, Expression.Constant(0));
        }

        if (orderMember.Type == typeof(bool))
        {
            Expression entityProp = Expression.Equal(orderMember, Expression.Constant(asc));
            Expression cursorProp = Expression.Equal(orderValue, Expression.Constant(!asc));

            return Expression.And(entityProp, cursorProp);
        }

        return asc
            ? Expression.GreaterThan(orderMember, orderValue)
            : Expression.LessThan(orderMember, orderValue);
    }

    /// <summary>
    /// Builds a combined equality expression for all previous ordering fields,
    /// ensuring that the current comparison applies only when previous fields match.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being queried.</typeparam>
    /// <param name="orderingSpecifications">The collection of ordering specifications.</param>
    /// <param name="upToIndex">The index of the current ordering expression.</param>
    /// <param name="lastCursor">The values to compare against for previous fields.</param>
    /// <param name="parameter">The parameter expression for the entity.</param>
    /// <returns>An <see cref="Expression"/> representing the combined equality of previous fields.</returns>
    private static Expression BuildPreviousEquals<TEntity>(
        IReadOnlyList<OrderingSpecification<TEntity>> orderingSpecifications,
        int upToIndex,
        DynamicCursor lastCursor,
        ParameterExpression parameter)
    {
        Expression? prevEquals = null;

        for (int j = 0; j < upToIndex; j++)
        {
            Expression<Func<TEntity, object>> prevSelector = orderingSpecifications[j].OrderBy;
            InvocationExpression prevMember = Expression.Invoke(prevSelector, parameter);
            UnaryExpression prevValue = Expression.Convert(
                Expression.Constant(lastCursor.Values[j]),
                prevMember.Type
            );

            BinaryExpression eq = Expression.Equal(prevMember, prevValue);
            prevEquals = prevEquals == null ? eq : Expression.AndAlso(prevEquals, eq);
        }

        return prevEquals!;
    }
}
