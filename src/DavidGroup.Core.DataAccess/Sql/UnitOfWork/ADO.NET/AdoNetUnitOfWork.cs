using System.Data;

using Microsoft.Data.SqlClient;

namespace DavidGroup.Core.DataAccess.Sql.UnitOfWork.ADO.NET;

/// <summary>
/// An ADO.NET-based implementation of the Unit of Work pattern.
/// </summary>
public class AdoNetUnitOfWork(string connectionString) : IAdoNetUnitOfWork, IDisposable, IAsyncDisposable
{
    private SqlConnection? _connection;
    private SqlTransaction? _transaction;

    private bool _disposed = false;

    /// <summary>
    /// Gets the active <see cref="SqlConnection"/> instance associated with the current unit of work.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no active connection found.</exception>
    /// <remarks>
    /// The connection is typically opened using <see cref="OpenConnectionAsync(CancellationToken)"/>.
    /// </remarks>
    public SqlConnection Connection => _connection ?? throw new InvalidOperationException("Connection is not initialized.");

    /// <summary>
    /// Gets the active <see cref="SqlTransaction"/> instance associated with the current unit of work.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no active transaction found.</exception>
    /// <remarks>
    /// The transaction is created when <see cref="CreateTransactionAsync(CancellationToken)"/> is called.
    /// It can be committed or rolled back using the corresponding methods from the base interface.
    /// </remarks>
    public SqlTransaction Transaction => _transaction ?? throw new InvalidOperationException("Transaction is not initialized.");

    /// <summary>
    /// Opens a new SQL database connection asynchronously if it is not already open.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method ensures that the <see cref="Connection"/> is available for executing commands.
    /// Calling this method multiple times will have no effect if the connection is already open.
    /// </remarks>
    public async Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection == null || _connection.State == ConnectionState.Broken)
        {
            _connection?.Dispose();
            _connection = new SqlConnection(connectionString);
        }

        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(cancellationToken);
    }

    /// <summary>
    /// Executes the specified asynchronous operation within a managed SQL transaction scope.
    /// </summary>
    /// <param name="action">
    /// The asynchronous delegate representing the operation to execute within the transaction.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous execution of the transactional operation.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method automatically opens the SQL connection (if not already open), begins a new transaction,
    /// executes the provided <paramref name="action"/>, and then commits or rolls back the transaction
    /// based on the operation's outcome.
    /// </para>
    /// <para>
    /// If the <paramref name="action"/> completes successfully, the transaction is committed.
    /// If an exception is thrown during execution, the transaction is rolled back and the exception is rethrown.
    /// </para>
    /// <para>
    /// A new transaction cannot be started while another is active; attempting to do so will result
    /// in an <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a transaction is already in progress when this method is called.
    /// </exception>
    /// <exception cref="SqlException">
    /// Thrown if a database error occurs during transaction initialization, execution, commit, or rollback.
    /// </exception>
    /// <example>
    /// The following example demonstrates how to use <see cref="ExecuteAsync"/>:
    /// <code>
    /// await unitOfWork.ExecuteAsync(async () =>
    /// {
    ///     await userRepository.AddAsync(user);
    ///     await orderRepository.AddAsync(order);
    /// });
    /// </code>
    /// </example>
    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("A transaction is already in progress.");

        await OpenConnectionAsync(cancellationToken);

        _transaction = (SqlTransaction)await _connection!.BeginTransactionAsync(cancellationToken);

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
        if (_transaction is not null)
            throw new InvalidOperationException("A transaction is already in progress.");

        await OpenConnectionAsync(cancellationToken);

        _transaction = (SqlTransaction)await _connection!.BeginTransactionAsync(cancellationToken);
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
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        await _transaction.CommitAsync(cancellationToken);

        await _transaction.DisposeAsync();
        _transaction = null;
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
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to rollback.");

        await _transaction.RollbackAsync(cancellationToken);

        await _transaction.DisposeAsync();
        _transaction = null;
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
