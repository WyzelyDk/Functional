using BenchmarkDotNet.Attributes;
using Funcky.Monads;

namespace Wyzely.Benchmarks;

public partial class LongChain
{
    private static readonly Exception FunckyRejected = new InvalidOperationException(Rejected);

    [Benchmark]
    [BenchmarkCategory("Value")]
    public int FunckyLib() => Result.Ok(_number)
        .Select(static x => x + 1)
        .SelectMany(static x => x > 0 ? Result.Ok(x) : Result<int>.Error(FunckyRejected))
        .SelectMany(static x => Result.Ok(x * 2))
        .Select(static x => x - 3)
        .SelectMany(static x => x != 0 ? Result.Ok(x) : Result<int>.Error(FunckyRejected))
        .SelectMany(static x => Result.Ok(x + 10))
        .Select(static x => x * 2)
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int FunckyRef() => Result.Ok(_customer)
        .Select(static c => c.Name)
        .SelectMany(static n => n.Length > 0 ? Result.Ok(n) : Result<string>.Error(FunckyRejected))
        .SelectMany(static n => Result.Ok(n))
        .Select(static n => n)
        .SelectMany(static n => n[0] != ' ' ? Result.Ok(n) : Result<string>.Error(FunckyRejected))
        .SelectMany(static n => Result.Ok(n))
        .Select(static n => n)
        .Match(static n => n.Length, static _ => -1);
}
