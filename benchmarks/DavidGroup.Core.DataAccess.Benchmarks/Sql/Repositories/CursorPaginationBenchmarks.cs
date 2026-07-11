using BenchmarkDotNet.Attributes;

using DavidGroup.Core.DataAccess.Benchmarks.Sql.Assets;
using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.DataAccess.Sql.Builders;

namespace DavidGroup.Core.DataAccess.Benchmarks.Sql.Repositories;

public class CursorPaginationBenchmarks : RepositoryBenchmarksBase
{
    // ------------------------------------------------------------------
    // Cursor pagination — first page
    // ------------------------------------------------------------------
    // Single Column Ordering

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Cursor: first page")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorPagination_FirstPage() =>
        await CursorPagination_SingleColOrderAsync(searchAfter: null);

    [Benchmark]
    [BenchmarkCategory("Cursor: first page")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorPagination_FirstPage_WithNextCursorAutocreation() =>
        await CursorPagination_WithNextCursorAutocreationAsync(searchAfter: null, SingleColumnOrder);

    // Multiple Column Ordering

    [Benchmark]
    [BenchmarkCategory("Cursor: first page")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorPagination_FirstPage_MultiColOrder() =>
        await CursorPagination_MultiColOrderAsync(searchAfter: null);

    [Benchmark]
    [BenchmarkCategory("Cursor: first page")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorPagination_FirstPage_WithNextCursorAutocreation_MultiColOrder() =>
        await CursorPagination_WithNextCursorAutocreationAsync(searchAfter: null, MultiColumnOrder);

    // ------------------------------------------------------------------
    // Cursor pagination — deep page.
    // This is the scenario cursor pagination is actually meant to be
    // good at.
    // ------------------------------------------------------------------

    // Single Column Ordering

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Cursor: deep page")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorPagination_DeepPage() =>
        await CursorPagination_SingleColOrderAsync(searchAfter: MidSetCursorSingleColOrder);

    [Benchmark]
    [BenchmarkCategory("Cursor: deep page")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorPagination_DeepPage_WithNextCursorAutocreation() =>
        await CursorPagination_WithNextCursorAutocreationAsync(searchAfter: MidSetCursorSingleColOrder, SingleColumnOrder);

    // Multiple Column Ordering

    [Benchmark]
    [BenchmarkCategory("Cursor: deep page")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorPagination_DeepPage_MultiColOrder() =>
        await CursorPagination_MultiColOrderAsync(searchAfter: MidSetCursorMultiColOrder);

    [Benchmark]
    [BenchmarkCategory("Cursor: deep page")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorPagination_DeepPage_WithNextCursorAutocreation_MultiColOrder() =>
        await CursorPagination_WithNextCursorAutocreationAsync(searchAfter: MidSetCursorMultiColOrder, MultiColumnOrder);

    // ------------------------------------------------------------------
    // Shared functions
    // ------------------------------------------------------------------

    private async Task<InfinitePageData<BenchmarkEntity>> CursorPagination_SingleColOrderAsync(DynamicCursor? searchAfter)
    {
        InfinitePageOptions options = new(PageSize, searchAfter?.Encode());

        OperationResult<IReadOnlyList<OrderingSpecification<BenchmarkEntity>>> spec =
            OrderingSpecification<BenchmarkEntity>.Parse(SingleColumnOrder, null);

        return await BenchmarkRepository.GetAllAsync(
            options,
            orderingSpecifications: spec.Value!,
            selector: e => e,
            nextCursorSelector: r => [r.Id]
        );
    }

    private async Task<InfinitePageData<BenchmarkEntity>> CursorPagination_MultiColOrderAsync(DynamicCursor? searchAfter)
    {
        InfinitePageOptions options = new(PageSize, searchAfter?.Encode());

        OperationResult<IReadOnlyList<OrderingSpecification<BenchmarkEntity>>> spec =
            OrderingSpecification<BenchmarkEntity>.Parse(MultiColumnOrder, null);

        return await BenchmarkRepository.GetAllAsync(
            options,
            orderingSpecifications: spec.Value!,
            selector: e => e,
            nextCursorSelector: r => [r.Year, r.Name, r.Id]
        );
    }

    private async Task<InfinitePageData<BenchmarkEntity>> CursorPagination_WithNextCursorAutocreationAsync(
        DynamicCursor? searchAfter, string orderBy)
    {
        InfinitePageOptions options = new(PageSize, searchAfter?.Encode());

        OperationResult<IReadOnlyList<OrderingSpecification<BenchmarkEntity>>> spec =
            OrderingSpecification<BenchmarkEntity>.Parse(orderBy, null);

        return await BenchmarkRepository.GetAllAsync(
            options,
            orderingSpecifications: spec.Value!,
            selector: e => e
        );
    }
}
