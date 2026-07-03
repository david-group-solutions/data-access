using System.Linq.Expressions;

using DavidGroup.Core.DataAccess.Pagination;
using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.DataAccess.Sql.Entities;

namespace DavidGroup.Core.DataAccess.Sql.Services;

/// <summary>
/// Defines a base service interface for readonly entity operations using DTO mapping for reading.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The entity's key type.</typeparam>
/// <typeparam name="TReadDto">The DTO type returned when reading an entity.</typeparam>
public interface IBaseReadonlyService<TEntity, in TKey, TReadDto>
    where TEntity : class, IEntity<TKey>
    where TKey : new()
    where TReadDto : class
{
    /// <summary>
    /// Retrieves all entities as read DTOs.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing a read-only collection of all entities mapped to read DTOs.
    /// </returns>
    Task<OperationResult<List<TReadDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities using offset pagination and maps them to read DTOs.
    /// </summary>
    /// <param name="options">Pagination options for offset-based pagination.</param>
    /// <param name="orderBy">Optional string specifying the column names and directions to order by. Ex. "Name asc, Date desc, Id desc"</param>
    /// <param name="allowedToOrderBy">A list of allowed expressions to restrict orderable fields. If <c>null</c>, all fields are allowed to be using in <paramref name="orderBy"/>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing a <see cref="PageData{T}"/> with paginated results mapped to read DTOs.
    /// </returns>
    Task<OperationResult<PageData<TReadDto>>> GetAllAsync(PageOptions options,
        string? orderBy = null,
        IReadOnlyList<Expression<Func<TEntity, object>>>? allowedToOrderBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities using cursor (infinite scroll) pagination and maps them to read DTOs.
    /// </summary>
    /// <param name="options">Pagination options for infinite scroll.</param>
    /// <param name="orderBy">Optional string specifying the column names and directions to order by. Ex. "Name asc, Date desc, Id desc"</param>
    /// <param name="allowedToOrderBy">A list of allowed expressions to restrict orderable fields. If <c>null</c>, all fields are allowed to be using in <paramref name="orderBy"/>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing an <see cref="InfinitePageData{T}"/> with paginated results mapped to read DTOs.
    /// </returns>
    /// <remarks>When <paramref name="orderBy"/> is <c>null</c> the primary key used for cursor.</remarks>
    Task<OperationResult<InfinitePageData<TReadDto>>> GetAllAsync(InfinitePageOptions options,
        string? orderBy = null,
        IReadOnlyList<Expression<Func<TEntity, object>>>? allowedToOrderBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single entity by its identifier and maps it to a read DTO.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/> for task cancellation.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing the read DTO if found; otherwise, an error result.
    /// </returns>
    Task<OperationResult<TReadDto>> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
}
