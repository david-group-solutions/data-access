using System.Data;
using System.Data.Common;

using DavidGroup.Core.DataAccess.Sql.UnitOfWork.ADO.NET;

using Microsoft.Data.Sqlite;

namespace DavidGroup.Core.DataAccess.Tests.Sql.UnitOfWork.ADO.NET;

public class AdoNetUnitOfWorkTests
{
    public abstract class AdoNetUnitOfWorkTestBase : IAsyncLifetime
    {
        private SqliteConnection _connection = null!;

        protected readonly string TableName = $"[UnitOfWorkTest_{Guid.NewGuid():N}]";

        public async Task InitializeAsync()
        {
            _connection = new SqliteConnection("Data Source=UnitOfWorkTests;Mode=Memory;Cache=Shared");
            await _connection.OpenAsync();

            await using SqliteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = $"CREATE TABLE {TableName} (Id INT IDENTITY PRIMARY KEY, Name NVARCHAR(100) NOT NULL)";
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DisposeAsync()
        {
            await _connection.DisposeAsync();
        }

        protected async Task<long> CountRowsAsync()
        {
            _connection = new SqliteConnection("Data Source=UnitOfWorkTests;Mode=Memory;Cache=Shared");
            await _connection.OpenAsync();

            await using SqliteCommand cmd = _connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {TableName}";
            return (long)(await cmd.ExecuteScalarAsync())!;
        }

        protected async Task InsertRowAsync(AdoNetUnitOfWork uow, string name)
        {
            await uow.OpenConnectionAsync();

            await using DbCommand cmd = uow.Connection.CreateCommand();

            cmd.Transaction = uow.Transaction;

            cmd.CommandText = $"INSERT INTO {TableName} (Name) VALUES (@name)";
            cmd.Parameters.Add(new SqliteParameter { ParameterName = "@name", SqliteType = SqliteType.Text, Direction = ParameterDirection.Input, Value = name });

            await cmd.ExecuteNonQueryAsync();
        }

        protected static AdoNetUnitOfWork CreateUnitOfWork()
            => new(() => new SqliteConnection("Data Source=UnitOfWorkTests;Mode=Memory;Cache=Shared"));
    }

    // =============================================================================
    // Guard clauses
    // =============================================================================

    public class GuardClauseTests : AdoNetUnitOfWorkTestBase
    {
        [Fact]
        public void Connection_Throws_InvalidOperationException_Before_Open()
        {
            // Arrange
            using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act, Assert
            Assert.Throws<InvalidOperationException>(() => _ = uow.Connection);
        }

        [Fact]
        public void Connection_Exception_Message_IsCorrect()
        {
            // Arrange
            using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act, Assert
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => _ = uow.Connection);

            Assert.Equal("Connection is not initialized.", ex.Message);
        }

        [Fact]
        public void Transaction_Throws_InvalidOperationException_Before_Create()
        {
            // Arrange
            using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act, Assert
            Assert.Throws<InvalidOperationException>(() => _ = uow.Transaction);
        }

        [Fact]
        public void Transaction_Exception_Message_IsCorrect()
        {
            // Arrange
            using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act, Assert
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => _ = uow.Transaction);

            Assert.Equal("Transaction is not initialized.", ex.Message);
        }

        [Fact]
        public async Task CreateTransactionAsync_Throws_When_Transaction_Already_Active()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();
            await uow.CreateTransactionAsync();

