using Ardalis.Result;
using BenchmarkDotNet.Attributes;

namespace Wyzely.Benchmarks;

public partial class SyncToAsync
{
    private static readonly Task<Result<int>> ArdalisDone = Task.FromResult(Result<int>.Success(43));
    private static readonly Task<Result<string>> ArdalisDoneRef = Task.FromResult(Result<string>.Success(Data.Customer.Name));

    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> Ardalis()
    {
        var first = Result<int>.Success(_number);
        var second = first.IsSuccess ? await ArdalisDone : Result<int>.Error("Rejected.");

        return second.IsSuccess ? second.Value + 1 : -1;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> ArdalisRef()
    {
        var first = Result<Customer>.Success(_customer);
        var second = first.IsSuccess ? await ArdalisDoneRef : Result<string>.Error("Rejected.");

        return second.IsSuccess ? second.Value.Length : -1;
    }
}
