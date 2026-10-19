using BenchmarkDotNet.Attributes;
using FluentResults;

namespace Wyzely.Benchmarks;

public partial class SyncToAsync
{
    private static readonly Task<Result<int>> FluentDone = Task.FromResult(Result.Ok(43));
    private static readonly Task<Result<string>> FluentDoneRef = Task.FromResult(Result.Ok(Data.Customer.Name));

    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> Fluent()
    {
        var first = Result.Ok(_number);
        var second = first.IsSuccess ? await FluentDone : Result.Fail<int>("Rejected.");

        return second.IsSuccess ? second.Value + 1 : -1;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> FluentRef()
    {
        var first = Result.Ok(_customer);
        var second = first.IsSuccess ? await FluentDoneRef : Result.Fail<string>("Rejected.");

        return second.IsSuccess ? second.Value.Length : -1;
    }
}
