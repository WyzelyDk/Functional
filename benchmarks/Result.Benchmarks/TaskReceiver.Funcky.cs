using BenchmarkDotNet.Attributes;
using Funcky.Monads;

namespace Wyzely.Benchmarks;

public partial class TaskReceiver
{
    private static readonly Exception FunckyRejected = new InvalidOperationException("Rejected.");

    private readonly Task<Result<int>> _funckyTask = Task.FromResult(Result.Ok(Data.Number));
    private readonly Task<Result<Customer>> _funckyTaskRef = Task.FromResult(Result.Ok(Data.Customer));

    // Funcky has no extension methods for Task<Result<T>>: the task is awaited and the synchronous chain runs on the result.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> FunckyLib() => (await _funckyTask)
        .Select(static x => x + 1)
        .SelectMany(static x => x > 0 ? Result.Ok(x) : Result<int>.Error(FunckyRejected))
        .SelectMany(static x => Result.Ok(x * 2))
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> FunckyRef() => (await _funckyTaskRef)
        .Select(static c => c.Name)
        .SelectMany(static n => n.Length > 0 ? Result.Ok(n) : Result<string>.Error(FunckyRejected))
        .SelectMany(static n => Result.Ok(n))
        .Match(static n => n.Length, static _ => -1);
}
