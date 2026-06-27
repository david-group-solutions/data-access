using DavidGroup.Core.DataAccess.Sql.UnitOfWork.EFCore;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DavidGroup.Core.DataAccessTests.Sql.UnitOfWork.EFCore;

public class EfUnitOfWorkTests
{
    public abstract class EfUnitOfWorkTestBase : IAsyncLifetime
    {
        private SqliteConnection _connection = null!;
        protected TestDbContext DbContext = null!;

        public async Task InitializeAsync()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            await _connection.OpenAsync();

            DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new TestDbContext(options);
            await DbContext.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }

        protected EfUnitOfWork<TestDbContext> CreateUnitOfWork() => new(DbContext);

        protected class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        protected class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
        {
            public DbSet<TestEntity> Entities => Set<TestEntity>();
        }
    }

    // =============================================================================
    // Constructor / Context property
    // =============================================================================

    public class ConstructorTests : EfUnitOfWorkTestBase
    {
        [Fact]
        public void Context_Property_Returns_Injected_DbContext()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            // Assert
            Assert.Same(DbContext, uow.Context);
        }

        [Fact]
        public void Transaction_Is_Null_Initially()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            // Assert
            Assert.Null(uow.Transaction);
        }
    }

    // =============================================================================
    // CreateTransactionAsync
    // =============================================================================

    public class CreateTransactionTests : EfUnitOfWorkTestBase
    {
        [Fact]
        public async Task Creates_A_Non_Null_Transaction()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            // Act
            await uow.CreateTransactionAsync();

            // Assert
            Assert.NotNull(uow.Transaction);
        }

        [Fact]
        public async Task Transaction_Property_Is_Set_After_Create()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            // Act
            await uow.CreateTransactionAsync();

            // Assert
            Assert.IsType<IDbContextTransaction>(uow.Transaction, exactMatch: false);
        }

        [Fact]
        public async Task Throws_InvalidOperationException_When_Transaction_Already_Active()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();
            await uow.CreateTransactionAsync();

            // Act, Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CreateTransactionAsync());
        }

        [Fact]
        public async Task Exception_Message_Mentions_Already_In_Progress()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();
            await uow.CreateTransactionAsync();

            // Act
            InvalidOperationException ex =
                await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CreateTransactionAsync());

            // Assert
            Assert.Contains("already in progress", ex.Message);
        }

        [Fact]
        public async Task Can_Create_New_Transaction_After_Commit()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            await uow.CreateTransactionAsync();
            await uow.CommitTransactionAsync();

            // Act
            // Should not throw — previous transaction is gone
            Exception? ex = await Record.ExceptionAsync(() => uow.CreateTransactionAsync());

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public async Task Can_Create_New_Transaction_After_Rollback()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            await uow.CreateTransactionAsync();
            await uow.RollbackTransactionAsync();

            // Act
            Exception? ex = await Record.ExceptionAsync(() => uow.CreateTransactionAsync());

            // Assert
            Assert.Null(ex);
        }
    }

    // =============================================================================
    // CommitTransactionAsync
    // =============================================================================

    public class CommitTransactionTests : EfUnitOfWorkTestBase
    {
        [Fact]
        public async Task Throws_InvalidOperationException_When_No_Active_Transaction()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            // Act, Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitTransactionAsync());
        }

        [Fact]
        public async Task Exception_Message_Mentions_No_Active_Transaction()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            // Act
            InvalidOperationException ex =
                await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitTransactionAsync());

            // Assert
            Assert.Contains("No active transaction", ex.Message);
        }

        [Fact]
        public async Task Transaction_Is_Null_After_Commit()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();
            await uow.CreateTransactionAsync();

            // Act
            await uow.CommitTransactionAsync();

            // Assert
            Assert.Null(uow.Transaction);
        }

        [Fact]
        public async Task Committed_Data_Is_Persisted()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            // Act
            await uow.CreateTransactionAsync();

            DbContext.Entities.Add(new TestEntity { Name = "committed" });
            await uow.SaveAsync();

            await uow.CommitTransactionAsync();

            // Assert
            int count = await DbContext.Entities.CountAsync();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Second_Commit_Without_New_Transaction_Throws()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            await uow.CreateTransactionAsync();
            await uow.CommitTransactionAsync();

            // Act, Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.CommitTransactionAsync());
        }
    }

    // =============================================================================
    // RollbackTransactionAsync
    // =============================================================================

    public class RollbackTransactionTests : EfUnitOfWorkTestBase
    {
        [Fact]
        public async Task Throws_InvalidOperationException_When_No_Active_Transaction()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            // Act, Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.RollbackTransactionAsync());
        }

        [Fact]
        public async Task Exception_Message_Mentions_No_Active_Transaction()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            // Act
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => uow.RollbackTransactionAsync());

            // Assert
            Assert.Contains("No active transaction", ex.Message);
        }

        [Fact]
        public async Task Transaction_Is_Null_After_Rollback()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();
            await uow.CreateTransactionAsync();

            // Act
            await uow.RollbackTransactionAsync();

            // Assert
            Assert.Null(uow.Transaction);
        }

        [Fact]
        public async Task Rolled_Back_Data_Is_Not_Persisted()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            // Act
            await uow.CreateTransactionAsync();

            DbContext.Entities.Add(new TestEntity { Name = "rolled-back" });
            await uow.SaveAsync();

            await uow.RollbackTransactionAsync();

            // Assert
            int count = await DbContext.Entities.CountAsync();
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task Only_Uncommitted_Data_Is_Rolled_Back()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            // Act

            // First transaction — committed
            await uow.CreateTransactionAsync();
            DbContext.Entities.Add(new TestEntity { Name = "committed" });
            await uow.SaveAsync();
            await uow.CommitTransactionAsync();

            // Second transaction — rolled back
            await uow.CreateTransactionAsync();
            DbContext.Entities.Add(new TestEntity { Name = "rolled-back" });
            await uow.SaveAsync();
            await uow.RollbackTransactionAsync();

            // Assert
            List<TestEntity> entities = await DbContext.Entities.ToListAsync();

            Assert.Single(entities);
            Assert.Equal("committed", entities[0].Name);
        }

        [Fact]
        public async Task Second_Rollback_Without_New_Transaction_Throws()
        {
            // Arrange
            using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            await uow.CreateTransactionAsync();
            await uow.RollbackTransactionAsync();

            // Act, Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.RollbackTransactionAsync());
        }
    }

    // =============================================================================
    // Dispose
    // =============================================================================

    public class DisposeTests : EfUnitOfWorkTestBase
    {
        [Fact]
        public void Dispose_Does_Not_Throw_When_No_Transaction_Active()
        {
            // Arrange
            EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            // Act
            Exception? ex = Record.Exception(uow.Dispose);

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public async Task Dispose_Sets_Transaction_To_Null()
        {
            // Arrange
            EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();
            await uow.CreateTransactionAsync();

            // Act
            uow.Dispose();

            // Assert
            Assert.Null(uow.Transaction);
        }

        [Fact]
        public void Dispose_Can_Be_Called_Multiple_Times_Without_Throwing()
        {
            // Arrange
            EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();

            // Act
            uow.Dispose();

            Exception? ex = Record.Exception(uow.Dispose);

            // Assert
            Assert.Null(ex);
        }

        [Fact]
        public void Using_Block_Disposes_UnitOfWork_Without_Throwing()
        {
            // Arrange, Act
            Exception? ex = Record.Exception(() =>
            {
                using EfUnitOfWork<TestDbContext> uow = CreateUnitOfWork();
                // No operations — just verifying clean disposal via using
            });

            // Assert
            Assert.Null(ex);
        }
    }
}
