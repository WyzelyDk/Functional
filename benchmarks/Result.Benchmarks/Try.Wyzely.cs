using BenchmarkDotNet.Attributes;
using Wyzely.Functional;

namespace Wyzely.Benchmarks;

public partial class Try
{
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ValueSuccess")]
    public int Wyzely() => Result.Try(static () => Parse("21")).Match(static x => x, static _ => -1);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ValueFailure")]
    public int WyzelyFailure() => Result.Try(static () => Parse("x")).Match(static x => x, static _ => -1);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReferenceSuccess")]
    public int WyzelyRef() => Result.Try(static () => Find("ada")).Match(static c => c.Name.Length, static _ => -1);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ReferenceFailure")]
    public int WyzelyRefFailure() => Result.Try(static () => Find(string.Empty)).Match(static c => c.Name.Length, static _ => -1);
}
