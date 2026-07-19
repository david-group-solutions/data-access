using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Pagination;
using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Results;
using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.DataAccess.Sql.Builders;
using DavidGroup.Core.DataAccess.Sql.Entities;
using DavidGroup.Core.DataAccess.Sql.Repositories;
using DavidGroup.Core.Utilities.Cache;

namespace DavidGroup.Core.DataAccess.Sql.Services;

/// <summary>
/// Implementation of a base service for readonly entity operations using DTO mapping for reading.
/// </summary>
/// <param name="repository">Repository of entities.</param>
/// <typeparam name="TRepository">The repository type.</typeparam>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity's key type.</typeparam>
/// <typeparam name="TReadDto">The DTO type returned when reading an entity.</typeparam>
public abstract class BaseReadonlyService<TRepository, TEntity, TKey, TReadDto>(TRepository repository)
    : IBaseReadonlyService<TEntity, TKey, TReadDto>
    where TRepository : class, IBaseRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : new()
    where TReadDto : class
{
    /// <summary>
    /// Repository of entities.
    /// </summary>
    protected readonly TRepository Repository = repository;

    /// <summary>
    /// Expression which takes entity and return read DTO.
    /// </summary>
    protected abstract Expression<Func<TEntity, TReadDto>> ToReadDto { get; }

    /// <inheritdoc />
    public virtual async Task<OperationResult<List<TReadDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        List<TReadDto> result = await Repository.GetAllAsync(
            selector: ToReadDto,
            cancellationToken: cancellationToken
        );

        return OperationResult<List<TReadDto>>.Success(result);
    }

    /// <inheritdoc />
    public virtual async Task<OperationResult<PageData<TReadDto>>> GetAllAsync(PageOptions options,
        string? orderBy = null,
        IReadOnlyList<Expression<Func<TEntity, object>>>? allowedToOrderBy = null,
        CancellationToken cancellationToken = default)
    {
        PageData<TReadDto> result;

        if (orderBy is null)
        {
            result = await Repository.GetAllAsync(
                options,
                orderBy: null,
                selector: ToReadDto,
                cancellationToken: cancellationToken
            );
        }
        else
        {
            OperationResult<IReadOnlyList<OrderingSpecification<TEntity>>> orderingSpecificationsResult =
                OrderingSpecification<TEntity>.Parse(orderBy, allowedToOrderBy);

            if (!orderingSpecificationsResult.Succeeded)
                return OperationResult<PageData<TReadDto>>.Failure(orderingSpecificationsResult.Messages[0]);

            result = await Repository.GetAllAsync(
                options,
                orderingSpecifications: orderingSpecificationsResult.Value,
                selector: ToReadDto,
                cancellationToken: cancellationToken
            );
        }

        return OperationResult<PageData<TReadDto>>.Success(result);
    }

    /// <inheritdoc />
    public virtual async Task<OperationResult<InfinitePageData<TReadDto>>> GetAllAsync(InfinitePageOptions options,
        string? orderBy = null,
        IReadOnlyList<Expression<Func<TEntity, object>>>? allowedToOrderBy = null,
        CancellationToken cancellationToken = default)
    {
        InfinitePageData<TReadDto> result;

        if (orderBy is null)
        {
            result = await Repository.GetAllAsync(
                options,
                orderingSpecifications: [new OrderingSpecification<TEntity>(e => e.Id!, IsDescending: true)],
                selector: ToReadDto,
                cancellationToken: cancellationToken
            );
        }
        else
        {
            OperationResult<IReadOnlyList<OrderingSpecification<TEntity>>> orderingSpecificationsResult =
                OrderingSpecification<TEntity>.Parse(orderBy, allowedToOrderBy);

            if (!orderingSpecificationsResult.Succeeded)
                return OperationResult<InfinitePageData<TReadDto>>.Failure(orderingSpecificationsResult.Messages[0]);

            result = await Repository.GetAllAsync(
                options,
                orderingSpecifications: orderingSpecificationsResult.Value,
                selector: ToReadDto,
                cancellationToken: cancellationToken
            );
        }

        return OperationResult<InfinitePageData<TReadDto>>.Success(result);
    }

    /// <inheritdoc />
    public virtual async Task<OperationResult<TReadDto>> GetByIdAsync(TKey id,
        CancellationToken cancellationToken = default)
    {
        TEntity? entity = await Repository.GetByIdAsync([id], cancellationToken);

        if (entity is null)
        {
            return OperationResult<TReadDto>.Failure(
                new OperationResultMessage(ErrorMessages.NotFound, OperationResultSeverity.Error));
        }

        TReadDto readDto = InMemoryCompiledExpressionsCache.StoreOrRetrieve(ToReadDto).Invoke(entity);

        return OperationResult<TReadDto>.Success(readDto);
    }
}
