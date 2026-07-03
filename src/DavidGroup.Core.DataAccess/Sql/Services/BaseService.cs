using DavidGroup.Core.DataAccess.Results;
using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.DataAccess.Sql.Entities;
using DavidGroup.Core.DataAccess.Sql.Repositories;
using DavidGroup.Core.DataAccess.Sql.UnitOfWork.EFCore;
using DavidGroup.Core.Utilities.Cache;

using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccess.Sql.Services;

/// <summary>
/// Implements a base service for entity operations using DTO mapping for creation, reading and updating.
/// </summary>
/// <param name="repository">Repository of entities.</param>
/// <param name="unitOfWork">EF Unit of Work instance.</param>
/// <typeparam name="TDbContext">Database context.</typeparam>
/// <typeparam name="TRepository">The repository type.</typeparam>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity's key type.</typeparam>
/// <typeparam name="TCreateModel">The model type used for creating an entity.</typeparam>
/// <typeparam name="TUpdateModel">The model type used for updating an entity.</typeparam>
/// <typeparam name="TReadDto">The DTO type returned when reading an entity.</typeparam>
public abstract class BaseService<TDbContext, TRepository, TEntity, TKey, TCreateModel, TUpdateModel, TReadDto>(
    TRepository repository,
    IEfUnitOfWork<TDbContext> unitOfWork)
    : BaseReadonlyService<TRepository, TEntity, TKey, TReadDto>(repository),
        IBaseService<TEntity, TKey, TCreateModel, TUpdateModel, TReadDto>
    where TDbContext : DbContext
    where TRepository : class, IBaseRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, ISelfManageable<TEntity, TCreateModel, TUpdateModel>
    where TKey : new()
    where TCreateModel : class
    where TUpdateModel : class
    where TReadDto : class
{
    /// <summary>
    /// EF Unit of Work instance.
    /// </summary>
    protected readonly IEfUnitOfWork<TDbContext> UnitOfWork = unitOfWork;

    /// <summary>
    /// Creates a new entity from a creation DTO and returns the resulting read DTO.
    /// </summary>
    /// <param name="model">The model containing data for the new entity.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing the created read DTO if successful.
    /// </returns>
    public virtual async Task<OperationResult<TReadDto>> CreateAsync(TCreateModel model,
        CancellationToken cancellationToken = default)
    {
        TEntity entity = TEntity.Create(model);

        await Repository.CreateAsync(entity, cancellationToken);
        await UnitOfWork.SaveAsync(cancellationToken);

        TReadDto readDto = InMemoryCompiledExpressionsCache.StoreOrRetrieve(ToReadDto).Invoke(entity);

        return OperationResult<TReadDto>.Success(readDto);
    }

    /// <summary>
    /// Updates an existing entity using an update DTO and returns a read DTO.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="model">The update model containing modified data.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing the updated read DTO if successful.
    /// </returns>
    public virtual async Task<OperationResult<TReadDto>> UpdateAsync(TKey id,
        TUpdateModel model,
        CancellationToken cancellationToken = default)
    {
        TEntity? entity = await Repository.GetByIdAsync([id], cancellationToken);

        if (entity is null)
        {
            return OperationResult<TReadDto>.Failure(
                new OperationResultMessage(ErrorMessages.NotFound, OperationResultSeverity.Error));
        }

        entity.Update(model);

        Repository.Update(entity);
        await UnitOfWork.SaveAsync(cancellationToken);

        TReadDto readDto = InMemoryCompiledExpressionsCache.StoreOrRetrieve(ToReadDto).Invoke(entity);

        return OperationResult<TReadDto>.Success(readDto);
    }

    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// An <see cref="OperationResult"/> representing the outcome of the delete operation.
    /// </returns>
    public virtual async Task<OperationResult> DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        TEntity? entity = await Repository.GetByIdAsync([id], cancellationToken);

        if (entity is null)
        {
            return OperationResult<TReadDto>.Failure(
                new OperationResultMessage(ErrorMessages.NotFound, OperationResultSeverity.Error));
        }

        await Repository.DeleteAsync(entity.Id, cancellationToken: cancellationToken);
        await UnitOfWork.SaveAsync(cancellationToken);

        return OperationResult.Success();
    }
}
