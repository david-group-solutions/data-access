using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccess.Benchmarks.Assets;

public class BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options)
    : DbContext(options)
{
    public DbSet<BenchmarkEntity> BenchmarkEntities { get; init; } = null!;
}
