using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccessTests.Sql.UnitOfWork;

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> Entities => Set<TestEntity>();
}
