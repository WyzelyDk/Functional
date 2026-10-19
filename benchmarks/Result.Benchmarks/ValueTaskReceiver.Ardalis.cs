using Ardalis.Result;
using BenchmarkDotNet.Attributes;

namespace Wyzely.Benchmarks;

public partial class ValueTaskReceiver
{
    // Ardalis.Result has no extension methods for ValueTask<Result<T>> and no Ensure: await, then a synchronous chain with a Bind check.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> Ardalis()
    {
        var result = (await new ValueTask<Result<int>>(Result<int>.Success(_number)))
            .Map(static x => x + 1)
            .Bind(static x => x > 0 ? Result<int>.Success(x) : Result<int>.Error("Rejected."))
            .Bind(static x => Result<int>.Success(x * 2));

        return result.Value;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> ArdalisRef()
    {
        var result = (await new ValueTask<Result<Customer>>(Result<Customer>.Success(_customer)))
            .Map(static c => c.Name)
            .Bind(static n => n.Length > 0 ? Result<string>.Success(n) : Result<string>.Error("Rejected."))
            .Bind(static n => Result<string>.Success(n));

        return result.Value.Length;
    }
}
