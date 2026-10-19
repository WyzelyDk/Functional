using BenchmarkDotNet.Attributes;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class SyncSuccess
{
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Value")]
    public int Wyzely() => Maybe.Some(_number)
        .Map(static x => x + 1)
        .Filter(static x => x > 0)
        .Bind(static x => Maybe.Some(x * 2))
        .Match(static x => x, static () => -1);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Reference")]
    public int WyzelyRef() => Maybe.Some(_customer)
        .Map(static c => c.Name)
        .Filter(static n => n.Length > 0)
        .Bind(static n => Maybe.Some(n))
        .Match(static n => n.Length, static () => -1);
}
