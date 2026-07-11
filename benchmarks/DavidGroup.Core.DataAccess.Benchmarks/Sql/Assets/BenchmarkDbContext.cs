using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccess.Benchmarks.Sql.Assets;

public class BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options)
    : DbContext(options)
{
    public DbSet<BenchmarkEntity> BenchmarkEntities { get; init; } = null!;
}
