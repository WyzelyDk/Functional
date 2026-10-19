using BenchmarkDotNet.Attributes;
using Funcky.Monads;

namespace Wyzely.Benchmarks;

public partial class SyncToAsync
{
    private static readonly Exception FunckyRejected = new InvalidOperationException("Rejected.");

    private static readonly Task<Result<int>> FunckyDone = Task.FromResult(Result.Ok(43));
    private static readonly Task<Result<string>> FunckyDoneRef = Task.FromResult(Result.Ok(Data.Customer.Name));

    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> FunckyLib()
    {
        var first = Result.Ok(_number);
        var second = first.Match(static _ => true, static _ => false) ? await FunckyDone : Result<int>.Error(FunckyRejected);

        return second.Match(static x => x + 1, static _ => -1);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> FunckyRef()
    {
        var first = Result.Ok(_customer);
        var second = first.Match(static _ => true, static _ => false) ? await FunckyDoneRef : Result<string>.Error(FunckyRejected);

        return second.Match(static n => n.Length, static _ => -1);
    }
}
