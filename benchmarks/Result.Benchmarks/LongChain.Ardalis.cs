using Ardalis.Result;
using BenchmarkDotNet.Attributes;

namespace Wyzely.Benchmarks;

public partial class LongChain
{
    // Ardalis.Result has no Ensure: each check is a Bind step.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int Ardalis()
    {
        var result = Result<int>.Success(_number)
            .Map(static x => x + 1)
            .Bind(static x => x > 0 ? Result<int>.Success(x) : Result<int>.Error(Rejected))
            .Bind(static x => Result<int>.Success(x * 2))
            .Map(static x => x - 3)
            .Bind(static x => x != 0 ? Result<int>.Success(x) : Result<int>.Error(Rejected))
            .Bind(static x => Result<int>.Success(x + 10))
            .Map(static x => x * 2);

        return result.IsSuccess ? result.Value : -1;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int ArdalisRef()
    {
        var result = Result<Customer>.Success(_customer)
            .Map(static c => c.Name)
            .Bind(static n => n.Length > 0 ? Result<string>.Success(n) : Result<string>.Error(Rejected))
            .Bind(static n => Result<string>.Success(n))
            .Map(static n => n)
            .Bind(static n => n[0] != ' ' ? Result<string>.Success(n) : Result<string>.Error(Rejected))
            .Bind(static n => Result<string>.Success(n))
            .Map(static n => n);

        return result.IsSuccess ? result.Value.Length : -1;
    }
}
