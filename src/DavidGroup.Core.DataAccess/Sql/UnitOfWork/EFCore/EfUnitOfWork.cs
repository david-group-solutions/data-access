using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DavidGroup.Core.DataAccess.Sql.UnitOfWork.EFCore;

/// <summary>
/// An Entity Framework Core implementation of the Unit of Work pattern.
/// </summary>
/// <typeparam name="TContext">
/// The type of the <see cref="DbContext"/> used for managing database operations.
/// </typeparam>
public class EfUnitOfWork<TContext>(TContext context) : IEfUnitOfWork<TContext>, IDisposable
    where TContext : DbContext
{
    /// <summary>
    /// Gets the <see cref="DbContext"/> instance associated with the current unit of work.
    /// </summary>
    public TContext Context { get; } = context;

    /// <summary>
    /// Gets the currently active database transaction if one exists.
    /// </summary>
    /// <remarks>
    /// This property may return <see langword="null"/> if a transaction has not been started
    /// using <see cref="CreateTransactionAsync(CancellationToken)"/>.
    /// </remarks>
    public IDbContextTransaction? Transaction { get; private set; }

    private bool _disposed = false;

    /// <summary>
    /// Begins a new database transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when there is an active transaction.</exception>
    /// <remarks>
    /// This method should be called before performing a series of operations
    /// that must either all succeed or all fail together.
    /// </remarks>
    public async Task CreateTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EfUnitOfWork<>));

        if (Transaction is not null)
            throw new InvalidOperationException("A transaction is already in progress.");

        Transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Commits the current transaction asynchronously, finalizing all changes made
    /// during the unit of work.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when there is no active transaction.</exception>
    /// <remarks>
    /// Once committed, the transaction cannot be rolled back.
    /// Call this method only after all operations within the unit of work have succeeded.
    /// </remarks>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EfUnitOfWork<>));

        if (Transaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        await Transaction.CommitAsync(cancellationToken);
        await Transaction.DisposeAsync();

        Transaction = null;
    }

    /// <summary>
    /// Rolls back the current transaction asynchronously, reverting all changes
    /// made during the unit of work.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when there is no active transaction.</exception>
    /// <remarks>
    /// This method should be invoked when an error occurs or when an operation fails,
    /// to ensure data consistency by undoing pending changes.
    /// </remarks>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EfUnitOfWork<>));

        if (Transaction is null)
            throw new InvalidOperationException("No active transaction to rollback.");

        await Transaction.RollbackAsync(cancellationToken);
        await Transaction.DisposeAsync();

        Transaction = null;
    }

    /// <summary>
    /// Saves all pending changes tracked by the current <see cref="DbContext"/> to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous save operation. The task result contains the number of state entries
    /// written to the database.
    /// </returns>
    /// <remarks>
    /// This method should be called to persist changes.
    /// </remarks>
    public async Task<int> SaveAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EfUnitOfWork<>));

        return await Context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// The typical "Dispose Pattern" implementation.
    /// https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The typical "Dispose Pattern" implementation.
    /// https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            Transaction?.Dispose();
            Transaction = null;
            Context.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// The typical "Dispose Pattern" implementation.
    /// https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern
    /// </summary>
    ~EfUnitOfWork() => Dispose(false);
}
