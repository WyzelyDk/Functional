using BenchmarkDotNet.Attributes;
using Wyzely.Functional;

namespace Wyzely.Benchmarks;

public partial class LongChain
{
    private static readonly Error WyzelyRejected = Error.Create(Rejected);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Value")]
    public int Wyzely() => Result.Success(_number)
        .Map(static x => x + 1)
        .Ensure(static x => x > 0, WyzelyRejected)
        .Bind(static x => Result.Success(x * 2))
        .Map(static x => x - 3)
        .Ensure(static x => x != 0, WyzelyRejected)
        .Bind(static x => Result.Success(x + 10))
        .Map(static x => x * 2)
        .Match(static x => x, static _ => -1);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Reference")]
    public int WyzelyRef() => Result.Success(_customer)
        .Map(static c => c.Name)
        .Ensure(static n => n.Length > 0, WyzelyRejected)
        .Bind(static n => Result.Success(n))
        .Map(static n => n)
        .Ensure(static n => n[0] != ' ', WyzelyRejected)
        .Bind(static n => Result.Success(n))
        .Map(static n => n)
        .Match(static n => n.Length, static _ => -1);
}
