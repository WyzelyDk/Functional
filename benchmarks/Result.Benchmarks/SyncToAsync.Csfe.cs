using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;

namespace Wyzely.Benchmarks;

public partial class SyncToAsync
{
    private static readonly Task<Result<int>> CsfeDone = Task.FromResult(Result.Success(43));
    private static readonly Task<Result<string>> CsfeDoneRef = Task.FromResult(Result.Success(Data.Customer.Name));

    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> Csfe()
    {
        var result = await Result.Success(_number)
            .Bind(static x => CsfeDone)
            .Map(static x => x + 1);

        return result.Value;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> CsfeRef()
    {
        var result = await Result.Success(_customer)
            .Bind(static c => CsfeDoneRef)
            .Map(static n => n.Length);

        return result.Value;
    }
}
