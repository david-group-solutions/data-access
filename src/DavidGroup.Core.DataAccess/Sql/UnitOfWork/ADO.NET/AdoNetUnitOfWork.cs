using System.Data;
using System.Data.Common;

using Microsoft.Data.SqlClient;

namespace DavidGroup.Core.DataAccess.Sql.UnitOfWork.ADO.NET;

/// <summary>
/// An ADO.NET-based implementation of the Unit of Work pattern.
/// </summary>
public class AdoNetUnitOfWork(Func<DbConnection> connectionFactory) : IAdoNetUnitOfWork, IDisposable, IAsyncDisposable
{
    private DbConnection? _connection;
    private DbTransaction? _transaction;

    private bool _disposed = false;

    /// <inheritdoc />
    public DbConnection Connection => _connection
                                      ?? throw new InvalidOperationException("Connection is not initialized.");

    /// <inheritdoc />
    public DbTransaction Transaction => _transaction
                                        ?? throw new InvalidOperationException("Transaction is not initialized.");

    /// <inheritdoc />
    public async Task OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AdoNetUnitOfWork));

        if (_connection == null || _connection.State == ConnectionState.Broken)
        {
            _connection?.Dispose();
            _connection = connectionFactory.Invoke();
        }

        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AdoNetUnitOfWork));

        if (_transaction is not null)
            throw new InvalidOperationException("A transaction is already in progress.");

        await OpenConnectionAsync(cancellationToken);

        _transaction = await _connection!.BeginTransactionAsync(cancellationToken);

        try
        {
            await action();
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await _transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;

            await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    /// <inheritdoc />
    public async Task CreateTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AdoNetUnitOfWork));

        if (_transaction is not null)
            throw new InvalidOperationException("A transaction is already in progress.");

        await OpenConnectionAsync(cancellationToken);

        _transaction = await _connection!.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AdoNetUnitOfWork));

        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        await _transaction.CommitAsync(cancellationToken);

        await _transaction.DisposeAsync();
        _transaction = null;

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AdoNetUnitOfWork));

        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to rollback.");

        await _transaction.RollbackAsync(cancellationToken);

        await _transaction.DisposeAsync();
        _transaction = null;

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    /// <summary>
    /// The typical "Dispose Pattern" implementation.
    /// https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The typical "Dispose Pattern" implementation.
    /// https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);

        Dispose(disposing: false);
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
            _transaction?.Dispose();
            _transaction = null;

            _connection?.Close();
            _connection?.Dispose();
            _connection = null;
        }

        _disposed = true;
    }

    /// <summary>
    /// The typical "Dispose Pattern" implementation.
    /// https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync().ConfigureAwait(false);

        if (_connection is not null)
        {
            await _connection.CloseAsync().ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        _connection = null;
        _transaction = null;
    }

    /// <summary>
    /// The typical "Dispose Pattern" implementation.
    /// https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern
    /// </summary>
    ~AdoNetUnitOfWork() => Dispose(false);
}
