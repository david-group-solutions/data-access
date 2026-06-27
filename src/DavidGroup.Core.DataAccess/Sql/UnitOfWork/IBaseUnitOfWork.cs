namespace DavidGroup.Core.DataAccess.Sql.UnitOfWork;

/// <summary>
/// Defines the contract for implementing the Unit of Work pattern,
/// providing methods to manage database transactions in a consistent and atomic manner.
/// </summary>
/// <remarks>
/// This interface abstracts transaction handling logic to ensure that a set of operations
/// can be committed or rolled back as a single unit of work.
/// Implementations are typically scoped per request in data-driven applications.
/// </remarks>
public interface IBaseUnitOfWork
{
    /// <summary>
    /// Begins a new database transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method should be called before performing a series of operations
    /// that must either all succeed or all fail together.
    /// </remarks>
    Task CreateTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Commits the current transaction asynchronously, finalizing all changes made
    /// during the unit of work.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// Once committed, the transaction cannot be rolled back.  
    /// Call this method only after all operations within the unit of work have succeeded.
    /// </remarks>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rolls back the current transaction asynchronously, reverting all changes
    /// made during the unit of work.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method should be invoked when an error occurs or when an operation fails,
    /// to ensure data consistency by undoing pending changes.
    /// </remarks>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}