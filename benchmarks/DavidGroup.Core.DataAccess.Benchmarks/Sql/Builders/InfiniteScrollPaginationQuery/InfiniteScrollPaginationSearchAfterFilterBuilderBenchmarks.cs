using System.Linq.Expressions;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

using DavidGroup.Core.DataAccess.Benchmarks.Assets;
using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;
using DavidGroup.Core.DataAccess.Sql.Builders;
using DavidGroup.Core.DataAccess.Sql.Builders.InfiniteScrollPaginationQuery;

namespace DavidGroup.Core.DataAccess.Benchmarks.Sql.Builders.InfiniteScrollPaginationQuery;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.Declared)]
public class InfiniteScrollPaginationSearchAfterFilterBuilderBenchmarks
{
    // ------------------------------------------------------------------
    // Build
    // ------------------------------------------------------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Build")]
    public Expression<Func<BenchmarkEntity, bool>> BuildSingleColOrder()
    {
        IReadOnlyList<OrderingSpecification<BenchmarkEntity>> orderingSpecifications =
        [
            new(entity => entity.Id, false)
        ];

        DynamicCursor cursor = new([5]);

        return InfiniteScrollPaginationSearchAfterFilterBuilder.Build(orderingSpecifications, cursor);
    }

    [Benchmark]
    [BenchmarkCategory("Build")]
    public Expression<Func<BenchmarkEntity, bool>> BuildMultipleColOrder()
    {
        IReadOnlyList<OrderingSpecification<BenchmarkEntity>> orderingSpecifications =
        [
            new(entity => entity.Year, true),
            new(entity => entity.Name, false),
            new(entity => entity.Id, true)
        ];

        DynamicCursor cursor = new([2026, "Bob", 5]);

        return InfiniteScrollPaginationSearchAfterFilterBuilder.Build(orderingSpecifications, cursor);
    }
}
