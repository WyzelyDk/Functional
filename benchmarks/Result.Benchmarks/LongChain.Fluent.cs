using BenchmarkDotNet.Attributes;
using FluentResults;

namespace Wyzely.Benchmarks;

public partial class LongChain
{
    // FluentResults has no Ensure: each check is a Bind step.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public int Fluent()
    {
        var result = Result.Ok(_number)
            .Map(static x => x + 1)
            .Bind(static x => x > 0 ? Result.Ok(x) : Result.Fail<int>(Rejected))
            .Bind(static x => Result.Ok(x * 2))
            .Map(static x => x - 3)
            .Bind(static x => x != 0 ? Result.Ok(x) : Result.Fail<int>(Rejected))
            .Bind(static x => Result.Ok(x + 10))
            .Map(static x => x * 2);

        return result.IsSuccess ? result.Value : -1;
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public int FluentRef()
    {
        var result = Result.Ok(_customer)
            .Map(static c => c.Name)
            .Bind(static n => n.Length > 0 ? Result.Ok(n) : Result.Fail<string>(Rejected))
            .Bind(static n => Result.Ok(n))
            .Map(static n => n)
            .Bind(static n => n[0] != ' ' ? Result.Ok(n) : Result.Fail<string>(Rejected))
            .Bind(static n => Result.Ok(n))
            .Map(static n => n);

        return result.IsSuccess ? result.Value.Length : -1;
    }
}
