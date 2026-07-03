using DavidGroup.Core.DataAccess.Results;
using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.DataAccess.Sql.Entities;

namespace DavidGroup.Core.DataAccess.Sql.Services;

/// <summary>
/// Defines a base service interface for entity operations using DTO mapping for creation, reading and updating.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity's key type.</typeparam>
/// <typeparam name="TCreateModel">The model type used for creating an entity.</typeparam>
/// <typeparam name="TUpdateModel">The model type used for updating an entity.</typeparam>
/// <typeparam name="TReadDto">The DTO type returned when reading an entity.</typeparam>
public interface IBaseService<TEntity, in TKey, in TCreateModel, in TUpdateModel, TReadDto> : IBaseReadonlyService<TEntity, TKey, TReadDto>
    where TEntity : class, IEntity<TKey>
    where TKey : new()
    where TCreateModel : class
    where TUpdateModel : class
    where TReadDto : class
{
    /// <summary>
    /// Creates a new entity from a creation DTO and returns the resulting read DTO.
    /// </summary>
    /// <param name="model">The model containing data for the new entity.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing the created read DTO if successful.
    /// </returns>
    Task<OperationResult<TReadDto>> CreateAsync(TCreateModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity using an update DTO and returns a read DTO.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="model">The update model containing modified data.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing the updated read DTO if successful.
    /// </returns>
    Task<OperationResult<TReadDto>> UpdateAsync(TKey id, TUpdateModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// An <see cref="OperationResult"/> representing the outcome of the delete operation.
    /// </returns>
    Task<OperationResult> DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}
