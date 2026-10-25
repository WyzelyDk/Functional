using BenchmarkDotNet.Attributes;
using Wyzely.Functional;

namespace Wyzely.Benchmarks;

public partial class Conditional
{
    private static readonly Error WyzelyRejected = Error.Create("Rejected.");

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Value")]
    public int Wyzely() => Result.Success(_number)
        .MapIf(static x => x > 0, static x => x + 1)
        .BindIf(static x => x > 1, static x => Result.Success(x * 2))
        .EnsureNot(static x => x <= 0, WyzelyRejected)
        .Match(static x => x, static _ => -1);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Reference")]
    public int WyzelyRef() => Result.Success(_name)
        .MapIf(static n => n.Length > 0, static n => n.Trim())
        .BindIf(static n => n.Length > 1, static n => Result.Success(n.TrimEnd()))
        .EnsureNot(static n => n.Length <= 0, WyzelyRejected)
        .Match(static n => n.Length, static _ => -1);

    // The same chain written with Map, Bind and Ensure and a conditional expression, to show what the conditional methods cost or save.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int WyzelyWithoutIf() => Result.Success(_number)
        .Map(static x => x > 0 ? x + 1 : x)
        .Bind(static x => x > 1 ? Result.Success(x * 2) : Result.Success(x))
        .Ensure(static x => x > 0, WyzelyRejected)
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int WyzelyWithoutIfRef() => Result.Success(_name)
        .Map(static n => n.Length > 0 ? n.Trim() : n)
        .Bind(static n => n.Length > 1 ? Result.Success(n.TrimEnd()) : Result.Success(n))
        .Ensure(static n => n.Length > 0, WyzelyRejected)
        .Match(static n => n.Length, static _ => -1);
}
