using DavidGroup.Core.DataAccess.Results;
using DavidGroup.Core.DataAccess.Results.Generic;

namespace DavidGroup.Core.DataAccess.Sql.Entities;

/// <summary>
/// Defines an entity that can create and update itself using dedicated models
/// and return the result of each operation.
/// </summary>
/// <typeparam name="TEntity">The entity type created by this interface.</typeparam>
/// <typeparam name="TCreateModel">The model type used for creating a new entity instance.</typeparam>
/// <typeparam name="TUpdateModel">The model type used for updating an existing entity instance.</typeparam>
public interface ISelfManageableWithResult<TEntity, in TCreateModel, in TUpdateModel>
{
    /// <summary>
    /// Creates a new instance of the entity based on the provided creation model.
    /// </summary>
    /// <param name="model">The model containing initial data for the entity.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing the newly created entity,
    /// or information about why the operation failed.
    /// </returns>
    static abstract OperationResult<TEntity> Create(TCreateModel model);

    /// <summary>
    /// Updates the current entity's state based on the provided update model.
    /// </summary>
    /// <param name="model">The model containing updated data for this entity.</param>
    /// <returns>
    /// An <see cref="OperationResult"/> indicating whether the update succeeded
    /// or providing information about why the operation failed.
    /// </returns>
    OperationResult Update(TUpdateModel model);
}
