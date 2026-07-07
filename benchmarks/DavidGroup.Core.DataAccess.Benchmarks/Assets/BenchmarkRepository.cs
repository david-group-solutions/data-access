using DavidGroup.Core.DataAccess.Sql.Repositories;

namespace DavidGroup.Core.DataAccess.Benchmarks.Assets;

public class BenchmarkRepository(BenchmarkDbContext context)
    : BaseRepository<BenchmarkEntity, int>(context);
