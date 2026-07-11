using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

using DavidGroup.Core.DataAccess.Pagination.InfiniteScroll;

namespace DavidGroup.Core.DataAccess.Benchmarks.Pagination.InfiniteScroll;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.Declared)]
public class DynamicCursorTokenizerBenchmarks
{
    private readonly DynamicCursor _cursor = new([
        true,
        "Hello World",
        20_011.35,
        new DateTime(2026, 7, 11, 16, 56, 45),
        15_950
    ]);

    private string _cursorToken = null!;

    [GlobalSetup]
    public void Setup()
    {
        _cursorToken = _cursor.Encode();
    }

    [Benchmark]
    public string DynamicCursorTokenizer_Encode() => _cursor.Encode();

    [Benchmark]
    public DynamicCursor DynamicCursorTokenizer_Decode() => DynamicCursorTokenizer.Decode(_cursorToken)!;
}
