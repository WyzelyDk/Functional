using BenchmarkDotNet.Attributes;
using FluentResults;

namespace Wyzely.Benchmarks;

public partial class Conditional
{
    // FluentResults has no conditional methods and no Ensure: the conditions are conditional expressions, and the check is a Bind step.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int Fluent()
    {
        var result = Result.Ok(_number)
            .Map(static x => x > 0 ? x + 1 : x)
            .Bind(static x => x > 1 ? Result.Ok(x * 2) : Result.Ok(x))
            .Bind(static x => x > 0 ? Result.Ok(x) : Result.Fail<int>("Rejected."));

        return result.IsSuccess ? result.Value : -1;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int FluentRef()
    {
        var result = Result.Ok(_name)
            .Map(static n => n.Length > 0 ? n.Trim() : n)
            .Bind(static n => n.Length > 1 ? Result.Ok(n.TrimEnd()) : Result.Ok(n))
            .Bind(static n => n.Length > 0 ? Result.Ok(n) : Result.Fail<string>("Rejected."));

        return result.IsSuccess ? result.Value.Length : -1;
    }
}
