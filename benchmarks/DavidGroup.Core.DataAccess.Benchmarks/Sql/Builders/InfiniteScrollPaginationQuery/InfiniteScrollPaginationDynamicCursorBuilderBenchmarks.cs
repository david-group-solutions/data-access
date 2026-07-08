using System.Linq.Expressions;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

using DavidGroup.Core.DataAccess.Benchmarks.Assets;
using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Sql.Builders.InfiniteScrollPaginationQuery;

using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccess.Benchmarks.Sql.Builders.InfiniteScrollPaginationQuery;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.Declared)]
public class InfiniteScrollPaginationDynamicCursorBuilderBenchmarks
{
    public int RowCount { get; set; } = 100;

    private BenchmarkDbContext _dbContext = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        DbContextOptions<BenchmarkDbContext> dbContextOptions;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            dbContextOptions = new DbContextOptionsBuilder<BenchmarkDbContext>()
                .UseSqlServer($@"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog={Guid.NewGuid()};Integrated Security=True;Encrypt=False;")
                .Options;
        }
        else
        {
            string? host = Environment.GetEnvironmentVariable("BENCHMARK_SQLSERVER_HOST");
            string? username = Environment.GetEnvironmentVariable("BENCHMARK_SQLSERVER_USERNAME");
            string? password = Environment.GetEnvironmentVariable("BENCHMARK_SQLSERVER_PASSWORD");

            dbContextOptions = new DbContextOptionsBuilder<BenchmarkDbContext>()
                .UseSqlServer($"Server={host};Database={Guid.NewGuid()};User={username};Password={password};TrustServerCertificate=True;")
                .Options;
        }

        _dbContext = new BenchmarkDbContext(dbContextOptions);
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();

        // Fixed seed so RowCount comparisons are reproducible run to run.
        Random random = new(Seed: 1337);
        List<BenchmarkEntity> entities = Enumerable.Range(1, RowCount)
            .Select(i => new BenchmarkEntity
            {
                Name = $"Entity {i}",
                Year = random.Next(1990, 2075)
            })
            .ToList();

        await _dbContext.BenchmarkEntities.AddRangeAsync(entities);
        await _dbContext.SaveChangesAsync();
    }

    // ------------------------------------------------------------------
    // Build next cursor
    // ------------------------------------------------------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Build next cursor")]
    public async Task<DynamicCursor> BuildNextCursorSingleColOrder()
    {
        IQueryable<BenchmarkEntity> ordered = _dbContext.BenchmarkEntities.OrderBy(entity => entity.Id);
        List<Expression<Func<BenchmarkEntity, object>>> orderBy = [entity => entity.Id];

        return await InfiniteScrollPaginationDynamicCursorBuilder.BuildNextCursorAsync(ordered, orderBy, 10);
    }

    [Benchmark]
    [BenchmarkCategory("Build next cursor")]
    public async Task<DynamicCursor> BuildNextCursorMultiColOrder()
    {
        IQueryable<BenchmarkEntity> ordered = _dbContext.BenchmarkEntities
            .OrderBy(entity => entity.Year)
            .ThenBy(entity => entity.Id);

        List<Expression<Func<BenchmarkEntity, object>>> orderBy =
        [
            entity => entity.Year,
            entity => entity.Id
        ];

        return await InfiniteScrollPaginationDynamicCursorBuilder.BuildNextCursorAsync(ordered, orderBy, 10);
    }
}
