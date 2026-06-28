using DavidGroup.Core.DataAccess.Sql.Entities;

using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccess.Tests.Sql.Interceptors;

public class SoftDeletableEntity : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
}

public class TimedEntity : ITimedEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime ModifiedAtUtc { get; set; }
}

public class FullEntity : ISoftDeletable, ITimedEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime ModifiedAtUtc { get; set; }
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<SoftDeletableEntity> SoftDeletables => Set<SoftDeletableEntity>();
    public DbSet<TimedEntity> TimedEntities => Set<TimedEntity>();
    public DbSet<FullEntity> FullEntities => Set<FullEntity>();
}

internal static class DbContextFactory
{
    public static TestDbContext Create(params Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[] interceptors)
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // isolated per test
            .AddInterceptors(interceptors)
            .Options;

        return new TestDbContext(options);
    }
}