            // Act, Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CreateTransactionAsync());
        }

        [Fact]
        public async Task CreateTransactionAsync_Exception_Message_IsCorrect()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();
            await uow.CreateTransactionAsync();

            // Act
            InvalidOperationException ex =
                await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CreateTransactionAsync());

            // Assert
            Assert.Equal("A transaction is already in progress.", ex.Message);
        }

        [Fact]
        public async Task CommitTransactionAsync_Throws_When_No_Active_Transaction()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act, Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitTransactionAsync());
        }

        [Fact]
        public async Task CommitTransactionAsync_Exception_Message_IsCorrect()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act, Assert
            InvalidOperationException ex =
                await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitTransactionAsync());

            Assert.Equal("No active transaction to commit.", ex.Message);
        }

        [Fact]
        public async Task RollbackTransactionAsync_Throws_When_No_Active_Transaction()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act, Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.RollbackTransactionAsync());
        }

        [Fact]
        public async Task RollbackTransactionAsync_Exception_Message_IsCorrect()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            InvalidOperationException ex =
                await Assert.ThrowsAsync<InvalidOperationException>(() => uow.RollbackTransactionAsync());

            // Assert
            Assert.Equal("No active transaction to rollback.", ex.Message);
        }

        [Fact]
        public async Task ExecuteAsync_Throws_InvalidOperationException_When_Transaction_Already_Active()
        {
            // Since the guard fires synchronously before any awaited I/O, we trigger
            // it via a fake action that attempts a nested ExecuteAsync.

            // Arrange
            AdoNetUnitOfWork uow = CreateUnitOfWork();

            try
            {
                // Act, Assert
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    uow.ExecuteAsync(async () =>
                    {
                        // Nested call while outer transaction is active
                        await uow.ExecuteAsync(() => Task.CompletedTask);
                    }));
            }
            finally
            {
                await uow.DisposeAsync();
            }
        }

        [Fact]
        public async Task ExecuteAsync_Throws_InvalidOperationException_When_Transaction_Already_Active_And_Message_IsCorrect()
        {
            // Since the guard fires synchronously before any awaited I/O, we trigger
            // it via a fake action that attempts a nested ExecuteAsync.

            // Arrange
            AdoNetUnitOfWork uow = CreateUnitOfWork();

            try
            {
                // Act, Assert
                InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    uow.ExecuteAsync(async () =>
                    {
                        // Nested call while outer transaction is active
                        await uow.ExecuteAsync(() => Task.CompletedTask);
                    }));

                Assert.Equal("A transaction is already in progress.", ex.Message);
            }
            finally
            {
                await uow.DisposeAsync();
            }
        }
    }

    // =============================================================================
    // Open connection
    // =============================================================================

    public class OpenConnectionTests : AdoNetUnitOfWorkTestBase
    {
        [Fact]
        public async Task OpenConnectionAsync_Makes_Connection_Available()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            await uow.OpenConnectionAsync();

            // Assert
            Assert.Equal(ConnectionState.Open, uow.Connection.State);
        }

        [Fact]
        public async Task OpenConnectionAsync_Is_Idempotent_When_Already_Open()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            await uow.OpenConnectionAsync();

            // Act
            Exception? ex = await Record.ExceptionAsync(() => uow.OpenConnectionAsync());

            // Assert
            Assert.Null(ex);
            Assert.Equal(ConnectionState.Open, uow.Connection.State);
        }
    }

    // =============================================================================
    // CreateTransactionAsync
    // =============================================================================

    public class CreateTransactionTests : AdoNetUnitOfWorkTestBase
    {
        [Fact]
        public async Task CreateTransactionAsync_Makes_Transaction_Available()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            await uow.CreateTransactionAsync();

            // Assert
            Assert.NotNull(uow.Transaction);
        }

        [Fact]
        public async Task CreateTransactionAsync_Opens_Connection_If_Not_Already_Open()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            await uow.CreateTransactionAsync();

            // Assert
            Assert.Equal(ConnectionState.Open, uow.Connection.State);
        }

        [Fact]
        public async Task Can_Create_New_Transaction_After_Commit()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            await uow.CreateTransactionAsync();
            await uow.CommitTransactionAsync();

            // Act
            Exception? ex = await Record.ExceptionAsync(() => uow.CreateTransactionAsync());

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public async Task Can_Create_New_Transaction_After_Rollback()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            await uow.CreateTransactionAsync();
            await uow.RollbackTransactionAsync();

            // Act
            Exception? ex = await Record.ExceptionAsync(() => uow.CreateTransactionAsync());

            // Assert
            Assert.Null(ex);
            await uow.RollbackTransactionAsync();
        }
    }

    // =============================================================================
    // CommitTransactionAsync
    // =============================================================================

    public class CommitTransactionTests : AdoNetUnitOfWorkTestBase
    {
        [Fact]
        public async Task CommitTransactionAsync_Persists_Data()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            await uow.CreateTransactionAsync();
            await InsertRowAsync(uow, "committed");
            await uow.CommitTransactionAsync();

            // Assert
            Assert.Equal(1, await CountRowsAsync());
        }

        [Fact]
        public async Task Transaction_Is_Null_After_Commit()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            await uow.CreateTransactionAsync();
            await uow.CommitTransactionAsync();

            // Assert
            Assert.Throws<InvalidOperationException>(() => _ = uow.Transaction);
        }

        [Fact]
        public async Task Second_Commit_Without_New_Transaction_Throws()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            await uow.CreateTransactionAsync();
            await uow.CommitTransactionAsync();

            // Act, Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitTransactionAsync());
        }
    }

    // =============================================================================
    // RollbackTransactionAsync
    // =============================================================================

    public class RollbackTransactionTests : AdoNetUnitOfWorkTestBase
    {
        [Fact]
        public async Task RollbackTransactionAsync_Reverts_Data()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            await uow.CreateTransactionAsync();
            await InsertRowAsync(uow, "rolled-back");
            await uow.RollbackTransactionAsync();

            // Assert
            Assert.Equal(0, await CountRowsAsync());
        }

        [Fact]
        public async Task Transaction_Is_Null_After_Rollback()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            await uow.CreateTransactionAsync();
            await uow.RollbackTransactionAsync();

            // Assert
            Assert.Throws<InvalidOperationException>(() => _ = uow.Transaction);
        }

        [Fact]
        public async Task Only_Uncommitted_Data_Is_Rolled_Back()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            await uow.CreateTransactionAsync();
            await InsertRowAsync(uow, "committed");
            await uow.CommitTransactionAsync();

            await uow.CreateTransactionAsync();
            await InsertRowAsync(uow, "rolled-back");
            await uow.RollbackTransactionAsync();

            // Assert
            Assert.Equal(1, await CountRowsAsync());
        }

        [Fact]
        public async Task Second_Rollback_Without_New_Transaction_Throws()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            await uow.CreateTransactionAsync();
            await uow.RollbackTransactionAsync();

            // Act, Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.RollbackTransactionAsync());
        }
    }

    // =============================================================================
    // ExecuteAsync
    // =============================================================================

    public class ExecuteTests : AdoNetUnitOfWorkTestBase
    {
        [Fact]
        public async Task ExecuteAsync_Commits_On_Success()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            await uow.ExecuteAsync(async () =>
            {
                await InsertRowAsync(uow, "via-execute");
            });

            // Assert
            Assert.Equal(1, await CountRowsAsync());
        }

        [Fact]
        public async Task ExecuteAsync_Rolls_Back_On_Exception()
        {
            // Arrange
            AdoNetUnitOfWork uow = CreateUnitOfWork();

            try
            {
                // Act
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    uow.ExecuteAsync(async () =>
                    {
                        await InsertRowAsync(uow, "should-rollback");

                        throw new InvalidOperationException("simulated failure");
                    }));

                // Assert
                Assert.Equal(0, await CountRowsAsync());
            }
            finally
            {
                await uow.DisposeAsync();
            }
        }

        [Fact]
        public async Task ExecuteAsync_Rethrows_Action_Exception()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();
            ApplicationException originalException = new("boom");

            // Act, Assert
            ApplicationException thrown = await Assert.ThrowsAsync<ApplicationException>(() =>
                uow.ExecuteAsync(() => throw originalException));

            Assert.Same(originalException, thrown);
        }

        [Fact]
        public async Task ExecuteAsync_Clears_Transaction_After_Success()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            await uow.ExecuteAsync(() => Task.CompletedTask);

            // Assert
            Assert.Throws<InvalidOperationException>(() => _ = uow.Transaction);
        }

        [Fact]
        public async Task ExecuteAsync_Clears_Transaction_After_Exception()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            await Assert.ThrowsAsync<Exception>(() =>
                uow.ExecuteAsync(() => throw new Exception("fail")));

            // Assert
            Assert.Throws<InvalidOperationException>(() => _ = uow.Transaction);
        }

        [Fact]
        public async Task ExecuteAsync_Can_Be_Called_Again_After_Success()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            await uow.ExecuteAsync(() => Task.CompletedTask);

            Exception? ex = await Record.ExceptionAsync(() => uow.ExecuteAsync(() => Task.CompletedTask));

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public async Task ExecuteAsync_Can_Be_Called_Again_After_Failure()
        {
            // Arrange
            await using AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            await Assert.ThrowsAsync<Exception>(() =>
                uow.ExecuteAsync(() => throw new Exception("first")));

            Exception? ex = await Record.ExceptionAsync(() => uow.ExecuteAsync(() => Task.CompletedTask));

            // Assert
            Assert.Null(ex);
        }
    }

    // =============================================================================
    // Dispose
    // =============================================================================

    public class DisposeTests : AdoNetUnitOfWorkTestBase
    {
        [Fact]
        public void Dispose_Does_Not_Throw_When_Never_Opened()
        {
            // Arrange
            AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            Exception? ex = Record.Exception(uow.Dispose);

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public async Task DisposeAsync_Does_Not_Throw_When_Never_Opened()
        {
            // Arrange
            AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            Exception? ex = await Record.ExceptionAsync(async () => await uow.DisposeAsync());

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public void Dispose_Is_Idempotent()
        {
            // Arrange
            AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            uow.Dispose();

            Exception? ex = Record.Exception(uow.Dispose);

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public async Task DisposeAsync_Is_Idempotent()
        {
            // Arrange
            AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            await uow.DisposeAsync();

            Exception? ex = await Record.ExceptionAsync(async () => await uow.DisposeAsync());

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public void Connection_Throws_After_Dispose()
        {
            // Arrange
            AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            uow.Dispose();

            // Assert
            Assert.Throws<InvalidOperationException>(() => _ = uow.Connection);
        }

        [Fact]
        public void Transaction_Throws_After_Dispose()
        {
            // Arrange
            AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            uow.Dispose();

            // Assert
            Assert.Throws<InvalidOperationException>(() => _ = uow.Transaction);
        }

        [Fact]
        public void Using_Block_Disposes_Without_Throwing()
        {
            // Arrange, Act
            Exception? ex = Record.Exception(() =>
            {
                using AdoNetUnitOfWork uow = CreateUnitOfWork();
            });

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public async Task Await_Using_Block_Disposes_Without_Throwing()
        {
            // Arrange, Act
            Exception? ex = await Record.ExceptionAsync(async () =>
            {
                await using AdoNetUnitOfWork uow = CreateUnitOfWork();
            });

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public async Task Dispose_With_Open_Transaction_Does_Not_Throw()
        {
            // Arrange
            AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            await uow.CreateTransactionAsync();

            Exception? ex = Record.Exception(uow.Dispose);

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public async Task DisposeAsync_With_Open_Transaction_Does_Not_Throw()
        {
            // Arrange
            AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act
            await uow.CreateTransactionAsync();

            Exception? ex = await Record.ExceptionAsync(async () => await uow.DisposeAsync());

            // Assert
            Assert.Null(ex);
        }
    }
}
