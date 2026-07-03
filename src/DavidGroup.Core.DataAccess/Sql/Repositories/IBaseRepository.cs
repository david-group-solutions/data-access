using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Pagination;
using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Sql.Builders;
using DavidGroup.Core.DataAccess.Sql.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;

namespace DavidGroup.Core.DataAccess.Sql.Repositories;

/// <summary>
/// Base contract of the repository containing CRUD operations.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TKey"></typeparam>
public interface IBaseRepository<TEntity, in TKey>
    where TEntity : class, IEntity<TKey>
{
    /// <summary>
    /// EF database context instance.
    /// </summary>
    DbContext Context { get; }

    /// <summary>
    /// EF DbSet of TEntities.
    /// </summary>
    DbSet<TEntity> Entities { get; }

    /// <summary>
    /// Gets all entities.
    /// </summary>
    /// <param name="selector">The selector for projection.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="orderBy">A function to order elements.</param>
    /// <param name="include">A function to include navigation properties.</param>
    /// <param name="disableTracking"><c>True</c> to disable changing tracking; otherwise, <c>false</c>. Default to <c>true</c>.</param>
    /// <param name="ignoreQueryFilters"><c>True</c> to disable query filters; otherwise, <c>false</c>. Default to <c>false</c>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// A <see cref="List{TResult}" /> that contains results.
    /// </returns>
    /// <remarks>This method executes a no-tracking query.</remarks>
    /// <remarks>This method executes a no-tracking query and does not ignore query filters by default.</remarks>
    Task<List<TResult>> GetAllAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
        where TResult : class;

    /// <summary>
    /// Gets all entities using offset pagination.
    /// </summary>
    /// <param name="options">PaginationOptions to paginate result.</param>
    /// <param name="selector">The selector for projection.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="orderBy">A function to order elements.</param>
    /// <param name="include">A function to include navigation properties.</param>
    /// <param name="disableTracking"><c>True</c> to disable changing tracking; otherwise, <c>false</c>. Default to <c>true</c>.</param>
    /// <param name="ignoreQueryFilters"><c>True</c> to disable query filters; otherwise, <c>false</c>. Default to <c>false</c>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// An <see cref="PageData{T}" /> that contains results. Additionally, it has metadata fields.
    /// </returns>
    /// <remarks>This method executes a no-tracking query.</remarks>
    /// <remarks>This method executes a no-tracking query and does not ignore query filters by default.</remarks>
    Task<PageData<TResult>> GetAllAsync<TResult>(
        PageOptions options,
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
        where TResult : class;

    /// <summary>
    /// Gets all entities using offset pagination. Ordering instructions are passed with a collection
    /// of <see cref="OrderingSpecification{TEntity}"/>.
    /// </summary>
    /// <param name="options">PaginationOptions to paginate result.</param>
    /// <param name="selector">The selector for projection.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="orderingSpecifications">A collection which represents ordering specifications.</param>
    /// <param name="include">A function to include navigation properties.</param>
    /// <param name="disableTracking"><c>True</c> to disable changing tracking; otherwise, <c>false</c>. Default to <c>true</c>.</param>
    /// <param name="ignoreQueryFilters"><c>True</c> to disable query filters; otherwise, <c>false</c>. Default to <c>false</c>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// An <see cref="PageData{T}" /> that contains results. Additionally, it has metadata fields.
    /// </returns>
    /// <remarks>This method executes a no-tracking query.</remarks>
    /// <remarks>This method executes a no-tracking query and does not ignore query filters by default.</remarks>
    Task<PageData<TResult>> GetAllAsync<TResult>(
        PageOptions options,
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        IReadOnlyList<OrderingSpecification<TEntity>>? orderingSpecifications = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
        where TResult : class;

    /// <summary>
    /// Gets all entities using cursor (infinite scroll) pagination.
    /// </summary>
    /// <param name="options">InfinitePaginationOptions to paginate result.</param>
    /// <param name="selector">The selector for projection.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="orderingSpecifications">A collection which represents ordering specifications.</param>
    /// <param name="include">A function to include navigation properties.</param>
    /// <param name="disableTracking"><c>True</c> to disable changing tracking; otherwise, <c>false</c>. Default to <c>true</c>.</param>
    /// <param name="ignoreQueryFilters"><c>True</c> to disable query filters; otherwise, <c>false</c>. Default to <c>false</c>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// An <see cref="InfinitePageData{T}" /> that contains results and next cursor which is presented and in array of
    /// objects and in base64 encoded token. Additionally, it has metadata fields.
    /// </returns>
    /// <remarks>This method executes a no-tracking query and does not ignore query filters by default.</remarks>
    Task<InfinitePageData<TResult>> GetAllAsync<TResult>(
        InfinitePageOptions options,
        IReadOnlyList<OrderingSpecification<TEntity>> orderingSpecifications,
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
        where TResult : class;

    /// <summary>
    /// Gets the first or default entity.
    /// </summary>
    /// <param name="selector">The selector for projection.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="orderBy">A function to order elements.</param>
    /// <param name="include">A function to include navigation properties</param>
    /// <param name="disableTracking"><c>True</c> to disable changing tracking; otherwise, <c>false</c>. Default to <c>true</c>.</param>
    /// <param name="ignoreQueryFilters"><c>True</c> to disable query filters; otherwise, <c>false</c>. Default to <c>false</c>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>A <see><cref>{TResult?}</cref></see> element or null nothing found.</returns>
    /// <remarks>This method executes a no-tracking query and does not ignore query filters by default.</remarks>
    Task<TResult?> FirstOrDefaultAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
        where TResult : class;

    /// <summary>
    /// Gets an entity by its primary key.
    /// </summary>
    /// <param name="id">An array representing the entity's key values.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>A <see cref="ValueTask{TEntity}"/> representing the asynchronous operation, containing the entity or <c>null</c> if not found.</returns>
    ValueTask<TEntity?> GetByIdAsync(TKey[] id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether any entities satisfy the specified condition.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="ignoreQueryFilters"><c>true</c> to disable query filters; otherwise, <c>false</c>. Defaults to <c>false</c>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns><c>true</c> if any elements satisfy the condition; otherwise, <c>false</c>.</returns>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the context asynchronously.
    /// </summary>
    /// <param name="model">The entity to add.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>The <see cref="EntityEntry{TEntity}"/> representing the added entity.</returns>
    ValueTask<EntityEntry<TEntity>> CreateAsync(TEntity model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the context.
    /// </summary>
    /// <param name="model">The entity to update.</param>
    void Update(TEntity model);

    /// <summary>
    /// Marks an entity as deleted. If not attached, it will attach it.
    /// </summary>
    /// <param name="model">The entity to delete.</param>
    void Delete(TEntity model);

    /// <summary>
    /// Deletes an entity asynchronously by its primary key.
    /// </summary>
    /// <param name="id">The identifier of the entity to delete.</param>
    /// <param name="ignoreQueryFilters"><c>true</c> to disable query filters; otherwise, <c>false</c>. Defaults to <c>false</c>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// A <see cref="Task{Boolean}"/> representing the asynchronous operation.
    /// Returns <c>true</c> if the entity was found and deleted successfully; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> DeleteAsync(TKey id,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default);
}
