using BenchmarkDotNet.Attributes;
using FluentResults;

namespace Wyzely.Benchmarks;

public partial class SyncFailure
{
    // FluentResults has no Ensure and its failures are objects: the failing Bind step creates a new failure on every call.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int Fluent()
    {
        var result = Result.Ok(_number)
            .Map(static x => x + 1)
            .Bind(static x => x < 0 ? Result.Ok(x) : Result.Fail<int>("Rejected."))
            .Bind(static x => Result.Ok(x * 2));

        return result.IsSuccess ? result.Value : -1;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int FluentRef()
    {
        var result = Result.Ok(_customer)
            .Map(static c => c.Name)
            .Bind(static n => n.Length > 100 ? Result.Ok(n) : Result.Fail<string>("Rejected."))
            .Bind(static n => Result.Ok(n));

        return result.IsSuccess ? result.Value.Length : -1;
    }
}
