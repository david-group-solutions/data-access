using DavidGroup.Core.DataAccess.Sql.UnitOfWork.ADO.NET;

using Microsoft.Data.Sqlite;

namespace DavidGroup.Core.DataAccessTests.Sql.UnitOfWork.ADO.NET;

public class AdoNetUnitOfWorkTests
{
    private static AdoNetUnitOfWork CreateUnitOfWork() =>
        new(() => new SqliteConnection("DataSource=:memory:"));

    public class GuardClauseTests
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

            // Act, Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                uow.ExecuteAsync(async () =>
                {
                    // Nested call while outer transaction is active
                    await uow.ExecuteAsync(() => Task.CompletedTask);

                    await uow.DisposeAsync();
                }));
        }

        [Fact]
        public async Task ExecuteAsync_Throws_InvalidOperationException_When_Transaction_Already_Active_And_Message_IsCorrect()
        {
            // Since the guard fires synchronously before any awaited I/O, we trigger
            // it via a fake action that attempts a nested ExecuteAsync.

            // Arrange
            AdoNetUnitOfWork uow = CreateUnitOfWork();

            // Act, Assert
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                uow.ExecuteAsync(async () =>
                {
                    // Nested call while outer transaction is active
                    await uow.ExecuteAsync(() => Task.CompletedTask);

                    await uow.DisposeAsync();
                }));

            Assert.Equal("A transaction is already in progress.", ex.Message);
        }
    }

    // =============================================================================
    // Dispose
    // =============================================================================

    public class DisposeTests
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
