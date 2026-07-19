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
    /// <inheritdoc />
    public DbContext Context { get; } = context;

    /// <inheritdoc />
    public DbSet<TEntity> Entities { get; } = context.Set<TEntity>();

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public virtual async ValueTask<TEntity?> GetByIdAsync(TKey[] id,
        CancellationToken cancellationToken = default)
    {
        return await Entities.FindAsync([..id], cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate,
        bool ignoreQueryFilters = false,
        CancellationToken cancellationToken = default)
    {
        BasicQueryBuilder<TEntity> builder = new BasicQueryBuilder<TEntity>(Entities)
            .WithIgnoreQueryFilters(ignoreQueryFilters);

        return await builder.Query.AnyAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async ValueTask<EntityEntry<TEntity>> CreateAsync(TEntity model,
        CancellationToken cancellationToken = default)
    {
        return await Entities.AddAsync(model, cancellationToken);
    }

    /// <inheritdoc />
    public virtual void Update(TEntity model)
    {
        Entities.Attach(model);
        Context.Entry(model).State = EntityState.Modified;
    }

    /// <inheritdoc />
    public virtual void Delete(TEntity model)
    {
        if (Context.Entry(model).State == EntityState.Detached)
            Entities.Attach(model);

        Entities.Remove(model);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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
