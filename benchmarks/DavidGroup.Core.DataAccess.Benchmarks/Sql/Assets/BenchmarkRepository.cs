using DavidGroup.Core.DataAccess.Sql.Repositories;

namespace DavidGroup.Core.DataAccess.Benchmarks.Sql.Assets;

public class BenchmarkRepository(BenchmarkDbContext context)
    : BaseRepository<BenchmarkEntity, int>(context);
