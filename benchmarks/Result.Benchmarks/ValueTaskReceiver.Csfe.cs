using BenchmarkDotNet.Attributes;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.ValueTasks;

namespace Wyzely.Benchmarks;

public partial class ValueTaskReceiver
{
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> Csfe()
    {
        var result = await new ValueTask<Result<int>>(Result.Success(_number))
            .Map(static x => x + 1)
            .Ensure(static x => x > 0, "Rejected.")
            .Bind(static x => Result.Success(x * 2));

        return result.Value;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> CsfeRef()
    {
        var result = await new ValueTask<Result<Customer>>(Result.Success(_customer))
            .Map(static c => c.Name)
            .Ensure(static n => n.Length > 0, "Rejected.")
            .Bind(static n => Result.Success(n));

        return result.Value.Length;
    }
}
