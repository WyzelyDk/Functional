using BenchmarkDotNet.Attributes;
using Wyzely.Functional;

namespace Wyzely.Benchmarks;

public partial class SyncToAsync
{
    private static readonly Task<Result<int>> WyzelyDone = Task.FromResult(Result.Success(43));
    private static readonly Task<Result<string>> WyzelyDoneRef = Task.FromResult(Result.Success(Data.Customer.Name));

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> Wyzely()
    {
        var result = await Result.Success(_number)
            .Bind(static x => WyzelyDone)
            .Map(static x => x + 1);

        return result.GetValue();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> WyzelyRef()
    {
        var result = await Result.Success(_customer)
            .Bind(static c => WyzelyDoneRef)
            .Map(static n => n.Length);

        return result.GetValue();
    }

    // The alternative to Bind with an asynchronous delegate: check the result, await the step.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> WyzelyAwaited()
    {
        var first = Result.Success(_number);
        var second = first.IsSuccess ? await WyzelyDone : first;

        return second.Map(static x => x + 1).Match(static x => x, static _ => -1);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> WyzelyRefAwaited()
    {
        var first = Result.Success(_customer);
        var second = first.IsSuccess ? await WyzelyDoneRef : Result.Failure<string>(Error.Create("Rejected."));

        return second.Map(static n => n.Length).Match(static n => n, static _ => -1);
    }
}
