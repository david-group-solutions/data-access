using BenchmarkDotNet.Attributes;

using DavidGroup.Core.DataAccess.Benchmarks.Sql.Assets;
using DavidGroup.Core.DataAccess.Pagination;
using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.DataAccess.Sql.Builders;

namespace DavidGroup.Core.DataAccess.Benchmarks.Sql.Repositories;

public class OffsetPaginationBenchmarks : RepositoryBenchmarksBase
{
    // ------------------------------------------------------------------
    // Offset pagination — first page
    // ------------------------------------------------------------------

    // Single Column Ordering

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Offset: first page")]
    public async Task<PageData<BenchmarkEntity>> OffsetPagination_FirstPage() =>
        await OffsetPaginationAsync(page: 1, SingleColumnOrder);

    // Multiple Column Ordering

    [Benchmark]
    [BenchmarkCategory("Offset: first page")]
    public async Task<PageData<BenchmarkEntity>> OffsetPagination_FirstPage_MultiColOrder() =>
        await OffsetPaginationAsync(page: 1, MultiColumnOrder);

    // ------------------------------------------------------------------
    // Offset pagination — deep page.
    // Skip() cost typically grows with how far into the table you page;
    // this is the classic argument for cursor pagination, and it's
    // invisible if you only ever benchmark page 1.
    // ------------------------------------------------------------------

    // Single Column Ordering

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Offset: deep page")]
    public async Task<PageData<BenchmarkEntity>> OffsetPagination_DeepPage() =>
        await OffsetPaginationAsync(page: MidPage, SingleColumnOrder);

    // Multiple Column Ordering

    [Benchmark]
    [BenchmarkCategory("Offset: deep page")]
    public async Task<PageData<BenchmarkEntity>> OffsetPagination_DeepPage_MultiColOrder() =>
        await OffsetPaginationAsync(page: MidPage, MultiColumnOrder);

    // ------------------------------------------------------------------
    // Shared functions
    // ------------------------------------------------------------------

    private async Task<PageData<BenchmarkEntity>> OffsetPaginationAsync(int page, string orderBy)
    {
        PageOptions options = new()
        {
            Page = page,
            Size = PageSize
        };

        OperationResult<IReadOnlyList<OrderingSpecification<BenchmarkEntity>>> spec =
            OrderingSpecification<BenchmarkEntity>.Parse(orderBy, null);

        return await BenchmarkRepository.GetAllAsync<BenchmarkEntity>(
            options,
            orderingSpecifications: spec.Value!
        );
    }
}
