using System.Linq.Dynamic.Core;

using BenchmarkDotNet.Attributes;

using DavidGroup.Core.DataAccess.Benchmarks.Assets;
using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.DataAccess.Sql.Builders;

using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccess.Benchmarks.Sql.Repositories;

public class CursorPaginationBenchmarks : RepositoryBenchmarksBase
{
    // ------------------------------------------------------------------
    // Cursor pagination — first page
    // ------------------------------------------------------------------

    // Single Column Ordering

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Cursor: first page")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorFirstPageHardcoded() =>
        await CursorHardcodedAsync(searchAfter: null);

    [Benchmark]
    [BenchmarkCategory("Cursor: first page")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorFirstPageDynamicLinq() =>
        await CursorDynamicLinqAsync(searchAfter: null);

    [Benchmark]
    [BenchmarkCategory("Cursor: first page")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorFirstPageDataAccess() =>
        await CursorDataAccessAsync(searchAfter: null);

    [Benchmark]
    [BenchmarkCategory("Cursor: first page")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorFirstPageDataAccessWithCursorAutocreation() =>
        await CursorDataAccessWithCursorAutocreationAsync(searchAfter: null, SingleColumnOrder);

    // Multiple Column Ordering

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Cursor: first page (multi-column)")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorFirstPageHardcodedMultiColOrder() =>
        await CursorHardcodedMultiColOrderAsync(searchAfter: null);

    [Benchmark]
    [BenchmarkCategory("Cursor: first page (multi-column)")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorFirstPageDynamicLinqMultiColOrder() =>
        await CursorDynamicLinqMultiColOrderAsync(searchAfter: null);

    [Benchmark]
    [BenchmarkCategory("Cursor: first page (multi-column)")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorFirstPageDataAccessMultiColOrder() =>
        await CursorDataAccessMultiColOrderAsync(searchAfter: null);

    [Benchmark]
    [BenchmarkCategory("Cursor: first page (multi-column)")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorFirstPageDataAccessWithCursorAutocreationMultiColOrder() =>
        await CursorDataAccessWithCursorAutocreationAsync(searchAfter: null, MultiColumnOrder);

    // ------------------------------------------------------------------
    // Cursor pagination — deep page.
    // This is the scenario cursor pagination is actually meant to be
    // good at.
    // ------------------------------------------------------------------

    // Single Column Ordering

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Cursor: deep page")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorDeepPageHardcoded() =>
        await CursorHardcodedAsync(searchAfter: MidSetCursorSingleColOrder);

    [Benchmark]
    [BenchmarkCategory("Cursor: deep page")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorDeepPageDynamicLinq() =>
        await CursorDynamicLinqAsync(searchAfter: MidSetCursorSingleColOrder);

    [Benchmark]
    [BenchmarkCategory("Cursor: deep page")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorDeepPageDataAccess() =>
        await CursorDataAccessAsync(searchAfter: MidSetCursorSingleColOrder);

    [Benchmark]
    [BenchmarkCategory("Cursor: deep page")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorDeepPageDataAccessWithCursorAutocreation() =>
        await CursorDataAccessWithCursorAutocreationAsync(searchAfter: MidSetCursorSingleColOrder, SingleColumnOrder);

    // Multiple Column Ordering

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Cursor: deep page (multi-column)")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorDeepPageHardcodedMultiColOrder() =>
        await CursorHardcodedMultiColOrderAsync(searchAfter: MidSetCursorMultiColOrder);

    [Benchmark]
    [BenchmarkCategory("Cursor: deep page (multi-column)")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorDeepPageDynamicLinqMultiColOrder() =>
        await CursorDynamicLinqMultiColOrderAsync(searchAfter: MidSetCursorMultiColOrder);

    [Benchmark]
    [BenchmarkCategory("Cursor: deep page (multi-column)")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorDeepPageDataAccessMultiColOrder() =>
        await CursorDataAccessMultiColOrderAsync(searchAfter: MidSetCursorMultiColOrder);

    [Benchmark]
    [BenchmarkCategory("Cursor: deep page (multi-column)")]
    public async Task<InfinitePageData<BenchmarkEntity>> CursorDeepPageDataAccessWithCursorAutocreationMultiColOrder() =>
        await CursorDataAccessWithCursorAutocreationAsync(searchAfter: MidSetCursorMultiColOrder, MultiColumnOrder);

    // ------------------------------------------------------------------
    // Shared functions
    // ------------------------------------------------------------------

    private async Task<InfinitePageData<BenchmarkEntity>> CursorHardcodedAsync(DynamicCursor? searchAfter)
    {
        InfinitePageOptions options = new(PageSize, searchAfter);

        List<BenchmarkEntity> temporaryResults = await DbContext.BenchmarkEntities
            .AsNoTracking()
            .OrderByDescending(e => e.Id)
            .Take(options.Size + 1)
            .ToListAsync();

        bool hasMore = temporaryResults.Count > options.Size;

        DynamicCursor? nextCursor = null;
        if (hasMore)
        {
            object[] nextValues = temporaryResults
                .Select(e => new object[] { e.Id })
                .Last();

            nextCursor = new DynamicCursor(nextValues);
        }

        return new InfinitePageData<BenchmarkEntity>(
            temporaryResults.Take(options.Size).ToList(),
            nextCursor,
            hasMore
        );
    }

    private async Task<InfinitePageData<BenchmarkEntity>> CursorHardcodedMultiColOrderAsync(DynamicCursor? searchAfter)
    {
        InfinitePageOptions options = new(PageSize, searchAfter);

        List<BenchmarkEntity> temporaryResults = await DbContext.BenchmarkEntities
            .AsNoTracking()
            .OrderByDescending(e => e.Year)
            .ThenBy(e => e.Name)
            .ThenByDescending(e => e.Id)
            .Take(options.Size + 1)
            .ToListAsync();

        bool hasMore = temporaryResults.Count > options.Size;

        DynamicCursor? nextCursor = null;
        if (hasMore)
        {
            object[] nextValues = temporaryResults
                .Select(e => new object[] { e.Year, e.Name, e.Id })
                .Last();

            nextCursor = new DynamicCursor(nextValues);
        }

        return new InfinitePageData<BenchmarkEntity>(
            temporaryResults.Take(options.Size).ToList(),
            nextCursor,
            hasMore
        );
    }

    private async Task<InfinitePageData<BenchmarkEntity>> CursorDynamicLinqAsync(DynamicCursor? searchAfter)
    {
        InfinitePageOptions options = new(PageSize, searchAfter);

        List<BenchmarkEntity> temporaryResults = await DbContext.BenchmarkEntities
            .AsNoTracking()
            .OrderBy(SingleColumnOrder)
            .Take(options.Size + 1)
            .ToListAsync();

        bool hasMore = temporaryResults.Count > options.Size;

        DynamicCursor? nextCursor = null;
        if (hasMore)
        {
            object[] nextValues = temporaryResults
                .Select(e => new object[] { e.Id })
                .Last();

            nextCursor = new DynamicCursor(nextValues);
        }

        return new InfinitePageData<BenchmarkEntity>(
            temporaryResults.Take(options.Size).ToList(),
            nextCursor,
            hasMore
        );
    }

    private async Task<InfinitePageData<BenchmarkEntity>> CursorDynamicLinqMultiColOrderAsync(DynamicCursor? searchAfter)
    {
        InfinitePageOptions options = new(PageSize, searchAfter);

        List<BenchmarkEntity> temporaryResults = await DbContext.BenchmarkEntities
            .AsNoTracking()
            .OrderBy(MultiColumnOrder)
            .Take(options.Size + 1)
            .ToListAsync();

        bool hasMore = temporaryResults.Count > options.Size;

        DynamicCursor? nextCursor = null;
        if (hasMore)
        {
            object[] nextValues = temporaryResults
                .Select(e => new object[] { e.Year, e.Name, e.Id })
                .Last();

            nextCursor = new DynamicCursor(nextValues);
        }

        return new InfinitePageData<BenchmarkEntity>(
            temporaryResults.Take(options.Size).ToList(),
            nextCursor,
            hasMore
        );
    }

    private async Task<InfinitePageData<BenchmarkEntity>> CursorDataAccessAsync(DynamicCursor? searchAfter)
    {
        InfinitePageOptions options = new(PageSize, searchAfter);

        OperationResult<IReadOnlyList<OrderingSpecification<BenchmarkEntity>>> spec =
            OrderingSpecification<BenchmarkEntity>.Parse(SingleColumnOrder, null);

        return await BenchmarkRepository.GetAllAsync(
            options,
            orderingSpecifications: spec.Value!,
            selector: e => e,
            nextCursorSelector: r => [r.Id]
        );
    }

    private async Task<InfinitePageData<BenchmarkEntity>> CursorDataAccessMultiColOrderAsync(DynamicCursor? searchAfter)
    {
        InfinitePageOptions options = new(PageSize, searchAfter);

        OperationResult<IReadOnlyList<OrderingSpecification<BenchmarkEntity>>> spec =
            OrderingSpecification<BenchmarkEntity>.Parse(MultiColumnOrder, null);

        return await BenchmarkRepository.GetAllAsync(
            options,
            orderingSpecifications: spec.Value!,
            selector: e => e,
            nextCursorSelector: r => [r.Year, r.Name, r.Id]
        );
    }

    private async Task<InfinitePageData<BenchmarkEntity>> CursorDataAccessWithCursorAutocreationAsync(
        DynamicCursor? searchAfter, string orderBy)
    {
        InfinitePageOptions options = new(PageSize, searchAfter);

        OperationResult<IReadOnlyList<OrderingSpecification<BenchmarkEntity>>> spec =
            OrderingSpecification<BenchmarkEntity>.Parse(orderBy, null);

        return await BenchmarkRepository.GetAllAsync(
            options,
            orderingSpecifications: spec.Value!,
            selector: e => e
        );
    }
}
