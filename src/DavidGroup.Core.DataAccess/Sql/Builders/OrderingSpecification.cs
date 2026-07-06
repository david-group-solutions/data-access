using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Results;
using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.Utilities.Cache;
using DavidGroup.Core.Utilities.Expressions;

namespace DavidGroup.Core.DataAccess.Sql.Builders;

/// <summary>
/// Provides specifications of ordering with functionality to dynamically construct ordering
/// expressions based on a textual representation of sorting parameters.
/// </summary>
/// <param name="OrderBy">Expression providing the property which should be used to order by.</param>
/// <param name="IsDescending">The direction of ordering.</param>
/// <typeparam name="TEntity">The type which must be used in ordering.</typeparam>
/// <remarks>
/// <para>
/// This approach allows dynamic sorting without relying on reflection at runtime,
/// while maintaining compatibility with Entity Framework Core's expression translation.
/// </para>
/// </remarks>
public record OrderingSpecification<TEntity>(
    Expression<Func<TEntity, object>> OrderBy,
    bool IsDescending
)
{
    /// <summary>
    /// Builds a collection of ordering expressions and direction flags
    /// based on a string-based <paramref name="orderBy"/> definition.
    /// </summary>
    /// <param name="orderBy">
    /// A comma-separated list of property names, optionally followed by
    /// <c>"desc"</c> to indicate descending order.
    /// For example: <c>"Name desc, CreatedAtUtc, Address.City desc"</c>.
    /// </param>
    /// <param name="allowedProperties">
    /// An optional list of property expressions that define which entity properties are allowed
    /// to be used for ordering. If <see langword="null"/>, all entity properties are considered valid.
    /// </param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing the parsed ordering specifications
    /// on success, or one or more error messages describing why parsing failed.
    /// </returns>
    public static OperationResult<IReadOnlyList<OrderingSpecification<TEntity>>> Parse(string orderBy,
        IReadOnlyList<Expression<Func<TEntity, object>>>? allowedProperties)
    {
        List<OrderingSpecification<TEntity>> orderingSpecifications = [];

        HashSet<string>? entityProps = allowedProperties is null
            ? InMemoryTypePropertiesCache.StoreOrRetrieve<TEntity>()
            : null;

        ParameterExpression parameter = Expression.Parameter(typeof(TEntity), "e");
        IEnumerable<string> orderParams = orderBy.Trim().Split(',').Select(p => p.Trim());

        foreach (string param in orderParams)
        {
            if (string.IsNullOrWhiteSpace(param))
                continue;

            string orderByProperty = param.Split(' ')[0];

            if (allowedProperties is not null)
            {
                bool propertyAllowed =
                    allowedProperties.Any(a => ExpressionsHelper.GetPropertyPath(a) == orderByProperty);

                if (!propertyAllowed)
                {
                    return OperationResult<IReadOnlyList<OrderingSpecification<TEntity>>>.Failure(
                        new OperationResultMessage(
                            $"Ordering parameter '{orderByProperty}' is not allowed.",
                            OperationResultSeverity.Error
                        )
                    );
                }
            }
            else
            {
                bool fieldExists = entityProps!.Contains(orderByProperty);
                if (!fieldExists)
                {
                    return OperationResult<IReadOnlyList<OrderingSpecification<TEntity>>>.Failure(
                        new OperationResultMessage(
                            $"Field '{orderByProperty}' does not not exist.",
                            OperationResultSeverity.Error
                        )
                    );
                }
            }

            Expression propertyExpression = orderByProperty
                .Split('.')
                .Aggregate<string?, Expression>(parameter, Expression.PropertyOrField!);

            UnaryExpression propertyExpressionObject = Expression.Convert(propertyExpression, typeof(object));
            Expression<Func<TEntity, object>> lambda = Expression.Lambda<Func<TEntity, object>>(propertyExpressionObject, parameter);

            orderingSpecifications.Add(new OrderingSpecification<TEntity>(lambda, param.EndsWith(" desc")));
        }

        return OperationResult<IReadOnlyList<OrderingSpecification<TEntity>>>.Success(orderingSpecifications);
    }

    /// <summary>
    /// Applies multiple ordering expressions to the specified <see cref="IQueryable{T}"/>.
    /// </summary>
    /// <param name="query">The query to apply ordering to.</param>
    /// <param name="orderBySpecifications">A readonly list of <see cref="OrderingSpecification{TEntity}"/> specifying the properties to order by.</param>
    /// <returns>An <see cref="IOrderedQueryable{TEntity}"/> representing the ordered query.</returns>
    /// <remarks>
    /// This method supports multi-level ordering: the first expression determines the primary order,
    /// subsequent expressions are applied as secondary, tertiary, etc. ordering using <c>ThenBy</c> or <c>ThenByDescending</c>.
    /// </remarks>
    public static IOrderedQueryable<TEntity> Apply(IQueryable<TEntity> query,
        IReadOnlyList<OrderingSpecification<TEntity>> orderBySpecifications)
    {
        if (orderBySpecifications.Count == 0)
            throw new InvalidOperationException("No ordering specifications were found.");

        IOrderedQueryable<TEntity> ordered = null!;

        for (int i = 0; i < orderBySpecifications.Count; i++)
        {
            if (i == 0)
            {
                ordered = !orderBySpecifications[i].IsDescending
                    ? query.OrderBy(orderBySpecifications[i].OrderBy)
                    : query.OrderByDescending(orderBySpecifications[i].OrderBy);
            }
            else
            {
                ordered = !orderBySpecifications[i].IsDescending
                    ? ordered.ThenBy(orderBySpecifications[i].OrderBy)
                    : ordered.ThenByDescending(orderBySpecifications[i].OrderBy);
            }
        }

        return ordered;
    }
}
