using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

using DavidGroup.Core.DataAccess.Benchmarks.Assets;
using DavidGroup.Core.DataAccess.Results.Generic;
using DavidGroup.Core.DataAccess.Sql.Builders;

namespace DavidGroup.Core.DataAccess.Benchmarks.Sql.Builders;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.Declared)]
public class OrderingSpecificationBenchmarks
{
    private const string SingleColumnOrder = "Id desc";
    private const string MultiColumnOrder = "Year desc, Name asc, Id desc";

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Parsing: single column")]
    public OperationResult<IReadOnlyList<OrderingSpecification<BenchmarkEntity>>> ParseSingleColumn()
        => OrderingSpecification<BenchmarkEntity>.Parse(SingleColumnOrder, null);

    [Benchmark]
    [BenchmarkCategory("Parsing: single column")]
    public OperationResult<IReadOnlyList<OrderingSpecification<BenchmarkEntity>>> ParseSingleColumnWithAllowList()
        => OrderingSpecification<BenchmarkEntity>.Parse(SingleColumnOrder, [e => e.Id]);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Parsing: multi-column")]
    public OperationResult<IReadOnlyList<OrderingSpecification<BenchmarkEntity>>> ParseMultiColumn()
        => OrderingSpecification<BenchmarkEntity>.Parse(MultiColumnOrder, null);

    [Benchmark]
    [BenchmarkCategory("Parsing: multi-column")]
    public OperationResult<IReadOnlyList<OrderingSpecification<BenchmarkEntity>>> ParseMultiColumnWithAllowList()
        => OrderingSpecification<BenchmarkEntity>.Parse(MultiColumnOrder, [e => e.Id, e => e.Name, e => e.Year]);
}
