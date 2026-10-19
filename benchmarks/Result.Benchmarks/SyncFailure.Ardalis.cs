using Ardalis.Result;
using BenchmarkDotNet.Attributes;

namespace Wyzely.Benchmarks;

public partial class SyncFailure
{
    // Ardalis.Result has no Ensure and its results are objects: the failing Bind step creates a new failure on every call.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int Ardalis()
    {
        var result = Result<int>.Success(_number)
            .Map(static x => x + 1)
            .Bind(static x => x < 0 ? Result<int>.Success(x) : Result<int>.Error("Rejected."))
            .Bind(static x => Result<int>.Success(x * 2));

        return result.IsSuccess ? result.Value : -1;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int ArdalisRef()
    {
        var result = Result<Customer>.Success(_customer)
            .Map(static c => c.Name)
            .Bind(static n => n.Length > 100 ? Result<string>.Success(n) : Result<string>.Error("Rejected."))
            .Bind(static n => Result<string>.Success(n));

        return result.IsSuccess ? result.Value.Length : -1;
    }
}
