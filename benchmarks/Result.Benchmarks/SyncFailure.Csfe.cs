using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;

namespace Wyzely.Benchmarks;

public partial class SyncFailure
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int Csfe() => Result.Success(_number)
        .Map(static x => x + 1)
        .Ensure(static x => x < 0, "Rejected.")
        .Bind(static x => Result.Success(x * 2))
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int CsfeRef() => Result.Success(_customer)
        .Map(static c => c.Name)
        .Ensure(static n => n.Length > 100, "Rejected.")
        .Bind(static n => Result.Success(n))
        .Match(static n => n.Length, static _ => -1);
}
