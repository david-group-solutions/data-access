using System.Linq.Expressions;

namespace DavidGroup.Core.DataAccess.Sql.Repositories;

/// <summary>
/// Defines common aggregation operations for entities in the repository.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IBaseAggregationRepository<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Counts the number of entities that satisfy an optional condition.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition. If <c>null</c>, counts all entities.</param>
    /// <param name="ignoreQueryFilters"><c>true</c> to disable global query filters; otherwise, <c>false</c>. Defaults to <c>false</c>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// A <see cref="Task{Int32}"/> representing the asynchronous operation.
    /// The task result contains the number of entities that satisfy the condition.
    /// </returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of entities that satisfy an optional condition, returning a <see cref="long"/> result.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition. If <c>null</c>, counts all entities.</param>
    /// <param name="ignoreQueryFilters"><c>true</c> to disable global query filters; otherwise, <c>false</c>. Defaults to <c>false</c>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// A <see cref="Task{Int64}"/> representing the asynchronous operation.
    /// The task result contains the number of entities that satisfy the condition.
    /// </returns>
    Task<long> LongCountAsync(Expression<Func<TEntity, bool>>? predicate = null,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes the average value of a sequence of entities based on the specified integer selector.
    /// </summary>
    /// <param name="selector">A function that projects each entity to an <see cref="int"/> value for averaging.</param>
    /// <param name="predicate">A function to test each element for a condition. If <c>null</c>, includes all entities.</param>
    /// <param name="ignoreQueryFilters"><c>true</c> to disable global query filters; otherwise, <c>false</c>. Defaults to <c>false</c>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// A <see cref="Task{Double}"/> representing the asynchronous operation.
    /// The task result contains the computed average value of the selected property.
    /// </returns>
    Task<double> AverageAsync(
        Expression<Func<TEntity, int>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default);
}
