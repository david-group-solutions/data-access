using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Pagination;
using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Sql.Builders;
using DavidGroup.Core.DataAccess.Sql.Builders.BasicQuery;
using DavidGroup.Core.DataAccess.Sql.Builders.InfiniteScrollPaginationQuery;
using DavidGroup.Core.DataAccess.Sql.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;

namespace DavidGroup.Core.DataAccess.Sql.Repositories;

/// <summary>
/// Defines common operations for entities in the repository.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The key type of entity</typeparam>
public abstract class BaseRepository<TEntity, TKey>(DbContext context)
    : IBaseRepository<TEntity, TKey>, IBaseAggregationRepository<TEntity>
    where TEntity : class, IEntity<TKey>
{
    /// <summary>
    /// EF database context instance.
    /// </summary>
    public DbContext Context { get; } = context;

    /// <summary>
    /// EF DbSet of TEntities.
    /// </summary>
    public DbSet<TEntity> Entities { get; } = context.Set<TEntity>();

    /// <summary>
    /// Gets all entities.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="orderBy">A function to order elements.</param>
    /// <param name="include">A function to include navigation properties.</param>
    /// <param name="selector">The selector for projection. Defaults to <c>e => e</c>.</param>
    /// <param name="disableTracking"><c>True</c> to disable changing tracking; otherwise, <c>false</c>. Default to <c>true</c>.</param>
    /// <param name="ignoreQueryFilters"><c>True</c> to disable query filters; otherwise, <c>false</c>. Default to <c>false</c>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// A <see cref="List{TResult}" /> that contains results.
    /// </returns>
    /// <remarks>This method executes a no-tracking query.</remarks>
    /// <remarks>This method executes a no-tracking query and does not ignore query filters by default.</remarks>
    public virtual async Task<List<TResult>> GetAllAsync<TResult>(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        Expression<Func<TEntity, TResult>>? selector = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
        where TResult : class
    {
        BasicQueryBuilder<TResult> builder = new BasicQueryBuilder<TEntity>(Entities)
            .WithTracking(!disableTracking)
            .WithIgnoreQueryFilters(ignoreQueryFilters)
            .WithInclude(include)
            .WithPredicate(predicate)
            .WithOrdering(orderBy)
            .WithProjection(selector);

        return await builder.Query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all entities using offset pagination.
    /// </summary>
    /// <param name="options">PaginationOptions to paginate result.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="orderBy">A function to order elements.</param>
    /// <param name="include">A function to include navigation properties.</param>
    /// <param name="selector">The selector for projection. Defaults to <c>e => e</c>.</param>
    /// <param name="disableTracking"><c>True</c> to disable changing tracking; otherwise, <c>false</c>. Default to <c>true</c>.</param>
    /// <param name="ignoreQueryFilters"><c>True</c> to disable query filters; otherwise, <c>false</c>. Default to <c>false</c>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// An <see cref="PageData{T}" /> that contains results. Additionally, it has metadata fields.
    /// </returns>
    /// <remarks>This method executes a no-tracking query.</remarks>
    /// <remarks>This method executes a no-tracking query and does not ignore query filters by default.</remarks>
    public virtual async Task<PageData<TResult>> GetAllAsync<TResult>(
        PageOptions options,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        Expression<Func<TEntity, TResult>>? selector = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
        where TResult : class
    {
        BasicQueryBuilder<TResult> builder = new BasicQueryBuilder<TEntity>(Entities)
            .WithTracking(!disableTracking)
            .WithIgnoreQueryFilters(ignoreQueryFilters)
            .WithInclude(include)
            .WithPredicate(predicate)
            .WithOrdering(orderBy)
            .WithProjection(selector);

        int count = await builder.Query.CountAsync(cancellationToken);

        List<TResult> entities = await builder.WithOffsetPagination(options).Query.ToListAsync(cancellationToken);

        return new PageData<TResult>(entities, count, options);
    }

    /// <summary>
    /// Gets all entities using offset pagination. Ordering instructions are passed with a collection
    /// of <see cref="OrderingSpecification{TEntity}"/>.
    /// </summary>
    /// <param name="options">PaginationOptions to paginate result.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="orderingSpecifications">A collection which represents ordering specifications.</param>
    /// <param name="include">A function to include navigation properties.</param>
    /// <param name="selector">The selector for projection. Defaults to <c>e => e</c>.</param>
    /// <param name="disableTracking"><c>True</c> to disable changing tracking; otherwise, <c>false</c>. Default to <c>true</c>.</param>
    /// <param name="ignoreQueryFilters"><c>True</c> to disable query filters; otherwise, <c>false</c>. Default to <c>false</c>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// An <see cref="PageData{T}" /> that contains results. Additionally, it has metadata fields.
    /// </returns>
    /// <remarks>This method executes a no-tracking query.</remarks>
    /// <remarks>This method executes a no-tracking query and does not ignore query filters by default.</remarks>
    public virtual async Task<PageData<TResult>> GetAllAsync<TResult>(
        PageOptions options,
        Expression<Func<TEntity, bool>>? predicate = null,
        IReadOnlyList<OrderingSpecification<TEntity>>? orderingSpecifications = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        Expression<Func<TEntity, TResult>>? selector = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
        where TResult : class
    {
        BasicQueryBuilder<TResult> builder = new BasicQueryBuilder<TEntity>(Entities)
            .WithTracking(!disableTracking)
            .WithIgnoreQueryFilters(ignoreQueryFilters)
            .WithInclude(include)
            .WithPredicate(predicate)
            .WithOrdering(orderingSpecifications)
            .WithProjection(selector);

        int count = await builder.Query.CountAsync(cancellationToken);

        List<TResult> entities = await builder.WithOffsetPagination(options).Query.ToListAsync(cancellationToken);

        return new PageData<TResult>(entities, count, options);
    }

    /// <summary>
    /// Gets all entities using cursor (infinite scroll) pagination.
    /// </summary>
    /// <param name="options">InfinitePaginationOptions to paginate result.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="orderingSpecifications">A collection which represents ordering specifications.</param>
    /// <param name="include">A function to include navigation properties.</param>
    /// <param name="selector">The selector for projection. Defaults to <c>e => e</c>.</param>
    /// <param name="nextCursorSelector">
    /// The selector for next cursor. By default, it will determine it automatically but execute the second SQL query,
    /// in order to increase performance you must specify it manually.
    /// </param>
    /// <param name="disableTracking"><c>True</c> to disable changing tracking; otherwise, <c>false</c>. Default to <c>true</c>.</param>
    /// <param name="ignoreQueryFilters"><c>True</c> to disable query filters; otherwise, <c>false</c>. Default to <c>false</c>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// An <see cref="InfinitePageData{T}" /> that contains results and next cursor which is presented and in array of
    /// objects and in base64 encoded token. Additionally, it has metadata fields.
    /// </returns>
    /// <remarks>This method executes a no-tracking query and does not ignore query filters by default.</remarks>
    public virtual async Task<InfinitePageData<TResult>> GetAllAsync<TResult>(
        InfinitePageOptions options,
        IReadOnlyList<OrderingSpecification<TEntity>> orderingSpecifications,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        Expression<Func<TEntity, TResult>>? selector = null,
        Func<TResult, object[]>? nextCursorSelector = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
        where TResult : class
    {
        if (orderingSpecifications.Count == 0)
            throw new ArgumentException("At least one ordering selector must be provided.", nameof(orderingSpecifications));

        InfiniteScrollPaginationQueryBuilder<TEntity>? builder =
            new InfiniteScrollPaginationQueryBuilder<TEntity>(Entities)
                .WithTracking(!disableTracking)
                .WithIgnoreQueryFilters(ignoreQueryFilters)
                .WithInclude(include)
                .WithPredicate(predicate) as InfiniteScrollPaginationQueryBuilder<TEntity>;

        return await builder!.ExecuteWithCursorPagination(options,
            orderingSpecifications,
            selector,
            nextCursorSelector,
            cancellationToken);
    }

    /// <summary>
    /// Gets the first or default entity.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="orderBy">A function to order elements.</param>
    /// <param name="include">A function to include navigation properties</param>
    /// <param name="selector">The selector for projection. Defaults to <c>e => e</c>.</param>
    /// <param name="disableTracking"><c>True</c> to disable changing tracking; otherwise, <c>false</c>. Default to <c>true</c>.</param>
    /// <param name="ignoreQueryFilters"><c>True</c> to disable query filters; otherwise, <c>false</c>. Default to <c>false</c>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>A <see><cref>{TResult?}</cref></see> element or null nothing found.</returns>
    /// <remarks>This method executes a no-tracking query and does not ignore query filters by default.</remarks>
    public virtual async Task<TResult?> FirstOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object?>>? include = null,
        Expression<Func<TEntity, TResult>>? selector = null,
        bool disableTracking = true,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
        where TResult : class
    {
        BasicQueryBuilder<TResult> builder = new BasicQueryBuilder<TEntity>(Entities)
            .WithTracking(!disableTracking)
            .WithIgnoreQueryFilters(ignoreQueryFilters)
            .WithInclude(include)
            .WithPredicate(predicate)
            .WithOrdering(orderBy)
            .WithProjection(selector);

        return await builder.Query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Gets an entity by its primary key.
    /// </summary>
    /// <param name="id">An array representing the entity's key values.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>A <see cref="ValueTask{TEntity}"/> representing the asynchronous operation, containing the entity or <c>null</c> if not found.</returns>
    public virtual async ValueTask<TEntity?> GetByIdAsync(TKey[] id,
        CancellationToken cancellationToken = default)
    {
        return await Entities.FindAsync([..id], cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Determines whether any entities satisfy the specified condition.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="ignoreQueryFilters"><c>true</c> to disable query filters; otherwise, <c>false</c>. Defaults to <c>false</c>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns><c>true</c> if any elements satisfy the condition; otherwise, <c>false</c>.</returns>
    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
    {
        BasicQueryBuilder<TEntity> builder = new BasicQueryBuilder<TEntity>(Entities)
            .WithIgnoreQueryFilters(ignoreQueryFilters);

        return await builder.Query.AnyAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Adds a new entity to the context asynchronously.
    /// </summary>
    /// <param name="model">The entity to add.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>The <see cref="EntityEntry{TEntity}"/> representing the added entity.</returns>
    public virtual async ValueTask<EntityEntry<TEntity>> CreateAsync(TEntity model,
        CancellationToken cancellationToken = default)
    {
        return await Entities.AddAsync(model, cancellationToken);
    }

    /// <summary>
    /// Updates an existing entity in the context.
    /// </summary>
    /// <param name="model">The entity to update.</param>
    public virtual void Update(TEntity model)
    {
        Entities.Attach(model);
        Context.Entry(model).State = EntityState.Modified;
    }

    /// <summary>
    /// Marks an entity as deleted. If not attached, it will attach it.
    /// </summary>
    /// <param name="model">The entity to delete.</param>
    public virtual void Delete(TEntity model)
    {
        if (Context.Entry(model).State == EntityState.Detached)
            Entities.Attach(model);

        Entities.Remove(model);
    }

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
    public virtual async Task<bool> DeleteAsync(TKey id,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
    {
        TEntity? entityToDelete = await Entities
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(e => id!.Equals(e.Id), cancellationToken);

        if (entityToDelete is null) return false;

        Delete(entityToDelete);

        return true;
    }

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
    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
    {
        BasicQueryBuilder<TEntity> builder = new BasicQueryBuilder<TEntity>(Entities)
            .WithTracking(false)
            .WithIgnoreQueryFilters(ignoreQueryFilters)
            .WithPredicate(predicate);

        return await builder.Query.CountAsync(cancellationToken);
    }

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
    public virtual async Task<long> LongCountAsync(Expression<Func<TEntity, bool>>? predicate = null,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
    {
        BasicQueryBuilder<TEntity> builder = new BasicQueryBuilder<TEntity>(Entities)
            .WithTracking(false)
            .WithIgnoreQueryFilters(ignoreQueryFilters)
            .WithPredicate(predicate);

        return await builder.Query.LongCountAsync(cancellationToken);
    }

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
    public virtual async Task<double> AverageAsync(
        Expression<Func<TEntity, int>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
    {
        BasicQueryBuilder<TEntity> builder = new BasicQueryBuilder<TEntity>(Entities)
            .WithTracking(false)
            .WithIgnoreQueryFilters(ignoreQueryFilters)
            .WithPredicate(predicate);

        return await builder.Query.AverageAsync(selector, cancellationToken);
    }
}
