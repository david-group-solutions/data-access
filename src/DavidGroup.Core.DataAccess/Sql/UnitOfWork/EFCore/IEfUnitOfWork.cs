using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DavidGroup.Core.DataAccess.Sql.UnitOfWork.EFCore;

/// <summary>
/// Defines the contract for an Entity Framework Core implementation of the Unit of Work pattern.
/// </summary>
/// <typeparam name="TContext">
/// The type of the <see cref="DbContext"/> used for managing database operations.
/// </typeparam>
/// <remarks>
/// This interface extends <see cref="IBaseUnitOfWork"/> to include EF Core–specific functionality,
/// such as direct access to the <see cref="DbContext"/> instance and the active transaction object.
/// It provides mechanisms for managing transactions and persisting changes to the database
/// in a consistent and atomic way.
/// </remarks>
public interface IEfUnitOfWork<out TContext> : IBaseUnitOfWork
    where TContext : DbContext
{
    /// <summary>
    /// Gets the <see cref="DbContext"/> instance associated with the current unit of work.
    /// </summary>
    /// <remarks>
    /// The context represents the session with the database and is responsible for tracking
    /// entity changes, executing queries, and managing persistence.
    /// </remarks>
    TContext Context { get; }
    
    /// <summary>
    /// Gets the currently active database transaction if one exists.
    /// </summary>
    /// <remarks>
    /// This property may return <see langword="null"/> if a transaction has not been started
    /// using <see cref="IBaseUnitOfWork.CreateTransactionAsync(CancellationToken)"/>.
    /// </remarks>
    IDbContextTransaction? Transaction { get; }

    /// <summary>
    /// Saves all pending changes tracked by the current <see cref="DbContext"/> to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    /// <remarks>
    /// This method should be called to persist changes.
    /// </remarks>
    Task SaveAsync(CancellationToken cancellationToken = default);
}
