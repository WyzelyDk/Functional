using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;

namespace Wyzely.Benchmarks;

public partial class Conditional
{
    // CSharpFunctionalExtensions has MapIf, BindIf and EnsureNot, with the condition as a function of the value.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int Csfe() => Result.Success(_number)
        .MapIf(static x => x > 0, static x => x + 1)
        .BindIf(static x => x > 1, static x => Result.Success(x * 2))
        .EnsureNot(static x => x <= 0, "Rejected.")
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int CsfeRef() => Result.Success(_name)
        .MapIf(static n => n.Length > 0, static n => n.Trim())
        .BindIf(static n => n.Length > 1, static n => Result.Success(n.TrimEnd()))
        .EnsureNot(static n => n.Length <= 0, "Rejected.")
        .Match(static n => n.Length, static _ => -1);
}
