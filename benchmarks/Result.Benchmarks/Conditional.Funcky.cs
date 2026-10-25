using BenchmarkDotNet.Attributes;
using Funcky.Monads;

namespace Wyzely.Benchmarks;

public partial class Conditional
{
    private static readonly Exception FunckyRejected = new InvalidOperationException("Rejected.");

    // Funcky has no conditional methods and no Ensure: the conditions are conditional expressions, and the check is a SelectMany step.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int FunckyLib() => Result.Ok(_number)
        .Select(static x => x > 0 ? x + 1 : x)
        .SelectMany(static x => x > 1 ? Result.Ok(x * 2) : Result.Ok(x))
        .SelectMany(static x => x > 0 ? Result.Ok(x) : Result<int>.Error(FunckyRejected))
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int FunckyRef() => Result.Ok(_name)
        .Select(static n => n.Length > 0 ? n.Trim() : n)
        .SelectMany(static n => n.Length > 1 ? Result.Ok(n.TrimEnd()) : Result.Ok(n))
        .SelectMany(static n => n.Length > 0 ? Result.Ok(n) : Result<string>.Error(FunckyRejected))
        .Match(static n => n.Length, static _ => -1);
}
