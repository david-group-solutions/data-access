using DavidGroup.Core.DataAccess.Sql.Entities;

namespace DavidGroup.Core.DataAccess.Benchmarks.Assets;

public class BenchmarkEntity : Entity<int>
{
    public string Name { get; set; } = null!;
    public int Year { get; set; }
}
