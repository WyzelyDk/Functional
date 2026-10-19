using BenchmarkDotNet.Attributes;
using FluentResults;

namespace Wyzely.Benchmarks;

public partial class ValueTaskReceiver
{
    // FluentResults has no extension methods for ValueTask<Result<T>> and no Ensure: await, then a synchronous chain with a Bind check.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> Fluent()
    {
        var result = (await new ValueTask<Result<int>>(Result.Ok(_number)))
            .Map(static x => x + 1)
            .Bind(static x => x > 0 ? Result.Ok(x) : Result.Fail<int>("Rejected."))
            .Bind(static x => Result.Ok(x * 2));

        return result.Value;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> FluentRef()
    {
        var result = (await new ValueTask<Result<Customer>>(Result.Ok(_customer)))
            .Map(static c => c.Name)
            .Bind(static n => n.Length > 0 ? Result.Ok(n) : Result.Fail<string>("Rejected."))
            .Bind(static n => Result.Ok(n));

        return result.Value.Length;
    }
}
