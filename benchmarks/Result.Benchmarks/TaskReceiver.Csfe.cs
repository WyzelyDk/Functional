using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;

namespace Wyzely.Benchmarks;

public partial class TaskReceiver
{
    private readonly Task<Result<int>> _csfeTask = Task.FromResult(Result.Success(Data.Number));
    private readonly Task<Result<Customer>> _csfeTaskRef = Task.FromResult(Result.Success(Data.Customer));

    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> Csfe()
    {
        var result = await _csfeTask
            .Map(static x => x + 1)
            .Ensure(static x => x > 0, "Rejected.")
            .Bind(static x => Result.Success(x * 2));

        return result.Value;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> CsfeRef()
    {
        var result = await _csfeTaskRef
            .Map(static c => c.Name)
            .Ensure(static n => n.Length > 0, "Rejected.")
            .Bind(static n => Result.Success(n));

        return result.Value.Length;
    }
}
