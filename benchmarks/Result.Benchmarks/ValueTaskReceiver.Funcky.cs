using BenchmarkDotNet.Attributes;
using Funcky.Monads;

namespace Wyzely.Benchmarks;

public partial class ValueTaskReceiver
{
    private static readonly Exception FunckyRejected = new InvalidOperationException("Rejected.");

    // Funcky has no extension methods for ValueTask<Result<T>>: await, then the synchronous chain.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> FunckyLib() => (await new ValueTask<Result<int>>(Result.Ok(_number)))
        .Select(static x => x + 1)
        .SelectMany(static x => x > 0 ? Result.Ok(x) : Result<int>.Error(FunckyRejected))
        .SelectMany(static x => Result.Ok(x * 2))
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> FunckyRef() => (await new ValueTask<Result<Customer>>(Result.Ok(_customer)))
        .Select(static c => c.Name)
        .SelectMany(static n => n.Length > 0 ? Result.Ok(n) : Result<string>.Error(FunckyRejected))
        .SelectMany(static n => Result.Ok(n))
        .Match(static n => n.Length, static _ => -1);
}
