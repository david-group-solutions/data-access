using System.Linq.Dynamic.Core;

using BenchmarkDotNet.Attributes;

using DavidGroup.Core.DataAccess.Benchmarks.Assets;
using DavidGroup.Core.DataAccess.Pagination;
using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.DataAccess.Sql.Builders;

using Microsoft.EntityFrameworkCore;

namespace DavidGroup.Core.DataAccess.Benchmarks.Sql.Repositories;

public class OffsetPaginationBenchmarks : RepositoryBenchmarksBase
{
    // ------------------------------------------------------------------
    // Offset pagination — first page
    // ------------------------------------------------------------------

    // Single Column Ordering

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Offset: first page")]
    public async Task<PageData<BenchmarkEntity>> OffsetFirstPageHardcoded() =>
        await OffsetHardcodedAsync(page: 1);

    [Benchmark]
    [BenchmarkCategory("Offset: first page")]
    public async Task<PageData<BenchmarkEntity>> OffsetFirstPageDynamicLinq() =>
        await OffsetDynamicLinqAsync(page: 1, SingleColumnOrder);

    [Benchmark]
    [BenchmarkCategory("Offset: first page")]
    public async Task<PageData<BenchmarkEntity>> OffsetFirstPageDataAccessWithOrderingSpecifications() =>
        await OffsetDataAccessWithOrderingSpecificationsAsync(page: 1, SingleColumnOrder);

    // Multiple Column Ordering

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Offset: first page (multi-column)")]
    public async Task<PageData<BenchmarkEntity>> OffsetFirstPageHardcodedMultiColOrder() =>
        await OffsetHardcodedMultiColOrderAsync(page: 1);

    [Benchmark]
    [BenchmarkCategory("Offset: first page (multi-column)")]
    public async Task<PageData<BenchmarkEntity>> OffsetFirstPageDynamicLinqMultiColOrder() =>
        await OffsetDynamicLinqAsync(page: 1, MultiColumnOrder);

    [Benchmark]
    [BenchmarkCategory("Offset: first page (multi-column)")]
    public async Task<PageData<BenchmarkEntity>> OffsetFirstPageDataAccessWithOrderingSpecificationsMultiColOrder() =>
        await OffsetDataAccessWithOrderingSpecificationsAsync(page: 1, MultiColumnOrder);

    // ------------------------------------------------------------------
    // Offset pagination — deep page.
    // Skip() cost typically grows with how far into the table you page;
    // this is the classic argument for cursor pagination, and it's
    // invisible if you only ever benchmark page 1.
    // ------------------------------------------------------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Offset: deep page")]
    public async Task<PageData<BenchmarkEntity>> OffsetDeepPageHardcoded() =>
        await OffsetHardcodedAsync(page: MidPage);

    [Benchmark]
    [BenchmarkCategory("Offset: deep page")]
    public async Task<PageData<BenchmarkEntity>> OffsetDeepPageDynamicLinq() =>
        await OffsetDynamicLinqAsync(page: MidPage, SingleColumnOrder);

    [Benchmark]
    [BenchmarkCategory("Offset: deep page")]
    public async Task<PageData<BenchmarkEntity>> OffsetDeepPageDataAccessWithOrderingSpecifications() =>
        await OffsetDataAccessWithOrderingSpecificationsAsync(page: MidPage, SingleColumnOrder);

    // Multiple Column Ordering

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Offset: deep page (multi-column)")]
    public async Task<PageData<BenchmarkEntity>> OffsetDeepPageHardcodedMultiColOrder() =>
        await OffsetHardcodedMultiColOrderAsync(page: MidPage);

    [Benchmark]
    [BenchmarkCategory("Offset: deep page (multi-column)")]
    public async Task<PageData<BenchmarkEntity>> OffsetDeepPageDynamicLinqMultiColOrder() =>
        await OffsetDynamicLinqAsync(page: MidPage, MultiColumnOrder);

    [Benchmark]
    [BenchmarkCategory("Offset: deep page (multi-column)")]
    public async Task<PageData<BenchmarkEntity>> OffsetDeepPageDataAccessWithOrderingSpecificationsMultiColOrder() =>
        await OffsetDataAccessWithOrderingSpecificationsAsync(page: MidPage, MultiColumnOrder);

    // ------------------------------------------------------------------
    // Shared functions
    // ------------------------------------------------------------------

    private async Task<PageData<BenchmarkEntity>> OffsetHardcodedAsync(int page)
    {
        PageOptions options = new()
        {
            Page = page,
            Size = PageSize
        };

        List<BenchmarkEntity> entities = await DbContext.BenchmarkEntities
            .AsNoTracking()
            .OrderByDescending(e => e.Id)
            .Skip((options.Page - 1) * options.Size)
            .Take(options.Size)
            .ToListAsync();

        int count = await DbContext.BenchmarkEntities.CountAsync();

        return new PageData<BenchmarkEntity>(entities, count, options);
    }

    private async Task<PageData<BenchmarkEntity>> OffsetHardcodedMultiColOrderAsync(int page)
    {
        PageOptions options = new()
        {
            Page = page,
            Size = PageSize
        };

        List<BenchmarkEntity> entities = await DbContext.BenchmarkEntities
            .AsNoTracking()
            .OrderByDescending(e => e.Year)
            .ThenBy(e => e.Name)
            .ThenByDescending(e => e.Id)
            .Skip((options.Page - 1) * options.Size)
            .Take(options.Size)
            .ToListAsync();

        int count = await DbContext.BenchmarkEntities.CountAsync();

        return new PageData<BenchmarkEntity>(entities, count, options);
    }

    private async Task<PageData<BenchmarkEntity>> OffsetDynamicLinqAsync(int page, string orderBy)
    {
        PageOptions options = new()
        {
            Page = page,
            Size = PageSize
        };

        List<BenchmarkEntity> entities = await DbContext.BenchmarkEntities
            .AsNoTracking()
            .OrderBy(orderBy)
            .Skip((options.Page - 1) * options.Size)
            .Take(options.Size)
            .ToListAsync();

        int count = await DbContext.BenchmarkEntities.CountAsync();

        return new PageData<BenchmarkEntity>(entities, count, options);
    }

    private async Task<PageData<BenchmarkEntity>> OffsetDataAccessWithOrderingSpecificationsAsync(int page, string orderBy)
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
