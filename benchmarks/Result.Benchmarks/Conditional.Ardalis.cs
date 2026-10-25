using Ardalis.Result;
using BenchmarkDotNet.Attributes;

namespace Wyzely.Benchmarks;

public partial class Conditional
{
    // Ardalis.Result has no conditional methods and no Ensure: the conditions are conditional expressions, and the check is a Bind step.
    // Its Result<T> is a class.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int Ardalis()
    {
        var result = Result<int>.Success(_number)
            .Map(static x => x > 0 ? x + 1 : x)
            .Bind(static x => x > 1 ? Result<int>.Success(x * 2) : Result<int>.Success(x))
            .Bind(static x => x > 0 ? Result<int>.Success(x) : Result<int>.Error("Rejected."));

        return result.IsSuccess ? result.Value : -1;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int ArdalisRef()
    {
        var result = Result<string>.Success(_name)
            .Map(static n => n.Length > 0 ? n.Trim() : n)
            .Bind(static n => n.Length > 1 ? Result<string>.Success(n.TrimEnd()) : Result<string>.Success(n))
            .Bind(static n => n.Length > 0 ? Result<string>.Success(n) : Result<string>.Error("Rejected."));

        return result.IsSuccess ? result.Value.Length : -1;
    }
}
