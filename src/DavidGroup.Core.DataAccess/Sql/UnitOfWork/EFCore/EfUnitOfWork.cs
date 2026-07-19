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
    /// <inheritdoc />
    public TContext Context { get; } = context;

    /// <inheritdoc />
    public IDbContextTransaction? Transaction { get; private set; }

    private bool _disposed = false;

    /// <inheritdoc />
    public async Task CreateTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(EfUnitOfWork<>));

        if (Transaction is not null)
            throw new InvalidOperationException("A transaction is already in progress.");

        Transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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
