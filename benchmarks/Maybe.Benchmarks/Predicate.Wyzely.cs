using BenchmarkDotNet.Attributes;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class Predicate
{
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ValueSome")]
    public bool Wyzely() => Maybe.Some(_number).Any(static x => x > 0);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ValueNone")]
    public bool WyzelyNone() => Maybe.None<int>().Any(static x => x > 0);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReferenceSome")]
    public bool WyzelyRef() => Maybe.Some(_name).Any(static n => n.Length > 0);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReferenceNone")]
    public bool WyzelyRefNone() => Maybe.None<string>().Any(static n => n.Length > 0);
}
