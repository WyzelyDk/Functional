using BenchmarkDotNet.Attributes;

namespace Wyzely.Functional.MaybeBenchmarks;

public partial class SyncToAsync
{
    private static readonly Task<Maybe<int>> WyzelyDone = Task.FromResult(Maybe.Some(43));
    private static readonly Task<Maybe<string>> WyzelyDoneRef = Task.FromResult(Maybe.Some(Data.Customer.Name));

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> Wyzely()
    {
        var maybe = await Maybe.Some(_number)
            .Bind(static x => WyzelyDone)
            .Map(static x => x + 1);

        return maybe.GetValue();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> WyzelyRef()
    {
        var maybe = await Maybe.Some(_customer)
            .Bind(static c => WyzelyDoneRef)
            .Map(static n => n.Length);

        return maybe.GetValue();
    }

    // The alternative to Bind with an asynchronous delegate: check the maybe, await the step.
    [Benchmark]
    [BenchmarkCategory("Value")]
    public async ValueTask<int> WyzelyAwaited()
    {
        var first = Maybe.Some(_number);
        var second = first.IsSome ? await WyzelyDone : first;

        return second.Map(static x => x + 1).Match(static x => x, static () => -1);
    }

    [Benchmark]
    [BenchmarkCategory("Reference")]
    public async ValueTask<int> WyzelyRefAwaited()
    {
        var first = Maybe.Some(_customer);
        var second = first.IsSome ? await WyzelyDoneRef : Maybe.None<string>();

        return second.Map(static n => n.Length).Match(static n => n, static () => -1);
    }
}
