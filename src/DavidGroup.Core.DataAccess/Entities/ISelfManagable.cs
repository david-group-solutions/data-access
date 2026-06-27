namespace DavidGroup.Core.DataAccess.Entities;

/// <summary>
/// Defines a contract for entities that can manage their own creation and update logic
/// using the same entity type as input.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements this interface.</typeparam>
public interface ISelfManageable<TEntity>
{
    /// <summary>
    /// Creates a new instance of the entity based on the provided model.
    /// </summary>
    /// <param name="model">The source model used to initialize the entity.</param>
    /// <returns>A new instance of <typeparamref name="TEntity"/> initialized from <paramref name="model"/>.</returns>
    static abstract TEntity Create(TEntity model);


    /// <summary>
    /// Updates the current entity's state based on the provided model.
    /// </summary>
    /// <param name="model">The model containing updated values for this entity.</param>
    void Update(TEntity model);
}

/// <summary>
/// Defines a contract for entities that can manage their own creation and update logic
/// using distinct model types for creation and update operations.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements this interface.</typeparam>
/// <typeparam name="TCreateModel">The model type used for creating a new entity instance.</typeparam>
/// <typeparam name="TUpdateModel">The model type used for updating an existing entity instance.</typeparam>
public interface ISelfManageable<out TEntity, in TCreateModel, in TUpdateModel>
{
    /// <summary>
    /// Creates a new instance of the entity based on the provided creation model.
    /// </summary>
    /// <param name="model">The model containing initial data for the entity.</param>
    /// <returns>A new instance of <typeparamref name="TEntity"/> initialized from <paramref name="model"/>.</returns>
    static abstract TEntity Create(TCreateModel model);

    /// <summary>
    /// Updates the current entity's state based on the provided update model.
    /// </summary>
    /// <param name="model">The model containing updated data for this entity.</param>
    void Update(TUpdateModel model);
}
