using System.Data.Common;

using Microsoft.Data.SqlClient;

namespace DavidGroup.Core.DataAccess.Sql.UnitOfWork.ADO.NET;

/// <summary>
/// Defines the contract for an ADO.NET-based implementation of the Unit of Work pattern.
/// </summary>
/// <remarks>
/// This interface extends <see cref="IBaseUnitOfWork"/> to manage SQL connections and transactions
/// directly using <see cref="DbConnection"/> and <see cref="DbTransaction"/>.
/// It provides methods for opening connections and executing operations within a transactional context.
/// </remarks>
public interface IAdoNetUnitOfWork : IBaseUnitOfWork
{
    /// <summary>
    /// Gets the active <see cref="DbConnection"/> instance associated with the current unit of work.
    /// </summary>
    /// <remarks>
    /// The connection is typically opened using <see cref="OpenConnectionAsync(CancellationToken)"/>.
    /// </remarks>
    DbConnection Connection { get; }

    /// <summary>
    /// Gets the active <see cref="DbTransaction"/> instance associated with the current unit of work.
    /// </summary>
    /// <remarks>
    /// The transaction is created when <see cref="IBaseUnitOfWork.CreateTransactionAsync(CancellationToken)"/> is called.
    /// It can be committed or rolled back using the corresponding methods from the base interface.
    /// </remarks>
    DbTransaction Transaction { get; }

    /// <summary>
    /// Opens a new SQL database connection asynchronously if it is not already open.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method ensures that the <see cref="Connection"/> is available for executing commands.
    /// Calling this method multiple times will have no effect if the connection is already open.
    /// </remarks>
    Task OpenConnectionAsync(CancellationToken cancellationToken = default);

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
    Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default);
}
