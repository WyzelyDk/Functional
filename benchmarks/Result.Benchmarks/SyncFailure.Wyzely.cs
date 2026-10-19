using BenchmarkDotNet.Attributes;
using Wyzely.Functional;

namespace Wyzely.Benchmarks;

public partial class SyncFailure
{
    private static readonly Error WyzelyRejected = Error.Create("Rejected.");

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Value")]
    public int Wyzely() => Result.Success(_number)
        .Map(static x => x + 1)
        .Ensure(static x => x < 0, WyzelyRejected)
        .Bind(static x => Result.Success(x * 2))
        .Match(static x => x, static _ => -1);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Reference")]
    public int WyzelyRef() => Result.Success(_customer)
        .Map(static c => c.Name)
        .Ensure(static n => n.Length > 100, WyzelyRejected)
        .Bind(static n => Result.Success(n))
        .Match(static n => n.Length, static _ => -1);
}
