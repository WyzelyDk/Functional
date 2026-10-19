using BenchmarkDotNet.Attributes;
using Wyzely.Functional;

namespace Wyzely.Benchmarks;

public partial class ValueTaskReceiver
{
    private static readonly Error WyzelyRejected = Error.Create("Rejected.");

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> Wyzely()
    {
        var result = await new ValueTask<Result<int>>(Result.Success(_number))
            .Map(static x => x + 1)
            .Ensure(static x => x > 0, WyzelyRejected)
            .Bind(static x => Result.Success(x * 2));

        return result.GetValue();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> WyzelyRef()
    {
        var result = await new ValueTask<Result<Customer>>(Result.Success(_customer))
            .Map(static c => c.Name)
            .Ensure(static n => n.Length > 0, WyzelyRejected)
            .Bind(static n => Result.Success(n));

        return result.GetValue().Length;
    }

    // The alternative to chaining on the value task: await it first, then run the synchronous chain.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> WyzelyAwaited() => (await new ValueTask<Result<int>>(Result.Success(_number)))
        .Map(static x => x + 1)
        .Ensure(static x => x > 0, WyzelyRejected)
        .Bind(static x => Result.Success(x * 2))
        .Match(static x => x, static _ => -1);

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> WyzelyRefAwaited() => (await new ValueTask<Result<Customer>>(Result.Success(_customer)))
        .Map(static c => c.Name)
        .Ensure(static n => n.Length > 0, WyzelyRejected)
        .Bind(static n => Result.Success(n))
        .Match(static n => n.Length, static _ => -1);
}
