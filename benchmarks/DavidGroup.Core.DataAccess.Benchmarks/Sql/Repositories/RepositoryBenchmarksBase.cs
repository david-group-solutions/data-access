using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

using DavidGroup.Core.DataAccess.Benchmarks.Assets;
using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Sql.Builders;

using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccess.Benchmarks.Sql.Repositories;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.Declared)]
public abstract class RepositoryBenchmarksBase
{
    [Params(100, 10_000)]
    public int RowCount { get; set; }

    protected const string SingleColumnOrder = "Id desc";
    protected const string MultiColumnOrder = "Year desc, Name asc, Id desc";

    protected const int PageSize = 10;

    protected BenchmarkDbContext DbContext = null!;
    protected BenchmarkRepository BenchmarkRepository = null!;

    protected DynamicCursor? MidSetCursorSingleColOrder;
    protected DynamicCursor? MidSetCursorMultiColOrder;
    protected int MidPage;

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

        DbContext = new BenchmarkDbContext(dbContextOptions);
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();

        // Fixed seed so RowCount comparisons are reproducible run to run.
        Random random = new(Seed: 1337);
        List<BenchmarkEntity> entities = Enumerable.Range(1, RowCount)
            .Select(i => new BenchmarkEntity
            {
                Name = $"Entity {i}",
                Year = random.Next(1990, 2075)
            })
            .ToList();

        await DbContext.BenchmarkEntities.AddRangeAsync(entities);
        await DbContext.SaveChangesAsync();

        BenchmarkRepository = new BenchmarkRepository(DbContext);

        MidPage = Math.Max(1, RowCount / PageSize / 2);

        IReadOnlyList<OrderingSpecification<BenchmarkEntity>> singleColumnSpec =
            OrderingSpecification<BenchmarkEntity>.Parse(SingleColumnOrder, null).Value!;

        IReadOnlyList<OrderingSpecification<BenchmarkEntity>> multiColumnSpec =
            OrderingSpecification<BenchmarkEntity>.Parse(MultiColumnOrder, null).Value!;

        MidSetCursorSingleColOrder = await CalculateMidSetCursorAsync(singleColumnSpec, r => [r.Id]);
        MidSetCursorMultiColOrder = await CalculateMidSetCursorAsync(multiColumnSpec, r => [r.Year, r.Name, r.Id]);
    }

    private async Task<DynamicCursor?> CalculateMidSetCursorAsync(
        IReadOnlyList<OrderingSpecification<BenchmarkEntity>> orderBy,
        Func<BenchmarkEntity, object[]> nextCursorSelector)
    {
        InfinitePageData<BenchmarkEntity> page = await BenchmarkRepository.GetAllAsync(
            options: new InfinitePageOptions(PageSize, searchAfter: null),
            selector: e => e,
            orderingSpecifications: orderBy
        );

        for (int i = 0; i < MidPage && page.HasNextPage; i++)
        {
            page = await BenchmarkRepository.GetAllAsync(
                options: new InfinitePageOptions(PageSize, searchAfter: page.NextCursor),
                selector: e => e,
                orderingSpecifications: orderBy,
                nextCursorSelector: nextCursorSelector
            );
        }

        return page.NextCursor;
    }
}
